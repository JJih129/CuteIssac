using System.Collections.Generic;
using CuteIssac.Common.Combat;
using CuteIssac.Core.Audio;
using CuteIssac.Core.Feedback;
using CuteIssac.Core.Gameplay;
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
        private static readonly Dictionary<int, List<ProjectileLogic>> ActiveOrbitProjectilesByInstigator = new();

        [Header("Optional Presentation")]
        [Tooltip("Optional visual bridge for sprite, trail, and effects. The projectile still works without it.")]
        [SerializeField] private ProjectileVisual projectileVisual;

        [Header("Speed Tuning")]
        [Tooltip("Global gameplay tuning applied after weapon/item projectile speed is resolved.")]
        [SerializeField] [Range(0.1f, 1.6f)] private float projectileSpeedMultiplier = 1.25f;

        [Header("Homing")]
        [Tooltip("Baseline search radius used when a projectile has homing enabled.")]
        [SerializeField] [Min(0.5f)] private float homingSearchRadius = 6f;
        [Tooltip("Baseline turn speed in degrees per second. Homing strength multiplies this value.")]
        [SerializeField] [Min(0f)] private float homingTurnRateDegrees = 180f;

        [Header("Trait Support")]
        [SerializeField] private DamageArea explosiveDamageArea;
        [SerializeField] [Min(0.25f)] private float explosiveRadiusBase = 1.35f;
        [SerializeField] [Min(0f)] private float explosiveRadiusPerStrength = 0.35f;
        [SerializeField] [Min(0.1f)] private float explosiveDamageMultiplierBase = 0.55f;
        [SerializeField] [Min(0f)] private float explosiveDamageMultiplierPerStrength = 0.18f;
        [SerializeField] [Min(0f)] private float explosiveKnockbackMultiplierBase = 1.1f;
        [SerializeField] [Min(0f)] private float explosiveKnockbackMultiplierPerStrength = 0.2f;
        [SerializeField] [Range(0.1f, 1f)] private float bounceSpeedRetention = 0.94f;
        [SerializeField] [Min(0.005f)] private float bounceSurfaceSeparation = 0.04f;
        [SerializeField] [Min(0f)] private float lifestealHealRatioBase = 0.08f;
        [SerializeField] [Min(0f)] private float lifestealHealRatioPerStrength = 0.04f;
        [SerializeField] [Min(2)] private int splitChildCountBase = 2;
        [SerializeField] [Min(0)] private int splitAdditionalChildrenPerStrength = 1;
        [SerializeField] [Range(1f, 180f)] private float splitSpreadDegrees = 26f;
        [SerializeField] [Range(0.1f, 1f)] private float splitDamageMultiplier = 0.62f;
        [SerializeField] [Range(0.1f, 1f)] private float splitSpeedMultiplier = 0.94f;
        [SerializeField] [Range(0.1f, 1f)] private float splitLifetimeMultiplier = 0.76f;
        [SerializeField] [Range(0.1f, 1f)] private float splitScaleMultiplier = 0.84f;
        [SerializeField] [Min(0.01f)] private float splitSpawnOffset = 0.18f;
        [SerializeField] [Min(0.05f)] private float shieldPulseInterval = 0.1f;
        [SerializeField] [Min(0.1f)] private float shieldRadiusBase = 0.85f;
        [SerializeField] [Min(0f)] private float shieldRadiusPerStrength = 0.24f;
        [SerializeField] [Min(0.5f)] private float laserLengthBase = 4.25f;
        [SerializeField] [Min(0f)] private float laserLengthPerStrength = 1.2f;
        [SerializeField] [Min(0.1f)] private float laserWidthBase = 0.62f;
        [SerializeField] [Min(0f)] private float laserWidthPerStrength = 0.16f;
        [SerializeField] [Range(0.1f, 2f)] private float laserDamageMultiplierBase = 0.72f;
        [SerializeField] [Min(0f)] private float laserDamageMultiplierPerStrength = 0.2f;
        [SerializeField] [Min(8)] private int laserHitBufferSize = 24;
        [SerializeField] [Min(1)] private int orbitCountBase = 1;
        [SerializeField] [Min(0)] private int orbitAdditionalCountPerStrength = 1;
        [SerializeField] [Min(0.25f)] private float orbitRadiusBase = 1.15f;
        [SerializeField] [Min(0f)] private float orbitRadiusPerStrength = 0.18f;
        [SerializeField] [Min(30f)] private float orbitAngularSpeedBase = 165f;
        [SerializeField] [Min(0f)] private float orbitAngularSpeedPerStrength = 28f;
        [SerializeField] [Min(0.05f)] private float orbitHitCooldown = 0.2f;

        private Rigidbody2D _rigidbody2D;
        private Collider2D _collider2D;
        private Collider2D _instigatorCollider;
        private Transform _instigator;
        private ProjectileLogic _projectilePrefabTemplate;
        private Vector2 _travelDirection = Vector2.right;
        private float _damage;
        private float _knockback;
        private float _remainingLifetime;
        private float _homingStrength;
        private int _remainingPierces;
        private int _remainingBounces;
        private float _nextShieldPulseTime;
        private ProjectileDamageTarget _damageTarget = ProjectileDamageTarget.Any;
        private ProjectileTraitState _traits;
        private OpeningCadenceVolleyRole _openingCadenceRole;
        private float _openingCadenceRoleWeight;
        private bool _isInitialized;
        private bool _isDespawning;
        private bool _isOrbiting;
        private Vector3 _initialLocalScale;
        private readonly HashSet<int> _hitTargetIds = new();
        private readonly List<Collider2D> _ignoredColliders = new();
        private readonly List<EnemyProjectileLogic> _enemyProjectileBuffer = new();
        private readonly HashSet<int> _laserProcessedTargets = new();
        private readonly Dictionary<int, float> _orbitDamageCooldowns = new();
        private Collider2D[] _laserHitBuffer;

        private void Awake()
        {
            _rigidbody2D = GetComponent<Rigidbody2D>();
            _collider2D = GetComponent<Collider2D>();
            _initialLocalScale = transform.localScale;

            if (projectileVisual == null)
            {
                projectileVisual = GetComponent<ProjectileVisual>();
            }

            if (explosiveDamageArea == null)
            {
                explosiveDamageArea = GetComponent<DamageArea>();
            }

            if (explosiveDamageArea == null)
            {
                explosiveDamageArea = gameObject.AddComponent<DamageArea>();
            }

            EnsureLaserBuffer();

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
            if (!_isInitialized || _isDespawning)
            {
                return;
            }

            if (_isOrbiting)
            {
                UpdateOrbitMotion();
                TryApplyShieldPulse();
                return;
            }

            TryApplyShieldPulse();

            if (_homingStrength <= 0f)
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
            UnregisterOrbit();

            if (_rigidbody2D != null)
            {
                _rigidbody2D.linearVelocity = Vector2.zero;
            }

            _instigatorCollider = null;
            _instigator = null;
            _projectilePrefabTemplate = null;
            _traits = default;
            _openingCadenceRole = OpeningCadenceVolleyRole.None;
            _openingCadenceRoleWeight = 0f;
            _orbitDamageCooldowns.Clear();
            _isInitialized = false;
            _isDespawning = false;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!_isInitialized || _isDespawning || other == _instigatorCollider)
            {
                return;
            }

            HandleHit(other, null);
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (!_isInitialized || _isDespawning || collision.collider == _instigatorCollider)
            {
                return;
            }

            Vector2? impactNormal = collision.contactCount > 0
                ? collision.GetContact(0).normal
                : null;
            HandleHit(collision.collider, impactNormal);
        }

        public void Initialize(in ProjectileSpawnRequest request)
        {
            RestoreIgnoredCollisions();
            _instigator = request.Instigator;
            _instigatorCollider = request.InstigatorCollider;
            _projectilePrefabTemplate = request.ProjectilePrefab;
            _damage = request.Damage;
            _knockback = request.Knockback;
            _remainingLifetime = Mathf.Max(0.01f, request.Lifetime);
            _remainingPierces = Mathf.Max(0, request.PierceCount);
            _remainingBounces = ResolveBounceCount(request.Traits);
            _nextShieldPulseTime = Time.time + shieldPulseInterval;
            _homingStrength = Mathf.Max(0f, request.HomingStrength);
            _damageTarget = request.DamageTarget;
            _traits = request.Traits;
            _openingCadenceRole = request.OpeningCadenceRole;
            _openingCadenceRoleWeight = Mathf.Clamp01(request.OpeningCadenceRoleWeight);
            _travelDirection = request.Direction.sqrMagnitude > 0.0001f
                ? request.Direction.normalized
                : Vector2.right;
            _hitTargetIds.Clear();

            transform.position = request.Position;
            transform.rotation = Quaternion.FromToRotation(Vector3.right, _travelDirection);
            transform.localScale = _initialLocalScale * Mathf.Max(0.05f, request.Scale);
            _isOrbiting = TryRegisterOrbit(request.Traits);

            if (_isOrbiting)
            {
                _rigidbody2D.linearVelocity = Vector2.zero;
                UpdateOrbitMotion(forceSnap: true);
            }
            else
            {
                float tunedSpeed = Mathf.Max(0f, request.Speed) * Mathf.Clamp(projectileSpeedMultiplier, 0.1f, 1.6f);
                _rigidbody2D.linearVelocity =
                    (_travelDirection * tunedSpeed) +
                    request.InheritedVelocity;
            }

            if (_instigatorCollider != null)
            {
                IgnoreCollision(_instigatorCollider);
            }

            projectileVisual?.HandleInitialized(_travelDirection, _traits, _openingCadenceRole, _openingCadenceRoleWeight);
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

        private void HandleHit(Collider2D other, Vector2? impactNormal)
        {
            Vector3 impactPosition = other.ClosestPoint(transform.position);

            RoomObstacleController obstacle = other.GetComponentInParent<RoomObstacleController>();

            if (obstacle != null && obstacle.TryHandleProjectile(false, impactPosition, out ProjectileImpactType obstacleImpactType))
            {
                if (_isOrbiting)
                {
                    return;
                }

                if (obstacleImpactType == ProjectileImpactType.Solid
                    && TryBounce(other, impactPosition, impactNormal))
                {
                    return;
                }

                Despawn(obstacleImpactType, impactPosition);
                return;
            }

            if (DamageableResolver.TryResolve(other, out IDamageable damageable))
            {
                if (!CanDamage(other))
                {
                    return;
                }

                EnemyHealth enemyHealth = other.GetComponentInParent<EnemyHealth>();

                int targetId = ResolveTargetId(other, damageable);

                if (_isOrbiting)
                {
                    if (_orbitDamageCooldowns.TryGetValue(targetId, out float nextHitTime) && Time.time < nextHitTime)
                    {
                        return;
                    }
                }
                else if (_hitTargetIds.Contains(targetId))
                {
                    return;
                }

                if (_isOrbiting)
                {
                    _orbitDamageCooldowns[targetId] = Time.time + orbitHitCooldown;
                }
                else
                {
                    _hitTargetIds.Add(targetId);
                }

                damageable.ApplyDamage(new DamageInfo(_damage, _travelDirection, _instigator, _knockback));
                TryApplyLifestealReward(impactPosition);
                if (enemyHealth != null)
                {
                    GameplayRuntimeEvents.RaisePlayerProjectileHit(new PlayerProjectileHitSignal(
                        _instigator,
                        this,
                        enemyHealth,
                        impactPosition,
                        _travelDirection,
                        _damage,
                        _openingCadenceRole,
                        _openingCadenceRoleWeight));
                }

                projectileVisual?.HandleImpact(ProjectileImpactType.Damageable, impactPosition);

                if (_isOrbiting)
                {
                    return;
                }

                if (_remainingPierces > 0)
                {
                    _remainingPierces--;
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

            if (TryBounce(other, impactPosition, impactNormal))
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
            TryPlayImpactAudio(impactType, effectPosition);
            TryApplyTraitImpact(impactType, effectPosition);
            TryApplyLaserCut(impactType, effectPosition);
            TrySpawnSplitProjectiles(impactType, effectPosition);
            projectileVisual?.HandleDespawn(impactType, effectPosition);
            RestoreIgnoredCollisions();
            PrefabPoolService.Return(gameObject);
        }

        private void TryPlayImpactAudio(ProjectileImpactType impactType, Vector3 effectPosition)
        {
            if (impactType != ProjectileImpactType.Solid || !_traits.IsExplosive)
            {
                return;
            }

            GameAudioEvents.Raise(GameAudioEventType.RocketWallImpact, effectPosition);
        }

        private void TryApplyTraitImpact(ProjectileImpactType impactType, Vector3 effectPosition)
        {
            if (impactType == ProjectileImpactType.None
                || !_traits.IsExplosive
                || explosiveDamageArea == null)
            {
                return;
            }

            float explosionStrength = Mathf.Max(0.01f, _traits.ExplosionStrength);
            float radius = explosiveRadiusBase + (Mathf.Max(0f, explosionStrength - 1f) * explosiveRadiusPerStrength);
            float damage = _damage * (explosiveDamageMultiplierBase + (Mathf.Max(0f, explosionStrength - 1f) * explosiveDamageMultiplierPerStrength));
            float knockback = _knockback * (explosiveKnockbackMultiplierBase + (Mathf.Max(0f, explosionStrength - 1f) * explosiveKnockbackMultiplierPerStrength));

            if (damage <= 0f || radius <= 0.05f)
            {
                return;
            }

            BombExplosionInfo explosionInfo = new(
                effectPosition,
                radius,
                damage,
                knockback,
                _instigator);
            explosiveDamageArea.ApplyExplosion(in explosionInfo, _damageTarget, _instigatorCollider);
        }

        private void TryApplyLifestealReward(Vector3 impactPosition)
        {
            if (!_traits.Has(ProjectileTraitFlags.Lifesteal)
                || _damageTarget != ProjectileDamageTarget.EnemyOnly
                || _instigator == null)
            {
                return;
            }

            PlayerHealth playerHealth = _instigator.GetComponent<PlayerHealth>();

            if (playerHealth == null)
            {
                playerHealth = _instigator.GetComponentInParent<PlayerHealth>();
            }

            if (playerHealth == null)
            {
                return;
            }

            float lifestealStrength = Mathf.Max(0.01f, _traits.LifestealStrength);
            float healRatio = lifestealHealRatioBase + (Mathf.Max(0f, lifestealStrength - 1f) * lifestealHealRatioPerStrength);
            float healAmount = _damage * healRatio;

            if (healAmount <= 0.01f || !playerHealth.RestoreHealth(healAmount))
            {
                return;
            }

            GameplayFeedbackEvents.RaiseFloatingFeedback(new FloatingFeedbackRequest(
                playerHealth.transform.position + new Vector3(0.1f, 0.42f, 0f),
                $"+{healAmount:0.#}",
                new Color(0.48f, 1f, 0.6f, 1f),
                0.48f,
                0.56f,
                1.08f,
                visualProfile: FloatingFeedbackVisualProfile.Pickup));
        }

        private bool TryRegisterOrbit(ProjectileTraitState traits)
        {
            if (!traits.IsOrbiting || _instigator == null)
            {
                return false;
            }

            int instigatorId = _instigator.GetInstanceID();
            if (!ActiveOrbitProjectilesByInstigator.TryGetValue(instigatorId, out List<ProjectileLogic> orbitGroup))
            {
                orbitGroup = new List<ProjectileLogic>();
                ActiveOrbitProjectilesByInstigator.Add(instigatorId, orbitGroup);
            }

            CompactOrbitGroup(orbitGroup);
            int maxOrbitCount = ResolveMaxOrbitCount(traits.OrbitStrength);

            while (orbitGroup.Count >= maxOrbitCount && orbitGroup.Count > 0)
            {
                ProjectileLogic oldest = orbitGroup[0];
                orbitGroup.RemoveAt(0);
                if (oldest != null && oldest != this)
                {
                    oldest.ForceDissipate();
                }
            }

            if (!orbitGroup.Contains(this))
            {
                orbitGroup.Add(this);
            }

            return true;
        }

        private void UnregisterOrbit()
        {
            if (_instigator == null)
            {
                _isOrbiting = false;
                return;
            }

            int instigatorId = _instigator.GetInstanceID();
            if (!ActiveOrbitProjectilesByInstigator.TryGetValue(instigatorId, out List<ProjectileLogic> orbitGroup))
            {
                _isOrbiting = false;
                return;
            }

            orbitGroup.Remove(this);
            CompactOrbitGroup(orbitGroup);
            if (orbitGroup.Count == 0)
            {
                ActiveOrbitProjectilesByInstigator.Remove(instigatorId);
            }

            _isOrbiting = false;
        }

        public void ForceDissipate(ProjectileImpactType impactType = ProjectileImpactType.None)
        {
            if (!_isInitialized || _isDespawning)
            {
                return;
            }

            Despawn(impactType, transform.position);
        }

        private void UpdateOrbitMotion(bool forceSnap = false)
        {
            if (!_isOrbiting)
            {
                return;
            }

            if (_instigator == null)
            {
                ForceDissipate();
                return;
            }

            int instigatorId = _instigator.GetInstanceID();
            if (!ActiveOrbitProjectilesByInstigator.TryGetValue(instigatorId, out List<ProjectileLogic> orbitGroup))
            {
                return;
            }

            CompactOrbitGroup(orbitGroup);
            int orbitIndex = orbitGroup.IndexOf(this);
            if (orbitIndex < 0)
            {
                orbitGroup.Add(this);
                orbitIndex = orbitGroup.Count - 1;
            }

            int orbitCount = Mathf.Max(1, orbitGroup.Count);
            float orbitRadius = orbitRadiusBase + (Mathf.Max(0f, _traits.OrbitStrength - 1f) * orbitRadiusPerStrength);
            float angularSpeed = orbitAngularSpeedBase + (Mathf.Max(0f, _traits.OrbitStrength - 1f) * orbitAngularSpeedPerStrength);
            float slotAngle = (360f / orbitCount) * orbitIndex;
            float currentAngle = slotAngle + (Time.time * angularSpeed);
            Vector2 radialOffset = Rotate(Vector2.right, currentAngle) * orbitRadius;
            Vector2 center = _instigator.position;
            Vector2 targetPosition = center + radialOffset;
            Vector2 tangentDirection = new Vector2(-radialOffset.y, radialOffset.x).normalized;

            if (tangentDirection.sqrMagnitude <= 0.0001f)
            {
                tangentDirection = Vector2.up;
            }

            _travelDirection = tangentDirection;
            transform.position = forceSnap
                ? (Vector3)targetPosition
                : Vector3.Lerp(transform.position, (Vector3)targetPosition, 0.42f);
            transform.rotation = Quaternion.FromToRotation(Vector3.right, tangentDirection);

            if (_rigidbody2D != null)
            {
                _rigidbody2D.linearVelocity = Vector2.zero;
            }
        }

        private void TryApplyLaserCut(ProjectileImpactType impactType, Vector3 effectPosition)
        {
            if (impactType == ProjectileImpactType.None
                || !_traits.IsLaser)
            {
                return;
            }

            EnsureLaserBuffer();
            _laserProcessedTargets.Clear();

            float laserStrength = Mathf.Max(0.01f, _traits.LaserStrength);
            float dynamicLength = Mathf.Max(
                laserLengthBase,
                (_rigidbody2D != null ? _rigidbody2D.linearVelocity.magnitude : 0f) * 0.48f);
            float length = dynamicLength + (Mathf.Max(0f, laserStrength - 1f) * laserLengthPerStrength);
            float width = laserWidthBase + (Mathf.Max(0f, laserStrength - 1f) * laserWidthPerStrength);
            float damage = _damage * (laserDamageMultiplierBase + (Mathf.Max(0f, laserStrength - 1f) * laserDamageMultiplierPerStrength));

            if (length <= 0.05f || width <= 0.05f || damage <= 0.01f)
            {
                return;
            }

            Vector2 direction = _travelDirection.sqrMagnitude > 0.0001f
                ? _travelDirection.normalized
                : Vector2.right;
            Vector2 center = (Vector2)effectPosition + (direction * (length * 0.5f));
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            ContactFilter2D contactFilter = BuildLaserContactFilter();
            int hitCount = Physics2D.OverlapBox(center, new Vector2(length, width), angle, contactFilter, _laserHitBuffer);

            while (hitCount >= _laserHitBuffer.Length)
            {
                _laserHitBuffer = new Collider2D[_laserHitBuffer.Length * 2];
                hitCount = Physics2D.OverlapBox(center, new Vector2(length, width), angle, contactFilter, _laserHitBuffer);
            }

            for (int index = 0; index < hitCount; index++)
            {
                Collider2D hit = _laserHitBuffer[index];
                if (hit == null || hit == _instigatorCollider || !CanDamage(hit))
                {
                    continue;
                }

                if (!DamageableResolver.TryResolve(hit, out IDamageable damageable))
                {
                    continue;
                }

                int targetId = ResolveTargetId(hit, damageable);
                if (_hitTargetIds.Contains(targetId) || !_laserProcessedTargets.Add(targetId))
                {
                    continue;
                }

                damageable.ApplyDamage(new DamageInfo(damage, direction, _instigator, _knockback));
            }

            for (int index = 0; index < hitCount; index++)
            {
                _laserHitBuffer[index] = null;
            }

            ClearEnemyProjectilesAlongLaser(effectPosition, direction, length, width);
        }

        private void TryApplyShieldPulse()
        {
            if (!_traits.IsShielded
                || Time.time < _nextShieldPulseTime)
            {
                return;
            }

            _nextShieldPulseTime = Time.time + Mathf.Max(0.02f, shieldPulseInterval);
            float shieldRadius = shieldRadiusBase + (Mathf.Max(0f, _traits.ShieldStrength - 1f) * shieldRadiusPerStrength);
            if (shieldRadius <= 0.05f)
            {
                return;
            }

            _enemyProjectileBuffer.Clear();
            EnemyProjectileLogic.CollectActiveProjectiles(_enemyProjectileBuffer);

            Vector2 center = transform.position;
            float shieldRadiusSq = shieldRadius * shieldRadius;
            for (int index = 0; index < _enemyProjectileBuffer.Count; index++)
            {
                EnemyProjectileLogic projectile = _enemyProjectileBuffer[index];
                if (projectile == null)
                {
                    continue;
                }

                Vector2 offset = projectile.WorldPosition - center;
                if (offset.sqrMagnitude > shieldRadiusSq)
                {
                    continue;
                }

                projectile.ForceDissipate(ProjectileImpactType.Solid);
            }
        }

        private void ClearEnemyProjectilesAlongLaser(Vector2 origin, Vector2 direction, float length, float width)
        {
            _enemyProjectileBuffer.Clear();
            EnemyProjectileLogic.CollectActiveProjectiles(_enemyProjectileBuffer);
            float maxDistanceSq = (length * length) + (width * width);
            float halfWidth = width * 0.5f;
            Vector2 segmentEnd = origin + (direction * length);

            for (int index = 0; index < _enemyProjectileBuffer.Count; index++)
            {
                EnemyProjectileLogic projectile = _enemyProjectileBuffer[index];
                if (projectile == null)
                {
                    continue;
                }

                Vector2 position = projectile.WorldPosition;
                if ((position - origin).sqrMagnitude > maxDistanceSq)
                {
                    continue;
                }

                if (DistanceToSegment(position, origin, segmentEnd) > halfWidth)
                {
                    continue;
                }

                projectile.ForceDissipate(ProjectileImpactType.Solid);
            }
        }

        private void TrySpawnSplitProjectiles(ProjectileImpactType impactType, Vector3 effectPosition)
        {
            if (impactType == ProjectileImpactType.None
                || !_traits.IsSplit
                || _projectilePrefabTemplate == null)
            {
                return;
            }

            int childCount = ResolveSplitChildCount(_traits.SplitStrength);
            if (childCount <= 0)
            {
                return;
            }

            float currentSpeed = _rigidbody2D != null
                ? _rigidbody2D.linearVelocity.magnitude
                : 0f;
            currentSpeed = Mathf.Max(0.1f, currentSpeed * splitSpeedMultiplier);
            float currentScale = ResolveCurrentScaleFactor();
            float childLifetime = Mathf.Max(0.12f, _remainingLifetime * splitLifetimeMultiplier);
            float childScale = Mathf.Max(0.18f, currentScale * splitScaleMultiplier);
            ProjectileTraitState childTraits = BuildSplitChildTraits();

            for (int childIndex = 0; childIndex < childCount; childIndex++)
            {
                Vector2 childDirection = ResolveSplitDirection(childIndex, childCount);
                ProjectileSpawnRequest childRequest = new ProjectileSpawnRequest
                {
                    ProjectilePrefab = _projectilePrefabTemplate,
                    Position = effectPosition + (Vector3)(childDirection * splitSpawnOffset),
                    Direction = childDirection,
                    InheritedVelocity = Vector2.zero,
                    Damage = _damage * splitDamageMultiplier,
                    Speed = currentSpeed,
                    Lifetime = childLifetime,
                    Scale = childScale,
                    Knockback = _knockback * splitDamageMultiplier,
                    PierceCount = _remainingPierces,
                    HomingStrength = _homingStrength,
                    Traits = childTraits,
                    Instigator = _instigator,
                    InstigatorCollider = _instigatorCollider,
                    DamageTarget = _damageTarget
                };

                Quaternion childRotation = Quaternion.FromToRotation(Vector3.right, childDirection);
                ProjectileLogic childProjectile = PrefabPoolService.Spawn(_projectilePrefabTemplate, childRequest.Position, childRotation);
                childProjectile?.Initialize(childRequest);
            }
        }

        private bool TryBounce(Collider2D other, Vector3 impactPosition, Vector2? impactNormal)
        {
            if (_remainingBounces <= 0
                || !_traits.Has(ProjectileTraitFlags.Bounce))
            {
                return false;
            }

            Vector2 bounceNormal = ResolveBounceNormal(other, impactPosition, impactNormal);

            if (bounceNormal.sqrMagnitude <= 0.0001f)
            {
                return false;
            }

            Vector2 currentVelocity = _rigidbody2D != null
                ? _rigidbody2D.linearVelocity
                : _travelDirection;

            if (currentVelocity.sqrMagnitude <= 0.0001f)
            {
                currentVelocity = _travelDirection.sqrMagnitude > 0.0001f
                    ? _travelDirection
                    : Vector2.right;
            }

            if (Vector2.Dot(bounceNormal, currentVelocity.normalized) > 0f)
            {
                bounceNormal = -bounceNormal;
            }

            Vector2 bouncedVelocity = Vector2.Reflect(currentVelocity, bounceNormal) * bounceSpeedRetention;

            if (bouncedVelocity.sqrMagnitude <= 0.0001f)
            {
                bouncedVelocity = Vector2.Reflect(_travelDirection, bounceNormal);
            }

            if (bouncedVelocity.sqrMagnitude <= 0.0001f)
            {
                return false;
            }

            _remainingBounces--;
            _travelDirection = bouncedVelocity.normalized;

            if (_rigidbody2D != null)
            {
                _rigidbody2D.linearVelocity = bouncedVelocity;
            }

            transform.position = impactPosition + (Vector3)(_travelDirection * bounceSurfaceSeparation);
            transform.rotation = Quaternion.FromToRotation(Vector3.right, _travelDirection);
            projectileVisual?.HandleImpact(ProjectileImpactType.Solid, impactPosition);
            return true;
        }

        private Vector2 ResolveBounceNormal(Collider2D other, Vector3 impactPosition, Vector2? impactNormal)
        {
            if (impactNormal.HasValue && impactNormal.Value.sqrMagnitude > 0.0001f)
            {
                return impactNormal.Value.normalized;
            }

            if (other == null)
            {
                return -_travelDirection;
            }

            Bounds bounds = other.bounds;
            Vector2 center = bounds.center;
            Vector2 extents = bounds.extents;
            Vector2 offset = (Vector2)impactPosition - center;

            if (extents.x <= 0.0001f || extents.y <= 0.0001f)
            {
                Vector2 fallback = offset.sqrMagnitude > 0.0001f ? offset.normalized : -_travelDirection;
                return fallback;
            }

            float normalizedX = offset.x / extents.x;
            float normalizedY = offset.y / extents.y;

            if (Mathf.Abs(normalizedX) > Mathf.Abs(normalizedY))
            {
                return new Vector2(Mathf.Sign(normalizedX), 0f);
            }

            return new Vector2(0f, Mathf.Sign(normalizedY));
        }

        private static int ResolveBounceCount(ProjectileTraitState traits)
        {
            if (!traits.Has(ProjectileTraitFlags.Bounce))
            {
                return 0;
            }

            float strength = Mathf.Max(0f, traits.BounceStrength);
            return Mathf.Max(1, Mathf.CeilToInt(strength));
        }

        private int ResolveSplitChildCount(float splitStrength)
        {
            float clampedStrength = Mathf.Max(0.01f, splitStrength);
            int bonusChildren = Mathf.Max(0, Mathf.FloorToInt(clampedStrength - 1f)) * splitAdditionalChildrenPerStrength;
            return Mathf.Max(2, splitChildCountBase + bonusChildren);
        }

        private int ResolveMaxOrbitCount(float orbitStrength)
        {
            float clampedStrength = Mathf.Max(0.01f, orbitStrength);
            int bonusOrbitCount = Mathf.Max(0, Mathf.FloorToInt(clampedStrength - 1f)) * orbitAdditionalCountPerStrength;
            return Mathf.Max(1, orbitCountBase + bonusOrbitCount);
        }

        private Vector2 ResolveSplitDirection(int childIndex, int childCount)
        {
            Vector2 baseDirection = _travelDirection.sqrMagnitude > 0.0001f
                ? _travelDirection.normalized
                : Vector2.right;

            if (childCount <= 1)
            {
                return baseDirection;
            }

            float centerIndex = (childCount - 1) * 0.5f;
            float angleOffset = (childIndex - centerIndex) * splitSpreadDegrees;
            return Rotate(baseDirection, angleOffset).normalized;
        }

        private ProjectileTraitState BuildSplitChildTraits()
        {
            ProjectileTraitState childTraits = _traits;
            childTraits.Flags &= ~ProjectileTraitFlags.Split;
            childTraits.SplitStrength = 0f;
            return childTraits;
        }

        private float ResolveCurrentScaleFactor()
        {
            float baseScale = Mathf.Abs(_initialLocalScale.x);

            if (baseScale <= 0.0001f)
            {
                return 1f;
            }

            return Mathf.Max(0.05f, Mathf.Abs(transform.localScale.x) / baseScale);
        }

        private void EnsureLaserBuffer()
        {
            int capacity = Mathf.Max(8, laserHitBufferSize);
            if (_laserHitBuffer == null || _laserHitBuffer.Length < capacity)
            {
                _laserHitBuffer = new Collider2D[capacity];
            }
        }

        private static ContactFilter2D BuildLaserContactFilter()
        {
            ContactFilter2D contactFilter = new ContactFilter2D
            {
                useLayerMask = true,
                useTriggers = true
            };
            contactFilter.SetLayerMask(Physics2D.AllLayers);
            return contactFilter;
        }

        private static float DistanceToSegment(Vector2 point, Vector2 segmentStart, Vector2 segmentEnd)
        {
            Vector2 segment = segmentEnd - segmentStart;
            float segmentLengthSq = segment.sqrMagnitude;
            if (segmentLengthSq <= 0.0001f)
            {
                return Vector2.Distance(point, segmentStart);
            }

            float t = Mathf.Clamp01(Vector2.Dot(point - segmentStart, segment) / segmentLengthSq);
            Vector2 closestPoint = segmentStart + (segment * t);
            return Vector2.Distance(point, closestPoint);
        }

        private static void CompactOrbitGroup(List<ProjectileLogic> orbitGroup)
        {
            if (orbitGroup == null)
            {
                return;
            }

            for (int index = orbitGroup.Count - 1; index >= 0; index--)
            {
                if (orbitGroup[index] == null)
                {
                    orbitGroup.RemoveAt(index);
                }
            }
        }

        private static Vector2 Rotate(Vector2 direction, float angleDegrees)
        {
            float radians = angleDegrees * Mathf.Deg2Rad;
            float sin = Mathf.Sin(radians);
            float cos = Mathf.Cos(radians);
            return new Vector2(
                (direction.x * cos) - (direction.y * sin),
                (direction.x * sin) + (direction.y * cos));
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
                    PlayerHealth playerHealth = PlayerRegistry.ActiveHealth;
                    return playerHealth != null ? playerHealth.transform : null;
                default:
                    return null;
            }
        }

        private Transform FindClosestEnemyTarget()
        {
            float searchRadiusSq = homingSearchRadius * homingSearchRadius;
            Transform closestTarget = null;
            float closestDistanceSq = searchRadiusSq;

            for (int index = 0; index < EnemyRegistry.Count; index++)
            {
                EnemyHealth enemyHealth = EnemyRegistry.GetAt(index);

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
