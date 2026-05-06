using System.Collections.Generic;
using UnityEngine;

namespace CuteIssac.Room.Gimmicks
{
    /// <summary>
    /// Lightweight random apple-bomb spawner for room prefabs. It avoids a wave system and only manages interval/count.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class AppleBombSpawner : MonoBehaviour
    {
        [Header("Prefab")]
        [SerializeField] private AppleBombGimmickController appleBombPrefab;
        [SerializeField] private Transform spawnedParent;

        [Header("Spawn Area")]
        [SerializeField] private BoxCollider2D spawnBounds;
        [SerializeField] private Transform fallbackCenter;
        [SerializeField] private Vector2 fallbackAreaSize = new(8f, 4.5f);

        [Header("Spawn Timing")]
        [SerializeField] private bool autoStart = true;
        [SerializeField] [Min(0.1f)] private float spawnInterval = 4f;
        [SerializeField] [Min(1)] private int maxSimultaneousBombs = 3;
        [SerializeField] private bool spawnImmediately;

        private readonly List<AppleBombGimmickController> _activeBombs = new();
        private float _spawnTimer;
        private bool _isRunning;

        public bool IsRunning => _isRunning;
        public int ActiveBombCount => _activeBombs.Count;

        private void OnEnable()
        {
            if (autoStart)
            {
                StartSpawning();
            }
        }

        private void OnDisable()
        {
            StopSpawning();
            _activeBombs.Clear();
        }

        private void Update()
        {
            if (!_isRunning)
            {
                return;
            }

            _spawnTimer -= Time.deltaTime;

            if (_spawnTimer > 0f)
            {
                return;
            }

            TrySpawnBomb();
            _spawnTimer = spawnInterval;
        }

        public void StartSpawning()
        {
            _isRunning = true;
            _spawnTimer = spawnImmediately ? 0f : spawnInterval;
        }

        public void StopSpawning()
        {
            _isRunning = false;
        }

        [ContextMenu("Spawn Apple Bomb")]
        public void TrySpawnBomb()
        {
            CleanupInactiveBombs();

            if (appleBombPrefab == null || _activeBombs.Count >= maxSimultaneousBombs)
            {
                return;
            }

            Vector2 targetPosition = ResolveRandomTargetPosition();
            AppleBombGimmickController bomb = Instantiate(
                appleBombPrefab,
                new Vector3(targetPosition.x, targetPosition.y, appleBombPrefab.transform.position.z),
                Quaternion.identity,
                spawnedParent != null ? spawnedParent : transform);

            if (bomb == null)
            {
                return;
            }

            bomb.Completed += HandleBombCompleted;
            _activeBombs.Add(bomb);
            bomb.StartDrop(targetPosition);
        }

        private Vector2 ResolveRandomTargetPosition()
        {
            if (spawnBounds != null)
            {
                Bounds bounds = spawnBounds.bounds;
                return new Vector2(
                    Random.Range(bounds.min.x, bounds.max.x),
                    Random.Range(bounds.min.y, bounds.max.y));
            }

            Vector2 center = fallbackCenter != null ? fallbackCenter.position : transform.position;
            Vector2 halfSize = fallbackAreaSize * 0.5f;
            return new Vector2(
                Random.Range(center.x - halfSize.x, center.x + halfSize.x),
                Random.Range(center.y - halfSize.y, center.y + halfSize.y));
        }

        private void HandleBombCompleted(AppleBombGimmickController bomb)
        {
            if (bomb != null)
            {
                bomb.Completed -= HandleBombCompleted;
            }

            RemoveBomb(bomb);
        }

        private void CleanupInactiveBombs()
        {
            for (int i = _activeBombs.Count - 1; i >= 0; i--)
            {
                AppleBombGimmickController bomb = _activeBombs[i];

                if (bomb != null && bomb.gameObject.activeInHierarchy && bomb.State != AppleBombGimmickController.AppleBombState.Exploded)
                {
                    continue;
                }

                if (bomb != null)
                {
                    bomb.Completed -= HandleBombCompleted;
                }

                RemoveBombAt(i);
            }
        }

        private void RemoveBomb(AppleBombGimmickController bomb)
        {
            for (int i = _activeBombs.Count - 1; i >= 0; i--)
            {
                if (_activeBombs[i] == bomb)
                {
                    RemoveBombAt(i);
                    return;
                }
            }
        }

        private void RemoveBombAt(int index)
        {
            int lastIndex = _activeBombs.Count - 1;

            if (index != lastIndex)
            {
                _activeBombs[index] = _activeBombs[lastIndex];
            }

            _activeBombs.RemoveAt(lastIndex);
        }

        private void Reset()
        {
            fallbackCenter = transform;
        }

        private void OnValidate()
        {
            spawnInterval = Mathf.Max(0.1f, spawnInterval);
            maxSimultaneousBombs = Mathf.Max(1, maxSimultaneousBombs);
            fallbackAreaSize.x = Mathf.Max(0.1f, fallbackAreaSize.x);
            fallbackAreaSize.y = Mathf.Max(0.1f, fallbackAreaSize.y);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.35f, 0.12f, 0.42f);

            if (spawnBounds != null)
            {
                Bounds bounds = spawnBounds.bounds;
                Gizmos.DrawWireCube(bounds.center, bounds.size);
                return;
            }

            Vector3 center = fallbackCenter != null ? fallbackCenter.position : transform.position;
            Gizmos.DrawWireCube(center, new Vector3(fallbackAreaSize.x, fallbackAreaSize.y, 0f));
        }
    }
}
