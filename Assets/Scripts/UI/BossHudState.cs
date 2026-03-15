using UnityEngine;

namespace CuteIssac.UI
{
    /// <summary>
    /// Lightweight payload that carries the currently active boss HUD state.
    /// Boss gameplay code publishes this data, and UI presenters decide how to render it.
    /// </summary>
    public readonly struct BossHudState
    {
        public BossHudState(int sourceId, string bossName, float normalizedHealth)
        {
            SourceId = sourceId;
            BossName = bossName;
            NormalizedHealth = Mathf.Clamp01(normalizedHealth);
            Phase = CuteIssac.Enemy.BossPhaseType.PhaseOne;
            PhaseLabel = string.Empty;
            Pattern = CuteIssac.Enemy.BossPatternType.Burst;
            PatternLabel = string.Empty;
            IsPhaseTransitioning = false;
        }

        public BossHudState(int sourceId, string bossName, float normalizedHealth, CuteIssac.Enemy.BossPhaseType phase, string phaseLabel)
        {
            SourceId = sourceId;
            BossName = bossName;
            NormalizedHealth = Mathf.Clamp01(normalizedHealth);
            Phase = phase;
            PhaseLabel = phaseLabel ?? string.Empty;
            Pattern = CuteIssac.Enemy.BossPatternType.Burst;
            PatternLabel = string.Empty;
            IsPhaseTransitioning = false;
        }

        public BossHudState(
            int sourceId,
            string bossName,
            float normalizedHealth,
            CuteIssac.Enemy.BossPhaseType phase,
            string phaseLabel,
            CuteIssac.Enemy.BossPatternType pattern,
            string patternLabel,
            bool isPhaseTransitioning = false)
        {
            SourceId = sourceId;
            BossName = bossName;
            NormalizedHealth = Mathf.Clamp01(normalizedHealth);
            Phase = phase;
            PhaseLabel = phaseLabel ?? string.Empty;
            Pattern = pattern;
            PatternLabel = patternLabel ?? string.Empty;
            IsPhaseTransitioning = isPhaseTransitioning;
        }

        public int SourceId { get; }

        public string BossName { get; }

        public float NormalizedHealth { get; }

        public CuteIssac.Enemy.BossPhaseType Phase { get; }

        public string PhaseLabel { get; }

        public CuteIssac.Enemy.BossPatternType Pattern { get; }

        public string PatternLabel { get; }

        public bool IsPhaseTransitioning { get; }
    }
}
