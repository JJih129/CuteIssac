using UnityEngine;

namespace CuteIssac.Combat
{
    /// <summary>
    /// Runtime projectile configuration packet.
    /// Future item modifiers can adjust this before the spawner instantiates the projectile.
    /// </summary>
    public struct ProjectileSpawnRequest
    {
        public ProjectileController ProjectilePrefab;
        public Vector2 Position;
        public Vector2 Direction;
        public float Damage;
        public float Speed;
        public float Lifetime;
        public Transform Instigator;
        public Collider2D InstigatorCollider;
    }
}
