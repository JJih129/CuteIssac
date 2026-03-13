using System.Collections.Generic;
using CuteIssac.Data.Dungeon;
using CuteIssac.Data.Room;
using UnityEngine;

namespace CuteIssac.Room
{
    /// <summary>
    /// Spawns room clear rewards from authored data.
    /// RoomController decides when a room is cleared; this component decides what prefab to drop and where to place it.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RoomRewardSpawner : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Owning room that requests reward drops after the encounter is cleared.")]
        [SerializeField] private RoomController roomController;
        [Tooltip("Data-driven reward table for this room.")]
        [SerializeField] private RoomRewardTable rewardTable;
        [Tooltip("Optional anchor for dropped rewards. Defaults to this transform if left empty.")]
        [SerializeField] private Transform rewardSpawnAnchor;
        [Tooltip("Optional parent for spawned pickup instances.")]
        [SerializeField] private Transform spawnedRewardParent;

        [Header("Reward Rules")]
        [Tooltip("Current room category used to filter reward table entries. Dungeon generation can override this later.")]
        [SerializeField] private RoomType roomTypeForRewards = RoomType.Normal;
        [Tooltip("When enabled, rewards spawn near the last enemy that died in this room before falling back to the anchor.")]
        [SerializeField] private bool preferLastEnemyDeathPosition = true;
        [SerializeField] [Min(0f)] private float scatterRadius = 0.8f;
        [SerializeField] private bool logRewardSpawnsInEditor = true;

        private readonly List<RoomRewardEntry> _candidateEntries = new();
        private readonly List<RoomRewardEntry> _selectionPool = new();
        private RoomRewardTable _runtimeRewardTableOverride;
        private bool _hasSpawnedRewards;
        private bool _isShuttingDown;

        public bool HasSpawnedRewards => _hasSpawnedRewards;

        /// <summary>
        /// Generated rooms inject their resolved room type so reward filtering follows the generated content instead of the prefab default.
        /// </summary>
        public void ConfigureRoomType(RoomType roomType)
        {
            roomTypeForRewards = roomType;
        }

        /// <summary>
        /// Generated room content can override the prefab default reward table without teaching RoomController about reward variants.
        /// </summary>
        public void ConfigureRewardRules(RoomType roomType, RoomRewardTable rewardTableOverride)
        {
            roomTypeForRewards = roomType;
            _runtimeRewardTableOverride = rewardTableOverride;
            _hasSpawnedRewards = false;
        }

        public void HandleRoomCleared(RoomController clearedRoom)
        {
            RoomRewardTable resolvedRewardTable = ResolveRewardTable();

            if (_isShuttingDown || _hasSpawnedRewards || clearedRoom == null || roomController != clearedRoom || resolvedRewardTable == null)
            {
                if (resolvedRewardTable == null && logRewardSpawnsInEditor)
                {
                    Debug.LogWarning("RoomRewardSpawner skipped reward spawn because no reward table is assigned.", this);
                }

                return;
            }

            resolvedRewardTable.CollectCandidates(roomTypeForRewards, _candidateEntries);

            if (_candidateEntries.Count == 0)
            {
                if (logRewardSpawnsInEditor && IsRewardEligibleRoomType(roomTypeForRewards))
                {
                    Debug.LogWarning($"RoomRewardSpawner found no reward candidates for room type {roomTypeForRewards}.", this);
                }

                return;
            }

            _selectionPool.Clear();
            _selectionPool.AddRange(_candidateEntries);

            int selectionCount = resolvedRewardTable.AllowDuplicateSelections
                ? resolvedRewardTable.GetSelectionCount()
                : Mathf.Min(resolvedRewardTable.GetSelectionCount(), _selectionPool.Count);

            int spawnIndex = 0;

            for (int selectionIndex = 0; selectionIndex < selectionCount; selectionIndex++)
            {
                if (_selectionPool.Count == 0)
                {
                    break;
                }

                int selectedIndex = SelectWeightedIndex(_selectionPool);

                if (selectedIndex < 0)
                {
                    break;
                }

                RoomRewardEntry selectedEntry = _selectionPool[selectedIndex];

                for (int quantityIndex = 0; quantityIndex < selectedEntry.Quantity; quantityIndex++)
                {
                    SpawnRewardInstance(selectedEntry, spawnIndex);
                    spawnIndex++;
                }

                if (!resolvedRewardTable.AllowDuplicateSelections)
                {
                    _selectionPool.RemoveAt(selectedIndex);
                }
            }

            _hasSpawnedRewards = spawnIndex > 0;

            if (_hasSpawnedRewards && logRewardSpawnsInEditor)
            {
                Debug.Log($"RoomRewardSpawner spawned {spawnIndex} reward pickup(s) for room '{clearedRoom.RoomId}'.", this);
            }
        }

        [ContextMenu("Spawn Debug Rewards")]
        public void SpawnDebugRewards()
        {
            HandleRoomCleared(roomController);
        }

        [ContextMenu("Reset Reward Spawn State")]
        public void ResetRewardSpawnState()
        {
            _hasSpawnedRewards = false;
        }

        private void Awake()
        {
            if (roomController == null)
            {
                roomController = GetComponent<RoomController>();
            }

            if (rewardSpawnAnchor == null)
            {
                rewardSpawnAnchor = transform;
            }
        }

        private RoomRewardTable ResolveRewardTable()
        {
            return _runtimeRewardTableOverride != null ? _runtimeRewardTableOverride : rewardTable;
        }

        private void OnDestroy()
        {
            _isShuttingDown = true;
        }

        private void OnApplicationQuit()
        {
            _isShuttingDown = true;
        }

        private void SpawnRewardInstance(RoomRewardEntry rewardEntry, int spawnIndex)
        {
            GameObject pickupPrefab = rewardEntry.PickupPrefab;

            if (pickupPrefab == null)
            {
                return;
            }

            Vector3 spawnPosition = ResolveSpawnPosition(spawnIndex);
            Quaternion spawnRotation = Quaternion.identity;
            Object instantiatedObject = Object.Instantiate((Object)pickupPrefab, spawnPosition, spawnRotation);

            if (instantiatedObject is GameObject instantiatedGameObject)
            {
                if (spawnedRewardParent != null)
                {
                    instantiatedGameObject.transform.SetParent(spawnedRewardParent, true);
                }

                return;
            }

            if (instantiatedObject is Component instantiatedComponent)
            {
                if (spawnedRewardParent != null)
                {
                    instantiatedComponent.transform.SetParent(spawnedRewardParent, true);
                }

                return;
            }

            Debug.LogWarning(
                $"RoomRewardSpawner instantiated reward '{pickupPrefab.name}', but the result was not a GameObject or Component.",
                this);
        }

        private Vector3 ResolveSpawnPosition(int spawnIndex)
        {
            Vector3 center = rewardSpawnAnchor != null ? rewardSpawnAnchor.position : transform.position;

            if (preferLastEnemyDeathPosition && roomController != null && roomController.TryGetLastEnemyDeathPosition(out Vector3 lastEnemyDeathPosition))
            {
                center = lastEnemyDeathPosition;
            }

            if (scatterRadius <= 0f || spawnIndex <= 0)
            {
                return center;
            }

            float angle = 137.5f * spawnIndex * Mathf.Deg2Rad;
            float radius = scatterRadius * Mathf.Clamp01(0.45f + (0.22f * spawnIndex));
            Vector2 offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            return center + new Vector3(offset.x, offset.y, 0f);
        }

        private static int SelectWeightedIndex(List<RoomRewardEntry> entries)
        {
            float totalWeight = 0f;

            for (int i = 0; i < entries.Count; i++)
            {
                totalWeight += entries[i].Weight;
            }

            if (totalWeight <= 0f)
            {
                return -1;
            }

            float threshold = Random.value * totalWeight;

            for (int i = 0; i < entries.Count; i++)
            {
                threshold -= entries[i].Weight;

                if (threshold <= 0f)
                {
                    return i;
                }
            }

            return entries.Count - 1;
        }

        private static bool IsRewardEligibleRoomType(RoomType roomType)
        {
            return roomType == RoomType.Normal
                || roomType == RoomType.Challenge;
        }

        private void Reset()
        {
            if (roomController == null)
            {
                roomController = GetComponent<RoomController>();
            }

            if (rewardSpawnAnchor == null)
            {
                rewardSpawnAnchor = transform;
            }
        }

        private void OnDrawGizmosSelected()
        {
            Vector3 center = rewardSpawnAnchor != null ? rewardSpawnAnchor.position : transform.position;
            float radius = Mathf.Max(0.05f, scatterRadius);

            Gizmos.color = new Color(1f, 0.84f, 0.2f, 0.9f);
            Gizmos.DrawSphere(center, 0.08f);

            Gizmos.color = new Color(1f, 0.84f, 0.2f, 0.35f);
            Gizmos.DrawWireSphere(center, radius);
        }
    }
}
