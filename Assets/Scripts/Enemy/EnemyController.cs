using System.Collections.Generic;
using CuteIssac.Common.Combat;
using CuteIssac.Core.Scene;
using CuteIssac.Player;
using UnityEngine;

namespace CuteIssac.Enemy
{
    /// <summary>
    /// Minimal chase AI for the prototype.
    /// It periodically finds the player, moves toward them, and applies contact damage through the shared damage contract.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(EnemyMovement))]
    [RequireComponent(typeof(EnemyHealth))]
    public sealed class EnemyController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private string enemyId;
        [SerializeField] private EnemyMovement enemyMovement;
        [SerializeField] private EnemyHealth enemyHealth;
        [SerializeField] private EnemyVisual enemyVisual;
        [SerializeField] private EnemyBrain enemyBrain;
        [SerializeField] private Transform targetOverride;

        [Header("Targeting")]
        [SerializeField] private string playerTag = "Player";
        [SerializeField] [Min(0.1f)] private float retargetInterval = 0.5f;

        [Header("Combat")]
        [SerializeField] [Min(0f)] private float contactDamage = 1f;

        private Transform _target;
        private float _retargetTimer;
        private float _spawnAggroDelayRemaining;
        private float _freezeRemaining;
        private bool _combatDormant;
        private float _formationContactDamageMultiplier = 1f;
        private float _runtimePressureContactDamageMultiplier = 1f;
        private float _runtimePressureSpeedMultiplier = 1f;
        private float _runtimeWebSpeedMultiplier = 1f;
        private GameplaySceneContext _sceneContext;
        private readonly List<RuntimeMultiplierSource> _runtimeWebSpeedSources = new();

        public EnemyMovement EnemyMovement => enemyMovement;
        public EnemyHealth EnemyHealth => enemyHealth;
        public EnemyVisual EnemyVisual => enemyVisual;
        public EnemyBrain ActiveBrain => enemyBrain;
        public string EnemyId => string.IsNullOrWhiteSpace(enemyId) ? SanitizeEnemyId(gameObject.name) : enemyId;
        public float ContactDamage => contactDamage;
        public Transform CurrentTarget => _target;
        public Vector2 Position => transform.position;
        public Vector2 TargetPosition => _target != null ? (Vector2)_target.position : Position;
        public bool HasTarget => _target != null;
        public bool IsFrozen => _freezeRemaining > 0f;
        public bool IsCombatDormant => _combatDormant;

        private void Awake()
        {
            if (!TryResolveDependencies())
            {
                enabled = false;
                return;
            }

            enemyHealth.Died += HandleDeath;
            enemyBrain.Initialize(this);
            ResolveTarget();
        }

        private void OnEnable()
        {
            if (enemyMovement == null || enemyHealth == null || enemyBrain == null)
            {
                return;
            }

            ResetForSpawn();
        }

        private void OnDestroy()
        {
            if (enemyHealth != null)
            {
                enemyHealth.Died -= HandleDeath;
            }
        }

        private void FixedUpdate()
        {
            if (enemyHealth.IsDead)
            {
                return;
            }

            if (_combatDormant)
            {
                enemyMovement?.SuspendMotion();
                enemyVisual?.SetMoveDirection(Vector2.zero);
                return;
            }

            if (_freezeRemaining > 0f)
            {
                _freezeRemaining = Mathf.Max(0f, _freezeRemaining - Time.fixedDeltaTime);
                StopMovement();
                return;
            }

            if (_spawnAggroDelayRemaining > 0f)
            {
                _spawnAggroDelayRemaining = Mathf.Max(0f, _spawnAggroDelayRemaining - Time.fixedDeltaTime);
                StopMovement();
                return;
            }

            _retargetTimer -= Time.fixedDeltaTime;

            if (_target == null || _retargetTimer <= 0f)
            {
                ResolveTarget();
            }

            if (_target == null)
            {
                StopMovement();
                return;
            }
            
            enemyBrain.TickBrain(Time.fixedDeltaTime);
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            TryApplyContactDamage(collision.collider);
        }

        private void OnCollisionStay2D(Collision2D collision)
        {
            TryApplyContactDamage(collision.collider);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            TryApplyContactDamage(other);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            TryApplyContactDamage(other);
        }

        private void TryApplyContactDamage(Collider2D hitCollider)
        {
            float effectiveContactDamage = contactDamage * _formationContactDamageMultiplier * _runtimePressureContactDamageMultiplier;

            if (enemyHealth.IsDead || effectiveContactDamage <= 0f || _target == null || _combatDormant || _spawnAggroDelayRemaining > 0f)
            {
                return;
            }

            if (hitCollider == null)
            {
                return;
            }

            // Restrict contact damage to the tracked target so enemies do not hurt unrelated damageable objects.
            if (hitCollider.transform != _target && !hitCollider.transform.IsChildOf(_target))
            {
                return;
            }

            if (!DamageableResolver.TryResolve(hitCollider, out IDamageable damageable))
            {
                return;
            }

            Vector2 hitDirection = ((Vector2)hitCollider.transform.position - (Vector2)transform.position).normalized;
            damageable.ApplyDamage(new DamageInfo(effectiveContactDamage, hitDirection, transform));
        }

        private bool TryResolveDependencies()
        {
            if (enemyMovement == null)
            {
                enemyMovement = GetComponent<EnemyMovement>();
            }

            if (enemyHealth == null)
            {
                enemyHealth = GetComponent<EnemyHealth>();
            }

            if (enemyVisual == null)
            {
                enemyVisual = GetComponent<EnemyVisual>();
            }

            if (enemyBrain == null)
            {
                enemyBrain = GetComponent<EnemyBrain>();
            }

            if (enemyMovement == null || enemyHealth == null || enemyBrain == null)
            {
                Debug.LogError("EnemyController requires EnemyMovement, EnemyHealth, and EnemyBrain components.", this);
                return false;
            }

            return true;
        }

        private void ResolveTarget()
        {
            _retargetTimer = retargetInterval;

            if (targetOverride != null)
            {
                _target = targetOverride;
                return;
            }

            // Search by explicit scene context first so enemy prefabs do not depend on tag names during normal gameplay.
            PlayerController playerController = ResolvePlayerControllerFromSceneContext();

            if (playerController == null)
            {
                // Registry remains a cheap runtime fallback for cases where enemies spawn before the context is active.
                playerController = PlayerRegistry.ActiveController;
            }

            if (playerController != null)
            {
                _target = playerController.transform;
                return;
            }

            if (!string.IsNullOrWhiteSpace(playerTag))
            {
                try
                {
                    GameObject taggedPlayer = GameObject.FindWithTag(playerTag);

                    if (taggedPlayer != null)
                    {
                        _target = taggedPlayer.transform;
                        return;
                    }
                }
                catch (UnityException)
                {
                    // Ignore missing tag setup. Direct reference or controller lookup is enough for the prototype.
                }
            }

            _target = null;
        }

        private PlayerController ResolvePlayerControllerFromSceneContext()
        {
            if (_sceneContext == null)
            {
                _sceneContext = GameplaySceneContext.Active;
            }
            else if (_sceneContext.PlayerController == null && GameplaySceneContext.Active != _sceneContext)
            {
                _sceneContext = GameplaySceneContext.Active;
            }

            if (_sceneContext == null)
            {
                return null;
            }

            return _sceneContext.PlayerController;
        }

        private void HandleDeath()
        {
            StopMovement();
        }

        public void ResetForSpawn()
        {
            enemyHealth.ResetForSpawn();
            enemyMovement?.ResumeMotion();
            enemyMovement?.Stop();
            enemyBrain.ResetBrainState();
            enemyVisual?.ResetPresentation();
            _spawnAggroDelayRemaining = 0f;
            _freezeRemaining = 0f;
            _combatDormant = false;
            _formationContactDamageMultiplier = 1f;
            _runtimePressureContactDamageMultiplier = 1f;
            _runtimePressureSpeedMultiplier = 1f;
            _runtimeWebSpeedMultiplier = 1f;
            _runtimeWebSpeedSources.Clear();
            enemyMovement?.SetFormationSpeedMultiplier(1f);
            ApplyRuntimePressureSpeedMultiplier();
            _retargetTimer = 0f;
            ResolveTarget();
        }

        public void ApplySpawnAggroDelay(float delaySeconds)
        {
            _spawnAggroDelayRemaining = Mathf.Max(_spawnAggroDelayRemaining, Mathf.Max(0f, delaySeconds));
        }

        public void ConfigureRuntimeData(string runtimeEnemyId, float runtimeContactDamage)
        {
            if (!string.IsNullOrWhiteSpace(runtimeEnemyId))
            {
                enemyId = runtimeEnemyId;
            }

            contactDamage = Mathf.Max(0f, runtimeContactDamage);
        }

        public void SetContactDamage(float runtimeContactDamage)
        {
            contactDamage = Mathf.Max(0f, runtimeContactDamage);
        }

        public void SetFormationContactDamageMultiplier(float multiplier)
        {
            _formationContactDamageMultiplier = Mathf.Max(0f, multiplier);
        }

        public void SetRuntimePressureContactDamageMultiplier(float multiplier)
        {
            _runtimePressureContactDamageMultiplier = Mathf.Max(0f, multiplier);
        }

        public void SetDesiredMoveDirection(Vector2 moveDirection)
        {
            enemyMovement.SetMoveDirection(moveDirection);
            enemyVisual?.SetMoveDirection(moveDirection);
        }

        public void StopMovement()
        {
            enemyMovement.Stop();
            enemyVisual?.SetMoveDirection(Vector2.zero);
        }

        public void SetMoveSpeedMultiplier(float multiplier)
        {
            enemyMovement.SetSpeedMultiplier(multiplier);
        }

        public void SetFormationSpeedMultiplier(float multiplier)
        {
            enemyMovement.SetFormationSpeedMultiplier(multiplier);
        }

        public void SetRuntimePressureSpeedMultiplier(float multiplier)
        {
            _runtimePressureSpeedMultiplier = Mathf.Max(0f, multiplier);
            ApplyRuntimePressureSpeedMultiplier();
        }

        public void SetRuntimeWebSpeedMultiplier(int sourceKey, float multiplier)
        {
            float resolvedMultiplier = Mathf.Max(0.05f, multiplier);

            for (int i = 0; i < _runtimeWebSpeedSources.Count; i++)
            {
                RuntimeMultiplierSource source = _runtimeWebSpeedSources[i];

                if (source.SourceKey != sourceKey)
                {
                    continue;
                }

                if (Mathf.Approximately(source.Multiplier, resolvedMultiplier))
                {
                    return;
                }

                source.Multiplier = resolvedMultiplier;
                _runtimeWebSpeedSources[i] = source;
                RebuildRuntimeWebSpeedMultiplier();
                ApplyRuntimePressureSpeedMultiplier();
                return;
            }

            _runtimeWebSpeedSources.Add(new RuntimeMultiplierSource(sourceKey, resolvedMultiplier));
            RebuildRuntimeWebSpeedMultiplier();
            ApplyRuntimePressureSpeedMultiplier();
        }

        public void ClearRuntimeWebSpeedMultiplier(int sourceKey)
        {
            for (int i = 0; i < _runtimeWebSpeedSources.Count; i++)
            {
                if (_runtimeWebSpeedSources[i].SourceKey != sourceKey)
                {
                    continue;
                }

                int lastIndex = _runtimeWebSpeedSources.Count - 1;
                if (i != lastIndex)
                {
                    _runtimeWebSpeedSources[i] = _runtimeWebSpeedSources[lastIndex];
                }

                _runtimeWebSpeedSources.RemoveAt(lastIndex);
                RebuildRuntimeWebSpeedMultiplier();
                ApplyRuntimePressureSpeedMultiplier();
                return;
            }
        }

        public void ClearRuntimeWebSpeedMultipliers()
        {
            if (_runtimeWebSpeedSources.Count == 0)
            {
                return;
            }

            _runtimeWebSpeedSources.Clear();
            _runtimeWebSpeedMultiplier = 1f;
            ApplyRuntimePressureSpeedMultiplier();
        }

        public void ScaleSpawnAggroDelay(float multiplier)
        {
            _spawnAggroDelayRemaining = Mathf.Max(0f, _spawnAggroDelayRemaining * Mathf.Max(0f, multiplier));
        }

        public void ApplyFreeze(float durationSeconds)
        {
            _freezeRemaining = Mathf.Max(_freezeRemaining, Mathf.Max(0f, durationSeconds));
            StopMovement();
        }

        public void SetCombatDormant(bool dormant)
        {
            if (_combatDormant == dormant)
            {
                return;
            }

            _combatDormant = dormant;

            if (_combatDormant)
            {
                enemyMovement?.SuspendMotion();
                enemyVisual?.SetMoveDirection(Vector2.zero);
                return;
            }

            _spawnAggroDelayRemaining = 0f;
            _freezeRemaining = 0f;
            enemyMovement?.ResumeMotion();
            ResolveTarget();
        }

        private void Reset()
        {
            enemyMovement = GetComponent<EnemyMovement>();
            enemyHealth = GetComponent<EnemyHealth>();
            enemyVisual = GetComponent<EnemyVisual>();
            enemyBrain = GetComponent<EnemyBrain>();
        }

        private void OnValidate()
        {
            if (enemyMovement == null)
            {
                enemyMovement = GetComponent<EnemyMovement>();
            }

            if (enemyHealth == null)
            {
                enemyHealth = GetComponent<EnemyHealth>();
            }

            if (enemyVisual == null)
            {
                enemyVisual = GetComponent<EnemyVisual>();
            }

            if (enemyBrain == null)
            {
                enemyBrain = GetComponent<EnemyBrain>();
            }
        }

        private void ApplyRuntimePressureSpeedMultiplier()
        {
            if (enemyMovement == null)
            {
                return;
            }

            enemyMovement.SetRuntimePressureSpeedMultiplier(_runtimePressureSpeedMultiplier * _runtimeWebSpeedMultiplier);
        }

        private void RebuildRuntimeWebSpeedMultiplier()
        {
            float multiplier = 1f;

            for (int i = 0; i < _runtimeWebSpeedSources.Count; i++)
            {
                multiplier *= _runtimeWebSpeedSources[i].Multiplier;
            }

            _runtimeWebSpeedMultiplier = multiplier;
        }

        private struct RuntimeMultiplierSource
        {
            public RuntimeMultiplierSource(int sourceKey, float multiplier)
            {
                SourceKey = sourceKey;
                Multiplier = multiplier;
            }

            public int SourceKey;
            public float Multiplier;
        }

        private static string SanitizeEnemyId(string rawName)
        {
            if (string.IsNullOrWhiteSpace(rawName))
            {
                return "enemy";
            }

            return rawName.Replace("(Clone)", string.Empty).Trim();
        }
    }
}
