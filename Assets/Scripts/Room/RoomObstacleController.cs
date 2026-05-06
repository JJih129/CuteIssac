using System.Collections.Generic;
using CuteIssac.Combat;
using CuteIssac.Common.Combat;
using CuteIssac.Enemy;
using CuteIssac.Player;
using UnityEngine;

namespace CuteIssac.Room
{
    /// <summary>
    /// Gameplay-facing room obstacle.
    /// Handles projectile interaction and optional contact hazard logic, while visuals stay in RoomObstacleVisual.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RoomObstacleController : MonoBehaviour
    {
        [Header("Identity")]
        [SerializeField] private RoomObstacleType obstacleType = RoomObstacleType.Rock;

        [Header("Collision")]
        [SerializeField] private Collider2D obstacleCollider;
        [SerializeField] private ProjectileObstacleResponse projectileResponse = ProjectileObstacleResponse.Solid;
        [SerializeField] private ProjectileImpactType projectileImpactType = ProjectileImpactType.Solid;

        [Header("Hazard")]
        [SerializeField] private bool damagePlayerOnContact;
        [SerializeField] private bool damageEnemiesOnContact;
        [SerializeField] [Min(0f)] private float contactDamage = 1f;
        [SerializeField] [Min(0f)] private float contactKnockback = 2f;
        [SerializeField] [Min(0.05f)] private float contactTickInterval = 0.5f;

        [Header("Traversal Slow")]
        [SerializeField] private bool slowPlayerOnContact;
        [SerializeField] private bool slowEnemiesOnContact;
        [SerializeField, Range(0.05f, 1f)] private float contactMoveSpeedMultiplier = 0.72f;
        [SerializeField, Min(0f)] private float contactSlowLingerDuration = 0.5f;

        [Header("Presentation")]
        [SerializeField] private RoomObstacleVisual obstacleVisual;

        private readonly Dictionary<int, float> _nextDamageTimes = new();
        private readonly List<SlowTargetState> _slowTargets = new();
        private readonly List<SlowColliderBinding> _slowColliderBindings = new();
        private int _slowSourceKey;

        public RoomObstacleType ObstacleType => obstacleType;
        public Collider2D ObstacleCollider => obstacleCollider;
        public bool DamagesPlayerOnContact => damagePlayerOnContact && contactDamage > 0f;
        public float ContactDamage => contactDamage;
        public float TraversalRiskRadius => obstacleCollider != null
            ? Mathf.Max(obstacleCollider.bounds.extents.x, obstacleCollider.bounds.extents.y)
            : 0.6f;

        private void Awake()
        {
            ResolveReferences();
            _slowSourceKey = GetInstanceID();
        }

        private void OnDisable()
        {
            _nextDamageTimes.Clear();
            ClearAllSlowContacts();
        }

        private void Update()
        {
            UpdateSlowContacts();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            TryApplyContactDamage(other);
            TryApplyContactSlow(other);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            TryApplyContactDamage(other);
            TryApplyContactSlow(other);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            TryReleaseContactSlow(other);
        }

        public bool TryHandleProjectile(bool isEnemyProjectile, Vector3 impactPosition, out ProjectileImpactType impactType)
        {
            impactType = projectileImpactType;

            switch (projectileResponse)
            {
                case ProjectileObstacleResponse.Ignore:
                    return false;
                case ProjectileObstacleResponse.Solid:
                case ProjectileObstacleResponse.Consume:
                    obstacleVisual?.HandleProjectileImpact();
                    return true;
                default:
                    return false;
            }
        }

        private void TryApplyContactDamage(Collider2D other)
        {
            if (contactDamage <= 0f || (!damagePlayerOnContact && !damageEnemiesOnContact) || other == null)
            {
                return;
            }

            if (!DamageableResolver.TryResolve(other, out IDamageable damageable))
            {
                return;
            }

            bool isPlayerTarget = other.GetComponentInParent<Player.PlayerHealth>() != null;
            bool isEnemyTarget = other.GetComponentInParent<Enemy.EnemyHealth>() != null;

            if ((isPlayerTarget && !damagePlayerOnContact) || (isEnemyTarget && !damageEnemiesOnContact))
            {
                return;
            }

            int targetId = (damageable as Object)?.GetInstanceID() ?? other.GetInstanceID();

            if (_nextDamageTimes.TryGetValue(targetId, out float nextDamageTime) && Time.time < nextDamageTime)
            {
                return;
            }

            Vector2 hitDirection = ((Vector2)other.bounds.center - (Vector2)transform.position).normalized;

            if (hitDirection.sqrMagnitude <= 0.0001f)
            {
                hitDirection = Vector2.up;
            }

            damageable.ApplyDamage(new DamageInfo(contactDamage, hitDirection, transform, contactKnockback));
            _nextDamageTimes[targetId] = Time.time + contactTickInterval;
            obstacleVisual?.HandleHazardTriggered();
        }

        private void TryApplyContactSlow(Collider2D other)
        {
            if (other == null || contactMoveSpeedMultiplier >= 0.999f || (!slowPlayerOnContact && !slowEnemiesOnContact))
            {
                return;
            }

            if (_slowSourceKey == 0)
            {
                _slowSourceKey = GetInstanceID();
            }

            int colliderId = other.GetInstanceID();
            int bindingIndex = FindColliderBindingIndex(colliderId);

            if (bindingIndex >= 0)
            {
                int existingTargetId = _slowColliderBindings[bindingIndex].TargetId;
                if (TryGetSlowTargetStateIndex(existingTargetId, out int targetIndex, out SlowTargetState existingState))
                {
                    existingState.ClearAt = float.PositiveInfinity;
                    _slowTargets[targetIndex] = existingState;
                }

                return;
            }

            if (!TryResolveSlowTarget(other, out int targetId, out PlayerStats playerStats, out EnemyController enemyController))
            {
                return;
            }

            _slowColliderBindings.Add(new SlowColliderBinding(colliderId, targetId));

            if (!TryGetSlowTargetStateIndex(targetId, out int index, out SlowTargetState state))
            {
                state = new SlowTargetState(targetId, playerStats, enemyController);
                _slowTargets.Add(state);
                index = _slowTargets.Count - 1;
            }

            state.ColliderCount++;
            state.ClearAt = float.PositiveInfinity;

            if (!state.IsApplied)
            {
                ApplySlowToTarget(state);
                state.IsApplied = true;
            }

            _slowTargets[index] = state;
            obstacleVisual?.HandleHazardTriggered();
        }

        private void TryReleaseContactSlow(Collider2D other)
        {
            if (other == null || _slowTargets.Count == 0)
            {
                return;
            }

            int colliderId = other.GetInstanceID();
            int bindingIndex = FindColliderBindingIndex(colliderId);

            if (bindingIndex < 0)
            {
                return;
            }

            int targetId = _slowColliderBindings[bindingIndex].TargetId;
            int lastBindingIndex = _slowColliderBindings.Count - 1;

            if (bindingIndex != lastBindingIndex)
            {
                _slowColliderBindings[bindingIndex] = _slowColliderBindings[lastBindingIndex];
            }

            _slowColliderBindings.RemoveAt(lastBindingIndex);

            if (!TryGetSlowTargetStateIndex(targetId, out int targetIndex, out SlowTargetState state))
            {
                return;
            }

            state.ColliderCount = Mathf.Max(0, state.ColliderCount - 1);

            if (state.ColliderCount <= 0)
            {
                state.ClearAt = Time.time + contactSlowLingerDuration;
            }

            _slowTargets[targetIndex] = state;
        }

        private void UpdateSlowContacts()
        {
            if (_slowTargets.Count == 0)
            {
                return;
            }

            float currentTime = Time.time;

            for (int i = _slowTargets.Count - 1; i >= 0; i--)
            {
                SlowTargetState state = _slowTargets[i];
                bool playerAlive = state.PlayerStats != null && state.PlayerStats.isActiveAndEnabled;
                bool enemyAlive = state.EnemyController != null && state.EnemyController.isActiveAndEnabled;

                if (!playerAlive && !enemyAlive)
                {
                    ClearSlowFromTarget(state);
                    RemoveColliderBindingsForTarget(state.TargetId);
                    RemoveSlowTargetAt(i);
                    continue;
                }

                if (!state.IsApplied && state.ColliderCount > 0)
                {
                    ApplySlowToTarget(state);
                    state.IsApplied = true;
                    _slowTargets[i] = state;
                }

                if (state.ColliderCount > 0 || currentTime < state.ClearAt)
                {
                    continue;
                }

                ClearSlowFromTarget(state);
                RemoveColliderBindingsForTarget(state.TargetId);
                RemoveSlowTargetAt(i);
            }
        }

        private void ClearAllSlowContacts()
        {
            if (_slowTargets.Count > 0)
            {
                for (int i = _slowTargets.Count - 1; i >= 0; i--)
                {
                    ClearSlowFromTarget(_slowTargets[i]);
                }
            }

            _slowTargets.Clear();
            _slowColliderBindings.Clear();
        }

        private void ApplySlowToTarget(in SlowTargetState state)
        {
            if (state.PlayerStats != null)
            {
                state.PlayerStats.SetRuntimeObstacleMoveSpeedMultiplier(_slowSourceKey, contactMoveSpeedMultiplier);
            }
            else if (state.EnemyController != null)
            {
                state.EnemyController.SetRuntimeWebSpeedMultiplier(_slowSourceKey, contactMoveSpeedMultiplier);
            }
        }

        private void ClearSlowFromTarget(in SlowTargetState state)
        {
            if (state.PlayerStats != null)
            {
                state.PlayerStats.ClearRuntimeObstacleMoveSpeedMultiplier(_slowSourceKey);
            }
            else if (state.EnemyController != null)
            {
                state.EnemyController.ClearRuntimeWebSpeedMultiplier(_slowSourceKey);
            }
        }

        private void RemoveColliderBindingsForTarget(int targetId)
        {
            for (int i = _slowColliderBindings.Count - 1; i >= 0; i--)
            {
                if (_slowColliderBindings[i].TargetId != targetId)
                {
                    continue;
                }

                int lastIndex = _slowColliderBindings.Count - 1;
                if (i != lastIndex)
                {
                    _slowColliderBindings[i] = _slowColliderBindings[lastIndex];
                }

                _slowColliderBindings.RemoveAt(lastIndex);
            }
        }

        private void RemoveSlowTargetAt(int index)
        {
            int lastIndex = _slowTargets.Count - 1;
            if (index != lastIndex)
            {
                _slowTargets[index] = _slowTargets[lastIndex];
            }

            _slowTargets.RemoveAt(lastIndex);
        }

        private bool TryResolveSlowTarget(Collider2D other, out int targetId, out PlayerStats playerStats, out EnemyController enemyController)
        {
            playerStats = null;
            enemyController = null;
            targetId = 0;

            if (slowPlayerOnContact)
            {
                playerStats = other.GetComponentInParent<PlayerStats>();
                if (playerStats != null)
                {
                    targetId = playerStats.GetInstanceID();
                    return true;
                }
            }

            if (slowEnemiesOnContact)
            {
                enemyController = other.GetComponentInParent<EnemyController>();
                if (enemyController != null)
                {
                    targetId = enemyController.GetInstanceID();
                    return true;
                }
            }

            return false;
        }

        private bool TryGetSlowTargetStateIndex(int targetId, out int index, out SlowTargetState state)
        {
            for (int i = 0; i < _slowTargets.Count; i++)
            {
                if (_slowTargets[i].TargetId != targetId)
                {
                    continue;
                }

                index = i;
                state = _slowTargets[i];
                return true;
            }

            index = -1;
            state = default;
            return false;
        }

        private int FindColliderBindingIndex(int colliderId)
        {
            for (int i = 0; i < _slowColliderBindings.Count; i++)
            {
                if (_slowColliderBindings[i].ColliderId == colliderId)
                {
                    return i;
                }
            }

            return -1;
        }

        private void ResolveReferences()
        {
            if (obstacleCollider == null)
            {
                obstacleCollider = GetComponent<Collider2D>();
            }

            if (obstacleVisual == null)
            {
                obstacleVisual = GetComponent<RoomObstacleVisual>();
            }
        }

        private void Reset()
        {
            ResolveReferences();
        }

        private void OnValidate()
        {
            ResolveReferences();
        }

        private readonly struct SlowColliderBinding
        {
            public SlowColliderBinding(int colliderId, int targetId)
            {
                ColliderId = colliderId;
                TargetId = targetId;
            }

            public int ColliderId { get; }
            public int TargetId { get; }
        }

        private struct SlowTargetState
        {
            public SlowTargetState(int targetId, PlayerStats playerStats, EnemyController enemyController)
            {
                TargetId = targetId;
                PlayerStats = playerStats;
                EnemyController = enemyController;
                ColliderCount = 0;
                ClearAt = float.PositiveInfinity;
                IsApplied = false;
            }

            public int TargetId { get; }
            public PlayerStats PlayerStats { get; }
            public EnemyController EnemyController { get; }
            public int ColliderCount { get; set; }
            public float ClearAt { get; set; }
            public bool IsApplied { get; set; }
        }
    }
}
