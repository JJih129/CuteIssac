using UnityEngine;

namespace CuteIssac.Enemy
{
    /// <summary>
    /// Direct pursuit brain used by the baseline Isaac-style enemy.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ChaserEnemyBrain : EnemyBrain
    {
        [Header("Chase")]
        [SerializeField] [Min(0f)] private float orbitRange = 1.9f;
        [SerializeField] [Range(0f, 1f)] private float orbitBlend = 0.42f;
        [SerializeField] [Min(0f)] private float surgeRange = 4.8f;
        [SerializeField] [Min(1f)] private float surgeSpeedMultiplier = 1.2f;

        private float _orbitSign = 1f;

        protected override void HandleInitialized()
        {
            HandleResetState();
        }

        protected override void HandleResetState()
        {
            _orbitSign = (GetInstanceID() & 1) == 0 ? 1f : -1f;
        }

        public override void TickBrain(float fixedDeltaTime)
        {
            Vector2 toTarget = Controller.TargetPosition - Controller.Position;

            if (toTarget.sqrMagnitude <= 0.0001f)
            {
                Controller.SetMoveSpeedMultiplier(1f);
                Controller.StopMovement();
                return;
            }

            float distance = toTarget.magnitude;
            Vector2 chaseDirection = toTarget / distance;
            Vector2 moveDirection = chaseDirection;

            if (distance <= orbitRange)
            {
                Vector2 perpendicular = new Vector2(-chaseDirection.y, chaseDirection.x * _orbitSign);
                moveDirection = (chaseDirection * (1f - orbitBlend)) + (perpendicular * orbitBlend);
            }

            Controller.SetMoveSpeedMultiplier(distance >= surgeRange ? surgeSpeedMultiplier : 1f);
            Controller.SetDesiredMoveDirection(moveDirection.normalized);
        }
    }
}
