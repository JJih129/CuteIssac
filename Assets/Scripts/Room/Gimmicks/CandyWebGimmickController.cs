using System;
using CuteIssac.Core.Pooling;
using UnityEngine;

namespace CuteIssac.Room.Gimmicks
{
    /// <summary>
    /// Candy forest boss-room web gimmick. It is a breakable blocker that applies movement lock without health damage.
    /// Boss-room restriction is handled by prefab placement; this component only exposes an intent flag for validation.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CandyWebGimmickController : MonoBehaviour
    {
        public enum WebState
        {
            Idle = 0,
            Active = 1,
            Broken = 2
        }

        [Header("References")]
        [SerializeField] private BreakableGimmick breakableGimmick;
        [SerializeField] private MovementLockGimmick movementLockGimmick;
        [SerializeField] private Transform visualRoot;
        [SerializeField] private Collider2D[] collidersToToggle;
        [SerializeField] private GameObject breakEffectPrefab;

        [Header("Placement")]
        [SerializeField] private bool bossRoomOnly = true;

        [Header("Gameplay")]
        [SerializeField] [Min(0.05f)] private float lockDuration = 1f;
        [SerializeField] [Min(1f)] private float maxHealth = 1f;
        [SerializeField] private bool destroyOnBroken = true;

        [Header("Fallback Safety")]
        [SerializeField] private bool autoCollectChildColliders = true;
        [SerializeField] private bool logDebugMessages;

        private Collider2D[] _resolvedCollidersToToggle;
        private Renderer[] _visualRenderers;
        private WebState _state = WebState.Idle;
        private bool _warnedAboutRootVisual;

        public event Action<CandyWebGimmickController> Broken;

        public WebState State => _state;
        public bool BossRoomOnly => bossRoomOnly;
        public float LockDuration => lockDuration;
        public float MaxHealth => maxHealth;
        public bool DestroyOnBroken => destroyOnBroken;

        private void Awake()
        {
            ResolveReferences();
            ResolveColliderCache();
            ResolveVisualRenderers();
            ConfigureGameplayComponents(resetHealth: true);
            SetActiveState(true);
        }

        private void OnEnable()
        {
            ResolveReferences();
            ResolveColliderCache();
            ResolveVisualRenderers();

            if (_state == WebState.Broken)
            {
                // Pooled/broken webs must not revive just because the GameObject was enabled again.
                ConfigureGameplayComponents(resetHealth: false);
                SetBrokenStatePresentation();
                return;
            }

            ConfigureGameplayComponents(resetHealth: true);
            SetActiveState(true);
        }

        private void OnDisable()
        {
            if (breakableGimmick != null)
            {
                breakableGimmick.Broken -= HandleBroken;
            }
        }

        public void Activate()
        {
            ActivateWeb();
        }

        /// <summary>
        /// Pooling reuse or manual reactivation only. This explicitly restores health, colliders, visual, and lock behavior.
        /// </summary>
        public void ActivateWeb()
        {
            ConfigureGameplayComponents(resetHealth: true);
            SetActiveState(true);
        }

        /// <summary>
        /// Pooling reuse or manual reactivation only. Kept as an explicit reset point so OnEnable never revives broken webs.
        /// </summary>
        public void ResetForReuse()
        {
            ActivateWeb();
        }

        public void Deactivate()
        {
            SetGameplayEnabled(false);
            SetVisualVisible(false);
            _state = WebState.Idle;
        }

        private void HandleBroken(BreakableGimmick brokenGimmick)
        {
            if (_state == WebState.Broken)
            {
                return;
            }

            _state = WebState.Broken;
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
                // CandyWeb owns reuse resets explicitly; BreakableGimmick.OnEnable must not revive pooled broken webs.
                breakableGimmick.ConfigureResetHealthOnEnable(false);
                breakableGimmick.ConfigureHealth(maxHealth, resetHealth);
                breakableGimmick.ConfigureBreakBehaviour(destroyOnBroken);
            }

            if (movementLockGimmick != null)
            {
                movementLockGimmick.ConfigureLockDuration(lockDuration);
            }
        }

        private void SetBrokenStatePresentation()
        {
            SetGameplayEnabled(false);
            SetVisualVisible(false);
        }

        private void SetActiveState(bool active)
        {
            _state = active ? WebState.Active : WebState.Idle;
            SetGameplayEnabled(active);
            SetVisualVisible(active);
        }

        private void SetGameplayEnabled(bool enabled)
        {
            if (breakableGimmick != null)
            {
                breakableGimmick.SetActive(enabled);
            }

            if (movementLockGimmick != null)
            {
                movementLockGimmick.SetActive(enabled);
            }

            SetCollidersEnabled(enabled);
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
                    Debug.LogWarning("CandyWeb visualRoot points to the controller root. Renderer fallback is used so the controller is not disabled.", this);
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

            if (movementLockGimmick == null)
            {
                movementLockGimmick = GetComponent<MovementLockGimmick>();
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
                // Fallback is cached; state changes do not repeatedly scan the hierarchy.
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

        private void Reset()
        {
            ResolveReferences();
            ResolveColliderCache();
            ResolveVisualRenderers();
        }

        private void OnValidate()
        {
            ResolveReferences();
            _resolvedCollidersToToggle = null;
            _visualRenderers = null;
            ResolveColliderCache();
            ResolveVisualRenderers();
            lockDuration = Mathf.Max(0.05f, lockDuration);
            maxHealth = Mathf.Max(1f, maxHealth);
        }
    }
}
