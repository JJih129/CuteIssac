using CuteIssac.Data.Item;
using CuteIssac.Room;
using UnityEngine;
using UnityEngine.UI;

namespace CuteIssac.UI
{
    /// <summary>
    /// Presentation-only trinket slot.
    /// It can be fully wired from the inspector, or receive runtime-created fallback references from HUDController.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TrinketPanelView : MonoBehaviour
    {
        [Header("Optional Root")]
        [SerializeField] private GameObject panelRoot;

        [Header("Skinnable Elements")]
        [SerializeField] private Image frameImage;
        [SerializeField] private Image iconImage;
        [SerializeField] private Text titleText;
        [SerializeField] private Text detailText;

        [Header("Colors")]
        [SerializeField] private Color placeholderColor = new(1f, 1f, 1f, 0.7f);
        [SerializeField] private Color commonAccent = new(0.86f, 0.86f, 0.86f, 1f);
        [SerializeField] private Color uncommonAccent = new(0.52f, 0.95f, 0.72f, 1f);
        [SerializeField] private Color rareAccent = new(0.45f, 0.8f, 1f, 1f);
        [SerializeField] private Color legendaryAccent = new(1f, 0.78f, 0.32f, 1f);
        [SerializeField] private Color challengeBaselineCategoryColor = new(1f, 0.94f, 0.82f, 0.72f);
        [SerializeField] private Color challengeBaselineTitleValueColor = new(1f, 0.88f, 0.62f, 1f);
        [SerializeField] private Color challengeBaselineDetailValueColor = new(1f, 0.9f, 0.74f, 0.9f);

        [Header("Top Bar Layout")]
        [SerializeField] private Vector2 topBarIconSize = new(34f, 34f);
        [SerializeField] private Vector2 compactTopBarIconSize = new(36f, 36f);
        [SerializeField] [Min(0f)] private float topBarPadding = 10f;
        [SerializeField] [Min(0f)] private float topBarBackgroundTopInset = 8f;
        [SerializeField] [Min(0f)] private float compactTopBarBackgroundTopInset = 12f;
        [SerializeField] [Min(0f)] private float compactTopBarSidePadding = 12f;
        [SerializeField] [Min(24f)] private float topBarBackgroundHeight = 86f;
        [SerializeField] [Min(24f)] private float compactTopBarBackgroundHeight = 78f;
        [SerializeField] [Min(0f)] private float compactTopBarIconTopInset = 12f;
        [SerializeField] [Min(0f)] private float compactTopBarTitleTopInset = 12f;
        [SerializeField] [Min(0f)] private float compactTopBarDetailTopInset = 33f;
        [SerializeField] [Min(20f)] private float compactTopBarTitleHeight = 22f;
        [SerializeField] [Min(20f)] private float compactTopBarDetailHeight = 24f;
        [SerializeField] [Min(0f)] private float compactTopBarTextGap = 12f;
        [SerializeField] [Min(0)] private int topBarTitleFontSize = 19;
        [SerializeField] [Min(0)] private int topBarCategoryFontSize = 10;
        [SerializeField] [Min(0)] private int topBarDetailFontSize = 14;
        [SerializeField] [Min(0)] private int compactTopBarCategoryFontSize = 10;
        [SerializeField] [Min(0)] private int compactTopBarTitleFontSize = 18;
        [SerializeField] [Min(0)] private int compactTopBarDetailValueFontSize = 13;
        [SerializeField] [Min(0.5f)] private float compactTopBarTitleLineSpacing = 0.88f;
        [SerializeField] [Min(0.5f)] private float compactTopBarDetailLineSpacing = 0.86f;
        [SerializeField] private Color categoryTextColor = new(0.92f, 0.95f, 1f, 0.62f);
        [SerializeField] private Color detailTextColor = new(1f, 1f, 1f, 0.82f);
        [SerializeField] [Range(0.5f, 1.2f)] private float compactFrameAlphaScale = 0.84f;
        [SerializeField] [Range(0.5f, 1.2f)] private float compactCategoryAlphaScale = 1.06f;
        [SerializeField] [Range(0.5f, 1.2f)] private float compactTitleValueAlphaScale = 1.1f;
        [SerializeField] [Range(0.5f, 1.2f)] private float compactDetailValueAlphaScale = 1.08f;
        [SerializeField] [Range(0f, 1f)] private float challengeFrameTintStrength = 0.46f;
        [SerializeField] [Range(0f, 1f)] private float challengeValueTintStrength = 0.58f;
        [SerializeField] [Range(0f, 1f)] private float challengeIconTintStrength = 0.28f;
        [SerializeField] [Range(0f, 1f)] private float challengeDetailTintStrength = 0.34f;
        [SerializeField] private Color compactTitleShadowColor = new(0.02f, 0.05f, 0.08f, 0.76f);
        [SerializeField] private Color compactDetailShadowColor = new(0.02f, 0.05f, 0.08f, 0.66f);
        [SerializeField] private Vector2 compactTextShadowDistance = new(1.2f, -1.2f);

        private bool _compactTopBarMode;
        private bool _warnedMissingRefs;
        private bool _hasChallengeThreatTheme;
        private Color _challengeThreatAccentColor = Color.white;
        private string _challengeThreatBadgeLabel = string.Empty;
        private ChallengeThreatStage _challengeThreatStage;
        private string _currentTitleCategory = "장신구 슬롯";
        private string _currentTitleValue = "비어 있음";
        private string _currentDetailCategory = "상태";
        private string _currentDetailValue = "빈 슬롯";
        private Color _currentFrameBaseColor = new(0f, 0f, 0f, 0.18f);
        private Color _currentIconBaseColor = Color.white;
        private Color _currentTitleValueColor = Color.white;
        private Color _currentDetailValueColor = Color.white;

        public void ConfigureRuntimeView(GameObject root, Image frame, Image icon, Text title, Text detail)
        {
            panelRoot = root;
            frameImage = frame;
            iconImage = icon;
            titleText = title;
            detailText = detail;
        }

        public void ShowPlaceholder()
        {
            if (panelRoot != null)
            {
                panelRoot.SetActive(true);
            }

            if (frameImage == null && iconImage == null && titleText == null && detailText == null && !_warnedMissingRefs)
            {
                Debug.LogWarning("TrinketPanelView has no presentation references assigned. The logical state will update, but nothing can be shown yet.", this);
                _warnedMissingRefs = true;
            }

            if (iconImage != null)
            {
                iconImage.enabled = false;
                iconImage.sprite = null;
                _currentIconBaseColor = Color.white;
                iconImage.color = ResolveIconColor(Time.unscaledTime);
            }

            if (titleText != null)
            {
                titleText.supportRichText = true;
                _currentTitleCategory = "장신구 슬롯";
                _currentTitleValue = "비어 있음";
                _currentTitleValueColor = placeholderColor;
                titleText.text = FormatTitle(_currentTitleCategory, _currentTitleValue, ResolveCategoryColor(Time.unscaledTime), ResolveTitleValueColor(Time.unscaledTime));
                titleText.color = placeholderColor;
            }

            if (detailText != null)
            {
                _currentDetailCategory = "상태";
                _currentDetailValue = "빈 슬롯";
                _currentDetailValueColor = new Color(1f, 1f, 1f, 0.5f);
                detailText.text = FormatDetail(_currentDetailCategory, _currentDetailValue, ResolveCategoryColor(Time.unscaledTime), ResolveDetailValueColor(Time.unscaledTime));
                detailText.color = _currentDetailValueColor;
                detailText.supportRichText = true;
            }

            if (frameImage != null)
            {
                _currentFrameBaseColor = new Color(0f, 0f, 0f, 0.18f);
                frameImage.color = ResolveFrameColor(Time.unscaledTime);
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

        public void SetTrinket(ItemData itemData)
        {
            if (itemData == null)
            {
                HidePanel();
                return;
            }

            if (panelRoot != null)
            {
                panelRoot.SetActive(true);
            }

            Color accent = ResolveAccent(itemData.Rarity);

            if (frameImage != null)
            {
                _currentFrameBaseColor = new Color(accent.r * 0.34f, accent.g * 0.34f, accent.b * 0.34f, 0.26f);
                frameImage.color = ResolveFrameColor(Time.unscaledTime);
            }

            if (iconImage != null)
            {
                iconImage.enabled = itemData.Icon != null;
                iconImage.sprite = itemData.Icon;
                _currentIconBaseColor = Color.white;
                iconImage.color = ResolveIconColor(Time.unscaledTime);
            }

            if (titleText != null)
            {
                titleText.supportRichText = true;
                _currentTitleCategory = "장신구";
                _currentTitleValue = itemData.DisplayName;
                _currentTitleValueColor = accent;
                titleText.text = FormatTitle(_currentTitleCategory, _currentTitleValue, ResolveCategoryColor(Time.unscaledTime), ResolveTitleValueColor(Time.unscaledTime));
                titleText.color = accent;
            }

            if (detailText != null)
            {
                _currentDetailCategory = ResolveCategoryLabel(itemData.ItemCategory);
                if (string.IsNullOrWhiteSpace(_currentDetailCategory))
                {
                    _currentDetailCategory = "설명";
                }

                _currentDetailValue = itemData.Description;
                _currentDetailValueColor = detailTextColor;
                detailText.text = FormatDetail(_currentDetailCategory, _currentDetailValue, ResolveCategoryColor(Time.unscaledTime), ResolveDetailValueColor(Time.unscaledTime));
                detailText.color = detailTextColor;
                detailText.supportRichText = true;
            }

            ApplyThreatTheme(Time.unscaledTime);
        }

        private Color ResolveAccent(ItemRarity rarity)
        {
            return rarity switch
            {
                ItemRarity.Uncommon => uncommonAccent,
                ItemRarity.Rare => rareAccent,
                ItemRarity.Legendary => legendaryAccent,
                _ => commonAccent,
            };
        }

        private static string ResolveCategoryLabel(ItemCategory category)
        {
            return category switch
            {
                ItemCategory.Damage => "\uACF5\uACA9",
                ItemCategory.FireRate => "\uC5F0\uC0AC",
                ItemCategory.Movement => "\uC774\uB3D9",
                ItemCategory.Projectile => "\uD0C4\uD658",
                ItemCategory.Defense => "\uBC29\uC5B4",
                ItemCategory.Utility => "\uC720\uD2F8",
                ItemCategory.Summon => "\uC18C\uD658",
                ItemCategory.Orbital => "\uC624\uBE44\uD0C8",
                ItemCategory.Laser => "\uB808\uC774\uC800",
                ItemCategory.Bomb => "\uD3ED\uD0C4",
                ItemCategory.Luck => "\uD589\uC6B4",
                ItemCategory.Economy => "\uC7AC\uD654",
                _ => string.Empty,
            };
        }

        public void ApplyTopBarLayout(bool compactMode)
        {
            _compactTopBarMode = compactMode;

            RectTransform rootRect = panelRoot != null
                ? panelRoot.transform as RectTransform
                : transform as RectTransform;

            if (rootRect == null)
            {
                return;
            }

            float width = rootRect.rect.width > 0f ? rootRect.rect.width : rootRect.sizeDelta.x;
            float sidePadding = compactMode ? compactTopBarSidePadding : topBarPadding;
            float backgroundTopInset = compactMode ? compactTopBarBackgroundTopInset : topBarBackgroundTopInset;
            float textGap = compactMode ? compactTopBarTextGap : topBarPadding;
            Vector2 resolvedIconSize = compactMode ? compactTopBarIconSize : topBarIconSize;
            float iconTopInset = compactMode ? compactTopBarIconTopInset : (topBarBackgroundTopInset + 8f);
            float titleTopInset = compactMode ? compactTopBarTitleTopInset : (topBarBackgroundTopInset + 6f);
            float detailTopInset = compactMode ? compactTopBarDetailTopInset : (topBarBackgroundTopInset + 34f);
            float backgroundHeight = compactMode ? compactTopBarBackgroundHeight : topBarBackgroundHeight;
            float titleHeight = compactMode ? compactTopBarTitleHeight : 28f;
            float detailHeight = compactMode ? compactTopBarDetailHeight : 24f;

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

            if (titleText != null)
            {
                RectTransform titleRect = titleText.rectTransform;
                titleRect.anchorMin = new Vector2(0f, 1f);
                titleRect.anchorMax = new Vector2(0f, 1f);
                titleRect.pivot = new Vector2(0f, 1f);
                titleRect.anchoredPosition = new Vector2(sidePadding + resolvedIconSize.x + textGap, -titleTopInset);
                titleRect.sizeDelta = new Vector2(width - (sidePadding * 2f) - resolvedIconSize.x - textGap, titleHeight);
                titleText.fontSize = Mathf.Max(titleText.fontSize, topBarTitleFontSize);
                titleText.alignment = TextAnchor.MiddleLeft;
                titleText.lineSpacing = compactMode ? compactTopBarTitleLineSpacing : 1f;
                ApplyCompactTextShadow(titleText, compactTitleShadowColor);
            }

            if (detailText != null)
            {
                RectTransform detailRect = detailText.rectTransform;
                detailRect.anchorMin = new Vector2(0f, 1f);
                detailRect.anchorMax = new Vector2(0f, 1f);
                detailRect.pivot = new Vector2(0f, 1f);
                detailRect.anchoredPosition = new Vector2(sidePadding + resolvedIconSize.x + textGap, -detailTopInset);
                detailRect.sizeDelta = new Vector2(width - (sidePadding * 2f) - resolvedIconSize.x - textGap, detailHeight);
                detailText.fontSize = Mathf.Max(detailText.fontSize, topBarDetailFontSize);
                detailText.alignment = TextAnchor.UpperLeft;
                detailText.lineSpacing = compactMode ? compactTopBarDetailLineSpacing : 1f;
                detailText.supportRichText = true;
                ApplyCompactTextShadow(detailText, compactDetailShadowColor);
            }

            ApplyThreatTheme(Time.unscaledTime);
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

            if (titleText != null)
            {
                titleText.text = FormatTitle(_currentTitleCategory, _currentTitleValue, ResolveCategoryColor(unscaledTime), ResolveTitleValueColor(unscaledTime));
            }

            if (detailText != null)
            {
                detailText.text = FormatDetail(_currentDetailCategory, _currentDetailValue, ResolveCategoryColor(unscaledTime), ResolveDetailValueColor(unscaledTime));
            }
        }

        private string FormatTitle(string category, string value, Color categoryColor, Color valueColor)
        {
            string categoryHex = ColorUtility.ToHtmlStringRGBA(categoryColor);
            string valueHex = ColorUtility.ToHtmlStringRGBA(valueColor);

            if (_compactTopBarMode)
            {
                return
                    $"<size={compactTopBarCategoryFontSize}><color=#{categoryHex}>{category}</color></size>\n" +
                    $"<size={compactTopBarTitleFontSize}><b><color=#{valueHex}>{value}</color></b></size>";
            }

            return
                $"<size={topBarCategoryFontSize}><color=#{categoryHex}>{category}</color></size>\n" +
                $"<size={topBarTitleFontSize}><b><color=#{valueHex}>{value}</color></b></size>";
        }

        private string FormatDetail(string category, string value, Color categoryColor, Color valueColor)
        {
            string categoryHex = ColorUtility.ToHtmlStringRGBA(categoryColor);
            string valueHex = ColorUtility.ToHtmlStringRGBA(valueColor);

            if (_compactTopBarMode)
            {
                return
                    $"<size={compactTopBarCategoryFontSize}><color=#{categoryHex}>{category}</color></size>\n" +
                    $"<size={compactTopBarDetailValueFontSize}><color=#{valueHex}>{value}</color></size>";
            }

            return
                $"<size={topBarCategoryFontSize}><color=#{categoryHex}>{category}</color></size>\n" +
                $"<color=#{valueHex}>{value}</color>";
        }

        private Color ResolveFrameColor(float unscaledTime)
        {
            if (!_hasChallengeThreatTheme)
            {
                return ApplyCompactAlpha(_currentFrameBaseColor, compactFrameAlphaScale);
            }

            float stageStrength = Mathf.Clamp01((ChallengeThreatPresentationResolver.ResolveStatusThemeStrength(_challengeThreatStage) - 0.92f) / 0.33f);
            float pulse = 0.5f + (0.5f * Mathf.Sin(unscaledTime * (1f + ChallengeThreatPresentationResolver.ResolveBannerPulseCycles(_challengeThreatStage))));
            float tint = challengeFrameTintStrength * Mathf.Lerp(0.76f, 1.08f, stageStrength) * Mathf.Lerp(0.86f, 1.1f, pulse);
            return ApplyCompactAlpha(Color.Lerp(_currentFrameBaseColor, Color.Lerp(_currentFrameBaseColor, _challengeThreatAccentColor, 0.42f), Mathf.Clamp01(tint)), compactFrameAlphaScale);
        }

        private Color ResolveIconColor(float unscaledTime)
        {
            if (!_hasChallengeThreatTheme)
            {
                return _currentIconBaseColor;
            }

            float pulse = 0.5f + (0.5f * Mathf.Sin(unscaledTime * (0.95f + ChallengeThreatPresentationResolver.ResolveBannerPulseCycles(_challengeThreatStage))));
            float tint = challengeIconTintStrength * Mathf.Lerp(0.86f, 1.14f, pulse);
            return Color.Lerp(_currentIconBaseColor, Color.Lerp(_currentIconBaseColor, _challengeThreatAccentColor, 0.38f), Mathf.Clamp01(tint));
        }

        private Color ResolveCategoryColor(float unscaledTime)
        {
            if (!_hasChallengeThreatTheme)
            {
                return ApplyCompactAlpha(categoryTextColor, compactCategoryAlphaScale);
            }

            float pulse = 0.5f + (0.5f * Mathf.Sin(unscaledTime * (0.84f + ChallengeThreatPresentationResolver.ResolveBannerPulseCycles(_challengeThreatStage))));
            float tint = challengeDetailTintStrength * Mathf.Lerp(0.84f, 1.08f, pulse);
            Color baseCategoryColor = UsesChallengeBaselineTheme() ? challengeBaselineCategoryColor : categoryTextColor;
            return ApplyCompactAlpha(Color.Lerp(baseCategoryColor, Color.Lerp(Color.white, _challengeThreatAccentColor, 0.2f), Mathf.Clamp01(tint)), compactCategoryAlphaScale);
        }

        private Color ResolveTitleValueColor(float unscaledTime)
        {
            if (!_hasChallengeThreatTheme)
            {
                return ApplyCompactAlpha(_currentTitleValueColor, compactTitleValueAlphaScale);
            }

            float stageStrength = Mathf.Clamp01((ChallengeThreatPresentationResolver.ResolveStatusThemeStrength(_challengeThreatStage) - 0.92f) / 0.33f);
            float pulse = 0.5f + (0.5f * Mathf.Sin(unscaledTime * (1.1f + ChallengeThreatPresentationResolver.ResolveBannerPulseCycles(_challengeThreatStage)) * Mathf.PI * 0.5f));
            float tint = challengeValueTintStrength * Mathf.Lerp(0.74f, 1.08f, stageStrength) * Mathf.Lerp(0.88f, 1.14f, pulse);
            Color baseTitleValueColor = UsesChallengeBaselineTheme()
                ? Color.Lerp(_currentTitleValueColor, challengeBaselineTitleValueColor, 0.42f)
                : _currentTitleValueColor;
            return ApplyCompactAlpha(Color.Lerp(baseTitleValueColor, Color.Lerp(baseTitleValueColor, _challengeThreatAccentColor, 0.66f), Mathf.Clamp01(tint)), compactTitleValueAlphaScale);
        }

        private Color ResolveDetailValueColor(float unscaledTime)
        {
            if (!_hasChallengeThreatTheme)
            {
                return ApplyCompactAlpha(_currentDetailValueColor, compactDetailValueAlphaScale);
            }

            float pulse = 0.5f + (0.5f * Mathf.Sin(unscaledTime * (0.9f + ChallengeThreatPresentationResolver.ResolveBannerPulseCycles(_challengeThreatStage))));
            float tint = challengeDetailTintStrength * Mathf.Lerp(0.82f, 1.1f, pulse);
            Color baseDetailValueColor = UsesChallengeBaselineTheme()
                ? Color.Lerp(_currentDetailValueColor, challengeBaselineDetailValueColor, 0.36f)
                : _currentDetailValueColor;
            return ApplyCompactAlpha(Color.Lerp(baseDetailValueColor, Color.Lerp(baseDetailValueColor, _challengeThreatAccentColor, 0.28f), Mathf.Clamp01(tint)), compactDetailValueAlphaScale);
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

        private void ApplyCompactTextShadow(Text text, Color shadowColor)
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

            shadow.effectColor = shadowColor;
            shadow.effectDistance = compactTextShadowDistance;
            shadow.useGraphicAlpha = true;
        }

        private bool UsesChallengeBaselineTheme()
        {
            return string.Equals(_challengeThreatBadgeLabel, "챌린지");
        }
    }
}
