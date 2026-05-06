using CuteIssac.Player;
using UnityEngine;

namespace CuteIssac.Room.Gimmicks
{
    /// <summary>
    /// Applies a temporary movement lock to the player. Web hazards should use this instead of touching PlayerMovement directly.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MovementLockGimmick : CandyGimmickBase
    {
        [Header("Movement Lock")]
        [SerializeField] [Min(0.05f)] private float lockDuration = 1f;
        [SerializeField] private bool addMissingLockStateComponent = true;
        [SerializeField] private bool forceContactColliderAsTrigger = true;

        private int _sourceKey;

        public float LockDuration => lockDuration;

        protected override void Awake()
        {
            base.Awake();
            _sourceKey = GetInstanceID();
            ConfigureCollider();
        }

        protected override void OnPlayerContact(Collider2D playerCollider, PlayerHealth playerHealth)
        {
            if (!TryResolveLockState(playerHealth, out PlayerMovementLockState lockState))
            {
                return;
            }

            PlayerMovement playerMovement = playerHealth.GetComponent<PlayerMovement>();
            playerMovement?.RefreshMovementLockState(lockState);
            lockState.LockMovement(_sourceKey, lockDuration);
        }

        public void ConfigureLockDuration(float duration)
        {
            lockDuration = Mathf.Max(0.05f, duration);
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            lockDuration = Mathf.Max(0.05f, lockDuration);
            ConfigureCollider();
        }

        private bool TryResolveLockState(PlayerHealth playerHealth, out PlayerMovementLockState lockState)
        {
            lockState = null;

            if (playerHealth == null)
            {
                return false;
            }

            lockState = playerHealth.GetComponent<PlayerMovementLockState>();

            if (lockState != null)
            {
                return true;
            }

            if (!addMissingLockStateComponent)
            {
                return false;
            }

            lockState = playerHealth.gameObject.AddComponent<PlayerMovementLockState>();
            return lockState != null;
        }

        private void ConfigureCollider()
        {
            if (forceContactColliderAsTrigger && ContactCollider != null)
            {
                ContactCollider.isTrigger = true;
            }
        }
    }
}
