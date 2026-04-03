using UnityEngine;

namespace CuteIssac.Data.Enemy
{
    [CreateAssetMenu(fileName = "EnemyVisualSet", menuName = "CuteIssac/Data/Enemy/Enemy Visual Set")]
    public sealed class EnemyVisualSet : ScriptableObject
    {
        [System.Serializable]
        public sealed class HealthVisualPhase
        {
            [SerializeField] [Range(0f, 1f)] private float healthThreshold = 0.5f;
            [SerializeField] private Sprite bodySprite;
            [SerializeField] private bool overrideBaseColor;
            [SerializeField] private Color baseColor = Color.white;
            [SerializeField] private bool overrideHitFlashColor;
            [SerializeField] private Color hitFlashColor = Color.white;
            [SerializeField] private bool overrideDamagedColor;
            [SerializeField] private Color damagedColor = new(1f, 0.48f, 0.48f, 1f);
            [SerializeField] private bool overrideDeadColor;
            [SerializeField] private Color deadColor = new(1f, 1f, 1f, 0.55f);
            [SerializeField] private bool overrideVisualScale;
            [SerializeField] [Min(0.5f)] private float visualScaleMultiplier = 1f;

            public float HealthThreshold => Mathf.Clamp01(healthThreshold);
            public Sprite BodySprite => bodySprite;
            public bool OverrideBaseColor => overrideBaseColor;
            public Color BaseColor => baseColor;
            public bool OverrideHitFlashColor => overrideHitFlashColor;
            public Color HitFlashColor => hitFlashColor;
            public bool OverrideDamagedColor => overrideDamagedColor;
            public Color DamagedColor => damagedColor;
            public bool OverrideDeadColor => overrideDeadColor;
            public Color DeadColor => deadColor;
            public bool OverrideVisualScale => overrideVisualScale;
            public float VisualScaleMultiplier => Mathf.Max(0.5f, visualScaleMultiplier);
        }

        [SerializeField] private Sprite bodySprite;
        [SerializeField] private Color baseColor = Color.white;
        [SerializeField] private Color hitFlashColor = Color.white;
        [SerializeField] private Color damagedColor = new(1f, 0.48f, 0.48f, 1f);
        [SerializeField] private Color deadColor = new(1f, 1f, 1f, 0.55f);
        [SerializeField] private HealthVisualPhase[] healthPhases = System.Array.Empty<HealthVisualPhase>();

        public Sprite BodySprite => bodySprite;
        public Color BaseColor => baseColor;
        public Color HitFlashColor => hitFlashColor;
        public Color DamagedColor => damagedColor;
        public Color DeadColor => deadColor;
        public HealthVisualPhase[] HealthPhases => healthPhases;

        public bool TryResolveHealthPhase(float normalizedHealth, out HealthVisualPhase phase, out int phaseIndex)
        {
            phase = null;
            phaseIndex = -1;

            if (healthPhases == null || healthPhases.Length == 0)
            {
                return false;
            }

            float health = Mathf.Clamp01(normalizedHealth);
            float resolvedThreshold = float.MaxValue;

            for (int i = 0; i < healthPhases.Length; i++)
            {
                HealthVisualPhase candidate = healthPhases[i];

                if (candidate == null)
                {
                    continue;
                }

                float threshold = candidate.HealthThreshold;

                if (health <= threshold && threshold < resolvedThreshold)
                {
                    phase = candidate;
                    phaseIndex = i;
                    resolvedThreshold = threshold;
                }
            }

            return phase != null;
        }
    }
}
