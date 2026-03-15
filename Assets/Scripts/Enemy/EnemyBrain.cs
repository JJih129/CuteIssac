using UnityEngine;

namespace CuteIssac.Enemy
{
    /// <summary>
    /// Base class for enemy AI behaviours.
    /// EnemyController owns target resolution and state, while each brain decides how to move and attack.
    /// </summary>
    public abstract class EnemyBrain : MonoBehaviour
    {
        protected EnemyController Controller { get; private set; }

        public void Initialize(EnemyController controller)
        {
            Controller = controller;
            HandleInitialized();
        }

        public void ResetBrainState()
        {
            if (Controller == null)
            {
                return;
            }

            HandleResetState();
        }

        public abstract void TickBrain(float fixedDeltaTime);

        protected virtual void HandleInitialized()
        {
        }

        protected virtual void HandleResetState()
        {
        }
    }
}
