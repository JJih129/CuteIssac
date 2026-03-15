using UnityEngine;

namespace CuteIssac.Data.Enemy
{
    [CreateAssetMenu(fileName = "ExploderEnemyData", menuName = "CuteIssac/Data/Enemy/Exploder Enemy Data")]
    public sealed class ExploderEnemyData : EnemyData
    {
        [Header("Exploder")]
        [SerializeField] [Min(0f)] private float triggerRange = 1.65f;
        [SerializeField] [Min(0f)] private float windupDuration = 0.65f;
        [SerializeField] [Min(0f)] private float windupMoveSpeedMultiplier = 0.2f;
        [SerializeField] [Min(0f)] private float explosionDamage = 2f;
        [SerializeField] [Min(0f)] private float explosionRadius = 1.6f;
        [SerializeField] [Min(0f)] private float explosionKnockback = 7f;
        [SerializeField] [Min(1f)] private float chaseSpeedMultiplier = 1.15f;
        [SerializeField] private Color telegraphColor = new(1f, 0.34f, 0.18f, 1f);

        public float TriggerRange => triggerRange;
        public float WindupDuration => windupDuration;
        public float WindupMoveSpeedMultiplier => windupMoveSpeedMultiplier;
        public float ExplosionDamage => explosionDamage;
        public float ExplosionRadius => explosionRadius;
        public float ExplosionKnockback => explosionKnockback;
        public float ChaseSpeedMultiplier => chaseSpeedMultiplier;
        public Color TelegraphColor => telegraphColor;
    }
}
