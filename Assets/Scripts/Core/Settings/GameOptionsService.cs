using System;
using CuteIssac.Core.Audio;
using UnityEngine;
using UnityEngine.UI;

namespace CuteIssac.Core.Settings
{
    /// <summary>
    /// Owns runtime option state and applies the subset that already has engine-level side effects.
    /// UI can bind to this later without touching persistence details.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GameOptionsService : MonoBehaviour
    {
        private const float ReferenceWidth = 1920f;
        private const float ReferenceHeight = 1080f;

        [Header("Defaults")]
        [SerializeField] [Range(0f, 1f)] private float defaultMasterVolume = 1f;
        [SerializeField] [Range(0f, 1f)] private float defaultMusicVolume = 0.85f;
        [SerializeField] [Range(0f, 1f)] private float defaultSfxVolume = 1f;
        [SerializeField] private bool defaultFullscreen = true;
        [SerializeField] [Min(640)] private int defaultResolutionWidth = 1920;
        [SerializeField] [Min(360)] private int defaultResolutionHeight = 1080;
        [SerializeField] [Range(0.75f, 1.5f)] private float defaultUiScale = 1f;
        [SerializeField] private bool defaultCameraShakeEnabled = true;
        [SerializeField] private bool defaultDamageNumbersEnabled = true;
        [SerializeField] private bool defaultHighContrastUi;
        [SerializeField] private bool defaultReduceFlashes;
        [SerializeField] private bool defaultColorBlindAssist;

        public event Action<GameOptionsData> OptionsChanged;

        public static bool CameraShakeEnabled { get; private set; } = true;
        public static bool DamageNumbersEnabled { get; private set; } = true;
        public static bool HighContrastUiEnabled { get; private set; }
        public static bool ReduceFlashesEnabled { get; private set; }
        public static bool ColorBlindAssistEnabled { get; private set; }

        public GameOptionsData CurrentOptions { get; private set; }

        private void Awake()
        {
            if (CurrentOptions == null)
            {
                CurrentOptions = BuildDefaultOptions();
            }

            ApplyOptions(CurrentOptions);
        }

        [ContextMenu("Reset Options To Defaults")]
        public void ResetToDefaults()
        {
            Import(BuildDefaultOptions());
        }

        public GameOptionsData Export()
        {
            GameOptionsData data = new();
            CopyOptions(CurrentOptions, data);
            return data;
        }

        public void Import(GameOptionsData data)
        {
            CurrentOptions ??= BuildDefaultOptions();
            CopyOptions(data ?? BuildDefaultOptions(), CurrentOptions);
            ApplyOptions(CurrentOptions);
            OptionsChanged?.Invoke(CurrentOptions);
        }

        private GameOptionsData BuildDefaultOptions()
        {
            return new GameOptionsData
            {
                MasterVolume = defaultMasterVolume,
                MusicVolume = defaultMusicVolume,
                SfxVolume = defaultSfxVolume,
                Fullscreen = defaultFullscreen,
                ResolutionWidth = Mathf.Max(640, defaultResolutionWidth),
                ResolutionHeight = Mathf.Max(360, defaultResolutionHeight),
                UiScale = Mathf.Clamp(defaultUiScale, 0.75f, 1.5f),
                CameraShakeEnabled = defaultCameraShakeEnabled,
                DamageNumbersEnabled = defaultDamageNumbersEnabled,
                HighContrastUi = defaultHighContrastUi,
                ReduceFlashes = defaultReduceFlashes,
                ColorBlindAssist = defaultColorBlindAssist
            };
        }

        private static void CopyOptions(GameOptionsData source, GameOptionsData destination)
        {
            if (destination == null)
            {
                return;
            }

            source ??= new GameOptionsData();
            destination.MasterVolume = Mathf.Clamp01(source.MasterVolume);
            destination.MusicVolume = Mathf.Clamp01(source.MusicVolume);
            destination.SfxVolume = Mathf.Clamp01(source.SfxVolume);
            destination.Fullscreen = source.Fullscreen;
            destination.ResolutionWidth = Mathf.Clamp(source.ResolutionWidth <= 0 ? 1920 : source.ResolutionWidth, 640, 7680);
            destination.ResolutionHeight = Mathf.Clamp(source.ResolutionHeight <= 0 ? 1080 : source.ResolutionHeight, 360, 4320);
            destination.UiScale = Mathf.Clamp(source.UiScale <= 0f ? 1f : source.UiScale, 0.75f, 1.5f);
            destination.CameraShakeEnabled = source.CameraShakeEnabled;
            destination.DamageNumbersEnabled = source.DamageNumbersEnabled;
            destination.HighContrastUi = source.HighContrastUi;
            destination.ReduceFlashes = source.ReduceFlashes;
            destination.ColorBlindAssist = source.ColorBlindAssist;
        }

        private static void ApplyOptions(GameOptionsData options)
        {
            if (options == null)
            {
                return;
            }

            AudioListener.volume = Mathf.Clamp01(options.MasterVolume);
            GameAudioSystem.SetGlobalVolumes(options.MusicVolume, options.SfxVolume);
            int width = Mathf.Clamp(options.ResolutionWidth <= 0 ? Screen.width : options.ResolutionWidth, 640, 7680);
            int height = Mathf.Clamp(options.ResolutionHeight <= 0 ? Screen.height : options.ResolutionHeight, 360, 4320);
            Screen.SetResolution(width, height, options.Fullscreen);
            ApplyUiScaleToOpenCanvases(options.UiScale);
            ApplyAccessibilityState(options);
        }

        public static void ApplyAccessibilityState(GameOptionsData options)
        {
            options ??= new GameOptionsData();
            CameraShakeEnabled = options.CameraShakeEnabled;
            DamageNumbersEnabled = options.DamageNumbersEnabled;
            HighContrastUiEnabled = options.HighContrastUi;
            ReduceFlashesEnabled = options.ReduceFlashes;
            ColorBlindAssistEnabled = options.ColorBlindAssist;
        }

        public static void ApplyUiScale(CanvasScaler scaler, float uiScale)
        {
            if (scaler == null)
            {
                return;
            }

            float resolvedScale = Mathf.Clamp(uiScale <= 0f ? 1f : uiScale, 0.75f, 1.5f);
            if (scaler.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize)
            {
                scaler.referenceResolution = new Vector2(ReferenceWidth / resolvedScale, ReferenceHeight / resolvedScale);
                return;
            }

            scaler.scaleFactor = resolvedScale;
        }

        private static void ApplyUiScaleToOpenCanvases(float uiScale)
        {
            CanvasScaler[] scalers = FindObjectsByType<CanvasScaler>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int index = 0; index < scalers.Length; index++)
            {
                ApplyUiScale(scalers[index], uiScale);
            }
        }
    }
}
