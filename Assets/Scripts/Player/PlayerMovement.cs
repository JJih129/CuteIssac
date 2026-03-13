using UnityEngine;

namespace CuteIssac.Player
{
    /// <summary>
    /// Applies 2D top-down movement to a Rigidbody2D.
    /// Keep this class focused on physics so controller and stat systems can evolve independently.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class PlayerMovement : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] [Min(0f)] private float baseMoveSpeed = 5f;

        [Header("Optional Runtime Providers")]
        [SerializeField] private MonoBehaviour moveSpeedProviderSource;

        private Rigidbody2D _rigidbody2D;
        private IPlayerMoveSpeedProvider _moveSpeedProvider;
        private Vector2 _moveInput;

        public float BaseMoveSpeed => baseMoveSpeed;
        public float CurrentMoveSpeed => ResolveMoveSpeed();

        private void Awake()
        {
            _rigidbody2D = GetComponent<Rigidbody2D>();
            _moveSpeedProvider = moveSpeedProviderSource as IPlayerMoveSpeedProvider;

            if (_moveSpeedProvider == null)
            {
                _moveSpeedProvider = GetComponent<PlayerStats>();
            }

            if (_rigidbody2D != null)
            {
                _rigidbody2D.gravityScale = 0f;
                _rigidbody2D.freezeRotation = true;
            }
        }

        private void FixedUpdate()
        {
            _rigidbody2D.linearVelocity = _moveInput * ResolveMoveSpeed();
        }

        private void OnDisable()
        {
            if (_rigidbody2D != null)
            {
                _rigidbody2D.linearVelocity = Vector2.zero;
            }
        }

        /// <summary>
        /// Stores the latest desired input. Diagonal input is clamped once here to avoid higher movement speed.
        /// </summary>
        public void SetMoveInput(Vector2 moveInput)
        {
            float sqrMagnitude = moveInput.sqrMagnitude;

            if (sqrMagnitude > 1f)
            {
                moveInput /= Mathf.Sqrt(sqrMagnitude);
            }

            _moveInput = moveInput;
        }

        public void Stop()
        {
            _moveInput = Vector2.zero;

            if (_rigidbody2D != null)
            {
                _rigidbody2D.linearVelocity = Vector2.zero;
            }
        }

        private float ResolveMoveSpeed()
        {
            if (_moveSpeedProvider != null)
            {
                return Mathf.Max(0f, _moveSpeedProvider.CurrentMoveSpeed);
            }

            return baseMoveSpeed;
        }
    }
}
