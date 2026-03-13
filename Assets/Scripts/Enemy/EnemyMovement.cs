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
            _rigidbody2D.linearVelocity = _moveDirection * moveSpeed;
        }

        private void OnDisable()
        {
            if (_rigidbody2D != null)
            {
                _rigidbody2D.linearVelocity = Vector2.zero;
            }
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

            if (_rigidbody2D != null)
            {
                _rigidbody2D.linearVelocity = Vector2.zero;
            }
        }
    }
}
