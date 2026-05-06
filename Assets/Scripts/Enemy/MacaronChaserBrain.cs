using UnityEngine;

namespace CuteIssac.Enemy
{
    /// <summary>
    /// Macaron-specific contact chaser: stalks slowly, then accelerates into a short fast chase without dash teleport timing.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MacaronChaserBrain : EnemyBrain
    {
        [Header("Chase Cycle")]
        [SerializeField] [Min(0.1f)] private float slowChaseDuration = 2.1f;
        [SerializeField] [Min(0.1f)] private float fastChaseDuration = 1.25f;
        [SerializeField] [Min(0f)] private float recoveryDuration = 0.35f;
        [SerializeField] [Min(0f)] private float fastTriggerRange = 6f;
        [SerializeField] [Min(0f)] private float slowSpeedMultiplier = 0.72f;
        [SerializeField] [Min(1f)] private float fastSpeedMultiplier = 1.65f;
        [SerializeField] [Min(0f)] private float recoverySpeedMultiplier = 0.9f;

        [Header("Approach")]
        [SerializeField] [Min(0f)] private float orbitRange = 1.45f;
        [SerializeField] [Range(0f, 1f)] private float orbitBlend = 0.25f;

        private enum ChaseState
        {
            Slow = 0,
            Fast = 1,
            Recovery = 2
        }

        private ChaseState _state;
        private float _stateTimer;
        private float _orbitSign = 1f;
        private Vector2 _lastChaseDirection = Vector2.down;

        public bool IsFastChasing => _state == ChaseState.Fast;
        public bool IsRecovering => _state == ChaseState.Recovery;
        public Vector2 LastChaseDirection => _lastChaseDirection;
        public float FastChaseProgressNormalized => _state == ChaseState.Fast && fastChaseDuration > 0f
            ? 1f - Mathf.Clamp01(_stateTimer / fastChaseDuration)
            : 0f;

        protected override void HandleInitialized()
        {
            HandleResetState();
        }

        protected override void HandleResetState()
        {
            _state = ChaseState.Slow;
            _stateTimer = slowChaseDuration;
            _orbitSign = (GetInstanceID() & 1) == 0 ? 1f : -1f;
            _lastChaseDirection = Vector2.down;
        }

        public override void TickBrain(float fixedDeltaTime)
        {
            Vector2 toTarget = Controller.TargetPosition - Controller.Position;

            if (toTarget.sqrMagnitude <= 0.0001f)
            {
                Controller.SetMoveSpeedMultiplier(1f);
                Controller.StopMovement();
                return;
            }

            float distance = toTarget.magnitude;
            Vector2 chaseDirection = toTarget / distance;
            _lastChaseDirection = chaseDirection;

            TickState(fixedDeltaTime, distance);

            float speedMultiplier = ResolveSpeedMultiplier();
            Vector2 moveDirection = ResolveMoveDirection(chaseDirection, distance);

            Controller.SetMoveSpeedMultiplier(speedMultiplier);
            Controller.SetDesiredMoveDirection(moveDirection.normalized);
        }

        private void TickState(float fixedDeltaTime, float distance)
        {
            _stateTimer = Mathf.Max(0f, _stateTimer - fixedDeltaTime);

            switch (_state)
            {
                case ChaseState.Slow:
                    if (_stateTimer <= 0f && distance <= fastTriggerRange)
                    {
                        _state = ChaseState.Fast;
                        _stateTimer = fastChaseDuration;
                    }
                    else if (_stateTimer <= 0f)
                    {
                        _stateTimer = 0.25f;
                    }

                    break;
                case ChaseState.Fast:
                    if (_stateTimer <= 0f)
                    {
                        _state = recoveryDuration > 0f ? ChaseState.Recovery : ChaseState.Slow;
                        _stateTimer = recoveryDuration > 0f ? recoveryDuration : slowChaseDuration;
                    }

                    break;
                case ChaseState.Recovery:
                    if (_stateTimer <= 0f)
                    {
                        _state = ChaseState.Slow;
                        _stateTimer = slowChaseDuration;
                    }

                    break;
            }
        }

        private float ResolveSpeedMultiplier()
        {
            switch (_state)
            {
                case ChaseState.Fast:
                    return fastSpeedMultiplier;
                case ChaseState.Recovery:
                    return recoverySpeedMultiplier;
                case ChaseState.Slow:
                default:
                    return slowSpeedMultiplier;
            }
        }

        private Vector2 ResolveMoveDirection(Vector2 chaseDirection, float distance)
        {
            Vector2 moveDirection = chaseDirection;

            if (distance <= orbitRange)
            {
                Vector2 perpendicular = new(-chaseDirection.y, chaseDirection.x * _orbitSign);
                moveDirection = (chaseDirection * (1f - orbitBlend)) + (perpendicular * orbitBlend);
            }

            return EnemyFormationTactics.ResolveEscortFrontlineMove(
                FormationModifier,
                Controller.Position,
                Controller.TargetPosition,
                moveDirection,
                1.18f,
                0.38f);
        }
    }
}
