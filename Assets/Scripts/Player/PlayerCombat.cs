using CuteIssac.Combat;
using CuteIssac.Common.Input;
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

        [Header("Attack Data")]
        [SerializeField] private PlayerAttackDefinition attackDefinition;

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

            ProjectileSpawnRequest spawnRequest = BuildSpawnRequest(attackDirection);
            projectileSpawner.Spawn(in spawnRequest);
            playerVisual?.HandleFired(attackDirection);
            _shotCooldown = ResolveFireInterval();
        }

        private ProjectileSpawnRequest BuildSpawnRequest(Vector2 attackDirection)
        {
            ProjectileDefinition projectileDefinition = attackDefinition.ProjectileDefinition;

            return new ProjectileSpawnRequest
            {
                ProjectilePrefab = projectileDefinition.ProjectilePrefab,
                Position = projectileSpawner.GetSpawnPosition(attackDirection, attackDefinition.MuzzleOffset),
                Direction = attackDirection,
                Damage = ResolveDamage(projectileDefinition),
                Speed = projectileDefinition.Speed,
                Lifetime = projectileDefinition.Lifetime,
                Instigator = transform,
                InstigatorCollider = ownerCollider
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
