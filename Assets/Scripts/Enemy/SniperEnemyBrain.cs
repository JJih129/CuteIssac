using CuteIssac.Data.Enemy;
using UnityEngine;

namespace CuteIssac.Enemy
{
    [DisallowMultipleComponent]
    public sealed class SniperEnemyBrain : EnemyBrain
    {
        private enum AttackPhase
        {
            PrimaryShot,
            FollowUpShot
        }

        [SerializeField] private SniperEnemyConfigurator configurator;
        [SerializeField] private EnemyCombat enemyCombat;

        private float _shotCooldown;
        private float _telegraphRemaining;
        private float _followUpDelayRemaining;
        private float _strafeSwapRemaining;
        private float _strafeSign = 1f;
        private Vector2 _preparedAimDirection = Vector2.right;
        private float _runtimeFirstAttackDelayBonus;
        private float _runtimeTelegraphDurationMultiplier = 1f;
        private AttackPhase _preparedAttackPhase;
        private bool _followUpQueued;
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
            SniperEnemyData enemyData = configurator != null ? configurator.EnemyData : null;
            _shotCooldown = _runtimeFirstAttackDelayBonus;
            _telegraphRemaining = 0f;
            _followUpDelayRemaining = 0f;
            _strafeSign = (GetInstanceID() & 1) == 0 ? 1f : -1f;
            _strafeSwapRemaining = enemyData != null ? enemyData.StrafeSwapInterval : 1f;
            _preparedAimDirection = Vector2.right;
            _preparedAttackPhase = AttackPhase.PrimaryShot;
            _followUpQueued = false;
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
                0.18f);

            SniperEnemyData enemyData = configurator != null ? configurator.EnemyData : null;

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

            if (_followUpDelayRemaining > 0f)
            {
                _followUpDelayRemaining -= fixedDeltaTime;
                Controller.SetMoveSpeedMultiplier(0.16f);
                Controller.StopMovement();

                if (_followUpDelayRemaining <= 0f && _followUpQueued)
                {
                    BeginPreparedAttack(
                        enemyData,
                        AttackPhase.FollowUpShot,
                        EnemyFormationTactics.ResolveCueTelegraphDuration(
                            enemyData.FollowUpTelegraphDuration,
                            _crossfireCueWindowRemaining,
                            0.52f),
                        enemyData.FollowUpTelegraphColor,
                        EnemyFormationTactics.ResolveCueAimDirection(
                            Controller.Position,
                            aimDirection,
                            _crossfireCueWindowRemaining,
                            _crossfireCueAnchor));
                }

                return;
            }

            if (_followUpQueued)
            {
                BeginPreparedAttack(
                    enemyData,
                    AttackPhase.FollowUpShot,
                    EnemyFormationTactics.ResolveCueTelegraphDuration(
                        enemyData.FollowUpTelegraphDuration,
                        _crossfireCueWindowRemaining,
                        0.52f),
                    enemyData.FollowUpTelegraphColor,
                    EnemyFormationTactics.ResolveCueAimDirection(
                        Controller.Position,
                        aimDirection,
                        _crossfireCueWindowRemaining,
                        _crossfireCueAnchor));
                return;
            }

            if (_telegraphRemaining > 0f)
            {
                _telegraphRemaining -= fixedDeltaTime;
                Controller.SetMoveSpeedMultiplier(ResolveTelegraphMoveSpeed(enemyData));
                Controller.StopMovement();

                if (_telegraphRemaining <= 0f)
                {
                    FirePreparedShot(enemyData);
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

            if (_shotCooldown > 0f || !enemyCombat.CanFire || distance < enemyData.RetreatRange - 0.2f)
            {
                return;
            }

            BeginPreparedAttack(
                enemyData,
                AttackPhase.PrimaryShot,
                EnemyFormationTactics.ResolveCueTelegraphDuration(
                    enemyData.TelegraphDuration,
                    _crossfireCueWindowRemaining,
                    0.58f),
                enemyData.TelegraphColor,
                EnemyFormationTactics.ResolveCueAimDirection(
                    Controller.Position,
                    aimDirection,
                    _crossfireCueWindowRemaining,
                    _crossfireCueAnchor));
        }

        private Vector2 ResolveMoveDirection(SniperEnemyData enemyData, Vector2 aimDirection, float distance)
        {
            if (distance < enemyData.RetreatRange)
            {
                return EnemyFormationTactics.ResolveCrossfireRangedMove(
                    FormationModifier,
                    Controller.Position,
                    Controller.TargetPosition,
                    -aimDirection,
                    enemyData.PreferredRange,
                    1.02f);
            }

            if (distance > enemyData.PreferredRange)
            {
                return EnemyFormationTactics.ResolveCrossfireRangedMove(
                    FormationModifier,
                    Controller.Position,
                    Controller.TargetPosition,
                    aimDirection * 0.45f,
                    enemyData.PreferredRange,
                    1.02f);
            }

            Vector2 strafeDirection = new(-aimDirection.y, aimDirection.x * _strafeSign);
            Vector2 fallbackDirection = strafeDirection * Mathf.Clamp01(enemyData.StrafeBlend);
            return EnemyFormationTactics.ResolveCrossfireRangedMove(
                FormationModifier,
                Controller.Position,
                Controller.TargetPosition,
                fallbackDirection,
                enemyData.PreferredRange,
                1.02f);
        }

        private void FirePreparedShot(SniperEnemyData enemyData)
        {
            Controller.EnemyVisual?.StopAttackTelegraph();

            Vector2 aimDirection = ResolveCurrentAimDirection();
            enemyCombat.Fire(aimDirection);
            Controller.EnemyVisual?.HandleAttack();

            if (_preparedAttackPhase == AttackPhase.PrimaryShot && ShouldQueueFollowUp(enemyData))
            {
                _followUpQueued = true;
                _followUpDelayRemaining = enemyData.FollowUpDelay;
                _shotCooldown = 0f;
                return;
            }

            _followUpQueued = false;
            _followUpDelayRemaining = 0f;
            _shotCooldown = ResolveRecovery(enemyData);
        }

        private void BeginPreparedAttack(
            SniperEnemyData enemyData,
            AttackPhase attackPhase,
            float telegraphDuration,
            Color telegraphColor,
            Vector2 aimDirection)
        {
            if (attackPhase == AttackPhase.FollowUpShot)
            {
                _followUpQueued = false;
            }

            _preparedAttackPhase = attackPhase;
            _preparedAimDirection = aimDirection;
            _telegraphRemaining = telegraphDuration * _runtimeTelegraphDurationMultiplier;

            if (_telegraphRemaining <= 0f)
            {
                FirePreparedShot(enemyData);
                return;
            }

            Controller.EnemyVisual?.StartAttackTelegraph(telegraphColor);
        }

        private bool ShouldQueueFollowUp(SniperEnemyData enemyData)
        {
            return enemyData.FollowUpTriggerRange > 0f
                && enemyData.FollowUpTelegraphDuration >= 0f
                && Vector2.Distance(Controller.Position, Controller.TargetPosition) >= enemyData.FollowUpTriggerRange;
        }

        private float ResolveTelegraphMoveSpeed(SniperEnemyData enemyData)
        {
            if (_preparedAttackPhase == AttackPhase.FollowUpShot)
            {
                return enemyData.MoveSpeedWhileFollowUpTelegraphing;
            }

            return enemyData.MoveSpeedWhileTelegraphing;
        }

        private float ResolveRecovery(SniperEnemyData enemyData)
        {
            if (_preparedAttackPhase == AttackPhase.FollowUpShot)
            {
                return Mathf.Max(enemyData.FireInterval, enemyData.FollowUpRecovery);
            }

            return enemyData.FireInterval;
        }

        private Vector2 ResolveCurrentAimDirection()
        {
            Vector2 toTarget = Controller.TargetPosition - Controller.Position;

            if (toTarget.sqrMagnitude > 0.0001f && _preparedAimDirection.sqrMagnitude > 0.0001f)
            {
                Vector2 blendedDirection = Vector2.Lerp(
                    toTarget.normalized,
                    _preparedAimDirection.normalized,
                    _crossfireCueWindowRemaining > 0.001f ? 0.72f : 0.35f);
                return blendedDirection.sqrMagnitude > 0.0001f
                    ? blendedDirection.normalized
                    : toTarget.normalized;
            }

            if (toTarget.sqrMagnitude > 0.0001f)
            {
                return toTarget.normalized;
            }

            return _preparedAimDirection;
        }

        private void ResolveReferences()
        {
            if (configurator == null)
            {
                configurator = GetComponent<SniperEnemyConfigurator>();
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
