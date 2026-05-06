using CuteIssac.Core.Feedback;
using CuteIssac.Core.Gameplay;
using CuteIssac.Data.Dungeon;
using CuteIssac.Dungeon;
using CuteIssac.Enemy;
using CuteIssac.Player;
using System.Collections.Generic;
using UnityEngine;

namespace CuteIssac.Room
{
    /// <summary>
    /// Directs the player toward the next traversal target after a room is fully secured.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RoomTraversalGuidanceController : MonoBehaviour
    {
        [SerializeField] private RoomNavigationController roomNavigationController;
        [SerializeField] private PlayerController playerController;
        [SerializeField] private PlayerMovement playerMovement;
        [SerializeField] private PlayerHealth playerHealth;
        [SerializeField] private PlayerInventory playerInventory;
        [SerializeField] private PlayerStats playerStats;
        [SerializeField] private PlayerCombat playerCombat;
        [SerializeField] private PlayerActiveItemController playerActiveItemController;
        [SerializeField] private PlayerTrinketHolder playerTrinketHolder;
        [SerializeField] private PlayerConsumableHolder playerConsumableHolder;
        [SerializeField] [Min(0.25f)] private float doorGuidanceDuration = 3.2f;
        [SerializeField] [Min(0f)] private float guidanceRepeatCooldown = 0.8f;
        [SerializeField] [Min(0.05f)] private float traversalFlowRefreshInterval = 0.12f;
        [SerializeField] [Min(0f)] private float traversalMoveBiasWeight = 7.5f;
        [SerializeField] [Min(0f)] private float traversalAimBiasWeight = 4.25f;
        [SerializeField] [Min(0.5f)] private float traversalNearDoorRadius = 4.6f;
        [SerializeField] [Min(0.05f)] private float traversalAimFreshnessWindow = 0.35f;
        [SerializeField] [Min(0.75f)] private float traversalDoorRiskScanRadius = 2.35f;
        [SerializeField] [Min(0f)] private float traversalDoorClearLaneBonus = 2.8f;
        [SerializeField] [Min(1f)] private float recentTraceBiasDuration = 12f;
        [SerializeField] [Min(1f)] private float recentChoiceIntentDuration = 10f;
        [SerializeField] [Min(0.25f)] private float choiceRouteFeedbackWindow = 1.6f;

        public event System.Action GuidanceStatusChanged;

        private readonly struct RouteNeedContext
        {
            public RouteNeedContext(
                float healthRatio,
                bool lowHealth,
                bool needsPower,
                bool cashRich,
                bool needsKeys,
                bool needsBombs,
                bool needsUtility,
                bool readyForBoss)
            {
                HealthRatio = healthRatio;
                LowHealth = lowHealth;
                NeedsPower = needsPower;
                CashRich = cashRich;
                NeedsKeys = needsKeys;
                NeedsBombs = needsBombs;
                NeedsUtility = needsUtility;
                ReadyForBoss = readyForBoss;
            }

            public float HealthRatio { get; }
            public bool LowHealth { get; }
            public bool NeedsPower { get; }
            public bool CashRich { get; }
            public bool NeedsKeys { get; }
            public bool NeedsBombs { get; }
            public bool NeedsUtility { get; }
            public bool ReadyForBoss { get; }
        }

        private readonly struct DoorGuidanceDecision
        {
            public DoorGuidanceDecision(RoomDoor door, float score, RoomType roomType, Color accentColor, string reasonTag)
            {
                Door = door;
                Score = score;
                RoomType = roomType;
                AccentColor = accentColor;
                ReasonTag = reasonTag ?? string.Empty;
            }

            public RoomDoor Door { get; }
            public float Score { get; }
            public RoomType RoomType { get; }
            public Color AccentColor { get; }
            public string ReasonTag { get; }
            public bool IsValid => Door != null;
        }

        private struct RecentRouteTraceBias
        {
            public RoomController Room;
            public float ExpiresAt;
            public float PowerWeight;
            public float CashWeight;
            public float KeyWeight;
            public float BombWeight;
            public float RecoveryWeight;
            public Color AccentColor;
        }

        private struct RecentChoiceIntentBias
        {
            public RoomController Room;
            public float ExpiresAt;
            public string ReasonTag;
            public string CompareLabel;
            public float Weight;
            public Color AccentColor;
            public bool HeldRoute;
        }

        private struct PendingChoiceRouteFeedback
        {
            public RoomController Room;
            public float ExpiresAt;
            public string Headline;
            public Color AccentColor;
            public bool HeldRoute;
            public bool HasCarryClosure;
        }

        private TraversalGuidanceBeacon _activeBeacon;
        private RoomController _activeSourceRoom;
        private RoomDoor _activeDoorTarget;
        private RoomType _activeTargetRoomType = RoomType.Normal;
        private TraversalDoorGuidanceStyle _activeDoorStyle = TraversalDoorGuidanceStyle.Guided;
        private Color _activeAccentColor = Color.white;
        private string _activeReasonTag = string.Empty;
        private string _activeStatusHeadline = string.Empty;
        private string _activeStatusDetail = string.Empty;
        private string _activeStatusCompactTag = string.Empty;
        private string _activeStatusDetailEyebrow = string.Empty;
        private float _lastGuidanceTimestamp = float.NegativeInfinity;
        private float _lastTraversalFlowRefreshTimestamp = float.NegativeInfinity;
        private RecentRouteTraceBias _recentTraceBias;
        private RecentChoiceIntentBias _recentChoiceIntentBias;
        private PendingChoiceRouteFeedback _pendingChoiceRouteFeedback;

        private void Awake()
        {
            ResolveReferences();
        }

        private void Update()
        {
            if (Time.unscaledTime - _lastTraversalFlowRefreshTimestamp < traversalFlowRefreshInterval)
            {
                return;
            }

            _lastTraversalFlowRefreshTimestamp = Time.unscaledTime;
            TryRefreshGuidanceForCurrentRoom();
        }

        private void OnEnable()
        {
            ResolveReferences();
            GameplayRuntimeEvents.RoomRewardPhaseCompleted += HandleRoomRewardPhaseCompleted;
            GameplayRuntimeEvents.RoomRewardCollected += HandleRoomRewardCollected;
            GameplayRuntimeEvents.PlayerLoadoutDelta += HandlePlayerLoadoutDelta;
            GameplayRuntimeEvents.PlayerInteractionOutcome += HandlePlayerInteractionOutcome;
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

            if (playerStats != null)
            {
                playerStats.StatsRecalculated -= HandleStatsRecalculated;
                playerStats.StatsRecalculated += HandleStatsRecalculated;
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

            if (playerConsumableHolder != null)
            {
                playerConsumableHolder.ConsumableStateChanged -= HandleConsumableStateChanged;
                playerConsumableHolder.ConsumableStateChanged += HandleConsumableStateChanged;
            }

            if (roomNavigationController != null)
            {
                roomNavigationController.CurrentRoomChanged -= HandleCurrentRoomChanged;
                roomNavigationController.CurrentRoomChanged += HandleCurrentRoomChanged;
            }
        }

        private void OnDisable()
        {
            GameplayRuntimeEvents.RoomRewardPhaseCompleted -= HandleRoomRewardPhaseCompleted;
            GameplayRuntimeEvents.RoomRewardCollected -= HandleRoomRewardCollected;
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

            if (playerStats != null)
            {
                playerStats.StatsRecalculated -= HandleStatsRecalculated;
            }

            if (playerActiveItemController != null)
            {
                playerActiveItemController.ActiveItemStateChanged -= HandleActiveItemStateChanged;
            }

            if (playerTrinketHolder != null)
            {
                playerTrinketHolder.TrinketChanged -= HandleTrinketChanged;
            }

            if (playerConsumableHolder != null)
            {
                playerConsumableHolder.ConsumableStateChanged -= HandleConsumableStateChanged;
            }

            if (roomNavigationController != null)
            {
                roomNavigationController.CurrentRoomChanged -= HandleCurrentRoomChanged;
            }

            ClearActiveGuidance();
            ClearRecentRouteTraceBias();
            ClearRecentChoiceIntentBias();
        }

        public void ConfigureRuntime(RoomNavigationController navigationController, PlayerController runtimePlayerController = null)
        {
            if (roomNavigationController != null && roomNavigationController != navigationController)
            {
                roomNavigationController.CurrentRoomChanged -= HandleCurrentRoomChanged;
            }

            roomNavigationController = navigationController;

            if (runtimePlayerController != null)
            {
                playerController = runtimePlayerController;
            }

            if (isActiveAndEnabled && roomNavigationController != null)
            {
                roomNavigationController.CurrentRoomChanged -= HandleCurrentRoomChanged;
                roomNavigationController.CurrentRoomChanged += HandleCurrentRoomChanged;
            }
        }

        public bool TryResolveDoorTraversalGuide(
            RoomDoor door,
            out Color accentColor,
            out RoomType targetRoomType,
            out TraversalDoorGuidanceStyle guidanceStyle)
        {
            if (door != null && door == _activeDoorTarget)
            {
                accentColor = _activeAccentColor;
                targetRoomType = _activeTargetRoomType;
                guidanceStyle = _activeDoorStyle;
                return true;
            }

            accentColor = door != null && door.ConnectedRoom != null
                ? ResolveRoomAccent(door.ConnectedRoom.RoomType)
                : ResolveRoomAccent(RoomType.Normal);
            targetRoomType = door != null && door.ConnectedRoom != null
                ? door.ConnectedRoom.RoomType
                : RoomType.Normal;
            guidanceStyle = ResolveFallbackDoorStyle(door);
            return false;
        }

        public bool TryGetActiveGuidanceStatus(
            RoomController room,
            out RoomType targetRoomType,
            out Color accentColor,
            out string headline,
            out string detail,
            out string badgeLabel,
            out string eyebrow,
            out string compactTag,
            out string detailEyebrow)
        {
            targetRoomType = RoomType.Normal;
            accentColor = Color.white;
            headline = string.Empty;
            detail = string.Empty;
            badgeLabel = "ROUTE";
            eyebrow = "GUIDED EXIT";
            compactTag = string.Empty;
            detailEyebrow = string.Empty;

            if (_activeSourceRoom == null
                || room == null
                || room != _activeSourceRoom)
            {
                return false;
            }

            targetRoomType = _activeTargetRoomType;
            accentColor = _activeAccentColor;
            headline = _activeStatusHeadline;
            detail = _activeStatusDetail;
            if (TryGetActiveChoicePlanCarry(room, out _, out bool heldRoute, out string compareLabel))
            {
                badgeLabel = "PLAN";
                eyebrow = heldRoute ? "PLAN HELD" : "REROUTE LIVE";

                if (IsShotProfileCompareLabel(compareLabel))
                {
                    compactTag = compareLabel;
                    detailEyebrow = compareLabel;
                    detail = heldRoute
                        ? $"PLAN HELD / {compareLabel}"
                        : $"REROUTE / {compareLabel}";
                }
            }

            if (string.IsNullOrWhiteSpace(compactTag))
            {
                compactTag = _activeStatusCompactTag;
            }

            if (string.IsNullOrWhiteSpace(detailEyebrow))
            {
                detailEyebrow = _activeStatusDetailEyebrow;
            }

            return !string.IsNullOrWhiteSpace(headline);
        }

        public bool TryGetActiveChoicePlanCarry(RoomController room, out string reasonTag, out bool heldRoute, out string compareLabel)
        {
            reasonTag = string.Empty;
            heldRoute = false;
            compareLabel = string.Empty;

            if (_activeSourceRoom == null
                || room == null
                || room != _activeSourceRoom
                || _recentChoiceIntentBias.Room != room
                || _recentChoiceIntentBias.ExpiresAt <= Time.unscaledTime
                || string.IsNullOrWhiteSpace(_recentChoiceIntentBias.ReasonTag))
            {
                return false;
            }

            if (!string.Equals(_activeReasonTag, _recentChoiceIntentBias.ReasonTag, System.StringComparison.Ordinal))
            {
                return false;
            }

            reasonTag = _recentChoiceIntentBias.ReasonTag;
            heldRoute = _recentChoiceIntentBias.HeldRoute;
            compareLabel = _recentChoiceIntentBias.CompareLabel ?? string.Empty;
            return true;
        }

        private void HandleRoomRewardPhaseCompleted(RoomRewardPhaseSignal signal)
        {
            if (!signal.HasRewards)
            {
                TryPresentTraversalGuidance(signal.Room, allowFeedbackBurst: true);
            }
        }

        private void HandleRoomRewardCollected(RoomRewardCollectedSignal signal)
        {
            if (signal.IsValid && signal.IsFinalRewardCollection)
            {
                TryPresentTraversalGuidance(signal.Room, allowFeedbackBurst: true);
            }
        }

        private void HandlePlayerLoadoutDelta(PlayerLoadoutDeltaSignal signal)
        {
            if (!signal.IsValid)
            {
                return;
            }

            if (TryAccumulateRecentRouteTraceBias(signal.Headline, signal.Detail, signal.AccentColor, signal.Emphasize, treatAsLoadoutSpike: true))
            {
                TryRefreshGuidanceForCurrentRoom();
            }
        }

        private void HandlePlayerInteractionOutcome(PlayerInteractionOutcomeSignal signal)
        {
            if (!signal.IsValid)
            {
                return;
            }

            if (TryAccumulateRecentRouteTraceBias(signal.Headline, signal.Detail, signal.AccentColor, signal.Emphasize, treatAsLoadoutSpike: false))
            {
                TryRefreshGuidanceForCurrentRoom();
            }
        }

        private void HandleChoiceRouteResolved(ChoiceRouteResolvedSignal signal)
        {
            if (!signal.IsValid)
            {
                return;
            }

            if (TryAccumulateRecentChoiceIntentBias(signal))
            {
                PrimeChoiceRouteFeedback(signal);
                TryRefreshGuidanceForCurrentRoom();
            }
        }

        private void HandleHealthChanged(float _, float __)
        {
            TryRefreshGuidanceForCurrentRoom();
        }

        private void HandleResourcesChanged(PlayerResourceSnapshot _)
        {
            TryRefreshGuidanceForCurrentRoom();
        }

        private void HandleInventoryChanged()
        {
            TryRefreshGuidanceForCurrentRoom();
        }

        private void HandleStatsRecalculated(PlayerStatSnapshot _)
        {
            TryRefreshGuidanceForCurrentRoom();
        }

        private void HandleActiveItemStateChanged(PlayerActiveItemSlotState _)
        {
            TryRefreshGuidanceForCurrentRoom();
        }

        private void HandleTrinketChanged()
        {
            TryRefreshGuidanceForCurrentRoom();
        }

        private void HandleConsumableStateChanged(PlayerConsumableSlotState _)
        {
            TryRefreshGuidanceForCurrentRoom();
        }

        private void HandleCurrentRoomChanged(RoomController _)
        {
            ClearActiveGuidance();
            ClearRecentRouteTraceBias();
            ClearRecentChoiceIntentBias();
            ClearPendingChoiceRouteFeedback();
        }

        private void TryRefreshGuidanceForCurrentRoom()
        {
            RoomController room = roomNavigationController != null
                ? roomNavigationController.CurrentRoom
                : _activeSourceRoom;

            if (room == null || room.State != RoomState.Rewarded)
            {
                return;
            }

            if (room.HasRewardContent && _activeSourceRoom != room)
            {
                return;
            }

            TryPresentTraversalGuidance(room, allowFeedbackBurst: false);
        }

        private void TryPresentTraversalGuidance(RoomController room, bool allowFeedbackBurst)
        {
            ResolveReferences();

            if (room == null
                || roomNavigationController == null
                || roomNavigationController.CurrentRoom != room)
            {
                return;
            }

            bool canBurstFeedback = allowFeedbackBurst
                && Time.unscaledTime - _lastGuidanceTimestamp >= guidanceRepeatCooldown;
            Vector2 playerPosition = playerController != null
                ? playerController.transform.position
                : room.CameraFocusPosition;

            FloorExit floorExit = room.GetComponentInChildren<FloorExit>(true);
            if (floorExit != null)
            {
                HideRoomDoorBeacons(room);
                Color portalAccent = ResolveRoomAccent(RoomType.Boss);
                TraversalGuidanceBeacon portalBeacon = EnsureBeacon(floorExit.gameObject);
                portalBeacon.ShowPortalGuidance(portalAccent, 0f);
                bool changed = _activeSourceRoom != room
                    || _activeDoorTarget != null
                    || _activeTargetRoomType != RoomType.Boss
                    || _activeReasonTag != "DESCEND";
                if (changed && canBurstFeedback)
                {
                    RaiseGuidanceLabel(
                        floorExit.transform.position + Vector3.up * 1.1f,
                        "DESCEND",
                        portalAccent);
                    _lastGuidanceTimestamp = Time.unscaledTime;
                }

                _activeSourceRoom = room;
                _activeBeacon = portalBeacon;
                _activeDoorTarget = null;
                _activeTargetRoomType = RoomType.Boss;
                _activeDoorStyle = TraversalDoorGuidanceStyle.Portal;
                _activeAccentColor = portalAccent;
                _activeReasonTag = "DESCEND";
                _activeStatusHeadline = "Exit Route Ready";
                _activeStatusDetail = "The floor exit is open. Descend once you finish sweeping the room.";
                _activeStatusCompactTag = "EXIT";
                _activeStatusDetailEyebrow = "DESCEND";
                if (changed)
                {
                    RaiseGuidanceStatusChanged();
                }
                return;
            }

            DoorGuidanceDecision decision = ResolvePreferredDoor(room);
            if (!decision.IsValid)
            {
                HideRoomDoorBeacons(room);
                return;
            }

            RoomDoor nextDoor = decision.Door;
            TraversalGuidanceBeacon beacon = EnsureBeacon(nextDoor.gameObject);
            TraversalDoorGuidanceStyle guidedDoorStyle = ApplyDoorBeaconPresentation(room, nextDoor, decision.AccentColor, decision.ReasonTag, playerPosition);
            TryGetActiveChoicePlanCarry(room, out _, out _, out string choicePlanCompareLabel);
            BuildGuidanceStatusCopy(nextDoor, decision.RoomType, decision.ReasonTag, choicePlanCompareLabel, out string statusHeadline, out string statusDetail, out string statusCompactTag, out string statusDetailEyebrow);
            bool hasPendingChoiceFeedback = TryGetPendingChoiceRouteFeedback(room, out PendingChoiceRouteFeedback pendingChoiceFeedback);
            bool routeChanged = _activeSourceRoom != room
                || _activeDoorTarget != nextDoor
                || _activeTargetRoomType != decision.RoomType
                || _activeDoorStyle != guidedDoorStyle
                || _activeReasonTag != decision.ReasonTag
                || _activeStatusHeadline != statusHeadline
                || _activeStatusDetail != statusDetail
                || _activeStatusCompactTag != statusCompactTag
                || _activeStatusDetailEyebrow != statusDetailEyebrow;

            if (hasPendingChoiceFeedback)
            {
                PlayChoiceRouteFeedbackBurst(nextDoor, beacon, pendingChoiceFeedback);
                RaiseGuidanceLabel(
                    nextDoor.transform.position + Vector3.up * 1.12f,
                    ResolveChoiceRouteBurstLabel(nextDoor, pendingChoiceFeedback),
                    pendingChoiceFeedback.AccentColor);
                _lastGuidanceTimestamp = Time.unscaledTime;
                ClearPendingChoiceRouteFeedback();
            }
            else if (routeChanged && canBurstFeedback)
            {
                RaiseGuidanceLabel(
                    nextDoor.transform.position + Vector3.up * 1.12f,
                    ResolveDoorGuidanceLabel(nextDoor, decision.ReasonTag, choicePlanCompareLabel),
                    decision.AccentColor);
                _lastGuidanceTimestamp = Time.unscaledTime;
            }

            _activeSourceRoom = room;
            _activeBeacon = beacon;
            _activeDoorTarget = nextDoor;
            _activeTargetRoomType = decision.RoomType;
            _activeDoorStyle = guidedDoorStyle;
            _activeAccentColor = decision.AccentColor;
            _activeReasonTag = decision.ReasonTag;
            _activeStatusHeadline = statusHeadline;
            _activeStatusDetail = statusDetail;
            _activeStatusCompactTag = statusCompactTag;
            _activeStatusDetailEyebrow = statusDetailEyebrow;
            if (routeChanged)
            {
                RaiseGuidanceStatusChanged();
            }
        }

        private DoorGuidanceDecision ResolvePreferredDoor(RoomController room)
        {
            if (room == null)
            {
                return default;
            }

            DoorGuidanceDecision bestDecision = default;
            float bestScore = float.NegativeInfinity;
            Vector3 playerPosition = playerController != null ? playerController.transform.position : room.CameraFocusPosition;
            Vector2 moveDirection = ResolveTraversalMoveDirection();
            bool hasMoveDirection = moveDirection.sqrMagnitude > 0.0001f;
            bool hasAimDirection = TryResolveTraversalAimDirection(out Vector2 aimDirection);
            IReadOnlyList<RoomDoor> roomDoors = room.RoomDoors;
            RouteNeedContext needContext = BuildRouteNeedContext();

            for (int i = 0; i < roomDoors.Count; i++)
            {
                RoomDoor door = roomDoors[i];
                if (door == null || door.IsLocked || door.ConnectedRoom == null || !CanAffordGuidedDoor(door))
                {
                    continue;
                }

                RoomController targetRoom = door.ConnectedRoom;
                float score = targetRoom.HasResolvedRoom ? 12f : 100f;
                score += ResolveRoomPriority(targetRoom.RoomType);
                score += Mathf.Clamp(5.5f - Vector2.Distance(playerPosition, door.transform.position), -6f, 6f);
                score += ResolveAdaptiveDoorPriority(door, targetRoom, needContext, out string strategicReasonTag);
                float traceBias = ResolveRecentTraceDoorPriority(room, targetRoom, needContext, out string traceReasonTag);
                float intentBias = ResolveChoiceIntentDoorPriority(room, targetRoom, needContext, out string intentReasonTag);
                float flowBias = ResolveTraversalFlowPriority(
                    door,
                    playerPosition,
                    moveDirection,
                    hasMoveDirection,
                    aimDirection,
                    hasAimDirection,
                    out string flowReasonTag);
                float riskBias = ResolveTraversalDoorRiskPriority(room, door, playerPosition, out string riskReasonTag);
                score += traceBias;
                score += intentBias;
                score += flowBias;
                score += riskBias;
                string reasonTag = ResolveDoorReasonTag(strategicReasonTag, flowReasonTag, flowBias, riskReasonTag, riskBias, intentReasonTag, intentBias, traceReasonTag, traceBias);
                Color accentColor = ResolveRouteAccent(targetRoom.RoomType, traceBias, intentBias);

                if (score > bestScore)
                {
                    bestScore = score;
                    bestDecision = new DoorGuidanceDecision(
                        door,
                        score,
                        targetRoom.RoomType,
                        accentColor,
                        reasonTag);
                }
            }

            return bestDecision;
        }

        private Vector2 ResolveTraversalMoveDirection()
        {
            if (playerMovement == null)
            {
                return Vector2.zero;
            }

            Vector2 velocity = playerMovement.CurrentVelocity;
            if (velocity.sqrMagnitude > 0.04f)
            {
                return velocity.normalized;
            }

            Vector2 moveInput = playerMovement.MoveInput;
            return moveInput.sqrMagnitude > 0.0001f
                ? moveInput.normalized
                : Vector2.zero;
        }

        private bool TryResolveTraversalAimDirection(out Vector2 aimDirection)
        {
            aimDirection = Vector2.zero;

            if (playerCombat == null
                || !playerCombat.TryGetRecentAimDirection(traversalAimFreshnessWindow, out Vector2 recentAimDirection))
            {
                return false;
            }

            aimDirection = recentAimDirection.normalized;
            return aimDirection.sqrMagnitude > 0.0001f;
        }

        private float ResolveTraversalFlowPriority(
            RoomDoor door,
            Vector2 playerPosition,
            Vector2 moveDirection,
            bool hasMoveDirection,
            Vector2 aimDirection,
            bool hasAimDirection,
            out string flowReasonTag)
        {
            flowReasonTag = string.Empty;

            if (door == null)
            {
                return 0f;
            }

            Vector2 toDoor = (Vector2)door.transform.position - playerPosition;
            float distance = toDoor.magnitude;

            if (distance <= 0.0001f)
            {
                return 0f;
            }

            Vector2 doorDirection = toDoor / distance;
            float nearFactor = Mathf.InverseLerp(traversalNearDoorRadius * 2.1f, traversalNearDoorRadius, distance);
            float totalBias = 0f;
            float strongestFlowWeight = float.NegativeInfinity;
            string bestFlowReasonTag = string.Empty;

            void RegisterFlowReason(float weight, string tag)
            {
                if (string.IsNullOrWhiteSpace(tag) || weight <= strongestFlowWeight)
                {
                    return;
                }

                strongestFlowWeight = weight;
                bestFlowReasonTag = tag;
            }

            if (hasMoveDirection)
            {
                float moveAlignment = Vector2.Dot(moveDirection, doorDirection);
                float moveBias = Mathf.Lerp(-traversalMoveBiasWeight * 0.42f, traversalMoveBiasWeight, Mathf.InverseLerp(-1f, 1f, moveAlignment));
                moveBias *= Mathf.Lerp(0.35f, 1f, nearFactor);
                totalBias += moveBias;

                if (moveAlignment >= 0.9f && distance <= traversalNearDoorRadius * 1.05f)
                {
                    RegisterFlowReason(moveBias + 1.25f, "ON PATH");
                }
                else if (moveAlignment >= 0.65f && distance <= traversalNearDoorRadius * 1.5f)
                {
                    RegisterFlowReason(moveBias, "CLEAN ENTRY");
                }
            }

            if (hasAimDirection)
            {
                float aimAlignment = Vector2.Dot(aimDirection, doorDirection);
                float aimBias = Mathf.Lerp(-traversalAimBiasWeight * 0.24f, traversalAimBiasWeight, Mathf.InverseLerp(-1f, 1f, aimAlignment));
                aimBias *= Mathf.Lerp(0.22f, 1f, nearFactor);
                totalBias += aimBias;

                if (aimAlignment >= 0.92f && distance <= traversalNearDoorRadius * 1.25f)
                {
                    RegisterFlowReason(aimBias + 0.9f, "AIM LINE");
                }
            }

            flowReasonTag = bestFlowReasonTag;
            return totalBias;
        }

        private float ResolveTraversalDoorRiskPriority(
            RoomController room,
            RoomDoor door,
            Vector2 playerPosition,
            out string riskReasonTag)
        {
            riskReasonTag = string.Empty;

            if (room == null || door == null)
            {
                return 0f;
            }

            Vector2 doorPosition = door.transform.position;
            float contactRisk = 0f;
            float congestionRisk = 0f;
            float strongestRiskWeight = float.NegativeInfinity;
            string bestRiskReasonTag = string.Empty;

            void RegisterRiskReason(float weight, string tag)
            {
                if (string.IsNullOrWhiteSpace(tag) || weight <= strongestRiskWeight)
                {
                    return;
                }

                strongestRiskWeight = weight;
                bestRiskReasonTag = tag;
            }

            RoomObstacleController[] obstacles = room.GetComponentsInChildren<RoomObstacleController>(true);
            for (int i = 0; i < obstacles.Length; i++)
            {
                RoomObstacleController obstacle = obstacles[i];
                Collider2D obstacleCollider = obstacle != null ? obstacle.ObstacleCollider : null;

                if (obstacle == null || obstacleCollider == null)
                {
                    continue;
                }

                Vector2 closestPoint = obstacleCollider.ClosestPoint(doorPosition);
                float obstacleRadius = Mathf.Max(0.36f, obstacle.TraversalRiskRadius);
                float distance = Vector2.Distance(closestPoint, doorPosition);

                if (distance > traversalDoorRiskScanRadius + obstacleRadius)
                {
                    continue;
                }

                float proximity = 1f - Mathf.Clamp01((distance - obstacleRadius) / traversalDoorRiskScanRadius);
                float severity = obstacle.ObstacleType switch
                {
                    RoomObstacleType.Spike => 6.8f,
                    RoomObstacleType.Pit => 3.9f,
                    RoomObstacleType.Web => 4.4f,
                    RoomObstacleType.Fountain => 3.3f,
                    RoomObstacleType.Rock => 2.4f,
                    _ => 1.8f
                };

                if (obstacle.DamagesPlayerOnContact)
                {
                    severity += Mathf.Max(1.4f, obstacle.ContactDamage * 2.2f);
                    float weightedRisk = proximity * severity;
                    contactRisk += weightedRisk;
                    RegisterRiskReason(
                        weightedRisk,
                        obstacle.ObstacleType == RoomObstacleType.Spike
                            ? "SPIKE EDGE"
                            : obstacle.ObstacleType == RoomObstacleType.Fountain
                                ? "SPLASH ZONE"
                                : "WATCH STEP");
                }
                else
                {
                    float weightedRisk = proximity * severity * 0.78f;
                    congestionRisk += weightedRisk;
                    RegisterRiskReason(
                        weightedRisk,
                        obstacle.ObstacleType == RoomObstacleType.Web
                            ? "WEB STRAND"
                            : obstacle.ObstacleType == RoomObstacleType.Fountain
                                ? "SPLASH ZONE"
                                : "TIGHT DOOR");
                }
            }

            EnemyMineController[] mines = room.GetComponentsInChildren<EnemyMineController>(true);
            for (int i = 0; i < mines.Length; i++)
            {
                EnemyMineController mine = mines[i];

                if (mine == null || !mine.IsThreatActive)
                {
                    continue;
                }

                float riskRadius = Mathf.Max(0.45f, mine.TraversalRiskRadius);
                float distance = Vector2.Distance(mine.transform.position, doorPosition);

                if (distance > traversalDoorRiskScanRadius + riskRadius)
                {
                    continue;
                }

                float proximity = 1f - Mathf.Clamp01((distance - riskRadius) / traversalDoorRiskScanRadius);
                float weightedRisk = proximity * 8.4f * mine.TraversalThreatWeight;
                contactRisk += weightedRisk;
                RegisterRiskReason(weightedRisk, "WATCH MINE");
            }

            float totalRisk = contactRisk + congestionRisk;
            float playerToDoorDistance = Vector2.Distance(playerPosition, doorPosition);

            if (totalRisk <= 0.45f && playerToDoorDistance <= traversalNearDoorRadius * 1.3f)
            {
                riskReasonTag = "CLEAR LANE";
                return traversalDoorClearLaneBonus;
            }

            if (string.IsNullOrWhiteSpace(bestRiskReasonTag))
            {
                if (contactRisk >= 3.2f)
                {
                    bestRiskReasonTag = "WATCH STEP";
                }
                else if (congestionRisk >= 2.4f)
                {
                    bestRiskReasonTag = "TIGHT DOOR";
                }
            }

            riskReasonTag = bestRiskReasonTag;
            return -totalRisk;
        }

        private string ResolveDoorReasonTag(
            string strategicReasonTag,
            string flowReasonTag,
            float flowBias,
            string riskReasonTag,
            float riskBias,
            string intentReasonTag,
            float intentBias,
            string traceReasonTag,
            float traceBias)
        {
            if (!string.IsNullOrWhiteSpace(riskReasonTag) && riskBias <= -7.8f)
            {
                return riskReasonTag;
            }

            if (!string.IsNullOrWhiteSpace(intentReasonTag)
                && (IsGenericStrategicReasonTag(strategicReasonTag) || intentBias >= 7.2f)
                && flowBias < 8.4f
                && (string.IsNullOrWhiteSpace(riskReasonTag) || riskBias > -4.5f))
            {
                return intentReasonTag;
            }

            if (!string.IsNullOrWhiteSpace(traceReasonTag)
                && (IsGenericStrategicReasonTag(strategicReasonTag) || traceBias >= 9.5f)
                && flowBias < 8.2f
                && (string.IsNullOrWhiteSpace(riskReasonTag) || riskBias > -4.5f))
            {
                return traceReasonTag;
            }

            if (string.IsNullOrWhiteSpace(flowReasonTag))
            {
                if (!string.IsNullOrWhiteSpace(riskReasonTag)
                    && (string.IsNullOrWhiteSpace(strategicReasonTag)
                    || strategicReasonTag == "SCOUT"
                    || strategicReasonTag == "SUPPLY RUN"
                    || strategicReasonTag == "PLAY SAFE"
                    || strategicReasonTag == "HOLD LINE"))
                {
                    return riskReasonTag;
                }

                return strategicReasonTag ?? riskReasonTag ?? string.Empty;
            }

            if (string.IsNullOrWhiteSpace(strategicReasonTag))
            {
                if (!string.IsNullOrWhiteSpace(riskReasonTag) && riskBias >= 2f)
                {
                    return riskReasonTag;
                }

                return flowReasonTag;
            }

            if (flowBias >= 8.5f)
            {
                return flowReasonTag;
            }

            return strategicReasonTag switch
            {
                "SCOUT" => !string.IsNullOrWhiteSpace(riskReasonTag) && riskBias >= 2f ? riskReasonTag : flowBias >= 4.2f ? flowReasonTag : strategicReasonTag,
                "SUPPLY RUN" => !string.IsNullOrWhiteSpace(riskReasonTag) && riskBias >= 2f ? riskReasonTag : flowBias >= 4.2f ? flowReasonTag : strategicReasonTag,
                "PLAY SAFE" => !string.IsNullOrWhiteSpace(riskReasonTag) && riskBias >= 2.2f ? riskReasonTag : flowBias >= 5.4f ? flowReasonTag : strategicReasonTag,
                "HOLD LINE" => !string.IsNullOrWhiteSpace(riskReasonTag) && riskBias >= 2.2f ? riskReasonTag : flowBias >= 5.8f ? flowReasonTag : strategicReasonTag,
                _ => !string.IsNullOrWhiteSpace(riskReasonTag) && riskBias <= -6.2f ? riskReasonTag : strategicReasonTag
            };
        }

        private static bool IsGenericStrategicReasonTag(string strategicReasonTag)
        {
            return string.IsNullOrWhiteSpace(strategicReasonTag)
                || strategicReasonTag == "SCOUT"
                || strategicReasonTag == "SUPPLY RUN"
                || strategicReasonTag == "PLAY SAFE"
                || strategicReasonTag == "HOLD LINE"
                || strategicReasonTag == "SAFE UPGRADE"
                || strategicReasonTag == "CACHE HUNT"
                || strategicReasonTag == "LOADOUT FIND";
        }

        private static float ResolveRoomPriority(RoomType roomType)
        {
            return roomType switch
            {
                RoomType.Boss => 44f,
                RoomType.Treasure => 38f,
                RoomType.Shop => 34f,
                RoomType.Secret => 30f,
                RoomType.MiniBoss => 28f,
                RoomType.Challenge => 24f,
                RoomType.Curse => 20f,
                RoomType.Trap => 14f,
                RoomType.Normal => 12f,
                _ => 0f
            };
        }

        public static Color ResolveRoomAccent(RoomType roomType)
        {
            return roomType switch
            {
                RoomType.Start => new Color(0.72f, 0.94f, 1f, 1f),
                RoomType.Treasure => new Color(1f, 0.84f, 0.28f, 1f),
                RoomType.Shop => new Color(0.36f, 0.95f, 0.84f, 1f),
                RoomType.Boss => new Color(1f, 0.4f, 0.34f, 1f),
                RoomType.Secret => new Color(0.84f, 0.58f, 1f, 1f),
                RoomType.Challenge => new Color(1f, 0.64f, 0.24f, 1f),
                RoomType.Curse => new Color(0.86f, 0.36f, 0.68f, 1f),
                RoomType.MiniBoss => new Color(0.96f, 0.52f, 0.82f, 1f),
                RoomType.Trap => new Color(1f, 0.48f, 0.34f, 1f),
                _ => new Color(0.66f, 0.92f, 1f, 1f)
            };
        }

        private bool CanAffordGuidedDoor(RoomDoor door)
        {
            if (door == null || !door.HasUnpaidHealthEntryCost || playerHealth == null)
            {
                return true;
            }

            return playerHealth.CurrentHealth > door.RequiredHealthToEnter;
        }

        private RouteNeedContext BuildRouteNeedContext()
        {
            float maxHealth = playerHealth != null ? Mathf.Max(1f, playerHealth.MaxHealth) : 6f;
            float currentHealth = playerHealth != null ? playerHealth.CurrentHealth : maxHealth;
            float healthRatio = Mathf.Clamp01(currentHealth / maxHealth);
            int passiveItemCount = playerInventory != null && playerInventory.PassiveItems != null
                ? playerInventory.PassiveItems.Count
                : 0;
            float currentDamage = playerStats != null ? playerStats.CurrentDamage : 0f;
            bool lowHealth = healthRatio <= 0.45f;
            bool needsPower = currentDamage < 5.25f || passiveItemCount < 3;
            bool cashRich = playerInventory != null && playerInventory.Coins >= 10;
            bool needsKeys = playerInventory != null && playerInventory.Keys <= 0;
            bool needsBombs = playerInventory != null && playerInventory.Bombs <= 0;
            bool needsUtility = (playerActiveItemController == null || !playerActiveItemController.HasEquippedItem)
                || (playerTrinketHolder == null || !playerTrinketHolder.HasTrinket)
                || (playerConsumableHolder == null || playerConsumableHolder.HeldConsumable == null);
            bool readyForBoss = healthRatio >= 0.72f && currentDamage >= 6f && passiveItemCount >= 3;

            return new RouteNeedContext(
                healthRatio,
                lowHealth,
                needsPower,
                cashRich,
                needsKeys,
                needsBombs,
                needsUtility,
                readyForBoss);
        }

        private float ResolveRecentTraceDoorPriority(
            RoomController sourceRoom,
            RoomController targetRoom,
            RouteNeedContext context,
            out string traceReasonTag)
        {
            traceReasonTag = string.Empty;

            if (sourceRoom == null
                || targetRoom == null
                || _recentTraceBias.Room != sourceRoom
                || _recentTraceBias.ExpiresAt <= Time.unscaledTime)
            {
                return 0f;
            }

            float totalBonus = 0f;
            float strongestTraceWeight = float.NegativeInfinity;
            string bestTraceReasonTag = string.Empty;

            void AddTraceBias(float bonus, string tag)
            {
                if (bonus <= 0f)
                {
                    return;
                }

                totalBonus += bonus;

                if (!string.IsNullOrWhiteSpace(tag) && bonus > strongestTraceWeight)
                {
                    strongestTraceWeight = bonus;
                    bestTraceReasonTag = tag;
                }
            }

            switch (targetRoom.RoomType)
            {
                case RoomType.Boss:
                    if (_recentTraceBias.PowerWeight > 0f && !context.LowHealth)
                    {
                        AddTraceBias(4f + (_recentTraceBias.PowerWeight * 5.2f), "PRESS ADVANTAGE");
                    }

                    if (_recentTraceBias.RecoveryWeight > 0f && !context.LowHealth)
                    {
                        AddTraceBias(2.5f + (_recentTraceBias.RecoveryWeight * 3.8f), "RECOVERY ONLINE");
                    }
                    break;
                case RoomType.MiniBoss:
                case RoomType.Challenge:
                    if (_recentTraceBias.PowerWeight > 0f && !context.LowHealth)
                    {
                        AddTraceBias(3f + (_recentTraceBias.PowerWeight * 4.1f), "PRESS ADVANTAGE");
                    }

                    if (_recentTraceBias.RecoveryWeight > 0f && !context.LowHealth)
                    {
                        AddTraceBias(2f + (_recentTraceBias.RecoveryWeight * 2.7f), "RECOVERY ONLINE");
                    }
                    break;
                case RoomType.Shop:
                    if (_recentTraceBias.CashWeight > 0f)
                    {
                        AddTraceBias(4f + (_recentTraceBias.CashWeight * 4.6f), "CASH WINDOW");
                    }
                    break;
                case RoomType.Treasure:
                    if (_recentTraceBias.KeyWeight > 0f)
                    {
                        AddTraceBias(4f + (_recentTraceBias.KeyWeight * 4.8f), "KEY WINDOW");
                    }
                    break;
                case RoomType.Secret:
                    if (_recentTraceBias.BombWeight > 0f)
                    {
                        AddTraceBias(4f + (_recentTraceBias.BombWeight * 5f), "BOMB LINE");
                    }
                    break;
            }

            traceReasonTag = bestTraceReasonTag;
            return totalBonus;
        }

        private float ResolveChoiceIntentDoorPriority(
            RoomController sourceRoom,
            RoomController targetRoom,
            RouteNeedContext context,
            out string intentReasonTag)
        {
            intentReasonTag = string.Empty;

            if (sourceRoom == null
                || targetRoom == null
                || _recentChoiceIntentBias.Room != sourceRoom
                || _recentChoiceIntentBias.ExpiresAt <= Time.unscaledTime
                || string.IsNullOrWhiteSpace(_recentChoiceIntentBias.ReasonTag))
            {
                return 0f;
            }

            float weight = Mathf.Clamp01(_recentChoiceIntentBias.Weight);
            float bonus = 0f;

            switch (_recentChoiceIntentBias.ReasonTag)
            {
                case "PRESS ADVANTAGE":
                    bonus = targetRoom.RoomType switch
                    {
                        RoomType.Boss when !context.LowHealth => 4.8f + (weight * 6.2f),
                        RoomType.MiniBoss when !context.LowHealth => 4f + (weight * 4.8f),
                        RoomType.Challenge when !context.LowHealth => 3.6f + (weight * 4.2f),
                        RoomType.Normal when !context.LowHealth => 1.2f + (weight * 1.6f),
                        _ => 0f
                    };
                    break;
                case "RECOVERY ONLINE":
                    bonus = targetRoom.RoomType switch
                    {
                        RoomType.Boss when !context.LowHealth => 3.8f + (weight * 4.6f),
                        RoomType.MiniBoss when !context.LowHealth => 3.2f + (weight * 3.8f),
                        RoomType.Challenge when !context.LowHealth => 2.6f + (weight * 3f),
                        RoomType.Normal when !context.LowHealth => 1.4f + (weight * 1.8f),
                        _ => 0f
                    };
                    break;
                case "CASH WINDOW":
                    bonus = targetRoom.RoomType switch
                    {
                        RoomType.Shop => 4.8f + (weight * 5.4f),
                        RoomType.Secret when playerInventory != null && playerInventory.Bombs > 0 => 1.4f + (weight * 1.8f),
                        _ => 0f
                    };
                    break;
                case "KEY WINDOW":
                    bonus = targetRoom.RoomType switch
                    {
                        RoomType.Treasure => 5f + (weight * 5.5f),
                        RoomType.Secret when playerInventory != null && playerInventory.Bombs > 0 => 1f + (weight * 1.4f),
                        _ => 0f
                    };
                    break;
                case "BOMB LINE":
                    bonus = targetRoom.RoomType switch
                    {
                        RoomType.Secret => 5f + (weight * 5.6f),
                        RoomType.Trap when !context.LowHealth => 1.2f + (weight * 1.6f),
                        _ => 0f
                    };
                    break;
                case "LOADOUT FIND":
                    bonus = targetRoom.RoomType switch
                    {
                        RoomType.Treasure => 4.2f + (weight * 4.4f),
                        RoomType.Shop when context.NeedsUtility => 2.2f + (weight * 2.6f),
                        RoomType.Normal => 1f + (weight * 1.4f),
                        _ => 0f
                    };
                    break;
                case "SAFE UPGRADE":
                    bonus = targetRoom.RoomType switch
                    {
                        RoomType.Treasure => 3.4f + (weight * 3.6f),
                        RoomType.Shop when context.LowHealth || context.NeedsUtility => 1.8f + (weight * 2.2f),
                        RoomType.Normal => 1.2f + (weight * 1.6f),
                        _ => 0f
                    };
                    break;
            }

            if (bonus <= 0f)
            {
                return 0f;
            }

            intentReasonTag = _recentChoiceIntentBias.ReasonTag;
            return bonus;
        }

        private float ResolveAdaptiveDoorPriority(RoomDoor door, RoomController targetRoom, RouteNeedContext context, out string reasonTag)
        {
            reasonTag = string.Empty;

            if (door == null || targetRoom == null)
            {
                return 0f;
            }

            float totalBonus = 0f;
            float strongestReasonBonus = float.NegativeInfinity;
            RoomType roomType = targetRoom.RoomType;
            string bestReasonTag = string.Empty;

            void AddReasonedBonus(float bonus, string tag)
            {
                totalBonus += bonus;

                if (string.IsNullOrWhiteSpace(tag) || bonus <= strongestReasonBonus)
                {
                    return;
                }

                strongestReasonBonus = bonus;
                bestReasonTag = tag;
            }

            if (door.HasUnpaidHealthEntryCost)
            {
                float healthCostPenalty = context.LowHealth
                    ? 16f + (door.RequiredHealthToEnter * 5f)
                    : door.RequiredHealthToEnter * 3.2f;
                totalBonus -= healthCostPenalty;
            }

            switch (roomType)
            {
                case RoomType.Shop:
                    if (context.LowHealth && playerInventory != null && playerInventory.Coins >= 5)
                    {
                        AddReasonedBonus(26f, "PATCH HP");
                    }

                    if (context.CashRich)
                    {
                        AddReasonedBonus(18f, "CASH OUT");
                    }

                    if (context.NeedsKeys)
                    {
                        AddReasonedBonus(14f, "LOOK FOR KEYS");
                    }

                    if (context.NeedsBombs)
                    {
                        AddReasonedBonus(10f, "RESTOCK BOMBS");
                    }

                    if (context.NeedsUtility)
                    {
                        AddReasonedBonus(12f, "FILL SLOT");
                    }

                    if (playerInventory != null && playerInventory.Coins <= 2)
                    {
                        totalBonus -= 8f;
                    }
                    break;
                case RoomType.Treasure:
                    if (context.NeedsPower)
                    {
                        AddReasonedBonus(22f, "POWER SPIKE");
                    }

                    if (context.NeedsUtility)
                    {
                        AddReasonedBonus(12f, "LOADOUT FIND");
                    }

                    if (context.LowHealth)
                    {
                        AddReasonedBonus(8f, "SAFE UPGRADE");
                    }
                    break;
                case RoomType.Secret:
                    if (playerInventory != null && playerInventory.Bombs >= 1)
                    {
                        AddReasonedBonus(14f, "SECRET LINE");
                    }

                    if (context.CashRich || context.NeedsKeys || context.NeedsBombs)
                    {
                        AddReasonedBonus(8f, "CACHE HUNT");
                    }
                    break;
                case RoomType.MiniBoss:
                    if (!context.LowHealth && context.NeedsPower)
                    {
                        AddReasonedBonus(10f, "ELITE SPIKE");
                    }
                    else if (context.LowHealth)
                    {
                        totalBonus -= 12f;
                    }
                    break;
                case RoomType.Challenge:
                    if (!context.LowHealth && !context.NeedsPower)
                    {
                        AddReasonedBonus(9f, "TEST BUILD");
                    }
                    else
                    {
                        totalBonus -= 10f;
                    }
                    break;
                case RoomType.Boss:
                    if (context.ReadyForBoss)
                    {
                        AddReasonedBonus(18f, "PRESS BOSS");
                    }
                    else
                    {
                        totalBonus -= context.LowHealth ? 34f : 14f;
                    }
                    break;
                case RoomType.Curse:
                    totalBonus -= context.LowHealth ? 20f : 8f;
                    break;
                case RoomType.Trap:
                    totalBonus -= context.LowHealth ? 16f : 6f;
                    break;
                case RoomType.Normal:
                    if (context.LowHealth)
                    {
                        AddReasonedBonus(6f, "PLAY SAFE");
                    }
                    else if (context.NeedsPower)
                    {
                        AddReasonedBonus(5f, "SCOUT MORE");
                    }
                    break;
            }

            reasonTag = bestReasonTag;

            if (string.IsNullOrWhiteSpace(reasonTag))
            {
                reasonTag = ResolveFallbackReasonTag(roomType, context);
            }

            return totalBonus;
        }

        private Color ResolveRouteAccent(RoomType roomType, float traceBias, float intentBias)
        {
            Color accent = ResolveRoomAccent(roomType);

            if (_recentChoiceIntentBias.ExpiresAt > Time.unscaledTime && intentBias > 0.01f)
            {
                Color intentAccent = _recentChoiceIntentBias.AccentColor.a > 0.01f
                    ? _recentChoiceIntentBias.AccentColor
                    : accent;
                accent = Color.Lerp(
                    accent,
                    intentAccent,
                    Mathf.Clamp01(0.14f + (intentBias / 24f)));
            }

            if (_recentTraceBias.ExpiresAt > Time.unscaledTime && traceBias > 0.01f)
            {
                accent = Color.Lerp(
                    accent,
                    _recentTraceBias.AccentColor.a > 0.01f ? _recentTraceBias.AccentColor : accent,
                    Mathf.Clamp01(0.1f + (traceBias / 28f)));
            }

            return accent;
        }

        private static string ResolveFallbackReasonTag(RoomType roomType, RouteNeedContext context)
        {
            return roomType switch
            {
                RoomType.Shop => context.CashRich ? "CASH OUT" : "SUPPLY RUN",
                RoomType.Treasure => "POWER SPIKE",
                RoomType.Secret => "SECRET LINE",
                RoomType.Boss => context.ReadyForBoss ? "PRESS BOSS" : "HOLD LINE",
                RoomType.MiniBoss => "ELITE PUSH",
                RoomType.Challenge => "TEST BUILD",
                RoomType.Curse => "HIGH RISK",
                RoomType.Trap => "WATCH STEP",
                _ => "SCOUT"
            };
        }

        private static void BuildGuidanceStatusCopy(
            RoomDoor door,
            RoomType roomType,
            string reasonTag,
            string compareLabel,
            out string headline,
            out string detail,
            out string compactTag,
            out string detailEyebrow)
        {
            string roomLabel = ResolveRoomTypeGuidanceLabel(roomType);
            headline = $"{roomLabel} Route Recommended";

            if (IsShotProfileCompareLabel(compareLabel))
            {
                detail = BuildShotProfileDetail(roomType, compareLabel, door);
                compactTag = compareLabel.Trim();
                detailEyebrow = compareLabel.Trim();
                return;
            }

            detail = BuildReasonDetail(roomType, reasonTag, door);
            compactTag = roomLabel;
            detailEyebrow = string.IsNullOrWhiteSpace(reasonTag) ? "NEXT PUSH" : reasonTag;
        }

        private static string BuildShotProfileDetail(RoomType roomType, string compareLabel, RoomDoor door)
        {
            string directionLabel = ResolveDirectionLabel(door != null ? door.DoorDirection : RoomDirection.Up);
            string roomLabel = ResolveRoomTypeGuidanceLabel(roomType);
            string routeLabel = string.IsNullOrWhiteSpace(directionLabel)
                ? roomLabel
                : $"{roomLabel} {directionLabel}";

            return compareLabel == "SHOT RESET"
                ? $"{routeLabel} is your cleanest push while the shot profile reset is still fresh."
                : $"{routeLabel} is the best follow-through while {compareLabel} is online.";
        }

        private static string BuildReasonDetail(RoomType roomType, string reasonTag, RoomDoor door)
        {
            string directionLabel = ResolveDirectionLabel(door != null ? door.DoorDirection : RoomDirection.Up);
            string roomLabel = ResolveRoomTypeGuidanceLabel(roomType);
            string routeLabel = string.IsNullOrWhiteSpace(directionLabel)
                ? roomLabel
                : $"{roomLabel} {directionLabel}";

            if (reasonTag == "SPLASH ZONE")
            {
                return $"{routeLabel} is still risky. The fountain spray will kill your pace if you rush it.";
            }

            return reasonTag switch
            {
                "CLEAR LANE" => $"{routeLabel} is the cleanest exit from your current position, with almost no doorway friction.",
                "TIGHT DOOR" => $"{routeLabel} is live, but the doorway is tighter than it looks because props choke the entry.",
                "SPIKE EDGE" => $"{routeLabel} stays viable, but a contact hazard is hugging the threshold. Enter clean.",
                "WEB STRAND" => $"{routeLabel} is pinned by sticky strands, so your tempo drops the moment you step in.",
                "WATCH MINE" => $"{routeLabel} still works, but a mine is close enough to punish a lazy entry angle.",
                "ON PATH" => $"{routeLabel} is already on your current line, so you keep tempo by taking it now.",
                "CLEAN ENTRY" => $"{routeLabel} is the smoothest entry from your current angle and door spacing.",
                "AIM LINE" => $"{routeLabel} is already lined up with your current aim, so the handoff stays clean.",
                "PATCH HP" => $"{routeLabel} is the cleanest patch line while your health is still shaky.",
                "CASH OUT" => $"{routeLabel} lets you convert stored coins into a real spike right now.",
                "LOOK FOR KEYS" => $"{routeLabel} is the fastest route to recover key economy and path control.",
                "RESTOCK BOMBS" => $"{routeLabel} can stabilize bomb economy before the next pressure room.",
                "FILL SLOT" => $"{routeLabel} is the best chance to fill an empty utility slot.",
                "POWER SPIKE" => $"{routeLabel} is the strongest upgrade line for your current damage curve.",
                "PRESS ADVANTAGE" => $"{routeLabel} is strongest right after your latest spike. Convert that gain before the tempo cools off.",
                "RECOVERY ONLINE" => $"{routeLabel} is more viable now that recovery just landed. Push while the health buffer is real.",
                "CASH WINDOW" => $"{routeLabel} can cash in the resources you just picked up before they sit idle.",
                "KEY WINDOW" => $"{routeLabel} gets better immediately because your fresh key economy can unlock extra value.",
                "BOMB LINE" => $"{routeLabel} is live now that bomb economy can crack hidden payout routes.",
                "LOADOUT FIND" => $"{routeLabel} is the best route to round out missing build utility.",
                "SAFE UPGRADE" => $"{routeLabel} grows the build without forcing a hard check first.",
                "SECRET LINE" => $"{routeLabel} has the best odds to turn current resources into hidden payout.",
                "CACHE HUNT" => $"{routeLabel} can stabilize supplies without overcommitting into risk.",
                "ELITE SPIKE" => $"{routeLabel} is a good elite check if you want a sharper payout now.",
                "TEST BUILD" => $"{routeLabel} is a value push your current kit can probably afford.",
                "PRESS BOSS" => $"{routeLabel} is live because health and damage are finally online.",
                "PLAY SAFE" => $"{routeLabel} is the least punishing line until the build stabilizes.",
                "SCOUT MORE" => $"{routeLabel} gives you one more room to round the build out.",
                "HIGH RISK" => $"{routeLabel} is a high-risk line. Take it only if you want volatility.",
                "WATCH STEP" => $"{routeLabel} is still risky. Respect the route before committing.",
                "SPLASH ZONE" => $"{routeLabel} is still risky. The fountain spray will kill your pace if you rush it.",
                "HOLD LINE" => $"{routeLabel} exists, but the build still wants one more stabilizing room.",
                "SUPPLY RUN" => $"{routeLabel} is the cleanest supply route from your current state.",
                _ => $"{routeLabel} is your best next push from the current build state."
            };
        }

        private static string ResolveRoomTypeGuidanceLabel(RoomType roomType)
        {
            return roomType switch
            {
                RoomType.Treasure => "TREASURE",
                RoomType.Shop => "SHOP",
                RoomType.Boss => "BOSS",
                RoomType.Secret => "SECRET",
                RoomType.Challenge => "CHALLENGE",
                RoomType.MiniBoss => "ELITE",
                RoomType.Curse => "CURSE",
                RoomType.Trap => "TRAP",
                _ => "ROUTE"
            };
        }

        private static string ResolveDirectionLabel(RoomDirection direction)
        {
            return direction switch
            {
                RoomDirection.Up => "NORTH",
                RoomDirection.Right => "EAST",
                RoomDirection.Down => "SOUTH",
                RoomDirection.Left => "WEST",
                _ => string.Empty
            };
        }

        private static string ResolveDoorGuidanceLabel(RoomDoor door, string reasonTag, string compareLabel = "")
        {
            if (door == null)
            {
                return "MOVE OUT";
            }

            string roomLabel = door.ConnectedRoom != null
                ? door.ConnectedRoom.RoomType switch
                {
                    RoomType.Treasure => "TREASURE",
                    RoomType.Shop => "SHOP",
                    RoomType.Boss => "BOSS",
                    RoomType.Secret => "SECRET",
                    RoomType.Challenge => "CHALLENGE",
                    RoomType.MiniBoss => "ELITE",
                    RoomType.Curse => "CURSE",
                    RoomType.Trap => "TRAP",
                    _ => "PUSH"
                }
                : "PUSH";

            string directionLabel = door.DoorDirection switch
            {
                RoomDirection.Up => "NORTH",
                RoomDirection.Right => "EAST",
                RoomDirection.Down => "SOUTH",
                RoomDirection.Left => "WEST",
                _ => string.Empty
            };

            string baseLabel = string.IsNullOrEmpty(directionLabel)
                ? roomLabel
                : $"{roomLabel} {directionLabel}";

            if (IsShotProfileCompareLabel(compareLabel))
            {
                return $"{baseLabel} / {compareLabel.Trim()}";
            }

            return string.IsNullOrWhiteSpace(reasonTag)
                ? baseLabel
                : $"{baseLabel} / {reasonTag}";
        }

        private static void RaiseGuidanceLabel(Vector3 position, string label, Color accentColor)
        {
            GameplayFeedbackEvents.RaiseFloatingFeedback(new FloatingFeedbackRequest(
                position,
                label,
                Color.Lerp(accentColor, Color.white, 0.18f),
                0.78f,
                0.82f,
                1.16f,
                visualProfile: FloatingFeedbackVisualProfile.Momentum));
        }

        private void PrimeChoiceRouteFeedback(ChoiceRouteResolvedSignal signal)
        {
            if (!signal.IsValid || signal.Room == null)
            {
                return;
            }

            _pendingChoiceRouteFeedback = new PendingChoiceRouteFeedback
            {
                Room = signal.Room,
                ExpiresAt = Time.unscaledTime + Mathf.Max(0.25f, choiceRouteFeedbackWindow),
                Headline = signal.HasCarryClosure
                    ? signal.CarryHeadline ?? string.Empty
                    : IsShotProfileCompareLabel(signal.CompareLabel)
                        ? signal.CompareLabel
                        : signal.Headline ?? string.Empty,
                AccentColor = signal.AccentColor.a > 0.01f
                    ? signal.AccentColor
                    : ResolveRoomAccent(signal.Room.RoomType),
                HeldRoute = signal.HeldRoute,
                HasCarryClosure = signal.HasCarryClosure
            };
        }

        private bool TryGetPendingChoiceRouteFeedback(RoomController room, out PendingChoiceRouteFeedback feedback)
        {
            feedback = _pendingChoiceRouteFeedback;
            if (room == null
                || feedback.Room != room
                || feedback.ExpiresAt <= Time.unscaledTime
                || string.IsNullOrWhiteSpace(feedback.Headline))
            {
                return false;
            }

            return true;
        }

        private void ClearPendingChoiceRouteFeedback()
        {
            _pendingChoiceRouteFeedback = default;
        }

        private void PlayChoiceRouteFeedbackBurst(RoomDoor nextDoor, TraversalGuidanceBeacon nextBeacon, PendingChoiceRouteFeedback feedback)
        {
            if (_activeDoorTarget != null && _activeDoorTarget != nextDoor)
            {
                TraversalGuidanceBeacon previousBeacon = EnsureBeacon(_activeDoorTarget.gameObject);
                previousBeacon?.PlayCommitRejected();
            }

            if (nextBeacon == null)
            {
                return;
            }

            if (feedback.HeldRoute)
            {
                nextBeacon.PlayCommitAccepted();
                return;
            }

            if (_activeDoorTarget == nextDoor)
            {
                nextBeacon.PlayCommitRejected();
                return;
            }

            nextBeacon.PlayCommitAccepted();
        }

        private static string ResolveChoiceRouteBurstLabel(RoomDoor nextDoor, PendingChoiceRouteFeedback feedback)
        {
            if (feedback.HasCarryClosure && !string.IsNullOrWhiteSpace(feedback.Headline))
            {
                return feedback.Headline;
            }

            if (feedback.HeldRoute)
            {
                return "PLAN HELD";
            }

            return nextDoor != null
                ? "REROUTE"
                : "PLAN SHIFT";
        }

        private bool TryAccumulateRecentRouteTraceBias(
            string headline,
            string detail,
            Color accentColor,
            bool emphasize,
            bool treatAsLoadoutSpike)
        {
            RoomController activeRoom = roomNavigationController != null
                ? roomNavigationController.CurrentRoom
                : _activeSourceRoom;

            if (activeRoom == null || activeRoom.State != RoomState.Rewarded)
            {
                return false;
            }

            RecentRouteTraceBias bias = _recentTraceBias;
            bool resetBias = bias.Room != activeRoom || bias.ExpiresAt <= Time.unscaledTime;
            if (resetBias)
            {
                bias = default;
                bias.Room = activeRoom;
                bias.AccentColor = accentColor.a > 0.01f ? accentColor : ResolveRoomAccent(activeRoom.RoomType);
            }

            bool changed = false;
            string normalizedHeadline = headline ?? string.Empty;
            string normalizedDetail = detail ?? string.Empty;

            if (treatAsLoadoutSpike || IsPowerTrace(normalizedHeadline, normalizedDetail, emphasize))
            {
                bias.PowerWeight += emphasize ? 1.35f : 1f;
                changed = true;
            }

            if (ContainsPositiveToken(normalizedDetail, "COIN"))
            {
                bias.CashWeight += 1.1f;
                changed = true;
            }

            if (ContainsPositiveToken(normalizedDetail, "KEY"))
            {
                bias.KeyWeight += 1.15f;
                changed = true;
            }

            if (ContainsPositiveToken(normalizedDetail, "BOMB"))
            {
                bias.BombWeight += 1.15f;
                changed = true;
            }

            if (ContainsIgnoreCase(normalizedHeadline, "HEAL") || ContainsPositiveToken(normalizedDetail, "HP"))
            {
                bias.RecoveryWeight += emphasize ? 1.3f : 1f;
                changed = true;
            }

            if (!changed)
            {
                return false;
            }

            bias.Room = activeRoom;
            bias.ExpiresAt = Time.unscaledTime + Mathf.Max(1f, recentTraceBiasDuration);
            if (accentColor.a > 0.01f)
            {
                bias.AccentColor = resetBias
                    ? accentColor
                    : Color.Lerp(bias.AccentColor, accentColor, 0.42f);
            }

            _recentTraceBias = bias;
            return true;
        }

        private bool TryAccumulateRecentChoiceIntentBias(ChoiceRouteResolvedSignal signal)
        {
            RoomController activeRoom = roomNavigationController != null
                ? roomNavigationController.CurrentRoom
                : _activeSourceRoom;

            if (!signal.IsValid
                || !signal.HasIntent
                || activeRoom == null
                || activeRoom.State != RoomState.Rewarded
                || signal.Room != activeRoom)
            {
                return false;
            }

            _recentChoiceIntentBias = new RecentChoiceIntentBias
            {
                Room = activeRoom,
                ExpiresAt = Time.unscaledTime + Mathf.Max(1f, recentChoiceIntentDuration),
                ReasonTag = signal.IntentReasonTag,
                CompareLabel = signal.CompareLabel,
                Weight = Mathf.Clamp01(signal.Strength),
                AccentColor = signal.AccentColor.a > 0.01f
                    ? signal.AccentColor
                    : ResolveRoomAccent(activeRoom.RoomType),
                HeldRoute = signal.HeldRoute
            };
            return true;
        }

        private void ClearRecentRouteTraceBias()
        {
            _recentTraceBias = default;
        }

        private void ClearRecentChoiceIntentBias()
        {
            _recentChoiceIntentBias = default;
        }

        private static bool IsPowerTrace(string headline, string detail, bool emphasize)
        {
            if (ContainsIgnoreCase(headline, "LOADOUT")
                || ContainsIgnoreCase(headline, "ACTIVE")
                || ContainsIgnoreCase(headline, "TRINKET")
                || ContainsIgnoreCase(headline, "BUFF")
                || ContainsIgnoreCase(headline, "PURCHASE CONFIRMED"))
            {
                return true;
            }

            if (ContainsPositiveToken(detail, "DMG")
                || ContainsPositiveToken(detail, "SHOT")
                || ContainsIgnoreCase(detail, "PIERCE")
                || ContainsIgnoreCase(detail, "LASER")
                || ContainsIgnoreCase(detail, "BUFF"))
            {
                return true;
            }

            return emphasize
                && !ContainsIgnoreCase(headline, "SUPPLY")
                && !ContainsIgnoreCase(headline, "HEAL")
                && !ContainsIgnoreCase(headline, "STASH");
        }

        private static bool IsShotProfileCompareLabel(string compareLabel)
        {
            if (string.IsNullOrWhiteSpace(compareLabel))
            {
                return false;
            }

            return compareLabel == "SHOT RESET"
                || compareLabel == "BASELINE"
                || compareLabel.Contains("LASER")
                || compareLabel.Contains("ORBIT")
                || compareLabel.Contains("SHIELD")
                || compareLabel.Contains("SPLIT")
                || compareLabel.Contains("BLAST")
                || compareLabel.Contains("LEECH")
                || compareLabel.Contains("BOUNCE");
        }

        private static bool ContainsPositiveToken(string text, string token)
        {
            if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(token))
            {
                return false;
            }

            string upperText = text.ToUpperInvariant();
            string upperToken = token.ToUpperInvariant();
            int tokenIndex = upperText.IndexOf(upperToken, System.StringComparison.Ordinal);

            while (tokenIndex >= 0)
            {
                int plusIndex = upperText.LastIndexOf('+', tokenIndex);
                int minusIndex = upperText.LastIndexOf('-', tokenIndex);
                if (plusIndex >= 0 && plusIndex > minusIndex && tokenIndex - plusIndex <= 6)
                {
                    return true;
                }

                tokenIndex = upperText.IndexOf(upperToken, tokenIndex + upperToken.Length, System.StringComparison.Ordinal);
            }

            return false;
        }

        private static bool ContainsIgnoreCase(string text, string token)
        {
            return !string.IsNullOrWhiteSpace(text)
                && !string.IsNullOrWhiteSpace(token)
                && text.IndexOf(token, System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private TraversalDoorGuidanceStyle ApplyDoorBeaconPresentation(
            RoomController room,
            RoomDoor guidedDoor,
            Color guidedAccentColor,
            string guidedReasonTag,
            Vector2 playerPosition)
        {
            if (room == null)
            {
                return TraversalDoorGuidanceStyle.Guided;
            }

            IReadOnlyList<RoomDoor> roomDoors = room.RoomDoors;
            TraversalDoorGuidanceStyle guidedDoorStyle = TraversalDoorGuidanceStyle.Guided;
            for (int i = 0; i < roomDoors.Count; i++)
            {
                RoomDoor door = roomDoors[i];

                if (door == null)
                {
                    continue;
                }

                TraversalGuidanceBeacon beacon = EnsureBeacon(door.gameObject);
                if (beacon == null)
                {
                    continue;
                }

                if (door.IsLocked || door.ConnectedRoom == null)
                {
                    beacon.Hide();
                    continue;
                }

                float riskBias = ResolveTraversalDoorRiskPriority(room, door, playerPosition, out string riskReasonTag);

                if (door == guidedDoor)
                {
                    guidedDoorStyle = ResolveGuidedDoorStyle(guidedReasonTag, riskReasonTag, riskBias);
                    beacon.ShowDoorGuidance(
                        guidedAccentColor,
                        door.DoorDirection,
                        doorGuidanceDuration,
                        guidedDoorStyle);
                    continue;
                }

                if (ShouldShowDoorRiskWarning(riskReasonTag, riskBias))
                {
                    beacon.ShowDoorRiskWarning(
                        ResolveDoorWarningAccent(door, riskReasonTag),
                        door.DoorDirection,
                        doorGuidanceDuration);
                    continue;
                }

                beacon.Hide();
            }

            return guidedDoorStyle;
        }

        private static TraversalDoorGuidanceStyle ResolveGuidedDoorStyle(string guidedReasonTag, string riskReasonTag, float riskBias)
        {
            if (IsRiskReasonTag(riskReasonTag) && riskBias <= -2.8f)
            {
                return TraversalDoorGuidanceStyle.GuidedRisk;
            }

            if (IsCleanReasonTag(guidedReasonTag) || IsCleanReasonTag(riskReasonTag) || riskBias >= 2f)
            {
                return TraversalDoorGuidanceStyle.GuidedClean;
            }

            return TraversalDoorGuidanceStyle.Guided;
        }

        private static bool ShouldShowDoorRiskWarning(string riskReasonTag, float riskBias)
        {
            return IsRiskReasonTag(riskReasonTag) && riskBias <= -2.6f;
        }

        private static bool IsRiskReasonTag(string reasonTag)
        {
            return reasonTag == "SPIKE EDGE"
                || reasonTag == "WATCH STEP"
                || reasonTag == "WATCH MINE"
                || reasonTag == "TIGHT DOOR"
                || reasonTag == "WEB STRAND"
                || reasonTag == "SPLASH ZONE";
        }

        private static bool IsCleanReasonTag(string reasonTag)
        {
            return reasonTag == "CLEAR LANE";
        }

        private static Color ResolveDoorWarningAccent(RoomDoor door, string riskReasonTag)
        {
            Color baseAccent = door != null && door.ConnectedRoom != null
                ? ResolveRoomAccent(door.ConnectedRoom.RoomType)
                : ResolveRoomAccent(RoomType.Normal);
            Color warningAccent = new Color(1f, 0.34f, 0.24f, 1f);

            return riskReasonTag switch
            {
                "WATCH MINE" => Color.Lerp(baseAccent, warningAccent, 0.78f),
                "SPIKE EDGE" => Color.Lerp(baseAccent, warningAccent, 0.7f),
                "WEB STRAND" => Color.Lerp(baseAccent, new Color(0.35f, 0.9f, 1f, 1f), 0.76f),
                "SPLASH ZONE" => Color.Lerp(baseAccent, new Color(0.34f, 0.86f, 1f, 1f), 0.78f),
                _ => Color.Lerp(baseAccent, warningAccent, 0.58f)
            };
        }

        private static TraversalDoorGuidanceStyle ResolveFallbackDoorStyle(RoomDoor door)
        {
            if (door == null)
            {
                return TraversalDoorGuidanceStyle.Guided;
            }

            return door.HasUnpaidHealthEntryCost
                ? TraversalDoorGuidanceStyle.GuidedRisk
                : TraversalDoorGuidanceStyle.Guided;
        }

        private static void HideRoomDoorBeacons(RoomController room)
        {
            if (room == null)
            {
                return;
            }

            IReadOnlyList<RoomDoor> roomDoors = room.RoomDoors;
            for (int i = 0; i < roomDoors.Count; i++)
            {
                RoomDoor door = roomDoors[i];
                if (door == null)
                {
                    continue;
                }

                TraversalGuidanceBeacon beacon = door.GetComponent<TraversalGuidanceBeacon>();
                beacon?.Hide();
            }
        }

        private static TraversalGuidanceBeacon EnsureBeacon(GameObject target)
        {
            if (target == null)
            {
                return null;
            }

            TraversalGuidanceBeacon beacon = target.GetComponent<TraversalGuidanceBeacon>();
            if (beacon == null)
            {
                beacon = target.AddComponent<TraversalGuidanceBeacon>();
            }

            return beacon;
        }

        private void ClearActiveGuidance()
        {
            bool hadGuidance = _activeSourceRoom != null
                || _activeDoorTarget != null
                || !string.IsNullOrWhiteSpace(_activeStatusHeadline);

            HideRoomDoorBeacons(_activeSourceRoom);

            if (_activeBeacon != null)
            {
                _activeBeacon.Hide();
                _activeBeacon = null;
            }

            _activeSourceRoom = null;
            _activeDoorTarget = null;
            _activeTargetRoomType = RoomType.Normal;
            _activeDoorStyle = TraversalDoorGuidanceStyle.Guided;
            _activeAccentColor = Color.white;
            _activeReasonTag = string.Empty;
            _activeStatusHeadline = string.Empty;
            _activeStatusDetail = string.Empty;
            _activeStatusCompactTag = string.Empty;
            _activeStatusDetailEyebrow = string.Empty;
            ClearPendingChoiceRouteFeedback();

            if (hadGuidance)
            {
                RaiseGuidanceStatusChanged();
            }
        }

        private void RaiseGuidanceStatusChanged()
        {
            GuidanceStatusChanged?.Invoke();
        }

        private void ResolveReferences()
        {
            if (roomNavigationController == null)
            {
                roomNavigationController = GetComponent<RoomNavigationController>();
            }

            if (playerController == null)
            {
                playerController = FindFirstObjectByType<PlayerController>(FindObjectsInactive.Exclude);
            }

            Transform playerTransform = playerController != null ? playerController.transform : null;
            if (playerTransform == null)
            {
                return;
            }

            if (playerHealth == null)
            {
                playerHealth = playerTransform.GetComponent<PlayerHealth>();
            }

            if (playerMovement == null)
            {
                playerMovement = playerTransform.GetComponent<PlayerMovement>();
            }

            if (playerInventory == null)
            {
                playerInventory = playerTransform.GetComponent<PlayerInventory>();
            }

            if (playerStats == null)
            {
                playerStats = playerTransform.GetComponent<PlayerStats>();
            }

            if (playerCombat == null)
            {
                playerCombat = playerTransform.GetComponent<PlayerCombat>();
            }

            if (playerActiveItemController == null)
            {
                playerActiveItemController = playerTransform.GetComponent<PlayerActiveItemController>();
            }

            if (playerTrinketHolder == null)
            {
                playerTrinketHolder = playerTransform.GetComponent<PlayerTrinketHolder>();
            }

            if (playerConsumableHolder == null)
            {
                playerConsumableHolder = playerTransform.GetComponent<PlayerConsumableHolder>();
            }
        }
    }
}
