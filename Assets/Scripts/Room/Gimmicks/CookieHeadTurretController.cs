using CuteIssac.Combat;
using CuteIssac.Core.Audio;
using CuteIssac.Core.Pooling;
using CuteIssac.Player;
using UnityEngine;

namespace CuteIssac.Room.Gimmicks
{
    /// <summary>
    /// Cookie head turret that fires a blood-tear enemy projectile at the player.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CookieHeadTurretController : StationaryGimmickTurretBase
    {
        [Header("Cookie Head Shot")]
        [SerializeField] private Transform firePoint;
        [SerializeField] private EnemyProjectileLogic bloodTearProjectilePrefab;
        [SerializeField] [Min(0f)] private float projectileSpeed = 4.5f;
        [SerializeField] [Min(0.05f)] private float projectileLifetime = 4f;
        [SerializeField] [Min(0)] private int prewarmCount = 8;
        [SerializeField] private Collider2D ownerCollider;

        private bool _prewarmed;

        protected override void Awake()
        {
            ResolveLocalReferences();
            base.Awake();
        }

        protected override void Reset()
        {
            ResolveLocalReferences();
            base.Reset();
        }

        protected override void OnValidate()
        {
            ResolveLocalReferences();
            base.OnValidate();
            projectileSpeed = Mathf.Max(0f, projectileSpeed);
            projectileLifetime = Mathf.Max(0.05f, projectileLifetime);
            prewarmCount = Mathf.Max(0, prewarmCount);
        }

        protected override void BeginAttack(PlayerHealth target)
        {
            if (target == null || target.IsDead || bloodTearProjectilePrefab == null)
            {
                CompleteAttack();
                return;
            }

            Transform origin = firePoint != null ? firePoint : transform;
            Vector2 direction = ResolveDirectionToTarget(target, origin);
            EnemyProjectileSpawnRequest spawnRequest = new()
            {
                ProjectilePrefab = bloodTearProjectilePrefab,
                Position = origin.position,
                Direction = direction,
                Damage = Damage,
                Speed = projectileSpeed,
                Lifetime = projectileLifetime,
                Instigator = transform,
                InstigatorCollider = ownerCollider
            };

            if (!_prewarmed && prewarmCount > 0)
            {
                PrefabPoolService.Prewarm(bloodTearProjectilePrefab.gameObject, prewarmCount);
                _prewarmed = true;
            }

            EnemyProjectileLogic projectile = PrefabPoolService.Spawn(
                bloodTearProjectilePrefab,
                spawnRequest.Position,
                Quaternion.FromToRotation(Vector3.right, direction));

            projectile?.Initialize(spawnRequest);
            GameAudioEvents.Raise(GameAudioEventType.ProjectileFired, transform.position, true, 0.8f);
            CompleteAttack();
        }

        private void ResolveLocalReferences()
        {
            if (ownerCollider == null)
            {
                ownerCollider = GetComponent<Collider2D>();
            }
        }
    }
}
