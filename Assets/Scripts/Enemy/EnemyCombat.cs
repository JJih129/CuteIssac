using System.Collections.Generic;
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

        private readonly HashSet<GameObject> _prewarmedPrefabs = new();

        public bool CanFire => projectileDefinition != null && projectileDefinition.IsValid;

        private void Awake()
        {
            ResolveReferences();
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
                Speed = projectileDefinition.Speed,
                Lifetime = projectileDefinition.Lifetime,
                HomingStrength = projectileDefinition.HomingStrength,
                HomingSearchRadius = projectileDefinition.HomingSearchRadius,
                HomingTurnRateDegrees = projectileDefinition.HomingTurnRateDegrees,
                Instigator = transform,
                InstigatorCollider = ownerCollider
            };

            if (prewarmCount > 0 && _prewarmedPrefabs.Add(spawnRequest.ProjectilePrefab.gameObject))
            {
                PrefabPoolService.Prewarm(spawnRequest.ProjectilePrefab.gameObject, prewarmCount);
            }

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
