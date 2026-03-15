using System.Collections.Generic;
using CuteIssac.Room;
using UnityEngine;
using UnityEngine.UI;

namespace CuteIssac.UI
{
    /// <summary>
    /// Pure presentation component for player health.
    /// Assign a text label and optionally a heart slot template plus sprites to swap the visual skin later without touching gameplay logic.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HealthPanelView : MonoBehaviour
    {
        private static Sprite s_runtimeFilledHeartSprite;
        private static Sprite s_runtimeEmptyHeartSprite;

        [Header("Optional Root")]
        [Tooltip("Optional. Hide or replace the whole panel by swapping this root object.")]
        [SerializeField] private GameObject panelRoot;

        [Header("Value Label")]
        [Tooltip("Optional. Displays a simple numeric HP label.")]
        [SerializeField] private Text healthValueText;

        [Header("Heart Slots")]
        [Tooltip("Parent transform that receives generated heart slot instances.")]
        [SerializeField] private RectTransform heartSlotParent;
        [Tooltip("Template image used to generate heart slots. It can be any skinned UI element with an Image component.")]
        [SerializeField] private Image heartSlotTemplate;
        [Tooltip("Spacing used when the panel lays out heart slots without a separate layout group.")]
        [SerializeField] [Min(0f)] private float slotSpacing = 6f;
        [Tooltip("Optional sprite shown for filled hearts.")]
        [SerializeField] private Sprite filledHeartSprite;
        [Tooltip("Optional sprite shown for empty hearts.")]
        [SerializeField] private Sprite emptyHeartSprite;
        [Tooltip("Fallback tint used when no filled sprite is assigned.")]
        [SerializeField] private Color filledHeartColor = new(1f, 0.36f, 0.36f, 1f);
        [Tooltip("Fallback tint used when no empty sprite is assigned.")]
        [SerializeField] private Color emptyHeartColor = new(0.28f, 0.3f, 0.34f, 0.72f);

        [Header("Top Bar Layout")]
        [SerializeField] private bool preferCompactSingleRow = true;
        [SerializeField] [Min(0f)] private float topBarValueInsetX = 16f;
        [SerializeField] [Min(0f)] private float topBarValueInsetY = 12f;
        [SerializeField] [Min(48f)] private float topBarValueWidth = 124f;
        [SerializeField] [Min(32f)] private float topBarValueHeight = 46f;
        [SerializeField] [Min(0f)] private float topBarHeartInsetX = 112f;
        [SerializeField] [Min(0f)] private float topBarHeartInsetY = 11f;
        [SerializeField] [Min(0f)] private float compactTopBarHeartInsetY = 10f;
        [SerializeField] [Min(-16f)] private float topBarHeartBaselineOffsetY = 2f;
        [SerializeField] private Vector2 topBarHeartSlotSize = new(26f, 26f);
        [SerializeField] private Vector2 compactTopBarHeartSlotSize = new(34f, 34f);
        [SerializeField] [Min(0f)] private float topBarSlotSpacing = 4f;
        [SerializeField] [Min(0f)] private float topBarRowSpacing = 2f;
        [SerializeField] [Min(1)] private int topBarMaxRows = 2;
        [SerializeField] [Min(0)] private int topBarValueFontSize = 24;
        [SerializeField] [Min(0)] private int topBarLabelFontSize = 13;
        [SerializeField] [Min(0)] private int topBarHealthValueFontSize = 28;
        [SerializeField] [Min(0)] private int compactTopBarLabelFontSize = 18;
        [SerializeField] [Min(0)] private int compactTopBarHealthValueFontSize = 36;
        [SerializeField] [Min(0)] private int compactTopBarMaxValueFontSize = 18;
        [SerializeField] [Min(0.5f)] private float topBarValueLineSpacing = 0.9f;

        [Header("Styling")]
        [SerializeField] private Color healthLabelColor = new(0.96f, 0.98f, 1f, 0.88f);
        [SerializeField] private Color currentHealthColor = new(1f, 0.48f, 0.46f, 1f);
        [SerializeField] private Color maxHealthColor = new(0.94f, 0.96f, 1f, 0.82f);
        [SerializeField] private Color criticalHealthColor = new(1f, 0.2f, 0.2f, 1f);
        [SerializeField] private Color valueTextColor = new(0.99f, 0.995f, 1f, 1f);
        [SerializeField] private Color challengeBaselineLabelColor = new(1f, 0.95f, 0.84f, 0.84f);
        [SerializeField] private Color challengeBaselineCurrentHealthColor = new(1f, 0.8f, 0.52f, 1f);
        [SerializeField] private Color challengeBaselineMaxHealthColor = new(1f, 0.92f, 0.78f, 0.88f);
        [SerializeField] private Color challengeBaselineFilledHeartColor = new(1f, 0.8f, 0.42f, 1f);
        [SerializeField] private Color challengeBaselineEmptyHeartColor = new(0.44f, 0.34f, 0.18f, 0.74f);
        [SerializeField] [Range(0.5f, 1.2f)] private float compactLabelAlphaScale = 1.04f;
        [SerializeField] [Range(0.5f, 1.2f)] private float compactCurrentHealthAlphaScale = 1.08f;
        [SerializeField] [Range(0.5f, 1.2f)] private float compactMaxHealthAlphaScale = 0.96f;
        [SerializeField] [Range(0.5f, 1.2f)] private float compactFilledHeartAlphaScale = 1.04f;
        [SerializeField] [Range(0.5f, 1.2f)] private float compactEmptyHeartAlphaScale = 0.9f;
        [SerializeField] [Range(0f, 1f)] private float challengeHeartTintStrength = 0.34f;
        [SerializeField] [Range(0f, 1f)] private float challengeValueTintStrength = 0.58f;
        [SerializeField] [Range(0f, 1f)] private float challengeLabelTintStrength = 0.3f;
        [SerializeField] [Range(0.2f, 1f)] private float topBarThreatPulseDamping = 0.72f;
        [SerializeField] private Color compactTextShadowColor = new(0.02f, 0.05f, 0.08f, 0.72f);
        [SerializeField] private Vector2 compactTextShadowDistance = new(1.4f, -1.4f);

        private readonly List<Image> _runtimeSlots = new();
        private bool _warnedMissingSlotSetup;
        private bool _compactTopBarMode;
        private bool _hasHealthSnapshot;
        private int _currentHealth;
        private int _currentMaxHealth;
        private int _currentFilledSlots;
        private int _currentTotalSlots;
        private bool _hasChallengeThreatTheme;
        private Color _challengeThreatAccentColor = Color.white;
        private string _challengeThreatBadgeLabel = string.Empty;
        private ChallengeThreatStage _challengeThreatStage;

        public void ConfigureDebugView(Text valueText, RectTransform slotParent, Image slotTemplate)
        {
            healthValueText = valueText;
            heartSlotParent = slotParent;
            heartSlotTemplate = slotTemplate;
        }

        public void SetHealth(float currentHealth, float maxHealth)
        {
            if (panelRoot != null && !panelRoot.activeSelf)
            {
                panelRoot.SetActive(true);
            }

            _currentHealth = Mathf.CeilToInt(currentHealth);
            _currentMaxHealth = Mathf.CeilToInt(maxHealth);
            _hasHealthSnapshot = true;

            if (healthValueText != null)
            {
                healthValueText.text = FormatHealthLabel(_currentHealth, _currentMaxHealth, Time.unscaledTime);
                float safeMaxHealth = Mathf.Max(1f, maxHealth);
                healthValueText.color = currentHealth / safeMaxHealth <= 0.25f ? criticalHealthColor : valueTextColor;
                healthValueText.supportRichText = true;
            }

            int totalSlots = Mathf.Max(0, Mathf.CeilToInt(Mathf.Max(0f, maxHealth)));
            int filledSlots = Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(0f, currentHealth)), 0, totalSlots);
            _currentTotalSlots = totalSlots;
            _currentFilledSlots = filledSlots;
            EnsureRuntimeHeartSprites();

            if (heartSlotParent == null || heartSlotTemplate == null)
            {
                if (!_warnedMissingSlotSetup)
                {
                    Debug.LogWarning("HealthPanelView is missing heartSlotParent or heartSlotTemplate. The panel will fall back to text-only health display.", this);
                    _warnedMissingSlotSetup = true;
                }

                return;
            }

            EnsureSlotCount(totalSlots);

            for (int i = 0; i < _runtimeSlots.Count; i++)
            {
                Image slotImage = _runtimeSlots[i];
                bool isFilled = i < filledSlots;

                if (slotImage == null)
                {
                    continue;
                }

                slotImage.gameObject.SetActive(true);

                if (isFilled && filledHeartSprite != null)
                {
                    slotImage.sprite = filledHeartSprite;
                }
                else if (!isFilled && emptyHeartSprite != null)
                {
                    slotImage.sprite = emptyHeartSprite;
                }

                slotImage.color = ResolveHeartColor(isFilled, Time.unscaledTime);
            }

            ApplyThreatTheme(Time.unscaledTime);
        }

        public void ApplyTopBarLayout(bool compactMode)
        {
            _compactTopBarMode = compactMode;
            Vector2 resolvedCompactHeartSlotSize = new(
                Mathf.Max(compactTopBarHeartSlotSize.x, 34f),
                Mathf.Max(compactTopBarHeartSlotSize.y, 34f));
            float resolvedCompactHeartInsetY = Mathf.Max(compactTopBarHeartInsetY, 10f);

            RectTransform rootRect = panelRoot != null
                ? panelRoot.transform as RectTransform
                : transform as RectTransform;

            if (rootRect == null)
            {
                return;
            }

            if (healthValueText != null)
            {
                healthValueText.gameObject.SetActive(!_compactTopBarMode);

                if (!_compactTopBarMode)
                {
                    RectTransform valueRect = healthValueText.rectTransform;
                    valueRect.anchorMin = new Vector2(0f, 1f);
                    valueRect.anchorMax = new Vector2(0f, 1f);
                    valueRect.pivot = new Vector2(0f, 1f);
                    valueRect.anchoredPosition = new Vector2(topBarValueInsetX, -topBarValueInsetY);
                    valueRect.sizeDelta = new Vector2(topBarValueWidth, topBarValueHeight);
                    healthValueText.alignment = TextAnchor.MiddleLeft;
                    healthValueText.fontSize = Mathf.Max(healthValueText.fontSize, topBarValueFontSize);
                    healthValueText.lineSpacing = topBarValueLineSpacing;
                    healthValueText.color = valueTextColor;
                    healthValueText.supportRichText = true;
                    ApplyCompactTextShadow(healthValueText);
                }
            }

            if (heartSlotParent != null)
            {
                if (_compactTopBarMode)
                {
                    heartSlotParent.anchorMin = new Vector2(0f, 1f);
                    heartSlotParent.anchorMax = new Vector2(1f, 1f);
                    heartSlotParent.pivot = new Vector2(0f, 1f);
                    heartSlotParent.anchoredPosition = new Vector2(-2f, -4f);
                    heartSlotParent.sizeDelta = new Vector2(-2f, resolvedCompactHeartSlotSize.y + 6f);
                }
                else
                {
                    heartSlotParent.anchorMin = new Vector2(0f, 1f);
                    heartSlotParent.anchorMax = new Vector2(1f, 1f);
                    heartSlotParent.pivot = new Vector2(0f, 1f);
                    float resolvedHeartInsetY = topBarHeartInsetY;
                    heartSlotParent.anchoredPosition = new Vector2(topBarHeartInsetX, -(resolvedHeartInsetY + topBarHeartBaselineOffsetY));
                    Vector2 resolvedHeartSlotSize = topBarHeartSlotSize;
                    float rows = Mathf.Max(1, topBarMaxRows);
                    float containerHeight = (resolvedHeartSlotSize.y * rows) + (topBarRowSpacing * (rows - 1f)) + 4f;
                    heartSlotParent.sizeDelta = new Vector2(-(topBarHeartInsetX + 12f), containerHeight);
                }
            }

            ApplyCompactBackdropVisibility();

            for (int i = 0; i < _runtimeSlots.Count; i++)
            {
                if (_runtimeSlots[i] != null)
                {
                    LayoutSlot(_runtimeSlots[i], i);
                }
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

        private void EnsureSlotCount(int desiredCount)
        {
            while (_runtimeSlots.Count < desiredCount)
            {
                Image newSlot = Instantiate(heartSlotTemplate, heartSlotParent);
                newSlot.gameObject.name = "HeartSlot";
                newSlot.gameObject.SetActive(true);
                newSlot.preserveAspect = true;
                _runtimeSlots.Add(newSlot);
            }

            for (int i = 0; i < _runtimeSlots.Count; i++)
            {
                bool shouldBeVisible = i < desiredCount;

                if (_runtimeSlots[i] != null)
                {
                    LayoutSlot(_runtimeSlots[i], i);
                    _runtimeSlots[i].gameObject.SetActive(shouldBeVisible);
                }
            }
        }

        private void LayoutSlot(Image slotImage, int index)
        {
            RectTransform slotRect = slotImage.rectTransform;

            if (_compactTopBarMode && preferCompactSingleRow)
            {
                Vector2 resolvedHeartSlotSize = new(
                    Mathf.Max(compactTopBarHeartSlotSize.x, 34f),
                    Mathf.Max(compactTopBarHeartSlotSize.y, 34f));
                slotRect.anchorMin = new Vector2(0f, 1f);
                slotRect.anchorMax = new Vector2(0f, 1f);
                slotRect.pivot = new Vector2(0f, 1f);
                slotRect.sizeDelta = resolvedHeartSlotSize;
                slotRect.localScale = Vector3.one;
                float slotStride = resolvedHeartSlotSize.x + Mathf.Max(topBarSlotSpacing, 4f);
                float availableWidth = heartSlotParent != null && heartSlotParent.rect.width > 0f
                    ? heartSlotParent.rect.width
                    : slotStride * Mathf.Max(1, _runtimeSlots.Count);
                int slotsPerRow = Mathf.Max(1, Mathf.FloorToInt((availableWidth + topBarSlotSpacing) / slotStride));
                int maxVisibleRows = 1;
                int column = index % slotsPerRow;
                int row = Mathf.Min(index / slotsPerRow, maxVisibleRows - 1);
                float compactX = column * slotStride;
                float compactY = row * (resolvedHeartSlotSize.y + topBarRowSpacing);
                slotRect.anchoredPosition = new Vector2(compactX, -compactY);
                return;
            }

            RectTransform templateRect = heartSlotTemplate.rectTransform;

            slotRect.anchorMin = templateRect.anchorMin;
            slotRect.anchorMax = templateRect.anchorMax;
            slotRect.pivot = templateRect.pivot;
            slotRect.sizeDelta = templateRect.sizeDelta;
            slotRect.localScale = templateRect.localScale;

            float width = templateRect.rect.width > 0f ? templateRect.rect.width : templateRect.sizeDelta.x;
            float defaultX = templateRect.anchoredPosition.x + (index * (width + slotSpacing));
            slotRect.anchoredPosition = new Vector2(defaultX, templateRect.anchoredPosition.y);
        }

        private void EnsureRuntimeHeartSprites()
        {
            if (filledHeartSprite == null)
            {
                filledHeartSprite = GetRuntimeHeartSprite(true);
            }

            if (emptyHeartSprite == null)
            {
                emptyHeartSprite = GetRuntimeHeartSprite(false);
            }
        }

        private static Sprite GetRuntimeHeartSprite(bool filled)
        {
            if (filled && s_runtimeFilledHeartSprite != null)
            {
                return s_runtimeFilledHeartSprite;
            }

            if (!filled && s_runtimeEmptyHeartSprite != null)
            {
                return s_runtimeEmptyHeartSprite;
            }

            const int size = 16;
            Texture2D texture = new(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

            Color clear = new(0f, 0f, 0f, 0f);
            Color pixel = filled ? Color.white : new Color(1f, 1f, 1f, 0.72f);
            string[] mask =
            {
                "0000000000000000",
                "0000110011000000",
                "0001111111110000",
                "0011111111111000",
                "0111111111111100",
                "0111111111111100",
                "0011111111111000",
                "0001111111110000",
                "0000111111100000",
                "0000011111000000",
                "0000001110000000",
                "0000000100000000",
                "0000000000000000",
                "0000000000000000",
                "0000000000000000",
                "0000000000000000",
            };

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

            if (filled)
            {
                s_runtimeFilledHeartSprite = sprite;
            }
            else
            {
                s_runtimeEmptyHeartSprite = sprite;
            }

            return sprite;
        }

        private void ApplyThreatTheme(float unscaledTime)
        {
            if (_hasHealthSnapshot && healthValueText != null)
            {
                healthValueText.text = FormatHealthLabel(_currentHealth, _currentMaxHealth, unscaledTime);
            }

            for (int i = 0; i < _runtimeSlots.Count; i++)
            {
                Image slotImage = _runtimeSlots[i];
                if (slotImage == null || !slotImage.gameObject.activeSelf)
                {
                    continue;
                }

                slotImage.color = ResolveHeartColor(i < _currentFilledSlots, unscaledTime);
            }
        }

        private string FormatHealthLabel(int currentHealth, int maxHealth, float unscaledTime)
        {
            string labelHex = ColorUtility.ToHtmlStringRGBA(ResolveLabelColor(unscaledTime));
            string currentHex = ColorUtility.ToHtmlStringRGBA(ResolveCurrentHealthColor(currentHealth, maxHealth, unscaledTime));
            string maxHex = ColorUtility.ToHtmlStringRGBA(ResolveMaxHealthColor(unscaledTime));

            if (_compactTopBarMode)
            {
                return
                    $"<size={compactTopBarLabelFontSize}><color=#{labelHex}>HP</color></size> " +
                    $"<size={compactTopBarHealthValueFontSize}><b><color=#{currentHex}>{currentHealth}</color></b></size>" +
                    $"<size={compactTopBarMaxValueFontSize}><color=#{maxHex}>/{maxHealth}</color></size>";
            }

            return
                $"<size={topBarLabelFontSize}><color=#{labelHex}>HP</color></size>\n" +
                $"<size={topBarHealthValueFontSize}><b><color=#{currentHex}>{currentHealth}</color></b></size>" +
                $"<size={topBarLabelFontSize + 2}><color=#{maxHex}> / {maxHealth}</color></size>";
        }

        private Color ResolveHeartColor(bool isFilled, float unscaledTime)
        {
            Color baseColor = isFilled
                ? (UsesChallengeBaselineTheme() ? challengeBaselineFilledHeartColor : filledHeartColor)
                : (UsesChallengeBaselineTheme() ? challengeBaselineEmptyHeartColor : emptyHeartColor);
            if (!_hasChallengeThreatTheme)
            {
                return ApplyCompactAlpha(baseColor, isFilled ? compactFilledHeartAlphaScale : compactEmptyHeartAlphaScale);
            }

            float stageStrength = Mathf.Clamp01((ChallengeThreatPresentationResolver.ResolveStatusThemeStrength(_challengeThreatStage) - 0.92f) / 0.33f);
            float pulse = 0.5f + (0.5f * Mathf.Sin(unscaledTime * (1.05f + ChallengeThreatPresentationResolver.ResolveBannerPulseCycles(_challengeThreatStage))));
            float tint = challengeHeartTintStrength * (_compactTopBarMode ? topBarThreatPulseDamping : 1f) * Mathf.Lerp(0.78f, 1.08f, stageStrength) * Mathf.Lerp(0.88f, 1.12f, pulse);
            float accentBlend = isFilled ? 0.48f : 0.24f;
            return ApplyCompactAlpha(
                Color.Lerp(baseColor, Color.Lerp(baseColor, _challengeThreatAccentColor, accentBlend), Mathf.Clamp01(tint)),
                isFilled ? compactFilledHeartAlphaScale : compactEmptyHeartAlphaScale);
        }

        private Color ResolveLabelColor(float unscaledTime)
        {
            if (!_hasChallengeThreatTheme)
            {
                return ApplyCompactAlpha(healthLabelColor, compactLabelAlphaScale);
            }

            float pulse = 0.5f + (0.5f * Mathf.Sin(unscaledTime * (0.88f + ChallengeThreatPresentationResolver.ResolveBannerPulseCycles(_challengeThreatStage))));
            float tint = challengeLabelTintStrength * (_compactTopBarMode ? topBarThreatPulseDamping : 1f) * Mathf.Lerp(0.84f, 1.1f, pulse);
            Color baseLabelColor = UsesChallengeBaselineTheme() ? challengeBaselineLabelColor : healthLabelColor;
            return ApplyCompactAlpha(Color.Lerp(baseLabelColor, Color.Lerp(Color.white, _challengeThreatAccentColor, 0.18f), Mathf.Clamp01(tint)), compactLabelAlphaScale);
        }

        private Color ResolveCurrentHealthColor(int currentHealth, int maxHealth, float unscaledTime)
        {
            Color baseColor = currentHealth <= Mathf.Max(1, maxHealth) * 0.25f ? criticalHealthColor : currentHealthColor;
            if (!_hasChallengeThreatTheme)
            {
                return ApplyCompactAlpha(baseColor, compactCurrentHealthAlphaScale);
            }

            float stageStrength = Mathf.Clamp01((ChallengeThreatPresentationResolver.ResolveStatusThemeStrength(_challengeThreatStage) - 0.92f) / 0.33f);
            float pulse = 0.5f + (0.5f * Mathf.Sin(unscaledTime * (1.14f + ChallengeThreatPresentationResolver.ResolveBannerPulseCycles(_challengeThreatStage)) * Mathf.PI * 0.5f));
            float tint = challengeValueTintStrength * (_compactTopBarMode ? topBarThreatPulseDamping : 1f) * Mathf.Lerp(0.76f, 1.1f, stageStrength) * Mathf.Lerp(0.88f, 1.16f, pulse);
            if (UsesChallengeBaselineTheme() && currentHealth > Mathf.Max(1, maxHealth) * 0.25f)
            {
                baseColor = challengeBaselineCurrentHealthColor;
            }
            return ApplyCompactAlpha(Color.Lerp(baseColor, Color.Lerp(baseColor, _challengeThreatAccentColor, 0.54f), Mathf.Clamp01(tint)), compactCurrentHealthAlphaScale);
        }

        private Color ResolveMaxHealthColor(float unscaledTime)
        {
            if (!_hasChallengeThreatTheme)
            {
                return ApplyCompactAlpha(maxHealthColor, compactMaxHealthAlphaScale);
            }

            float pulse = 0.5f + (0.5f * Mathf.Sin(unscaledTime * (0.92f + ChallengeThreatPresentationResolver.ResolveBannerPulseCycles(_challengeThreatStage))));
            float tint = challengeLabelTintStrength * (_compactTopBarMode ? topBarThreatPulseDamping : 1f) * Mathf.Lerp(0.82f, 1.08f, pulse);
            Color baseColor = UsesChallengeBaselineTheme() ? challengeBaselineMaxHealthColor : maxHealthColor;
            return ApplyCompactAlpha(Color.Lerp(baseColor, Color.Lerp(baseColor, _challengeThreatAccentColor, 0.24f), Mathf.Clamp01(tint)), compactMaxHealthAlphaScale);
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

        private void ApplyCompactBackdropVisibility()
        {
            if (panelRoot == null)
            {
                return;
            }

            Image[] images = panelRoot.GetComponentsInChildren<Image>(true);
            for (int i = 0; i < images.Length; i++)
            {
                Image image = images[i];
                if (image == null)
                {
                    continue;
                }

                bool isHeartImage = image == heartSlotTemplate || _runtimeSlots.Contains(image);
                if (isHeartImage)
                {
                    image.enabled = true;
                    continue;
                }

                image.enabled = !_compactTopBarMode;
            }
        }
    }
}
