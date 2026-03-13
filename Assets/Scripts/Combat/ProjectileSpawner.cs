using UnityEngine;

namespace CuteIssac.Combat
{
    /// <summary>
    /// Central projectile factory. Instantiation lives here so future pooling can replace it in one place.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ProjectileSpawner : MonoBehaviour
    {
        [Header("Spawn Origin")]
        [SerializeField] private Transform spawnOrigin;

        public Transform SpawnOrigin => spawnOrigin;

        public ProjectileController Spawn(in ProjectileSpawnRequest request)
        {
            if (request.ProjectilePrefab == null)
            {
                Debug.LogError("ProjectileSpawner received a spawn request without a projectile prefab.", this);
                return null;
            }

            Quaternion rotation = Quaternion.FromToRotation(Vector3.right, request.Direction);
            ProjectileController projectileInstance = Instantiate(request.ProjectilePrefab, request.Position, rotation);
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
