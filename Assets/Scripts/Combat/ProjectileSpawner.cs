using System.Collections.Generic;
using UnityEngine;
using CuteIssac.Core.Pooling;

namespace CuteIssac.Combat
{
    /// <summary>
    /// Central projectile factory.
    /// Combat systems request projectiles here so pooling stays isolated from firing logic.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ProjectileSpawner : MonoBehaviour
    {
        [Header("Spawn Origin")]
        [SerializeField] private Transform spawnOrigin;
        [SerializeField] [Min(0)] private int prewarmCount = 24;

        private readonly HashSet<GameObject> _prewarmedPrefabs = new();

        public Transform SpawnOrigin => spawnOrigin;

        public ProjectileLogic Spawn(in ProjectileSpawnRequest request)
        {
            if (request.ProjectilePrefab == null)
            {
                Debug.LogError("ProjectileSpawner received a spawn request without a projectile prefab.", this);
                return null;
            }

            if (prewarmCount > 0 && _prewarmedPrefabs.Add(request.ProjectilePrefab.gameObject))
            {
                PrefabPoolService.Prewarm(request.ProjectilePrefab.gameObject, prewarmCount);
            }

            Quaternion rotation = Quaternion.FromToRotation(Vector3.right, request.Direction);
            ProjectileLogic projectileInstance = PrefabPoolService.Spawn(request.ProjectilePrefab, request.Position, rotation);
            projectileInstance.Initialize(request);
            return projectileInstance;
        }

        public Vector2 GetSpawnPosition(Vector2 direction, Vector2 localOffset)
        {
            Transform origin = spawnOrigin != null ? spawnOrigin : transform;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            Vector2 rotatedOffset = Quaternion.Euler(0f, 0f, angle) * localOffset;
            return (Vector2)origin.position + rotatedOffset;
        }

        /// <summary>
        /// Lets presentation prefabs provide a muzzle anchor without hard-coding child names into combat logic.
        /// </summary>
        public void SetSpawnOrigin(Transform origin)
        {
            spawnOrigin = origin;
        }
    }
}
