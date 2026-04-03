using System.Collections.Generic;
using CuteIssac.Combat;
using CuteIssac.Data.Dungeon;
using CuteIssac.Enemy;
using CuteIssac.Room;
using UnityEngine;

namespace CuteIssac.Player
{
    /// <summary>
    /// Reuses the room highlight layer to point at the best opening target while a carried route plan opener is active.
    /// This keeps the first few seconds of combat readable without hardwiring authored VFX.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerRoutePlanCarryController))]
    public sealed class CombatOpeningTargetHintController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerRoutePlanCarryController routePlanCarryController;

        [Header("Hint Timing")]
        [SerializeField] [Min(0.05f)] private float retargetInterval = 0.18f;
        [SerializeField] [Min(0.1f)] private float repulseInterval = 0.55f;
        [SerializeField] [Min(0.2f)] private float highlightDuration = 0.78f;

        [Header("Hint Scoring")]
        [SerializeField] [Min(0.1f)] private float distanceFalloff = 0.22f;
        [SerializeField] [Min(0f)] private float focusPriorityBonus = 1.15f;
        [SerializeField] [Min(0f)] private float criticalPriorityBonus = 2.1f;
        [SerializeField] [Min(0f)] private float bossPriorityBonus = 2.6f;
        [SerializeField] [Min(0f)] private float elitePriorityBonus = 1.1f;

        [Header("Hint Radius")]
        [SerializeField] [Min(0.3f)] private float baseHighlightRadius = 0.7f;
        [SerializeField] [Min(0f)] private float focusRadiusBonus = 0.08f;
        [SerializeField] [Min(0f)] private float criticalRadiusBonus = 0.16f;
        [SerializeField] [Min(0f)] private float bossRadiusBonus = 0.2f;

        [Header("Safe Pocket Scoring")]
        [SerializeField] [Min(0f)] private float safePocketThreatBonus = 1.1f;
        [SerializeField] [Min(0f)] private float safePocketRoleBonus = 0.88f;
        [SerializeField] [Min(0f)] private float safePocketIntrusionBonus = 1.4f;
        [SerializeField] [Min(0f)] private float safePocketBossThreatBonus = 0.95f;

        [Header("Impact Relief Scoring")]
        [SerializeField] [Min(0f)] private float impactReliefThreatBonus = 1.05f;
        [SerializeField] [Min(0f)] private float impactReliefRoleBonus = 0.84f;
        [SerializeField] [Min(0f)] private float impactReliefIntrusionBonus = 1.32f;
        [SerializeField] [Min(0f)] private float impactReliefBossThreatBonus = 0.82f;

        [Header("Impact Secure Scoring")]
        [SerializeField] [Min(0f)] private float impactSecureThreatBonus = 1.18f;
        [SerializeField] [Min(0f)] private float impactSecureRoleBonus = 0.94f;
        [SerializeField] [Min(0f)] private float impactSecureIntrusionBonus = 1.44f;
        [SerializeField] [Min(0f)] private float impactSecureBossThreatBonus = 0.9f;

        [Header("Preferred Secure Hold Scoring")]
        [SerializeField] [Min(0f)] private float preferredImpactHoldThreatBonus = 1.26f;
        [SerializeField] [Min(0f)] private float preferredImpactHoldRoleBonus = 1.02f;
        [SerializeField] [Min(0f)] private float preferredImpactHoldIntrusionBonus = 1.52f;
        [SerializeField] [Min(0f)] private float preferredImpactHoldBossThreatBonus = 0.96f;

        [Header("Preferred Secure Hit Scoring")]
        [SerializeField] [Min(0f)] private float recentPreferredImpactHitThreatBonus = 1.18f;
        [SerializeField] [Min(0f)] private float recentPreferredImpactHitRoleBonus = 0.94f;
        [SerializeField] [Min(0f)] private float recentPreferredImpactHitIntrusionBonus = 1.3f;
        [SerializeField] [Min(0f)] private float recentPreferredImpactHitBossThreatBonus = 0.86f;
        [SerializeField] [Min(0f)] private float recentPreferredImpactHitContinuityBonus = 0.42f;

        [Header("Preferred Secure Drive Scoring")]
        [SerializeField] [Min(0f)] private float recentPreferredImpactDriveThreatBonus = 1.3f;
        [SerializeField] [Min(0f)] private float recentPreferredImpactDriveRoleBonus = 1.04f;
        [SerializeField] [Min(0f)] private float recentPreferredImpactDriveIntrusionBonus = 1.42f;
        [SerializeField] [Min(0f)] private float recentPreferredImpactDriveBossThreatBonus = 0.94f;
        [SerializeField] [Min(0f)] private float recentPreferredImpactDriveContinuityBonus = 0.52f;

        [Header("Breakthrough Scoring")]
        [SerializeField] [Min(0f)] private float breakthroughLaneAlignmentBonus = 1.05f;
        [SerializeField] [Min(0f)] private float breakthroughLaneProgressBonus = 1.2f;
        [SerializeField] [Min(0f)] private float breakthroughLaneRoleBonus = 0.86f;
        [SerializeField] [Min(0f)] private float breakthroughLaneOuterBonus = 0.7f;

        private readonly List<EnemyHealth> _candidateBuffer = new();
        private readonly List<Vector2> _impactSecureAnchorBuffer = new();
        private EnemyHealth _activeTarget;
        private RoomController _activeRoom;
        private CombatRoomArenaPressureController _activeArenaPressureController;
        private string _activeReasonTag = string.Empty;
        private string _activeCompareLabel = string.Empty;
        private Vector2 _safePocketCenter;
        private float _safePocketRadius;
        private bool _hasSafePocket;
        private Vector2 _impactReliefCenter;
        private float _impactReliefRadius;
        private bool _hasImpactRelief;
        private bool _impactSecureHoldActive;
        private bool _preferredImpactHoldDriveActive;
        private float _preferredImpactHoldDriveSide;
        private bool _recentPreferredImpactDriveActive;
        private float _recentPreferredImpactDriveSide;
        private float _recentPreferredImpactDriveStrength;
        private bool _recentPreferredImpactHitActive;
        private float _recentPreferredImpactHitSide;
        private float _recentPreferredImpactHitStrength;
        private bool _breakthroughChainReady;
        private OpeningCadenceVolleyRole _recentCadenceRole;
        private float _recentCadenceRoleWeight;
        private bool _hasBreakthroughLane;
        private Vector2 _breakthroughLaneOrigin;
        private Vector2 _breakthroughLaneDirection;
        private float _breakthroughLaneLength;
        private float _breakthroughLaneHalfWidth;
        private float _nextRetargetAt;
        private float _nextRepulseAt;
        private float _impactSecureAnchorRadius;

        public EnemyHealth ActiveTarget => _activeTarget;
        public bool HasActiveTarget => _activeTarget != null;
        public string ActiveTargetCompactTag => DecorateActiveTargetCompactTag(ResolveTargetCompactTag(_activeTarget));
        public string ActiveTargetDirective => DecorateActiveTargetDirective(ResolveTargetDirective(_activeTarget, _activeReasonTag, _activeCompareLabel, _hasSafePocket, _hasImpactRelief, _impactSecureHoldActive, _breakthroughChainReady, _recentCadenceRole));
        public string ActiveTargetDetail => DecorateActiveTargetDetail(ResolveTargetDetail(_activeTarget, _activeReasonTag, _activeCompareLabel, _hasSafePocket, _hasImpactRelief, _impactSecureHoldActive, _breakthroughChainReady, _recentCadenceRole));

        private void Awake()
        {
            ResolveReferences();
        }

        private void OnEnable()
        {
            ResolveReferences();

            if (routePlanCarryController != null)
            {
                routePlanCarryController.OpeningStateChanged -= HandleOpeningStateChanged;
                routePlanCarryController.OpeningStateChanged += HandleOpeningStateChanged;
            }

            ResetHintState();
        }

        private void OnDisable()
        {
            if (routePlanCarryController != null)
            {
                routePlanCarryController.OpeningStateChanged -= HandleOpeningStateChanged;
            }

            ResetHintState();
        }

        private void Update()
        {
            ResolveReferences();

            if (routePlanCarryController == null || !routePlanCarryController.IsOpeningActive)
            {
                if (_activeRoom != null || _activeTarget != null)
                {
                    ResetHintState();
                }

                return;
            }

            RoomController room = routePlanCarryController.ActiveRoom;
            if (room == null)
            {
                ResetHintState();
                return;
            }

            string reasonTag = routePlanCarryController.ActiveReasonTag;
            string compareLabel = routePlanCarryController.ActiveCompareLabel;
            OpeningCadenceVolleyRole recentCadenceRole = routePlanCarryController.RecentOpeningCadenceHitRole;
            float recentCadenceRoleWeight = routePlanCarryController.RecentOpeningCadenceHitRoleWeight;
            bool reliefStateChanged = RefreshSafePocketState(room);
            bool secureAnchorStateChanged = RefreshImpactSecureAnchorState();
            bool breakthroughStateChanged = _breakthroughChainReady != routePlanCarryController.IsBreakthroughChainReady;
            bool secureHoldStateChanged = _impactSecureHoldActive != (routePlanCarryController != null && routePlanCarryController.IsImpactSecureHoldActive);
            bool recentPreferredImpactDriveActive = routePlanCarryController != null && routePlanCarryController.HasRecentPreferredImpactDrive;
            float recentPreferredImpactDriveSide = recentPreferredImpactDriveActive
                ? Mathf.Sign(routePlanCarryController.RecentPreferredImpactDriveSide)
                : 0f;
            float recentPreferredImpactDriveStrength = recentPreferredImpactDriveActive
                ? routePlanCarryController.RecentPreferredImpactDriveStrength
                : 0f;
            bool preferredHoldDriveActive = routePlanCarryController != null && routePlanCarryController.HasPreferredImpactHoldDrive;
            float preferredHoldDriveSide = preferredHoldDriveActive
                && routePlanCarryController.TryGetPreferredSecureAnchorSide(out float resolvedPreferredSide, out _)
                ? Mathf.Sign(resolvedPreferredSide)
                : 0f;
            bool recentPreferredImpactDriveStateChanged = _recentPreferredImpactDriveActive != recentPreferredImpactDriveActive
                || (recentPreferredImpactDriveActive
                    && (Mathf.Abs(_recentPreferredImpactDriveSide - recentPreferredImpactDriveSide) > 0.5f
                        || Mathf.Abs(_recentPreferredImpactDriveStrength - recentPreferredImpactDriveStrength) > 0.05f));
            bool recentPreferredImpactHitActive = routePlanCarryController != null && routePlanCarryController.HasRecentPreferredImpactHit;
            float recentPreferredImpactHitSide = recentPreferredImpactHitActive
                ? Mathf.Sign(routePlanCarryController.RecentPreferredImpactHitSide)
                : 0f;
            float recentPreferredImpactHitStrength = recentPreferredImpactHitActive
                ? routePlanCarryController.RecentPreferredImpactHitStrength
                : 0f;
            bool preferredHoldStateChanged = _preferredImpactHoldDriveActive != preferredHoldDriveActive
                || (preferredHoldDriveActive && Mathf.Abs(_preferredImpactHoldDriveSide - preferredHoldDriveSide) > 0.5f);
            bool recentPreferredImpactHitStateChanged = _recentPreferredImpactHitActive != recentPreferredImpactHitActive
                || (recentPreferredImpactHitActive
                    && (Mathf.Abs(_recentPreferredImpactHitSide - recentPreferredImpactHitSide) > 0.5f
                        || Mathf.Abs(_recentPreferredImpactHitStrength - recentPreferredImpactHitStrength) > 0.05f));
            bool cadenceRoleStateChanged = _recentCadenceRole != recentCadenceRole
                || Mathf.Abs(_recentCadenceRoleWeight - recentCadenceRoleWeight) > 0.05f;
            bool needsRetarget = room != _activeRoom
                || !string.Equals(reasonTag, _activeReasonTag)
                || !string.Equals(compareLabel, _activeCompareLabel)
                || reliefStateChanged
                || secureAnchorStateChanged
                || breakthroughStateChanged
                || secureHoldStateChanged
                || recentPreferredImpactDriveStateChanged
                || preferredHoldStateChanged
                || recentPreferredImpactHitStateChanged
                || cadenceRoleStateChanged
                || _activeTarget == null
                || !IsValidTarget(_activeTarget, room)
                || Time.unscaledTime >= _nextRetargetAt;

            if (needsRetarget)
            {
                ResolveAndPresentTarget(room, reasonTag, compareLabel, true, reliefStateChanged, breakthroughStateChanged || secureHoldStateChanged);
                return;
            }

            if (_activeTarget != null && Time.unscaledTime >= _nextRepulseAt)
            {
                PresentHint(_activeTarget, room, true);
                _nextRepulseAt = Time.unscaledTime + repulseInterval;
            }
        }

        private void HandleOpeningStateChanged()
        {
            if (routePlanCarryController == null || !routePlanCarryController.IsOpeningActive)
            {
                ResetHintState();
                return;
            }

            bool reliefStateChanged = RefreshSafePocketState(routePlanCarryController.ActiveRoom);
            bool secureAnchorStateChanged = RefreshImpactSecureAnchorState();
            bool breakthroughStateChanged = _breakthroughChainReady != routePlanCarryController.IsBreakthroughChainReady;
            bool secureHoldStateChanged = _impactSecureHoldActive != (routePlanCarryController != null && routePlanCarryController.IsImpactSecureHoldActive);
            bool recentPreferredImpactDriveActive = routePlanCarryController != null && routePlanCarryController.HasRecentPreferredImpactDrive;
            float recentPreferredImpactDriveSide = recentPreferredImpactDriveActive
                ? Mathf.Sign(routePlanCarryController.RecentPreferredImpactDriveSide)
                : 0f;
            float recentPreferredImpactDriveStrength = recentPreferredImpactDriveActive
                ? routePlanCarryController.RecentPreferredImpactDriveStrength
                : 0f;
            bool recentPreferredImpactDriveStateChanged = _recentPreferredImpactDriveActive != recentPreferredImpactDriveActive
                || (recentPreferredImpactDriveActive
                    && (Mathf.Abs(_recentPreferredImpactDriveSide - recentPreferredImpactDriveSide) > 0.5f
                        || Mathf.Abs(_recentPreferredImpactDriveStrength - recentPreferredImpactDriveStrength) > 0.05f));
            bool preferredHoldStateChanged = _preferredImpactHoldDriveActive != (routePlanCarryController != null && routePlanCarryController.HasPreferredImpactHoldDrive);
            bool recentPreferredImpactHitActive = routePlanCarryController != null && routePlanCarryController.HasRecentPreferredImpactHit;
            float recentPreferredImpactHitSide = recentPreferredImpactHitActive
                ? Mathf.Sign(routePlanCarryController.RecentPreferredImpactHitSide)
                : 0f;
            float recentPreferredImpactHitStrength = recentPreferredImpactHitActive
                ? routePlanCarryController.RecentPreferredImpactHitStrength
                : 0f;
            bool recentPreferredImpactHitStateChanged = _recentPreferredImpactHitActive != recentPreferredImpactHitActive
                || (recentPreferredImpactHitActive
                    && (Mathf.Abs(_recentPreferredImpactHitSide - recentPreferredImpactHitSide) > 0.5f
                        || Mathf.Abs(_recentPreferredImpactHitStrength - recentPreferredImpactHitStrength) > 0.05f));
            ResolveAndPresentTarget(
                routePlanCarryController.ActiveRoom,
                routePlanCarryController.ActiveReasonTag,
                routePlanCarryController.ActiveCompareLabel,
                true,
                reliefStateChanged || secureAnchorStateChanged || recentPreferredImpactDriveStateChanged || preferredHoldStateChanged || recentPreferredImpactHitStateChanged,
                breakthroughStateChanged || secureHoldStateChanged || secureAnchorStateChanged || recentPreferredImpactDriveStateChanged || preferredHoldStateChanged || recentPreferredImpactHitStateChanged);
        }

        private void ResolveAndPresentTarget(RoomController room, string reasonTag, string compareLabel, bool primary, bool reliefStateChanged, bool breakthroughStateChanged)
        {
            if (room == null)
            {
                ResetHintState();
                return;
            }

            room.CollectAliveEnemies(_candidateBuffer);
            EnemyHealth previousTarget = _activeTarget;
            string previousReasonTag = _activeReasonTag;
            string previousCompareLabel = _activeCompareLabel;
            bool previousBreakthroughState = _breakthroughChainReady;
            bool previousSecureHoldState = _impactSecureHoldActive;
            bool previousPreferredHoldState = _preferredImpactHoldDriveActive;
            float previousPreferredHoldSide = _preferredImpactHoldDriveSide;
            bool previousRecentPreferredImpactDriveState = _recentPreferredImpactDriveActive;
            float previousRecentPreferredImpactDriveSide = _recentPreferredImpactDriveSide;
            float previousRecentPreferredImpactDriveStrength = _recentPreferredImpactDriveStrength;
            bool previousRecentPreferredImpactHitState = _recentPreferredImpactHitActive;
            float previousRecentPreferredImpactHitSide = _recentPreferredImpactHitSide;
            float previousRecentPreferredImpactHitStrength = _recentPreferredImpactHitStrength;
            OpeningCadenceVolleyRole previousRecentCadenceRole = _recentCadenceRole;
            float previousRecentCadenceRoleWeight = _recentCadenceRoleWeight;

            _breakthroughChainReady = routePlanCarryController != null && routePlanCarryController.IsBreakthroughChainReady;
            _impactSecureHoldActive = routePlanCarryController != null && routePlanCarryController.IsImpactSecureHoldActive;
            _recentPreferredImpactDriveActive = routePlanCarryController != null && routePlanCarryController.HasRecentPreferredImpactDrive;
            _recentPreferredImpactDriveSide = _recentPreferredImpactDriveActive
                ? Mathf.Sign(routePlanCarryController.RecentPreferredImpactDriveSide)
                : 0f;
            _recentPreferredImpactDriveStrength = _recentPreferredImpactDriveActive
                ? routePlanCarryController.RecentPreferredImpactDriveStrength
                : 0f;
            _preferredImpactHoldDriveActive = routePlanCarryController != null && routePlanCarryController.HasPreferredImpactHoldDrive;
            _preferredImpactHoldDriveSide = _preferredImpactHoldDriveActive
                && routePlanCarryController.TryGetPreferredSecureAnchorSide(out float preferredHoldSide, out _)
                ? Mathf.Sign(preferredHoldSide)
                : 0f;
            _recentPreferredImpactHitActive = routePlanCarryController != null && routePlanCarryController.HasRecentPreferredImpactHit;
            _recentPreferredImpactHitSide = _recentPreferredImpactHitActive
                ? Mathf.Sign(routePlanCarryController.RecentPreferredImpactHitSide)
                : 0f;
            _recentPreferredImpactHitStrength = _recentPreferredImpactHitActive
                ? routePlanCarryController.RecentPreferredImpactHitStrength
                : 0f;
            _recentCadenceRole = routePlanCarryController != null
                ? routePlanCarryController.RecentOpeningCadenceHitRole
                : OpeningCadenceVolleyRole.None;
            _recentCadenceRoleWeight = routePlanCarryController != null
                ? routePlanCarryController.RecentOpeningCadenceHitRoleWeight
                : 0f;
            RefreshBreakthroughLane(previousTarget, breakthroughStateChanged || reliefStateChanged);

            EnemyHealth bestTarget = null;
            float bestScore = float.NegativeInfinity;

            for (int i = 0; i < _candidateBuffer.Count; i++)
            {
                EnemyHealth candidate = _candidateBuffer[i];
                if (!IsValidTarget(candidate, room))
                {
                    continue;
                }

                float score = ScoreTarget(candidate, room, reasonTag, compareLabel);
                if (score <= bestScore)
                {
                    continue;
                }

                bestScore = score;
                bestTarget = candidate;
            }

            _activeRoom = room;
            _activeReasonTag = reasonTag ?? string.Empty;
            _activeCompareLabel = compareLabel ?? string.Empty;
            _activeTarget = bestTarget;
            _nextRetargetAt = Time.unscaledTime + retargetInterval;
            _nextRepulseAt = Time.unscaledTime + repulseInterval;

            if (routePlanCarryController != null
                && (previousTarget != _activeTarget
                    || !string.Equals(previousReasonTag, _activeReasonTag)
                    || !string.Equals(previousCompareLabel, _activeCompareLabel)
                    || reliefStateChanged
                    || breakthroughStateChanged
                    || previousBreakthroughState != _breakthroughChainReady
                    || previousSecureHoldState != _impactSecureHoldActive
                    || previousRecentPreferredImpactDriveState != _recentPreferredImpactDriveActive
                    || Mathf.Abs(previousRecentPreferredImpactDriveSide - _recentPreferredImpactDriveSide) > 0.5f
                    || Mathf.Abs(previousRecentPreferredImpactDriveStrength - _recentPreferredImpactDriveStrength) > 0.05f
                    || previousPreferredHoldState != _preferredImpactHoldDriveActive
                    || Mathf.Abs(previousPreferredHoldSide - _preferredImpactHoldDriveSide) > 0.5f
                    || previousRecentPreferredImpactHitState != _recentPreferredImpactHitActive
                    || Mathf.Abs(previousRecentPreferredImpactHitSide - _recentPreferredImpactHitSide) > 0.5f
                    || Mathf.Abs(previousRecentPreferredImpactHitStrength - _recentPreferredImpactHitStrength) > 0.05f
                    || previousRecentCadenceRole != _recentCadenceRole
                    || Mathf.Abs(previousRecentCadenceRoleWeight - _recentCadenceRoleWeight) > 0.05f))
            {
                routePlanCarryController.NotifyOpeningPresentationChanged();
            }

            if (_activeTarget != null)
            {
                PresentHint(_activeTarget, room, primary);
            }
        }

        private void PresentHint(EnemyHealth target, RoomController room, bool primary)
        {
            if (target == null || room == null || routePlanCarryController == null)
            {
                return;
            }

            RoomEntryObjectHighlightPresentation presentation = target.GetComponent<RoomEntryObjectHighlightPresentation>();
            if (presentation == null)
            {
                presentation = target.gameObject.AddComponent<RoomEntryObjectHighlightPresentation>();
            }

            Color highlightAccent = routePlanCarryController.ActiveAccentColor;
            float highlightRadius = ResolveHighlightRadius(target);
            if (routePlanCarryController.HasRecentPreferredImpactDrive)
            {
                highlightAccent = routePlanCarryController.RecentPreferredImpactDriveAccentColor;
                highlightRadius *= Mathf.Lerp(1.22f, 1.36f, Mathf.Clamp01(routePlanCarryController.RecentPreferredImpactDriveStrength));
            }
            else if (routePlanCarryController.HasRecentPreferredImpactHit)
            {
                highlightAccent = routePlanCarryController.RecentPreferredImpactHitAccentColor;
                highlightRadius *= Mathf.Lerp(1.16f, 1.28f, Mathf.Clamp01(routePlanCarryController.RecentPreferredImpactHitStrength));
            }
            else if (routePlanCarryController.HasPreferredImpactHoldDrive)
            {
                highlightAccent = routePlanCarryController.PreferredImpactHoldDriveAccentColor;
                highlightRadius *= routePlanCarryController.IsImpactSecureHoldChainActive ? 1.26f : 1.18f;
            }
            else if (routePlanCarryController.HasRecentOpeningCadenceHitRoleBurst)
            {
                highlightAccent = routePlanCarryController.RecentOpeningCadenceHitRoleAccentColor;
                float roleRadiusMultiplier = routePlanCarryController.RecentOpeningCadenceHitRole switch
                {
                    OpeningCadenceVolleyRole.Core => 1.08f,
                    OpeningCadenceVolleyRole.Flank => 1.16f,
                    OpeningCadenceVolleyRole.Edge => 1.2f,
                    _ => 1f
                };
                highlightRadius *= Mathf.Lerp(1f, roleRadiusMultiplier, Mathf.Clamp01(routePlanCarryController.RecentOpeningCadenceHitRoleWeight));
            }

            presentation.PlayHighlight(
                room.RoomType,
                highlightAccent,
                primary,
                guidedRoute: true,
                duration: highlightDuration,
                radius: highlightRadius);
        }

        private float ScoreTarget(EnemyHealth candidate, RoomController room, string reasonTag, string compareLabel)
        {
            float distance = Vector2.Distance(transform.position, candidate.transform.position);
            float score = Mathf.Clamp(2.4f - (distance * distanceFalloff), -2f, 2.4f);

            if (room.RoomType == RoomType.Boss || room.RoomType == RoomType.MiniBoss)
            {
                if (candidate.GetComponent<BossEnemyController>() != null)
                {
                    score += bossPriorityBonus;
                }
                else if (candidate.GetComponent<ChampionEnemyModifier>() != null)
                {
                    score += elitePriorityBonus;
                }
            }

            EnemyFormationModifier formationModifier = candidate.GetComponent<EnemyFormationModifier>();
            if (formationModifier == null)
            {
                return score
                    + ResolveReasonFallbackBonus(reasonTag, candidate)
                    + ResolveShotProfileFallbackBonus(compareLabel, candidate, room, distance)
                    + ResolveSafePocketThreatBonus(candidate, null)
                    + ResolveImpactReliefThreatBonus(candidate, null)
                    + ResolveImpactSecureThreatBonus(candidate, null)
                    + ResolveRecentPreferredImpactDriveThreatBonus(candidate, null)
                    + ResolvePreferredImpactHoldThreatBonus(candidate, null)
                    + ResolveRecentPreferredImpactHitThreatBonus(candidate, null)
                    + ResolveBreakthroughChainBonus(candidate, null)
                    + ResolveRecentOpeningCadenceRoleBonus(candidate, null, room, distance);
            }

            switch (formationModifier.PriorityLevel)
            {
                case EnemyFormationPriorityLevel.Critical:
                    score += criticalPriorityBonus;
                    break;
                case EnemyFormationPriorityLevel.Focus:
                    score += focusPriorityBonus;
                    break;
            }

            score += ResolveReasonRoleBonus(reasonTag, formationModifier.FormationRole, formationModifier.PriorityLevel);
            score += ResolveShotProfileRoleBonus(compareLabel, formationModifier.FormationRole, candidate, room, distance);
            score += ResolveSafePocketThreatBonus(candidate, formationModifier);
            score += ResolveImpactReliefThreatBonus(candidate, formationModifier);
            score += ResolveImpactSecureThreatBonus(candidate, formationModifier);
            score += ResolveRecentPreferredImpactDriveThreatBonus(candidate, formationModifier);
            score += ResolvePreferredImpactHoldThreatBonus(candidate, formationModifier);
            score += ResolveRecentPreferredImpactHitThreatBonus(candidate, formationModifier);
            score += ResolveBreakthroughChainBonus(candidate, formationModifier);
            score += ResolveRecentOpeningCadenceRoleBonus(candidate, formationModifier, room, distance);
            return score;
        }

        private void RefreshBreakthroughLane(EnemyHealth previousTarget, bool shouldRefresh)
        {
            if (!_breakthroughChainReady || !_hasSafePocket)
            {
                ClearBreakthroughLane();
                return;
            }

            if (!shouldRefresh && _hasBreakthroughLane)
            {
                return;
            }

            if (shouldRefresh)
            {
                ClearBreakthroughLane();
            }

            if (_activeArenaPressureController != null
                && _activeArenaPressureController.TryGetOpeningBreakthroughLaneData(
                    out Vector2 liveLaneOrigin,
                    out Vector2 liveLaneDirection,
                    out float liveLaneLength,
                    out float liveLaneHalfWidth,
                    out _,
                    out _))
            {
                _hasBreakthroughLane = true;
                _breakthroughLaneOrigin = liveLaneOrigin;
                _breakthroughLaneDirection = liveLaneDirection;
                _breakthroughLaneLength = liveLaneLength;
                _breakthroughLaneHalfWidth = liveLaneHalfWidth;
                return;
            }

            EnemyHealth anchorTarget = previousTarget;
            if (anchorTarget == null && _activeTarget != null)
            {
                anchorTarget = _activeTarget;
            }

            if (anchorTarget == null)
            {
                return;
            }

            Vector2 anchorPosition = anchorTarget.transform.position;
            Vector2 laneVector = anchorPosition - _safePocketCenter;
            float laneLength = laneVector.magnitude;
            if (laneLength <= 0.16f)
            {
                return;
            }

            _hasBreakthroughLane = true;
            _breakthroughLaneOrigin = _safePocketCenter;
            _breakthroughLaneDirection = laneVector / laneLength;
            _breakthroughLaneLength = laneLength;
            _breakthroughLaneHalfWidth = Mathf.Max(0.4f, _safePocketRadius * 0.92f);
        }

        private void ClearBreakthroughLane()
        {
            _hasBreakthroughLane = false;
            _breakthroughLaneOrigin = Vector2.zero;
            _breakthroughLaneDirection = Vector2.up;
            _breakthroughLaneLength = 0f;
            _breakthroughLaneHalfWidth = 0f;
        }

        private float ResolveSafePocketThreatBonus(EnemyHealth candidate, EnemyFormationModifier formationModifier)
        {
            if (!_hasSafePocket || candidate == null)
            {
                return 0f;
            }

            float distanceToPocket = Vector2.Distance(candidate.transform.position, _safePocketCenter);
            float pocketOuterBand = Mathf.Max(0.45f, _safePocketRadius * 1.25f);
            float edgeThreat = 1f - Mathf.Clamp01((distanceToPocket - _safePocketRadius) / pocketOuterBand);
            float score = edgeThreat * safePocketThreatBonus;

            if (distanceToPocket <= _safePocketRadius * 0.94f)
            {
                score += safePocketIntrusionBonus;
            }

            if (candidate.GetComponent<BossEnemyController>() != null)
            {
                score += safePocketBossThreatBonus;
            }

            if (formationModifier != null)
            {
                score += safePocketRoleBonus * (formationModifier.FormationRole switch
                {
                    EnemyFormationRole.Controller => 1f,
                    EnemyFormationRole.Ranged => 0.92f,
                    EnemyFormationRole.Siege => 0.88f,
                    EnemyFormationRole.Support => 0.76f,
                    EnemyFormationRole.Frontline => 0.6f,
                    _ => 0.28f
                });
            }

            return score;
        }

        private float ResolveImpactReliefThreatBonus(EnemyHealth candidate, EnemyFormationModifier formationModifier)
        {
            if (!_hasImpactRelief || candidate == null)
            {
                return 0f;
            }

            float distanceToImpact = Vector2.Distance(candidate.transform.position, _impactReliefCenter);
            float impactOuterBand = Mathf.Max(0.4f, _impactReliefRadius * 1.18f);
            float edgeThreat = 1f - Mathf.Clamp01((distanceToImpact - _impactReliefRadius) / impactOuterBand);
            float score = edgeThreat * impactReliefThreatBonus;

            if (distanceToImpact <= _impactReliefRadius * 0.92f)
            {
                score += impactReliefIntrusionBonus;
            }

            if (candidate.GetComponent<BossEnemyController>() != null)
            {
                score += impactReliefBossThreatBonus;
            }

            if (formationModifier != null)
            {
                score += impactReliefRoleBonus * (formationModifier.FormationRole switch
                {
                    EnemyFormationRole.Controller => 1f,
                    EnemyFormationRole.Ranged => 0.96f,
                    EnemyFormationRole.Siege => 0.92f,
                    EnemyFormationRole.Support => 0.74f,
                    EnemyFormationRole.Frontline => 0.56f,
                    _ => 0.24f
                });
            }

            return score;
        }

        private float ResolveImpactSecureThreatBonus(EnemyHealth candidate, EnemyFormationModifier formationModifier)
        {
            if (!_impactSecureHoldActive || !_hasImpactRelief || candidate == null)
            {
                return 0f;
            }

            Vector2 candidatePosition = candidate.transform.position;
            float impactRadius = HasImpactSecureAnchors() ? _impactSecureAnchorRadius : _impactReliefRadius;
            float distanceToImpact = HasImpactSecureAnchors()
                ? ResolveNearestImpactSecureAnchorDistance(candidatePosition)
                : Vector2.Distance(candidatePosition, _impactReliefCenter);
            float impactOuterBand = Mathf.Max(0.36f, impactRadius * 1.1f);
            float edgeThreat = 1f - Mathf.Clamp01((distanceToImpact - (impactRadius * 0.94f)) / impactOuterBand);
            float score = edgeThreat * impactSecureThreatBonus;

            if (distanceToImpact <= impactRadius * 0.9f)
            {
                score += impactSecureIntrusionBonus;
            }

            if (candidate.GetComponent<BossEnemyController>() != null)
            {
                score += impactSecureBossThreatBonus;
            }

            if (formationModifier != null)
            {
                score += impactSecureRoleBonus * (formationModifier.FormationRole switch
                {
                    EnemyFormationRole.Controller => 1f,
                    EnemyFormationRole.Ranged => 0.98f,
                    EnemyFormationRole.Siege => 0.94f,
                    EnemyFormationRole.Support => 0.72f,
                    EnemyFormationRole.Frontline => 0.52f,
                    _ => 0.24f
                });
            }

            return score;
        }

        private float ResolvePreferredImpactHoldThreatBonus(EnemyHealth candidate, EnemyFormationModifier formationModifier)
        {
            if (!_preferredImpactHoldDriveActive
                || candidate == null
                || routePlanCarryController == null
                || !routePlanCarryController.TryGetPreferredImpactSecureAnchor(out Vector2 preferredAnchor, out float preferredRadius, out float preferredNormalized))
            {
                return 0f;
            }

            float preferredInfluence = routePlanCarryController.TryGetPreferredSecureAnchorSide(out _, out float resolvedPreferredInfluence)
                ? Mathf.Clamp01(resolvedPreferredInfluence)
                : 1f;
            Vector2 candidatePosition = candidate.transform.position;
            float distanceToPreferred = Vector2.Distance(candidatePosition, preferredAnchor);
            float preferredOuterBand = Mathf.Max(0.34f, preferredRadius * 1.08f);
            float edgeThreat = 1f - Mathf.Clamp01((distanceToPreferred - (preferredRadius * 0.92f)) / preferredOuterBand);
            float score = edgeThreat
                * preferredImpactHoldThreatBonus
                * Mathf.Lerp(0.86f, 1.18f, Mathf.Max(preferredNormalized, preferredInfluence));

            if (distanceToPreferred <= preferredRadius * 0.88f)
            {
                score += preferredImpactHoldIntrusionBonus * Mathf.Lerp(0.84f, 1.12f, preferredInfluence);
            }

            if (candidate.GetComponent<BossEnemyController>() != null)
            {
                score += preferredImpactHoldBossThreatBonus * Mathf.Lerp(0.92f, 1.18f, preferredInfluence);
            }

            if (formationModifier != null)
            {
                score += preferredImpactHoldRoleBonus * (formationModifier.FormationRole switch
                {
                    EnemyFormationRole.Controller => 1f,
                    EnemyFormationRole.Ranged => 0.98f,
                    EnemyFormationRole.Siege => 0.96f,
                    EnemyFormationRole.Support => 0.74f,
                    EnemyFormationRole.Frontline => 0.56f,
                    _ => 0.24f
                });
            }

            return score;
        }

        private float ResolveRecentPreferredImpactDriveThreatBonus(EnemyHealth candidate, EnemyFormationModifier formationModifier)
        {
            if (!_recentPreferredImpactDriveActive
                || candidate == null
                || routePlanCarryController == null
                || !routePlanCarryController.TryGetPreferredImpactSecureAnchor(out Vector2 preferredAnchor, out float preferredRadius, out float preferredNormalized))
            {
                return 0f;
            }

            float preferredInfluence = routePlanCarryController.TryGetPreferredSecureAnchorSide(out float preferredSide, out float resolvedPreferredInfluence)
                ? Mathf.Clamp01(resolvedPreferredInfluence)
                : 1f;
            float sideContinuity = Mathf.Abs(preferredSide) > 0.5f && Mathf.Abs(_recentPreferredImpactDriveSide) > 0.5f
                ? Mathf.Sign(preferredSide) == Mathf.Sign(_recentPreferredImpactDriveSide)
                    ? 1f
                    : 0.68f
                : 0.9f;
            float driveStrength = Mathf.Clamp01(_recentPreferredImpactDriveStrength);
            Vector2 candidatePosition = candidate.transform.position;
            float distanceToPreferred = Vector2.Distance(candidatePosition, preferredAnchor);
            float preferredOuterBand = Mathf.Max(0.36f, preferredRadius * 1.08f);
            float edgeThreat = 1f - Mathf.Clamp01((distanceToPreferred - (preferredRadius * 0.88f)) / preferredOuterBand);
            float score = edgeThreat
                * recentPreferredImpactDriveThreatBonus
                * Mathf.Lerp(0.9f, 1.28f, Mathf.Max(preferredNormalized, preferredInfluence))
                * Mathf.Lerp(0.86f, 1.22f, driveStrength)
                * sideContinuity;

            if (distanceToPreferred <= preferredRadius * 0.82f)
            {
                score += recentPreferredImpactDriveIntrusionBonus * Mathf.Lerp(0.84f, 1.18f, driveStrength) * sideContinuity;
            }

            score += recentPreferredImpactDriveContinuityBonus * Mathf.Lerp(0.76f, 1.14f, driveStrength) * sideContinuity;

            if (candidate.GetComponent<BossEnemyController>() != null)
            {
                score += recentPreferredImpactDriveBossThreatBonus * Mathf.Lerp(0.92f, 1.16f, driveStrength);
            }

            if (formationModifier != null)
            {
                score += recentPreferredImpactDriveRoleBonus * (formationModifier.FormationRole switch
                {
                    EnemyFormationRole.Controller => 1f,
                    EnemyFormationRole.Ranged => 0.98f,
                    EnemyFormationRole.Siege => 0.98f,
                    EnemyFormationRole.Support => 0.76f,
                    EnemyFormationRole.Frontline => 0.58f,
                    _ => 0.26f
                }) * Mathf.Lerp(0.86f, 1.14f, driveStrength);
            }

            return score;
        }

        private float ResolveRecentPreferredImpactHitThreatBonus(EnemyHealth candidate, EnemyFormationModifier formationModifier)
        {
            if (!_recentPreferredImpactHitActive
                || candidate == null
                || routePlanCarryController == null
                || !routePlanCarryController.TryGetPreferredImpactSecureAnchor(out Vector2 preferredAnchor, out float preferredRadius, out float preferredNormalized))
            {
                return 0f;
            }

            float preferredInfluence = routePlanCarryController.TryGetPreferredSecureAnchorSide(out float preferredSide, out float resolvedPreferredInfluence)
                ? Mathf.Clamp01(resolvedPreferredInfluence)
                : 1f;
            float sideContinuity = Mathf.Abs(preferredSide) > 0.5f && Mathf.Abs(_recentPreferredImpactHitSide) > 0.5f
                ? Mathf.Sign(preferredSide) == Mathf.Sign(_recentPreferredImpactHitSide)
                    ? 1f
                    : 0.72f
                : 0.86f;
            float hitStrength = Mathf.Clamp01(_recentPreferredImpactHitStrength);
            Vector2 candidatePosition = candidate.transform.position;
            float distanceToPreferred = Vector2.Distance(candidatePosition, preferredAnchor);
            float preferredOuterBand = Mathf.Max(0.34f, preferredRadius * 1.04f);
            float edgeThreat = 1f - Mathf.Clamp01((distanceToPreferred - (preferredRadius * 0.9f)) / preferredOuterBand);
            float score = edgeThreat
                * recentPreferredImpactHitThreatBonus
                * Mathf.Lerp(0.84f, 1.2f, Mathf.Max(preferredNormalized, preferredInfluence))
                * Mathf.Lerp(0.82f, 1.18f, hitStrength)
                * sideContinuity;

            if (distanceToPreferred <= preferredRadius * 0.84f)
            {
                score += recentPreferredImpactHitIntrusionBonus * Mathf.Lerp(0.8f, 1.14f, hitStrength) * sideContinuity;
            }

            score += recentPreferredImpactHitContinuityBonus * Mathf.Lerp(0.72f, 1.1f, hitStrength) * sideContinuity;

            if (candidate.GetComponent<BossEnemyController>() != null)
            {
                score += recentPreferredImpactHitBossThreatBonus * Mathf.Lerp(0.88f, 1.12f, hitStrength);
            }

            if (formationModifier != null)
            {
                score += recentPreferredImpactHitRoleBonus * (formationModifier.FormationRole switch
                {
                    EnemyFormationRole.Controller => 1f,
                    EnemyFormationRole.Ranged => 0.98f,
                    EnemyFormationRole.Siege => 0.96f,
                    EnemyFormationRole.Support => 0.72f,
                    EnemyFormationRole.Frontline => 0.54f,
                    _ => 0.24f
                }) * Mathf.Lerp(0.82f, 1.1f, hitStrength);
            }

            return score;
        }

        private float ResolveNearestImpactSecureAnchorDistance(Vector2 point)
        {
            if (!HasImpactSecureAnchors())
            {
                return Vector2.Distance(point, _impactReliefCenter);
            }

            float nearestDistance = float.PositiveInfinity;
            for (int index = 0; index < _impactSecureAnchorBuffer.Count; index++)
            {
                float distance = Vector2.Distance(point, _impactSecureAnchorBuffer[index]);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                }
            }

            return float.IsPositiveInfinity(nearestDistance)
                ? Vector2.Distance(point, _impactReliefCenter)
                : nearestDistance;
        }

        private float ResolveBreakthroughChainBonus(EnemyHealth candidate, EnemyFormationModifier formationModifier)
        {
            if (!_breakthroughChainReady || candidate == null)
            {
                return 0f;
            }

            float score = 0f;
            if (_hasSafePocket)
            {
                float pocketDistance = Vector2.Distance(candidate.transform.position, _safePocketCenter);
                if (pocketDistance >= _safePocketRadius * 0.94f)
                {
                    score += breakthroughLaneOuterBonus * Mathf.Clamp01((pocketDistance - (_safePocketRadius * 0.94f)) / Mathf.Max(0.35f, _safePocketRadius));
                }
            }

            if (_hasBreakthroughLane)
            {
                Vector2 candidateOffset = (Vector2)candidate.transform.position - _breakthroughLaneOrigin;
                float along = Vector2.Dot(candidateOffset, _breakthroughLaneDirection);
                if (along > 0.04f)
                {
                    float progress = Mathf.Clamp01(along / Mathf.Max(0.1f, _breakthroughLaneLength));
                    Vector2 laneNormal = new(-_breakthroughLaneDirection.y, _breakthroughLaneDirection.x);
                    float lateral = Mathf.Abs(Vector2.Dot(candidateOffset, laneNormal));
                    float laneHalfWidth = Mathf.Max(0.4f, _breakthroughLaneHalfWidth);
                    float laneFit = 1f - Mathf.Clamp01(lateral / laneHalfWidth);
                    score += progress * breakthroughLaneProgressBonus;
                    score += laneFit * breakthroughLaneAlignmentBonus;
                }
            }

            if (candidate.GetComponent<BossEnemyController>() != null)
            {
                score += breakthroughLaneRoleBonus;
            }

            if (formationModifier != null)
            {
                score += breakthroughLaneRoleBonus * (formationModifier.FormationRole switch
                {
                    EnemyFormationRole.Controller => 1f,
                    EnemyFormationRole.Ranged => 0.96f,
                    EnemyFormationRole.Siege => 0.94f,
                    EnemyFormationRole.Support => 0.62f,
                    EnemyFormationRole.Frontline => 0.54f,
                    _ => 0.28f
                });
            }

            return score;
        }

        private float ResolveRecentOpeningCadenceRoleBonus(EnemyHealth candidate, EnemyFormationModifier formationModifier, RoomController room, float distance)
        {
            if (_recentCadenceRole == OpeningCadenceVolleyRole.None || candidate == null)
            {
                return 0f;
            }

            float weight = Mathf.Lerp(0.72f, 1.28f, Mathf.Clamp01(_recentCadenceRoleWeight));
            bool nearEdge = room != null && IsNearRoomEdge(candidate.transform.position, room.RoomBounds);
            bool isBoss = candidate.GetComponent<BossEnemyController>() != null;
            float score = 0f;

            switch (_recentCadenceRole)
            {
                case OpeningCadenceVolleyRole.Core:
                {
                    if (_hasImpactRelief)
                    {
                        float impactFit = 1f - Mathf.Clamp01(
                            Vector2.Distance(candidate.transform.position, _impactReliefCenter)
                            / Mathf.Max(0.4f, _impactReliefRadius * 1.18f));
                        score += impactFit * 0.9f;
                    }
                    else if (_hasSafePocket)
                    {
                        float pocketFit = 1f - Mathf.Clamp01(
                            Vector2.Distance(candidate.transform.position, _safePocketCenter)
                            / Mathf.Max(0.4f, _safePocketRadius * 1.48f));
                        score += pocketFit * 0.58f;
                    }

                    if (isBoss)
                    {
                        score += 0.86f;
                    }

                    score += formationModifier != null
                        ? formationModifier.FormationRole switch
                        {
                            EnemyFormationRole.Controller => 1.02f,
                            EnemyFormationRole.Frontline => 0.78f,
                            EnemyFormationRole.Ranged => 0.62f,
                            EnemyFormationRole.Support => 0.46f,
                            EnemyFormationRole.Siege => 0.34f,
                            _ => formationModifier.PriorityLevel == EnemyFormationPriorityLevel.Critical ? 0.42f : 0.18f
                        }
                        : distance <= 2.1f ? 0.28f : 0f;
                    break;
                }
                case OpeningCadenceVolleyRole.Flank:
                {
                    if (_hasImpactRelief)
                    {
                        float distanceToImpact = Vector2.Distance(candidate.transform.position, _impactReliefCenter);
                        float inner = _impactReliefRadius * 0.82f;
                        float outer = Mathf.Max(inner + 0.35f, _impactReliefRadius * 1.82f);
                        if (distanceToImpact > inner)
                        {
                            score += (1f - Mathf.Clamp01((distanceToImpact - inner) / Mathf.Max(0.1f, outer - inner))) * 0.72f;
                        }
                    }
                    else if (_hasSafePocket)
                    {
                        float distanceToPocket = Vector2.Distance(candidate.transform.position, _safePocketCenter);
                        float inner = _safePocketRadius * 0.94f;
                        float outer = Mathf.Max(inner + 0.4f, _safePocketRadius * 1.96f);
                        if (distanceToPocket > inner)
                        {
                            score += (1f - Mathf.Clamp01((distanceToPocket - inner) / Mathf.Max(0.1f, outer - inner))) * 0.58f;
                        }
                    }

                    score += nearEdge ? 0.16f : 0f;
                    score += formationModifier != null
                        ? formationModifier.FormationRole switch
                        {
                            EnemyFormationRole.Ranged => 1.04f,
                            EnemyFormationRole.Support => 0.88f,
                            EnemyFormationRole.Controller => 0.72f,
                            EnemyFormationRole.Siege => 0.58f,
                            EnemyFormationRole.Frontline => 0.34f,
                            _ => formationModifier.PriorityLevel == EnemyFormationPriorityLevel.Focus ? 0.28f : 0.12f
                        }
                        : distance >= 1.2f ? 0.24f : 0f;
                    break;
                }
                case OpeningCadenceVolleyRole.Edge:
                {
                    if (nearEdge)
                    {
                        score += 0.94f;
                    }

                    if (_hasImpactRelief)
                    {
                        float distanceToImpact = Vector2.Distance(candidate.transform.position, _impactReliefCenter);
                        if (distanceToImpact >= _impactReliefRadius * 0.96f)
                        {
                            score += 0.38f * Mathf.Clamp01((distanceToImpact - (_impactReliefRadius * 0.96f)) / Mathf.Max(0.35f, _impactReliefRadius));
                        }
                    }
                    else if (_hasSafePocket)
                    {
                        float distanceToPocket = Vector2.Distance(candidate.transform.position, _safePocketCenter);
                        if (distanceToPocket >= _safePocketRadius * 0.98f)
                        {
                            score += 0.3f * Mathf.Clamp01((distanceToPocket - (_safePocketRadius * 0.98f)) / Mathf.Max(0.35f, _safePocketRadius));
                        }
                    }

                    score += formationModifier != null
                        ? formationModifier.FormationRole switch
                        {
                            EnemyFormationRole.Siege => 1.08f,
                            EnemyFormationRole.Ranged => 0.84f,
                            EnemyFormationRole.Controller => 0.66f,
                            EnemyFormationRole.Support => 0.42f,
                            EnemyFormationRole.Frontline => 0.22f,
                            _ => formationModifier.PriorityLevel == EnemyFormationPriorityLevel.Critical ? 0.34f : 0.14f
                        }
                        : nearEdge ? 0.26f : 0f;
                    break;
                }
            }

            return score * weight;
        }

        private static float ResolveReasonFallbackBonus(string reasonTag, EnemyHealth candidate)
        {
            return NormalizeReason(reasonTag) switch
            {
                "PRESS ADVANTAGE" => candidate.GetComponent<BossEnemyController>() != null ? 1.4f : 0.3f,
                "RECOVERY ONLINE" => 0.2f,
                "SUPPLY WINDOW" => 0.35f,
                "LOADOUT FIND" => candidate.GetComponent<BossEnemyController>() != null ? 0.8f : 0.25f,
                "SAFE UPGRADE" => 0.15f,
                _ => 0f
            };
        }

        private static float ResolveShotProfileFallbackBonus(string compareLabel, EnemyHealth candidate, RoomController room, float distance)
        {
            if (!IsShotProfileCompareLabel(compareLabel) || candidate == null)
            {
                return 0f;
            }

            string normalized = ResolveShotProfileKeyword(compareLabel);
            float score = 0f;

            if (candidate.GetComponent<BossEnemyController>() != null)
            {
                score += normalized switch
                {
                    "LASER" => 0.95f,
                    "BLAST" => 0.72f,
                    "BOUNCE" => 0.62f,
                    _ => 0.48f
                };
            }

            if (distance <= 2.4f)
            {
                score += normalized switch
                {
                    "ORBIT" => 0.86f,
                    "SHIELD" => 0.74f,
                    "LEECH" => 0.58f,
                    _ => 0f
                };
            }
            else
            {
                score += normalized switch
                {
                    "LASER" => 0.58f,
                    "BOUNCE" => 0.44f,
                    _ => 0f
                };
            }

            if (room != null && IsNearRoomEdge(candidate.transform.position, room.RoomBounds))
            {
                score += normalized switch
                {
                    "BOUNCE" => 0.74f,
                    "LASER" => 0.22f,
                    _ => 0f
                };
            }

            return score;
        }

        private static float ResolveReasonRoleBonus(string reasonTag, EnemyFormationRole role, EnemyFormationPriorityLevel priorityLevel)
        {
            float priorityBias = priorityLevel == EnemyFormationPriorityLevel.Critical
                ? 0.35f
                : priorityLevel == EnemyFormationPriorityLevel.Focus
                    ? 0.18f
                    : 0f;

            return NormalizeReason(reasonTag) switch
            {
                "PRESS ADVANTAGE" => priorityBias + (role switch
                {
                    EnemyFormationRole.Controller => 1.1f,
                    EnemyFormationRole.Ranged => 0.9f,
                    EnemyFormationRole.Support => 0.65f,
                    EnemyFormationRole.Siege => 0.8f,
                    _ => 0.25f
                }),
                "RECOVERY ONLINE" => priorityBias + (role switch
                {
                    EnemyFormationRole.Support => 1.25f,
                    EnemyFormationRole.Controller => 0.95f,
                    EnemyFormationRole.Siege => 0.72f,
                    EnemyFormationRole.Ranged => 0.55f,
                    _ => 0.2f
                }),
                "SUPPLY WINDOW" => priorityBias + (role switch
                {
                    EnemyFormationRole.Ranged => 1.2f,
                    EnemyFormationRole.Siege => 1.05f,
                    EnemyFormationRole.Controller => 0.84f,
                    _ => 0.28f
                }),
                "LOADOUT FIND" => priorityBias + (role switch
                {
                    EnemyFormationRole.Controller => 0.82f,
                    EnemyFormationRole.Ranged => 0.76f,
                    EnemyFormationRole.Support => 0.58f,
                    EnemyFormationRole.Siege => 0.72f,
                    _ => 0.24f
                }),
                "SAFE UPGRADE" => priorityBias + (role switch
                {
                    EnemyFormationRole.Support => 1.08f,
                    EnemyFormationRole.Controller => 0.86f,
                    EnemyFormationRole.Ranged => 0.46f,
                    _ => 0.18f
                }),
                _ => priorityBias
            };
        }

        private static float ResolveShotProfileRoleBonus(string compareLabel, EnemyFormationRole role, EnemyHealth candidate, RoomController room, float distance)
        {
            if (!IsShotProfileCompareLabel(compareLabel) || candidate == null)
            {
                return 0f;
            }

            string normalized = ResolveShotProfileKeyword(compareLabel);
            float distanceBias = distance <= 2.25f ? 0.22f : 0f;
            float edgeBias = room != null && IsNearRoomEdge(candidate.transform.position, room.RoomBounds) ? 0.2f : 0f;

            return normalized switch
            {
                "LASER" => role switch
                {
                    EnemyFormationRole.Ranged => 1.02f,
                    EnemyFormationRole.Siege => 0.94f,
                    EnemyFormationRole.Controller => 0.78f,
                    EnemyFormationRole.Support => 0.34f,
                    EnemyFormationRole.Frontline => 0.18f,
                    _ => 0f
                },
                "SPLIT" => role switch
                {
                    EnemyFormationRole.Frontline => 1.08f,
                    EnemyFormationRole.Support => 0.82f,
                    EnemyFormationRole.Controller => 0.54f,
                    EnemyFormationRole.Ranged => 0.36f,
                    EnemyFormationRole.Siege => 0.42f,
                    _ => 0f
                },
                "ORBIT" => distanceBias + (role switch
                {
                    EnemyFormationRole.Frontline => 1.02f,
                    EnemyFormationRole.Support => 0.76f,
                    EnemyFormationRole.Controller => 0.68f,
                    EnemyFormationRole.Ranged => 0.34f,
                    EnemyFormationRole.Siege => 0.24f,
                    _ => 0f
                }),
                "SHIELD" => distanceBias + (role switch
                {
                    EnemyFormationRole.Controller => 1.04f,
                    EnemyFormationRole.Ranged => 0.88f,
                    EnemyFormationRole.Support => 0.62f,
                    EnemyFormationRole.Siege => 0.52f,
                    EnemyFormationRole.Frontline => 0.26f,
                    _ => 0f
                }),
                "BOUNCE" => edgeBias + (role switch
                {
                    EnemyFormationRole.Siege => 0.94f,
                    EnemyFormationRole.Ranged => 0.86f,
                    EnemyFormationRole.Controller => 0.52f,
                    EnemyFormationRole.Support => 0.28f,
                    EnemyFormationRole.Frontline => 0.2f,
                    _ => 0f
                }),
                "BLAST" => role switch
                {
                    EnemyFormationRole.Frontline => 1.04f,
                    EnemyFormationRole.Siege => 0.82f,
                    EnemyFormationRole.Controller => 0.58f,
                    EnemyFormationRole.Ranged => 0.44f,
                    EnemyFormationRole.Support => 0.36f,
                    _ => 0f
                },
                "LEECH" => distanceBias + (role switch
                {
                    EnemyFormationRole.Support => 1.08f,
                    EnemyFormationRole.Controller => 0.74f,
                    EnemyFormationRole.Ranged => 0.38f,
                    EnemyFormationRole.Frontline => 0.32f,
                    EnemyFormationRole.Siege => 0.28f,
                    _ => 0f
                }),
                _ => 0f
            };
        }

        private static bool IsNearRoomEdge(Vector3 worldPosition, Bounds roomBounds)
        {
            float edgeDistanceX = Mathf.Min(Mathf.Abs(worldPosition.x - roomBounds.min.x), Mathf.Abs(roomBounds.max.x - worldPosition.x));
            float edgeDistanceY = Mathf.Min(Mathf.Abs(worldPosition.y - roomBounds.min.y), Mathf.Abs(roomBounds.max.y - worldPosition.y));
            return Mathf.Min(edgeDistanceX, edgeDistanceY) <= 1.1f;
        }

        private static bool IsShotProfileCompareLabel(string compareLabel)
        {
            if (string.IsNullOrWhiteSpace(compareLabel))
            {
                return false;
            }

            string normalized = compareLabel.Trim().ToUpperInvariant();
            return normalized == "SHOT RESET"
                || normalized == "BASELINE"
                || normalized.Contains("LASER")
                || normalized.Contains("ORBIT")
                || normalized.Contains("SHIELD")
                || normalized.Contains("SPLIT")
                || normalized.Contains("BLAST")
                || normalized.Contains("LEECH")
                || normalized.Contains("BOUNCE");
        }

        private static string ResolveShotProfileKeyword(string compareLabel)
        {
            if (string.IsNullOrWhiteSpace(compareLabel))
            {
                return string.Empty;
            }

            string normalized = compareLabel.Trim().ToUpperInvariant();
            if (normalized.Contains("LASER"))
            {
                return "LASER";
            }

            if (normalized.Contains("SPLIT"))
            {
                return "SPLIT";
            }

            if (normalized.Contains("ORBIT"))
            {
                return "ORBIT";
            }

            if (normalized.Contains("SHIELD"))
            {
                return "SHIELD";
            }

            if (normalized.Contains("BOUNCE"))
            {
                return "BOUNCE";
            }

            if (normalized.Contains("BLAST"))
            {
                return "BLAST";
            }

            if (normalized.Contains("LEECH"))
            {
                return "LEECH";
            }

            if (normalized.Contains("SHOT RESET"))
            {
                return "SHOT RESET";
            }

            if (normalized.Contains("BASELINE"))
            {
                return "BASELINE";
            }

            return normalized;
        }

        private static string ResolveShotProfileBossDirective(string compareLabel)
        {
            return ResolveShotProfileKeyword(compareLabel) switch
            {
                "LASER" => "LASER BOSS",
                "SPLIT" => "SPLIT BOSS",
                "ORBIT" => "ORBIT BOSS",
                "SHIELD" => "SHIELD BOSS",
                "BOUNCE" => "BOUNCE BOSS",
                "BLAST" => "BLAST BOSS",
                "LEECH" => "LEECH BOSS",
                "SHOT RESET" => "RESET BOSS",
                _ => "PRESS BOSS"
            };
        }

        private static string ResolveShotProfileEliteDirective(string compareLabel)
        {
            return ResolveShotProfileKeyword(compareLabel) switch
            {
                "LASER" => "LASER ELITE",
                "SPLIT" => "SPLIT ELITE",
                "ORBIT" => "ORBIT ELITE",
                "SHIELD" => "SHIELD ELITE",
                "BOUNCE" => "BOUNCE ELITE",
                "BLAST" => "BLAST ELITE",
                "LEECH" => "LEECH ELITE",
                "SHOT RESET" => "RESET ELITE",
                _ => "CUT ELITE"
            };
        }

        private static string ResolveShotProfileRoleDirective(string compareLabel, EnemyFormationRole role, EnemyFormationPriorityLevel priorityLevel)
        {
            string prefix = ResolveShotProfileKeyword(compareLabel) switch
            {
                "LASER" => "LASER",
                "SPLIT" => "SPLIT",
                "ORBIT" => "ORBIT",
                "SHIELD" => "SHIELD",
                "BOUNCE" => "BOUNCE",
                "BLAST" => "BLAST",
                "LEECH" => "LEECH",
                "SHOT RESET" => "RESET",
                _ => "SHOT"
            };

            return role switch
            {
                EnemyFormationRole.Support => $"{prefix} HEAL",
                EnemyFormationRole.Controller => $"{prefix} CTRL",
                EnemyFormationRole.Ranged => $"{prefix} LINE",
                EnemyFormationRole.Siege => $"{prefix} NEST",
                EnemyFormationRole.Frontline => $"{prefix} FRONT",
                _ => priorityLevel switch
                {
                    EnemyFormationPriorityLevel.Critical => $"{prefix} CUT",
                    EnemyFormationPriorityLevel.Focus => $"{prefix} MARK",
                    _ => $"{prefix} HOLD"
                }
            };
        }

        private static string ResolveRecentCadenceRoleBossDirective(OpeningCadenceVolleyRole recentCadenceRole)
        {
            return $"{ResolveRecentCadenceRolePrefix(recentCadenceRole)} BOSS";
        }

        private static string ResolveRecentCadenceRoleEliteDirective(OpeningCadenceVolleyRole recentCadenceRole)
        {
            return $"{ResolveRecentCadenceRolePrefix(recentCadenceRole)} ELITE";
        }

        private static string ResolveRecentCadenceRoleDirective(OpeningCadenceVolleyRole recentCadenceRole, EnemyFormationRole role, EnemyFormationPriorityLevel priorityLevel)
        {
            string prefix = ResolveRecentCadenceRolePrefix(recentCadenceRole);
            return role switch
            {
                EnemyFormationRole.Support => $"{prefix} HEAL",
                EnemyFormationRole.Controller => $"{prefix} CTRL",
                EnemyFormationRole.Ranged => $"{prefix} LINE",
                EnemyFormationRole.Siege => $"{prefix} NEST",
                EnemyFormationRole.Frontline => $"{prefix} FRONT",
                _ => priorityLevel switch
                {
                    EnemyFormationPriorityLevel.Critical => $"{prefix} CUT",
                    EnemyFormationPriorityLevel.Focus => $"{prefix} MARK",
                    _ => $"{prefix} HOLD"
                }
            };
        }

        private static string ResolveRecentCadenceRolePrefix(OpeningCadenceVolleyRole recentCadenceRole)
        {
            return recentCadenceRole switch
            {
                OpeningCadenceVolleyRole.Core => "CORE",
                OpeningCadenceVolleyRole.Flank => "FLANK",
                OpeningCadenceVolleyRole.Edge => "EDGE",
                _ => "OPEN"
            };
        }

        private static string ResolveRecentCadenceRoleDetailAnchor(OpeningCadenceVolleyRole recentCadenceRole)
        {
            return recentCadenceRole switch
            {
                OpeningCadenceVolleyRole.Core => "CORE LOCK",
                OpeningCadenceVolleyRole.Flank => "FLANK BREAK",
                OpeningCadenceVolleyRole.Edge => "EDGE CLEAR",
                _ => string.Empty
            };
        }

        private static string ComposeRoleAwareDetail(string detail, string cadenceRoleAnchor)
        {
            if (string.IsNullOrWhiteSpace(detail) || string.IsNullOrWhiteSpace(cadenceRoleAnchor))
            {
                return detail;
            }

            return $"{cadenceRoleAnchor} / {detail}";
        }

        private string DecorateActiveTargetCompactTag(string compactTag)
        {
            if (string.IsNullOrWhiteSpace(compactTag))
            {
                return compactTag;
            }

            if (routePlanCarryController != null && routePlanCarryController.HasRecentPreferredImpactDrive)
            {
                string driveTag = routePlanCarryController.RecentPreferredImpactDriveCompactTag;
                return string.IsNullOrWhiteSpace(driveTag)
                    ? compactTag
                    : $"{driveTag}-{compactTag}";
            }

            if (routePlanCarryController != null && routePlanCarryController.HasRecentPreferredImpactHit)
            {
                string hitTag = routePlanCarryController.RecentPreferredImpactHitCompactTag;
                return string.IsNullOrWhiteSpace(hitTag)
                    ? compactTag
                    : $"{hitTag}-{compactTag}";
            }

            if (routePlanCarryController != null && routePlanCarryController.HasPreferredImpactHoldDrive)
            {
                string holdTag = routePlanCarryController.PreferredImpactHoldDriveCompactTag;
                return string.IsNullOrWhiteSpace(holdTag)
                    ? compactTag
                    : $"{holdTag}-{compactTag}";
            }

            if (!TryResolvePreferredSideLabels(out string shortLabel, out _))
            {
                return compactTag;
            }

            return $"{shortLabel}-{compactTag}";
        }

        private string DecorateActiveTargetDirective(string directive)
        {
            if (string.IsNullOrWhiteSpace(directive))
            {
                return directive;
            }

            if (routePlanCarryController != null && routePlanCarryController.HasRecentPreferredImpactDrive)
            {
                string driveDetail = routePlanCarryController.RecentPreferredImpactDriveDetail;
                return string.IsNullOrWhiteSpace(driveDetail)
                    ? directive
                    : $"{driveDetail} {directive}";
            }

            if (routePlanCarryController != null && routePlanCarryController.HasRecentPreferredImpactHit)
            {
                string hitDetail = routePlanCarryController.RecentPreferredImpactHitDetail;
                return string.IsNullOrWhiteSpace(hitDetail)
                    ? directive
                    : $"{hitDetail} {directive}";
            }

            if (routePlanCarryController != null && routePlanCarryController.HasPreferredImpactHoldDrive)
            {
                string holdDetail = routePlanCarryController.PreferredImpactHoldDriveDetail;
                return string.IsNullOrWhiteSpace(holdDetail)
                    ? directive
                    : $"{holdDetail} {directive}";
            }

            if (!TryResolvePreferredSideLabels(out _, out string longLabel))
            {
                return directive;
            }

            return $"{longLabel} {directive}";
        }

        private string DecorateActiveTargetDetail(string detail)
        {
            if (string.IsNullOrWhiteSpace(detail))
            {
                return detail;
            }

            if (routePlanCarryController != null && routePlanCarryController.HasRecentPreferredImpactDrive)
            {
                string driveDetail = routePlanCarryController.RecentPreferredImpactDriveDetail;
                return string.IsNullOrWhiteSpace(driveDetail)
                    ? detail
                    : $"{driveDetail} / {detail}";
            }

            if (routePlanCarryController != null && routePlanCarryController.HasRecentPreferredImpactHit)
            {
                string hitDetail = routePlanCarryController.RecentPreferredImpactHitDetail;
                return string.IsNullOrWhiteSpace(hitDetail)
                    ? detail
                    : $"{hitDetail} / {detail}";
            }

            if (routePlanCarryController != null && routePlanCarryController.HasPreferredImpactHoldDrive)
            {
                string holdDetail = routePlanCarryController.PreferredImpactHoldDriveDetail;
                return string.IsNullOrWhiteSpace(holdDetail)
                    ? detail
                    : $"{holdDetail} / {detail}";
            }

            if (!TryResolvePreferredSideLabels(out _, out string longLabel))
            {
                return detail;
            }

            return $"{longLabel} / {detail}";
        }

        private bool TryResolvePreferredSideLabels(out string shortLabel, out string longLabel)
        {
            shortLabel = string.Empty;
            longLabel = string.Empty;

            if (routePlanCarryController == null
                || _recentCadenceRole == OpeningCadenceVolleyRole.Core
                || _recentCadenceRole == OpeningCadenceVolleyRole.None
                || !routePlanCarryController.TryGetPreferredSecureAnchorSide(out float preferredSide, out _))
            {
                return false;
            }

            shortLabel = preferredSide > 0f ? "L" : "R";
            longLabel = preferredSide > 0f ? "LEFT" : "RIGHT";
            return true;
        }

        private float ResolveHighlightRadius(EnemyHealth target)
        {
            if (target == null)
            {
                return baseHighlightRadius;
            }

            float radius = baseHighlightRadius;
            EnemyFormationModifier formationModifier = target.GetComponent<EnemyFormationModifier>();
            if (formationModifier != null)
            {
                radius += formationModifier.PriorityLevel switch
                {
                    EnemyFormationPriorityLevel.Critical => criticalRadiusBonus,
                    EnemyFormationPriorityLevel.Focus => focusRadiusBonus,
                    _ => 0f
                };
            }

            if (target.GetComponent<BossEnemyController>() != null)
            {
                radius += bossRadiusBonus;
            }

            return radius;
        }

        private static string NormalizeReason(string reasonTag)
        {
            return string.IsNullOrWhiteSpace(reasonTag)
                ? string.Empty
                : reasonTag.Trim().ToUpperInvariant();
        }

        private static string ResolveTargetCompactTag(EnemyHealth target)
        {
            if (target == null)
            {
                return string.Empty;
            }

            BossEnemyController bossEnemyController = target.GetComponent<BossEnemyController>();
            if (bossEnemyController != null)
            {
                return "BOSS";
            }

            EnemyFormationModifier formationModifier = target.GetComponent<EnemyFormationModifier>();
            if (formationModifier != null)
            {
                return formationModifier.FormationRole switch
                {
                    EnemyFormationRole.Support => "HEAL",
                    EnemyFormationRole.Controller => "CTRL",
                    EnemyFormationRole.Ranged => "RNG",
                    EnemyFormationRole.Siege => "NEST",
                    EnemyFormationRole.Frontline => "FRONT",
                    _ => formationModifier.PriorityLevel switch
                    {
                        EnemyFormationPriorityLevel.Critical => "CRIT",
                        EnemyFormationPriorityLevel.Focus => "FOCUS",
                        _ => "MARK"
                    }
                };
            }

            ChampionEnemyModifier championEnemyModifier = target.GetComponent<ChampionEnemyModifier>();
            if (championEnemyModifier != null && championEnemyModifier.IsChampion)
            {
                return "ELITE";
            }

            string enemyId = target.EnemyId;
            if (string.IsNullOrWhiteSpace(enemyId))
            {
                return "MARK";
            }

            enemyId = enemyId.Replace('_', ' ').Trim();
            int separatorIndex = enemyId.IndexOf(' ');
            string compact = separatorIndex > 0 ? enemyId[..separatorIndex] : enemyId;
            return compact.Length <= 5
                ? compact.ToUpperInvariant()
                : compact[..5].ToUpperInvariant();
        }

        private static string ResolveTargetDirective(EnemyHealth target, string reasonTag, string compareLabel, bool safePocketActive, bool impactReliefActive, bool impactSecureHoldActive, bool breakthroughChainReady, OpeningCadenceVolleyRole recentCadenceRole)
        {
            if (target == null)
            {
                return string.Empty;
            }

            BossEnemyController bossEnemyController = target.GetComponent<BossEnemyController>();
            if (bossEnemyController != null)
            {
                if (impactSecureHoldActive)
                {
                    return breakthroughChainReady ? "CHAIN BOSS" : "SECURE BOSS";
                }

                if (recentCadenceRole != OpeningCadenceVolleyRole.None)
                {
                    return ResolveRecentCadenceRoleBossDirective(recentCadenceRole);
                }

                if (impactReliefActive)
                {
                    return "IMPACT BOSS";
                }

                if (breakthroughChainReady)
                {
                    return "BREACH BOSS";
                }

                if (IsShotProfileCompareLabel(compareLabel))
                {
                    return ResolveShotProfileBossDirective(compareLabel);
                }

                return safePocketActive ? "POCKET BOSS" : "PRESS BOSS";
            }

            EnemyFormationModifier formationModifier = target.GetComponent<EnemyFormationModifier>();
            if (formationModifier != null)
            {
                if (impactSecureHoldActive)
                {
                    return formationModifier.FormationRole switch
                    {
                        EnemyFormationRole.Support => breakthroughChainReady ? "CHAIN HEAL" : "SECURE HEAL",
                        EnemyFormationRole.Controller => breakthroughChainReady ? "CHAIN CTRL" : "SECURE CTRL",
                        EnemyFormationRole.Ranged => breakthroughChainReady ? "CHAIN LINE" : "SECURE LINE",
                        EnemyFormationRole.Siege => breakthroughChainReady ? "CHAIN NEST" : "SECURE NEST",
                        EnemyFormationRole.Frontline => breakthroughChainReady ? "CHAIN FRONT" : "SECURE FRONT",
                        _ => formationModifier.PriorityLevel switch
                        {
                            EnemyFormationPriorityLevel.Critical => breakthroughChainReady ? "CHAIN CUT" : "SECURE CUT",
                            EnemyFormationPriorityLevel.Focus => breakthroughChainReady ? "CHAIN MARK" : "SECURE MARK",
                            _ => breakthroughChainReady ? "CHAIN HOLD" : "SECURE HOLD"
                        }
                    };
                }

                if (recentCadenceRole != OpeningCadenceVolleyRole.None)
                {
                    return ResolveRecentCadenceRoleDirective(recentCadenceRole, formationModifier.FormationRole, formationModifier.PriorityLevel);
                }

                if (impactReliefActive)
                {
                    return formationModifier.FormationRole switch
                    {
                        EnemyFormationRole.Support => "IMPACT HEAL",
                        EnemyFormationRole.Controller => "IMPACT CTRL",
                        EnemyFormationRole.Ranged => "IMPACT LINE",
                        EnemyFormationRole.Siege => "IMPACT NEST",
                        EnemyFormationRole.Frontline => "IMPACT FRONT",
                        _ => formationModifier.PriorityLevel switch
                        {
                            EnemyFormationPriorityLevel.Critical => "IMPACT CUT",
                            EnemyFormationPriorityLevel.Focus => "IMPACT MARK",
                            _ => "IMPACT HOLD"
                        }
                    };
                }

                if (breakthroughChainReady)
                {
                    return formationModifier.FormationRole switch
                    {
                        EnemyFormationRole.Support => "BREACH HEAL",
                        EnemyFormationRole.Controller => "BREACH CTRL",
                        EnemyFormationRole.Ranged => "BREACH LINE",
                        EnemyFormationRole.Siege => "BREACH NEST",
                        EnemyFormationRole.Frontline => "BREACH FRONT",
                        _ => formationModifier.PriorityLevel switch
                        {
                            EnemyFormationPriorityLevel.Critical => "BREACH CUT",
                            EnemyFormationPriorityLevel.Focus => "BREACH MARK",
                            _ => "BREACH HOLD"
                        }
                    };
                }

                if (safePocketActive)
                {
                    return formationModifier.FormationRole switch
                    {
                        EnemyFormationRole.Support => "POCKET HEAL",
                        EnemyFormationRole.Controller => "POCKET CTRL",
                        EnemyFormationRole.Ranged => "POCKET LINE",
                        EnemyFormationRole.Siege => "POCKET NEST",
                        EnemyFormationRole.Frontline => "POCKET FRONT",
                        _ => formationModifier.PriorityLevel switch
                        {
                            EnemyFormationPriorityLevel.Critical => "POCKET CUT",
                            EnemyFormationPriorityLevel.Focus => "POCKET MARK",
                            _ => "POCKET HOLD"
                        }
                    };
                }

                if (IsShotProfileCompareLabel(compareLabel))
                {
                    return ResolveShotProfileRoleDirective(compareLabel, formationModifier.FormationRole, formationModifier.PriorityLevel);
                }

                return formationModifier.FormationRole switch
                {
                    EnemyFormationRole.Support => "CUT HEALER",
                    EnemyFormationRole.Controller => "BREAK CTRL",
                    EnemyFormationRole.Ranged => NormalizeReason(reasonTag) == "SUPPLY WINDOW" ? "CLEAR LINE" : "CUT RANGED",
                    EnemyFormationRole.Siege => "CRUSH NEST",
                    EnemyFormationRole.Frontline => "OPEN FRONT",
                    _ => formationModifier.PriorityLevel switch
                    {
                        EnemyFormationPriorityLevel.Critical => "CUT MARK",
                        EnemyFormationPriorityLevel.Focus => "PRESS MARK",
                        _ => "OPEN TARGET"
                    }
                };
            }

            ChampionEnemyModifier championEnemyModifier = target.GetComponent<ChampionEnemyModifier>();
            if (championEnemyModifier != null && championEnemyModifier.IsChampion)
            {
                if (impactSecureHoldActive)
                {
                    return breakthroughChainReady ? "CHAIN ELITE" : "SECURE ELITE";
                }

                if (recentCadenceRole != OpeningCadenceVolleyRole.None)
                {
                    return ResolveRecentCadenceRoleEliteDirective(recentCadenceRole);
                }

                if (impactReliefActive)
                {
                    return "IMPACT ELITE";
                }

                if (breakthroughChainReady)
                {
                    return "BREACH ELITE";
                }

                if (IsShotProfileCompareLabel(compareLabel))
                {
                    return ResolveShotProfileEliteDirective(compareLabel);
                }

                return safePocketActive ? "POCKET ELITE" : "CUT ELITE";
            }

            if (impactSecureHoldActive)
            {
                return breakthroughChainReady ? "CHAIN HOLD" : "SECURE HOLD";
            }

            if (recentCadenceRole != OpeningCadenceVolleyRole.None)
            {
                return ResolveRecentCadenceRoleDirective(recentCadenceRole, EnemyFormationRole.None, EnemyFormationPriorityLevel.None);
            }

            if (impactReliefActive)
            {
                return "IMPACT HOLD";
            }

            if (breakthroughChainReady)
            {
                return "BREACH HOLD";
            }

            return safePocketActive ? "POCKET HOLD" : "OPEN TARGET";
        }

        private static string ResolveTargetDetail(EnemyHealth target, string reasonTag, string compareLabel, bool safePocketActive, bool impactReliefActive, bool impactSecureHoldActive, bool breakthroughChainReady, OpeningCadenceVolleyRole recentCadenceRole)
        {
            if (target == null)
            {
                return string.Empty;
            }

            string cadenceRoleAnchor = ResolveRecentCadenceRoleDetailAnchor(recentCadenceRole);

            BossEnemyController bossEnemyController = target.GetComponent<BossEnemyController>();
            if (bossEnemyController != null)
            {
                string detail = string.IsNullOrWhiteSpace(bossEnemyController.BossDisplayName)
                    ? "PRESS BOSS CORE"
                    : $"PRESS {bossEnemyController.BossDisplayName.ToUpperInvariant()}";
                if (impactSecureHoldActive)
                {
                    return breakthroughChainReady
                        ? ComposeRoleAwareDetail($"SECURE ZONE / CHAIN HOLD / {detail}", cadenceRoleAnchor)
                        : ComposeRoleAwareDetail($"SECURE ZONE / {detail}", cadenceRoleAnchor);
                }

                if (impactReliefActive)
                {
                    return breakthroughChainReady
                        ? ComposeRoleAwareDetail($"IMPACT ZONE / BREACH LANE / {detail}", cadenceRoleAnchor)
                        : ComposeRoleAwareDetail($"IMPACT ZONE / {detail}", cadenceRoleAnchor);
                }

                if (breakthroughChainReady)
                {
                    return ComposeRoleAwareDetail($"BREACH LANE / {detail}", cadenceRoleAnchor);
                }

                return safePocketActive
                    ? ComposeRoleAwareDetail($"SAFE POCKET / {detail}", cadenceRoleAnchor)
                    : ComposeRoleAwareDetail(detail, cadenceRoleAnchor);
            }

            ChampionEnemyModifier championEnemyModifier = target.GetComponent<ChampionEnemyModifier>();
            if (championEnemyModifier != null && championEnemyModifier.IsChampion)
            {
                string variantLabel = championEnemyModifier.VariantLabel;
                string detail = string.IsNullOrWhiteSpace(variantLabel)
                    ? "ELITE THREAT"
                    : $"{variantLabel.ToUpperInvariant()} THREAT";
                if (impactSecureHoldActive)
                {
                    return breakthroughChainReady
                        ? ComposeRoleAwareDetail($"SECURE ZONE / CHAIN HOLD / {detail}", cadenceRoleAnchor)
                        : ComposeRoleAwareDetail($"SECURE ZONE / {detail}", cadenceRoleAnchor);
                }

                if (impactReliefActive)
                {
                    return breakthroughChainReady
                        ? ComposeRoleAwareDetail($"IMPACT ZONE / BREACH LANE / {detail}", cadenceRoleAnchor)
                        : ComposeRoleAwareDetail($"IMPACT ZONE / {detail}", cadenceRoleAnchor);
                }

                if (breakthroughChainReady)
                {
                    return ComposeRoleAwareDetail($"BREACH LANE / {detail}", cadenceRoleAnchor);
                }

                return safePocketActive
                    ? ComposeRoleAwareDetail($"SAFE POCKET / {detail}", cadenceRoleAnchor)
                    : ComposeRoleAwareDetail(detail, cadenceRoleAnchor);
            }

            string directive = ResolveTargetDirective(target, reasonTag, compareLabel, safePocketActive, impactReliefActive, impactSecureHoldActive, breakthroughChainReady, recentCadenceRole);
            EnemyFormationModifier formationModifier = target.GetComponent<EnemyFormationModifier>();
            if (formationModifier == null)
            {
                if (impactSecureHoldActive)
                {
                    return breakthroughChainReady
                        ? ComposeRoleAwareDetail($"SECURE ZONE / CHAIN HOLD / {directive}", cadenceRoleAnchor)
                        : ComposeRoleAwareDetail($"SECURE ZONE / {directive}", cadenceRoleAnchor);
                }

                if (impactReliefActive)
                {
                    return breakthroughChainReady
                        ? ComposeRoleAwareDetail($"IMPACT ZONE / BREACH LANE / {directive}", cadenceRoleAnchor)
                        : ComposeRoleAwareDetail($"IMPACT ZONE / {directive}", cadenceRoleAnchor);
                }

                if (breakthroughChainReady)
                {
                    return ComposeRoleAwareDetail($"BREACH LANE / {directive}", cadenceRoleAnchor);
                }

                return safePocketActive
                    ? ComposeRoleAwareDetail($"SAFE POCKET / {directive}", cadenceRoleAnchor)
                    : ComposeRoleAwareDetail(directive, cadenceRoleAnchor);
            }

            string detailLabel = formationModifier.PriorityLevel switch
            {
                EnemyFormationPriorityLevel.Critical => $"{directive} / CRITICAL",
                EnemyFormationPriorityLevel.Focus => $"{directive} / FOCUS",
                _ => directive
            };

            if (impactSecureHoldActive)
            {
                return breakthroughChainReady
                    ? ComposeRoleAwareDetail($"SECURE ZONE / CHAIN HOLD / {detailLabel}", cadenceRoleAnchor)
                    : ComposeRoleAwareDetail($"SECURE ZONE / {detailLabel}", cadenceRoleAnchor);
            }

            if (impactReliefActive)
            {
                return breakthroughChainReady
                    ? ComposeRoleAwareDetail($"IMPACT ZONE / BREACH LANE / {detailLabel}", cadenceRoleAnchor)
                    : ComposeRoleAwareDetail($"IMPACT ZONE / {detailLabel}", cadenceRoleAnchor);
            }

            if (breakthroughChainReady)
            {
                return ComposeRoleAwareDetail($"BREACH LANE / {detailLabel}", cadenceRoleAnchor);
            }

            return safePocketActive
                ? ComposeRoleAwareDetail($"SAFE POCKET / {detailLabel}", cadenceRoleAnchor)
                : ComposeRoleAwareDetail(detailLabel, cadenceRoleAnchor);
        }

        private static bool IsValidTarget(EnemyHealth candidate, RoomController room)
        {
            return candidate != null
                && room != null
                && !candidate.IsDead
                && candidate.gameObject.activeInHierarchy
                && room.RoomBounds.Contains(candidate.transform.position);
        }

        private void ResetHintState()
        {
            _candidateBuffer.Clear();
            _activeTarget = null;
            _activeRoom = null;
            _activeArenaPressureController = null;
            _activeReasonTag = string.Empty;
            _activeCompareLabel = string.Empty;
            _safePocketCenter = Vector2.zero;
            _safePocketRadius = 0f;
            _hasSafePocket = false;
            _impactReliefCenter = Vector2.zero;
            _impactReliefRadius = 0f;
            _hasImpactRelief = false;
            _impactSecureHoldActive = false;
            _preferredImpactHoldDriveActive = false;
            _preferredImpactHoldDriveSide = 0f;
            _recentPreferredImpactDriveActive = false;
            _recentPreferredImpactDriveSide = 0f;
            _recentPreferredImpactDriveStrength = 0f;
            _recentPreferredImpactHitActive = false;
            _recentPreferredImpactHitSide = 0f;
            _recentPreferredImpactHitStrength = 0f;
            _impactSecureAnchorBuffer.Clear();
            _impactSecureAnchorRadius = 0f;
            _breakthroughChainReady = false;
            _recentCadenceRole = OpeningCadenceVolleyRole.None;
            _recentCadenceRoleWeight = 0f;
            ClearBreakthroughLane();
            _nextRetargetAt = 0f;
            _nextRepulseAt = 0f;
        }

        private bool RefreshSafePocketState(RoomController room)
        {
            bool previousHasSafePocket = _hasSafePocket;
            Vector2 previousSafePocketCenter = _safePocketCenter;
            float previousSafePocketRadius = _safePocketRadius;
            bool previousHasImpactRelief = _hasImpactRelief;
            Vector2 previousImpactReliefCenter = _impactReliefCenter;
            float previousImpactReliefRadius = _impactReliefRadius;

            _activeArenaPressureController = room != null
                ? room.GetComponentInChildren<CombatRoomArenaPressureController>(true)
                : null;

            if (_activeArenaPressureController != null
                && _activeArenaPressureController.TryGetOpeningReliefPocket(out Vector2 center, out float radius, out _, out _))
            {
                _hasSafePocket = true;
                _safePocketCenter = center;
                _safePocketRadius = radius;
            }
            else
            {
                _hasSafePocket = false;
                _safePocketCenter = Vector2.zero;
                _safePocketRadius = 0f;
            }

            if (_activeArenaPressureController != null
                && (_activeArenaPressureController.TryGetOpeningImpactProtectedPocket(out Vector2 impactCenter, out float impactRadius, out _, out _)
                    || _activeArenaPressureController.TryGetOpeningImpactRelief(out impactCenter, out impactRadius, out _, out _)))
            {
                _hasImpactRelief = true;
                _impactReliefCenter = impactCenter;
                _impactReliefRadius = impactRadius;
            }
            else
            {
                _hasImpactRelief = false;
                _impactReliefCenter = Vector2.zero;
                _impactReliefRadius = 0f;
            }

            bool safePocketChanged = previousHasSafePocket != _hasSafePocket
                || (_hasSafePocket
                    && ((previousSafePocketCenter - _safePocketCenter).sqrMagnitude > 0.04f
                        || Mathf.Abs(previousSafePocketRadius - _safePocketRadius) > 0.08f));
            bool impactReliefChanged = previousHasImpactRelief != _hasImpactRelief
                || (_hasImpactRelief
                    && ((previousImpactReliefCenter - _impactReliefCenter).sqrMagnitude > 0.04f
                        || Mathf.Abs(previousImpactReliefRadius - _impactReliefRadius) > 0.08f));
            return safePocketChanged || impactReliefChanged;
        }

        private bool RefreshImpactSecureAnchorState()
        {
            int previousCount = _impactSecureAnchorBuffer.Count;
            float previousRadius = _impactSecureAnchorRadius;
            Vector2[] previousAnchors = previousCount > 0 ? _impactSecureAnchorBuffer.ToArray() : null;

            _impactSecureAnchorBuffer.Clear();
            _impactSecureAnchorRadius = 0f;

            if (routePlanCarryController != null
                && routePlanCarryController.TryGetImpactSecurePulseAnchors(_impactSecureAnchorBuffer, out float anchorRadius, out _))
            {
                _impactSecureAnchorRadius = anchorRadius;
            }

            if (previousCount != _impactSecureAnchorBuffer.Count
                || Mathf.Abs(previousRadius - _impactSecureAnchorRadius) > 0.08f)
            {
                return true;
            }

            if (previousAnchors == null)
            {
                return false;
            }

            for (int index = 0; index < previousAnchors.Length; index++)
            {
                if ((previousAnchors[index] - _impactSecureAnchorBuffer[index]).sqrMagnitude > 0.04f)
                {
                    return true;
                }
            }

            return false;
        }

        private bool HasImpactSecureAnchors()
        {
            return _impactSecureAnchorBuffer.Count > 0 && _impactSecureAnchorRadius > 0.01f;
        }

        private void ResolveReferences()
        {
            if (routePlanCarryController == null)
            {
                routePlanCarryController = GetComponent<PlayerRoutePlanCarryController>();
            }
        }
    }
}
