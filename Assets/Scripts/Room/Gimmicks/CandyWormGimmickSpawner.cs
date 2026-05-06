using System.Collections.Generic;
using UnityEngine;

namespace CuteIssac.Room.Gimmicks
{
    /// <summary>
    /// Lightweight random spawner for candy worm gimmicks. It only handles interval, count, and spawn bounds.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CandyWormGimmickSpawner : MonoBehaviour
    {
        [Header("Prefab")]
        [SerializeField] private CandyWormGimmickController wormPrefab;
        [SerializeField] private Transform spawnedParent;

        [Header("Spawn Area")]
        [SerializeField] private BoxCollider2D spawnBounds;
        [SerializeField] private Transform fallbackCenter;
        [SerializeField] private Vector2 fallbackAreaSize = new(8f, 4.5f);

        [Header("Spawn Timing")]
        [SerializeField] private bool autoStart = true;
        [SerializeField] [Min(0.1f)] private float spawnInterval = 5f;
        [SerializeField] [Min(1)] private int maxSimultaneousWorms = 4;
        [SerializeField] private bool spawnImmediately;

        private readonly List<CandyWormGimmickController> _activeWorms = new();
        private float _spawnTimer;
        private bool _isRunning;

        public bool IsRunning => _isRunning;
        public int ActiveWormCount => _activeWorms.Count;

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
            UnsubscribeActiveWorms();
            _activeWorms.Clear();
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

            TrySpawnWorm();
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

        [ContextMenu("Spawn Candy Worm")]
        public void TrySpawnWorm()
        {
            CleanupInactiveWorms();

            if (wormPrefab == null || _activeWorms.Count >= maxSimultaneousWorms)
            {
                return;
            }

            Vector2 targetPosition = ResolveRandomTargetPosition();
            CandyWormGimmickController worm = Instantiate(
                wormPrefab,
                new Vector3(targetPosition.x, targetPosition.y, wormPrefab.transform.position.z),
                Quaternion.identity,
                spawnedParent != null ? spawnedParent : transform);

            if (worm == null)
            {
                return;
            }

            worm.Completed += HandleWormCompleted;
            _activeWorms.Add(worm);
            worm.StartWarning(targetPosition);
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

        private void HandleWormCompleted(CandyWormGimmickController worm)
        {
            if (worm != null)
            {
                worm.Completed -= HandleWormCompleted;
            }

            RemoveWorm(worm);
        }

        private void CleanupInactiveWorms()
        {
            for (int i = _activeWorms.Count - 1; i >= 0; i--)
            {
                CandyWormGimmickController worm = _activeWorms[i];

                if (worm != null && worm.gameObject.activeInHierarchy && worm.State != CandyWormGimmickController.WormState.Broken)
                {
                    continue;
                }

                if (worm != null)
                {
                    worm.Completed -= HandleWormCompleted;
                }

                RemoveWormAt(i);
            }
        }

        private void UnsubscribeActiveWorms()
        {
            for (int i = _activeWorms.Count - 1; i >= 0; i--)
            {
                if (_activeWorms[i] != null)
                {
                    _activeWorms[i].Completed -= HandleWormCompleted;
                }
            }
        }

        private void RemoveWorm(CandyWormGimmickController worm)
        {
            for (int i = _activeWorms.Count - 1; i >= 0; i--)
            {
                if (_activeWorms[i] == worm)
                {
                    RemoveWormAt(i);
                    return;
                }
            }
        }

        private void RemoveWormAt(int index)
        {
            int lastIndex = _activeWorms.Count - 1;

            if (index != lastIndex)
            {
                _activeWorms[index] = _activeWorms[lastIndex];
            }

            _activeWorms.RemoveAt(lastIndex);
        }

        private void Reset()
        {
            fallbackCenter = transform;
        }

        private void OnValidate()
        {
            spawnInterval = Mathf.Max(0.1f, spawnInterval);
            maxSimultaneousWorms = Mathf.Max(1, maxSimultaneousWorms);
            fallbackAreaSize.x = Mathf.Max(0.1f, fallbackAreaSize.x);
            fallbackAreaSize.y = Mathf.Max(0.1f, fallbackAreaSize.y);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.55f, 0.34f, 0.18f, 0.48f);

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
