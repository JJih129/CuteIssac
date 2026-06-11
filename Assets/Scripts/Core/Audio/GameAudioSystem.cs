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
        private readonly struct RuntimeCue
        {
            public RuntimeCue(string resourcePath, float volume, Vector2 pitchRange, bool playInWorldSpace, float spatialBlend)
                : this(new[] { resourcePath }, volume, pitchRange, playInWorldSpace, spatialBlend)
            {
            }

            public RuntimeCue(string[] resourcePaths, float volume, Vector2 pitchRange, bool playInWorldSpace, float spatialBlend)
                : this(resourcePaths, volume, pitchRange, playInWorldSpace, spatialBlend, 0f, false)
            {
            }

            public RuntimeCue(string resourcePath, float volume, Vector2 pitchRange, bool playInWorldSpace, float spatialBlend, float maxPlaybackDuration, bool interruptSameEvent)
                : this(new[] { resourcePath }, volume, pitchRange, playInWorldSpace, spatialBlend, maxPlaybackDuration, interruptSameEvent)
            {
            }

            public RuntimeCue(string[] resourcePaths, float volume, Vector2 pitchRange, bool playInWorldSpace, float spatialBlend, float maxPlaybackDuration, bool interruptSameEvent)
            {
                ResourcePaths = resourcePaths;
                Volume = volume;
                PitchRange = pitchRange;
                PlayInWorldSpace = playInWorldSpace;
                SpatialBlend = spatialBlend;
                MaxPlaybackDuration = maxPlaybackDuration;
                InterruptSameEvent = interruptSameEvent;
            }

            public string[] ResourcePaths { get; }
            public float Volume { get; }
            public Vector2 PitchRange { get; }
            public bool PlayInWorldSpace { get; }
            public float SpatialBlend { get; }
            public float MaxPlaybackDuration { get; }
            public bool InterruptSameEvent { get; }

            public float GetPitch()
            {
                float minPitch = Mathf.Min(PitchRange.x, PitchRange.y);
                float maxPitch = Mathf.Max(PitchRange.x, PitchRange.y);
                return Mathf.Approximately(minPitch, maxPitch)
                    ? minPitch
                    : Random.Range(minPitch, maxPitch);
            }
        }

        private static readonly Dictionary<GameAudioEventType, RuntimeCue> BuiltInCues = new()
        {
            { GameAudioEventType.PistolFired, new RuntimeCue("Sound/pistol_fire", 0.74f, new Vector2(0.97f, 1.04f), true, 0.15f) },
            { GameAudioEventType.ShotgunFired, new RuntimeCue("Sound/shotgun_fire", 0.86f, new Vector2(0.96f, 1.03f), true, 0.18f) },
            { GameAudioEventType.AssaultRifleFired, new RuntimeCue("Sound/assault_rifle_fire", 0.34f, new Vector2(0.98f, 1.05f), true, 0.13f, 0.16f, true) },
            { GameAudioEventType.SmgFired, new RuntimeCue("Sound/smg_fire", 0.28f, new Vector2(0.98f, 1.06f), true, 0.12f, 0.14f, true) },
            { GameAudioEventType.SniperFired, new RuntimeCue("Sound/sniper_fire", 0.9f, new Vector2(0.96f, 1.02f), true, 0.2f) },
            { GameAudioEventType.MinigunFired, new RuntimeCue("Sound/minigun_fire", 0.24f, new Vector2(0.99f, 1.04f), true, 0.11f, 0.12f, true) },
            { GameAudioEventType.RocketLauncherFired, new RuntimeCue("Sound/rocket_fire", 0.9f, new Vector2(0.96f, 1.02f), true, 0.22f) },
            { GameAudioEventType.WeaponReloadStarted, new RuntimeCue("Sound/weapon_reload", 0.72f, new Vector2(0.98f, 1.03f), false, 0f) },
            { GameAudioEventType.WeaponDryFired, new RuntimeCue("Sound/weapon_dry_fire", 0.74f, new Vector2(0.98f, 1.04f), false, 0f) },
            { GameAudioEventType.PlayerDamaged, new RuntimeCue("Sound/player_hit", 0.86f, Vector2.one, false, 0f) },
            { GameAudioEventType.RocketWallImpact, new RuntimeCue(new[] { "Sound/rocket_wall_impact", "Sound/rocket_fire" }, 0.88f, new Vector2(0.97f, 1.03f), true, 0.2f) }
        };

        [Header("Cue Library")]
        [SerializeField] private List<AudioCueData> audioCues = new();

        [Header("Playback")]
        [SerializeField] private Transform listenerAnchor;
        [SerializeField] private Transform oneShotRoot;
        [SerializeField] [Min(1)] private int initialOneShotPoolSize = 12;
        [SerializeField] [Min(1)] private int maximumOneShotSources = 32;
        [SerializeField] private bool logMissingCuesInEditor = true;
        [SerializeField] private bool applyMusicVolumeToLoopingSources = true;

        private readonly Dictionary<GameAudioEventType, AudioCueData> _cueLookup = new();
        private readonly Dictionary<GameAudioEventType, AudioClip[]> _runtimeClipLookup = new();
        private readonly List<AudioSource> _oneShotSources = new();
        private readonly List<OneShotPlaybackState> _oneShotStates = new();
        private readonly List<AudioSource> _musicSources = new();
        private readonly List<float> _musicSourceBaseVolumes = new();
        private float _musicVolume = 1f;
        private float _sfxVolume = 1f;

        private struct OneShotPlaybackState
        {
            public GameAudioEventType EventType;
            public float StopAtTime;
            public bool HasStopLimit;
        }

        private void Awake()
        {
            RebuildLookup();
            WarmOneShotPool();
            ApplyGlobalVolumes(s_globalMusicVolume, s_globalSfxVolume);
        }

        private void OnEnable()
        {
            GameAudioEvents.Requested += HandleAudioRequested;
            GameAudioEvents.StopRequested += HandleAudioStopRequested;
            ApplyGlobalVolumes(s_globalMusicVolume, s_globalSfxVolume);
        }

        private void OnDisable()
        {
            GameAudioEvents.Requested -= HandleAudioRequested;
            GameAudioEvents.StopRequested -= HandleAudioStopRequested;
        }

        private void Update()
        {
            float now = Time.unscaledTime;
            for (int i = 0; i < _oneShotSources.Count; i++)
            {
                AudioSource audioSource = _oneShotSources[i];
                if (audioSource == null || !audioSource.isPlaying)
                {
                    continue;
                }

                OneShotPlaybackState state = _oneShotStates[i];
                if (!state.HasStopLimit || now < state.StopAtTime)
                {
                    continue;
                }

                audioSource.Stop();
                state.HasStopLimit = false;
                _oneShotStates[i] = state;
            }
        }

        private void HandleAudioRequested(AudioPlaybackRequest request)
        {
            if (!_cueLookup.TryGetValue(request.EventType, out AudioCueData cueData) || cueData == null)
            {
                if (TryPlayBuiltInCue(request))
                {
                    return;
                }

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
            AudioSource audioSource = GetAvailableOneShotSource(request.EventType);
            if (audioSource == null)
            {
                return;
            }

            audioSource.transform.position = ResolvePlaybackPosition(cueData, request);
            audioSource.clip = clip;
            audioSource.outputAudioMixerGroup = cueData.OutputMixerGroup;
            audioSource.volume = cueData.Volume * Mathf.Max(0f, request.VolumeScale) * _sfxVolume;
            audioSource.pitch = Mathf.Max(0.05f, cueData.GetPitch() * request.PitchScale);
            audioSource.spatialBlend = cueData.PlayInWorldSpace && request.UseWorldPosition ? cueData.SpatialBlend : 0f;
            audioSource.minDistance = cueData.MinDistance;
            audioSource.maxDistance = cueData.MaxDistance;
            audioSource.Play();
            TrackOneShot(audioSource, request.EventType, 0f);
        }

        private bool TryPlayBuiltInCue(AudioPlaybackRequest request)
        {
            if (!BuiltInCues.TryGetValue(request.EventType, out RuntimeCue runtimeCue))
            {
                return false;
            }

            if (!TryGetRuntimeClip(request.EventType, runtimeCue.ResourcePaths, out AudioClip clip))
            {
                return false;
            }

            if (runtimeCue.InterruptSameEvent)
            {
                StopSourcesForEvent(request.EventType);
            }

            AudioSource audioSource = GetAvailableOneShotSource(request.EventType);
            if (audioSource == null)
            {
                return false;
            }

            audioSource.transform.position = ResolveRuntimePlaybackPosition(runtimeCue, request);
            audioSource.clip = clip;
            audioSource.outputAudioMixerGroup = null;
            audioSource.volume = runtimeCue.Volume * Mathf.Max(0f, request.VolumeScale) * _sfxVolume;
            audioSource.pitch = Mathf.Max(0.05f, runtimeCue.GetPitch() * request.PitchScale);
            audioSource.spatialBlend = runtimeCue.PlayInWorldSpace && request.UseWorldPosition ? runtimeCue.SpatialBlend : 0f;
            audioSource.minDistance = 1f;
            audioSource.maxDistance = 15f;
            audioSource.Play();
            TrackOneShot(audioSource, request.EventType, runtimeCue.MaxPlaybackDuration);
            return true;
        }

        private void HandleAudioStopRequested(GameAudioEventType eventType)
        {
            StopSourcesForEvent(eventType);
        }

        private void StopSourcesForEvent(GameAudioEventType eventType)
        {
            for (int i = 0; i < _oneShotSources.Count; i++)
            {
                AudioSource audioSource = _oneShotSources[i];
                if (audioSource == null)
                {
                    continue;
                }

                OneShotPlaybackState state = _oneShotStates[i];
                if (state.EventType != eventType)
                {
                    continue;
                }

                if (audioSource.isPlaying)
                {
                    audioSource.Stop();
                }

                state.HasStopLimit = false;
                _oneShotStates[i] = state;
            }
        }

        private void WarmOneShotPool()
        {
            int targetCount = Mathf.Clamp(initialOneShotPoolSize, 1, Mathf.Max(1, maximumOneShotSources));
            while (_oneShotSources.Count < targetCount)
            {
                _oneShotSources.Add(CreateOneShotSource(GameAudioEventType.ProjectileFired));
                _oneShotStates.Add(default);
            }
        }

        private AudioSource GetAvailableOneShotSource(GameAudioEventType eventType)
        {
            for (int i = 0; i < _oneShotSources.Count; i++)
            {
                AudioSource candidate = _oneShotSources[i];
                if (candidate != null && !candidate.isPlaying)
                {
                    candidate.gameObject.name = $"{eventType}_Audio";
                    return candidate;
                }
            }

            if (_oneShotSources.Count >= Mathf.Max(1, maximumOneShotSources))
            {
                return null;
            }

            AudioSource created = CreateOneShotSource(eventType);
            _oneShotSources.Add(created);
            _oneShotStates.Add(default);
            return created;
        }

        private AudioSource CreateOneShotSource(GameAudioEventType eventType)
        {
            GameObject audioObject = new($"{eventType}_Audio");
            audioObject.transform.SetParent(oneShotRoot != null ? oneShotRoot : transform, false);

            AudioSource audioSource = audioObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.loop = false;
            return audioSource;
        }

        private void TrackOneShot(AudioSource audioSource, GameAudioEventType eventType, float maxPlaybackDuration)
        {
            int sourceIndex = _oneShotSources.IndexOf(audioSource);
            if (sourceIndex < 0 || sourceIndex >= _oneShotStates.Count)
            {
                return;
            }

            _oneShotStates[sourceIndex] = new OneShotPlaybackState
            {
                EventType = eventType,
                StopAtTime = Time.unscaledTime + Mathf.Max(0f, maxPlaybackDuration),
                HasStopLimit = maxPlaybackDuration > 0f
            };
        }

        private bool TryGetRuntimeClip(GameAudioEventType eventType, string[] resourcePaths, out AudioClip clip)
        {
            if (_runtimeClipLookup.TryGetValue(eventType, out AudioClip[] cachedClips))
            {
                clip = PickRuntimeClip(cachedClips);
                return clip != null;
            }

            List<AudioClip> loadedClips = new();
            if (resourcePaths != null)
            {
                for (int i = 0; i < resourcePaths.Length; i++)
                {
                    string resourcePath = resourcePaths[i];
                    if (string.IsNullOrWhiteSpace(resourcePath))
                    {
                        continue;
                    }

                    AudioClip loadedClip = Resources.Load<AudioClip>(resourcePath);
                    if (loadedClip != null)
                    {
                        loadedClips.Add(loadedClip);
                    }
                }
            }

            AudioClip[] clips = loadedClips.ToArray();
            _runtimeClipLookup[eventType] = clips;
            clip = PickRuntimeClip(clips);
            if (clip == null && logMissingCuesInEditor)
            {
                string joinedPaths = resourcePaths != null ? string.Join(", ", resourcePaths) : string.Empty;
                UnityEngine.Debug.LogWarning($"GameAudioSystem could not load built-in cue for {eventType}: {joinedPaths}", this);
            }

            return clip != null;
        }

        private static AudioClip PickRuntimeClip(AudioClip[] clips)
        {
            if (clips == null || clips.Length == 0)
            {
                return null;
            }

            return clips.Length == 1 ? clips[0] : clips[Random.Range(0, clips.Length)];
        }

        private Vector3 ResolveRuntimePlaybackPosition(RuntimeCue runtimeCue, AudioPlaybackRequest request)
        {
            if (runtimeCue.PlayInWorldSpace && request.UseWorldPosition)
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

        private static float s_globalMusicVolume = 1f;
        private static float s_globalSfxVolume = 1f;

        public static void SetGlobalVolumes(float musicVolume, float sfxVolume)
        {
            s_globalMusicVolume = Mathf.Clamp01(musicVolume);
            s_globalSfxVolume = Mathf.Clamp01(sfxVolume);

            GameAudioSystem[] systems = FindObjectsByType<GameAudioSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int index = 0; index < systems.Length; index++)
            {
                if (systems[index] != null)
                {
                    systems[index].ApplyGlobalVolumes(s_globalMusicVolume, s_globalSfxVolume);
                }
            }
        }

        private void ApplyGlobalVolumes(float musicVolume, float sfxVolume)
        {
            float previousMusicVolume = Mathf.Max(0.0001f, _musicVolume);
            _musicVolume = Mathf.Clamp01(musicVolume);
            _sfxVolume = Mathf.Clamp01(sfxVolume);
            ApplyMusicVolume(previousMusicVolume);
        }

        private void ApplyMusicVolume(float previousMusicVolume)
        {
            if (!applyMusicVolumeToLoopingSources)
            {
                return;
            }

            RefreshMusicSources(previousMusicVolume);

            for (int index = 0; index < _musicSources.Count; index++)
            {
                AudioSource source = _musicSources[index];
                if (source != null)
                {
                    source.volume = _musicSourceBaseVolumes[index] * _musicVolume;
                }
            }
        }

        private void RefreshMusicSources(float previousMusicVolume)
        {
            _musicSources.Clear();
            _musicSourceBaseVolumes.Clear();

            AudioSource[] sources = FindObjectsByType<AudioSource>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            for (int index = 0; index < sources.Length; index++)
            {
                AudioSource source = sources[index];
                if (source == null || !source.loop || _oneShotSources.Contains(source))
                {
                    continue;
                }

                _musicSources.Add(source);
                _musicSourceBaseVolumes.Add(Mathf.Clamp01(source.volume / Mathf.Max(0.0001f, previousMusicVolume)));
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
