using System.Collections.Generic;
using CuteIssac.Core.Feedback;
using CuteIssac.Core.Save;
using CuteIssac.Data.Dungeon;
using CuteIssac.Data.Item;
using CuteIssac.Dungeon;
using CuteIssac.Player;
using CuteIssac.Room;
using UnityEngine;

namespace CuteIssac.Core.Run
{
    /// <summary>
    /// Applies a saved active run snapshot back into runtime systems.
    /// It only restores durable run state; temporary combat buffs are intentionally cleared.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RunRestoreController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameSaveSystem gameSaveSystem;
        [SerializeField] private RunManager runManager;
        [SerializeField] private StartingBuildManager startingBuildManager;
        [SerializeField] private PlayerInventory playerInventory;
        [SerializeField] private PlayerStats playerStats;
        [SerializeField] private PlayerHealth playerHealth;
        [SerializeField] private PlayerController playerController;
        [SerializeField] private PlayerActiveItemController playerActiveItemController;
        [SerializeField] private PlayerConsumableHolder playerConsumableHolder;
        [SerializeField] private PlayerTrinketHolder playerTrinketHolder;
        [SerializeField] private RunItemPoolService runItemPoolService;
        [SerializeField] private DungeonInstantiator dungeonInstantiator;
        [SerializeField] private RoomNavigationController roomNavigationController;

        [Header("Resume Policy")]
        [SerializeField] private RunResumeCurrentRoomPolicy currentRoomResumePolicy = RunResumeCurrentRoomPolicy.RestartEncounter;

        [Header("Feedback")]
        [SerializeField] private bool announceRunResume = true;
        [SerializeField] private bool announceCurrentRoomPolicy = true;
        [SerializeField] private Color runResumeAccentColor = new(0.52f, 0.88f, 1f, 1f);
        [SerializeField] private Color restartEncounterAccentColor = new(1f, 0.72f, 0.34f, 1f);
        [SerializeField] [Min(0.25f)] private float runResumeBannerDuration = 1.6f;
        [SerializeField] [Min(0.25f)] private float policyBannerDuration = 1.45f;

        private readonly Dictionary<string, ItemData> _itemsById = new();
        private readonly Dictionary<string, ActiveItemData> _activeItemsById = new();
        private readonly Dictionary<string, ConsumableItemData> _consumablesById = new();
        private readonly List<ItemData> _resolvedPassiveItems = new();
        private RunSaveData _pendingRestoreSaveData;
        private readonly RunRestoreReport _lastRestoreReport = new();
        private string _pendingEquippedActiveItemId = string.Empty;
        private string _pendingHeldConsumableItemId = string.Empty;
        private string _pendingEquippedTrinketItemId = string.Empty;
        private string _pendingActiveTimedEffectSourceItemId = string.Empty;
        private string _pendingConsumableTimedEffectSourceItemId = string.Empty;
        private bool _pendingActiveTimedEffectRequested;
        private bool _pendingActiveTimedEffectRestored;
        private bool _pendingConsumableTimedEffectRequested;
        private bool _pendingConsumableTimedEffectRestored;

        public string LastRestoreSummary => _lastRestoreReport.BuildSummary();
        public RunRestoreReport LastRestoreReport => _lastRestoreReport;

        private void OnEnable()
        {
            ResolveReferences();

            if (dungeonInstantiator != null)
            {
                dungeonInstantiator.DungeonInstantiated -= HandleDungeonInstantiated;
                dungeonInstantiator.DungeonInstantiated += HandleDungeonInstantiated;
            }
        }

        private void OnDisable()
        {
            if (dungeonInstantiator != null)
            {
                dungeonInstantiator.DungeonInstantiated -= HandleDungeonInstantiated;
            }
        }

        public bool TryResumeLatestRun()
        {
            ResolveReferences();

            if (gameSaveSystem == null || runManager == null)
            {
                SetRestoreReportHeader("런 복원", "사용 불가");
                return false;
            }

            if (!gameSaveSystem.TryLoadRunSnapshot(out RunSaveData saveData) || !IsRestorable(saveData))
            {
                SetRestoreReportHeader("런 복원", "복원 가능한 저장이 없습니다");
                return false;
            }

            startingBuildManager?.SuppressNextRunStartLoadout();
            _pendingRestoreSaveData = saveData;
            SetRestoreReportHeader("런 복원", "복원 준비 중");
            _lastRestoreReport.FloorIndex = saveData.CurrentFloorIndex;
            _lastRestoreReport.RoomId = saveData.CurrentRoomId;
            runManager.StartRestoredRun(saveData);
            ApplySavedPlayerState(saveData);
            return true;
        }

        private bool IsRestorable(RunSaveData saveData)
        {
            if (saveData == null)
            {
                return false;
            }

            if (saveData.CurrentFloorIndex <= 0)
            {
                return false;
            }

            return saveData.RunState != RunState.Defeat
                && saveData.RunState != RunState.Victory
                && saveData.RunState != RunState.FrontEnd
                && saveData.RunState != RunState.Idle;
        }

        private RunResumeCurrentRoomPolicy ResolveCurrentRoomResumePolicy()
        {
            return runManager != null && runManager.Configuration != null
                ? runManager.Configuration.CurrentRoomResumePolicy
                : currentRoomResumePolicy;
        }

        private bool ShouldAnnounceRunResume()
        {
            return runManager != null && runManager.Configuration != null
                ? runManager.Configuration.AnnounceRunResume
                : announceRunResume;
        }

        private bool ShouldAnnounceCurrentRoomPolicy()
        {
            return runManager != null && runManager.Configuration != null
                ? runManager.Configuration.AnnounceCurrentRoomPolicy
                : announceCurrentRoomPolicy;
        }

        private Color ResolveRunResumeAccentColor()
        {
            return runManager != null && runManager.Configuration != null
                ? runManager.Configuration.RunResumeAccentColor
                : runResumeAccentColor;
        }

        private Color ResolveCurrentRoomPolicyAccentColor()
        {
            return runManager != null && runManager.Configuration != null
                ? runManager.Configuration.CurrentRoomPolicyAccentColor
                : restartEncounterAccentColor;
        }

        private float ResolveRunResumeBannerDuration()
        {
            return runManager != null && runManager.Configuration != null
                ? runManager.Configuration.RunResumeBannerDuration
                : runResumeBannerDuration;
        }

        private float ResolveCurrentRoomPolicyBannerDuration()
        {
            return runManager != null && runManager.Configuration != null
                ? runManager.Configuration.CurrentRoomPolicyBannerDuration
                : policyBannerDuration;
        }

        private void ApplySavedPlayerState(RunSaveData saveData)
        {
            ResolveItemIds(saveData.Inventory.PassiveItemIds);
            playerInventory?.ApplyStartingLoadout(
                saveData.Inventory.Coins,
                saveData.Inventory.Keys,
                saveData.Inventory.Bombs,
                _resolvedPassiveItems);

            playerStats?.SetStartingBuildModifiers(null, null);
            playerStats?.SetConsumableRuntimeModifiers(null, null);
            playerStats?.SetActiveItemRuntimeModifiers(null, null);
            playerStats?.SetEventRuntimeModifiers(null, null);
            ActiveItemData equippedActiveItem = ResolveActiveItem(saveData.Inventory.EquippedActiveItemId);
            ConsumableItemData heldConsumableItem = ResolveConsumableItem(saveData.Inventory.HeldConsumableItemId);
            ItemData equippedTrinketItem = ResolveTrinketItem(saveData.Inventory.EquippedTrinketItemId);
            ActiveItemData activeTimedEffectSource = ResolveActiveItem(saveData.Inventory.ActiveTimedEffectSourceItemId);
            ConsumableItemData consumableTimedEffectSource = ResolveConsumableItem(saveData.Inventory.ConsumableTimedEffectSourceItemId);
            _pendingEquippedActiveItemId = equippedActiveItem != null ? equippedActiveItem.ItemId : string.Empty;
            _pendingHeldConsumableItemId = heldConsumableItem != null ? heldConsumableItem.ItemId : string.Empty;
            _pendingEquippedTrinketItemId = equippedTrinketItem != null ? equippedTrinketItem.ItemId : string.Empty;
            _pendingActiveTimedEffectSourceItemId = activeTimedEffectSource != null ? activeTimedEffectSource.ItemId : string.Empty;
            _pendingConsumableTimedEffectSourceItemId = consumableTimedEffectSource != null ? consumableTimedEffectSource.ItemId : string.Empty;
            _pendingActiveTimedEffectRequested = !string.IsNullOrWhiteSpace(saveData.Inventory.ActiveTimedEffectSourceItemId)
                && saveData.Inventory.ActiveTimedEffectRemainingSeconds > 0f;
            _pendingConsumableTimedEffectRequested = !string.IsNullOrWhiteSpace(saveData.Inventory.ConsumableTimedEffectSourceItemId)
                && saveData.Inventory.ConsumableTimedEffectRemainingSeconds > 0f;

            playerActiveItemController?.RestoreForRunResume(
                equippedActiveItem,
                saveData.Inventory.ActiveItemCurrentCharge);
            playerConsumableHolder?.RestoreForRunResume(heldConsumableItem);
            playerTrinketHolder?.RestoreForRunResume(equippedTrinketItem);
            _pendingActiveTimedEffectRestored = playerActiveItemController != null && playerActiveItemController.TryRestoreTimedEffectFromItem(
                activeTimedEffectSource,
                saveData.Inventory.ActiveTimedEffectRemainingSeconds);
            _pendingConsumableTimedEffectRestored = playerConsumableHolder != null && playerConsumableHolder.TryRestoreTimedEffect(
                consumableTimedEffectSource,
                saveData.Inventory.ConsumableTimedEffectRemainingSeconds);
            playerHealth?.RestoreForRunResume(saveData.PlayerStats.CurrentHealth);
            runItemPoolService?.SyncOwnedItems(playerInventory != null ? playerInventory.PassiveItems : null);
        }

        private void HandleDungeonInstantiated(DungeonInstantiationResult instantiationResult)
        {
            if (_pendingRestoreSaveData == null || instantiationResult == null || instantiationResult.DungeonMap == null)
            {
                return;
            }

            if (_pendingRestoreSaveData.DungeonSeed != instantiationResult.DungeonMap.Seed)
            {
                return;
            }

            int runtimeFloorIndex = instantiationResult.DungeonMap.FloorConfig != null
                ? instantiationResult.DungeonMap.FloorConfig.FloorIndex
                : 0;

            if (_pendingRestoreSaveData.CurrentFloorIndex != runtimeFloorIndex)
            {
                return;
            }

            RestoreRoomStates(instantiationResult, _pendingRestoreSaveData);
            RestorePlayerLocation(instantiationResult, _pendingRestoreSaveData, out bool restartedEncounter, out bool returnedToStartRoom);
            RaiseResumeFeedback(runtimeFloorIndex, restartedEncounter, returnedToStartRoom);
            UpdateLastRestoreSummary(runtimeFloorIndex, restartedEncounter, returnedToStartRoom);
            _pendingRestoreSaveData = null;
        }

        private void RestorePlayerLocation(
            DungeonInstantiationResult instantiationResult,
            RunSaveData saveData,
            out bool restartedEncounter,
            out bool returnedToStartRoom)
        {
            restartedEncounter = false;
            returnedToStartRoom = false;
            RoomController targetRoom = ResolveTargetRoom(instantiationResult, saveData.CurrentRoomId);
            bool shouldApplyCurrentRoomPolicy = ShouldApplyCurrentRoomResumePolicy(instantiationResult, targetRoom, saveData);
            Vector3 targetPosition = new(saveData.PlayerPositionX, saveData.PlayerPositionY, playerController != null ? playerController.transform.position.z : 0f);

            if (targetRoom == null)
            {
                targetRoom = instantiationResult.StartRoom;
                shouldApplyCurrentRoomPolicy = false;
                returnedToStartRoom = true;

                if (targetRoom != null)
                {
                    targetPosition = targetRoom.DefaultPlayerSpawnPosition;
                }
            }
            else if (shouldApplyCurrentRoomPolicy)
            {
                switch (ResolveCurrentRoomResumePolicy())
                {
                    case RunResumeCurrentRoomPolicy.ReturnToStartRoom:
                        targetRoom = instantiationResult.StartRoom != null ? instantiationResult.StartRoom : targetRoom;
                        targetPosition = targetRoom.DefaultPlayerSpawnPosition;
                        returnedToStartRoom = true;
                        break;

                    case RunResumeCurrentRoomPolicy.ResumeAtSavedPosition:
                        break;

                    default:
                        targetPosition = targetRoom.DefaultPlayerSpawnPosition;
                        restartedEncounter = true;
                        break;
                }
            }

            if (targetRoom == null)
            {
                return;
            }

            if (roomNavigationController != null)
            {
                roomNavigationController.TryRestoreRoomState(targetRoom, targetPosition);
            }
            else if (playerController != null)
            {
                playerController.transform.position = targetPosition;
            }

            if (restartedEncounter && targetRoom.State == RoomState.Idle)
            {
                targetRoom.EnterRoom();
            }
        }

        private static void RestoreRoomStates(DungeonInstantiationResult instantiationResult, RunSaveData saveData)
        {
            if (instantiationResult == null || saveData == null || saveData.VisitedRooms == null)
            {
                return;
            }

            for (int index = 0; index < saveData.VisitedRooms.Count; index++)
            {
                RoomExplorationSaveRecord roomRecord = saveData.VisitedRooms[index];

                if (!roomRecord.IsCleared)
                {
                    continue;
                }

                GridPosition gridPosition = new(roomRecord.GridX, roomRecord.GridY);

                if (!instantiationResult.TryGetRoom(gridPosition, out RoomController roomController) || roomController == null)
                {
                    continue;
                }

                bool hadCombatEncounter = roomRecord.RoomType == RoomType.Normal
                    || roomRecord.RoomType == RoomType.Challenge
                    || roomRecord.RoomType == RoomType.MiniBoss
                    || roomRecord.RoomType == RoomType.Boss;

                roomController.ApplyRestoredResolvedState(roomRecord.HasRewardContent, hadCombatEncounter);
            }
        }

        private static RoomController ResolveTargetRoom(DungeonInstantiationResult instantiationResult, string roomId)
        {
            if (instantiationResult == null || string.IsNullOrWhiteSpace(roomId))
            {
                return null;
            }

            foreach (KeyValuePair<GridPosition, RoomController> roomPair in instantiationResult.RoomsByPosition)
            {
                RoomController roomController = roomPair.Value;

                if (roomController != null && string.Equals(roomController.RoomId, roomId, System.StringComparison.OrdinalIgnoreCase))
                {
                    return roomController;
                }
            }

            return null;
        }

        private bool ShouldApplyCurrentRoomResumePolicy(
            DungeonInstantiationResult instantiationResult,
            RoomController targetRoom,
            RunSaveData saveData)
        {
            if (instantiationResult == null || targetRoom == null || saveData == null || saveData.VisitedRooms == null)
            {
                return false;
            }

            GridPosition? targetPosition = null;

            foreach (KeyValuePair<GridPosition, RoomController> roomPair in instantiationResult.RoomsByPosition)
            {
                if (roomPair.Value == targetRoom)
                {
                    targetPosition = roomPair.Key;
                    break;
                }
            }

            if (!targetPosition.HasValue)
            {
                return false;
            }

            for (int index = 0; index < saveData.VisitedRooms.Count; index++)
            {
                RoomExplorationSaveRecord candidate = saveData.VisitedRooms[index];

                if (candidate.GridX != targetPosition.Value.X || candidate.GridY != targetPosition.Value.Y)
                {
                    continue;
                }

                if (candidate.IsCleared)
                {
                    return false;
                }

                return candidate.ExplorationState == RoomExplorationState.Current
                    || candidate.ExplorationState == RoomExplorationState.Visited;
            }

            return false;
        }

        private void RaiseResumeFeedback(int floorIndex, bool restartedEncounter, bool returnedToStartRoom)
        {
            Color runResumeColor = ResolveRunResumeAccentColor();
            Color currentRoomPolicyColor = ResolveCurrentRoomPolicyAccentColor();
            float resumeDuration = ResolveRunResumeBannerDuration();
            float currentRoomPolicyDuration = ResolveCurrentRoomPolicyBannerDuration();

            if (ShouldAnnounceRunResume())
            {
                GameplayFeedbackEvents.RaiseBannerFeedback(new BannerFeedbackRequest(
                    "런 복원 완료",
                    $"{Mathf.Max(1, floorIndex)}층",
                    runResumeColor,
                    resumeDuration));
            }

            if (!ShouldAnnounceCurrentRoomPolicy())
            {
                return;
            }

            if (restartedEncounter)
            {
                GameplayFeedbackEvents.RaiseBannerFeedback(new BannerFeedbackRequest(
                    "전투 재시작",
                    "현재 방",
                    currentRoomPolicyColor,
                    currentRoomPolicyDuration));
                return;
            }

            if (returnedToStartRoom)
            {
                GameplayFeedbackEvents.RaiseBannerFeedback(new BannerFeedbackRequest(
                    "안전 복귀",
                    "시작 방",
                    runResumeColor,
                    currentRoomPolicyDuration));
            }
        }

        private void UpdateLastRestoreSummary(int floorIndex, bool restartedEncounter, bool returnedToStartRoom)
        {
            SetRestoreReportHeader("런 복원", "복원 완료");
            _lastRestoreReport.FloorIndex = Mathf.Max(1, floorIndex);
            _lastRestoreReport.RoomId = _pendingRestoreSaveData != null ? _pendingRestoreSaveData.CurrentRoomId : string.Empty;
            _lastRestoreReport.ActiveItemId = _pendingEquippedActiveItemId;
            _lastRestoreReport.ConsumableItemId = _pendingHeldConsumableItemId;
            _lastRestoreReport.TrinketItemId = _pendingEquippedTrinketItemId;
            _lastRestoreReport.ActiveTimedEffectSourceItemId = _pendingActiveTimedEffectSourceItemId;
            _lastRestoreReport.ConsumableTimedEffectSourceItemId = _pendingConsumableTimedEffectSourceItemId;
            _lastRestoreReport.ActiveTimedEffectResult = ResolveEntryResult(_pendingActiveTimedEffectRequested, _pendingActiveTimedEffectRestored, _pendingActiveTimedEffectSourceItemId);
            _lastRestoreReport.ConsumableTimedEffectResult = ResolveEntryResult(_pendingConsumableTimedEffectRequested, _pendingConsumableTimedEffectRestored, _pendingConsumableTimedEffectSourceItemId);
            _lastRestoreReport.RoomPolicyResult = ResolveRoomPolicyResult(restartedEncounter, returnedToStartRoom);
            ClearPendingRestoreSummaryData();
        }

        private void ClearPendingRestoreSummaryData()
        {
            _pendingEquippedActiveItemId = string.Empty;
            _pendingHeldConsumableItemId = string.Empty;
            _pendingEquippedTrinketItemId = string.Empty;
            _pendingActiveTimedEffectSourceItemId = string.Empty;
            _pendingConsumableTimedEffectSourceItemId = string.Empty;
            _pendingActiveTimedEffectRequested = false;
            _pendingActiveTimedEffectRestored = false;
            _pendingConsumableTimedEffectRequested = false;
            _pendingConsumableTimedEffectRestored = false;
        }

        private void SetRestoreReportHeader(string headline, string detail)
        {
            _lastRestoreReport.Headline = headline;
            _lastRestoreReport.Detail = detail;
            _lastRestoreReport.FloorIndex = 0;
            _lastRestoreReport.RoomId = string.Empty;
            _lastRestoreReport.ActiveItemId = string.Empty;
            _lastRestoreReport.ConsumableItemId = string.Empty;
            _lastRestoreReport.TrinketItemId = string.Empty;
            _lastRestoreReport.ActiveTimedEffectSourceItemId = string.Empty;
            _lastRestoreReport.ConsumableTimedEffectSourceItemId = string.Empty;
            _lastRestoreReport.ActiveTimedEffectResult = RunRestoreEntryResult.None;
            _lastRestoreReport.ConsumableTimedEffectResult = RunRestoreEntryResult.None;
            _lastRestoreReport.RoomPolicyResult = RunRestoreRoomPolicyResult.None;
        }

        private static RunRestoreEntryResult ResolveEntryResult(bool requested, bool restored, string sourceItemId)
        {
            if (!requested)
            {
                return RunRestoreEntryResult.None;
            }

            if (restored)
            {
                return RunRestoreEntryResult.Restored;
            }

            return string.IsNullOrWhiteSpace(sourceItemId)
                ? RunRestoreEntryResult.Unavailable
                : RunRestoreEntryResult.Skipped;
        }

        private static RunRestoreRoomPolicyResult ResolveRoomPolicyResult(bool restartedEncounter, bool returnedToStartRoom)
        {
            if (restartedEncounter)
            {
                return RunRestoreRoomPolicyResult.EncounterReset;
            }

            if (returnedToStartRoom)
            {
                return RunRestoreRoomPolicyResult.SafeReturn;
            }

            return RunRestoreRoomPolicyResult.ResumePosition;
        }

        private void ResolveItemIds(IReadOnlyList<string> passiveItemIds)
        {
            _resolvedPassiveItems.Clear();
            RebuildItemIndex();

            if (passiveItemIds == null)
            {
                return;
            }

            for (int index = 0; index < passiveItemIds.Count; index++)
            {
                string itemId = passiveItemIds[index];

                if (string.IsNullOrWhiteSpace(itemId))
                {
                    continue;
                }

                if (_itemsById.TryGetValue(itemId, out ItemData itemData) && itemData != null && !_resolvedPassiveItems.Contains(itemData))
                {
                    _resolvedPassiveItems.Add(itemData);
                }
            }
        }

        private void RebuildItemIndex()
        {
            _itemsById.Clear();
            _activeItemsById.Clear();
            _consumablesById.Clear();

            if (runManager == null)
            {
                return;
            }

            for (int floorIndex = 1; runManager.TryGetFloorConfig(floorIndex, out FloorConfig floorConfig); floorIndex++)
            {
                if (floorConfig == null)
                {
                    continue;
                }

                RegisterPoolItems(floorConfig.TreasureRoomItemPool);
                RegisterPoolItems(floorConfig.ShopRoomItemPool);
                RegisterPoolItems(floorConfig.BossRewardItemPool);
                RegisterPoolItems(floorConfig.SecretRoomItemPool);
            }

            if (runManager.Configuration != null)
            {
                RegisterActiveItems(runManager.Configuration.RestorableActiveItems);
                RegisterConsumableItems(runManager.Configuration.RestorableConsumables);
                RegisterItems(runManager.Configuration.RestorableTrinkets);
            }

            if (playerInventory != null)
            {
                IReadOnlyList<ItemData> ownedItems = playerInventory.PassiveItems;

                for (int index = 0; index < ownedItems.Count; index++)
                {
                    RegisterItem(ownedItems[index]);
                }
            }
        }

        private ActiveItemData ResolveActiveItem(string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId))
            {
                return null;
            }

            return _activeItemsById.TryGetValue(itemId, out ActiveItemData activeItemData) ? activeItemData : null;
        }

        private ItemData ResolveTrinketItem(string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId))
            {
                return null;
            }

            if (!_itemsById.TryGetValue(itemId, out ItemData itemData) || itemData == null)
            {
                return null;
            }

            return itemData.ItemType == ItemType.Trinket ? itemData : null;
        }

        private ConsumableItemData ResolveConsumableItem(string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId))
            {
                return null;
            }

            return _consumablesById.TryGetValue(itemId, out ConsumableItemData consumableItemData) ? consumableItemData : null;
        }

        private void RegisterPoolItems(ItemPoolData itemPool)
        {
            if (itemPool == null)
            {
                return;
            }

            IReadOnlyList<ItemPoolEntry> entries = itemPool.Entries;

            for (int index = 0; index < entries.Count; index++)
            {
                RegisterItem(entries[index].ItemData);
            }
        }

        private void RegisterItems(IReadOnlyList<ItemData> items)
        {
            if (items == null)
            {
                return;
            }

            for (int index = 0; index < items.Count; index++)
            {
                RegisterItem(items[index]);
            }
        }

        private void RegisterItem(ItemData itemData)
        {
            if (itemData == null || string.IsNullOrWhiteSpace(itemData.ItemId) || _itemsById.ContainsKey(itemData.ItemId))
            {
                return;
            }

            _itemsById.Add(itemData.ItemId, itemData);
        }

        private void RegisterActiveItems(IReadOnlyList<ActiveItemData> activeItems)
        {
            if (activeItems == null)
            {
                return;
            }

            for (int index = 0; index < activeItems.Count; index++)
            {
                RegisterActiveItem(activeItems[index]);
            }
        }

        private void RegisterConsumableItems(IReadOnlyList<ConsumableItemData> consumableItems)
        {
            if (consumableItems == null)
            {
                return;
            }

            for (int index = 0; index < consumableItems.Count; index++)
            {
                RegisterConsumableItem(consumableItems[index]);
            }
        }

        private void RegisterActiveItem(ActiveItemData itemData)
        {
            if (itemData == null || string.IsNullOrWhiteSpace(itemData.ItemId) || _activeItemsById.ContainsKey(itemData.ItemId))
            {
                return;
            }

            _activeItemsById.Add(itemData.ItemId, itemData);
        }

        private void RegisterConsumableItem(ConsumableItemData itemData)
        {
            if (itemData == null || string.IsNullOrWhiteSpace(itemData.ItemId) || _consumablesById.ContainsKey(itemData.ItemId))
            {
                return;
            }

            _consumablesById.Add(itemData.ItemId, itemData);
        }

        private void ResolveReferences()
        {
            if (gameSaveSystem == null)
            {
                gameSaveSystem = GetComponent<GameSaveSystem>();
            }

            if (runManager == null)
            {
                runManager = GetComponent<RunManager>();
            }

            if (startingBuildManager == null)
            {
                startingBuildManager = GetComponent<StartingBuildManager>();
            }

            if (runItemPoolService == null)
            {
                runItemPoolService = GetComponent<RunItemPoolService>();
            }

            if (playerInventory == null)
            {
                playerInventory = FindFirstObjectByType<PlayerInventory>(FindObjectsInactive.Exclude);
            }

            if (playerStats == null)
            {
                playerStats = FindFirstObjectByType<PlayerStats>(FindObjectsInactive.Exclude);
            }

            if (playerHealth == null)
            {
                playerHealth = FindFirstObjectByType<PlayerHealth>(FindObjectsInactive.Exclude);
            }

            if (playerController == null)
            {
                playerController = FindFirstObjectByType<PlayerController>(FindObjectsInactive.Exclude);
            }

            if (playerActiveItemController == null)
            {
                playerActiveItemController = FindFirstObjectByType<PlayerActiveItemController>(FindObjectsInactive.Exclude);
            }

            if (playerConsumableHolder == null)
            {
                playerConsumableHolder = FindFirstObjectByType<PlayerConsumableHolder>(FindObjectsInactive.Exclude);
            }

            if (playerTrinketHolder == null)
            {
                playerTrinketHolder = FindFirstObjectByType<PlayerTrinketHolder>(FindObjectsInactive.Exclude);
            }

            if (dungeonInstantiator == null)
            {
                dungeonInstantiator = GetComponent<DungeonInstantiator>();
            }

            if (roomNavigationController == null)
            {
                roomNavigationController = GetComponent<RoomNavigationController>();
            }
        }
    }
}
