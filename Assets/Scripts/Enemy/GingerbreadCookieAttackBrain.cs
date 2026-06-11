using UnityEngine;

namespace CuteIssac.Enemy
{
    [DisallowMultipleComponent]
    public sealed class GingerbreadCookieAttackBrain : EnemyBrain
    {
        [Header("Attack")]
        [SerializeField] [Min(0.1f)] private float attackRange = 1.45f;
        [SerializeField] [Min(0.02f)] private float attackDuration = 0.48f;
        [SerializeField] [Min(0f)] private float damageActiveDelay = 0.08f;
        [SerializeField] [Min(0.02f)] private float damageActiveDuration = 0.2f;
        [SerializeField] [Min(0f)] private float attackCooldown = 0.72f;
        [SerializeField] [Min(0f)] private float attackDamage = 1f;
        [SerializeField] private GingerbreadCookieAttackHitbox attackHitbox;
        [SerializeField] private GingerbreadCookieSlashVfx slashVfx;

        [Header("Chase")]
        [SerializeField] [Min(0f)] private float stopDistance = 0.36f;
        [SerializeField] [Min(0f)] private float surgeRange = 4.8f;
        [SerializeField] [Min(1f)] private float surgeSpeedMultiplier = 1.12f;

        private float _attackRemaining;
        private float _attackElapsed;
        private float _cooldownRemaining;
        private bool _hitboxActivated;
        private Vector2 _lastAimDirection = Vector2.down;

        public bool IsAttacking => _attackRemaining > 0f;
        public Vector2 LastAimDirection => _lastAimDirection;

        protected override void HandleInitialized()
        {
            ResolveReferences();
            HandleResetState();
        }

        protected override void HandleResetState()
        {
            _attackRemaining = 0f;
            _attackElapsed = 0f;
            _cooldownRemaining = 0f;
            _hitboxActivated = false;
            attackHitbox?.Deactivate();
        }

        public override void TickBrain(float fixedDeltaTime)
        {
            ResolveReferences();

            if (_cooldownRemaining > 0f)
            {
                _cooldownRemaining = Mathf.Max(0f, _cooldownRemaining - fixedDeltaTime);
            }

            Vector2 toTarget = Controller.TargetPosition - Controller.Position;

            if (toTarget.sqrMagnitude > 0.0001f)
            {
                _lastAimDirection = toTarget.normalized;
            }

            if (IsAttacking)
            {
                TickAttack(fixedDeltaTime);
                return;
            }

            float distance = toTarget.magnitude;

            if (_cooldownRemaining <= 0f && distance <= attackRange)
            {
                BeginAttack();
                return;
            }

            ChaseTarget(toTarget, distance);
        }

        private void BeginAttack()
        {
            _attackRemaining = attackDuration;
            _attackElapsed = 0f;
            _hitboxActivated = false;
            Controller.SetMoveSpeedMultiplier(1f);
            Controller.StopMovement();
        }

        private void TickAttack(float fixedDeltaTime)
        {
            Controller.StopMovement();

            _attackElapsed += fixedDeltaTime;
            _attackRemaining = Mathf.Max(0f, _attackRemaining - fixedDeltaTime);

            if (!_hitboxActivated && _attackElapsed >= damageActiveDelay)
            {
                _hitboxActivated = true;
                attackHitbox?.Activate(damageActiveDuration, attackDamage, _lastAimDirection);
                slashVfx?.Play(_lastAimDirection);
            }

            if (_attackRemaining <= 0f)
            {
                attackHitbox?.Deactivate();
                _cooldownRemaining = attackCooldown;
            }
        }

        private void ChaseTarget(Vector2 toTarget, float distance)
        {
            if (toTarget.sqrMagnitude <= 0.0001f || distance <= stopDistance)
            {
                Controller.SetMoveSpeedMultiplier(1f);
                Controller.StopMovement();
                return;
            }

            Vector2 chaseDirection = toTarget / distance;
            Vector2 moveDirection = EnemyFormationTactics.ResolveEscortFrontlineMove(
                FormationModifier,
                Controller.Position,
                Controller.TargetPosition,
                chaseDirection,
                1.12f,
                0.28f);

            Controller.SetMoveSpeedMultiplier(distance >= surgeRange ? surgeSpeedMultiplier : 1f);
            Controller.SetDesiredMoveDirection(moveDirection.normalized);
        }

        private void ResolveReferences()
        {
            if (attackHitbox == null)
            {
                attackHitbox = GetComponentInChildren<GingerbreadCookieAttackHitbox>(true);
            }

            if (slashVfx == null)
            {
                slashVfx = GetComponentInChildren<GingerbreadCookieSlashVfx>(true);
            }
        }

        private void OnValidate()
        {
            attackRange = Mathf.Max(0.1f, attackRange);
            attackDuration = Mathf.Max(0.02f, attackDuration);
            damageActiveDelay = Mathf.Max(0f, damageActiveDelay);
            damageActiveDuration = Mathf.Max(0.02f, damageActiveDuration);
            attackCooldown = Mathf.Max(0f, attackCooldown);
            attackDamage = Mathf.Max(0f, attackDamage);
            stopDistance = Mathf.Max(0f, stopDistance);
            surgeRange = Mathf.Max(0f, surgeRange);
            surgeSpeedMultiplier = Mathf.Max(1f, surgeSpeedMultiplier);
        }
    }
}
