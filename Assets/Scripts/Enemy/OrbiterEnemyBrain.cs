using CuteIssac.Data.Enemy;
using UnityEngine;

namespace CuteIssac.Enemy
{
    [DisallowMultipleComponent]
    public sealed class OrbiterEnemyBrain : EnemyBrain
    {
        [SerializeField] private OrbiterEnemyConfigurator configurator;

        private float _orbitSign = 1f;
        private float _orbitSwapRemaining;

        protected override void HandleInitialized()
        {
            ResolveReferences();
            HandleResetState();
        }

        protected override void HandleResetState()
        {
            OrbiterEnemyData enemyData = configurator != null ? configurator.EnemyData : null;
            _orbitSign = (GetInstanceID() & 1) == 0 ? 1f : -1f;
            _orbitSwapRemaining = enemyData != null ? enemyData.OrbitSwapInterval : 1f;
        }

        public override void TickBrain(float fixedDeltaTime)
        {
            OrbiterEnemyData enemyData = configurator != null ? configurator.EnemyData : null;

            if (enemyData == null || !Controller.HasTarget)
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
            Vector2 chaseDirection = toTarget / distance;
            _orbitSwapRemaining -= fixedDeltaTime;

            if (_orbitSwapRemaining <= 0f)
            {
                _orbitSwapRemaining = enemyData.OrbitSwapInterval;
                _orbitSign *= -1f;
            }

            Vector2 moveDirection = ResolveMoveDirection(enemyData, chaseDirection, distance);
            float speedMultiplier = distance > enemyData.EngageRange ? enemyData.SurgeSpeedMultiplier : 1f;
            Controller.SetMoveSpeedMultiplier(speedMultiplier);
            Controller.SetDesiredMoveDirection(moveDirection);
        }

        private Vector2 ResolveMoveDirection(OrbiterEnemyData enemyData, Vector2 chaseDirection, float distance)
        {
            if (distance > enemyData.EngageRange)
            {
                return chaseDirection;
            }

            if (distance < enemyData.RetreatRange)
            {
                return -chaseDirection;
            }

            Vector2 orbitDirection = new(-chaseDirection.y, chaseDirection.x * _orbitSign);

            if (distance > enemyData.PreferredOrbitRange)
            {
                return ((chaseDirection * (1f - enemyData.OrbitBlend)) + (orbitDirection * enemyData.OrbitBlend)).normalized;
            }

            return ((-chaseDirection * (1f - enemyData.OrbitBlend)) + (orbitDirection * enemyData.OrbitBlend)).normalized;
        }

        private void ResolveReferences()
        {
            if (configurator == null)
            {
                configurator = GetComponent<OrbiterEnemyConfigurator>();
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
