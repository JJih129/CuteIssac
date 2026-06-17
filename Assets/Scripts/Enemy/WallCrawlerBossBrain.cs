using CuteIssac.Room;
using UnityEngine;

namespace CuteIssac.Enemy
{
    /// <summary>
    /// 2층 벽타기 보스 전용 AI.
    /// 방 경계 안쪽의 외곽 사각 경로만 따라 이동하고, 이동 중 정해진 탄막 패턴을 섞는다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WallCrawlerBossBrain : EnemyBrain
    {
        private enum AttackPattern
        {
            AimedBurst = 0,
            LaneFan = 1,
            Crossfire = 2,
            RadialPulse = 3
        }

        [Header("References")]
        [SerializeField] private EnemyCombat enemyCombat;
        [SerializeField] private BossVisual bossVisual;
        [SerializeField] private RoomController roomController;

        [Header("Wall Route")]
        [Tooltip("방 경계에서 이 거리만큼 안쪽으로 들어온 외곽 라인을 따라 이동한다.")]
        [SerializeField] [Min(0.2f)] private float wallInset = 1.35f;
        [SerializeField] [Min(0.1f)] private float routeProgressSpeed = 2.05f;
        [Tooltip("다음 경로 점을 얼마나 앞서 보며 따라갈지 정한다.")]
        [SerializeField] [Min(0.1f)] private float routeLookAheadDistance = 1.1f;
        [SerializeField] [Range(0.02f, 0.5f)] private float routeSnapDistance = 0.16f;
        [SerializeField] [Min(0.1f)] private float minimumRouteClearance = 1.25f;
        [SerializeField] private bool clockwise = true;
        [SerializeField] private bool startOnTopLane = true;
        [Tooltip("방 경계를 못 찾는 테스트 환경에서 사용할 기본 전투 영역 크기.")]
        [SerializeField] private Vector2 fallbackArenaSize = new(14f, 8f);

        [Header("Route Variation")]
        [SerializeField] [Min(0.2f)] private float movementVariantMinInterval = 1.8f;
        [SerializeField] [Min(0.2f)] private float movementVariantMaxInterval = 3.4f;
        [SerializeField] [Range(0f, 1f)] private float reverseDirectionChance = 0.36f;
        [SerializeField] [Range(0f, 1f)] private float skipAheadChance = 0.42f;
        [SerializeField] [Min(0f)] private float skipAheadMinDistance = 1.1f;
        [SerializeField] [Min(0f)] private float skipAheadMaxDistance = 2.8f;
        [SerializeField] [Min(0f)] private float sprintDuration = 0.65f;
        [SerializeField] [Min(1f)] private float sprintMoveSpeedMultiplier = 1.35f;

        [Header("Attack Cadence")]
        [SerializeField] [Min(0.2f)] private float attackInterval = 1.75f;
        [SerializeField] [Min(0f)] private float firstAttackDelay = 0.85f;
        [SerializeField] [Min(0.05f)] private float telegraphDuration = 0.28f;
        [SerializeField] [Range(0.1f, 1f)] private float attackMoveSpeedMultiplier = 0.55f;
        [Tooltip("중앙선을 지나갈 때 추가 패턴을 발동할 최소 간격.")]
        [SerializeField] [Min(0.2f)] private float centerCrossPatternCooldown = 2.8f;

        [Header("Aimed Burst")]
        [SerializeField] [Min(1)] private int aimedBurstCount = 3;
        [SerializeField] [Min(0f)] private float aimedBurstSpreadAngle = 18f;

        [Header("Lane Fan")]
        [SerializeField] [Min(3)] private int laneFanProjectileCount = 7;
        [SerializeField] [Min(0f)] private float laneFanSpreadAngle = 82f;

        [Header("Crossfire")]
        [SerializeField] [Min(0f)] private float crossfireAngleOffset = 18f;

        [Header("Radial Pulse")]
        [SerializeField] [Min(4)] private int radialProjectileCount = 10;
        [SerializeField] [Min(0f)] private float radialAngleStepOffset = 11f;

        private Bounds _arenaBounds;
        private Vector2 _routeMin;
        private Vector2 _routeMax;
        private Vector2 _lastPosition;
        private float _routeProgress;
        private float _attackCooldownRemaining;
        private float _telegraphRemaining;
        private float _centerCrossCooldownRemaining;
        private float _movementVariantRemaining;
        private float _sprintRemaining;
        private int _patternIndex;
        private float _radialAngleOffset;
        private bool _hasArenaBounds;
        private bool _isTelegraphing;
        private AttackPattern _queuedPattern;

        public bool IsOnUpperLane => _hasArenaBounds && Controller != null && Controller.Position.y >= _arenaBounds.center.y;
        public bool IsTelegraphing => _isTelegraphing;

        protected override void HandleInitialized()
        {
            ResolveReferences();
            HandleResetState();
        }

        protected override void HandleResetState()
        {
            ResolveReferences();
            RefreshArenaBounds();
            _routeProgress = ResolveNearestRouteProgress(Controller != null ? Controller.Position : (Vector2)transform.position);
            if (startOnTopLane && _hasArenaBounds)
            {
                _routeProgress = ResolveNearestRouteProgress(new Vector2(Controller.Position.x, _routeMax.y));
            }

            _lastPosition = Controller != null ? Controller.Position : (Vector2)transform.position;
            _attackCooldownRemaining = Mathf.Max(0f, firstAttackDelay);
            _telegraphRemaining = 0f;
            _centerCrossCooldownRemaining = centerCrossPatternCooldown * 0.5f;
            _movementVariantRemaining = RollMovementVariantInterval();
            _sprintRemaining = 0f;
            _patternIndex = 0;
            _radialAngleOffset = 0f;
            _isTelegraphing = false;
            _queuedPattern = AttackPattern.AimedBurst;
            Controller?.SetMoveSpeedMultiplier(1f);
            bossVisual?.SetTelegraphActive(false, BossPatternType.Fan);
        }

        public override void TickBrain(float fixedDeltaTime)
        {
            if (Controller == null)
            {
                return;
            }

            RefreshArenaBounds();
            TickRouteVariation(fixedDeltaTime);
            TickMovement(fixedDeltaTime);
            TickAttackState(fixedDeltaTime);
            _lastPosition = Controller.Position;
        }

        private void TickMovement(float fixedDeltaTime)
        {
            if (!_hasArenaBounds)
            {
                Vector2 fallbackDirection = Controller.HasTarget
                    ? (Controller.TargetPosition - Controller.Position).normalized
                    : Vector2.zero;
                Controller.SetDesiredMoveDirection(fallbackDirection);
                return;
            }

            float directionSign = clockwise ? 1f : -1f;
            float perimeter = GetRoutePerimeter();
            float routeSpeedMultiplier = ResolveCurrentMoveSpeedMultiplier();
            _routeProgress = RepeatProgress(_routeProgress + (directionSign * routeProgressSpeed * routeSpeedMultiplier * fixedDeltaTime), perimeter);
            float targetProgress = RepeatProgress(_routeProgress + (directionSign * routeLookAheadDistance), perimeter);
            Vector2 targetPoint = EvaluateRoutePoint(targetProgress);
            Vector2 toTarget = targetPoint - Controller.Position;

            if (toTarget.sqrMagnitude <= routeSnapDistance * routeSnapDistance)
            {
                targetProgress = RepeatProgress(_routeProgress + (directionSign * routeLookAheadDistance * 1.5f), perimeter);
                targetPoint = EvaluateRoutePoint(targetProgress);
                toTarget = targetPoint - Controller.Position;
            }

            Controller.SetMoveSpeedMultiplier(routeSpeedMultiplier);
            Controller.SetDesiredMoveDirection(toTarget.sqrMagnitude > 0.0001f ? toTarget.normalized : Vector2.zero);
        }

        private void TickRouteVariation(float fixedDeltaTime)
        {
            _sprintRemaining = Mathf.Max(0f, _sprintRemaining - fixedDeltaTime);
            _movementVariantRemaining = Mathf.Max(0f, _movementVariantRemaining - fixedDeltaTime);

            if (_movementVariantRemaining > 0f || !_hasArenaBounds)
            {
                return;
            }

            float roll = Random.value;
            if (roll < reverseDirectionChance)
            {
                clockwise = !clockwise;
            }
            else if (roll < reverseDirectionChance + skipAheadChance)
            {
                float directionSign = clockwise ? 1f : -1f;
                float skipDistance = Random.Range(skipAheadMinDistance, Mathf.Max(skipAheadMinDistance, skipAheadMaxDistance));
                _routeProgress = RepeatProgress(_routeProgress + (directionSign * skipDistance), GetRoutePerimeter());
            }
            else
            {
                _sprintRemaining = sprintDuration;
            }

            _movementVariantRemaining = RollMovementVariantInterval();
        }

        private float ResolveCurrentMoveSpeedMultiplier()
        {
            float multiplier = _isTelegraphing ? attackMoveSpeedMultiplier : 1f;

            if (_sprintRemaining > 0f)
            {
                multiplier *= sprintMoveSpeedMultiplier;
            }

            return multiplier;
        }

        private void TickAttackState(float fixedDeltaTime)
        {
            _centerCrossCooldownRemaining = Mathf.Max(0f, _centerCrossCooldownRemaining - fixedDeltaTime);

            if (_isTelegraphing)
            {
                _telegraphRemaining = Mathf.Max(0f, _telegraphRemaining - fixedDeltaTime);
                if (_telegraphRemaining <= 0f)
                {
                    FirePattern(_queuedPattern);
                    ClearTelegraph();
                    _attackCooldownRemaining = attackInterval;
                }

                return;
            }

            if (TryTriggerCenterCrossPattern())
            {
                return;
            }

            _attackCooldownRemaining = Mathf.Max(0f, _attackCooldownRemaining - fixedDeltaTime);
            if (_attackCooldownRemaining > 0f)
            {
                return;
            }

            AttackPattern nextPattern = ResolveNextPattern();
            BeginTelegraph(nextPattern);
        }

        private bool TryTriggerCenterCrossPattern()
        {
            if (!_hasArenaBounds || _centerCrossCooldownRemaining > 0f)
            {
                return false;
            }

            float centerX = _arenaBounds.center.x;
            float previous = _lastPosition.x - centerX;
            float current = Controller.Position.x - centerX;

            if (Mathf.Abs(previous) < 0.02f || Mathf.Abs(current) < 0.02f || Mathf.Sign(previous) == Mathf.Sign(current))
            {
                return false;
            }

            _centerCrossCooldownRemaining = centerCrossPatternCooldown;
            BeginTelegraph(AttackPattern.Crossfire);
            return true;
        }

        private AttackPattern ResolveNextPattern()
        {
            AttackPattern pattern = (AttackPattern)(_patternIndex % 4);
            _patternIndex++;
            return pattern;
        }

        private void BeginTelegraph(AttackPattern pattern)
        {
            _queuedPattern = pattern;
            _telegraphRemaining = Mathf.Max(0.05f, telegraphDuration);
            _isTelegraphing = true;
            bossVisual?.SetTelegraphDirection(ResolveAimDirection());
            bossVisual?.SetTelegraphActive(true, ResolveBossPatternType(pattern));
        }

        private void ClearTelegraph()
        {
            _isTelegraphing = false;
            bossVisual?.SetTelegraphActive(false, ResolveBossPatternType(_queuedPattern));
        }

        private void FirePattern(AttackPattern pattern)
        {
            if (enemyCombat == null || !enemyCombat.CanFire)
            {
                return;
            }

            switch (pattern)
            {
                case AttackPattern.LaneFan:
                    FireSpread(ResolveAimDirection(), laneFanProjectileCount, laneFanSpreadAngle);
                    break;
                case AttackPattern.Crossfire:
                    FireCrossfire();
                    break;
                case AttackPattern.RadialPulse:
                    FireRadialPulse();
                    break;
                default:
                    FireSpread(ResolveAimDirection(), aimedBurstCount, aimedBurstSpreadAngle);
                    break;
            }

            bossVisual?.HandleAttack();
        }

        private void FireSpread(Vector2 centerDirection, int projectileCount, float spreadAngle)
        {
            int count = Mathf.Max(1, projectileCount);
            if (count == 1)
            {
                enemyCombat.Fire(centerDirection);
                return;
            }

            float halfSpread = Mathf.Max(0f, spreadAngle) * 0.5f;
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)(count - 1);
                float angle = Mathf.Lerp(-halfSpread, halfSpread, t);
                Vector2 direction = (Quaternion.Euler(0f, 0f, angle) * centerDirection).normalized;
                enemyCombat.Fire(direction);
            }
        }

        private void FireCrossfire()
        {
            Vector2 aimDirection = ResolveAimDirection();
            Vector2 tangent = new Vector2(-aimDirection.y, aimDirection.x).normalized;
            FireAxis(aimDirection);
            FireAxis(tangent);

            Vector2 rotatedAim = (Quaternion.Euler(0f, 0f, crossfireAngleOffset) * aimDirection).normalized;
            Vector2 rotatedTangent = new Vector2(-rotatedAim.y, rotatedAim.x).normalized;
            FireAxis(rotatedAim);
            FireAxis(rotatedTangent);
        }

        private void FireAxis(Vector2 direction)
        {
            enemyCombat.Fire(direction.normalized);
            enemyCombat.Fire((-direction).normalized);
        }

        private void FireRadialPulse()
        {
            int count = Mathf.Max(4, radialProjectileCount);
            float angleStep = 360f / count;

            for (int i = 0; i < count; i++)
            {
                float angle = _radialAngleOffset + (angleStep * i);
                Vector2 direction = (Quaternion.Euler(0f, 0f, angle) * Vector2.right).normalized;
                enemyCombat.Fire(direction);
            }

            _radialAngleOffset += radialAngleStepOffset;
        }

        private Vector2 ResolveAimDirection()
        {
            if (Controller == null || !Controller.HasTarget)
            {
                return IsOnUpperLane ? Vector2.down : Vector2.up;
            }

            Vector2 direction = Controller.TargetPosition - Controller.Position;
            if (direction.sqrMagnitude <= 0.0001f)
            {
                return IsOnUpperLane ? Vector2.down : Vector2.up;
            }

            return direction.normalized;
        }

        private static BossPatternType ResolveBossPatternType(AttackPattern pattern)
        {
            switch (pattern)
            {
                case AttackPattern.LaneFan:
                    return BossPatternType.Fan;
                case AttackPattern.Crossfire:
                    return BossPatternType.Crossfire;
                case AttackPattern.RadialPulse:
                    return BossPatternType.Shockwave;
                default:
                    return BossPatternType.Volley;
            }
        }

        private void RefreshArenaBounds()
        {
            if (roomController == null)
            {
                roomController = GetComponentInParent<RoomController>();
            }

            if (roomController == null)
            {
                roomController = ResolveContainingRoom();
            }

            Bounds bounds = roomController != null
                ? roomController.RoomBounds
                : new Bounds(transform.position, new Vector3(fallbackArenaSize.x, fallbackArenaSize.y, 0f));

            if (bounds.size.x <= 0.1f || bounds.size.y <= 0.1f)
            {
                bounds = new Bounds(transform.position, new Vector3(fallbackArenaSize.x, fallbackArenaSize.y, 0f));
            }

            float resolvedInset = Mathf.Max(wallInset, minimumRouteClearance);
            float insetX = Mathf.Min(resolvedInset, Mathf.Max(0.2f, bounds.extents.x - 0.4f));
            float insetY = Mathf.Min(resolvedInset, Mathf.Max(0.2f, bounds.extents.y - 0.4f));
            _arenaBounds = bounds;
            _routeMin = new Vector2(bounds.min.x + insetX, bounds.min.y + insetY);
            _routeMax = new Vector2(bounds.max.x - insetX, bounds.max.y - insetY);
            _hasArenaBounds = _routeMax.x > _routeMin.x + 0.5f && _routeMax.y > _routeMin.y + 0.5f;
        }

        private RoomController ResolveContainingRoom()
        {
            RoomController[] rooms = FindObjectsByType<RoomController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            Vector2 position = transform.position;

            for (int i = 0; i < rooms.Length; i++)
            {
                RoomController candidate = rooms[i];
                if (candidate == null)
                {
                    continue;
                }

                Bounds bounds = candidate.RoomBounds;
                if (bounds.Contains(position))
                {
                    return candidate;
                }
            }

            return null;
        }

        private float ResolveNearestRouteProgress(Vector2 position)
        {
            RefreshArenaBounds();
            if (!_hasArenaBounds)
            {
                return 0f;
            }

            float width = _routeMax.x - _routeMin.x;
            float height = _routeMax.y - _routeMin.y;
            Vector2 clamped = new(
                Mathf.Clamp(position.x, _routeMin.x, _routeMax.x),
                Mathf.Clamp(position.y, _routeMin.y, _routeMax.y));

            float bottomDistance = Mathf.Abs(clamped.y - _routeMin.y);
            float rightDistance = Mathf.Abs(clamped.x - _routeMax.x);
            float topDistance = Mathf.Abs(clamped.y - _routeMax.y);
            float leftDistance = Mathf.Abs(clamped.x - _routeMin.x);
            float bestDistance = bottomDistance;
            float progress = clamped.x - _routeMin.x;

            if (rightDistance < bestDistance)
            {
                bestDistance = rightDistance;
                progress = width + (clamped.y - _routeMin.y);
            }

            if (topDistance < bestDistance)
            {
                bestDistance = topDistance;
                progress = width + height + (_routeMax.x - clamped.x);
            }

            if (leftDistance < bestDistance)
            {
                progress = width + height + width + (_routeMax.y - clamped.y);
            }

            return RepeatProgress(progress, GetRoutePerimeter());
        }

        private Vector2 EvaluateRoutePoint(float progress)
        {
            float width = _routeMax.x - _routeMin.x;
            float height = _routeMax.y - _routeMin.y;
            progress = RepeatProgress(progress, GetRoutePerimeter());

            if (progress <= width)
            {
                return new Vector2(_routeMin.x + progress, _routeMin.y);
            }

            progress -= width;
            if (progress <= height)
            {
                return new Vector2(_routeMax.x, _routeMin.y + progress);
            }

            progress -= height;
            if (progress <= width)
            {
                return new Vector2(_routeMax.x - progress, _routeMax.y);
            }

            progress -= width;
            return new Vector2(_routeMin.x, _routeMax.y - progress);
        }

        private float GetRoutePerimeter()
        {
            if (!_hasArenaBounds)
            {
                return 1f;
            }

            float width = Mathf.Max(0.1f, _routeMax.x - _routeMin.x);
            float height = Mathf.Max(0.1f, _routeMax.y - _routeMin.y);
            return (width + height) * 2f;
        }

        private static float RepeatProgress(float value, float length)
        {
            return length <= 0.001f ? 0f : Mathf.Repeat(value, length);
        }

        private float RollMovementVariantInterval()
        {
            float min = Mathf.Min(movementVariantMinInterval, movementVariantMaxInterval);
            float max = Mathf.Max(movementVariantMinInterval, movementVariantMaxInterval);
            return Random.Range(min, max);
        }

        private void ResolveReferences()
        {
            if (enemyCombat == null)
            {
                enemyCombat = GetComponent<EnemyCombat>();
            }

            if (bossVisual == null)
            {
                bossVisual = GetComponent<BossVisual>();
            }

            if (roomController == null)
            {
                roomController = GetComponentInParent<RoomController>();
            }
        }

        private void Reset()
        {
            ResolveReferences();
        }

        private void OnValidate()
        {
            ResolveReferences();
        }
    }
}
