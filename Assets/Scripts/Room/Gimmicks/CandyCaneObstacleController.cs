using System;
using UnityEngine;

namespace CuteIssac.Room.Gimmicks
{
    /// <summary>
    /// Candy village cane blocker. It is a solid, breakable obstacle with no contact damage or movement lock.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CandyCaneObstacleController : MonoBehaviour
    {
        public enum ObstacleState
        {
            Idle = 0,
            Active = 1,
            Broken = 2
        }

        [Header("References")]
        [SerializeField] private BreakableGimmick breakableGimmick;
        [SerializeField] private Transform visualRoot;
        [SerializeField] private Collider2D[] collidersToToggle;
        [SerializeField] private GameObject breakEffectPrefab;

        [Header("Gameplay")]
        [SerializeField] [Min(1f)] private float maxHealth = 3f;
        [SerializeField] private bool destroyOnBroken = true;

        [Header("Blocking")]
        [SerializeField] private bool enforceSolidColliders = true;

        [Header("Fallback Safety")]
        [SerializeField] private bool autoCollectChildColliders = true;
        [SerializeField] private bool logDebugMessages;

        private Collider2D[] _resolvedCollidersToToggle;
        private Renderer[] _visualRenderers;
        private ObstacleState _state = ObstacleState.Idle;
        private bool _warnedAboutRootVisual;

        public event Action<CandyCaneObstacleController> Broken;

        public ObstacleState State => _state;
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

            if (_state == ObstacleState.Broken)
            {
                // Pooled/broken canes must not revive just because the GameObject was enabled again.
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
            ActivateObstacle();
        }

        /// <summary>
        /// Pooling reuse or manual reactivation only. This explicitly restores health, colliders, and visual state.
        /// </summary>
        public void ActivateObstacle()
        {
            ConfigureGameplayComponents(resetHealth: true);
            SetActiveState(true);
        }

        /// <summary>
        /// Pooling reuse or manual reactivation only. Kept explicit so OnEnable never revives broken obstacles.
        /// </summary>
        public void ResetForReuse()
        {
            ActivateObstacle();
        }

        public void Deactivate()
        {
            SetGameplayEnabled(false);
            SetVisualVisible(false);
            _state = ObstacleState.Idle;
        }

        private void HandleBroken(BreakableGimmick brokenGimmick)
        {
            if (_state == ObstacleState.Broken)
            {
                return;
            }

            _state = ObstacleState.Broken;
            SetGameplayEnabled(false);
            SetVisualVisible(false);
            SpawnBreakEffect();
            Broken?.Invoke(this);
        }

        private void ConfigureGameplayComponents(bool resetHealth)
        {
            if (breakableGimmick == null)
            {
                return;
            }

            breakableGimmick.Broken -= HandleBroken;
            breakableGimmick.Broken += HandleBroken;
            // CandyCane owns reuse resets explicitly; BreakableGimmick.OnEnable must not revive pooled broken obstacles.
            breakableGimmick.ConfigureResetHealthOnEnable(false);
            breakableGimmick.ConfigureHealth(maxHealth, resetHealth);
            breakableGimmick.ConfigureBreakBehaviour(destroyOnBroken);
        }

        private void SetBrokenStatePresentation()
        {
            SetGameplayEnabled(false);
            SetVisualVisible(false);
        }

        private void SetActiveState(bool active)
        {
            _state = active ? ObstacleState.Active : ObstacleState.Idle;
            SetGameplayEnabled(active);
            SetVisualVisible(active);
        }

        private void SetGameplayEnabled(bool enabled)
        {
            if (breakableGimmick != null)
            {
                breakableGimmick.SetActive(enabled);
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
                Collider2D blocker = colliders[i];

                if (blocker == null)
                {
                    continue;
                }

                if (enforceSolidColliders)
                {
                    blocker.isTrigger = false;
                }

                blocker.enabled = enabled;
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
                    Debug.LogWarning("CandyCane visualRoot points to the controller root. Renderer fallback is used so the controller is not disabled.", this);
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
                Instantiate(breakEffectPrefab, transform.position, Quaternion.identity);
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
                // Fallback is cached; final prefabs should still explicitly assign only blocker colliders.
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
            maxHealth = Mathf.Max(1f, maxHealth);
        }
    }
}
