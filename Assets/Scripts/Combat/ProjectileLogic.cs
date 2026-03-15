using System.Collections.Generic;
using CuteIssac.Common.Combat;
using CuteIssac.Core.Pooling;
using CuteIssac.Enemy;
using CuteIssac.Player;
using CuteIssac.Room;
using UnityEngine;

namespace CuteIssac.Combat
{
    /// <summary>
    /// Owns projectile gameplay behavior only.
    /// Motion, collision, damage, and expiry live here so visuals can be swapped without touching combat logic.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public sealed class ProjectileLogic : MonoBehaviour
    {
        [Header("Optional Presentation")]
        [Tooltip("Optional visual bridge for sprite, trail, and effects. The projectile still works without it.")]
        [SerializeField] private ProjectileVisual projectileVisual;

        [Header("Homing")]
        [Tooltip("Baseline search radius used when a projectile has homing enabled.")]
        [SerializeField] [Min(0.5f)] private float homingSearchRadius = 6f;
        [Tooltip("Baseline turn speed in degrees per second. Homing strength multiplies this value.")]
        [SerializeField] [Min(0f)] private float homingTurnRateDegrees = 180f;

        private Rigidbody2D _rigidbody2D;
        private Collider2D _collider2D;
        private Collider2D _instigatorCollider;
        private Transform _instigator;
        private Vector2 _travelDirection = Vector2.right;
        private float _damage;
        private float _knockback;
        private float _remainingLifetime;
        private float _homingStrength;
        private int _remainingPierces;
        private ProjectileDamageTarget _damageTarget = ProjectileDamageTarget.Any;
        private bool _isInitialized;
        private bool _isDespawning;
        private Vector3 _initialLocalScale;
        private readonly HashSet<int> _hitTargetIds = new();
        private readonly List<Collider2D> _ignoredColliders = new();

        private void Awake()
        {
            _rigidbody2D = GetComponent<Rigidbody2D>();
            _collider2D = GetComponent<Collider2D>();
            _initialLocalScale = transform.localScale;

            if (projectileVisual == null)
            {
                projectileVisual = GetComponent<ProjectileVisual>();
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

            Transform target = FindHomingTarget();

            if (target == null)
            {
                return;
            }

            Vector2 toTarget = (Vector2)target.position - (Vector2)transform.position;

            if (toTarget.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            float speed = _rigidbody2D.linearVelocity.magnitude;
            float maxRadiansDelta = homingTurnRateDegrees * Mathf.Max(0f, _homingStrength) * Mathf.Deg2Rad * Time.fixedDeltaTime;
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

        public void Initialize(in ProjectileSpawnRequest request)
        {
            RestoreIgnoredCollisions();
            _instigator = request.Instigator;
            _instigatorCollider = request.InstigatorCollider;
            _damage = request.Damage;
            _knockback = request.Knockback;
            _remainingLifetime = Mathf.Max(0.01f, request.Lifetime);
            _remainingPierces = Mathf.Max(0, request.PierceCount);
            _homingStrength = Mathf.Max(0f, request.HomingStrength);
            _damageTarget = request.DamageTarget;
            _travelDirection = request.Direction.sqrMagnitude > 0.0001f
                ? request.Direction.normalized
                : Vector2.right;
            _hitTargetIds.Clear();

            transform.position = request.Position;
            transform.rotation = Quaternion.FromToRotation(Vector3.right, _travelDirection);
            transform.localScale = _initialLocalScale * Mathf.Max(0.05f, request.Scale);
            _rigidbody2D.linearVelocity =
                (_travelDirection * Mathf.Max(0f, request.Speed)) +
                request.InheritedVelocity;

            if (_instigatorCollider != null)
            {
                IgnoreCollision(_instigatorCollider);
            }

            projectileVisual?.HandleInitialized(_travelDirection);
            _isInitialized = true;
            _isDespawning = false;
        }

        private void Reset()
        {
            projectileVisual = GetComponent<ProjectileVisual>();
        }

        private void OnValidate()
        {
            if (projectileVisual == null)
            {
                projectileVisual = GetComponent<ProjectileVisual>();
            }
        }

        private void HandleHit(Collider2D other)
        {
            Vector3 impactPosition = other.ClosestPoint(transform.position);

            RoomObstacleController obstacle = other.GetComponentInParent<RoomObstacleController>();

            if (obstacle != null && obstacle.TryHandleProjectile(false, impactPosition, out ProjectileImpactType obstacleImpactType))
            {
                Despawn(obstacleImpactType, impactPosition);
                return;
            }

            if (DamageableResolver.TryResolve(other, out IDamageable damageable))
            {
                if (!CanDamage(other))
                {
                    return;
                }

                int targetId = ResolveTargetId(other, damageable);

                if (_hitTargetIds.Contains(targetId))
                {
                    return;
                }

                _hitTargetIds.Add(targetId);

                damageable.ApplyDamage(new DamageInfo(_damage, _travelDirection, _instigator, _knockback));

                if (_remainingPierces > 0)
                {
                    _remainingPierces--;
                    projectileVisual?.HandleImpact(ProjectileImpactType.Damageable, impactPosition);
                    IgnoreCollision(other);
                    return;
                }

                Despawn(ProjectileImpactType.Damageable, impactPosition);
                return;
            }

            // Room bounds, door sensors, pickups, and other gameplay triggers should not eat projectiles.
            // Solid colliders still stop the shot so walls remain meaningful.
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

        private bool CanDamage(Collider2D other)
        {
            switch (_damageTarget)
            {
                case ProjectileDamageTarget.PlayerOnly:
                    return other.GetComponentInParent<PlayerHealth>() != null;
                case ProjectileDamageTarget.EnemyOnly:
                    return other.GetComponentInParent<EnemyHealth>() != null;
                default:
                    return true;
            }
        }

        private Transform FindHomingTarget()
        {
            switch (_damageTarget)
            {
                case ProjectileDamageTarget.EnemyOnly:
                    return FindClosestEnemyTarget();
                case ProjectileDamageTarget.PlayerOnly:
                    PlayerHealth playerHealth = FindFirstObjectByType<PlayerHealth>(FindObjectsInactive.Exclude);
                    return playerHealth != null ? playerHealth.transform : null;
                default:
                    return null;
            }
        }

        private Transform FindClosestEnemyTarget()
        {
            EnemyHealth[] candidates = FindObjectsByType<EnemyHealth>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            float searchRadiusSq = homingSearchRadius * homingSearchRadius;
            Transform closestTarget = null;
            float closestDistanceSq = searchRadiusSq;

            for (int index = 0; index < candidates.Length; index++)
            {
                EnemyHealth enemyHealth = candidates[index];

                if (enemyHealth == null || enemyHealth.IsDead || enemyHealth.transform == _instigator)
                {
                    continue;
                }

                if (_hitTargetIds.Contains(enemyHealth.GetInstanceID()))
                {
                    continue;
                }

                float distanceSq = ((Vector2)enemyHealth.transform.position - (Vector2)transform.position).sqrMagnitude;

                if (distanceSq > closestDistanceSq)
                {
                    continue;
                }

                closestDistanceSq = distanceSq;
                closestTarget = enemyHealth.transform;
            }

            return closestTarget;
        }

        private static int ResolveTargetId(Collider2D other, IDamageable damageable)
        {
            if (damageable is Object unityObject)
            {
                return unityObject.GetInstanceID();
            }

            return other.GetInstanceID();
        }
    }
}
