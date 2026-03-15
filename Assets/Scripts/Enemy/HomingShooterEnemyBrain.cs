using CuteIssac.Data.Enemy;
using UnityEngine;

namespace CuteIssac.Enemy
{
    [DisallowMultipleComponent]
    public sealed class HomingShooterEnemyBrain : EnemyBrain
    {
        private enum PreparedAttackMode
        {
            SingleShot,
            PanicBurst
        }

        [SerializeField] private HomingShooterConfigurator configurator;
        [SerializeField] private EnemyCombat enemyCombat;

        private float _shotCooldown;
        private float _telegraphRemaining;
        private float _strafeSwapRemaining;
        private float _strafeSign = 1f;
        private Vector2 _preparedAimDirection = Vector2.right;
        private float _runtimeFirstAttackDelayBonus;
        private float _runtimeTelegraphDurationMultiplier = 1f;
        private PreparedAttackMode _preparedAttackMode;

        protected override void HandleInitialized()
        {
            ResolveReferences();
            HandleResetState();
        }

        protected override void HandleResetState()
        {
            HomingShooterEnemyData enemyData = configurator != null ? configurator.EnemyData : null;
            _shotCooldown = _runtimeFirstAttackDelayBonus;
            _telegraphRemaining = 0f;
            _strafeSign = (GetInstanceID() & 1) == 0 ? 1f : -1f;
            _strafeSwapRemaining = enemyData != null ? enemyData.StrafeSwapInterval : 1f;
            _preparedAimDirection = Vector2.right;
            _preparedAttackMode = PreparedAttackMode.SingleShot;
            Controller?.EnemyVisual?.StopAttackTelegraph();
        }

        public void ApplyEncounterPacing(float firstAttackDelayBonus, float telegraphDurationMultiplier)
        {
            _runtimeFirstAttackDelayBonus = Mathf.Max(0f, firstAttackDelayBonus);
            _runtimeTelegraphDurationMultiplier = Mathf.Clamp(telegraphDurationMultiplier, 0.5f, 2f);
        }

        public override void TickBrain(float fixedDeltaTime)
        {
            HomingShooterEnemyData enemyData = configurator != null ? configurator.EnemyData : null;

            if (enemyData == null || enemyCombat == null)
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
                Controller.SetMoveSpeedMultiplier(ResolveTelegraphMoveSpeed(enemyData));
                Controller.StopMovement();

                if (_telegraphRemaining <= 0f)
                {
                    FirePreparedAttack(enemyData, _preparedAimDirection);
                }

                return;
            }

            _shotCooldown = Mathf.Max(0f, _shotCooldown - fixedDeltaTime);
            _strafeSwapRemaining -= fixedDeltaTime;

            if (_strafeSwapRemaining <= 0f)
            {
                _strafeSwapRemaining = enemyData.StrafeSwapInterval;
                _strafeSign *= -1f;
            }

            Vector2 moveDirection = ResolveMoveDirection(enemyData, aimDirection, distance);
            Controller.SetMoveSpeedMultiplier(1f);
            Controller.SetDesiredMoveDirection(moveDirection);

            if (_shotCooldown > 0f || !enemyCombat.CanFire || distance > enemyData.PreferredRange + 1f)
            {
                return;
            }

            _preparedAimDirection = aimDirection;
            if (ShouldPreparePanicBurst(enemyData, distance))
            {
                BeginPreparedAttack(
                    enemyData,
                    PreparedAttackMode.PanicBurst,
                    enemyData.PanicBurstTelegraphDuration,
                    enemyData.PanicBurstTelegraphColor);
                return;
            }

            BeginPreparedAttack(
                enemyData,
                PreparedAttackMode.SingleShot,
                enemyData.TelegraphDuration,
                enemyData.TelegraphColor);
        }

        private Vector2 ResolveMoveDirection(HomingShooterEnemyData enemyData, Vector2 aimDirection, float distance)
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
            return strafeDirection * Mathf.Clamp01(enemyData.StrafeBlend);
        }

        private void FirePreparedAttack(HomingShooterEnemyData enemyData, Vector2 aimDirection)
        {
            if (_preparedAttackMode == PreparedAttackMode.PanicBurst)
            {
                FirePanicBurst(enemyData, aimDirection);
                return;
            }

            FireSingleShot(enemyData, aimDirection);
        }

        private void FireSingleShot(HomingShooterEnemyData enemyData, Vector2 aimDirection)
        {
            Controller.EnemyVisual?.StopAttackTelegraph();
            enemyCombat.Fire(aimDirection);
            Controller.EnemyVisual?.HandleAttack();
            _shotCooldown = enemyData.FireInterval;
        }

        private void FirePanicBurst(HomingShooterEnemyData enemyData, Vector2 aimDirection)
        {
            Controller.EnemyVisual?.StopAttackTelegraph();

            int projectileCount = Mathf.Max(1, enemyData.PanicBurstProjectileCount);
            float spread = Mathf.Max(0f, enemyData.PanicBurstSpreadAngle);
            float startAngle = -spread * 0.5f;
            float step = projectileCount > 1 ? spread / (projectileCount - 1) : 0f;

            for (int index = 0; index < projectileCount; index++)
            {
                float angleOffset = startAngle + (step * index);
                enemyCombat.Fire(Rotate(aimDirection, angleOffset));
            }

            Controller.EnemyVisual?.HandleAttack();
            _shotCooldown = Mathf.Max(enemyData.FireInterval, enemyData.PanicBurstCooldown);
        }

        private bool ShouldPreparePanicBurst(HomingShooterEnemyData enemyData, float distance)
        {
            return enemyData.PanicBurstProjectileCount > 1
                && enemyData.PanicBurstTriggerRange > 0f
                && distance <= enemyData.PanicBurstTriggerRange;
        }

        private float ResolveTelegraphMoveSpeed(HomingShooterEnemyData enemyData)
        {
            if (_preparedAttackMode == PreparedAttackMode.PanicBurst)
            {
                return enemyData.PanicBurstMoveSpeedWhileTelegraphing;
            }

            return enemyData.MoveSpeedWhileTelegraphing;
        }

        private void BeginPreparedAttack(
            HomingShooterEnemyData enemyData,
            PreparedAttackMode attackMode,
            float telegraphDuration,
            Color telegraphColor)
        {
            _preparedAttackMode = attackMode;
            _telegraphRemaining = telegraphDuration * _runtimeTelegraphDurationMultiplier;

            if (_telegraphRemaining <= 0f)
            {
                FirePreparedAttack(enemyData, _preparedAimDirection);
                return;
            }

            Controller.EnemyVisual?.StartAttackTelegraph(telegraphColor);
        }

        private static Vector2 Rotate(Vector2 direction, float degrees)
        {
            return (Quaternion.Euler(0f, 0f, degrees) * direction).normalized;
        }

        private void ResolveReferences()
        {
            if (configurator == null)
            {
                configurator = GetComponent<HomingShooterConfigurator>();
            }

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
