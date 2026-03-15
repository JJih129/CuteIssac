using CuteIssac.Core.Audio;
using UnityEngine;
using UnityEngine.Audio;

namespace CuteIssac.Data.Audio
{
    /// <summary>
    /// Designer-authored audio cue for one gameplay event.
    /// Swapping clips or playback settings here should not require gameplay code changes.
    /// </summary>
    [CreateAssetMenu(fileName = "AudioCueData", menuName = "CuteIssac/Data/Audio/Audio Cue")]
    public sealed class AudioCueData : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private GameAudioEventType eventType;

        [Header("Clips")]
        [SerializeField] private AudioClip[] clips;
        [SerializeField] private AudioMixerGroup outputMixerGroup;

        [Header("Playback")]
        [SerializeField] private bool playInWorldSpace = true;
        [SerializeField] [Range(0f, 1f)] private float volume = 1f;
        [SerializeField] private Vector2 pitchRange = Vector2.one;
        [SerializeField] [Range(0f, 1f)] private float spatialBlend = 1f;
        [SerializeField] [Min(0.1f)] private float minDistance = 1f;
        [SerializeField] [Min(0.1f)] private float maxDistance = 15f;

        public GameAudioEventType EventType => eventType;
        public AudioMixerGroup OutputMixerGroup => outputMixerGroup;
        public bool PlayInWorldSpace => playInWorldSpace;
        public float Volume => volume;
        public float SpatialBlend => spatialBlend;
        public float MinDistance => minDistance;
        public float MaxDistance => maxDistance;

        public bool TryPickClip(out AudioClip clip)
        {
            if (clips == null || clips.Length == 0)
            {
                clip = null;
                return false;
            }

            int selectedIndex = clips.Length == 1 ? 0 : Random.Range(0, clips.Length);
            clip = clips[selectedIndex];
            return clip != null;
        }

        public float GetPitch()
        {
            float minPitch = Mathf.Min(pitchRange.x, pitchRange.y);
            float maxPitch = Mathf.Max(pitchRange.x, pitchRange.y);
            return Mathf.Approximately(minPitch, maxPitch)
                ? minPitch
                : Random.Range(minPitch, maxPitch);
        }
    }
}
