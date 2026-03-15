using System;
using System.Collections.Generic;
using UnityEngine;

namespace CuteIssac.Room
{
    /// <summary>
    /// Spawns a lightweight authored obstacle set when a combat room is entered.
    /// The room keeps logic ownership while obstacle prefabs remain fully swappable in the inspector.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RoomObstacleSpawner : MonoBehaviour
    {
        [Serializable]
        private struct ObstacleSpawnEntry
        {
            public GameObject prefab;
            public Vector2 localPosition;
            public float rotationZ;
        }

        [SerializeField] private RoomController roomController;
        [SerializeField] private RoomEnemySpawner roomEnemySpawner;
        [SerializeField] private Transform obstacleRoot;
        [SerializeField] private bool spawnOnlyForCombatRooms = true;
        [SerializeField] private ObstacleSpawnEntry[] obstacles = Array.Empty<ObstacleSpawnEntry>();

        private readonly List<GameObject> _spawnedObstacles = new();
        private bool _hasSpawned;

        private void Awake()
        {
            ResolveReferences();
        }

        private void OnEnable()
        {
            if (roomController != null)
            {
                roomController.RoomEntered += HandleRoomEntered;
            }
        }

        private void OnDisable()
        {
            if (roomController != null)
            {
                roomController.RoomEntered -= HandleRoomEntered;
            }
        }

        private void HandleRoomEntered(RoomController enteredRoom)
        {
            if (_hasSpawned || enteredRoom == null || enteredRoom != roomController)
            {
                return;
            }

            if (spawnOnlyForCombatRooms && roomEnemySpawner != null && !roomEnemySpawner.CanStartCombat())
            {
                return;
            }

            SpawnObstacles();
        }

        private void SpawnObstacles()
        {
            Transform parent = obstacleRoot != null ? obstacleRoot : transform;

            for (int i = 0; i < obstacles.Length; i++)
            {
                ObstacleSpawnEntry entry = obstacles[i];

                if (entry.prefab == null)
                {
                    continue;
                }

                Vector3 worldPosition = parent.TransformPoint(entry.localPosition);
                Quaternion rotation = parent.rotation * Quaternion.Euler(0f, 0f, entry.rotationZ);
                GameObject spawnedObstacle = Instantiate(entry.prefab, worldPosition, rotation, parent);

                if (spawnedObstacle != null)
                {
                    _spawnedObstacles.Add(spawnedObstacle);
                }
            }

            _hasSpawned = _spawnedObstacles.Count > 0;
        }

        private void ResolveReferences()
        {
            if (roomController == null)
            {
                roomController = GetComponent<RoomController>();
            }

            if (roomEnemySpawner == null)
            {
                roomEnemySpawner = GetComponent<RoomEnemySpawner>();
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
