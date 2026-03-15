using CuteIssac.Data.Enemy;
using UnityEngine;

namespace CuteIssac.Enemy
{
    /// <summary>
    /// Slow advance unit that keeps its shield facing the player and pressures with body contact.
    /// The shield itself is handled by a separate child damage receiver.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ShieldEnemyBrain : EnemyBrain
    {
        [SerializeField] private ShieldEnemyConfigurator configurator;

        protected override void HandleInitialized()
        {
            if (configurator == null)
            {
                configurator = GetComponent<ShieldEnemyConfigurator>();
            }
        }

        public override void TickBrain(float fixedDeltaTime)
        {
            ShieldEnemyData enemyData = configurator != null ? configurator.EnemyData : null;

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
            Vector2 facingDirection = toTarget / distance;
            Controller.EnemyVisual?.SetMoveDirection(facingDirection);

            if (distance <= enemyData.ContactRange)
            {
                Controller.SetMoveSpeedMultiplier(0.6f);
                Controller.SetDesiredMoveDirection(facingDirection * 0.25f);
                return;
            }

            Controller.SetMoveSpeedMultiplier(enemyData.AdvanceSpeedMultiplier);
            Controller.SetDesiredMoveDirection(facingDirection);
        }

        private void Reset()
        {
            if (configurator == null)
            {
                configurator = GetComponent<ShieldEnemyConfigurator>();
            }
        }

        private void OnValidate()
        {
            if (configurator == null)
            {
                configurator = GetComponent<ShieldEnemyConfigurator>();
            }
        }
    }
}
