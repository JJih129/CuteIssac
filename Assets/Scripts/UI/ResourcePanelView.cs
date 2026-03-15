using CuteIssac.Player;
using CuteIssac.Room;
using UnityEngine;
using UnityEngine.UI;

namespace CuteIssac.UI
{
    /// <summary>
    /// Presentation-only panel for the three core Isaac-like resources.
    /// Each icon, value label, and background can be replaced independently in the inspector.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ResourcePanelView : MonoBehaviour
    {
        private static Sprite s_runtimeCoinSprite;
        private static Sprite s_runtimeKeySprite;
        private static Sprite s_runtimeBombSprite;

        [Header("Top Bar Layout")]
        [SerializeField] private Vector2 topBarIconSize = new(20f, 20f);
        [SerializeField] private Vector2 compactTopBarIconSize = new(30f, 30f);
        [SerializeField] private Vector2 topBarBackgroundHeight = new(52f, 0f);
        [SerializeField] [Min(24f)] private float compactTopBarBackgroundHeight = 96f;
        [SerializeField] [Min(0f)] private float topBarSectionPadding = 8f;
        [SerializeField] [Min(0f)] private float compactTopBarSectionPadding = 8f;
        [SerializeField] [Min(0f)] private float topBarBackgroundTopInset = 8f;
        [SerializeField] [Min(0f)] private float compactTopBarBackgroundTopInset = 10f;
        [SerializeField] [Min(0f)] private float topBarIconTopInset = 10f;
        [SerializeField] [Min(0f)] private float topBarIconValueGap = 10f;
        [SerializeField] [Min(0f)] private float topBarValueTopInset = 22f;
        [SerializeField] [Min(24f)] private float topBarValueHeight = 34f;
        [SerializeField] [Min(12f)] private float compactTopBarValueHeight = 30f;
        [SerializeField] [Min(0)] private int topBarLabelFontSize = 12;
        [SerializeField] [Min(0)] private int topBarValueFontSize = 26;
        [SerializeField] [Min(0)] private int compactTopBarValueFontSize = 24;
        [SerializeField] [Min(0f)] private float compactTopBarRowGap = 10f;
        [SerializeField] [Min(0f)] private float compactTopBarRowInsetX = 4f;
        [SerializeField] [Min(0.5f)] private float topBarValueLineSpacing = 0.9f;

        [Header("Styling")]
        [SerializeField] private Color coinSlotColor = new(0.92f, 0.76f, 0.18f, 0.22f);
        [SerializeField] private Color keySlotColor = new(0.72f, 0.84f, 1f, 0.2f);
        [SerializeField] private Color bombSlotColor = new(1f, 0.66f, 0.34f, 0.2f);
        [SerializeField] private Color labelTextColor = new(0.96f, 0.98f, 1f, 0.8f);
        [SerializeField] private Color valueTextColor = new(0.99f, 0.995f, 1f, 1f);
        [SerializeField] private Color challengeBaselineLabelColor = new(1f, 0.94f, 0.84f, 0.78f);
        [SerializeField] private Color challengeBaselineValueColor = new(1f, 0.9f, 0.64f, 0.98f);
        [SerializeField] private FontStyle valueFontStyle = FontStyle.Bold;
        [SerializeField] [Range(0f, 1f)] private float challengeSlotTintStrength = 0.48f;
        [SerializeField] [Range(0f, 1f)] private float challengeValueTintStrength = 0.56f;
        [SerializeField] [Range(0f, 1f)] private float challengeIconTintStrength = 0.3f;
        [SerializeField] [Range(0f, 1f)] private float iconAlpha = 0.92f;
        [SerializeField] [Range(0f, 1f)] private float challengeIconAlpha = 0.98f;

        [Header("Optional Root")]
        [Tooltip("Optional. Root object to hide or swap when replacing the whole resource panel prefab.")]
        [SerializeField] private GameObject panelRoot;

        [Header("Coin")]
        [Tooltip("Optional background image for the coin slot.")]
        [SerializeField] private Image coinBackgroundImage;
        [Tooltip("Optional icon image for the coin slot.")]
        [SerializeField] private Image coinIconImage;
        [Tooltip("Numeric label for the player's coin count.")]
        [SerializeField] private Text coinValueText;

        [Header("Key")]
        [Tooltip("Optional background image for the key slot.")]
        [SerializeField] private Image keyBackgroundImage;
        [Tooltip("Optional icon image for the key slot.")]
        [SerializeField] private Image keyIconImage;
        [Tooltip("Numeric label for the player's key count.")]
        [SerializeField] private Text keyValueText;

        [Header("Bomb")]
        [Tooltip("Optional background image for the bomb slot.")]
        [SerializeField] private Image bombBackgroundImage;
        [Tooltip("Optional icon image for the bomb slot.")]
        [SerializeField] private Image bombIconImage;
        [Tooltip("Numeric label for the player's bomb count.")]
        [SerializeField] private Text bombValueText;

        private bool _warnedMissingValueText;
        private bool _compactTopBarMode;
        private PlayerResourceSnapshot _currentResources;
        private bool _hasResourceSnapshot;
        private bool _hasChallengeThreatTheme;
        private Color _challengeThreatAccentColor = Color.white;
        private string _challengeThreatBadgeLabel = string.Empty;
        private ChallengeThreatStage _challengeThreatStage;

        public void ConfigureDebugView(
            Text coinText,
            Text keyText,
            Text bombText,
            Image coinBackground = null,
            Image keyBackground = null,
            Image bombBackground = null,
            Image coinIcon = null,
            Image keyIcon = null,
            Image bombIcon = null)
        {
            coinValueText = coinText;
            keyValueText = keyText;
            bombValueText = bombText;
            coinBackgroundImage = coinBackground;
            keyBackgroundImage = keyBackground;
            bombBackgroundImage = bombBackground;
            coinIconImage = coinIcon;
            keyIconImage = keyIcon;
            bombIconImage = bombIcon;
        }

        public void SetResources(PlayerResourceSnapshot resources)
        {
            _currentResources = resources;
            _hasResourceSnapshot = true;

            if (panelRoot != null && !panelRoot.activeSelf)
            {
                panelRoot.SetActive(true);
            }

            if ((coinValueText == null || keyValueText == null || bombValueText == null) && !_warnedMissingValueText)
            {
                Debug.LogWarning("ResourcePanelView is missing one or more value text references. Missing fields will simply not update until they are assigned.", this);
                _warnedMissingValueText = true;
            }

            EnsureRuntimeIcons();
            RefreshResourceText();
            ApplyThreatTheme(Time.unscaledTime);
        }

        public void ApplyTopBarLayout(bool compactMode)
        {
            _compactTopBarMode = compactMode;

            if (!compactMode)
            {
                return;
            }

            RectTransform rootRect = panelRoot != null
                ? panelRoot.transform as RectTransform
                : transform as RectTransform;

            if (rootRect == null)
            {
                return;
            }

            EnsureRuntimeIcons();

            float width = rootRect.rect.width > 0f ? rootRect.rect.width : rootRect.sizeDelta.x;
            float sectionPadding = _compactTopBarMode ? compactTopBarSectionPadding : topBarSectionPadding;
            float sectionWidth = _compactTopBarMode
                ? Mathf.Max(44f, width - (sectionPadding * 2f))
                : Mathf.Max(44f, (width - (sectionPadding * 2f)) / 3f);
            float usableHeight = rootRect.rect.height > 0f ? rootRect.rect.height : rootRect.sizeDelta.y;
            float backgroundHeight = _compactTopBarMode
                ? compactTopBarBackgroundHeight
                : (topBarBackgroundHeight.x > 0f
                    ? topBarBackgroundHeight.x
                    : Mathf.Max(36f, usableHeight - 12f));

            LayoutResourceSlot(coinBackgroundImage, coinIconImage, coinValueText, 0, sectionWidth, backgroundHeight);
            LayoutResourceSlot(bombBackgroundImage, bombIconImage, bombValueText, 1, sectionWidth, backgroundHeight);
            LayoutResourceSlot(keyBackgroundImage, keyIconImage, keyValueText, 2, sectionWidth, backgroundHeight);
            ApplyThreatTheme(Time.unscaledTime);
        }

        public void SetChallengeThreatTheme(bool active, Color accentColor, string badgeLabel, ChallengeThreatStage stage)
        {
            _hasChallengeThreatTheme = active;
            _challengeThreatAccentColor = accentColor;
            _challengeThreatBadgeLabel = active ? badgeLabel ?? string.Empty : string.Empty;
            _challengeThreatStage = stage;
            RefreshResourceText();
            ApplyThreatTheme(Time.unscaledTime);
        }

        private void Update()
        {
            if (!_hasResourceSnapshot && !_hasChallengeThreatTheme)
            {
                return;
            }

            RefreshResourceText();
            ApplyThreatTheme(Time.unscaledTime);
        }

        private void LayoutResourceSlot(Image backgroundImage, Image iconImage, Text valueText, int index, float sectionWidth, float backgroundHeight)
        {
            float resolvedCompactSectionPadding = Mathf.Max(compactTopBarSectionPadding, 8f);
            float resolvedCompactRowGap = Mathf.Max(compactTopBarRowGap, 10f);
            float resolvedCompactRowInsetX = Mathf.Max(compactTopBarRowInsetX, 4f);
            Vector2 resolvedCompactIconSize = new(
                Mathf.Max(compactTopBarIconSize.x, 30f),
                Mathf.Max(compactTopBarIconSize.y, 30f));
            float resolvedCompactValueHeight = Mathf.Max(compactTopBarValueHeight, 30f);
            int resolvedCompactValueFontSize = Mathf.Max(compactTopBarValueFontSize, 24);
            float sectionPadding = _compactTopBarMode ? resolvedCompactSectionPadding : topBarSectionPadding;
            float left = sectionPadding + (sectionWidth * index);
            float slotWidth = sectionWidth - sectionPadding;

            if (backgroundImage != null)
            {
                backgroundImage.enabled = !_compactTopBarMode;
            }

            if (backgroundImage != null && !_compactTopBarMode)
            {
                RectTransform backgroundRect = backgroundImage.rectTransform;
                backgroundRect.anchorMin = new Vector2(0f, 1f);
                backgroundRect.anchorMax = new Vector2(0f, 1f);
                backgroundRect.pivot = new Vector2(0f, 1f);
                float backgroundTopInset = _compactTopBarMode ? compactTopBarBackgroundTopInset : topBarBackgroundTopInset;
                backgroundRect.anchoredPosition = new Vector2(left, -backgroundTopInset);
                backgroundRect.sizeDelta = new Vector2(sectionWidth - sectionPadding, backgroundHeight);
            }

            if (iconImage != null)
            {
                RectTransform iconRect = iconImage.rectTransform;
                Vector2 resolvedIconSize = _compactTopBarMode ? resolvedCompactIconSize : topBarIconSize;
                iconRect.sizeDelta = resolvedIconSize;

                if (_compactTopBarMode)
                {
                    float rowHeight = resolvedCompactIconSize.y + resolvedCompactRowGap;
                    float topInset = 1f;
                    iconRect.anchorMin = new Vector2(0f, 1f);
                    iconRect.anchorMax = new Vector2(0f, 1f);
                    iconRect.pivot = new Vector2(0f, 1f);
                    iconRect.anchoredPosition = new Vector2(resolvedCompactRowInsetX, -(topInset + (index * rowHeight)));
                }
                else
                {
                    iconRect.anchorMin = new Vector2(0f, 1f);
                    iconRect.anchorMax = new Vector2(0f, 1f);
                    iconRect.pivot = new Vector2(0.5f, 1f);
                    float iconTopInset = topBarIconTopInset;
                    iconRect.anchoredPosition = new Vector2(left + (slotWidth * 0.5f), -iconTopInset);
                }
            }

            if (valueText != null)
            {
                RectTransform valueRect = valueText.rectTransform;
                if (_compactTopBarMode)
                {
                    float rowHeight = resolvedCompactIconSize.y + resolvedCompactRowGap;
                    float topInset = 0f;
                    float iconWidth = resolvedCompactIconSize.x;
                    valueRect.anchorMin = new Vector2(0f, 1f);
                    valueRect.anchorMax = new Vector2(0f, 1f);
                    valueRect.pivot = new Vector2(0f, 1f);
                    valueRect.anchoredPosition = new Vector2(resolvedCompactRowInsetX + iconWidth + 8f, -(topInset + (index * rowHeight)));
                    valueRect.sizeDelta = new Vector2(Mathf.Max(56f, slotWidth - iconWidth - 8f), resolvedCompactValueHeight);
                    valueText.alignment = TextAnchor.MiddleRight;
                    valueText.fontSize = resolvedCompactValueFontSize;
                }
                else
                {
                    float resolvedValueTopInset = Mathf.Max(topBarValueTopInset, topBarIconTopInset + topBarIconSize.y + topBarIconValueGap);
                    valueRect.anchorMin = new Vector2(0f, 1f);
                    valueRect.anchorMax = new Vector2(0f, 1f);
                    valueRect.pivot = new Vector2(0f, 1f);
                    valueRect.anchoredPosition = new Vector2(left, -resolvedValueTopInset);
                    valueRect.sizeDelta = new Vector2(slotWidth, topBarValueHeight);
                    valueText.alignment = TextAnchor.MiddleCenter;
                    valueText.fontSize = Mathf.Max(valueText.fontSize, topBarValueFontSize);
                }

                valueText.lineSpacing = topBarValueLineSpacing;
            }
        }

        private void RefreshResourceText()
        {
            if (!_hasResourceSnapshot)
            {
                return;
            }

            if (coinValueText != null)
            {
                coinValueText.text = FormatResourceValue("COIN", _currentResources.Coins, coinSlotColor, Time.unscaledTime);
            }

            if (keyValueText != null)
            {
                keyValueText.text = FormatResourceValue("KEY", _currentResources.Keys, keySlotColor, Time.unscaledTime);
            }

            if (bombValueText != null)
            {
                bombValueText.text = FormatResourceValue("BOMB", _currentResources.Bombs, bombSlotColor, Time.unscaledTime);
            }
        }

        private void ApplyThreatTheme(float unscaledTime)
        {
            EnsureRuntimeIcons();
            ApplyResourceSkin(unscaledTime);
        }

        private void EnsureRuntimeIcons()
        {
            AssignRuntimeIcon(coinIconImage, GetRuntimeResourceSprite(ResourceIconType.Coin));
            AssignRuntimeIcon(keyIconImage, GetRuntimeResourceSprite(ResourceIconType.Key));
            AssignRuntimeIcon(bombIconImage, GetRuntimeResourceSprite(ResourceIconType.Bomb));
        }

        private static void AssignRuntimeIcon(Image iconImage, Sprite fallbackSprite)
        {
            if (iconImage == null)
            {
                return;
            }

            if (iconImage.sprite == null)
            {
                iconImage.sprite = fallbackSprite;
            }

            iconImage.preserveAspect = true;
        }

        private void ApplyResourceSkin(float unscaledTime)
        {
            ApplySlotSkin(coinBackgroundImage, coinIconImage, coinValueText, coinSlotColor, unscaledTime);
            ApplySlotSkin(keyBackgroundImage, keyIconImage, keyValueText, keySlotColor, unscaledTime);
            ApplySlotSkin(bombBackgroundImage, bombIconImage, bombValueText, bombSlotColor, unscaledTime);
        }

        private void ApplySlotSkin(Image backgroundImage, Image iconImage, Text valueText, Color backgroundColor, float unscaledTime)
        {
            if (backgroundImage != null)
            {
                backgroundImage.color = ResolveSlotBackgroundColor(backgroundColor, unscaledTime);
            }

            if (iconImage != null)
            {
                iconImage.color = ResolveIconColor(unscaledTime);
            }

            if (valueText != null)
            {
                valueText.color = valueTextColor;
                valueText.fontStyle = valueFontStyle;
                valueText.supportRichText = true;
            }
        }

        private string FormatResourceValue(string label, int value, Color accentColor, float unscaledTime)
        {
            string valueHex = ColorUtility.ToHtmlStringRGBA(ResolveValueColor(accentColor, unscaledTime));

            if (_compactTopBarMode)
            {
                return $"<size={Mathf.Max(compactTopBarValueFontSize, 24)}><b><color=#{valueHex}>{value}</color></b></size>";
            }

            string labelHex = ColorUtility.ToHtmlStringRGBA(ResolveLabelColor(unscaledTime));
            return
                $"<size={topBarLabelFontSize}><color=#{labelHex}>{label}</color></size>\n" +
                $"<size={topBarValueFontSize}><b><color=#{valueHex}>{value}</color></b></size>";
        }

        private Color ResolveSlotBackgroundColor(Color baseColor, float unscaledTime)
        {
            if (!_hasChallengeThreatTheme)
            {
                return baseColor;
            }

            float stageStrength = Mathf.Clamp01((ChallengeThreatPresentationResolver.ResolveStatusThemeStrength(_challengeThreatStage) - 0.92f) / 0.33f);
            float pulse = 0.5f + (0.5f * Mathf.Sin(unscaledTime * (1f + ChallengeThreatPresentationResolver.ResolveBannerPulseCycles(_challengeThreatStage))));
            float tint = challengeSlotTintStrength * Mathf.Lerp(0.76f, 1.08f, stageStrength) * Mathf.Lerp(0.86f, 1.08f, pulse);
            return Color.Lerp(baseColor, Color.Lerp(baseColor, _challengeThreatAccentColor, 0.34f), Mathf.Clamp01(tint));
        }

        private Color ResolveLabelColor(float unscaledTime)
        {
            if (!_hasChallengeThreatTheme)
            {
                return labelTextColor;
            }

            float pulse = 0.5f + (0.5f * Mathf.Sin(unscaledTime * (0.8f + ChallengeThreatPresentationResolver.ResolveBannerPulseCycles(_challengeThreatStage))));
            float tint = challengeValueTintStrength * 0.36f * Mathf.Lerp(0.84f, 1.08f, pulse);
            Color baseLabelColor = UsesChallengeBaselineTheme() ? challengeBaselineLabelColor : labelTextColor;
            return Color.Lerp(baseLabelColor, Color.Lerp(Color.white, _challengeThreatAccentColor, 0.18f), Mathf.Clamp01(tint));
        }

        private Color ResolveValueColor(Color baseAccentColor, float unscaledTime)
        {
            Color fallbackValueColor = UsesChallengeBaselineTheme() ? challengeBaselineValueColor : valueTextColor;
            Color defaultValueColor = Color.Lerp(baseAccentColor, fallbackValueColor, 0.65f);
            if (!_hasChallengeThreatTheme)
            {
                return defaultValueColor;
            }

            float stageStrength = Mathf.Clamp01((ChallengeThreatPresentationResolver.ResolveStatusThemeStrength(_challengeThreatStage) - 0.92f) / 0.33f);
            float pulse = 0.5f + (0.5f * Mathf.Sin(unscaledTime * (1.1f + ChallengeThreatPresentationResolver.ResolveBannerPulseCycles(_challengeThreatStage)) * Mathf.PI * 0.5f));
            float tint = challengeValueTintStrength * Mathf.Lerp(0.72f, 1.08f, stageStrength) * Mathf.Lerp(0.88f, 1.14f, pulse);
            return Color.Lerp(defaultValueColor, Color.Lerp(defaultValueColor, _challengeThreatAccentColor, 0.62f), Mathf.Clamp01(tint));
        }

        private Color ResolveIconColor(float unscaledTime)
        {
            if (!_hasChallengeThreatTheme)
            {
                return new Color(1f, 1f, 1f, iconAlpha);
            }

            float pulse = 0.5f + (0.5f * Mathf.Sin(unscaledTime * (1.15f + ChallengeThreatPresentationResolver.ResolveBannerPulseCycles(_challengeThreatStage))));
            float tint = challengeIconTintStrength * Mathf.Lerp(0.86f, 1.14f, pulse);
            Color color = Color.Lerp(Color.white, Color.Lerp(Color.white, _challengeThreatAccentColor, 0.42f), Mathf.Clamp01(tint));
            color.a = challengeIconAlpha;
            return color;
        }

        private bool UsesChallengeBaselineTheme()
        {
            return string.Equals(_challengeThreatBadgeLabel, "챌린지");
        }

        private enum ResourceIconType
        {
            Coin,
            Key,
            Bomb
        }

        private static Sprite GetRuntimeResourceSprite(ResourceIconType iconType)
        {
            switch (iconType)
            {
                case ResourceIconType.Coin:
                    if (s_runtimeCoinSprite == null)
                    {
                        s_runtimeCoinSprite = CreateRuntimeResourceSprite(new[]
                        {
                            "0000011111100000",
                            "0001111111111000",
                            "0011111111111100",
                            "0111111111111110",
                            "0111110001111110",
                            "1111100000111111",
                            "1111100000111111",
                            "1111100000111111",
                            "1111100000111111",
                            "1111100000111111",
                            "0111110001111110",
                            "0111111111111110",
                            "0011111111111100",
                            "0001111111111000",
                            "0000011111100000",
                            "0000000000000000",
                        });
                    }

                    return s_runtimeCoinSprite;

                case ResourceIconType.Key:
                    if (s_runtimeKeySprite == null)
                    {
                        s_runtimeKeySprite = CreateRuntimeResourceSprite(new[]
                        {
                            "0000000000000000",
                            "0000011110000000",
                            "0000110011000000",
                            "0001100001100000",
                            "0001100001100000",
                            "0000110011000000",
                            "0000011111111000",
                            "0000000011000000",
                            "0000000011000000",
                            "0000000011111000",
                            "0000000011000000",
                            "0000000011110000",
                            "0000000011000000",
                            "0000000000000000",
                            "0000000000000000",
                            "0000000000000000",
                        });
                    }

                    return s_runtimeKeySprite;

                default:
                    if (s_runtimeBombSprite == null)
                    {
                        s_runtimeBombSprite = CreateRuntimeResourceSprite(new[]
                        {
                            "0000000110000000",
                            "0000001111000000",
                            "0000000110000000",
                            "0000011111100000",
                            "0001111111111000",
                            "0011111111111100",
                            "0111111111111110",
                            "0111111111111110",
                            "0111111111111110",
                            "0111111111111110",
                            "0011111111111100",
                            "0011111111111100",
                            "0001111111111000",
                            "0000111111110000",
                            "0000001111000000",
                            "0000000000000000",
                        });
                    }

                    return s_runtimeBombSprite;
            }
        }

        private static Sprite CreateRuntimeResourceSprite(string[] mask)
        {
            const int size = 16;
            Texture2D texture = new(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };

            Color clear = new(0f, 0f, 0f, 0f);
            Color pixel = Color.white;

            for (int y = 0; y < size; y++)
            {
                string row = mask[size - 1 - y];
                for (int x = 0; x < size; x++)
                {
                    texture.SetPixel(x, y, row[x] == '1' ? pixel : clear);
                }
            }

            texture.Apply();
            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
            sprite.name = "RuntimeResourceIcon";
            return sprite;
        }
    }
}
