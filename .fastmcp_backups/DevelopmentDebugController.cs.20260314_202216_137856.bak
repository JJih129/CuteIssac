using System.Collections.Generic;
using CuteIssac.Core.Run;
using CuteIssac.Core.Spawning;
using CuteIssac.Data.Balance;
using CuteIssac.Data.Debug;
using CuteIssac.Data.Item;
using CuteIssac.Dungeon;
using CuteIssac.Enemy;
using CuteIssac.Player;
using CuteIssac.Room;
using CuteIssac.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace CuteIssac.Core.Debug
{
    /// <summary>
    /// Development-only control surface for rapid prototype iteration.
    /// It stays separate from gameplay systems and only calls public runtime APIs.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DevelopmentDebugController : MonoBehaviour
    {
        [Header("Availability")]
        [SerializeField] private bool enableInEditor = true;
        [SerializeField] private bool enableInDevelopmentBuild = true;
        [SerializeField] private Key toggleKey = Key.F9;

        [Header("References")]
        [SerializeField] private RunManager runManager;
        [SerializeField] private RoomNavigationController roomNavigationController;
        [SerializeField] private PlayerInventory playerInventory;
        [SerializeField] private PlayerItemManager playerItemManager;
        [SerializeField] private PlayerHealth playerHealth;
        [SerializeField] private RunSaveSystem runSaveSystem;
        [SerializeField] private RunRestoreController runRestoreController;
        [SerializeField] private DevelopmentDebugPanelView panelView;

        [Header("Data")]
        [SerializeField] private DevelopmentDebugCatalog debugCatalog;
        [SerializeField] private string resourcesCatalogPath = "Debug/DefaultDevelopmentDebugCatalog";
        [SerializeField] private BalanceConfig balanceConfig;
        [SerializeField] private string balanceResourcesPath = "Balance/DefaultBalanceConfig";

        private readonly List<DebugPanelButtonModel> _buttonModels = new();

        private void Awake()
        {
            ResolveReferences();
            ResolveCatalog();

            if (!IsDebugPanelAllowed())
            {
                enabled = false;
                return;
            }
        }

        private void OnEnable()
        {
            ResolveReferences();
            ResolveCatalog();

            if (!IsDebugPanelAllowed())
            {
                enabled = false;
                return;
            }

            EnsurePanelView();
            RebuildPanel();
            panelView?.Hide();
        }

        private void OnDisable()
        {
            panelView?.Hide();
        }

        private void Update()
        {
            if (!IsDebugPanelAllowed() || Keyboard.current == null)
            {
                return;
            }

            if (Keyboard.current[toggleKey].wasPressedThisFrame)
            {
                TogglePanel();
            }
        }

        private void TogglePanel()
        {
            if (panelView == null)
            {
                return;
            }

            if (panelView.IsVisible)
            {
                panelView.Hide();
                return;
            }

            RebuildPanel();
            panelView.Present(_buttonModels, BuildSubtitle(), BuildBalanceSnapshot());
        }

        private void RebuildPanel()
        {
            _buttonModels.Clear();
            _buttonModels.Add(new DebugPanelButtonModel("+5 코인", new Color(0.65f, 0.56f, 0.18f, 1f), () => playerInventory?.AddCoins(5)));
            _buttonModels.Add(new DebugPanelButtonModel("+1 열쇠", new Color(0.28f, 0.46f, 0.7f, 1f), () => playerInventory?.AddKeys(1)));
            _buttonModels.Add(new DebugPanelButtonModel("+1 폭탄", new Color(0.36f, 0.52f, 0.36f, 1f), () => playerInventory?.AddBombs(1)));
            _buttonModels.Add(new DebugPanelButtonModel("런 저장", new Color(0.28f, 0.56f, 0.74f, 1f), SaveRunSnapshot));
            _buttonModels.Add(new DebugPanelButtonModel("런 복원", new Color(0.62f, 0.44f, 0.22f, 1f), ResumeRunSnapshot));
            _buttonModels.Add(new DebugPanelButtonModel("저장 삭제", new Color(0.54f, 0.24f, 0.24f, 1f), ClearRunSnapshot));
            _buttonModels.Add(new DebugPanelButtonModel("현재 방 즉시 클리어", new Color(0.3f, 0.6f, 0.32f, 1f), ClearCurrentRoom));
            _buttonModels.Add(new DebugPanelButtonModel("다음 층 이동", new Color(0.28f, 0.52f, 0.76f, 1f), GoToNextFloor));
            _buttonModels.Add(new DebugPanelButtonModel("여기에 보스 소환", new Color(0.74f, 0.34f, 0.28f, 1f), SpawnBossInCurrentRoom));
            _buttonModels.Add(new DebugPanelButtonModel("밸런스 스냅샷 로그", new Color(0.56f, 0.48f, 0.24f, 1f), LogBalanceSnapshot));
            _buttonModels.Add(new DebugPanelButtonModel("스폰 스냅샷 로그", new Color(0.42f, 0.34f, 0.18f, 1f), LogSpawnSnapshot));
            _buttonModels.Add(new DebugPanelButtonModel("복원 요약 로그", new Color(0.46f, 0.3f, 0.58f, 1f), LogRestoreSummary));
            _buttonModels.Add(new DebugPanelButtonModel(
                playerHealth != null && playerHealth.IsDebugInvulnerable ? "무적: 켜짐" : "무적: 꺼짐",
                playerHealth != null && playerHealth.IsDebugInvulnerable ? new Color(0.74f, 0.22f, 0.22f, 1f) : new Color(0.36f, 0.28f, 0.62f, 1f),
                ToggleInvincible));

            if (debugCatalog != null)
            {
                IReadOnlyList<ItemData> items = debugCatalog.GrantableItems;

                for (int index = 0; index < items.Count; index++)
                {
                    ItemData itemData = items[index];

                    if (itemData == null)
                    {
                        continue;
                    }

                    _buttonModels.Add(new DebugPanelButtonModel(
                        $"아이템 지급: {itemData.DisplayName}",
                        new Color(0.22f, 0.42f, 0.58f, 1f),
                        () => GrantItem(itemData)));
                }
            }
        }

        private void ClearCurrentRoom()
        {
            roomNavigationController?.CurrentRoom?.ClearRoom();
            RefreshVisiblePanel();
        }

        private void SaveRunSnapshot()
        {
            runSaveSystem?.SaveCurrentRun();
            RefreshVisiblePanel();
        }

        private void ResumeRunSnapshot()
        {
            if (runRestoreController == null || runSaveSystem == null)
            {
                return;
            }

            if (!runSaveSystem.TryLoadLatestRun(out _))
            {
                UnityEngine.Debug.LogWarning("DevelopmentDebugController could not find a run snapshot to resume.", this);
                RefreshVisiblePanel();
                return;
            }

            runManager?.ReturnToFrontEnd();
            runRestoreController.TryResumeLatestRun();
            RefreshVisiblePanel();
        }

        private void ClearRunSnapshot()
        {
            runSaveSystem?.DeleteRunSave();
            RefreshVisiblePanel();
        }

        private void GoToNextFloor()
        {
            if (runManager != null && runManager.CurrentContext.HasActiveRun)
            {
                int nextFloorIndex = runManager.CurrentContext.CurrentFloorIndex + 1;

                if (runManager.HasFloor(nextFloorIndex))
                {
                    runManager.AdvanceFloor();
                }
            }

            RefreshVisiblePanel();
        }

        private void SpawnBossInCurrentRoom()
        {
            RoomController currentRoom = roomNavigationController != null ? roomNavigationController.CurrentRoom : null;
            EnemyController bossPrefab = debugCatalog != null ? debugCatalog.BossPrefab : null;

            if (currentRoom == null || bossPrefab == null)
            {
                return;
            }

            currentRoom.DebugForceCombatState();
            Vector3 spawnPosition = currentRoom.CameraFocusPosition + new Vector3(0f, 0.75f, 0f);
            EnemyController spawnedBoss = GameplaySpawnFactory.SpawnComponent(
                bossPrefab,
                spawnPosition,
                Quaternion.identity,
                currentRoom.transform,
                SpawnReusePolicy.Instantiate);

            if (spawnedBoss == null)
            {
                return;
            }

            RoomEnemyMember enemyMember = spawnedBoss.GetComponent<RoomEnemyMember>();

            if (enemyMember == null)
            {
                enemyMember = spawnedBoss.gameObject.AddComponent<RoomEnemyMember>();
            }

            enemyMember.AssignRoom(currentRoom);
            RefreshVisiblePanel();
        }

        private void ToggleInvincible()
        {
            if (playerHealth == null)
            {
                return;
            }

            playerHealth.SetDebugInvulnerable(!playerHealth.IsDebugInvulnerable);
            RefreshVisiblePanel();
        }

        private void GrantItem(ItemData itemData)
        {
            playerItemManager?.AcquirePassiveItem(itemData);
            RefreshVisiblePanel();
        }

        private void RefreshVisiblePanel()
        {
            if (panelView == null || !panelView.IsVisible)
            {
                return;
            }

            RebuildPanel();
            panelView.Present(_buttonModels, BuildSubtitle(), BuildBalanceSnapshot());
        }

        private string BuildSubtitle()
        {
            int floor = runManager != null && runManager.CurrentContext.HasActiveRun
                ? runManager.CurrentContext.CurrentFloorIndex
                : 0;
            string roomLabel = roomNavigationController != null && roomNavigationController.CurrentRoom != null
                ? roomNavigationController.CurrentRoom.RoomId
                : "없음";
            return $"{floor}층 · 방 {roomLabel} · F9 토글";
        }

        private string BuildBalanceSnapshot()
        {
            string balanceSnapshot = BalanceDebugSnapshotBuilder.BuildSnapshot(balanceConfig, runManager, roomNavigationController);
            string spawnSnapshot = GameplaySpawnTelemetry.BuildSummary();
            string runSaveSnapshot = BuildRunSaveSnapshotSummary();
            return $"{balanceSnapshot}\n\n{spawnSnapshot}\n\n{runSaveSnapshot}";
        }

        private string BuildRunSaveSnapshotSummary()
        {
            string restoreSummary = BuildRestoreReportSummary();

            if (runSaveSystem == null)
            {
                return $"런 저장\n- 사용 불가\n\n{restoreSummary}";
            }

            if (!runSaveSystem.TryLoadLatestRun(out RunSaveData saveData) || saveData == null)
            {
                return $"런 저장\n- 경로: {runSaveSystem.SaveFilePath}\n- 상태: 없음\n\n{restoreSummary}";
            }

            string roomLabel = string.IsNullOrWhiteSpace(saveData.CurrentRoomId) ? "없음" : saveData.CurrentRoomId;
            string activeItemLabel = string.IsNullOrWhiteSpace(saveData.Inventory.EquippedActiveItemId) ? "없음" : saveData.Inventory.EquippedActiveItemId;
            string consumableLabel = string.IsNullOrWhiteSpace(saveData.Inventory.HeldConsumableItemId) ? "없음" : saveData.Inventory.HeldConsumableItemId;
            string activeTimedLabel = string.IsNullOrWhiteSpace(saveData.Inventory.ActiveTimedEffectSourceItemId) ? "없음" : saveData.Inventory.ActiveTimedEffectSourceItemId;
            string consumableTimedLabel = string.IsNullOrWhiteSpace(saveData.Inventory.ConsumableTimedEffectSourceItemId) ? "없음" : saveData.Inventory.ConsumableTimedEffectSourceItemId;

            string runSaveSummary =
                $"런 저장\n- 경로: {runSaveSystem.SaveFilePath}\n- 층: {saveData.CurrentFloorIndex}\n- 방: {roomLabel}\n- 액티브: {activeItemLabel} ({saveData.Inventory.ActiveItemCurrentCharge})\n- 소비형: {consumableLabel}";
            string timedSummary =
                $"\n- 액티브 버프: {activeTimedLabel} ({saveData.Inventory.ActiveTimedEffectRemainingSeconds:0.0}초)\n- 소비형 버프: {consumableTimedLabel} ({saveData.Inventory.ConsumableTimedEffectRemainingSeconds:0.0}초)";

            return $"{runSaveSummary}{timedSummary}\n\n{restoreSummary}";
        }

        private string BuildRestoreReportSummary()
        {
            if (runRestoreController == null)
            {
                return "런 복원\n- 사용 불가";
            }

            RunRestoreReport report = runRestoreController.LastRestoreReport;

            if (report == null)
            {
                return "런 복원\n- 사용 불가";
            }

            return report.BuildSummary();
        }

        private void LogBalanceSnapshot()
        {
            UnityEngine.Debug.Log(BalanceDebugSnapshotBuilder.BuildSnapshot(balanceConfig, runManager, roomNavigationController), this);
            RefreshVisiblePanel();
        }

        private void LogSpawnSnapshot()
        {
            UnityEngine.Debug.Log(GameplaySpawnTelemetry.BuildSummary(), this);
            RefreshVisiblePanel();
        }

        private void LogRestoreSummary()
        {
            UnityEngine.Debug.Log(BuildRestoreReportSummary(), this);
            RefreshVisiblePanel();
        }

        private void ResolveReferences()
        {
            if (runManager == null)
            {
                runManager = GetComponent<RunManager>();

                if (runManager == null)
                {
                    runManager = FindFirstObjectByType<RunManager>(FindObjectsInactive.Exclude);
                }
            }

            if (roomNavigationController == null)
            {
                roomNavigationController = GetComponent<RoomNavigationController>();

                if (roomNavigationController == null)
                {
                    roomNavigationController = FindFirstObjectByType<RoomNavigationController>(FindObjectsInactive.Exclude);
                }
            }

            if (playerInventory == null)
            {
                playerInventory = FindFirstObjectByType<PlayerInventory>(FindObjectsInactive.Exclude);
            }

            if (playerItemManager == null)
            {
                playerItemManager = FindFirstObjectByType<PlayerItemManager>(FindObjectsInactive.Exclude);
            }

            if (playerHealth == null)
            {
                playerHealth = FindFirstObjectByType<PlayerHealth>(FindObjectsInactive.Exclude);
            }

            if (runSaveSystem == null)
            {
                runSaveSystem = GetComponent<RunSaveSystem>();

                if (runSaveSystem == null)
                {
                    runSaveSystem = FindFirstObjectByType<RunSaveSystem>(FindObjectsInactive.Exclude);
                }
            }

            if (runRestoreController == null)
            {
                runRestoreController = GetComponent<RunRestoreController>();

                if (runRestoreController == null)
                {
                    runRestoreController = FindFirstObjectByType<RunRestoreController>(FindObjectsInactive.Exclude);
                }
            }
        }

        private void ResolveCatalog()
        {
            if (debugCatalog == null && !string.IsNullOrWhiteSpace(resourcesCatalogPath))
            {
                debugCatalog = Resources.Load<DevelopmentDebugCatalog>(resourcesCatalogPath);
            }

            if (balanceConfig == null && !string.IsNullOrWhiteSpace(balanceResourcesPath))
            {
                balanceConfig = Resources.Load<BalanceConfig>(balanceResourcesPath);
            }
        }

        private void EnsurePanelView()
        {
            if (panelView != null)
            {
                return;
            }

            panelView = FindFirstObjectByType<DevelopmentDebugPanelView>(FindObjectsInactive.Include);

            if (panelView != null)
            {
                return;
            }

            InputSystemEventSystemBootstrap.EnsureReady();
            Canvas canvas = FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);

            if (canvas == null)
            {
                GameObject canvasObject = new("DevelopmentDebugCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvas = canvasObject.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                scaler.matchWidthOrHeight = 0.5f;
            }

            panelView = DevelopmentDebugPanelView.CreateRuntime(canvas);
        }

        private bool IsDebugPanelAllowed()
        {
            if (Application.isEditor)
            {
                return enableInEditor;
            }

            return enableInDevelopmentBuild && UnityEngine.Debug.isDebugBuild;
        }
    }
}
