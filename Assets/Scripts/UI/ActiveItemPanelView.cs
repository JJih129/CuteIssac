using UnityEngine;
using UnityEngine.UI;
using CuteIssac.Player;
using CuteIssac.Room;

namespace CuteIssac.UI
{
    /// <summary>
    /// Presentation-only active item slot.
    /// The current prototype reserves the layout and placeholder state so a skinned active item system can plug in later.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ActiveItemPanelView : MonoBehaviour
    {
        [Header("Optional Root")]
        [Tooltip("Optional. Root object for the entire active item panel.")]
        [SerializeField] private GameObject panelRoot;

        [Header("Skinnable Elements")]
        [Tooltip("Optional frame image for the active item slot.")]
        [SerializeField] private Image frameImage;
        [Tooltip("Optional icon image for the active item itself.")]
        [SerializeField] private Image iconImage;
        [Tooltip("Optional slider used by skins that prefer a fill bar for charge.")]
        [SerializeField] private Slider chargeSlider;
        [Tooltip("Optional image fill used by skins that prefer a radial or horizontal charge overlay.")]
        [SerializeField] private Image chargeFillImage;
        [Tooltip("Optional fallback label. Useful when the slot has no icon yet.")]
        [SerializeField] private Text labelText;

        [Header("Top Bar Layout")]
        [SerializeField] private Vector2 topBarIconSize = new(42f, 42f);
        [SerializeField] private Vector2 compactTopBarIconSize = new(104f, 104f);
        [SerializeField] [Min(0f)] private float topBarPadding = 10f;
        [SerializeField] [Min(0f)] private float topBarBackgroundTopInset = 8f;
        [SerializeField] [Min(0f)] private float compactTopBarBackgroundTopInset = 12f;
        [SerializeField] [Min(0f)] private float compactTopBarSidePadding = 7f;
        [SerializeField] [Min(24f)] private float topBarBackgroundHeight = 86f;
        [SerializeField] [Min(24f)] private float compactTopBarBackgroundHeight = 156f;
        [SerializeField] [Min(0f)] private float compactTopBarIconTopInset = 20f;
        [SerializeField] [Min(0f)] private float compactTopBarLabelTopInset = 12f;
        [SerializeField] [Min(20f)] private float compactTopBarLabelHeight = 32f;
        [SerializeField] [Min(0f)] private float compactTopBarChargeBottomInset = 6f;
        [SerializeField] [Min(0f)] private float compactTopBarTextGap = 12f;
        [SerializeField] [Min(0)] private int topBarLabelFontSize = 18;
        [SerializeField] [Min(0)] private int topBarTitleFontSize = 10;
        [SerializeField] [Min(0)] private int topBarValueFontSize = 20;
        [SerializeField] [Min(0)] private int compactTopBarTitleFontSize = 11;
        [SerializeField] [Min(0)] private int compactTopBarValueFontSize = 18;
        [SerializeField] [Min(10f)] private float topBarChargeHeight = 16f;
        [SerializeField] [Min(10f)] private float compactTopBarChargeHeight = 20f;
        [SerializeField] [Min(0.5f)] private float compactTopBarLineSpacing = 0.88f;

        [Header("Styling")]
        [SerializeField] private Color titleTextColor = new(0.92f, 0.95f, 1f, 0.62f);
        [SerializeField] private Color placeholderTextColor = new(1f, 1f, 1f, 0.68f);
        [SerializeField] private Color challengeBaselineTitleColor = new(1f, 0.94f, 0.82f, 0.72f);
        [SerializeField] private Color challengeBaselineValueColor = new(1f, 0.88f, 0.62f, 1f);
        [SerializeField] private Color frameTint = new(0.1f, 0.18f, 0.24f, 0.18f);
        [SerializeField] private Color readyAccent = new(1f, 0.82f, 0.42f, 1f);
        [SerializeField] private Color cooldownAccent = new(0.5f, 0.85f, 1f, 1f);
        [SerializeField] private Color timedAccent = new(0.58f, 1f, 0.74f, 1f);
        [SerializeField] [Range(0.5f, 1.2f)] private float compactFrameAlphaScale = 0.84f;
        [SerializeField] [Range(0.5f, 1.2f)] private float compactTitleAlphaScale = 1.08f;
        [SerializeField] [Range(0.5f, 1.2f)] private float compactValueAlphaScale = 1.1f;
        [SerializeField] [Range(0.5f, 1.2f)] private float compactChargeAlphaScale = 1.06f;
        [SerializeField] [Range(0f, 1f)] private float challengeFrameTintStrength = 0.54f;
        [SerializeField] [Range(0f, 1f)] private float challengeValueTintStrength = 0.62f;
        [SerializeField] [Range(0f, 1f)] private float challengeIconTintStrength = 0.3f;
        [SerializeField] private Color compactTextShadowColor = new(0.02f, 0.05f, 0.08f, 0.74f);
        [SerializeField] private Vector2 compactTextShadowDistance = new(1.25f, -1.25f);

        private bool _compactTopBarMode;
        private bool _warnedMissingPresentationRef;
        private bool _hasChallengeThreatTheme;
        private Color _challengeThreatAccentColor = Color.white;
        private string _challengeThreatBadgeLabel = string.Empty;
        private ChallengeThreatStage _challengeThreatStage;
        private string _currentTitle = "액티브 슬롯";
        private string _currentValue = "비어 있음";
        private Color _currentValueColor = Color.white;
        private Color _currentFrameBaseColor;
        private Color _currentIconBaseColor = Color.white;
        private Color _currentChargeBaseColor;

        public void ConfigureDebugView(Text label, Image frame = null)
        {
            labelText = label;
            frameImage = frame;
            panelRoot = label != null && label.transform.parent != null
                ? label.transform.parent.gameObject
                : panelRoot;
        }

        public void ShowPlaceholder()
        {
            if (panelRoot != null)
            {
                panelRoot.SetActive(true);
            }

            if (labelText == null && iconImage == null && chargeSlider == null && chargeFillImage == null && !_warnedMissingPresentationRef)
            {
                Debug.LogWarning("ActiveItemPanelView has no skinnable references assigned. The placeholder slot will stay logical-only until at least one view reference is connected.", this);
                _warnedMissingPresentationRef = true;
            }

            if (iconImage != null)
            {
                iconImage.enabled = false;
            }

            if (labelText != null)
            {
                _currentTitle = "액티브 슬롯";
                _currentValue = "비어 있음";
                _currentValueColor = placeholderTextColor;
                labelText.text = FormatLabel(_currentTitle, _currentValue, ResolveValueColor(Time.unscaledTime));
                labelText.color = placeholderTextColor;
                labelText.supportRichText = true;
            }

            if (chargeSlider != null)
            {
                chargeSlider.value = 0f;
            }

            if (chargeFillImage != null)
            {
                chargeFillImage.fillAmount = 0f;
                _currentChargeBaseColor = cooldownAccent;
                chargeFillImage.color = ResolveChargeColor(Time.unscaledTime);
            }

            if (frameImage != null)
            {
                _currentFrameBaseColor = frameTint;
                frameImage.color = ResolveFrameColor(Time.unscaledTime);
            }

            if (iconImage != null)
            {
                _currentIconBaseColor = Color.white;
            }

            ApplyThreatTheme(Time.unscaledTime);
        }

        public void HidePanel()
        {
            if (panelRoot != null)
            {
                panelRoot.SetActive(false);
            }
        }

        public void SetConsumableSlot(PlayerConsumableSlotState slotState)
        {
            if (panelRoot != null)
            {
                panelRoot.SetActive(true);
            }

            if (!slotState.HasConsumable && !slotState.HasActiveTimedEffect)
            {
                HidePanel();
                return;
            }

            if (iconImage != null)
            {
                iconImage.enabled = slotState.Icon != null;
                iconImage.sprite = slotState.Icon;
                _currentIconBaseColor = slotState.HasConsumable ? Color.white : new Color(1f, 1f, 1f, 0.2f);
                iconImage.color = ResolveIconColor(Time.unscaledTime);
            }

            if (labelText != null)
            {
                _currentTitle = slotState.HasConsumable ? "소모 아이템" : "버프 상태";
                _currentValue = slotState.HasConsumable ? slotState.DisplayName : "진행 중";
                _currentValueColor = slotState.HasConsumable ? Color.white : timedAccent;
                labelText.text = FormatLabel(_currentTitle, _currentValue, ResolveValueColor(Time.unscaledTime));
                labelText.color = Color.white;
                labelText.supportRichText = true;
            }

            if (chargeSlider != null)
            {
                chargeSlider.value = slotState.HasActiveTimedEffect ? slotState.TimedEffectNormalizedRemaining : 0f;
            }

            if (chargeFillImage != null)
            {
                chargeFillImage.fillAmount = slotState.HasActiveTimedEffect ? slotState.TimedEffectNormalizedRemaining : 0f;
                _currentChargeBaseColor = slotState.HasActiveTimedEffect ? timedAccent : cooldownAccent;
                chargeFillImage.color = ResolveChargeColor(Time.unscaledTime);
            }

            if (frameImage != null)
            {
                _currentFrameBaseColor = slotState.HasActiveTimedEffect
                    ? new Color(timedAccent.r * 0.35f, timedAccent.g * 0.35f, timedAccent.b * 0.35f, 0.28f)
                    : frameTint;
                frameImage.color = ResolveFrameColor(Time.unscaledTime);
            }

            ApplyThreatTheme(Time.unscaledTime);
        }

        public void SetActiveItemSlot(PlayerActiveItemSlotState slotState)
        {
            if (panelRoot != null)
            {
                panelRoot.SetActive(true);
            }

            if (!slotState.HasItem)
            {
                HidePanel();
                return;
            }

            if (iconImage != null)
            {
                iconImage.enabled = slotState.Icon != null;
                iconImage.sprite = slotState.Icon;
                _currentIconBaseColor = Color.white;
                iconImage.color = ResolveIconColor(Time.unscaledTime);
            }

            if (labelText != null)
            {
                Color accent = slotState.HasTimedEffect
                    ? timedAccent
                    : (slotState.CanUse ? readyAccent : cooldownAccent);
                _currentTitle = slotState.HasTimedEffect
                    ? "지속 효과"
                    : (slotState.CanUse ? "사용 가능" : "충전 중");
                _currentValue = slotState.DisplayName;
                _currentValueColor = accent;
                labelText.text = FormatLabel(_currentTitle, _currentValue, ResolveValueColor(Time.unscaledTime));
                labelText.color = Color.white;
                labelText.supportRichText = true;
            }

            if (chargeSlider != null)
            {
                chargeSlider.value = slotState.HasTimedEffect
                    ? slotState.TimedEffectNormalizedRemaining
                    : slotState.ChargeNormalized;
            }

            if (chargeFillImage != null)
            {
                bool hasTimedEffect = slotState.HasTimedEffect;
                chargeFillImage.fillAmount = hasTimedEffect
                    ? slotState.TimedEffectNormalizedRemaining
                    : slotState.ChargeNormalized;
                _currentChargeBaseColor = hasTimedEffect
                    ? timedAccent
                    : (slotState.CanUse ? readyAccent : cooldownAccent);
                chargeFillImage.color = ResolveChargeColor(Time.unscaledTime);
            }

            if (frameImage != null)
            {
                Color accent = slotState.HasTimedEffect
                    ? timedAccent
                    : (slotState.CanUse ? readyAccent : cooldownAccent);
                _currentFrameBaseColor = new Color(accent.r * 0.34f, accent.g * 0.34f, accent.b * 0.34f, 0.28f);
                frameImage.color = ResolveFrameColor(Time.unscaledTime);
            }

            ApplyThreatTheme(Time.unscaledTime);
        }

        public void ApplyTopBarLayout(bool compactMode)
        {
            _compactTopBarMode = compactMode;
            Vector2 resolvedCompactIconSize = new(
                Mathf.Max(compactTopBarIconSize.x, 104f),
                Mathf.Max(compactTopBarIconSize.y, 104f));
            float resolvedCompactChargeHeight = Mathf.Max(compactTopBarChargeHeight, 20f);
            float resolvedCompactSidePadding = Mathf.Max(compactTopBarSidePadding, 7f);
            float resolvedCompactChargeBottomInset = Mathf.Max(compactTopBarChargeBottomInset, 6f);

            RectTransform rootRect = panelRoot != null
                ? panelRoot.transform as RectTransform
                : transform as RectTransform;

            if (rootRect == null)
            {
                return;
            }

            if (compactMode)
            {
                if (frameImage != null)
                {
                    RectTransform frameRect = frameImage.rectTransform;
                    frameRect.anchorMin = Vector2.zero;
                    frameRect.anchorMax = Vector2.one;
                    frameRect.offsetMin = Vector2.zero;
                    frameRect.offsetMax = Vector2.zero;
                    frameImage.color = ResolveFrameColor(Time.unscaledTime);
                }

                if (iconImage != null)
                {
                    RectTransform iconRect = iconImage.rectTransform;
                    iconRect.anchorMin = new Vector2(0f, 1f);
                    iconRect.anchorMax = new Vector2(0f, 1f);
                    iconRect.pivot = new Vector2(0f, 1f);
                    iconRect.anchoredPosition = new Vector2(0f, 0f);
                    iconRect.sizeDelta = resolvedCompactIconSize;
                }

                if (labelText != null)
                {
                    labelText.gameObject.SetActive(false);
                }

                if (chargeSlider != null)
                {
                    RectTransform sliderRect = chargeSlider.GetComponent<RectTransform>();
                    sliderRect.anchorMin = new Vector2(0f, 0f);
                    sliderRect.anchorMax = new Vector2(1f, 0f);
                    sliderRect.pivot = new Vector2(0.5f, 0f);
                    sliderRect.anchoredPosition = new Vector2(0f, resolvedCompactChargeBottomInset);
                    sliderRect.sizeDelta = new Vector2(-(resolvedCompactSidePadding * 2f), resolvedCompactChargeHeight);
                }

                if (chargeFillImage != null)
                {
                    RectTransform fillRect = chargeFillImage.rectTransform;
                    fillRect.anchorMin = new Vector2(0f, 0f);
                    fillRect.anchorMax = new Vector2(1f, 0f);
                    fillRect.pivot = new Vector2(0.5f, 0f);
                    fillRect.anchoredPosition = new Vector2(0f, resolvedCompactChargeBottomInset);
                    fillRect.sizeDelta = new Vector2(-(resolvedCompactSidePadding * 2f), resolvedCompactChargeHeight);
                }

                return;
            }

            if (labelText != null)
            {
                labelText.gameObject.SetActive(true);
            }

            float width = rootRect.rect.width > 0f ? rootRect.rect.width : rootRect.sizeDelta.x;
            float sidePadding = compactMode ? compactTopBarSidePadding : topBarPadding;
            float backgroundTopInset = compactMode ? compactTopBarBackgroundTopInset : topBarBackgroundTopInset;
            float textGap = compactMode ? compactTopBarTextGap : topBarPadding;
            Vector2 resolvedIconSize = compactMode ? compactTopBarIconSize : topBarIconSize;
            float resolvedChargeHeight = compactMode ? compactTopBarChargeHeight : topBarChargeHeight;
            float labelTopInset = compactMode ? compactTopBarLabelTopInset : (topBarBackgroundTopInset + 4f);
            float labelHeight = compactMode ? compactTopBarLabelHeight : 36f;
            float chargeBottomInset = compactMode ? compactTopBarChargeBottomInset : 8f;
            float backgroundHeight = compactMode ? compactTopBarBackgroundHeight : topBarBackgroundHeight;
            float iconTopInset = compactMode ? compactTopBarIconTopInset : (topBarBackgroundTopInset + 10f);

            if (frameImage != null)
            {
                RectTransform frameRect = frameImage.rectTransform;
                frameRect.anchorMin = new Vector2(0f, 1f);
                frameRect.anchorMax = new Vector2(0f, 1f);
                frameRect.pivot = new Vector2(0f, 1f);
                frameRect.anchoredPosition = new Vector2(0f, -backgroundTopInset);
                frameRect.sizeDelta = new Vector2(width, backgroundHeight);
            }

            if (iconImage != null)
            {
                RectTransform iconRect = iconImage.rectTransform;
                iconRect.anchorMin = new Vector2(0f, 1f);
                iconRect.anchorMax = new Vector2(0f, 1f);
                iconRect.pivot = new Vector2(0f, 1f);
                iconRect.anchoredPosition = new Vector2(sidePadding, -iconTopInset);
                iconRect.sizeDelta = resolvedIconSize;
            }

            if (labelText != null)
            {
                RectTransform labelRect = labelText.rectTransform;
                labelRect.anchorMin = new Vector2(0f, 1f);
                labelRect.anchorMax = new Vector2(0f, 1f);
                labelRect.pivot = new Vector2(0f, 1f);
                labelRect.anchoredPosition = new Vector2(sidePadding + resolvedIconSize.x + textGap, -labelTopInset);
                labelRect.sizeDelta = new Vector2(width - (sidePadding * 2f) - resolvedIconSize.x - textGap, labelHeight);
                labelText.alignment = TextAnchor.MiddleLeft;
                labelText.fontSize = Mathf.Max(labelText.fontSize, topBarLabelFontSize);
                labelText.lineSpacing = compactMode ? compactTopBarLineSpacing : 1f;
                labelText.supportRichText = true;
                ApplyCompactTextShadow(labelText);
            }

            if (chargeSlider != null)
            {
                RectTransform sliderRect = chargeSlider.GetComponent<RectTransform>();
                sliderRect.anchorMin = new Vector2(0f, 0f);
                sliderRect.anchorMax = new Vector2(1f, 0f);
                sliderRect.pivot = new Vector2(0.5f, 0f);
                sliderRect.anchoredPosition = new Vector2(0f, chargeBottomInset);
                sliderRect.sizeDelta = new Vector2(-(sidePadding * 2f), resolvedChargeHeight);
            }

            if (chargeFillImage != null)
            {
                RectTransform fillRect = chargeFillImage.rectTransform;
                fillRect.anchorMin = new Vector2(0f, 0f);
                fillRect.anchorMax = new Vector2(1f, 0f);
                fillRect.pivot = new Vector2(0.5f, 0f);
                fillRect.anchoredPosition = new Vector2(0f, chargeBottomInset);
                fillRect.sizeDelta = new Vector2(-(sidePadding * 2f), resolvedChargeHeight);
            }
        }

        public void SetChallengeThreatTheme(bool active, Color accentColor, string badgeLabel, ChallengeThreatStage stage)
        {
            _hasChallengeThreatTheme = active;
            _challengeThreatAccentColor = accentColor;
            _challengeThreatBadgeLabel = active ? badgeLabel ?? string.Empty : string.Empty;
            _challengeThreatStage = stage;
            ApplyThreatTheme(Time.unscaledTime);
        }

        private void Update()
        {
            if (!_hasChallengeThreatTheme)
            {
                return;
            }

            ApplyThreatTheme(Time.unscaledTime);
        }

        private string FormatLabel(string title, string value)
        {
            return FormatLabel(title, value, Color.white);
        }

        private string FormatLabel(string title, string value, Color valueColor)
        {
            string titleHex = ColorUtility.ToHtmlStringRGBA(ResolveTitleColor(Time.unscaledTime));
            string valueHex = ColorUtility.ToHtmlStringRGBA(valueColor);

            if (_compactTopBarMode)
            {
                return
                    $"<size={compactTopBarTitleFontSize}><color=#{titleHex}>{title}</color></size>\n" +
                    $"<size={compactTopBarValueFontSize}><b><color=#{valueHex}>{value}</color></b></size>";
            }

            return
                $"<size={topBarTitleFontSize}><color=#{titleHex}>{title}</color></size>\n" +
                $"<size={topBarValueFontSize}><b><color=#{valueHex}>{value}</color></b></size>";
        }

        private void ApplyThreatTheme(float unscaledTime)
        {
            if (frameImage != null)
            {
                frameImage.color = ResolveFrameColor(unscaledTime);
            }

            if (iconImage != null)
            {
                iconImage.color = ResolveIconColor(unscaledTime);
            }

            if (chargeFillImage != null)
            {
                chargeFillImage.color = ResolveChargeColor(unscaledTime);
            }

            if (labelText != null)
            {
                labelText.text = FormatLabel(_currentTitle, _currentValue, ResolveValueColor(unscaledTime));
            }
        }

        private Color ResolveFrameColor(float unscaledTime)
        {
            if (!_hasChallengeThreatTheme)
            {
                Color frameColor = _currentFrameBaseColor == default ? frameTint : _currentFrameBaseColor;
                return ApplyCompactAlpha(frameColor, compactFrameAlphaScale);
            }

            Color baseColor = _currentFrameBaseColor == default ? frameTint : _currentFrameBaseColor;
            float pulse = 0.5f + (0.5f * Mathf.Sin(unscaledTime * (1f + ChallengeThreatPresentationResolver.ResolveBannerPulseCycles(_challengeThreatStage))));
            float tint = challengeFrameTintStrength * Mathf.Lerp(0.82f, 1.14f, pulse);
            return ApplyCompactAlpha(Color.Lerp(baseColor, Color.Lerp(baseColor, _challengeThreatAccentColor, 0.48f), Mathf.Clamp01(tint)), compactFrameAlphaScale);
        }

        private Color ResolveChargeColor(float unscaledTime)
        {
            Color baseColor = _currentChargeBaseColor == default ? cooldownAccent : _currentChargeBaseColor;
            if (!_hasChallengeThreatTheme)
            {
                return ApplyCompactAlpha(baseColor, compactChargeAlphaScale);
            }

            float stageStrength = Mathf.Clamp01((ChallengeThreatPresentationResolver.ResolveStatusThemeStrength(_challengeThreatStage) - 0.92f) / 0.33f);
            float pulse = 0.5f + (0.5f * Mathf.Sin(unscaledTime * (1.15f + ChallengeThreatPresentationResolver.ResolveBannerPulseCycles(_challengeThreatStage))));
            float tint = challengeValueTintStrength * Mathf.Lerp(0.74f, 1.08f, stageStrength) * Mathf.Lerp(0.88f, 1.12f, pulse);
            return ApplyCompactAlpha(Color.Lerp(baseColor, Color.Lerp(baseColor, _challengeThreatAccentColor, 0.62f), Mathf.Clamp01(tint)), compactChargeAlphaScale);
        }

        private Color ResolveIconColor(float unscaledTime)
        {
            Color baseColor = _currentIconBaseColor == default ? Color.white : _currentIconBaseColor;
            if (!_hasChallengeThreatTheme)
            {
                return baseColor;
            }

            float pulse = 0.5f + (0.5f * Mathf.Sin(unscaledTime * (0.9f + ChallengeThreatPresentationResolver.ResolveBannerPulseCycles(_challengeThreatStage))));
            float tint = challengeIconTintStrength * Mathf.Lerp(0.84f, 1.14f, pulse);
            return Color.Lerp(baseColor, Color.Lerp(baseColor, _challengeThreatAccentColor, 0.42f), Mathf.Clamp01(tint));
        }

        private Color ResolveTitleColor(float unscaledTime)
        {
            if (!_hasChallengeThreatTheme)
            {
                return ApplyCompactAlpha(titleTextColor, compactTitleAlphaScale);
            }

            float pulse = 0.5f + (0.5f * Mathf.Sin(unscaledTime * (0.82f + ChallengeThreatPresentationResolver.ResolveBannerPulseCycles(_challengeThreatStage))));
            float tint = challengeValueTintStrength * 0.34f * Mathf.Lerp(0.84f, 1.08f, pulse);
            Color baseTitleColor = UsesChallengeBaselineTheme() ? challengeBaselineTitleColor : titleTextColor;
            return ApplyCompactAlpha(Color.Lerp(baseTitleColor, Color.Lerp(Color.white, _challengeThreatAccentColor, 0.18f), Mathf.Clamp01(tint)), compactTitleAlphaScale);
        }

        private Color ResolveValueColor(float unscaledTime)
        {
            if (!_hasChallengeThreatTheme)
            {
                return ApplyCompactAlpha(_currentValueColor, compactValueAlphaScale);
            }

            float stageStrength = Mathf.Clamp01((ChallengeThreatPresentationResolver.ResolveStatusThemeStrength(_challengeThreatStage) - 0.92f) / 0.33f);
            float pulse = 0.5f + (0.5f * Mathf.Sin(unscaledTime * (1.1f + ChallengeThreatPresentationResolver.ResolveBannerPulseCycles(_challengeThreatStage)) * Mathf.PI * 0.5f));
            float tint = challengeValueTintStrength * Mathf.Lerp(0.76f, 1.08f, stageStrength) * Mathf.Lerp(0.88f, 1.14f, pulse);
            Color baseValueColor = UsesChallengeBaselineTheme() ? Color.Lerp(_currentValueColor, challengeBaselineValueColor, 0.42f) : _currentValueColor;
            return ApplyCompactAlpha(Color.Lerp(baseValueColor, Color.Lerp(baseValueColor, _challengeThreatAccentColor, 0.62f), Mathf.Clamp01(tint)), compactValueAlphaScale);
        }

        private Color ApplyCompactAlpha(Color color, float alphaScale)
        {
            if (!_compactTopBarMode)
            {
                return color;
            }

            color.a = Mathf.Clamp01(color.a * alphaScale);
            return color;
        }

        private void ApplyCompactTextShadow(Text text)
        {
            if (text == null)
            {
                return;
            }

            Shadow shadow = text.GetComponent<Shadow>();
            if (_compactTopBarMode && shadow == null)
            {
                shadow = text.gameObject.AddComponent<Shadow>();
            }

            if (shadow == null)
            {
                return;
            }

            shadow.enabled = _compactTopBarMode;
            if (!_compactTopBarMode)
            {
                return;
            }

            shadow.effectColor = compactTextShadowColor;
            shadow.effectDistance = compactTextShadowDistance;
            shadow.useGraphicAlpha = true;
        }

        private bool UsesChallengeBaselineTheme()
        {
            return string.Equals(_challengeThreatBadgeLabel, "챌린지");
        }
    }
}
