using System;
using CuteIssac.Core.Pooling;
using CuteIssac.Data.Enemy;
using UnityEngine;

namespace CuteIssac.Enemy
{
    /// <summary>
    /// Shared horror-phase controller for regular enemies. It can auto-enter by health ratio or be driven manually by attack patterns.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EnemyHorrorPhaseController : MonoBehaviour
    {
        public enum HorrorPhaseState
        {
            Normal = 0,
            Horror = 1,
            Disabled = 2
        }

        [Header("References")]
        [SerializeField] private EnemyHealth enemyHealth;
        [SerializeField] private EnemyVisual enemyVisual;
        [SerializeField] private SpriteRenderer bodySpriteRenderer;
        [SerializeField] private Transform normalVisualRoot;
        [SerializeField] private Transform horrorVisualRoot;
        [SerializeField] private GameObject transitionEffectPrefab;

        [Header("Visual Sets")]
        [SerializeField] private EnemyVisualSet normalVisualSet;
        [SerializeField] private EnemyVisualSet horrorVisualSet;

        [Header("Sprite Fallback")]
        [SerializeField] private Sprite normalSprite;
        [SerializeField] private Sprite horrorSprite;
        [SerializeField] private bool cacheInitialSpriteAsNormal = true;

        [Header("Rules")]
        [SerializeField] private bool automaticHorrorPhase = true;
        [SerializeField] [Range(0.01f, 1f)] private float healthThreshold = 0.5f;
        [SerializeField] [Min(0.05f)] private float healthCheckInterval = 0.2f;

        [Header("Safety")]
        [SerializeField] private bool logDebugMessages;

        private HorrorPhaseState _state = HorrorPhaseState.Normal;
        private Renderer[] _normalRootRenderers;
        private Renderer[] _horrorRootRenderers;
        private float _nextHealthCheckTime;
        private bool _warnedNormalRootIsSelf;
        private bool _warnedHorrorRootIsSelf;

        public event Action<bool> HorrorPhaseChanged;

        public HorrorPhaseState State => _state;
        public bool IsHorrorPhase => _state == HorrorPhaseState.Horror;
        public bool AutomaticHorrorPhase => automaticHorrorPhase;
        public float HealthThreshold => healthThreshold;

        private void Awake()
        {
            ResolveReferences();
            CacheInitialSprite();
            PooledEffectSpawner.Prewarm(transitionEffectPrefab, 1);
            ApplyPhaseVisuals(false, spawnEffect: false);
        }

        private void OnEnable()
        {
            ResolveReferences();
            Subscribe();
            ResetPhase();
            EvaluateAutomaticPhase(force: true);
        }

        private void OnDisable()
        {
            Unsubscribe();
            _state = HorrorPhaseState.Disabled;
        }

        private void Update()
        {
            if (!automaticHorrorPhase || _state == HorrorPhaseState.Disabled || enemyHealth == null || enemyHealth.IsDead)
            {
                return;
            }

            if (Time.time < _nextHealthCheckTime)
            {
                return;
            }

            _nextHealthCheckTime = Time.time + healthCheckInterval;
            EvaluateAutomaticPhase(force: false);
        }

        public void EnterHorrorPhaseManual()
        {
            SetHorrorPhase(true);
        }

        public void ExitHorrorPhaseManual()
        {
            SetHorrorPhase(false);
        }

        public void SetHorrorPhase(bool enabled)
        {
            if (_state == HorrorPhaseState.Disabled)
            {
                _state = HorrorPhaseState.Normal;
            }

            SetHorrorPhaseInternal(enabled, spawnEffect: true);
        }

        public void ResetForReuse()
        {
            ResetPhase();
            EvaluateAutomaticPhase(force: true);
        }

        public void ResetPhase()
        {
            _state = HorrorPhaseState.Normal;
            _nextHealthCheckTime = 0f;
            ApplyPhaseVisuals(false, spawnEffect: false);
        }

        public void DisablePhaseController()
        {
            _state = HorrorPhaseState.Disabled;
            SetRootVisible(horrorVisualRoot, false, ref _horrorRootRenderers, ref _warnedHorrorRootIsSelf);
            SetRootVisible(normalVisualRoot, true, ref _normalRootRenderers, ref _warnedNormalRootIsSelf);
        }

        private void HandleDamaged()
        {
            EvaluateAutomaticPhase(force: true);
        }

        private void HandleDied()
        {
            _state = HorrorPhaseState.Disabled;
        }

        private void EvaluateAutomaticPhase(bool force)
        {
            if (!automaticHorrorPhase || enemyHealth == null || enemyHealth.IsDead || enemyHealth.MaxHealth <= 0f)
            {
                return;
            }

            float normalizedHealth = Mathf.Clamp01(enemyHealth.CurrentHealth / enemyHealth.MaxHealth);

            if (force || normalizedHealth <= healthThreshold)
            {
                SetHorrorPhaseInternal(normalizedHealth <= healthThreshold, spawnEffect: normalizedHealth <= healthThreshold);
            }
        }

        private void SetHorrorPhaseInternal(bool enabled, bool spawnEffect)
        {
            HorrorPhaseState targetState = enabled ? HorrorPhaseState.Horror : HorrorPhaseState.Normal;

            if (_state == targetState)
            {
                return;
            }

            _state = targetState;
            ApplyPhaseVisuals(enabled, spawnEffect);
            HorrorPhaseChanged?.Invoke(enabled);
        }

        private void ApplyPhaseVisuals(bool horrorEnabled, bool spawnEffect)
        {
            ApplyVisualSet(horrorEnabled);
            ApplySprite(horrorEnabled);
            SetRootVisible(normalVisualRoot, !horrorEnabled, ref _normalRootRenderers, ref _warnedNormalRootIsSelf);
            SetRootVisible(horrorVisualRoot, horrorEnabled, ref _horrorRootRenderers, ref _warnedHorrorRootIsSelf);

            if (spawnEffect && transitionEffectPrefab != null)
            {
                PooledEffectSpawner.Spawn(transitionEffectPrefab, transform.position, Quaternion.identity);
            }
        }

        private void ApplyVisualSet(bool horrorEnabled)
        {
            if (enemyVisual == null)
            {
                return;
            }

            EnemyVisualSet visualSet = horrorEnabled ? horrorVisualSet : normalVisualSet;

            if (visualSet != null)
            {
                enemyVisual.ApplyVisualSet(visualSet);
            }
            else if (!horrorEnabled)
            {
                enemyVisual.ResetPresentation();
            }
        }

        private void ApplySprite(bool horrorEnabled)
        {
            if (bodySpriteRenderer == null)
            {
                return;
            }

            Sprite resolvedSprite = horrorEnabled ? horrorSprite : normalSprite;

            if (resolvedSprite != null)
            {
                bodySpriteRenderer.sprite = resolvedSprite;
            }
        }

        private void SetRootVisible(Transform root, bool visible, ref Renderer[] rendererCache, ref bool warnedRootIsSelf)
        {
            if (root == null)
            {
                return;
            }

            if (root == transform)
            {
                if (!warnedRootIsSelf && logDebugMessages)
                {
                    Debug.LogWarning("EnemyHorrorPhase visual root points to the enemy root. Skipping SetActive to avoid disabling gameplay.", this);
                    warnedRootIsSelf = true;
                }

                SetRenderersVisible(root, visible, ref rendererCache);
                return;
            }

            root.gameObject.SetActive(visible);
        }

        private static void SetRenderersVisible(Transform root, bool visible, ref Renderer[] rendererCache)
        {
            if (rendererCache == null || rendererCache.Length == 0)
            {
                rendererCache = root.GetComponentsInChildren<Renderer>(true);
            }

            for (int i = 0; i < rendererCache.Length; i++)
            {
                if (rendererCache[i] != null)
                {
                    rendererCache[i].enabled = visible;
                }
            }
        }

        private void Subscribe()
        {
            if (enemyHealth == null)
            {
                return;
            }

            enemyHealth.Damaged -= HandleDamaged;
            enemyHealth.Damaged += HandleDamaged;
            enemyHealth.Died -= HandleDied;
            enemyHealth.Died += HandleDied;
        }

        private void Unsubscribe()
        {
            if (enemyHealth == null)
            {
                return;
            }

            enemyHealth.Damaged -= HandleDamaged;
            enemyHealth.Died -= HandleDied;
        }

        private void ResolveReferences()
        {
            if (enemyHealth == null)
            {
                enemyHealth = GetComponent<EnemyHealth>();
            }

            if (enemyVisual == null)
            {
                enemyVisual = GetComponent<EnemyVisual>();
            }

            if (bodySpriteRenderer == null && enemyVisual != null)
            {
                bodySpriteRenderer = enemyVisual.BodySpriteRenderer;
            }
        }

        private void CacheInitialSprite()
        {
            if (!cacheInitialSpriteAsNormal || normalSprite != null || bodySpriteRenderer == null)
            {
                return;
            }

            normalSprite = bodySpriteRenderer.sprite;
        }

        private void Reset()
        {
            ResolveReferences();
            CacheInitialSprite();
        }

        private void OnValidate()
        {
            ResolveReferences();
            _normalRootRenderers = null;
            _horrorRootRenderers = null;
            CacheInitialSprite();
            healthThreshold = Mathf.Clamp01(healthThreshold);
            healthCheckInterval = Mathf.Max(0.05f, healthCheckInterval);
        }
    }
}
