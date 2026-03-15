using System.Collections.Generic;
using CuteIssac.Data.Audio;
using UnityEngine;

namespace CuteIssac.Core.Audio
{
    /// <summary>
    /// Central one-shot audio player.
    /// Logic scripts never touch AudioSource directly and instead go through GameAudioEvents.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GameAudioSystem : MonoBehaviour
    {
        [Header("Cue Library")]
        [SerializeField] private List<AudioCueData> audioCues = new();

        [Header("Playback")]
        [SerializeField] private Transform listenerAnchor;
        [SerializeField] private Transform oneShotRoot;
        [SerializeField] private bool logMissingCuesInEditor = true;

        private readonly Dictionary<GameAudioEventType, AudioCueData> _cueLookup = new();

        private void Awake()
        {
            RebuildLookup();
        }

        private void OnEnable()
        {
            GameAudioEvents.Requested += HandleAudioRequested;
        }

        private void OnDisable()
        {
            GameAudioEvents.Requested -= HandleAudioRequested;
        }

        private void HandleAudioRequested(AudioPlaybackRequest request)
        {
            if (!_cueLookup.TryGetValue(request.EventType, out AudioCueData cueData) || cueData == null)
            {
                if (logMissingCuesInEditor)
                {
                    UnityEngine.Debug.LogWarning($"GameAudioSystem has no cue assigned for {request.EventType}.", this);
                }

                return;
            }

            if (!cueData.TryPickClip(out AudioClip clip))
            {
                return;
            }

            PlayOneShot(cueData, clip, request);
        }

        private void PlayOneShot(AudioCueData cueData, AudioClip clip, AudioPlaybackRequest request)
        {
            GameObject audioObject = new($"{request.EventType}_Audio");
            audioObject.transform.SetParent(oneShotRoot != null ? oneShotRoot : transform, false);
            audioObject.transform.position = ResolvePlaybackPosition(cueData, request);

            AudioSource audioSource = audioObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.loop = false;
            audioSource.clip = clip;
            audioSource.outputAudioMixerGroup = cueData.OutputMixerGroup;
            audioSource.volume = cueData.Volume * Mathf.Max(0f, request.VolumeScale);
            audioSource.pitch = Mathf.Max(0.05f, cueData.GetPitch() * request.PitchScale);
            audioSource.spatialBlend = cueData.PlayInWorldSpace && request.UseWorldPosition ? cueData.SpatialBlend : 0f;
            audioSource.minDistance = cueData.MinDistance;
            audioSource.maxDistance = cueData.MaxDistance;
            audioSource.Play();

            float duration = audioSource.pitch > 0.0001f ? clip.length / audioSource.pitch : clip.length;
            Destroy(audioObject, duration + 0.1f);
        }

        private Vector3 ResolvePlaybackPosition(AudioCueData cueData, AudioPlaybackRequest request)
        {
            if (cueData.PlayInWorldSpace && request.UseWorldPosition)
            {
                return request.Position;
            }

            if (listenerAnchor != null)
            {
                return listenerAnchor.position;
            }

            Camera mainCamera = Camera.main;
            return mainCamera != null ? mainCamera.transform.position : transform.position;
        }

        private void RebuildLookup()
        {
            _cueLookup.Clear();

            for (int i = 0; i < audioCues.Count; i++)
            {
                AudioCueData cueData = audioCues[i];

                if (cueData == null)
                {
                    continue;
                }

                _cueLookup[cueData.EventType] = cueData;
            }
        }

        private void Reset()
        {
            listenerAnchor = Camera.main != null ? Camera.main.transform : transform;
        }

        private void OnValidate()
        {
            RebuildLookup();

            if (listenerAnchor == null && Camera.main != null)
            {
                listenerAnchor = Camera.main.transform;
            }
        }
    }
}
