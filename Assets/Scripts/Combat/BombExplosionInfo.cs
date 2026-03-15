using UnityEngine;

namespace CuteIssac.Combat
{
    /// <summary>
    /// Runtime payload broadcast to damageables and bomb-reactive objects.
    /// Keeping this as data avoids direct coupling between bombs and specific room or wall implementations.
    /// </summary>
    public readonly struct BombExplosionInfo
    {
        public BombExplosionInfo(Vector2 position, float radius, float damage, float knockbackForce, Transform source)
        {
            Position = position;
            Radius = radius;
            Damage = damage;
            KnockbackForce = knockbackForce;
            Source = source;
        }

        public Vector2 Position { get; }
        public float Radius { get; }
        public float Damage { get; }
        public float KnockbackForce { get; }
        public Transform Source { get; }
    }
}
