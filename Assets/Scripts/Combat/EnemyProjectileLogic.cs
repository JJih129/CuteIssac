using CuteIssac.Common.Combat;
using CuteIssac.Core.Pooling;
using CuteIssac.Player;
using CuteIssac.Room;
using System.Collections.Generic;
using UnityEngine;

namespace CuteIssac.Combat
{
    /// <summary>
    /// Enemy projectile gameplay path.
    /// This projectile only damages the player and ignores unrelated triggers.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public sealed class EnemyProjectileLogic : MonoBehaviour
    {
        [Header("Optional Presentation")]
        [SerializeField] private EnemyProjectileVisual projectileVisual;
        [Header("Homing")]
        [SerializeField] [Min(0.5f)] private float fallbackHomingSearchRadius = 5.5f;
        [SerializeField] [Min(0f)] private float fallbackHomingTurnRateDegrees = 135f;

        private Rigidbody2D _rigidbody2D;
        private Collider2D _collider2D;
        private Collider2D _instigatorCollider;
        private Transform _instigator;
        private Vector2 _travelDirection = Vector2.right;
        private float _damage;
        private float _remainingLifetime;
        private float _homingStrength;
        private float _homingSearchRadius;
        private float _homingTurnRateDegrees;
        private bool _isInitialized;
        private bool _isDespawning;
        private readonly List<Collider2D> _ignoredColliders = new();

        private void Awake()
        {
            _rigidbody2D = GetComponent<Rigidbody2D>();
            _collider2D = GetComponent<Collider2D>();

            if (projectileVisual == null)
            {
                projectileVisual = GetComponent<EnemyProjectileVisual>();
            }

            _rigidbody2D.gravityScale = 0f;
            _rigidbody2D.freezeRotation = true;
        }

        private void Update()
        {
            if (!_isInitialized || _isDespawning)
            {
                return;
            }

            _remainingLifetime -= Time.deltaTime;

            if (_remainingLifetime <= 0f)
            {
                Despawn(ProjectileImpactType.None, transform.position);
            }
        }

        private void FixedUpdate()
        {
            if (!_isInitialized || _isDespawning || _homingStrength <= 0f)
            {
                return;
            }

            PlayerHealth playerHealth = FindFirstObjectByType<PlayerHealth>(FindObjectsInactive.Exclude);

            if (playerHealth == null || playerHealth.IsDead)
            {
                return;
            }

            Vector2 toTarget = (Vector2)playerHealth.transform.position - (Vector2)transform.position;

            if (toTarget.sqrMagnitude <= 0.0001f || toTarget.sqrMagnitude > _homingSearchRadius * _homingSearchRadius)
            {
                return;
            }

            float speed = _rigidbody2D.linearVelocity.magnitude;
            float maxRadiansDelta = _homingTurnRateDegrees * Mathf.Max(0f, _homingStrength) * Mathf.Deg2Rad * Time.fixedDeltaTime;
            Vector3 rotatedDirection = Vector3.RotateTowards(_travelDirection, toTarget.normalized, maxRadiansDelta, 0f);
            _travelDirection = ((Vector2)rotatedDirection).normalized;

            if (_travelDirection.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            transform.rotation = Quaternion.FromToRotation(Vector3.right, _travelDirection);
            _rigidbody2D.linearVelocity = _travelDirection * speed;
        }

        private void OnDisable()
        {
            RestoreIgnoredCollisions();

            if (_rigidbody2D != null)
            {
                _rigidbody2D.linearVelocity = Vector2.zero;
            }

            _instigatorCollider = null;
            _instigator = null;
            _isInitialized = false;
            _isDespawning = false;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!_isInitialized || _isDespawning || other == _instigatorCollider)
            {
                return;
            }

            HandleHit(other);
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (!_isInitialized || _isDespawning || collision.collider == _instigatorCollider)
            {
                return;
            }

            HandleHit(collision.collider);
        }

        public void Initialize(in EnemyProjectileSpawnRequest request)
        {
            RestoreIgnoredCollisions();
            _instigator = request.Instigator;
            _instigatorCollider = request.InstigatorCollider;
            _damage = request.Damage;
            _remainingLifetime = Mathf.Max(0.01f, request.Lifetime);
            _homingStrength = Mathf.Max(0f, request.HomingStrength);
            _homingSearchRadius = request.HomingSearchRadius > 0f ? request.HomingSearchRadius : fallbackHomingSearchRadius;
            _homingTurnRateDegrees = request.HomingTurnRateDegrees > 0f ? request.HomingTurnRateDegrees : fallbackHomingTurnRateDegrees;
            _travelDirection = request.Direction.sqrMagnitude > 0.0001f
                ? request.Direction.normalized
                : Vector2.right;

            transform.position = request.Position;
            transform.rotation = Quaternion.FromToRotation(Vector3.right, _travelDirection);
            _rigidbody2D.linearVelocity = _travelDirection * Mathf.Max(0f, request.Speed);

            if (_instigatorCollider != null)
            {
                IgnoreCollision(_instigatorCollider);
            }

            projectileVisual?.HandleInitialized(_travelDirection);
            _isInitialized = true;
            _isDespawning = false;
        }

        private void HandleHit(Collider2D other)
        {
            Vector3 impactPosition = other.ClosestPoint(transform.position);

            RoomObstacleController obstacle = other.GetComponentInParent<RoomObstacleController>();

            if (obstacle != null && obstacle.TryHandleProjectile(true, impactPosition, out ProjectileImpactType obstacleImpactType))
            {
                Despawn(obstacleImpactType, impactPosition);
                return;
            }

            if (DamageableResolver.TryResolve(other, out IDamageable damageable))
            {
                if (other.GetComponentInParent<PlayerHealth>() == null)
                {
                    return;
                }

                damageable.ApplyDamage(new DamageInfo(_damage, _travelDirection, _instigator));
                Despawn(ProjectileImpactType.Damageable, impactPosition);
                return;
            }

            if (other.isTrigger)
            {
                return;
            }

            Despawn(ProjectileImpactType.Solid, impactPosition);
        }

        private void Despawn(ProjectileImpactType impactType, Vector3 effectPosition)
        {
            if (_isDespawning)
            {
                return;
            }

            _isDespawning = true;
            _isInitialized = false;
            projectileVisual?.HandleDespawn(impactType, effectPosition);
            RestoreIgnoredCollisions();
            PrefabPoolService.Return(gameObject);
        }

        private void Reset()
        {
            projectileVisual = GetComponent<EnemyProjectileVisual>();
        }

        private void OnValidate()
        {
            if (projectileVisual == null)
            {
                projectileVisual = GetComponent<EnemyProjectileVisual>();
            }
        }

        private void IgnoreCollision(Collider2D other)
        {
            if (_collider2D == null || other == null)
            {
                return;
            }

            Physics2D.IgnoreCollision(_collider2D, other, true);

            if (!_ignoredColliders.Contains(other))
            {
                _ignoredColliders.Add(other);
            }
        }

        private void RestoreIgnoredCollisions()
        {
            if (_collider2D == null)
            {
                _ignoredColliders.Clear();
                return;
            }

            for (int index = 0; index < _ignoredColliders.Count; index++)
            {
                Collider2D ignoredCollider = _ignoredColliders[index];

                if (ignoredCollider != null)
                {
                    Physics2D.IgnoreCollision(_collider2D, ignoredCollider, false);
                }
            }

            _ignoredColliders.Clear();
        }
    }
}
