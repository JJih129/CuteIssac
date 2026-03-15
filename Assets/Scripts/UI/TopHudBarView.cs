using CuteIssac.Room;
using UnityEngine;
using UnityEngine.UI;

namespace CuteIssac.UI
{
    /// <summary>
    /// Presentation-only layout helper that groups HUD panels into a single Isaac-style top bar.
    /// It owns section sizing and runtime containers while the child panel views keep their own rendering logic.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TopHudBarView : MonoBehaviour
    {
        private sealed class SectionChrome
        {
            public Image Background;
            public Image HeaderBackground;
            public Text HeaderLabel;
            public Shadow HeaderShadow;
        }

        [Header("Optional Wiring")]
        [SerializeField] private RectTransform safeAreaRoot;
        [SerializeField] private RectTransform barRoot;
        [SerializeField] private Image barBackgroundImage;

        [Header("Sizing")]
        [SerializeField] private bool useMinimalIsaacStrip = true;
        [SerializeField] [Min(56f)] private float barHeight = 236f;
        [SerializeField] [Min(56f)] private float compactBarHeight = 236f;
        [SerializeField] [Min(0f)] private float topPadding = 14f;
        [SerializeField] [Min(0f)] private float sidePadding = 16f;
        [SerializeField] [Min(0f)] private float sectionSpacing = 12f;
        [SerializeField] [Min(0f)] private float compactRowSpacing = 12f;
        [SerializeField] [Min(640f)] private float compactLayoutBreakpoint = 1180f;
        [SerializeField] [Min(0f)] private float stripSlotGap = 10f;
        [SerializeField] [Min(32f)] private float stripActiveWidth = 168f;
        [SerializeField] [Min(32f)] private float stripActiveHeight = 168f;
        [SerializeField] [Min(80f)] private float stripHealthWidth = 360f;
        [SerializeField] [Min(20f)] private float stripHealthHeight = 58f;
        [SerializeField] [Min(56f)] private float stripResourceWidth = 176f;
        [SerializeField] [Min(48f)] private float stripResourceHeight = 154f;
        [SerializeField] [Min(120f)] private float stripMinimapWidth = 360f;
        [SerializeField] [Min(100f)] private float stripMinimapHeight = 244f;
        [SerializeField] [Min(0f)] private float stripLeftTopInset = 8f;
        [SerializeField] [Min(0f)] private float stripResourceRowOffsetY = 8f;
        [SerializeField] [Min(0f)] private float stripMinimapTopInset = 6f;
        [SerializeField] [Min(80f)] private float minimapWidth = 520f;
        [SerializeField] [Min(80f)] private float minimapMinWidth = 420f;
        [SerializeField] [Min(80f)] private float compactMinimapWidth = 560f;
        [SerializeField] [Min(80f)] private float compactMinimapMinWidth = 460f;
        [SerializeField] [Min(80f)] private float resourceWidth = 220f;
        [SerializeField] [Min(80f)] private float resourceMinWidth = 184f;
        [SerializeField] [Min(80f)] private float compactResourceWidth = 236f;
        [SerializeField] [Min(80f)] private float compactResourceMinWidth = 196f;
        [SerializeField] [Min(80f)] private float combatStatWidth = 148f;
        [SerializeField] [Min(80f)] private float combatStatMinWidth = 136f;
        [SerializeField] [Min(80f)] private float compactCombatStatWidth = 164f;
        [SerializeField] [Min(80f)] private float compactCombatStatMinWidth = 142f;
        [SerializeField] [Min(80f)] private float activeWidth = 176f;
        [SerializeField] [Min(80f)] private float activeMinWidth = 156f;
        [SerializeField] [Min(80f)] private float compactActiveWidth = 164f;
        [SerializeField] [Min(80f)] private float compactActiveMinWidth = 144f;
        [SerializeField] [Min(80f)] private float trinketWidth = 136f;
        [SerializeField] [Min(80f)] private float trinketMinWidth = 122f;
        [SerializeField] [Min(80f)] private float compactTrinketWidth = 126f;
        [SerializeField] [Min(80f)] private float compactTrinketMinWidth = 114f;
        [SerializeField] [Min(160f)] private float preferredHealthWidth = 720f;
        [SerializeField] [Min(160f)] private float minimumHealthWidth = 580f;
        [SerializeField] [Min(160f)] private float compactPreferredHealthWidth = 780f;
        [SerializeField] [Min(160f)] private float compactMinimumHealthWidth = 620f;
        [SerializeField] [Min(0f)] private float sectionContentSidePadding = 14f;
        [SerializeField] [Min(0f)] private float compactStripContentSidePadding = 6f;
        [SerializeField] [Min(0f)] private float compactStripMinimapSidePadding = 4f;
        [SerializeField] [Min(0f)] private float sectionContentBottomPadding = 12f;
        [SerializeField] [Min(0f)] private float compactSectionContentBottomPadding = 10f;
        [SerializeField] [Min(0f)] private float sectionContentTopInset = 10f;
        [SerializeField] [Min(0f)] private float compactSectionContentTopInset = 14f;
        [SerializeField] private float minimapContentSideTrim = -2f;
        [SerializeField] private float minimapContentBottomTrim = -1f;
        [SerializeField] private float minimapContentTopTrim = -2f;
        [SerializeField] private float healthContentSideTrim = 2f;
        [SerializeField] [Min(0f)] private float bossPanelTopGap = 6f;
        [SerializeField] [Min(12f)] private float sectionHeaderHeight = 34f;
        [SerializeField] [Min(0f)] private float sectionHeaderGap = 10f;
        [SerializeField] [Min(0f)] private float sectionHeaderInsetX = 10f;
        [SerializeField] [Min(0)] private int sectionHeaderFontSize = 17;
        [SerializeField] [Min(0)] private int priorityHeaderFontBoost = 1;
        [SerializeField] [Range(0.8f, 1.2f)] private float minimapHeaderPriority = 1.08f;
        [SerializeField] [Range(0.8f, 1.2f)] private float healthHeaderPriority = 1.1f;
        [SerializeField] [Range(0.8f, 1.2f)] private float activeHeaderPriority = 0.92f;
        [SerializeField] [Range(0.8f, 1.2f)] private float trinketHeaderPriority = 0.9f;

        [Header("Colors")]
        [SerializeField] private Color barBackgroundColor = new(0.05f, 0.06f, 0.08f, 0f);
        [SerializeField] private Color sectionBackgroundColor = new(1f, 1f, 1f, 0.065f);
        [SerializeField] private Color sectionBorderColor = new(1f, 1f, 1f, 0.12f);
        [SerializeField] private Color sectionHeaderBackgroundColor = new(0.8f, 0.86f, 0.96f, 0.14f);
        [SerializeField] private Color sectionHeaderTextColor = new(0.96f, 0.98f, 1f, 0.96f);
        [SerializeField] private Color sectionHeaderShadowColor = new(0f, 0f, 0f, 0.38f);
        [SerializeField] [Range(0.5f, 1.2f)] private float compactSectionBackgroundAlphaScale = 0.84f;
        [SerializeField] [Range(0.5f, 1.2f)] private float compactHeaderBackgroundAlphaScale = 0.9f;
        [SerializeField] [Range(0.5f, 1.2f)] private float compactHeaderTextAlphaScale = 1.04f;
        [SerializeField] private Color challengeBaselineBarTint = new(1f, 0.7f, 0.28f, 1f);
        [SerializeField] private Color challengeThreatBarTint = new(1f, 0.42f, 0.2f, 1f);
        [SerializeField] [Min(0f)] private float challengeThreatPulseAmplitude = 0.14f;
        [SerializeField] [Min(0f)] private float challengeThreatPulseFrequency = 1.15f;
        [SerializeField] [Range(0f, 1f)] private float challengeThreatSectionTintStrength = 0.58f;
        [SerializeField] [Range(0.2f, 1f)] private float hudPulseDamping = 0.72f;
        [SerializeField] [Range(0f, 1f)] private float minimapThreatChromeWeight = 1f;
        [SerializeField] [Range(0f, 1f)] private float resourceThreatChromeWeight = 0.42f;
        [SerializeField] [Range(0f, 1f)] private float combatThreatChromeWeight = 0.88f;
        [SerializeField] [Range(0f, 1f)] private float activeThreatChromeWeight = 0.32f;
        [SerializeField] [Range(0f, 1f)] private float trinketThreatChromeWeight = 0.28f;
        [SerializeField] [Range(0f, 1f)] private float healthThreatChromeWeight = 0.54f;
        [SerializeField] [Range(0f, 1f)] private float challengeThreatHeaderBoost = 0.18f;

        private RectTransform _minimapSlot;
        private RectTransform _resourceSlot;
        private RectTransform _combatStatSlot;
        private RectTransform _activeSlot;
        private RectTransform _trinketSlot;
        private RectTransform _healthSlot;
        private RectTransform _bossSlot;
        private SectionChrome _minimapChrome;
        private SectionChrome _resourceChrome;
        private SectionChrome _combatChrome;
        private SectionChrome _activeChrome;
        private SectionChrome _trinketChrome;
        private SectionChrome _healthChrome;
        private float _lastKnownWidth = -1f;
        private bool _usingCompactLayout;
        private bool _hasChallengeThreatTheme;
        private Color _challengeThreatAccentColor = Color.white;
        private string _challengeThreatBadgeLabel = string.Empty;
        private ChallengeThreatStage _challengeThreatStage;

        public void ApplyLayout(
            RectTransform minimapPanel,
            RectTransform resourcePanel,
            RectTransform combatStatPanel,
            RectTransform activePanel,
            RectTransform trinketPanel,
            RectTransform healthPanel,
            RectTransform bossPanel)
        {
            EnsureRuntimeHierarchy();
            ParentIntoSlot(minimapPanel, _minimapSlot);
            ParentIntoSlot(resourcePanel, _resourceSlot);
            ParentIntoSlot(combatStatPanel, _combatStatSlot);
            ParentIntoSlot(activePanel, _activeSlot);
            ParentIntoSlot(trinketPanel, _trinketSlot);
            ParentIntoSlot(healthPanel, _healthSlot);
            ParentIntoSlot(bossPanel, _bossSlot);
            ApplyHeaderFont(ResolveHeaderFont(minimapPanel, resourcePanel, combatStatPanel, activePanel, trinketPanel, healthPanel, bossPanel));
            LayoutNow();
        }

        public void SetChallengeThreatTheme(bool active, Color accentColor, string badgeLabel, ChallengeThreatStage stage)
        {
            _hasChallengeThreatTheme = active;
            _challengeThreatAccentColor = accentColor;
            _challengeThreatBadgeLabel = active ? badgeLabel ?? string.Empty : string.Empty;
            _challengeThreatStage = stage;
            ApplyThreatTheme(Time.unscaledTime);
        }

        private void LateUpdate()
        {
            if (barRoot == null)
            {
                return;
            }

            float currentWidth = ResolveSafeAreaRoot().rect.width;

            if (!Mathf.Approximately(currentWidth, _lastKnownWidth))
            {
                LayoutNow();
            }

            ApplyThreatTheme(Time.unscaledTime);
        }

        private void EnsureRuntimeHierarchy()
        {
            RectTransform safeArea = ResolveSafeAreaRoot();

            if (safeArea == null)
            {
                return;
            }

            if (barRoot == null)
            {
                GameObject barObject = new("TopHudBar", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                barObject.transform.SetParent(safeArea, false);
                barRoot = barObject.GetComponent<RectTransform>();
                barBackgroundImage = barObject.GetComponent<Image>();
                barBackgroundImage.raycastTarget = false;
            }

            barRoot.anchorMin = new Vector2(0f, 1f);
            barRoot.anchorMax = new Vector2(1f, 1f);
            barRoot.pivot = new Vector2(0.5f, 1f);
            barRoot.anchoredPosition = new Vector2(0f, -topPadding);
            barRoot.sizeDelta = new Vector2(0f, barHeight);

            if (barBackgroundImage != null)
            {
                barBackgroundImage.color = barBackgroundColor;
                barBackgroundImage.enabled = !useMinimalIsaacStrip;
            }

            _minimapSlot ??= CreateSection("MinimapSection", "MAP", ref _minimapChrome);
            _resourceSlot ??= CreateSection("ResourceSection", "RESOURCES", ref _resourceChrome);
            _combatStatSlot ??= CreateSection("CombatStatSection", "COMBAT", ref _combatChrome);
            _activeSlot ??= CreateSection("ActiveSection", "ACTIVE", ref _activeChrome);
            _trinketSlot ??= CreateSection("TrinketSection", "TRINKET", ref _trinketChrome);
            _healthSlot ??= CreateSection("HealthSection", "HEALTH", ref _healthChrome);
            _bossSlot ??= CreateContainer("BossHudSlot", safeArea);
            ApplyMinimalStripChrome();
        }

        private RectTransform ResolveSafeAreaRoot()
        {
            if (safeAreaRoot != null)
            {
                return safeAreaRoot;
            }

            if (transform is RectTransform rectTransform && rectTransform.childCount > 0 && rectTransform.GetChild(0) is RectTransform childRect)
            {
                safeAreaRoot = childRect;
                return safeAreaRoot;
            }

            safeAreaRoot = transform as RectTransform;
            return safeAreaRoot;
        }

        private RectTransform CreateSection(string name, string headerLabel, ref SectionChrome chrome)
        {
            RectTransform section = CreateContainer(name, barRoot);
            Image background = section.gameObject.AddComponent<Image>();
            background.raycastTarget = false;
            background.color = sectionBackgroundColor;

            Outline outline = section.gameObject.AddComponent<Outline>();
            outline.effectDistance = new Vector2(1f, -1f);
            outline.effectColor = sectionBorderColor;
            outline.useGraphicAlpha = true;

            chrome ??= new SectionChrome();
            chrome.Background = background;
            chrome.HeaderBackground = CreateStretchImage("HeaderBackground", section, sectionHeaderBackgroundColor);
            chrome.HeaderLabel = CreateHeaderText("HeaderLabel", section, headerLabel);
            chrome.HeaderShadow = chrome.HeaderLabel.GetComponent<Shadow>();
            return section;
        }

        private static RectTransform CreateContainer(string name, Transform parent)
        {
            GameObject containerObject = new(name, typeof(RectTransform));
            RectTransform rectTransform = containerObject.GetComponent<RectTransform>();
            rectTransform.SetParent(parent, false);
            rectTransform.localScale = Vector3.one;
            return rectTransform;
        }

        private void LayoutNow()
        {
            if (barRoot == null)
            {
                return;
            }

            float safeWidth = ResolveSafeAreaRoot().rect.width;
            _lastKnownWidth = safeWidth;
            _usingCompactLayout = ShouldUseCompactLayout(safeWidth);
            float currentBarHeight = Mathf.Max(_usingCompactLayout ? compactBarHeight : barHeight, 236f);
            barRoot.sizeDelta = new Vector2(0f, currentBarHeight);

            if (useMinimalIsaacStrip)
            {
                LayoutMinimalIsaacStrip(safeWidth);
            }
            else if (_usingCompactLayout)
            {
                LayoutCompactSections(safeWidth);
            }
            else
            {
                float availableWidth = Mathf.Max(
                    0f,
                    safeWidth - (sidePadding * 2f) - (sectionSpacing * 5f));

                float[] widths = ResolveSectionWidths(availableWidth);
                float currentX = sidePadding;
                float sectionHeight = Mathf.Max(64f, barHeight - 16f);

                LayoutSection(_minimapSlot, _minimapChrome, currentX, widths[0], sectionHeight);
                currentX += widths[0] + sectionSpacing;
                LayoutSection(_resourceSlot, _resourceChrome, currentX, widths[1], sectionHeight);
                currentX += widths[1] + sectionSpacing;
                LayoutSection(_combatStatSlot, _combatChrome, currentX, widths[2], sectionHeight);
                currentX += widths[2] + sectionSpacing;
                LayoutSection(_activeSlot, _activeChrome, currentX, widths[3], sectionHeight);
                currentX += widths[3] + sectionSpacing;
                LayoutSection(_trinketSlot, _trinketChrome, currentX, widths[4], sectionHeight);
                currentX += widths[4] + sectionSpacing;
                LayoutSection(_healthSlot, _healthChrome, currentX, widths[5], sectionHeight);
            }

            if (_bossSlot != null && !useMinimalIsaacStrip)
            {
                _bossSlot.anchorMin = new Vector2(0.5f, 1f);
                _bossSlot.anchorMax = new Vector2(0.5f, 1f);
                _bossSlot.pivot = new Vector2(0.5f, 1f);
                if (useMinimalIsaacStrip)
                {
                    currentBarHeight = Mathf.Max(currentBarHeight, stripMinimapHeight + stripMinimapTopInset);
                }
                _bossSlot.anchoredPosition = new Vector2(0f, -(topPadding + currentBarHeight + bossPanelTopGap));
                _bossSlot.sizeDelta = new Vector2(Mathf.Min(720f, safeWidth - (sidePadding * 2f)), 92f);
            }
        }

        private void LayoutMinimalIsaacStrip(float safeWidth)
        {
            float resolvedTopInset = Mathf.Max(stripLeftTopInset, 8f);
            float resolvedActiveWidth = Mathf.Max(stripActiveWidth, 168f);
            float resolvedActiveHeight = Mathf.Max(stripActiveHeight, 168f);
            float resolvedHealthWidth = Mathf.Max(stripHealthWidth, 360f);
            float resolvedHealthHeight = Mathf.Max(stripHealthHeight, 58f);
            float resolvedResourceWidth = Mathf.Max(stripResourceWidth, 176f);
            float resolvedResourceHeight = Mathf.Max(stripResourceHeight, 154f);
            float resolvedMinimapWidth = Mathf.Max(stripMinimapWidth, 360f);
            float resolvedMinimapHeight = Mathf.Max(stripMinimapHeight, 244f);
            float resolvedSlotGap = Mathf.Max(stripSlotGap, 10f);
            float resolvedResourceOffsetY = Mathf.Max(stripResourceRowOffsetY, 8f);
            float resolvedMinimapTopInset = Mathf.Max(stripMinimapTopInset, 6f);

            float topY = -resolvedTopInset;
            float activeY = topY;
            float healthY = topY;
            float resourceY = -(resolvedTopInset + resolvedActiveHeight + resolvedResourceOffsetY);

            float activeX = sidePadding;
            float healthX = activeX + resolvedActiveWidth + resolvedSlotGap;
            float resourceX = activeX + 2f;

            float minimapX = Mathf.Max(
                healthX + resolvedHealthWidth + resolvedSlotGap,
                safeWidth - sidePadding - resolvedMinimapWidth);
            float minimapY = -resolvedMinimapTopInset;

            LayoutMinimalSlot(_activeSlot, activeX, resolvedActiveWidth, resolvedActiveHeight, activeY, true);
            LayoutMinimalSlot(_healthSlot, healthX, resolvedHealthWidth, resolvedHealthHeight, healthY, true);
            LayoutMinimalSlot(_resourceSlot, resourceX, resolvedResourceWidth, resolvedResourceHeight, resourceY, true);
            LayoutMinimalSlot(_minimapSlot, minimapX, resolvedMinimapWidth, resolvedMinimapHeight, minimapY, true);

            LayoutMinimalSlot(_combatStatSlot, 0f, 0f, 0f, topY, false);
            LayoutMinimalSlot(_trinketSlot, 0f, 0f, 0f, topY, false);

            if (_bossSlot != null)
            {
                _bossSlot.anchorMin = new Vector2(0.5f, 1f);
                _bossSlot.anchorMax = new Vector2(0.5f, 1f);
                _bossSlot.pivot = new Vector2(0.5f, 1f);

                float leftClusterRight = healthX + resolvedHealthWidth;
                float rightClusterLeft = minimapX;
                float bossAvailableWidth = Mathf.Max(420f, rightClusterLeft - leftClusterRight - (resolvedSlotGap * 2f));
                float bossWidth = Mathf.Min(760f, bossAvailableWidth);
                float bossCenterX = ((leftClusterRight + rightClusterLeft) * 0.5f) - (safeWidth * 0.5f);
                float bossTopInset = Mathf.Max(4f, resolvedTopInset - 2f);

                _bossSlot.anchoredPosition = new Vector2(bossCenterX, -bossTopInset);
                _bossSlot.sizeDelta = new Vector2(bossWidth, 106f);
            }
        }

        private bool ShouldUseCompactLayout(float safeWidth)
        {
            return safeWidth <= compactLayoutBreakpoint;
        }

        private void LayoutCompactSections(float safeWidth)
        {
            float rowAvailableWidth = Mathf.Max(0f, safeWidth - (sidePadding * 2f));
            float[] topWidths = ResolveRowSectionWidths(
                rowAvailableWidth,
                new[] { compactMinimapWidth, compactResourceWidth, compactCombatStatWidth, compactActiveWidth },
                new[] { compactMinimapMinWidth, compactResourceMinWidth, compactCombatStatMinWidth, compactActiveMinWidth });
            float[] bottomWidths = ResolveRowSectionWidths(
                rowAvailableWidth,
                new[] { compactTrinketWidth, compactPreferredHealthWidth },
                new[] { compactTrinketMinWidth, compactMinimumHealthWidth });

            float totalSectionHeight = Mathf.Max(112f, compactBarHeight - 16f);
            float sectionHeight = Mathf.Max(56f, (totalSectionHeight - compactRowSpacing) * 0.5f);
            float topRowY = -8f;
            float bottomRowY = topRowY - sectionHeight - compactRowSpacing;

            float topX = sidePadding;
            LayoutSection(_minimapSlot, _minimapChrome, topX, topWidths[0], sectionHeight, topRowY);
            topX += topWidths[0] + sectionSpacing;
            LayoutSection(_resourceSlot, _resourceChrome, topX, topWidths[1], sectionHeight, topRowY);
            topX += topWidths[1] + sectionSpacing;
            LayoutSection(_combatStatSlot, _combatChrome, topX, topWidths[2], sectionHeight, topRowY);
            topX += topWidths[2] + sectionSpacing;
            LayoutSection(_activeSlot, _activeChrome, topX, topWidths[3], sectionHeight, topRowY);

            float bottomX = sidePadding;
            LayoutSection(_trinketSlot, _trinketChrome, bottomX, bottomWidths[0], sectionHeight, bottomRowY);
            bottomX += bottomWidths[0] + sectionSpacing;
            LayoutSection(_healthSlot, _healthChrome, bottomX, bottomWidths[1], sectionHeight, bottomRowY);
        }

        private void LayoutSection(RectTransform section, SectionChrome chrome, float x, float width, float height)
        {
            LayoutSection(section, chrome, x, width, height, -6f);
        }

        private void LayoutSection(RectTransform section, SectionChrome chrome, float x, float width, float height, float y)
        {
            if (section == null)
            {
                return;
            }

            section.anchorMin = new Vector2(0f, 1f);
            section.anchorMax = new Vector2(0f, 1f);
            section.pivot = new Vector2(0f, 1f);
            section.anchoredPosition = new Vector2(x, y);
            section.sizeDelta = new Vector2(width, height);

            if (chrome?.HeaderBackground != null)
            {
                RectTransform headerBackgroundRect = chrome.HeaderBackground.rectTransform;
                headerBackgroundRect.anchorMin = new Vector2(0f, 1f);
                headerBackgroundRect.anchorMax = new Vector2(1f, 1f);
                headerBackgroundRect.pivot = new Vector2(0.5f, 1f);
                headerBackgroundRect.anchoredPosition = Vector2.zero;
                headerBackgroundRect.sizeDelta = new Vector2(0f, sectionHeaderHeight);
                chrome.HeaderBackground.color = sectionHeaderBackgroundColor;
            }

            if (chrome?.HeaderLabel != null)
            {
                RectTransform headerLabelRect = chrome.HeaderLabel.rectTransform;
                headerLabelRect.anchorMin = new Vector2(0f, 1f);
                headerLabelRect.anchorMax = new Vector2(1f, 1f);
                headerLabelRect.pivot = new Vector2(0.5f, 1f);
                headerLabelRect.anchoredPosition = Vector2.zero;
                headerLabelRect.sizeDelta = new Vector2(-(sectionHeaderInsetX * 2f), sectionHeaderHeight);
                chrome.HeaderLabel.color = sectionHeaderTextColor;
                chrome.HeaderLabel.fontSize = Mathf.Max(chrome.HeaderLabel.fontSize, sectionHeaderFontSize);
                chrome.HeaderLabel.fontStyle = FontStyle.Bold;
            }

            if (chrome?.HeaderShadow != null)
            {
                chrome.HeaderShadow.effectColor = sectionHeaderShadowColor;
            }

            ApplyCompactChromeAlpha(chrome);
            ApplyHeaderPriority(chrome);
        }

        private void LayoutMinimalSlot(RectTransform section, float x, float width, float height, float y, bool visible)
        {
            if (section == null)
            {
                return;
            }

            section.gameObject.SetActive(visible);
            if (!visible)
            {
                section.sizeDelta = Vector2.zero;
                return;
            }

            section.anchorMin = new Vector2(0f, 1f);
            section.anchorMax = new Vector2(0f, 1f);
            section.pivot = new Vector2(0f, 1f);
            section.anchoredPosition = new Vector2(x, y);
            section.sizeDelta = new Vector2(width, height);
        }

        private void ApplyThreatTheme(float unscaledTime)
        {
            if (barBackgroundImage == null)
            {
                return;
            }

            if (useMinimalIsaacStrip)
            {
                barBackgroundImage.enabled = false;
                return;
            }

            if (!_hasChallengeThreatTheme)
            {
                barBackgroundImage.color = barBackgroundColor;
                ApplyThreatChrome(_minimapChrome, 0f, 0f, challengeThreatBarTint);
                ApplyThreatChrome(_resourceChrome, 0f, 0f, challengeThreatBarTint);
                ApplyThreatChrome(_combatChrome, 0f, 0f, challengeThreatBarTint);
                ApplyThreatChrome(_activeChrome, 0f, 0f, challengeThreatBarTint);
                ApplyThreatChrome(_trinketChrome, 0f, 0f, challengeThreatBarTint);
                ApplyThreatChrome(_healthChrome, 0f, 0f, challengeThreatBarTint);
                return;
            }

            float stageStrength = Mathf.Clamp01((ChallengeThreatPresentationResolver.ResolveStatusThemeStrength(_challengeThreatStage) - 0.92f) / 0.33f);
            float pulseFrequencyScale = ChallengeThreatPresentationResolver.ResolveFeedbackPulseFrequencyScale(_challengeThreatBadgeLabel, _challengeThreatStage);
            float pulseAmplitudeScale = ChallengeThreatPresentationResolver.ResolveFeedbackPulseAmplitudeScale(_challengeThreatBadgeLabel, _challengeThreatStage);
            float dampedPulseFrequencyScale = Mathf.Lerp(1f, pulseFrequencyScale, hudPulseDamping);
            float dampedPulseAmplitudeScale = Mathf.Lerp(1f, pulseAmplitudeScale, hudPulseDamping);
            float pulse = 1f + (Mathf.Sin(unscaledTime * challengeThreatPulseFrequency * dampedPulseFrequencyScale) * challengeThreatPulseAmplitude * dampedPulseAmplitudeScale * hudPulseDamping * Mathf.Lerp(0.42f, 0.82f, stageStrength));
            Color themeBaseColor = ResolveThreatBaseColor();
            Color themeColor = Color.Lerp(themeBaseColor, _challengeThreatAccentColor, 0.72f);

            barBackgroundImage.color = Color.Lerp(
                barBackgroundColor,
                themeColor,
                Mathf.Clamp01((0.14f + (stageStrength * 0.2f)) * pulse));

            ApplyThreatChrome(_minimapChrome, ResolveSectionThreatEmphasis(stageStrength, minimapThreatChromeWeight), pulse, themeColor);
            ApplyThreatChrome(_resourceChrome, ResolveSectionThreatEmphasis(stageStrength, resourceThreatChromeWeight), pulse, themeColor);
            ApplyThreatChrome(_combatChrome, ResolveSectionThreatEmphasis(stageStrength, combatThreatChromeWeight), pulse, themeColor);
            ApplyThreatChrome(_activeChrome, ResolveSectionThreatEmphasis(stageStrength, activeThreatChromeWeight), pulse, themeColor);
            ApplyThreatChrome(_trinketChrome, ResolveSectionThreatEmphasis(stageStrength, trinketThreatChromeWeight), pulse, themeColor);
            ApplyThreatChrome(_healthChrome, ResolveSectionThreatEmphasis(stageStrength, healthThreatChromeWeight), pulse, themeColor);
        }

        private void ApplyThreatChrome(SectionChrome chrome, float emphasis, float pulse, Color themeColor)
        {
            if (chrome == null)
            {
                return;
            }

            float tintStrength = Mathf.Clamp01(emphasis * challengeThreatSectionTintStrength * hudPulseDamping);

            if (chrome.Background != null)
            {
                chrome.Background.color = Color.Lerp(
                    sectionBackgroundColor,
                    Color.Lerp(sectionBackgroundColor, themeColor, 0.2f + (0.18f * pulse)),
                    tintStrength);
            }

            if (chrome.HeaderBackground != null)
            {
                float headerTintStrength = Mathf.Clamp01(tintStrength + (challengeThreatHeaderBoost * tintStrength));
                chrome.HeaderBackground.color = Color.Lerp(
                    sectionHeaderBackgroundColor,
                    Color.Lerp(sectionHeaderBackgroundColor, themeColor, 0.36f + (0.12f * pulse)),
                    headerTintStrength);
            }

            if (chrome.HeaderLabel != null)
            {
                float headerLabelStrength = Mathf.Clamp01(tintStrength + (challengeThreatHeaderBoost * 0.72f * tintStrength));
                chrome.HeaderLabel.color = Color.Lerp(
                    sectionHeaderTextColor,
                    Color.Lerp(Color.white, themeColor, 0.3f),
                    headerLabelStrength);
            }

            if (chrome.HeaderShadow != null)
            {
                Color shadowColor = sectionHeaderShadowColor;
                shadowColor.a = Mathf.Lerp(sectionHeaderShadowColor.a, 0.56f, tintStrength);
                chrome.HeaderShadow.effectColor = shadowColor;
            }

            ApplyCompactChromeAlpha(chrome);
            ApplyHeaderPriority(chrome);
        }

        private void ApplyCompactChromeAlpha(SectionChrome chrome)
        {
            if (!_usingCompactLayout || chrome == null)
            {
                return;
            }

            if (chrome.Background != null)
            {
                chrome.Background.color = ScaleColorAlpha(chrome.Background.color, compactSectionBackgroundAlphaScale);
            }

            if (chrome.HeaderBackground != null)
            {
                chrome.HeaderBackground.color = ScaleColorAlpha(chrome.HeaderBackground.color, compactHeaderBackgroundAlphaScale);
            }

            if (chrome.HeaderLabel != null)
            {
                chrome.HeaderLabel.color = ScaleColorAlpha(chrome.HeaderLabel.color, compactHeaderTextAlphaScale);
            }
        }

        private float ResolveSectionThreatEmphasis(float stageStrength, float weight)
        {
            return Mathf.Clamp01(stageStrength * Mathf.Clamp01(weight));
        }

        private Color ResolveThreatBaseColor()
        {
            return UsesChallengeBaselineTheme() ? challengeBaselineBarTint : challengeThreatBarTint;
        }

        private bool UsesChallengeBaselineTheme()
        {
            return string.Equals(_challengeThreatBadgeLabel, "챌린지");
        }

        private void ParentIntoSlot(RectTransform panelRect, RectTransform slotRect)
        {
            if (panelRect == null || slotRect == null)
            {
                return;
            }

            if (panelRect.parent != slotRect)
            {
                panelRect.SetParent(slotRect, false);
            }

            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            float horizontalInset = ResolveContentHorizontalInset(slotRect);
            float bottomInset = ResolveContentBottomInset(slotRect);
            float topInset = ResolveContentTopInset(slotRect);
            panelRect.offsetMin = new Vector2(horizontalInset, bottomInset);
            panelRect.offsetMax = new Vector2(-horizontalInset, -topInset);
            panelRect.localScale = Vector3.one;
        }

        private float ResolveContentHorizontalInset(RectTransform slotRect)
        {
            if (useMinimalIsaacStrip)
            {
                return slotRect == _minimapSlot
                    ? compactStripMinimapSidePadding
                    : compactStripContentSidePadding;
            }

            float inset = sectionContentSidePadding;

            if (slotRect == _minimapSlot)
            {
                inset += minimapContentSideTrim;
            }
            else if (slotRect == _healthSlot)
            {
                inset += healthContentSideTrim;
            }

            return Mathf.Max(0f, inset);
        }

        private float ResolveContentBottomInset(RectTransform slotRect)
        {
            if (useMinimalIsaacStrip)
            {
                return 0f;
            }

            float inset = _usingCompactLayout
                ? compactSectionContentBottomPadding
                : sectionContentBottomPadding;

            if (slotRect == _minimapSlot)
            {
                inset += minimapContentBottomTrim;
            }

            return Mathf.Max(0f, inset);
        }

        private float ResolveContentTopInset(RectTransform slotRect)
        {
            if (useMinimalIsaacStrip)
            {
                return 0f;
            }

            float contentInset = _usingCompactLayout
                ? compactSectionContentTopInset
                : sectionContentTopInset;
            float inset = sectionHeaderHeight + sectionHeaderGap + contentInset;

            if (slotRect == _minimapSlot)
            {
                inset += minimapContentTopTrim;
            }

            return Mathf.Max(sectionHeaderHeight + sectionHeaderGap, inset);
        }

        private float[] ResolveSectionWidths(float availableWidth)
        {
            float[] preferred = { minimapWidth, resourceWidth, combatStatWidth, activeWidth, trinketWidth, preferredHealthWidth };
            float[] minimums = { minimapMinWidth, resourceMinWidth, combatStatMinWidth, activeMinWidth, trinketMinWidth, minimumHealthWidth };
            return ResolveRowSectionWidths(availableWidth, preferred, minimums);
        }

        private float[] ResolveRowSectionWidths(float availableWidth, float[] preferred, float[] minimums)
        {
            float preferredTotal = 0f;
            float spacingTotal = sectionSpacing * Mathf.Max(0, preferred.Length - 1);
            float usableWidth = Mathf.Max(0f, availableWidth - spacingTotal);

            for (int i = 0; i < preferred.Length; i++)
            {
                preferredTotal += preferred[i];
            }

            if (usableWidth >= preferredTotal)
            {
                preferred[preferred.Length - 1] += usableWidth - preferredTotal;
                return preferred;
            }

            float deficit = preferredTotal - usableWidth;
            float reducible = 0f;

            for (int i = 0; i < preferred.Length; i++)
            {
                reducible += Mathf.Max(0f, preferred[i] - minimums[i]);
            }

            if (reducible <= 0.01f)
            {
                float fallbackWidth = usableWidth / preferred.Length;

                for (int i = 0; i < preferred.Length; i++)
                {
                    preferred[i] = fallbackWidth;
                }

                return preferred;
            }

            for (int i = 0; i < preferred.Length; i++)
            {
                float slack = Mathf.Max(0f, preferred[i] - minimums[i]);
                float reduction = deficit * (slack / reducible);
                preferred[i] = Mathf.Max(minimums[i], preferred[i] - reduction);
            }

            float finalTotal = 0f;

            for (int i = 0; i < preferred.Length; i++)
            {
                finalTotal += preferred[i];
            }

            float remainder = usableWidth - finalTotal;

            if (Mathf.Abs(remainder) > 0.01f)
            {
                preferred[preferred.Length - 1] += remainder;
            }

            return preferred;
        }

        private static Image CreateStretchImage(string name, Transform parent, Color color)
        {
            GameObject imageObject = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            RectTransform rectTransform = imageObject.GetComponent<RectTransform>();
            rectTransform.SetParent(parent, false);
            rectTransform.localScale = Vector3.one;

            Image image = imageObject.GetComponent<Image>();
            image.raycastTarget = false;
            image.color = color;
            return image;
        }

        private static Text CreateHeaderText(string name, Transform parent, string label)
        {
            GameObject textObject = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text), typeof(Shadow));
            RectTransform rectTransform = textObject.GetComponent<RectTransform>();
            rectTransform.SetParent(parent, false);
            rectTransform.localScale = Vector3.one;

            Text text = textObject.GetComponent<Text>();
            text.raycastTarget = false;
            text.text = label;
            text.alignment = TextAnchor.MiddleLeft;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.fontStyle = FontStyle.Bold;

            Shadow shadow = textObject.GetComponent<Shadow>();
            shadow.effectDistance = new Vector2(1f, -1f);
            shadow.useGraphicAlpha = true;
            return text;
        }

        private Font ResolveHeaderFont(params RectTransform[] panels)
        {
            for (int i = 0; i < panels.Length; i++)
            {
                if (panels[i] == null)
                {
                    continue;
                }

                Text[] texts = panels[i].GetComponentsInChildren<Text>(true);

                for (int textIndex = 0; textIndex < texts.Length; textIndex++)
                {
                    if (texts[textIndex] != null && texts[textIndex].font != null)
                    {
                        return texts[textIndex].font;
                    }
                }
            }

            return null;
        }

        private void ApplyHeaderFont(Font font)
        {
            if (font == null)
            {
                return;
            }

            ApplyHeaderFont(_minimapChrome, font);
            ApplyHeaderFont(_resourceChrome, font);
            ApplyHeaderFont(_combatChrome, font);
            ApplyHeaderFont(_activeChrome, font);
            ApplyHeaderFont(_trinketChrome, font);
            ApplyHeaderFont(_healthChrome, font);
        }

        private static void ApplyHeaderFont(SectionChrome chrome, Font font)
        {
            if (chrome?.HeaderLabel == null)
            {
                return;
            }

            chrome.HeaderLabel.font = font;
            chrome.HeaderLabel.fontStyle = FontStyle.Bold;
        }

        private void ApplyHeaderPriority(SectionChrome chrome)
        {
            if (chrome == null)
            {
                return;
            }

            float priority = ResolveHeaderPriority(chrome);

            if (chrome.HeaderLabel != null)
            {
                int priorityBoost = priority >= 1.04f ? priorityHeaderFontBoost : 0;
                chrome.HeaderLabel.fontSize = Mathf.Max(sectionHeaderFontSize + priorityBoost, 1);
                chrome.HeaderLabel.color = ScaleColorAlpha(chrome.HeaderLabel.color, Mathf.Lerp(0.9f, 1.08f, Mathf.Clamp01((priority - 0.9f) / 0.2f)));
            }

            if (chrome.HeaderBackground != null)
            {
                chrome.HeaderBackground.color = ScaleColorAlpha(chrome.HeaderBackground.color, Mathf.Lerp(0.86f, 1.12f, Mathf.Clamp01((priority - 0.9f) / 0.2f)));
            }

            if (chrome.HeaderShadow != null)
            {
                Color shadowColor = chrome.HeaderShadow.effectColor;
                shadowColor.a = Mathf.Clamp01(shadowColor.a * Mathf.Lerp(0.88f, 1.18f, Mathf.Clamp01((priority - 0.9f) / 0.2f)));
                chrome.HeaderShadow.effectColor = shadowColor;
            }
        }

        private float ResolveHeaderPriority(SectionChrome chrome)
        {
            if (chrome == _minimapChrome)
            {
                return minimapHeaderPriority;
            }

            if (chrome == _healthChrome)
            {
                return healthHeaderPriority;
            }

            if (chrome == _activeChrome)
            {
                return activeHeaderPriority;
            }

            if (chrome == _trinketChrome)
            {
                return trinketHeaderPriority;
            }

            return 1f;
        }

        private static Color ScaleColorAlpha(Color color, float alphaScale)
        {
            color.a = Mathf.Clamp01(color.a * alphaScale);
            return color;
        }

        private void ApplyMinimalStripChrome()
        {
            if (!useMinimalIsaacStrip)
            {
                return;
            }

            HideChrome(_minimapChrome);
            HideChrome(_resourceChrome);
            HideChrome(_combatChrome);
            HideChrome(_activeChrome);
            HideChrome(_trinketChrome);
            HideChrome(_healthChrome);
        }

        private static void HideChrome(SectionChrome chrome)
        {
            if (chrome == null)
            {
                return;
            }

            if (chrome.Background != null)
            {
                chrome.Background.enabled = false;
            }

            if (chrome.HeaderBackground != null)
            {
                chrome.HeaderBackground.enabled = false;
            }

            if (chrome.HeaderLabel != null)
            {
                chrome.HeaderLabel.enabled = false;
            }

            if (chrome.HeaderShadow != null)
            {
                chrome.HeaderShadow.enabled = false;
            }
        }
    }
}
