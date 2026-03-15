using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using CuteIssac.Core.Pooling;
using CuteIssac.Core.Feedback;
using CuteIssac.Room;

namespace CuteIssac.UI
{
    [DisallowMultipleComponent]
    public sealed class FloatingFeedbackView : MonoBehaviour, IUiModalDismissible
    {
        private static Sprite s_fallbackSprite;
        private static readonly Regex s_numericTokenRegex = new(@"[-+]?\d+(?:[\.,]\d+)?", RegexOptions.Compiled);

        [Header("Fallback Presentation")]
        [SerializeField] private TextMesh textMesh;
        [SerializeField] [Min(0)] private int sortingOrder = 620;
        [SerializeField] private bool minimalTextOnly = true;
        [SerializeField] private SpriteRenderer glowSpriteRenderer;
        [SerializeField] private SpriteRenderer backdropSpriteRenderer;
        [SerializeField] private SpriteRenderer accentSpriteRenderer;
        [SerializeField] private SpriteRenderer shadowSpriteRenderer;
        [SerializeField] [Min(0f)] private float lateralDrift = 0.18f;
        [SerializeField] [Min(0f)] private float popScaleMultiplier = 1.16f;
        [SerializeField] [Min(1)] private int fallbackFontSize = 92;
        [SerializeField] [Min(0.01f)] private float fallbackCharacterSize = 0.18f;
        [SerializeField] [Min(1)] private int enemyDamageFontSize = 62;
        [SerializeField] [Min(1)] private int playerDamageFontSize = 64;
        [SerializeField] [Min(1)] private int pickupFontSize = 68;
        [SerializeField] [Min(1)] private int eventLabelFontSize = 56;
        [SerializeField] [Min(0.01f)] private float enemyDamageCharacterSize = 0.092f;
        [SerializeField] [Min(0.01f)] private float playerDamageCharacterSize = 0.096f;
        [SerializeField] [Min(0.01f)] private float pickupCharacterSize = 0.142f;
        [SerializeField] [Min(0.01f)] private float eventLabelCharacterSize = 0.124f;
        [SerializeField] [Min(0.5f)] private float enemyDamageLineSpacing = 0.9f;
        [SerializeField] [Min(0.5f)] private float playerDamageLineSpacing = 0.86f;
        [SerializeField] [Min(0.5f)] private float pickupLineSpacing = 0.98f;
        [SerializeField] [Min(0.5f)] private float eventLabelLineSpacing = 0.92f;
        [SerializeField] [Min(1)] private int challengeBaselineBadgeFontSize = 40;
        [SerializeField] [Min(1)] private int challengePaceBadgeFontSize = 42;
        [SerializeField] [Min(1)] private int eliteWarningBadgeFontSize = 44;
        [SerializeField] [Min(1)] private int challengeBaselineMainFontSize = 70;
        [SerializeField] [Min(1)] private int challengePaceMainFontSize = 74;
        [SerializeField] [Min(1)] private int eliteWarningMainFontSize = 78;
        [SerializeField] [Min(0.01f)] private float challengeBaselineCharacterSize = 0.138f;
        [SerializeField] [Min(0.01f)] private float challengePaceCharacterSize = 0.142f;
        [SerializeField] [Min(0.01f)] private float eliteWarningCharacterSize = 0.148f;
        [SerializeField] [Min(0.5f)] private float challengeBaselineLineSpacing = 0.92f;
        [SerializeField] [Min(0.5f)] private float challengePaceLineSpacing = 0.88f;
        [SerializeField] [Min(0.5f)] private float eliteWarningLineSpacing = 0.84f;
        [SerializeField] private Vector2 fallbackGlowSize = new(1.62f, 0.72f);
        [SerializeField] private Vector2 fallbackBackdropSize = new(1.46f, 0.6f);
        [SerializeField] private Vector2 fallbackAccentSize = new(1.24f, 0.1f);
        [SerializeField] private Vector2 fallbackShadowSize = new(1.18f, 0.42f);
        [SerializeField] private Vector2 enemyDamageGlowSize = new(1.92f, 0.84f);
        [SerializeField] private Vector2 enemyDamageBackdropSize = new(1.72f, 0.68f);
        [SerializeField] private Vector2 enemyDamageAccentSize = new(1.38f, 0.12f);
        [SerializeField] private Vector2 enemyDamageShadowSize = new(1.34f, 0.5f);
        [SerializeField] private Vector2 playerDamageGlowSize = new(2f, 0.92f);
        [SerializeField] private Vector2 playerDamageBackdropSize = new(1.82f, 0.74f);
        [SerializeField] private Vector2 playerDamageAccentSize = new(1.46f, 0.13f);
        [SerializeField] private Vector2 playerDamageShadowSize = new(1.42f, 0.54f);
        [SerializeField] private Vector2 pickupGlowSize = new(1.12f, 0.5f);
        [SerializeField] private Vector2 pickupBackdropSize = new(1.02f, 0.42f);
        [SerializeField] private Vector2 pickupAccentSize = new(0.84f, 0.08f);
        [SerializeField] private Vector2 pickupShadowSize = new(0.9f, 0.28f);
        [SerializeField] private Vector2 eventLabelGlowSize = new(0.9f, 0.38f);
        [SerializeField] private Vector2 eventLabelBackdropSize = new(0.8f, 0.3f);
        [SerializeField] private Vector2 eventLabelAccentSize = new(0.68f, 0.04f);
        [SerializeField] private Vector2 eventLabelShadowSize = new(0.74f, 0.2f);
        [SerializeField] private Color enemyDamageBackdropColor = new(0.14f, 0.08f, 0.05f, 0.96f);
        [SerializeField] private Color enemyDamageGlowColor = new(1f, 0.88f, 0.36f, 0.3f);
        [SerializeField] private Color playerDamageBackdropColor = new(0.2f, 0.06f, 0.06f, 0.98f);
        [SerializeField] private Color playerDamageGlowColor = new(1f, 0.42f, 0.36f, 0.34f);
        [SerializeField] private Color pickupBackdropColor = new(0.06f, 0.15f, 0.1f, 0.92f);
        [SerializeField] private Color pickupGlowColor = new(0.48f, 1f, 0.72f, 0.24f);
        [SerializeField] private Color eventLabelBackdropColor = new(0.1f, 0.1f, 0.08f, 0.84f);
        [SerializeField] private Color eventLabelGlowColor = new(1f, 0.9f, 0.48f, 0.16f);
        [SerializeField] private Color enemyDamageTextColor = new(1f, 0.34f, 0.34f, 1f);
        [SerializeField] private Color playerDamageTextColor = new(1f, 0.34f, 0.34f, 1f);
        [SerializeField] private Color pickupTextColor = new(0.86f, 1f, 0.9f, 1f);
        [SerializeField] private Color eventLabelTextColor = new(1f, 0.96f, 0.82f, 1f);
        [SerializeField] [Range(0f, 1f)] private float enemyDamageAccentAlpha = 0.98f;
        [SerializeField] [Range(0f, 1f)] private float playerDamageAccentAlpha = 1f;
        [SerializeField] [Range(0f, 1f)] private float pickupAccentAlpha = 0.9f;
        [SerializeField] [Range(0f, 1f)] private float eventLabelAccentAlpha = 0.58f;
        [SerializeField] [Range(0f, 1f)] private float enemyDamageShadowAlpha = 0.48f;
        [SerializeField] [Range(0f, 1f)] private float playerDamageShadowAlpha = 0.56f;
        [SerializeField] [Range(0f, 1f)] private float pickupShadowAlpha = 0.34f;
        [SerializeField] [Range(0f, 1f)] private float eventLabelShadowAlpha = 0.22f;
        [SerializeField] [Min(0f)] private float enemyDamagePulseAmplitude = 1.18f;
        [SerializeField] [Min(0f)] private float playerDamagePulseAmplitude = 1.28f;
        [SerializeField] [Min(0f)] private float pickupPulseAmplitude = 0.92f;
        [SerializeField] [Min(0f)] private float eventLabelPulseAmplitude = 0.64f;
        [SerializeField] [Min(0f)] private float enemyDamagePulseFrequency = 1.1f;
        [SerializeField] [Min(0f)] private float playerDamagePulseFrequency = 1.18f;
        [SerializeField] [Min(0f)] private float pickupPulseFrequency = 0.94f;
        [SerializeField] [Min(0f)] private float eventLabelPulseFrequency = 0.82f;
        [SerializeField] private Vector2 challengeBaselineGlowSize = new(1.58f, 0.68f);
        [SerializeField] private Vector2 challengePaceGlowSize = new(1.68f, 0.72f);
        [SerializeField] private Vector2 eliteWarningGlowSize = new(1.8f, 0.78f);
        [SerializeField] private Vector2 challengeBaselineBackdropSize = new(1.42f, 0.58f);
        [SerializeField] private Vector2 challengePaceBackdropSize = new(1.52f, 0.62f);
        [SerializeField] private Vector2 eliteWarningBackdropSize = new(1.64f, 0.68f);
        [SerializeField] private Vector2 challengeBaselineAccentSize = new(1.24f, 0.09f);
        [SerializeField] private Vector2 challengePaceAccentSize = new(1.34f, 0.095f);
        [SerializeField] private Vector2 eliteWarningAccentSize = new(1.46f, 0.1f);
        [SerializeField] private Vector2 challengeBaselineShadowSize = new(1.22f, 0.4f);
        [SerializeField] private Vector2 challengePaceShadowSize = new(1.3f, 0.44f);
        [SerializeField] private Vector2 eliteWarningShadowSize = new(1.4f, 0.48f);
        [SerializeField] private float challengeBaselineTextOffsetY = 0.03f;
        [SerializeField] private float challengePaceTextOffsetY = 0.04f;
        [SerializeField] private float eliteWarningTextOffsetY = 0.05f;
        [SerializeField] private float challengeBaselineAccentOffsetY = -0.18f;
        [SerializeField] private float challengePaceAccentOffsetY = -0.195f;
        [SerializeField] private float eliteWarningAccentOffsetY = -0.21f;
        [SerializeField] private Color challengeBaselineBadgeColor = new(1f, 0.92f, 0.72f, 1f);
        [SerializeField] private Color challengePaceBadgeColor = new(1f, 0.9f, 0.62f, 1f);
        [SerializeField] private Color eliteWarningBadgeColor = new(1f, 0.82f, 0.76f, 1f);
        [SerializeField] private Color challengeBaselineBackdropColor = new(0.2f, 0.13f, 0.08f, 0.94f);
        [SerializeField] private Color challengePaceBackdropColor = new(0.22f, 0.16f, 0.1f, 0.96f);
        [SerializeField] private Color eliteWarningBackdropColor = new(0.24f, 0.08f, 0.06f, 0.98f);
        [SerializeField] private Color challengeBaselineGlowColor = new(1f, 0.78f, 0.34f, 0.22f);
        [SerializeField] private Color challengePaceGlowColor = new(1f, 0.84f, 0.38f, 0.24f);
        [SerializeField] private Color eliteWarningGlowColor = new(1f, 0.5f, 0.24f, 0.28f);

        private Vector3 _startPosition;
        private Vector3 _targetPosition;
        private float _lifetime = 0.6f;
        private float _elapsed;
        private float _startScale = 1f;
        private Color _baseColor = Color.white;
        private Vector3 _popScale = Vector3.one;
        private bool _hasChallengeMetadata;
        private FloatingFeedbackVisualProfile _visualProfile = FloatingFeedbackVisualProfile.Default;
        private string _challengeBadgeLabel = string.Empty;
        private ChallengeThreatStage _challengeStage = ChallengeThreatStage.Baseline;
        private ChallengeBannerLayoutProfile _challengeLayoutProfile = ChallengeBannerLayoutProfile.None;
        private Color _badgeColor = Color.white;
        private Color _backdropBaseColor = new(0.04f, 0.05f, 0.09f, 0.86f);
        private Color _glowBaseColor = new(1f, 1f, 1f, 0.16f);
        private float _pulseFrequencyScale = 1f;
        private float _pulseAmplitudeScale = 1f;
        private bool _hasCachedBaseLayoutMetrics;
        private Vector3 _baseTextLocalPosition;
        private Vector3 _baseGlowLocalPosition;
        private Vector3 _baseBackdropLocalPosition;
        private Vector3 _baseAccentLocalPosition;
        private Vector3 _baseShadowLocalPosition;
        private Vector2 _baseGlowSize;
        private Vector2 _baseBackdropSize;
        private Vector2 _baseAccentSize;
        private Vector2 _baseShadowSize;
        private float _baseCharacterSize;
        private float _baseLineSpacing;

        private void Awake()
        {
            ResolveReferences();
            CacheBaseLayoutMetrics();
        }

        private void OnEnable()
        {
            UiModalDismissRegistry.Register(this);
        }

        private void OnDisable()
        {
            UiModalDismissRegistry.Unregister(this);
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;
            float normalized = Mathf.Clamp01(_elapsed / _lifetime);
            float eased = 1f - Mathf.Pow(1f - normalized, 2f);
            float popT = normalized < 0.28f
                ? Mathf.Sin((normalized / 0.28f) * Mathf.PI * 0.5f)
                : 1f - Mathf.Clamp01((normalized - 0.28f) / 0.72f);

            transform.position = Vector3.LerpUnclamped(_startPosition, _targetPosition, eased);
            transform.localScale = Vector3.Lerp(Vector3.one * _startScale, _popScale, popT);

            ApplyAlpha(1f - normalized);
            ApplyPulse(normalized);

            if (normalized >= 1f)
            {
                PrefabPoolService.Return(gameObject);
            }
        }

        public void Initialize(FloatingFeedbackRequest request)
        {
            ResolveReferences();

            _startPosition = transform.position;
            _targetPosition = _startPosition + Vector3.up * request.RiseDistance;
            _lifetime = Mathf.Max(0.1f, request.Lifetime);
            _elapsed = 0f;
            _startScale = Mathf.Max(0.1f, request.StartScale);
            _baseColor = request.TextColor;
            _popScale = Vector3.one * Mathf.Max(1f, _startScale * popScaleMultiplier);
            _hasChallengeMetadata = !minimalTextOnly && request.HasChallengeMetadata;
            _visualProfile = request.VisualProfile;
            _challengeBadgeLabel = _hasChallengeMetadata ? request.ChallengeBadgeLabel : string.Empty;
            _challengeStage = request.ChallengeStage;
            _challengeLayoutProfile = _hasChallengeMetadata
                ? request.ChallengeLayoutProfile
                : ChallengeBannerLayoutProfile.None;
            if (_challengeLayoutProfile == ChallengeBannerLayoutProfile.None && _hasChallengeMetadata)
            {
                _challengeLayoutProfile = ChallengeThreatPresentationResolver.ResolveBannerLayoutProfile(_challengeBadgeLabel);
            }
            _badgeColor = ResolveBadgeColor();
            _backdropBaseColor = ResolveBackdropColor();
            _glowBaseColor = ResolveGlowColor();
            _pulseFrequencyScale = _hasChallengeMetadata
                ? ChallengeThreatPresentationResolver.ResolveFeedbackPulseFrequencyScale(_challengeBadgeLabel, _challengeStage)
                : ResolveStandardPulseFrequency();
            _pulseAmplitudeScale = _hasChallengeMetadata
                ? ChallengeThreatPresentationResolver.ResolveFeedbackPulseAmplitudeScale(_challengeBadgeLabel, _challengeStage)
                : ResolveStandardPulseAmplitude();

            if (_visualProfile == FloatingFeedbackVisualProfile.Pickup)
            {
                _lifetime = Mathf.Min(_lifetime, 0.7f);
            }
            else if (_visualProfile == FloatingFeedbackVisualProfile.EventLabel)
            {
                _lifetime = Mathf.Min(_lifetime, 0.45f);
            }

            transform.localScale = Vector3.one * _startScale;
            ApplyLayoutProfile();
            if (!_hasChallengeMetadata)
            {
                textMesh.fontSize = ResolveStandardFontSize();
            }
            textMesh.richText = !minimalTextOnly;
            textMesh.text = string.Empty;
            EnsureMainTextMeshVisible();

            if (minimalTextOnly)
            {
                ResetMinimalTextPresentation();
            }

            string resolvedDisplayText = minimalTextOnly
                ? ResolveMinimalDisplayText(request.Text)
                : _hasChallengeMetadata
                    ? BuildChallengeDisplayText(request.Text)
                    : BuildStandardDisplayText(request.Text);

            if (string.IsNullOrWhiteSpace(resolvedDisplayText))
            {
                PrefabPoolService.Return(gameObject);
                return;
            }

            textMesh.text = resolvedDisplayText;
            textMesh.color = ResolveTextColor();
            _targetPosition.x += Random.Range(-lateralDrift, lateralDrift);
            ApplyBaseColors();
        }

        public void DismissForModal()
        {
            PrefabPoolService.Return(gameObject);
        }

        private void ResolveReferences()
        {
            if (textMesh == null)
            {
                textMesh = GetComponentInChildren<TextMesh>(true);
            }

            if (textMesh == null)
            {
                GameObject labelObject = new("Label");
                labelObject.transform.SetParent(transform, false);
                textMesh = labelObject.AddComponent<TextMesh>();
                textMesh.anchor = TextAnchor.MiddleCenter;
                textMesh.alignment = TextAlignment.Center;
                textMesh.characterSize = fallbackCharacterSize;
                textMesh.fontSize = fallbackFontSize;
                textMesh.richText = true;
                LocalizedUiFontProvider.ApplyNumericWorld(textMesh);
            }

            MeshRenderer renderer = textMesh.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.sortingOrder = sortingOrder;
                renderer.enabled = true;
                if (textMesh.font != null)
                {
                    renderer.sharedMaterial = textMesh.font.material;
                }
            }

            if (minimalTextOnly)
            {
                DisableRenderer(glowSpriteRenderer);
                DisableRenderer(backdropSpriteRenderer);
                DisableRenderer(accentSpriteRenderer);
                DisableRenderer(shadowSpriteRenderer);
                LocalizedUiFontProvider.ApplyNumericWorld(textMesh);
                EnsureMainTextMeshVisible();
                SuppressSupplementalChildrenForMinimalMode();
                return;
            }

            if (glowSpriteRenderer == null)
            {
                GameObject glowObject = new("Glow");
                glowObject.transform.SetParent(transform, false);
                glowObject.transform.localPosition = new Vector3(0f, 0.03f, 0.06f);
                glowSpriteRenderer = glowObject.AddComponent<SpriteRenderer>();
                glowSpriteRenderer.sprite = GetFallbackSprite();
                glowSpriteRenderer.drawMode = SpriteDrawMode.Sliced;
                glowSpriteRenderer.size = fallbackGlowSize;
                glowSpriteRenderer.sortingOrder = sortingOrder - 3;
            }

            if (backdropSpriteRenderer == null)
            {
                GameObject backdropObject = new("Backdrop");
                backdropObject.transform.SetParent(transform, false);
                backdropObject.transform.localPosition = new Vector3(0f, 0.02f, 0.04f);
                backdropSpriteRenderer = backdropObject.AddComponent<SpriteRenderer>();
                backdropSpriteRenderer.sprite = GetFallbackSprite();
                backdropSpriteRenderer.drawMode = SpriteDrawMode.Sliced;
                backdropSpriteRenderer.size = fallbackBackdropSize;
                backdropSpriteRenderer.sortingOrder = sortingOrder - 2;
            }

            if (accentSpriteRenderer == null)
            {
                GameObject accentObject = new("Accent");
                accentObject.transform.SetParent(transform, false);
                accentObject.transform.localPosition = new Vector3(0f, -0.16f, 0.03f);
                accentSpriteRenderer = accentObject.AddComponent<SpriteRenderer>();
                accentSpriteRenderer.sprite = GetFallbackSprite();
                accentSpriteRenderer.drawMode = SpriteDrawMode.Sliced;
                accentSpriteRenderer.size = fallbackAccentSize;
                accentSpriteRenderer.sortingOrder = sortingOrder - 1;
            }

            if (shadowSpriteRenderer == null)
            {
                GameObject shadowObject = new("Shadow");
                shadowObject.transform.SetParent(transform, false);
                shadowObject.transform.localPosition = new Vector3(0f, -0.02f, 0.02f);
                shadowSpriteRenderer = shadowObject.AddComponent<SpriteRenderer>();
                shadowSpriteRenderer.sprite = GetFallbackSprite();
                shadowSpriteRenderer.drawMode = SpriteDrawMode.Sliced;
                shadowSpriteRenderer.size = fallbackShadowSize;
                shadowSpriteRenderer.sortingOrder = sortingOrder - 1;
            }
        }

        private void CacheBaseLayoutMetrics()
        {
            if (_hasCachedBaseLayoutMetrics)
            {
                return;
            }

            if (textMesh != null)
            {
                _baseTextLocalPosition = textMesh.transform.localPosition;
                _baseCharacterSize = textMesh.characterSize;
                _baseLineSpacing = textMesh.lineSpacing;
            }

            if (glowSpriteRenderer != null)
            {
                _baseGlowLocalPosition = glowSpriteRenderer.transform.localPosition;
                _baseGlowSize = glowSpriteRenderer.size;
            }

            if (backdropSpriteRenderer != null)
            {
                _baseBackdropLocalPosition = backdropSpriteRenderer.transform.localPosition;
                _baseBackdropSize = backdropSpriteRenderer.size;
            }

            if (accentSpriteRenderer != null)
            {
                _baseAccentLocalPosition = accentSpriteRenderer.transform.localPosition;
                _baseAccentSize = accentSpriteRenderer.size;
            }

            if (shadowSpriteRenderer != null)
            {
                _baseShadowLocalPosition = shadowSpriteRenderer.transform.localPosition;
                _baseShadowSize = shadowSpriteRenderer.size;
            }

            _hasCachedBaseLayoutMetrics = true;
        }

        private void ApplyLayoutProfile()
        {
            CacheBaseLayoutMetrics();

            if (!_hasCachedBaseLayoutMetrics)
            {
                return;
            }

            if (textMesh != null)
            {
                Vector3 textLocalPosition = _baseTextLocalPosition;
                textLocalPosition.y += ResolveTextOffsetY();
                textMesh.transform.localPosition = textLocalPosition;
                textMesh.characterSize = ResolveCharacterSize();
                textMesh.lineSpacing = ResolveLineSpacing();
            }

            if (glowSpriteRenderer != null)
            {
                glowSpriteRenderer.transform.localPosition = _baseGlowLocalPosition;
                glowSpriteRenderer.size = ResolveGlowSize();
            }

            if (backdropSpriteRenderer != null)
            {
                backdropSpriteRenderer.transform.localPosition = _baseBackdropLocalPosition;
                backdropSpriteRenderer.size = ResolveBackdropSize();
            }

            if (accentSpriteRenderer != null)
            {
                Vector3 accentLocalPosition = _baseAccentLocalPosition;
                accentLocalPosition.y = ResolveAccentOffsetY();
                accentSpriteRenderer.transform.localPosition = accentLocalPosition;
                accentSpriteRenderer.size = ResolveAccentSize();
            }

            if (shadowSpriteRenderer != null)
            {
                shadowSpriteRenderer.transform.localPosition = _baseShadowLocalPosition;
                shadowSpriteRenderer.size = ResolveShadowSize();
            }
        }

        private void ApplyAlpha(float alpha)
        {
            if (textMesh == null)
            {
                return;
            }

            Color nextColor = ResolveTextColor();
            nextColor.a *= Mathf.Clamp01(alpha);
            textMesh.color = nextColor;

            if (glowSpriteRenderer != null)
            {
                Color glowColor = _glowBaseColor;
                glowColor.a *= Mathf.Clamp01(alpha);
                glowSpriteRenderer.color = glowColor;
            }

            if (backdropSpriteRenderer != null)
            {
                Color backdropColor = _backdropBaseColor;
                backdropColor.a *= Mathf.Clamp01(alpha);
                backdropSpriteRenderer.color = backdropColor;
            }

            if (accentSpriteRenderer != null)
            {
                Color accentColor = _baseColor;
                accentColor.a *= ResolveAccentAlpha() * Mathf.Clamp01(alpha);
                accentSpriteRenderer.color = accentColor;
            }

            if (shadowSpriteRenderer != null)
            {
                Color shadowColor = shadowSpriteRenderer.color;
                shadowColor.a = ResolveShadowAlpha() * Mathf.Clamp01(alpha);
                shadowSpriteRenderer.color = shadowColor;
            }
        }

        private void ApplyBaseColors()
        {
            if (glowSpriteRenderer != null)
            {
                glowSpriteRenderer.color = _glowBaseColor;
            }

            if (backdropSpriteRenderer != null)
            {
                backdropSpriteRenderer.color = _backdropBaseColor;
            }

            if (accentSpriteRenderer != null)
            {
                accentSpriteRenderer.color = _baseColor;
            }

            if (shadowSpriteRenderer == null)
            {
                return;
            }

            shadowSpriteRenderer.color = new Color(0f, 0f, 0f, ResolveShadowAlpha());
        }

        private void ApplyPulse(float normalized)
        {
            float pulse = 1f + Mathf.Sin(normalized * Mathf.PI * 4f * _pulseFrequencyScale) * 0.04f * _pulseAmplitudeScale;

            if (glowSpriteRenderer != null)
            {
                glowSpriteRenderer.transform.localScale = new Vector3(pulse * 1.08f, pulse * (1.16f + ((_pulseAmplitudeScale - 1f) * 0.18f)), 1f);
            }

            if (backdropSpriteRenderer != null)
            {
                backdropSpriteRenderer.transform.localScale = new Vector3(1f + (pulse - 1f) * 0.35f, 1f + (pulse - 1f) * 0.08f, 1f);
            }
        }

        private string BuildChallengeDisplayText(string text)
        {
            string badgeColorHex = ColorUtility.ToHtmlStringRGBA(_badgeColor);
            string mainColorHex = ColorUtility.ToHtmlStringRGBA(Color.Lerp(_baseColor, Color.white, 0.16f));
            int badgeFontSize = ResolveChallengeBadgeFontSize();
            int mainFontSize = ResolveChallengeMainFontSize();
            return
                $"<size={badgeFontSize}><color=#{badgeColorHex}><b>{_challengeBadgeLabel}</b></color></size>\n" +
                $"<size={mainFontSize}><color=#{mainColorHex}><b>{text}</b></color></size>";
        }

        private string BuildStandardDisplayText(string text)
        {
            if (_visualProfile == FloatingFeedbackVisualProfile.EventLabel)
            {
                text = FloatingFeedbackLabelUtility.NormalizeEventLabel(text, "EVENT");
                if (text.Length > 10)
                {
                    text = text.Substring(0, 9).TrimEnd() + "…";
                }
            }
            else if (_visualProfile == FloatingFeedbackVisualProfile.Pickup)
            {
                text = text.Replace('\n', ' ').Replace('\r', ' ').Trim();
                if (text.Length > 16)
                {
                    text = text.Substring(0, 15).TrimEnd() + "…";
                }
            }

            Color textColor = ResolveTextColor();
            string textColorHex = ColorUtility.ToHtmlStringRGBA(textColor);
            return $"<size={ResolveStandardFontSize()}><b><color=#{textColorHex}>{text}</color></b></size>";
        }

        private string ResolveMinimalDisplayText(string text)
        {
            string normalizedText = BuildPlainDisplayText(text);

            if (string.IsNullOrWhiteSpace(normalizedText))
            {
                return string.Empty;
            }

            if (_visualProfile != FloatingFeedbackVisualProfile.EnemyDamage
                && _visualProfile != FloatingFeedbackVisualProfile.PlayerDamage)
            {
                return string.Empty;
            }

            return TryNormalizeDamageText(normalizedText, out string damageText)
                ? EnsureDamageSign(damageText)
                : string.Empty;
        }

        private string BuildPlainDisplayText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }

            return NormalizeWhitespace(StripRichTextTags(text));
        }

        private static string StripRichTextTags(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }

            StringBuilder builder = new(text.Length);
            bool insideTag = false;

            for (int index = 0; index < text.Length; index++)
            {
                char character = text[index];

                if (character == '<')
                {
                    insideTag = true;
                    continue;
                }

                if (character == '>')
                {
                    insideTag = false;
                    continue;
                }

                if (!insideTag)
                {
                    builder.Append(character);
                }
            }

            return builder.ToString();
        }

        private static string NormalizeWhitespace(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }

            StringBuilder builder = new(text.Length);
            bool pendingWhitespace = false;

            for (int index = 0; index < text.Length; index++)
            {
                char character = text[index];

                if (char.IsWhiteSpace(character))
                {
                    pendingWhitespace = builder.Length > 0;
                    continue;
                }

                if (pendingWhitespace)
                {
                    builder.Append(' ');
                    pendingWhitespace = false;
                }

                builder.Append(character);
            }

            return builder.ToString().Trim();
        }

        private static bool TryNormalizeDamageText(string text, out string normalizedText)
        {
            normalizedText = string.Empty;

            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            string trimmed = text.Trim();

            if (int.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out int integerValue))
            {
                normalizedText = integerValue.ToString(CultureInfo.InvariantCulture);
                return true;
            }

            if (!TryParseNumericToken(trimmed, out float floatValue))
            {
                return false;
            }

            int roundedValue = floatValue < 0f
                ? -Mathf.CeilToInt(Mathf.Abs(floatValue))
                : Mathf.CeilToInt(floatValue);
            normalizedText = roundedValue.ToString(CultureInfo.InvariantCulture);
            return true;
        }

        private static string EnsureDamageSign(string damageText)
        {
            if (string.IsNullOrWhiteSpace(damageText))
            {
                return string.Empty;
            }

            string trimmed = damageText.Trim();
            if (!int.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value))
            {
                return trimmed.StartsWith("-", System.StringComparison.Ordinal) ? trimmed : $"-{trimmed}";
            }

            int absoluteValue = Mathf.Abs(value);
            return $"-{absoluteValue.ToString(CultureInfo.InvariantCulture)}";
        }

        private static bool TryParseNumericToken(string text, out float value)
        {
            value = 0f;

            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            if (float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
            {
                return true;
            }

            Match match = s_numericTokenRegex.Match(text);
            if (!match.Success)
            {
                return false;
            }

            string numericToken = match.Value.Replace(',', '.');
            return float.TryParse(numericToken, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }

        private int ResolveChallengeBadgeFontSize()
        {
            return _challengeLayoutProfile switch
            {
                ChallengeBannerLayoutProfile.Pace => challengePaceBadgeFontSize,
                ChallengeBannerLayoutProfile.Baseline => challengeBaselineBadgeFontSize,
                ChallengeBannerLayoutProfile.EliteWarning => eliteWarningBadgeFontSize,
                _ => challengeBaselineBadgeFontSize
            };
        }

        private int ResolveChallengeMainFontSize()
        {
            return _challengeLayoutProfile switch
            {
                ChallengeBannerLayoutProfile.Pace => challengePaceMainFontSize,
                ChallengeBannerLayoutProfile.Baseline => challengeBaselineMainFontSize,
                ChallengeBannerLayoutProfile.EliteWarning => eliteWarningMainFontSize,
                _ => challengeBaselineMainFontSize
            };
        }

        private float ResolveCharacterSize()
        {
            if (!_hasChallengeMetadata)
            {
                return ResolveStandardCharacterSize();
            }

            return _challengeLayoutProfile switch
            {
                ChallengeBannerLayoutProfile.Pace => challengePaceCharacterSize,
                ChallengeBannerLayoutProfile.Baseline => challengeBaselineCharacterSize,
                ChallengeBannerLayoutProfile.EliteWarning => eliteWarningCharacterSize,
                _ => ResolveStandardCharacterSize()
            };
        }

        private float ResolveLineSpacing()
        {
            if (!_hasChallengeMetadata)
            {
                return ResolveStandardLineSpacing();
            }

            return _challengeLayoutProfile switch
            {
                ChallengeBannerLayoutProfile.Pace => challengePaceLineSpacing,
                ChallengeBannerLayoutProfile.Baseline => challengeBaselineLineSpacing,
                ChallengeBannerLayoutProfile.EliteWarning => eliteWarningLineSpacing,
                _ => ResolveStandardLineSpacing()
            };
        }

        private Vector2 ResolveGlowSize()
        {
            if (!_hasChallengeMetadata)
            {
                return ResolveStandardGlowSize();
            }

            return _challengeLayoutProfile switch
            {
                ChallengeBannerLayoutProfile.Pace => challengePaceGlowSize,
                ChallengeBannerLayoutProfile.Baseline => challengeBaselineGlowSize,
                ChallengeBannerLayoutProfile.EliteWarning => eliteWarningGlowSize,
                _ => ResolveStandardGlowSize()
            };
        }

        private Vector2 ResolveBackdropSize()
        {
            if (!_hasChallengeMetadata)
            {
                return ResolveStandardBackdropSize();
            }

            return _challengeLayoutProfile switch
            {
                ChallengeBannerLayoutProfile.Pace => challengePaceBackdropSize,
                ChallengeBannerLayoutProfile.Baseline => challengeBaselineBackdropSize,
                ChallengeBannerLayoutProfile.EliteWarning => eliteWarningBackdropSize,
                _ => ResolveStandardBackdropSize()
            };
        }

        private Vector2 ResolveAccentSize()
        {
            if (!_hasChallengeMetadata)
            {
                return ResolveStandardAccentSize();
            }

            return _challengeLayoutProfile switch
            {
                ChallengeBannerLayoutProfile.Pace => challengePaceAccentSize,
                ChallengeBannerLayoutProfile.Baseline => challengeBaselineAccentSize,
                ChallengeBannerLayoutProfile.EliteWarning => eliteWarningAccentSize,
                _ => ResolveStandardAccentSize()
            };
        }

        private Vector2 ResolveShadowSize()
        {
            if (!_hasChallengeMetadata)
            {
                return ResolveStandardShadowSize();
            }

            return _challengeLayoutProfile switch
            {
                ChallengeBannerLayoutProfile.Pace => challengePaceShadowSize,
                ChallengeBannerLayoutProfile.Baseline => challengeBaselineShadowSize,
                ChallengeBannerLayoutProfile.EliteWarning => eliteWarningShadowSize,
                _ => ResolveStandardShadowSize()
            };
        }

        private float ResolveTextOffsetY()
        {
            return _challengeLayoutProfile switch
            {
                ChallengeBannerLayoutProfile.Pace => challengePaceTextOffsetY,
                ChallengeBannerLayoutProfile.Baseline => challengeBaselineTextOffsetY,
                ChallengeBannerLayoutProfile.EliteWarning => eliteWarningTextOffsetY,
                _ => 0f
            };
        }

        private float ResolveAccentOffsetY()
        {
            return _challengeLayoutProfile switch
            {
                ChallengeBannerLayoutProfile.Pace => challengePaceAccentOffsetY,
                ChallengeBannerLayoutProfile.Baseline => challengeBaselineAccentOffsetY,
                ChallengeBannerLayoutProfile.EliteWarning => eliteWarningAccentOffsetY,
                _ => _baseAccentLocalPosition.y
            };
        }

        private Color ResolveBadgeColor()
        {
            if (!_hasChallengeMetadata)
            {
                return _baseColor;
            }

            if (string.Equals(_challengeBadgeLabel, "도전 페이스"))
            {
                return Color.Lerp(challengePaceBadgeColor, _baseColor, 0.2f);
            }

            if (string.Equals(_challengeBadgeLabel, "챌린지"))
            {
                return Color.Lerp(challengeBaselineBadgeColor, _baseColor, 0.14f);
            }

            return Color.Lerp(eliteWarningBadgeColor, _baseColor, 0.22f);
        }

        private Color ResolveBackdropColor()
        {
            if (!_hasChallengeMetadata)
            {
                return _visualProfile switch
                {
                    FloatingFeedbackVisualProfile.EnemyDamage => enemyDamageBackdropColor,
                    FloatingFeedbackVisualProfile.PlayerDamage => playerDamageBackdropColor,
                    FloatingFeedbackVisualProfile.Pickup => pickupBackdropColor,
                    FloatingFeedbackVisualProfile.EventLabel => eventLabelBackdropColor,
                    _ => new Color(0.04f, 0.05f, 0.09f, 0.86f)
                };
            }

            if (string.Equals(_challengeBadgeLabel, "도전 페이스"))
            {
                return Color.Lerp(challengePaceBackdropColor, _baseColor, 0.08f);
            }

            if (string.Equals(_challengeBadgeLabel, "챌린지"))
            {
                return Color.Lerp(challengeBaselineBackdropColor, _baseColor, 0.08f);
            }

            return Color.Lerp(eliteWarningBackdropColor, _baseColor, 0.12f);
        }

        private Color ResolveGlowColor()
        {
            if (!_hasChallengeMetadata)
            {
                return _visualProfile switch
                {
                    FloatingFeedbackVisualProfile.EnemyDamage => enemyDamageGlowColor,
                    FloatingFeedbackVisualProfile.PlayerDamage => playerDamageGlowColor,
                    FloatingFeedbackVisualProfile.Pickup => pickupGlowColor,
                    FloatingFeedbackVisualProfile.EventLabel => eventLabelGlowColor,
                    _ => new Color(_baseColor.r, _baseColor.g, _baseColor.b, 0.16f)
                };
            }

            if (string.Equals(_challengeBadgeLabel, "도전 페이스"))
            {
                return Color.Lerp(challengePaceGlowColor, _baseColor, 0.18f);
            }

            if (string.Equals(_challengeBadgeLabel, "챌린지"))
            {
                return Color.Lerp(challengeBaselineGlowColor, _baseColor, 0.14f);
            }

            return Color.Lerp(eliteWarningGlowColor, _baseColor, 0.24f);
        }

        private Color ResolveTextColor()
        {
            if (_hasChallengeMetadata)
            {
                return Color.white;
            }

            return _visualProfile switch
            {
                FloatingFeedbackVisualProfile.EnemyDamage => enemyDamageTextColor,
                FloatingFeedbackVisualProfile.PlayerDamage => playerDamageTextColor,
                FloatingFeedbackVisualProfile.Pickup => pickupTextColor,
                FloatingFeedbackVisualProfile.EventLabel => eventLabelTextColor,
                _ => _baseColor
            };
        }

        private float ResolveAccentAlpha()
        {
            if (_hasChallengeMetadata)
            {
                return 0.92f;
            }

            return _visualProfile switch
            {
                FloatingFeedbackVisualProfile.EnemyDamage => enemyDamageAccentAlpha,
                FloatingFeedbackVisualProfile.PlayerDamage => playerDamageAccentAlpha,
                FloatingFeedbackVisualProfile.Pickup => pickupAccentAlpha,
                FloatingFeedbackVisualProfile.EventLabel => eventLabelAccentAlpha,
                _ => 0.92f
            };
        }

        private float ResolveShadowAlpha()
        {
            if (_hasChallengeMetadata)
            {
                return 0.22f;
            }

            return _visualProfile switch
            {
                FloatingFeedbackVisualProfile.EnemyDamage => enemyDamageShadowAlpha,
                FloatingFeedbackVisualProfile.PlayerDamage => playerDamageShadowAlpha,
                FloatingFeedbackVisualProfile.Pickup => pickupShadowAlpha,
                FloatingFeedbackVisualProfile.EventLabel => eventLabelShadowAlpha,
                _ => 0.22f
            };
        }

        private int ResolveStandardFontSize()
        {
            return _visualProfile switch
            {
                FloatingFeedbackVisualProfile.EnemyDamage => enemyDamageFontSize,
                FloatingFeedbackVisualProfile.PlayerDamage => playerDamageFontSize,
                FloatingFeedbackVisualProfile.Pickup => pickupFontSize,
                FloatingFeedbackVisualProfile.EventLabel => eventLabelFontSize,
                _ => fallbackFontSize
            };
        }

        private float ResolveStandardCharacterSize()
        {
            return _visualProfile switch
            {
                FloatingFeedbackVisualProfile.EnemyDamage => enemyDamageCharacterSize,
                FloatingFeedbackVisualProfile.PlayerDamage => playerDamageCharacterSize,
                FloatingFeedbackVisualProfile.Pickup => pickupCharacterSize,
                FloatingFeedbackVisualProfile.EventLabel => eventLabelCharacterSize,
                _ => Mathf.Max(_baseCharacterSize > 0f ? _baseCharacterSize : 0f, fallbackCharacterSize)
            };
        }

        private float ResolveStandardLineSpacing()
        {
            return _visualProfile switch
            {
                FloatingFeedbackVisualProfile.EnemyDamage => enemyDamageLineSpacing,
                FloatingFeedbackVisualProfile.PlayerDamage => playerDamageLineSpacing,
                FloatingFeedbackVisualProfile.Pickup => pickupLineSpacing,
                FloatingFeedbackVisualProfile.EventLabel => eventLabelLineSpacing,
                _ => Mathf.Min(_baseLineSpacing > 0f ? _baseLineSpacing : 1f, 0.96f)
            };
        }

        private Vector2 ResolveStandardGlowSize()
        {
            return _visualProfile switch
            {
                FloatingFeedbackVisualProfile.EnemyDamage => enemyDamageGlowSize,
                FloatingFeedbackVisualProfile.PlayerDamage => playerDamageGlowSize,
                FloatingFeedbackVisualProfile.Pickup => pickupGlowSize,
                FloatingFeedbackVisualProfile.EventLabel => eventLabelGlowSize,
                _ => _baseGlowSize
            };
        }

        private Vector2 ResolveStandardBackdropSize()
        {
            return _visualProfile switch
            {
                FloatingFeedbackVisualProfile.EnemyDamage => enemyDamageBackdropSize,
                FloatingFeedbackVisualProfile.PlayerDamage => playerDamageBackdropSize,
                FloatingFeedbackVisualProfile.Pickup => pickupBackdropSize,
                FloatingFeedbackVisualProfile.EventLabel => eventLabelBackdropSize,
                _ => _baseBackdropSize
            };
        }

        private Vector2 ResolveStandardAccentSize()
        {
            return _visualProfile switch
            {
                FloatingFeedbackVisualProfile.EnemyDamage => enemyDamageAccentSize,
                FloatingFeedbackVisualProfile.PlayerDamage => playerDamageAccentSize,
                FloatingFeedbackVisualProfile.Pickup => pickupAccentSize,
                FloatingFeedbackVisualProfile.EventLabel => eventLabelAccentSize,
                _ => _baseAccentSize
            };
        }

        private Vector2 ResolveStandardShadowSize()
        {
            return _visualProfile switch
            {
                FloatingFeedbackVisualProfile.EnemyDamage => enemyDamageShadowSize,
                FloatingFeedbackVisualProfile.PlayerDamage => playerDamageShadowSize,
                FloatingFeedbackVisualProfile.Pickup => pickupShadowSize,
                FloatingFeedbackVisualProfile.EventLabel => eventLabelShadowSize,
                _ => _baseShadowSize
            };
        }

        private float ResolveStandardPulseAmplitude()
        {
            return _visualProfile switch
            {
                FloatingFeedbackVisualProfile.EnemyDamage => enemyDamagePulseAmplitude,
                FloatingFeedbackVisualProfile.PlayerDamage => playerDamagePulseAmplitude,
                FloatingFeedbackVisualProfile.Pickup => pickupPulseAmplitude,
                FloatingFeedbackVisualProfile.EventLabel => eventLabelPulseAmplitude,
                _ => 1f
            };
        }

        private float ResolveStandardPulseFrequency()
        {
            return _visualProfile switch
            {
                FloatingFeedbackVisualProfile.EnemyDamage => enemyDamagePulseFrequency,
                FloatingFeedbackVisualProfile.PlayerDamage => playerDamagePulseFrequency,
                FloatingFeedbackVisualProfile.Pickup => pickupPulseFrequency,
                FloatingFeedbackVisualProfile.EventLabel => eventLabelPulseFrequency,
                _ => 1f
            };
        }

        private static Sprite GetFallbackSprite()
        {
            if (s_fallbackSprite != null)
            {
                return s_fallbackSprite;
            }

            Texture2D texture = Texture2D.whiteTexture;
            s_fallbackSprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), texture.width);
            return s_fallbackSprite;
        }

        private static void DisableRenderer(Renderer renderer)
        {
            if (renderer != null)
            {
                renderer.enabled = false;
            }
        }

        private void SuppressSupplementalChildrenForMinimalMode()
        {
            TextMesh[] textMeshes = GetComponentsInChildren<TextMesh>(true);
            for (int index = 0; index < textMeshes.Length; index++)
            {
                TextMesh candidate = textMeshes[index];

                if (candidate == null || candidate == textMesh)
                {
                    continue;
                }

                candidate.gameObject.SetActive(false);
            }

            SpriteRenderer[] spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
            for (int index = 0; index < spriteRenderers.Length; index++)
            {
                if (spriteRenderers[index] != null)
                {
                    spriteRenderers[index].enabled = false;
                }
            }
        }

        private void ResetMinimalTextPresentation()
        {
            if (textMesh == null)
            {
                return;
            }

            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.fontStyle = FontStyle.Normal;
            textMesh.fontSize = Mathf.Clamp(ResolveStandardFontSize(), 52, 68);
            textMesh.characterSize = Mathf.Clamp(ResolveStandardCharacterSize(), 0.07f, 0.1f);
            textMesh.lineSpacing = 1f;
            textMesh.offsetZ = 0f;
            textMesh.transform.localPosition = Vector3.zero;
            textMesh.transform.localScale = Vector3.one;
            LocalizedUiFontProvider.ApplyNumericWorld(textMesh);
            EnsureMainTextMeshVisible();
        }

        private void EnsureMainTextMeshVisible()
        {
            if (textMesh == null)
            {
                return;
            }

            if (!textMesh.gameObject.activeSelf)
            {
                textMesh.gameObject.SetActive(true);
            }

            MeshRenderer renderer = textMesh.GetComponent<MeshRenderer>();
            if (renderer == null)
            {
                return;
            }

            renderer.enabled = true;
            renderer.sortingOrder = sortingOrder;

            if (textMesh.font != null)
            {
                renderer.sharedMaterial = textMesh.font.material;
            }
        }
    }
}
