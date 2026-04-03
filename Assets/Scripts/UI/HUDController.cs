using System;
using System.Collections.Generic;
using CuteIssac.Core.Feedback;
using CuteIssac.Core.Gameplay;
using CuteIssac.Core.Run;
using CuteIssac.Data.Item;
using CuteIssac.Dungeon;
using CuteIssac.Data.Dungeon;
using CuteIssac.Player;
using CuteIssac.Room;
using UnityEngine;
using UnityEngine.UI;

namespace CuteIssac.UI
{
    /// <summary>
    /// Connects player-facing data sources to HUD panel views.
    /// This class should stay thin: it subscribes to health and inventory changes, then forwards presentation data to replaceable views.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HUDController : MonoBehaviour
    {
        private const string RunTraceTitleLabel = "RUN TRACE";
        private const string RunTraceEmptyHeadline = "NO RECENT SWING";
        private const string RunTraceEmptyDetail = "Recent build, supply, and shop changes will stack here.";

        [Header("Data Sources")]
        [Tooltip("Optional. Assign the player's health component here. If empty, the controller tries to find one in the scene.")]
        [SerializeField] private PlayerHealth playerHealth;
        [Tooltip("Optional. Assign the player's inventory component here. If empty, the controller tries to find one in the scene.")]
        [SerializeField] private PlayerInventory playerInventory;
        [Tooltip("Optional. Assign the player's consumable holder here. If empty, the controller tries to find one in the scene.")]
        [SerializeField] private PlayerConsumableHolder playerConsumableHolder;
        [Tooltip("Optional. Assign the player's active item controller here. If empty, the controller tries to find one in the scene.")]
        [SerializeField] private PlayerActiveItemController playerActiveItemController;
        [Tooltip("Optional. Assign the player's trinket holder here. If empty, the controller tries to find one in the scene.")]
        [SerializeField] private PlayerTrinketHolder playerTrinketHolder;
        [Tooltip("Optional. Assign the player's stat resolver here. If empty, the controller tries to find one in the scene.")]
        [SerializeField] private PlayerStats playerStats;
        [Tooltip("Optional. Assign the player's weapon loadout here. If empty, the controller tries to find one in the scene.")]
        [SerializeField] private PlayerWeaponLoadout playerWeaponLoadout;
        [Tooltip("Optional. Assign the player's combat momentum controller here. If empty, the controller tries to find one in the scene.")]
        [SerializeField] private PlayerCombatMomentumController playerCombatMomentumController;
        [Tooltip("Optional. Assign the player's route plan carry controller here. If empty, the controller tries to find one in the scene.")]
        [SerializeField] private PlayerRoutePlanCarryController playerRoutePlanCarryController;
        [Tooltip("Optional. Assign the room navigation controller here. If empty, the controller tries to find one in the scene.")]
        [SerializeField] private RoomNavigationController roomNavigationController;
        [Tooltip("Optional. Assign the traversal guidance controller here. If empty, the controller tries to find one from room navigation.")]
        [SerializeField] private RoomTraversalGuidanceController roomTraversalGuidanceController;

        [Header("Panel Views")]
        [Tooltip("Replaceable health panel view. This owns heart slot visuals and value text.")]
        [SerializeField] private HealthPanelView healthPanelView;
        [Tooltip("Replaceable resource panel view for coins, keys, and bombs.")]
        [SerializeField] private ResourcePanelView resourcePanelView;
        [Tooltip("Replaceable active item slot view. Current prototype shows a reserved placeholder.")]
        [SerializeField] private ActiveItemPanelView activeItemPanelView;
        [Tooltip("Replaceable combat stat panel view. If empty, HUDController creates a fallback compact stat panel at runtime.")]
        [SerializeField] private CombatStatPanelView combatStatPanelView;
        [Tooltip("Replaceable trinket slot view. If empty, HUDController creates a fallback panel at runtime.")]
        [SerializeField] private TrinketPanelView trinketPanelView;
        [Tooltip("Replaceable weapon loadout view. If empty, HUDController creates a fallback panel at runtime.")]
        [SerializeField] private WeaponPanelView weaponPanelView;
        [Tooltip("Replaceable recent run trace history strip. If empty, HUDController creates a fallback panel at runtime.")]
        [SerializeField] private LoadoutHistoryPanelView loadoutHistoryPanelView;
        [Tooltip("Replaceable boss HP panel view. Hidden until a boss is explicitly shown.")]
        [SerializeField] private BossHpPanelView bossHpPanelView;
        [Tooltip("Replaceable minimap panel view. Current prototype only reserves the space.")]
        [SerializeField] private MinimapPanelView minimapPanelView;
        [Tooltip("Replaceable top HUD bar layout view. If empty, HUDController creates a fallback layout controller at runtime.")]
        [SerializeField] private TopHudBarView topHudBarView;
        [Tooltip("Replaceable pause menu controller. If empty, HUDController creates one at runtime.")]
        [SerializeField] private PauseMenuController pauseMenuController;
        [Tooltip("Optional pause menu view. If empty, PauseMenuController creates a fallback parchment menu at runtime.")]
        [SerializeField] private PauseMenuView pauseMenuView;

        [Header("Run Sources")]
        [Tooltip("Optional. Assign the run manager here. If empty, the controller tries to find one in the scene.")]
        [SerializeField] private RunManager runManager;
        [SerializeField] [Min(0.05f)] private float challengeStatusRefreshInterval = 0.15f;
        [SerializeField] [Min(0.25f)] private float secretRevealStatusDuration = 3.5f;
        [SerializeField] [Min(0.25f)] private float momentumRewardTransitionStatusDuration = 2.4f;
        [SerializeField] [Min(0.25f)] private float momentumRewardClaimStatusDuration = 1.8f;
        [SerializeField] [Min(0.25f)] private float choiceRouteStatusDuration = 2.2f;
        [SerializeField] [Min(0.25f)] private float loadoutRecapStatusDuration = 3.2f;
        [SerializeField] [Min(1)] private int maxLoadoutHistoryEntries = 3;
        [SerializeField] [Min(2f)] private float loadoutHistoryEntryLifetime = 18f;

        private bool _warnedMissingHealth;
        private bool _warnedMissingInventory;
        private bool _warnedMissingConsumableHolder;
        private bool _warnedMissingActiveItemController;
        private bool _warnedMissingTrinketHolder;
        private bool _warnedMissingPlayerStats;
        private bool _warnedMissingRoomNavigationController;
        private bool _warnedMissingHealthPanelView;
        private bool _warnedMissingResourcePanelView;
        private bool _warnedMissingMinimapPanelView;
        private bool _warnedMissingTopHudBarView;
        private bool _loggedAutoReboundHudViews;
        private RoomController _observedRoom;
        private float _nextChallengeStatusRefreshTime;
        private int _challengeFeedbackRoomInstanceId = -1;
        private ChallengeClearRank _lastChallengePaceRank = ChallengeClearRank.None;
        private RoomController _secretRevealSourceRoom;
        private RoomDirection _secretRevealDirection;
        private float _secretRevealStatusExpiresAt;
        private RoomController _momentumRewardPresentationRoom;
        private RoomType _momentumRewardPresentationRoomType = RoomType.Normal;
        private float _momentumRewardPresentationExpiresAt;
        private string _momentumRewardPresentationHeadline = string.Empty;
        private string _momentumRewardPresentationDetail = string.Empty;
        private string _momentumRewardPresentationBadgeLabel = "FLOW";
        private string _momentumRewardPresentationEyebrow = string.Empty;
        private string _momentumRewardPresentationCompactTag = string.Empty;
        private string _momentumRewardPresentationDetailEyebrow = string.Empty;
        private Color _momentumRewardPresentationAccent = new(1f, 0.84f, 0.36f, 1f);
        private RoomController _choiceRoutePresentationRoom;
        private RoomType _choiceRoutePresentationRoomType = RoomType.Normal;
        private float _choiceRoutePresentationExpiresAt;
        private string _choiceRoutePresentationHeadline = string.Empty;
        private string _choiceRoutePresentationDetail = string.Empty;
        private string _choiceRoutePresentationBadgeLabel = "ROUTE";
        private string _choiceRoutePresentationEyebrow = string.Empty;
        private string _choiceRoutePresentationCompactTag = string.Empty;
        private string _choiceRoutePresentationDetailEyebrow = string.Empty;
        private Color _choiceRoutePresentationAccent = new(0.72f, 0.9f, 1f, 1f);
        private RoomController _loadoutRecapPresentationRoom;
        private RoomType _loadoutRecapPresentationRoomType = RoomType.Normal;
        private float _loadoutRecapPresentationExpiresAt;
        private string _loadoutRecapPresentationHeadline = string.Empty;
        private string _loadoutRecapPresentationDetail = string.Empty;
        private string _loadoutRecapPresentationBadgeLabel = "TRACE";
        private string _loadoutRecapPresentationEyebrow = string.Empty;
        private string _loadoutRecapPresentationCompactTag = string.Empty;
        private string _loadoutRecapPresentationDetailEyebrow = string.Empty;
        private Color _loadoutRecapPresentationAccent = new(0.7f, 0.88f, 1f, 1f);
        private readonly Dictionary<int, RoomDirection> _revealedSecretDirectionsByRoom = new();
        private readonly HashSet<int> _roomsWithUncollectedRewards = new();
        private readonly Dictionary<int, RoomRewardPhaseSummary> _rewardPhaseSummariesByRoom = new();
        private readonly Dictionary<int, ItemRarity> _manifestedCurseRewardRaritiesByRoom = new();
        private readonly Dictionary<int, RoomLoadoutRecapSummary> _loadoutRecapSummariesByRoom = new();
        private readonly List<LoadoutHistoryRuntimeEntry> _loadoutHistoryEntries = new();
        private readonly List<LoadoutHistoryPanelView.EntryPresentation> _loadoutHistoryPresentationBuffer = new();

        private readonly struct LoadoutHistoryRuntimeEntry
        {
            public LoadoutHistoryRuntimeEntry(string headline, string detail, Color accentColor, bool emphasize, float timestamp)
            {
                Headline = headline ?? string.Empty;
                Detail = detail ?? string.Empty;
                AccentColor = accentColor.a > 0.01f
                    ? accentColor
                    : Color.white;
                Emphasize = emphasize;
                Timestamp = timestamp;
            }

            public string Headline { get; }
            public string Detail { get; }
            public Color AccentColor { get; }
            public bool Emphasize { get; }
            public float Timestamp { get; }
        }

        private struct RoomLoadoutRecapSummary
        {
            public RoomController Room;
            public RoomType RoomType;
            public int ChangeCount;
            public int LoadoutChangeCount;
            public int OutcomeChangeCount;
            public int EmphasizedChangeCount;
            public string LatestHeadline;
            public string LatestDetail;
            public string PreviousHeadline;
            public Color AccentColor;
            public bool WasPresented;
        }

        private void Awake()
        {
            ResolveReferences();
            RefreshAll();
        }

        private void OnEnable()
        {
            ResolveReferences();
            Subscribe();
            RefreshAll();
        }

        private void Start()
        {
            // Scene object Awake order is not guaranteed across roots.
            // Refresh once more after all Awake calls so initial HP/resources are correct even in test scenes.
            ResolveReferences();
            if (isActiveAndEnabled)
            {
                Subscribe();
            }
            RefreshAll();
        }

        private void Update()
        {
            float now = Time.unscaledTime;

            if (TryExpireSecretRevealStatus(now))
            {
                RefreshRoomStatus();
            }

            if (TryExpireMomentumRewardPresentationStatus(now))
            {
                RefreshRoomStatus();
            }

            if (TryExpireChoiceRoutePresentationStatus(now))
            {
                RefreshRoomStatus();
            }

            if (TryExpireLoadoutRecapPresentationStatus(now))
            {
                RefreshRoomStatus();
            }

            TryTrimExpiredLoadoutHistoryEntries(now);
            RefreshLoadoutHistory(now);

            if (!ShouldRefreshChallengeStatusRealtime() || now < _nextChallengeStatusRefreshTime)
            {
                return;
            }

            _nextChallengeStatusRefreshTime = now + Mathf.Max(0.05f, challengeStatusRefreshInterval);
            RaiseChallengePaceFeedbackIfNeeded();
            RefreshRoomStatus();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        /// <summary>
        /// Optional helper for tooling or tests that want to inject a prepared HUD view hierarchy at runtime.
        /// The normal game path should use inspector-assigned panel references on a scene or prefab HUD root.
        /// </summary>
        public void ConfigureViews(
            HealthPanelView healthView,
            ResourcePanelView resourceView,
            ActiveItemPanelView activeView,
            BossHpPanelView bossView,
            MinimapPanelView minimapView,
            TrinketPanelView trinketView = null)
        {
            healthPanelView = healthView;
            resourcePanelView = resourceView;
            activeItemPanelView = activeView;
            bossHpPanelView = bossView;
            minimapPanelView = minimapView;
            trinketPanelView = trinketView;
            RefreshAll();
        }

        public void RefreshViewState()
        {
            RefreshAll();
        }

        public void ShowBoss(string bossName, float normalizedHealth, string subtitle = "")
        {
            if (bossHpPanelView != null)
            {
                bossHpPanelView.ShowBoss(bossName, normalizedHealth, subtitle);
            }
        }

        public void ShowBoss(
            string bossName,
            float normalizedHealth,
            string subtitle,
            Color backgroundAccent,
            Color fillAccent,
            Color nameAccent,
            Color subtitleAccent)
        {
            ShowBoss(
                bossName,
                normalizedHealth,
                subtitle,
                backgroundAccent,
                fillAccent,
                nameAccent,
                subtitleAccent,
                null,
                null);
        }

        public void ShowBoss(
            string bossName,
            float normalizedHealth,
            string subtitle,
            Color backgroundAccent,
            Color fillAccent,
            Color nameAccent,
            Color subtitleAccent,
            string badgeText,
            string statusText)
        {
            if (bossHpPanelView != null)
            {
                bossHpPanelView.ShowBoss(
                    bossName,
                    normalizedHealth,
                    subtitle,
                    backgroundAccent,
                    fillAccent,
                    nameAccent,
                    subtitleAccent,
                    badgeText,
                    statusText);
            }
        }

        public void HideBoss()
        {
            if (bossHpPanelView != null)
            {
                bossHpPanelView.HideBoss();
            }
        }

        public void SetMinimapVisible(bool visible)
        {
            if (minimapPanelView != null)
            {
                minimapPanelView.SetVisible(visible);
            }
        }

        private void ResolveReferences()
        {
            PlayerWeaponLoadout previousWeaponLoadout = playerWeaponLoadout;
            ResolveLocalHudViews();

            if (playerHealth == null)
            {
                playerHealth = FindFirstObjectByType<PlayerHealth>(FindObjectsInactive.Exclude);

                if (playerHealth == null && !_warnedMissingHealth)
                {
                    Debug.LogWarning("HUDController could not find a PlayerHealth source. Assign one in the inspector for reliable scene setup.", this);
                    _warnedMissingHealth = true;
                }
            }

            if (playerInventory == null)
            {
                playerInventory = FindFirstObjectByType<PlayerInventory>(FindObjectsInactive.Exclude);

                if (playerInventory == null && !_warnedMissingInventory)
                {
                    Debug.LogWarning("HUDController could not find a PlayerInventory source. Assign one in the inspector for reliable scene setup.", this);
                    _warnedMissingInventory = true;
                }
            }

            if (playerConsumableHolder == null)
            {
                playerConsumableHolder = FindFirstObjectByType<PlayerConsumableHolder>(FindObjectsInactive.Exclude);

                if (playerConsumableHolder == null && !_warnedMissingConsumableHolder)
                {
                    Debug.LogWarning("HUDController could not find a PlayerConsumableHolder source. Active slot UI will stay in placeholder mode until one is assigned.", this);
                    _warnedMissingConsumableHolder = true;
                }
            }

            if (playerActiveItemController == null)
            {
                playerActiveItemController = FindFirstObjectByType<PlayerActiveItemController>(FindObjectsInactive.Exclude);

                if (playerActiveItemController == null && !_warnedMissingActiveItemController)
                {
                    Debug.LogWarning("HUDController could not find a PlayerActiveItemController source. Active item UI will stay in placeholder or consumable mode until one is assigned.", this);
                    _warnedMissingActiveItemController = true;
                }
            }

            if (playerTrinketHolder == null)
            {
                playerTrinketHolder = FindFirstObjectByType<PlayerTrinketHolder>(FindObjectsInactive.Exclude);

                if (playerTrinketHolder == null && !_warnedMissingTrinketHolder)
                {
                    Debug.LogWarning("HUDController could not find a PlayerTrinketHolder source. Trinket UI will stay in placeholder mode until one is assigned.", this);
                    _warnedMissingTrinketHolder = true;
                }
            }

            if (playerStats == null)
            {
                playerStats = FindFirstObjectByType<PlayerStats>(FindObjectsInactive.Exclude);

                if (playerStats == null && !_warnedMissingPlayerStats)
                {
                    Debug.LogWarning("HUDController could not find a PlayerStats source. Combat stat HUD will stay in placeholder mode until one is assigned.", this);
                    _warnedMissingPlayerStats = true;
                }
            }

            if (playerWeaponLoadout == null && playerStats != null)
            {
                playerWeaponLoadout = playerStats.GetComponent<PlayerWeaponLoadout>();
            }

            if (playerWeaponLoadout == null)
            {
                playerWeaponLoadout = FindFirstObjectByType<PlayerWeaponLoadout>(FindObjectsInactive.Exclude);
            }

            if (!ReferenceEquals(previousWeaponLoadout, playerWeaponLoadout) && isActiveAndEnabled)
            {
                if (previousWeaponLoadout != null)
                {
                    previousWeaponLoadout.WeaponStateChanged -= HandleWeaponStateChanged;
                }

                if (playerWeaponLoadout != null)
                {
                    playerWeaponLoadout.WeaponStateChanged -= HandleWeaponStateChanged;
                    playerWeaponLoadout.WeaponStateChanged += HandleWeaponStateChanged;
                }
            }

            if (playerCombatMomentumController == null && playerStats != null)
            {
                playerCombatMomentumController = playerStats.GetComponent<PlayerCombatMomentumController>();
            }

            if (playerCombatMomentumController == null)
            {
                playerCombatMomentumController = FindFirstObjectByType<PlayerCombatMomentumController>(FindObjectsInactive.Exclude);
            }

            if (playerRoutePlanCarryController == null && playerStats != null)
            {
                playerRoutePlanCarryController = playerStats.GetComponent<PlayerRoutePlanCarryController>();
            }

            if (playerRoutePlanCarryController == null)
            {
                playerRoutePlanCarryController = FindFirstObjectByType<PlayerRoutePlanCarryController>(FindObjectsInactive.Exclude);
            }

            if (combatStatPanelView == null)
            {
                combatStatPanelView = CreateFallbackCombatStatPanel();
            }

            if (trinketPanelView == null)
            {
                trinketPanelView = CreateFallbackTrinketPanel();
            }

            if (weaponPanelView == null)
            {
                weaponPanelView = CreateFallbackWeaponPanel();
            }

            ApplyWeaponHudLayout(false);

            if (loadoutHistoryPanelView == null)
            {
                loadoutHistoryPanelView = CreateFallbackLoadoutHistoryPanel();
            }

            if (topHudBarView == null)
            {
                topHudBarView = GetComponent<TopHudBarView>();

                if (topHudBarView == null)
                {
                    topHudBarView = gameObject.AddComponent<TopHudBarView>();
                }
            }

            if (roomNavigationController == null)
            {
                roomNavigationController = FindFirstObjectByType<RoomNavigationController>(FindObjectsInactive.Exclude);

                if (roomNavigationController == null && !_warnedMissingRoomNavigationController)
                {
                    Debug.LogWarning("HUDController could not find a RoomNavigationController source. Current room status HUD will stay in fallback mode until one is assigned.", this);
                    _warnedMissingRoomNavigationController = true;
                }
            }

            if (roomTraversalGuidanceController == null && roomNavigationController != null)
            {
                roomTraversalGuidanceController = roomNavigationController.GetComponent<RoomTraversalGuidanceController>();
            }

            if (roomTraversalGuidanceController == null)
            {
                roomTraversalGuidanceController = FindFirstObjectByType<RoomTraversalGuidanceController>(FindObjectsInactive.Exclude);
            }

            if (runManager == null)
            {
                runManager = FindFirstObjectByType<RunManager>(FindObjectsInactive.Exclude);
            }

            if (pauseMenuController == null)
            {
                pauseMenuController = GetComponent<PauseMenuController>();

                if (pauseMenuController == null)
                {
                    pauseMenuController = gameObject.AddComponent<PauseMenuController>();
                }
            }

            pauseMenuController?.ConfigureRuntime(
                pauseMenuView,
                playerStats,
                runManager,
                ResolveHudSafeArea());

            AuditHudViewReferences();
        }

        private void Subscribe()
        {
            GameplayRuntimeEvents.SecretRoomRevealed -= HandleSecretRoomRevealed;
            GameplayRuntimeEvents.SecretRoomRevealed += HandleSecretRoomRevealed;
            GameplayRuntimeEvents.RoomRewardPhaseCompleted -= HandleRoomRewardPhaseCompleted;
            GameplayRuntimeEvents.RoomRewardPhaseCompleted += HandleRoomRewardPhaseCompleted;
            GameplayRuntimeEvents.RoomRewardCollected -= HandleRoomRewardCollected;
            GameplayRuntimeEvents.RoomRewardCollected += HandleRoomRewardCollected;
            GameplayRuntimeEvents.RoomCleared -= HandleRoomCleared;
            GameplayRuntimeEvents.RoomCleared += HandleRoomCleared;
            GameplayRuntimeEvents.CurseRewardManifested -= HandleCurseRewardManifested;
            GameplayRuntimeEvents.CurseRewardManifested += HandleCurseRewardManifested;
            GameplayRuntimeEvents.ChampionEnemyPromoted -= HandleChampionEnemyPromoted;
            GameplayRuntimeEvents.ChampionEnemyPromoted += HandleChampionEnemyPromoted;
            GameplayRuntimeEvents.MomentumExecuted -= HandleMomentumExecuted;
            GameplayRuntimeEvents.MomentumExecuted += HandleMomentumExecuted;
            GameplayRuntimeEvents.PlayerLoadoutDelta -= HandlePlayerLoadoutDelta;
            GameplayRuntimeEvents.PlayerLoadoutDelta += HandlePlayerLoadoutDelta;
            GameplayRuntimeEvents.PlayerInteractionOutcome -= HandlePlayerInteractionOutcome;
            GameplayRuntimeEvents.PlayerInteractionOutcome += HandlePlayerInteractionOutcome;
            GameplayRuntimeEvents.ChoiceRouteResolved -= HandleChoiceRouteResolved;
            GameplayRuntimeEvents.ChoiceRouteResolved += HandleChoiceRouteResolved;

            if (playerHealth != null)
            {
                playerHealth.HealthChanged -= HandleHealthChanged;
                playerHealth.HealthChanged += HandleHealthChanged;
            }

            if (playerInventory != null)
            {
                playerInventory.ResourcesChanged -= HandleResourcesChanged;
                playerInventory.ResourcesChanged += HandleResourcesChanged;
                playerInventory.InventoryChanged -= HandleInventoryChanged;
                playerInventory.InventoryChanged += HandleInventoryChanged;
            }

            if (playerConsumableHolder != null)
            {
                playerConsumableHolder.ConsumableStateChanged -= HandleConsumableStateChanged;
                playerConsumableHolder.ConsumableStateChanged += HandleConsumableStateChanged;
            }

            if (playerActiveItemController != null)
            {
                playerActiveItemController.ActiveItemStateChanged -= HandleActiveItemStateChanged;
                playerActiveItemController.ActiveItemStateChanged += HandleActiveItemStateChanged;
            }

            if (playerTrinketHolder != null)
            {
                playerTrinketHolder.TrinketChanged -= HandleTrinketChanged;
                playerTrinketHolder.TrinketChanged += HandleTrinketChanged;
            }

            if (playerWeaponLoadout != null)
            {
                playerWeaponLoadout.WeaponStateChanged -= HandleWeaponStateChanged;
                playerWeaponLoadout.WeaponStateChanged += HandleWeaponStateChanged;
            }

            if (playerStats != null)
            {
                playerStats.StatsRecalculated -= HandleStatsRecalculated;
                playerStats.StatsRecalculated += HandleStatsRecalculated;
            }

            if (playerCombatMomentumController != null)
            {
                playerCombatMomentumController.MomentumStateChanged -= HandleMomentumStateChanged;
                playerCombatMomentumController.MomentumStateChanged += HandleMomentumStateChanged;
            }

            if (playerRoutePlanCarryController != null)
            {
                playerRoutePlanCarryController.OpeningStateChanged -= HandleRoutePlanOpeningStateChanged;
                playerRoutePlanCarryController.OpeningStateChanged += HandleRoutePlanOpeningStateChanged;
            }

            if (roomNavigationController != null)
            {
                roomNavigationController.CurrentRoomChanged -= HandleCurrentRoomChanged;
                roomNavigationController.CurrentRoomChanged += HandleCurrentRoomChanged;
            }

            if (roomTraversalGuidanceController != null)
            {
                roomTraversalGuidanceController.GuidanceStatusChanged -= HandleTraversalGuidanceStatusChanged;
                roomTraversalGuidanceController.GuidanceStatusChanged += HandleTraversalGuidanceStatusChanged;
            }

            SyncObservedRoom();
        }

        private void Unsubscribe()
        {
            GameplayRuntimeEvents.SecretRoomRevealed -= HandleSecretRoomRevealed;
            GameplayRuntimeEvents.RoomRewardPhaseCompleted -= HandleRoomRewardPhaseCompleted;
            GameplayRuntimeEvents.RoomRewardCollected -= HandleRoomRewardCollected;
            GameplayRuntimeEvents.RoomCleared -= HandleRoomCleared;
            GameplayRuntimeEvents.CurseRewardManifested -= HandleCurseRewardManifested;
            GameplayRuntimeEvents.ChampionEnemyPromoted -= HandleChampionEnemyPromoted;
            GameplayRuntimeEvents.MomentumExecuted -= HandleMomentumExecuted;
            GameplayRuntimeEvents.PlayerLoadoutDelta -= HandlePlayerLoadoutDelta;
            GameplayRuntimeEvents.PlayerInteractionOutcome -= HandlePlayerInteractionOutcome;
            GameplayRuntimeEvents.ChoiceRouteResolved -= HandleChoiceRouteResolved;

            if (playerHealth != null)
            {
                playerHealth.HealthChanged -= HandleHealthChanged;
            }

            if (playerInventory != null)
            {
                playerInventory.ResourcesChanged -= HandleResourcesChanged;
                playerInventory.InventoryChanged -= HandleInventoryChanged;
            }

            if (playerConsumableHolder != null)
            {
                playerConsumableHolder.ConsumableStateChanged -= HandleConsumableStateChanged;
            }

            if (playerActiveItemController != null)
            {
                playerActiveItemController.ActiveItemStateChanged -= HandleActiveItemStateChanged;
            }

            if (playerTrinketHolder != null)
            {
                playerTrinketHolder.TrinketChanged -= HandleTrinketChanged;
            }

            if (playerWeaponLoadout != null)
            {
                playerWeaponLoadout.WeaponStateChanged -= HandleWeaponStateChanged;
            }

            if (playerStats != null)
            {
                playerStats.StatsRecalculated -= HandleStatsRecalculated;
            }

            if (playerCombatMomentumController != null)
            {
                playerCombatMomentumController.MomentumStateChanged -= HandleMomentumStateChanged;
            }

            if (playerRoutePlanCarryController != null)
            {
                playerRoutePlanCarryController.OpeningStateChanged -= HandleRoutePlanOpeningStateChanged;
            }

            if (roomNavigationController != null)
            {
                roomNavigationController.CurrentRoomChanged -= HandleCurrentRoomChanged;
            }

            if (roomTraversalGuidanceController != null)
            {
                roomTraversalGuidanceController.GuidanceStatusChanged -= HandleTraversalGuidanceStatusChanged;
            }

            UnsubscribeObservedRoom();
        }

        private void HandleHealthChanged(float currentHealth, float maxHealth)
        {
            if (healthPanelView != null)
            {
                healthPanelView.SetHealth(currentHealth, maxHealth);
            }
        }

        private void HandleResourcesChanged(PlayerResourceSnapshot resources)
        {
            if (resourcePanelView != null)
            {
                resourcePanelView.SetResources(resources);
            }
        }

        private void HandleInventoryChanged()
        {
            RefreshActiveSlot();
        }

        private void HandleConsumableStateChanged(PlayerConsumableSlotState slotState)
        {
            if (activeItemPanelView != null && (playerActiveItemController == null || !playerActiveItemController.HasEquippedItem))
            {
                activeItemPanelView.SetConsumableSlot(slotState);
            }
        }

        private void HandleActiveItemStateChanged(PlayerActiveItemSlotState slotState)
        {
            if (activeItemPanelView != null)
            {
                activeItemPanelView.SetActiveItemSlot(slotState);
            }
        }

        private void HandleTrinketChanged()
        {
            RefreshTrinketSlot();
        }

        private void HandleWeaponStateChanged()
        {
            RefreshWeaponPanel();
        }

        private void HandleStatsRecalculated(PlayerStatSnapshot snapshot)
        {
            if (combatStatPanelView != null)
            {
                combatStatPanelView.SetMomentumStateSource(playerCombatMomentumController);
                combatStatPanelView.SetRoutePlanStateSource(playerRoutePlanCarryController);
                combatStatPanelView.SetProjectileTraitStateSource(playerStats);
                combatStatPanelView.SetStats(snapshot);
            }
        }

        private void HandleMomentumStateChanged()
        {
            RefreshCombatStats();
            RefreshRoomStatus();
        }

        private void HandleRoutePlanOpeningStateChanged()
        {
            RefreshCombatStats();
            RefreshRoomStatus();
        }

        private void HandleTraversalGuidanceStatusChanged()
        {
            RefreshRoomStatus();
        }

        private void HandleCurrentRoomChanged(RoomController roomController)
        {
            RoomController previousRoom = _observedRoom;

            if (previousRoom != null && previousRoom != roomController)
            {
                PrimeLoadoutRecapStatus(previousRoom, carryForward: true);
                _loadoutRecapSummariesByRoom.Remove(previousRoom.GetInstanceID());
            }

            ResetChallengePaceFeedbackState(true);
            ClearSecretRevealStatus();
            SyncObservedRoom(roomController);
        }

        private void HandleSecretRoomRevealed(SecretRoomRevealedSignal signal)
        {
            if (signal.SourceRoom == null)
            {
                return;
            }

            _revealedSecretDirectionsByRoom[signal.SourceRoom.GetInstanceID()] = signal.RevealDirection;
            _secretRevealSourceRoom = signal.SourceRoom;
            _secretRevealDirection = signal.RevealDirection;
            _secretRevealStatusExpiresAt = Time.unscaledTime + Mathf.Max(0.25f, secretRevealStatusDuration);

            if (signal.SourceRoom == _observedRoom)
            {
                RefreshRoomStatus();
            }
        }

        private void HandleObservedRoomStateChanged(RoomController roomController, RoomState _)
        {
            if (roomController == _observedRoom)
            {
                if (roomController.State != RoomState.Combat)
                {
                    ResetChallengePaceFeedbackState();
                }

                RefreshRoomStatus();
            }
        }

        private void HandleObservedRewardPhaseCompleted(RoomController roomController, int _)
        {
            if (roomController == _observedRoom)
            {
                RefreshRoomStatus();
            }
        }

        private void HandleRoomRewardPhaseCompleted(RoomRewardPhaseSignal signal)
        {
            if (!signal.HasRewards || signal.Room == null)
            {
                return;
            }

            _rewardPhaseSummariesByRoom[signal.Room.GetInstanceID()] = signal.Summary;
            _roomsWithUncollectedRewards.Add(signal.Room.GetInstanceID());
            PrimeMomentumRewardReadyStatus(signal);

            if (signal.Room == _observedRoom)
            {
                RefreshRoomStatus();
            }
        }

        private void HandleRoomRewardCollected(RoomRewardCollectedSignal signal)
        {
            if (!signal.IsValid)
            {
                return;
            }

            if (signal.IsFinalRewardCollection)
            {
                _roomsWithUncollectedRewards.Remove(signal.Room.GetInstanceID());
            }
            else
            {
                _roomsWithUncollectedRewards.Add(signal.Room.GetInstanceID());
            }

            PrimeMomentumRewardClaimStatus(signal);
            _manifestedCurseRewardRaritiesByRoom.Remove(signal.Room.GetInstanceID());

            if (signal.IsFinalRewardCollection)
            {
                PrimeLoadoutRecapStatus(signal.Room, carryForward: false);
            }

            if (signal.Room == _observedRoom)
            {
                RefreshRoomStatus();
            }
        }

        private void HandleRoomCleared(RoomClearSignal signal)
        {
            if (signal.Room == null || !signal.HadCombatEncounter)
            {
                return;
            }

            if (signal.Room.HasRewardContent)
            {
                return;
            }

            PrimeLoadoutRecapStatus(signal.Room, carryForward: false);

            if (signal.Room == _observedRoom)
            {
                RefreshRoomStatus();
            }
        }

        private void HandleCurseRewardManifested(CurseRewardManifestedSignal signal)
        {
            if (!signal.IsValid || signal.Room.RoomType != RoomType.Curse)
            {
                return;
            }

            int roomInstanceId = signal.Room.GetInstanceID();
            if (!_manifestedCurseRewardRaritiesByRoom.TryGetValue(roomInstanceId, out ItemRarity currentRarity)
                || signal.Rarity > currentRarity)
            {
                _manifestedCurseRewardRaritiesByRoom[roomInstanceId] = signal.Rarity;
            }

            if (signal.Room == _observedRoom)
            {
                RefreshRoomStatus();
            }
        }

        private void HandleChampionEnemyPromoted(ChampionEnemyPromotedSignal signal)
        {
            if (!signal.IsValid)
            {
                return;
            }

            if (signal.Room == _observedRoom)
            {
                RefreshRoomStatus();
            }
        }

        private void HandleMomentumExecuted(MomentumExecutionSignal signal)
        {
            if (_observedRoom != null && signal.Room == _observedRoom)
            {
                RefreshRoomStatus();
            }
        }

        private void HandlePlayerLoadoutDelta(PlayerLoadoutDeltaSignal signal)
        {
            if (!signal.IsValid)
            {
                return;
            }

            AppendRunTraceEntry(signal.Headline, signal.Detail, signal.AccentColor, signal.Emphasize, countsAsLoadout: true);
        }

        private void HandlePlayerInteractionOutcome(PlayerInteractionOutcomeSignal signal)
        {
            if (!signal.IsValid)
            {
                return;
            }

            AppendRunTraceEntry(signal.Headline, signal.Detail, signal.AccentColor, signal.Emphasize, countsAsLoadout: false);
        }

        private void HandleChoiceRouteResolved(ChoiceRouteResolvedSignal signal)
        {
            if (!signal.IsValid)
            {
                return;
            }

            AppendRunTraceEntry(
                signal.HasCarryClosure ? signal.CarryHeadline : signal.Headline,
                BuildChoiceRouteTraceDetail(signal),
                signal.AccentColor,
                signal.Emphasize,
                countsAsLoadout: false);

            if (signal.Room == _observedRoom)
            {
                PrimeChoiceRoutePresentationStatus(signal);
                RefreshRoomStatus();
            }
        }

        private void AppendRunTraceEntry(
            string headline,
            string detail,
            Color accentColor,
            bool emphasize,
            bool countsAsLoadout)
        {
            RoomController activeRoom = roomNavigationController != null
                ? roomNavigationController.CurrentRoom
                : _observedRoom;

            if (activeRoom != null)
            {
                int roomInstanceId = activeRoom.GetInstanceID();
                _loadoutRecapSummariesByRoom.TryGetValue(roomInstanceId, out RoomLoadoutRecapSummary recapSummary);
                recapSummary.Room = activeRoom;
                recapSummary.RoomType = activeRoom.RoomType;
                recapSummary.ChangeCount++;

                if (countsAsLoadout)
                {
                    recapSummary.LoadoutChangeCount++;
                }
                else
                {
                    recapSummary.OutcomeChangeCount++;
                }

                if (emphasize)
                {
                    recapSummary.EmphasizedChangeCount++;
                }

                if (!string.Equals(recapSummary.LatestHeadline, headline))
                {
                    recapSummary.PreviousHeadline = recapSummary.LatestHeadline;
                }

                recapSummary.LatestHeadline = headline;
                recapSummary.LatestDetail = detail;
                recapSummary.AccentColor = recapSummary.ChangeCount > 1
                    ? Color.Lerp(recapSummary.AccentColor, accentColor, 0.56f)
                    : accentColor;
                recapSummary.WasPresented = false;
                _loadoutRecapSummariesByRoom[roomInstanceId] = recapSummary;
            }

            _loadoutHistoryEntries.Insert(0, new LoadoutHistoryRuntimeEntry(
                headline,
                detail,
                accentColor,
                emphasize,
                Time.unscaledTime));

            int entryLimit = Mathf.Max(1, maxLoadoutHistoryEntries);
            if (_loadoutHistoryEntries.Count > entryLimit)
            {
                _loadoutHistoryEntries.RemoveRange(entryLimit, _loadoutHistoryEntries.Count - entryLimit);
            }

            RefreshLoadoutHistory(Time.unscaledTime);
        }

        private void RefreshAll()
        {
            if (healthPanelView != null && playerHealth != null)
            {
                healthPanelView.SetHealth(playerHealth.CurrentHealth, playerHealth.MaxHealth);
            }

            if (resourcePanelView != null && playerInventory != null)
            {
                resourcePanelView.SetResources(playerInventory.Resources);
            }

            RefreshActiveSlot();
            RefreshTrinketSlot();
            RefreshWeaponPanel();
            RefreshCombatStats();

            if (bossHpPanelView != null)
            {
                bossHpPanelView.HideBoss();
            }

            if (minimapPanelView != null)
            {
                minimapPanelView.ShowPlaceholder();
            }

            SyncObservedRoom();
            ApplyTopHudBarLayout();
            RefreshLoadoutHistory(Time.unscaledTime);
        }

        private void RefreshActiveSlot()
        {
            if (activeItemPanelView == null)
            {
                return;
            }

            if (playerConsumableHolder != null)
            {
                if (playerActiveItemController != null && playerActiveItemController.HasEquippedItem)
                {
                    activeItemPanelView.SetActiveItemSlot(playerActiveItemController.BuildSlotState());
                    return;
                }

                activeItemPanelView.SetConsumableSlot(playerConsumableHolder.BuildSlotState());
                return;
            }

            if (playerActiveItemController != null && playerActiveItemController.HasEquippedItem)
            {
                activeItemPanelView.SetActiveItemSlot(playerActiveItemController.BuildSlotState());
                return;
            }

            activeItemPanelView.HidePanel();
        }

        private void RefreshTrinketSlot()
        {
            if (trinketPanelView == null)
            {
                return;
            }

            if (playerTrinketHolder != null && playerTrinketHolder.HasTrinket)
            {
                trinketPanelView.SetTrinket(playerTrinketHolder.EquippedTrinket);
                return;
            }

            trinketPanelView.HidePanel();
        }

        private void RefreshWeaponPanel()
        {
            if (weaponPanelView == null)
            {
                return;
            }

            if (playerWeaponLoadout != null && playerWeaponLoadout.HasWeapon)
            {
                PlayerWeaponHudState weaponState = playerWeaponLoadout.BuildHudState();
                ApplyWeaponHudLayout(weaponState.IsCarouselOpen);
                weaponPanelView.SetWeapon(weaponState);
                return;
            }

            ApplyWeaponHudLayout(false);
            weaponPanelView.HidePanel();
        }

        private void RefreshCombatStats()
        {
            if (combatStatPanelView == null)
            {
                return;
            }

            combatStatPanelView.SetMomentumStateSource(playerCombatMomentumController);
            combatStatPanelView.SetRoutePlanStateSource(playerRoutePlanCarryController);
            combatStatPanelView.SetProjectileTraitStateSource(playerStats);

            if (playerStats != null)
            {
                combatStatPanelView.SetStats(playerStats.CurrentStats);
                return;
            }

            combatStatPanelView.HidePanel();
        }

        private void AuditHudViewReferences()
        {
            if (healthPanelView == null && !_warnedMissingHealthPanelView)
            {
                Debug.LogWarning("HUDController is missing a HealthPanelView reference. Health HUD will not render until the view is assigned.", this);
                _warnedMissingHealthPanelView = true;
            }

            if (resourcePanelView == null && !_warnedMissingResourcePanelView)
            {
                Debug.LogWarning("HUDController is missing a ResourcePanelView reference. Resource HUD will not render until the view is assigned.", this);
                _warnedMissingResourcePanelView = true;
            }

            if (minimapPanelView == null && !_warnedMissingMinimapPanelView)
            {
                Debug.LogWarning("HUDController is missing a MinimapPanelView reference. Minimap HUD will stay unavailable until the view is assigned.", this);
                _warnedMissingMinimapPanelView = true;
            }

            if (topHudBarView == null && !_warnedMissingTopHudBarView)
            {
                Debug.LogWarning("HUDController is missing a TopHudBarView reference. Compact top HUD layout will not be applied until the bar view is assigned.", this);
                _warnedMissingTopHudBarView = true;
            }
        }

        private void ApplyTopHudBarLayout()
        {
            if (topHudBarView == null)
            {
                return;
            }

            if (minimapPanelView != null)
            {
                minimapPanelView.SetCompactMode(true);
            }

            topHudBarView.ApplyLayout(
                minimapPanelView != null ? minimapPanelView.transform as RectTransform : null,
                resourcePanelView != null ? resourcePanelView.transform as RectTransform : null,
                combatStatPanelView != null ? combatStatPanelView.transform as RectTransform : null,
                activeItemPanelView != null ? activeItemPanelView.transform as RectTransform : null,
                trinketPanelView != null ? trinketPanelView.transform as RectTransform : null,
                healthPanelView != null ? healthPanelView.transform as RectTransform : null,
                bossHpPanelView != null ? bossHpPanelView.transform as RectTransform : null);

            resourcePanelView?.ApplyTopBarLayout(true);
            combatStatPanelView?.ApplyTopBarLayout(true);
            activeItemPanelView?.ApplyTopBarLayout(true);
            trinketPanelView?.ApplyTopBarLayout(true);
            healthPanelView?.ApplyTopBarLayout(true);
            bossHpPanelView?.ApplyTopHudLayout(true);
        }

        private CombatStatPanelView CreateFallbackCombatStatPanel()
        {
            RectTransform parent = ResolveHudSafeArea();

            if (parent == null)
            {
                return null;
            }

            GameObject panelObject = new("CombatStatPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CombatStatPanelView));
            RectTransform panelRect = panelObject.GetComponent<RectTransform>();
            panelRect.SetParent(parent, false);
            panelRect.anchorMin = new Vector2(0f, 1f);
            panelRect.anchorMax = new Vector2(0f, 1f);
            panelRect.pivot = new Vector2(0f, 1f);
            panelRect.anchoredPosition = new Vector2(24f, -188f);
            panelRect.sizeDelta = new Vector2(184f, 116f);
            panelRect.localScale = Vector3.one;

            Image backgroundImage = panelObject.GetComponent<Image>();
            backgroundImage.color = new Color(0f, 0f, 0f, 0.18f);
            backgroundImage.raycastTarget = false;

            Text attackText = CreateCombatHudText("AttackStat", panelRect, new Vector2(10f, -8f), new Vector2(78f, 24f));
            Text fireRateText = CreateCombatHudText("FireRateStat", panelRect, new Vector2(96f, -8f), new Vector2(78f, 24f));
            Text projectileSpeedText = CreateCombatHudText("ProjectileSpeedStat", panelRect, new Vector2(10f, -36f), new Vector2(78f, 24f));
            Text luckText = CreateCombatHudText("LuckStat", panelRect, new Vector2(96f, -36f), new Vector2(78f, 24f));
            Text momentumText = CreateCombatHudText("MomentumStatus", panelRect, new Vector2(10f, -64f), new Vector2(164f, 18f));
            Text projectileTraitText = CreateCombatHudText("ProjectileTraitStatus", panelRect, new Vector2(10f, -84f), new Vector2(164f, 18f));

            CombatStatPanelView view = panelObject.GetComponent<CombatStatPanelView>();
            view.ConfigureRuntimeView(panelObject, backgroundImage, attackText, fireRateText, projectileSpeedText, luckText, momentumText, projectileTraitText);
            return view;
        }

        private TrinketPanelView CreateFallbackTrinketPanel()
        {
            RectTransform parent = ResolveHudSafeArea();

            if (parent == null)
            {
                return null;
            }

            GameObject panelObject = new("TrinketPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(TrinketPanelView));
            RectTransform panelRect = panelObject.GetComponent<RectTransform>();
            panelRect.SetParent(parent, false);
            panelRect.anchorMin = new Vector2(1f, 1f);
            panelRect.anchorMax = new Vector2(1f, 1f);
            panelRect.pivot = new Vector2(1f, 1f);
            panelRect.anchoredPosition = new Vector2(-24f, -148f);
            panelRect.sizeDelta = new Vector2(188f, 74f);
            panelRect.localScale = Vector3.one;

            Image frameImage = panelObject.GetComponent<Image>();
            frameImage.color = new Color(0f, 0f, 0f, 0.2f);
            frameImage.raycastTarget = false;

            GameObject iconObject = new("TrinketIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            RectTransform iconRect = iconObject.GetComponent<RectTransform>();
            iconRect.SetParent(panelRect, false);
            iconRect.anchorMin = new Vector2(0f, 1f);
            iconRect.anchorMax = new Vector2(0f, 1f);
            iconRect.pivot = new Vector2(0f, 1f);
            iconRect.anchoredPosition = new Vector2(10f, -10f);
            iconRect.sizeDelta = new Vector2(34f, 34f);

            Image iconImage = iconObject.GetComponent<Image>();
            iconImage.color = new Color(1f, 1f, 1f, 0.2f);
            iconImage.raycastTarget = false;

            Text titleText = CreateFallbackHudText("TrinketTitle", panelRect, new Vector2(54f, -8f), new Vector2(124f, 24f), 20, FontStyle.Bold);
            Text detailText = CreateFallbackHudText("TrinketDetail", panelRect, new Vector2(54f, -34f), new Vector2(124f, 24f), 15, FontStyle.Normal);
            detailText.color = new Color(1f, 1f, 1f, 0.82f);

            TrinketPanelView view = panelObject.GetComponent<TrinketPanelView>();
            view.ConfigureRuntimeView(panelObject, frameImage, iconImage, titleText, detailText);
            return view;
        }

        private WeaponPanelView CreateFallbackWeaponPanel()
        {
            RectTransform parent = ResolveHudSafeArea();

            if (parent == null)
            {
                return null;
            }

            GameObject panelObject = new("WeaponPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(WeaponPanelView));
            RectTransform panelRect = panelObject.GetComponent<RectTransform>();
            panelRect.SetParent(parent, false);
            panelRect.anchorMin = new Vector2(1f, 0f);
            panelRect.anchorMax = new Vector2(1f, 0f);
            panelRect.pivot = new Vector2(1f, 0f);
            panelRect.anchoredPosition = new Vector2(-32f, 32f);
            panelRect.sizeDelta = new Vector2(560f, 224f);
            panelRect.localScale = Vector3.one;

            Image backgroundImage = panelObject.GetComponent<Image>();
            backgroundImage.color = new Color(0.03f, 0.06f, 0.1f, 0.88f);
            backgroundImage.raycastTarget = false;

            GameObject iconBackdropObject = new("WeaponIconBackdrop", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            RectTransform iconBackdropRect = iconBackdropObject.GetComponent<RectTransform>();
            iconBackdropRect.SetParent(panelRect, false);
            iconBackdropRect.anchorMin = new Vector2(0f, 1f);
            iconBackdropRect.anchorMax = new Vector2(0f, 1f);
            iconBackdropRect.pivot = new Vector2(0f, 1f);
            iconBackdropRect.anchoredPosition = new Vector2(18f, -18f);
            iconBackdropRect.sizeDelta = new Vector2(116f, 116f);
            Image iconBackdropImage = iconBackdropObject.GetComponent<Image>();
            iconBackdropImage.color = new Color(0.07f, 0.12f, 0.18f, 0.97f);
            iconBackdropImage.raycastTarget = false;

            GameObject iconObject = new("WeaponIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            RectTransform iconRect = iconObject.GetComponent<RectTransform>();
            iconRect.SetParent(iconBackdropRect, false);
            iconRect.anchorMin = new Vector2(0.5f, 0.5f);
            iconRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.anchoredPosition = Vector2.zero;
            iconRect.sizeDelta = new Vector2(88f, 88f);
            Image iconImage = iconObject.GetComponent<Image>();
            iconImage.raycastTarget = false;

            GameObject statusPlateObject = new("WeaponStatusPlate", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            RectTransform statusPlateRect = statusPlateObject.GetComponent<RectTransform>();
            statusPlateRect.SetParent(panelRect, false);
            statusPlateRect.anchorMin = new Vector2(1f, 1f);
            statusPlateRect.anchorMax = new Vector2(1f, 1f);
            statusPlateRect.pivot = new Vector2(1f, 1f);
            statusPlateRect.anchoredPosition = new Vector2(-18f, -18f);
            statusPlateRect.sizeDelta = new Vector2(140f, 42f);
            Image statusPlateImage = statusPlateObject.GetComponent<Image>();
            statusPlateImage.color = new Color(0.09f, 0.15f, 0.22f, 0.96f);
            statusPlateImage.raycastTarget = false;

            GameObject ammoPlateObject = new("WeaponAmmoPlate", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            RectTransform ammoPlateRect = ammoPlateObject.GetComponent<RectTransform>();
            ammoPlateRect.SetParent(panelRect, false);
            ammoPlateRect.anchorMin = new Vector2(0f, 1f);
            ammoPlateRect.anchorMax = new Vector2(0f, 1f);
            ammoPlateRect.pivot = new Vector2(0f, 1f);
            ammoPlateRect.anchoredPosition = new Vector2(152f, -62f);
            ammoPlateRect.sizeDelta = new Vector2(246f, 78f);
            Image ammoPlateImage = ammoPlateObject.GetComponent<Image>();
            ammoPlateImage.color = new Color(0.07f, 0.11f, 0.17f, 0.97f);
            ammoPlateImage.raycastTarget = false;

            GameObject reloadTrackObject = new("ReloadTrack", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            RectTransform reloadTrackRect = reloadTrackObject.GetComponent<RectTransform>();
            reloadTrackRect.SetParent(panelRect, false);
            reloadTrackRect.anchorMin = new Vector2(0f, 0f);
            reloadTrackRect.anchorMax = new Vector2(1f, 0f);
            reloadTrackRect.pivot = new Vector2(0.5f, 0f);
            reloadTrackRect.offsetMin = new Vector2(20f, 16f);
            reloadTrackRect.offsetMax = new Vector2(-20f, 40f);
            Image reloadTrackImage = reloadTrackObject.GetComponent<Image>();
            reloadTrackImage.color = new Color(0.16f, 0.24f, 0.34f, 0.94f);
            reloadTrackImage.raycastTarget = false;

            GameObject reloadFillObject = new("ReloadFill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            RectTransform reloadFillRect = reloadFillObject.GetComponent<RectTransform>();
            reloadFillRect.SetParent(reloadTrackRect, false);
            reloadFillRect.anchorMin = Vector2.zero;
            reloadFillRect.anchorMax = Vector2.one;
            reloadFillRect.offsetMin = Vector2.zero;
            reloadFillRect.offsetMax = Vector2.zero;
            Image reloadFillImage = reloadFillObject.GetComponent<Image>();
            reloadFillImage.type = Image.Type.Filled;
            reloadFillImage.fillMethod = Image.FillMethod.Horizontal;
            reloadFillImage.fillOrigin = 0;
            reloadFillImage.raycastTarget = false;

            Text titleText = CreateFallbackHudText("WeaponTitle", panelRect, new Vector2(152f, -18f), new Vector2(278f, 34f), 30, FontStyle.Bold);
            Text statusText = CreateFallbackHudText("WeaponStatus", statusPlateRect, new Vector2(0f, 0f), new Vector2(140f, 42f), 20, FontStyle.Bold);
            statusText.alignment = TextAnchor.MiddleCenter;
            statusText.rectTransform.anchorMin = Vector2.zero;
            statusText.rectTransform.anchorMax = Vector2.one;
            statusText.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            statusText.rectTransform.anchoredPosition = Vector2.zero;
            statusText.rectTransform.sizeDelta = Vector2.zero;
            Text ammoText = CreateFallbackHudText("WeaponAmmo", ammoPlateRect, new Vector2(12f, -5f), new Vector2(220f, 68f), 52, FontStyle.Bold);
            ammoText.alignment = TextAnchor.MiddleLeft;
            Text detailText = CreateFallbackHudText("WeaponDetail", panelRect, new Vector2(20f, -152f), new Vector2(520f, 26f), 18, FontStyle.Normal);
            Text loadoutText = CreateFallbackHudText("WeaponLoadout", panelRect, new Vector2(20f, -182f), new Vector2(520f, 22f), 16, FontStyle.Normal);

            WeaponPanelView view = panelObject.GetComponent<WeaponPanelView>();
            view.ConfigureRuntimeView(panelObject, backgroundImage, iconBackdropImage, iconImage, statusPlateImage, ammoPlateImage, reloadTrackImage, reloadFillImage, titleText, statusText, ammoText, detailText, loadoutText);
            view.ShowPlaceholder();
            return view;
        }

        private LoadoutHistoryPanelView CreateFallbackLoadoutHistoryPanel()
        {
            RectTransform parent = ResolveHudSafeArea();

            if (parent == null)
            {
                return null;
            }

            GameObject panelObject = new("LoadoutHistoryPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(LoadoutHistoryPanelView));
            RectTransform panelRect = panelObject.GetComponent<RectTransform>();
            panelRect.SetParent(parent, false);
            panelRect.anchorMin = new Vector2(1f, 1f);
            panelRect.anchorMax = new Vector2(1f, 1f);
            panelRect.pivot = new Vector2(1f, 1f);
            panelRect.anchoredPosition = new Vector2(-24f, -336f);
            panelRect.sizeDelta = new Vector2(288f, 176f);
            panelRect.localScale = Vector3.one;

            Image backgroundImage = panelObject.GetComponent<Image>();
            backgroundImage.color = new Color(0.02f, 0.04f, 0.08f, 0.26f);
            backgroundImage.raycastTarget = false;

            Text titleText = CreateFallbackHudText("HistoryTitle", panelRect, new Vector2(14f, -10f), new Vector2(256f, 20f), 15, FontStyle.Bold);
            titleText.color = new Color(0.96f, 0.98f, 1f, 0.92f);

            Text firstEntryText = CreateFallbackHudText("HistoryEntryA", panelRect, new Vector2(14f, -36f), new Vector2(256f, 40f), 16, FontStyle.Normal);
            ConfigureLoadoutHistoryEntryText(firstEntryText);

            Text secondEntryText = CreateFallbackHudText("HistoryEntryB", panelRect, new Vector2(14f, -84f), new Vector2(256f, 40f), 15, FontStyle.Normal);
            ConfigureLoadoutHistoryEntryText(secondEntryText);

            Text thirdEntryText = CreateFallbackHudText("HistoryEntryC", panelRect, new Vector2(14f, -132f), new Vector2(256f, 34f), 15, FontStyle.Normal);
            ConfigureLoadoutHistoryEntryText(thirdEntryText);

            LoadoutHistoryPanelView view = panelObject.GetComponent<LoadoutHistoryPanelView>();
            view.ConfigureRuntimeView(panelObject, backgroundImage, titleText, firstEntryText, secondEntryText, thirdEntryText);
            view.ConfigureLabels(RunTraceTitleLabel, RunTraceEmptyHeadline, RunTraceEmptyDetail);
            return view;
        }

        private RectTransform ResolveHudSafeArea()
        {
            if (transform is RectTransform rootRect && rootRect.childCount > 0 && rootRect.GetChild(0) is RectTransform safeAreaRect)
            {
                return safeAreaRect;
            }

            return transform as RectTransform;
        }

        private void ResolveLocalHudViews()
        {
            bool missingHealthPanelView = healthPanelView == null;
            bool missingResourcePanelView = resourcePanelView == null;
            bool missingActiveItemPanelView = activeItemPanelView == null;
            bool missingCombatStatPanelView = combatStatPanelView == null;
            bool missingTrinketPanelView = trinketPanelView == null;
            bool missingWeaponPanelView = weaponPanelView == null;
            bool missingLoadoutHistoryPanelView = loadoutHistoryPanelView == null;
            bool missingBossHpPanelView = bossHpPanelView == null;
            bool missingMinimapPanelView = minimapPanelView == null;
            bool missingPauseMenuView = pauseMenuView == null;
            bool missingTopHudBarView = topHudBarView == null;
            bool missingPauseMenuController = pauseMenuController == null;

            healthPanelView ??= FindLocalView<HealthPanelView>();
            resourcePanelView ??= FindLocalView<ResourcePanelView>();
            activeItemPanelView ??= FindLocalView<ActiveItemPanelView>();
            combatStatPanelView ??= FindLocalView<CombatStatPanelView>();
            trinketPanelView ??= FindLocalView<TrinketPanelView>();
            weaponPanelView ??= FindLocalView<WeaponPanelView>();
            ApplyWeaponHudLayout(false);
            loadoutHistoryPanelView ??= FindLocalView<LoadoutHistoryPanelView>();
            bossHpPanelView ??= FindLocalView<BossHpPanelView>();
            minimapPanelView ??= FindLocalView<MinimapPanelView>();
            pauseMenuView ??= FindLocalView<PauseMenuView>();
            topHudBarView ??= GetComponent<TopHudBarView>();
            pauseMenuController ??= GetComponent<PauseMenuController>();

            if (!_loggedAutoReboundHudViews)
            {
                LogAutoReboundHudView("HealthPanelView", missingHealthPanelView, healthPanelView);
                LogAutoReboundHudView("ResourcePanelView", missingResourcePanelView, resourcePanelView);
                LogAutoReboundHudView("ActiveItemPanelView", missingActiveItemPanelView, activeItemPanelView);
                LogAutoReboundHudView("CombatStatPanelView", missingCombatStatPanelView, combatStatPanelView);
                LogAutoReboundHudView("TrinketPanelView", missingTrinketPanelView, trinketPanelView);
                LogAutoReboundHudView("WeaponPanelView", missingWeaponPanelView, weaponPanelView);
                LogAutoReboundHudView("LoadoutHistoryPanelView", missingLoadoutHistoryPanelView, loadoutHistoryPanelView);
                LogAutoReboundHudView("BossHpPanelView", missingBossHpPanelView, bossHpPanelView);
                LogAutoReboundHudView("MinimapPanelView", missingMinimapPanelView, minimapPanelView);
                LogAutoReboundHudView("PauseMenuView", missingPauseMenuView, pauseMenuView);
                LogAutoReboundHudView("TopHudBarView", missingTopHudBarView, topHudBarView);
                LogAutoReboundHudView("PauseMenuController", missingPauseMenuController, pauseMenuController);
                _loggedAutoReboundHudViews = true;
            }
        }

        private void ApplyWeaponHudLayout(bool centerForCarousel)
        {
            if (weaponPanelView == null || weaponPanelView.transform is not RectTransform panelRect)
            {
                return;
            }

            if (centerForCarousel)
            {
                panelRect.anchorMin = new Vector2(0.5f, 0.5f);
                panelRect.anchorMax = new Vector2(0.5f, 0.5f);
                panelRect.pivot = new Vector2(0.5f, 0.5f);
                panelRect.anchoredPosition = new Vector2(0f, -12f);
                panelRect.sizeDelta = new Vector2(720f, 280f);
                return;
            }

            panelRect.anchorMin = new Vector2(1f, 0f);
            panelRect.anchorMax = new Vector2(1f, 0f);
            panelRect.pivot = new Vector2(1f, 0f);
            panelRect.anchoredPosition = new Vector2(-32f, 32f);
            panelRect.sizeDelta = new Vector2(560f, 224f);
        }

        private T FindLocalView<T>() where T : Component
        {
            T[] candidates = GetComponentsInChildren<T>(true);

            for (int index = 0; index < candidates.Length; index++)
            {
                T candidate = candidates[index];

                if (candidate != null && candidate.gameObject != gameObject)
                {
                    return candidate;
                }
            }

            return GetComponent<T>();
        }

        private void LogAutoReboundHudView(string label, bool wasMissing, Component resolvedComponent)
        {
            if (!wasMissing || resolvedComponent == null)
            {
                return;
            }
        }

        private Text CreateFallbackHudText(
            string objectName,
            RectTransform parent,
            Vector2 anchoredPosition,
            Vector2 sizeDelta,
            int fontSize,
            FontStyle fontStyle)
        {
            GameObject textObject = new(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.SetParent(parent, false);
            textRect.anchorMin = new Vector2(0f, 1f);
            textRect.anchorMax = new Vector2(0f, 1f);
            textRect.pivot = new Vector2(0f, 1f);
            textRect.anchoredPosition = anchoredPosition;
            textRect.sizeDelta = sizeDelta;

            Text text = textObject.GetComponent<Text>();
            text.font = ResolveFallbackFont();
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.alignment = TextAnchor.UpperLeft;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;
            text.color = Color.white;
            return text;
        }

        private Text CreateCombatHudText(string objectName, RectTransform parent, Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            GameObject textObject = new(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.SetParent(parent, false);
            textRect.anchorMin = new Vector2(0f, 1f);
            textRect.anchorMax = new Vector2(0f, 1f);
            textRect.pivot = new Vector2(0f, 1f);
            textRect.anchoredPosition = anchoredPosition;
            textRect.sizeDelta = sizeDelta;

            Text text = textObject.GetComponent<Text>();
            LocalizedUiFontProvider.Apply(text);
            text.fontSize = 18;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.UpperLeft;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;
            text.color = Color.white;
            return text;
        }

        private static void ConfigureLoadoutHistoryEntryText(Text text)
        {
            if (text == null)
            {
                return;
            }

            LocalizedUiFontProvider.ApplyReadableDefaults(
                text,
                text.fontSize,
                TextAnchor.UpperLeft,
                FontStyle.Normal,
                supportRichText: true,
                horizontalOverflow: HorizontalWrapMode.Wrap,
                verticalOverflow: VerticalWrapMode.Truncate,
                lineSpacing: 0.92f);
            text.color = new Color(1f, 1f, 1f, 0.94f);
        }

        private bool TryTrimExpiredLoadoutHistoryEntries(float now)
        {
            if (_loadoutHistoryEntries.Count == 0)
            {
                return false;
            }

            float lifetime = Mathf.Max(2f, loadoutHistoryEntryLifetime);
            bool trimmed = false;

            for (int index = _loadoutHistoryEntries.Count - 1; index >= 0; index--)
            {
                if (now - _loadoutHistoryEntries[index].Timestamp <= lifetime)
                {
                    continue;
                }

                _loadoutHistoryEntries.RemoveAt(index);
                trimmed = true;
            }

            return trimmed;
        }

        private void RefreshLoadoutHistory(float now)
        {
            ConfigureRunTracePanelView();

            if (loadoutHistoryPanelView == null)
            {
                return;
            }

            if (_loadoutHistoryEntries.Count == 0)
            {
                loadoutHistoryPanelView.ShowPlaceholder();
                return;
            }

            _loadoutHistoryPresentationBuffer.Clear();
            float lifetime = Mathf.Max(2f, loadoutHistoryEntryLifetime);
            int entryLimit = Mathf.Min(Mathf.Max(1, maxLoadoutHistoryEntries), _loadoutHistoryEntries.Count);

            for (int index = 0; index < entryLimit; index++)
            {
                LoadoutHistoryRuntimeEntry entry = _loadoutHistoryEntries[index];
                float freshness = 1f - Mathf.Clamp01((now - entry.Timestamp) / lifetime);

                _loadoutHistoryPresentationBuffer.Add(new LoadoutHistoryPanelView.EntryPresentation(
                    entry.Headline,
                    entry.Detail,
                    entry.AccentColor,
                    entry.Emphasize,
                    freshness));
            }

            loadoutHistoryPanelView.SetEntries(_loadoutHistoryPresentationBuffer);
        }

        private void ConfigureRunTracePanelView()
        {
            loadoutHistoryPanelView?.ConfigureLabels(
                RunTraceTitleLabel,
                RunTraceEmptyHeadline,
                RunTraceEmptyDetail);
        }

        private Font ResolveFallbackFont()
        {
            Text[] existingTexts = GetComponentsInChildren<Text>(true);

            for (int index = 0; index < existingTexts.Length; index++)
            {
                if (existingTexts[index] != null && existingTexts[index].font != null)
                {
                    return existingTexts[index].font;
                }
            }

            return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        private void SyncObservedRoom(RoomController preferredRoom = null)
        {
            RoomController nextRoom = preferredRoom != null
                ? preferredRoom
                : roomNavigationController != null
                    ? roomNavigationController.CurrentRoom
                    : null;

            if (_observedRoom != nextRoom)
            {
                UnsubscribeObservedRoom();
                _observedRoom = nextRoom;
                SubscribeObservedRoom();
            }

            RefreshRoomStatus();
        }

        private void SubscribeObservedRoom()
        {
            if (_observedRoom == null)
            {
                return;
            }

            _observedRoom.StateChanged -= HandleObservedRoomStateChanged;
            _observedRoom.StateChanged += HandleObservedRoomStateChanged;
            _observedRoom.RewardPhaseCompleted -= HandleObservedRewardPhaseCompleted;
            _observedRoom.RewardPhaseCompleted += HandleObservedRewardPhaseCompleted;
        }

        private void UnsubscribeObservedRoom()
        {
            if (_observedRoom == null)
            {
                return;
            }

            _observedRoom.StateChanged -= HandleObservedRoomStateChanged;
            _observedRoom.RewardPhaseCompleted -= HandleObservedRewardPhaseCompleted;
        }

        private void BuildCleanRoomStatus(out string badgeLabel, out string headline, out string detail, out Color accentColor)
        {
            badgeLabel = string.Empty;
            if (_observedRoom == null)
            {
                headline = "현재 방";
                detail = "전투와 보상을 정리하세요";
                accentColor = new Color(0.86f, 0.92f, 1f, 1f);
                return;
            }

            switch (_observedRoom.RoomType)
            {
                case RoomType.Shop:
                    headline = "상점";
                    detail = "가까이 가서 E 또는 Shift로 구매";
                    accentColor = new Color(0.38f, 1f, 0.86f, 1f);
                    return;
                case RoomType.Treasure:
                    headline = _observedRoom.HasRewardContent ? "보물 획득 가능" : "보물방";
                    detail = _observedRoom.HasRewardContent
                        ? "방 안의 보상을 확인하고 선택하세요."
                        : "남은 보상이나 상호작용 요소를 확인하세요.";
                    accentColor = new Color(1f, 0.86f, 0.34f, 1f);
                    return;
                case RoomType.Secret:
                    headline = _observedRoom.HasRewardContent ? "비밀 보상" : "비밀방";
                    detail = _observedRoom.HasRewardContent
                        ? "숨겨진 보상이 열려 있습니다. 놓치지 마세요."
                        : "숨겨진 길과 보상을 다시 살펴보세요.";
                    accentColor = new Color(0.88f, 0.62f, 1f, 1f);
                    return;
                case RoomType.Boss:
                    BuildCleanBossRoomStatus(out headline, out detail, out accentColor);
                    return;
                case RoomType.MiniBoss:
                    BuildCleanMiniBossRoomStatus(out headline, out detail, out accentColor);
                    return;
                case RoomType.Challenge:
                    BuildCleanChallengeRoomStatus(out badgeLabel, out _, out _, out _, out headline, out detail, out accentColor);
                    return;
                case RoomType.Trap:
                    BuildCleanTrapRoomStatus(out headline, out detail, out accentColor);
                    return;
                case RoomType.Curse:
                    BuildCleanCurseRoomStatus(out headline, out detail, out accentColor);
                    return;
                default:
                    BuildCleanNormalRoomStatus(out headline, out detail, out accentColor);
                    return;
            }
        }

        private bool ShouldRefreshChallengeStatusRealtime()
        {
            return minimapPanelView != null
                && _observedRoom != null
                && _observedRoom.RoomType == RoomType.Challenge
                && _observedRoom.State == RoomState.Combat;
        }

        private bool TryGetChallengeRewardSettings(out ChallengeRewardSettings challengeRewardSettings)
        {
            challengeRewardSettings = null;

            if (runManager == null || !runManager.CurrentContext.HasActiveRun)
            {
                return false;
            }

            if (!runManager.TryGetFloorConfig(runManager.CurrentContext.CurrentFloorIndex, out FloorConfig floorConfig) || floorConfig == null)
            {
                return false;
            }

            challengeRewardSettings = floorConfig.ChallengeRewardSettings;
            return challengeRewardSettings != null;
        }

        private bool TryGetChallengeLiveRank(
            out ChallengeClearRank liveRank,
            out float elapsedSeconds,
            out ChallengeRewardSettings challengeRewardSettings)
        {
            liveRank = ChallengeClearRank.None;
            elapsedSeconds = 0f;
            challengeRewardSettings = null;

            if (!ShouldRefreshChallengeStatusRealtime()
                || !TryGetChallengeRewardSettings(out challengeRewardSettings)
                || _observedRoom == null)
            {
                return false;
            }

            elapsedSeconds = _observedRoom.CurrentCombatDuration;
            if (elapsedSeconds <= 0f)
            {
                return false;
            }

            liveRank = challengeRewardSettings.EvaluateRank(elapsedSeconds);
            return liveRank != ChallengeClearRank.None;
        }

        private void RaiseChallengePaceFeedbackIfNeeded()
        {
            if (!TryGetChallengeLiveRank(out ChallengeClearRank currentRank, out float elapsedSeconds, out ChallengeRewardSettings challengeRewardSettings))
            {
                ResetChallengePaceFeedbackState();
                return;
            }

            int roomInstanceId = _observedRoom.GetInstanceID();
            if (_challengeFeedbackRoomInstanceId != roomInstanceId)
            {
                _challengeFeedbackRoomInstanceId = roomInstanceId;
                _lastChallengePaceRank = currentRank;
                return;
            }

            if (_lastChallengePaceRank == currentRank)
            {
                return;
            }

            ChallengePaceBannerPresentation pacePresentation = ChallengeThreatPresentationResolver.BuildPaceBannerPresentation(
                _lastChallengePaceRank,
                currentRank,
                challengeRewardSettings,
                elapsedSeconds);
            GameplayFeedbackEvents.RaiseBannerFeedback(new BannerFeedbackRequest(
                pacePresentation.Title,
                pacePresentation.Subtitle,
                pacePresentation.AccentColor,
                pacePresentation.Duration,
                true,
                pacePresentation.BadgeLabel,
                pacePresentation.SubtitleEyebrow,
                pacePresentation.Stage,
                pacePresentation.LayoutProfile));

            if (_observedRoom != null)
            {
                GameplayFeedbackEvents.RaiseFloatingFeedback(new FloatingFeedbackRequest(
                    _observedRoom.CameraFocusPosition + Vector3.up * 1.08f,
                    pacePresentation.FloatingLabel,
                    pacePresentation.AccentColor,
                    0.7f,
                    0.82f,
                    1.02f,
                    true,
                    pacePresentation.BadgeLabel,
                    pacePresentation.Stage,
                    pacePresentation.LayoutProfile));
            }

            _lastChallengePaceRank = currentRank;
        }

        private void ResetChallengePaceFeedbackState(bool resetRoomBinding = false)
        {
            _lastChallengePaceRank = ChallengeClearRank.None;

            if (resetRoomBinding)
            {
                _challengeFeedbackRoomInstanceId = -1;
            }
        }

        private bool TryExpireSecretRevealStatus(float now)
        {
            if (!HasSecretRevealStatus())
            {
                return false;
            }

            if (now < _secretRevealStatusExpiresAt)
            {
                return false;
            }

            ClearSecretRevealStatus();
            return true;
        }

        private bool TryExpireMomentumRewardPresentationStatus(float now)
        {
            if (!HasMomentumRewardPresentationStatus())
            {
                return false;
            }

            if (now < _momentumRewardPresentationExpiresAt)
            {
                return false;
            }

            ClearMomentumRewardPresentationStatus();
            return true;
        }

        private bool TryExpireLoadoutRecapPresentationStatus(float now)
        {
            if (!HasLoadoutRecapPresentationStatus())
            {
                return false;
            }

            if (now < _loadoutRecapPresentationExpiresAt)
            {
                return false;
            }

            ClearLoadoutRecapPresentationStatus();
            return true;
        }

        private bool TryExpireChoiceRoutePresentationStatus(float now)
        {
            if (!HasChoiceRoutePresentationStatus())
            {
                return false;
            }

            if (now < _choiceRoutePresentationExpiresAt)
            {
                return false;
            }

            ClearChoiceRoutePresentationStatus();
            return true;
        }

        private bool HasSecretRevealStatus()
        {
            return _secretRevealSourceRoom != null
                && _secretRevealStatusExpiresAt > 0f;
        }

        private void ClearSecretRevealStatus()
        {
            _secretRevealSourceRoom = null;
            _secretRevealDirection = RoomDirection.Up;
            _secretRevealStatusExpiresAt = 0f;
        }

        private bool HasMomentumRewardPresentationStatus()
        {
            return _momentumRewardPresentationRoom != null
                && _momentumRewardPresentationExpiresAt > 0f;
        }

        private bool HasLoadoutRecapPresentationStatus()
        {
            return _loadoutRecapPresentationRoom != null
                && _loadoutRecapPresentationExpiresAt > 0f;
        }

        private bool HasChoiceRoutePresentationStatus()
        {
            return _choiceRoutePresentationRoom != null
                && _choiceRoutePresentationExpiresAt > 0f;
        }

        private void ClearMomentumRewardPresentationStatus()
        {
            _momentumRewardPresentationRoom = null;
            _momentumRewardPresentationRoomType = RoomType.Normal;
            _momentumRewardPresentationExpiresAt = 0f;
            _momentumRewardPresentationHeadline = string.Empty;
            _momentumRewardPresentationDetail = string.Empty;
            _momentumRewardPresentationBadgeLabel = "FLOW";
            _momentumRewardPresentationEyebrow = string.Empty;
            _momentumRewardPresentationCompactTag = string.Empty;
            _momentumRewardPresentationDetailEyebrow = string.Empty;
            _momentumRewardPresentationAccent = new Color(1f, 0.84f, 0.36f, 1f);
        }

        private void ClearLoadoutRecapPresentationStatus()
        {
            _loadoutRecapPresentationRoom = null;
            _loadoutRecapPresentationRoomType = RoomType.Normal;
            _loadoutRecapPresentationExpiresAt = 0f;
            _loadoutRecapPresentationHeadline = string.Empty;
            _loadoutRecapPresentationDetail = string.Empty;
            _loadoutRecapPresentationBadgeLabel = "TRACE";
            _loadoutRecapPresentationEyebrow = string.Empty;
            _loadoutRecapPresentationCompactTag = string.Empty;
            _loadoutRecapPresentationDetailEyebrow = string.Empty;
            _loadoutRecapPresentationAccent = new Color(0.7f, 0.88f, 1f, 1f);
        }

        private void ClearChoiceRoutePresentationStatus()
        {
            _choiceRoutePresentationRoom = null;
            _choiceRoutePresentationRoomType = RoomType.Normal;
            _choiceRoutePresentationExpiresAt = 0f;
            _choiceRoutePresentationHeadline = string.Empty;
            _choiceRoutePresentationDetail = string.Empty;
            _choiceRoutePresentationBadgeLabel = "ROUTE";
            _choiceRoutePresentationEyebrow = string.Empty;
            _choiceRoutePresentationCompactTag = string.Empty;
            _choiceRoutePresentationDetailEyebrow = string.Empty;
            _choiceRoutePresentationAccent = new Color(0.72f, 0.9f, 1f, 1f);
        }

        private void PrimeLoadoutRecapStatus(RoomController room, bool carryForward)
        {
            if (room == null
                || !_loadoutRecapSummariesByRoom.TryGetValue(room.GetInstanceID(), out RoomLoadoutRecapSummary summary)
                || summary.ChangeCount <= 0
                || summary.WasPresented)
            {
                return;
            }

            summary.WasPresented = true;
            _loadoutRecapSummariesByRoom[room.GetInstanceID()] = summary;

            bool hasLoadoutGain = summary.LoadoutChangeCount > 0;
            bool hasMixedGain = hasLoadoutGain && summary.OutcomeChangeCount > 0;

            _loadoutRecapPresentationRoom = room;
            _loadoutRecapPresentationRoomType = summary.RoomType;
            _loadoutRecapPresentationHeadline = carryForward
                ? $"{ResolveLoadoutRecapRoomLabel(summary.RoomType)} Locked In"
                : hasMixedGain
                    ? $"{ResolveLoadoutRecapRoomLabel(summary.RoomType)} Trace Gain"
                    : hasLoadoutGain
                        ? summary.ChangeCount > 1
                            ? $"{ResolveLoadoutRecapRoomLabel(summary.RoomType)} Build Gain"
                            : summary.LatestHeadline
                        : summary.ChangeCount > 1
                            ? $"{ResolveLoadoutRecapRoomLabel(summary.RoomType)} Supply Gain"
                            : summary.LatestHeadline;
            _loadoutRecapPresentationDetail = BuildRunTraceRecapDetail(summary, carryForward);
            _loadoutRecapPresentationBadgeLabel = ResolveRunTraceBadgeLabel(summary);
            _loadoutRecapPresentationEyebrow = ResolveRunTraceEyebrow(summary, carryForward);
            _loadoutRecapPresentationCompactTag = ResolveRunTraceCompactTag(summary);
            _loadoutRecapPresentationDetailEyebrow = ResolveRunTraceDetailEyebrow(summary);
            _loadoutRecapPresentationAccent = summary.AccentColor.a > 0.01f
                ? summary.AccentColor
                : new Color(0.7f, 0.88f, 1f, 1f);
            _loadoutRecapPresentationExpiresAt = Time.unscaledTime + Mathf.Max(0.25f, loadoutRecapStatusDuration);
        }

        private void PrimeChoiceRoutePresentationStatus(ChoiceRouteResolvedSignal signal)
        {
            if (!signal.IsValid || signal.Room == null)
            {
                return;
            }

            _choiceRoutePresentationRoom = signal.Room;
            _choiceRoutePresentationRoomType = signal.Room.RoomType;
            _choiceRoutePresentationHeadline = signal.HasCarryClosure
                ? signal.CarryHeadline
                : signal.Headline;
            _choiceRoutePresentationDetail = BuildChoiceRouteTraceDetail(signal);
            _choiceRoutePresentationBadgeLabel = "ROUTE";
            _choiceRoutePresentationEyebrow = signal.HasCarryClosure
                ? "PLAN RESULT"
                : signal.HeldRoute ? "CHOICE SYNC" : "REROUTE LIVE";
            _choiceRoutePresentationCompactTag = signal.HasCarryClosure
                ? signal.CarryHeld ? "LOCKED" : "PIVOT"
                : IsShotProfileCompareLabel(signal.CompareLabel)
                    ? signal.CompareLabel
                    : signal.HeldRoute ? "HELD" : "BREAK";
            _choiceRoutePresentationDetailEyebrow = IsShotProfileCompareLabel(signal.CompareLabel)
                ? signal.CompareLabel
                : signal.HasIntent
                    ? signal.IntentReasonTag
                : signal.HasCarryClosure
                    ? signal.CarryHeld ? "LOCKED PLAN" : "NEW PLAN"
                    : signal.HeldRoute ? "LOCKED PLAN" : "NEW PLAN";
            _choiceRoutePresentationAccent = signal.AccentColor.a > 0.01f
                ? signal.AccentColor
                : new Color(0.72f, 0.9f, 1f, 1f);
            _choiceRoutePresentationExpiresAt = Time.unscaledTime + Mathf.Max(0.25f, choiceRouteStatusDuration + (signal.Emphasize ? 0.24f : 0f));
        }

        private static string BuildChoiceRouteTraceDetail(ChoiceRouteResolvedSignal signal)
        {
            if (signal.HasCarryClosure)
            {
                if (signal.HasCarryDetail && signal.HasDetail)
                {
                    return $"{signal.CarryDetail} / {signal.Detail}";
                }

                if (signal.HasCarryDetail)
                {
                    return signal.CarryDetail;
                }
            }

            if (signal.HasDetail)
            {
                return signal.Detail;
            }

            return signal.HeldRoute
                ? "Your choice reinforced the route recommendation."
                : "Your choice broke the old lane and forced a reroute.";
        }

        private static bool IsShotProfileCompareLabel(string compareLabel)
        {
            if (string.IsNullOrWhiteSpace(compareLabel))
            {
                return false;
            }

            return compareLabel == "SHOT RESET"
                || compareLabel == "BASELINE"
                || compareLabel.Contains("LASER", StringComparison.Ordinal)
                || compareLabel.Contains("ORBIT", StringComparison.Ordinal)
                || compareLabel.Contains("SHIELD", StringComparison.Ordinal)
                || compareLabel.Contains("SPLIT", StringComparison.Ordinal)
                || compareLabel.Contains("BLAST", StringComparison.Ordinal)
                || compareLabel.Contains("LEECH", StringComparison.Ordinal)
                || compareLabel.Contains("BOUNCE", StringComparison.Ordinal);
        }

        private void PrimeMomentumRewardReadyStatus(RoomRewardPhaseSignal signal)
        {
            if (signal.Room == null
                || signal.Room.RoomType == RoomType.Challenge
                || !signal.HasMomentumBonusPresentation)
            {
                return;
            }

            _momentumRewardPresentationRoom = signal.Room;
            _momentumRewardPresentationRoomType = signal.Room.RoomType;
            _momentumRewardPresentationHeadline = "Momentum Payout Live";
            _momentumRewardPresentationDetail = $"{BuildMomentumRewardStatusSegment(signal.Summary)} 쨌 Highlighted flow rewards just dropped into the room.";
            _momentumRewardPresentationBadgeLabel = "FLOW";
            _momentumRewardPresentationEyebrow = "PAYOUT LIVE";
            _momentumRewardPresentationCompactTag = ResolveMomentumRewardCompactTag(signal.Summary);
            _momentumRewardPresentationDetailEyebrow = signal.MomentumBonusItemRolls > 0 ? "HIGHLIGHTED ITEM" : "FLOW CACHE";
            _momentumRewardPresentationAccent = signal.MomentumAccentColor;
            _momentumRewardPresentationExpiresAt = Time.unscaledTime + Mathf.Max(0.25f, momentumRewardTransitionStatusDuration);
        }

        private void PrimeMomentumRewardClaimStatus(RoomRewardCollectedSignal signal)
        {
            if (!signal.IsValid || !signal.IsMomentumReward || signal.Room.RoomType == RoomType.Challenge)
            {
                return;
            }

            _momentumRewardPresentationRoom = signal.Room;
            _momentumRewardPresentationRoomType = signal.RoomType;
            _momentumRewardPresentationBadgeLabel = "FLOW";
            _momentumRewardPresentationAccent = signal.MomentumAccentColor;
            _momentumRewardPresentationCompactTag = signal.IsHighValueMomentumReward ? "ITEM" : "CACHE";
            _momentumRewardPresentationEyebrow = signal.IsHighValueMomentumReward ? "FLOW CLAIMED" : "CACHE CLAIMED";
            _momentumRewardPresentationDetailEyebrow = signal.IsFinalRewardCollection ? "ROOM SECURED" : "PAYOUT CHAIN";
            _momentumRewardPresentationHeadline = signal.IsHighValueMomentumReward
                ? "Flow Item Secured"
                : signal.IsFinalRewardCollection
                    ? "Momentum Payout Secured"
                    : "Flow Cache Secured";
            _momentumRewardPresentationDetail = signal.IsFinalRewardCollection
                ? "All highlighted flow rewards in this room have been secured."
                : $"{signal.RemainingRewardCount} highlighted flow reward{(signal.RemainingRewardCount == 1 ? string.Empty : "s")} still remain in this room.";
            _momentumRewardPresentationExpiresAt = Time.unscaledTime + Mathf.Max(0.25f, momentumRewardClaimStatusDuration);
        }

        private bool TryBuildSecretRevealStatus(out string headline, out string detail, out Color accentColor)
        {
            headline = string.Empty;
            detail = string.Empty;
            accentColor = new Color(0.86f, 0.58f, 1f, 1f);

            if (_observedRoom == null
                || _observedRoom != _secretRevealSourceRoom
                || !HasSecretRevealStatus()
                || Time.unscaledTime >= _secretRevealStatusExpiresAt)
            {
                return false;
            }

            headline = "비밀 통로 개방";
            detail = $"{ResolveSecretRevealDirectionLabel(_secretRevealDirection)} 비밀방이 드러났습니다";
            return true;
        }

        private bool TryBuildTransientMomentumRewardStatus(
            out string headline,
            out string detail,
            out Color accentColor,
            out string badgeLabel,
            out string eyebrow,
            out string compactTag,
            out string detailEyebrow)
        {
            headline = string.Empty;
            detail = string.Empty;
            accentColor = new Color(1f, 0.84f, 0.36f, 1f);
            badgeLabel = "FLOW";
            eyebrow = string.Empty;
            compactTag = string.Empty;
            detailEyebrow = string.Empty;

            if (_observedRoom == null
                || _observedRoom != _momentumRewardPresentationRoom
                || !HasMomentumRewardPresentationStatus()
                || Time.unscaledTime >= _momentumRewardPresentationExpiresAt)
            {
                return false;
            }

            headline = _momentumRewardPresentationHeadline;
            detail = _momentumRewardPresentationDetail;
            accentColor = _momentumRewardPresentationAccent;
            badgeLabel = _momentumRewardPresentationBadgeLabel;
            eyebrow = _momentumRewardPresentationEyebrow;
            compactTag = _momentumRewardPresentationCompactTag;
            detailEyebrow = _momentumRewardPresentationDetailEyebrow;
            return true;
        }

        private bool TryBuildTransientLoadoutRecapStatus(
            out RoomType roomType,
            out string headline,
            out string detail,
            out Color accentColor,
            out string badgeLabel,
            out string eyebrow,
            out string compactTag,
            out string detailEyebrow)
        {
            roomType = RoomType.Normal;
            headline = string.Empty;
            detail = string.Empty;
            accentColor = new Color(0.7f, 0.88f, 1f, 1f);
            badgeLabel = "TRACE";
            eyebrow = string.Empty;
            compactTag = string.Empty;
            detailEyebrow = string.Empty;

            if (!HasLoadoutRecapPresentationStatus()
                || Time.unscaledTime >= _loadoutRecapPresentationExpiresAt)
            {
                return false;
            }

            roomType = _loadoutRecapPresentationRoomType;
            headline = _loadoutRecapPresentationHeadline;
            detail = _loadoutRecapPresentationDetail;
            accentColor = _loadoutRecapPresentationAccent;
            badgeLabel = _loadoutRecapPresentationBadgeLabel;
            eyebrow = _loadoutRecapPresentationEyebrow;
            compactTag = _loadoutRecapPresentationCompactTag;
            detailEyebrow = _loadoutRecapPresentationDetailEyebrow;
            return true;
        }

        private bool TryBuildTransientChoiceRouteStatus(
            out RoomType roomType,
            out string headline,
            out string detail,
            out Color accentColor,
            out string badgeLabel,
            out string eyebrow,
            out string compactTag,
            out string detailEyebrow)
        {
            roomType = RoomType.Normal;
            headline = string.Empty;
            detail = string.Empty;
            accentColor = new Color(0.72f, 0.9f, 1f, 1f);
            badgeLabel = "ROUTE";
            eyebrow = string.Empty;
            compactTag = string.Empty;
            detailEyebrow = string.Empty;

            if (_observedRoom == null
                || _observedRoom != _choiceRoutePresentationRoom
                || !HasChoiceRoutePresentationStatus()
                || Time.unscaledTime >= _choiceRoutePresentationExpiresAt)
            {
                return false;
            }

            roomType = _choiceRoutePresentationRoomType;
            headline = _choiceRoutePresentationHeadline;
            detail = _choiceRoutePresentationDetail;
            accentColor = _choiceRoutePresentationAccent;
            badgeLabel = _choiceRoutePresentationBadgeLabel;
            eyebrow = _choiceRoutePresentationEyebrow;
            compactTag = _choiceRoutePresentationCompactTag;
            detailEyebrow = _choiceRoutePresentationDetailEyebrow;
            return true;
        }

        private bool TryBuildOpenedSecretLinkStatus(out string headline, out string detail, out Color accentColor)
        {
            headline = string.Empty;
            detail = string.Empty;
            accentColor = new Color(0.82f, 0.55f, 1f, 1f);

            if (_observedRoom == null
                || _observedRoom.RoomType == RoomType.Secret
                || !_revealedSecretDirectionsByRoom.TryGetValue(_observedRoom.GetInstanceID(), out RoomDirection revealDirection))
            {
                return false;
            }

            headline = "비밀 통로 연결됨";
            detail = $"{ResolveSecretRevealDirectionLabel(revealDirection)} 비밀문이 열려 있습니다";
            return true;
        }

        private bool TryBuildSecretRewardReadyStatus(out string headline, out string detail, out Color accentColor)
        {
            headline = string.Empty;
            detail = string.Empty;
            accentColor = minimapPanelView != null
                ? minimapPanelView.SecretRewardThemeColor
                : new Color(1f, 0.88f, 0.48f, 1f);

            if (_observedRoom == null
                || _observedRoom.RoomType != RoomType.Secret
                || !_roomsWithUncollectedRewards.Contains(_observedRoom.GetInstanceID()))
            {
                return false;
            }

            headline = "비밀 보상 회수 가능";
            detail = "숨은 보상이 아직 남아 있습니다";
            return true;
        }

        private bool TryBuildCurseEntryCostStatus(out string headline, out string detail, out Color accentColor)
        {
            headline = string.Empty;
            detail = string.Empty;
            accentColor = minimapPanelView != null
                ? minimapPanelView.CurseAlertThemeColor
                : new Color(0.96f, 0.34f, 0.48f, 1f);

            if (_observedRoom == null || _observedRoom.RoomType == RoomType.Curse)
            {
                return false;
            }

            IReadOnlyList<RoomDoor> roomDoors = _observedRoom.RoomDoors;
            if (roomDoors == null)
            {
                return false;
            }

            for (int i = 0; i < roomDoors.Count; i++)
            {
                RoomDoor roomDoor = roomDoors[i];
                if (roomDoor == null || roomDoor.IsLocked || !roomDoor.HasUnpaidHealthEntryCost)
                {
                    continue;
                }

                RoomController connectedRoom = roomDoor.ConnectedRoom;
                if (connectedRoom == null || connectedRoom.RoomType != RoomType.Curse)
                {
                    continue;
                }

                int healthCost = Mathf.Max(1, Mathf.CeilToInt(roomDoor.RequiredHealthToEnter));
                headline = "저주방 입장 경고";
                detail = $"{ResolveSecretRevealDirectionLabel(roomDoor.DoorDirection)} 문은 체력 {healthCost} 소모";
                return true;
            }

            return false;
        }

        private bool TryBuildCurseAltarStatus(out string headline, out string detail, out Color accentColor, out bool emphasize)
        {
            headline = string.Empty;
            detail = string.Empty;
            accentColor = minimapPanelView != null
                ? minimapPanelView.CurseAlertThemeColor
                : new Color(0.88f, 0.36f, 0.54f, 1f);
            emphasize = false;

            if (_observedRoom == null || _observedRoom.RoomType != RoomType.Curse)
            {
                return false;
            }

            if (_observedRoom.HasRewardContent)
            {
                ItemRarity manifestedRarity = ResolveObservedCurseRewardRarity();
                BuildCurseRewardReadyStatus(manifestedRarity, out headline, out detail, out accentColor);
                emphasize = true;
                return true;
            }

            if (_observedRoom.State == RoomState.Rewarded)
            {
                headline = "제단이 잠잠해졌다";
                detail = "대가를 모두 회수했습니다";
                accentColor = new Color(0.78f, 0.48f, 0.62f, 1f);
                return true;
            }

            headline = "피의 제단 대기";
            detail = "제단이 아직 대가를 기다립니다";
            accentColor = new Color(0.84f, 0.34f, 0.66f, 1f);
            return true;
        }

        private ItemRarity ResolveObservedCurseRewardRarity()
        {
            if (_observedRoom == null)
            {
                return ItemRarity.Common;
            }

            return _manifestedCurseRewardRaritiesByRoom.TryGetValue(_observedRoom.GetInstanceID(), out ItemRarity rarity)
                ? rarity
                : ItemRarity.Common;
        }

        private void BuildCurseRewardReadyStatus(ItemRarity rarity, out string headline, out string detail, out Color accentColor)
        {
            accentColor = minimapPanelView != null
                ? minimapPanelView.GetCurseRewardThemeColor(rarity)
                : new Color(0.98f, 0.7f, 0.34f, 1f);

            switch (rarity)
            {
                case ItemRarity.Rare:
                    headline = "진귀한 대가 활성";
                    detail = "희귀 보상이 제단에 맺혔습니다";
                    return;
                case ItemRarity.Legendary:
                    headline = "금단의 대가 활성";
                    detail = "전설급 보상이 드러났습니다";
                    return;
                case ItemRarity.Relic:
                    headline = "유물의 대가 활성";
                    detail = "유물급 보상이 제단에 응결됐습니다";
                    return;
                case ItemRarity.Boss:
                    headline = "왕관의 대가 활성";
                    detail = "보스급 보상이 제단에 드러났습니다";
                    return;
                default:
                    headline = "피의 제단 활성";
                    detail = "제단이 대가를 받아들였습니다";
                    return;
            }
        }

        private bool TryBuildChampionCombatStatus(out string headline, out string detail, out Color accentColor)
        {
            headline = string.Empty;
            detail = string.Empty;
            accentColor = new Color(1f, 0.42f, 0.3f, 1f);

            if (_observedRoom == null || _observedRoom.State != RoomState.Combat)
            {
                return false;
            }

            if (!_observedRoom.TryGetChampionEncounterSummary(out string variantLabel, out Color championAccentColor, out int aliveChampionCount))
            {
                return false;
            }

            accentColor = championAccentColor;
            headline = aliveChampionCount > 1 ? "챔피언 다수" : "챔피언 전투";
            detail = aliveChampionCount > 1
                ? $"{variantLabel} 계열 포함 {aliveChampionCount}체가 전장을 압박합니다"
                : $"{variantLabel} 변종이 공간을 좁혀 옵니다";
            return true;
        }

        private static string ResolveSecretRevealDirectionLabel(RoomDirection direction)
        {
            return direction switch
            {
                RoomDirection.Up => "위쪽",
                RoomDirection.Right => "오른쪽",
                RoomDirection.Down => "아래쪽",
                RoomDirection.Left => "왼쪽",
                _ => "인접한"
            };
        }

        private static string ResolveChallengePaceBannerTitle(ChallengeClearRank previousRank, ChallengeClearRank currentRank)
        {
            return ChallengeThreatPresentationResolver.ResolvePaceBannerTitle(previousRank, currentRank);
        }

        private static string ResolveChallengePaceBannerSubtitle(
            ChallengeClearRank currentRank,
            ChallengeRewardSettings challengeRewardSettings,
            float elapsedSeconds)
        {
            return ChallengeThreatPresentationResolver.ResolvePaceBannerSubtitle(
                currentRank,
                challengeRewardSettings,
                elapsedSeconds);
        }

        private static Color ResolveChallengePaceBannerAccent(ChallengeClearRank currentRank)
        {
            // Centralize pace banner accents through the shared resolver so challenge HUD copy stays consistent.
            return ChallengeThreatPresentationResolver.ResolvePaceBannerAccent(currentRank);
        }

        private bool TryBuildChallengeProgressStatus(out string badgeLabel, out string eyebrow, out string compactTag, out string detailEyebrow, out string headline, out string detail, out Color accentColor)
        {
            badgeLabel = string.Empty;
            eyebrow = string.Empty;
            compactTag = string.Empty;
            detailEyebrow = string.Empty;
            headline = string.Empty;
            detail = string.Empty;
            accentColor = new Color(1f, 0.58f, 0.24f, 1f);

            if (_observedRoom == null || _observedRoom.RoomType != RoomType.Challenge)
            {
                return false;
            }

            if (_observedRoom.State == RoomState.Combat)
            {
                float elapsedSeconds = _observedRoom.CurrentCombatDuration;

                if (TryGetChallengeRewardSettings(out ChallengeRewardSettings sharedChallengeRewardSettings))
                {
                    ChallengeClearRank liveRank = elapsedSeconds <= sharedChallengeRewardSettings.SRankTimeSeconds
                        ? ChallengeClearRank.S
                        : elapsedSeconds <= sharedChallengeRewardSettings.ARankTimeSeconds
                            ? ChallengeClearRank.A
                            : ChallengeClearRank.B;

                    ChallengeRoomStatusPresentation progressPresentation = ChallengeThreatPresentationResolver.BuildProgressStatusPresentation(
                        liveRank,
                        elapsedSeconds,
                        sharedChallengeRewardSettings);
                    badgeLabel = progressPresentation.BadgeLabel;
                    eyebrow = progressPresentation.Eyebrow;
                    compactTag = progressPresentation.CompactTag;
                    detailEyebrow = string.IsNullOrWhiteSpace(progressPresentation.DetailEyebrow)
                        ? progressPresentation.Eyebrow
                        : progressPresentation.DetailEyebrow;
                    headline = progressPresentation.Headline;
                    detail = progressPresentation.Detail;
                    accentColor = progressPresentation.AccentColor;
                    AppendChallengeThreatHeadline(ref headline, ref accentColor);
                    AppendChallengeThreatPreview(ref detail, ref accentColor);
                    return true;
                }

                ChallengeRoomStatusPresentation combatPresentation = ChallengeThreatPresentationResolver.BuildCombatStatusPresentation(elapsedSeconds);
                badgeLabel = combatPresentation.BadgeLabel;
                eyebrow = combatPresentation.Eyebrow;
                compactTag = combatPresentation.CompactTag;
                detailEyebrow = string.IsNullOrWhiteSpace(combatPresentation.DetailEyebrow)
                    ? combatPresentation.Eyebrow
                    : combatPresentation.DetailEyebrow;
                headline = combatPresentation.Headline;
                detail = combatPresentation.Detail;
                accentColor = combatPresentation.AccentColor;
                AppendChallengeThreatHeadline(ref headline, ref accentColor);
                AppendChallengeThreatPreview(ref detail, ref accentColor);
                return true;
            }

            if (_observedRoom.State == RoomState.Rewarded
                && TryGetObservedChallengeRewardSummary(out RoomRewardPhaseSummary rewardSummary))
            {
                int observedRoomInstanceId = _observedRoom.GetInstanceID();
                bool hasUncollectedChallengeRewards = _roomsWithUncollectedRewards.Contains(observedRoomInstanceId);
                ChallengeRoomStatusPresentation rewardPresentation = hasUncollectedChallengeRewards
                    ? BuildChallengeRewardReadyStatusPresentation(rewardSummary)
                    : BuildChallengeRewardClaimedStatusPresentation(rewardSummary);
                badgeLabel = rewardPresentation.BadgeLabel;
                eyebrow = rewardPresentation.Eyebrow;
                compactTag = rewardPresentation.CompactTag;
                detailEyebrow = string.IsNullOrWhiteSpace(rewardPresentation.DetailEyebrow)
                    ? rewardPresentation.Eyebrow
                    : rewardPresentation.DetailEyebrow;
                headline = rewardPresentation.Headline;
                detail = rewardPresentation.Detail;
                accentColor = rewardPresentation.AccentColor;
                return true;
            }

            if (_observedRoom.State == RoomState.Rewarded
                && _observedRoom.HasRewardContent
                && TryGetChallengeRewardSettings(out ChallengeRewardSettings sharedResolvedRewardSettings))
            {
                ChallengeClearRank sharedClearRank = sharedResolvedRewardSettings.EvaluateRank(_observedRoom.LastCombatDuration);
                ChallengeRoomStatusPresentation clearPresentation = ChallengeThreatPresentationResolver.BuildClearStatusPresentation(sharedClearRank);
                badgeLabel = clearPresentation.BadgeLabel;
                eyebrow = clearPresentation.Eyebrow;
                compactTag = clearPresentation.CompactTag;
                detailEyebrow = string.IsNullOrWhiteSpace(clearPresentation.DetailEyebrow)
                    ? clearPresentation.Eyebrow
                    : clearPresentation.DetailEyebrow;
                headline = clearPresentation.Headline;
                detail = clearPresentation.Detail;
                accentColor = clearPresentation.AccentColor;
                return true;
            }

            return false;
        }

        private bool TryGetObservedChallengeRewardSummary(out RoomRewardPhaseSummary summary)
        {
            summary = default;

            if (_observedRoom == null || _observedRoom.RoomType != RoomType.Challenge)
            {
                return false;
            }

            if (!_rewardPhaseSummariesByRoom.TryGetValue(_observedRoom.GetInstanceID(), out summary))
            {
                return false;
            }

            return summary.HasChallengeBonusPresentation || summary.HasRewards;
        }

        private static ChallengeRoomStatusPresentation BuildChallengeRewardReadyStatusPresentation(RoomRewardPhaseSummary summary)
        {
            string detail = BuildChallengeRewardStatusDetail(summary, true);
            Color accentColor = ResolveChallengeRewardStatusAccent(summary, true);
            return new ChallengeRoomStatusPresentation(
                ResolveChallengeRewardStatusBadge(true),
                ResolveChallengeRewardStatusHeadline(summary, true),
                detail,
                accentColor,
                ResolveChallengeRewardStatusEyebrow(true),
                ResolveChallengeRewardStatusCompactTag(summary, true),
                ResolveChallengeRewardStatusDetailEyebrow(true));
        }

        private static ChallengeRoomStatusPresentation BuildChallengeRewardClaimedStatusPresentation(RoomRewardPhaseSummary summary)
        {
            string detail = BuildChallengeRewardStatusDetail(summary, false);
            Color accentColor = ResolveChallengeRewardStatusAccent(summary, false);
            return new ChallengeRoomStatusPresentation(
                ResolveChallengeRewardStatusBadge(false),
                ResolveChallengeRewardStatusHeadline(summary, false),
                detail,
                accentColor,
                ResolveChallengeRewardStatusEyebrow(false),
                ResolveChallengeRewardStatusCompactTag(summary, false),
                ResolveChallengeRewardStatusDetailEyebrow(false));
        }

        private static string BuildChallengeRewardStatusDetail(RoomRewardPhaseSummary summary, bool rewardReady)
        {
            string rankSegment = summary.ChallengeClearRank switch
            {
                ChallengeClearRank.S => "\u0053 \uD398\uC774\uC2A4 \uD655\uBCF4",
                ChallengeClearRank.A => "\u0041 \uD398\uC774\uC2A4 \uD655\uBCF4",
                ChallengeClearRank.B => "\uAE30\uBCF8 \uBCF4\uC0C1 \uD655\uBCF4",
                _ => "\uB3C4\uC804 \uBCF4\uC0C1 \uD655\uBCF4"
            };

            string pressureSegment = summary.ChallengePressureTier switch
            {
                ChallengePressureTier.Deadly => "\uCD5C\uACE0 \uC555\uBC15 \uB3CC\uD30C",
                ChallengePressureTier.Elite => "\uC5D8\uB9AC\uD2B8 \uC555\uBC15 \uB3CC\uD30C",
                ChallengePressureTier.Reinforced => "\uC99D\uC6D0 \uC555\uBC15 \uB3CC\uD30C",
                _ => string.Empty
            };

            string bonusSegment = summary.BonusRewardSelections > 0
                ? $"+\uBCF4\uC0C1 {summary.BonusRewardSelections}"
                : summary.BonusItemRolls > 0
                    ? $"+\uC544\uC774\uD15C {summary.BonusItemRolls}"
                    : summary.RewardCount > 0
                        ? summary.RewardCount == 1
                            ? "\uBCF4\uC0C1 1\uAC1C"
                            : $"\uBCF4\uC0C1 {summary.RewardCount}\uAC1C"
                        : string.Empty;

            string actionSegment = ResolveChallengeRewardStatusAction(summary, rewardReady);

            string summaryLine = rankSegment;
            if (!string.IsNullOrWhiteSpace(pressureSegment))
            {
                summaryLine = $"{summaryLine} \u00B7 {pressureSegment}";
            }

            if (!string.IsNullOrWhiteSpace(bonusSegment))
            {
                summaryLine = $"{summaryLine} \u00B7 {bonusSegment}";
            }

            string momentumSegment = BuildMomentumRewardStatusSegment(summary);
            if (!string.IsNullOrWhiteSpace(momentumSegment))
            {
                summaryLine = $"{summaryLine} \u00B7 {momentumSegment}";
            }

            return $"{summaryLine} \u00B7 {actionSegment}";
        }

        private static string ResolveChallengeRewardStatusBadge(bool rewardReady)
        {
            return rewardReady
                ? "\uB3C4\uC804 \uBCF4\uC0C1"
                : "\uB3C4\uC804 \uC644\uB8CC";
        }

        private static string ResolveChallengeRewardStatusEyebrow(bool rewardReady)
        {
            return rewardReady
                ? "\uD68C\uC218 \uB300\uAE30"
                : "\uC815\uB9AC \uC644\uB8CC";
        }

        private static string ResolveChallengeRewardStatusCompactTag(RoomRewardPhaseSummary summary, bool rewardReady)
        {
            string momentumCompactTag = ResolveMomentumRewardCompactTag(summary);
            if (!string.IsNullOrWhiteSpace(momentumCompactTag))
            {
                return momentumCompactTag;
            }

            if (rewardReady)
            {
                return summary.IsChallengeFinale
                    ? "\uD68C\uC218"
                    : "\uB300\uAE30";
            }

            return summary.IsChallengeFinale && summary.ChallengeClearRank == ChallengeClearRank.S
                ? "\uC81C\uC555"
                : "\uC644\uB8CC";
        }

        private static string ResolveChallengeRewardStatusDetailEyebrow(bool rewardReady)
        {
            return rewardReady
                ? "\uBCF4\uC0C1 \uC815\uB9AC"
                : "\uACB0\uACFC \uC694\uC57D";
        }

        private static string ResolveChallengeRewardStatusHeadline(RoomRewardPhaseSummary summary, bool rewardReady)
        {
            if (rewardReady)
            {
                return summary.IsChallengeFinale
                    ? summary.ChallengeClearRank == ChallengeClearRank.S
                        ? "\uB3C4\uC804 \uC644\uC804 \uC81C\uC555"
                        : "\uB3C4\uC804\uBC29 \uCD5C\uC885 \uC815\uB9AC"
                    : "\uB3C4\uC804 \uBCF4\uC0C1 \uAC1C\uBC29";
            }

            return summary.IsChallengeFinale
                ? summary.ChallengeClearRank == ChallengeClearRank.S
                    ? "\uB3C4\uC804 \uC644\uC804 \uC81C\uC555 \uC644\uB8CC"
                    : "\uB3C4\uC804\uBC29 \uC815\uB9AC \uC644\uB8CC"
                : "\uB3C4\uC804 \uBCF4\uC0C1 \uD68C\uC218 \uC644\uB8CC";
        }

        private static string ResolveChallengeRewardStatusAction(RoomRewardPhaseSummary summary, bool rewardReady)
        {
            if (rewardReady)
            {
                return "\uBCF4\uC0C1\uC744 \uCC59\uAE30\uBA74 \uB3C4\uC804\uBC29\uC774 \uC644\uB8CC\uB429\uB2C8\uB2E4";
            }

            return summary.IsChallengeFinale
                ? "\uCD5C\uC885 \uC6E8\uC774\uBE0C\uC640 \uBCF4\uC0C1 \uD68C\uC218\uB97C \uBAA8\uB450 \uB9C8\uCCE4\uC2B5\uB2C8\uB2E4"
                : "\uB0A8\uC740 \uC804\uD22C \uBCF4\uC0C1 \uC815\uB9AC\uAC00 \uB05D\uB0AC\uC2B5\uB2C8\uB2E4";
        }

        private static Color ResolveChallengeRewardStatusAccent(RoomRewardPhaseSummary summary, bool rewardReady)
        {
            Color baseAccent = summary.ChallengePressureTier switch
            {
                ChallengePressureTier.Deadly => new Color(1f, 0.56f, 0.22f, 1f),
                ChallengePressureTier.Elite => new Color(1f, 0.68f, 0.26f, 1f),
                ChallengePressureTier.Reinforced => new Color(1f, 0.76f, 0.3f, 1f),
                _ => summary.ChallengeClearRank == ChallengeClearRank.S
                    ? new Color(1f, 0.84f, 0.34f, 1f)
                    : new Color(0.92f, 0.76f, 0.34f, 1f)
            };

            if (summary.HasMomentumBonusPresentation)
            {
                baseAccent = Color.Lerp(baseAccent, new Color(1f, 0.84f, 0.36f, 1f), 0.34f);
            }

            return rewardReady
                ? baseAccent
                : Color.Lerp(baseAccent, new Color(0.52f, 1f, 0.72f, 1f), 0.4f);
        }

        private static string BuildMomentumRewardStatusSegment(RoomRewardPhaseSummary summary)
        {
            if (!summary.HasMomentumBonusPresentation)
            {
                return string.Empty;
            }

            if (summary.MomentumBonusRewardSelections > 0 && summary.MomentumBonusItemRolls > 0)
            {
                return $"FLOW +{summary.MomentumBonusRewardSelections}R / +{summary.MomentumBonusItemRolls}I";
            }

            if (summary.MomentumBonusRewardSelections > 0)
            {
                return $"FLOW +{summary.MomentumBonusRewardSelections}R";
            }

            return $"FLOW +{summary.MomentumBonusItemRolls}I";
        }

        private static string ResolveMomentumRewardCompactTag(RoomRewardPhaseSummary summary)
        {
            if (!summary.HasMomentumBonusPresentation)
            {
                return string.Empty;
            }

            return summary.MomentumBonusItemRolls > 0
                ? $"+I{summary.MomentumBonusItemRolls}"
                : $"+R{summary.MomentumBonusRewardSelections}";
        }

        private static string ResolveRouteOpeningCompactTag(string reasonTag)
        {
            return reasonTag switch
            {
                "PRESS ADVANTAGE" => "ADV",
                "RECOVERY ONLINE" => "SAFE",
                "SUPPLY WINDOW" => "SUPPLY",
                "LOADOUT FIND" => "BUILD",
                "SAFE UPGRADE" => "CLEAN",
                _ => "OPEN"
            };
        }

        private static string ResolveRouteOpeningDetailEyebrow(string reasonTag)
        {
            return reasonTag switch
            {
                "PRESS ADVANTAGE" => "FIRST STRIKE",
                "RECOVERY ONLINE" => "SAFE ENTRY",
                "SUPPLY WINDOW" => "FAST LINE",
                "LOADOUT FIND" => "BUILD TEST",
                "SAFE UPGRADE" => "CLEAN ENTRY",
                _ => "ENTRY WINDOW"
            };
        }

        private void AppendChallengeThreatHeadline(ref string headline, ref Color accentColor)
        {
            if (!TryBuildChallengeThreatSegments(
                    out string headlineSegment,
                    out _,
                    out ChallengeThreatPresentation presentation))
            {
                return;
            }

            headline = string.IsNullOrWhiteSpace(headline)
                ? headlineSegment
                : $"{headline} · {headlineSegment}";

            ApplyChallengeThreatAccent(ref accentColor, presentation);
        }

        private void AppendChallengeThreatPreview(ref string detail, ref Color accentColor)
        {
            if (!TryBuildChallengeThreatSegments(
                    out _,
                    out string detailSegment,
                    out ChallengeThreatPresentation presentation))
            {
                return;
            }

            detail = string.IsNullOrWhiteSpace(detail)
                ? detailSegment
                : $"{detail} · {detailSegment}";

            ApplyChallengeThreatAccent(ref accentColor, presentation);
        }

        private bool TryBuildChallengeThreatSegments(
            out string headlineSegment,
            out string detailSegment,
            out ChallengeThreatPresentation presentation)
        {
            headlineSegment = string.Empty;
            detailSegment = string.Empty;
            presentation = default;

            if (!TryGetUpcomingChallengeThreatPresentation(out presentation))
            {
                return false;
            }

            headlineSegment = presentation.HeadlineSegment;
            detailSegment = presentation.DetailSegment;
            return true;
        }

        private bool TryGetUpcomingChallengeThreatPresentation(out ChallengeThreatPresentation presentation)
        {
            presentation = default;

            if (_observedRoom == null
                || _observedRoom.RoomType != RoomType.Challenge
                || !_observedRoom.TryGetUpcomingChallengeWaveThreat(
                    out int nextWaveNumber,
                    out int totalWaveCount,
                    out int enemyCount,
                    out int guaranteedChampionCount,
                    out float championChanceBonus))
            {
                return false;
            }

            presentation = ChallengeThreatPresentationResolver.Build(
                nextWaveNumber,
                totalWaveCount,
                enemyCount,
                guaranteedChampionCount,
                championChanceBonus);
            return true;
        }

        private static void ApplyChallengeThreatAccent(ref Color accentColor, ChallengeThreatPresentation presentation)
        {
            accentColor = presentation.Stage == ChallengeThreatStage.ElitePressure
                ? presentation.AccentColor
                : Color.Lerp(
                    accentColor,
                    presentation.AccentColor,
                    presentation.Stage >= ChallengeThreatStage.EliteReinforcement ? 0.5f : 0.35f);
        }

        private void BuildCleanBossRoomStatus(out string headline, out string detail, out Color accentColor)
        {
            switch (_observedRoom.State)
            {
                case RoomState.Combat:
                    headline = "보스전 진행 중";
                    detail = "패턴을 읽고 치고 빠지세요";
                    accentColor = new Color(1f, 0.36f, 0.36f, 1f);
                    return;
                case RoomState.Rewarded when _observedRoom.HasRewardContent:
                    headline = "보스 처치 완료";
                    detail = "다음 층 전 보상을 챙기세요";
                    accentColor = new Color(1f, 0.8f, 0.34f, 1f);
                    return;
                default:
                    headline = "보스방";
                    detail = "입장 전 회피 공간을 확인하세요";
                    accentColor = new Color(1f, 0.56f, 0.4f, 1f);
                    return;
            }
        }

        private void BuildCleanChallengeRoomStatus(out string badgeLabel, out string eyebrow, out string compactTag, out string detailEyebrow, out string headline, out string detail, out Color accentColor)
        {
            if (TryBuildChallengeProgressStatus(out badgeLabel, out eyebrow, out compactTag, out detailEyebrow, out headline, out detail, out accentColor))
            {
                return;
            }

            ChallengeRoomStatusPresentation presentation = ChallengeThreatPresentationResolver.BuildFallbackRoomStatusPresentation(
                _observedRoom.State,
                _observedRoom.HasRewardContent);
            badgeLabel = presentation.BadgeLabel;
            eyebrow = presentation.Eyebrow;
            compactTag = presentation.Eyebrow;
            detailEyebrow = string.IsNullOrWhiteSpace(presentation.DetailEyebrow)
                ? presentation.Eyebrow
                : presentation.DetailEyebrow;
            headline = presentation.Headline;
            detail = presentation.Detail;
            accentColor = presentation.AccentColor;
        }

        private void BuildCleanMiniBossRoomStatus(out string headline, out string detail, out Color accentColor)
        {
            switch (_observedRoom.State)
            {
                case RoomState.Combat:
                    headline = "미니보스 전투 중";
                    detail = "패턴 간격을 먼저 읽으세요";
                    accentColor = new Color(0.96f, 0.46f, 0.82f, 1f);
                    return;
                case RoomState.Rewarded when _observedRoom.HasRewardContent:
                    headline = "미니보스 처치 완료";
                    detail = "추가 보상을 확인하세요";
                    accentColor = new Color(1f, 0.82f, 0.38f, 1f);
                    return;
                default:
                    headline = "미니보스방";
                    detail = "고난도 전투가 이어집니다";
                    accentColor = new Color(0.88f, 0.54f, 1f, 1f);
                    return;
            }
        }

        private void BuildCleanTrapRoomStatus(out string headline, out string detail, out Color accentColor)
        {
            switch (_observedRoom.State)
            {
                case RoomState.Rewarded when _observedRoom.HasRewardContent:
                    headline = "함정방 보상";
                    detail = "보상 전 바닥 함정을 확인하세요";
                    accentColor = new Color(1f, 0.78f, 0.3f, 1f);
                    return;
                case RoomState.Rewarded:
                    headline = "함정 활성 중";
                    detail = "열렸지만 경로는 아직 위험합니다";
                    accentColor = new Color(1f, 0.46f, 0.34f, 1f);
                    return;
                default:
                    headline = "함정방";
                    detail = "바닥과 장애물을 먼저 보세요";
                    accentColor = new Color(1f, 0.54f, 0.3f, 1f);
                    return;
            }
        }

        private void BuildCleanCurseRoomStatus(out string headline, out string detail, out Color accentColor)
        {
            switch (_observedRoom.State)
            {
                case RoomState.Rewarded when _observedRoom.HasRewardContent:
                    headline = "저주방 보상";
                    detail = "위험한 대가가 열려 있습니다";
                    accentColor = new Color(0.96f, 0.54f, 0.74f, 1f);
                    return;
                case RoomState.Rewarded:
                    headline = "저주방 개방";
                    detail = "방 안 상호작용을 확인하세요";
                    accentColor = new Color(0.9f, 0.42f, 0.66f, 1f);
                    return;
                default:
                    headline = "저주방";
                    detail = "리스크와 보상을 맞바꾸는 방입니다";
                    accentColor = new Color(0.84f, 0.34f, 0.66f, 1f);
                    return;
            }
        }

        private void BuildCleanNormalRoomStatus(out string headline, out string detail, out Color accentColor)
        {
            switch (_observedRoom.State)
            {
                case RoomState.Combat:
                    headline = "전투 진행 중";
                    detail = "공간을 유지하며 적을 정리하세요";
                    accentColor = new Color(1f, 0.48f, 0.38f, 1f);
                    return;
                case RoomState.Rewarded when _observedRoom.HasRewardContent:
                    headline = "보상 개방";
                    detail = "이동 전 보상을 챙기세요";
                    accentColor = new Color(1f, 0.82f, 0.32f, 1f);
                    return;
                case RoomState.Rewarded:
                    headline = "방 정리 완료";
                    detail = "문이 열렸습니다";
                    accentColor = new Color(0.52f, 1f, 0.72f, 1f);
                    return;
                case RoomState.Entered:
                    headline = "방 진입";
                    detail = "적 배치와 회피 경로를 먼저 확인하세요.";
                    accentColor = new Color(0.72f, 0.86f, 1f, 1f);
                    return;
                default:
                    headline = "탐색 중";
                    detail = "방 구조를 먼저 읽으세요";
                    accentColor = new Color(0.86f, 0.92f, 1f, 1f);
                    return;
            }
        }

        private bool TryBuildMomentumCombatStatus(
            out string headline,
            out string detail,
            out Color accentColor,
            out string badgeLabel,
            out string eyebrow,
            out string compactTag,
            out string detailEyebrow)
        {
            headline = string.Empty;
            detail = string.Empty;
            accentColor = Color.white;
            badgeLabel = "FLOW";
            eyebrow = "MOMENTUM";
            compactTag = "PUSH";
            detailEyebrow = "BREAK WINDOW";

            if (_observedRoom == null
                || _observedRoom.State != RoomState.Combat
                || playerCombatMomentumController == null
                || !playerCombatMomentumController.IsMomentumActive)
            {
                return false;
            }

            accentColor = playerCombatMomentumController.AccentColor;
            compactTag = playerCombatMomentumController.ChainCount > 1
                ? $"X{playerCombatMomentumController.ChainCount}"
                : "PUSH";

            headline = playerCombatMomentumController.ChainCount > 1
                ? $"Momentum Chain x{playerCombatMomentumController.ChainCount}"
                : "Momentum Open";

            switch (playerCombatMomentumController.ActiveFormationId)
            {
                case "escort":
                    detail = "Escort line broke. Dash through the front before it resets.";
                    detailEyebrow = "ESCORT BREAK";
                    break;
                case "crossfire":
                    detail = "Crossfire opened up. Dive through the seam and collapse the angle.";
                    detailEyebrow = "ANGLE OPEN";
                    break;
                case "siege":
                    detail = "Backline pressure is shaking. Crush the nest before it stabilizes.";
                    detailEyebrow = "BACKLINE BREAK";
                    break;
                default:
                    detail = "Enemy rhythm broke. Push hard before the formation reforms.";
                    detailEyebrow = "PUSH WINDOW";
                    break;
            }

            RoomMomentumRewardController payoutController = _observedRoom.GetComponent<RoomMomentumRewardController>();
            if (payoutController != null
                && payoutController.TryGetLiveCombatProjection(
                    out int payoutRewardSelections,
                    out int payoutItemRolls,
                    out float rewardProgress,
                    out float itemProgress,
                    out int executionCount,
                    out int criticalExecutionCount,
                    out Color payoutAccent))
            {
                accentColor = Color.Lerp(accentColor, payoutAccent, 0.4f);
                badgeLabel = "FLOW";
                eyebrow = "MOMENTUM PAYOUT";

                if (payoutItemRolls > 0)
                {
                    compactTag = $"+I{payoutItemRolls}";
                    detailEyebrow = "ITEM LOCKED";
                    detail = $"+Reward {payoutRewardSelections} / +Item {payoutItemRolls} banked. {executionCount} cuts in this room.";
                }
                else if (payoutRewardSelections > 0)
                {
                    compactTag = $"+R{payoutRewardSelections}";
                    detailEyebrow = "REWARD LOCKED";
                    detail = $"Reward payout locked. Item roll {Mathf.RoundToInt(itemProgress * 100f)}% · {executionCount} cuts.";
                }
                else
                {
                    compactTag = $"{Mathf.RoundToInt(rewardProgress * 100f):00}%";
                    detailEyebrow = criticalExecutionCount > 0 ? "PAYOUT BUILD / EXECUTE" : "PAYOUT BUILD";
                    detail = $"Reward payout {Mathf.RoundToInt(rewardProgress * 100f)}% · {executionCount} cuts. Keep the window open.";
                }
            }

            return true;
        }

        private bool TryBuildRouteOpeningCombatStatus(
            out string headline,
            out string detail,
            out Color accentColor,
            out string badgeLabel,
            out string eyebrow,
            out string compactTag,
            out string detailEyebrow)
        {
            headline = string.Empty;
            detail = string.Empty;
            accentColor = Color.white;
            badgeLabel = "OPEN";
            eyebrow = "PLAN OPEN";
            compactTag = "OPEN";
            detailEyebrow = "ENTRY WINDOW";

            if (_observedRoom == null
                || _observedRoom.State != RoomState.Combat
                || playerRoutePlanCarryController == null
                || !playerRoutePlanCarryController.IsOpeningActive)
            {
                return false;
            }

            if (playerCombatMomentumController != null && playerCombatMomentumController.IsMomentumActive)
            {
                return false;
            }

            accentColor = playerRoutePlanCarryController.ActiveAccentColor;
            headline = playerRoutePlanCarryController.ActiveOpeningHeadline;
            int remainingPercent = Mathf.RoundToInt(playerRoutePlanCarryController.RemainingOpeningNormalized * 100f);
            string openingLabel = ResolveRouteOpeningCompactTag(playerRoutePlanCarryController.ActiveReasonTag);
            string openingCompareLabel = playerRoutePlanCarryController.ActiveCompareLabel;
            bool hasRecentPreferredDrive = playerRoutePlanCarryController.HasRecentPreferredImpactDrive;
            string recentPreferredDriveCompactTag = playerRoutePlanCarryController.RecentPreferredImpactDriveCompactTag;
            string recentPreferredDriveDetail = playerRoutePlanCarryController.RecentPreferredImpactDriveDetail;
            bool hasRecentPreferredHit = playerRoutePlanCarryController.HasRecentPreferredImpactHit;
            string recentPreferredHitCompactTag = playerRoutePlanCarryController.RecentPreferredImpactHitCompactTag;
            string recentPreferredHitDetail = playerRoutePlanCarryController.RecentPreferredImpactHitDetail;
            bool hasPreferredHold = playerRoutePlanCarryController.HasPreferredImpactHoldDrive;
            string preferredHoldCompactTag = playerRoutePlanCarryController.PreferredImpactHoldDriveCompactTag;
            string preferredHoldDetail = playerRoutePlanCarryController.PreferredImpactHoldDriveDetail;
            bool hasTargetHint = playerRoutePlanCarryController.HasOpeningTargetHint;
            string targetCompactTag = playerRoutePlanCarryController.OpeningTargetCompactTag;
            string targetDirective = playerRoutePlanCarryController.OpeningTargetDirective;
            string targetDetail = playerRoutePlanCarryController.OpeningTargetDetail;
            bool hasRecentRoleBurst = playerRoutePlanCarryController.HasRecentOpeningCadenceHitRoleBurst;
            string recentRoleBurstCompactTag = playerRoutePlanCarryController.RecentOpeningCadenceHitRoleCompactTag;
            string recentRoleBurstDetail = playerRoutePlanCarryController.RecentOpeningCadenceHitRoleDetail;
            bool hasCadenceFeedback = playerRoutePlanCarryController.HasOpeningCadenceFeedback;
            string cadenceLabel = playerRoutePlanCarryController.OpeningCadenceLabel;
            bool hasCollapseFeedback = playerRoutePlanCarryController.HasOpeningCollapseFeedback;
            string collapseLabel = playerRoutePlanCarryController.OpeningCollapseLabel;
            if (hasRecentPreferredDrive)
            {
                accentColor = playerRoutePlanCarryController.RecentPreferredImpactDriveAccentColor;
            }
            else if (hasRecentPreferredHit)
            {
                accentColor = playerRoutePlanCarryController.RecentPreferredImpactHitAccentColor;
            }
            else if (hasPreferredHold)
            {
                accentColor = playerRoutePlanCarryController.PreferredImpactHoldDriveAccentColor;
            }
            if (hasTargetHint && !string.IsNullOrWhiteSpace(targetDetail))
            {
                List<string> detailSegments = new List<string>(6);
                if (hasRecentPreferredDrive && !string.IsNullOrWhiteSpace(recentPreferredDriveDetail))
                {
                    detailSegments.Add(recentPreferredDriveDetail);
                }

                if (hasRecentPreferredHit && !string.IsNullOrWhiteSpace(recentPreferredHitDetail))
                {
                    detailSegments.Add(recentPreferredHitDetail);
                }

                if (hasPreferredHold && !string.IsNullOrWhiteSpace(preferredHoldDetail))
                {
                    detailSegments.Add(preferredHoldDetail);
                }

                if (hasRecentRoleBurst && !string.IsNullOrWhiteSpace(recentRoleBurstDetail))
                {
                    detailSegments.Add(recentRoleBurstDetail);
                }

                detailSegments.Add(targetDetail);

                if (detailSegments.Count == 1 && hasCadenceFeedback && !string.IsNullOrWhiteSpace(cadenceLabel))
                {
                    detailSegments.Add(cadenceLabel);
                }

                detail = $"{string.Join(" / ", detailSegments)} / {remainingPercent:00}%";
            }
            else
            {
                detail = $"{playerRoutePlanCarryController.ActiveOpeningDetail} / {remainingPercent:00}%";
            }
            eyebrow = playerRoutePlanCarryController.ActiveHeldPlan ? "PLAN LOCKED" : "PLAN PIVOT";
            compactTag = hasRecentPreferredDrive && !string.IsNullOrWhiteSpace(recentPreferredDriveCompactTag)
                ? recentPreferredDriveCompactTag
                : hasRecentPreferredHit && !string.IsNullOrWhiteSpace(recentPreferredHitCompactTag)
                ? recentPreferredHitCompactTag
                : hasPreferredHold && !string.IsNullOrWhiteSpace(preferredHoldCompactTag)
                ? preferredHoldCompactTag
                : hasRecentRoleBurst && !string.IsNullOrWhiteSpace(recentRoleBurstCompactTag)
                ? recentRoleBurstCompactTag
                : hasTargetHint && !string.IsNullOrWhiteSpace(targetCompactTag)
                ? targetCompactTag
                : hasCadenceFeedback && !string.IsNullOrWhiteSpace(cadenceLabel)
                    ? cadenceLabel
                : IsShotProfileCompareLabel(openingCompareLabel)
                    ? openingCompareLabel
                : openingLabel;
            detailEyebrow = hasCollapseFeedback && !string.IsNullOrWhiteSpace(collapseLabel)
                ? collapseLabel
                : hasRecentPreferredDrive && !string.IsNullOrWhiteSpace(recentPreferredDriveDetail)
                    ? recentPreferredDriveDetail
                : hasRecentPreferredHit && !string.IsNullOrWhiteSpace(recentPreferredHitDetail)
                    ? recentPreferredHitDetail
                : hasPreferredHold && !string.IsNullOrWhiteSpace(preferredHoldDetail)
                    ? preferredHoldDetail
                : hasRecentRoleBurst && !string.IsNullOrWhiteSpace(recentRoleBurstDetail)
                    ? recentRoleBurstDetail
                : hasCadenceFeedback && !string.IsNullOrWhiteSpace(cadenceLabel)
                    ? cadenceLabel
                : hasTargetHint && !string.IsNullOrWhiteSpace(targetDirective)
                ? targetDirective
                : IsShotProfileCompareLabel(openingCompareLabel)
                    ? openingCompareLabel
                : ResolveRouteOpeningDetailEyebrow(playerRoutePlanCarryController.ActiveReasonTag);
            return true;
        }

        private bool TryBuildMomentumRewardStatus(
            out string headline,
            out string detail,
            out Color accentColor,
            out string badgeLabel,
            out string eyebrow,
            out string compactTag,
            out string detailEyebrow)
        {
            headline = string.Empty;
            detail = string.Empty;
            accentColor = new Color(1f, 0.84f, 0.36f, 1f);
            badgeLabel = "FLOW";
            eyebrow = "PAYOUT READY";
            compactTag = string.Empty;
            detailEyebrow = "BONUS LOOT";

            if (_observedRoom == null
                || _observedRoom.RoomType == RoomType.Challenge
                || _observedRoom.State != RoomState.Rewarded
                || !_rewardPhaseSummariesByRoom.TryGetValue(_observedRoom.GetInstanceID(), out RoomRewardPhaseSummary rewardSummary)
                || !rewardSummary.HasMomentumBonusPresentation)
            {
                return false;
            }

            accentColor = rewardSummary.MomentumAccentColor;
            bool rewardReady = _roomsWithUncollectedRewards.Contains(_observedRoom.GetInstanceID());
            compactTag = ResolveMomentumRewardCompactTag(rewardSummary);
            eyebrow = rewardReady ? "PAYOUT READY" : "PAYOUT CLAIMED";
            detailEyebrow = rewardReady ? "BONUS LOOT" : "RUN RESULT";
            headline = rewardReady ? "Momentum Payout Ready" : "Momentum Payout Claimed";

            string payoutSegment = BuildMomentumRewardStatusSegment(rewardSummary);
            string rewardSegment = rewardSummary.RewardCount == 1
                ? "Reward 1 opened"
                : rewardSummary.RewardCount > 1
                    ? $"Reward {rewardSummary.RewardCount} opened"
                    : "Bonus payout secured";
            string actionSegment = rewardReady
                ? "Collect the payout before leaving the room."
                : "This room already converted flow into loot.";

            detail = $"{payoutSegment} · {rewardSegment} · {actionSegment}";
            return true;
        }

        private bool TryBuildTraversalGuidanceStatus(
            out RoomType roomType,
            out string headline,
            out string detail,
            out Color accentColor,
            out string badgeLabel,
            out string eyebrow,
            out string compactTag,
            out string detailEyebrow)
        {
            roomType = RoomType.Normal;
            headline = string.Empty;
            detail = string.Empty;
            accentColor = new Color(0.72f, 0.9f, 1f, 1f);
            badgeLabel = "ROUTE";
            eyebrow = "GUIDED EXIT";
            compactTag = string.Empty;
            detailEyebrow = string.Empty;

            if (_observedRoom == null
                || roomTraversalGuidanceController == null
                || !roomTraversalGuidanceController.TryGetActiveGuidanceStatus(
                    _observedRoom,
                    out roomType,
                    out accentColor,
                    out headline,
                    out detail,
                    out badgeLabel,
                    out eyebrow,
                    out compactTag,
                    out detailEyebrow))
            {
                return false;
            }

            return true;
        }

        private static string BuildRunTraceRecapDetail(RoomLoadoutRecapSummary summary, bool carryForward)
        {
            string lead;
            if (summary.LoadoutChangeCount > 0 && summary.OutcomeChangeCount > 0)
            {
                lead = $"{summary.LoadoutChangeCount} build shift{(summary.LoadoutChangeCount == 1 ? string.Empty : "s")} and {summary.OutcomeChangeCount} supply swing{(summary.OutcomeChangeCount == 1 ? string.Empty : "s")} landed.";
            }
            else if (summary.LoadoutChangeCount > 0)
            {
                lead = summary.LoadoutChangeCount > 1
                    ? $"{summary.LoadoutChangeCount} build shifts landed."
                    : $"{summary.LatestHeadline} landed.";
            }
            else
            {
                lead = summary.ChangeCount > 1
                    ? $"{summary.ChangeCount} supply swings landed."
                    : $"{summary.LatestHeadline} landed.";
            }

            string detail = !string.IsNullOrWhiteSpace(summary.LatestDetail)
                ? summary.LatestDetail
                : !string.IsNullOrWhiteSpace(summary.PreviousHeadline)
                    ? $"{summary.PreviousHeadline} also connected."
                    : summary.LoadoutChangeCount > 0
                        ? "Your build moved forward in this room."
                        : "Your supplies shifted in this room.";
            string tail = carryForward
                ? "Locked in before you pushed onward."
                : "Locked in for this room.";

            return $"{lead} {detail} {tail}";
        }

        private static string ResolveRunTraceBadgeLabel(RoomLoadoutRecapSummary summary)
        {
            if (summary.LoadoutChangeCount > 0 && summary.OutcomeChangeCount > 0)
            {
                return "TRACE";
            }

            return summary.LoadoutChangeCount > 0
                ? "BUILD"
                : "FLOW";
        }

        private static string ResolveRunTraceEyebrow(RoomLoadoutRecapSummary summary, bool carryForward)
        {
            if (carryForward)
            {
                return "LAST ROOM GAIN";
            }

            if (summary.LoadoutChangeCount > 0 && summary.OutcomeChangeCount > 0)
            {
                return "BUILD + FLOW";
            }

            return summary.LoadoutChangeCount > 0
                ? "LOADOUT LOCKED"
                : "SUPPLY LOCKED";
        }

        private static string ResolveRunTraceCompactTag(RoomLoadoutRecapSummary summary)
        {
            return summary.ChangeCount > 1
                ? $"+{summary.ChangeCount}"
                : summary.LoadoutChangeCount > 0
                    ? "LOCK"
                    : "GAIN";
        }

        private static string ResolveRunTraceDetailEyebrow(RoomLoadoutRecapSummary summary)
        {
            if (summary.LoadoutChangeCount > 0 && summary.OutcomeChangeCount > 0)
            {
                return "MIXED GAIN";
            }

            if (summary.LoadoutChangeCount > 0)
            {
                return summary.EmphasizedChangeCount > 0
                    ? "MAJOR SHIFT"
                    : "RECENT GAIN";
            }

            return summary.EmphasizedChangeCount > 0
                ? "MAJOR FLOW"
                : "RECENT FLOW";
        }

        private static string ResolveLoadoutRecapRoomLabel(RoomType roomType)
        {
            return roomType switch
            {
                RoomType.Shop => "Shop",
                RoomType.Treasure => "Treasure",
                RoomType.Secret => "Secret",
                RoomType.Boss => "Boss",
                RoomType.MiniBoss => "Elite",
                RoomType.Challenge => "Challenge",
                RoomType.Trap => "Trap",
                RoomType.Curse => "Curse",
                _ => "Room"
            };
        }

        private void RefreshRoomStatus()
        {
            if (minimapPanelView == null)
            {
                SyncTopHudThreatTheme();
                return;
            }

            SyncChallengeThreatNodeState();
            SyncChallengeThreatThemeState();

            if (TryBuildSecretRevealStatus(out string secretHeadline, out string secretDetail, out Color secretAccent))
            {
                minimapPanelView.SetRoomStatus(RoomType.Secret, secretHeadline, secretDetail, secretAccent, true);
                return;
            }

            if (TryBuildOpenedSecretLinkStatus(out string openedSecretHeadline, out string openedSecretDetail, out Color openedSecretAccent))
            {
                minimapPanelView.SetRoomStatus(RoomType.Secret, openedSecretHeadline, openedSecretDetail, openedSecretAccent, true);
                return;
            }

            if (TryBuildSecretRewardReadyStatus(out string secretRewardHeadline, out string secretRewardDetail, out Color secretRewardAccent))
            {
                minimapPanelView.SetRoomStatus(RoomType.Secret, secretRewardHeadline, secretRewardDetail, secretRewardAccent, true);
                return;
            }

            if (TryBuildCurseEntryCostStatus(out string curseEntryHeadline, out string curseEntryDetail, out Color curseEntryAccent))
            {
                minimapPanelView.SetRoomStatus(RoomType.Curse, curseEntryHeadline, curseEntryDetail, curseEntryAccent, true);
                return;
            }

            if (TryBuildCurseAltarStatus(out string curseAltarHeadline, out string curseAltarDetail, out Color curseAltarAccent, out bool emphasizeCurseAltar))
            {
                minimapPanelView.SetRoomStatus(RoomType.Curse, curseAltarHeadline, curseAltarDetail, curseAltarAccent, emphasizeCurseAltar);
                return;
            }

            if (TryBuildTransientMomentumRewardStatus(
                out string transientMomentumHeadline,
                out string transientMomentumDetail,
                out Color transientMomentumAccent,
                out string transientMomentumBadgeLabel,
                out string transientMomentumEyebrow,
                out string transientMomentumCompactTag,
                out string transientMomentumDetailEyebrow))
            {
                minimapPanelView.SetRoomStatus(
                    _momentumRewardPresentationRoomType,
                    transientMomentumHeadline,
                    transientMomentumDetail,
                    transientMomentumAccent,
                    true,
                    transientMomentumBadgeLabel,
                    transientMomentumEyebrow,
                    transientMomentumCompactTag,
                    transientMomentumDetailEyebrow);
                return;
            }

            if (TryBuildMomentumRewardStatus(
                out string momentumRewardHeadline,
                out string momentumRewardDetail,
                out Color momentumRewardAccent,
                out string momentumRewardBadgeLabel,
                out string momentumRewardEyebrow,
                out string momentumRewardCompactTag,
                out string momentumRewardDetailEyebrow))
            {
                minimapPanelView.SetRoomStatus(
                    _observedRoom.RoomType,
                    momentumRewardHeadline,
                    momentumRewardDetail,
                    momentumRewardAccent,
                    true,
                    momentumRewardBadgeLabel,
                    momentumRewardEyebrow,
                    momentumRewardCompactTag,
                    momentumRewardDetailEyebrow);
                return;
            }

            if (TryBuildRouteOpeningCombatStatus(
                out string routeOpeningHeadline,
                out string routeOpeningDetail,
                out Color routeOpeningAccent,
                out string routeOpeningBadgeLabel,
                out string routeOpeningEyebrow,
                out string routeOpeningCompactTag,
                out string routeOpeningDetailEyebrow))
            {
                minimapPanelView.SetRoomStatus(
                    _observedRoom.RoomType,
                    routeOpeningHeadline,
                    routeOpeningDetail,
                    routeOpeningAccent,
                    true,
                    routeOpeningBadgeLabel,
                    routeOpeningEyebrow,
                    routeOpeningCompactTag,
                    routeOpeningDetailEyebrow);
                return;
            }

            if (TryBuildMomentumCombatStatus(
                out string momentumHeadline,
                out string momentumDetail,
                out Color momentumAccent,
                out string momentumBadgeLabel,
                out string momentumEyebrow,
                out string momentumCompactTag,
                out string momentumDetailEyebrow))
            {
                minimapPanelView.SetRoomStatus(
                    _observedRoom.RoomType,
                    momentumHeadline,
                    momentumDetail,
                    momentumAccent,
                    true,
                    momentumBadgeLabel,
                    momentumEyebrow,
                    momentumCompactTag,
                    momentumDetailEyebrow);
                return;
            }

            if (TryBuildChampionCombatStatus(out string championHeadline, out string championDetail, out Color championAccent))
            {
                minimapPanelView.SetRoomStatus(_observedRoom != null ? _observedRoom.RoomType : RoomType.Normal, championHeadline, championDetail, championAccent, true);
                return;
            }

            if (_observedRoom != null && _observedRoom.RoomType == RoomType.Challenge)
            {
                BuildCleanChallengeRoomStatus(out string challengeBadgeLabel, out string challengeEyebrow, out string challengeCompactTag, out string challengeDetailEyebrow, out string challengeHeadline, out string challengeDetail, out Color challengeAccentColor);
                minimapPanelView.SetRoomStatus(
                    RoomType.Challenge,
                    challengeHeadline,
                    challengeDetail,
                    challengeAccentColor,
                    false,
                    challengeBadgeLabel,
                    challengeEyebrow,
                    challengeCompactTag,
                    challengeDetailEyebrow);
                return;
            }

            if (TryBuildTransientLoadoutRecapStatus(
                out RoomType loadoutRecapRoomType,
                out string loadoutRecapHeadline,
                out string loadoutRecapDetail,
                out Color loadoutRecapAccent,
                out string loadoutRecapBadgeLabel,
                out string loadoutRecapEyebrow,
                out string loadoutRecapCompactTag,
                out string loadoutRecapDetailEyebrow))
            {
                minimapPanelView.SetRoomStatus(
                    loadoutRecapRoomType,
                    loadoutRecapHeadline,
                    loadoutRecapDetail,
                    loadoutRecapAccent,
                    true,
                    loadoutRecapBadgeLabel,
                    loadoutRecapEyebrow,
                    loadoutRecapCompactTag,
                    loadoutRecapDetailEyebrow);
                return;
            }

            if (TryBuildTransientChoiceRouteStatus(
                out RoomType choiceRouteRoomType,
                out string choiceRouteHeadline,
                out string choiceRouteDetail,
                out Color choiceRouteAccent,
                out string choiceRouteBadgeLabel,
                out string choiceRouteEyebrow,
                out string choiceRouteCompactTag,
                out string choiceRouteDetailEyebrow))
            {
                minimapPanelView.SetRoomStatus(
                    choiceRouteRoomType,
                    choiceRouteHeadline,
                    choiceRouteDetail,
                    choiceRouteAccent,
                    true,
                    choiceRouteBadgeLabel,
                    choiceRouteEyebrow,
                    choiceRouteCompactTag,
                    choiceRouteDetailEyebrow);
                return;
            }

            if (TryBuildTraversalGuidanceStatus(
                out RoomType routeRoomType,
                out string routeHeadline,
                out string routeDetail,
                out Color routeAccent,
                out string routeBadgeLabel,
                out string routeEyebrow,
                out string routeCompactTag,
                out string routeDetailEyebrow))
            {
                minimapPanelView.SetRoomStatus(
                    routeRoomType,
                    routeHeadline,
                    routeDetail,
                    routeAccent,
                    true,
                    routeBadgeLabel,
                    routeEyebrow,
                    routeCompactTag,
                    routeDetailEyebrow);
                return;
            }

            BuildCleanRoomStatus(out string badgeLabel, out string headline, out string detail, out Color accentColor);
            minimapPanelView.SetRoomStatus(
                _observedRoom != null ? _observedRoom.RoomType : RoomType.Normal,
                headline,
                detail,
                accentColor,
                false,
                badgeLabel);
        }

        private void SyncChallengeThreatNodeState()
        {
            if (minimapPanelView == null)
            {
                return;
            }

            if (_observedRoom != null
                && _observedRoom.RoomType == RoomType.Challenge
                && _observedRoom.TryGetUpcomingChallengeWaveThreat(
                    out int nextWaveNumber,
                    out _,
                    out _,
                    out int guaranteedChampionCount,
                    out float championChanceBonus))
            {
                minimapPanelView.SetChallengeThreatNodeState(true, nextWaveNumber, guaranteedChampionCount, championChanceBonus);
                return;
            }

            minimapPanelView.SetChallengeThreatNodeState(false, 0, 0, 0f);
        }

        private void SyncChallengeThreatThemeState()
        {
            bool hasMinimapThreatThemeConsumer = minimapPanelView != null;
            bool hasTopHudThreatThemeConsumer = topHudBarView != null
                || combatStatPanelView != null
                || resourcePanelView != null
                || activeItemPanelView != null
                || trinketPanelView != null
                || healthPanelView != null;

            if (!hasMinimapThreatThemeConsumer && !hasTopHudThreatThemeConsumer)
            {
                return;
            }

            bool hasPresentation = TryGetUpcomingChallengeThreatPresentation(out ChallengeThreatPresentation presentation);
            ApplyChallengeThreatStatusTheme(hasPresentation, presentation);
            ApplyTopHudThreatTheme(hasPresentation, presentation);
        }

        private void ApplyChallengeThreatStatusTheme(bool hasPresentation, ChallengeThreatPresentation presentation)
        {
            if (minimapPanelView == null)
            {
                return;
            }

            if (hasPresentation)
            {
                minimapPanelView.SetChallengeThreatStatusTheme(
                    true,
                    presentation.AccentColor,
                    presentation.BadgeLabel,
                    presentation.HeadlineEyebrow,
                    presentation.DetailEyebrow,
                    presentation.CompactTag,
                    presentation.Stage);
                return;
            }

            minimapPanelView.SetChallengeThreatStatusTheme(false, Color.white, string.Empty, string.Empty, string.Empty, string.Empty, ChallengeThreatStage.Baseline);
        }

        private void SyncTopHudThreatTheme()
        {
            bool hasPresentation = TryGetUpcomingChallengeThreatPresentation(out ChallengeThreatPresentation presentation);
            ApplyTopHudThreatTheme(hasPresentation, presentation);
        }

        private void ApplyTopHudThreatTheme(bool hasPresentation, ChallengeThreatPresentation presentation)
        {
            if (topHudBarView == null && combatStatPanelView == null && resourcePanelView == null && activeItemPanelView == null && trinketPanelView == null && healthPanelView == null)
            {
                return;
            }

            if (hasPresentation)
            {
                topHudBarView?.SetChallengeThreatTheme(true, presentation.AccentColor, presentation.BadgeLabel, presentation.Stage);
                combatStatPanelView?.SetChallengeThreatTheme(true, presentation.AccentColor, presentation.BadgeLabel, presentation.Stage);
                resourcePanelView?.SetChallengeThreatTheme(true, presentation.AccentColor, presentation.BadgeLabel, presentation.Stage);
                activeItemPanelView?.SetChallengeThreatTheme(true, presentation.AccentColor, presentation.BadgeLabel, presentation.Stage);
                trinketPanelView?.SetChallengeThreatTheme(true, presentation.AccentColor, presentation.BadgeLabel, presentation.Stage);
                healthPanelView?.SetChallengeThreatTheme(true, presentation.AccentColor, presentation.BadgeLabel, presentation.Stage);
                return;
            }

            topHudBarView?.SetChallengeThreatTheme(false, Color.white, string.Empty, ChallengeThreatStage.Baseline);
            combatStatPanelView?.SetChallengeThreatTheme(false, Color.white, string.Empty, ChallengeThreatStage.Baseline);
            resourcePanelView?.SetChallengeThreatTheme(false, Color.white, string.Empty, ChallengeThreatStage.Baseline);
            activeItemPanelView?.SetChallengeThreatTheme(false, Color.white, string.Empty, ChallengeThreatStage.Baseline);
            trinketPanelView?.SetChallengeThreatTheme(false, Color.white, string.Empty, ChallengeThreatStage.Baseline);
            healthPanelView?.SetChallengeThreatTheme(false, Color.white, string.Empty, ChallengeThreatStage.Baseline);
        }

        private void BuildReadableRoomStatus(out string headline, out string detail, out Color accentColor)
        {
            if (_observedRoom == null)
            {
                headline = "현재 방";
                detail = "방을 탐험하고 전투를 정리하세요.";
                accentColor = new Color(0.86f, 0.92f, 1f, 1f);
                return;
            }

            switch (_observedRoom.RoomType)
            {
                case RoomType.Shop:
                    headline = "상점";
                    detail = "가까이 이동한 뒤 E 또는 Shift로 구매";
                    accentColor = new Color(0.38f, 1f, 0.86f, 1f);
                    return;
                case RoomType.Treasure:
                    headline = _observedRoom.HasRewardContent ? "보물 선택 가능" : "보물방";
                    detail = _observedRoom.HasRewardContent ? "열기 전에 보상을 확인하세요." : "보상 선택지를 확인하세요.";
                    accentColor = new Color(1f, 0.86f, 0.34f, 1f);
                    return;
                case RoomType.Secret:
                    headline = _observedRoom.HasRewardContent ? "비밀 보상" : "비밀방";
                    detail = _observedRoom.HasRewardContent ? "이동 전에 숨겨진 보상을 챙기세요." : "방 안의 숨은 보상을 찾아보세요.";
                    accentColor = new Color(0.88f, 0.62f, 1f, 1f);
                    return;
                case RoomType.Boss:
                    BuildReadableBossRoomStatus(out headline, out detail, out accentColor);
                    return;
                case RoomType.MiniBoss:
                    BuildReadableMiniBossRoomStatus(out headline, out detail, out accentColor);
                    return;
                case RoomType.Challenge:
                    BuildReadableChallengeRoomStatus(out headline, out detail, out accentColor);
                    return;
                case RoomType.Trap:
                    BuildReadableTrapRoomStatus(out headline, out detail, out accentColor);
                    return;
                case RoomType.Curse:
                    BuildReadableCurseRoomStatus(out headline, out detail, out accentColor);
                    return;
                default:
                    BuildReadableNormalRoomStatus(out headline, out detail, out accentColor);
                    return;
            }
        }

        private void BuildReadableBossRoomStatus(out string headline, out string detail, out Color accentColor)
        {
            switch (_observedRoom.State)
            {
                case RoomState.Combat:
                    headline = "보스전 진행 중";
                    detail = "큰 동작을 읽고 짧게 피한 뒤 공격하세요.";
                    accentColor = new Color(1f, 0.36f, 0.36f, 1f);
                    return;
                case RoomState.Rewarded when _observedRoom.HasRewardContent:
                    headline = "보스 처치 완료";
                    detail = "다음 층으로 가기 전에 보상을 챙기세요.";
                    accentColor = new Color(1f, 0.8f, 0.34f, 1f);
                    return;
                default:
                    headline = "보스방";
                    detail = "전투 시작 전에 이동 경로를 먼저 확인하세요.";
                    accentColor = new Color(1f, 0.56f, 0.4f, 1f);
                    return;
            }
        }

        private void BuildReadableChallengeRoomStatus(out string headline, out string detail, out Color accentColor)
        {
            if (TryBuildChallengeProgressStatus(out _, out _, out _, out _, out headline, out detail, out accentColor))
            {
                return;
            }

            ChallengeThreatPresentationResolver.BuildFallbackRoomStatus(
                _observedRoom.State,
                _observedRoom.HasRewardContent,
                out headline,
                out detail,
                out accentColor);
        }

        private void BuildReadableMiniBossRoomStatus(out string headline, out string detail, out Color accentColor)
        {
            switch (_observedRoom.State)
            {
                case RoomState.Combat:
                    headline = "미니보스 전투 중";
                    detail = "작은 보스지만 공격 구간을 먼저 읽으세요.";
                    accentColor = new Color(0.96f, 0.46f, 0.82f, 1f);
                    return;
                case RoomState.Rewarded when _observedRoom.HasRewardContent:
                    headline = "미니보스 처치 완료";
                    detail = "이동 전에 특별 보상을 챙기세요.";
                    accentColor = new Color(1f, 0.82f, 0.38f, 1f);
                    return;
                default:
                    headline = "미니보스방";
                    detail = "일반 웨이브보다 더 날카로운 전투가 이어집니다.";
                    accentColor = new Color(0.88f, 0.54f, 1f, 1f);
                    return;
            }
        }

        private void BuildReadableTrapRoomStatus(out string headline, out string detail, out Color accentColor)
        {
            switch (_observedRoom.State)
            {
                case RoomState.Rewarded when _observedRoom.HasRewardContent:
                    headline = "함정방 보상";
                    detail = "보상을 챙기되 바닥 함정도 끝까지 보세요.";
                    accentColor = new Color(1f, 0.78f, 0.3f, 1f);
                    return;
                case RoomState.Rewarded:
                    headline = "함정 활성화";
                    detail = "방은 열렸지만 안전한 경로는 아직 위험합니다.";
                    accentColor = new Color(1f, 0.46f, 0.34f, 1f);
                    return;
                default:
                    headline = "함정방";
                    detail = "바닥을 읽고 안전한 길로 이동하세요.";
                    accentColor = new Color(1f, 0.54f, 0.3f, 1f);
                    return;
            }
        }

        private void BuildReadableCurseRoomStatus(out string headline, out string detail, out Color accentColor)
        {
            switch (_observedRoom.State)
            {
                case RoomState.Rewarded when _observedRoom.HasRewardContent:
                    headline = "저주방 보상";
                    detail = "위험을 감수한 뒤 얻는 특수 보상이 남아 있습니다.";
                    accentColor = new Color(0.96f, 0.54f, 0.74f, 1f);
                    return;
                case RoomState.Rewarded:
                    headline = "저주방 개방";
                    detail = "방은 열렸지만 안쪽 오브젝트를 한 번 더 확인하세요.";
                    accentColor = new Color(0.9f, 0.42f, 0.66f, 1f);
                    return;
                default:
                    headline = "저주방";
                    detail = "특수 보상과 교환되는 위험 방입니다.";
                    accentColor = new Color(0.84f, 0.34f, 0.66f, 1f);
                    return;
            }
        }

        private void BuildReadableNormalRoomStatus(out string headline, out string detail, out Color accentColor)
        {
            switch (_observedRoom.State)
            {
                case RoomState.Combat:
                    headline = "전투 진행 중";
                    detail = "공간을 먼저 확보한 뒤 적을 정리하세요.";
                    accentColor = new Color(1f, 0.48f, 0.38f, 1f);
                    return;
                case RoomState.Rewarded when _observedRoom.HasRewardContent:
                    headline = "보상 개방 완료";
                    detail = "다음 방으로 가기 전에 보상을 챙길 수 있습니다.";
                    accentColor = new Color(1f, 0.82f, 0.32f, 1f);
                    return;
                case RoomState.Rewarded:
                    headline = "방 클리어";
                    detail = "문이 열렸고 전투 압박이 사라졌습니다.";
                    accentColor = new Color(0.52f, 1f, 0.72f, 1f);
                    return;
                case RoomState.Entered:
                    headline = "방 진입";
                    detail = "첫 배치와 경로를 확인하세요";
                    accentColor = new Color(0.72f, 0.86f, 1f, 1f);
                    return;
                default:
                    headline = "탐색 중";
                    detail = "서두르지 말고 방 구조를 먼저 읽으세요.";
                    accentColor = new Color(0.86f, 0.92f, 1f, 1f);
                    return;
            }
        }

        private void BuildRoomStatus(out string headline, out string detail, out Color accentColor)
        {
            if (_observedRoom == null)
            {
                headline = "현재 방";
                detail = "방을 탐색하고 전투를 정리하세요";
                accentColor = new Color(0.86f, 0.92f, 1f, 1f);
                return;
            }

            switch (_observedRoom.RoomType)
            {
                case RoomType.Shop:
                    headline = "상점";
                    detail = "가까이 이동한 뒤 E 또는 Shift로 구매";
                    accentColor = new Color(0.38f, 1f, 0.86f, 1f);
                    return;
                case RoomType.Treasure:
                    headline = _observedRoom.HasRewardContent ? "보물 획득 가능" : "보물방";
                    detail = _observedRoom.HasRewardContent ? "나가기 전에 보상을 선택하세요" : "보상 선택지를 확인하세요";
                    accentColor = new Color(1f, 0.86f, 0.34f, 1f);
                    return;
                case RoomType.Secret:
                    headline = _observedRoom.HasRewardContent ? "비밀 보상" : "비밀방";
                    detail = _observedRoom.HasRewardContent ? "이동 전에 숨겨진 보상을 챙기세요" : "방 안의 숨은 이득을 찾아보세요";
                    accentColor = new Color(0.88f, 0.62f, 1f, 1f);
                    return;
                case RoomType.Boss:
                    BuildBossRoomStatus(out headline, out detail, out accentColor);
                    return;
                case RoomType.MiniBoss:
                    BuildMiniBossRoomStatus(out headline, out detail, out accentColor);
                    return;
                case RoomType.Challenge:
                    BuildChallengeRoomStatus(out headline, out detail, out accentColor);
                    return;
                case RoomType.Trap:
                    BuildTrapRoomStatus(out headline, out detail, out accentColor);
                    return;
                case RoomType.Curse:
                    BuildCurseRoomStatus(out headline, out detail, out accentColor);
                    return;
                default:
                    BuildNormalRoomStatus(out headline, out detail, out accentColor);
                    return;
            }
        }

        private void BuildBossRoomStatus(out string headline, out string detail, out Color accentColor)
        {
            switch (_observedRoom.State)
            {
                case RoomState.Combat:
                    headline = "보스전 진행 중";
                    detail = "예고 동작을 읽고 넓게 피한 뒤 공격하세요";
                    accentColor = new Color(1f, 0.36f, 0.36f, 1f);
                    return;
                case RoomState.Rewarded when _observedRoom.HasRewardContent:
                    headline = "보스 처치 완료";
                    detail = "포탈에 들어가기 전에 보상을 챙기세요";
                    accentColor = new Color(1f, 0.8f, 0.34f, 1f);
                    return;
                default:
                    headline = "보스방";
                    detail = "전투 시작 전에 이동 경로를 먼저 확인하세요";
                    accentColor = new Color(1f, 0.56f, 0.4f, 1f);
                    return;
            }
        }

        private void BuildChallengeRoomStatus(out string headline, out string detail, out Color accentColor)
        {
            if (TryBuildChallengeProgressStatus(out _, out _, out _, out _, out headline, out detail, out accentColor))
            {
                return;
            }

            ChallengeThreatPresentationResolver.BuildFallbackRoomStatus(
                _observedRoom.State,
                _observedRoom.HasRewardContent,
                out headline,
                out detail,
                out accentColor);
        }

        private void BuildMiniBossRoomStatus(out string headline, out string detail, out Color accentColor)
        {
            switch (_observedRoom.State)
            {
                case RoomState.Combat:
                    headline = "미니보스 전투 중";
                    detail = "작은 보스전처럼 공간부터 확보하세요";
                    accentColor = new Color(0.96f, 0.46f, 0.82f, 1f);
                    return;
                case RoomState.Rewarded when _observedRoom.HasRewardContent:
                    headline = "미니보스 처치 완료";
                    detail = "이동 전에 엘리트 보상을 챙기세요";
                    accentColor = new Color(1f, 0.82f, 0.38f, 1f);
                    return;
                default:
                    headline = "미니보스방";
                    detail = "일반 웨이브 대신 엘리트 전투가 나옵니다";
                    accentColor = new Color(0.88f, 0.54f, 1f, 1f);
                    return;
            }
        }

        private void BuildTrapRoomStatus(out string headline, out string detail, out Color accentColor)
        {
            switch (_observedRoom.State)
            {
                case RoomState.Rewarded when _observedRoom.HasRewardContent:
                    headline = "함정방 보상";
                    detail = "보상을 챙기되 바닥 함정을 끝까지 보세요";
                    accentColor = new Color(1f, 0.78f, 0.3f, 1f);
                    return;
                case RoomState.Rewarded:
                    headline = "함정 활성화";
                    detail = "방은 열렸지만 욕심낸 경로는 아직 위험합니다";
                    accentColor = new Color(1f, 0.46f, 0.34f, 1f);
                    return;
                default:
                    headline = "함정방";
                    detail = "바닥을 읽고 안전한 길로 이동하세요";
                    accentColor = new Color(1f, 0.54f, 0.3f, 1f);
                    return;
            }
        }

        private void BuildCurseRoomStatus(out string headline, out string detail, out Color accentColor)
        {
            switch (_observedRoom.State)
            {
                case RoomState.Rewarded when _observedRoom.HasRewardContent:
                    headline = "저주방 보상";
                    detail = "위험을 감수한 뒤 특수 보상이 열렸습니다";
                    accentColor = new Color(0.96f, 0.54f, 0.74f, 1f);
                    return;
                case RoomState.Rewarded:
                    headline = "저주방 개방";
                    detail = "방은 열렸지만 내부 상호작용을 확인하세요";
                    accentColor = new Color(0.9f, 0.42f, 0.66f, 1f);
                    return;
                default:
                    headline = "저주방";
                    detail = "위험을 감수하고 추가 보상을 노리는 방입니다";
                    accentColor = new Color(0.84f, 0.34f, 0.66f, 1f);
                    return;
            }
        }

        private void BuildNormalRoomStatus(out string headline, out string detail, out Color accentColor)
        {
            switch (_observedRoom.State)
            {
                case RoomState.Combat:
                    headline = "적 전투 진행 중";
                    detail = "공간을 먼저 확보한 뒤 적을 압박하세요";
                    accentColor = new Color(1f, 0.48f, 0.38f, 1f);
                    return;
                case RoomState.Rewarded when _observedRoom.HasRewardContent:
                    headline = "보상 드랍 완료";
                    detail = "다음 방으로 가기 전에 보상을 챙길 수 있습니다";
                    accentColor = new Color(1f, 0.82f, 0.32f, 1f);
                    return;
                case RoomState.Rewarded:
                    headline = "방 클리어";
                    detail = "문이 열렸고 전투 압박이 사라졌습니다";
                    accentColor = new Color(0.52f, 1f, 0.72f, 1f);
                    return;
                case RoomState.Entered:
                    headline = "새 방 진입";
                    detail = "적 배치와 도주 경로를 먼저 확인하세요";
                    accentColor = new Color(0.72f, 0.86f, 1f, 1f);
                    return;
                default:
                    headline = "탐색 중";
                    detail = "서두르지 말고 방 구조를 먼저 읽으세요";
                    accentColor = new Color(0.86f, 0.92f, 1f, 1f);
                    return;
            }
        }
    }
}
