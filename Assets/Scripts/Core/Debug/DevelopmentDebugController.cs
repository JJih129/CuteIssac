using System;
using System.Collections.Generic;
using System.Text;
using CuteIssac.Core.Pooling;
using CuteIssac.Core.Run;
using CuteIssac.Core.Scene;
using CuteIssac.Core.Spawning;
using CuteIssac.Data.Balance;
using CuteIssac.Data.Debug;
using CuteIssac.Data.Dungeon;
using CuteIssac.Data.Enemy;
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
        [SerializeField] private Key toggleKey = Key.F12;
        [SerializeField] private Key warpToBossRoomKey = Key.F2;
        [SerializeField] [Min(0.25f)] private float testEnemySpawnDistance = 1.35f;

        [Header("References")]
        [Tooltip("씬에 정적으로 배치된 핵심 참조 허브입니다. 비워두면 Active Context를 먼저 사용하고, 마지막에만 씬 검색으로 보정합니다.")]
        [SerializeField] private GameplaySceneContext sceneContext;
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
        private readonly List<FloorConfig.EnemyWavePresetEntry> _bossWavePresetBuffer = new();
        private readonly List<EnemySpawnEntry> _bossSpawnEntryBuffer = new();
        private readonly List<PrefabPoolService.PoolDebugSnapshot> _poolSnapshots = new();
        private readonly StringBuilder _poolSnapshotBuilder = new(1024);
        private static readonly DevelopmentLogCategory[] DebugLogCategories =
        {
            DevelopmentLogCategory.Room,
            DevelopmentLogCategory.Spawn,
            DevelopmentLogCategory.Reward,
            DevelopmentLogCategory.Save,
            DevelopmentLogCategory.Combat,
            DevelopmentLogCategory.UI
        };
        private static readonly RoomType[] SpecialRoomWarpTypes =
        {
            RoomType.Treasure,
            RoomType.Shop,
            RoomType.Secret,
            RoomType.Curse,
            RoomType.Trap,
            RoomType.Challenge,
            RoomType.MiniBoss,
            RoomType.Boss
        };

        private static readonly Key[] TestEnemySpawnKeys =
        {
            Key.F4,
            Key.F5,
            Key.F6,
            Key.F7,
            Key.F8,
            Key.F9,
            Key.F10,
            Key.F11
        };

        private void Awake()
        {
            if (!IsDebugPanelAllowed())
            {
                enabled = false;
                return;
            }

            ResolveReferences();
            ResolveCatalog();
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

            if (Keyboard.current[warpToBossRoomKey].wasPressedThisFrame)
            {
                WarpToCurrentFloorBossRoom();
            }

            HandleTestEnemySpawnKeys(Keyboard.current);
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
            AddSpecialRoomWarpButtons();
            _buttonModels.Add(new DebugPanelButtonModel("디버그 스냅샷 로그", new Color(0.48f, 0.38f, 0.2f, 1f), LogDebugSnapshot));
            _buttonModels.Add(new DebugPanelButtonModel("Log Pool State", new Color(0.24f, 0.42f, 0.5f, 1f), LogPoolDebugSnapshot));
            AddLogCategoryToggleButtons();
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

        private void AddLogCategoryToggleButtons()
        {
            for (int index = 0; index < DebugLogCategories.Length; index++)
            {
                DevelopmentLogCategory category = DebugLogCategories[index];
                DevelopmentLogCategory capturedCategory = category;
                bool isEnabled = DevelopmentLogService.IsEnabled(category);
                _buttonModels.Add(new DebugPanelButtonModel(
                    $"Log {category}: {(isEnabled ? "ON" : "OFF")}",
                    isEnabled ? new Color(0.24f, 0.5f, 0.34f, 1f) : new Color(0.5f, 0.24f, 0.24f, 1f),
                    () => ToggleLogCategory(capturedCategory)));
            }
        }

        private void ToggleLogCategory(DevelopmentLogCategory category)
        {
            DevelopmentLogService.Toggle(category);
            RefreshVisiblePanel();
        }

        private void AddSpecialRoomWarpButtons()
        {
            if (roomNavigationController == null)
            {
                ResolveReferences();
            }

            if (roomNavigationController == null)
            {
                return;
            }

            for (int i = 0; i < SpecialRoomWarpTypes.Length; i++)
            {
                RoomType roomType = SpecialRoomWarpTypes[i];

                if (!roomNavigationController.HasDebugRoomType(roomType))
                {
                    continue;
                }

                RoomType capturedRoomType = roomType;
                _buttonModels.Add(new DebugPanelButtonModel(
                    $"{ResolveRoomTypeDebugLabel(capturedRoomType)} 이동",
                    ResolveRoomTypeDebugColor(capturedRoomType),
                    () => WarpToRoomType(capturedRoomType)));
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
                DevelopmentLogService.LogWarning(
                    DevelopmentLogCategory.Save,
                    "DevelopmentDebugController could not find a run snapshot to resume.",
                    this);
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

        private void WarpToCurrentFloorBossRoom()
        {
            WarpToRoomType(RoomType.Boss);
        }

        private void WarpToRoomType(RoomType roomType)
        {
            if (roomNavigationController == null)
            {
                ResolveReferences();
            }

            if (roomNavigationController == null || !roomNavigationController.TryDebugWarpToRoomType(roomType))
            {
                DevelopmentLogService.LogWarning(
                    DevelopmentLogCategory.Room,
                    $"DevelopmentDebugController could not warp to {roomType} in the current generated floor.",
                    this);
            }

            RefreshVisiblePanel();
        }

        private void SpawnBossInCurrentRoom()
        {
            RoomController currentRoom = roomNavigationController != null ? roomNavigationController.CurrentRoom : null;
            EnemyController bossPrefab = ResolveCurrentFloorBossPrefab(out int floorIndex, out string bossSourceLabel);

            if (currentRoom == null || bossPrefab == null)
            {
                DevelopmentLogService.LogWarning(
                    DevelopmentLogCategory.Spawn,
                    $"DevelopmentDebugController could not spawn a floor {floorIndex} boss. Current room: {(currentRoom != null ? currentRoom.RoomId : "none")}, source: {bossSourceLabel}.",
                    this);
                RefreshVisiblePanel();
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
            DevelopmentLogService.Log(
                DevelopmentLogCategory.Spawn,
                $"Spawned debug boss for floor {floorIndex}: {spawnedBoss.EnemyId} ({bossSourceLabel}).",
                spawnedBoss);
            RefreshVisiblePanel();
        }

        private EnemyController ResolveCurrentFloorBossPrefab(out int floorIndex, out string sourceLabel)
        {
            floorIndex = ResolveCurrentFloorIndex();

            if (runManager != null && runManager.TryGetFloorConfig(floorIndex, out FloorConfig floorConfig))
            {
                if (TryResolveBossPrefabFromFloorConfig(floorConfig, floorIndex, out EnemyController floorBossPrefab, out sourceLabel))
                {
                    return floorBossPrefab;
                }
            }

            sourceLabel = debugCatalog != null && debugCatalog.BossPrefab != null
                ? "debug catalog fallback"
                : "no floor boss data or fallback debug catalog boss";
            return debugCatalog != null ? debugCatalog.BossPrefab : null;
        }

        private int ResolveCurrentFloorIndex()
        {
            if (runManager != null && runManager.CurrentContext.HasActiveRun)
            {
                return Mathf.Max(1, runManager.CurrentContext.CurrentFloorIndex);
            }

            return 1;
        }

        private bool TryResolveBossPrefabFromFloorConfig(
            FloorConfig floorConfig,
            int floorIndex,
            out EnemyController bossPrefab,
            out string sourceLabel)
        {
            if (TryResolveBossPrefabFromWavePresets(floorConfig, floorIndex, out bossPrefab, out sourceLabel))
            {
                return true;
            }

            if (TryResolveBossPrefabFromEnemyPool(floorConfig, floorIndex, out bossPrefab, out sourceLabel))
            {
                return true;
            }

            bossPrefab = null;
            sourceLabel = $"floor {floorIndex} has no valid boss wave or boss pool entry";
            return false;
        }

        private bool TryResolveBossPrefabFromWavePresets(
            FloorConfig floorConfig,
            int floorIndex,
            out EnemyController bossPrefab,
            out string sourceLabel)
        {
            bossPrefab = null;
            sourceLabel = string.Empty;

            if (floorConfig == null)
            {
                return false;
            }

            _bossWavePresetBuffer.Clear();
            floorConfig.CollectEnemyWavePresets(EnemyEncounterTier.Boss, _bossWavePresetBuffer);

            int totalWeight = 0;

            for (int index = 0; index < _bossWavePresetBuffer.Count; index++)
            {
                FloorConfig.EnemyWavePresetEntry entry = _bossWavePresetBuffer[index];

                if (entry == null || !TryResolveBossPrefabFromWaveData(entry.WaveData, out _))
                {
                    continue;
                }

                totalWeight += Mathf.Max(1, entry.SelectionWeight);
            }

            if (totalWeight <= 0)
            {
                return false;
            }

            int roll = UnityEngine.Random.Range(0, totalWeight);

            for (int index = 0; index < _bossWavePresetBuffer.Count; index++)
            {
                FloorConfig.EnemyWavePresetEntry entry = _bossWavePresetBuffer[index];

                if (entry == null || !TryResolveBossPrefabFromWaveData(entry.WaveData, out EnemyController candidatePrefab))
                {
                    continue;
                }

                roll -= Mathf.Max(1, entry.SelectionWeight);

                if (roll >= 0)
                {
                    continue;
                }

                bossPrefab = candidatePrefab;
                sourceLabel = $"floor {floorIndex} boss wave '{entry.PresetId}'";
                return true;
            }

            return false;
        }

        private bool TryResolveBossPrefabFromEnemyPool(
            FloorConfig floorConfig,
            int floorIndex,
            out EnemyController bossPrefab,
            out string sourceLabel)
        {
            bossPrefab = null;
            sourceLabel = string.Empty;

            if (floorConfig == null)
            {
                return false;
            }

            _bossSpawnEntryBuffer.Clear();
            EnemyPoolData enemyPool = floorConfig.StageProfile != null ? floorConfig.StageProfile.EnemyPool : floorConfig.EnemyPool;

            if (enemyPool != null)
            {
                enemyPool.CollectEntries(EnemyEncounterTier.Boss, floorIndex, _bossSpawnEntryBuffer);
            }
            else
            {
                floorConfig.CollectEnemySpawnEntries(EnemyEncounterTier.Boss, _bossSpawnEntryBuffer);
            }

            int totalWeight = 0;

            for (int index = 0; index < _bossSpawnEntryBuffer.Count; index++)
            {
                EnemySpawnEntry entry = _bossSpawnEntryBuffer[index];

                if (entry == null || entry.EnemyPrefab == null || !IsBossPrefabCandidate(entry.EnemyPrefab, entry.EnemyId))
                {
                    continue;
                }

                totalWeight += Mathf.Max(1, entry.SelectionWeight);
            }

            if (totalWeight <= 0)
            {
                return false;
            }

            int roll = UnityEngine.Random.Range(0, totalWeight);

            for (int index = 0; index < _bossSpawnEntryBuffer.Count; index++)
            {
                EnemySpawnEntry entry = _bossSpawnEntryBuffer[index];

                if (entry == null || entry.EnemyPrefab == null || !IsBossPrefabCandidate(entry.EnemyPrefab, entry.EnemyId))
                {
                    continue;
                }

                roll -= Mathf.Max(1, entry.SelectionWeight);

                if (roll >= 0)
                {
                    continue;
                }

                bossPrefab = entry.EnemyPrefab;
                sourceLabel = $"floor {floorIndex} boss enemy pool entry '{entry.EnemyId}'";
                return true;
            }

            return false;
        }

        private static bool TryResolveBossPrefabFromWaveData(EnemyWaveData waveData, out EnemyController bossPrefab)
        {
            bossPrefab = null;

            if (waveData == null)
            {
                return false;
            }

            IReadOnlyList<EnemyWaveEntry> entries = waveData.Entries;

            for (int index = 0; index < entries.Count; index++)
            {
                EnemyWaveEntry entry = entries[index];

                if (entry != null && IsBossPrefabCandidate(entry.EnemyPrefab, entry.EnemyId))
                {
                    bossPrefab = entry.EnemyPrefab;
                    return true;
                }
            }

            return false;
        }

        private static bool IsBossPrefabCandidate(EnemyController enemyPrefab, string authoredEnemyId)
        {
            if (enemyPrefab == null)
            {
                return false;
            }

            if (enemyPrefab.GetComponent<BossEnemyController>() != null)
            {
                return true;
            }

            return ContainsBossToken(authoredEnemyId) || ContainsBossToken(enemyPrefab.EnemyId);
        }

        private static bool ContainsBossToken(string enemyId)
        {
            return !string.IsNullOrWhiteSpace(enemyId)
                && enemyId.IndexOf("boss", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void HandleTestEnemySpawnKeys(Keyboard keyboard)
        {
            if (debugCatalog == null || keyboard == null)
            {
                return;
            }

            IReadOnlyList<EnemyController> enemyPrefabs = debugCatalog.TestSpawnEnemyPrefabs;
            int count = Mathf.Min(enemyPrefabs.Count, TestEnemySpawnKeys.Length);

            for (int index = 0; index < count; index++)
            {
                if (!keyboard[TestEnemySpawnKeys[index]].wasPressedThisFrame)
                {
                    continue;
                }

                SpawnTestEnemy(enemyPrefabs[index], index);
                return;
            }
        }

        private void SpawnTestEnemy(EnemyController enemyPrefab, int spawnIndex)
        {
            if (enemyPrefab == null)
            {
                return;
            }

            PlayerController playerController = PlayerRegistry.ActiveController;

            if (playerController == null && sceneContext != null)
            {
                playerController = sceneContext.PlayerController;
            }

            if (playerController == null && GameplaySceneContext.Active != null)
            {
                playerController = GameplaySceneContext.Active.PlayerController;
            }

            if (playerController == null)
            {
                playerController = FindFirstObjectByType<PlayerController>(FindObjectsInactive.Exclude);
            }

            if (playerController == null)
            {
                DevelopmentLogService.LogWarning(
                    DevelopmentLogCategory.Spawn,
                    "DevelopmentDebugController could not spawn test enemy because no active player was found.",
                    this);
                return;
            }

            RoomController currentRoom = roomNavigationController != null ? roomNavigationController.CurrentRoom : null;
            Vector2 spawnDirection = ResolvePlayerForwardDirection(playerController);
            Vector3 spawnPosition = playerController.transform.position + (Vector3)(spawnDirection * testEnemySpawnDistance);
            Transform spawnParent = currentRoom != null && !currentRoom.HasResolvedRoom ? currentRoom.transform : null;
            EnemyController spawnedEnemy = GameplaySpawnFactory.SpawnComponent(
                enemyPrefab,
                spawnPosition,
                Quaternion.identity,
                spawnParent,
                SpawnReusePolicy.Instantiate);

            if (spawnedEnemy == null)
            {
                return;
            }

            if (currentRoom != null && !currentRoom.HasResolvedRoom)
            {
                RoomEnemyMember enemyMember = spawnedEnemy.GetComponent<RoomEnemyMember>();

                if (enemyMember == null)
                {
                    enemyMember = spawnedEnemy.gameObject.AddComponent<RoomEnemyMember>();
                }

                enemyMember.AssignRoom(currentRoom);
                currentRoom.DebugBeginInjectedCombatState();
            }

            DevelopmentLogService.Log(
                DevelopmentLogCategory.Spawn,
                $"Spawned test enemy F{spawnIndex + 4}: {spawnedEnemy.EnemyId}",
                spawnedEnemy);
            RefreshVisiblePanel();
        }

        private static Vector2 ResolvePlayerForwardDirection(PlayerController playerController)
        {
            PlayerMovement playerMovement = playerController.GetComponent<PlayerMovement>();

            if (playerMovement != null)
            {
                Vector2 moveInput = playerMovement.MoveInput;
                if (moveInput.sqrMagnitude > 0.0001f)
                {
                    return moveInput.normalized;
                }

                Vector2 velocity = playerMovement.CurrentVelocity;
                if (velocity.sqrMagnitude > 0.0001f)
                {
                    return velocity.normalized;
                }
            }

            return Vector2.up;
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
            return $"{floor}층 · 방 {roomLabel} · F12 토글";
        }

        private string BuildBalanceSnapshot()
        {
            string balanceSnapshot = BalanceDebugSnapshotBuilder.BuildSnapshot(balanceConfig, runManager, roomNavigationController);
            string spawnSnapshot = GameplaySpawnTelemetry.BuildSummary();
            string logSnapshot = DevelopmentLogService.BuildCategorySummary();
            string runSaveSnapshot = BuildRunSaveSnapshotSummary();
            string poolSnapshot = BuildPoolDebugSnapshot();
            return $"{balanceSnapshot}\n\n{spawnSnapshot}\n\n{poolSnapshot}\n\n{logSnapshot}\n\n{runSaveSnapshot}";
        }

        private string BuildPoolDebugSnapshot()
        {
            PrefabPoolService.CopyDebugSnapshots(_poolSnapshots);
            _poolSnapshotBuilder.Clear();
            _poolSnapshotBuilder.AppendLine("POOL STATE");

            if (_poolSnapshots.Count == 0)
            {
                _poolSnapshotBuilder.Append("- no pools");
                return _poolSnapshotBuilder.ToString();
            }

            int warningCount = 0;
            for (int index = 0; index < _poolSnapshots.Count; index++)
            {
                PrefabPoolService.PoolDebugSnapshot snapshot = _poolSnapshots[index];
                if (snapshot.HasWarning)
                {
                    warningCount++;
                }
            }

            _poolSnapshotBuilder
                .Append("- pools: ").Append(_poolSnapshots.Count)
                .Append(" warnings: ").AppendLine(warningCount.ToString());

            for (int index = 0; index < _poolSnapshots.Count; index++)
            {
                PrefabPoolService.PoolDebugSnapshot snapshot = _poolSnapshots[index];
                _poolSnapshotBuilder
                    .Append("- ").Append(snapshot.PrefabName)
                    .Append("  Active:").Append(snapshot.ActiveCount)
                    .Append("  Available:").Append(snapshot.AvailableCount)
                    .Append("  Total:").Append(snapshot.TotalCreated)
                    .Append("  Root:").Append(FormatVector(snapshot.RootPosition));

                if (snapshot.RootNearGameplayArea)
                {
                    _poolSnapshotBuilder.Append("  WARN root near map");
                }

                if (snapshot.ActiveAvailableCount > 0)
                {
                    _poolSnapshotBuilder.Append("  WARN active pooled:").Append(snapshot.ActiveAvailableCount);
                }

                if (index < _poolSnapshots.Count - 1)
                {
                    _poolSnapshotBuilder.AppendLine();
                }
            }

            return _poolSnapshotBuilder.ToString();
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

        private void LogDebugSnapshot()
        {
            DevelopmentLogService.Log(DevelopmentLogCategory.UI, BuildBalanceSnapshot(), this);
            RefreshVisiblePanel();
        }

        private void LogPoolDebugSnapshot()
        {
            DevelopmentLogService.Log(DevelopmentLogCategory.UI, BuildPoolDebugSnapshot(), this);
            RefreshVisiblePanel();
        }

        private static string FormatVector(Vector3 position)
        {
            return $"{position.x:0.#},{position.y:0.#},{position.z:0.#}";
        }

        private static string ResolveRoomTypeDebugLabel(RoomType roomType)
        {
            return roomType switch
            {
                RoomType.Treasure => "보물방",
                RoomType.Shop => "상점",
                RoomType.Secret => "비밀방",
                RoomType.Curse => "저주방",
                RoomType.Trap => "함정방",
                RoomType.Challenge => "도전방",
                RoomType.MiniBoss => "미니보스방",
                RoomType.Boss => "보스방",
                _ => roomType.ToString()
            };
        }

        private static Color ResolveRoomTypeDebugColor(RoomType roomType)
        {
            return roomType switch
            {
                RoomType.Treasure => new Color(0.84f, 0.62f, 0.22f, 1f),
                RoomType.Shop => new Color(0.28f, 0.62f, 0.56f, 1f),
                RoomType.Secret => new Color(0.48f, 0.42f, 0.68f, 1f),
                RoomType.Curse => new Color(0.56f, 0.28f, 0.44f, 1f),
                RoomType.Trap => new Color(0.62f, 0.38f, 0.2f, 1f),
                RoomType.Challenge => new Color(0.7f, 0.48f, 0.22f, 1f),
                RoomType.MiniBoss => new Color(0.62f, 0.26f, 0.32f, 1f),
                RoomType.Boss => new Color(0.74f, 0.24f, 0.24f, 1f),
                _ => new Color(0.28f, 0.34f, 0.44f, 1f)
            };
        }

        private void ResolveReferences()
        {
            ResolveReferencesFromSceneContext();

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

        private void ResolveReferencesFromSceneContext()
        {
            if (sceneContext == null)
            {
                sceneContext = GameplaySceneContext.Active;
            }

            if (sceneContext == null)
            {
                return;
            }

            sceneContext.ResolveMissingReferences();
            runManager ??= sceneContext.RunManager;
            roomNavigationController ??= sceneContext.RoomNavigationController;
            playerInventory ??= sceneContext.PlayerInventory;
            playerItemManager ??= sceneContext.PlayerItemManager;
            playerHealth ??= sceneContext.PlayerHealth;
            runSaveSystem ??= sceneContext.RunSaveSystem;
            runRestoreController ??= sceneContext.RunRestoreController;
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
            Canvas canvas = sceneContext != null ? sceneContext.OverlayCanvas : null;

            if (canvas == null)
            {
                canvas = FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
            }

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
