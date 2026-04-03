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
        [SerializeField] private PlayerRoutePlanCarryController routePlanCarryController;
        [SerializeField] private PlayerWeaponLoadout weaponLoadout;

        [Header("Attack Data")]
        [SerializeField] private PlayerAttackDefinition attackDefinition;
        [SerializeField] [Range(0f, 30f)] private float multishotSpreadDegrees = 9f;
        [SerializeField] [Range(0f, 1f)] private float inertiaFactor = 0.22f;

        private IPlayerInputReader _inputReader;
        private float _shotCooldown;
        private Vector2 _lastAttackDirection = Vector2.right;
        private float _lastAimInputTimestamp = float.NegativeInfinity;

        public PlayerAttackDefinition StartingAttackDefinition => attackDefinition;
        public PlayerAttackDefinition AttackDefinition => weaponLoadout != null && weaponLoadout.CurrentAttackDefinition != null
            ? weaponLoadout.CurrentAttackDefinition
            : attackDefinition;
        public Vector2 LastAttackDirection => _lastAttackDirection;

        private void Awake()
        {
            if (GetComponent<PlayerCombatMomentumController>() == null)
            {
                gameObject.AddComponent<PlayerCombatMomentumController>();
            }

            if (GetComponent<PlayerCombatMomentumVisual>() == null)
            {
                gameObject.AddComponent<PlayerCombatMomentumVisual>();
            }

            if (GetComponent<PlayerCombatMomentumExecutionController>() == null)
            {
                gameObject.AddComponent<PlayerCombatMomentumExecutionController>();
            }

            if (GetComponent<PlayerRoutePlanCarryController>() == null)
            {
                gameObject.AddComponent<PlayerRoutePlanCarryController>();
            }

            if (GetComponent<CombatOpeningTargetHintController>() == null)
            {
                gameObject.AddComponent<CombatOpeningTargetHintController>();
            }

            if (GetComponent<PlayerRoutePlanCarryBurstVisual>() == null)
            {
                gameObject.AddComponent<PlayerRoutePlanCarryBurstVisual>();
            }

            if (GetComponent<PlayerLoadoutDeltaPresentation>() == null)
            {
                gameObject.AddComponent<PlayerLoadoutDeltaPresentation>();
            }

            if (GetComponent<PlayerLoadoutDeltaController>() == null)
            {
                gameObject.AddComponent<PlayerLoadoutDeltaController>();
            }

            if (GetComponent<PlayerWeaponLoadout>() == null)
            {
                gameObject.AddComponent<PlayerWeaponLoadout>();
            }

            if (GetComponent<EnemyAmmoDropSpawner>() == null)
            {
                gameObject.AddComponent<EnemyAmmoDropSpawner>();
            }

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

            if (routePlanCarryController == null)
            {
                routePlanCarryController = GetComponent<PlayerRoutePlanCarryController>();
            }

            if (weaponLoadout == null)
            {
                weaponLoadout = GetComponent<PlayerWeaponLoadout>();
            }

            if (playerVisual != null && playerVisual.MuzzleAnchor != null)
            {
                projectileSpawner.SetSpawnOrigin(playerVisual.MuzzleAnchor);
            }
        }

        private void Update()
        {
            if (_inputReader == null && !TryResolveInputReader())
            {
                return;
            }

            if (_shotCooldown > 0f)
            {
                _shotCooldown -= Time.deltaTime;
            }

            PlayerGameplayInputState inputState = _inputReader.ReadState();
            weaponLoadout?.ProcessInput(inputState, Time.deltaTime);
            PlayerAttackDefinition resolvedAttackDefinition = AttackDefinition;

            if (resolvedAttackDefinition == null || !resolvedAttackDefinition.IsValid)
            {
                return;
            }

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
            _lastAimInputTimestamp = Time.unscaledTime;
            playerVisual?.SetAimDirection(attackDirection);
            TryFire(attackDirection, resolvedAttackDefinition);
        }

        public bool TryGetRecentAimDirection(float freshnessWindow, out Vector2 aimDirection)
        {
            aimDirection = _lastAttackDirection;

            if (_lastAttackDirection.sqrMagnitude <= 0.0001f)
            {
                return false;
            }

            float clampedWindow = Mathf.Max(0.01f, freshnessWindow);
            return Time.unscaledTime - _lastAimInputTimestamp <= clampedWindow;
        }

        private void TryFire(Vector2 attackDirection, PlayerAttackDefinition resolvedAttackDefinition)
        {
            if (_shotCooldown > 0f)
            {
                return;
            }

            if (weaponLoadout != null && !weaponLoadout.TryConsumeShot())
            {
                return;
            }

            int baseShotCount = ResolveShotCount();
            int shotCount = routePlanCarryController != null
                ? routePlanCarryController.ResolveRecentOpeningCadenceHitRoleShotCount(
                    routePlanCarryController.ResolveOpeningCadenceShotCount(baseShotCount))
                : baseShotCount;
            Vector2 fireDirection = attackDirection.normalized;
            Vector2 inheritedVelocity = ResolveInheritedVelocity();

            for (int shotIndex = 0; shotIndex < shotCount; shotIndex++)
            {
                Vector2 shotDirection = ResolveShotDirection(fireDirection, shotIndex, shotCount);
                shotDirection = ResolveRouteAssistedShotDirection(shotDirection, resolvedAttackDefinition);
                shotDirection = routePlanCarryController != null
                    ? routePlanCarryController.ResolveOpeningCadenceShotDirection(shotDirection, shotIndex, shotCount)
                    : shotDirection;
                if (routePlanCarryController != null && projectileSpawner != null && resolvedAttackDefinition != null)
                {
                    Vector2 shotOrigin = projectileSpawner.GetSpawnPosition(shotDirection, resolvedAttackDefinition.MuzzleOffset);
                    shotDirection = routePlanCarryController.ResolveRecentOpeningCadenceHitRoleShotDirection(shotOrigin, shotDirection, shotIndex, shotCount);
                }
                ProjectileSpawnRequest spawnRequest = BuildSpawnRequest(shotDirection, inheritedVelocity, resolvedAttackDefinition);
                routePlanCarryController?.ApplyOpeningCadenceToSpawnRequest(ref spawnRequest, shotIndex, shotCount);
                routePlanCarryController?.ApplyRecentOpeningCadenceHitRoleToSpawnRequest(ref spawnRequest, shotIndex, shotCount);
                projectileSpawner.Spawn(in spawnRequest);
            }

            playerVisual?.HandleFired(attackDirection);
            GameplayRuntimeEvents.RaiseProjectileFired(new ProjectileFiredSignal(
                transform,
                projectileSpawner.GetSpawnPosition(fireDirection, resolvedAttackDefinition.MuzzleOffset),
                fireDirection,
                shotCount));
            GameAudioEvents.Raise(GameAudioEventType.ProjectileFired, transform.position);
            float resolvedFireInterval = ResolveFireInterval();
            if (routePlanCarryController != null)
            {
                resolvedFireInterval = routePlanCarryController.ResolveOpeningCadenceFireInterval(resolvedFireInterval);
                resolvedFireInterval = routePlanCarryController.ResolveRecentOpeningCadenceHitRoleFireInterval(resolvedFireInterval);
            }

            _shotCooldown = resolvedFireInterval;
        }

        private ProjectileSpawnRequest BuildSpawnRequest(Vector2 attackDirection, Vector2 inheritedVelocity, PlayerAttackDefinition resolvedAttackDefinition)
        {
            ProjectileDefinition projectileDefinition = resolvedAttackDefinition.ProjectileDefinition;

            return new ProjectileSpawnRequest
            {
                ProjectilePrefab = projectileDefinition.ProjectilePrefab,
                Position = projectileSpawner.GetSpawnPosition(attackDirection, resolvedAttackDefinition.MuzzleOffset),
                Direction = attackDirection,
                InheritedVelocity = inheritedVelocity,
                Damage = ResolveDamage(projectileDefinition),
                Speed = ResolveProjectileSpeed(projectileDefinition),
                Lifetime = ResolveProjectileLifetime(projectileDefinition),
                Scale = ResolveProjectileScale(projectileDefinition),
                Knockback = ResolveKnockback(),
                PierceCount = ResolvePierceCount(),
                HomingStrength = ResolveHomingStrength(),
                Traits = ResolveProjectileTraits(),
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

            PlayerAttackDefinition resolvedAttackDefinition = AttackDefinition;
            return resolvedAttackDefinition != null
                ? resolvedAttackDefinition.FireInterval
                : 0.3f;
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
            float baseKnockback = 0f;

            if (playerStats != null)
            {
                baseKnockback = playerStats.CurrentKnockback;
            }

            return baseKnockback * (weaponLoadout != null ? weaponLoadout.CurrentKnockbackMultiplier : 1f);
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

        private ProjectileTraitState ResolveProjectileTraits()
        {
            return playerStats != null
                ? playerStats.CurrentProjectileTraits
                : ProjectileTraitState.Default;
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

            return weaponLoadout != null
                ? weaponLoadout.ResolveShotCount(guaranteedShots)
                : guaranteedShots;
        }

        private Vector2 ResolveShotDirection(Vector2 baseDirection, int shotIndex, int shotCount)
        {
            if (shotCount <= 1)
            {
                return baseDirection;
            }

            float centerIndex = (shotCount - 1) * 0.5f;
            float spreadDegrees = weaponLoadout != null
                ? weaponLoadout.ResolveSpreadDegrees(multishotSpreadDegrees)
                : multishotSpreadDegrees;
            float angleOffset = (shotIndex - centerIndex) * spreadDegrees;
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

        private Vector2 ResolveRouteAssistedShotDirection(Vector2 baseDirection, PlayerAttackDefinition resolvedAttackDefinition)
        {
            if (routePlanCarryController == null
                || projectileSpawner == null
                || resolvedAttackDefinition == null)
            {
                return baseDirection;
            }

            Vector2 normalizedDirection = baseDirection.sqrMagnitude > 0.0001f
                ? baseDirection.normalized
                : Vector2.right;
            Vector2 shotOrigin = projectileSpawner.GetSpawnPosition(normalizedDirection, resolvedAttackDefinition.MuzzleOffset);
            return routePlanCarryController.TryResolveBreakthroughShotDirection(shotOrigin, normalizedDirection, out Vector2 adjustedDirection)
                ? adjustedDirection
                : normalizedDirection;
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
            weaponLoadout = GetComponent<PlayerWeaponLoadout>();
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

            if (routePlanCarryController == null)
            {
                routePlanCarryController = GetComponent<PlayerRoutePlanCarryController>();
            }

            if (weaponLoadout == null)
            {
                weaponLoadout = GetComponent<PlayerWeaponLoadout>();
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
