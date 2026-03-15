using UnityEngine;

namespace CuteIssac.Combat
{
    /// <summary>
    /// Runtime projectile configuration packet.
    /// Future item modifiers can adjust this before the spawner instantiates the projectile.
    /// </summary>
    public struct ProjectileSpawnRequest
    {
        public ProjectileLogic ProjectilePrefab;
        public Vector2 Position;
        public Vector2 Direction;
        public Vector2 InheritedVelocity;
        public float Damage;
        public float Speed;
        public float Lifetime;
        public float Scale;
        public float Knockback;
        public int PierceCount;
        public float HomingStrength;
        public Transform Instigator;
        public Collider2D InstigatorCollider;
        public ProjectileDamageTarget DamageTarget;
    }
}
