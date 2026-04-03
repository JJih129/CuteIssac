using CuteIssac.Combat;
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
        [SerializeField] private Text momentumStatusText;
        [SerializeField] private Text projectileTraitStatusText;

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
        [SerializeField] [Min(14f)] private float topBarMomentumRowHeight = 24f;
        [SerializeField] [Min(14f)] private float compactTopBarMomentumRowHeight = 18f;
        [SerializeField] [Min(14f)] private float topBarTraitRowHeight = 22f;
        [SerializeField] [Min(14f)] private float compactTopBarTraitRowHeight = 17f;
        [SerializeField] [Min(24f)] private float topBarBackgroundHeight = 86f;
        [SerializeField] [Min(24f)] private float compactTopBarBackgroundHeight = 78f;
        [SerializeField] [Min(0f)] private float topBarMomentumTopGap = 6f;
        [SerializeField] [Min(0f)] private float compactTopBarMomentumTopGap = 4f;
        [SerializeField] [Min(0f)] private float topBarTraitTopGap = 4f;
        [SerializeField] [Min(0f)] private float compactTopBarTraitTopGap = 3f;
        [SerializeField] [Min(0.5f)] private float topBarLineSpacing = 0.9f;

        [Header("Styling")]
        [SerializeField] private Color panelTint = new(0.2f, 0.28f, 0.38f, 0.24f);
        [SerializeField] private Color statTextColor = new(0.98f, 0.99f, 1f, 1f);
        [SerializeField] private Color statLabelColor = new(0.96f, 0.98f, 1f, 0.78f);
        [SerializeField] private Color statValueColor = new(1f, 0.9f, 0.58f, 1f);
        [SerializeField] private Color challengeBaselinePanelTint = new(0.34f, 0.22f, 0.1f, 0.24f);
        [SerializeField] private Color challengeBaselineLabelColor = new(1f, 0.94f, 0.82f, 0.78f);
        [SerializeField] private Color challengeBaselineValueColor = new(1f, 0.9f, 0.62f, 1f);
        [SerializeField] private Color momentumReadyLabelColor = new(0.82f, 0.94f, 1f, 0.9f);
        [SerializeField] private Color momentumReadyValueColor = new(0.96f, 1f, 0.98f, 1f);
        [SerializeField] [Range(0f, 1f)] private float momentumPanelTintStrength = 0.24f;
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
        private PlayerCombatMomentumController _momentumController;
        private PlayerRoutePlanCarryController _routePlanCarryController;
        private PlayerStats _playerStats;

        public void ConfigureRuntimeView(
            GameObject root,
            Image background,
            Text attackValue,
            Text fireRateValue,
            Text projectileSpeedValue,
            Text luckValue,
            Text momentumStatus = null,
            Text projectileTraitStatus = null)
        {
            panelRoot = root;
            backgroundImage = background;
            attackValueText = attackValue;
            fireRateValueText = fireRateValue;
            projectileSpeedValueText = projectileSpeedValue;
            luckValueText = luckValue;
            momentumStatusText = momentumStatus;
            projectileTraitStatusText = projectileTraitStatus;
        }

        public void SetMomentumStateSource(PlayerCombatMomentumController momentumController)
        {
            _momentumController = momentumController;
            EnsureMomentumStatusText();
            RefreshMomentumStatus(Time.unscaledTime);
        }

        public void SetRoutePlanStateSource(PlayerRoutePlanCarryController routePlanCarryController)
        {
            _routePlanCarryController = routePlanCarryController;
            EnsureMomentumStatusText();
            RefreshMomentumStatus(Time.unscaledTime);
        }

        public void SetProjectileTraitStateSource(PlayerStats playerStats)
        {
            _playerStats = playerStats;
            EnsureProjectileTraitStatusText();
            RefreshProjectileTraitStatus(Time.unscaledTime);
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

            RefreshMomentumStatus(Time.unscaledTime);
            RefreshProjectileTraitStatus(Time.unscaledTime);
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

            RefreshMomentumStatus(Time.unscaledTime);
            RefreshProjectileTraitStatus(Time.unscaledTime);
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
            float momentumTop = secondRowTop + rowHeight + (_compactTopBarMode ? compactTopBarMomentumTopGap : topBarMomentumTopGap);
            float momentumRowHeight = _compactTopBarMode ? compactTopBarMomentumRowHeight : topBarMomentumRowHeight;
            float traitTop = momentumTop + momentumRowHeight + (_compactTopBarMode ? compactTopBarTraitTopGap : topBarTraitTopGap);
            float traitRowHeight = _compactTopBarMode ? compactTopBarTraitRowHeight : topBarTraitRowHeight;

            LayoutStatText(attackValueText, new Vector2(leftInset, -contentTopInset), columnWidth, rowHeight);
            LayoutStatText(fireRateValueText, new Vector2(leftInset + columnWidth + columnGap, -contentTopInset), columnWidth, rowHeight);
            LayoutStatText(projectileSpeedValueText, new Vector2(leftInset, -secondRowTop), columnWidth, rowHeight);
            LayoutStatText(luckValueText, new Vector2(leftInset + columnWidth + columnGap, -secondRowTop), columnWidth, rowHeight);
            LayoutStatusText(momentumStatusText, new Vector2(leftInset, -momentumTop), compactBlockWidth, momentumRowHeight);
            LayoutStatusText(projectileTraitStatusText, new Vector2(leftInset, -traitTop), compactBlockWidth, traitRowHeight);
            ApplyCompactTextShadow(attackValueText);
            ApplyCompactTextShadow(fireRateValueText);
            ApplyCompactTextShadow(projectileSpeedValueText);
            ApplyCompactTextShadow(luckValueText);
            ApplyCompactTextShadow(momentumStatusText);
            ApplyCompactTextShadow(projectileTraitStatusText);
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
            RefreshMomentumStatus(Time.unscaledTime);
            RefreshProjectileTraitStatus(Time.unscaledTime);
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

            if (momentumStatusText != null)
            {
                height += (_compactTopBarMode ? compactTopBarMomentumRowHeight : topBarMomentumRowHeight)
                    + (_compactTopBarMode ? compactTopBarMomentumTopGap : topBarMomentumTopGap);
            }

            if (projectileTraitStatusText != null)
            {
                height += (_compactTopBarMode ? compactTopBarTraitRowHeight : topBarTraitRowHeight)
                    + (_compactTopBarMode ? compactTopBarTraitTopGap : topBarTraitTopGap);
            }

            backgroundRect.anchoredPosition = new Vector2(0f, -topInset);
            backgroundRect.sizeDelta = new Vector2(width, height);
        }

        private void LayoutStatusText(Text text, Vector2 anchoredPosition, float width, float rowHeight)
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
            rectTransform.sizeDelta = new Vector2(width, rowHeight);
            text.alignment = TextAnchor.MiddleCenter;
            text.fontSize = _compactTopBarMode ? compactTopBarLabelFontSize : topBarLabelFontSize;
            text.lineSpacing = 1f;
            text.supportRichText = true;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;
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
            Color resolvedColor;

            if (!_hasChallengeThreatTheme)
            {
                resolvedColor = panelTint;
            }
            else
            {
                float stageStrength = Mathf.Clamp01((ChallengeThreatPresentationResolver.ResolveStatusThemeStrength(_challengeThreatStage) - 0.92f) / 0.33f);
                float pulseCycles = Mathf.Max(1.4f, ChallengeThreatPresentationResolver.ResolveBannerPulseCycles(_challengeThreatStage) - 0.8f);
                float pulse = 0.5f + (0.5f * Mathf.Sin(unscaledTime * (1.1f + pulseCycles)));
                float tintStrength = challengePanelTintStrength * Mathf.Lerp(0.72f, 1f, stageStrength) * Mathf.Lerp(0.82f, 1.08f, pulse);
                Color baseTint = UsesChallengeBaselineTheme() ? challengeBaselinePanelTint : panelTint;
                resolvedColor = Color.Lerp(baseTint, Color.Lerp(baseTint, _challengeThreatAccentColor, 0.34f), Mathf.Clamp01(tintStrength));
            }

            if (_momentumController != null && _momentumController.IsMomentumActive)
            {
                float momentumPulse = 0.5f + (0.5f * Mathf.Sin(unscaledTime * 6.4f));
                float momentumStrength = momentumPanelTintStrength
                    * Mathf.Lerp(0.72f, 1f, momentumPulse)
                    * Mathf.Lerp(0.7f, 1f, _momentumController.RemainingDurationNormalized);
                resolvedColor = Color.Lerp(resolvedColor, Color.Lerp(resolvedColor, _momentumController.AccentColor, 0.46f), Mathf.Clamp01(momentumStrength));
            }

            return ApplyCompactAlpha(resolvedColor, compactPanelAlphaScale);
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

        private void EnsureMomentumStatusText()
        {
            if (momentumStatusText != null)
            {
                return;
            }

            Transform parent = panelRoot != null ? panelRoot.transform : transform;
            if (parent == null)
            {
                return;
            }

            GameObject textObject = new("MomentumStatus", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            textObject.transform.SetParent(parent, false);
            momentumStatusText = textObject.GetComponent<Text>();
            momentumStatusText.font = attackValueText != null && attackValueText.font != null
                ? attackValueText.font
                : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            LocalizedUiFontProvider.Apply(momentumStatusText);
            momentumStatusText.fontStyle = FontStyle.Bold;
            momentumStatusText.raycastTarget = false;
        }

        private void EnsureProjectileTraitStatusText()
        {
            if (projectileTraitStatusText != null)
            {
                return;
            }

            Transform parent = panelRoot != null ? panelRoot.transform : transform;
            if (parent == null)
            {
                return;
            }

            GameObject textObject = new("ProjectileTraitStatus", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            textObject.transform.SetParent(parent, false);
            projectileTraitStatusText = textObject.GetComponent<Text>();
            projectileTraitStatusText.font = attackValueText != null && attackValueText.font != null
                ? attackValueText.font
                : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            LocalizedUiFontProvider.Apply(projectileTraitStatusText);
            projectileTraitStatusText.fontStyle = FontStyle.Bold;
            projectileTraitStatusText.raycastTarget = false;
        }

        private void RefreshMomentumStatus(float unscaledTime)
        {
            EnsureMomentumStatusText();

            if (momentumStatusText == null)
            {
                return;
            }

            Color labelColor = ApplyCompactAlpha(momentumReadyLabelColor, compactLabelAlphaScale);
            Color valueColor = ApplyCompactAlpha(momentumReadyValueColor, compactValueAlphaScale);
            string headline = "FLOW";
            string detail = "READY";

            if (_routePlanCarryController != null && _routePlanCarryController.IsOpeningActive)
            {
                headline = _compactTopBarMode
                    ? (_routePlanCarryController.ActiveHeldPlan ? "OPEN" : "PIVOT")
                    : _routePlanCarryController.ActiveOpeningHeadline;

                int remainingPercent = Mathf.RoundToInt(_routePlanCarryController.RemainingOpeningNormalized * 100f);
                string openerLabel = ResolveRouteOpeningLabel(_routePlanCarryController.ActiveReasonTag);
                string recentPreferredDriveLabel = _routePlanCarryController.HasRecentPreferredImpactDrive
                    ? _routePlanCarryController.RecentPreferredImpactDriveCompactTag
                    : string.Empty;
                string recentPreferredHitLabel = _routePlanCarryController.HasRecentPreferredImpactHit
                    ? _routePlanCarryController.RecentPreferredImpactHitCompactTag
                    : string.Empty;
                string preferredHoldLabel = _routePlanCarryController.HasPreferredImpactHoldDrive
                    ? _routePlanCarryController.PreferredImpactHoldDriveCompactTag
                    : string.Empty;
                string targetCompactTag = _routePlanCarryController.HasOpeningTargetHint
                    ? _routePlanCarryController.OpeningTargetCompactTag
                    : string.Empty;
                string recentRoleBurstLabel = _routePlanCarryController.HasRecentOpeningCadenceHitRoleBurst
                    ? _routePlanCarryController.RecentOpeningCadenceHitRoleCompactTag
                    : string.Empty;
                string cadenceLabel = _routePlanCarryController.HasOpeningCadenceFeedback
                    ? _routePlanCarryController.OpeningCadenceLabel
                    : string.Empty;
                string resolvedTargetLabel = !string.IsNullOrWhiteSpace(recentPreferredDriveLabel)
                    ? recentPreferredDriveLabel
                    : !string.IsNullOrWhiteSpace(recentPreferredHitLabel)
                    ? recentPreferredHitLabel
                    : !string.IsNullOrWhiteSpace(preferredHoldLabel)
                    ? preferredHoldLabel
                    : !string.IsNullOrWhiteSpace(recentRoleBurstLabel)
                    ? recentRoleBurstLabel
                    : !string.IsNullOrWhiteSpace(cadenceLabel)
                    ? cadenceLabel
                    : !string.IsNullOrWhiteSpace(targetCompactTag)
                        ? targetCompactTag
                        : openerLabel;
                detail = _compactTopBarMode
                    ? $"{resolvedTargetLabel} {remainingPercent:00}%"
                    : $"{resolvedTargetLabel} OPEN {remainingPercent:00}%";
                Color routeAccent = _routePlanCarryController.HasRecentPreferredImpactDrive
                    ? _routePlanCarryController.RecentPreferredImpactDriveAccentColor
                    : _routePlanCarryController.HasRecentPreferredImpactHit
                    ? _routePlanCarryController.RecentPreferredImpactHitAccentColor
                    : _routePlanCarryController.HasPreferredImpactHoldDrive
                    ? _routePlanCarryController.PreferredImpactHoldDriveAccentColor
                    : _routePlanCarryController.ActiveAccentColor;
                labelColor = ApplyCompactAlpha(Color.Lerp(momentumReadyLabelColor, routeAccent, 0.6f), compactLabelAlphaScale);
                valueColor = ApplyCompactAlpha(Color.Lerp(momentumReadyValueColor, routeAccent, 0.38f), compactValueAlphaScale);
            }
            else if (_momentumController != null && _momentumController.IsMomentumActive)
            {
                headline = _momentumController.ChainCount > 1
                    ? $"FLOW x{_momentumController.ChainCount}"
                    : "FLOW OPEN";

                string formationLabel = ResolveMomentumFormationLabel(_momentumController.ActiveFormationId);
                int remainingPercent = Mathf.RoundToInt(_momentumController.RemainingDurationNormalized * 100f);
                detail = _compactTopBarMode
                    ? $"{formationLabel} {remainingPercent:00}%"
                    : $"{formationLabel} WINDOW {remainingPercent:00}%";
                labelColor = ApplyCompactAlpha(Color.Lerp(momentumReadyLabelColor, _momentumController.AccentColor, 0.58f), compactLabelAlphaScale);
                valueColor = ApplyCompactAlpha(Color.Lerp(momentumReadyValueColor, _momentumController.AccentColor, 0.34f), compactValueAlphaScale);
            }

            string labelHex = ColorUtility.ToHtmlStringRGBA(labelColor);
            string valueHex = ColorUtility.ToHtmlStringRGBA(valueColor);
            int labelSize = _compactTopBarMode ? compactTopBarLabelFontSize : topBarLabelFontSize;
            int valueSize = _compactTopBarMode ? compactTopBarValueFontSize - 6 : topBarValueFontSize - 8;
            momentumStatusText.text =
                $"<size={labelSize}><color=#{labelHex}>{headline}</color></size> " +
                $"<size={valueSize}><b><color=#{valueHex}>{detail}</color></b></size>";
        }

        private void RefreshProjectileTraitStatus(float unscaledTime)
        {
            EnsureProjectileTraitStatusText();

            if (projectileTraitStatusText == null)
            {
                return;
            }

            string detail = ResolveProjectileTraitDetail();
            Color accentColor = ResolveProjectileTraitAccent();
            Color labelColor = ApplyCompactAlpha(Color.Lerp(momentumReadyLabelColor, accentColor, 0.18f), compactLabelAlphaScale);
            Color valueColor = ApplyCompactAlpha(Color.Lerp(momentumReadyValueColor, accentColor, 0.52f), compactValueAlphaScale);
            float pulse = 0.5f + (0.5f * Mathf.Sin(unscaledTime * 4.6f));
            valueColor = Color.Lerp(valueColor, Color.Lerp(valueColor, accentColor, 0.28f), pulse * 0.32f);

            string labelHex = ColorUtility.ToHtmlStringRGBA(labelColor);
            string valueHex = ColorUtility.ToHtmlStringRGBA(valueColor);
            int labelSize = _compactTopBarMode ? compactTopBarLabelFontSize : topBarLabelFontSize;
            int valueSize = _compactTopBarMode ? compactTopBarValueFontSize - 8 : topBarValueFontSize - 10;
            projectileTraitStatusText.text =
                $"<size={labelSize}><color=#{labelHex}>SHOT</color></size> " +
                $"<size={valueSize}><b><color=#{valueHex}>{detail}</color></b></size>";
        }

        private string ResolveProjectileTraitDetail()
        {
            string recentPreferredDriveLabel = _routePlanCarryController != null && _routePlanCarryController.HasRecentPreferredImpactDrive
                ? _routePlanCarryController.RecentPreferredImpactDriveCompactTag
                : string.Empty;
            string recentPreferredHitLabel = _routePlanCarryController != null && _routePlanCarryController.HasRecentPreferredImpactHit
                ? _routePlanCarryController.RecentPreferredImpactHitCompactTag
                : string.Empty;
            string preferredHoldLabel = _routePlanCarryController != null && _routePlanCarryController.HasPreferredImpactHoldDrive
                ? _routePlanCarryController.PreferredImpactHoldDriveCompactTag
                : string.Empty;
            string recentRoleBurstLabel = _routePlanCarryController != null && _routePlanCarryController.HasRecentOpeningCadenceHitRoleBurst
                ? _routePlanCarryController.RecentOpeningCadenceHitRoleCompactTag
                : string.Empty;
            ProjectileTraitState traits = _playerStats != null
                ? _playerStats.CurrentProjectileTraits
                : ProjectileTraitState.Default;

            string[] labels = new string[7];
            int count = 0;

            if (traits.IsLaser)
            {
                labels[count++] = "LASER";
            }

            if (traits.IsOrbiting)
            {
                labels[count++] = "ORBIT";
            }

            if (traits.IsShielded)
            {
                labels[count++] = "SHIELD";
            }

            if (traits.IsSplit)
            {
                labels[count++] = "SPLIT";
            }

            if (traits.IsExplosive)
            {
                labels[count++] = "BLAST";
            }

            if (traits.Has(ProjectileTraitFlags.Lifesteal))
            {
                labels[count++] = "LEECH";
            }

            if (traits.Has(ProjectileTraitFlags.Bounce))
            {
                labels[count++] = "BOUNCE";
            }

            if (count == 0)
            {
                string baseLabel = _compactTopBarMode ? "BASE" : "BASELINE";
                return !string.IsNullOrWhiteSpace(recentPreferredDriveLabel)
                    ? $"{recentPreferredDriveLabel} / {baseLabel}"
                    : !string.IsNullOrWhiteSpace(recentPreferredHitLabel)
                    ? $"{recentPreferredHitLabel} / {baseLabel}"
                    : !string.IsNullOrWhiteSpace(preferredHoldLabel)
                    ? $"{preferredHoldLabel} / {baseLabel}"
                    : !string.IsNullOrWhiteSpace(recentRoleBurstLabel)
                    ? $"{recentRoleBurstLabel} / {baseLabel}"
                    : baseLabel;
            }

            string traitLabel;
            if (count <= 2)
            {
                traitLabel = count == 2
                    ? $"{labels[0]} / {labels[1]}"
                    : labels[0];
            }
            else
            {
                traitLabel = $"{labels[0]} / {labels[1]} / +{count - 2}";
            }

            return !string.IsNullOrWhiteSpace(recentPreferredDriveLabel)
                ? $"{recentPreferredDriveLabel} / {traitLabel}"
                : !string.IsNullOrWhiteSpace(recentPreferredHitLabel)
                ? $"{recentPreferredHitLabel} / {traitLabel}"
                : !string.IsNullOrWhiteSpace(preferredHoldLabel)
                ? $"{preferredHoldLabel} / {traitLabel}"
                : !string.IsNullOrWhiteSpace(recentRoleBurstLabel)
                ? $"{recentRoleBurstLabel} / {traitLabel}"
                : traitLabel;
        }

        private Color ResolveProjectileTraitAccent()
        {
            if (_routePlanCarryController != null && _routePlanCarryController.HasRecentPreferredImpactDrive)
            {
                return _routePlanCarryController.RecentPreferredImpactDriveAccentColor;
            }

            if (_routePlanCarryController != null && _routePlanCarryController.HasRecentPreferredImpactHit)
            {
                return _routePlanCarryController.RecentPreferredImpactHitAccentColor;
            }

            if (_routePlanCarryController != null && _routePlanCarryController.HasPreferredImpactHoldDrive)
            {
                return _routePlanCarryController.PreferredImpactHoldDriveAccentColor;
            }

            if (_routePlanCarryController != null && _routePlanCarryController.HasRecentOpeningCadenceHitRoleBurst)
            {
                return _routePlanCarryController.RecentOpeningCadenceHitRoleAccentColor;
            }

            ProjectileTraitState traits = _playerStats != null
                ? _playerStats.CurrentProjectileTraits
                : ProjectileTraitState.Default;

            if (traits.IsOrbiting)
            {
                return new Color(1f, 0.84f, 0.42f, 1f);
            }

            if (traits.IsShielded)
            {
                return new Color(0.46f, 0.92f, 1f, 1f);
            }

            if (traits.IsLaser)
            {
                return new Color(1f, 0.48f, 0.52f, 1f);
            }

            if (traits.IsExplosive)
            {
                return new Color(1f, 0.66f, 0.36f, 1f);
            }

            if (traits.Has(ProjectileTraitFlags.Lifesteal))
            {
                return new Color(0.56f, 1f, 0.62f, 1f);
            }

            if (traits.IsSplit)
            {
                return new Color(1f, 0.62f, 0.82f, 1f);
            }

            if (traits.Has(ProjectileTraitFlags.Bounce))
            {
                return new Color(0.86f, 0.92f, 1f, 1f);
            }

            return new Color(0.78f, 0.9f, 1f, 1f);
        }

        private static string ResolveMomentumFormationLabel(string formationId)
        {
            return formationId switch
            {
                "escort" => "ESCORT",
                "crossfire" => "CROSSFIRE",
                "siege" => "SIEGE",
                _ => "PUSH"
            };
        }

        private static string ResolveRouteOpeningLabel(string reasonTag)
        {
            return reasonTag switch
            {
                "PRESS ADVANTAGE" => "ADV",
                "RECOVERY ONLINE" => "SAFE",
                "SUPPLY WINDOW" => "SUPPLY",
                "LOADOUT FIND" => "BUILD",
                "SAFE UPGRADE" => "CLEAN",
                _ => "OPEN"
            };
        }

        private bool UsesChallengeBaselineTheme()
        {
            return string.Equals(_challengeThreatBadgeLabel, "챌린지");
        }
    }
}
