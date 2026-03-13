using CuteIssac.Data.Enemy;
using CuteIssac.Data.Dungeon;
using CuteIssac.Enemy;
using UnityEngine;

namespace CuteIssac.Room
{
    /// <summary>
    /// Owns enemy instantiation for one room.
    /// RoomController decides when combat starts, while this component decides what to spawn and where to place it.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RoomEnemySpawner : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("RoomController that requests combat spawning. Auto-filled from the same object when possible.")]
        [SerializeField] private RoomController roomController;
        [Tooltip("Optional parent used to keep spawned enemies grouped under the room hierarchy.")]
        [SerializeField] private Transform spawnedEnemyParent;

        [Header("Wave Source")]
        [Tooltip("Authored test wave for this room. Generated rooms can override this at runtime with ConfigureWave.")]
        [SerializeField] private EnemyWaveData enemyWaveData;
        [Tooltip("Default room type used when no generated room metadata was injected.")]
        [SerializeField] private RoomType defaultRoomTypeForSpawns = RoomType.Normal;
        [SerializeField] [Min(0)] private int distanceFromStartOverride;
        [SerializeField] [Min(0)] private int targetBudgetOverride;

        [Header("Spawn Points")]
        [Tooltip("Preferred spawn points inside the room. Leave empty to use the room center fallback.")]
        [SerializeField] private Transform[] spawnAnchors;
        [SerializeField] [Min(0f)] private float anchorScatterRadius = 0.25f;
        [SerializeField] private Vector2 fallbackSpawnExtents = new(1.8f, 1.2f);

        private EnemyWaveAssignment _runtimeWaveAssignment;
        private RoomType? _runtimeRoomType;
        private bool _hasSpawnedEncounter;

        public bool HasSpawnedEncounter => _hasSpawnedEncounter;

        /// <summary>
        /// Non-combat rooms should clear immediately on entry.
        /// This lets RoomController stay state-focused while combat eligibility remains owned by the encounter spawner.
        /// </summary>
        public bool CanStartCombat()
        {
            if (_hasSpawnedEncounter)
            {
                return false;
            }

            if (!IsCombatRoomType(GetEffectiveRoomType()))
            {
                return false;
            }

            EnemyWaveAssignment enemyWaveAssignment = ResolveWaveAssignment();
            return enemyWaveAssignment != null && enemyWaveAssignment.TotalEnemyCount > 0;
        }

        private void Awake()
        {
            if (roomController == null)
            {
                roomController = GetComponent<RoomController>();
            }
        }

        /// <summary>
        /// Generated dungeon flow can inject a pre-resolved runtime wave here.
        /// The room then spawns that exact composition on first combat start.
        /// </summary>
        public void ConfigureWave(EnemyWaveAssignment enemyWaveAssignment)
        {
            _runtimeWaveAssignment = enemyWaveAssignment;
            _hasSpawnedEncounter = false;
        }

        /// <summary>
        /// Generated rooms inject their resolved room type and wave here.
        /// External runtime data always wins over the inspector-authored fallback wave.
        /// </summary>
        public void ConfigureEncounter(RoomType roomType, EnemyWaveAssignment enemyWaveAssignment)
        {
            _runtimeRoomType = roomType;
            _runtimeWaveAssignment = enemyWaveAssignment;
            _hasSpawnedEncounter = false;
        }

        /// <summary>
        /// Called by RoomController when combat starts for the first time.
        /// Spawning is idempotent so cleared rooms and re-entry never create a second wave.
        /// </summary>
        public int HandleCombatStarted(RoomController combatRoom)
        {
            if (_hasSpawnedEncounter)
            {
                return 0;
            }

            RoomController targetRoom = combatRoom != null ? combatRoom : roomController;

            if (targetRoom == null)
            {
                Debug.LogWarning("RoomEnemySpawner could not spawn because no RoomController was assigned.", this);
                return 0;
            }

            if (!IsCombatRoomType(GetEffectiveRoomType()))
            {
                return 0;
            }

            EnemyWaveAssignment enemyWaveAssignment = ResolveWaveAssignment();

            if (enemyWaveAssignment == null || enemyWaveAssignment.TotalEnemyCount <= 0)
            {
                return 0;
            }

            int spawnIndex = 0;
            int spawnedEnemyCount = 0;

            for (int i = 0; i < enemyWaveAssignment.SpawnGroups.Count; i++)
            {
                EnemyWaveSpawnGroup spawnGroup = enemyWaveAssignment.SpawnGroups[i];

                if (spawnGroup == null || spawnGroup.EnemyPrefab == null || spawnGroup.Count <= 0)
                {
                    continue;
                }

                for (int countIndex = 0; countIndex < spawnGroup.Count; countIndex++)
                {
                    if (SpawnEnemyInstance(targetRoom, spawnGroup, spawnIndex))
                    {
                        spawnedEnemyCount++;
                    }

                    spawnIndex++;
                }
            }

            if (spawnedEnemyCount > 0)
            {
                _hasSpawnedEncounter = true;
            }

            return spawnedEnemyCount;
        }

        private EnemyWaveAssignment ResolveWaveAssignment()
        {
            if (_runtimeWaveAssignment != null)
            {
                return _runtimeWaveAssignment;
            }

            if (enemyWaveData == null)
            {
                return null;
            }

            return enemyWaveData.BuildAssignment(distanceFromStartOverride, targetBudgetOverride);
        }

        private RoomType GetEffectiveRoomType()
        {
            return _runtimeRoomType ?? defaultRoomTypeForSpawns;
        }

        private static bool IsCombatRoomType(RoomType roomType)
        {
            return roomType == RoomType.Normal
                || roomType == RoomType.Challenge
                || roomType == RoomType.Boss;
        }

        private bool SpawnEnemyInstance(RoomController targetRoom, EnemyWaveSpawnGroup spawnGroup, int spawnIndex)
        {
            EnemyController spawnedEnemy = Instantiate(
                spawnGroup.EnemyPrefab,
                ResolveSpawnPosition(targetRoom, spawnIndex),
                Quaternion.identity);

            if (spawnedEnemy == null)
            {
                Debug.LogWarning($"RoomEnemySpawner failed to instantiate enemy prefab for wave '{spawnGroup.EnemyId}'.", this);
                return false;
            }

            Transform parent = spawnedEnemyParent != null ? spawnedEnemyParent : targetRoom.transform;
            spawnedEnemy.transform.SetParent(parent, true);

            EnemyHealth enemyHealth = spawnedEnemy.GetComponent<EnemyHealth>();

            if (enemyHealth == null)
            {
                Debug.LogWarning("Spawned enemy is missing EnemyHealth, so the room cannot track clear state correctly.", spawnedEnemy);
                return false;
            }

            RoomEnemyMember roomEnemyMember = spawnedEnemy.GetComponent<RoomEnemyMember>();

            if (roomEnemyMember == null)
            {
                roomEnemyMember = spawnedEnemy.gameObject.AddComponent<RoomEnemyMember>();
            }

            roomEnemyMember.AssignRoom(targetRoom);
            return true;
        }

        private Vector3 ResolveSpawnPosition(RoomController targetRoom, int spawnIndex)
        {
            if (spawnAnchors != null && spawnAnchors.Length > 0)
            {
                Transform anchor = spawnAnchors[spawnIndex % spawnAnchors.Length];

                if (anchor != null)
                {
                    return anchor.position + ComputeScatterOffset(spawnIndex, anchorScatterRadius);
                }
            }

            Vector3 fallbackCenter = targetRoom != null ? targetRoom.transform.position : transform.position;
            return fallbackCenter + ComputeFallbackOffset(spawnIndex);
        }

        private Vector3 ComputeScatterOffset(int spawnIndex, float radius)
        {
            if (radius <= 0f)
            {
                return Vector3.zero;
            }

            float angle = 137.5f * spawnIndex * Mathf.Deg2Rad;
            float scaledRadius = radius * (0.45f + ((spawnIndex % 3) * 0.275f));
            return new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * scaledRadius;
        }

        private Vector3 ComputeFallbackOffset(int spawnIndex)
        {
            float horizontal = Mathf.Clamp(fallbackSpawnExtents.x, 0f, 100f);
            float vertical = Mathf.Clamp(fallbackSpawnExtents.y, 0f, 100f);

            if (horizontal <= 0f && vertical <= 0f)
            {
                return Vector3.zero;
            }

            float angle = 83f * spawnIndex * Mathf.Deg2Rad;
            float horizontalSign = (spawnIndex & 1) == 0 ? 1f : -1f;
            float verticalSign = ((spawnIndex / 2) & 1) == 0 ? 1f : -1f;

            return new Vector3(
                Mathf.Cos(angle) * horizontal * 0.55f * horizontalSign,
                Mathf.Sin(angle) * vertical * 0.55f * verticalSign,
                0f);
        }

        private void Reset()
        {
            roomController = GetComponent<RoomController>();
        }

        private void OnValidate()
        {
            if (roomController == null)
            {
                roomController = GetComponent<RoomController>();
            }
        }
    }
}
