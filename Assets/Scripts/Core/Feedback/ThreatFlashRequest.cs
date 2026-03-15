using UnityEngine;

namespace CuteIssac.Core.Feedback
{
    public readonly struct ThreatFlashRequest
    {
        public ThreatFlashRequest(
            Color flashColor,
            float opacity,
            float duration,
            int pulseCount = 1,
            float pulseStrength = 0.2f,
            float pulseFrequencyScale = 1f,
            float decaySoftness = 0.36f)
        {
            FlashColor = flashColor;
            Opacity = Mathf.Clamp01(opacity);
            Duration = Mathf.Max(0.05f, duration);
            PulseCount = Mathf.Max(1, pulseCount);
            PulseStrength = Mathf.Clamp01(pulseStrength);
            PulseFrequencyScale = Mathf.Max(0.2f, pulseFrequencyScale);
            DecaySoftness = Mathf.Clamp01(decaySoftness);
        }

        public Color FlashColor { get; }
        public float Opacity { get; }
        public float Duration { get; }
        public int PulseCount { get; }
        public float PulseStrength { get; }
        public float PulseFrequencyScale { get; }
        public float DecaySoftness { get; }
    }
}
