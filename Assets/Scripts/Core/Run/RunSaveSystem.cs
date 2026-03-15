using System.Collections.Generic;
using System.IO;
using CuteIssac.Core.Gameplay;
using CuteIssac.Dungeon;
using CuteIssac.Player;
using CuteIssac.Room;
using CuteIssac.UI;
using UnityEngine;

namespace CuteIssac.Core.Run
{
    /// <summary>
    /// Writes a lightweight run snapshot to disk whenever core run progress changes.
    /// This keeps save file ownership in one place while gameplay systems remain unaware of file IO.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RunSaveSystem : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private RunManager runManager;
        [SerializeField] private DungeonInstantiator dungeonInstantiator;
        [SerializeField] private RoomNavigationController roomNavigationController;
        [SerializeField] private MinimapController minimapController;
        [SerializeField] private PlayerStats playerStats;
        [SerializeField] private PlayerInventory playerInventory;
        [SerializeField] private PlayerHealth playerHealth;
        [SerializeField] private PlayerController playerController;
        [SerializeField] private PlayerActiveItemController playerActiveItemController;
        [SerializeField] private PlayerConsumableHolder playerConsumableHolder;
        [SerializeField] private PlayerTrinketHolder playerTrinketHolder;

        [Header("Storage")]
        [SerializeField] private string saveFileName = "run-save.json";
        [SerializeField] private bool saveOnDungeonGenerated = true;
        [SerializeField] private bool saveOnRoomChanged = true;
        [SerializeField] private bool saveOnRoomCleared = true;
        [SerializeField] private bool saveOnResourceChanged = true;
        [SerializeField] private bool saveOnInventoryChanged = true;
        [SerializeField] private bool saveOnHealthChanged = true;
        [SerializeField] private bool saveOnActiveItemChanged = true;
        [SerializeField] private bool saveOnConsumableChanged = true;
        [SerializeField] private bool debounceFrequentSaves = true;
        [SerializeField] [Min(0f)] private float minimumSaveIntervalSeconds = 0.2f;
        [SerializeField] private bool saveOnApplicationQuit = true;

        private readonly List<RoomExplorationSaveRecord> _explorationBuffer = new();
        private bool _saveRequested;
        private float _lastSaveRealtime;

        public string SaveFilePath => Path.Combine(Application.persistentDataPath, saveFileName);

        private void Awake()
        {
            ResolveReferences();
        }

        private void OnEnable()
        {
            ResolveReferences();
            SubscribeToSources();
        }

        private void Start()
        {
            ResolveReferences();

            if (dungeonInstantiator != null && dungeonInstantiator.CurrentInstance != null)
            {
                TryRestoreExplorationSnapshot(dungeonInstantiator.CurrentInstance);
                SaveCurrentRun();
            }
        }

        private void OnDisable()
        {
            FlushPendingSave();
            UnsubscribeFromSources();
        }

        private void OnApplicationQuit()
        {
            if (!saveOnApplicationQuit)
            {
                return;
            }

            SaveCurrentRunImmediate();
        }

        private void Update()
        {
            if (!_saveRequested)
            {
                return;
            }

            if (debounceFrequentSaves && Time.realtimeSinceStartup - _lastSaveRealtime < minimumSaveIntervalSeconds)
            {
                return;
            }

            SaveCurrentRunImmediate();
        }

        private void OnValidate()
        {
            ResolveReferences();
        }

        [ContextMenu("Save Current Run")]
        public void SaveCurrentRun()
        {
            RequestSave();
        }

        private void SaveCurrentRunImmediate()
        {
            RunSaveData saveData = BuildSaveData();

            if (saveData == null)
            {
                return;
            }

            string directory = Path.GetDirectoryName(SaveFilePath);

            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(SaveFilePath, JsonUtility.ToJson(saveData, true));
            _saveRequested = false;
            _lastSaveRealtime = Time.realtimeSinceStartup;
        }

        [ContextMenu("Delete Run Save")]
        public void DeleteRunSave()
        {
            if (File.Exists(SaveFilePath))
            {
                File.Delete(SaveFilePath);
            }
        }

        public bool TryLoadLatestRun(out RunSaveData saveData)
        {
            saveData = null;

            if (!File.Exists(SaveFilePath))
            {
                return false;
            }

            string json = File.ReadAllText(SaveFilePath);

            if (string.IsNullOrWhiteSpace(json))
            {
                return false;
            }

            saveData = JsonUtility.FromJson<RunSaveData>(json);
            return saveData != null;
        }

        private RunSaveData BuildSaveData()
        {
            ResolveReferences();

            RunSaveData saveData = new();
            PopulateRunState(saveData);
            PopulateStats(saveData);
            PopulateInventory(saveData);
            PopulateVisitedRooms(saveData);
            return saveData;
        }

        private void PopulateRunState(RunSaveData saveData)
        {
            if (runManager != null)
            {
                RunContext context = runManager.CurrentContext;
                saveData.DungeonSeed = context.Seed;
                saveData.CurrentFloorIndex = context.CurrentFloorIndex;
                saveData.ClearedRoomCount = context.ClearedRoomCount;
                saveData.TotalClearedRoomCount = context.TotalClearedRoomCount;
                saveData.ResolvedRoomCount = context.ResolvedRoomCount;
                saveData.TotalResolvedRoomCount = context.TotalResolvedRoomCount;
                saveData.BossRoomClearCount = context.BossRoomClearCount;
                saveData.RunState = context.State;
                saveData.EndReason = context.EndReason;
                saveData.CurrentRoomId = roomNavigationController != null && roomNavigationController.CurrentRoom != null
                    ? roomNavigationController.CurrentRoom.RoomId
                    : string.Empty;

                if (playerController != null)
                {
                    Vector3 playerPosition = playerController.transform.position;
                    saveData.PlayerPositionX = playerPosition.x;
                    saveData.PlayerPositionY = playerPosition.y;
                }
                return;
            }

            DungeonInstantiationResult currentInstance = dungeonInstantiator != null ? dungeonInstantiator.CurrentInstance : null;

            if (currentInstance != null && currentInstance.DungeonMap != null)
            {
                saveData.DungeonSeed = currentInstance.DungeonMap.Seed;
                saveData.CurrentFloorIndex = currentInstance.DungeonMap.FloorConfig != null
                    ? currentInstance.DungeonMap.FloorConfig.FloorIndex
                    : 0;
            }
        }

        private void PopulateStats(RunSaveData saveData)
        {
            if (playerStats == null)
            {
                return;
            }

            PlayerStatSnapshot currentStats = playerStats.CurrentStats;
            saveData.PlayerStats.MoveSpeed = currentStats.MoveSpeed;
            saveData.PlayerStats.Damage = currentStats.Damage;
            saveData.PlayerStats.FireInterval = currentStats.FireInterval;
            saveData.PlayerStats.ProjectileSpeed = currentStats.ProjectileSpeed;
            saveData.PlayerStats.ProjectileLifetime = currentStats.ProjectileLifetime;
            saveData.PlayerStats.ProjectileScale = currentStats.ProjectileScale;
            saveData.PlayerStats.CurrentHealth = playerHealth != null ? playerHealth.CurrentHealth : 0f;
        }

        private void PopulateInventory(RunSaveData saveData)
        {
            if (playerInventory == null)
            {
                return;
            }

            saveData.Inventory.Coins = playerInventory.Coins;
            saveData.Inventory.Keys = playerInventory.Keys;
            saveData.Inventory.Bombs = playerInventory.Bombs;
            saveData.Inventory.EquippedActiveItemId = playerActiveItemController != null && playerActiveItemController.EquippedItem != null
                ? playerActiveItemController.EquippedItem.ItemId
                : string.Empty;
            saveData.Inventory.ActiveItemCurrentCharge = playerActiveItemController != null
                ? playerActiveItemController.BuildSlotState().CurrentCharge
                : 0;
            saveData.Inventory.ActiveTimedEffectSourceItemId = playerActiveItemController != null && playerActiveItemController.TimedEffectSourceItem != null
                ? playerActiveItemController.TimedEffectSourceItem.ItemId
                : string.Empty;
            saveData.Inventory.ActiveTimedEffectRemainingSeconds = playerActiveItemController != null
                ? playerActiveItemController.TimedEffectRemainingSeconds
                : 0f;
            saveData.Inventory.HeldConsumableItemId = playerConsumableHolder != null && playerConsumableHolder.HeldConsumable != null
                ? playerConsumableHolder.HeldConsumable.ItemId
                : string.Empty;
            saveData.Inventory.EquippedTrinketItemId = playerTrinketHolder != null && playerTrinketHolder.EquippedTrinket != null
                ? playerTrinketHolder.EquippedTrinket.ItemId
                : string.Empty;
            saveData.Inventory.ConsumableTimedEffectSourceItemId = playerConsumableHolder != null && playerConsumableHolder.TimedEffectSourceConsumable != null
                ? playerConsumableHolder.TimedEffectSourceConsumable.ItemId
                : string.Empty;
            saveData.Inventory.ConsumableTimedEffectRemainingSeconds = playerConsumableHolder != null
                ? playerConsumableHolder.TimedEffectRemainingSeconds
                : 0f;
            saveData.Inventory.PassiveItemIds.Clear();

            IReadOnlyList<Data.Item.ItemData> passiveItems = playerInventory.PassiveItems;

            for (int i = 0; i < passiveItems.Count; i++)
            {
                Data.Item.ItemData itemData = passiveItems[i];

                if (itemData != null && !string.IsNullOrWhiteSpace(itemData.ItemId))
                {
                    saveData.Inventory.PassiveItemIds.Add(itemData.ItemId);
                }
            }
        }

        private void PopulateVisitedRooms(RunSaveData saveData)
        {
            saveData.VisitedRooms.Clear();

            if (minimapController == null)
            {
                return;
            }

            minimapController.CopyExplorationSnapshot(_explorationBuffer);

            for (int i = 0; i < _explorationBuffer.Count; i++)
            {
                saveData.VisitedRooms.Add(_explorationBuffer[i]);
            }
        }

        private void ResolveReferences()
        {
            if (runManager == null)
            {
                runManager = GetComponent<RunManager>();
            }

            if (dungeonInstantiator == null)
            {
                dungeonInstantiator = GetComponent<DungeonInstantiator>();
            }

            if (roomNavigationController == null)
            {
                roomNavigationController = GetComponent<RoomNavigationController>();
            }

            if (minimapController == null)
            {
                minimapController = GetComponent<MinimapController>();
            }

            if (playerStats == null)
            {
                playerStats = FindFirstObjectByType<PlayerStats>(FindObjectsInactive.Exclude);
            }

            if (playerInventory == null)
            {
                playerInventory = FindFirstObjectByType<PlayerInventory>(FindObjectsInactive.Exclude);
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
        }

        private void SubscribeToSources()
        {
            if (runManager != null)
            {
                runManager.RunStarted -= HandleRunStarted;
                runManager.RunStarted += HandleRunStarted;
                runManager.RunEnded -= HandleRunEnded;
                runManager.RunEnded += HandleRunEnded;
                runManager.FloorTransitionCompleted -= HandleFloorTransitionCompleted;
                runManager.FloorTransitionCompleted += HandleFloorTransitionCompleted;
            }

            if (dungeonInstantiator != null)
            {
                dungeonInstantiator.DungeonInstantiated -= HandleDungeonInstantiated;
                dungeonInstantiator.DungeonInstantiated += HandleDungeonInstantiated;
            }

            if (roomNavigationController != null)
            {
                roomNavigationController.CurrentRoomChanged -= HandleCurrentRoomChanged;
                roomNavigationController.CurrentRoomChanged += HandleCurrentRoomChanged;
            }

            if (playerInventory != null)
            {
                playerInventory.ResourcesChanged -= HandleResourcesChanged;
                playerInventory.ResourcesChanged += HandleResourcesChanged;
                playerInventory.InventoryChanged -= HandleInventoryChanged;
                playerInventory.InventoryChanged += HandleInventoryChanged;
            }

            if (playerHealth != null)
            {
                playerHealth.HealthChanged -= HandleHealthChanged;
                playerHealth.HealthChanged += HandleHealthChanged;
            }

            if (playerActiveItemController != null)
            {
                playerActiveItemController.ActiveItemStateChanged -= HandleActiveItemStateChanged;
                playerActiveItemController.ActiveItemStateChanged += HandleActiveItemStateChanged;
            }

            if (playerConsumableHolder != null)
            {
                playerConsumableHolder.ConsumableStateChanged -= HandleConsumableStateChanged;
                playerConsumableHolder.ConsumableStateChanged += HandleConsumableStateChanged;
            }

            if (playerTrinketHolder != null)
            {
                playerTrinketHolder.TrinketChanged -= HandleTrinketChanged;
                playerTrinketHolder.TrinketChanged += HandleTrinketChanged;
            }

            GameplayRuntimeEvents.RoomResolved -= HandleRoomResolved;
            GameplayRuntimeEvents.RoomResolved += HandleRoomResolved;
        }

        private void UnsubscribeFromSources()
        {
            if (runManager != null)
            {
                runManager.RunStarted -= HandleRunStarted;
                runManager.RunEnded -= HandleRunEnded;
                runManager.FloorTransitionCompleted -= HandleFloorTransitionCompleted;
            }

            if (dungeonInstantiator != null)
            {
                dungeonInstantiator.DungeonInstantiated -= HandleDungeonInstantiated;
            }

            if (roomNavigationController != null)
            {
                roomNavigationController.CurrentRoomChanged -= HandleCurrentRoomChanged;
            }

            if (playerInventory != null)
            {
                playerInventory.ResourcesChanged -= HandleResourcesChanged;
                playerInventory.InventoryChanged -= HandleInventoryChanged;
            }

            if (playerHealth != null)
            {
                playerHealth.HealthChanged -= HandleHealthChanged;
            }

            if (playerActiveItemController != null)
            {
                playerActiveItemController.ActiveItemStateChanged -= HandleActiveItemStateChanged;
            }

            if (playerConsumableHolder != null)
            {
                playerConsumableHolder.ConsumableStateChanged -= HandleConsumableStateChanged;
            }

            if (playerTrinketHolder != null)
            {
                playerTrinketHolder.TrinketChanged -= HandleTrinketChanged;
            }

            GameplayRuntimeEvents.RoomResolved -= HandleRoomResolved;
        }

        private void HandleRunStarted(RunContext _)
        {
            SaveCurrentRunImmediate();
        }

        private void HandleRunEnded(RunContext _, RunEndReason __)
        {
            SaveCurrentRunImmediate();
        }

        private void HandleFloorTransitionCompleted(RunFloorTransitionInfo _)
        {
            SaveCurrentRunImmediate();
        }

        private void HandleDungeonInstantiated(DungeonInstantiationResult _)
        {
            TryRestoreExplorationSnapshot(dungeonInstantiator != null ? dungeonInstantiator.CurrentInstance : null);

            if (saveOnDungeonGenerated)
            {
                SaveCurrentRunImmediate();
            }
        }

        private void HandleCurrentRoomChanged(RoomController _)
        {
            if (saveOnRoomChanged)
            {
                RequestSave();
            }
        }

        private void HandleRoomResolved(RoomResolvedSignal _)
        {
            if (saveOnRoomCleared)
            {
                RequestSave();
            }
        }

        private void HandleResourcesChanged(PlayerResourceSnapshot _)
        {
            if (saveOnResourceChanged)
            {
                RequestSave();
            }
        }

        private void HandleInventoryChanged()
        {
            if (saveOnInventoryChanged)
            {
                RequestSave();
            }
        }

        private void HandleHealthChanged(float _, float __)
        {
            if (saveOnHealthChanged)
            {
                RequestSave();
            }
        }

        private void HandleActiveItemStateChanged(PlayerActiveItemSlotState _)
        {
            if (saveOnActiveItemChanged)
            {
                RequestSave();
            }
        }

        private void HandleConsumableStateChanged(PlayerConsumableSlotState _)
        {
            if (saveOnConsumableChanged)
            {
                RequestSave();
            }
        }

        private void HandleTrinketChanged()
        {
            if (saveOnInventoryChanged)
            {
                RequestSave();
            }
        }

        private void RequestSave()
        {
            if (!Application.isPlaying || !debounceFrequentSaves || minimumSaveIntervalSeconds <= 0f)
            {
                SaveCurrentRunImmediate();
                return;
            }

            _saveRequested = true;
        }

        private void FlushPendingSave()
        {
            if (_saveRequested)
            {
                SaveCurrentRunImmediate();
            }
        }

        private void TryRestoreExplorationSnapshot(DungeonInstantiationResult instantiationResult)
        {
            if (instantiationResult == null || minimapController == null)
            {
                return;
            }

            if (!TryLoadLatestRun(out RunSaveData saveData) || saveData == null || saveData.VisitedRooms == null || saveData.VisitedRooms.Count == 0)
            {
                return;
            }

            DungeonMap dungeonMap = instantiationResult.DungeonMap;

            if (dungeonMap == null)
            {
                return;
            }

            int runtimeFloorIndex = dungeonMap.FloorConfig != null ? dungeonMap.FloorConfig.FloorIndex : 0;

            if (saveData.DungeonSeed != dungeonMap.Seed || saveData.CurrentFloorIndex != runtimeFloorIndex)
            {
                return;
            }

            minimapController.ApplyExplorationSnapshot(saveData.VisitedRooms);
        }
    }
}
