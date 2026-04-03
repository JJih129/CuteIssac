using CuteIssac.Data.Enemy;
using UnityEngine;

namespace CuteIssac.Enemy
{
    [DisallowMultipleComponent]
    public sealed class BurstShooterEnemyBrain : EnemyBrain
    {
        [SerializeField] private BurstShooterConfigurator configurator;
        [SerializeField] private EnemyCombat enemyCombat;

        private float _shotCooldown;
        private float _telegraphRemaining;
        private float _strafeSwapRemaining;
        private float _strafeSign = 1f;
        private Vector2 _preparedAimDirection = Vector2.right;
        private float _runtimeFirstAttackDelayBonus;
        private float _runtimeTelegraphDurationMultiplier = 1f;
        private float _crossfireCueWindowRemaining;
        private Vector2 _crossfireCueAnchor = Vector2.right;
        private int _lastCrossfireCueSerial;

        protected override void HandleInitialized()
        {
            ResolveReferences();
            HandleResetState();
        }

        protected override void HandleResetState()
        {
            BurstShooterEnemyData enemyData = configurator != null ? configurator.EnemyData : null;
            _shotCooldown = _runtimeFirstAttackDelayBonus;
            _telegraphRemaining = 0f;
            _strafeSign = (GetInstanceID() & 1) == 0 ? 1f : -1f;
            _strafeSwapRemaining = enemyData != null ? enemyData.StrafeSwapInterval : 1f;
            _preparedAimDirection = Vector2.right;
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
            EnemyFormationTactics.TryPrimeCrossfireCue(
                FormationModifier,
                ref _lastCrossfireCueSerial,
                ref _crossfireCueWindowRemaining,
                ref _crossfireCueAnchor,
                ref _shotCooldown,
                fixedDeltaTime,
                0.14f);

            BurstShooterEnemyData enemyData = configurator != null ? configurator.EnemyData : null;

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
                Controller.SetMoveSpeedMultiplier(enemyData.MoveSpeedWhileTelegraphing);
                Controller.StopMovement();

                if (_telegraphRemaining <= 0f)
                {
                    FireBurst(enemyData, _preparedAimDirection);
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

            if (_shotCooldown > 0f || !enemyCombat.CanFire || distance > enemyData.PreferredRange + 0.7f)
            {
                return;
            }

            _preparedAimDirection = EnemyFormationTactics.ResolveCueAimDirection(
                Controller.Position,
                aimDirection,
                _crossfireCueWindowRemaining,
                _crossfireCueAnchor);
            _telegraphRemaining = EnemyFormationTactics.ResolveCueTelegraphDuration(
                enemyData.TelegraphDuration,
                _crossfireCueWindowRemaining,
                0.58f) * _runtimeTelegraphDurationMultiplier;
            Controller.EnemyVisual?.StartAttackTelegraph(enemyData.TelegraphColor);
        }

        private Vector2 ResolveMoveDirection(BurstShooterEnemyData enemyData, Vector2 aimDirection, float distance)
        {
            Vector2 fallbackDirection;

            if (distance > enemyData.PreferredRange)
            {
                fallbackDirection = aimDirection;
                return EnemyFormationTactics.ResolveCrossfireRangedMove(
                    FormationModifier,
                    Controller.Position,
                    Controller.TargetPosition,
                    fallbackDirection,
                    enemyData.PreferredRange,
                    0.92f);
            }

            if (distance < enemyData.RetreatRange)
            {
                fallbackDirection = -aimDirection;
                return EnemyFormationTactics.ResolveCrossfireRangedMove(
                    FormationModifier,
                    Controller.Position,
                    Controller.TargetPosition,
                    fallbackDirection,
                    enemyData.PreferredRange,
                    0.92f);
            }

            Vector2 strafeDirection = new(-aimDirection.y, aimDirection.x * _strafeSign);
            fallbackDirection = strafeDirection * Mathf.Clamp01(enemyData.StrafeBlend);
            return EnemyFormationTactics.ResolveCrossfireRangedMove(
                FormationModifier,
                Controller.Position,
                Controller.TargetPosition,
                fallbackDirection,
                enemyData.PreferredRange,
                0.92f);
        }

        private void FireBurst(BurstShooterEnemyData enemyData, Vector2 aimDirection)
        {
            Controller.EnemyVisual?.StopAttackTelegraph();

            int projectileCount = enemyData.BurstProjectileCount;
            float spread = enemyData.BurstSpreadAngle;
            float startAngle = -spread * 0.5f;
            float step = projectileCount > 1 ? spread / (projectileCount - 1) : 0f;

            for (int index = 0; index < projectileCount; index++)
            {
                float angleOffset = startAngle + (step * index);
                enemyCombat.Fire(Rotate(aimDirection, angleOffset));
            }

            Controller.EnemyVisual?.HandleAttack();
            _shotCooldown = enemyData.FireInterval;
        }

        private static Vector2 Rotate(Vector2 direction, float degrees)
        {
            return (Quaternion.Euler(0f, 0f, degrees) * direction).normalized;
        }

        private void ResolveReferences()
        {
            if (configurator == null)
            {
                configurator = GetComponent<BurstShooterConfigurator>();
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
