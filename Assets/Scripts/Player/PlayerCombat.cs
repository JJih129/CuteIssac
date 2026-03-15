using CuteIssac.Combat;
using CuteIssac.Common.Input;
using CuteIssac.Core.Audio;
using CuteIssac.Core.Gameplay;
using CuteIssac.Data.Combat;
using UnityEngine;

namespace CuteIssac.Player
{
    /// <summary>
    /// Consumes player aim input and requests projectile spawns.
    /// Fire cadence and projectile data live in ScriptableObjects so item systems can extend them later.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerCombat : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private ProjectileSpawner projectileSpawner;
        [SerializeField] private Collider2D ownerCollider;
        [SerializeField] private MonoBehaviour inputReaderSource;
        [SerializeField] private PlayerStats playerStats;
        [SerializeField] private PlayerVisual playerVisual;
        [SerializeField] private Rigidbody2D playerRigidbody;

        [Header("Attack Data")]
        [SerializeField] private PlayerAttackDefinition attackDefinition;
        [SerializeField] [Range(0f, 30f)] private float multishotSpreadDegrees = 9f;
        [SerializeField] [Range(0f, 1f)] private float inertiaFactor = 0.22f;

        private IPlayerInputReader _inputReader;
        private float _shotCooldown;
        private Vector2 _lastAttackDirection = Vector2.right;

        public PlayerAttackDefinition AttackDefinition => attackDefinition;

        private void Awake()
        {
            if (!TryResolveProjectileSpawner() || !TryResolveInputReader())
            {
                enabled = false;
                return;
            }

            if (ownerCollider == null)
            {
                ownerCollider = GetComponent<Collider2D>();
            }

            if (playerStats == null)
            {
                playerStats = GetComponent<PlayerStats>();
            }

            if (playerVisual == null)
            {
                playerVisual = GetComponent<PlayerVisual>();
            }

            if (playerRigidbody == null)
            {
                playerRigidbody = GetComponent<Rigidbody2D>();
            }

            if (playerVisual != null && playerVisual.MuzzleAnchor != null)
            {
                projectileSpawner.SetSpawnOrigin(playerVisual.MuzzleAnchor);
            }
        }

        private void Update()
        {
            if (_shotCooldown > 0f)
            {
                _shotCooldown -= Time.deltaTime;
            }

            if (attackDefinition == null || !attackDefinition.IsValid)
            {
                return;
            }

            PlayerGameplayInputState inputState = _inputReader.ReadState();

            if (!inputState.HasAimInput)
            {
                return;
            }

            Vector2 attackDirection = QuantizeAimToCardinal(inputState.Aim);

            if (attackDirection == Vector2.zero)
            {
                return;
            }

            _lastAttackDirection = attackDirection;
            playerVisual?.SetAimDirection(attackDirection);
            TryFire(attackDirection);
        }

        private void TryFire(Vector2 attackDirection)
        {
            if (_shotCooldown > 0f)
            {
                return;
            }

            int shotCount = ResolveShotCount();
            Vector2 fireDirection = attackDirection.normalized;
            Vector2 inheritedVelocity = ResolveInheritedVelocity();

            for (int shotIndex = 0; shotIndex < shotCount; shotIndex++)
            {
                Vector2 shotDirection = ResolveShotDirection(fireDirection, shotIndex, shotCount);
                ProjectileSpawnRequest spawnRequest = BuildSpawnRequest(shotDirection, inheritedVelocity);
                projectileSpawner.Spawn(in spawnRequest);
            }

            playerVisual?.HandleFired(attackDirection);
            GameplayRuntimeEvents.RaiseProjectileFired(new ProjectileFiredSignal(
                transform,
                projectileSpawner.GetSpawnPosition(fireDirection, attackDefinition.MuzzleOffset),
                fireDirection,
                shotCount));
            GameAudioEvents.Raise(GameAudioEventType.ProjectileFired, transform.position);
            _shotCooldown = ResolveFireInterval();
        }

        private ProjectileSpawnRequest BuildSpawnRequest(Vector2 attackDirection, Vector2 inheritedVelocity)
        {
            ProjectileDefinition projectileDefinition = attackDefinition.ProjectileDefinition;

            return new ProjectileSpawnRequest
            {
                ProjectilePrefab = projectileDefinition.ProjectilePrefab,
                Position = projectileSpawner.GetSpawnPosition(attackDirection, attackDefinition.MuzzleOffset),
                Direction = attackDirection,
                InheritedVelocity = inheritedVelocity,
                Damage = ResolveDamage(projectileDefinition),
                Speed = ResolveProjectileSpeed(projectileDefinition),
                Lifetime = ResolveProjectileLifetime(projectileDefinition),
                Scale = ResolveProjectileScale(projectileDefinition),
                Knockback = ResolveKnockback(),
                PierceCount = ResolvePierceCount(),
                HomingStrength = ResolveHomingStrength(),
                Instigator = transform,
                InstigatorCollider = ownerCollider,
                DamageTarget = ProjectileDamageTarget.EnemyOnly
            };
        }

        private float ResolveDamage(ProjectileDefinition projectileDefinition)
        {
            if (playerStats != null)
            {
                return playerStats.CurrentDamage;
            }

            return projectileDefinition.Damage;
        }

        private float ResolveFireInterval()
        {
            if (playerStats != null)
            {
                return playerStats.CurrentFireInterval;
            }

            return attackDefinition.FireInterval;
        }

        private float ResolveProjectileSpeed(ProjectileDefinition projectileDefinition)
        {
            if (playerStats != null)
            {
                return playerStats.CurrentProjectileSpeed;
            }

            return projectileDefinition.Speed;
        }

        private float ResolveProjectileLifetime(ProjectileDefinition projectileDefinition)
        {
            if (playerStats != null)
            {
                return playerStats.CurrentProjectileLifetime;
            }

            return projectileDefinition.Lifetime;
        }

        private float ResolveProjectileScale(ProjectileDefinition projectileDefinition)
        {
            if (playerStats != null)
            {
                return playerStats.CurrentProjectileScale;
            }

            return projectileDefinition.Scale;
        }

        private float ResolveKnockback()
        {
            if (playerStats != null)
            {
                return playerStats.CurrentKnockback;
            }

            return 0f;
        }

        private int ResolvePierceCount()
        {
            if (playerStats == null)
            {
                return 0;
            }

            return Mathf.Max(0, Mathf.FloorToInt(playerStats.CurrentProjectilePierce));
        }

        private float ResolveHomingStrength()
        {
            if (playerStats == null)
            {
                return 0f;
            }

            return Mathf.Max(0f, playerStats.CurrentHomingStrength);
        }

        private int ResolveShotCount()
        {
            if (playerStats == null)
            {
                return 1;
            }

            float projectileCount = Mathf.Max(1f, playerStats.CurrentProjectileCount);
            int guaranteedShots = Mathf.Max(1, Mathf.FloorToInt(projectileCount));
            float fractionalChance = projectileCount - guaranteedShots;
            float luckBonus = Mathf.Max(0f, playerStats.CurrentLuck) * 0.05f;

            if (Random.value < Mathf.Clamp01(fractionalChance + luckBonus))
            {
                guaranteedShots += 1;
            }

            return guaranteedShots;
        }

        private Vector2 ResolveShotDirection(Vector2 baseDirection, int shotIndex, int shotCount)
        {
            if (shotCount <= 1)
            {
                return baseDirection;
            }

            float centerIndex = (shotCount - 1) * 0.5f;
            float angleOffset = (shotIndex - centerIndex) * multishotSpreadDegrees;
            return Rotate(baseDirection, angleOffset).normalized;
        }

        private Vector2 ResolveInheritedVelocity()
        {
            if (playerRigidbody == null || inertiaFactor <= 0f)
            {
                return Vector2.zero;
            }

            return playerRigidbody.linearVelocity * inertiaFactor;
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

        private Vector2 QuantizeAimToCardinal(Vector2 rawAim)
        {
            float absX = Mathf.Abs(rawAim.x);
            float absY = Mathf.Abs(rawAim.y);

            if (absX <= 0.0001f && absY <= 0.0001f)
            {
                return Vector2.zero;
            }

            if (Mathf.Approximately(absX, absY))
            {
                if (_lastAttackDirection.x != 0f && absX > 0f)
                {
                    return new Vector2(Mathf.Sign(rawAim.x), 0f);
                }

                return new Vector2(0f, Mathf.Sign(rawAim.y));
            }

            return absX > absY
                ? new Vector2(Mathf.Sign(rawAim.x), 0f)
                : new Vector2(0f, Mathf.Sign(rawAim.y));
        }

        private bool TryResolveProjectileSpawner()
        {
            if (projectileSpawner != null)
            {
                return true;
            }

            projectileSpawner = GetComponent<ProjectileSpawner>();

            if (projectileSpawner != null)
            {
                return true;
            }

            Debug.LogError("PlayerCombat requires a ProjectileSpawner reference.", this);
            return false;
        }

        private void Reset()
        {
            projectileSpawner = GetComponent<ProjectileSpawner>();
            playerStats = GetComponent<PlayerStats>();
            playerVisual = GetComponent<PlayerVisual>();
            playerRigidbody = GetComponent<Rigidbody2D>();
            ownerCollider = GetComponent<Collider2D>();
        }

        private void OnValidate()
        {
            if (projectileSpawner == null)
            {
                projectileSpawner = GetComponent<ProjectileSpawner>();
            }

            if (playerStats == null)
            {
                playerStats = GetComponent<PlayerStats>();
            }

            if (playerVisual == null)
            {
                playerVisual = GetComponent<PlayerVisual>();
            }

            if (playerRigidbody == null)
            {
                playerRigidbody = GetComponent<Rigidbody2D>();
            }

            if (ownerCollider == null)
            {
                ownerCollider = GetComponent<Collider2D>();
            }
        }

        private bool TryResolveInputReader()
        {
            if (inputReaderSource is IPlayerInputReader serializedReader)
            {
                _inputReader = serializedReader;
                return true;
            }

            MonoBehaviour[] sceneBehaviours = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

            for (int i = 0; i < sceneBehaviours.Length; i++)
            {
                if (sceneBehaviours[i] is IPlayerInputReader sceneReader)
                {
                    _inputReader = sceneReader;
                    return true;
                }
            }

            Debug.LogError("PlayerCombat could not find an IPlayerInputReader in the scene.", this);
            return false;
        }
    }
}
