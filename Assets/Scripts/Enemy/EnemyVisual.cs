using CuteIssac.Data.Enemy;
using UnityEngine;

namespace CuteIssac.Enemy
{
    /// <summary>
    /// Presentation layer for enemy prefabs.
    /// Replace sprites, animator, and effect anchors here without changing enemy gameplay logic.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EnemyVisual : MonoBehaviour
    {
        private enum ChampionMarkerStyle
        {
            None = 0,
            Swift = 1,
            Bulwark = 2,
            Volatile = 3
        }

        private struct ChampionBurst
        {
            public Transform Transform;
            public SpriteRenderer Renderer;
            public Vector3 StartScale;
            public Vector3 EndScale;
            public Color StartColor;
            public Color EndColor;
            public float SpawnTime;
            public float Lifetime;
            public float RotationSpeed;
        }

        private enum FacingMode
        {
            None = 0,
            FlipX = 1,
            RotateVisualRoot = 2
        }

        [Header("Logic References")]
        [Tooltip("Optional. Used to subscribe to damage and death events without embedding visuals into EnemyHealth.")]
        [SerializeField] private EnemyHealth enemyHealth;

        [Header("Presentation Roots")]
        [Tooltip("Optional root that contains all artist-facing presentation children.")]
        [SerializeField] private Transform visualRoot;
        [Tooltip("Optional root that receives brief hit punch scaling without affecting gameplay transforms.")]
        [SerializeField] private Transform hitFeedbackRoot;
        [Tooltip("Optional transform that flips or rotates with movement direction.")]
        [SerializeField] private Transform facingRoot;
        [Tooltip("Optional extra root for highlight or accessory visuals.")]
        [SerializeField] private Transform optionalHighlightRoot;
        [Tooltip("Optional extra root for shadow visuals.")]
        [SerializeField] private Transform optionalShadowRoot;

        [Header("Renderer References")]
        [Tooltip("Primary body sprite to swap when enemy art changes.")]
        [SerializeField] private SpriteRenderer bodySpriteRenderer;
        [Tooltip("Optional animator. Enemy logic does not require it.")]
        [SerializeField] private Animator bodyAnimator;
        [Tooltip("Optional separate renderer used for hit flash if the body sprite should stay untouched.")]
        [SerializeField] private SpriteRenderer hitFlashTarget;

        [Header("Effect Anchors")]
        [Tooltip("Optional anchor for melee or ranged attack effects.")]
        [SerializeField] private Transform attackEffectAnchor;
        [Tooltip("Optional anchor for damage numbers or hit spark effects.")]
        [SerializeField] private Transform hitEffectAnchor;
        [Tooltip("Optional anchor for death burst or dissolve effects.")]
        [SerializeField] private Transform deathEffectAnchor;

        [Header("Facing")]
        [SerializeField] private FacingMode facingMode = FacingMode.FlipX;

        [Header("Feedback")]
        [SerializeField] private Color baseColor = Color.white;
        [SerializeField] private Color hitFlashColor = new(1f, 1f, 1f, 1f);
        [SerializeField] private Color damagedColor = new(1f, 0.48f, 0.48f, 1f);
        [SerializeField] [Min(0f)] private float damagedFlashDuration = 0.08f;
        [SerializeField] [Min(1f)] private float hitScaleMultiplier = 1.14f;
        [SerializeField] [Min(0f)] private float hitScaleRecoverSpeed = 7f;
        [SerializeField] private Color deadColor = new(1f, 1f, 1f, 0.55f);
        [SerializeField] [Min(0.1f)] private float attackTelegraphPulseSpeed = 9f;
        [SerializeField] [Range(0f, 1f)] private float attackTelegraphBlend = 0.75f;

        [Header("Champion Visuals")]
        [SerializeField] private Vector3 championMarkerLocalOffset = new(0f, 1.2f, 0f);
        [SerializeField] private Vector2 championAuraSize = new(0.65f, 0.65f);
        [SerializeField] private Vector2 championCoreSize = new(0.28f, 0.28f);
        [SerializeField] private Vector2 championGlyphSize = new(0.42f, 0.14f);
        [SerializeField] [Min(0f)] private float championBobAmplitude = 0.06f;
        [SerializeField] [Min(0f)] private float championBobSpeed = 2.4f;
        [SerializeField] [Min(0f)] private float championPulseAmplitude = 0.22f;
        [SerializeField] [Min(0f)] private float championPulseSpeed = 4.2f;
        [SerializeField] [Min(0.05f)] private float championBurstLifetime = 0.55f;
        [SerializeField] private Color championAuraBaseColor = new(1f, 1f, 1f, 0.16f);
        [SerializeField] private Vector2 bulwarkGuardAuraSize = new(1.65f, 1.2f);
        [SerializeField] private Vector2 bulwarkGuardCoreSize = new(1.02f, 0.82f);
        [SerializeField] [Min(0f)] private float bulwarkGuardPulseSpeed = 5.2f;
        [SerializeField] [Min(0f)] private float bulwarkGuardPulseAmplitude = 0.26f;

        [Header("Optional Animator Parameters")]
        [SerializeField] private string moveXParameter = "MoveX";
        [SerializeField] private string moveYParameter = "MoveY";
        [SerializeField] private string speedParameter = "Speed";
        [SerializeField] private string damagedTriggerParameter = "Damaged";
        [SerializeField] private string deadBoolParameter = "Dead";

        [Header("Health Phase Animator")]
        [SerializeField] private string healthNormalizedParameter = "HealthNormalized";
        [SerializeField] private string lowHealthBoolParameter = "LowHealth";
        [SerializeField] private string healthPhaseIndexParameter = "HealthPhaseIndex";
        [SerializeField] private string healthPhaseChangedTriggerParameter = "HealthPhaseChanged";

        [Header("Temporary Low Health Preview")]
        [SerializeField] private bool useTemporaryLowHealthPreview = true;
        [SerializeField] [Range(0f, 1f)] private float temporaryLowHealthThreshold = 0.5f;
        [SerializeField] [Range(0f, 1f)] private float temporaryLowHealthTintBlend = 0.72f;
        [SerializeField] private Color temporaryLowHealthTint = new(0.88f, 0.54f, 0.54f, 1f);
        [SerializeField] private Color temporaryLowHealthHitFlashColor = new(1f, 0.9f, 0.9f, 1f);
        [SerializeField] private Color temporaryLowHealthDamagedColor = new(0.84f, 0.16f, 0.16f, 1f);
        [SerializeField] private Color temporaryLowHealthDeadColor = new(0.46f, 0.08f, 0.08f, 0.55f);
        [SerializeField] [Min(0.5f)] private float temporaryLowHealthScaleMultiplier = 0.92f;
        [SerializeField] private Color temporaryLowHealthSlashColor = new(0.58f, 0.06f, 0.06f, 0.9f);

        public Transform AttackEffectAnchor => attackEffectAnchor != null ? attackEffectAnchor : transform;
        public Transform HitEffectAnchor => hitEffectAnchor != null ? hitEffectAnchor : AttackEffectAnchor;
        public Transform DeathEffectAnchor => deathEffectAnchor != null ? deathEffectAnchor : transform;
        public Transform OptionalShadowRoot => optionalShadowRoot;
        public Transform OptionalHighlightRoot => optionalHighlightRoot;
        public SpriteRenderer BodySpriteRenderer => bodySpriteRenderer;
        public Animator BodyAnimator => bodyAnimator;

        private float _damagedFlashRemaining;
        private float _damagedFlashTotalDuration;
        private bool _warnedMissingRenderer;
        private Vector3 _initialHitFeedbackScale = Vector3.one;
        private bool _hasInitialHitFeedbackScale;
        private bool _attackTelegraphActive;
        private Color _attackTelegraphColor = Color.white;
        private EnemyVisualSet _activeVisualSet;
        private int _activeHealthPhaseIndex = int.MinValue;
        private float _lastNormalizedHealth = -1f;
        private bool _temporaryLowHealthPreviewActive;
        private Color _authoredBaseColor = Color.white;
        private Color _authoredHitFlashColor = Color.white;
        private Color _authoredDamagedColor = Color.white;
        private Color _authoredDeadColor = Color.white;
        private bool _hasAuthoredPalette;
        private Sprite _authoredBodySprite;
        private bool _hasAuthoredBodySprite;
        private Vector3 _baselineVisualRootScale = Vector3.one;
        private bool _hasBaselineVisualRootScale;
        private Vector3 _authoredVisualRootScale = Vector3.one;
        private bool _hasAuthoredVisualRootScale;
        private static Sprite s_championFallbackSprite;
        private bool _championVisualActive;
        private ChampionMarkerStyle _championMarkerStyle;
        private Color _championAccentColor = Color.white;
        private string _championVariantId = string.Empty;
        private float _championColorBlend;
        private float _championScaleMultiplier = 1f;
        private Transform _championMarkerRoot;
        private SpriteRenderer _championAuraRenderer;
        private SpriteRenderer _championCoreRenderer;
        private SpriteRenderer _championGlyphRenderer;
        private Transform _temporaryLowHealthPreviewRoot;
        private SpriteRenderer _temporaryLowHealthSlashRendererA;
        private SpriteRenderer _temporaryLowHealthSlashRendererB;
        private SpriteRenderer _temporaryLowHealthSlashRendererC;
        private Transform _bulwarkGuardRoot;
        private SpriteRenderer _bulwarkGuardAuraRenderer;
        private SpriteRenderer _bulwarkGuardCoreRenderer;
        private Vector3 _championMarkerBaseLocalPosition;
        private float _championMarkerPhaseOffset;
        private readonly System.Collections.Generic.List<ChampionBurst> _championBursts = new();
        private bool _bulwarkGuardVisualActive;

        private void Awake()
        {
            ResolveReferences();
            CacheAuthoredPalette();
            CacheAuthoredBodySprite();
            CacheBaselineVisualRootScale();
            CacheAuthoredVisualRootScale();
            CacheHitFeedbackScale();
            ApplyBodyColor(baseColor);
            _championMarkerPhaseOffset = Random.Range(0f, Mathf.PI * 2f);
        }

        private void OnEnable()
        {
            ResolveReferences();
            ResetPresentation();

            if (enemyHealth != null)
            {
                enemyHealth.Damaged += HandleDamaged;
                enemyHealth.Died += HandleDied;
            }
        }

        private void OnDisable()
        {
            if (enemyHealth != null)
            {
                enemyHealth.Damaged -= HandleDamaged;
                enemyHealth.Died -= HandleDied;
            }
        }

        private void Update()
        {
            RefreshHealthPhasePresentation();
            RecoverHitScale();

            if (_damagedFlashRemaining > 0f)
            {
                _damagedFlashRemaining -= Time.deltaTime;

                if (_damagedFlashRemaining <= 0f)
                {
                    if (enemyHealth != null && !enemyHealth.IsDead)
                    {
                        ApplyBodyColor(_attackTelegraphActive ? ResolveTelegraphColor() : baseColor);
                    }

                    return;
                }

                UpdateHitFlashColor();
                return;
            }

            if (_attackTelegraphActive && (enemyHealth == null || !enemyHealth.IsDead))
            {
                ApplyBodyColor(ResolveTelegraphColor());
            }

            UpdateChampionMarker();
            UpdateBulwarkGuardVisual();
            UpdateChampionBursts();
        }

        public void SetMoveDirection(Vector2 moveDirection)
        {
            ApplyFacing(moveDirection);
            UpdateAnimatorMove(moveDirection);
        }

        public void HandleAttack()
        {
            // Reserved for future attack VFX/animation triggers.
        }

        public void StartAttackTelegraph(Color telegraphColor)
        {
            _attackTelegraphActive = true;
            _attackTelegraphColor = telegraphColor;

            if (_damagedFlashRemaining <= 0f && (enemyHealth == null || !enemyHealth.IsDead))
            {
                ApplyBodyColor(ResolveTelegraphColor());
            }
        }

        public void StopAttackTelegraph()
        {
            _attackTelegraphActive = false;

            if (_damagedFlashRemaining <= 0f && (enemyHealth == null || !enemyHealth.IsDead))
            {
                ApplyBodyColor(baseColor);
            }
        }

        public void HandleDamaged()
        {
            _damagedFlashRemaining = damagedFlashDuration;
            _damagedFlashTotalDuration = Mathf.Max(0.01f, damagedFlashDuration);
            ApplyBodyColor(hitFlashColor);
            ApplyHitScalePunch();
            SetAnimatorTrigger(damagedTriggerParameter);
        }

        public void HandleDied()
        {
            _damagedFlashRemaining = 0f;
            ApplyBodyColor(deadColor);
            SetAnimatorBool(deadBoolParameter, true);

            if (_championVisualActive)
            {
                SpawnChampionBurst();
                SetChampionMarkerVisible(false);
            }
        }

        public void ApplyVisualSet(EnemyVisualSet visualSet)
        {
            ResolveReferences();
            _activeVisualSet = visualSet;
            _activeHealthPhaseIndex = int.MinValue;
            _lastNormalizedHealth = -1f;
            RefreshHealthPhasePresentation(forceApply: true);
        }

        public void ApplyChampionPresentation(string variantId, Color accentColor, float colorBlend, float scaleMultiplier)
        {
            _championVariantId = variantId ?? string.Empty;
            _championColorBlend = colorBlend;
            _championScaleMultiplier = scaleMultiplier;
            ApplyChampionPresentationInternal(true, accentColor);
        }

        public void UpdateChampionPresentation(string variantId, Color accentColor, float colorBlend, float scaleMultiplier)
        {
            if (!_championVisualActive && string.IsNullOrWhiteSpace(_championVariantId))
            {
                return;
            }

            _championVariantId = variantId ?? string.Empty;
            _championColorBlend = colorBlend;
            _championScaleMultiplier = scaleMultiplier;
            ApplyChampionPresentationInternal(false, accentColor);
        }

        public void ResetChampionPresentation()
        {
            RestoreAuthoredPresentation();
            _championVisualActive = false;
            _championMarkerStyle = ChampionMarkerStyle.None;
            _championAccentColor = Color.white;
            _championVariantId = string.Empty;
            _championColorBlend = 0f;
            _championScaleMultiplier = 1f;
            _bulwarkGuardVisualActive = false;
            SetChampionMarkerVisible(false);
            SetBulwarkGuardVisible(false);
            ClearChampionBursts();

            if (_damagedFlashRemaining <= 0f && (enemyHealth == null || !enemyHealth.IsDead))
            {
                ApplyBodyColor(_attackTelegraphActive ? ResolveTelegraphColor() : baseColor);
            }
        }

        public void ResetPresentation()
        {
            RestoreAuthoredPresentation();
            _damagedFlashRemaining = 0f;
            _damagedFlashTotalDuration = 0f;
            _attackTelegraphActive = false;
            _hasInitialHitFeedbackScale = false;
            CacheHitFeedbackScale();
            ClearChampionBursts();
            SetChampionMarkerVisible(false);
            SetBulwarkGuardVisible(false);
            _bulwarkGuardVisualActive = false;
            _activeHealthPhaseIndex = int.MinValue;
            _lastNormalizedHealth = -1f;
            RefreshHealthPhasePresentation(forceApply: true);
            ApplyBodyColor(baseColor);
            SetAnimatorBool(deadBoolParameter, false);
        }

        public void SetChampionGuardState(bool active, Color accentColor)
        {
            _bulwarkGuardVisualActive = active;

            if (accentColor.a > 0f)
            {
                _championAccentColor = accentColor;
            }

            BuildBulwarkGuardIfNeeded();
            ApplyBulwarkGuardTheme();
            SetBulwarkGuardVisible(active);
        }

        private void RefreshHealthPhasePresentation(bool forceApply = false)
        {
            ResolveReferences();

            float normalizedHealth = enemyHealth != null && enemyHealth.MaxHealth > 0f
                ? Mathf.Clamp01(enemyHealth.CurrentHealth / enemyHealth.MaxHealth)
                : 1f;

            EnemyVisualSet.HealthVisualPhase resolvedPhase = null;
            int resolvedPhaseIndex = -1;

            if (_activeVisualSet != null)
            {
                _activeVisualSet.TryResolveHealthPhase(normalizedHealth, out resolvedPhase, out resolvedPhaseIndex);
            }

            bool useTemporaryPreview = ShouldUseTemporaryLowHealthPreview(normalizedHealth, resolvedPhase);
            bool phaseChanged = resolvedPhaseIndex != _activeHealthPhaseIndex
                || useTemporaryPreview != _temporaryLowHealthPreviewActive;

            if (forceApply || phaseChanged)
            {
                if (useTemporaryPreview)
                {
                    ApplyTemporaryLowHealthPreviewPresentation();
                }
                else
                {
                    ApplyResolvedHealthPhasePresentation(resolvedPhase);
                }

                _activeHealthPhaseIndex = resolvedPhaseIndex;
                _temporaryLowHealthPreviewActive = useTemporaryPreview;

                if (!forceApply && phaseChanged)
                {
                    SetAnimatorTrigger(healthPhaseChangedTriggerParameter);
                }
            }

            if (forceApply || phaseChanged || !Mathf.Approximately(_lastNormalizedHealth, normalizedHealth))
            {
                SetAnimatorFloat(healthNormalizedParameter, normalizedHealth);
                SetAnimatorBool(lowHealthBoolParameter, resolvedPhaseIndex >= 0 || useTemporaryPreview);
                SetAnimatorInteger(healthPhaseIndexParameter, resolvedPhaseIndex >= 0 ? resolvedPhaseIndex + 1 : useTemporaryPreview ? 1 : 0);
                _lastNormalizedHealth = normalizedHealth;
            }
        }

        private void ApplyResolvedHealthPhasePresentation(EnemyVisualSet.HealthVisualPhase phase)
        {
            SetTemporaryLowHealthPreviewVisible(false);

            Sprite resolvedBodySprite = ResolveBodySpriteForPhase(phase);
            Color resolvedBaseColor = phase != null && phase.OverrideBaseColor
                ? phase.BaseColor
                : _activeVisualSet != null
                    ? _activeVisualSet.BaseColor
                    : _hasAuthoredPalette
                        ? _authoredBaseColor
                        : baseColor;
            Color resolvedHitFlashColor = phase != null && phase.OverrideHitFlashColor
                ? phase.HitFlashColor
                : _activeVisualSet != null
                    ? _activeVisualSet.HitFlashColor
                    : _hasAuthoredPalette
                        ? _authoredHitFlashColor
                        : hitFlashColor;
            Color resolvedDamagedColor = phase != null && phase.OverrideDamagedColor
                ? phase.DamagedColor
                : _activeVisualSet != null
                    ? _activeVisualSet.DamagedColor
                    : _hasAuthoredPalette
                        ? _authoredDamagedColor
                        : damagedColor;
            Color resolvedDeadColor = phase != null && phase.OverrideDeadColor
                ? phase.DeadColor
                : _activeVisualSet != null
                    ? _activeVisualSet.DeadColor
                    : _hasAuthoredPalette
                        ? _authoredDeadColor
                        : deadColor;
            float scaleMultiplier = phase != null && phase.OverrideVisualScale
                ? phase.VisualScaleMultiplier
                : 1f;

            if (bodySpriteRenderer != null && resolvedBodySprite != null)
            {
                bodySpriteRenderer.sprite = resolvedBodySprite;
            }

            baseColor = resolvedBaseColor;
            hitFlashColor = resolvedHitFlashColor;
            damagedColor = resolvedDamagedColor;
            deadColor = resolvedDeadColor;

            ApplyResolvedVisualScale(scaleMultiplier);
            CacheAuthoredBodySprite();
            CacheAuthoredPalette();
            CacheAuthoredVisualRootScale();

            if (_championVisualActive)
            {
                ApplyChampionPresentationInternal(false, _championAccentColor);
            }
            else if (_damagedFlashRemaining <= 0f && (enemyHealth == null || !enemyHealth.IsDead))
            {
                ApplyBodyColor(_attackTelegraphActive ? ResolveTelegraphColor() : baseColor);
            }
        }

        private void ApplyTemporaryLowHealthPreviewPresentation()
        {
            Sprite resolvedBodySprite = ResolveBodySpriteForPhase(null);
            if (bodySpriteRenderer != null && resolvedBodySprite != null)
            {
                bodySpriteRenderer.sprite = resolvedBodySprite;
            }

            Color authoredBaseColor = _activeVisualSet != null
                ? _activeVisualSet.BaseColor
                : _hasAuthoredPalette
                    ? _authoredBaseColor
                    : baseColor;
            Color authoredHitFlashColor = _activeVisualSet != null
                ? _activeVisualSet.HitFlashColor
                : _hasAuthoredPalette
                    ? _authoredHitFlashColor
                    : hitFlashColor;
            Color authoredDamagedColor = _activeVisualSet != null
                ? _activeVisualSet.DamagedColor
                : _hasAuthoredPalette
                    ? _authoredDamagedColor
                    : damagedColor;
            Color authoredDeadColor = _activeVisualSet != null
                ? _activeVisualSet.DeadColor
                : _hasAuthoredPalette
                    ? _authoredDeadColor
                    : deadColor;
            float tintBlend = Mathf.Clamp01(temporaryLowHealthTintBlend);

            baseColor = Color.Lerp(authoredBaseColor, temporaryLowHealthTint, tintBlend);
            hitFlashColor = Color.Lerp(authoredHitFlashColor, temporaryLowHealthHitFlashColor, tintBlend);
            damagedColor = Color.Lerp(authoredDamagedColor, temporaryLowHealthDamagedColor, tintBlend);
            deadColor = Color.Lerp(authoredDeadColor, temporaryLowHealthDeadColor, tintBlend);

            ApplyResolvedVisualScale(temporaryLowHealthScaleMultiplier);
            SetTemporaryLowHealthPreviewVisible(true);

            if (_championVisualActive)
            {
                ApplyChampionPresentationInternal(false, _championAccentColor);
            }
            else if (_damagedFlashRemaining <= 0f && (enemyHealth == null || !enemyHealth.IsDead))
            {
                ApplyBodyColor(_attackTelegraphActive ? ResolveTelegraphColor() : baseColor);
            }
        }

        private Sprite ResolveBodySpriteForPhase(EnemyVisualSet.HealthVisualPhase phase)
        {
            if (phase != null && phase.BodySprite != null)
            {
                return phase.BodySprite;
            }

            if (_activeVisualSet != null && _activeVisualSet.BodySprite != null)
            {
                return _activeVisualSet.BodySprite;
            }

            if (_hasAuthoredBodySprite)
            {
                return _authoredBodySprite;
            }

            return bodySpriteRenderer != null ? bodySpriteRenderer.sprite : null;
        }

        private void ApplyResolvedVisualScale(float scaleMultiplier)
        {
            Transform scaleRoot = visualRoot != null ? visualRoot : transform;

            if (scaleRoot == null)
            {
                return;
            }

            Vector3 baselineScale = _hasBaselineVisualRootScale
                ? _baselineVisualRootScale
                : scaleRoot.localScale;
            scaleRoot.localScale = baselineScale * Mathf.Max(0.5f, scaleMultiplier);
        }

        private void ApplyChampionPresentationInternal(bool captureAuthoredBase, Color accentColor)
        {
            if (captureAuthoredBase)
            {
                CacheAuthoredBodySprite();
                CacheAuthoredPalette();
                CacheAuthoredVisualRootScale();
            }

            float blend = Mathf.Clamp01(_championColorBlend);
            baseColor = Color.Lerp(_authoredBaseColor, accentColor, blend);
            damagedColor = Color.Lerp(_authoredDamagedColor, accentColor, blend * 0.35f);
            deadColor = Color.Lerp(_authoredDeadColor, accentColor, blend * 0.18f);
            deadColor.a = _authoredDeadColor.a;

            Transform scaleRoot = visualRoot != null ? visualRoot : transform;

            if (scaleRoot != null && _hasAuthoredVisualRootScale)
            {
                scaleRoot.localScale = _authoredVisualRootScale * Mathf.Max(0.5f, _championScaleMultiplier);
            }

            _championVisualActive = true;
            _championAccentColor = accentColor;
            _championMarkerStyle = ResolveChampionMarkerStyle(_championVariantId);
            BuildChampionMarkerIfNeeded();
            BuildBulwarkGuardIfNeeded();
            ApplyChampionMarkerTheme();
            SetChampionMarkerVisible(true);
            _hasInitialHitFeedbackScale = false;
            CacheHitFeedbackScale();

            if (_damagedFlashRemaining <= 0f && (enemyHealth == null || !enemyHealth.IsDead))
            {
                ApplyBodyColor(_attackTelegraphActive ? ResolveTelegraphColor() : baseColor);
            }
        }

        private Color ResolveTelegraphColor()
        {
            float pulse = 0.5f + (0.5f * Mathf.Sin(Time.time * attackTelegraphPulseSpeed));
            float blend = Mathf.Clamp01(Mathf.Lerp(attackTelegraphBlend * 0.6f, attackTelegraphBlend, pulse));
            return Color.Lerp(baseColor, _attackTelegraphColor, blend);
        }

        private void UpdateHitFlashColor()
        {
            if (_damagedFlashTotalDuration <= 0f || enemyHealth == null || enemyHealth.IsDead)
            {
                return;
            }

            float progress = 1f - Mathf.Clamp01(_damagedFlashRemaining / _damagedFlashTotalDuration);
            Color nextColor = progress < 0.35f
                ? Color.Lerp(hitFlashColor, damagedColor, progress / 0.35f)
                : Color.Lerp(damagedColor, baseColor, (progress - 0.35f) / 0.65f);
            ApplyBodyColor(nextColor);
        }

        private void ResolveReferences()
        {
            if (enemyHealth == null)
            {
                TryGetComponent(out enemyHealth);
            }

            if (visualRoot == null)
            {
                visualRoot = transform;
            }

            if (facingRoot == null)
            {
                facingRoot = visualRoot;
            }

            if (hitFeedbackRoot == null)
            {
                hitFeedbackRoot = facingRoot != null ? facingRoot : visualRoot;
            }

            if (bodySpriteRenderer == null)
            {
                bodySpriteRenderer = GetComponentInChildren<SpriteRenderer>(true);
            }

            if (bodyAnimator == null)
            {
                bodyAnimator = GetComponentInChildren<Animator>(true);
            }

            if (hitFlashTarget == null)
            {
                hitFlashTarget = bodySpriteRenderer;
            }
        }

        private bool ShouldUseTemporaryLowHealthPreview(float normalizedHealth, EnemyVisualSet.HealthVisualPhase resolvedPhase)
        {
            if (!Application.isPlaying)
            {
                return false;
            }

            return useTemporaryLowHealthPreview
                && resolvedPhase == null
                && normalizedHealth <= Mathf.Clamp01(temporaryLowHealthThreshold);
        }

        private void EnsureTemporaryLowHealthPreviewBuilt()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            if (_temporaryLowHealthPreviewRoot != null)
            {
                return;
            }

            Sprite fallbackSprite = GetChampionFallbackSprite();
            if (fallbackSprite == null)
            {
                return;
            }

            Transform previewParent = optionalHighlightRoot != null
                ? optionalHighlightRoot
                : visualRoot != null
                    ? visualRoot
                    : transform;

            GameObject rootObject = new("LowHealthPreview");
            rootObject.transform.SetParent(previewParent, false);
            rootObject.transform.localPosition = Vector3.zero;
            rootObject.transform.localRotation = Quaternion.identity;
            rootObject.transform.localScale = Vector3.one;
            rootObject.layer = gameObject.layer;

            _temporaryLowHealthPreviewRoot = rootObject.transform;
            _temporaryLowHealthSlashRendererA = CreateTemporaryLowHealthPreviewLayer(
                "SlashA",
                fallbackSprite,
                new Vector3(-0.18f, 0.02f, 0f),
                new Vector2(0.08f, 0.86f),
                -18f,
                temporaryLowHealthSlashColor,
                31);
            _temporaryLowHealthSlashRendererB = CreateTemporaryLowHealthPreviewLayer(
                "SlashB",
                fallbackSprite,
                new Vector3(0f, 0.04f, 0f),
                new Vector2(0.075f, 0.94f),
                -18f,
                temporaryLowHealthSlashColor,
                32);
            _temporaryLowHealthSlashRendererC = CreateTemporaryLowHealthPreviewLayer(
                "SlashC",
                fallbackSprite,
                new Vector3(0.18f, 0.02f, 0f),
                new Vector2(0.08f, 0.86f),
                -18f,
                new Color(
                    temporaryLowHealthSlashColor.r,
                    temporaryLowHealthSlashColor.g,
                    temporaryLowHealthSlashColor.b,
                    temporaryLowHealthSlashColor.a * 0.9f),
                33);
            _temporaryLowHealthPreviewRoot.gameObject.SetActive(false);
        }

        private SpriteRenderer CreateTemporaryLowHealthPreviewLayer(
            string name,
            Sprite sprite,
            Vector3 localPosition,
            Vector2 size,
            float rotationZ,
            Color color,
            int sortingOrder)
        {
            GameObject child = new(name);
            child.transform.SetParent(_temporaryLowHealthPreviewRoot, false);
            child.transform.localPosition = localPosition;
            child.transform.localRotation = Quaternion.Euler(0f, 0f, rotationZ);
            child.transform.localScale = new Vector3(size.x, size.y, 1f);
            child.layer = gameObject.layer;

            SpriteRenderer spriteRenderer = child.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = sprite;
            spriteRenderer.color = color;
            spriteRenderer.sortingOrder = sortingOrder;
            return spriteRenderer;
        }

        private void SetTemporaryLowHealthPreviewVisible(bool visible)
        {
            EnsureTemporaryLowHealthPreviewBuilt();

            if (_temporaryLowHealthPreviewRoot == null)
            {
                return;
            }

            if (visible)
            {
                if (_temporaryLowHealthSlashRendererA != null)
                {
                    _temporaryLowHealthSlashRendererA.color = temporaryLowHealthSlashColor;
                }

                if (_temporaryLowHealthSlashRendererB != null)
                {
                    _temporaryLowHealthSlashRendererB.color = new Color(
                        temporaryLowHealthSlashColor.r,
                        temporaryLowHealthSlashColor.g,
                        temporaryLowHealthSlashColor.b,
                        temporaryLowHealthSlashColor.a);
                }

                if (_temporaryLowHealthSlashRendererC != null)
                {
                    _temporaryLowHealthSlashRendererC.color = new Color(
                        temporaryLowHealthSlashColor.r,
                        temporaryLowHealthSlashColor.g,
                        temporaryLowHealthSlashColor.b,
                        temporaryLowHealthSlashColor.a * 0.9f);
                }
            }

            _temporaryLowHealthPreviewRoot.gameObject.SetActive(visible);
        }

        private void BuildChampionMarkerIfNeeded()
        {
            if (_championMarkerRoot != null)
            {
                return;
            }

            Sprite fallbackSprite = GetChampionFallbackSprite();
            if (fallbackSprite == null)
            {
                return;
            }

            Transform markerParent = optionalHighlightRoot != null
                ? optionalHighlightRoot
                : visualRoot != null
                    ? visualRoot
                    : transform;

            GameObject rootObject = new("ChampionMarker");
            rootObject.transform.SetParent(markerParent, false);
            rootObject.transform.localPosition = championMarkerLocalOffset;
            rootObject.transform.localRotation = Quaternion.identity;
            rootObject.transform.localScale = Vector3.one;
            rootObject.layer = gameObject.layer;
            _championMarkerRoot = rootObject.transform;
            _championMarkerBaseLocalPosition = championMarkerLocalOffset;

            _championAuraRenderer = CreateChampionMarkerLayer(
                "Aura",
                fallbackSprite,
                Vector3.zero,
                championAuraSize,
                45f,
                championAuraBaseColor,
                30);
            _championCoreRenderer = CreateChampionMarkerLayer(
                "Core",
                fallbackSprite,
                Vector3.zero,
                championCoreSize,
                45f,
                Color.white,
                31);
            _championGlyphRenderer = CreateChampionMarkerLayer(
                "Glyph",
                fallbackSprite,
                Vector3.zero,
                championGlyphSize,
                0f,
                Color.white,
                32);

            SetChampionMarkerVisible(false);
        }

        private SpriteRenderer CreateChampionMarkerLayer(
            string name,
            Sprite sprite,
            Vector3 localPosition,
            Vector2 size,
            float rotationZ,
            Color color,
            int sortingOrder)
        {
            GameObject child = new(name);
            child.transform.SetParent(_championMarkerRoot, false);
            child.transform.localPosition = localPosition;
            child.transform.localRotation = Quaternion.Euler(0f, 0f, rotationZ);
            child.transform.localScale = new Vector3(size.x, size.y, 1f);
            child.layer = gameObject.layer;

            SpriteRenderer spriteRenderer = child.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = sprite;
            spriteRenderer.color = color;
            spriteRenderer.sortingOrder = sortingOrder;
            return spriteRenderer;
        }

        private void ApplyChampionMarkerTheme()
        {
            if (_championMarkerRoot == null)
            {
                return;
            }

            Color auraColor = Color.Lerp(championAuraBaseColor, _championAccentColor, 0.72f);
            auraColor.a = Mathf.Max(0.2f, auraColor.a);
            Color coreColor = Color.Lerp(Color.white, _championAccentColor, 0.38f);
            Color glyphColor = Color.Lerp(Color.white, _championAccentColor, 0.82f);

            if (_championAuraRenderer != null)
            {
                _championAuraRenderer.color = auraColor;
            }

            if (_championCoreRenderer != null)
            {
                _championCoreRenderer.color = coreColor;
            }

            if (_championGlyphRenderer != null)
            {
                _championGlyphRenderer.color = glyphColor;
            }
        }

        private void BuildBulwarkGuardIfNeeded()
        {
            if (_bulwarkGuardRoot != null)
            {
                return;
            }

            Sprite fallbackSprite = GetChampionFallbackSprite();
            if (fallbackSprite == null)
            {
                return;
            }

            Transform guardParent = optionalShadowRoot != null
                ? optionalShadowRoot
                : visualRoot != null
                    ? visualRoot
                    : transform;

            GameObject rootObject = new("BulwarkGuardAura");
            rootObject.transform.SetParent(guardParent, false);
            rootObject.transform.localPosition = new Vector3(0f, 0.12f, 0f);
            rootObject.transform.localRotation = Quaternion.identity;
            rootObject.transform.localScale = Vector3.one;
            rootObject.layer = gameObject.layer;
            _bulwarkGuardRoot = rootObject.transform;

            _bulwarkGuardAuraRenderer = CreateBulwarkGuardLayer(
                "Aura",
                fallbackSprite,
                Vector3.zero,
                bulwarkGuardAuraSize,
                0f,
                new Color(1f, 1f, 1f, 0.18f),
                4);
            _bulwarkGuardCoreRenderer = CreateBulwarkGuardLayer(
                "Core",
                fallbackSprite,
                Vector3.zero,
                bulwarkGuardCoreSize,
                45f,
                new Color(1f, 1f, 1f, 0.28f),
                5);

            SetBulwarkGuardVisible(false);
        }

        private SpriteRenderer CreateBulwarkGuardLayer(
            string name,
            Sprite sprite,
            Vector3 localPosition,
            Vector2 size,
            float rotationZ,
            Color color,
            int sortingOrder)
        {
            GameObject child = new(name);
            child.transform.SetParent(_bulwarkGuardRoot, false);
            child.transform.localPosition = localPosition;
            child.transform.localRotation = Quaternion.Euler(0f, 0f, rotationZ);
            child.transform.localScale = new Vector3(size.x, size.y, 1f);
            child.layer = gameObject.layer;

            SpriteRenderer spriteRenderer = child.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = sprite;
            spriteRenderer.color = color;
            spriteRenderer.sortingOrder = sortingOrder;
            return spriteRenderer;
        }

        private void ApplyBulwarkGuardTheme()
        {
            if (_bulwarkGuardRoot == null)
            {
                return;
            }

            Color auraColor = Color.Lerp(new Color(1f, 1f, 1f, 0.14f), _championAccentColor, 0.72f);
            auraColor.a = Mathf.Max(0.2f, auraColor.a);
            Color coreColor = Color.Lerp(new Color(1f, 1f, 1f, 0.22f), _championAccentColor, 0.82f);
            coreColor.a = Mathf.Max(0.28f, coreColor.a);

            if (_bulwarkGuardAuraRenderer != null)
            {
                _bulwarkGuardAuraRenderer.color = auraColor;
            }

            if (_bulwarkGuardCoreRenderer != null)
            {
                _bulwarkGuardCoreRenderer.color = coreColor;
            }
        }

        private void UpdateChampionMarker()
        {
            if (!_championVisualActive || _championMarkerRoot == null || (enemyHealth != null && enemyHealth.IsDead))
            {
                return;
            }

            float time = Time.time + _championMarkerPhaseOffset;
            float pulse = 0.5f + (0.5f * Mathf.Sin(time * championPulseSpeed));
            float bob = Mathf.Sin(time * championBobSpeed) * championBobAmplitude;
            _championMarkerRoot.localPosition = _championMarkerBaseLocalPosition + (Vector3.up * bob);

            float auraScaleMultiplier = 1f + (pulse * championPulseAmplitude);
            float glyphRotation = 0f;
            Vector3 glyphScale = new(championGlyphSize.x, championGlyphSize.y, 1f);
            Vector3 coreScale = new(championCoreSize.x, championCoreSize.y, 1f);

            switch (_championMarkerStyle)
            {
                case ChampionMarkerStyle.Swift:
                    glyphRotation = time * 180f;
                    glyphScale = new Vector3(championGlyphSize.x * 1.18f, championGlyphSize.y * 0.72f, 1f);
                    coreScale = new Vector3(championCoreSize.x * 0.88f, championCoreSize.y * 0.88f, 1f);
                    auraScaleMultiplier += pulse * 0.12f;
                    break;
                case ChampionMarkerStyle.Bulwark:
                    glyphRotation = 45f;
                    glyphScale = new Vector3(championGlyphSize.x * 0.92f, championGlyphSize.y * 2.1f, 1f);
                    coreScale = new Vector3(championCoreSize.x * 1.24f, championCoreSize.y * 1.24f, 1f);
                    auraScaleMultiplier *= 1.08f;
                    break;
                case ChampionMarkerStyle.Volatile:
                    glyphRotation = 45f + (time * 84f);
                    glyphScale = new Vector3(championGlyphSize.x * 1.1f, championGlyphSize.y * 1.1f, 1f);
                    coreScale = new Vector3(championCoreSize.x * (1f + (pulse * 0.4f)), championCoreSize.y * (1f + (pulse * 0.4f)), 1f);
                    auraScaleMultiplier += pulse * 0.18f;
                    break;
            }

            if (_championAuraRenderer != null)
            {
                _championAuraRenderer.transform.localScale = new Vector3(
                    championAuraSize.x * auraScaleMultiplier,
                    championAuraSize.y * auraScaleMultiplier,
                    1f);
            }

            if (_championCoreRenderer != null)
            {
                _championCoreRenderer.transform.localScale = coreScale;
                _championCoreRenderer.transform.localRotation = Quaternion.Euler(0f, 0f, glyphRotation * 0.35f);
            }

            if (_championGlyphRenderer != null)
            {
                _championGlyphRenderer.transform.localScale = glyphScale;
                _championGlyphRenderer.transform.localRotation = Quaternion.Euler(0f, 0f, glyphRotation);
            }
        }

        private void UpdateBulwarkGuardVisual()
        {
            if (!_bulwarkGuardVisualActive || _bulwarkGuardRoot == null || (enemyHealth != null && enemyHealth.IsDead))
            {
                return;
            }

            float time = Time.time + (_championMarkerPhaseOffset * 0.6f);
            float pulse = 0.5f + (0.5f * Mathf.Sin(time * bulwarkGuardPulseSpeed));
            float auraScaleMultiplier = 1f + (pulse * bulwarkGuardPulseAmplitude);
            float coreScaleMultiplier = 1f + (pulse * bulwarkGuardPulseAmplitude * 0.7f);

            if (_bulwarkGuardAuraRenderer != null)
            {
                Color auraColor = _bulwarkGuardAuraRenderer.color;
                auraColor.a = Mathf.Lerp(0.22f, 0.42f, pulse);
                _bulwarkGuardAuraRenderer.color = auraColor;
                _bulwarkGuardAuraRenderer.transform.localScale = new Vector3(
                    bulwarkGuardAuraSize.x * auraScaleMultiplier,
                    bulwarkGuardAuraSize.y * (1f + (pulse * bulwarkGuardPulseAmplitude * 0.55f)),
                    1f);
            }

            if (_bulwarkGuardCoreRenderer != null)
            {
                Color coreColor = _bulwarkGuardCoreRenderer.color;
                coreColor.a = Mathf.Lerp(0.3f, 0.58f, pulse);
                _bulwarkGuardCoreRenderer.color = coreColor;
                _bulwarkGuardCoreRenderer.transform.localScale = new Vector3(
                    bulwarkGuardCoreSize.x * coreScaleMultiplier,
                    bulwarkGuardCoreSize.y * coreScaleMultiplier,
                    1f);
                _bulwarkGuardCoreRenderer.transform.localRotation = Quaternion.Euler(0f, 0f, 45f + (pulse * 8f));
            }
        }

        private void SpawnChampionBurst()
        {
            Sprite fallbackSprite = GetChampionFallbackSprite();
            if (fallbackSprite == null)
            {
                return;
            }

            float rotationSpeed = _championMarkerStyle switch
            {
                ChampionMarkerStyle.Swift => 220f,
                ChampionMarkerStyle.Bulwark => 64f,
                ChampionMarkerStyle.Volatile => 300f,
                _ => 120f
            };

            CreateChampionBurstLayer(
                "ChampionBurstOuter",
                fallbackSprite,
                _championAccentColor,
                new Color(_championAccentColor.r, _championAccentColor.g, _championAccentColor.b, 0f),
                new Vector3(0.4f, 0.4f, 1f),
                new Vector3(1.45f, 1.45f, 1f),
                rotationSpeed);
            CreateChampionBurstLayer(
                "ChampionBurstInner",
                fallbackSprite,
                Color.Lerp(Color.white, _championAccentColor, 0.58f),
                new Color(_championAccentColor.r, _championAccentColor.g, _championAccentColor.b, 0f),
                new Vector3(0.22f, 0.22f, 1f),
                new Vector3(0.88f, 0.88f, 1f),
                -rotationSpeed * 0.72f);
        }

        private void CreateChampionBurstLayer(
            string name,
            Sprite sprite,
            Color startColor,
            Color endColor,
            Vector3 startScale,
            Vector3 endScale,
            float rotationSpeed)
        {
            GameObject burstObject = new(name);
            burstObject.transform.SetParent(transform, false);
            burstObject.transform.localPosition = _championMarkerBaseLocalPosition;
            burstObject.transform.localRotation = Quaternion.Euler(0f, 0f, _championMarkerStyle == ChampionMarkerStyle.Bulwark ? 45f : 0f);
            burstObject.transform.localScale = startScale;
            burstObject.layer = gameObject.layer;

            SpriteRenderer spriteRenderer = burstObject.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = sprite;
            spriteRenderer.color = startColor;
            spriteRenderer.sortingOrder = 33;

            _championBursts.Add(new ChampionBurst
            {
                Transform = burstObject.transform,
                Renderer = spriteRenderer,
                StartScale = startScale,
                EndScale = endScale,
                StartColor = startColor,
                EndColor = endColor,
                SpawnTime = Time.time,
                Lifetime = championBurstLifetime,
                RotationSpeed = rotationSpeed
            });
        }

        private void UpdateChampionBursts()
        {
            if (_championBursts.Count == 0)
            {
                return;
            }

            float now = Time.time;

            for (int i = _championBursts.Count - 1; i >= 0; i--)
            {
                ChampionBurst burst = _championBursts[i];
                if (burst.Transform == null || burst.Renderer == null)
                {
                    _championBursts.RemoveAt(i);
                    continue;
                }

                float normalized = Mathf.Clamp01((now - burst.SpawnTime) / Mathf.Max(0.05f, burst.Lifetime));
                burst.Transform.localScale = Vector3.LerpUnclamped(burst.StartScale, burst.EndScale, normalized);
                burst.Transform.localRotation *= Quaternion.Euler(0f, 0f, burst.RotationSpeed * Time.deltaTime);
                burst.Renderer.color = Color.LerpUnclamped(burst.StartColor, burst.EndColor, normalized);

                if (normalized >= 1f)
                {
                    Destroy(burst.Transform.gameObject);
                    _championBursts.RemoveAt(i);
                }
            }
        }

        private void ClearChampionBursts()
        {
            for (int i = _championBursts.Count - 1; i >= 0; i--)
            {
                if (_championBursts[i].Transform != null)
                {
                    Destroy(_championBursts[i].Transform.gameObject);
                }
            }

            _championBursts.Clear();
        }

        private void SetChampionMarkerVisible(bool visible)
        {
            if (_championMarkerRoot != null)
            {
                _championMarkerRoot.gameObject.SetActive(visible);
            }
        }

        private void SetBulwarkGuardVisible(bool visible)
        {
            if (_bulwarkGuardRoot != null)
            {
                _bulwarkGuardRoot.gameObject.SetActive(visible);
            }
        }

        private static ChampionMarkerStyle ResolveChampionMarkerStyle(string variantId)
        {
            if (string.IsNullOrWhiteSpace(variantId))
            {
                return ChampionMarkerStyle.None;
            }

            return variantId.Trim().ToLowerInvariant() switch
            {
                "fast" => ChampionMarkerStyle.Swift,
                "tank" => ChampionMarkerStyle.Bulwark,
                "explosive" => ChampionMarkerStyle.Volatile,
                _ => ChampionMarkerStyle.None
            };
        }

        private static Sprite GetChampionFallbackSprite()
        {
            if (s_championFallbackSprite != null)
            {
                return s_championFallbackSprite;
            }

            Texture2D texture = new(1, 1, TextureFormat.RGBA32, false)
            {
                hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                name = "ChampionFallbackSprite"
            };
            texture.SetPixel(0, 0, Color.white);
            texture.Apply(false, true);

            s_championFallbackSprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
            s_championFallbackSprite.hideFlags = HideFlags.HideAndDontSave;
            return s_championFallbackSprite;
        }

        private void CacheHitFeedbackScale()
        {
            if (hitFeedbackRoot == null || _hasInitialHitFeedbackScale)
            {
                return;
            }

            _initialHitFeedbackScale = hitFeedbackRoot.localScale;
            _hasInitialHitFeedbackScale = true;
        }

        private void CacheAuthoredPalette()
        {
            _authoredBaseColor = baseColor;
            _authoredHitFlashColor = hitFlashColor;
            _authoredDamagedColor = damagedColor;
            _authoredDeadColor = deadColor;
            _hasAuthoredPalette = true;
        }

        private void CacheAuthoredBodySprite()
        {
            if (bodySpriteRenderer == null)
            {
                return;
            }

            _authoredBodySprite = bodySpriteRenderer.sprite;
            _hasAuthoredBodySprite = true;
        }

        private void CacheBaselineVisualRootScale()
        {
            Transform scaleRoot = visualRoot != null ? visualRoot : transform;

            if (scaleRoot == null)
            {
                return;
            }

            _baselineVisualRootScale = scaleRoot.localScale;
            _hasBaselineVisualRootScale = true;
        }

        private void CacheAuthoredVisualRootScale()
        {
            Transform scaleRoot = visualRoot != null ? visualRoot : transform;

            if (scaleRoot == null)
            {
                return;
            }

            _authoredVisualRootScale = scaleRoot.localScale;
            _hasAuthoredVisualRootScale = true;
        }

        private void RestoreAuthoredPresentation()
        {
            SetTemporaryLowHealthPreviewVisible(false);

            if (_hasAuthoredBodySprite && bodySpriteRenderer != null)
            {
                bodySpriteRenderer.sprite = _authoredBodySprite;
            }

            if (_hasAuthoredPalette)
            {
                baseColor = _authoredBaseColor;
                hitFlashColor = _authoredHitFlashColor;
                damagedColor = _authoredDamagedColor;
                deadColor = _authoredDeadColor;
            }

            Transform scaleRoot = visualRoot != null ? visualRoot : transform;

            if (scaleRoot != null && _hasAuthoredVisualRootScale)
            {
                scaleRoot.localScale = _authoredVisualRootScale;
            }

            if (hitFeedbackRoot != null && _hasInitialHitFeedbackScale)
            {
                hitFeedbackRoot.localScale = _initialHitFeedbackScale;
            }
        }

        private void ApplyHitScalePunch()
        {
            CacheHitFeedbackScale();

            if (!_hasInitialHitFeedbackScale || hitFeedbackRoot == null)
            {
                return;
            }

            hitFeedbackRoot.localScale = _initialHitFeedbackScale * hitScaleMultiplier;
        }

        private void RecoverHitScale()
        {
            if (!_hasInitialHitFeedbackScale || hitFeedbackRoot == null)
            {
                return;
            }

            hitFeedbackRoot.localScale = Vector3.MoveTowards(
                hitFeedbackRoot.localScale,
                _initialHitFeedbackScale,
                hitScaleRecoverSpeed * Time.deltaTime);
        }

        private void ApplyFacing(Vector2 moveDirection)
        {
            if (moveDirection.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            switch (facingMode)
            {
                case FacingMode.FlipX:
                    if (bodySpriteRenderer != null)
                    {
                        bodySpriteRenderer.flipX = moveDirection.x < -0.01f;
                    }
                    break;
                case FacingMode.RotateVisualRoot:
                    Transform targetRoot = facingRoot != null ? facingRoot : visualRoot;
                    if (targetRoot != null)
                    {
                        float angle = Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg;
                        targetRoot.localRotation = Quaternion.Euler(0f, 0f, angle);
                    }
                    break;
            }
        }

        private void UpdateAnimatorMove(Vector2 moveDirection)
        {
            if (bodyAnimator == null)
            {
                return;
            }

            SetAnimatorFloat(moveXParameter, moveDirection.x);
            SetAnimatorFloat(moveYParameter, moveDirection.y);
            SetAnimatorFloat(speedParameter, moveDirection.sqrMagnitude);
        }

        private void ApplyBodyColor(Color color)
        {
            SpriteRenderer targetRenderer = hitFlashTarget != null ? hitFlashTarget : bodySpriteRenderer;

            if (targetRenderer == null)
            {
                if (!_warnedMissingRenderer)
                {
                    Debug.LogWarning("EnemyVisual has no SpriteRenderer assigned. Visual feedback will be skipped.", this);
                    _warnedMissingRenderer = true;
                }

                return;
            }

            targetRenderer.color = color;
        }

        private void SetAnimatorFloat(string parameterName, float value)
        {
            if (bodyAnimator == null || string.IsNullOrWhiteSpace(parameterName))
            {
                return;
            }

            bodyAnimator.SetFloat(parameterName, value);
        }

        private void SetAnimatorInteger(string parameterName, int value)
        {
            if (bodyAnimator == null || string.IsNullOrWhiteSpace(parameterName))
            {
                return;
            }

            bodyAnimator.SetInteger(parameterName, value);
        }

        private void SetAnimatorBool(string parameterName, bool value)
        {
            if (bodyAnimator == null || string.IsNullOrWhiteSpace(parameterName))
            {
                return;
            }

            bodyAnimator.SetBool(parameterName, value);
        }

        private void SetAnimatorTrigger(string parameterName)
        {
            if (bodyAnimator == null || string.IsNullOrWhiteSpace(parameterName))
            {
                return;
            }

            bodyAnimator.SetTrigger(parameterName);
        }

        private void Reset()
        {
            ResolveReferences();
            CacheAuthoredBodySprite();
            CacheBaselineVisualRootScale();
            CacheAuthoredPalette();
            CacheAuthoredVisualRootScale();
            CacheHitFeedbackScale();
        }

        private void OnValidate()
        {
            ResolveReferences();
            CacheAuthoredBodySprite();
            CacheBaselineVisualRootScale();
            CacheAuthoredPalette();
            CacheAuthoredVisualRootScale();
            CacheHitFeedbackScale();
        }
    }
}
