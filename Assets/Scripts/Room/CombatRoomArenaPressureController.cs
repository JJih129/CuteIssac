using System.Collections.Generic;
using CuteIssac.Combat;
using CuteIssac.Data.Dungeon;
using CuteIssac.Enemy;
using CuteIssac.Player;
using UnityEngine;

namespace CuteIssac.Room
{
    /// <summary>
    /// Runtime fallback for combat-room setpieces.
    /// It periodically spawns telegraphed pressure zones so boss, miniboss, and challenge rooms gain a room-level movement test.
    /// Authored prefabs can replace the visuals later while keeping this controller and hazard contract intact.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CombatRoomArenaPressureController : MonoBehaviour
    {
        private enum BossPressurePatternMode
        {
            Default = 0,
            BurstCluster = 1,
            ChargeLine = 2,
            VolleyArc = 3,
            SweepWall = 4,
            ShockwaveRing = 5,
            Crossfire = 6,
            SpiralOrbit = 7,
            PhaseCollapse = 8
        }

        [Header("References")]
        [SerializeField] private RoomController roomController;
        [SerializeField] private Transform pressureRoot;
        [SerializeField] private ArenaPressureReliefPocketPresentation reliefPocketPresentation;
        [SerializeField] private PlayerRoutePlanCarryController routePlanCarryController;

        [Header("Runtime")]
        [SerializeField] private RoomType roomType = RoomType.Normal;
        [SerializeField] private Color accentColor = new(0.98f, 0.6f, 0.24f, 1f);

        [Header("Timing")]
        [SerializeField] [Min(0.1f)] private float initialBurstDelay = 1.5f;
        [SerializeField] [Min(0.2f)] private float burstInterval = 3.2f;
        [SerializeField] [Range(1, 4)] private int burstCount = 2;
        [SerializeField] [Min(0.1f)] private float telegraphDuration = 0.9f;

        [Header("Hazard")]
        [SerializeField] [Min(0.25f)] private float hazardRadius = 1.1f;
        [SerializeField] [Min(0f)] private float hazardDamage = 1f;
        [SerializeField] [Min(0f)] private float hazardKnockback = 4f;
        [SerializeField] [Min(0f)] private float targetBiasRadius = 0.8f;
        [SerializeField] [Min(0.1f)] private float spreadRadius = 2f;
        [SerializeField] [Min(0.1f)] private float roomEdgeInset = 1.1f;
        [SerializeField] [Range(0f, 1f)] private float centerWeight = 0.22f;

        [Header("Opening Relief")]
        [SerializeField] [Min(0.2f)] private float openingReliefDuration = 2.1f;
        [SerializeField] [Min(0.1f)] private float openingReliefMinimumBurstDelay = 1.05f;
        [SerializeField] [Range(0f, 1.5f)] private float openingReliefIntensitySuppression = 0.72f;
        [SerializeField] [Range(0f, 1f)] private float openingReliefIntervalBonus = 0.18f;
        [SerializeField] [Range(0f, 1f)] private float openingReliefTelegraphBonus = 0.2f;
        [SerializeField] [Range(0, 2)] private int openingReliefBurstCountReduction = 1;
        [SerializeField] [Min(0.5f)] private float openingReliefSafeRadius = 2.2f;
        [SerializeField] [Range(0f, 1f)] private float openingReliefSafeRadiusBonus = 0.45f;
        [SerializeField] [Min(0f)] private float openingReliefHazardPadding = 0.28f;
        [SerializeField] [Min(0.1f)] private float openingBreakthroughDuration = 1.2f;
        [SerializeField] [Range(0f, 1f)] private float openingBreakthroughSuppressionBonus = 0.22f;
        [SerializeField] [Range(0f, 1f)] private float openingBreakthroughIntervalBonus = 0.12f;
        [SerializeField] [Range(0f, 1f)] private float openingBreakthroughTelegraphBonus = 0.14f;
        [SerializeField] [Range(0f, 1f)] private float openingBreakthroughSafeRadiusBonus = 0.34f;
        [SerializeField] [Range(0, 1)] private int openingBreakthroughBurstCountReduction = 1;
        [SerializeField] [Min(0f)] private float openingBreakthroughLanePadding = 0.42f;
        [SerializeField] [Range(0f, 1f)] private float openingBreakthroughLaneWidthBonus = 0.3f;
        [SerializeField] [Range(0f, 0.4f)] private float recentCadenceRoleCoreLaneWidthBonus = 0.18f;
        [SerializeField] [Range(0.2f, 0.75f)] private float recentCadenceRoleFlankBandOffset = 0.46f;
        [SerializeField] [Range(0.12f, 0.42f)] private float recentCadenceRoleFlankBandWidth = 0.28f;
        [SerializeField] [Range(0.55f, 0.96f)] private float recentCadenceRoleEdgeBandOffset = 0.82f;
        [SerializeField] [Range(0.08f, 0.26f)] private float recentCadenceRoleEdgeBandWidth = 0.16f;
        [SerializeField] [Range(0f, 0.4f)] private float recentCadenceRoleCoreImpactRadiusBonus = 0.2f;
        [SerializeField] [Range(0.16f, 0.62f)] private float recentCadenceRoleFlankImpactOffset = 0.34f;
        [SerializeField] [Range(0.18f, 0.48f)] private float recentCadenceRoleFlankImpactRadius = 0.3f;
        [SerializeField] [Range(0.24f, 0.78f)] private float recentCadenceRoleEdgeImpactOffset = 0.56f;
        [SerializeField] [Range(0.12f, 0.34f)] private float recentCadenceRoleEdgeImpactRadius = 0.2f;
        [SerializeField] [Range(1f, 1.4f)] private float preferredSecureHoldBiasMultiplier = 1.18f;
        [SerializeField] [Range(0f, 1f)] private float preferredSecureHoldBiasFloor = 0.72f;
        [SerializeField] [Range(0f, 1f)] private float preferredSecureHoldChainBiasFloor = 0.92f;
        [SerializeField] [Range(1f, 1.5f)] private float recentPreferredImpactDriveBiasMultiplier = 1.22f;
        [SerializeField] [Range(0f, 1f)] private float recentPreferredImpactDriveBiasFloor = 0.76f;
        [SerializeField] [Range(1f, 1.4f)] private float recentPreferredImpactHitBiasMultiplier = 1.14f;
        [SerializeField] [Range(0f, 1f)] private float recentPreferredImpactHitBiasFloor = 0.64f;

        private readonly List<ArenaPressurePulseHazard> _activeHazards = new();
        private readonly List<Vector2> _hazardPositionBuffer = new();
        private RoomController _boundRoom;
        private BossEnemyController _trackedBossController;
        private bool _pressureActive;
        private float _burstTimer;
        private float _bossDiscoveryCooldown;
        private float _bossPatternModeHoldRemaining;
        private float _patternPhase;
        private int _combatEntryEnemyCount;
        private float _openingReliefExpiresAt;
        private float _openingBreakthroughExpiresAt;
        private float _openingImpactReliefExpiresAt;
        private float _openingImpactReliefStartedAt;
        private float _openingImpactReliefDuration;
        private Vector2 _openingImpactReliefCenter;
        private float _openingImpactReliefRadius;
        private float _baseInitialBurstDelay;
        private float _baseBurstInterval;
        private int _baseBurstCount;
        private float _baseTelegraphDuration;
        private float _baseHazardRadius;
        private float _baseTargetBiasRadius;
        private float _baseSpreadRadius;
        private float _baseCenterWeight;
        private BossPressurePatternMode _bossPressurePatternMode;

        private void OnEnable()
        {
            BindRoomEvents();
            EnsureReliefPocketPresentation();
            EnsureRoutePlanCarryController();
        }

        private void OnDisable()
        {
            _pressureActive = false;
            _openingReliefExpiresAt = 0f;
            _openingBreakthroughExpiresAt = 0f;
            _openingImpactReliefExpiresAt = 0f;
            _openingImpactReliefStartedAt = 0f;
            _openingImpactReliefDuration = 0f;
            _openingImpactReliefCenter = Vector2.zero;
            _openingImpactReliefRadius = 0f;
            ClearActiveHazards();
            UnbindBossController();
            UnbindRoomEvents();
        }

        private void Update()
        {
            CleanupHazardList();

            if (!_pressureActive || roomController == null || roomController.State != RoomState.Combat)
            {
                return;
            }

            if (_bossPatternModeHoldRemaining > 0f)
            {
                _bossPatternModeHoldRemaining = Mathf.Max(0f, _bossPatternModeHoldRemaining - Time.deltaTime);
            }

            TryBindBossControllerIfNeeded();

            if (_trackedBossController == null || (!_trackedBossController.IsPhaseTransitioning && !_trackedBossController.CurrentTelegraphedPattern.HasValue && _bossPatternModeHoldRemaining <= 0f))
            {
                _bossPressurePatternMode = BossPressurePatternMode.Default;
            }

            _burstTimer -= Time.deltaTime;

            if (_burstTimer > 0f)
            {
                return;
            }

            SpawnBurst();
            _burstTimer = ResolveActiveBurstInterval(ResolvePressureIntensity());
        }

        public void Configure(RoomController controller, RoomType configuredRoomType, Color configuredAccentColor)
        {
            roomController = controller;
            roomType = configuredRoomType;
            accentColor = configuredAccentColor;
            ApplyRoomTypeProfile(configuredRoomType);
            CacheBaseProfile();
            _patternPhase = Random.Range(0f, Mathf.PI * 2f);
            BindRoomEvents();
            EnsureReliefPocketPresentation();
            EnsureRoutePlanCarryController();
            ResetBurstTimer();
            _pressureActive = roomController != null && roomController.State == RoomState.Combat && SupportsPressure(roomType);
        }

        public void ApplyOpeningRelief(float durationScale = 1f, float minimumBurstDelay = -1f)
        {
            if (!SupportsPressure(roomType))
            {
                return;
            }

            float duration = Mathf.Max(0.1f, openingReliefDuration * Mathf.Max(0.25f, durationScale));
            _openingReliefExpiresAt = Mathf.Max(_openingReliefExpiresAt, Time.time + duration);

            float resolvedMinimumDelay = minimumBurstDelay > 0f
                ? minimumBurstDelay
                : openingReliefMinimumBurstDelay;
            _burstTimer = Mathf.Max(_burstTimer, Mathf.Max(0.1f, resolvedMinimumDelay));
        }

        public bool TryActivateOpeningBreakthrough(float durationScale = 1f)
        {
            if (!SupportsPressure(roomType) || !HasOpeningRelief())
            {
                return false;
            }

            float duration = Mathf.Max(0.1f, openingBreakthroughDuration * Mathf.Max(0.35f, durationScale));
            _openingBreakthroughExpiresAt = Mathf.Max(_openingBreakthroughExpiresAt, Time.time + duration);
            _openingReliefExpiresAt = Mathf.Max(_openingReliefExpiresAt, Time.time + Mathf.Max(duration, openingReliefDuration * 0.45f));
            _burstTimer = Mathf.Max(_burstTimer, Mathf.Max(0.1f, openingReliefMinimumBurstDelay * 1.15f));
            return true;
        }

        public bool IsOpeningBreakthroughActive => HasOpeningBreakthrough();

        public bool TryGetOpeningReliefPocket(out Vector2 center, out float radius, out Color color, out float normalized)
        {
            center = Vector2.zero;
            radius = 0f;
            color = Color.white;
            normalized = 0f;

            if (!HasOpeningRelief() || roomController == null || roomController.State != RoomState.Combat)
            {
                return false;
            }

            Bounds roomBounds = roomController.RoomBounds;

            if (roomBounds.size.sqrMagnitude <= 0.0001f)
            {
                return false;
            }

            float pressureIntensity = ResolvePressureIntensity();
            float activeHazardRadius = ResolveActiveHazardRadius(pressureIntensity);
            center = ResolvePlayerPosition(roomBounds, activeHazardRadius);
            normalized = ResolveOpeningReliefNormalized();
            radius = ResolveOpeningReliefSafeRadius(activeHazardRadius);
            color = ResolveOpeningReliefPocketColor(normalized);
            return radius > 0.01f;
        }

        public bool TryGetOpeningBreakthroughLaneData(
            out Vector2 origin,
            out Vector2 direction,
            out float length,
            out float halfWidth,
            out Color color,
            out float normalized)
        {
            origin = Vector2.zero;
            direction = Vector2.up;
            length = 0f;
            halfWidth = 0f;
            color = Color.white;
            normalized = 0f;

            if (!HasOpeningBreakthrough() || roomController == null || roomController.State != RoomState.Combat)
            {
                return false;
            }

            Bounds roomBounds = roomController.RoomBounds;
            if (roomBounds.size.sqrMagnitude <= 0.0001f)
            {
                return false;
            }

            float pressureIntensity = ResolvePressureIntensity();
            float activeHazardRadius = ResolveActiveHazardRadius(pressureIntensity);
            if (!TryGetOpeningBreakthroughLane(roomBounds, activeHazardRadius, out origin, out direction, out length, out halfWidth))
            {
                return false;
            }

            normalized = ResolveOpeningBreakthroughNormalized();
            color = ResolveOpeningReliefPocketColor(Mathf.Max(ResolveOpeningReliefNormalized(), normalized));
            return length > 0.01f;
        }

        public bool TryGetOpeningImpactRelief(out Vector2 center, out float radius, out Color color, out float normalized)
        {
            center = Vector2.zero;
            radius = 0f;
            color = Color.white;
            normalized = 0f;

            if (!HasOpeningImpactRelief() || roomController == null || roomController.State != RoomState.Combat)
            {
                return false;
            }

            Bounds roomBounds = roomController.RoomBounds;
            if (roomBounds.size.sqrMagnitude <= 0.0001f)
            {
                return false;
            }

            float pressureIntensity = ResolvePressureIntensity();
            float activeHazardRadius = ResolveActiveHazardRadius(pressureIntensity);
            center = ClampPointToRoom(roomBounds, _openingImpactReliefCenter, roomEdgeInset + activeHazardRadius);
            radius = Mathf.Max(activeHazardRadius * 0.72f, _openingImpactReliefRadius);
            normalized = ResolveOpeningImpactReliefNormalized();
            color = ResolveOpeningImpactReliefColor(normalized);
            return radius > 0.01f;
        }

        public bool TryGetOpeningImpactProtectedPocket(out Vector2 center, out float radius, out Color color, out float normalized)
        {
            center = Vector2.zero;
            radius = 0f;
            color = Color.white;
            normalized = 0f;

            if (!TryGetOpeningImpactRelief(out Vector2 impactCenter, out float impactRadius, out color, out normalized))
            {
                return false;
            }

            center = impactCenter;
            radius = impactRadius;

            if (roomController == null || roomController.State != RoomState.Combat)
            {
                return true;
            }

            Bounds roomBounds = roomController.RoomBounds;
            if (roomBounds.size.sqrMagnitude <= 0.0001f)
            {
                return true;
            }

            float pressureIntensity = ResolvePressureIntensity();
            float activeHazardRadius = ResolveActiveHazardRadius(pressureIntensity);
            Vector2 referencePoint = ResolvePlayerPosition(roomBounds, activeHazardRadius);
            if (!TryResolveOpeningImpactReliefProtectedPocket(
                    roomBounds,
                    impactCenter,
                    impactRadius,
                    activeHazardRadius,
                    referencePoint,
                    out Vector2 protectedPocketCenter,
                    out float protectedPocketRadius))
            {
                return true;
            }

            center = protectedPocketCenter;
            radius = protectedPocketRadius;
            return radius > 0.01f;
        }

        public int ClearHazardsInOpeningBreakthroughLane(float extraPadding = 0f)
        {
            CleanupHazardList();

            if (!TryGetOpeningBreakthroughLaneData(
                    out Vector2 laneOrigin,
                    out Vector2 laneDirection,
                    out float laneLength,
                    out float laneHalfWidth,
                    out _,
                    out _))
            {
                return 0;
            }

            Vector2 laneNormal = new(-laneDirection.y, laneDirection.x);
            int clearedCount = 0;

            for (int index = _activeHazards.Count - 1; index >= 0; index--)
            {
                ArenaPressurePulseHazard hazard = _activeHazards[index];
                if (hazard == null)
                {
                    _activeHazards.RemoveAt(index);
                    continue;
                }

                Vector2 hazardOffset = hazard.WorldPosition - laneOrigin;
                float along = Vector2.Dot(hazardOffset, laneDirection);
                float laneStartAllowance = hazard.Radius * 0.25f;
                float laneEndAllowance = hazard.Radius * 0.35f;
                if (along <= -laneStartAllowance || along >= laneLength + laneEndAllowance)
                {
                    continue;
                }

                float signedLateral = Vector2.Dot(hazardOffset, laneNormal);
                if (!TryResolveOpeningBreakthroughProtectedBand(
                        laneHalfWidth,
                        hazard.Radius,
                        signedLateral,
                        out float protectedBandCenter,
                        out float protectedBandHalfWidth))
                {
                    continue;
                }

                float effectiveHalfWidth = Mathf.Max(
                    0.18f,
                    protectedBandHalfWidth + Mathf.Max(0f, extraPadding) + (hazard.Radius * 0.45f));
                if (Mathf.Abs(signedLateral - protectedBandCenter) > effectiveHalfWidth)
                {
                    continue;
                }

                hazard.ForceDissipate();
                _activeHazards.RemoveAt(index);
                clearedCount++;
            }

            return clearedCount;
        }

        public int ClearHazardsNearPoint(Vector2 point, float radius, float extraPadding = 0f)
        {
            CleanupHazardList();

            float effectiveRadius = Mathf.Max(0.01f, radius + Mathf.Max(0f, extraPadding));
            float effectiveRadiusSqr = effectiveRadius * effectiveRadius;
            int clearedCount = 0;

            for (int index = _activeHazards.Count - 1; index >= 0; index--)
            {
                ArenaPressurePulseHazard hazard = _activeHazards[index];
                if (hazard == null)
                {
                    _activeHazards.RemoveAt(index);
                    continue;
                }

                float hazardReach = effectiveRadius + (hazard.Radius * 0.45f);
                if ((hazard.WorldPosition - point).sqrMagnitude > hazardReach * hazardReach)
                {
                    continue;
                }

                hazard.ForceDissipate();
                _activeHazards.RemoveAt(index);
                clearedCount++;
            }

            return clearedCount;
        }

        public bool TryActivateOpeningImpactRelief(Vector2 center, float radius, float duration)
        {
            if (roomController == null || roomController.State != RoomState.Combat)
            {
                return false;
            }

            float resolvedRadius = Mathf.Max(0.1f, radius);
            float resolvedDuration = Mathf.Max(0.1f, duration);
            float carriedDuration = Mathf.Max(0f, _openingImpactReliefExpiresAt - Time.time);
            _openingImpactReliefCenter = center;
            _openingImpactReliefRadius = Mathf.Max(_openingImpactReliefRadius, resolvedRadius);
            _openingImpactReliefStartedAt = Time.time;
            _openingImpactReliefDuration = Mathf.Max(resolvedDuration, carriedDuration);
            _openingImpactReliefExpiresAt = Time.time + _openingImpactReliefDuration;
            _burstTimer = Mathf.Max(_burstTimer, Mathf.Max(0.1f, openingReliefMinimumBurstDelay * 0.85f));
            return true;
        }

        private void HandleCombatStarted(RoomController startedRoom)
        {
            if (startedRoom != roomController || !SupportsPressure(roomType))
            {
                return;
            }

            _combatEntryEnemyCount = roomController != null ? Mathf.Max(1, roomController.AliveEnemyCount) : 0;
            TryBindBossController(forceImmediate: true);
            _pressureActive = true;
            ResetBurstTimer();
        }

        private void HandleRoomEnded(RoomController endedRoom)
        {
            if (endedRoom != roomController)
            {
                return;
            }

            _pressureActive = false;
            ClearActiveHazards();
            UnbindBossController();
            _bossPressurePatternMode = BossPressurePatternMode.Default;
            _bossPatternModeHoldRemaining = 0f;
            _openingReliefExpiresAt = 0f;
            _openingBreakthroughExpiresAt = 0f;
            _openingImpactReliefExpiresAt = 0f;
            _openingImpactReliefStartedAt = 0f;
            _openingImpactReliefDuration = 0f;
            _openingImpactReliefCenter = Vector2.zero;
            _openingImpactReliefRadius = 0f;
        }

        private void HandleRoomStateChanged(RoomController changedRoom, RoomState state)
        {
            if (changedRoom != roomController)
            {
                return;
            }

            if (state == RoomState.Combat && SupportsPressure(roomType))
            {
                _pressureActive = true;
                ResetBurstTimer();
                return;
            }

            if (state != RoomState.Combat)
            {
                _pressureActive = false;
                ClearActiveHazards();
                UnbindBossController();
                _bossPressurePatternMode = BossPressurePatternMode.Default;
                _bossPatternModeHoldRemaining = 0f;
                _openingReliefExpiresAt = 0f;
                _openingBreakthroughExpiresAt = 0f;
                _openingImpactReliefExpiresAt = 0f;
                _openingImpactReliefStartedAt = 0f;
                _openingImpactReliefDuration = 0f;
                _openingImpactReliefCenter = Vector2.zero;
                _openingImpactReliefRadius = 0f;
            }
        }

        private void SpawnBurst()
        {
            if (roomController == null)
            {
                return;
            }

            Bounds roomBounds = roomController.RoomBounds;

            if (roomBounds.size.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            float pressureIntensity = ResolvePressureIntensity();
            int activeBurstCount = ResolveActiveBurstCount(pressureIntensity);
            float activeTelegraphDuration = ResolveActiveTelegraphDuration(pressureIntensity);
            float activeHazardRadius = ResolveActiveHazardRadius(pressureIntensity);
            float activeTargetBiasRadius = ResolveActiveTargetBiasRadius(pressureIntensity);
            float activeSpreadRadius = ResolveActiveSpreadRadius(pressureIntensity);
            float activeCenterWeight = ResolveActiveCenterWeight(pressureIntensity);
            Vector2 primaryTarget = ResolvePrimaryTarget(roomBounds, activeCenterWeight, activeHazardRadius);
            Vector2 bossAnchorPosition = ResolveBossAnchorPosition(roomBounds, primaryTarget);
            bool usePatternLayout = TryBuildPatternHazardPositions(
                roomBounds,
                bossAnchorPosition,
                primaryTarget,
                activeBurstCount,
                activeHazardRadius,
                activeTargetBiasRadius,
                activeSpreadRadius,
                pressureIntensity);

            if (!usePatternLayout)
            {
                _hazardPositionBuffer.Clear();

                for (int index = 0; index < activeBurstCount; index++)
                {
                    float resolvedRadius = ResolveHazardRadius(index, activeHazardRadius);
                    Vector2 hazardPosition = ResolveHazardPosition(
                        roomBounds,
                        primaryTarget,
                        index,
                        activeBurstCount,
                        activeTargetBiasRadius,
                        activeSpreadRadius,
                        activeHazardRadius);
                    AddClampedHazardPosition(roomBounds, hazardPosition, resolvedRadius);
                }
            }

            for (int index = 0; index < _hazardPositionBuffer.Count; index++)
            {
                Vector2 hazardPosition = _hazardPositionBuffer[index];
                float resolvedRadius = ResolveHazardRadius(index, activeHazardRadius);
                Color resolvedColor = ResolveHazardColor(index, pressureIntensity);
                SpawnHazard(hazardPosition, resolvedRadius, resolvedColor, activeTelegraphDuration);
            }

            _patternPhase += 0.78f;
        }

        private void SpawnHazard(Vector2 position, float radius, Color color, float resolvedTelegraphDuration)
        {
            Transform parent = pressureRoot != null ? pressureRoot : transform;
            GameObject hazardObject = new($"ArenaPulse_{_activeHazards.Count + 1}");
            hazardObject.layer = gameObject.layer;
            hazardObject.transform.SetParent(parent, false);
            hazardObject.transform.position = new Vector3(position.x, position.y, 0f);

            ArenaPressurePulseHazard hazard = hazardObject.AddComponent<ArenaPressurePulseHazard>();
            hazard.Configure(roomController, radius, resolvedTelegraphDuration, hazardDamage, hazardKnockback, color);
            _activeHazards.Add(hazard);
        }

        private Vector2 ResolvePrimaryTarget(Bounds roomBounds, float resolvedCenterWeight, float resolvedHazardRadius)
        {
            Vector2 roomCenter = roomBounds.center;
            Vector2 clampedPlayerPosition = ResolvePlayerPosition(roomBounds, resolvedHazardRadius);
            return Vector2.Lerp(clampedPlayerPosition, roomCenter, resolvedCenterWeight);
        }

        private Vector2 ResolveHazardPosition(
            Bounds roomBounds,
            Vector2 primaryTarget,
            int burstIndex,
            int activeBurstCount,
            float resolvedTargetBiasRadius,
            float resolvedSpreadRadius,
            float resolvedHazardRadius)
        {
            if (burstIndex <= 0)
            {
                return primaryTarget;
            }

            float angleStep = Mathf.PI * 2f / Mathf.Max(2, activeBurstCount);
            float angle = _patternPhase + (angleStep * burstIndex);
            float distance = Mathf.Lerp(resolvedTargetBiasRadius, resolvedSpreadRadius, burstIndex / (float)Mathf.Max(1, activeBurstCount - 1));
            Vector2 offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;
            Vector2 candidate = primaryTarget + offset;

            if (roomType == RoomType.Boss && burstIndex == activeBurstCount - 1)
            {
                float centerOrbitDistance = Mathf.Min(roomBounds.extents.x, roomBounds.extents.y) * 0.42f;
                Vector2 centerOrbit = (Vector2)roomBounds.center + (offset.normalized * centerOrbitDistance);
                candidate = Vector2.Lerp(candidate, centerOrbit, 0.55f);
            }

            return ClampPointToRoom(roomBounds, candidate, roomEdgeInset + ResolveHazardRadius(burstIndex, resolvedHazardRadius));
        }

        private float ResolveHazardRadius(int burstIndex, float activeHazardRadius)
        {
            float sizeMultiplier = 1f + ((burstIndex % 2) * 0.08f);

            if (roomType == RoomType.Boss && burstIndex == 0)
            {
                sizeMultiplier *= 1.12f;
            }

            return activeHazardRadius * sizeMultiplier;
        }

        private Color ResolveHazardColor(int burstIndex, float pressureIntensity)
        {
            float blend = Mathf.Clamp01((0.14f * burstIndex) + (pressureIntensity * 0.08f));
            Color hotColor = ResolvePatternHighlightColor();
            return Color.Lerp(accentColor, hotColor, blend);
        }

        private Vector2 ResolveBossAnchorPosition(Bounds roomBounds, Vector2 fallbackTarget)
        {
            Vector2 bossPosition = _trackedBossController != null
                ? (Vector2)_trackedBossController.transform.position
                : fallbackTarget;
            return ClampPointToRoom(roomBounds, bossPosition, roomEdgeInset + (_baseHazardRadius * 0.45f));
        }

        private bool TryBuildPatternHazardPositions(
            Bounds roomBounds,
            Vector2 bossAnchorPosition,
            Vector2 primaryTarget,
            int activeBurstCount,
            float activeHazardRadius,
            float activeTargetBiasRadius,
            float activeSpreadRadius,
            float pressureIntensity)
        {
            _hazardPositionBuffer.Clear();

            if (roomType != RoomType.Boss || _bossPressurePatternMode == BossPressurePatternMode.Default)
            {
                return false;
            }

            switch (_bossPressurePatternMode)
            {
                case BossPressurePatternMode.BurstCluster:
                    BuildBurstClusterPattern(roomBounds, primaryTarget, activeBurstCount, activeHazardRadius, pressureIntensity);
                    break;
                case BossPressurePatternMode.ChargeLine:
                    BuildChargeLinePattern(roomBounds, bossAnchorPosition, primaryTarget, activeBurstCount, activeHazardRadius, activeSpreadRadius);
                    break;
                case BossPressurePatternMode.VolleyArc:
                    BuildVolleyArcPattern(roomBounds, bossAnchorPosition, primaryTarget, activeBurstCount, activeHazardRadius, activeTargetBiasRadius);
                    break;
                case BossPressurePatternMode.SweepWall:
                    BuildSweepWallPattern(roomBounds, bossAnchorPosition, primaryTarget, activeBurstCount, activeHazardRadius);
                    break;
                case BossPressurePatternMode.ShockwaveRing:
                    BuildShockwaveRingPattern(roomBounds, bossAnchorPosition, activeBurstCount, activeHazardRadius, activeTargetBiasRadius);
                    break;
                case BossPressurePatternMode.Crossfire:
                    BuildCrossfirePattern(roomBounds, bossAnchorPosition, primaryTarget, activeBurstCount, activeHazardRadius, activeSpreadRadius);
                    break;
                case BossPressurePatternMode.SpiralOrbit:
                    BuildSpiralOrbitPattern(roomBounds, bossAnchorPosition, activeBurstCount, activeHazardRadius, activeSpreadRadius);
                    break;
                case BossPressurePatternMode.PhaseCollapse:
                    BuildPhaseCollapsePattern(roomBounds, primaryTarget, activeBurstCount, activeHazardRadius, activeSpreadRadius);
                    break;
            }

            return _hazardPositionBuffer.Count > 0;
        }

        private void BuildBurstClusterPattern(Bounds roomBounds, Vector2 primaryTarget, int activeBurstCount, float activeHazardRadius, float pressureIntensity)
        {
            int hazardCount = Mathf.Clamp(Mathf.Max(3, activeBurstCount), 3, 5);
            float radius = Mathf.Lerp(activeHazardRadius * 0.55f, activeHazardRadius * 1.1f, Mathf.Clamp01(pressureIntensity * 0.4f));

            for (int index = 0; index < hazardCount; index++)
            {
                float angle = _patternPhase + ((Mathf.PI * 2f) / hazardCount * index);
                Vector2 offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                AddClampedHazardPosition(roomBounds, primaryTarget + offset, activeHazardRadius);
            }
        }

        private void BuildChargeLinePattern(Bounds roomBounds, Vector2 bossAnchorPosition, Vector2 primaryTarget, int activeBurstCount, float activeHazardRadius, float activeSpreadRadius)
        {
            int hazardCount = Mathf.Clamp(Mathf.Max(3, activeBurstCount + 1), 3, 5);
            Vector2 direction = ResolveDirectionOrFallback(primaryTarget - bossAnchorPosition, roomBounds.center - (Vector3)bossAnchorPosition, Vector2.right);
            float spacing = Mathf.Max(activeHazardRadius * 1.4f, activeSpreadRadius * 0.55f);

            for (int index = 0; index < hazardCount; index++)
            {
                Vector2 position = bossAnchorPosition + (direction * spacing * (index + 1));
                AddClampedHazardPosition(roomBounds, position, activeHazardRadius);
            }
        }

        private void BuildVolleyArcPattern(Bounds roomBounds, Vector2 bossAnchorPosition, Vector2 primaryTarget, int activeBurstCount, float activeHazardRadius, float activeTargetBiasRadius)
        {
            int hazardCount = Mathf.Clamp(Mathf.Max(3, activeBurstCount + 1), 3, 5);
            Vector2 direction = ResolveDirectionOrFallback(primaryTarget - bossAnchorPosition, roomBounds.center - (Vector3)bossAnchorPosition, Vector2.up);
            Vector2 perpendicular = new Vector2(-direction.y, direction.x);
            float lateralRadius = Mathf.Max(activeTargetBiasRadius * 1.15f, activeHazardRadius * 1.7f);
            float forwardRadius = Mathf.Max(activeHazardRadius * 0.75f, activeTargetBiasRadius * 0.55f);

            for (int index = 0; index < hazardCount; index++)
            {
                float normalized = hazardCount <= 1 ? 0f : (index / (float)(hazardCount - 1)) * 2f - 1f;
                Vector2 position = primaryTarget + (perpendicular * normalized * lateralRadius);
                position += direction * (1f - Mathf.Abs(normalized)) * forwardRadius;
                AddClampedHazardPosition(roomBounds, position, activeHazardRadius);
            }
        }

        private void BuildSweepWallPattern(Bounds roomBounds, Vector2 bossAnchorPosition, Vector2 primaryTarget, int activeBurstCount, float activeHazardRadius)
        {
            int hazardCount = Mathf.Clamp(Mathf.Max(4, activeBurstCount + 1), 4, 6);
            Vector2 direction = ResolveDirectionOrFallback(primaryTarget - bossAnchorPosition, roomBounds.center - (Vector3)primaryTarget, Vector2.right);
            Vector2 wallAxis = Mathf.Abs(direction.x) >= Mathf.Abs(direction.y) ? Vector2.up : Vector2.right;
            float wallHalfSpan = wallAxis == Vector2.up
                ? Mathf.Max(activeHazardRadius, roomBounds.extents.y - roomEdgeInset - activeHazardRadius)
                : Mathf.Max(activeHazardRadius, roomBounds.extents.x - roomEdgeInset - activeHazardRadius);
            Vector2 wallCenter = primaryTarget + (direction * activeHazardRadius * 0.45f);

            for (int index = 0; index < hazardCount; index++)
            {
                float normalized = hazardCount <= 1 ? 0f : (index / (float)(hazardCount - 1)) * 2f - 1f;
                Vector2 position = wallCenter + (wallAxis * normalized * wallHalfSpan);
                AddClampedHazardPosition(roomBounds, position, activeHazardRadius);
            }
        }

        private void BuildShockwaveRingPattern(Bounds roomBounds, Vector2 bossAnchorPosition, int activeBurstCount, float activeHazardRadius, float activeTargetBiasRadius)
        {
            int hazardCount = Mathf.Clamp(Mathf.Max(4, activeBurstCount + 1), 4, 6);
            float ringRadius = Mathf.Max(activeTargetBiasRadius * 1.35f, activeHazardRadius * 1.85f);

            for (int index = 0; index < hazardCount; index++)
            {
                float angle = _patternPhase + ((Mathf.PI * 2f) / hazardCount * index);
                Vector2 offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * ringRadius;
                AddClampedHazardPosition(roomBounds, bossAnchorPosition + offset, activeHazardRadius);
            }

            AddClampedHazardPosition(roomBounds, bossAnchorPosition, activeHazardRadius * 0.9f);
        }

        private void BuildCrossfirePattern(Bounds roomBounds, Vector2 bossAnchorPosition, Vector2 primaryTarget, int activeBurstCount, float activeHazardRadius, float activeSpreadRadius)
        {
            Vector2 crossCenter = ClampPointToRoom(roomBounds, Vector2.Lerp(bossAnchorPosition, primaryTarget, 0.5f), roomEdgeInset + activeHazardRadius);
            float armLength = Mathf.Max(activeSpreadRadius, activeHazardRadius * 2f);
            AddClampedHazardPosition(roomBounds, crossCenter, activeHazardRadius);
            AddClampedHazardPosition(roomBounds, crossCenter + Vector2.right * armLength, activeHazardRadius);
            AddClampedHazardPosition(roomBounds, crossCenter + Vector2.left * armLength, activeHazardRadius);
            AddClampedHazardPosition(roomBounds, crossCenter + Vector2.up * armLength, activeHazardRadius);
            AddClampedHazardPosition(roomBounds, crossCenter + Vector2.down * armLength, activeHazardRadius);

            if (activeBurstCount >= 4)
            {
                float diagonalLength = armLength * 0.72f;
                AddClampedHazardPosition(roomBounds, crossCenter + new Vector2(diagonalLength, diagonalLength), activeHazardRadius);
                AddClampedHazardPosition(roomBounds, crossCenter + new Vector2(-diagonalLength, -diagonalLength), activeHazardRadius);
            }
        }

        private void BuildSpiralOrbitPattern(Bounds roomBounds, Vector2 bossAnchorPosition, int activeBurstCount, float activeHazardRadius, float activeSpreadRadius)
        {
            int hazardCount = Mathf.Clamp(Mathf.Max(4, activeBurstCount + 1), 4, 6);
            float startRadius = Mathf.Max(activeHazardRadius * 1.1f, activeSpreadRadius * 0.45f);
            float endRadius = Mathf.Max(startRadius + activeHazardRadius, activeSpreadRadius * 1.15f);

            for (int index = 0; index < hazardCount; index++)
            {
                float t = hazardCount <= 1 ? 0f : index / (float)(hazardCount - 1);
                float angle = _patternPhase + (t * Mathf.PI * 1.8f);
                float orbitRadius = Mathf.Lerp(startRadius, endRadius, t);
                Vector2 offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * orbitRadius;
                AddClampedHazardPosition(roomBounds, bossAnchorPosition + offset, activeHazardRadius);
            }
        }

        private void BuildPhaseCollapsePattern(Bounds roomBounds, Vector2 primaryTarget, int activeBurstCount, float activeHazardRadius, float activeSpreadRadius)
        {
            Vector2 collapseCenter = ClampPointToRoom(roomBounds, Vector2.Lerp((Vector2)roomBounds.center, primaryTarget, 0.38f), roomEdgeInset + activeHazardRadius);
            float ringRadius = Mathf.Max(activeSpreadRadius * 0.9f, activeHazardRadius * 2.05f);
            int hazardCount = Mathf.Clamp(Mathf.Max(4, activeBurstCount + 1), 4, 6);

            AddClampedHazardPosition(roomBounds, collapseCenter, activeHazardRadius);

            for (int index = 0; index < hazardCount; index++)
            {
                float angle = _patternPhase + ((Mathf.PI * 2f) / hazardCount * index);
                Vector2 offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * ringRadius;
                AddClampedHazardPosition(roomBounds, collapseCenter + offset, activeHazardRadius);
            }
        }

        private void AddClampedHazardPosition(Bounds roomBounds, Vector2 position, float activeHazardRadius)
        {
            Vector2 clampedPosition = ClampPointToRoom(roomBounds, position, roomEdgeInset + activeHazardRadius);
            Vector2 resolvedPosition = ResolveOpeningReliefHazardPosition(roomBounds, clampedPosition, activeHazardRadius);
            _hazardPositionBuffer.Add(resolvedPosition);
        }

        private Vector2 ResolvePlayerPosition(Bounds roomBounds, float resolvedHazardRadius)
        {
            PlayerController playerController = FindFirstObjectByType<PlayerController>(FindObjectsInactive.Exclude);
            Vector2 fallbackPosition = roomBounds.center;
            Vector2 playerPosition = playerController != null
                ? (Vector2)playerController.transform.position
                : fallbackPosition;
            return ClampPointToRoom(roomBounds, playerPosition, roomEdgeInset + resolvedHazardRadius);
        }

        private Vector2 ResolveOpeningReliefHazardPosition(Bounds roomBounds, Vector2 candidate, float activeHazardRadius)
        {
            if (!HasOpeningRelief())
            {
                return candidate;
            }

            Vector2 playerPosition = ResolvePlayerPosition(roomBounds, activeHazardRadius);
            float safeRadius = ResolveOpeningReliefSafeRadius(activeHazardRadius);
            float minimumSeparation = Mathf.Max(activeHazardRadius + openingReliefHazardPadding, activeHazardRadius * 1.15f);
            Vector2 adjustedPosition = candidate;

            if (IsInsideOpeningReliefSafePocket(adjustedPosition, playerPosition, safeRadius))
            {
                adjustedPosition = PushHazardOutsideOpeningSafePocket(
                    roomBounds,
                    adjustedPosition,
                    playerPosition,
                    safeRadius,
                    activeHazardRadius);
            }

            adjustedPosition = ResolveOpeningBreakthroughLanePosition(roomBounds, adjustedPosition, activeHazardRadius);

            if (IsInsideOpeningReliefSafePocket(adjustedPosition, playerPosition, safeRadius)
                || IsTooCloseToBufferedHazards(adjustedPosition, minimumSeparation))
            {
                adjustedPosition = ResolveOpeningReliefOrbitPosition(
                    roomBounds,
                    adjustedPosition,
                    playerPosition,
                    safeRadius,
                    activeHazardRadius,
                    minimumSeparation);
            }

            adjustedPosition = ResolveOpeningBreakthroughLanePosition(roomBounds, adjustedPosition, activeHazardRadius);
            adjustedPosition = ResolveOpeningImpactReliefPosition(roomBounds, adjustedPosition, activeHazardRadius, minimumSeparation);
            return ClampPointToRoom(roomBounds, adjustedPosition, roomEdgeInset + activeHazardRadius);
        }

        private Vector2 ResolveOpeningBreakthroughLanePosition(Bounds roomBounds, Vector2 candidate, float activeHazardRadius)
        {
            if (!TryGetOpeningBreakthroughLane(roomBounds, activeHazardRadius, out Vector2 laneOrigin, out Vector2 laneDirection, out float laneLength, out float laneHalfWidth))
            {
                return candidate;
            }

            Vector2 candidateOffset = candidate - laneOrigin;
            float along = Vector2.Dot(candidateOffset, laneDirection);
            if (along <= activeHazardRadius * 0.15f || along >= laneLength - (activeHazardRadius * 0.2f))
            {
                return candidate;
            }

            Vector2 laneNormal = new(-laneDirection.y, laneDirection.x);
            float lateral = Vector2.Dot(candidateOffset, laneNormal);
            if (!TryResolveOpeningBreakthroughProtectedBand(
                    laneHalfWidth,
                    activeHazardRadius,
                    lateral,
                    out float protectedBandCenter,
                    out float protectedBandHalfWidth))
            {
                return candidate;
            }

            float sign = Mathf.Abs(protectedBandCenter) > 0.04f
                ? Mathf.Sign(protectedBandCenter)
                : Mathf.Abs(lateral) > 0.01f
                    ? Mathf.Sign(lateral)
                    : Mathf.Sign(Mathf.Sin(_patternPhase + along));
            if (Mathf.Abs(sign) < 0.5f)
            {
                sign = 1f;
            }

            float shiftedLateral = sign * (laneHalfWidth + Mathf.Max(activeHazardRadius * 0.28f, protectedBandHalfWidth * 0.36f));
            Vector2 shiftedPosition = laneOrigin + (laneDirection * along) + (laneNormal * shiftedLateral);
            return ClampPointToRoom(roomBounds, shiftedPosition, roomEdgeInset + activeHazardRadius);
        }

        private Vector2 ResolveOpeningImpactReliefPosition(
            Bounds roomBounds,
            Vector2 candidate,
            float activeHazardRadius,
            float minimumSeparation)
        {
            if (!HasOpeningImpactRelief())
            {
                return candidate;
            }

            Vector2 impactCenter = ClampPointToRoom(roomBounds, _openingImpactReliefCenter, roomEdgeInset + activeHazardRadius);
            float impactRadius = Mathf.Max(activeHazardRadius * 1.1f, _openingImpactReliefRadius);
            Vector2 adjustedPosition = candidate;
            Vector2 protectedPocketCenter = impactCenter;
            float protectedPocketRadius = impactRadius;
            bool insideProtectedPocket = TryResolveOpeningImpactReliefProtectedPocket(
                roomBounds,
                impactCenter,
                impactRadius,
                activeHazardRadius,
                adjustedPosition,
                out protectedPocketCenter,
                out protectedPocketRadius);

            if (insideProtectedPocket)
            {
                adjustedPosition = PushHazardOutsideOpeningSafePocket(
                    roomBounds,
                    adjustedPosition,
                    protectedPocketCenter,
                    protectedPocketRadius,
                    activeHazardRadius);
            }

            insideProtectedPocket = TryResolveOpeningImpactReliefProtectedPocket(
                roomBounds,
                impactCenter,
                impactRadius,
                activeHazardRadius,
                adjustedPosition,
                out protectedPocketCenter,
                out protectedPocketRadius);

            if (insideProtectedPocket
                || IsTooCloseToBufferedHazards(adjustedPosition, minimumSeparation))
            {
                adjustedPosition = ResolveOpeningReliefOrbitPosition(
                    roomBounds,
                    adjustedPosition,
                    protectedPocketCenter,
                    protectedPocketRadius,
                    activeHazardRadius,
                    minimumSeparation);
            }

            return adjustedPosition;
        }

        private Vector2 PushHazardOutsideOpeningSafePocket(
            Bounds roomBounds,
            Vector2 candidate,
            Vector2 playerPosition,
            float safeRadius,
            float activeHazardRadius)
        {
            Vector2 fallbackDirection = new(Mathf.Cos(_patternPhase), Mathf.Sin(_patternPhase));
            Vector2 pushDirection = ResolveDirectionOrFallback(
                candidate - playerPosition,
                playerPosition - (Vector2)roomBounds.center,
                fallbackDirection);
            Vector2 pushedPosition = playerPosition + (pushDirection * safeRadius);
            return ClampPointToRoom(roomBounds, pushedPosition, roomEdgeInset + activeHazardRadius);
        }

        private Vector2 ResolveOpeningReliefOrbitPosition(
            Bounds roomBounds,
            Vector2 candidate,
            Vector2 playerPosition,
            float safeRadius,
            float activeHazardRadius,
            float minimumSeparation)
        {
            Vector2 fallbackDirection = new(Mathf.Cos(_patternPhase), Mathf.Sin(_patternPhase));
            Vector2 seedDirection = ResolveDirectionOrFallback(
                candidate - playerPosition,
                playerPosition - (Vector2)roomBounds.center,
                fallbackDirection);
            float seedAngle = Mathf.Atan2(seedDirection.y, seedDirection.x);

            for (int attempt = 0; attempt < 6; attempt++)
            {
                float ringRadius = safeRadius + (activeHazardRadius * (0.35f + (attempt * 0.12f)));
                float signedStep = attempt == 0
                    ? 0f
                    : Mathf.Ceil(attempt * 0.5f) * (attempt % 2 == 0 ? 1f : -1f);
                float angle = seedAngle + (signedStep * 0.58f);
                Vector2 orbitDirection = new(Mathf.Cos(angle), Mathf.Sin(angle));
                Vector2 orbitPosition = playerPosition + (orbitDirection * ringRadius);
                Vector2 clampedOrbitPosition = ClampPointToRoom(roomBounds, orbitPosition, roomEdgeInset + activeHazardRadius);

                if (IsInsideOpeningReliefSafePocket(clampedOrbitPosition, playerPosition, safeRadius))
                {
                    continue;
                }

                if (IsTooCloseToBufferedHazards(clampedOrbitPosition, minimumSeparation))
                {
                    continue;
                }

                return clampedOrbitPosition;
            }

            return candidate;
        }

        private bool IsTooCloseToBufferedHazards(Vector2 candidate, float minimumSeparation)
        {
            float minimumSeparationSqr = minimumSeparation * minimumSeparation;

            for (int index = 0; index < _hazardPositionBuffer.Count; index++)
            {
                if ((_hazardPositionBuffer[index] - candidate).sqrMagnitude < minimumSeparationSqr)
                {
                    return true;
                }
            }

            return false;
        }

        private float ResolveOpeningReliefSafeRadius(float activeHazardRadius)
        {
            if (!HasOpeningRelief())
            {
                return 0f;
            }

            float normalizedRelief = ResolveOpeningReliefNormalized();
            float baseRadius = Mathf.Max(openingReliefSafeRadius, activeHazardRadius * 1.45f);
            float radiusBonus = activeHazardRadius * openingReliefSafeRadiusBonus * normalizedRelief;
            if (HasOpeningBreakthrough())
            {
                radiusBonus += activeHazardRadius * openingBreakthroughSafeRadiusBonus * ResolveOpeningBreakthroughNormalized();
            }

            return baseRadius + radiusBonus;
        }

        private bool HasOpeningImpactRelief()
        {
            return _openingImpactReliefExpiresAt > 0f
                && Time.time < _openingImpactReliefExpiresAt
                && _openingImpactReliefRadius > 0.01f;
        }

        private bool TryGetOpeningBreakthroughLane(
            Bounds roomBounds,
            float activeHazardRadius,
            out Vector2 laneOrigin,
            out Vector2 laneDirection,
            out float laneLength,
            out float laneHalfWidth)
        {
            laneOrigin = Vector2.zero;
            laneDirection = Vector2.up;
            laneLength = 0f;
            laneHalfWidth = 0f;

            if (!HasOpeningBreakthrough())
            {
                return false;
            }

            EnsureRoutePlanCarryController();
            if (routePlanCarryController == null
                || !routePlanCarryController.IsOpeningActive
                || !routePlanCarryController.TryGetOpeningTargetWorldPosition(out Vector2 targetWorldPosition))
            {
                return false;
            }

            laneOrigin = ResolvePlayerPosition(roomBounds, activeHazardRadius);
            Vector2 laneTarget = ClampPointToRoom(roomBounds, targetWorldPosition, roomEdgeInset + activeHazardRadius);
            Vector2 laneVector = laneTarget - laneOrigin;
            laneLength = laneVector.magnitude;
            if (laneLength <= activeHazardRadius * 0.8f)
            {
                return false;
            }

            laneDirection = laneVector / laneLength;
            laneHalfWidth = Mathf.Max(
                activeHazardRadius + openingBreakthroughLanePadding,
                activeHazardRadius * (1.05f + (openingBreakthroughLaneWidthBonus * ResolveOpeningBreakthroughNormalized())));
            return true;
        }

        private Color ResolveOpeningReliefPocketColor(float normalizedRelief)
        {
            Color safeAccent = new(0.58f, 1f, 0.88f, 1f);
            Color blendedAccent = Color.Lerp(accentColor, safeAccent, 0.6f);
            if (HasOpeningBreakthrough())
            {
                blendedAccent = Color.Lerp(blendedAccent, Color.white, 0.12f + (ResolveOpeningBreakthroughNormalized() * 0.18f));
            }

            return Color.Lerp(blendedAccent, Color.white, normalizedRelief * 0.18f);
        }

        private Color ResolveOpeningImpactReliefColor(float normalizedRelief)
        {
            Color pocketColor = ResolveOpeningReliefPocketColor(Mathf.Max(ResolveOpeningReliefNormalized(), normalizedRelief));
            Color impactWarm = Color.Lerp(new Color(1f, 0.9f, 0.62f, 1f), accentColor, 0.28f);
            Color blendedAccent = Color.Lerp(pocketColor, impactWarm, 0.62f);
            return Color.Lerp(blendedAccent, Color.white, 0.14f + (normalizedRelief * 0.26f));
        }

        private bool TryResolveOpeningBreakthroughProtectedBand(
            float laneHalfWidth,
            float activeHazardRadius,
            float signedLateral,
            out float protectedBandCenter,
            out float protectedBandHalfWidth)
        {
            protectedBandCenter = 0f;
            protectedBandHalfWidth = laneHalfWidth;

            if (!TryGetRecentOpeningCadenceRoleLaneProfile(out OpeningCadenceVolleyRole recentRole, out float recentRoleWeight))
            {
                return Mathf.Abs(signedLateral) < laneHalfWidth;
            }

            int bandCount = recentRole == OpeningCadenceVolleyRole.Core ? 1 : 2;
            float closestBandDistance = float.PositiveInfinity;
            bool matched = false;

            for (int bandIndex = 0; bandIndex < bandCount; bandIndex++)
            {
                ResolveOpeningBreakthroughProtectedBand(
                    recentRole,
                    recentRoleWeight,
                    laneHalfWidth,
                    activeHazardRadius,
                    bandIndex,
                    out float bandCenter,
                    out float bandHalfWidth);

                float bandDistance = Mathf.Abs(signedLateral - bandCenter);
                if (bandDistance > bandHalfWidth || bandDistance >= closestBandDistance)
                {
                    continue;
                }

                protectedBandCenter = bandCenter;
                protectedBandHalfWidth = bandHalfWidth;
                closestBandDistance = bandDistance;
                matched = true;
            }

            return matched;
        }

        private void ResolveOpeningBreakthroughProtectedBand(
            OpeningCadenceVolleyRole recentRole,
            float recentRoleWeight,
            float laneHalfWidth,
            float activeHazardRadius,
            int bandIndex,
            out float bandCenter,
            out float bandHalfWidth)
        {
            float normalizedWeight = Mathf.Clamp01(recentRoleWeight);
            float minimumBandHalfWidth = Mathf.Max(activeHazardRadius * 0.2f, laneHalfWidth * 0.12f);

            switch (recentRole)
            {
                case OpeningCadenceVolleyRole.Core:
                    bandCenter = 0f;
                    bandHalfWidth = Mathf.Min(
                        laneHalfWidth * 0.98f,
                        Mathf.Max(minimumBandHalfWidth, laneHalfWidth * Mathf.Lerp(0.72f, 0.72f + recentCadenceRoleCoreLaneWidthBonus, normalizedWeight)));
                    return;

                case OpeningCadenceVolleyRole.Flank:
                {
                    float desiredBandHalfWidth = Mathf.Max(
                        minimumBandHalfWidth,
                        laneHalfWidth * Mathf.Lerp(0.2f, recentCadenceRoleFlankBandWidth, normalizedWeight));
                    float maxCenter = Mathf.Max(0f, (laneHalfWidth * 0.98f) - desiredBandHalfWidth);
                    float desiredCenter = laneHalfWidth * Mathf.Lerp(0.26f, recentCadenceRoleFlankBandOffset, normalizedWeight);
                    float bandSide = bandIndex == 0 ? -1f : 1f;
                    bandHalfWidth = desiredBandHalfWidth;
                    bandCenter = bandSide * Mathf.Min(maxCenter, desiredCenter);
                    if (TryGetPreferredSecureAnchorSideProfile(out float preferredSide, out float preferredSideInfluence))
                    {
                        float sideBias = Mathf.Sign(preferredSide) == Mathf.Sign(bandSide)
                            ? Mathf.Lerp(1f, 1.22f, preferredSideInfluence)
                            : Mathf.Lerp(1f, 0.78f, preferredSideInfluence);
                        bandHalfWidth = Mathf.Min(laneHalfWidth * 0.98f, Mathf.Max(minimumBandHalfWidth, bandHalfWidth * sideBias));
                        bandCenter *= Mathf.Sign(preferredSide) == Mathf.Sign(bandSide)
                            ? Mathf.Lerp(1f, 1.06f, preferredSideInfluence)
                            : Mathf.Lerp(1f, 0.94f, preferredSideInfluence);
                    }
                    return;
                }

                case OpeningCadenceVolleyRole.Edge:
                {
                    float desiredBandHalfWidth = Mathf.Max(
                        minimumBandHalfWidth * 0.9f,
                        laneHalfWidth * Mathf.Lerp(0.1f, recentCadenceRoleEdgeBandWidth, normalizedWeight));
                    float maxCenter = Mathf.Max(0f, (laneHalfWidth * 0.98f) - desiredBandHalfWidth);
                    float desiredCenter = laneHalfWidth * Mathf.Lerp(0.62f, recentCadenceRoleEdgeBandOffset, normalizedWeight);
                    float bandSide = bandIndex == 0 ? -1f : 1f;
                    bandHalfWidth = desiredBandHalfWidth;
                    bandCenter = bandSide * Mathf.Min(maxCenter, desiredCenter);
                    if (TryGetPreferredSecureAnchorSideProfile(out float preferredSide, out float preferredSideInfluence))
                    {
                        float sideBias = Mathf.Sign(preferredSide) == Mathf.Sign(bandSide)
                            ? Mathf.Lerp(1f, 1.28f, preferredSideInfluence)
                            : Mathf.Lerp(1f, 0.72f, preferredSideInfluence);
                        bandHalfWidth = Mathf.Min(laneHalfWidth * 0.98f, Mathf.Max(minimumBandHalfWidth * 0.9f, bandHalfWidth * sideBias));
                        bandCenter *= Mathf.Sign(preferredSide) == Mathf.Sign(bandSide)
                            ? Mathf.Lerp(1f, 1.08f, preferredSideInfluence)
                            : Mathf.Lerp(1f, 0.92f, preferredSideInfluence);
                    }
                    return;
                }

                default:
                    bandCenter = 0f;
                    bandHalfWidth = laneHalfWidth;
                    return;
            }
        }

        private bool TryGetRecentOpeningCadenceRoleLaneProfile(out OpeningCadenceVolleyRole recentRole, out float recentRoleWeight)
        {
            recentRole = OpeningCadenceVolleyRole.None;
            recentRoleWeight = 0f;

            EnsureRoutePlanCarryController();
            if (routePlanCarryController == null
                || !routePlanCarryController.IsOpeningActive
                || !routePlanCarryController.HasRecentOpeningCadenceHitRoleBurst)
            {
                return false;
            }

            recentRole = routePlanCarryController.RecentOpeningCadenceHitRole;
            recentRoleWeight = Mathf.Clamp01(routePlanCarryController.RecentOpeningCadenceHitRoleWeight);
            return recentRole != OpeningCadenceVolleyRole.None && recentRoleWeight > 0.01f;
        }

        private bool TryResolveOpeningImpactReliefProtectedPocket(
            Bounds roomBounds,
            Vector2 impactCenter,
            float impactRadius,
            float activeHazardRadius,
            Vector2 candidate,
            out Vector2 protectedPocketCenter,
            out float protectedPocketRadius)
        {
            protectedPocketCenter = impactCenter;
            protectedPocketRadius = impactRadius;

            if (!TryGetRecentOpeningCadenceRoleLaneProfile(out OpeningCadenceVolleyRole recentRole, out float recentRoleWeight))
            {
                return IsInsideOpeningReliefSafePocket(candidate, impactCenter, impactRadius);
            }

            if (!TryResolveOpeningImpactReliefRoleAxes(roomBounds, impactCenter, activeHazardRadius, out _, out Vector2 pocketNormal))
            {
                return IsInsideOpeningReliefSafePocket(candidate, impactCenter, impactRadius);
            }

            int pocketCount = recentRole == OpeningCadenceVolleyRole.Core ? 1 : 3;
            float closestPocketDistance = float.PositiveInfinity;
            bool matched = false;

            for (int pocketIndex = 0; pocketIndex < pocketCount; pocketIndex++)
            {
                ResolveOpeningImpactReliefProtectedPocket(
                    recentRole,
                    recentRoleWeight,
                    impactCenter,
                    impactRadius,
                    pocketNormal,
                    pocketIndex,
                    out Vector2 pocketCenter,
                    out float pocketRadius);

                float pocketDistance = (candidate - pocketCenter).magnitude;
                if (pocketDistance > pocketRadius || pocketDistance >= closestPocketDistance)
                {
                    continue;
                }

                protectedPocketCenter = pocketCenter;
                protectedPocketRadius = pocketRadius;
                closestPocketDistance = pocketDistance;
                matched = true;
            }

            return matched;
        }

        private bool TryResolveOpeningImpactReliefRoleAxes(
            Bounds roomBounds,
            Vector2 impactCenter,
            float activeHazardRadius,
            out Vector2 pocketDirection,
            out Vector2 pocketNormal)
        {
            pocketDirection = Vector2.up;
            pocketNormal = Vector2.right;

            if (TryGetOpeningBreakthroughLane(roomBounds, activeHazardRadius, out _, out Vector2 laneDirection, out _, out _))
            {
                pocketDirection = laneDirection;
                pocketNormal = new Vector2(-laneDirection.y, laneDirection.x);
                return true;
            }

            EnsureRoutePlanCarryController();
            if (routePlanCarryController != null
                && routePlanCarryController.TryGetOpeningTargetWorldPosition(out Vector2 targetWorldPosition))
            {
                Vector2 toTarget = targetWorldPosition - impactCenter;
                if (toTarget.sqrMagnitude > 0.0001f)
                {
                    pocketDirection = toTarget.normalized;
                    pocketNormal = new Vector2(-pocketDirection.y, pocketDirection.x);
                    return true;
                }
            }

            Vector2 fallbackDirection = ResolveDirectionOrFallback(
                impactCenter - (Vector2)roomBounds.center,
                Vector3.up,
                Vector2.up);
            if (fallbackDirection.sqrMagnitude <= 0.0001f)
            {
                return false;
            }

            pocketDirection = fallbackDirection.normalized;
            pocketNormal = new Vector2(-pocketDirection.y, pocketDirection.x);
            return true;
        }

        private void ResolveOpeningImpactReliefProtectedPocket(
            OpeningCadenceVolleyRole recentRole,
            float recentRoleWeight,
            Vector2 impactCenter,
            float impactRadius,
            Vector2 pocketNormal,
            int pocketIndex,
            out Vector2 pocketCenter,
            out float pocketRadius)
        {
            float normalizedWeight = Mathf.Clamp01(recentRoleWeight);

            switch (recentRole)
            {
                case OpeningCadenceVolleyRole.Core:
                    pocketCenter = impactCenter;
                    pocketRadius = impactRadius * Mathf.Lerp(1f, 1f + recentCadenceRoleCoreImpactRadiusBonus, normalizedWeight);
                    return;

                case OpeningCadenceVolleyRole.Flank:
                {
                    if (pocketIndex == 0)
                    {
                        pocketCenter = impactCenter;
                        pocketRadius = impactRadius * Mathf.Lerp(0.36f, 0.48f, normalizedWeight);
                        return;
                    }

                    float side = pocketIndex == 1 ? -1f : 1f;
                    float offset = impactRadius * Mathf.Lerp(0.2f, recentCadenceRoleFlankImpactOffset, normalizedWeight);
                    pocketCenter = impactCenter + (pocketNormal * side * offset);
                    pocketRadius = impactRadius * Mathf.Lerp(0.22f, recentCadenceRoleFlankImpactRadius, normalizedWeight);
                    if (TryGetPreferredSecureAnchorSideProfile(out float preferredSide, out float preferredSideInfluence))
                    {
                        bool sideMatched = Mathf.Sign(preferredSide) == Mathf.Sign(side);
                        pocketRadius *= sideMatched
                            ? Mathf.Lerp(1f, 1.24f, preferredSideInfluence)
                            : Mathf.Lerp(1f, 0.76f, preferredSideInfluence);
                        pocketCenter = impactCenter + (pocketNormal * side * offset * (sideMatched
                            ? Mathf.Lerp(1f, 1.06f, preferredSideInfluence)
                            : Mathf.Lerp(1f, 0.94f, preferredSideInfluence)));
                    }
                    return;
                }

                case OpeningCadenceVolleyRole.Edge:
                {
                    if (pocketIndex == 0)
                    {
                        pocketCenter = impactCenter;
                        pocketRadius = impactRadius * Mathf.Lerp(0.18f, 0.28f, normalizedWeight);
                        return;
                    }

                    float side = pocketIndex == 1 ? -1f : 1f;
                    float offset = impactRadius * Mathf.Lerp(0.34f, recentCadenceRoleEdgeImpactOffset, normalizedWeight);
                    pocketCenter = impactCenter + (pocketNormal * side * offset);
                    pocketRadius = impactRadius * Mathf.Lerp(0.14f, recentCadenceRoleEdgeImpactRadius, normalizedWeight);
                    if (TryGetPreferredSecureAnchorSideProfile(out float preferredSide, out float preferredSideInfluence))
                    {
                        bool sideMatched = Mathf.Sign(preferredSide) == Mathf.Sign(side);
                        pocketRadius *= sideMatched
                            ? Mathf.Lerp(1f, 1.3f, preferredSideInfluence)
                            : Mathf.Lerp(1f, 0.7f, preferredSideInfluence);
                        pocketCenter = impactCenter + (pocketNormal * side * offset * (sideMatched
                            ? Mathf.Lerp(1f, 1.08f, preferredSideInfluence)
                            : Mathf.Lerp(1f, 0.92f, preferredSideInfluence)));
                    }
                    return;
                }

                default:
                    pocketCenter = impactCenter;
                    pocketRadius = impactRadius;
                    return;
            }
        }

        private bool TryGetPreferredSecureAnchorSideProfile(out float preferredSide, out float preferredSideInfluence)
        {
            preferredSide = 0f;
            preferredSideInfluence = 0f;

            EnsureRoutePlanCarryController();
            if (routePlanCarryController == null)
            {
                return false;
            }

            bool hasPreferredSide = routePlanCarryController.TryGetPreferredSecureAnchorSide(out preferredSide, out preferredSideInfluence);

            if (hasPreferredSide && routePlanCarryController.HasPreferredImpactHoldDrive)
            {
                float influenceFloor = routePlanCarryController.IsImpactSecureHoldChainActive
                    ? preferredSecureHoldChainBiasFloor
                    : preferredSecureHoldBiasFloor;
                preferredSideInfluence = Mathf.Clamp01(Mathf.Max(preferredSideInfluence, influenceFloor));
                preferredSideInfluence = Mathf.Clamp01(preferredSideInfluence * preferredSecureHoldBiasMultiplier);
            }

            if (routePlanCarryController.HasRecentPreferredImpactDrive)
            {
                float recentPreferredDriveSide = Mathf.Sign(routePlanCarryController.RecentPreferredImpactDriveSide);
                float recentPreferredDriveInfluence = Mathf.Clamp01(routePlanCarryController.RecentPreferredImpactDriveStrength);
                if (Mathf.Abs(recentPreferredDriveSide) > 0.5f && recentPreferredDriveInfluence > 0.01f)
                {
                    recentPreferredDriveInfluence = Mathf.Clamp01(Mathf.Max(recentPreferredDriveInfluence, recentPreferredImpactDriveBiasFloor));
                    recentPreferredDriveInfluence = Mathf.Clamp01(recentPreferredDriveInfluence * recentPreferredImpactDriveBiasMultiplier);

                    if (!hasPreferredSide || Mathf.Abs(preferredSide) <= 0.5f || preferredSideInfluence <= 0.01f)
                    {
                        preferredSide = recentPreferredDriveSide;
                        preferredSideInfluence = recentPreferredDriveInfluence;
                        hasPreferredSide = true;
                    }
                    else if (Mathf.Sign(preferredSide) == Mathf.Sign(recentPreferredDriveSide))
                    {
                        preferredSideInfluence = Mathf.Clamp01(Mathf.Lerp(
                            preferredSideInfluence,
                            Mathf.Max(preferredSideInfluence, recentPreferredDriveInfluence),
                            0.74f));
                    }
                    else if (recentPreferredDriveInfluence > preferredSideInfluence * 1.04f)
                    {
                        preferredSide = recentPreferredDriveSide;
                        preferredSideInfluence = Mathf.Clamp01(Mathf.Lerp(preferredSideInfluence, recentPreferredDriveInfluence, 0.78f));
                    }
                }
            }

            if (routePlanCarryController.HasRecentPreferredImpactHit)
            {
                float recentPreferredSide = Mathf.Sign(routePlanCarryController.RecentPreferredImpactHitSide);
                float recentPreferredInfluence = Mathf.Clamp01(routePlanCarryController.RecentPreferredImpactHitStrength);
                if (Mathf.Abs(recentPreferredSide) > 0.5f && recentPreferredInfluence > 0.01f)
                {
                    recentPreferredInfluence = Mathf.Clamp01(Mathf.Max(recentPreferredInfluence, recentPreferredImpactHitBiasFloor));
                    recentPreferredInfluence = Mathf.Clamp01(recentPreferredInfluence * recentPreferredImpactHitBiasMultiplier);

                    if (!hasPreferredSide || Mathf.Abs(preferredSide) <= 0.5f || preferredSideInfluence <= 0.01f)
                    {
                        preferredSide = recentPreferredSide;
                        preferredSideInfluence = recentPreferredInfluence;
                        hasPreferredSide = true;
                    }
                    else if (Mathf.Sign(preferredSide) == Mathf.Sign(recentPreferredSide))
                    {
                        preferredSideInfluence = Mathf.Clamp01(Mathf.Lerp(
                            preferredSideInfluence,
                            Mathf.Max(preferredSideInfluence, recentPreferredInfluence),
                            0.68f));
                    }
                    else if (recentPreferredInfluence > preferredSideInfluence * 1.08f)
                    {
                        preferredSide = recentPreferredSide;
                        preferredSideInfluence = Mathf.Clamp01(Mathf.Lerp(preferredSideInfluence, recentPreferredInfluence, 0.72f));
                    }
                }
            }

            return Mathf.Abs(preferredSide) > 0.5f && preferredSideInfluence > 0.01f;
        }

        private static bool IsInsideOpeningReliefSafePocket(Vector2 candidate, Vector2 playerPosition, float safeRadius)
        {
            return (candidate - playerPosition).sqrMagnitude < safeRadius * safeRadius;
        }

        private static Vector2 ResolveDirectionOrFallback(Vector2 primary, Vector3 secondary, Vector2 fallback)
        {
            if (primary.sqrMagnitude > 0.0001f)
            {
                return primary.normalized;
            }

            Vector2 secondaryDirection = secondary;

            if (secondaryDirection.sqrMagnitude > 0.0001f)
            {
                return secondaryDirection.normalized;
            }

            return fallback.normalized;
        }

        private Color ResolvePatternHighlightColor()
        {
            if (roomType != RoomType.Boss)
            {
                return Color.white;
            }

            return _bossPressurePatternMode switch
            {
                BossPressurePatternMode.ChargeLine => new Color(1f, 0.42f, 0.28f, 1f),
                BossPressurePatternMode.VolleyArc => new Color(1f, 0.78f, 0.3f, 1f),
                BossPressurePatternMode.SweepWall => new Color(0.94f, 0.88f, 0.34f, 1f),
                BossPressurePatternMode.ShockwaveRing => new Color(1f, 0.32f, 0.48f, 1f),
                BossPressurePatternMode.Crossfire => new Color(1f, 0.34f, 0.34f, 1f),
                BossPressurePatternMode.SpiralOrbit => new Color(0.98f, 0.54f, 0.88f, 1f),
                BossPressurePatternMode.PhaseCollapse => new Color(1f, 0.26f, 0.3f, 1f),
                BossPressurePatternMode.BurstCluster => new Color(1f, 0.62f, 0.24f, 1f),
                _ => new Color(1f, 0.34f, 0.3f, 1f)
            };
        }

        private static Vector2 ClampPointToRoom(Bounds roomBounds, Vector2 point, float inset)
        {
            Vector3 min = roomBounds.min + new Vector3(inset, inset, 0f);
            Vector3 max = roomBounds.max - new Vector3(inset, inset, 0f);

            if (min.x > max.x)
            {
                min.x = max.x = roomBounds.center.x;
            }

            if (min.y > max.y)
            {
                min.y = max.y = roomBounds.center.y;
            }

            return new Vector2(
                Mathf.Clamp(point.x, min.x, max.x),
                Mathf.Clamp(point.y, min.y, max.y));
        }

        private void ApplyRoomTypeProfile(RoomType configuredRoomType)
        {
            switch (configuredRoomType)
            {
                case RoomType.Boss:
                    initialBurstDelay = 1.15f;
                    burstInterval = 2.8f;
                    burstCount = 3;
                    telegraphDuration = 0.82f;
                    hazardRadius = 1.22f;
                    hazardDamage = 1f;
                    hazardKnockback = 5.6f;
                    targetBiasRadius = 0.95f;
                    spreadRadius = 2.55f;
                    roomEdgeInset = 1.18f;
                    centerWeight = 0.28f;
                    break;
                case RoomType.MiniBoss:
                    initialBurstDelay = 1.35f;
                    burstInterval = 3.35f;
                    burstCount = 2;
                    telegraphDuration = 0.9f;
                    hazardRadius = 1.12f;
                    hazardDamage = 1f;
                    hazardKnockback = 4.8f;
                    targetBiasRadius = 0.82f;
                    spreadRadius = 2.1f;
                    roomEdgeInset = 1.08f;
                    centerWeight = 0.2f;
                    break;
                case RoomType.Challenge:
                    initialBurstDelay = 1.7f;
                    burstInterval = 4.1f;
                    burstCount = 2;
                    telegraphDuration = 0.96f;
                    hazardRadius = 1.04f;
                    hazardDamage = 1f;
                    hazardKnockback = 4.2f;
                    targetBiasRadius = 0.74f;
                    spreadRadius = 1.82f;
                    roomEdgeInset = 1.04f;
                    centerWeight = 0.16f;
                    break;
                default:
                    initialBurstDelay = 1.5f;
                    burstInterval = 3.2f;
                    burstCount = 1;
                    telegraphDuration = 0.9f;
                    hazardRadius = 1f;
                    hazardDamage = 1f;
                    hazardKnockback = 4f;
                    targetBiasRadius = 0.8f;
                    spreadRadius = 2f;
                    roomEdgeInset = 1.1f;
                    centerWeight = 0.2f;
                    break;
            }
        }

        private void ResetBurstTimer()
        {
            _burstTimer = ResolveActiveInitialBurstDelay(ResolvePressureIntensity());
        }

        private void CleanupHazardList()
        {
            for (int index = _activeHazards.Count - 1; index >= 0; index--)
            {
                if (_activeHazards[index] == null)
                {
                    _activeHazards.RemoveAt(index);
                }
            }
        }

        private void ClearActiveHazards()
        {
            for (int index = _activeHazards.Count - 1; index >= 0; index--)
            {
                ArenaPressurePulseHazard hazard = _activeHazards[index];

                if (hazard == null)
                {
                    continue;
                }

                if (Application.isPlaying)
                {
                    Destroy(hazard.gameObject);
                }
                else
                {
                    DestroyImmediate(hazard.gameObject);
                }
            }

            _activeHazards.Clear();
        }

        private void CacheBaseProfile()
        {
            _baseInitialBurstDelay = initialBurstDelay;
            _baseBurstInterval = burstInterval;
            _baseBurstCount = burstCount;
            _baseTelegraphDuration = telegraphDuration;
            _baseHazardRadius = hazardRadius;
            _baseTargetBiasRadius = targetBiasRadius;
            _baseSpreadRadius = spreadRadius;
            _baseCenterWeight = centerWeight;
        }

        private void EnsureReliefPocketPresentation()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            if (reliefPocketPresentation == null)
            {
                reliefPocketPresentation = GetComponent<ArenaPressureReliefPocketPresentation>();
            }

            if (reliefPocketPresentation == null)
            {
                reliefPocketPresentation = gameObject.AddComponent<ArenaPressureReliefPocketPresentation>();
            }
        }

        private void EnsureRoutePlanCarryController()
        {
            if (!Application.isPlaying || routePlanCarryController != null)
            {
                return;
            }

            routePlanCarryController = FindFirstObjectByType<PlayerRoutePlanCarryController>(FindObjectsInactive.Exclude);
        }

        private void TryBindBossControllerIfNeeded()
        {
            if (roomType != RoomType.Boss || _trackedBossController != null)
            {
                return;
            }

            _bossDiscoveryCooldown -= Time.deltaTime;

            if (_bossDiscoveryCooldown > 0f)
            {
                return;
            }

            TryBindBossController();
            _bossDiscoveryCooldown = _trackedBossController == null ? 0.45f : 0f;
        }

        private void TryBindBossController(bool forceImmediate = false)
        {
            if (roomType != RoomType.Boss || roomController == null || _trackedBossController != null)
            {
                return;
            }

            if (!forceImmediate && _bossDiscoveryCooldown > 0f)
            {
                return;
            }

            BossEnemyController[] bossControllers = roomController.GetComponentsInChildren<BossEnemyController>(true);

            for (int index = 0; index < bossControllers.Length; index++)
            {
                BossEnemyController candidate = bossControllers[index];

                if (candidate == null)
                {
                    continue;
                }

                EnemyHealth enemyHealth = candidate.GetComponent<EnemyHealth>();

                if (enemyHealth != null && enemyHealth.IsDead)
                {
                    continue;
                }

                BindBossController(candidate);
                return;
            }
        }

        private void BindBossController(BossEnemyController bossController)
        {
            if (bossController == null || bossController == _trackedBossController)
            {
                return;
            }

            UnbindBossController();
            _trackedBossController = bossController;
            _trackedBossController.PhaseChanged += HandleBossPhaseChanged;
            _trackedBossController.EnrageChanged += HandleBossEnrageChanged;
            _trackedBossController.TelegraphPatternChanged += HandleBossTelegraphPatternChanged;
            _bossPressurePatternMode = ResolveBossPressurePatternMode(_trackedBossController.CurrentTelegraphedPattern, _trackedBossController.IsPhaseTransitioning);
            ExpediteNextBurst(0.34f);
        }

        private void UnbindBossController()
        {
            if (_trackedBossController == null)
            {
                return;
            }

            _trackedBossController.PhaseChanged -= HandleBossPhaseChanged;
            _trackedBossController.EnrageChanged -= HandleBossEnrageChanged;
            _trackedBossController.TelegraphPatternChanged -= HandleBossTelegraphPatternChanged;
            _trackedBossController = null;
        }

        private void HandleBossPhaseChanged(BossPhaseType _)
        {
            ExpediteNextBurst(0.28f);
        }

        private void HandleBossEnrageChanged(bool isEnraged)
        {
            if (!isEnraged)
            {
                return;
            }

            ExpediteNextBurst(0.18f);
        }

        private void HandleBossTelegraphPatternChanged(BossPatternType? patternType, bool isPhaseTransitioning)
        {
            if (isPhaseTransitioning || patternType.HasValue)
            {
                _bossPressurePatternMode = ResolveBossPressurePatternMode(patternType, isPhaseTransitioning);
                _bossPatternModeHoldRemaining = isPhaseTransitioning ? 1.15f : 0.9f;
                ExpediteNextBurst(isPhaseTransitioning ? 0.16f : 0.22f);
                return;
            }

            if (_bossPatternModeHoldRemaining <= 0f)
            {
                _bossPressurePatternMode = BossPressurePatternMode.Default;
            }
        }

        private static BossPressurePatternMode ResolveBossPressurePatternMode(BossPatternType? patternType, bool isPhaseTransitioning)
        {
            if (isPhaseTransitioning)
            {
                return BossPressurePatternMode.PhaseCollapse;
            }

            return patternType switch
            {
                BossPatternType.Burst => BossPressurePatternMode.BurstCluster,
                BossPatternType.Charge => BossPressurePatternMode.ChargeLine,
                BossPatternType.Volley => BossPressurePatternMode.VolleyArc,
                BossPatternType.Fan => BossPressurePatternMode.VolleyArc,
                BossPatternType.Sweep => BossPressurePatternMode.SweepWall,
                BossPatternType.Shockwave => BossPressurePatternMode.ShockwaveRing,
                BossPatternType.Crossfire => BossPressurePatternMode.Crossfire,
                BossPatternType.Spiral => BossPressurePatternMode.SpiralOrbit,
                _ => BossPressurePatternMode.Default
            };
        }

        private void ExpediteNextBurst(float leadTime)
        {
            _burstTimer = Mathf.Min(_burstTimer, Mathf.Max(0.1f, leadTime));
        }

        private float ResolvePressureIntensity()
        {
            float intensity = 0f;

            if (roomType == RoomType.Boss)
            {
                if (_trackedBossController == null)
                {
                    return intensity;
                }

                intensity += _trackedBossController.CurrentPhase switch
                {
                    BossPhaseType.PhaseThree => 1.55f,
                    BossPhaseType.PhaseTwo => 0.82f,
                    _ => 0f
                };

                if (_trackedBossController.IsEnraged)
                {
                    intensity += 0.65f;
                }

                return Mathf.Max(0f, intensity - ResolveOpeningReliefSuppression());
            }

            if (roomType == RoomType.MiniBoss)
            {
                int baselineCount = Mathf.Max(1, _combatEntryEnemyCount);
                int aliveCount = roomController != null ? Mathf.Max(0, roomController.AliveEnemyCount) : baselineCount;
                float completion = 1f - Mathf.Clamp01(aliveCount / (float)baselineCount);
                intensity += Mathf.Lerp(0f, 0.8f, completion);
                return Mathf.Max(0f, intensity - ResolveOpeningReliefSuppression());
            }

            if (roomType == RoomType.Challenge && roomController != null)
            {
                if (roomController.CurrentCombatDuration >= 32f)
                {
                    intensity += 0.6f;
                }
                else if (roomController.CurrentCombatDuration >= 18f)
                {
                    intensity += 0.32f;
                }
            }

            return Mathf.Max(0f, intensity - ResolveOpeningReliefSuppression());
        }

        private float ResolveActiveInitialBurstDelay(float pressureIntensity)
        {
            float reduction = Mathf.Min(0.3f, pressureIntensity * 0.08f);
            return Mathf.Max(0.1f, _baseInitialBurstDelay * (1f - reduction));
        }

        private float ResolveActiveBurstInterval(float pressureIntensity)
        {
            float reduction = Mathf.Min(0.34f, pressureIntensity * 0.15f);
            float interval = Mathf.Max(0.9f, _baseBurstInterval * (1f - reduction));
            if (HasOpeningRelief())
            {
                interval *= 1f + Mathf.Lerp(0.04f, openingReliefIntervalBonus, ResolveOpeningReliefNormalized());
            }

            if (HasOpeningBreakthrough())
            {
                interval *= 1f + Mathf.Lerp(0.04f, openingBreakthroughIntervalBonus, ResolveOpeningBreakthroughNormalized());
            }

            return interval;
        }

        private int ResolveActiveBurstCount(float pressureIntensity)
        {
            int bonusCount = pressureIntensity >= 1.4f
                ? 2
                : pressureIntensity >= 0.6f
                    ? 1
                    : 0;
            int maxBurstCount = roomType == RoomType.Boss ? 5 : 4;
            int resolvedCount = Mathf.Clamp(_baseBurstCount + bonusCount, 1, maxBurstCount);
            if (HasOpeningRelief())
            {
                resolvedCount = Mathf.Max(1, resolvedCount - openingReliefBurstCountReduction);
            }

            if (HasOpeningBreakthrough())
            {
                resolvedCount = Mathf.Max(1, resolvedCount - openingBreakthroughBurstCountReduction);
            }

            return resolvedCount;
        }

        private float ResolveActiveTelegraphDuration(float pressureIntensity)
        {
            float reduction = Mathf.Min(0.24f, pressureIntensity * 0.08f);
            float duration = Mathf.Max(0.45f, _baseTelegraphDuration * (1f - reduction));
            if (HasOpeningRelief())
            {
                duration *= 1f + Mathf.Lerp(0.06f, openingReliefTelegraphBonus, ResolveOpeningReliefNormalized());
            }

            if (HasOpeningBreakthrough())
            {
                duration *= 1f + Mathf.Lerp(0.04f, openingBreakthroughTelegraphBonus, ResolveOpeningBreakthroughNormalized());
            }

            return duration;
        }

        private float ResolveActiveHazardRadius(float pressureIntensity)
        {
            float bonus = Mathf.Min(0.22f, pressureIntensity * 0.08f);
            return _baseHazardRadius * (1f + bonus);
        }

        private float ResolveActiveTargetBiasRadius(float pressureIntensity)
        {
            float bonus = Mathf.Min(0.24f, pressureIntensity * 0.09f);
            return _baseTargetBiasRadius * (1f + bonus);
        }

        private float ResolveActiveSpreadRadius(float pressureIntensity)
        {
            float bonus = Mathf.Min(0.28f, pressureIntensity * 0.1f);
            return _baseSpreadRadius * (1f + bonus);
        }

        private float ResolveActiveCenterWeight(float pressureIntensity)
        {
            return Mathf.Clamp01(_baseCenterWeight + Mathf.Min(0.16f, pressureIntensity * 0.06f));
        }

        private bool HasOpeningRelief()
        {
            return _openingReliefExpiresAt > 0f && Time.time < _openingReliefExpiresAt;
        }

        private bool HasOpeningBreakthrough()
        {
            return _openingBreakthroughExpiresAt > 0f && Time.time < _openingBreakthroughExpiresAt;
        }

        private float ResolveOpeningReliefNormalized()
        {
            if (!HasOpeningRelief())
            {
                return 0f;
            }

            float remaining = Mathf.Max(0f, _openingReliefExpiresAt - Time.time);
            float duration = Mathf.Max(0.1f, openingReliefDuration);
            return Mathf.Clamp01(remaining / duration);
        }

        private float ResolveOpeningBreakthroughNormalized()
        {
            if (!HasOpeningBreakthrough())
            {
                return 0f;
            }

            float remaining = Mathf.Max(0f, _openingBreakthroughExpiresAt - Time.time);
            float duration = Mathf.Max(0.1f, openingBreakthroughDuration);
            return Mathf.Clamp01(remaining / duration);
        }

        private float ResolveOpeningImpactReliefNormalized()
        {
            if (!HasOpeningImpactRelief())
            {
                return 0f;
            }

            float elapsed = Mathf.Max(0f, Time.time - _openingImpactReliefStartedAt);
            float duration = Mathf.Max(0.1f, _openingImpactReliefDuration);
            return 1f - Mathf.Clamp01(elapsed / duration);
        }

        private float ResolveOpeningReliefSuppression()
        {
            if (!HasOpeningRelief())
            {
                return 0f;
            }

            float suppression = openingReliefIntensitySuppression * ResolveOpeningReliefNormalized();
            if (HasOpeningBreakthrough())
            {
                suppression += openingBreakthroughSuppressionBonus * ResolveOpeningBreakthroughNormalized();
            }

            return suppression;
        }

        private void BindRoomEvents()
        {
            if (roomController == null || roomController == _boundRoom)
            {
                return;
            }

            UnbindRoomEvents();
            _boundRoom = roomController;
            _boundRoom.CombatStarted += HandleCombatStarted;
            _boundRoom.RoomCleared += HandleRoomEnded;
            _boundRoom.NonCombatResolved += HandleRoomEnded;
            _boundRoom.StateChanged += HandleRoomStateChanged;
        }

        private void UnbindRoomEvents()
        {
            if (_boundRoom == null)
            {
                return;
            }

            _boundRoom.CombatStarted -= HandleCombatStarted;
            _boundRoom.RoomCleared -= HandleRoomEnded;
            _boundRoom.NonCombatResolved -= HandleRoomEnded;
            _boundRoom.StateChanged -= HandleRoomStateChanged;
            _boundRoom = null;
        }

        private static bool SupportsPressure(RoomType configuredRoomType)
        {
            return configuredRoomType == RoomType.Boss
                || configuredRoomType == RoomType.MiniBoss
                || configuredRoomType == RoomType.Challenge;
        }
    }
}
