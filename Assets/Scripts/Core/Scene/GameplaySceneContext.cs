using CuteIssac.Core.Meta;
using CuteIssac.Core.Run;
using CuteIssac.Core.Save;
using CuteIssac.Core.Feedback;
using CuteIssac.Core.Input;
using CuteIssac.Core.Settings;
using CuteIssac.Dungeon;
using CuteIssac.Player;
using CuteIssac.Room;
using CuteIssac.UI;
using System.Text;
using UnityEngine;

namespace CuteIssac.Core.Scene
{
    /// <summary>
    /// Scene-level reference hub for core gameplay systems.
    /// 씬에 정적으로 배치해서 HUD, Run, Room, Player 계열 참조가 어디서 연결되는지 한곳에서 확인한다.
    /// </summary>
    [DefaultExecutionOrder(-500)]
    [DisallowMultipleComponent]
    public sealed class GameplaySceneContext : MonoBehaviour
    {
        [Header("Run Systems")]
        [SerializeField] private RunManager runManager;
        [SerializeField] private CharacterProfileManager characterProfileManager;
        [SerializeField] private RunSaveSystem runSaveSystem;
        [SerializeField] private RunRestoreController runRestoreController;
        [SerializeField] private RunItemPoolService runItemPoolService;
        [SerializeField] private GameSaveSystem gameSaveSystem;
        [SerializeField] private MetaProgressionManager metaProgressionManager;

        [Header("Dungeon Systems")]
        [SerializeField] private DungeonInstantiator dungeonInstantiator;
        [SerializeField] private RoomNavigationController roomNavigationController;
        [SerializeField] private RoomTraversalGuidanceController roomTraversalGuidanceController;
        [SerializeField] private MinimapController minimapController;

        [Header("Player Systems")]
        [SerializeField] private PlayerController playerController;
        [SerializeField] private PlayerHealth playerHealth;
        [SerializeField] private PlayerInventory playerInventory;
        [SerializeField] private PlayerItemManager playerItemManager;
        [SerializeField] private PlayerStats playerStats;
        [SerializeField] private PlayerWeaponLoadout playerWeaponLoadout;
        [SerializeField] private PlayerActiveItemController playerActiveItemController;
        [SerializeField] private PlayerConsumableHolder playerConsumableHolder;
        [SerializeField] private PlayerTrinketHolder playerTrinketHolder;
        [SerializeField] private PlayerSpeedBuffState playerSpeedBuffState;
        [SerializeField] private PlayerCombatMomentumController playerCombatMomentumController;
        [SerializeField] private PlayerRoutePlanCarryController playerRoutePlanCarryController;
        [Tooltip("탭 미니맵 토글 등 플레이 중 입력을 읽는 입력 어댑터입니다. UI/룸 시스템은 이 참조를 우선 사용합니다.")]
        [SerializeField] private InputSystemPlayerInputReader playerInputReader;

        [Header("Presentation")]
        [SerializeField] private HUDController hudController;
        [SerializeField] private Camera mainCamera;
        [Tooltip("피격 흔들림 등 화면 피드백을 담당하는 선택 참조입니다. 피드백 프리팹/씬 배치 변경 시 여기서 확인합니다.")]
        [SerializeField] private PlayerScreenFeedback playerScreenFeedback;
        [Tooltip("배너, 결과창, 피드백 UI가 붙는 기본 오버레이 Canvas입니다. 비워두면 초기화 때 씬에서 한 번 보정합니다.")]
        [SerializeField] private Canvas overlayCanvas;
        [Tooltip("미니맵 테마 색상 등 피드백 표현에서 재사용하는 선택 UI 참조입니다.")]
        [SerializeField] private MinimapPanelView minimapPanelView;
        [Tooltip("UI 스케일, 접근성, 피드백 옵션을 읽는 선택 서비스입니다.")]
        [SerializeField] private GameOptionsService gameOptionsService;

        [Header("Validation")]
        [Tooltip("Awake에서 핵심 씬 참조 누락을 한 번에 경고합니다. 협업 중 Inspector 연결 누락을 빨리 찾기 위한 옵션입니다.")]
        [SerializeField] private bool logMissingReferencesOnAwake = true;

        private readonly StringBuilder _missingReferenceBuilder = new();

        public static GameplaySceneContext Active { get; private set; }

        public RunManager RunManager => runManager;
        public CharacterProfileManager CharacterProfileManager => characterProfileManager;
        public RunSaveSystem RunSaveSystem => runSaveSystem;
        public RunRestoreController RunRestoreController => runRestoreController;
        public RunItemPoolService RunItemPoolService => runItemPoolService;
        public GameSaveSystem GameSaveSystem => gameSaveSystem;
        public MetaProgressionManager MetaProgressionManager => metaProgressionManager;
        public DungeonInstantiator DungeonInstantiator => dungeonInstantiator;
        public RoomNavigationController RoomNavigationController => roomNavigationController;
        public RoomTraversalGuidanceController RoomTraversalGuidanceController => roomTraversalGuidanceController;
        public MinimapController MinimapController => minimapController;
        public PlayerController PlayerController => playerController;
        public PlayerHealth PlayerHealth => playerHealth;
        public PlayerInventory PlayerInventory => playerInventory;
        public PlayerItemManager PlayerItemManager => playerItemManager;
        public PlayerStats PlayerStats => playerStats;
        public PlayerWeaponLoadout PlayerWeaponLoadout => playerWeaponLoadout;
        public PlayerActiveItemController PlayerActiveItemController => playerActiveItemController;
        public PlayerConsumableHolder PlayerConsumableHolder => playerConsumableHolder;
        public PlayerTrinketHolder PlayerTrinketHolder => playerTrinketHolder;
        public PlayerSpeedBuffState PlayerSpeedBuffState => playerSpeedBuffState;
        public PlayerCombatMomentumController PlayerCombatMomentumController => playerCombatMomentumController;
        public PlayerRoutePlanCarryController PlayerRoutePlanCarryController => playerRoutePlanCarryController;
        public InputSystemPlayerInputReader PlayerInputReader => playerInputReader;
        public HUDController HudController => hudController;
        public Camera MainCamera => mainCamera;
        public PlayerScreenFeedback PlayerScreenFeedback => playerScreenFeedback;
        public Canvas OverlayCanvas => overlayCanvas;
        public MinimapPanelView MinimapPanelView => minimapPanelView;
        public GameOptionsService GameOptionsService => gameOptionsService;

        private void Awake()
        {
            RegisterActiveContext();
            ResolveMissingReferences();

            if (logMissingReferencesOnAwake)
            {
                ValidateReferences(logWarnings: true);
            }
        }

        private void OnEnable()
        {
            RegisterActiveContext();
        }

        private void OnDestroy()
        {
            if (Active == this)
            {
                Active = null;
            }
        }

        [ContextMenu("Auto Assign Scene References")]
        public void AutoAssignSceneReferences()
        {
            ResolveMissingReferences();
        }

        public void ResolveMissingReferences()
        {
            runManager = Resolve(runManager);
            characterProfileManager = Resolve(characterProfileManager);
            runSaveSystem = Resolve(runSaveSystem);
            runRestoreController = Resolve(runRestoreController);
            runItemPoolService = Resolve(runItemPoolService);
            gameSaveSystem = Resolve(gameSaveSystem);
            metaProgressionManager = Resolve(metaProgressionManager);
            dungeonInstantiator = Resolve(dungeonInstantiator);
            roomNavigationController = Resolve(roomNavigationController);
            roomTraversalGuidanceController = Resolve(roomTraversalGuidanceController);
            minimapController = Resolve(minimapController);
            hudController = Resolve(hudController);
            mainCamera = mainCamera != null ? mainCamera : Camera.main;
            playerScreenFeedback = Resolve(playerScreenFeedback);
            overlayCanvas = Resolve(overlayCanvas);
            minimapPanelView = Resolve(minimapPanelView);
            gameOptionsService = Resolve(gameOptionsService);
            playerInputReader = Resolve(playerInputReader);

            playerController = Resolve(playerController);
            ResolvePlayerScopedReferences();
        }

        [ContextMenu("Validate Scene References")]
        public bool ValidateReferences()
        {
            return ValidateReferences(logWarnings: true);
        }

        public bool ValidateReferences(bool logWarnings)
        {
            _missingReferenceBuilder.Clear();
            // 선택 기능 컴포넌트는 프리팹/런 설정에 따라 없을 수 있으므로, SampleScene의 필수 축만 실패 처리한다.
            AppendMissing(runManager, nameof(runManager));
            AppendMissing(runSaveSystem, nameof(runSaveSystem));
            AppendMissing(runItemPoolService, nameof(runItemPoolService));
            AppendMissing(dungeonInstantiator, nameof(dungeonInstantiator));
            AppendMissing(roomNavigationController, nameof(roomNavigationController));
            AppendMissing(minimapController, nameof(minimapController));
            AppendMissing(playerController, nameof(playerController));
            AppendMissing(playerHealth, nameof(playerHealth));
            AppendMissing(playerInventory, nameof(playerInventory));
            AppendMissing(playerItemManager, nameof(playerItemManager));
            AppendMissing(playerStats, nameof(playerStats));
            AppendMissing(hudController, nameof(hudController));
            AppendMissing(mainCamera, nameof(mainCamera));

            bool isValid = _missingReferenceBuilder.Length == 0;
            if (!isValid && logWarnings)
            {
                UnityEngine.Debug.LogWarning(
                    $"GameplaySceneContext has missing scene references:\n{_missingReferenceBuilder}",
                    this);
            }

            return isValid;
        }

        private void RegisterActiveContext()
        {
            if (Active != null && Active != this)
            {
                UnityEngine.Debug.LogWarning("Multiple GameplaySceneContext instances are active. The latest enabled context is now active.", this);
            }

            Active = this;
        }

        private void ResolvePlayerScopedReferences()
        {
            Transform playerRoot = playerController != null ? playerController.transform : null;
            playerHealth = ResolvePlayerComponent(playerHealth, playerRoot);
            playerInventory = ResolvePlayerComponent(playerInventory, playerRoot);
            playerItemManager = ResolvePlayerComponent(playerItemManager, playerRoot);
            playerStats = ResolvePlayerComponent(playerStats, playerRoot);
            playerWeaponLoadout = ResolvePlayerComponent(playerWeaponLoadout, playerRoot);
            playerActiveItemController = ResolvePlayerComponent(playerActiveItemController, playerRoot);
            playerConsumableHolder = ResolvePlayerComponent(playerConsumableHolder, playerRoot);
            playerTrinketHolder = ResolvePlayerComponent(playerTrinketHolder, playerRoot);
            playerSpeedBuffState = ResolvePlayerComponent(playerSpeedBuffState, playerRoot);
            playerCombatMomentumController = ResolvePlayerComponent(playerCombatMomentumController, playerRoot);
            playerRoutePlanCarryController = ResolvePlayerComponent(playerRoutePlanCarryController, playerRoot);
        }

        private static T ResolvePlayerComponent<T>(T current, Transform playerRoot) where T : Component
        {
            if (current != null)
            {
                return current;
            }

            T fromPlayerRoot = playerRoot != null ? playerRoot.GetComponent<T>() : null;
            return fromPlayerRoot != null
                ? fromPlayerRoot
                : Resolve(current);
        }

        private static T Resolve<T>(T current) where T : Object
        {
            return current != null
                ? current
                : FindFirstObjectByType<T>(FindObjectsInactive.Exclude);
        }

        private void AppendMissing(Object reference, string fieldName)
        {
            if (reference != null)
            {
                return;
            }

            _missingReferenceBuilder.Append("- ").Append(fieldName).AppendLine();
        }
    }
}
