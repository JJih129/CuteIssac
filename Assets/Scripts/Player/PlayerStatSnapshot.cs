using System;

namespace CuteIssac.Player
{
    /// <summary>
    /// Final stat values consumed by gameplay systems.
    /// Using an immutable snapshot makes it obvious when recalculation happens and avoids hidden stat drift.
    /// </summary>
    [Serializable]
    public readonly struct PlayerStatSnapshot
    {
        public PlayerStatSnapshot(
            float maxHealth,
            float moveSpeed,
            float damage,
            float fireInterval,
            float projectileSpeed,
            float range,
            float projectileLifetime,
            float projectileScale,
            float luck,
            float projectileCount,
            float knockback,
            float projectilePierce,
            float homingStrength)
        {
            MaxHealth = maxHealth;
            MoveSpeed = moveSpeed;
            Damage = damage;
            FireInterval = fireInterval;
            ProjectileSpeed = projectileSpeed;
            Range = range;
            ProjectileLifetime = projectileLifetime;
            ProjectileScale = projectileScale;
            Luck = luck;
            ProjectileCount = projectileCount;
            Knockback = knockback;
            ProjectilePierce = projectilePierce;
            HomingStrength = homingStrength;
        }

        public float MaxHealth { get; }
        public float MoveSpeed { get; }
        public float Damage { get; }
        public float FireInterval { get; }
        public float ProjectileSpeed { get; }
        public float Range { get; }
        public float ProjectileLifetime { get; }
        public float ProjectileScale { get; }
        public float Luck { get; }
        public float ProjectileCount { get; }
        public float Knockback { get; }
        public float ProjectilePierce { get; }
        public float HomingStrength { get; }
    }
}
