using UnityEngine;

namespace CuteIssac.Enemy
{
    /// <summary>
    /// Periodically bursts toward the player to create a distinct timing-based melee threat.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DasherEnemyBrain : EnemyBrain
    {
        [Header("Dash")]
        [SerializeField] [Min(0.1f)] private float dashCooldown = 1.6f;
        [SerializeField] [Min(0f)] private float initialDashDelay = 0.4f;
        [SerializeField] [Min(0.05f)] private float dashWindupDuration = 0.2f;
        [SerializeField] [Min(0.05f)] private float dashDuration = 0.32f;
        [SerializeField] [Min(0f)] private float dashRecoveryDuration = 0.22f;
        [SerializeField] [Min(1f)] private float dashSpeedMultiplier = 2.8f;
        [SerializeField] [Min(0f)] private float dashTriggerRange = 4.25f;
        [SerializeField] private Color dashTelegraphColor = new(1f, 0.3f, 0.3f, 1f);

        [Header("Approach")]
        [SerializeField] [Range(0f, 1f)] private float approachBlend = 0.55f;
        [SerializeField] [Range(0f, 1f)] private float orbitBlend = 0.35f;

        private float _dashCooldownRemaining;
        private float _dashWindupRemaining;
        private float _dashRemaining;
        private float _dashRecoveryRemaining;
        private float _initialDashDelayRemaining;
        private Vector2 _dashDirection = Vector2.right;
        private float _orbitSign = 1f;
        private float _runtimeFirstAttackDelayBonus;
        private float _runtimeTelegraphDurationMultiplier = 1f;

        protected override void HandleInitialized()
        {
            HandleResetState();
        }

        protected override void HandleResetState()
        {
            _dashCooldownRemaining = 0f;
            _dashWindupRemaining = 0f;
            _dashRemaining = 0f;
            _dashRecoveryRemaining = 0f;
            _initialDashDelayRemaining = initialDashDelay + _runtimeFirstAttackDelayBonus;
            _dashDirection = Vector2.right;
            _orbitSign = (GetInstanceID() & 1) == 0 ? 1f : -1f;
            Controller?.EnemyVisual?.StopAttackTelegraph();
        }

        public void ApplyEncounterPacing(float firstAttackDelayBonus, float telegraphDurationMultiplier)
        {
            _runtimeFirstAttackDelayBonus = Mathf.Max(0f, firstAttackDelayBonus);
            _runtimeTelegraphDurationMultiplier = Mathf.Clamp(telegraphDurationMultiplier, 0.5f, 2f);
        }

        public override void TickBrain(float fixedDeltaTime)
        {
            Vector2 toTarget = Controller.TargetPosition - Controller.Position;

            if (toTarget.sqrMagnitude <= 0.0001f)
            {
                _dashWindupRemaining = 0f;
                Controller.EnemyVisual?.StopAttackTelegraph();
                ExitDash();
                Controller.StopMovement();
                return;
            }

            float distance = toTarget.magnitude;
            Vector2 normalizedDirection = toTarget / distance;
            _initialDashDelayRemaining = Mathf.Max(0f, _initialDashDelayRemaining - fixedDeltaTime);

            if (_dashWindupRemaining > 0f)
            {
                _dashWindupRemaining -= fixedDeltaTime;
                Controller.EnemyVisual?.StartAttackTelegraph(dashTelegraphColor);
                Controller.SetMoveSpeedMultiplier(0.4f);
                Controller.StopMovement();

                if (_dashWindupRemaining <= 0f)
                {
                    Controller.EnemyVisual?.StopAttackTelegraph();
                    _dashRemaining = dashDuration;
                    Controller.EnemyVisual?.HandleAttack();
                }

                return;
            }

            if (_dashRemaining > 0f)
            {
                _dashRemaining -= fixedDeltaTime;
                Controller.SetMoveSpeedMultiplier(dashSpeedMultiplier);
                Controller.SetDesiredMoveDirection(_dashDirection);

                if (_dashRemaining <= 0f)
                {
                    ExitDash();
                }

                return;
            }

            if (_dashRecoveryRemaining > 0f)
            {
                _dashRecoveryRemaining -= fixedDeltaTime;
                Controller.SetMoveSpeedMultiplier(0.75f);
                Controller.SetDesiredMoveDirection(Vector2.zero);
                return;
            }

            _dashCooldownRemaining = Mathf.Max(0f, _dashCooldownRemaining - fixedDeltaTime);

            if (_dashCooldownRemaining <= 0f && _initialDashDelayRemaining <= 0f && distance <= dashTriggerRange)
            {
                _dashDirection = normalizedDirection;
                _dashWindupRemaining = dashWindupDuration * _runtimeTelegraphDurationMultiplier;
                _dashCooldownRemaining = dashCooldown;
                return;
            }

            Controller.SetMoveSpeedMultiplier(1f);
            Vector2 perpendicular = new Vector2(-normalizedDirection.y, normalizedDirection.x * _orbitSign);
            Vector2 moveDirection = (normalizedDirection * approachBlend) + (perpendicular * orbitBlend);
            Controller.SetDesiredMoveDirection(moveDirection.normalized * approachBlend);
        }

        private void ExitDash()
        {
            _dashRemaining = 0f;
            _dashRecoveryRemaining = dashRecoveryDuration;
            Controller.SetMoveSpeedMultiplier(1f);
        }
    }
}
