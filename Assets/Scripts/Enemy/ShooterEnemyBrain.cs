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

        protected override void HandleInitialized()
        {
            if (enemyCombat == null)
            {
                enemyCombat = GetComponent<EnemyCombat>();
            }

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
            Controller?.EnemyVisual?.StopAttackTelegraph();
        }

        public void ApplyEncounterPacing(float firstAttackDelayBonus, float telegraphDurationMultiplier)
        {
            _runtimeFirstAttackDelayBonus = Mathf.Max(0f, firstAttackDelayBonus);
            _runtimeTelegraphDurationMultiplier = Mathf.Clamp(telegraphDurationMultiplier, 0.5f, 2f);
        }

        public override void TickBrain(float fixedDeltaTime)
        {
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
            if (distance > preferredRange)
            {
                return aimDirection;
            }

            if (distance < retreatRange)
            {
                return -aimDirection;
            }

            Vector2 strafeDirection = new Vector2(-aimDirection.y, aimDirection.x * _strafeSign);
            return strafeDirection * Mathf.Clamp01(strafeBlend);
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

            if (_shotsRemainingInBurst <= 0)
            {
                _shotsRemainingInBurst = Mathf.Max(1, shotsPerBurst);

                if (burstTelegraphDuration > 0f)
                {
                    _preparedBurstAimDirection = aimDirection;
                    _burstTelegraphRemaining = burstTelegraphDuration * _runtimeTelegraphDurationMultiplier;
                    Controller.EnemyVisual?.StartAttackTelegraph(burstTelegraphColor);
                    return;
                }
            }

            FireBurstShot(aimDirection);
        }

        private void FireBurstShot(Vector2 aimDirection)
        {
            Controller.EnemyVisual?.StopAttackTelegraph();
            enemyCombat.Fire(aimDirection);
            Controller.EnemyVisual?.HandleAttack();
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
