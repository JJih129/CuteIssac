using CuteIssac.Combat;
using CuteIssac.Enemy;
using UnityEngine;

namespace CuteIssac.Core.Gameplay
{
    public readonly struct PlayerProjectileHitSignal
    {
        public PlayerProjectileHitSignal(
            Transform source,
            ProjectileLogic projectile,
            EnemyHealth enemyHealth,
            Vector3 impactPosition,
            Vector2 direction,
            float damage,
            OpeningCadenceVolleyRole openingCadenceRole,
            float openingCadenceRoleWeight)
        {
            Source = source;
            Projectile = projectile;
            EnemyHealth = enemyHealth;
            ImpactPosition = impactPosition;
            Direction = direction;
            Damage = damage;
            OpeningCadenceRole = openingCadenceRole;
            OpeningCadenceRoleWeight = openingCadenceRoleWeight;
        }

        public Transform Source { get; }
        public ProjectileLogic Projectile { get; }
        public EnemyHealth EnemyHealth { get; }
        public Vector3 ImpactPosition { get; }
        public Vector2 Direction { get; }
        public float Damage { get; }
        public OpeningCadenceVolleyRole OpeningCadenceRole { get; }
        public float OpeningCadenceRoleWeight { get; }
    }
}
