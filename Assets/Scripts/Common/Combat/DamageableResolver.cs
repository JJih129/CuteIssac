using UnityEngine;

namespace CuteIssac.Common.Combat
{
    /// <summary>
    /// Resolves a damageable target from a hit collider.
    /// This keeps projectile code simple and supports colliders placed on child objects.
    /// </summary>
    public static class DamageableResolver
    {
        public static bool TryResolve(Collider2D hitCollider, out IDamageable damageable)
        {
            damageable = null;

            if (hitCollider == null)
            {
                return false;
            }

            damageable = hitCollider.GetComponentInParent<IDamageable>();
            return damageable != null;
        }
    }
}
