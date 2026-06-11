using CuteIssac.Core.Gameplay;
using CuteIssac.Item;
using CuteIssac.Player;
using CuteIssac.Data.Item;
using UnityEngine;
using System.Collections.Generic;

namespace CuteIssac.Room
{
    /// <summary>
    /// Tracks the current actionable target inside a room and upgrades its local affordance while the player is nearby.
    /// This keeps selection clarity alive after the initial arrival beat fades.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RoomController))]
    public sealed class RoomInteractionAffordanceController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private RoomController roomController;
        [SerializeField] private PlayerController playerController;
        [SerializeField] private PlayerInventory playerInventory;
        [SerializeField] private PlayerItemManager playerItemManager;
        [SerializeField] private PlayerHealth playerHealth;
        [SerializeField] private PlayerStats playerStats;
        [SerializeField] private PlayerTrinketHolder playerTrinketHolder;
        [SerializeField] private PlayerActiveItemController playerActiveItemController;
        [SerializeField] private PlayerConsumableHolder playerConsumableHolder;
        [SerializeField] private TreasureRoomSpawner treasureRoomSpawner;
        [SerializeField] private RoomTypeContentController roomTypeContentController;

        [Header("Behavior")]
        [SerializeField] [Min(0.5f)] private float treasureAffordanceDistance = 2.4f;
        [SerializeField] [Min(0.25f)] private float treasureCommitReadyDistance = 0.92f;
        [SerializeField] [Min(0.25f)] private float shopCommitReadyDistance = 1.1f;
        [SerializeField] [Min(0.5f)] private float fallbackAffordanceDistance = 2.1f;
        [SerializeField] [Min(0.25f)] private float fallbackCommitReadyDistance = 0.96f;
        [SerializeField] [Min(0.1f)] private float minimumArrivalAssistDuration = 0.82f;
        [SerializeField] [Min(0.25f)] private float routeRecommendationSyncDuration = 2.4f;
        [SerializeField] [Min(0.25f)] private float planCarryPresentationDuration = 1.1f;
        [SerializeField] [Min(0.05f)] private float recommendationRefreshInterval = 0.18f;
        [SerializeField] [Range(1, 3)] private int recommendationVisibleCount = 2;
        [SerializeField] [Min(0f)] private float recommendationSecondaryScoreWindow = 11f;
        [SerializeField] [Min(0.1f)] private float previewResolutionDuration = 0.96f;
        [SerializeField] [Min(0.1f)] private float previewDeltaBridgeWindow = 1.32f;

        private readonly List<Transform> _fallbackTargetBuffer = new();
        private readonly List<Transform> _arrivalAssistCandidateBuffer = new();
        private readonly List<Transform> _recommendationTargetBuffer = new();
        private readonly List<RecommendationCandidate> _recommendationCandidateBuffer = new();
        private readonly List<Transform> _visibleRecommendationTargets = new();
        private readonly HashSet<Transform> _markedRecommendationTargets = new();
        private Transform _activeTarget;
        private RoomInteractionAffordancePresentation _activePresentation;
        private BasePickupLogic _activePickupLogic;
        private ShopInventory _boundShopInventory;
        private PlayerInteractionReceiptPresentation _playerReceiptPresentation;
        private PlayerInteractionPreviewPresentation _playerPreviewPresentation;
        private Transform _arrivalAssistTarget;
        private string _arrivalAssistCompareLabel = string.Empty;
        private Color _arrivalAssistAccentColor = Color.white;
        private Color _arrivalAssistCompareColor = Color.white;
        private float _arrivalAssistExpiresAt = float.NegativeInfinity;
        private bool _arrivalAssistPulsePending;
        private string _arrivalAssistCarryLabel = string.Empty;
        private Color _arrivalAssistCarryColor = Color.white;
        private float _arrivalAssistCarryExpiresAt = float.NegativeInfinity;
        private string _routeRecommendationReasonTag = string.Empty;
        private Color _routeRecommendationAccentColor = Color.white;
        private float _routeRecommendationExpiresAt = float.NegativeInfinity;
        private string _routeRecommendationCarryLabel = string.Empty;
        private Color _routeRecommendationCarryColor = Color.white;
        private float _routeRecommendationCarryExpiresAt = float.NegativeInfinity;
        private Transform _primaryRecommendationTarget;
        private string _primaryRecommendationCompareLabel = string.Empty;
        private string _primaryRecommendationRouteReasonTag = string.Empty;
        private Color _primaryRecommendationAccentColor = Color.white;
        private float _primaryRecommendationDominance;
        private bool _activeStrong;
        private bool _activeReady;
        private float _nextRecommendationRefreshAt = float.NegativeInfinity;
        private Transform _livePreviewTarget;
        private string _livePreviewTitle = string.Empty;
        private string _livePreviewCompareLabel = string.Empty;
        private string _livePreviewOutcomeLabel = string.Empty;
        private Color _livePreviewAccentColor = Color.white;
        private Color _livePreviewCompareColor = Color.white;
        private Color _livePreviewOutcomeColor = Color.white;
        private float _previewResolutionExpiresAt = float.NegativeInfinity;
        private string _previewResolutionTitle = string.Empty;
        private string _previewResolutionDetail = string.Empty;
        private string _previewResolutionCompareLabel = string.Empty;
        private string _previewResolutionOutcomeLabel = string.Empty;
        private Color _previewResolutionAccentColor = Color.white;
        private Color _previewResolutionCompareColor = Color.white;
        private Color _previewResolutionOutcomeColor = Color.white;
        private bool _previewResolutionEmphasize;
        private float _pendingPreviewDeltaBridgeExpiresAt = float.NegativeInfinity;
        private string _pendingPreviewSourceTitle = string.Empty;
        private string _pendingPreviewFallbackOutcome = string.Empty;
        private Color _pendingPreviewFallbackOutcomeColor = Color.white;
        private bool _pendingPreviewEmphasize;

        private struct RecommendationCandidate
        {
            public Transform Target;
            public float Score;
            public string CompareLabel;
            public Color CompareColor;
            public bool Blocked;
            public bool RouteSync;
            public string RouteLabel;
            public float RouteBias;
        }

        private readonly struct RouteChoiceResolution
        {
            public RouteChoiceResolution(
                string headline,
                string compareLabel,
                string detailLabel,
                string intentReasonTag,
                Color accentColor,
                bool heldRoute,
                float strength,
                bool emphasize)
            {
                Headline = headline ?? string.Empty;
                CompareLabel = compareLabel ?? string.Empty;
                DetailLabel = detailLabel ?? string.Empty;
                IntentReasonTag = intentReasonTag ?? string.Empty;
                AccentColor = accentColor.a > 0.01f
                    ? new Color(accentColor.r, accentColor.g, accentColor.b, 1f)
                    : Color.white;
                HeldRoute = heldRoute;
                Strength = Mathf.Clamp01(strength);
                Emphasize = emphasize;
            }

            public string Headline { get; }
            public string CompareLabel { get; }
            public string DetailLabel { get; }
            public string IntentReasonTag { get; }
            public Color AccentColor { get; }
            public bool HeldRoute { get; }
            public float Strength { get; }
            public bool Emphasize { get; }
            public bool IsValid => !string.IsNullOrWhiteSpace(Headline);
        }

        private readonly struct PlanCarryResolution
        {
            public PlanCarryResolution(string headline, string detailLabel, Color accentColor, bool heldPlan, bool emphasize)
            {
                Headline = headline ?? string.Empty;
                DetailLabel = detailLabel ?? string.Empty;
                AccentColor = accentColor.a > 0.01f
                    ? new Color(accentColor.r, accentColor.g, accentColor.b, 1f)
                    : Color.white;
                HeldPlan = heldPlan;
                Emphasize = emphasize;
            }

            public string Headline { get; }
            public string DetailLabel { get; }
            public Color AccentColor { get; }
            public bool HeldPlan { get; }
            public bool Emphasize { get; }
            public bool IsValid => !string.IsNullOrWhiteSpace(Headline);
        }

        public void ConfigureRuntime(RoomController targetRoom)
        {
            roomController = targetRoom;
            ResolveReferences();
        }

        public bool TryResolveGuidedArrivalAssistTarget(
            Transform hintedTarget,
            string routeReasonTag,
            out Transform target,
            out string compareLabel,
            out Color compareColor)
        {
            ResolveReferences();

            target = hintedTarget;
            compareLabel = routeReasonTag ?? string.Empty;
            compareColor = RoomTraversalGuidanceController.ResolveRoomAccent(roomController != null
                ? roomController.RoomType
                : Data.Dungeon.RoomType.Normal);

            _arrivalAssistCandidateBuffer.Clear();
            CollectArrivalAssistCandidates(_arrivalAssistCandidateBuffer);
            if (hintedTarget != null
                && hintedTarget.gameObject.activeInHierarchy
                && !_arrivalAssistCandidateBuffer.Contains(hintedTarget))
            {
                _arrivalAssistCandidateBuffer.Insert(0, hintedTarget);
            }

            float bestScore = float.NegativeInfinity;

            for (int i = 0; i < _arrivalAssistCandidateBuffer.Count; i++)
            {
                Transform candidate = _arrivalAssistCandidateBuffer[i];
                if (candidate == null || !candidate.gameObject.activeInHierarchy)
                {
                    continue;
                }

                if (!RoomInteractionPreviewResolver.TryResolvePriority(
                        candidate,
                        roomController,
                        playerInventory,
                        playerItemManager,
                        playerHealth,
                        playerStats,
                        playerTrinketHolder,
                        playerActiveItemController,
                        playerConsumableHolder,
                        out float priorityScore,
                        out string candidateCompareLabel,
                        out Color candidateCompareColor))
                {
                    continue;
                }

                priorityScore += ResolveArrivalAssistReasonBias(candidate, routeReasonTag, candidateCompareLabel);
                if (candidate == hintedTarget)
                {
                    priorityScore += 0.75f;
                }

                if (priorityScore <= bestScore)
                {
                    continue;
                }

                bestScore = priorityScore;
                target = candidate;
                compareLabel = string.IsNullOrWhiteSpace(candidateCompareLabel)
                    ? compareLabel
                    : candidateCompareLabel;
                compareColor = candidateCompareColor.a > 0.01f
                    ? new Color(candidateCompareColor.r, candidateCompareColor.g, candidateCompareColor.b, 1f)
                    : compareColor;
            }

            return target != null;
        }

        public void PrimeArrivalAssist(Transform target, string compareLabel, Color accentColor, Color compareColor, float duration, string routeReasonTag = "", string carryLabel = "")
        {
            _arrivalAssistTarget = target;
            _arrivalAssistCompareLabel = compareLabel ?? string.Empty;
            _arrivalAssistAccentColor = accentColor.a > 0.01f
                ? new Color(accentColor.r, accentColor.g, accentColor.b, 1f)
                : RoomTraversalGuidanceController.ResolveRoomAccent(roomController != null ? roomController.RoomType : Data.Dungeon.RoomType.Normal);
            _arrivalAssistCompareColor = compareColor.a > 0.01f
                ? new Color(compareColor.r, compareColor.g, compareColor.b, 1f)
                : Color.Lerp(_arrivalAssistAccentColor, Color.white, 0.18f);
            _arrivalAssistExpiresAt = Time.unscaledTime + Mathf.Max(minimumArrivalAssistDuration, duration);
            _arrivalAssistPulsePending = target != null;

            if (!string.IsNullOrWhiteSpace(routeReasonTag))
            {
                _routeRecommendationReasonTag = routeReasonTag;
                _routeRecommendationAccentColor = _arrivalAssistAccentColor;
                _routeRecommendationExpiresAt = Time.unscaledTime + Mathf.Max(routeRecommendationSyncDuration, duration);
                _nextRecommendationRefreshAt = float.NegativeInfinity;
            }

            if (!string.IsNullOrWhiteSpace(carryLabel))
            {
                float carryDuration = Mathf.Max(planCarryPresentationDuration, Mathf.Min(routeRecommendationSyncDuration, duration));
                Color carryColor = Color.Lerp(_arrivalAssistAccentColor, Color.white, 0.18f);
                _arrivalAssistCarryLabel = carryLabel;
                _arrivalAssistCarryColor = carryColor;
                _arrivalAssistCarryExpiresAt = Time.unscaledTime + carryDuration;
                _routeRecommendationCarryLabel = carryLabel;
                _routeRecommendationCarryColor = carryColor;
                _routeRecommendationCarryExpiresAt = Time.unscaledTime + carryDuration;
                _nextRecommendationRefreshAt = float.NegativeInfinity;
            }
            else
            {
                ClearArrivalAssistCarryPresentation();
                ClearRouteRecommendationCarryPresentation();
            }
        }

        private void Awake()
        {
            ResolveReferences();
        }

        private void OnEnable()
        {
            GameplayRuntimeEvents.PlayerLoadoutDelta += HandlePlayerLoadoutDelta;
            GameplayRuntimeEvents.PlayerInteractionOutcome += HandlePlayerInteractionOutcome;
        }

        private void OnDisable()
        {
            GameplayRuntimeEvents.PlayerLoadoutDelta -= HandlePlayerLoadoutDelta;
            GameplayRuntimeEvents.PlayerInteractionOutcome -= HandlePlayerInteractionOutcome;
            ClearArrivalAssist();
            ClearChoiceRecommendations();
            ClearActiveAffordance();
            ClearLivePreviewState();
            ClearPreviewResolution();
            ClearPendingPreviewDeltaBridge();
            UnbindRoomSignals();
        }

        private void Update()
        {
            ResolveReferences();
            EnsureRoomSignalsBound();

            if (Time.unscaledTime > _pendingPreviewDeltaBridgeExpiresAt)
            {
                ClearPendingPreviewDeltaBridge();
            }

            if (roomController == null || !roomController.IsCurrentRoom || playerController == null)
            {
                ClearArrivalAssist();
                ClearRouteRecommendationSync();
                ClearChoiceRecommendations();
                ClearActiveAffordance();
                ClearLivePreviewState();
                ClearPreviewResolution();
                ClearPendingPreviewDeltaBridge();
                return;
            }

            UpdateChoiceRecommendations();

            if (!TryResolveAffordanceTarget(out Transform target, out bool strong, out bool ready, out float radius))
            {
                ClearActiveAffordance(clearPreview: !HasActivePreviewResolution());
                if (HasActivePreviewResolution())
                {
                    UpdatePlayerPreview(null, strong: true, ready: true);
                    return;
                }

                ClearLivePreviewState();
                return;
            }

            if (target != _activeTarget || strong != _activeStrong)
            {
                ClearActiveAffordance();
                _activeTarget = target;
                _activeStrong = strong;
                _activePresentation = EnsurePresentation(target);
                BindTargetSignals(target);

                if (ready)
                {
                    _activePresentation?.TriggerCommitPulse(strong ? 1f : 0.72f);
                }
            }
            else if (ready && !_activeReady)
            {
                _activePresentation?.TriggerCommitPulse(strong ? 1f : 0.72f);
            }

            bool arrivalAssistActive = IsArrivalAssistTarget(target);
            _activePresentation?.SetAffordance(
                roomController.RoomType,
                arrivalAssistActive ? _arrivalAssistAccentColor : RoomTraversalGuidanceController.ResolveRoomAccent(roomController.RoomType),
                strong,
                ready,
                radius);
            if (arrivalAssistActive && _arrivalAssistPulsePending)
            {
                _activePresentation?.TriggerCommitPulse(0.82f);
                _arrivalAssistPulsePending = false;
            }
            UpdatePlayerPreview(target, strong, ready);
            _activeReady = ready;
        }

        private bool TryResolveAffordanceTarget(out Transform targetTransform, out bool strong, out bool ready, out float radius)
        {
            targetTransform = null;
            strong = false;
            ready = false;
            radius = 0.5f;

            if (roomController == null || playerController == null)
            {
                return false;
            }

            Vector3 playerPosition = playerController.transform.position;
            if (TryResolveArrivalAssistTarget(playerPosition, out targetTransform, out strong, out ready, out radius))
            {
                return true;
            }

            switch (roomController.RoomType)
            {
                case Data.Dungeon.RoomType.Treasure:
                    if (treasureRoomSpawner != null
                        && treasureRoomSpawner.TryResolveClosestTreasureChoice(playerPosition, treasureAffordanceDistance, out targetTransform))
                    {
                        strong = true;
                        ready = Vector3.Distance(playerPosition, targetTransform.position) <= treasureCommitReadyDistance;
                        radius = ready ? 0.64f : 0.56f;
                        return true;
                    }

                    break;
                case Data.Dungeon.RoomType.Shop:
                    if (roomTypeContentController != null
                        && roomTypeContentController.TryResolveShopAffordanceTarget(out targetTransform, out bool canPurchase)
                        && targetTransform != null)
                    {
                        float targetDistance = Vector3.Distance(playerPosition, targetTransform.position);
                        strong = canPurchase;
                        ready = canPurchase && targetDistance <= shopCommitReadyDistance;
                        radius = ready
                            ? 0.66f
                            : canPurchase
                                ? 0.58f
                                : 0.48f;
                        return true;
                    }

                    break;
                case Data.Dungeon.RoomType.Curse:
                    if (roomTypeContentController != null
                        && roomTypeContentController.TryResolveContentFocusTarget(out Vector3 focusPosition, out _)
                        && Vector3.Distance(playerPosition, focusPosition) <= fallbackAffordanceDistance)
                    {
                        targetTransform = ResolveFallbackTargetTransform();
                        strong = true;
                        ready = targetTransform != null && Vector3.Distance(playerPosition, targetTransform.position) <= fallbackCommitReadyDistance;
                        radius = ready ? 0.62f : 0.54f;
                        return targetTransform != null;
                    }

                    break;
            }

            return false;
        }

        private Transform ResolveFallbackTargetTransform()
        {
            if (roomTypeContentController == null)
            {
                return null;
            }

            _fallbackTargetBuffer.Clear();
            roomTypeContentController.CollectEntryHighlightTargets(_fallbackTargetBuffer);
            return _fallbackTargetBuffer.Count > 0 ? _fallbackTargetBuffer[0] : null;
        }

        private RoomInteractionAffordancePresentation EnsurePresentation(Transform target)
        {
            if (target == null)
            {
                return null;
            }

            RoomInteractionAffordancePresentation presentation = target.GetComponent<RoomInteractionAffordancePresentation>();
            if (presentation == null)
            {
                presentation = target.gameObject.AddComponent<RoomInteractionAffordancePresentation>();
            }

            return presentation;
        }

        private RoomChoiceRecommendationPresentation EnsureChoiceRecommendationPresentation(Transform target)
        {
            if (target == null)
            {
                return null;
            }

            RoomChoiceRecommendationPresentation presentation = target.GetComponent<RoomChoiceRecommendationPresentation>();
            if (presentation == null)
            {
                presentation = target.gameObject.AddComponent<RoomChoiceRecommendationPresentation>();
            }

            return presentation;
        }

        private void ClearActiveAffordance(bool clearPreview = true)
        {
            UnbindTargetSignals();
            _activePresentation?.ClearAffordance();
            if (clearPreview)
            {
                _playerPreviewPresentation?.ClearPreview();
            }
            _activePresentation = null;
            _activeTarget = null;
            _activeStrong = false;
            _activeReady = false;
        }

        private void UpdateChoiceRecommendations()
        {
            if (!SupportsChoiceRecommendations(roomController != null ? roomController.RoomType : Data.Dungeon.RoomType.Normal))
            {
                ClearRouteRecommendationSync();
                ClearChoiceRecommendations();
                return;
            }

            string routeReasonTag = string.Empty;
            Color routeAccentColor = Color.white;
            bool hasRouteSync = TryGetActiveRouteRecommendationSync(out routeReasonTag, out routeAccentColor);
            bool hasCarryPresentation = TryGetActiveRouteRecommendationCarryPresentation(out string carryPresentationLabel, out _);

            if (Time.unscaledTime < _nextRecommendationRefreshAt)
            {
                return;
            }

            _nextRecommendationRefreshAt = Time.unscaledTime + recommendationRefreshInterval;
            CollectRecommendationCandidates(_recommendationTargetBuffer);

            _recommendationCandidateBuffer.Clear();
            for (int i = 0; i < _recommendationTargetBuffer.Count; i++)
            {
                Transform candidate = _recommendationTargetBuffer[i];
                if (candidate == null || !candidate.gameObject.activeInHierarchy)
                {
                    continue;
                }

                if (!RoomInteractionPreviewResolver.TryResolvePriority(
                        candidate,
                        roomController,
                        playerInventory,
                        playerItemManager,
                        playerHealth,
                        playerStats,
                        playerTrinketHolder,
                        playerActiveItemController,
                        playerConsumableHolder,
                        out float priorityScore,
                        out string compareLabel,
                        out Color compareColor))
                {
                    continue;
                }

                float routeBias = hasRouteSync
                    ? ResolveArrivalAssistReasonBias(candidate, routeReasonTag, compareLabel)
                    : 0f;
                priorityScore += routeBias;

                Color resolvedCompareColor = compareColor;
                if (hasRouteSync && routeBias > 0f)
                {
                    Color baseColor = resolvedCompareColor.a > 0.01f
                        ? resolvedCompareColor
                        : routeAccentColor;
                    resolvedCompareColor = Color.Lerp(baseColor, routeAccentColor, 0.4f);
                }

                _recommendationCandidateBuffer.Add(new RecommendationCandidate
                {
                    Target = candidate,
                    Score = priorityScore,
                    CompareLabel = compareLabel,
                    CompareColor = resolvedCompareColor,
                    Blocked = priorityScore < 0f,
                    RouteSync = hasRouteSync && routeBias > 0f,
                    RouteLabel = hasRouteSync ? routeReasonTag : string.Empty,
                    RouteBias = routeBias
                });
            }

            if (_recommendationCandidateBuffer.Count == 0)
            {
                ClearChoiceRecommendations();
                return;
            }

            _recommendationCandidateBuffer.Sort((left, right) => right.Score.CompareTo(left.Score));
            _markedRecommendationTargets.Clear();

            float bestScore = _recommendationCandidateBuffer[0].Score;
            Transform primaryRouteTarget = _recommendationCandidateBuffer[0].Target;
            float runnerUpScore = _recommendationCandidateBuffer.Count > 1
                ? _recommendationCandidateBuffer[1].Score
                : bestScore - Mathf.Max(4f, recommendationSecondaryScoreWindow * 0.5f);
            float primaryLead = Mathf.Max(0f, bestScore - runnerUpScore);
            float routeDominance = _recommendationCandidateBuffer[0].RouteSync
                ? Mathf.Clamp01(primaryLead / Mathf.Max(4f, recommendationSecondaryScoreWindow))
                : 0f;
            CachePrimaryRecommendation(_recommendationCandidateBuffer[0], routeDominance);
            bool primaryRouteFocused = primaryRouteTarget != null
                && (primaryRouteTarget == _activeTarget || IsArrivalAssistTarget(primaryRouteTarget));
            bool primaryRouteCommitReady = primaryRouteTarget != null
                && primaryRouteTarget == _activeTarget
                && _activeReady;
            float commitLaneStrength = _recommendationCandidateBuffer[0].RouteSync
                ? Mathf.Clamp01((routeDominance * 0.6f) + (primaryRouteFocused ? 0.18f : 0f) + (primaryRouteCommitReady ? 0.32f : 0f))
                : 0f;
            if (hasCarryPresentation && _recommendationCandidateBuffer[0].RouteSync)
            {
                commitLaneStrength = Mathf.Clamp01(commitLaneStrength + 0.12f);
            }
            bool suppressAlternateChoices = _recommendationCandidateBuffer[0].RouteSync
                && (primaryRouteFocused || primaryRouteCommitReady);
            float alternateSuppressionStrength = suppressAlternateChoices
                ? Mathf.Clamp01(0.3f + (routeDominance * 0.32f) + (primaryRouteCommitReady ? 0.18f : 0f))
                : 0f;
            int visibleCount = Mathf.Max(1, recommendationVisibleCount);
            int appliedCount = 0;

            for (int i = 0; i < _recommendationCandidateBuffer.Count && appliedCount < visibleCount; i++)
            {
                RecommendationCandidate candidate = _recommendationCandidateBuffer[i];
                if (candidate.Target == null)
                {
                    continue;
                }

                if (appliedCount > 0 && candidate.Blocked && bestScore >= 0f)
                {
                    continue;
                }

                if (appliedCount > 0 && (bestScore - candidate.Score) > recommendationSecondaryScoreWindow)
                {
                    continue;
                }

                bool focusedCandidate = candidate.Target == _activeTarget || IsArrivalAssistTarget(candidate.Target);
                bool commitReadyCandidate = candidate.Target == _activeTarget && _activeReady;
                bool primaryCandidate = candidate.Target == primaryRouteTarget;
                bool suppressedCandidate = suppressAlternateChoices
                    && candidate.Target != primaryRouteTarget
                    && !candidate.Blocked;
                float laneStrength = primaryCandidate && candidate.RouteSync
                    ? commitLaneStrength
                    : 0f;
                float suppressionStrength = suppressedCandidate
                    ? alternateSuppressionStrength
                    : 0f;
                RoomChoiceRecommendationPresentation presentation = EnsureChoiceRecommendationPresentation(candidate.Target);
                presentation?.SetRecommendation(
                    roomController.RoomType,
                    candidate.CompareColor,
                    ResolveRecommendationLabel(
                        candidate,
                        appliedCount,
                        focusedCandidate,
                        commitReadyCandidate,
                        primaryCandidate && hasCarryPresentation ? carryPresentationLabel : string.Empty),
                    appliedCount == 0,
                    candidate.Blocked,
                    candidate.RouteSync,
                    focusedCandidate,
                    commitReadyCandidate,
                    suppressedCandidate,
                    laneStrength,
                    suppressionStrength);
                _markedRecommendationTargets.Add(candidate.Target);
                appliedCount++;
            }

            for (int i = _visibleRecommendationTargets.Count - 1; i >= 0; i--)
            {
                Transform target = _visibleRecommendationTargets[i];
                if (target != null && !_markedRecommendationTargets.Contains(target))
                {
                    RoomChoiceRecommendationPresentation presentation = target.GetComponent<RoomChoiceRecommendationPresentation>();
                    presentation?.ClearRecommendation();
                }
            }

            _visibleRecommendationTargets.Clear();
            foreach (Transform markedTarget in _markedRecommendationTargets)
            {
                if (markedTarget != null)
                {
                    _visibleRecommendationTargets.Add(markedTarget);
                }
            }
        }

        private void ClearChoiceRecommendations()
        {
            for (int i = _visibleRecommendationTargets.Count - 1; i >= 0; i--)
            {
                Transform target = _visibleRecommendationTargets[i];
                if (target == null)
                {
                    continue;
                }

                RoomChoiceRecommendationPresentation presentation = target.GetComponent<RoomChoiceRecommendationPresentation>();
                presentation?.ClearRecommendation();
            }

            _visibleRecommendationTargets.Clear();
            _markedRecommendationTargets.Clear();
            _recommendationCandidateBuffer.Clear();
            ClearPrimaryRecommendation();
            _nextRecommendationRefreshAt = float.NegativeInfinity;
        }

        private void CollectRecommendationCandidates(List<Transform> targetBuffer)
        {
            if (targetBuffer == null)
            {
                return;
            }

            targetBuffer.Clear();

            switch (roomController != null ? roomController.RoomType : Data.Dungeon.RoomType.Normal)
            {
                case Data.Dungeon.RoomType.Treasure:
                    treasureRoomSpawner?.CollectTreasureChoiceTargets(targetBuffer);
                    break;
                case Data.Dungeon.RoomType.Shop:
                    if (roomTypeContentController != null
                        && roomTypeContentController.TryGetSpawnedShopInventory(out ShopInventory shopInventory)
                        && shopInventory != null)
                    {
                        shopInventory.CollectAvailableShopTargets(targetBuffer);
                    }

                    break;
            }
        }

        private static bool SupportsChoiceRecommendations(Data.Dungeon.RoomType roomType)
        {
            return roomType == Data.Dungeon.RoomType.Treasure
                || roomType == Data.Dungeon.RoomType.Shop;
        }

        private static string ResolveRecommendationLabel(RecommendationCandidate candidate, int visibleIndex, bool focused, bool commitReady, string carryPresentationLabel = "")
        {
            if (candidate.RouteSync && !candidate.Blocked && visibleIndex == 0)
            {
                if (commitReady)
                {
                    return "LOCK IN";
                }

                if (focused)
                {
                    return "COMMIT";
                }

                if (!string.IsNullOrWhiteSpace(carryPresentationLabel))
                {
                    return carryPresentationLabel;
                }

                if (IsShotProfileCompareLabel(candidate.CompareLabel))
                {
                    return candidate.CompareLabel;
                }

                return ResolveRouteSyncLabel(candidate.RouteLabel, candidate.CompareLabel);
            }

            if (!string.IsNullOrWhiteSpace(candidate.CompareLabel))
            {
                return candidate.CompareLabel;
            }

            if (candidate.Blocked)
            {
                return "LOCKED";
            }

            return visibleIndex == 0 ? "BEST PICK" : "ALT PICK";
        }

        private static string ResolveRouteSyncLabel(string routeReasonTag, string fallbackLabel)
        {
            if (IsShotProfileCompareLabel(fallbackLabel))
            {
                return fallbackLabel;
            }

            return routeReasonTag switch
            {
                "PRESS ADVANTAGE" => "ADV ROUTE",
                "RECOVERY ONLINE" => "HEAL ROUTE",
                "CASH WINDOW" => "CASH ROUTE",
                "KEY WINDOW" => "KEY ROUTE",
                "BOMB LINE" => "BOMB ROUTE",
                "PATCH HP" => "HEAL ROUTE",
                "POWER SPIKE" => "SPIKE ROUTE",
                "LOADOUT FIND" => "BUILD ROUTE",
                "CASH OUT" => "SHOP ROUTE",
                "FILL SLOT" => "FILL ROUTE",
                _ => !string.IsNullOrWhiteSpace(fallbackLabel) ? fallbackLabel : "ROUTE SYNC"
            };
        }

        private void ResolveReferences()
        {
            if (roomController == null)
            {
                roomController = GetComponent<RoomController>();
            }

            if (playerController == null)
            {
                playerController = FindFirstObjectByType<PlayerController>(FindObjectsInactive.Exclude);
            }

            if (playerInventory == null && playerController != null)
            {
                playerInventory = playerController.GetComponent<PlayerInventory>();
            }

            if (playerItemManager == null && playerController != null)
            {
                playerItemManager = playerController.GetComponent<PlayerItemManager>();
            }

            if (playerHealth == null && playerController != null)
            {
                playerHealth = playerController.GetComponent<PlayerHealth>();
            }

            if (playerStats == null && playerController != null)
            {
                playerStats = playerController.GetComponent<PlayerStats>();
            }

            if (playerTrinketHolder == null && playerController != null)
            {
                playerTrinketHolder = playerController.GetComponent<PlayerTrinketHolder>();
            }

            if (playerActiveItemController == null && playerController != null)
            {
                playerActiveItemController = playerController.GetComponent<PlayerActiveItemController>();
            }

            if (playerConsumableHolder == null && playerController != null)
            {
                playerConsumableHolder = playerController.GetComponent<PlayerConsumableHolder>();
            }

            if (treasureRoomSpawner == null)
            {
                treasureRoomSpawner = GetComponent<TreasureRoomSpawner>();
            }

            if (roomTypeContentController == null)
            {
                roomTypeContentController = GetComponent<RoomTypeContentController>();
            }
        }

        private void EnsureRoomSignalsBound()
        {
            if (roomTypeContentController == null)
            {
                UnbindRoomSignals();
                return;
            }

            if (!roomTypeContentController.TryGetSpawnedShopInventory(out ShopInventory shopInventory))
            {
                return;
            }

            if (_boundShopInventory == shopInventory)
            {
                return;
            }

            UnbindRoomSignals();
            _boundShopInventory = shopInventory;
            _boundShopInventory.PurchaseAttemptResolved += HandleShopPurchaseAttemptResolved;
        }

        private void UnbindRoomSignals()
        {
            if (_boundShopInventory != null)
            {
                _boundShopInventory.PurchaseAttemptResolved -= HandleShopPurchaseAttemptResolved;
                _boundShopInventory = null;
            }
        }

        private void BindTargetSignals(Transform target)
        {
            if (target == null)
            {
                return;
            }

            _activePickupLogic = target.GetComponent<BasePickupLogic>();

            if (_activePickupLogic == null)
            {
                _activePickupLogic = target.GetComponentInChildren<BasePickupLogic>(true);
            }

            if (_activePickupLogic != null)
            {
                _activePickupLogic.Collected += HandleActivePickupCollected;
            }
        }

        private void UnbindTargetSignals()
        {
            if (_activePickupLogic != null)
            {
                _activePickupLogic.Collected -= HandleActivePickupCollected;
                _activePickupLogic = null;
            }
        }

        private void HandleActivePickupCollected(BasePickupLogic collectedPickup)
        {
            if (collectedPickup == null)
            {
                return;
            }

            bool hasRouteResolution = TryResolveRouteChoiceResolution(collectedPickup.transform, out RouteChoiceResolution routeResolution);
            bool hasPlanCarryResolution = TryResolvePlanCarryResolution(collectedPickup.transform, out PlanCarryResolution planCarryResolution);
            Color pickupAccent = ResolvePickupReceiptAccent(collectedPickup);

            RoomInteractionAffordancePresentation presentation = EnsurePresentation(collectedPickup.transform);
            presentation?.TriggerResolutionFeedback(true, 1f);
            EnsurePlayerReceiptPresentation()?.PlayReceipt(
                collectedPickup.transform.position,
                pickupAccent,
                ResolvePickupReceiptLabel(collectedPickup),
                success: true,
                emphasize: ResolvePickupReceiptEmphasis(collectedPickup),
                detailLabel: hasRouteResolution ? routeResolution.DetailLabel : ResolvePickupReceiptContextLabel(collectedPickup),
                detailColor: hasRouteResolution
                    ? routeResolution.AccentColor
                    : Color.Lerp(
                        RoomTraversalGuidanceController.ResolveRoomAccent(roomController != null ? roomController.RoomType : Data.Dungeon.RoomType.Normal),
                        Color.white,
                        0.16f));
            PrimePreviewDeltaBridge(
                collectedPickup.transform,
                ResolvePickupReceiptLabel(collectedPickup),
                _livePreviewOutcomeLabel,
                pickupAccent,
                _livePreviewOutcomeColor.a > 0.01f ? _livePreviewOutcomeColor : pickupAccent,
                ResolvePickupReceiptEmphasis(collectedPickup) || (hasRouteResolution && routeResolution.Emphasize),
                hasRouteResolution ? routeResolution.Headline : "LOCKED IN",
                hasRouteResolution ? routeResolution.CompareLabel : "CONFIRMED",
                hasRouteResolution ? routeResolution.AccentColor : Color.Lerp(pickupAccent, Color.white, 0.18f));
            if (hasRouteResolution)
            {
                RaiseChoiceRouteResolution(routeResolution, hasPlanCarryResolution ? planCarryResolution : default);
                ResolveChoiceRouteSyncAfterSelection();
            }
            else if (hasPlanCarryResolution)
            {
                ClearPlanCarryPresentation();
            }
            RaisePickupInteractionOutcome(collectedPickup);
            _nextRecommendationRefreshAt = float.NegativeInfinity;
        }

        private void HandleShopPurchaseAttemptResolved(ShopItem shopItem, bool purchased)
        {
            if (shopItem == null)
            {
                return;
            }

            RouteChoiceResolution routeResolution = default;
            PlanCarryResolution planCarryResolution = default;
            bool hasRouteResolution = purchased && TryResolveRouteChoiceResolution(shopItem.transform, out routeResolution);
            bool hasPlanCarryResolution = purchased && TryResolvePlanCarryResolution(shopItem.transform, out planCarryResolution);
            Color successAccent = ResolveShopRewardAccent(shopItem);
            Color failureAccent = ResolveShopFailureAccent(shopItem);

            RoomInteractionAffordancePresentation presentation = EnsurePresentation(shopItem.transform);
            presentation?.TriggerResolutionFeedback(purchased, purchased ? 1f : 0.86f);
            EnsurePlayerReceiptPresentation()?.PlayReceipt(
                shopItem.transform.position,
                purchased ? successAccent : failureAccent,
                purchased ? ResolveShopSuccessReceiptLabel(shopItem) : ResolveShopFailureReceiptLabel(shopItem),
                purchased,
                purchased && ResolveShopReceiptEmphasis(shopItem),
                detailLabel: purchased
                    ? hasRouteResolution ? routeResolution.DetailLabel : ResolveShopCostReceiptLabel(shopItem)
                    : ResolveShopAttemptDetailLabel(shopItem),
                detailColor: purchased
                    ? hasRouteResolution ? routeResolution.AccentColor : ResolveShopCurrencyAccent(shopItem.CurrencyType)
                    : ResolveShopCurrencyAccent(shopItem.CurrencyType));
            if (purchased)
            {
                PrimePreviewDeltaBridge(
                    shopItem.transform,
                    ResolveShopSuccessReceiptLabel(shopItem),
                    _livePreviewOutcomeLabel,
                    successAccent,
                    _livePreviewOutcomeColor.a > 0.01f ? _livePreviewOutcomeColor : successAccent,
                    ResolveShopReceiptEmphasis(shopItem) || (hasRouteResolution && routeResolution.Emphasize),
                    hasRouteResolution ? routeResolution.Headline : "LOCKED IN",
                    hasRouteResolution ? routeResolution.CompareLabel : "CONFIRMED",
                    hasRouteResolution ? routeResolution.AccentColor : Color.Lerp(successAccent, Color.white, 0.18f));
                if (hasRouteResolution)
                {
                    RaiseChoiceRouteResolution(routeResolution, hasPlanCarryResolution ? planCarryResolution : default);
                    ResolveChoiceRouteSyncAfterSelection();
                }
                else if (hasPlanCarryResolution)
                {
                    ClearPlanCarryPresentation();
                }
                RaiseShopInteractionOutcome(shopItem);
            }
            else
            {
                ClearPendingPreviewDeltaBridge();
            }
            _nextRecommendationRefreshAt = float.NegativeInfinity;
        }

        private PlayerInteractionReceiptPresentation EnsurePlayerReceiptPresentation()
        {
            if (playerController == null)
            {
                return null;
            }

            if (_playerReceiptPresentation != null && _playerReceiptPresentation.gameObject != playerController.gameObject)
            {
                _playerReceiptPresentation = null;
            }

            if (_playerReceiptPresentation == null)
            {
                _playerReceiptPresentation = playerController.GetComponent<PlayerInteractionReceiptPresentation>();
            }

            if (_playerReceiptPresentation == null)
            {
                _playerReceiptPresentation = playerController.gameObject.AddComponent<PlayerInteractionReceiptPresentation>();
            }

            return _playerReceiptPresentation;
        }

        private PlayerInteractionPreviewPresentation EnsurePlayerPreviewPresentation()
        {
            if (playerController == null)
            {
                return null;
            }

            if (_playerPreviewPresentation != null && _playerPreviewPresentation.gameObject != playerController.gameObject)
            {
                _playerPreviewPresentation = null;
            }

            if (_playerPreviewPresentation == null)
            {
                _playerPreviewPresentation = playerController.GetComponent<PlayerInteractionPreviewPresentation>();
            }

            if (_playerPreviewPresentation == null)
            {
                _playerPreviewPresentation = playerController.gameObject.AddComponent<PlayerInteractionPreviewPresentation>();
            }

            return _playerPreviewPresentation;
        }

        private void UpdatePlayerPreview(Transform target, bool strong, bool ready)
        {
            PlayerInteractionPreviewPresentation previewPresentation = EnsurePlayerPreviewPresentation();

            if (previewPresentation == null)
            {
                return;
            }

            if (TryPresentPreviewResolution(previewPresentation))
            {
                return;
            }

            if (target == null
                || !RoomInteractionPreviewResolver.TryResolve(
                    target,
                    roomController,
                    playerInventory,
                    playerItemManager,
                    playerHealth,
                    playerStats,
                    playerTrinketHolder,
                    playerActiveItemController,
                    playerConsumableHolder,
                    out string title,
                    out string detail,
                    out Color accentColor,
                    out string compareLabel,
                    out Color compareColor,
                    out bool emphasize))
            {
                ClearLivePreviewState();
                previewPresentation.ClearPreview();
                return;
            }

            RoomInteractionPreviewResolver.TryResolveOutcomePreview(
                target,
                playerInventory,
                playerItemManager,
                playerHealth,
                playerStats,
                out string outcomeLabel,
                out Color outcomeColor);

            if (IsArrivalAssistTarget(target))
            {
                if (TryGetActiveArrivalAssistCarryPresentation(out string carryLabel, out Color carryColor))
                {
                    compareLabel = carryLabel;
                    compareColor = carryColor;
                }
                else if (!string.IsNullOrWhiteSpace(_arrivalAssistCompareLabel))
                {
                    compareLabel = _arrivalAssistCompareLabel;
                    compareColor = _arrivalAssistCompareColor;
                }

                emphasize = true;
                strong = true;
            }

            CacheLivePreview(target, title, compareLabel, outcomeLabel, accentColor, compareColor, outcomeColor);
            previewPresentation.SetPreview(title, detail, compareLabel, outcomeLabel, accentColor, compareColor, outcomeColor, strong, ready, emphasize);
        }

        private bool TryPresentPreviewResolution(PlayerInteractionPreviewPresentation previewPresentation)
        {
            if (previewPresentation == null)
            {
                return false;
            }

            if (!HasActivePreviewResolution())
            {
                if (Time.unscaledTime > _previewResolutionExpiresAt)
                {
                    ClearPreviewResolution();
                }

                return false;
            }

            previewPresentation.SetPreview(
                _previewResolutionTitle,
                _previewResolutionDetail,
                _previewResolutionCompareLabel,
                _previewResolutionOutcomeLabel,
                _previewResolutionAccentColor,
                _previewResolutionCompareColor,
                _previewResolutionOutcomeColor,
                strong: true,
                ready: true,
                emphasize: _previewResolutionEmphasize);
            return true;
        }

        private bool HasActivePreviewResolution()
        {
            return Time.unscaledTime <= _previewResolutionExpiresAt
                && !string.IsNullOrWhiteSpace(_previewResolutionTitle);
        }

        private void CacheLivePreview(
            Transform target,
            string title,
            string compareLabel,
            string outcomeLabel,
            Color accentColor,
            Color compareColor,
            Color outcomeColor)
        {
            _livePreviewTarget = target;
            _livePreviewTitle = title ?? string.Empty;
            _livePreviewCompareLabel = compareLabel ?? string.Empty;
            _livePreviewOutcomeLabel = outcomeLabel ?? string.Empty;
            _livePreviewAccentColor = accentColor;
            _livePreviewCompareColor = compareColor;
            _livePreviewOutcomeColor = outcomeColor;
        }

        private void ClearLivePreviewState()
        {
            _livePreviewTarget = null;
            _livePreviewTitle = string.Empty;
            _livePreviewCompareLabel = string.Empty;
            _livePreviewOutcomeLabel = string.Empty;
            _livePreviewAccentColor = Color.white;
            _livePreviewCompareColor = Color.white;
            _livePreviewOutcomeColor = Color.white;
        }

        private void PlayPreviewResolution(
            string title,
            string detail,
            string compareLabel,
            string outcomeLabel,
            Color accentColor,
            Color compareColor,
            Color outcomeColor,
            bool emphasize,
            float duration)
        {
            _previewResolutionTitle = title ?? string.Empty;
            _previewResolutionDetail = detail ?? string.Empty;
            _previewResolutionCompareLabel = compareLabel ?? string.Empty;
            _previewResolutionOutcomeLabel = outcomeLabel ?? string.Empty;
            _previewResolutionAccentColor = accentColor.a > 0.01f ? accentColor : Color.white;
            _previewResolutionCompareColor = compareColor.a > 0.01f ? compareColor : Color.Lerp(_previewResolutionAccentColor, Color.white, 0.18f);
            _previewResolutionOutcomeColor = outcomeColor.a > 0.01f ? outcomeColor : _previewResolutionAccentColor;
            _previewResolutionEmphasize = emphasize;
            _previewResolutionExpiresAt = Time.unscaledTime + Mathf.Max(0.1f, duration);
        }

        private void ClearPreviewResolution()
        {
            _previewResolutionExpiresAt = float.NegativeInfinity;
            _previewResolutionTitle = string.Empty;
            _previewResolutionDetail = string.Empty;
            _previewResolutionCompareLabel = string.Empty;
            _previewResolutionOutcomeLabel = string.Empty;
            _previewResolutionAccentColor = Color.white;
            _previewResolutionCompareColor = Color.white;
            _previewResolutionOutcomeColor = Color.white;
            _previewResolutionEmphasize = false;
        }

        private void PrimePreviewDeltaBridge(
            Transform target,
            string fallbackTitle,
            string fallbackOutcomeLabel,
            Color accentColor,
            Color outcomeColor,
            bool emphasize,
            string bridgeTitle = "LOCKED IN",
            string bridgeCompareLabel = "CONFIRMED",
            Color? bridgeCompareColor = null)
        {
            bool useLivePreview = target != null && target == _livePreviewTarget;
            string sourceTitle = useLivePreview && !string.IsNullOrWhiteSpace(_livePreviewTitle)
                ? _livePreviewTitle
                : fallbackTitle ?? string.Empty;
            string sourceOutcome = useLivePreview && !string.IsNullOrWhiteSpace(_livePreviewOutcomeLabel)
                ? _livePreviewOutcomeLabel
                : fallbackOutcomeLabel ?? string.Empty;
            Color resolvedOutcomeColor = useLivePreview && _livePreviewOutcomeColor.a > 0.01f
                ? _livePreviewOutcomeColor
                : outcomeColor;

            _pendingPreviewSourceTitle = sourceTitle;
            _pendingPreviewFallbackOutcome = sourceOutcome;
            _pendingPreviewFallbackOutcomeColor = resolvedOutcomeColor.a > 0.01f ? resolvedOutcomeColor : accentColor;
            _pendingPreviewEmphasize = emphasize;
            _pendingPreviewDeltaBridgeExpiresAt = Time.unscaledTime + Mathf.Max(previewDeltaBridgeWindow, previewResolutionDuration);

            PlayPreviewResolution(
                string.IsNullOrWhiteSpace(bridgeTitle) ? "LOCKED IN" : bridgeTitle,
                sourceTitle,
                string.IsNullOrWhiteSpace(bridgeCompareLabel) ? "CONFIRMED" : bridgeCompareLabel,
                sourceOutcome,
                accentColor,
                bridgeCompareColor ?? Color.Lerp(accentColor, Color.white, 0.18f),
                _pendingPreviewFallbackOutcomeColor,
                emphasize,
                previewResolutionDuration);
        }

        private void CachePrimaryRecommendation(RecommendationCandidate candidate, float routeDominance)
        {
            if (candidate.Target == null || candidate.Blocked || !candidate.RouteSync)
            {
                ClearPrimaryRecommendation();
                return;
            }

            _primaryRecommendationTarget = candidate.Target;
            _primaryRecommendationCompareLabel = candidate.CompareLabel ?? string.Empty;
            _primaryRecommendationRouteReasonTag = candidate.RouteLabel ?? string.Empty;
            _primaryRecommendationAccentColor = candidate.CompareColor.a > 0.01f
                ? new Color(candidate.CompareColor.r, candidate.CompareColor.g, candidate.CompareColor.b, 1f)
                : RoomTraversalGuidanceController.ResolveRoomAccent(roomController != null ? roomController.RoomType : Data.Dungeon.RoomType.Normal);
            _primaryRecommendationDominance = Mathf.Clamp01(routeDominance);
        }

        private void ClearPrimaryRecommendation()
        {
            _primaryRecommendationTarget = null;
            _primaryRecommendationCompareLabel = string.Empty;
            _primaryRecommendationRouteReasonTag = string.Empty;
            _primaryRecommendationAccentColor = Color.white;
            _primaryRecommendationDominance = 0f;
        }

        private bool TryResolveRouteChoiceResolution(Transform selectedTarget, out RouteChoiceResolution resolution)
        {
            resolution = default;
            if (selectedTarget == null
                || roomController == null
                || !roomController.IsCurrentRoom
                || !SupportsChoiceRecommendations(roomController.RoomType)
                || Time.unscaledTime > _routeRecommendationExpiresAt
                || _primaryRecommendationTarget == null
                || string.IsNullOrWhiteSpace(_primaryRecommendationRouteReasonTag))
            {
                return false;
            }

            string routeLabel = ResolveRouteSyncLabel(_primaryRecommendationRouteReasonTag, _primaryRecommendationCompareLabel);
            Color routeAccent = _primaryRecommendationAccentColor.a > 0.01f
                ? _primaryRecommendationAccentColor
                : RoomTraversalGuidanceController.ResolveRoomAccent(roomController.RoomType);
            bool heldRoute = selectedTarget == _primaryRecommendationTarget;
            string heldIntentReasonTag = NormalizeRouteIntentReason(_primaryRecommendationRouteReasonTag, routeLabel);

            if (heldRoute)
            {
                resolution = new RouteChoiceResolution(
                    "ROUTE HELD",
                    routeLabel,
                    routeLabel,
                    heldIntentReasonTag,
                    routeAccent,
                    true,
                    Mathf.Lerp(0.48f, 1f, _primaryRecommendationDominance),
                    _primaryRecommendationDominance >= 0.35f);
                return true;
            }

            string alternateLabel = ResolveAlternateChoiceRouteLabel(selectedTarget);
            Color alternateAccent = ResolveAlternateChoiceRouteAccent(selectedTarget, routeAccent);
            string rerouteIntentReasonTag = NormalizeRouteIntentReason(string.Empty, alternateLabel);
            resolution = new RouteChoiceResolution(
                "ROUTE BREAK",
                "REROUTE",
                alternateLabel,
                rerouteIntentReasonTag,
                alternateAccent,
                false,
                Mathf.Lerp(0.24f, 0.68f, _primaryRecommendationDominance),
                _primaryRecommendationDominance >= 0.55f);
            return true;
        }

        private bool TryResolvePlanCarryResolution(Transform selectedTarget, out PlanCarryResolution resolution)
        {
            resolution = default;
            Color carryAccent = Color.white;
            if (selectedTarget == null
                || roomController == null
                || !roomController.IsCurrentRoom
                || !SupportsChoiceRecommendations(roomController.RoomType)
                || !TryGetActiveArrivalAssistCarryPresentation(out _, out carryAccent))
            {
                return false;
            }

            Transform expectedTarget = _arrivalAssistTarget != null
                ? _arrivalAssistTarget
                : _primaryRecommendationTarget;
            if (expectedTarget == null)
            {
                return false;
            }

            bool heldPlan = selectedTarget == expectedTarget || selectedTarget == _primaryRecommendationTarget;
            string detailLabel = heldPlan
                ? ResolvePlanCarryHeldDetail(selectedTarget)
                : ResolveAlternateChoiceRouteLabel(selectedTarget);
            Color accentColor = heldPlan
                ? ResolvePlanCarryHeldAccent(carryAccent)
                : ResolveAlternateChoiceRouteAccent(selectedTarget, carryAccent);
            bool emphasize = heldPlan
                ? _primaryRecommendationDominance >= 0.28f || _activeStrong
                : _primaryRecommendationDominance >= 0.42f;

            resolution = new PlanCarryResolution(
                heldPlan ? "PLAN LOCKED" : "PLAN PIVOT",
                detailLabel,
                accentColor,
                heldPlan,
                emphasize);
            return true;
        }

        private string ResolvePlanCarryHeldDetail(Transform selectedTarget)
        {
            if (selectedTarget != null
                && RoomInteractionPreviewResolver.TryResolvePriority(
                    selectedTarget,
                    roomController,
                    playerInventory,
                    playerItemManager,
                    playerHealth,
                    playerStats,
                    playerTrinketHolder,
                    playerActiveItemController,
                    playerConsumableHolder,
                    out _,
                    out string compareLabel,
                    out _)
                && !string.IsNullOrWhiteSpace(compareLabel))
            {
                return compareLabel;
            }

            string routeLabel = ResolveRouteSyncLabel(_primaryRecommendationRouteReasonTag, _primaryRecommendationCompareLabel);
            return !string.IsNullOrWhiteSpace(routeLabel) ? routeLabel : "GUIDED PICK";
        }

        private Color ResolvePlanCarryHeldAccent(Color fallbackAccent)
        {
            if (_primaryRecommendationAccentColor.a > 0.01f)
            {
                return _primaryRecommendationAccentColor;
            }

            if (_arrivalAssistCompareColor.a > 0.01f)
            {
                return _arrivalAssistCompareColor;
            }

            return fallbackAccent.a > 0.01f
                ? fallbackAccent
                : RoomTraversalGuidanceController.ResolveRoomAccent(roomController != null ? roomController.RoomType : Data.Dungeon.RoomType.Normal);
        }

        private void RaiseChoiceRouteResolution(RouteChoiceResolution resolution, PlanCarryResolution planCarryResolution = default)
        {
            if (!resolution.IsValid || roomController == null)
            {
                return;
            }

            GameplayRuntimeEvents.RaiseChoiceRouteResolved(new ChoiceRouteResolvedSignal(
                roomController,
                resolution.Headline,
                resolution.DetailLabel,
                resolution.CompareLabel,
                resolution.IntentReasonTag,
                resolution.AccentColor,
                resolution.HeldRoute,
                resolution.Strength,
                resolution.Emphasize,
                planCarryResolution.IsValid ? planCarryResolution.Headline : string.Empty,
                planCarryResolution.IsValid ? planCarryResolution.DetailLabel : string.Empty,
                planCarryResolution.IsValid && planCarryResolution.HeldPlan));
        }

        private void ResolveChoiceRouteSyncAfterSelection()
        {
            ClearArrivalAssist();
            ClearRouteRecommendationSync();
            ClearChoiceRecommendations();
        }

        private void ClearPlanCarryPresentation()
        {
            ClearArrivalAssistCarryPresentation();
            ClearRouteRecommendationCarryPresentation();
        }

        private string ResolveAlternateChoiceRouteLabel(Transform selectedTarget)
        {
            if (selectedTarget == null)
            {
                return "ALT LINE";
            }

            if (RoomInteractionPreviewResolver.TryResolvePriority(
                    selectedTarget,
                    roomController,
                    playerInventory,
                    playerItemManager,
                    playerHealth,
                    playerStats,
                    playerTrinketHolder,
                    playerActiveItemController,
                    playerConsumableHolder,
                    out _,
                    out string compareLabel,
                    out _)
                && !string.IsNullOrWhiteSpace(compareLabel))
            {
                return compareLabel;
            }

            return "ALT LINE";
        }

        private Color ResolveAlternateChoiceRouteAccent(Transform selectedTarget, Color fallbackAccent)
        {
            if (selectedTarget != null
                && RoomInteractionPreviewResolver.TryResolvePriority(
                    selectedTarget,
                    roomController,
                    playerInventory,
                    playerItemManager,
                    playerHealth,
                    playerStats,
                    playerTrinketHolder,
                    playerActiveItemController,
                    playerConsumableHolder,
                    out _,
                    out _,
                    out Color compareColor)
                && compareColor.a > 0.01f)
            {
                return Color.Lerp(compareColor, fallbackAccent, 0.18f);
            }

            return Color.Lerp(fallbackAccent, new Color(1f, 0.58f, 0.44f, 1f), 0.34f);
        }

        private static string NormalizeRouteIntentReason(string routeReasonTag, string compareLabel)
        {
            switch (routeReasonTag)
            {
                case "PRESS ADVANTAGE":
                case "POWER SPIKE":
                case "SPIKE ROUTE":
                case "ADV ROUTE":
                    return "PRESS ADVANTAGE";
                case "RECOVERY ONLINE":
                case "PATCH HP":
                case "HEAL ROUTE":
                    return "RECOVERY ONLINE";
                case "CASH WINDOW":
                case "CASH OUT":
                case "CASH ROUTE":
                    return "CASH WINDOW";
                case "KEY WINDOW":
                case "LOOK FOR KEYS":
                case "KEY ROUTE":
                    return "KEY WINDOW";
                case "BOMB LINE":
                case "RESTOCK BOMBS":
                case "BOMB ROUTE":
                    return "BOMB LINE";
                case "LOADOUT FIND":
                case "BUILD ROUTE":
                case "FILL SLOT":
                    return "LOADOUT FIND";
                case "SAFE UPGRADE":
                    return "SAFE UPGRADE";
            }

            return ResolveIntentReasonFromCompareLabel(compareLabel);
        }

        private static string ResolveIntentReasonFromCompareLabel(string compareLabel)
        {
            if (IsShotProfileCompareLabel(compareLabel))
            {
                return compareLabel.Contains("SHIELD") || compareLabel.Contains("ORBIT")
                    ? "LOADOUT FIND"
                    : "PRESS ADVANTAGE";
            }

            return compareLabel switch
            {
                "DMG SPIKE" or "SHOT SPIKE" or "EXTRA SHOT" or "DMG UP" or "SHOT UP" or "PIERCE" or "HOMING" or "EXPLOSIVE" or "LASER SHIFT" or "SPLIT SHOT" or "RICOCHET" or "LIFESTEAL" => "PRESS ADVANTAGE",
                "FULL HEAL" or "CLUTCH HEAL" or "HEAL NOW" or "HP SPIKE" or "HP UP" or "FULL HP" => "RECOVERY ONLINE",
                "KEY RELIEF" or "KEY UP" => "KEY WINDOW",
                "BOMB RELIEF" or "BOMB UP" => "BOMB LINE",
                "NO WEAPON" => "LOADOUT FIND",
                "AMMO NOW" or "RESTOCK AMMO" or "AMMO FULL" => "CASH WINDOW",
                "SHOP FUEL" => "CASH WINDOW",
                "ACTIVE OPEN" or "TRINKET OPEN" or "ACTIVE SWAP" or "TRINKET SWAP" or "BUILD SHIFT" or "SHOT SHIFT" or "SUMMON TECH" or "ORBITAL" => "LOADOUT FIND",
                "SPEED SPIKE" or "SPEED UP" or "SPEED HEART" => "SAFE UPGRADE",
                _ => string.Empty
            };
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

        private void ClearPendingPreviewDeltaBridge()
        {
            _pendingPreviewDeltaBridgeExpiresAt = float.NegativeInfinity;
            _pendingPreviewSourceTitle = string.Empty;
            _pendingPreviewFallbackOutcome = string.Empty;
            _pendingPreviewFallbackOutcomeColor = Color.white;
            _pendingPreviewEmphasize = false;
        }

        private void HandlePlayerLoadoutDelta(PlayerLoadoutDeltaSignal signal)
        {
            if (!signal.IsValid
                || roomController == null
                || !roomController.IsCurrentRoom
                || playerController == null
                || Time.unscaledTime > _pendingPreviewDeltaBridgeExpiresAt)
            {
                return;
            }

            string resolvedOutcome = CondensePreviewResolutionText(signal.Detail, _pendingPreviewFallbackOutcome);
            PlayPreviewResolution(
                signal.Headline,
                _pendingPreviewSourceTitle,
                "DELTA LIVE",
                resolvedOutcome,
                signal.AccentColor,
                Color.Lerp(signal.AccentColor, Color.white, 0.18f),
                signal.AccentColor,
                signal.Emphasize || _pendingPreviewEmphasize,
                previewResolutionDuration + (signal.Emphasize ? 0.18f : 0.08f));
            ClearPendingPreviewDeltaBridge();
        }

        private void HandlePlayerInteractionOutcome(PlayerInteractionOutcomeSignal signal)
        {
            if (!signal.IsValid
                || roomController == null
                || !roomController.IsCurrentRoom
                || playerController == null
                || Time.unscaledTime > _pendingPreviewDeltaBridgeExpiresAt)
            {
                return;
            }

            string resolvedOutcome = CondensePreviewResolutionText(signal.Detail, _pendingPreviewFallbackOutcome);
            PlayPreviewResolution(
                signal.Headline,
                _pendingPreviewSourceTitle,
                "OUTCOME LIVE",
                resolvedOutcome,
                signal.AccentColor,
                Color.Lerp(signal.AccentColor, Color.white, 0.18f),
                signal.AccentColor,
                signal.Emphasize || _pendingPreviewEmphasize,
                previewResolutionDuration + (signal.Emphasize ? 0.12f : 0.04f));
        }

        private void RaisePickupInteractionOutcome(BasePickupLogic pickupLogic)
        {
            if (!TryBuildPickupInteractionOutcomeSignal(pickupLogic, out PlayerInteractionOutcomeSignal signal))
            {
                return;
            }

            GameplayRuntimeEvents.RaisePlayerInteractionOutcome(signal);
        }

        private bool TryBuildPickupInteractionOutcomeSignal(BasePickupLogic pickupLogic, out PlayerInteractionOutcomeSignal signal)
        {
            signal = default;
            if (pickupLogic == null)
            {
                return false;
            }

            RoomInteractionPreviewResolver.TryResolveOutcomePreview(
                pickupLogic.transform,
                playerInventory,
                playerItemManager,
                playerHealth,
                playerStats,
                out string outcomeLabel,
                out Color outcomeColor);

            string headline = pickupLogic switch
            {
                HeartPickupLogic => "HEAL CONFIRMED",
                ResourcePickupLogic => "SUPPLY CONFIRMED",
                ConsumablePickupLogic consumablePickup when consumablePickup.ConsumableItemData != null
                    && consumablePickup.ConsumableItemData.PickupMode == Data.Item.ConsumablePickupMode.StoreInHolder => "STASH CONFIRMED",
                ConsumablePickupLogic => "SUPPLY CONFIRMED",
                ActiveItemPickupLogic => "ACTIVE LOCKED",
                TrinketPickupLogic => "TRINKET LOCKED",
                ItemPickupLogic => "LOADOUT LOCKED",
                _ => "PICKUP CONFIRMED"
            };

            Color accent = outcomeColor.a > 0.01f
                ? outcomeColor
                : ResolvePickupReceiptAccent(pickupLogic);

            signal = new PlayerInteractionOutcomeSignal(
                pickupLogic.transform.position,
                headline,
                outcomeLabel,
                accent,
                ResolvePickupReceiptEmphasis(pickupLogic));
            return true;
        }

        private void RaiseShopInteractionOutcome(ShopItem shopItem)
        {
            if (!TryBuildShopInteractionOutcomeSignal(shopItem, out PlayerInteractionOutcomeSignal signal))
            {
                return;
            }

            GameplayRuntimeEvents.RaisePlayerInteractionOutcome(signal);
        }

        private bool TryBuildShopInteractionOutcomeSignal(ShopItem shopItem, out PlayerInteractionOutcomeSignal signal)
        {
            signal = default;
            if (shopItem?.ShopItemData == null)
            {
                return false;
            }

            RoomInteractionPreviewResolver.TryResolveOutcomePreview(
                shopItem.transform,
                playerInventory,
                playerItemManager,
                playerHealth,
                playerStats,
                out string outcomeLabel,
                out Color outcomeColor);

            string costLabel = ResolveShopCostReceiptLabel(shopItem);
            string detail = string.IsNullOrWhiteSpace(outcomeLabel)
                ? costLabel
                : string.IsNullOrWhiteSpace(costLabel)
                    ? outcomeLabel
                    : $"{outcomeLabel} / {costLabel}";

            string headline = shopItem.ShopItemData.Offer.RewardType switch
            {
                ShopOfferRewardType.Health => "HEAL CONFIRMED",
                ShopOfferRewardType.Ammo => "AMMO CONFIRMED",
                ShopOfferRewardType.Coins or ShopOfferRewardType.Keys or ShopOfferRewardType.Bombs => "SUPPLY CONFIRMED",
                ShopOfferRewardType.PassiveItem => "PURCHASE CONFIRMED",
                _ => "PURCHASE CONFIRMED"
            };

            Color accent = outcomeColor.a > 0.01f
                ? outcomeColor
                : ResolveShopRewardAccent(shopItem);

            signal = new PlayerInteractionOutcomeSignal(
                shopItem.transform.position,
                headline,
                detail,
                accent,
                ResolveShopReceiptEmphasis(shopItem));
            return true;
        }

        private static string CondensePreviewResolutionText(string detail, string fallback)
        {
            string source = !string.IsNullOrWhiteSpace(detail) ? detail : fallback;
            if (string.IsNullOrWhiteSpace(source))
            {
                return string.Empty;
            }

            string[] parts = source.Split(" / ");
            if (parts.Length <= 2)
            {
                return source;
            }

            return $"{parts[0]} / {parts[1]}";
        }

        private void CollectArrivalAssistCandidates(List<Transform> targetBuffer)
        {
            if (targetBuffer == null)
            {
                return;
            }

            targetBuffer.Clear();
            roomTypeContentController?.CollectEntryHighlightTargets(targetBuffer);
            if (targetBuffer.Count > 0)
            {
                return;
            }

            treasureRoomSpawner?.CollectTreasureChoiceTargets(targetBuffer);
            if (targetBuffer.Count > 0)
            {
                return;
            }

            if (roomTypeContentController != null
                && roomTypeContentController.TryGetSpawnedShopInventory(out ShopInventory shopInventory)
                && shopInventory != null)
            {
                shopInventory.CollectAvailableShopTargets(targetBuffer);
            }

            if (targetBuffer.Count > 0)
            {
                return;
            }

            Transform fallbackTarget = ResolveFallbackTargetTransform();
            if (fallbackTarget != null)
            {
                targetBuffer.Add(fallbackTarget);
            }
        }

        private float ResolveArrivalAssistReasonBias(Transform candidate, string routeReasonTag, string compareLabel)
        {
            if (candidate == null || string.IsNullOrWhiteSpace(routeReasonTag))
            {
                return 0f;
            }

            return routeReasonTag switch
            {
                "PATCH HP" => ResolvePatchHealthBias(candidate, compareLabel),
                "LOOK FOR KEYS" => compareLabel is "KEY RELIEF" or "KEY UP" ? 18f : 0f,
                "RESTOCK BOMBS" => compareLabel is "BOMB RELIEF" or "BOMB UP" ? 18f : 0f,
                "POWER SPIKE" => ResolvePowerSpikeBias(candidate, compareLabel),
                "PRESS ADVANTAGE" => Mathf.Max(ResolvePowerSpikeBias(candidate, compareLabel) + 2f, ResolveLoadoutBias(candidate, compareLabel)),
                "SAFE UPGRADE" => ResolveSafeUpgradeBias(candidate, compareLabel),
                "RECOVERY ONLINE" => Mathf.Max(ResolveSafeUpgradeBias(candidate, compareLabel), ResolvePatchHealthBias(candidate, compareLabel) * 0.72f),
                "LOADOUT FIND" => ResolveLoadoutBias(candidate, compareLabel),
                "CASH OUT" => ResolveCashOutBias(candidate, compareLabel),
                "CASH WINDOW" => ResolveCashOutBias(candidate, compareLabel) + 3f,
                "KEY WINDOW" => Mathf.Max(ResolvePowerSpikeBias(candidate, compareLabel) + 1.5f, ResolveLoadoutBias(candidate, compareLabel)),
                "BOMB LINE" => ResolveSupplyRunBias(candidate, compareLabel),
                "FILL SLOT" => ResolveFillSlotBias(candidate, compareLabel),
                "SUPPLY RUN" => ResolveSupplyRunBias(candidate, compareLabel),
                _ => 0f
            };
        }

        private static float ResolvePatchHealthBias(Transform candidate, string compareLabel)
        {
            return compareLabel switch
            {
                "CLUTCH HEAL" => 24f,
                "FULL HEAL" => 22f,
                "HEAL NOW" => 18f,
                "HP SPIKE" or "HP UP" => 12f,
                "FULL HP" => -12f,
                _ when IsHealthCandidate(candidate) => 10f,
                _ => 0f
            };
        }

        private static float ResolvePowerSpikeBias(Transform candidate, string compareLabel)
        {
            return compareLabel switch
            {
                "DMG SPIKE" => 18f,
                "SHOT SPIKE" => 17f,
                "EXTRA SHOT" => 16f,
                "PIERCE" or "HOMING" or "EXPLOSIVE" or "LASER SHIFT" or "SPLIT SHOT" or "RICOCHET" or "LIFESTEAL" => 15f,
                "DMG UP" or "SHOT UP" or "HP SPIKE" => 11f,
                _ when IsPowerCandidate(candidate) => 7f,
                _ => 0f
            };
        }

        private static float ResolveSafeUpgradeBias(Transform candidate, string compareLabel)
        {
            return compareLabel switch
            {
                "CLUTCH HEAL" => 18f,
                "FULL HEAL" => 16f,
                "HEAL NOW" => 13f,
                "HP SPIKE" or "HP UP" or "SPEED SPIKE" or "SPEED UP" or "SPEED HEART" => 11f,
                "TRADE OFF" => -8f,
                _ when IsHealthCandidate(candidate) => 9f,
                _ => 0f
            };
        }

        private static float ResolveLoadoutBias(Transform candidate, string compareLabel)
        {
            return compareLabel switch
            {
                "TRINKET OPEN" or "ACTIVE OPEN" => 17f,
                "TRINKET SWAP" or "ACTIVE SWAP" => 13f,
                "BUILD SHIFT" or "SHOT SHIFT" or "LASER SHIFT" or "SUMMON TECH" or "ORBITAL" => 11f,
                _ when IsLoadoutCandidate(candidate) => 7f,
                _ => 0f
            };
        }

        private static float ResolveCashOutBias(Transform candidate, string compareLabel)
        {
            ShopItem shopItem = ResolveShopItem(candidate);
            if (shopItem?.ShopItemData == null)
            {
                return compareLabel == "SHOP FUEL" ? 6f : 0f;
            }

            float score = compareLabel is "COIN SHORT" or "KEY SHORT" or "BOMB SHORT" or "HP SHORT" ? -8f : 0f;
            score += shopItem.ShopItemData.Offer.RewardType == ShopOfferRewardType.PassiveItem ? 12f : 4f;
            score += Mathf.Clamp(shopItem.Price, 0, 24) * 0.28f;
            return score;
        }

        private static float ResolveFillSlotBias(Transform candidate, string compareLabel)
        {
            ShopItem shopItem = ResolveShopItem(candidate);
            if (shopItem?.ShopItemData == null)
            {
                return IsLoadoutCandidate(candidate) ? 6f : 0f;
            }

            return shopItem.ShopItemData.Offer.RewardType == ShopOfferRewardType.PassiveItem
                ? 17f
                : compareLabel is "ACTIVE OPEN" or "TRINKET OPEN"
                    ? 10f
                    : 0f;
        }

        private static float ResolveSupplyRunBias(Transform candidate, string compareLabel)
        {
            return compareLabel switch
            {
                "KEY RELIEF" or "BOMB RELIEF" => 16f,
                "CLUTCH HEAL" => 14f,
                "NO WEAPON" => -8f,
                "AMMO FULL" => 3f,
                "AMMO NOW" or "RESTOCK AMMO" => 13f,
                "KEY UP" or "BOMB UP" or "HEAL NOW" or "SHOP FUEL" => 11f,
                _ when IsHealthCandidate(candidate) || IsAmmoCandidate(candidate) => 7f,
                _ => 0f
            };
        }

        private static bool IsHealthCandidate(Transform candidate)
        {
            ShopItem shopItem = ResolveShopItem(candidate);
            if (shopItem?.ShopItemData != null)
            {
                return shopItem.ShopItemData.Offer.RewardType == ShopOfferRewardType.Health;
            }

            BasePickupLogic pickupLogic = ResolvePickupLogic(candidate);
            return pickupLogic is HeartPickupLogic
                || pickupLogic is ConsumablePickupLogic consumablePickup && consumablePickup.ConsumableItemData != null && consumablePickup.ConsumableItemData.HealAmount > 0f;
        }

        private static bool IsAmmoCandidate(Transform candidate)
        {
            ShopItem shopItem = ResolveShopItem(candidate);
            if (shopItem?.ShopItemData != null)
            {
                return shopItem.ShopItemData.Offer.RewardType == ShopOfferRewardType.Ammo;
            }

            BasePickupLogic pickupLogic = ResolvePickupLogic(candidate);
            return pickupLogic is AmmoPickupLogic;
        }

        private static bool IsPowerCandidate(Transform candidate)
        {
            ShopItem shopItem = ResolveShopItem(candidate);
            if (shopItem?.ShopItemData != null)
            {
                return shopItem.ShopItemData.Offer.RewardType == ShopOfferRewardType.PassiveItem;
            }

            BasePickupLogic pickupLogic = ResolvePickupLogic(candidate);
            return pickupLogic is ItemPickupLogic
                || pickupLogic is ActiveItemPickupLogic
                || pickupLogic is TrinketPickupLogic;
        }

        private static bool IsLoadoutCandidate(Transform candidate)
        {
            BasePickupLogic pickupLogic = ResolvePickupLogic(candidate);
            if (pickupLogic is ItemPickupLogic || pickupLogic is TrinketPickupLogic || pickupLogic is ActiveItemPickupLogic)
            {
                return true;
            }

            ShopItem shopItem = ResolveShopItem(candidate);
            return shopItem?.ShopItemData != null && shopItem.ShopItemData.Offer.RewardType == ShopOfferRewardType.PassiveItem;
        }

        private static ShopItem ResolveShopItem(Transform candidate)
        {
            if (candidate == null)
            {
                return null;
            }

            ShopItem shopItem = candidate.GetComponent<ShopItem>();
            if (shopItem == null)
            {
                shopItem = candidate.GetComponentInChildren<ShopItem>(true);
            }

            return shopItem;
        }

        private static BasePickupLogic ResolvePickupLogic(Transform candidate)
        {
            if (candidate == null)
            {
                return null;
            }

            BasePickupLogic pickupLogic = candidate.GetComponent<BasePickupLogic>();
            if (pickupLogic == null)
            {
                pickupLogic = candidate.GetComponentInChildren<BasePickupLogic>(true);
            }

            return pickupLogic;
        }

        private bool TryResolveArrivalAssistTarget(Vector3 playerPosition, out Transform targetTransform, out bool strong, out bool ready, out float radius)
        {
            targetTransform = null;
            strong = false;
            ready = false;
            radius = 0.5f;

            if (Time.unscaledTime > _arrivalAssistExpiresAt)
            {
                ClearArrivalAssist();
                return false;
            }

            if (_arrivalAssistTarget == null || !_arrivalAssistTarget.gameObject.activeInHierarchy)
            {
                return false;
            }

            targetTransform = _arrivalAssistTarget;
            strong = true;
            float commitReadyDistance = ResolveCommitReadyDistance(roomController != null ? roomController.RoomType : Data.Dungeon.RoomType.Normal);
            ready = Vector3.Distance(playerPosition, targetTransform.position) <= commitReadyDistance;
            radius = ready ? 0.7f : 0.62f;
            return true;
        }

        private bool IsArrivalAssistTarget(Transform target)
        {
            return target != null
                && target == _arrivalAssistTarget
                && Time.unscaledTime <= _arrivalAssistExpiresAt;
        }

        private bool TryGetActiveArrivalAssistCarryPresentation(out string carryLabel, out Color carryColor)
        {
            carryLabel = string.Empty;
            carryColor = Color.white;

            if (Time.unscaledTime > _arrivalAssistCarryExpiresAt || string.IsNullOrWhiteSpace(_arrivalAssistCarryLabel))
            {
                ClearArrivalAssistCarryPresentation();
                return false;
            }

            carryLabel = _arrivalAssistCarryLabel;
            carryColor = _arrivalAssistCarryColor.a > 0.01f
                ? _arrivalAssistCarryColor
                : Color.Lerp(_arrivalAssistAccentColor, Color.white, 0.18f);
            return true;
        }

        private bool TryGetActiveRouteRecommendationSync(out string routeReasonTag, out Color routeAccentColor)
        {
            routeReasonTag = string.Empty;
            routeAccentColor = Color.white;

            if (Time.unscaledTime > _routeRecommendationExpiresAt || string.IsNullOrWhiteSpace(_routeRecommendationReasonTag))
            {
                ClearRouteRecommendationSync();
                return false;
            }

            routeReasonTag = _routeRecommendationReasonTag;
            routeAccentColor = _routeRecommendationAccentColor.a > 0.01f
                ? _routeRecommendationAccentColor
                : RoomTraversalGuidanceController.ResolveRoomAccent(roomController != null ? roomController.RoomType : Data.Dungeon.RoomType.Normal);
            return true;
        }

        private bool TryGetActiveRouteRecommendationCarryPresentation(out string carryLabel, out Color carryColor)
        {
            carryLabel = string.Empty;
            carryColor = Color.white;

            if (Time.unscaledTime > _routeRecommendationCarryExpiresAt || string.IsNullOrWhiteSpace(_routeRecommendationCarryLabel))
            {
                ClearRouteRecommendationCarryPresentation();
                return false;
            }

            carryLabel = _routeRecommendationCarryLabel;
            carryColor = _routeRecommendationCarryColor.a > 0.01f
                ? _routeRecommendationCarryColor
                : Color.Lerp(
                    _routeRecommendationAccentColor.a > 0.01f
                        ? _routeRecommendationAccentColor
                        : RoomTraversalGuidanceController.ResolveRoomAccent(roomController != null ? roomController.RoomType : Data.Dungeon.RoomType.Normal),
                    Color.white,
                    0.18f);
            return true;
        }

        private void ClearArrivalAssist()
        {
            _arrivalAssistTarget = null;
            _arrivalAssistCompareLabel = string.Empty;
            _arrivalAssistAccentColor = Color.white;
            _arrivalAssistCompareColor = Color.white;
            _arrivalAssistExpiresAt = float.NegativeInfinity;
            _arrivalAssistPulsePending = false;
            ClearArrivalAssistCarryPresentation();
        }

        private void ClearRouteRecommendationSync()
        {
            _routeRecommendationReasonTag = string.Empty;
            _routeRecommendationAccentColor = Color.white;
            _routeRecommendationExpiresAt = float.NegativeInfinity;
            ClearRouteRecommendationCarryPresentation();
            ClearPrimaryRecommendation();
        }

        private void ClearArrivalAssistCarryPresentation()
        {
            _arrivalAssistCarryLabel = string.Empty;
            _arrivalAssistCarryColor = Color.white;
            _arrivalAssistCarryExpiresAt = float.NegativeInfinity;
        }

        private void ClearRouteRecommendationCarryPresentation()
        {
            _routeRecommendationCarryLabel = string.Empty;
            _routeRecommendationCarryColor = Color.white;
            _routeRecommendationCarryExpiresAt = float.NegativeInfinity;
        }

        private float ResolveCommitReadyDistance(Data.Dungeon.RoomType roomType)
        {
            return roomType switch
            {
                Data.Dungeon.RoomType.Treasure => treasureCommitReadyDistance,
                Data.Dungeon.RoomType.Shop => shopCommitReadyDistance,
                _ => fallbackCommitReadyDistance
            };
        }

        private string ResolvePickupReceiptLabel(BasePickupLogic pickupLogic)
        {
            if (pickupLogic != null && !string.IsNullOrWhiteSpace(pickupLogic.PreviewFeedbackLabel))
            {
                return pickupLogic.PreviewFeedbackLabel;
            }

            return "PICKUP SECURED";
        }

        private string ResolvePickupReceiptContextLabel(BasePickupLogic pickupLogic)
        {
            Data.Dungeon.RoomType roomType = roomController != null ? roomController.RoomType : Data.Dungeon.RoomType.Normal;

            if (roomType == Data.Dungeon.RoomType.Treasure)
            {
                return "TREASURE SECURED";
            }

            if (roomType == Data.Dungeon.RoomType.Curse)
            {
                return "PACT SEALED";
            }

            return pickupLogic switch
            {
                HeartPickupLogic => "RECOVERY",
                ResourcePickupLogic => "SUPPLY GAIN",
                ConsumablePickupLogic => "UTILITY READY",
                ActiveItemPickupLogic => "ACTIVE ONLINE",
                TrinketPickupLogic => "TRINKET SLOT",
                ItemPickupLogic => "LOADOUT SHIFT",
                _ => "PICKUP SECURED"
            };
        }

        private Color ResolvePickupReceiptAccent(BasePickupLogic pickupLogic)
        {
            if (pickupLogic != null && pickupLogic.PreviewFeedbackColor.a > 0.01f)
            {
                return pickupLogic.PreviewFeedbackColor;
            }

            return RoomTraversalGuidanceController.ResolveRoomAccent(roomController != null ? roomController.RoomType : Data.Dungeon.RoomType.Normal);
        }

        private static bool ResolvePickupReceiptEmphasis(BasePickupLogic pickupLogic)
        {
            return pickupLogic is ItemPickupLogic
                || pickupLogic is ActiveItemPickupLogic
                || pickupLogic is TrinketPickupLogic
                || pickupLogic is ConsumablePickupLogic;
        }

        private static string ResolveShopSuccessReceiptLabel(ShopItem shopItem)
        {
            if (shopItem == null)
            {
                return "PURCHASED";
            }

            ShopItemData itemData = shopItem.ShopItemData;
            if (itemData == null)
            {
                return "PURCHASED";
            }

            ShopOffer offer = itemData.Offer;
            return offer.RewardType switch
            {
                ShopOfferRewardType.PassiveItem when offer.PassiveItem != null => offer.PassiveItem.DisplayName.ToUpperInvariant(),
                ShopOfferRewardType.Health => $"+{offer.HealthAmount:0.#} HP",
                ShopOfferRewardType.Ammo => $"+{offer.ResourceAmount} AMMO",
                ShopOfferRewardType.Coins => $"+{offer.ResourceAmount} COIN",
                ShopOfferRewardType.Keys => $"+{offer.ResourceAmount} KEY",
                ShopOfferRewardType.Bombs => $"+{offer.ResourceAmount} BOMB",
                _ when shopItem.Price >= 16 => "PREMIUM SECURED",
                _ => "PURCHASED"
            };
        }

        private string ResolveShopFailureReceiptLabel(ShopItem shopItem)
        {
            if (shopItem == null)
            {
                return "PURCHASE LOCKED";
            }

            ShopSlotState slotState = shopItem.BuildSlotState(false, playerInventory, playerItemManager, playerHealth);
            if (slotState.StatusLabel == "HP NEEDED")
            {
                return "HP SHORT";
            }

            return slotState.StatusLabel switch
            {
                "코인 부족" => "COIN SHORT",
                "열쇠 부족" => "KEY SHORT",
                "폭탄 부족" => "BOMB SHORT",
                "이미 보유" => "ALREADY OWNED",
                "체력 가득" => "HEALTH FULL",
                "무기 없음" => "NO WEAPON",
                "탄약 가득" => "AMMO FULL",
                "판매 완료" => "SOLD OUT",
                _ => "PURCHASE LOCKED"
            };
        }

        private static string ResolveShopCostReceiptLabel(ShopItem shopItem)
        {
            if (shopItem == null)
            {
                return string.Empty;
            }

            string currencyLabel = shopItem.CurrencyType switch
            {
                ShopCurrencyType.Keys => "KEY",
                ShopCurrencyType.Bombs => "BOMB",
                ShopCurrencyType.Health => "HP",
                _ => "COIN"
            };

            return $"-{shopItem.Price} {currencyLabel}";
        }

        private static string ResolveShopAttemptDetailLabel(ShopItem shopItem)
        {
            if (shopItem?.ShopItemData == null || string.IsNullOrWhiteSpace(shopItem.ShopItemData.DisplayName))
            {
                return string.Empty;
            }

            return shopItem.ShopItemData.DisplayName.ToUpperInvariant();
        }

        private static bool ResolveShopReceiptEmphasis(ShopItem shopItem)
        {
            if (shopItem?.ShopItemData == null)
            {
                return false;
            }

            if (shopItem.Price >= 16)
            {
                return true;
            }

            ShopOffer offer = shopItem.ShopItemData.Offer;
            return offer.RewardType == ShopOfferRewardType.PassiveItem
                && offer.PassiveItem != null
                && offer.PassiveItem.Rarity >= ItemRarity.Rare;
        }

        private static Color ResolveShopRewardAccent(ShopItem shopItem)
        {
            if (shopItem?.ShopItemData == null)
            {
                return RoomTraversalGuidanceController.ResolveRoomAccent(Data.Dungeon.RoomType.Shop);
            }

            ShopOffer offer = shopItem.ShopItemData.Offer;
            return offer.RewardType switch
            {
                ShopOfferRewardType.PassiveItem when offer.PassiveItem != null => ResolveItemAccent(offer.PassiveItem.Rarity),
                ShopOfferRewardType.Health => new Color(1f, 0.52f, 0.62f, 1f),
                ShopOfferRewardType.Ammo => new Color(0.96f, 0.78f, 0.28f, 1f),
                ShopOfferRewardType.Coins => new Color(0.96f, 0.84f, 0.28f, 1f),
                ShopOfferRewardType.Keys => new Color(0.74f, 0.88f, 1f, 1f),
                ShopOfferRewardType.Bombs => new Color(1f, 0.56f, 0.24f, 1f),
                _ => RoomTraversalGuidanceController.ResolveRoomAccent(Data.Dungeon.RoomType.Shop)
            };
        }

        private static Color ResolveShopFailureAccent(ShopItem shopItem)
        {
            Color currencyColor = ResolveShopCurrencyAccent(shopItem != null ? shopItem.CurrencyType : ShopCurrencyType.Coins);
            return Color.Lerp(currencyColor, new Color(1f, 0.42f, 0.4f, 1f), 0.48f);
        }

        private static Color ResolveShopCurrencyAccent(ShopCurrencyType currencyType)
        {
            return currencyType switch
            {
                ShopCurrencyType.Keys => new Color(0.74f, 0.88f, 1f, 1f),
                ShopCurrencyType.Bombs => new Color(1f, 0.56f, 0.24f, 1f),
                ShopCurrencyType.Health => new Color(1f, 0.42f, 0.52f, 1f),
                _ => new Color(0.96f, 0.84f, 0.28f, 1f)
            };
        }

        private static Color ResolveItemAccent(ItemRarity rarity)
        {
            return rarity switch
            {
                ItemRarity.Uncommon => new Color(0.52f, 1f, 0.68f, 1f),
                ItemRarity.Rare => new Color(1f, 0.84f, 0.42f, 1f),
                ItemRarity.Legendary => new Color(1f, 0.48f, 0.82f, 1f),
                ItemRarity.Relic => new Color(1f, 0.92f, 0.6f, 1f),
                ItemRarity.Boss => new Color(1f, 0.4f, 0.4f, 1f),
                _ => new Color(0.72f, 0.85f, 1f, 1f)
            };
        }
    }
}
