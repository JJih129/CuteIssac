using System;
using CuteIssac.Player;
using CuteIssac.Core.Pooling;
using UnityEngine;

namespace CuteIssac.Room.Gimmicks
{
    /// <summary>
    /// Shared base for fixed candy gimmick turrets. It owns player detection, breakable lifecycle, and pooling-safe state.
    /// Concrete turrets only implement the attack payload.
    /// </summary>
    [DisallowMultipleComponent]
    public abstract class StationaryGimmickTurretBase : MonoBehaviour
    {
        public enum TurretState
        {
            Idle = 0,
            Active = 1,
            Attacking = 2,
            Broken = 3
        }

        [Header("References")]
        [SerializeField] private BreakableGimmick breakableGimmick;
        [SerializeField] private Transform visualRoot;
        [SerializeField] private Collider2D[] collidersToToggle;
        [SerializeField] private GameObject breakEffectPrefab;

        [Header("Gameplay")]
        [SerializeField] [Min(1f)] private float maxHealth = 3f;
        [SerializeField] [Min(0.1f)] private float detectionRange = 8f;
        [SerializeField] [Min(0.05f)] private float detectionInterval = 0.2f;
        [SerializeField] [Min(0.05f)] private float attackCooldown = 2f;
        [SerializeField] [Min(0f)] private float damage = 1f;
        [SerializeField] private bool destroyOnBroken = true;
        [SerializeField] private bool rotateToTarget;

        [Header("Detection")]
        [SerializeField] private LayerMask targetLayerMask = Physics2D.AllLayers;
        [SerializeField] [Min(4)] private int initialTargetBufferSize = 8;

        [Header("Fallback Safety")]
        [SerializeField] private bool autoCollectChildColliders = true;
        [SerializeField] private bool logDebugMessages;

        private Collider2D[] _resolvedCollidersToToggle;
        private Renderer[] _visualRenderers;
        private Collider2D[] _targetBuffer;
        private TurretState _state = TurretState.Idle;
        private PlayerHealth _currentTarget;
        private float _nextDetectionTime;
        private float _nextAttackTime;
        private bool _warnedAboutRootVisual;

        public event Action<StationaryGimmickTurretBase> Broken;

        public TurretState State => _state;
        public float MaxHealth => maxHealth;
        public float DetectionRange => detectionRange;
        public float AttackCooldown => attackCooldown;
        public float Damage => damage;
        public bool DestroyOnBroken => destroyOnBroken;
        protected PlayerHealth CurrentTarget => _currentTarget;

        protected virtual void Awake()
        {
            ResolveReferences();
            ResolveColliderCache();
            ResolveVisualRenderers();
            EnsureTargetBuffer();
            ConfigureGameplayComponents(resetHealth: true);
            SetActiveState(true);
        }

        protected virtual void OnEnable()
        {
            ResolveReferences();
            ResolveColliderCache();
            ResolveVisualRenderers();
            EnsureTargetBuffer();

            if (_state == TurretState.Broken)
            {
                // Pooled/broken turrets must not revive just because the GameObject was enabled again.
                ConfigureGameplayComponents(resetHealth: false);
                SetBrokenStatePresentation();
                return;
            }

            ConfigureGameplayComponents(resetHealth: true);
            SetActiveState(true);
        }

        protected virtual void OnDisable()
        {
            if (breakableGimmick != null)
            {
                breakableGimmick.Broken -= HandleBroken;
            }

            _currentTarget = null;
            OnTurretDeactivated();
        }

        private void Update()
        {
            if (_state != TurretState.Active)
            {
                return;
            }

            float currentTime = Time.time;

            if (currentTime >= _nextDetectionTime)
            {
                _nextDetectionTime = currentTime + detectionInterval;
                _currentTarget = FindTarget();
            }

            if (_currentTarget == null || _currentTarget.IsDead || currentTime < _nextAttackTime)
            {
                return;
            }

            Vector2 toTarget = (Vector2)_currentTarget.transform.position - (Vector2)transform.position;
            if (toTarget.sqrMagnitude > detectionRange * detectionRange)
            {
                _currentTarget = null;
                return;
            }

            if (rotateToTarget && toTarget.sqrMagnitude > 0.0001f)
            {
                transform.rotation = Quaternion.FromToRotation(Vector3.right, toTarget.normalized);
            }

            _state = TurretState.Attacking;
            _nextAttackTime = currentTime + attackCooldown;
            BeginAttack(_currentTarget);
        }

        public void Activate()
        {
            ActivateTurret();
        }

        /// <summary>
        /// Pooling reuse or manual reactivation only. This explicitly restores health, colliders, visual, and attack behavior.
        /// </summary>
        public void ActivateTurret()
        {
            ConfigureGameplayComponents(resetHealth: true);
            SetActiveState(true);
        }

        /// <summary>
        /// Pooling reuse or manual reactivation only. Kept explicit so OnEnable never revives broken turrets.
        /// </summary>
        public void ResetForReuse()
        {
            ActivateTurret();
        }

        public void Deactivate()
        {
            SetActiveState(false);
        }

        protected abstract void BeginAttack(PlayerHealth target);

        protected void CompleteAttack()
        {
            if (_state == TurretState.Attacking)
            {
                _state = TurretState.Active;
            }
        }

        protected Vector2 ResolveDirectionToTarget(PlayerHealth target, Transform origin)
        {
            Vector2 start = origin != null ? (Vector2)origin.position : (Vector2)transform.position;
            Vector2 targetPosition = target != null ? (Vector2)target.transform.position : start + Vector2.right;
            Vector2 direction = targetPosition - start;
            return direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;
        }

        protected virtual void OnTurretDeactivated()
        {
        }

        private void HandleBroken(BreakableGimmick brokenGimmick)
        {
            if (_state == TurretState.Broken)
            {
                return;
            }

            _state = TurretState.Broken;
            _currentTarget = null;
            OnTurretDeactivated();
            SetGameplayEnabled(false);
            SetVisualVisible(false);
            SpawnBreakEffect();
            Broken?.Invoke(this);
        }

        private void ConfigureGameplayComponents(bool resetHealth)
        {
            if (breakableGimmick != null)
            {
                breakableGimmick.Broken -= HandleBroken;
                breakableGimmick.Broken += HandleBroken;
                // Turret controllers own reuse resets explicitly; BreakableGimmick.OnEnable must not revive pooled broken turrets.
                breakableGimmick.ConfigureResetHealthOnEnable(false);
                breakableGimmick.ConfigureHealth(maxHealth, resetHealth);
                breakableGimmick.ConfigureBreakBehaviour(destroyOnBroken);
            }
        }

        private void SetActiveState(bool active)
        {
            _state = active ? TurretState.Active : TurretState.Idle;
            _currentTarget = null;
            _nextDetectionTime = 0f;
            _nextAttackTime = 0f;

            if (!active)
            {
                OnTurretDeactivated();
            }

            SetGameplayEnabled(active);
            SetVisualVisible(active);
        }

        private void SetBrokenStatePresentation()
        {
            OnTurretDeactivated();
            SetGameplayEnabled(false);
            SetVisualVisible(false);
        }

        private void SetGameplayEnabled(bool enabled)
        {
            if (breakableGimmick != null)
            {
                breakableGimmick.SetActive(enabled);
            }

            SetCollidersEnabled(enabled);
        }

        private PlayerHealth FindTarget()
        {
            EnsureTargetBuffer();
            ContactFilter2D targetFilter = new();
            targetFilter.SetLayerMask(targetLayerMask);
            targetFilter.useTriggers = true;
            int hitCount = Physics2D.OverlapCircle(transform.position, detectionRange, targetFilter, _targetBuffer);

            while (hitCount >= _targetBuffer.Length)
            {
                _targetBuffer = new Collider2D[_targetBuffer.Length * 2];
                hitCount = Physics2D.OverlapCircle(transform.position, detectionRange, targetFilter, _targetBuffer);
            }

            PlayerHealth bestTarget = null;
            float bestDistanceSq = detectionRange * detectionRange;

            for (int i = 0; i < hitCount; i++)
            {
                Collider2D hit = _targetBuffer[i];
                _targetBuffer[i] = null;

                if (hit == null)
                {
                    continue;
                }

                PlayerHealth playerHealth = hit.GetComponentInParent<PlayerHealth>();

                if (playerHealth == null || playerHealth.IsDead)
                {
                    continue;
                }

                float distanceSq = ((Vector2)playerHealth.transform.position - (Vector2)transform.position).sqrMagnitude;
                if (distanceSq > bestDistanceSq)
                {
                    continue;
                }

                bestDistanceSq = distanceSq;
                bestTarget = playerHealth;
            }

            return bestTarget;
        }

        private void SetCollidersEnabled(bool enabled)
        {
            Collider2D[] colliders = ResolveColliderCache();

            if (colliders == null || colliders.Length == 0)
            {
                return;
            }

            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i] != null)
                {
                    colliders[i].enabled = enabled;
                }
            }
        }

        private void SetVisualVisible(bool visible)
        {
            if (visualRoot == null)
            {
                SetVisualRenderersEnabled(visible);
                return;
            }

            if (visualRoot == transform)
            {
                if (!_warnedAboutRootVisual && logDebugMessages)
                {
                    Debug.LogWarning("Stationary turret visualRoot points to the controller root. Renderer fallback is used so the controller is not disabled.", this);
                    _warnedAboutRootVisual = true;
                }

                SetVisualRenderersEnabled(visible);
                return;
            }

            visualRoot.gameObject.SetActive(visible);
        }

        private void SpawnBreakEffect()
        {
            if (breakEffectPrefab != null)
            {
                PooledEffectSpawner.Spawn(breakEffectPrefab, transform.position, Quaternion.identity);
            }
        }

        private void ResolveReferences()
        {
            if (breakableGimmick == null)
            {
                breakableGimmick = GetComponent<BreakableGimmick>();
            }
        }

        private Collider2D[] ResolveColliderCache()
        {
            if (collidersToToggle != null && collidersToToggle.Length > 0)
            {
                _resolvedCollidersToToggle = collidersToToggle;
                return _resolvedCollidersToToggle;
            }

            if (!autoCollectChildColliders)
            {
                _resolvedCollidersToToggle = null;
                return null;
            }

            if (_resolvedCollidersToToggle == null || _resolvedCollidersToToggle.Length == 0)
            {
                // Fallback is cached; final prefabs should explicitly assign only turret body colliders.
                _resolvedCollidersToToggle = GetComponentsInChildren<Collider2D>(true);
            }

            return _resolvedCollidersToToggle;
        }

        private void ResolveVisualRenderers()
        {
            if (_visualRenderers != null && _visualRenderers.Length > 0)
            {
                return;
            }

            Transform root = visualRoot != null && visualRoot != transform ? visualRoot : transform;
            _visualRenderers = root.GetComponentsInChildren<Renderer>(true);
        }

        private void SetVisualRenderersEnabled(bool enabled)
        {
            ResolveVisualRenderers();

            if (_visualRenderers == null)
            {
                return;
            }

            for (int i = 0; i < _visualRenderers.Length; i++)
            {
                if (_visualRenderers[i] != null)
                {
                    _visualRenderers[i].enabled = enabled;
                }
            }
        }

        private void EnsureTargetBuffer()
        {
            int capacity = Mathf.Max(4, initialTargetBufferSize);

            if (_targetBuffer == null || _targetBuffer.Length < capacity)
            {
                _targetBuffer = new Collider2D[capacity];
            }
        }

        protected virtual void Reset()
        {
            ResolveReferences();
            ResolveColliderCache();
            ResolveVisualRenderers();
            EnsureTargetBuffer();
        }

        protected virtual void OnValidate()
        {
            ResolveReferences();
            _resolvedCollidersToToggle = null;
            _visualRenderers = null;
            _targetBuffer = null;
            ResolveColliderCache();
            ResolveVisualRenderers();
            EnsureTargetBuffer();
            maxHealth = Mathf.Max(1f, maxHealth);
            detectionRange = Mathf.Max(0.1f, detectionRange);
            detectionInterval = Mathf.Max(0.05f, detectionInterval);
            attackCooldown = Mathf.Max(0.05f, attackCooldown);
            damage = Mathf.Max(0f, damage);
            initialTargetBufferSize = Mathf.Max(4, initialTargetBufferSize);
        }
    }
}
