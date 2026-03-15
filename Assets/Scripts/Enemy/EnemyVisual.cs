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
        private Color _authoredBaseColor = Color.white;
        private Color _authoredHitFlashColor = Color.white;
        private Color _authoredDamagedColor = Color.white;
        private Color _authoredDeadColor = Color.white;
        private bool _hasAuthoredPalette;
        private Vector3 _authoredVisualRootScale = Vector3.one;
        private bool _hasAuthoredVisualRootScale;
        private static Sprite s_championFallbackSprite;
        private bool _championVisualActive;
        private ChampionMarkerStyle _championMarkerStyle;
        private Color _championAccentColor = Color.white;
        private Transform _championMarkerRoot;
        private SpriteRenderer _championAuraRenderer;
        private SpriteRenderer _championCoreRenderer;
        private SpriteRenderer _championGlyphRenderer;
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
            if (visualSet == null)
            {
                return;
            }

            ResolveReferences();

            baseColor = visualSet.BaseColor;
            hitFlashColor = visualSet.HitFlashColor;
            damagedColor = visualSet.DamagedColor;
            deadColor = visualSet.DeadColor;
            CacheAuthoredPalette();

            if (bodySpriteRenderer != null && visualSet.BodySprite != null)
            {
                bodySpriteRenderer.sprite = visualSet.BodySprite;
            }

            if (_damagedFlashRemaining <= 0f)
            {
                ApplyBodyColor(baseColor);
            }
        }

        public void ApplyChampionPresentation(string variantId, Color accentColor, float colorBlend, float scaleMultiplier)
        {
            CacheAuthoredPalette();
            CacheAuthoredVisualRootScale();

            float blend = Mathf.Clamp01(colorBlend);
            baseColor = Color.Lerp(_authoredBaseColor, accentColor, blend);
            damagedColor = Color.Lerp(_authoredDamagedColor, accentColor, blend * 0.35f);
            deadColor = Color.Lerp(_authoredDeadColor, accentColor, blend * 0.18f);
            deadColor.a = _authoredDeadColor.a;

            Transform scaleRoot = visualRoot != null ? visualRoot : transform;

            if (scaleRoot != null && _hasAuthoredVisualRootScale)
            {
                scaleRoot.localScale = _authoredVisualRootScale * Mathf.Max(0.5f, scaleMultiplier);
            }

            _championVisualActive = true;
            _championAccentColor = accentColor;
            _championMarkerStyle = ResolveChampionMarkerStyle(variantId);
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

        public void ResetChampionPresentation()
        {
            RestoreAuthoredPresentation();
            _championVisualActive = false;
            _championMarkerStyle = ChampionMarkerStyle.None;
            _championAccentColor = Color.white;
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

            Transform target = facingRoot != null ? facingRoot : visualRoot;

            if (target == null)
            {
                return;
            }

            switch (facingMode)
            {
                case FacingMode.FlipX:
                    if (Mathf.Abs(moveDirection.x) > 0.0001f)
                    {
                        Vector3 localScale = target.localScale;
                        localScale.x = Mathf.Abs(localScale.x) * Mathf.Sign(moveDirection.x);
                        target.localScale = localScale;
                    }

                    break;
                case FacingMode.RotateVisualRoot:
                    float angle = Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg;
                    target.localRotation = Quaternion.Euler(0f, 0f, angle);
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
            CacheHitFeedbackScale();
        }

        private void OnValidate()
        {
            ResolveReferences();
            CacheHitFeedbackScale();
        }
    }
}
