using UnityEngine;

namespace CuteIssac.Enemy
{
    /// <summary>
    /// Maintains range and fires toward the player.
    /// The projectile path is still shared with the rest of the combat system.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ShooterEnemyBrain : EnemyBrain
    {
        [Header("References")]
        [SerializeField] private EnemyCombat enemyCombat;

        [Header("Attack")]
        [SerializeField] [Min(0.1f)] private float fireInterval = 1.1f;
        [SerializeField] [Min(1)] private int shotsPerBurst = 2;
        [SerializeField] [Min(0.05f)] private float burstSpacing = 0.18f;
        [SerializeField] [Min(0f)] private float postBurstRecovery = 0.55f;
        [SerializeField] [Min(0f)] private float burstTelegraphDuration = 0.22f;
        [SerializeField] [Min(0f)] private float burstTelegraphMoveSpeedMultiplier = 0.25f;
        [SerializeField] private Color burstTelegraphColor = new(1f, 0.78f, 0.22f, 1f);

        [Header("Movement")]
        [SerializeField] [Min(0f)] private float preferredRange = 5f;
        [SerializeField] [Min(0f)] private float retreatRange = 2.25f;
        [SerializeField] [Range(0f, 1f)] private float strafeBlend = 0.45f;
        [SerializeField] [Min(0.05f)] private float strafeSwapInterval = 1.1f;

        private float _shotCooldown;
        private float _strafeSwapRemaining;
        private float _strafeSign = 1f;
        private int _shotsRemainingInBurst;
        private float _burstTelegraphRemaining;
        private Vector2 _preparedBurstAimDirection = Vector2.right;
        private float _runtimeFirstAttackDelayBonus;
        private float _runtimeTelegraphDurationMultiplier = 1f;
        private float _crossfireCueWindowRemaining;
        private Vector2 _crossfireCueAnchor = Vector2.right;
        private int _lastCrossfireCueSerial;
        private IEnemyRangedAttackPresentation _attackPresentation;

        protected override void HandleInitialized()
        {
            if (enemyCombat == null)
            {
                enemyCombat = GetComponent<EnemyCombat>();
            }

            _attackPresentation = GetComponent<IEnemyRangedAttackPresentation>();
            HandleResetState();
        }

        protected override void HandleResetState()
        {
            _shotCooldown = _runtimeFirstAttackDelayBonus;
            _shotsRemainingInBurst = 0;
            _strafeSign = (GetInstanceID() & 1) == 0 ? 1f : -1f;
            _strafeSwapRemaining = strafeSwapInterval;
            _burstTelegraphRemaining = 0f;
            _preparedBurstAimDirection = Vector2.right;
            _crossfireCueWindowRemaining = 0f;
            _crossfireCueAnchor = Vector2.right;
            _lastCrossfireCueSerial = 0;
            Controller?.EnemyVisual?.StopAttackTelegraph();
        }

        public void ApplyEncounterPacing(float firstAttackDelayBonus, float telegraphDurationMultiplier)
        {
            _runtimeFirstAttackDelayBonus = Mathf.Max(0f, firstAttackDelayBonus);
            _runtimeTelegraphDurationMultiplier = Mathf.Clamp(telegraphDurationMultiplier, 0.5f, 2f);
        }

        public override void TickBrain(float fixedDeltaTime)
        {
            if (_attackPresentation != null && _attackPresentation.IsAttackPresentationActive)
            {
                Controller.SetMoveSpeedMultiplier(0f);
                Controller.StopMovement();
                return;
            }

            EnemyFormationTactics.TryPrimeCrossfireCue(
                FormationModifier,
                ref _lastCrossfireCueSerial,
                ref _crossfireCueWindowRemaining,
                ref _crossfireCueAnchor,
                ref _shotCooldown,
                fixedDeltaTime,
                0.12f);

            if (_burstTelegraphRemaining > 0f)
            {
                _burstTelegraphRemaining -= fixedDeltaTime;
                Controller.SetMoveSpeedMultiplier(Mathf.Clamp01(burstTelegraphMoveSpeedMultiplier));
                Controller.StopMovement();

                if (_burstTelegraphRemaining <= 0f)
                {
                    FireBurstShot(_preparedBurstAimDirection);
                }

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
                Controller.StopMovement();
                return;
            }

            float distance = toTarget.magnitude;
            Vector2 aimDirection = toTarget / distance;
            Vector2 moveDirection = ResolveMoveDirection(aimDirection, distance);

            Controller.SetDesiredMoveDirection(moveDirection);
            TryFire(aimDirection, distance);
        }

        private Vector2 ResolveMoveDirection(Vector2 aimDirection, float distance)
        {
            Vector2 fallbackDirection;

            if (distance > preferredRange)
            {
                fallbackDirection = aimDirection;
                return EnemyFormationTactics.ResolveCrossfireRangedMove(
                    FormationModifier,
                    Controller.Position,
                    Controller.TargetPosition,
                    fallbackDirection,
                    preferredRange,
                    0.88f);
            }

            if (distance < retreatRange)
            {
                fallbackDirection = -aimDirection;
                return EnemyFormationTactics.ResolveCrossfireRangedMove(
                    FormationModifier,
                    Controller.Position,
                    Controller.TargetPosition,
                    fallbackDirection,
                    preferredRange,
                    0.88f);
            }

            Vector2 strafeDirection = new Vector2(-aimDirection.y, aimDirection.x * _strafeSign);
            fallbackDirection = strafeDirection * Mathf.Clamp01(strafeBlend);
            return EnemyFormationTactics.ResolveCrossfireRangedMove(
                FormationModifier,
                Controller.Position,
                Controller.TargetPosition,
                fallbackDirection,
                preferredRange,
                0.88f);
        }

        private void TryFire(Vector2 aimDirection, float distance)
        {
            if (_shotCooldown > 0f || enemyCombat == null || !enemyCombat.CanFire)
            {
                return;
            }

            if (distance > preferredRange + 0.9f)
            {
                return;
            }

            Vector2 preparedAimDirection = EnemyFormationTactics.ResolveCueAimDirection(
                Controller.Position,
                aimDirection,
                _crossfireCueWindowRemaining,
                _crossfireCueAnchor);

            if (_shotsRemainingInBurst <= 0)
            {
                _shotsRemainingInBurst = Mathf.Max(1, shotsPerBurst);

                if (burstTelegraphDuration > 0f)
                {
                    _preparedBurstAimDirection = preparedAimDirection;
                    _burstTelegraphRemaining = EnemyFormationTactics.ResolveCueTelegraphDuration(
                        burstTelegraphDuration,
                        _crossfireCueWindowRemaining,
                        0.56f) * _runtimeTelegraphDurationMultiplier;
                    Controller.EnemyVisual?.StartAttackTelegraph(burstTelegraphColor);
                    return;
                }
            }

            FireBurstShot(preparedAimDirection);
        }

        private void FireBurstShot(Vector2 aimDirection)
        {
            Controller.EnemyVisual?.StopAttackTelegraph();

            bool presentationHandledFire = _attackPresentation != null
                && _attackPresentation.TryPlayAttack(aimDirection, enemyCombat);

            if (!presentationHandledFire)
            {
                enemyCombat.Fire(aimDirection);
                Controller.EnemyVisual?.HandleAttack();
            }

            _shotsRemainingInBurst--;
            _shotCooldown = _shotsRemainingInBurst > 0 ? burstSpacing : fireInterval + postBurstRecovery;
        }

        private void Reset()
        {
            enemyCombat = GetComponent<EnemyCombat>();
        }

        private void OnValidate()
        {
            if (enemyCombat == null)
            {
                enemyCombat = GetComponent<EnemyCombat>();
            }
        }
    }
}
