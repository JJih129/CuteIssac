using UnityEngine;

namespace CuteIssac.Enemy
{
    [DisallowMultipleComponent]
    public sealed class CandyBatShooterBrain : EnemyBrain
    {
        [Header("References")]
        [SerializeField] private EnemyCombat enemyCombat;

        [Header("Attack")]
        [SerializeField] [Min(0.1f)] private float fireInterval = 1.7f;
        [SerializeField] [Min(0.1f)] private float attackWindupDuration = 2f;
        [SerializeField] private Color attackTelegraphColor = new(0.62f, 1f, 0.32f, 1f);

        [Header("Movement")]
        [SerializeField] [Min(0f)] private float preferredRange = 5.2f;
        [SerializeField] [Min(0f)] private float retreatRange = 2.1f;
        [SerializeField] [Range(0f, 1f)] private float strafeBlend = 0.68f;
        [SerializeField] [Min(0.05f)] private float strafeSwapInterval = 1.05f;

        private float _shotCooldown;
        private float _attackWindupRemaining;
        private float _strafeSwapRemaining;
        private float _strafeSign = 1f;
        private float _runtimeFirstAttackDelayBonus;
        private float _runtimeTelegraphDurationMultiplier = 1f;
        private bool _isChargingShot;
        private Vector2 _preparedAimDirection = Vector2.down;
        private Vector2 _lastAimDirection = Vector2.down;

        public bool IsChargingShot => _isChargingShot;
        public Vector2 LastAimDirection => _lastAimDirection;

        protected override void HandleInitialized()
        {
            ResolveReferences();
            HandleResetState();
        }

        protected override void HandleResetState()
        {
            _shotCooldown = 0.65f + _runtimeFirstAttackDelayBonus;
            _attackWindupRemaining = 0f;
            _strafeSign = (GetInstanceID() & 1) == 0 ? 1f : -1f;
            _strafeSwapRemaining = strafeSwapInterval;
            _isChargingShot = false;
            _preparedAimDirection = Vector2.down;
            _lastAimDirection = Vector2.down;
            Controller?.EnemyVisual?.StopAttackTelegraph();
        }

        public void ApplyEncounterPacing(float firstAttackDelayBonus, float telegraphDurationMultiplier)
        {
            _runtimeFirstAttackDelayBonus = Mathf.Max(0f, firstAttackDelayBonus);
            _runtimeTelegraphDurationMultiplier = Mathf.Clamp(telegraphDurationMultiplier, 0.5f, 2f);
            _shotCooldown = Mathf.Max(_shotCooldown, _runtimeFirstAttackDelayBonus);
        }

        public override void TickBrain(float fixedDeltaTime)
        {
            if (_isChargingShot)
            {
                TickAttackWindup(fixedDeltaTime);
                return;
            }

            _shotCooldown = Mathf.Max(0f, _shotCooldown - fixedDeltaTime);
            _strafeSwapRemaining -= fixedDeltaTime;

            if (_strafeSwapRemaining <= 0f)
            {
                _strafeSwapRemaining = strafeSwapInterval;
                _strafeSign *= -1f;
            }

            Vector2 toTarget = Controller.TargetPosition - Controller.Position;

            if (toTarget.sqrMagnitude <= 0.0001f)
            {
                Controller.SetMoveSpeedMultiplier(1f);
                Controller.StopMovement();
                return;
            }

            float distance = toTarget.magnitude;
            Vector2 aimDirection = toTarget / distance;
            _lastAimDirection = aimDirection;

            if (TryBeginAttack(aimDirection, distance))
            {
                return;
            }

            Controller.SetMoveSpeedMultiplier(1f);
            Controller.SetDesiredMoveDirection(ResolveMoveDirection(aimDirection, distance));
        }

        private void TickAttackWindup(float fixedDeltaTime)
        {
            _attackWindupRemaining -= fixedDeltaTime;
            Controller.SetMoveSpeedMultiplier(0f);
            Controller.StopMovement();

            Vector2 toTarget = Controller.TargetPosition - Controller.Position;
            if (toTarget.sqrMagnitude > 0.0001f)
            {
                _preparedAimDirection = toTarget.normalized;
                _lastAimDirection = _preparedAimDirection;
            }

            if (_attackWindupRemaining > 0f)
            {
                return;
            }

            Controller.EnemyVisual?.StopAttackTelegraph();

            if (enemyCombat != null && enemyCombat.CanFire)
            {
                enemyCombat.Fire(_preparedAimDirection);
                Controller.EnemyVisual?.HandleAttack();
            }

            _isChargingShot = false;
            _shotCooldown = fireInterval;
            Controller.SetMoveSpeedMultiplier(1f);
        }

        private bool TryBeginAttack(Vector2 aimDirection, float distance)
        {
            if (_shotCooldown > 0f || enemyCombat == null || !enemyCombat.CanFire)
            {
                return false;
            }

            if (distance > preferredRange + 0.75f)
            {
                return false;
            }

            _preparedAimDirection = aimDirection;
            _lastAimDirection = aimDirection;
            _attackWindupRemaining = attackWindupDuration * _runtimeTelegraphDurationMultiplier;
            _isChargingShot = true;
            Controller.SetMoveSpeedMultiplier(0f);
            Controller.StopMovement();
            Controller.EnemyVisual?.StartAttackTelegraph(attackTelegraphColor);
            return true;
        }

        private Vector2 ResolveMoveDirection(Vector2 aimDirection, float distance)
        {
            Vector2 fallbackDirection;

            if (distance > preferredRange)
            {
                fallbackDirection = aimDirection;
            }
            else if (distance < retreatRange)
            {
                fallbackDirection = -aimDirection;
            }
            else
            {
                Vector2 strafeDirection = new(-aimDirection.y, aimDirection.x * _strafeSign);
                fallbackDirection = strafeDirection * Mathf.Clamp01(strafeBlend);
            }

            return EnemyFormationTactics.ResolveCrossfireRangedMove(
                FormationModifier,
                Controller.Position,
                Controller.TargetPosition,
                fallbackDirection,
                preferredRange,
                0.88f);
        }

        private void ResolveReferences()
        {
            if (enemyCombat == null)
            {
                enemyCombat = GetComponent<EnemyCombat>();
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
