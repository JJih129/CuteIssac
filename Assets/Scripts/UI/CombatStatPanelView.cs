using CuteIssac.Player;
using CuteIssac.Room;
using UnityEngine;
using UnityEngine.UI;

namespace CuteIssac.UI
{
    /// <summary>
    /// Presentation-only panel for compact combat stats shown in the top HUD bar.
    /// Designers can swap the root, background, and text styling without touching gameplay code.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CombatStatPanelView : MonoBehaviour
    {
        [Header("Optional Root")]
        [SerializeField] private GameObject panelRoot;

        [Header("Skinnable Elements")]
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Text attackValueText;
        [SerializeField] private Text fireRateValueText;
        [SerializeField] private Text projectileSpeedValueText;
        [SerializeField] private Text luckValueText;

        [Header("Top Bar Layout")]
        [SerializeField] [Min(0f)] private float topBarPadding = 8f;
        [SerializeField] [Min(0f)] private float topBarBackgroundTopInset = 8f;
        [SerializeField] [Min(0f)] private float compactTopBarBackgroundTopInset = 10f;
        [SerializeField] [Min(0f)] private float topBarColumnGap = 8f;
        [SerializeField] [Min(0f)] private float compactTopBarColumnGap = 6f;
        [SerializeField] [Min(0f)] private float topBarRowGap = 6f;
        [SerializeField] [Min(0f)] private float compactTopBarRowGap = 5f;
        [SerializeField] [Min(0f)] private float topBarContentTopInset = 10f;
        [SerializeField] [Min(0f)] private float compactTopBarContentTopInset = 12f;
        [SerializeField] [Min(48f)] private float compactTopBarColumnWidth = 76f;
        [SerializeField] [Min(0)] private int topBarFontSize = 20;
        [SerializeField] [Min(0)] private int topBarLabelFontSize = 12;
        [SerializeField] [Min(0)] private int topBarValueFontSize = 24;
        [SerializeField] [Min(0)] private int compactTopBarLabelFontSize = 13;
        [SerializeField] [Min(0)] private int compactTopBarValueFontSize = 22;
        [SerializeField] [Min(20f)] private float topBarRowHeight = 38f;
        [SerializeField] [Min(20f)] private float compactTopBarRowHeight = 34f;
        [SerializeField] [Min(24f)] private float topBarBackgroundHeight = 86f;
        [SerializeField] [Min(24f)] private float compactTopBarBackgroundHeight = 78f;
        [SerializeField] [Min(0.5f)] private float topBarLineSpacing = 0.9f;

        [Header("Styling")]
        [SerializeField] private Color panelTint = new(0.2f, 0.28f, 0.38f, 0.24f);
        [SerializeField] private Color statTextColor = new(0.98f, 0.99f, 1f, 1f);
        [SerializeField] private Color statLabelColor = new(0.96f, 0.98f, 1f, 0.78f);
        [SerializeField] private Color statValueColor = new(1f, 0.9f, 0.58f, 1f);
        [SerializeField] private Color challengeBaselinePanelTint = new(0.34f, 0.22f, 0.1f, 0.24f);
        [SerializeField] private Color challengeBaselineLabelColor = new(1f, 0.94f, 0.82f, 0.78f);
        [SerializeField] private Color challengeBaselineValueColor = new(1f, 0.9f, 0.62f, 1f);
        [SerializeField] [Range(0.5f, 1.2f)] private float compactPanelAlphaScale = 0.84f;
        [SerializeField] [Range(0.5f, 1.2f)] private float compactLabelAlphaScale = 1.02f;
        [SerializeField] [Range(0.5f, 1.2f)] private float compactValueAlphaScale = 1.08f;
        [SerializeField] [Range(0f, 1f)] private float challengePanelTintStrength = 0.42f;
        [SerializeField] [Range(0f, 1f)] private float challengeValueTintStrength = 0.58f;
        [SerializeField] [Min(0f)] private float challengePulseAmplitude = 0.12f;
        [SerializeField] private Color compactTextShadowColor = new(0.02f, 0.05f, 0.08f, 0.74f);
        [SerializeField] private Vector2 compactTextShadowDistance = new(1.25f, -1.25f);

        private bool _compactTopBarMode;
        private bool _hasChallengeThreatTheme;
        private Color _challengeThreatAccentColor = Color.white;
        private string _challengeThreatBadgeLabel = string.Empty;
        private ChallengeThreatStage _challengeThreatStage;

        public void ConfigureRuntimeView(
            GameObject root,
            Image background,
            Text attackValue,
            Text fireRateValue,
            Text projectileSpeedValue,
            Text luckValue)
        {
            panelRoot = root;
            backgroundImage = background;
            attackValueText = attackValue;
            fireRateValueText = fireRateValue;
            projectileSpeedValueText = projectileSpeedValue;
            luckValueText = luckValue;
        }

        public void ShowPlaceholder()
        {
            if (panelRoot != null)
            {
                panelRoot.SetActive(true);
            }

            if (backgroundImage != null)
            {
                backgroundImage.color = panelTint;
            }

            if (attackValueText != null)
            {
                attackValueText.text = FormatStat("DMG", "--");
            }

            if (fireRateValueText != null)
            {
                fireRateValueText.text = FormatStat("RATE", "--");
            }

            if (projectileSpeedValueText != null)
            {
                projectileSpeedValueText.text = FormatStat("SPD", "--");
            }

            if (luckValueText != null)
            {
                luckValueText.text = FormatStat("LUCK", "--");
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

        public void SetStats(PlayerStatSnapshot snapshot)
        {
            if (panelRoot != null)
            {
                panelRoot.SetActive(true);
            }

            if (backgroundImage != null)
            {
                backgroundImage.color = panelTint;
            }

            if (attackValueText != null)
            {
                attackValueText.text = FormatStat("DMG", snapshot.Damage.ToString("0.0"));
            }

            if (fireRateValueText != null)
            {
                fireRateValueText.text = FormatStat("RATE", ResolveShotsPerSecond(snapshot.FireInterval).ToString("0.0"));
            }

            if (projectileSpeedValueText != null)
            {
                projectileSpeedValueText.text = FormatStat("SPD", snapshot.ProjectileSpeed.ToString("0.0"));
            }

            if (luckValueText != null)
            {
                luckValueText.text = FormatStat("LUCK", snapshot.Luck.ToString("0.0"));
            }

            ApplyThreatTheme(Time.unscaledTime);
        }

        private static float ResolveShotsPerSecond(float fireInterval)
        {
            return fireInterval > 0.001f
                ? 1f / fireInterval
                : 0f;
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

            float width = rootRect.rect.width > 0f ? rootRect.rect.width : rootRect.sizeDelta.x;
            LayoutBackground(rootRect, width);
            float columnGap = _compactTopBarMode ? compactTopBarColumnGap : topBarColumnGap;
            float rowGap = _compactTopBarMode ? compactTopBarRowGap : topBarRowGap;
            float columnWidth = _compactTopBarMode
                ? compactTopBarColumnWidth
                : Mathf.Max(68f, (width - (topBarPadding * 2f) - columnGap) * 0.5f);
            float contentTopInset = _compactTopBarMode ? compactTopBarContentTopInset : topBarContentTopInset;
            float rowHeight = _compactTopBarMode ? compactTopBarRowHeight : topBarRowHeight;
            float compactBlockWidth = (columnWidth * 2f) + columnGap;
            float leftInset = _compactTopBarMode
                ? Mathf.Max(topBarPadding, (width - compactBlockWidth) * 0.5f)
                : topBarPadding;
            float secondRowTop = contentTopInset + rowHeight + rowGap;

            LayoutStatText(attackValueText, new Vector2(leftInset, -contentTopInset), columnWidth, rowHeight);
            LayoutStatText(fireRateValueText, new Vector2(leftInset + columnWidth + columnGap, -contentTopInset), columnWidth, rowHeight);
            LayoutStatText(projectileSpeedValueText, new Vector2(leftInset, -secondRowTop), columnWidth, rowHeight);
            LayoutStatText(luckValueText, new Vector2(leftInset + columnWidth + columnGap, -secondRowTop), columnWidth, rowHeight);
            ApplyCompactTextShadow(attackValueText);
            ApplyCompactTextShadow(fireRateValueText);
            ApplyCompactTextShadow(projectileSpeedValueText);
            ApplyCompactTextShadow(luckValueText);
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
            ApplyThreatTheme(Time.unscaledTime);
        }

        private void LayoutStatText(Text text, Vector2 anchoredPosition, float columnWidth, float rowHeight)
        {
            if (text == null)
            {
                return;
            }

            RectTransform rectTransform = text.rectTransform;
            rectTransform.anchorMin = new Vector2(0f, 1f);
            rectTransform.anchorMax = new Vector2(0f, 1f);
            rectTransform.pivot = new Vector2(0f, 1f);
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.sizeDelta = new Vector2(columnWidth, rowHeight);
            text.fontSize = Mathf.Max(text.fontSize, topBarFontSize);
            text.alignment = _compactTopBarMode ? TextAnchor.MiddleCenter : TextAnchor.MiddleLeft;
            text.lineSpacing = topBarLineSpacing;
            text.color = statTextColor;
            text.supportRichText = true;
        }

        private void LayoutBackground(RectTransform rootRect, float width)
        {
            if (backgroundImage == null)
            {
                return;
            }

            RectTransform backgroundRect = backgroundImage.rectTransform;
            backgroundRect.anchorMin = new Vector2(0f, 1f);
            backgroundRect.anchorMax = new Vector2(0f, 1f);
            backgroundRect.pivot = new Vector2(0f, 1f);
            float topInset = _compactTopBarMode ? compactTopBarBackgroundTopInset : topBarBackgroundTopInset;
            float height = _compactTopBarMode ? compactTopBarBackgroundHeight : topBarBackgroundHeight;
            backgroundRect.anchoredPosition = new Vector2(0f, -topInset);
            backgroundRect.sizeDelta = new Vector2(width, height);
        }

        private string FormatStat(string label, string value)
        {
            Color labelColor = ResolveLabelColor(Time.unscaledTime);
            Color valueColor = ResolveValueColor(Time.unscaledTime);
            string labelHex = ColorUtility.ToHtmlStringRGBA(labelColor);
            string valueHex = ColorUtility.ToHtmlStringRGBA(valueColor);

            if (_compactTopBarMode)
            {
                string compactLabel = ResolveCompactLabel(label);
                return
                    $"<size={compactTopBarLabelFontSize}><color=#{labelHex}>{compactLabel}</color></size> " +
                    $"<size={compactTopBarValueFontSize}><b><color=#{valueHex}>{value}</color></b></size>";
            }

            return
                $"<size={topBarLabelFontSize}><color=#{labelHex}>{label}</color></size>\n" +
                $"<size={topBarValueFontSize}><b><color=#{valueHex}>{value}</color></b></size>";
        }

        private void ApplyThreatTheme(float unscaledTime)
        {
            if (backgroundImage == null)
            {
                return;
            }

            backgroundImage.color = ResolveBackgroundColor(unscaledTime);
        }

        private Color ResolveBackgroundColor(float unscaledTime)
        {
            if (!_hasChallengeThreatTheme)
            {
                return ApplyCompactAlpha(panelTint, compactPanelAlphaScale);
            }

            float stageStrength = Mathf.Clamp01((ChallengeThreatPresentationResolver.ResolveStatusThemeStrength(_challengeThreatStage) - 0.92f) / 0.33f);
            float pulseCycles = Mathf.Max(1.4f, ChallengeThreatPresentationResolver.ResolveBannerPulseCycles(_challengeThreatStage) - 0.8f);
            float pulse = 0.5f + (0.5f * Mathf.Sin(unscaledTime * (1.1f + pulseCycles)));
            float tintStrength = challengePanelTintStrength * Mathf.Lerp(0.72f, 1f, stageStrength) * Mathf.Lerp(0.82f, 1.08f, pulse);
            Color baseTint = UsesChallengeBaselineTheme() ? challengeBaselinePanelTint : panelTint;
            return ApplyCompactAlpha(Color.Lerp(baseTint, Color.Lerp(baseTint, _challengeThreatAccentColor, 0.34f), Mathf.Clamp01(tintStrength)), compactPanelAlphaScale);
        }

        private Color ResolveLabelColor(float unscaledTime)
        {
            if (!_hasChallengeThreatTheme)
            {
                return ApplyCompactAlpha(statLabelColor, compactLabelAlphaScale);
            }

            float pulse = 0.5f + (0.5f * Mathf.Sin(unscaledTime * (0.9f + ChallengeThreatPresentationResolver.ResolveBannerPulseCycles(_challengeThreatStage))));
            float tint = challengeValueTintStrength * 0.42f * Mathf.Lerp(0.85f, 1.1f, pulse);
            Color baseLabelColor = UsesChallengeBaselineTheme() ? challengeBaselineLabelColor : statLabelColor;
            return ApplyCompactAlpha(Color.Lerp(baseLabelColor, Color.Lerp(Color.white, _challengeThreatAccentColor, 0.24f), Mathf.Clamp01(tint)), compactLabelAlphaScale);
        }

        private Color ResolveValueColor(float unscaledTime)
        {
            if (!_hasChallengeThreatTheme)
            {
                return ApplyCompactAlpha(statValueColor, compactValueAlphaScale);
            }

            float stageStrength = Mathf.Clamp01((ChallengeThreatPresentationResolver.ResolveStatusThemeStrength(_challengeThreatStage) - 0.92f) / 0.33f);
            float pulse = 0.5f + (0.5f * Mathf.Sin(unscaledTime * (1.2f + ChallengeThreatPresentationResolver.ResolveBannerPulseCycles(_challengeThreatStage)) * Mathf.PI * 0.5f));
            float tint = challengeValueTintStrength * Mathf.Lerp(0.74f, 1.08f, stageStrength) * (1f + ((pulse - 0.5f) * challengePulseAmplitude));
            Color baseValueColor = UsesChallengeBaselineTheme() ? challengeBaselineValueColor : statValueColor;
            return ApplyCompactAlpha(Color.Lerp(baseValueColor, Color.Lerp(baseValueColor, _challengeThreatAccentColor, 0.7f), Mathf.Clamp01(tint)), compactValueAlphaScale);
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

        private static string ResolveCompactLabel(string label)
        {
            return label switch
            {
                "RATE" => "RPS",
                "LUCK" => "LUK",
                _ => label
            };
        }

        private bool UsesChallengeBaselineTheme()
        {
            return string.Equals(_challengeThreatBadgeLabel, "챌린지");
        }
    }
}
