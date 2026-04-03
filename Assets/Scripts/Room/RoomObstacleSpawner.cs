using System;
using System.Collections.Generic;
using CuteIssac.Data.Dungeon;
using CuteIssac.Dungeon;
using UnityEngine;

namespace CuteIssac.Room
{
    /// <summary>
    /// Spawns a lightweight obstacle set when a generated room is entered.
    /// It can either place authored entries directly or sample a random layout from the configured prefab pool.
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
        [SerializeField] private bool useRandomizedPlacement = true;
        [SerializeField] private bool spawnInStartRooms;
        [SerializeField] [Min(1)] private int minimumRandomObstacleCount = 4;
        [SerializeField] [Min(1)] private int maximumRandomObstacleCount = 6;
        [SerializeField] [Min(1)] private int placementAttemptsPerObstacle = 18;
        [SerializeField] [Min(0f)] private float roomEdgePadding = 0.9f;
        [SerializeField] [Min(0f)] private float obstacleSpacingPadding = 0.55f;
        [SerializeField] [Min(0f)] private float doorSafeHalfWidth = 1.45f;
        [SerializeField] [Min(0f)] private float doorSafeDepth = 2.9f;
        [SerializeField] [Min(0.1f)] private float obstacleScaleMultiplier = 0.6f;
        [SerializeField] private ObstacleSpawnEntry[] obstacles = Array.Empty<ObstacleSpawnEntry>();

        private readonly List<GameObject> _spawnedObstacles = new();
        private readonly List<GameObject> _prefabPool = new();
        private readonly List<PlacedObstacle> _placedObstacles = new();
        private bool _hasSpawned;

        private readonly struct PlacedObstacle
        {
            public PlacedObstacle(Vector2 position, float radius)
            {
                Position = position;
                Radius = radius;
            }

            public Vector2 Position { get; }

            public float Radius { get; }
        }

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

            if (!ShouldSpawnForRoom())
            {
                _hasSpawned = true;
                return;
            }

            SpawnObstacles();
        }

        private void SpawnObstacles()
        {
            Transform parent = obstacleRoot != null ? obstacleRoot : transform;
            _placedObstacles.Clear();

            if (useRandomizedPlacement && TryBuildPrefabPool())
            {
                SpawnRandomizedObstacles(parent);
            }
            else
            {
                SpawnAuthoredObstacles(parent);
            }

            _hasSpawned = true;
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

        private bool ShouldSpawnForRoom()
        {
            if (roomController == null)
            {
                return false;
            }

            if (!spawnInStartRooms && roomController.RoomType == RoomType.Start)
            {
                return false;
            }

            if (roomController.RoomType == RoomType.Treasure || roomController.RoomType == RoomType.Shop)
            {
                return false;
            }

            if (spawnOnlyForCombatRooms && roomEnemySpawner != null && !roomEnemySpawner.CanStartCombat())
            {
                return false;
            }

            return true;
        }

        private bool TryBuildPrefabPool()
        {
            _prefabPool.Clear();

            for (int i = 0; i < obstacles.Length; i++)
            {
                GameObject prefab = obstacles[i].prefab;

                if (prefab != null)
                {
                    _prefabPool.Add(prefab);
                }
            }

            return _prefabPool.Count > 0;
        }

        private void SpawnAuthoredObstacles(Transform parent)
        {
            for (int i = 0; i < obstacles.Length; i++)
            {
                ObstacleSpawnEntry entry = obstacles[i];

                if (entry.prefab == null)
                {
                    continue;
                }

                Vector3 worldPosition = parent.TransformPoint(entry.localPosition);
                Quaternion rotation = parent.rotation * Quaternion.Euler(0f, 0f, entry.rotationZ);
                RegisterSpawnedObstacle(Instantiate(entry.prefab, worldPosition, rotation, parent));
            }
        }

        private void SpawnRandomizedObstacles(Transform parent)
        {
            if (roomController == null || _prefabPool.Count == 0)
            {
                return;
            }

            Bounds roomBounds = roomController.RoomBounds;
            if (roomBounds.size.x <= 0.01f || roomBounds.size.y <= 0.01f)
            {
                return;
            }

            System.Random random = new(CreateDeterministicSeed());
            int minimumCount = Mathf.Max(1, Mathf.Min(minimumRandomObstacleCount, maximumRandomObstacleCount));
            int maximumCount = Mathf.Max(minimumCount, maximumRandomObstacleCount);
            int targetCount = random.Next(minimumCount, maximumCount + 1);

            for (int obstacleIndex = 0; obstacleIndex < targetCount; obstacleIndex++)
            {
                bool placed = false;

                for (int attempt = 0; attempt < placementAttemptsPerObstacle; attempt++)
                {
                    GameObject prefab = _prefabPool[random.Next(0, _prefabPool.Count)];
                    float obstacleRadius = EstimateObstacleRadius(prefab) * ResolveObstacleScaleMultiplier();

                    if (!TrySampleSpawnPosition(random, roomBounds, obstacleRadius, out Vector2 worldPosition))
                    {
                        continue;
                    }

                    if (!IsValidRandomSpawnPosition(worldPosition, obstacleRadius))
                    {
                        continue;
                    }

                    float rotationZ = Mathf.Lerp(-18f, 18f, (float)random.NextDouble());
                    GameObject spawnedObstacle = Instantiate(
                        prefab,
                        worldPosition,
                        parent.rotation * Quaternion.Euler(0f, 0f, rotationZ),
                        parent);

                    if (spawnedObstacle == null)
                    {
                        continue;
                    }

                    RegisterSpawnedObstacle(spawnedObstacle);
                    _placedObstacles.Add(new PlacedObstacle(worldPosition, obstacleRadius));
                    placed = true;
                    break;
                }

                if (!placed)
                {
                    break;
                }
            }
        }

        private bool TrySampleSpawnPosition(System.Random random, Bounds roomBounds, float obstacleRadius, out Vector2 worldPosition)
        {
            float minX = roomBounds.min.x + roomEdgePadding + obstacleRadius;
            float maxX = roomBounds.max.x - roomEdgePadding - obstacleRadius;
            float minY = roomBounds.min.y + roomEdgePadding + obstacleRadius;
            float maxY = roomBounds.max.y - roomEdgePadding - obstacleRadius;

            if (minX >= maxX || minY >= maxY)
            {
                worldPosition = Vector2.zero;
                return false;
            }

            worldPosition = new Vector2(
                Mathf.Lerp(minX, maxX, (float)random.NextDouble()),
                Mathf.Lerp(minY, maxY, (float)random.NextDouble()));
            return true;
        }

        private bool IsValidRandomSpawnPosition(Vector2 worldPosition, float obstacleRadius)
        {
            for (int i = 0; i < _placedObstacles.Count; i++)
            {
                PlacedObstacle placedObstacle = _placedObstacles[i];
                float minimumDistance = placedObstacle.Radius + obstacleRadius + obstacleSpacingPadding;

                if ((placedObstacle.Position - worldPosition).sqrMagnitude < minimumDistance * minimumDistance)
                {
                    return false;
                }
            }

            IReadOnlyList<RoomDoor> roomDoors = roomController != null ? roomController.RoomDoors : Array.Empty<RoomDoor>();

            for (int i = 0; i < roomDoors.Count; i++)
            {
                RoomDoor roomDoor = roomDoors[i];
                if (roomDoor == null)
                {
                    continue;
                }

                Vector2 inward = ResolveDoorInward(roomDoor.DoorDirection);
                Vector2 lateral = new(-inward.y, inward.x);
                Vector2 toCandidate = worldPosition - (Vector2)roomDoor.transform.position;
                float depth = Vector2.Dot(toCandidate, inward);

                if (depth < -obstacleRadius || depth > doorSafeDepth + obstacleRadius)
                {
                    continue;
                }

                float lateralOffset = Mathf.Abs(Vector2.Dot(toCandidate, lateral));
                if (lateralOffset <= doorSafeHalfWidth + obstacleRadius)
                {
                    return false;
                }
            }

            return true;
        }

        private int CreateDeterministicSeed()
        {
            unchecked
            {
                int hash = 17;
                string roomId = roomController != null && !string.IsNullOrWhiteSpace(roomController.RoomId)
                    ? roomController.RoomId
                    : gameObject.name;

                for (int i = 0; i < roomId.Length; i++)
                {
                    hash = (hash * 31) + roomId[i];
                }

                hash = (hash * 31) + (roomController != null ? (int)roomController.RoomType : 0);
                hash = (hash * 31) + Mathf.RoundToInt(transform.position.x * 100f);
                hash = (hash * 31) + Mathf.RoundToInt(transform.position.y * 100f);
                return hash;
            }
        }

        private static float EstimateObstacleRadius(GameObject prefab)
        {
            if (prefab == null)
            {
                return 0.6f;
            }

            Collider2D obstacleCollider = prefab.GetComponent<Collider2D>();
            if (obstacleCollider is BoxCollider2D boxCollider)
            {
                Vector3 scale = prefab.transform.localScale;
                float extentX = Mathf.Abs(boxCollider.size.x * scale.x) * 0.5f;
                float extentY = Mathf.Abs(boxCollider.size.y * scale.y) * 0.5f;
                return Mathf.Max(0.45f, Mathf.Max(extentX, extentY));
            }

            if (obstacleCollider is CircleCollider2D circleCollider)
            {
                Vector3 scale = prefab.transform.localScale;
                return Mathf.Max(0.45f, circleCollider.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y)));
            }

            return 0.7f;
        }

        private static Vector2 ResolveDoorInward(RoomDirection direction)
        {
            return direction switch
            {
                RoomDirection.Up => Vector2.down,
                RoomDirection.Right => Vector2.left,
                RoomDirection.Down => Vector2.up,
                RoomDirection.Left => Vector2.right,
                _ => Vector2.zero
            };
        }

        private void RegisterSpawnedObstacle(GameObject spawnedObstacle)
        {
            if (spawnedObstacle != null)
            {
                float obstacleScale = ResolveObstacleScaleMultiplier();
                spawnedObstacle.transform.localScale *= obstacleScale;
                _spawnedObstacles.Add(spawnedObstacle);
            }
        }

        private float ResolveObstacleScaleMultiplier()
        {
            return Mathf.Clamp(obstacleScaleMultiplier, 0.1f, 1f);
        }
    }
}
