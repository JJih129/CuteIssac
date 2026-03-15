using System;
using CuteIssac.Enemy;
using UnityEngine;

namespace CuteIssac.Data.Enemy
{
    [CreateAssetMenu(fileName = "BossPhaseProfile", menuName = "CuteIssac/Data/Enemy/Boss Phase Profile")]
    public sealed class BossPhaseProfile : ScriptableObject
    {
        [Serializable]
        public sealed class BossPhaseDefinition
        {
            [SerializeField] private BossPhaseType phase = BossPhaseType.PhaseOne;
            [SerializeField] [Range(0.01f, 1f)] private float healthThreshold = 1f;
            [SerializeField] private string displayLabel = "1페이즈";
            [SerializeField] private BossPatternType[] patternCycle = { BossPatternType.Burst, BossPatternType.Charge };
            [SerializeField] [Min(0.2f)] private float burstCooldownMultiplier = 1f;
            [SerializeField] [Min(0.2f)] private float volleyCooldownMultiplier = 1f;
            [SerializeField] [Min(0.2f)] private float chargeCooldownMultiplier = 1f;
            [SerializeField] [Min(0.2f)] private float sweepCooldownMultiplier = 1f;
            [SerializeField] [Min(0.2f)] private float spiralCooldownMultiplier = 1f;
            [SerializeField] [Min(0.2f)] private float fanCooldownMultiplier = 1f;
            [SerializeField] [Min(0.2f)] private float shockwaveCooldownMultiplier = 1f;
            [SerializeField] [Min(0.2f)] private float crossfireCooldownMultiplier = 1f;
            [SerializeField] [Min(0.2f)] private float burstTelegraphMultiplier = 1f;
            [SerializeField] [Min(0.2f)] private float volleyTelegraphMultiplier = 1f;
            [SerializeField] [Min(0.2f)] private float chargeTelegraphMultiplier = 1f;
            [SerializeField] [Min(0.2f)] private float sweepTelegraphMultiplier = 1f;
            [SerializeField] [Min(0.2f)] private float spiralTelegraphMultiplier = 1f;
            [SerializeField] [Min(0.2f)] private float fanTelegraphMultiplier = 1f;
            [SerializeField] [Min(0.2f)] private float shockwaveTelegraphMultiplier = 1f;
            [SerializeField] [Min(0.2f)] private float crossfireTelegraphMultiplier = 1f;
            [SerializeField] [Min(0.5f)] private float burstCountMultiplier = 1f;
            [SerializeField] [Min(0.5f)] private float volleyCountMultiplier = 1f;
            [SerializeField] [Min(0.5f)] private float chargeSpeedMultiplier = 1f;
            [SerializeField] [Min(0.5f)] private float sweepCountMultiplier = 1f;
            [SerializeField] [Min(0.5f)] private float spiralCountMultiplier = 1f;
            [SerializeField] [Min(0.5f)] private float fanCountMultiplier = 1f;
            [SerializeField] [Min(0.5f)] private float shockwaveCountMultiplier = 1f;
            [SerializeField] [Min(0.5f)] private float crossfireCountMultiplier = 1f;
            [SerializeField] [Min(0f)] private float transitionDuration = 0.85f;

            public BossPhaseDefinition(
                BossPhaseType phase,
                float healthThreshold,
                string displayLabel,
                BossPatternType[] patternCycle,
                float burstCooldownMultiplier,
                float volleyCooldownMultiplier,
                float chargeCooldownMultiplier,
                float sweepCooldownMultiplier,
                float spiralCooldownMultiplier,
                float fanCooldownMultiplier,
                float shockwaveCooldownMultiplier,
                float crossfireCooldownMultiplier,
                float burstTelegraphMultiplier,
                float volleyTelegraphMultiplier,
                float chargeTelegraphMultiplier,
                float sweepTelegraphMultiplier,
                float spiralTelegraphMultiplier,
                float fanTelegraphMultiplier,
                float shockwaveTelegraphMultiplier,
                float crossfireTelegraphMultiplier,
                float burstCountMultiplier,
                float volleyCountMultiplier,
                float chargeSpeedMultiplier,
                float sweepCountMultiplier,
                float spiralCountMultiplier,
                float fanCountMultiplier,
                float shockwaveCountMultiplier,
                float crossfireCountMultiplier,
                float transitionDuration)
            {
                this.phase = phase;
                this.healthThreshold = healthThreshold;
                this.displayLabel = displayLabel;
                this.patternCycle = patternCycle;
                this.burstCooldownMultiplier = burstCooldownMultiplier;
                this.volleyCooldownMultiplier = volleyCooldownMultiplier;
                this.chargeCooldownMultiplier = chargeCooldownMultiplier;
                this.sweepCooldownMultiplier = sweepCooldownMultiplier;
                this.spiralCooldownMultiplier = spiralCooldownMultiplier;
                this.fanCooldownMultiplier = fanCooldownMultiplier;
                this.shockwaveCooldownMultiplier = shockwaveCooldownMultiplier;
                this.crossfireCooldownMultiplier = crossfireCooldownMultiplier;
                this.burstTelegraphMultiplier = burstTelegraphMultiplier;
                this.volleyTelegraphMultiplier = volleyTelegraphMultiplier;
                this.chargeTelegraphMultiplier = chargeTelegraphMultiplier;
                this.sweepTelegraphMultiplier = sweepTelegraphMultiplier;
                this.spiralTelegraphMultiplier = spiralTelegraphMultiplier;
                this.fanTelegraphMultiplier = fanTelegraphMultiplier;
                this.shockwaveTelegraphMultiplier = shockwaveTelegraphMultiplier;
                this.crossfireTelegraphMultiplier = crossfireTelegraphMultiplier;
                this.burstCountMultiplier = burstCountMultiplier;
                this.volleyCountMultiplier = volleyCountMultiplier;
                this.chargeSpeedMultiplier = chargeSpeedMultiplier;
                this.sweepCountMultiplier = sweepCountMultiplier;
                this.spiralCountMultiplier = spiralCountMultiplier;
                this.fanCountMultiplier = fanCountMultiplier;
                this.shockwaveCountMultiplier = shockwaveCountMultiplier;
                this.crossfireCountMultiplier = crossfireCountMultiplier;
                this.transitionDuration = transitionDuration;
            }

            public BossPhaseType Phase => phase;
            public float HealthThreshold => Mathf.Clamp01(healthThreshold);
            public string DisplayLabel => string.IsNullOrWhiteSpace(displayLabel) ? phase.ToString() : displayLabel;
            public BossPatternType[] PatternCycle => patternCycle;
            public float BurstCooldownMultiplier => Mathf.Max(0.2f, burstCooldownMultiplier);
            public float VolleyCooldownMultiplier => Mathf.Max(0.2f, volleyCooldownMultiplier);
            public float ChargeCooldownMultiplier => Mathf.Max(0.2f, chargeCooldownMultiplier);
            public float SweepCooldownMultiplier => Mathf.Max(0.2f, sweepCooldownMultiplier);
            public float SpiralCooldownMultiplier => Mathf.Max(0.2f, spiralCooldownMultiplier);
            public float FanCooldownMultiplier => Mathf.Max(0.2f, fanCooldownMultiplier);
            public float ShockwaveCooldownMultiplier => Mathf.Max(0.2f, shockwaveCooldownMultiplier);
            public float CrossfireCooldownMultiplier => Mathf.Max(0.2f, crossfireCooldownMultiplier);
            public float BurstTelegraphMultiplier => Mathf.Max(0.2f, burstTelegraphMultiplier);
            public float VolleyTelegraphMultiplier => Mathf.Max(0.2f, volleyTelegraphMultiplier);
            public float ChargeTelegraphMultiplier => Mathf.Max(0.2f, chargeTelegraphMultiplier);
            public float SweepTelegraphMultiplier => Mathf.Max(0.2f, sweepTelegraphMultiplier);
            public float SpiralTelegraphMultiplier => Mathf.Max(0.2f, spiralTelegraphMultiplier);
            public float FanTelegraphMultiplier => Mathf.Max(0.2f, fanTelegraphMultiplier);
            public float ShockwaveTelegraphMultiplier => Mathf.Max(0.2f, shockwaveTelegraphMultiplier);
            public float CrossfireTelegraphMultiplier => Mathf.Max(0.2f, crossfireTelegraphMultiplier);
            public float BurstCountMultiplier => Mathf.Max(0.5f, burstCountMultiplier);
            public float VolleyCountMultiplier => Mathf.Max(0.5f, volleyCountMultiplier);
            public float ChargeSpeedMultiplier => Mathf.Max(0.5f, chargeSpeedMultiplier);
            public float SweepCountMultiplier => Mathf.Max(0.5f, sweepCountMultiplier);
            public float SpiralCountMultiplier => Mathf.Max(0.5f, spiralCountMultiplier);
            public float FanCountMultiplier => Mathf.Max(0.5f, fanCountMultiplier);
            public float ShockwaveCountMultiplier => Mathf.Max(0.5f, shockwaveCountMultiplier);
            public float CrossfireCountMultiplier => Mathf.Max(0.5f, crossfireCountMultiplier);
            public float TransitionDuration => Mathf.Max(0f, transitionDuration);
        }

        [SerializeField] private BossPhaseDefinition[] phaseDefinitions =
        {
            new BossPhaseDefinition(
                BossPhaseType.PhaseOne,
                1f,
                "1페이즈",
                new[] { BossPatternType.Burst, BossPatternType.Charge },
                1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f,
                1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f,
                1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f,
                0f),
            new BossPhaseDefinition(
                BossPhaseType.PhaseTwo,
                0.7f,
                "2페이즈",
                new[] { BossPatternType.Burst, BossPatternType.Volley, BossPatternType.Spiral, BossPatternType.Fan, BossPatternType.Shockwave, BossPatternType.Sweep, BossPatternType.Charge },
                0.94f, 0.88f, 0.92f, 0.9f, 0.9f, 0.92f, 0.94f, 1f,
                0.92f, 0.9f, 0.9f, 0.86f, 0.88f, 0.9f, 0.9f, 1f,
                1.18f, 1.12f, 1.08f, 1.18f, 1.16f, 1.1f, 1.08f, 1f,
                0.9f),
            new BossPhaseDefinition(
                BossPhaseType.PhaseThree,
                0.35f,
                "3페이즈",
                new[] { BossPatternType.Burst, BossPatternType.Spiral, BossPatternType.Crossfire, BossPatternType.Fan, BossPatternType.Shockwave, BossPatternType.Sweep, BossPatternType.Volley, BossPatternType.Charge, BossPatternType.Crossfire },
                0.86f, 0.76f, 0.84f, 0.74f, 0.72f, 0.74f, 0.76f, 0.78f,
                0.84f, 0.78f, 0.82f, 0.72f, 0.7f, 0.72f, 0.74f, 0.76f,
                1.42f, 1.3f, 1.18f, 1.32f, 1.32f, 1.24f, 1.26f, 1.22f,
                1.05f),
        };

        public static BossPhaseProfile CreateRuntimeDefault()
        {
            BossPhaseProfile profile = CreateInstance<BossPhaseProfile>();
            profile.hideFlags = HideFlags.HideAndDontSave;
            return profile;
        }

        public BossPhaseType EvaluatePhase(float normalizedHealth)
        {
            float health = Mathf.Clamp01(normalizedHealth);
            BossPhaseType resolvedPhase = BossPhaseType.PhaseOne;
            float resolvedThreshold = float.MaxValue;

            if (phaseDefinitions == null)
            {
                return resolvedPhase;
            }

            for (int i = 0; i < phaseDefinitions.Length; i++)
            {
                BossPhaseDefinition definition = phaseDefinitions[i];

                if (definition == null)
                {
                    continue;
                }

                float threshold = definition.HealthThreshold;

                if (health <= threshold && threshold < resolvedThreshold)
                {
                    resolvedPhase = definition.Phase;
                    resolvedThreshold = threshold;
                }
            }

            return resolvedPhase;
        }

        public bool TryGetDefinition(BossPhaseType phase, out BossPhaseDefinition definition)
        {
            if (phaseDefinitions != null)
            {
                for (int i = 0; i < phaseDefinitions.Length; i++)
                {
                    BossPhaseDefinition candidate = phaseDefinitions[i];

                    if (candidate != null && candidate.Phase == phase)
                    {
                        definition = candidate;
                        return true;
                    }
                }
            }

            definition = null;
            return false;
        }
    }
}
