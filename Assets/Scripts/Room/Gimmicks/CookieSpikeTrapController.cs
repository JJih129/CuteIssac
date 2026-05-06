using System;
using UnityEngine;

namespace CuteIssac.Room.Gimmicks
{
    /// <summary>
    /// Cookie house spike floor trap. It damages and knocks back the player through a trigger, but cannot be destroyed.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CookieSpikeTrapController : MonoBehaviour
    {
        public enum TrapState
        {
            Idle = 0,
            Active = 1,
            Disabled = 2
        }

        [Header("References")]
        [SerializeField] private ContactDamageGimmick contactDamageGimmick;
        [SerializeField] private Transform visualRoot;
        [SerializeField] private Collider2D[] collidersToToggle;

        [Header("Gameplay")]
        [SerializeField] [Min(0f)] private float damage = 1f;
        [SerializeField] [Min(0.05f)] private float damageCooldown = 0.8f;
        [SerializeField] [Min(0f)] private float knockbackForce = 5f;

        [Header("Trigger")]
        [SerializeField] private bool enforceTriggerColliders = true;

        [Header("Fallback Safety")]
        [SerializeField] private bool autoCollectChildColliders = true;
        [SerializeField] private bool logDebugMessages;

        private Collider2D[] _resolvedCollidersToToggle;
        private Renderer[] _visualRenderers;
        private TrapState _state = TrapState.Idle;
        private bool _warnedAboutRootVisual;

        public event Action<CookieSpikeTrapController> Disabled;

        public TrapState State => _state;
        public float Damage => damage;
        public float DamageCooldown => damageCooldown;
        public float KnockbackForce => knockbackForce;
        public bool Destroyable => false;

        private void Awake()
        {
            ResolveReferences();
            ResolveColliderCache();
            ResolveVisualRenderers();
            ConfigureGameplayComponents();
            SetActiveState(true);
        }

        private void OnEnable()
        {
            ResolveReferences();
            ResolveColliderCache();
            ResolveVisualRenderers();

            if (_state == TrapState.Disabled)
            {
                // Disabled pooled traps should remain inert until ActivateTrap or ResetForReuse is called.
                ConfigureGameplayComponents();
                SetDisabledStatePresentation();
                return;
            }

            ConfigureGameplayComponents();
            SetActiveState(true);
        }

        public void Activate()
        {
            ActivateTrap();
        }

        /// <summary>
        /// Pooling reuse or manual reactivation only. This explicitly restores trigger damage, colliders, and visual state.
        /// </summary>
        public void ActivateTrap()
        {
            ConfigureGameplayComponents();
            SetActiveState(true);
        }

        /// <summary>
        /// Pooling reuse or manual reactivation only. Kept explicit so OnEnable never revives disabled traps.
        /// </summary>
        public void ResetForReuse()
        {
            ActivateTrap();
        }

        public void Deactivate()
        {
            if (_state == TrapState.Disabled)
            {
                return;
            }

            _state = TrapState.Disabled;
            SetDisabledStatePresentation();
            Disabled?.Invoke(this);
        }

        private void ConfigureGameplayComponents()
        {
            if (contactDamageGimmick != null)
            {
                contactDamageGimmick.ConfigureContactDamage(damage, damageCooldown, knockbackForce);
            }
        }

        private void SetActiveState(bool active)
        {
            _state = active ? TrapState.Active : TrapState.Idle;

            if (contactDamageGimmick != null)
            {
                contactDamageGimmick.SetActive(active);
            }

            SetCollidersEnabled(active);
            SetVisualVisible(active);
        }

        private void SetDisabledStatePresentation()
        {
            if (contactDamageGimmick != null)
            {
                contactDamageGimmick.SetActive(false);
            }

            SetCollidersEnabled(false);
            SetVisualVisible(false);
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
                Collider2D trigger = colliders[i];

                if (trigger == null)
                {
                    continue;
                }

                if (enforceTriggerColliders)
                {
                    trigger.isTrigger = true;
                }

                trigger.enabled = enabled;
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
                    Debug.LogWarning("CookieSpikeTrap visualRoot points to the controller root. Renderer fallback is used so the controller is not disabled.", this);
                    _warnedAboutRootVisual = true;
                }

                SetVisualRenderersEnabled(visible);
                return;
            }

            visualRoot.gameObject.SetActive(visible);
        }

        private void ResolveReferences()
        {
            if (contactDamageGimmick == null)
            {
                contactDamageGimmick = GetComponent<ContactDamageGimmick>();
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
                // Fallback is cached; final prefabs should explicitly assign only spike trigger colliders.
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
            damage = Mathf.Max(0f, damage);
            damageCooldown = Mathf.Max(0.05f, damageCooldown);
            knockbackForce = Mathf.Max(0f, knockbackForce);
        }
    }
}
