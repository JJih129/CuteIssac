using UnityEngine;

namespace CuteIssac.Enemy
{
    /// <summary>
    /// Stationary ranged threat that periodically telegraphs and fires a short spread volley.
    /// It creates lane denial pressure without collapsing onto the player.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TurretEnemyBrain : EnemyBrain
    {
        [Header("References")]
        [SerializeField] private EnemyCombat enemyCombat;

        [Header("Attack")]
        [SerializeField] [Min(0.2f)] private float fireInterval = 1.65f;
        [SerializeField] [Min(1)] private int shotsPerVolley = 3;
        [SerializeField] [Min(0.05f)] private float volleySpacing = 0.14f;
        [SerializeField] [Min(0f)] private float telegraphDuration = 0.32f;
        [SerializeField] [Min(0f)] private float engageRange = 8f;
        [SerializeField] [Range(0f, 45f)] private float spreadAngle = 16f;
        [SerializeField] private Color telegraphColor = new(1f, 0.54f, 0.18f, 1f);

        private float _shotCooldown;
        private float _telegraphRemaining;
        private int _shotsRemainingInVolley;
        private int _nextShotIndexInVolley;
        private Vector2 _preparedAimDirection = Vector2.right;
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
            _telegraphRemaining = 0f;
            _shotsRemainingInVolley = 0;
            _nextShotIndexInVolley = 0;
            _preparedAimDirection = Vector2.right;
            Controller?.EnemyVisual?.StopAttackTelegraph();
        }

        public void ApplyEncounterPacing(float firstAttackDelayBonus, float telegraphDurationMultiplier)
        {
            _runtimeFirstAttackDelayBonus = Mathf.Max(0f, firstAttackDelayBonus);
            _runtimeTelegraphDurationMultiplier = Mathf.Clamp(telegraphDurationMultiplier, 0.5f, 2f);
        }

        public override void TickBrain(float fixedDeltaTime)
        {
            if (!Controller.HasTarget)
            {
                Controller.StopMovement();
                return;
            }

            Vector2 toTarget = Controller.TargetPosition - Controller.Position;

            if (toTarget.sqrMagnitude <= 0.0001f)
            {
                Controller.StopMovement();
                return;
            }

            Vector2 aimDirection = toTarget.normalized;
            Controller.StopMovement();
            Controller.EnemyVisual?.SetMoveDirection(aimDirection);

            if (_telegraphRemaining > 0f)
            {
                _telegraphRemaining -= fixedDeltaTime;

                if (_telegraphRemaining <= 0f)
                {
                    FireVolleyShot(_preparedAimDirection);
                }

                return;
            }

            _shotCooldown = Mathf.Max(0f, _shotCooldown - fixedDeltaTime);

            if (_shotCooldown > 0f || enemyCombat == null || !enemyCombat.CanFire)
            {
                return;
            }

            if (toTarget.magnitude > engageRange)
            {
                return;
            }

            if (_shotsRemainingInVolley <= 0)
            {
                _shotsRemainingInVolley = Mathf.Max(1, shotsPerVolley);
                _nextShotIndexInVolley = 0;

                if (telegraphDuration > 0f)
                {
                    _preparedAimDirection = aimDirection;
                    _telegraphRemaining = telegraphDuration * _runtimeTelegraphDurationMultiplier;
                    Controller.EnemyVisual?.StartAttackTelegraph(telegraphColor);
                    return;
                }
            }

            FireVolleyShot(aimDirection);
        }

        private void FireVolleyShot(Vector2 aimDirection)
        {
            Controller.EnemyVisual?.StopAttackTelegraph();

            int volleyCount = Mathf.Max(1, shotsPerVolley);
            float normalizedIndex = volleyCount <= 1 ? 0.5f : (float)_nextShotIndexInVolley / (volleyCount - 1);
            float angleOffset = Mathf.Lerp(-spreadAngle, spreadAngle, normalizedIndex);
            Vector2 shotDirection = Quaternion.Euler(0f, 0f, angleOffset) * aimDirection;

            enemyCombat.Fire(shotDirection);
            Controller.EnemyVisual?.HandleAttack();

            _nextShotIndexInVolley++;
            _shotsRemainingInVolley--;
            _shotCooldown = _shotsRemainingInVolley > 0 ? volleySpacing : fireInterval;
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
