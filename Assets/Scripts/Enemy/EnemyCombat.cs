using CuteIssac.Data.Combat;
using CuteIssac.Core.Audio;
using CuteIssac.Combat;
using CuteIssac.Core.Pooling;
using UnityEngine;

namespace CuteIssac.Enemy
{
    /// <summary>
    /// Owns enemy shooting only.
    /// AI decides when to fire, while this component owns projectile creation and authored tuning data.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EnemyCombat : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private EnemyVisual enemyVisual;
        [SerializeField] private Collider2D ownerCollider;
        [SerializeField] private Transform spawnOrigin;

        [Header("Projectile")]
        [SerializeField] private EnemyProjectileDefinition projectileDefinition;
        [SerializeField] private Vector2 muzzleOffset = new(0.42f, 0f);
        [SerializeField] [Min(0)] private int prewarmCount = 12;
        [SerializeField] [Range(0.1f, 2f)] private float runtimeProjectileSpeedMultiplier = 1f;
        [SerializeField] [Range(0.1f, 3f)] private float runtimeProjectileLifetimeMultiplier = 1f;

        public bool CanFire => projectileDefinition != null && projectileDefinition.IsValid;
        public int PrewarmCount => Mathf.Max(0, prewarmCount);

        public void SetProjectileSpeedMultiplier(float multiplier)
        {
            runtimeProjectileSpeedMultiplier = Mathf.Clamp(multiplier, 0.1f, 2f);
        }

        public void SetProjectileLifetimeMultiplier(float multiplier)
        {
            runtimeProjectileLifetimeMultiplier = Mathf.Clamp(multiplier, 0.1f, 3f);
        }

        public void SetSpawnOrigin(Transform origin)
        {
            if (origin != null)
            {
                spawnOrigin = origin;
            }
        }

        private void Awake()
        {
            ResolveReferences();
            PrewarmProjectiles(prewarmCount);
        }

        public void PrewarmProjectiles(int count)
        {
            if (!CanFire || count <= 0)
            {
                return;
            }

            GameObject projectilePrefab = projectileDefinition.ProjectilePrefab.gameObject;
            int desiredCount = Mathf.Max(0, count);
            PrefabPoolService.EnsurePrewarmed(projectilePrefab, desiredCount);
        }

        public void PrewarmProjectilesForExpectedShooters(int expectedShooterCount)
        {
            if (expectedShooterCount <= 0)
            {
                return;
            }

            int desiredCount = Mathf.Max(PrewarmCount, PrewarmCount * expectedShooterCount);
            PrewarmProjectiles(desiredCount);
        }

        public EnemyProjectileLogic Fire(Vector2 direction)
        {
            if (!CanFire)
            {
                return null;
            }

            Vector2 normalizedDirection = direction.sqrMagnitude > 0.0001f
                ? direction.normalized
                : Vector2.right;

            EnemyProjectileSpawnRequest spawnRequest = new()
            {
                ProjectilePrefab = projectileDefinition.ProjectilePrefab,
                Position = GetSpawnPosition(normalizedDirection),
                Direction = normalizedDirection,
                Damage = projectileDefinition.Damage,
                Speed = projectileDefinition.Speed * runtimeProjectileSpeedMultiplier,
                Lifetime = projectileDefinition.Lifetime * runtimeProjectileLifetimeMultiplier,
                HomingStrength = projectileDefinition.HomingStrength,
                HomingSearchRadius = projectileDefinition.HomingSearchRadius,
                HomingTurnRateDegrees = projectileDefinition.HomingTurnRateDegrees,
                Instigator = transform,
                InstigatorCollider = ownerCollider
            };

            PrewarmProjectiles(prewarmCount);

            EnemyProjectileLogic projectileInstance = PrefabPoolService.Spawn(
                spawnRequest.ProjectilePrefab,
                spawnRequest.Position,
                Quaternion.FromToRotation(Vector3.right, normalizedDirection));

            projectileInstance.Initialize(spawnRequest);
            GameAudioEvents.Raise(GameAudioEventType.ProjectileFired, transform.position, true, 0.9f);
            return projectileInstance;
        }

        private Vector2 GetSpawnPosition(Vector2 direction)
        {
            Transform origin = spawnOrigin != null ? spawnOrigin : transform;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            Vector2 rotatedOffset = Quaternion.Euler(0f, 0f, angle) * muzzleOffset;
            return (Vector2)origin.position + rotatedOffset;
        }

        private void ResolveReferences()
        {
            if (enemyVisual == null)
            {
                enemyVisual = GetComponent<EnemyVisual>();
            }

            if (ownerCollider == null)
            {
                ownerCollider = GetComponent<Collider2D>();
            }

            if (spawnOrigin == null && enemyVisual != null)
            {
                spawnOrigin = enemyVisual.AttackEffectAnchor;
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
