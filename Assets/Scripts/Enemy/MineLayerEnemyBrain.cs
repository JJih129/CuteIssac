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

            _telegraphRemaining = enemyData.TelegraphDuration * _runtimeTelegraphDurationMultiplier;
            Controller.EnemyVisual?.StartAttackTelegraph(enemyData.TelegraphColor);
        }

        private Vector2 ResolveMoveDirection(MineLayerEnemyData enemyData, Vector2 aimDirection, float distance)
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

        private void LayMine(MineLayerEnemyData enemyData, Vector2 aimDirection)
        {
            Controller.EnemyVisual?.StopAttackTelegraph();

            Vector2 right = new(aimDirection.y, -aimDirection.x);
            Vector2 dropOffset = (aimDirection * enemyData.MineDropOffset.y) + (right * enemyData.MineDropOffset.x);
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

        private void TryPrewarmMine(MineLayerEnemyData enemyData)
        {
            if (_hasPrewarmedMine || enemyData?.MinePrefab == null || enemyData.MineSpawnReusePolicy != SpawnReusePolicy.Pooled)
            {
                return;
            }

            if (enemyData.MinePrewarmCount > 0)
            {
                PrefabPoolService.Prewarm(enemyData.MinePrefab.gameObject, enemyData.MinePrewarmCount);
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
