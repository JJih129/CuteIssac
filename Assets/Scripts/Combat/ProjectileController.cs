using CuteIssac.Common.Combat;
using UnityEngine;

namespace CuteIssac.Combat
{
    /// <summary>
    /// Owns straight-line projectile motion, expiry, and damage dispatch.
    /// The projectile only talks to IDamageable, not to specific enemy classes.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public sealed class ProjectileController : MonoBehaviour
    {
        private Rigidbody2D _rigidbody2D;
        private Collider2D _collider2D;
        private Collider2D _instigatorCollider;
        private Transform _instigator;
        private Vector2 _travelDirection = Vector2.right;
        private float _damage;
        private float _remainingLifetime;
        private bool _isInitialized;

        private void Awake()
        {
            _rigidbody2D = GetComponent<Rigidbody2D>();
            _collider2D = GetComponent<Collider2D>();

            _rigidbody2D.gravityScale = 0f;
            _rigidbody2D.freezeRotation = true;
        }

        private void Update()
        {
            if (!_isInitialized)
            {
                return;
            }

            _remainingLifetime -= Time.deltaTime;

            if (_remainingLifetime <= 0f)
            {
                Despawn();
            }
        }

        private void OnDisable()
        {
            if (_rigidbody2D != null)
            {
                _rigidbody2D.linearVelocity = Vector2.zero;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!_isInitialized || other == _instigatorCollider)
            {
                return;
            }

            HandleHit(other);
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (!_isInitialized || collision.collider == _instigatorCollider)
            {
                return;
            }

            HandleHit(collision.collider);
        }

        public void Initialize(in ProjectileSpawnRequest request)
        {
            _instigator = request.Instigator;
            _instigatorCollider = request.InstigatorCollider;
            _damage = request.Damage;
            _remainingLifetime = Mathf.Max(0.01f, request.Lifetime);
            _travelDirection = request.Direction.sqrMagnitude > 0.0001f
                ? request.Direction.normalized
                : Vector2.right;

            transform.position = request.Position;
            transform.rotation = Quaternion.FromToRotation(Vector3.right, _travelDirection);
            _rigidbody2D.linearVelocity = _travelDirection * Mathf.Max(0f, request.Speed);

            if (_instigatorCollider != null)
            {
                Physics2D.IgnoreCollision(_collider2D, _instigatorCollider, true);
            }

            _isInitialized = true;
        }

        private void HandleHit(Collider2D other)
        {
            if (DamageableResolver.TryResolve(other, out IDamageable damageable))
            {
                damageable.ApplyDamage(new DamageInfo(_damage, _travelDirection, _instigator));
                Despawn();
                return;
            }

            // Room bounds, door sensors, pickups, and other gameplay triggers should not eat projectiles.
            // Solid colliders still stop the shot so walls remain meaningful.
            if (other.isTrigger)
            {
                return;
            }

            Despawn();
        }

        private void Despawn()
        {
            _isInitialized = false;
            Destroy(gameObject);
        }
    }
}
