using System;
using UnityEngine;

namespace CuteIssac.Core.Settings
{
    /// <summary>
    /// Owns runtime option state and applies the subset that already has engine-level side effects.
    /// UI can bind to this later without touching persistence details.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GameOptionsService : MonoBehaviour
    {
        [Header("Defaults")]
        [SerializeField] [Range(0f, 1f)] private float defaultMasterVolume = 1f;
        [SerializeField] [Range(0f, 1f)] private float defaultMusicVolume = 0.85f;
        [SerializeField] [Range(0f, 1f)] private float defaultSfxVolume = 1f;
        [SerializeField] private bool defaultFullscreen = true;
        [SerializeField] private bool defaultCameraShakeEnabled = true;
        [SerializeField] private bool defaultDamageNumbersEnabled = true;

        public event Action<GameOptionsData> OptionsChanged;

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
                CameraShakeEnabled = defaultCameraShakeEnabled,
                DamageNumbersEnabled = defaultDamageNumbersEnabled
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
            destination.CameraShakeEnabled = source.CameraShakeEnabled;
            destination.DamageNumbersEnabled = source.DamageNumbersEnabled;
        }

        private static void ApplyOptions(GameOptionsData options)
        {
            if (options == null)
            {
                return;
            }

            AudioListener.volume = Mathf.Clamp01(options.MasterVolume);
            Screen.fullScreen = options.Fullscreen;
        }
    }
}
