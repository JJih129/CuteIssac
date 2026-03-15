using System.Collections.Generic;
using CuteIssac.Combat;
using CuteIssac.Common.Combat;
using UnityEngine;

namespace CuteIssac.Room
{
    /// <summary>
    /// Gameplay-facing room obstacle.
    /// Handles projectile interaction and optional contact hazard logic, while visuals stay in RoomObstacleVisual.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RoomObstacleController : MonoBehaviour
    {
        [Header("Identity")]
        [SerializeField] private RoomObstacleType obstacleType = RoomObstacleType.Rock;

        [Header("Collision")]
        [SerializeField] private Collider2D obstacleCollider;
        [SerializeField] private ProjectileObstacleResponse projectileResponse = ProjectileObstacleResponse.Solid;
        [SerializeField] private ProjectileImpactType projectileImpactType = ProjectileImpactType.Solid;

        [Header("Hazard")]
        [SerializeField] private bool damagePlayerOnContact;
        [SerializeField] private bool damageEnemiesOnContact;
        [SerializeField] [Min(0f)] private float contactDamage = 1f;
        [SerializeField] [Min(0f)] private float contactKnockback = 2f;
        [SerializeField] [Min(0.05f)] private float contactTickInterval = 0.5f;

        [Header("Presentation")]
        [SerializeField] private RoomObstacleVisual obstacleVisual;

        private readonly Dictionary<int, float> _nextDamageTimes = new();

        public RoomObstacleType ObstacleType => obstacleType;
        public Collider2D ObstacleCollider => obstacleCollider;

        private void Awake()
        {
            ResolveReferences();
        }

        private void OnDisable()
        {
            _nextDamageTimes.Clear();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            TryApplyContactDamage(other);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            TryApplyContactDamage(other);
        }

        public bool TryHandleProjectile(bool isEnemyProjectile, Vector3 impactPosition, out ProjectileImpactType impactType)
        {
            impactType = projectileImpactType;

            switch (projectileResponse)
            {
                case ProjectileObstacleResponse.Ignore:
                    return false;
                case ProjectileObstacleResponse.Solid:
                case ProjectileObstacleResponse.Consume:
                    obstacleVisual?.HandleProjectileImpact();
                    return true;
                default:
                    return false;
            }
        }

        private void TryApplyContactDamage(Collider2D other)
        {
            if (contactDamage <= 0f || (!damagePlayerOnContact && !damageEnemiesOnContact) || other == null)
            {
                return;
            }

            if (!DamageableResolver.TryResolve(other, out IDamageable damageable))
            {
                return;
            }

            bool isPlayerTarget = other.GetComponentInParent<Player.PlayerHealth>() != null;
            bool isEnemyTarget = other.GetComponentInParent<Enemy.EnemyHealth>() != null;

            if ((isPlayerTarget && !damagePlayerOnContact) || (isEnemyTarget && !damageEnemiesOnContact))
            {
                return;
            }

            int targetId = (damageable as Object)?.GetInstanceID() ?? other.GetInstanceID();

            if (_nextDamageTimes.TryGetValue(targetId, out float nextDamageTime) && Time.time < nextDamageTime)
            {
                return;
            }

            Vector2 hitDirection = ((Vector2)other.bounds.center - (Vector2)transform.position).normalized;

            if (hitDirection.sqrMagnitude <= 0.0001f)
            {
                hitDirection = Vector2.up;
            }

            damageable.ApplyDamage(new DamageInfo(contactDamage, hitDirection, transform, contactKnockback));
            _nextDamageTimes[targetId] = Time.time + contactTickInterval;
            obstacleVisual?.HandleHazardTriggered();
        }

        private void ResolveReferences()
        {
            if (obstacleCollider == null)
            {
                obstacleCollider = GetComponent<Collider2D>();
            }

            if (obstacleVisual == null)
            {
                obstacleVisual = GetComponent<RoomObstacleVisual>();
            }
        }

        private void Reset()
        {
            ResolveReferences();
        }

        private void OnValidate()
        {
            ResolveReferences();
        }
    }
}
