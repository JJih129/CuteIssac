using CuteIssac.Common.Combat;
using CuteIssac.Data.Enemy;
using CuteIssac.Player;
using UnityEngine;

namespace CuteIssac.Enemy
{
    [DisallowMultipleComponent]
    public sealed class PullerEnemyBrain : EnemyBrain
    {
        [SerializeField] private PullerEnemyConfigurator configurator;

        private float _pullCooldown;
        private float _telegraphRemaining;
        private float _strafeSwapRemaining;
        private float _strafeSign = 1f;
        private float _runtimeFirstAttackDelayBonus;
        private float _runtimeTelegraphDurationMultiplier = 1f;

        protected override void HandleInitialized()
        {
            ResolveReferences();
            HandleResetState();
        }

        protected override void HandleResetState()
        {
            PullerEnemyData enemyData = configurator != null ? configurator.EnemyData : null;
            _pullCooldown = _runtimeFirstAttackDelayBonus;
            _telegraphRemaining = 0f;
            _strafeSign = (GetInstanceID() & 1) == 0 ? 1f : -1f;
            _strafeSwapRemaining = enemyData != null ? enemyData.StrafeSwapInterval : 1f;
            Controller?.EnemyVisual?.StopAttackTelegraph();
        }

        public void ApplyEncounterPacing(float firstAttackDelayBonus, float telegraphDurationMultiplier)
        {
            _runtimeFirstAttackDelayBonus = Mathf.Max(0f, firstAttackDelayBonus);
            _runtimeTelegraphDurationMultiplier = Mathf.Clamp(telegraphDurationMultiplier, 0.5f, 2f);
        }

        public override void TickBrain(float fixedDeltaTime)
        {
            PullerEnemyData enemyData = configurator != null ? configurator.EnemyData : null;

            if (enemyData == null)
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

            float distance = toTarget.magnitude;
            Vector2 aimDirection = toTarget / distance;

            if (_telegraphRemaining > 0f)
            {
                _telegraphRemaining -= fixedDeltaTime;
                Controller.SetMoveSpeedMultiplier(enemyData.MoveSpeedWhileTelegraphing);
                Controller.StopMovement();

                if (_telegraphRemaining <= 0f)
                {
                    ExecutePull(enemyData);
                }

                return;
            }

            _pullCooldown = Mathf.Max(0f, _pullCooldown - fixedDeltaTime);
            _strafeSwapRemaining -= fixedDeltaTime;

            if (_strafeSwapRemaining <= 0f)
            {
                _strafeSwapRemaining = enemyData.StrafeSwapInterval;
                _strafeSign *= -1f;
            }

            Vector2 moveDirection = ResolveMoveDirection(enemyData, aimDirection, distance);
            Controller.SetMoveSpeedMultiplier(distance >= enemyData.PullRange ? enemyData.SurgeSpeedMultiplier : 1f);
            Controller.SetDesiredMoveDirection(moveDirection);

            if (_pullCooldown > 0f || distance > enemyData.PullRange)
            {
                return;
            }

            _telegraphRemaining = enemyData.TelegraphDuration * _runtimeTelegraphDurationMultiplier;
            Controller.EnemyVisual?.StartAttackTelegraph(enemyData.TelegraphColor);
        }

        private Vector2 ResolveMoveDirection(PullerEnemyData enemyData, Vector2 aimDirection, float distance)
        {
            if (distance > enemyData.PreferredRange)
            {
                return aimDirection;
            }

            if (distance < enemyData.RetreatRange)
            {
                return -aimDirection;
            }

            Vector2 strafeDirection = new(-aimDirection.y, aimDirection.x * _strafeSign);
            return strafeDirection * Mathf.Clamp01(enemyData.OrbitBlend);
        }

        private void ExecutePull(PullerEnemyData enemyData)
        {
            Controller.EnemyVisual?.StopAttackTelegraph();
            Controller.EnemyVisual?.HandleAttack();
            _pullCooldown = enemyData.PullInterval;

            Transform target = Controller.CurrentTarget;

            if (target == null)
            {
                return;
            }

            PlayerHealth playerHealth = target.GetComponent<PlayerHealth>();

            if (playerHealth == null)
            {
                playerHealth = target.GetComponentInParent<PlayerHealth>();
            }

            if (playerHealth == null || playerHealth.IsDead)
            {
                return;
            }

            Vector2 pullDirection = ((Vector2)transform.position - (Vector2)playerHealth.transform.position).normalized;

            if (pullDirection.sqrMagnitude <= 0.0001f)
            {
                pullDirection = Vector2.down;
            }

            playerHealth.ApplyDamage(new DamageInfo(enemyData.PullDamage, pullDirection, transform));
        }

        private void ResolveReferences()
        {
            if (configurator == null)
            {
                configurator = GetComponent<PullerEnemyConfigurator>();
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
