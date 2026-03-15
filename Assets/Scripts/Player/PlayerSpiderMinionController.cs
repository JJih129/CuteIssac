using CuteIssac.Common.Combat;
using CuteIssac.Core.Pooling;
using CuteIssac.Enemy;
using UnityEngine;

namespace CuteIssac.Player
{
    /// <summary>
    /// Lightweight summoned ally that chases the nearest enemy and deals contact damage.
    /// It stays self-contained so active items can spawn it without touching enemy AI code.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerSpiderMinionController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerSpiderMinionVisual minionVisual;

        [Header("Lifetime")]
        [SerializeField] [Min(0.1f)] private float lifetimeSeconds = 8f;

        [Header("Movement")]
        [SerializeField] [Min(0.1f)] private float moveSpeed = 5.25f;
        [SerializeField] [Min(0.1f)] private float retargetInterval = 0.35f;

        [Header("Attack")]
        [SerializeField] [Min(0.05f)] private float attackRange = 0.55f;
        [SerializeField] [Min(0.05f)] private float attackCooldown = 0.45f;
        [SerializeField] [Min(0f)] private float attackDamage = 2f;
        [SerializeField] [Min(0f)] private float knockbackForce = 1.8f;

        private Transform _owner;
        private EnemyHealth _target;
        private float _remainingLifetime;
        private float _retargetRemaining;
        private float _attackCooldownRemaining;

        private void Awake()
        {
            if (minionVisual == null)
            {
                minionVisual = GetComponentInChildren<PlayerSpiderMinionVisual>();
            }
        }

        private void OnEnable()
        {
            _remainingLifetime = lifetimeSeconds;
            _retargetRemaining = 0f;
            _attackCooldownRemaining = 0f;
            _target = null;
            minionVisual?.HandleSpawned();
        }

        private void Update()
        {
            _remainingLifetime -= Time.deltaTime;

            if (_remainingLifetime <= 0f)
            {
                PrefabPoolService.Return(gameObject);
                return;
            }

            if (_attackCooldownRemaining > 0f)
            {
                _attackCooldownRemaining = Mathf.Max(0f, _attackCooldownRemaining - Time.deltaTime);
            }

            _retargetRemaining -= Time.deltaTime;

            if (_target == null || _target.IsDead || _retargetRemaining <= 0f)
            {
                _target = ResolveNearestEnemy();
                _retargetRemaining = retargetInterval;
            }

            Vector3 desiredPosition = _target != null
                ? _target.transform.position
                : (_owner != null ? _owner.position : transform.position);
            Vector2 moveDirection = ((Vector2)(desiredPosition - transform.position)).normalized;
            transform.position = Vector3.MoveTowards(transform.position, desiredPosition, moveSpeed * Time.deltaTime);
            minionVisual?.SetMoveDirection(moveDirection);

            if (_target == null || _attackCooldownRemaining > 0f)
            {
                return;
            }

            float distance = Vector2.Distance(transform.position, _target.transform.position);

            if (distance > attackRange)
            {
                return;
            }

            Collider2D targetCollider = _target.GetComponent<Collider2D>();

            if (targetCollider == null || !DamageableResolver.TryResolve(targetCollider, out IDamageable damageable))
            {
                return;
            }

            Vector2 hitDirection = ((Vector2)_target.transform.position - (Vector2)transform.position).normalized;
            damageable.ApplyDamage(new DamageInfo(attackDamage, hitDirection, transform, knockbackForce));
            _attackCooldownRemaining = attackCooldown;
            minionVisual?.HandleAttack();
        }

        private void OnDisable()
        {
            _target = null;
            _owner = null;
            minionVisual?.ResetPresentation();
        }

        public void Initialize(Transform owner)
        {
            _owner = owner;
        }

        private EnemyHealth ResolveNearestEnemy()
        {
            EnemyHealth[] enemies = FindObjectsByType<EnemyHealth>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            EnemyHealth best = null;
            float bestDistance = float.MaxValue;

            for (int index = 0; index < enemies.Length; index++)
            {
                EnemyHealth enemy = enemies[index];

                if (enemy == null || enemy.IsDead)
                {
                    continue;
                }

                float distance = Vector2.SqrMagnitude(enemy.transform.position - transform.position);

                if (distance >= bestDistance)
                {
                    continue;
                }

                best = enemy;
                bestDistance = distance;
            }

            return best;
        }
    }
}
