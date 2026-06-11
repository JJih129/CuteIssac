using System;
using System.Collections;
using System.Collections.Generic;
using CuteIssac.Core.Pooling;
using UnityEngine;

namespace CuteIssac.Room.Gimmicks
{
    /// <summary>
    /// Candy village worm gimmick. It telegraphs from underground, emerges, then becomes a fixed breakable contact hazard.
    /// Damage and destruction are delegated to BreakableGimmick and ContactDamageGimmick.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CandyWormGimmickController : MonoBehaviour
    {
        public enum WormState
        {
            Idle = 0,
            Warning = 1,
            Emerging = 2,
            Active = 3,
            Broken = 4
        }

        [Header("References")]
        [SerializeField] private BreakableGimmick breakableGimmick;
        [SerializeField] private ContactDamageGimmick contactDamageGimmick;
        [SerializeField] private Transform visualRoot;
        [SerializeField] private Collider2D[] collidersToToggle;
        [SerializeField] private bool autoCollectChildColliders = true;
        [SerializeField] private bool logDebugMessages;

        [Header("Warning")]
        [SerializeField] private GameObject warningEffectPrefab;
        [SerializeField] private Transform warningEffectParent;

        [Header("Timing")]
        [SerializeField] private bool startOnEnable;
        [SerializeField] [Min(0.05f)] private float warningDuration = 2f;
        [SerializeField] [Min(0.05f)] private float emergeDuration = 0.25f;

        [Header("Gameplay")]
        [SerializeField] [Min(1f)] private float maxHealth = 1f;
        [SerializeField] [Min(0f)] private float contactDamage = 1f;
        [SerializeField] [Min(0.05f)] private float contactDamageCooldown = 0.7f;
        [SerializeField] [Min(0f)] private float knockbackForce = 4f;

        [Header("Presentation")]
        [SerializeField] [Range(0f, 0.4f)] private float hiddenScaleY = 0.05f;
        [SerializeField] [Min(0f)] private float emergeYOffset = 0.32f;

        private Coroutine _sequenceRoutine;
        private GameObject _warningEffectInstance;
        private WormState _state = WormState.Idle;
        private Collider2D[] _resolvedCollidersToToggle;
        private Renderer[] _visualRenderers;
        private Vector3 _visualBaseScale = Vector3.one;
        private Vector3 _visualBaseLocalPosition;
        private bool _visualStateCached;
        private bool _warnedAboutRootVisual;

        public event Action<CandyWormGimmickController> Completed;

        public WormState State => _state;
        public bool IsRunning => _sequenceRoutine != null;
        public float WarningDuration => warningDuration;
        public float EmergeDuration => emergeDuration;
        public float MaxHealth => maxHealth;
        public float ContactDamage => contactDamage;
        public float ContactDamageCooldown => contactDamageCooldown;
        public float KnockbackForce => knockbackForce;

        private void Awake()
        {
            ResolveReferences();
            ResolveColliderCache();
            ResolveVisualRenderers();
            CacheVisualState();
            ConfigureGameplayComponents();
            PrefabPoolService.EnsurePrewarmed(warningEffectPrefab, 1);
            SetGameplayEnabled(false);
            SetVisualHidden();
        }

        private void OnEnable()
        {
            ResolveReferences();
            ResolveColliderCache();
            ResolveVisualRenderers();

            if (startOnEnable)
            {
                StartWarning(transform.position);
            }
        }

        private void OnDisable()
        {
            StopSequence();
            ClearWarningEffect();
            _state = WormState.Idle;
            SetGameplayEnabled(false);
            SetVisualHidden();
        }

        public void StartWarning(Vector2 worldPosition)
        {
            StopSequence();
            ResolveReferences();
            ResolveColliderCache();
            ResolveVisualRenderers();
            CacheVisualState();
            ConfigureGameplayComponents();
            transform.position = new Vector3(worldPosition.x, worldPosition.y, transform.position.z);
            _sequenceRoutine = StartCoroutine(RunWormSequence());
        }

        [ContextMenu("Start Warning At Current Position")]
        public void StartWarningAtCurrentPosition()
        {
            StartWarning(transform.position);
        }

        private IEnumerator RunWormSequence()
        {
            _state = WormState.Warning;
            SetGameplayEnabled(false);
            SetVisualHidden();
            SpawnWarningEffect();

            float warningTimer = 0f;
            while (warningTimer < warningDuration)
            {
                warningTimer += Time.deltaTime;
                yield return null;
            }

            ClearWarningEffect();
            _state = WormState.Emerging;
            SetVisualVisible();

            float emergeTimer = 0f;
            float safeEmergeDuration = Mathf.Max(0.05f, emergeDuration);
            Vector3 hiddenScale = ResolveHiddenScale();
            Vector3 hiddenPosition = _visualBaseLocalPosition - new Vector3(0f, emergeYOffset, 0f);

            while (emergeTimer < safeEmergeDuration)
            {
                emergeTimer += Time.deltaTime;
                float normalized = Mathf.Clamp01(emergeTimer / safeEmergeDuration);
                float eased = 1f - ((1f - normalized) * (1f - normalized));
                ApplyVisualPose(
                    Vector3.LerpUnclamped(hiddenScale, _visualBaseScale, eased),
                    Vector3.LerpUnclamped(hiddenPosition, _visualBaseLocalPosition, eased));
                yield return null;
            }

            ApplyVisualPose(_visualBaseScale, _visualBaseLocalPosition);
            SetGameplayEnabled(true);
            _state = WormState.Active;
            _sequenceRoutine = null;
        }

        private void HandleBroken(BreakableGimmick brokenGimmick)
        {
            if (_state == WormState.Broken)
            {
                return;
            }

            _state = WormState.Broken;
            StopSequence();
            ClearWarningEffect();
            SetGameplayEnabled(false);
            SetVisualVisible(false);
            Completed?.Invoke(this);

            if (TryGetComponent(out PooledObject _))
            {
                PrefabPoolService.Return(gameObject);
            }
        }

        private void ConfigureGameplayComponents()
        {
            if (breakableGimmick != null)
            {
                breakableGimmick.Broken -= HandleBroken;
                breakableGimmick.Broken += HandleBroken;
                breakableGimmick.ConfigureHealth(maxHealth, resetCurrentHealth: true);
            }

            if (contactDamageGimmick != null)
            {
                contactDamageGimmick.ConfigureContactDamage(contactDamage, contactDamageCooldown, knockbackForce);
            }
        }

        private void SetGameplayEnabled(bool enabled)
        {
            if (breakableGimmick != null)
            {
                breakableGimmick.SetActive(enabled);
            }

            if (contactDamageGimmick != null)
            {
                contactDamageGimmick.SetActive(enabled);
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

        private void SpawnWarningEffect()
        {
            ClearWarningEffect();

            if (warningEffectPrefab == null)
            {
                return;
            }

            Transform parent = warningEffectParent != null ? warningEffectParent : transform;
            _warningEffectInstance = PrefabPoolService.Spawn(warningEffectPrefab, transform.position, Quaternion.identity, parent);
        }

        private void ClearWarningEffect()
        {
            if (_warningEffectInstance == null)
            {
                return;
            }

            PrefabPoolService.Return(_warningEffectInstance);
            _warningEffectInstance = null;
        }

        private void SetVisualHidden()
        {
            SetVisualVisible(false);
            ApplyVisualPose(ResolveHiddenScale(), _visualBaseLocalPosition - new Vector3(0f, emergeYOffset, 0f));
        }

        private void SetVisualVisible(bool visible = true)
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
                    Debug.LogWarning("CandyWorm visualRoot points to the controller root. Renderer fallback is used so the coroutine is not stopped.", this);
                    _warnedAboutRootVisual = true;
                }

                SetVisualRenderersEnabled(visible);
                return;
            }

            visualRoot.gameObject.SetActive(visible);
        }

        private void ApplyVisualPose(Vector3 scale, Vector3 localPosition)
        {
            if (visualRoot == null)
            {
                return;
            }

            visualRoot.localScale = scale;
            visualRoot.localPosition = localPosition;
        }

        private Vector3 ResolveHiddenScale()
        {
            return new Vector3(_visualBaseScale.x, _visualBaseScale.y * hiddenScaleY, _visualBaseScale.z);
        }

        private void StopSequence()
        {
            if (_sequenceRoutine == null)
            {
                return;
            }

            StopCoroutine(_sequenceRoutine);
            _sequenceRoutine = null;
        }

        private void CacheVisualState()
        {
            if (_visualStateCached || visualRoot == null)
            {
                return;
            }

            _visualBaseScale = visualRoot.localScale;
            _visualBaseLocalPosition = visualRoot.localPosition;
            _visualStateCached = true;
        }

        private void ResolveReferences()
        {
            if (breakableGimmick == null)
            {
                breakableGimmick = GetComponent<BreakableGimmick>();
            }

            if (contactDamageGimmick == null)
            {
                contactDamageGimmick = GetComponent<ContactDamageGimmick>();
            }

            // Keep null valid: hiding the root GameObject would stop this controller's coroutine.
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
                // Fallback is cached; gameplay state changes do not repeatedly scan the hierarchy.
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
            warningDuration = Mathf.Max(0.05f, warningDuration);
            emergeDuration = Mathf.Max(0.05f, emergeDuration);
            maxHealth = Mathf.Max(1f, maxHealth);
            contactDamage = Mathf.Max(0f, contactDamage);
            contactDamageCooldown = Mathf.Max(0.05f, contactDamageCooldown);
            knockbackForce = Mathf.Max(0f, knockbackForce);
            hiddenScaleY = Mathf.Clamp(hiddenScaleY, 0f, 0.4f);
            emergeYOffset = Mathf.Max(0f, emergeYOffset);
        }
    }
}
