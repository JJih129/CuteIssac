using UnityEngine;

namespace CuteIssac.Enemy
{
    /// <summary>
    /// Handles Rigidbody2D movement for a simple chasing enemy.
    /// Keep physics work here so higher-level AI can stay small and replaceable.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class EnemyMovement : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] [Min(0f)] private float moveSpeed = 2.5f;

        private Rigidbody2D _rigidbody2D;
        private Vector2 _moveDirection;
        private float _speedMultiplier = 1f;
        private float _externalSpeedMultiplier = 1f;
        private Vector2 _externalVelocity;

        private const float KnockbackDamping = 12f;

        public float MoveSpeed => moveSpeed;
        public Vector2 CurrentMoveDirection => _moveDirection;

        private void Awake()
        {
            _rigidbody2D = GetComponent<Rigidbody2D>();
            _rigidbody2D.gravityScale = 0f;
            _rigidbody2D.freezeRotation = true;
        }

        private void FixedUpdate()
        {
            _rigidbody2D.linearVelocity = (_moveDirection * (moveSpeed * _speedMultiplier * _externalSpeedMultiplier)) + _externalVelocity;
            _externalVelocity = Vector2.Lerp(_externalVelocity, Vector2.zero, KnockbackDamping * Time.fixedDeltaTime);
        }

        private void OnDisable()
        {
            if (_rigidbody2D != null)
            {
                _rigidbody2D.linearVelocity = Vector2.zero;
            }

            _externalVelocity = Vector2.zero;
            _externalSpeedMultiplier = 1f;
        }

        /// <summary>
        /// Expects a normalized direction. Clamp as a safety net so bad callers cannot increase speed.
        /// </summary>
        public void SetMoveDirection(Vector2 direction)
        {
            float sqrMagnitude = direction.sqrMagnitude;

            if (sqrMagnitude > 1f)
            {
                direction /= Mathf.Sqrt(sqrMagnitude);
            }

            _moveDirection = direction;
        }

        public void Stop()
        {
            _moveDirection = Vector2.zero;
            _speedMultiplier = 1f;

            if (_rigidbody2D != null)
            {
                _rigidbody2D.linearVelocity = Vector2.zero;
            }
        }

        public void SetSpeedMultiplier(float multiplier)
        {
            _speedMultiplier = Mathf.Max(0f, multiplier);
        }

        public void SetExternalSpeedMultiplier(float multiplier)
        {
            _externalSpeedMultiplier = Mathf.Max(0f, multiplier);
        }

        public void SetBaseMoveSpeed(float speed)
        {
            moveSpeed = Mathf.Max(0f, speed);
        }

        public void ApplyImpulse(Vector2 impulse)
        {
            _externalVelocity += impulse;
        }
    }
}
