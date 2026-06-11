using CuteIssac.Core.Pooling;
using CuteIssac.Core.Spawning;
using CuteIssac.Data.Enemy;
using UnityEngine;

namespace CuteIssac.Enemy
{
    [DisallowMultipleComponent]
    public sealed class MineLayerEnemyBrain : EnemyBrain
    {
        [SerializeField] private MineLayerEnemyConfigurator configurator;
        [SerializeField] private Collider2D ownerCollider;

        private float _layCooldown;
        private float _telegraphRemaining;
        private float _strafeSwapRemaining;
        private float _strafeSign = 1f;
        private float _runtimeFirstAttackDelayBonus;
        private float _runtimeTelegraphDurationMultiplier = 1f;
        private bool _hasPrewarmedMine;
        private float _siegeCueWindowRemaining;
        private Vector2 _siegeCueAnchor = Vector2.right;
        private int _lastSiegeCueSerial;
        private bool _siegeCueRaisedForCurrentTelegraph;

        protected override void HandleInitialized()
        {
            ResolveReferences();
            HandleResetState();
        }

        protected override void HandleResetState()
        {
            MineLayerEnemyData enemyData = configurator != null ? configurator.EnemyData : null;
            _layCooldown = _runtimeFirstAttackDelayBonus;
            _telegraphRemaining = 0f;
            _strafeSign = (GetInstanceID() & 1) == 0 ? 1f : -1f;
            _strafeSwapRemaining = enemyData != null ? enemyData.StrafeSwapInterval : 1f;
            _siegeCueWindowRemaining = 0f;
            _siegeCueAnchor = Vector2.right;
            _lastSiegeCueSerial = 0;
            _siegeCueRaisedForCurrentTelegraph = false;
            Controller?.EnemyVisual?.StopAttackTelegraph();
            TryPrewarmMine(enemyData);
        }

        public void ApplyEncounterPacing(float firstAttackDelayBonus, float telegraphDurationMultiplier)
        {
            _runtimeFirstAttackDelayBonus = Mathf.Max(0f, firstAttackDelayBonus);
            _runtimeTelegraphDurationMultiplier = Mathf.Clamp(telegraphDurationMultiplier, 0.5f, 2f);
        }

        public override void TickBrain(float fixedDeltaTime)
        {
            EnemyFormationTactics.TryPrimeSiegeCue(
                FormationModifier,
                ref _lastSiegeCueSerial,
                ref _siegeCueWindowRemaining,
                ref _siegeCueAnchor,
                ref _layCooldown,
                fixedDeltaTime,
                0.24f);

            MineLayerEnemyData enemyData = configurator != null ? configurator.EnemyData : null;

            if (enemyData == null)
            {
                Controller.StopMovement();
                return;
            }

            TryPrewarmMine(enemyData);

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
                if (!_siegeCueRaisedForCurrentTelegraph)
                {
                    EnemyFormationTactics.BroadcastSiegeCue(FormationModifier, Controller.TargetPosition, 0.86f, 1f);
                    _siegeCueRaisedForCurrentTelegraph = true;
                }

                _telegraphRemaining -= fixedDeltaTime;
                Controller.SetMoveSpeedMultiplier(enemyData.MoveSpeedWhileTelegraphing);
                Controller.StopMovement();

                if (_telegraphRemaining <= 0f)
                {
                    LayMine(enemyData, aimDirection);
                }

                return;
            }

            _layCooldown = Mathf.Max(0f, _layCooldown - fixedDeltaTime);
            _strafeSwapRemaining -= fixedDeltaTime;

            if (_strafeSwapRemaining <= 0f)
            {
                _strafeSwapRemaining = enemyData.StrafeSwapInterval;
                _strafeSign *= -1f;
            }

            Vector2 moveDirection = ResolveMoveDirection(enemyData, aimDirection, distance);
            Controller.SetMoveSpeedMultiplier(1f);
            Controller.SetDesiredMoveDirection(moveDirection);

            if (_layCooldown > 0f || enemyData.MinePrefab == null || distance > enemyData.PreferredRange + 0.75f)
            {
                return;
            }

            BeginLayTelegraph(enemyData);
        }

        private Vector2 ResolveMoveDirection(MineLayerEnemyData enemyData, Vector2 aimDirection, float distance)
        {
            Vector2 fallbackDirection;

            if (distance > enemyData.PreferredRange)
            {
                fallbackDirection = aimDirection;
                return EnemyFormationTactics.ResolveSiegeBacklineMove(
                    FormationModifier,
                    Controller.Position,
                    Controller.TargetPosition,
                    fallbackDirection,
                    0.68f);
            }

            if (distance < enemyData.RetreatRange)
            {
                fallbackDirection = -aimDirection;
                return EnemyFormationTactics.ResolveSiegeBacklineMove(
                    FormationModifier,
                    Controller.Position,
                    Controller.TargetPosition,
                    fallbackDirection,
                    0.68f);
            }

            Vector2 strafeDirection = new(-aimDirection.y, aimDirection.x * _strafeSign);
            fallbackDirection = strafeDirection * Mathf.Clamp01(enemyData.StrafeBlend);
            return EnemyFormationTactics.ResolveSiegeBacklineMove(
                FormationModifier,
                Controller.Position,
                Controller.TargetPosition,
                fallbackDirection,
                0.68f);
        }

        private void LayMine(MineLayerEnemyData enemyData, Vector2 aimDirection)
        {
            Controller.EnemyVisual?.StopAttackTelegraph();
            _siegeCueRaisedForCurrentTelegraph = false;

            Vector2 coordinatedAimDirection = EnemyFormationTactics.ResolveCueAimDirection(
                Controller.Position,
                aimDirection,
                _siegeCueWindowRemaining,
                _siegeCueAnchor);
            Vector2 right = new(coordinatedAimDirection.y, -coordinatedAimDirection.x);
            Vector2 dropOffset = (coordinatedAimDirection * enemyData.MineDropOffset.y) + (right * enemyData.MineDropOffset.x);
            Vector3 spawnPosition = Controller.Position + dropOffset;
            EnemyMineController spawnedMine = GameplaySpawnFactory.SpawnComponent(
                enemyData.MinePrefab,
                spawnPosition,
                Quaternion.identity,
                null,
                enemyData.MineSpawnReusePolicy);

            if (spawnedMine != null)
            {
                spawnedMine.Configure(
                    transform,
                    ownerCollider,
                    enemyData.MineArmDelay,
                    enemyData.MineTriggerRange,
                    enemyData.MineTriggerWindupSeconds,
                    enemyData.MineExplosionRadius,
                    enemyData.MineExplosionDamage,
                    enemyData.MineExplosionKnockback);
            }

            Controller.EnemyVisual?.HandleAttack();
            _layCooldown = enemyData.LayInterval;
        }

        private void BeginLayTelegraph(MineLayerEnemyData enemyData)
        {
            _telegraphRemaining = EnemyFormationTactics.ResolveCueTelegraphDuration(
                enemyData.TelegraphDuration,
                _siegeCueWindowRemaining,
                0.58f) * _runtimeTelegraphDurationMultiplier;
            _siegeCueRaisedForCurrentTelegraph = true;
            Controller.EnemyVisual?.StartAttackTelegraph(enemyData.TelegraphColor);
            EnemyFormationTactics.BroadcastSiegeCue(FormationModifier, Controller.TargetPosition, 0.86f, 1f);
        }

        private void TryPrewarmMine(MineLayerEnemyData enemyData)
        {
            if (_hasPrewarmedMine || enemyData?.MinePrefab == null || enemyData.MineSpawnReusePolicy != SpawnReusePolicy.Pooled)
            {
                return;
            }

            if (enemyData.MinePrewarmCount > 0)
            {
                PrefabPoolService.EnsurePrewarmed(enemyData.MinePrefab.gameObject, enemyData.MinePrewarmCount);
            }

            _hasPrewarmedMine = true;
        }

        private void ResolveReferences()
        {
            if (configurator == null)
            {
                configurator = GetComponent<MineLayerEnemyConfigurator>();
            }

            if (ownerCollider == null)
            {
                ownerCollider = GetComponent<Collider2D>();
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
