using UnityEngine;
using UnityEngine.UI;

namespace CuteIssac.UI
{
    /// <summary>
    /// Presentation-only boss health bar panel.
    /// Hide it by default and let future boss encounter systems show it explicitly.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BossHpPanelView : MonoBehaviour
    {
        [Header("Optional Root")]
        [Tooltip("Optional. Root object for the entire boss HP presentation.")]
        [SerializeField] private GameObject panelRoot;

        [Header("Skinnable Elements")]
        [Tooltip("Optional decorative background image.")]
        [SerializeField] private Image backgroundImage;
        [Tooltip("Optional fill image for boss HP skins that use Image.fillAmount.")]
        [SerializeField] private Image fillImage;
        [Tooltip("Optional fill rect for skins that scale a rectangular bar instead of using Image.fillAmount.")]
        [SerializeField] private RectTransform fillRect;
        [Tooltip("Optional slider for boss HP skins that prefer Slider-driven bars.")]
        [SerializeField] private Slider fillSlider;
        [Tooltip("Optional boss name label.")]
        [SerializeField] private Text bossNameText;
        [Tooltip("Optional subtitle label for phase or pattern context.")]
        [SerializeField] private Text bossSubtitleText;
        [Tooltip("Optional category badge label shown above the boss name.")]
        [SerializeField] private Text bossBadgeText;
        [Tooltip("Optional boss status label such as danger level.")]
        [SerializeField] private Text bossStatusText;
        [Tooltip("Optional boss HP percentage label.")]
        [SerializeField] private Text bossHealthValueText;

        [Header("Fallback Subtitle")]
        [SerializeField] [Min(12)] private int fallbackSubtitleFontSize = 20;
        [SerializeField] private Color subtitleColor = new(1f, 0.88f, 0.62f, 0.96f);
        [Header("Accent Transition")]
        [SerializeField] [Min(1f)] private float accentTransitionSpeed = 10f;

        [Header("Top HUD Layout")]
        [SerializeField] private Vector2 topHudPanelSize = new(760f, 92f);
        [SerializeField] private Vector2 topHudNameSize = new(420f, 30f);
        [SerializeField] private Vector2 topHudSubtitleSize = new(420f, 24f);
        [SerializeField] private Vector2 topHudBadgeSize = new(120f, 20f);
        [SerializeField] private Vector2 topHudStatusSize = new(160f, 22f);
        [SerializeField] private Vector2 topHudValueSize = new(120f, 28f);
        [SerializeField] [Min(0)] private int topHudBadgeFontSize = 14;
        [SerializeField] [Min(0)] private int topHudNameFontSize = 28;
        [SerializeField] [Min(0)] private int topHudSubtitleFontSize = 18;
        [SerializeField] [Min(0)] private int topHudStatusFontSize = 16;
        [SerializeField] [Min(0)] private int topHudValueFontSize = 20;
        [SerializeField] [Min(12f)] private float topHudBarHeight = 22f;
        [SerializeField] [Min(8f)] private float compactHorizontalPadding = 18f;
        [SerializeField] [Min(96f)] private float compactMetricColumnWidth = 156f;
        [SerializeField] [Min(120f)] private float compactNameMinimumWidth = 240f;
        [SerializeField] [Min(0f)] private float compactNameTopOffset = 24f;
        [SerializeField] [Min(0f)] private float compactBadgeTopOffset = 8f;
        [SerializeField] [Min(0f)] private float compactSubtitleBottomOffset = 34f;
        [SerializeField] [Min(0f)] private float compactBarBottomOffset = 10f;

        [Header("Styling")]
        [SerializeField] private Color badgeColor = new(1f, 0.78f, 0.42f, 0.96f);
        [SerializeField] private Color statusColor = new(0.94f, 0.9f, 0.82f, 0.96f);
        [SerializeField] private Color healthValueColor = new(1f, 0.95f, 0.8f, 0.98f);
        [SerializeField] private Color criticalFillAccent = new(1f, 0.3f, 0.24f, 1f);
        [SerializeField] [Range(0f, 1f)] private float criticalHealthThreshold = 0.2f;

        private bool _warnedMissingPresentationRef;
        private bool _hasCachedDefaultColors;
        private Color _defaultBackgroundColor;
        private Color _defaultFillColor;
        private Color _defaultNameColor;
        private Color _defaultSubtitleColor;
        private Color _targetBackgroundColor;
        private Color _targetFillColor;
        private Color _targetNameColor;
        private Color _targetSubtitleColor;

        public void ConfigureDebugView(Text nameText, Image fill, Image background = null)
        {
            bossNameText = nameText;
            fillImage = fill;
            backgroundImage = background;
            panelRoot = nameText != null && nameText.transform.parent != null
                ? nameText.transform.parent.gameObject
                : panelRoot;
        }

        public void ShowBoss(string bossName, float normalizedHealth, string subtitle = "")
        {
            CacheDefaultColors();
            ShowBoss(
                bossName,
                normalizedHealth,
                subtitle,
                _defaultBackgroundColor,
                _defaultFillColor,
                _defaultNameColor,
                _defaultSubtitleColor);
        }

        public void ShowBoss(
            string bossName,
            float normalizedHealth,
            string subtitle,
            Color backgroundAccent,
            Color fillAccent,
            Color nameAccent,
            Color subtitleAccent)
        {
            ShowBoss(
                bossName,
                normalizedHealth,
                subtitle,
                backgroundAccent,
                fillAccent,
                nameAccent,
                subtitleAccent,
                null,
                null);
        }

        public void ShowBoss(
            string bossName,
            float normalizedHealth,
            string subtitle,
            Color backgroundAccent,
            Color fillAccent,
            Color nameAccent,
            Color subtitleAccent,
            string badgeOverride,
            string statusOverride)
        {
            if (panelRoot != null)
            {
                panelRoot.SetActive(true);
            }

            if (bossNameText == null && fillImage == null && fillSlider == null && bossHealthValueText == null && !_warnedMissingPresentationRef)
            {
                Debug.LogWarning("BossHpPanelView has no name or fill references assigned. The boss HUD can still reserve layout space, but it will not show meaningful data until references are connected.", this);
                _warnedMissingPresentationRef = true;
            }

            if (bossNameText != null)
            {
                bossNameText.text = bossName;
            }

            EnsureBadgeText();
            EnsureSubtitleText();
            EnsureStatusText();
            EnsureHealthValueText();
            SetAccent(backgroundAccent, fillAccent, nameAccent, subtitleAccent);

            if (bossBadgeText != null)
            {
                bossBadgeText.supportRichText = true;
                bossBadgeText.text = !string.IsNullOrWhiteSpace(badgeOverride)
                    ? badgeOverride
                    : ResolveBossBadgeText(subtitle);
                bossBadgeText.color = badgeColor;
            }

            if (bossSubtitleText != null)
            {
                bossSubtitleText.text = subtitle ?? string.Empty;
                bossSubtitleText.gameObject.SetActive(!string.IsNullOrWhiteSpace(subtitle));
            }

            float clamped = Mathf.Clamp01(normalizedHealth);

            if (bossStatusText != null)
            {
                bossStatusText.supportRichText = true;
                bossStatusText.text = !string.IsNullOrWhiteSpace(statusOverride)
                    ? statusOverride
                    : ResolveStatusText(clamped);
                bossStatusText.color = clamped <= criticalHealthThreshold ? criticalFillAccent : statusColor;
            }

            if (bossHealthValueText != null)
            {
                bossHealthValueText.supportRichText = true;
                bossHealthValueText.text = $"<size=16>HP</size>\n<size=28><b>{Mathf.RoundToInt(clamped * 100f)}%</b></size>";
            }

            if (clamped <= criticalHealthThreshold)
            {
                _targetFillColor = criticalFillAccent;
                _targetNameColor = Color.Lerp(nameAccent, criticalFillAccent, 0.45f);
            }

            if (fillSlider != null)
            {
                fillSlider.value = clamped;
            }

            if (fillImage != null)
            {
                fillImage.fillAmount = clamped;
            }

            if (fillRect != null)
            {
                Vector3 scale = fillRect.localScale;
                scale.x = clamped;
                fillRect.localScale = scale;
            }
        }

        public void HideBoss()
        {
            if (panelRoot != null)
            {
                panelRoot.SetActive(false);
            }

            CacheDefaultColors();
            SetAccent(_defaultBackgroundColor, _defaultFillColor, _defaultNameColor, _defaultSubtitleColor);
        }

        public void ApplyTopHudLayout(bool compactMode)
        {
            if (!compactMode)
            {
                return;
            }

            RectTransform rootRect = panelRoot != null
                ? panelRoot.transform as RectTransform
                : transform as RectTransform;
            RectTransform parentRect = rootRect != null ? rootRect.parent as RectTransform : null;
            float panelWidth = parentRect != null && parentRect.rect.width > 0f
                ? parentRect.rect.width
                : topHudPanelSize.x;
            float panelHeight = parentRect != null && parentRect.rect.height > 0f
                ? parentRect.rect.height
                : topHudPanelSize.y;
            float horizontalPadding = Mathf.Max(8f, compactHorizontalPadding);
            float metricColumnWidth = Mathf.Min(compactMetricColumnWidth, Mathf.Max(120f, panelWidth * 0.28f));
            float leftColumnWidth = Mathf.Max(
                compactNameMinimumWidth,
                panelWidth - metricColumnWidth - (horizontalPadding * 3f));
            float barWidth = Mathf.Max(180f, panelWidth - (horizontalPadding * 2f));

            if (rootRect != null)
            {
                if (parentRect != null)
                {
                    rootRect.anchorMin = Vector2.zero;
                    rootRect.anchorMax = Vector2.one;
                    rootRect.pivot = new Vector2(0.5f, 0.5f);
                    rootRect.offsetMin = Vector2.zero;
                    rootRect.offsetMax = Vector2.zero;
                }
                else
                {
                    rootRect.sizeDelta = topHudPanelSize;
                }
            }

            if (bossNameText != null)
            {
                RectTransform nameRect = bossNameText.rectTransform;
                nameRect.anchorMin = new Vector2(0f, 1f);
                nameRect.anchorMax = new Vector2(0f, 1f);
                nameRect.pivot = new Vector2(0f, 1f);
                nameRect.anchoredPosition = new Vector2(horizontalPadding, -compactNameTopOffset);
                nameRect.sizeDelta = new Vector2(leftColumnWidth, topHudNameSize.y);
                bossNameText.fontSize = Mathf.Max(bossNameText.fontSize, topHudNameFontSize);
                bossNameText.alignment = TextAnchor.MiddleLeft;
            }

            EnsureSubtitleText();
            EnsureBadgeText();
            EnsureStatusText();
            EnsureHealthValueText();

            if (bossBadgeText != null)
            {
                RectTransform badgeRect = bossBadgeText.rectTransform;
                badgeRect.anchorMin = new Vector2(0f, 1f);
                badgeRect.anchorMax = new Vector2(0f, 1f);
                badgeRect.pivot = new Vector2(0f, 1f);
                badgeRect.anchoredPosition = new Vector2(horizontalPadding, -compactBadgeTopOffset);
                badgeRect.sizeDelta = new Vector2(Mathf.Min(topHudBadgeSize.x, leftColumnWidth * 0.42f), topHudBadgeSize.y);
                bossBadgeText.fontSize = Mathf.Max(bossBadgeText.fontSize, topHudBadgeFontSize);
                bossBadgeText.alignment = TextAnchor.MiddleLeft;
                bossBadgeText.color = badgeColor;
            }

            if (bossSubtitleText != null)
            {
                RectTransform subtitleRect = bossSubtitleText.rectTransform;
                subtitleRect.anchorMin = new Vector2(0f, 1f);
                subtitleRect.anchorMax = new Vector2(0f, 1f);
                subtitleRect.pivot = new Vector2(0f, 1f);
                subtitleRect.anchoredPosition = new Vector2(horizontalPadding, -(panelHeight - compactSubtitleBottomOffset));
                subtitleRect.sizeDelta = new Vector2(leftColumnWidth, topHudSubtitleSize.y);
                bossSubtitleText.fontSize = Mathf.Max(bossSubtitleText.fontSize, topHudSubtitleFontSize);
                bossSubtitleText.alignment = TextAnchor.MiddleLeft;
            }

            if (bossStatusText != null)
            {
                RectTransform statusRect = bossStatusText.rectTransform;
                statusRect.anchorMin = new Vector2(1f, 1f);
                statusRect.anchorMax = new Vector2(1f, 1f);
                statusRect.pivot = new Vector2(1f, 1f);
                statusRect.anchoredPosition = new Vector2(-horizontalPadding, -14f);
                statusRect.sizeDelta = new Vector2(metricColumnWidth, topHudStatusSize.y);
                bossStatusText.fontSize = Mathf.Max(bossStatusText.fontSize, topHudStatusFontSize);
                bossStatusText.alignment = TextAnchor.MiddleRight;
            }

            if (bossHealthValueText != null)
            {
                RectTransform valueRect = bossHealthValueText.rectTransform;
                valueRect.anchorMin = new Vector2(1f, 1f);
                valueRect.anchorMax = new Vector2(1f, 1f);
                valueRect.pivot = new Vector2(1f, 1f);
                valueRect.anchoredPosition = new Vector2(-horizontalPadding, -40f);
                valueRect.sizeDelta = new Vector2(metricColumnWidth, topHudValueSize.y);
                bossHealthValueText.fontSize = Mathf.Max(bossHealthValueText.fontSize, topHudValueFontSize);
                bossHealthValueText.alignment = TextAnchor.MiddleRight;
                bossHealthValueText.color = healthValueColor;
            }

            if (fillSlider != null)
            {
                RectTransform sliderRect = fillSlider.GetComponent<RectTransform>();
                sliderRect.anchorMin = new Vector2(0.5f, 0f);
                sliderRect.anchorMax = new Vector2(0.5f, 0f);
                sliderRect.pivot = new Vector2(0.5f, 0f);
                sliderRect.anchoredPosition = new Vector2(0f, compactBarBottomOffset);
                sliderRect.sizeDelta = new Vector2(barWidth, topHudBarHeight);
            }

            if (fillRect != null)
            {
                RectTransform barParentRect = fillRect.parent as RectTransform;

                if (barParentRect != null)
                {
                    barParentRect.anchorMin = new Vector2(0.5f, 0f);
                    barParentRect.anchorMax = new Vector2(0.5f, 0f);
                    barParentRect.pivot = new Vector2(0.5f, 0f);
                    barParentRect.anchoredPosition = new Vector2(0f, compactBarBottomOffset);
                    barParentRect.sizeDelta = new Vector2(barWidth, topHudBarHeight);
                }
            }
        }

        private void Update()
        {
            CacheDefaultColors();
            float step = Mathf.Clamp01(Time.unscaledDeltaTime * accentTransitionSpeed);

            if (backgroundImage != null)
            {
                backgroundImage.color = Color.Lerp(backgroundImage.color, _targetBackgroundColor, step);
            }

            if (fillImage != null)
            {
                fillImage.color = Color.Lerp(fillImage.color, _targetFillColor, step);
            }

            if (bossNameText != null)
            {
                bossNameText.color = Color.Lerp(bossNameText.color, _targetNameColor, step);
            }

            if (bossSubtitleText != null)
            {
                bossSubtitleText.color = Color.Lerp(bossSubtitleText.color, _targetSubtitleColor, step);
            }
        }

        private void EnsureSubtitleText()
        {
            if (bossSubtitleText != null)
            {
                return;
            }

            RectTransform parentRect = null;

            if (bossNameText != null && bossNameText.transform.parent is RectTransform nameParent)
            {
                parentRect = nameParent;
            }
            else if (panelRoot != null && panelRoot.transform is RectTransform panelRect)
            {
                parentRect = panelRect;
            }
            else if (transform is RectTransform selfRect)
            {
                parentRect = selfRect;
            }

            if (parentRect == null)
            {
                return;
            }

            GameObject subtitleObject = new("BossSubtitle", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            subtitleObject.transform.SetParent(parentRect, false);
            RectTransform subtitleRect = subtitleObject.GetComponent<RectTransform>();
            subtitleRect.anchorMin = new Vector2(0.5f, 1f);
            subtitleRect.anchorMax = new Vector2(0.5f, 1f);
            subtitleRect.pivot = new Vector2(0.5f, 1f);
            subtitleRect.anchoredPosition = new Vector2(0f, -30f);
            subtitleRect.sizeDelta = new Vector2(560f, 28f);

            bossSubtitleText = subtitleObject.GetComponent<Text>();
            bossSubtitleText.font = LocalizedUiFontProvider.GetFont();
            bossSubtitleText.fontSize = fallbackSubtitleFontSize;
            bossSubtitleText.fontStyle = FontStyle.Bold;
            bossSubtitleText.alignment = TextAnchor.UpperCenter;
            bossSubtitleText.color = subtitleColor;
            bossSubtitleText.raycastTarget = false;
            bossSubtitleText.gameObject.SetActive(false);
            CacheDefaultColors();
        }

        private void EnsureBadgeText()
        {
            if (bossBadgeText != null)
            {
                return;
            }

            RectTransform parentRect = ResolveTextParent();

            if (parentRect == null)
            {
                return;
            }

            GameObject badgeObject = new("BossBadge", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            badgeObject.transform.SetParent(parentRect, false);
            RectTransform badgeRect = badgeObject.GetComponent<RectTransform>();
            badgeRect.anchorMin = new Vector2(0f, 1f);
            badgeRect.anchorMax = new Vector2(0f, 1f);
            badgeRect.pivot = new Vector2(0f, 1f);
            badgeRect.anchoredPosition = new Vector2(20f, -6f);
            badgeRect.sizeDelta = topHudBadgeSize;

            bossBadgeText = badgeObject.GetComponent<Text>();
            bossBadgeText.font = LocalizedUiFontProvider.GetFont();
            bossBadgeText.fontSize = topHudBadgeFontSize;
            bossBadgeText.fontStyle = FontStyle.Bold;
            bossBadgeText.alignment = TextAnchor.MiddleLeft;
            bossBadgeText.color = badgeColor;
            bossBadgeText.raycastTarget = false;
        }

        private void EnsureStatusText()
        {
            if (bossStatusText != null)
            {
                return;
            }

            RectTransform parentRect = ResolveTextParent();

            if (parentRect == null)
            {
                return;
            }

            GameObject statusObject = new("BossStatus", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            statusObject.transform.SetParent(parentRect, false);
            RectTransform statusRect = statusObject.GetComponent<RectTransform>();
            statusRect.anchorMin = new Vector2(1f, 1f);
            statusRect.anchorMax = new Vector2(1f, 1f);
            statusRect.pivot = new Vector2(1f, 1f);
            statusRect.anchoredPosition = new Vector2(-20f, -14f);
            statusRect.sizeDelta = topHudStatusSize;

            bossStatusText = statusObject.GetComponent<Text>();
            bossStatusText.font = LocalizedUiFontProvider.GetFont();
            bossStatusText.fontSize = topHudStatusFontSize;
            bossStatusText.fontStyle = FontStyle.Bold;
            bossStatusText.alignment = TextAnchor.MiddleRight;
            bossStatusText.color = statusColor;
            bossStatusText.raycastTarget = false;
        }

        private void EnsureHealthValueText()
        {
            if (bossHealthValueText != null)
            {
                return;
            }

            RectTransform parentRect = ResolveTextParent();

            if (parentRect == null)
            {
                return;
            }

            GameObject valueObject = new("BossHealthValue", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            valueObject.transform.SetParent(parentRect, false);
            RectTransform valueRect = valueObject.GetComponent<RectTransform>();
            valueRect.anchorMin = new Vector2(1f, 1f);
            valueRect.anchorMax = new Vector2(1f, 1f);
            valueRect.pivot = new Vector2(1f, 1f);
            valueRect.anchoredPosition = new Vector2(-20f, -14f);
            valueRect.sizeDelta = topHudValueSize;

            bossHealthValueText = valueObject.GetComponent<Text>();
            bossHealthValueText.font = LocalizedUiFontProvider.GetFont();
            bossHealthValueText.fontSize = topHudValueFontSize;
            bossHealthValueText.fontStyle = FontStyle.Bold;
            bossHealthValueText.alignment = TextAnchor.MiddleRight;
            bossHealthValueText.color = healthValueColor;
            bossHealthValueText.raycastTarget = false;
        }

        private RectTransform ResolveTextParent()
        {
            if (bossNameText != null && bossNameText.transform.parent is RectTransform nameParent)
            {
                return nameParent;
            }

            if (panelRoot != null && panelRoot.transform is RectTransform panelRect)
            {
                return panelRect;
            }

            return transform as RectTransform;
        }

        private void CacheDefaultColors()
        {
            if (_hasCachedDefaultColors)
            {
                return;
            }

            _defaultBackgroundColor = backgroundImage != null ? backgroundImage.color : Color.white;
            _defaultFillColor = fillImage != null ? fillImage.color : Color.white;
            _defaultNameColor = bossNameText != null ? bossNameText.color : Color.white;
            _defaultSubtitleColor = bossSubtitleText != null ? bossSubtitleText.color : subtitleColor;
            _targetBackgroundColor = _defaultBackgroundColor;
            _targetFillColor = _defaultFillColor;
            _targetNameColor = _defaultNameColor;
            _targetSubtitleColor = _defaultSubtitleColor;
            _hasCachedDefaultColors = true;
        }

        private void SetAccent(Color backgroundAccent, Color fillAccent, Color nameAccent, Color subtitleAccent)
        {
            _targetBackgroundColor = backgroundAccent;
            _targetFillColor = fillAccent;
            _targetNameColor = nameAccent;
            _targetSubtitleColor = subtitleAccent;
        }

        private static string ResolveBossBadgeText(string subtitle)
        {
            if (!string.IsNullOrWhiteSpace(subtitle))
            {
                return "<b>보스전</b>";
            }

            return "<b>위협 대상</b>";
        }

        private static string ResolveStatusText(float normalizedHealth)
        {
            if (normalizedHealth <= 0.2f)
            {
                return "<size=14>상태</size>\n<b>위험</b>";
            }

            if (normalizedHealth <= 0.5f)
            {
                return "<size=14>상태</size>\n<b>경계</b>";
            }

            return "<size=14>상태</size>\n<b>우세</b>";
        }
    }
}
