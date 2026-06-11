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
        [SerializeField] private MetaProgressionManager metaProgressionManager;
        [SerializeField] private PlayerItemManager playerItemManager;
        [SerializeField] private PlayerActiveItemController playerActiveItemController;

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

        public bool GrantUnlockKey(string unlockKey)
        {
            if (string.IsNullOrWhiteSpace(unlockKey))
            {
                return false;
            }

            if (!_unlockedKeys.Add(unlockKey))
            {
                return false;
            }

            SyncRuntimeUnlocks();
            UnlockStateChanged?.Invoke();
            return true;
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

            if (metaProgressionManager != null)
            {
                metaProgressionManager.ProgressionChanged -= HandleProgressionChanged;
                metaProgressionManager.ProgressionChanged += HandleProgressionChanged;
            }
        }

        private void Unsubscribe()
        {
            if (runManager != null)
            {
                runManager.RunStarted -= HandleRunStarted;
                runManager.FloorTransitionCompleted -= HandleFloorTransitionCompleted;
            }

            GameplayRuntimeEvents.EnemyKilled -= HandleEnemyKilled;

            if (metaProgressionManager != null)
            {
                metaProgressionManager.ProgressionChanged -= HandleProgressionChanged;
            }
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

                if (definition == null ||
                    (definition.ConditionType != UnlockConditionType.BossKill &&
                     definition.ConditionType != UnlockConditionType.CumulativeEnemyKillCount))
                {
                    continue;
                }

                if (!MatchesEnemyId(definition.RequiredEnemyId, signal.EnemyId))
                {
                    continue;
                }

                if (definition.ConditionType == UnlockConditionType.BossKill)
                {
                    TryUnlock(definition, $"enemy kill {signal.EnemyId}");
                }
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

                if (definition == null ||
                    (definition.ConditionType != UnlockConditionType.AcquireItem &&
                     definition.ConditionType != UnlockConditionType.ItemDiscovered))
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

        private void HandleActiveItemEquipped(ActiveItemData activeItemData)
        {
            if (activeItemData == null || string.IsNullOrWhiteSpace(activeItemData.ItemId))
            {
                return;
            }

            for (int index = 0; index < _unlockDefinitions.Count; index++)
            {
                UnlockData definition = _unlockDefinitions[index];

                if (definition == null ||
                    (definition.ConditionType != UnlockConditionType.AcquireItem &&
                     definition.ConditionType != UnlockConditionType.ItemDiscovered))
                {
                    continue;
                }

                if (!string.Equals(definition.RequiredItemId, activeItemData.ItemId, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                TryUnlock(definition, $"active item equipped {activeItemData.ItemId}");
            }
        }

        public void EvaluateProgressionUnlocks(MetaProgressionSaveData progression)
        {
            if (progression == null)
            {
                return;
            }

            for (int index = 0; index < _unlockDefinitions.Count; index++)
            {
                UnlockData definition = _unlockDefinitions[index];

                if (definition == null || !IsMetaConditionMet(definition, progression))
                {
                    continue;
                }

                TryUnlock(definition, $"meta condition {definition.ConditionType}");
            }
        }

        private void HandleProgressionChanged()
        {
            if (metaProgressionManager != null)
            {
                EvaluateProgressionUnlocks(metaProgressionManager.Progression);
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

            if (metaProgressionManager != null)
            {
                EvaluateProgressionUnlocks(metaProgressionManager.Progression);
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

            if (metaProgressionManager == null)
            {
                metaProgressionManager = GetComponent<MetaProgressionManager>();
            }
        }

        private void TryBindPlayerItemManager()
        {
            if (playerItemManager == null)
            {
                playerItemManager = FindFirstObjectByType<PlayerItemManager>(FindObjectsInactive.Exclude);
            }

            if (playerItemManager != null)
            {
                playerItemManager.PassiveItemAcquired -= HandlePassiveItemAcquired;
                playerItemManager.PassiveItemAcquired += HandlePassiveItemAcquired;
            }

            TryBindPlayerActiveItemController();
        }

        private void TryBindPlayerActiveItemController()
        {
            if (playerActiveItemController == null)
            {
                playerActiveItemController = FindFirstObjectByType<PlayerActiveItemController>(FindObjectsInactive.Exclude);
            }

            if (playerActiveItemController == null)
            {
                return;
            }

            playerActiveItemController.ActiveItemEquipped -= HandleActiveItemEquipped;
            playerActiveItemController.ActiveItemEquipped += HandleActiveItemEquipped;
        }

        private void UnbindPlayerItemManager()
        {
            if (playerItemManager != null)
            {
                playerItemManager.PassiveItemAcquired -= HandlePassiveItemAcquired;
            }

            if (playerActiveItemController != null)
            {
                playerActiveItemController.ActiveItemEquipped -= HandleActiveItemEquipped;
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

            runItemPoolService.ClearUnlocks();
            _itemUnlockBuffer.Clear();

            foreach (string unlockedKey in _unlockedKeys)
            {
                if (!string.IsNullOrWhiteSpace(unlockedKey))
                {
                    _itemUnlockBuffer.Add(unlockedKey);
                }
            }

            for (int index = 0; index < _unlockDefinitions.Count; index++)
            {
                UnlockData definition = _unlockDefinitions[index];

                if (definition == null ||
                    (definition.TargetType != UnlockTargetType.Item && definition.TargetType != UnlockTargetType.ShopItem) ||
                    string.IsNullOrWhiteSpace(definition.TargetKey) ||
                    !_unlockedKeys.Contains(definition.TargetKey))
                {
                    continue;
                }

                if (!_itemUnlockBuffer.Contains(definition.TargetKey))
                {
                    _itemUnlockBuffer.Add(definition.TargetKey);
                }
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

        private static bool IsMetaConditionMet(UnlockData definition, MetaProgressionSaveData progression)
        {
            return definition.ConditionType switch
            {
                UnlockConditionType.CumulativeEnemyKillCount => GetCounterValue(progression.EnemyKillCounts, definition.RequiredEnemyId) >= definition.RequiredCount,
                UnlockConditionType.CharacterClearMark => HasCharacterClearMark(progression, definition.RequiredCharacterId, definition.RequiredClearMark),
                UnlockConditionType.ItemDiscovered => ContainsId(progression.DiscoveredItemIds, definition.RequiredItemId),
                UnlockConditionType.RoomTypeClearCount => GetCounterValue(progression.RoomTypeClearCounts, definition.RequiredRoomType.ToString()) >= definition.RequiredCount,
                UnlockConditionType.ReachFloor => progression.BestFloor >= definition.RequiredFloorIndex,
                UnlockConditionType.AcquireItem => ContainsId(progression.DiscoveredItemIds, definition.RequiredItemId),
                UnlockConditionType.TotalRuns => progression.TotalRuns >= definition.RequiredCount,
                UnlockConditionType.TotalWins => progression.TotalWins >= definition.RequiredCount,
                UnlockConditionType.TotalEnemyKills => progression.TotalEnemyKills >= definition.RequiredCount,
                UnlockConditionType.CurrentWinStreak => progression.CurrentWinStreak >= definition.RequiredCount,
                UnlockConditionType.BestWinStreak => progression.BestWinStreak >= definition.RequiredCount,
                UnlockConditionType.AchievementCompleted => ContainsId(progression.CompletedAchievementIds, definition.RequiredAchievementId),
                _ => false
            };
        }

        private static int GetCounterValue(List<MetaProgressionCounterRecord> records, string id)
        {
            if (records == null || string.IsNullOrWhiteSpace(id))
            {
                return 0;
            }

            for (int index = 0; index < records.Count; index++)
            {
                MetaProgressionCounterRecord record = records[index];

                if (record != null && string.Equals(record.Id, id, StringComparison.OrdinalIgnoreCase))
                {
                    return Mathf.Max(0, record.Count);
                }
            }

            return 0;
        }

        private static bool HasCharacterClearMark(MetaProgressionSaveData progression, string characterId, string clearMark)
        {
            if (progression?.CharacterRecords == null || string.IsNullOrWhiteSpace(characterId))
            {
                return false;
            }

            string resolvedClearMark = string.IsNullOrWhiteSpace(clearMark) ? "run_victory" : clearMark;

            for (int index = 0; index < progression.CharacterRecords.Count; index++)
            {
                CharacterProgressionRecord record = progression.CharacterRecords[index];

                if (record == null || !string.Equals(record.CharacterId, characterId, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                return ContainsId(record.ClearMarks, resolvedClearMark);
            }

            return false;
        }

        private static bool ContainsId(List<string> values, string id)
        {
            if (values == null || string.IsNullOrWhiteSpace(id))
            {
                return false;
            }

            for (int index = 0; index < values.Count; index++)
            {
                if (string.Equals(values[index], id, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
