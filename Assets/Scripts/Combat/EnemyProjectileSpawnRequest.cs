using CuteIssac.Enemy;
using UnityEngine;

namespace CuteIssac.Combat
{
    /// <summary>
    /// Runtime packet for enemy-only projectile spawning.
    /// Keeping this separate from player projectiles avoids coupling both combat paths together.
    /// </summary>
    public struct EnemyProjectileSpawnRequest
    {
        public EnemyProjectileLogic ProjectilePrefab;
        public Vector2 Position;
        public Vector2 Direction;
        public float Damage;
        public float Speed;
        public float Lifetime;
        public float HomingStrength;
        public float HomingSearchRadius;
        public float HomingTurnRateDegrees;
        public Transform Instigator;
        public Collider2D InstigatorCollider;
    }
}
