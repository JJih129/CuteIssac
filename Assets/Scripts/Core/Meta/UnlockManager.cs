using System;
using System.Collections.Generic;
using CuteIssac.Core.Gameplay;
using CuteIssac.Core.Run;
using CuteIssac.Data.Dungeon;
using CuteIssac.Data.Item;
using CuteIssac.Data.Unlock;
using CuteIssac.Player;
using UnityEngine;

namespace CuteIssac.Core.Meta
{
    /// <summary>
    /// Persistent meta progression service.
    /// Loads unlock rules from Resources and stores unlocked keys to disk so future runs can read them.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class UnlockManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private RunManager runManager;
        [SerializeField] private RunItemPoolService runItemPoolService;
        [SerializeField] private PlayerItemManager playerItemManager;

        [Header("Data")]
        [SerializeField] private string unlockResourcePath = "Unlocks";

        [Header("Debug")]
        [SerializeField] private bool verboseLogging = true;

        public event Action UnlockStateChanged;

        public static UnlockManager Current { get; private set; }

        private readonly HashSet<string> _unlockedKeys = new(StringComparer.OrdinalIgnoreCase);
        private readonly List<UnlockData> _unlockDefinitions = new();
        private readonly List<string> _itemUnlockBuffer = new();
        public static bool IsUnlocked(string unlockKey, bool unlockedByDefault = true)
        {
            if (string.IsNullOrWhiteSpace(unlockKey))
            {
                return unlockedByDefault;
            }

            return Current != null
                ? Current.IsUnlockedInternal(unlockKey, unlockedByDefault)
                : unlockedByDefault;
        }

        public static bool IsRoomTypeUnlocked(RoomType roomType)
        {
            return Current == null || Current.IsRoomTypeUnlockedInternal(roomType);
        }

        private void Awake()
        {
            if (Current != null && Current != this)
            {
                Destroy(this);
                return;
            }

            Current = this;
            ResolveReferences();
            LoadDefinitions();
            SyncRuntimeUnlocks();
        }

        private void OnEnable()
        {
            ResolveReferences();
            Subscribe();
            TryBindPlayerItemManager();
            SyncRuntimeUnlocks();
            EvaluateCurrentFloor();
        }

        private void OnDisable()
        {
            Unsubscribe();
            UnbindPlayerItemManager();

            if (Current == this)
            {
                Current = null;
            }
        }

        [ContextMenu("Clear Meta Unlock Save")]
        public void ClearSaveData()
        {
            _unlockedKeys.Clear();
            SyncRuntimeUnlocks();
            UnlockStateChanged?.Invoke();
        }

        public bool HasUnlocked(string unlockKey)
        {
            return IsUnlockedInternal(unlockKey, false);
        }

        public UnlockSaveData ExportSaveData()
        {
            UnlockSaveData saveData = new();
            saveData.UnlockedKeys.AddRange(_unlockedKeys);
            return saveData;
        }

        public void ImportSaveData(UnlockSaveData saveData)
        {
            _unlockedKeys.Clear();

            if (saveData?.UnlockedKeys != null)
            {
                for (int index = 0; index < saveData.UnlockedKeys.Count; index++)
                {
                    string unlockKey = saveData.UnlockedKeys[index];

                    if (!string.IsNullOrWhiteSpace(unlockKey))
                    {
                        _unlockedKeys.Add(unlockKey);
                    }
                }
            }

            SyncRuntimeUnlocks();
        }

        private void Subscribe()
        {
            if (runManager != null)
            {
                runManager.RunStarted -= HandleRunStarted;
                runManager.RunStarted += HandleRunStarted;
                runManager.FloorTransitionCompleted -= HandleFloorTransitionCompleted;
                runManager.FloorTransitionCompleted += HandleFloorTransitionCompleted;
            }

            GameplayRuntimeEvents.EnemyKilled -= HandleEnemyKilled;
            GameplayRuntimeEvents.EnemyKilled += HandleEnemyKilled;
        }

        private void Unsubscribe()
        {
            if (runManager != null)
            {
                runManager.RunStarted -= HandleRunStarted;
                runManager.FloorTransitionCompleted -= HandleFloorTransitionCompleted;
            }

            GameplayRuntimeEvents.EnemyKilled -= HandleEnemyKilled;
        }

        private void HandleRunStarted(RunContext context)
        {
            TryBindPlayerItemManager();
            SyncRuntimeUnlocks();
            EvaluateFloorReached(context != null ? context.CurrentFloorIndex : 1);
        }

        private void HandleFloorTransitionCompleted(RunFloorTransitionInfo info)
        {
            TryBindPlayerItemManager();
            EvaluateFloorReached(info.NextFloorIndex);
        }

        private void HandleEnemyKilled(EnemyKilledSignal signal)
        {
            for (int index = 0; index < _unlockDefinitions.Count; index++)
            {
                UnlockData definition = _unlockDefinitions[index];

                if (definition == null || definition.ConditionType != UnlockConditionType.BossKill)
                {
                    continue;
                }

                if (!MatchesEnemyId(definition.RequiredEnemyId, signal.EnemyId))
                {
                    continue;
                }

                TryUnlock(definition, $"boss kill {signal.EnemyId}");
            }
        }

        private void HandlePassiveItemAcquired(ItemData itemData)
        {
            if (itemData == null || string.IsNullOrWhiteSpace(itemData.ItemId))
            {
                return;
            }

            for (int index = 0; index < _unlockDefinitions.Count; index++)
            {
                UnlockData definition = _unlockDefinitions[index];

                if (definition == null || definition.ConditionType != UnlockConditionType.AcquireItem)
                {
                    continue;
                }

                if (!string.Equals(definition.RequiredItemId, itemData.ItemId, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                TryUnlock(definition, $"item acquired {itemData.ItemId}");
            }
        }

        private void EvaluateFloorReached(int floorIndex)
        {
            for (int index = 0; index < _unlockDefinitions.Count; index++)
            {
                UnlockData definition = _unlockDefinitions[index];

                if (definition == null || definition.ConditionType != UnlockConditionType.ReachFloor)
                {
                    continue;
                }

                if (floorIndex < definition.RequiredFloorIndex)
                {
                    continue;
                }

                TryUnlock(definition, $"floor reached {floorIndex}");
            }
        }

        private void EvaluateCurrentFloor()
        {
            if (runManager != null && runManager.CurrentContext.HasActiveRun)
            {
                EvaluateFloorReached(runManager.CurrentContext.CurrentFloorIndex);
            }
        }

        private bool TryUnlock(UnlockData definition, string reason)
        {
            if (definition == null || string.IsNullOrWhiteSpace(definition.TargetKey))
            {
                return false;
            }

            if (!_unlockedKeys.Add(definition.TargetKey))
            {
                return false;
            }

            SyncRuntimeUnlocks();
            UnlockStateChanged?.Invoke();

            if (verboseLogging)
            {
                UnityEngine.Debug.Log($"UnlockManager unlocked '{definition.DisplayName}' via {reason}.", this);
            }

            return true;
        }

        private bool IsUnlockedInternal(string unlockKey, bool unlockedByDefault)
        {
            if (string.IsNullOrWhiteSpace(unlockKey))
            {
                return unlockedByDefault;
            }

            return unlockedByDefault || _unlockedKeys.Contains(unlockKey);
        }

        private bool IsRoomTypeUnlockedInternal(RoomType roomType)
        {
            bool hasRoomTypeDefinition = false;

            for (int index = 0; index < _unlockDefinitions.Count; index++)
            {
                UnlockData definition = _unlockDefinitions[index];

                if (definition == null || !definition.TargetsRoomType(roomType))
                {
                    continue;
                }

                hasRoomTypeDefinition = true;

                if (_unlockedKeys.Contains(definition.TargetKey))
                {
                    return true;
                }
            }

            return !hasRoomTypeDefinition;
        }

        private void ResolveReferences()
        {
            if (runManager == null)
            {
                runManager = GetComponent<RunManager>();
            }

            if (runItemPoolService == null)
            {
                runItemPoolService = GetComponent<RunItemPoolService>();
            }
        }

        private void TryBindPlayerItemManager()
        {
            if (playerItemManager == null)
            {
                playerItemManager = FindFirstObjectByType<PlayerItemManager>(FindObjectsInactive.Exclude);
            }

            if (playerItemManager == null)
            {
                return;
            }

            playerItemManager.PassiveItemAcquired -= HandlePassiveItemAcquired;
            playerItemManager.PassiveItemAcquired += HandlePassiveItemAcquired;
        }

        private void UnbindPlayerItemManager()
        {
            if (playerItemManager != null)
            {
                playerItemManager.PassiveItemAcquired -= HandlePassiveItemAcquired;
            }
        }

        private void LoadDefinitions()
        {
            _unlockDefinitions.Clear();
            UnlockData[] loadedDefinitions = Resources.LoadAll<UnlockData>(unlockResourcePath);

            for (int index = 0; index < loadedDefinitions.Length; index++)
            {
                UnlockData definition = loadedDefinitions[index];

                if (definition != null)
                {
                    _unlockDefinitions.Add(definition);
                }
            }
        }

        private void SyncRuntimeUnlocks()
        {
            if (runItemPoolService == null)
            {
                runItemPoolService = GetComponent<RunItemPoolService>();
            }

            if (runItemPoolService == null)
            {
                return;
            }

            _itemUnlockBuffer.Clear();

            for (int index = 0; index < _unlockDefinitions.Count; index++)
            {
                UnlockData definition = _unlockDefinitions[index];

                if (definition == null ||
                    definition.TargetType != UnlockTargetType.Item ||
                    string.IsNullOrWhiteSpace(definition.TargetKey) ||
                    !_unlockedKeys.Contains(definition.TargetKey))
                {
                    continue;
                }

                _itemUnlockBuffer.Add(definition.TargetKey);
            }

            for (int index = 0; index < _itemUnlockBuffer.Count; index++)
            {
                runItemPoolService.GrantUnlock(_itemUnlockBuffer[index]);
            }
        }

        private static bool MatchesEnemyId(string requiredEnemyId, string actualEnemyId)
        {
            if (string.IsNullOrWhiteSpace(requiredEnemyId) || string.IsNullOrWhiteSpace(actualEnemyId))
            {
                return false;
            }

            return string.Equals(requiredEnemyId.Trim(), actualEnemyId.Trim(), StringComparison.OrdinalIgnoreCase);
        }
    }
}
