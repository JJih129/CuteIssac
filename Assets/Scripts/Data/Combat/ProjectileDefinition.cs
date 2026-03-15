using CuteIssac.Combat;
using UnityEngine;

namespace CuteIssac.Data.Combat
{
    /// <summary>
    /// Shared projectile tuning data so shots can be reconfigured without editing player scripts.
    /// </summary>
    [CreateAssetMenu(fileName = "ProjectileDefinition", menuName = "CuteIssac/Data/Combat/Projectile Definition")]
    public sealed class ProjectileDefinition : ScriptableObject
    {
        [SerializeField] private ProjectileLogic projectilePrefab;
        [SerializeField] [Min(0f)] private float damage = 3f;
        [SerializeField] [Min(0f)] private float speed = 12f;
        [SerializeField] [Min(0.05f)] private float lifetime = 1.5f;
        [SerializeField] [Min(0.05f)] private float scale = 1f;

        public ProjectileLogic ProjectilePrefab => projectilePrefab;
        public float Damage => damage;
        public float Speed => speed;
        public float Lifetime => lifetime;
        public float Scale => scale;
        public bool IsValid => projectilePrefab != null;
    }
}
