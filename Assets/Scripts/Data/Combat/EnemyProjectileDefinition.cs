using CuteIssac.Combat;
using UnityEngine;

namespace CuteIssac.Data.Combat
{
    /// <summary>
    /// Shared tuning data for enemy-fired projectiles.
    /// Separate from player projectile data so enemy combat can evolve independently.
    /// </summary>
    [CreateAssetMenu(fileName = "EnemyProjectileDefinition", menuName = "CuteIssac/Data/Combat/Enemy Projectile Definition")]
    public sealed class EnemyProjectileDefinition : ScriptableObject
    {
        [SerializeField] private EnemyProjectileLogic projectilePrefab;
        [SerializeField] [Min(0f)] private float damage = 1f;
        [SerializeField] [Min(0f)] private float speed = 10f;
        [SerializeField] [Min(0.05f)] private float lifetime = 2f;
        [SerializeField] [Min(0f)] private float homingStrength;
        [SerializeField] [Min(0.5f)] private float homingSearchRadius = 5.5f;
        [SerializeField] [Min(0f)] private float homingTurnRateDegrees = 135f;

        public EnemyProjectileLogic ProjectilePrefab => projectilePrefab;
        public float Damage => damage;
        public float Speed => speed;
        public float Lifetime => lifetime;
        public float HomingStrength => homingStrength;
        public float HomingSearchRadius => homingSearchRadius;
        public float HomingTurnRateDegrees => homingTurnRateDegrees;
        public bool IsValid => projectilePrefab != null;
    }
}
