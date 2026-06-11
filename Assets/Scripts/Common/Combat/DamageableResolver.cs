using UnityEngine;
using CuteIssac.Enemy;
using CuteIssac.Player;

namespace CuteIssac.Common.Combat
{
    /// <summary>
    /// Resolves a damageable target from a hit collider.
    /// This keeps projectile code simple and supports colliders placed on child objects.
    /// </summary>
    public static class DamageableResolver
    {
        private const int MaxCachedColliderTargets = 768;

        private struct CachedTarget
        {
            public Collider2D Collider;
            public IDamageable Damageable;
            public PlayerHealth PlayerHealth;
            public EnemyHealth EnemyHealth;
            public int TargetId;
        }

        public readonly struct ResolvedTarget
        {
            public ResolvedTarget(IDamageable damageable, PlayerHealth playerHealth, EnemyHealth enemyHealth, int targetId)
            {
                Damageable = damageable;
                PlayerHealth = playerHealth;
                EnemyHealth = enemyHealth;
                TargetId = targetId;
            }

            public IDamageable Damageable { get; }
            public PlayerHealth PlayerHealth { get; }
            public EnemyHealth EnemyHealth { get; }
            public int TargetId { get; }
            public bool IsPlayer => PlayerHealth != null;
            public bool IsEnemy => EnemyHealth != null;
        }

        private static readonly System.Collections.Generic.Dictionary<int, CachedTarget> TargetCache = new(MaxCachedColliderTargets);

        public static bool TryResolve(Collider2D hitCollider, out IDamageable damageable)
        {
            damageable = null;

            if (!TryResolveTarget(hitCollider, out ResolvedTarget target))
            {
                return false;
            }

            damageable = target.Damageable;
            return true;
        }

        public static bool TryResolveTarget(Collider2D hitCollider, out ResolvedTarget target)
        {
            target = default;

            if (hitCollider == null)
            {
                return false;
            }

            int colliderId = hitCollider.GetInstanceID();
            if (TargetCache.TryGetValue(colliderId, out CachedTarget cachedTarget)
                && cachedTarget.Collider == hitCollider)
            {
                if (cachedTarget.Damageable == null)
                {
                    return false;
                }

                target = new ResolvedTarget(
                    cachedTarget.Damageable,
                    cachedTarget.PlayerHealth,
                    cachedTarget.EnemyHealth,
                    cachedTarget.TargetId);
                return true;
            }

            IDamageable damageable = hitCollider.GetComponentInParent<IDamageable>();

            if (damageable == null)
            {
                CacheTarget(colliderId, new CachedTarget
                {
                    Collider = hitCollider
                });
                return false;
            }

            PlayerHealth playerHealth = damageable as PlayerHealth;
            EnemyHealth enemyHealth = damageable as EnemyHealth;

            if (playerHealth == null)
            {
                playerHealth = hitCollider.GetComponentInParent<PlayerHealth>();
            }

            if (enemyHealth == null)
            {
                enemyHealth = hitCollider.GetComponentInParent<EnemyHealth>();
            }

            int targetId = ResolveTargetId(hitCollider, damageable);
            CachedTarget resolved = new()
            {
                Collider = hitCollider,
                Damageable = damageable,
                PlayerHealth = playerHealth,
                EnemyHealth = enemyHealth,
                TargetId = targetId
            };
            CacheTarget(colliderId, resolved);
            target = new ResolvedTarget(damageable, playerHealth, enemyHealth, targetId);
            return true;
        }

        private static void CacheTarget(int colliderId, CachedTarget target)
        {
            if (TargetCache.Count >= MaxCachedColliderTargets)
            {
                TargetCache.Clear();
            }

            TargetCache[colliderId] = target;
        }

        private static int ResolveTargetId(Collider2D fallbackCollider, IDamageable damageable)
        {
            if (damageable is Object unityObject)
            {
                return unityObject.GetInstanceID();
            }

            return fallbackCollider != null ? fallbackCollider.GetInstanceID() : 0;
        }
    }
}
