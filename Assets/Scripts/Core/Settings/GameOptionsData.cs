using System;

namespace CuteIssac.Core.Settings
{
    /// <summary>
    /// Serializable player-facing options payload for persistent settings.
    /// </summary>
    [Serializable]
    public sealed class GameOptionsData
    {
        public float MasterVolume = 1f;
        public float MusicVolume = 0.85f;
        public float SfxVolume = 1f;
        public bool Fullscreen = true;
        public int ResolutionWidth = 1920;
        public int ResolutionHeight = 1080;
        public float UiScale = 1f;
        public bool CameraShakeEnabled = true;
        public bool DamageNumbersEnabled = true;
        public bool HighContrastUi;
        public bool ReduceFlashes;
        public bool ColorBlindAssist;
    }
}
