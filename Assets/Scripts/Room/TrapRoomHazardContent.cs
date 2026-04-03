using System;
using System.Collections.Generic;
using UnityEngine;

namespace CuteIssac.Room
{
    /// <summary>
    /// Spawns a lightweight set of authored hazards when trap-room content is instantiated.
    /// The room system stays data-driven because the trap prefab and positions are fully swappable in the inspector.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TrapRoomHazardContent : MonoBehaviour
    {
        [Serializable]
        private struct HazardSpawnEntry
        {
            public GameObject prefab;
            public Vector2 localPosition;
            public float rotationZ;
        }

        [SerializeField] private Transform spawnRoot;
        [SerializeField] private HazardSpawnEntry[] hazards = Array.Empty<HazardSpawnEntry>();

        private readonly List<GameObject> _spawnedHazards = new();
        private bool _hasSpawned;

        private void Start()
        {
            SpawnIfNeeded();
        }

        public void SpawnIfNeeded()
        {
            if (_hasSpawned)
            {
                return;
            }

            Transform parent = spawnRoot != null ? spawnRoot : transform;

            for (int i = 0; i < hazards.Length; i++)
            {
                HazardSpawnEntry entry = hazards[i];

                if (entry.prefab == null)
                {
                    continue;
                }

                Vector3 worldPosition = parent.TransformPoint(entry.localPosition);
                Quaternion rotation = parent.rotation * Quaternion.Euler(0f, 0f, entry.rotationZ);
                GameObject spawnedHazard = Instantiate(entry.prefab, worldPosition, rotation, parent);

                if (spawnedHazard != null)
                {
                    _spawnedHazards.Add(spawnedHazard);
                }
            }

            _hasSpawned = _spawnedHazards.Count > 0;
        }

        public bool TryResolveHazardFocusTarget(out Vector3 focusPosition, out float focusRadius)
        {
            Vector3 accumulatedPosition = Vector3.zero;
            int activeHazardCount = 0;

            for (int i = 0; i < _spawnedHazards.Count; i++)
            {
                GameObject spawnedHazard = _spawnedHazards[i];

                if (spawnedHazard == null)
                {
                    continue;
                }

                accumulatedPosition += spawnedHazard.transform.position;
                activeHazardCount++;
            }

            if (activeHazardCount > 0)
            {
                focusPosition = accumulatedPosition / activeHazardCount;
                focusRadius = Mathf.Max(1.16f, 0.9f + (activeHazardCount * 0.18f));
                return true;
            }

            Transform parent = spawnRoot != null ? spawnRoot : transform;
            focusPosition = parent.position;
            focusRadius = 1.18f;
            return parent != null;
        }

        public void CollectHazardTargets(List<Transform> targetBuffer)
        {
            if (targetBuffer == null)
            {
                return;
            }

            for (int i = 0; i < _spawnedHazards.Count; i++)
            {
                GameObject spawnedHazard = _spawnedHazards[i];

                if (spawnedHazard == null)
                {
                    continue;
                }

                targetBuffer.Add(spawnedHazard.transform);
            }
        }
    }
}
