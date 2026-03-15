using UnityEngine;
using UnityEngine.UI;
using CuteIssac.Core.Pooling;
using CuteIssac.Core.Feedback;
using CuteIssac.Room;

namespace CuteIssac.UI
{
    [DisallowMultipleComponent]
    public sealed class ScreenFeedbackBannerView : MonoBehaviour, IUiModalDismissible
    {
        [Header("Fallback Presentation")]
        [SerializeField] private RectTransform rectTransform;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Image shadowImage;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image frameImage;
        [SerializeField] private Image accentImage;
        [SerializeField] private Image subtitleCardImage;
        [SerializeField] private Image progressTrackImage;
        [SerializeField] private Image progressFillImage;
        [SerializeField] private Text badgeText;
        [SerializeField] private Text titleText;
        [SerializeField] private Text subtitleText;
        [SerializeField] [Min(960f)] private float challengeBaselineBannerWidth = 996f;
        [SerializeField] [Min(184f)] private float challengeBaselineBannerHeight = 188f;
        [SerializeField] [Min(960f)] private float challengePaceBannerWidth = 1028f;
        [SerializeField] [Min(184f)] private float challengePaceBannerHeight = 194f;
        [SerializeField] [Min(960f)] private float eliteWarningBannerWidth = 1060f;
        [SerializeField] [Min(184f)] private float eliteWarningBannerHeight = 204f;
        [SerializeField] [Min(220f)] private float challengeBaselineBadgeWidth = 296f;
        [SerializeField] [Min(220f)] private float challengePaceBadgeWidth = 320f;
        [SerializeField] [Min(220f)] private float eliteWarningBadgeWidth = 346f;
        [SerializeField] [Min(18)] private int challengeBaselineBadgeFontSize = 19;
        [SerializeField] [Min(18)] private int challengePaceBadgeFontSize = 20;
        [SerializeField] [Min(18)] private int eliteWarningBadgeFontSize = 21;
        [SerializeField] [Min(40)] private int challengeBaselineTitleFontSize = 42;
        [SerializeField] [Min(40)] private int challengePaceTitleFontSize = 40;
        [SerializeField] [Min(40)] private int eliteWarningTitleFontSize = 38;
        [SerializeField] private float challengeBaselineTitleOffsetX = 0f;
        [SerializeField] private float challengePaceTitleOffsetX = 8f;
        [SerializeField] private float eliteWarningTitleOffsetX = 16f;
        [SerializeField] [Min(0)] private int challengeSubtitleEyebrowFontSize = 14;
        [SerializeField] [Min(0)] private int challengeSubtitleDetailFontSize = 21;
        [SerializeField] private Color challengeSubtitleEyebrowColor = new(1f, 0.85f, 0.68f, 0.96f);
        [SerializeField] private Color challengeSubtitleDetailColor = new(0.94f, 0.96f, 1f, 0.96f);
        [SerializeField] [Min(42f)] private float challengeBaselineSubtitleCardHeight = 54f;
        [SerializeField] [Min(42f)] private float challengePaceSubtitleCardHeight = 58f;
        [SerializeField] [Min(42f)] private float eliteWarningSubtitleCardHeight = 62f;
        [SerializeField] [Min(34f)] private float challengeBaselineSubtitleTextHeight = 46f;
        [SerializeField] [Min(34f)] private float challengePaceSubtitleTextHeight = 52f;
        [SerializeField] [Min(34f)] private float eliteWarningSubtitleTextHeight = 56f;
        [SerializeField] private float challengeBaselineContentShiftX = 10f;
        [SerializeField] private float challengePaceContentShiftX = 18f;
        [SerializeField] private float eliteWarningContentShiftX = 28f;
        [SerializeField] [Min(0f)] private float challengeBaselineSubtitleWidthTrim = 18f;
        [SerializeField] [Min(0f)] private float challengePaceSubtitleWidthTrim = 30f;
        [SerializeField] [Min(0f)] private float eliteWarningSubtitleWidthTrim = 44f;
        [SerializeField] [Min(0f)] private float challengeBaselineProgressWidthTrim = 10f;
        [SerializeField] [Min(0f)] private float challengePaceProgressWidthTrim = 20f;
        [SerializeField] [Min(0f)] private float eliteWarningProgressWidthTrim = 34f;
        [SerializeField] private float challengeBaselineSubtitleTextOffsetY = -2f;
        [SerializeField] private float challengePaceSubtitleTextOffsetY = -4f;
        [SerializeField] private float eliteWarningSubtitleTextOffsetY = -6f;
        [SerializeField] [Min(0.8f)] private float challengeBaselineSubtitleLineSpacing = 0.96f;
        [SerializeField] [Min(0.8f)] private float challengePaceSubtitleLineSpacing = 0.92f;
        [SerializeField] [Min(0.8f)] private float eliteWarningSubtitleLineSpacing = 0.88f;
        [SerializeField] private Color challengeBaselineBadgeColor = new(1f, 0.92f, 0.72f, 1f);
        [SerializeField] private Color challengeBaselineTitleColor = new(1f, 0.94f, 0.8f, 1f);
        [SerializeField] private Color challengeBaselineSubtitleCardColor = new(0.24f, 0.17f, 0.11f, 0.96f);
        [SerializeField] private Color challengeBaselineProgressColor = new(0.96f, 0.74f, 0.28f, 1f);
        [SerializeField] private Color challengeBaselineSubtitleEyebrowColor = new(1f, 0.9f, 0.72f, 0.98f);
        [SerializeField] private Color challengeBaselineSubtitleDetailColor = new(1f, 0.95f, 0.88f, 0.96f);
        [SerializeField] private Color challengePaceBadgeColor = new(1f, 0.9f, 0.62f, 1f);
        [SerializeField] private Color challengePaceTitleColor = new(1f, 0.95f, 0.82f, 1f);
        [SerializeField] private Color challengePaceSubtitleCardColor = new(0.23f, 0.18f, 0.12f, 0.96f);
        [SerializeField] private Color challengePaceProgressColor = new(0.98f, 0.84f, 0.34f, 1f);
        [SerializeField] private Color eliteWarningBadgeColor = new(1f, 0.82f, 0.76f, 1f);
        [SerializeField] private Color defaultBannerShadowColor = new(0f, 0f, 0f, 0.34f);
        [SerializeField] private Color defaultBannerBackgroundColor = new(0.05f, 0.08f, 0.13f, 0.96f);
        [SerializeField] private Color defaultBannerFrameColor = new(1f, 1f, 1f, 0.14f);

        [Header("Animation")]
        [SerializeField] [Min(0.01f)] private float fadeInDuration = 0.12f;
        [SerializeField] [Min(0.01f)] private float fadeOutDuration = 0.28f;
        [SerializeField] [Min(0f)] private float subtitleFadeRatio = 0.92f;
        [SerializeField] [Min(0f)] private float accentPulseAmplitude = 0.12f;
        [SerializeField] [Min(0f)] private float defaultEntryDropDistance = 12f;
        [SerializeField] [Min(0f)] private float defaultEntryOvershoot = 0.022f;
        [SerializeField] [Min(0.5f)] private float defaultBannerPulseCycles = 2f;

        private float _duration = 1.8f;
        private float _elapsed;
        private Color _accentColor = Color.white;
        private float _bannerPulseScale = 1f;
        private float _bannerScaleBoost = 0.01f;
        private float _bannerPulseCycles = 2f;
        private float _bannerEntryOvershoot = 0.022f;
        private float _bannerEntryDropDistance = 12f;
        private float _detailPulseFrequencyScale = 1f;
        private float _detailPulseAmplitudeScale = 1f;
        private float _currentProgressWidth = 910f;
        private Vector2 _bannerBaseSize;
        private Vector2 _baseAnchoredPosition;
        private Vector2 _shadowBaseSize;
        private Vector2 _backgroundBaseSize;
        private Vector2 _frameBaseSize;
        private Vector2 _accentBaseAnchoredPosition;
        private Vector2 _accentBaseSize;
        private Vector2 _badgeBaseAnchoredPosition;
        private Vector2 _badgeBaseSize;
        private int _badgeBaseFontSize;
        private Vector2 _titleBaseSize;
        private Vector2 _titleBaseAnchoredPosition;
        private int _titleBaseFontSize;
        private Vector2 _progressTrackBaseAnchoredPosition;
        private Vector2 _progressTrackBaseSize;
        private Vector2 _progressFillBaseAnchoredPosition;
        private Vector2 _progressFillBaseSize;
        private Vector2 _subtitleCardBaseAnchoredPosition;
        private Vector2 _subtitleCardBaseSize;
        private Vector2 _subtitleTextBaseAnchoredPosition;
        private Vector2 _subtitleTextBaseSize;
        private float _subtitleTextBaseLineSpacing = 1f;
        private bool _isChallengeBaselineBanner;
        private bool _isChallengePaceBanner;
        private bool _isEliteWarningBanner;

        private void Awake()
        {
            ResolveReferences();
            _bannerBaseSize = rectTransform != null ? rectTransform.sizeDelta : Vector2.zero;
            _baseAnchoredPosition = rectTransform != null ? rectTransform.anchoredPosition : Vector2.zero;
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

            float holdEnd = Mathf.Max(fadeInDuration, _duration - fadeOutDuration);
            float alpha = _elapsed <= fadeInDuration
                ? Mathf.Clamp01(_elapsed / fadeInDuration)
                : _elapsed >= holdEnd
                    ? 1f - Mathf.Clamp01((_elapsed - holdEnd) / Mathf.Max(0.01f, fadeOutDuration))
                    : 1f;

            canvasGroup.alpha = alpha;

            float normalized = Mathf.Clamp01(_duration <= 0.0001f ? 1f : _elapsed / _duration);
            float scale = _elapsed <= fadeInDuration
                ? EvaluateEntryScale(Mathf.Clamp01(_elapsed / fadeInDuration))
                : 1f + (_bannerScaleBoost * 0.3f * Mathf.Clamp01(1f - normalized));

            rectTransform.localScale = Vector3.one * scale;
            rectTransform.anchoredPosition = _elapsed <= fadeInDuration
                ? _baseAnchoredPosition + new Vector2(0f, EvaluateEntryOffsetY(Mathf.Clamp01(_elapsed / fadeInDuration)))
                : _baseAnchoredPosition;

            float remainingRatio = 1f - normalized;
            ApplyProgress(remainingRatio);
            ApplyAccentPulse(normalized);

            if (subtitleText != null)
            {
                Color subtitleColor = subtitleText.color;
                subtitleColor.a = alpha * subtitleFadeRatio;
                subtitleText.color = subtitleColor;
            }

            if (_elapsed >= _duration)
            {
                PrefabPoolService.Return(gameObject);
            }
        }

        public void Initialize(CuteIssac.Core.Feedback.BannerFeedbackRequest request)
        {
            ResolveReferences();

            _elapsed = 0f;
            _duration = Mathf.Max(0.25f, request.Duration);
            _accentColor = request.AccentColor;

            bool hasChallengeBannerCopy = request.HasChallengeMetadata;
            string challengeBadgeLabel = request.ChallengeBadgeLabel;
            string challengeSubtitleEyebrow = request.ChallengeSubtitleEyebrow;
            ChallengeThreatStage challengeStage = request.ChallengeStage;
            ChallengeBannerLayoutProfile challengeLayoutProfile = request.ChallengeLayoutProfile;

            if (!hasChallengeBannerCopy)
            {
                hasChallengeBannerCopy = ChallengeThreatPresentationResolver.TryResolveBannerCopy(
                    request.Title,
                    request.Subtitle,
                    out challengeBadgeLabel,
                    out challengeSubtitleEyebrow,
                    out challengeStage);
            }

            if (challengeLayoutProfile == ChallengeBannerLayoutProfile.None && hasChallengeBannerCopy)
            {
                challengeLayoutProfile = ChallengeThreatPresentationResolver.ResolveBannerLayoutProfile(challengeBadgeLabel);
            }

            _isChallengeBaselineBanner = hasChallengeBannerCopy && string.Equals(challengeBadgeLabel, "챌린지");
            _isChallengePaceBanner = hasChallengeBannerCopy && string.Equals(challengeBadgeLabel, "도전 페이스");
            _isEliteWarningBanner = hasChallengeBannerCopy && string.Equals(challengeBadgeLabel, "엘리트 경보");
            ResolveChallengeThreatAnimation(hasChallengeBannerCopy, challengeBadgeLabel, challengeStage);
            ApplyBannerFrameLayoutProfile(challengeLayoutProfile);
            ApplyTypographyLayoutProfile(challengeLayoutProfile);
            ApplySubtitleLayoutProfile(challengeLayoutProfile);
            _baseAnchoredPosition = rectTransform.anchoredPosition;

            titleText.text = request.Title;
            subtitleText.supportRichText = true;
            subtitleText.text = ResolveSubtitleMarkup(request.Subtitle, hasChallengeBannerCopy ? challengeSubtitleEyebrow : string.Empty);
            if (badgeText != null)
            {
                badgeText.text = ResolveBadgeLabel(request.Title, request.Subtitle, hasChallengeBannerCopy ? challengeBadgeLabel : null);
            }

            ApplyBaseColors();
            canvasGroup.alpha = 0f;
            rectTransform.localScale = Vector3.one * EvaluateEntryScale(0f);
            rectTransform.anchoredPosition = _baseAnchoredPosition + new Vector2(0f, _bannerEntryDropDistance);
            ApplyProgress(1f);
        }

        public void DismissForModal()
        {
            PrefabPoolService.Return(gameObject);
        }

        private void ResolveReferences()
        {
            if (rectTransform == null)
            {
                rectTransform = transform as RectTransform;
            }

            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
            }

            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            if (backgroundImage == null || accentImage == null || titleText == null || subtitleText == null)
            {
                BuildFallbackVisuals();
            }
        }

        private void CacheBaseLayoutMetrics()
        {
            if (rectTransform != null)
            {
                _bannerBaseSize = rectTransform.sizeDelta;
            }

            if (shadowImage != null)
            {
                _shadowBaseSize = shadowImage.rectTransform.sizeDelta;
            }

            if (backgroundImage != null)
            {
                _backgroundBaseSize = backgroundImage.rectTransform.sizeDelta;
            }

            if (frameImage != null)
            {
                _frameBaseSize = frameImage.rectTransform.sizeDelta;
            }

            if (accentImage != null)
            {
                RectTransform accentRect = accentImage.rectTransform;
                _accentBaseAnchoredPosition = accentRect.anchoredPosition;
                _accentBaseSize = accentRect.sizeDelta;
            }

            if (badgeText != null)
            {
                RectTransform badgeRect = badgeText.rectTransform;
                _badgeBaseAnchoredPosition = badgeRect.anchoredPosition;
                _badgeBaseSize = badgeRect.sizeDelta;
                _badgeBaseFontSize = badgeText.fontSize;
            }

            if (titleText != null)
            {
                _titleBaseAnchoredPosition = titleText.rectTransform.anchoredPosition;
                _titleBaseSize = titleText.rectTransform.sizeDelta;
                _titleBaseFontSize = titleText.fontSize;
            }

            if (subtitleCardImage != null)
            {
                RectTransform subtitleCardRect = subtitleCardImage.rectTransform;
                _subtitleCardBaseAnchoredPosition = subtitleCardRect.anchoredPosition;
                _subtitleCardBaseSize = subtitleCardRect.sizeDelta;
            }

            if (subtitleText != null)
            {
                RectTransform subtitleTextRect = subtitleText.rectTransform;
                _subtitleTextBaseAnchoredPosition = subtitleTextRect.anchoredPosition;
                _subtitleTextBaseSize = subtitleTextRect.sizeDelta;
                _subtitleTextBaseLineSpacing = subtitleText.lineSpacing;
            }

            if (progressTrackImage != null)
            {
                RectTransform progressTrackRect = progressTrackImage.rectTransform;
                _progressTrackBaseAnchoredPosition = progressTrackRect.anchoredPosition;
                _progressTrackBaseSize = progressTrackRect.sizeDelta;
            }

            if (progressFillImage != null)
            {
                RectTransform progressFillRect = progressFillImage.rectTransform;
                _progressFillBaseAnchoredPosition = progressFillRect.anchoredPosition;
                _progressFillBaseSize = progressFillRect.sizeDelta;
                _currentProgressWidth = progressFillRect.sizeDelta.x;
            }
        }

        private void ApplyTypographyLayoutProfile(ChallengeBannerLayoutProfile layoutProfile)
        {
            bool usesChallengeLayout = layoutProfile != ChallengeBannerLayoutProfile.None;
            float bannerWidthDelta = rectTransform != null ? rectTransform.sizeDelta.x - _bannerBaseSize.x : 0f;
            float contentShiftX = usesChallengeLayout ? ResolveContentShiftX(layoutProfile) : 0f;
            float subtitleWidthTrim = usesChallengeLayout ? ResolveSubtitleWidthTrim(layoutProfile) : 0f;

            if (badgeText != null)
            {
                RectTransform badgeRect = badgeText.rectTransform;
                badgeRect.anchoredPosition = _badgeBaseAnchoredPosition;
                badgeRect.sizeDelta = usesChallengeLayout
                    ? new Vector2(ResolveBadgeWidth(layoutProfile), _badgeBaseSize.y)
                    : _badgeBaseSize;
                badgeText.fontSize = usesChallengeLayout ? ResolveBadgeFontSize(layoutProfile) : _badgeBaseFontSize;
                badgeText.resizeTextForBestFit = usesChallengeLayout;
                badgeText.resizeTextMinSize = Mathf.Max(16, badgeText.fontSize - 4);
                badgeText.resizeTextMaxSize = badgeText.fontSize;
            }

            if (titleText != null)
            {
                RectTransform titleRect = titleText.rectTransform;
                titleRect.anchoredPosition = usesChallengeLayout
                    ? _titleBaseAnchoredPosition + new Vector2(contentShiftX + ResolveTitleOffsetX(layoutProfile), 0f)
                    : _titleBaseAnchoredPosition;
                titleRect.sizeDelta = new Vector2(
                    _titleBaseSize.x + (bannerWidthDelta * (usesChallengeLayout ? 0.42f : 0f)) - (subtitleWidthTrim * 0.28f),
                    _titleBaseSize.y);
                titleText.fontSize = usesChallengeLayout ? ResolveTitleFontSize(layoutProfile) : _titleBaseFontSize;
                titleText.resizeTextForBestFit = usesChallengeLayout;
                titleText.resizeTextMinSize = Mathf.Max(34, titleText.fontSize - 8);
                titleText.resizeTextMaxSize = titleText.fontSize;
                titleText.horizontalOverflow = HorizontalWrapMode.Wrap;
            }
        }

        private void ApplyBannerFrameLayoutProfile(ChallengeBannerLayoutProfile layoutProfile)
        {
            if (rectTransform == null)
            {
                return;
            }

            bool usesChallengeLayout = layoutProfile != ChallengeBannerLayoutProfile.None;
            Vector2 targetBannerSize = usesChallengeLayout
                ? ResolveBannerSize(layoutProfile)
                : _bannerBaseSize;
            rectTransform.sizeDelta = targetBannerSize;

            float widthScale = _bannerBaseSize.x > 0.01f ? targetBannerSize.x / _bannerBaseSize.x : 1f;
            float heightDelta = targetBannerSize.y - _bannerBaseSize.y;
            float contentShiftX = usesChallengeLayout ? ResolveContentShiftX(layoutProfile) : 0f;
            float subtitleWidthTrim = usesChallengeLayout ? ResolveSubtitleWidthTrim(layoutProfile) : 0f;
            float progressWidthTrim = usesChallengeLayout ? ResolveProgressWidthTrim(layoutProfile) : 0f;

            if (shadowImage != null)
            {
                shadowImage.rectTransform.sizeDelta = new Vector2(_shadowBaseSize.x * widthScale, _shadowBaseSize.y + heightDelta);
            }

            if (backgroundImage != null)
            {
                backgroundImage.rectTransform.sizeDelta = new Vector2(_backgroundBaseSize.x * widthScale, _backgroundBaseSize.y + heightDelta);
            }

            if (frameImage != null)
            {
                frameImage.rectTransform.sizeDelta = new Vector2(_frameBaseSize.x * widthScale, _frameBaseSize.y + heightDelta);
            }

            if (accentImage != null)
            {
                RectTransform accentRect = accentImage.rectTransform;
                accentRect.anchoredPosition = _accentBaseAnchoredPosition + new Vector2(0f, -heightDelta * 0.18f);
                accentRect.sizeDelta = new Vector2(_accentBaseSize.x, _accentBaseSize.y + (heightDelta * 0.7f));
            }

            if (titleText != null)
            {
                titleText.rectTransform.sizeDelta = new Vector2(
                    _titleBaseSize.x + (targetBannerSize.x - _bannerBaseSize.x) * 0.55f - (subtitleWidthTrim * 0.22f),
                    _titleBaseSize.y);
            }

            if (subtitleCardImage != null)
            {
                RectTransform subtitleCardRect = subtitleCardImage.rectTransform;
                subtitleCardRect.anchoredPosition = _subtitleCardBaseAnchoredPosition + new Vector2(contentShiftX * 0.72f, -heightDelta * 0.24f);
                subtitleCardRect.sizeDelta = new Vector2(
                    _subtitleCardBaseSize.x + (targetBannerSize.x - _bannerBaseSize.x) * 0.58f - subtitleWidthTrim,
                    subtitleCardRect.sizeDelta.y);
            }

            if (subtitleText != null)
            {
                RectTransform subtitleTextRect = subtitleText.rectTransform;
                subtitleTextRect.anchoredPosition = _subtitleTextBaseAnchoredPosition + new Vector2(contentShiftX * 0.72f, -heightDelta * 0.24f);
                subtitleTextRect.sizeDelta = new Vector2(
                    _subtitleTextBaseSize.x + (targetBannerSize.x - _bannerBaseSize.x) * 0.55f - subtitleWidthTrim,
                    subtitleTextRect.sizeDelta.y);
            }

            if (progressTrackImage != null)
            {
                RectTransform progressTrackRect = progressTrackImage.rectTransform;
                progressTrackRect.anchoredPosition = _progressTrackBaseAnchoredPosition + new Vector2(contentShiftX * 0.46f, -heightDelta * 0.38f);
                progressTrackRect.sizeDelta = new Vector2(
                    _progressTrackBaseSize.x + (targetBannerSize.x - _bannerBaseSize.x) * 0.82f - progressWidthTrim,
                    _progressTrackBaseSize.y);
                _currentProgressWidth = progressTrackRect.sizeDelta.x;
            }

            if (progressFillImage != null)
            {
                RectTransform progressFillRect = progressFillImage.rectTransform;
                progressFillRect.anchoredPosition = _progressFillBaseAnchoredPosition + new Vector2(contentShiftX * 0.46f, -heightDelta * 0.38f);
                progressFillRect.sizeDelta = new Vector2(_currentProgressWidth, _progressFillBaseSize.y);
            }
        }

        private void ApplySubtitleLayoutProfile(ChallengeBannerLayoutProfile layoutProfile)
        {
            bool usesChallengeLayout = layoutProfile != ChallengeBannerLayoutProfile.None;

            if (subtitleCardImage != null)
            {
                RectTransform subtitleCardRect = subtitleCardImage.rectTransform;
                subtitleCardRect.anchoredPosition = usesChallengeLayout
                    ? subtitleCardRect.anchoredPosition
                    : _subtitleCardBaseAnchoredPosition;
                subtitleCardRect.sizeDelta = usesChallengeLayout
                    ? new Vector2(subtitleCardRect.sizeDelta.x, ResolveSubtitleCardHeight(layoutProfile))
                    : _subtitleCardBaseSize;
            }

            if (subtitleText != null)
            {
                RectTransform subtitleTextRect = subtitleText.rectTransform;
                subtitleTextRect.anchoredPosition = usesChallengeLayout
                    ? subtitleTextRect.anchoredPosition + new Vector2(0f, ResolveSubtitleTextOffsetY(layoutProfile))
                    : _subtitleTextBaseAnchoredPosition;
                subtitleTextRect.sizeDelta = usesChallengeLayout
                    ? new Vector2(subtitleTextRect.sizeDelta.x, ResolveSubtitleTextHeight(layoutProfile))
                    : _subtitleTextBaseSize;
                subtitleText.lineSpacing = usesChallengeLayout ? ResolveSubtitleLineSpacing(layoutProfile) : _subtitleTextBaseLineSpacing;
            }
        }

        private Vector2 ResolveBannerSize(ChallengeBannerLayoutProfile layoutProfile)
        {
            return layoutProfile switch
            {
                ChallengeBannerLayoutProfile.Baseline => new Vector2(challengeBaselineBannerWidth, challengeBaselineBannerHeight),
                ChallengeBannerLayoutProfile.Pace => new Vector2(challengePaceBannerWidth, challengePaceBannerHeight),
                ChallengeBannerLayoutProfile.EliteWarning => new Vector2(eliteWarningBannerWidth, eliteWarningBannerHeight),
                _ => _bannerBaseSize
            };
        }

        private float ResolveBadgeWidth(ChallengeBannerLayoutProfile layoutProfile)
        {
            return layoutProfile switch
            {
                ChallengeBannerLayoutProfile.Baseline => challengeBaselineBadgeWidth,
                ChallengeBannerLayoutProfile.Pace => challengePaceBadgeWidth,
                ChallengeBannerLayoutProfile.EliteWarning => eliteWarningBadgeWidth,
                _ => _badgeBaseSize.x
            };
        }

        private int ResolveBadgeFontSize(ChallengeBannerLayoutProfile layoutProfile)
        {
            return layoutProfile switch
            {
                ChallengeBannerLayoutProfile.Baseline => challengeBaselineBadgeFontSize,
                ChallengeBannerLayoutProfile.Pace => challengePaceBadgeFontSize,
                ChallengeBannerLayoutProfile.EliteWarning => eliteWarningBadgeFontSize,
                _ => _badgeBaseFontSize
            };
        }

        private int ResolveTitleFontSize(ChallengeBannerLayoutProfile layoutProfile)
        {
            return layoutProfile switch
            {
                ChallengeBannerLayoutProfile.Baseline => challengeBaselineTitleFontSize,
                ChallengeBannerLayoutProfile.Pace => challengePaceTitleFontSize,
                ChallengeBannerLayoutProfile.EliteWarning => eliteWarningTitleFontSize,
                _ => _titleBaseFontSize
            };
        }

        private float ResolveTitleOffsetX(ChallengeBannerLayoutProfile layoutProfile)
        {
            return layoutProfile switch
            {
                ChallengeBannerLayoutProfile.Baseline => challengeBaselineTitleOffsetX,
                ChallengeBannerLayoutProfile.Pace => challengePaceTitleOffsetX,
                ChallengeBannerLayoutProfile.EliteWarning => eliteWarningTitleOffsetX,
                _ => 0f
            };
        }

        private float ResolveSubtitleCardHeight(ChallengeBannerLayoutProfile layoutProfile)
        {
            return layoutProfile switch
            {
                ChallengeBannerLayoutProfile.Baseline => challengeBaselineSubtitleCardHeight,
                ChallengeBannerLayoutProfile.Pace => challengePaceSubtitleCardHeight,
                ChallengeBannerLayoutProfile.EliteWarning => eliteWarningSubtitleCardHeight,
                _ => _subtitleCardBaseSize.y
            };
        }

        private float ResolveSubtitleTextHeight(ChallengeBannerLayoutProfile layoutProfile)
        {
            return layoutProfile switch
            {
                ChallengeBannerLayoutProfile.Baseline => challengeBaselineSubtitleTextHeight,
                ChallengeBannerLayoutProfile.Pace => challengePaceSubtitleTextHeight,
                ChallengeBannerLayoutProfile.EliteWarning => eliteWarningSubtitleTextHeight,
                _ => _subtitleTextBaseSize.y
            };
        }

        private float ResolveContentShiftX(ChallengeBannerLayoutProfile layoutProfile)
        {
            return layoutProfile switch
            {
                ChallengeBannerLayoutProfile.Baseline => challengeBaselineContentShiftX,
                ChallengeBannerLayoutProfile.Pace => challengePaceContentShiftX,
                ChallengeBannerLayoutProfile.EliteWarning => eliteWarningContentShiftX,
                _ => 0f
            };
        }

        private float ResolveSubtitleWidthTrim(ChallengeBannerLayoutProfile layoutProfile)
        {
            return layoutProfile switch
            {
                ChallengeBannerLayoutProfile.Baseline => challengeBaselineSubtitleWidthTrim,
                ChallengeBannerLayoutProfile.Pace => challengePaceSubtitleWidthTrim,
                ChallengeBannerLayoutProfile.EliteWarning => eliteWarningSubtitleWidthTrim,
                _ => 0f
            };
        }

        private float ResolveProgressWidthTrim(ChallengeBannerLayoutProfile layoutProfile)
        {
            return layoutProfile switch
            {
                ChallengeBannerLayoutProfile.Baseline => challengeBaselineProgressWidthTrim,
                ChallengeBannerLayoutProfile.Pace => challengePaceProgressWidthTrim,
                ChallengeBannerLayoutProfile.EliteWarning => eliteWarningProgressWidthTrim,
                _ => 0f
            };
        }

        private float ResolveSubtitleTextOffsetY(ChallengeBannerLayoutProfile layoutProfile)
        {
            return layoutProfile switch
            {
                ChallengeBannerLayoutProfile.Baseline => challengeBaselineSubtitleTextOffsetY,
                ChallengeBannerLayoutProfile.Pace => challengePaceSubtitleTextOffsetY,
                ChallengeBannerLayoutProfile.EliteWarning => eliteWarningSubtitleTextOffsetY,
                _ => 0f
            };
        }

        private float ResolveSubtitleLineSpacing(ChallengeBannerLayoutProfile layoutProfile)
        {
            return layoutProfile switch
            {
                ChallengeBannerLayoutProfile.Baseline => challengeBaselineSubtitleLineSpacing,
                ChallengeBannerLayoutProfile.Pace => challengePaceSubtitleLineSpacing,
                ChallengeBannerLayoutProfile.EliteWarning => eliteWarningSubtitleLineSpacing,
                _ => _subtitleTextBaseLineSpacing
            };
        }

        private void BuildFallbackVisuals()
        {
            if (rectTransform == null)
            {
                return;
            }

            if (shadowImage == null)
            {
                shadowImage = CreatePanelImage(
                    "Shadow",
                    new Vector2(0f, -8f),
                    new Vector2(988f, 176f),
                    new Color(0f, 0f, 0f, 0.26f));
            }

            if (backgroundImage == null)
            {
                backgroundImage = CreatePanelImage(
                    "Background",
                    Vector2.zero,
                    new Vector2(960f, 164f),
                    new Color(0.06f, 0.1f, 0.16f, 0.94f));
            }

            if (frameImage == null)
            {
                frameImage = CreatePanelImage(
                    "Frame",
                    Vector2.zero,
                    new Vector2(968f, 172f),
                    new Color(1f, 1f, 1f, 0.08f));
            }

            if (accentImage == null)
            {
                accentImage = CreatePanelImage(
                    "Accent",
                    new Vector2(-428f, -16f),
                    new Vector2(18f, 132f),
                    Color.white);
            }

            if (badgeText == null)
            {
                badgeText = CreateText(
                    "Badge",
                    18,
                    new Vector2(-270f, -18f),
                    new Vector2(260f, 28f),
                    TextAnchor.UpperLeft,
                    new Color(0.88f, 0.94f, 1f, 0.96f));
                badgeText.fontStyle = FontStyle.Bold;
            }

            if (titleText == null)
            {
                titleText = CreateText(
                    "Title",
                    38,
                    new Vector2(36f, -44f),
                    new Vector2(640f, 54f),
                    TextAnchor.UpperLeft,
                    Color.white);
                titleText.fontStyle = FontStyle.Bold;
            }

            if (subtitleCardImage == null)
            {
                subtitleCardImage = CreatePanelImage(
                    "SubtitleCard",
                    new Vector2(20f, -102f),
                    new Vector2(760f, 42f),
                    new Color(0.13f, 0.17f, 0.24f, 0.94f));
            }

            if (subtitleText == null)
            {
                subtitleText = CreateText(
                    "Subtitle",
                    21,
                    new Vector2(28f, -106f),
                    new Vector2(660f, 44f),
                    TextAnchor.MiddleLeft,
                    new Color(0.88f, 0.93f, 1f, 0.94f));
                subtitleText.supportRichText = true;
            }

            if (progressTrackImage == null)
            {
                progressTrackImage = CreatePanelImage(
                    "ProgressTrack",
                    new Vector2(0f, -144f),
                    new Vector2(910f, 8f),
                    new Color(1f, 1f, 1f, 0.08f));
            }

            if (progressFillImage == null)
            {
                progressFillImage = CreatePanelImage(
                    "ProgressFill",
                    new Vector2(-455f, -144f),
                    new Vector2(910f, 8f),
                    Color.white);
                progressFillImage.rectTransform.pivot = new Vector2(0f, 0.5f);
            }
        }

        private void ApplyBaseColors()
        {
            if (shadowImage != null)
            {
                shadowImage.color = ResolveShadowColor();
            }

            if (backgroundImage != null)
            {
                backgroundImage.color = ResolveBackgroundColor();
            }

            if (frameImage != null)
            {
                Color frameColor = ResolveFrameColor();
                frameImage.color = frameColor;
            }

            if (accentImage != null)
            {
                accentImage.color = _accentColor;
            }

            if (subtitleCardImage != null)
            {
                subtitleCardImage.color = ResolveSubtitleCardColor();
            }

            if (progressTrackImage != null)
            {
                progressTrackImage.color = new Color(1f, 1f, 1f, 0.08f);
            }

            if (progressFillImage != null)
            {
                progressFillImage.color = ResolveProgressColor();
            }

            if (badgeText != null)
            {
                badgeText.color = ResolveBadgeColor();
            }

            if (titleText != null)
            {
                titleText.color = ResolveTitleColor();
            }
        }

        private void ApplyProgress(float remainingRatio)
        {
            if (progressFillImage == null)
            {
                return;
            }

            RectTransform fillRect = progressFillImage.rectTransform;
            fillRect.sizeDelta = new Vector2(_currentProgressWidth * Mathf.Clamp01(remainingRatio), _progressFillBaseSize.y);
        }

        private void ApplyAccentPulse(float normalized)
        {
            if (accentImage == null)
            {
                return;
            }

            Color accentColor = _accentColor;
            float pulse = 1f + (Mathf.Sin(normalized * Mathf.PI * _bannerPulseCycles) * accentPulseAmplitude * _bannerPulseScale);
            accentColor *= pulse;
            accentColor.a = _accentColor.a;
            accentImage.color = accentColor;

            if (progressFillImage != null)
            {
                Color progressColor = ResolveProgressColor();
                progressColor *= 0.96f + (Mathf.Sin(normalized * Mathf.PI * Mathf.Max(1.6f, _bannerPulseCycles - 0.6f)) * accentPulseAmplitude * 0.4f * _bannerPulseScale);
                progressColor.a = ResolveProgressColor().a;
                progressFillImage.color = progressColor;
            }

            if (subtitleCardImage != null)
            {
                float detailPulse = 1f + Mathf.Sin(normalized * Mathf.PI * 4f * _detailPulseFrequencyScale) * accentPulseAmplitude * 0.18f * _detailPulseAmplitudeScale;
                Color subtitleCardColor = ResolveSubtitleCardColor();
                subtitleCardColor *= 0.98f + ((detailPulse - 1f) * 0.55f);
                subtitleCardColor.a = ResolveSubtitleCardColor().a;
                subtitleCardImage.color = subtitleCardColor;
                subtitleCardImage.rectTransform.localScale = new Vector3(1f + ((detailPulse - 1f) * 0.18f), 1f + ((detailPulse - 1f) * 0.08f), 1f);
            }
        }

        private float EvaluateEntryScale(float entryNormalized)
        {
            float eased = 1f - Mathf.Pow(1f - entryNormalized, 3f);
            float startScale = 0.94f - _bannerScaleBoost;
            float targetScale = 1f + (_bannerScaleBoost * 0.3f);
            float overshoot = Mathf.Sin(entryNormalized * Mathf.PI) * _bannerEntryOvershoot;
            return Mathf.Lerp(startScale, targetScale, eased) + overshoot;
        }

        private float EvaluateEntryOffsetY(float entryNormalized)
        {
            float eased = 1f - Mathf.Pow(1f - entryNormalized, 3f);
            float overshoot = Mathf.Sin(entryNormalized * Mathf.PI) * _bannerEntryDropDistance * _bannerEntryOvershoot * 1.8f;
            return Mathf.Lerp(_bannerEntryDropDistance, 0f, eased) - overshoot;
        }

        private void ResolveChallengeThreatAnimation(bool hasChallengeStage, string badgeLabel, ChallengeThreatStage stage)
        {
            if (!hasChallengeStage)
            {
                _bannerPulseScale = 1f;
                _bannerScaleBoost = 0.01f;
                _bannerPulseCycles = defaultBannerPulseCycles;
                _bannerEntryOvershoot = defaultEntryOvershoot;
                _bannerEntryDropDistance = defaultEntryDropDistance;
                _detailPulseFrequencyScale = 1f;
                _detailPulseAmplitudeScale = 1f;
                return;
            }

            _bannerPulseScale = ChallengeThreatPresentationResolver.ResolveBannerPulseScale(stage);
            _bannerScaleBoost = ChallengeThreatPresentationResolver.ResolveBannerScaleBoost(badgeLabel, stage);
            _bannerPulseCycles = ChallengeThreatPresentationResolver.ResolveBannerPulseCycles(badgeLabel, stage);
            _bannerEntryOvershoot = ChallengeThreatPresentationResolver.ResolveBannerEntryOvershoot(badgeLabel, stage);
            _bannerEntryDropDistance = ChallengeThreatPresentationResolver.ResolveBannerEntryDropDistance(badgeLabel, stage);
            _detailPulseFrequencyScale = ChallengeThreatPresentationResolver.ResolveFeedbackPulseFrequencyScale(badgeLabel, stage);
            _detailPulseAmplitudeScale = ChallengeThreatPresentationResolver.ResolveFeedbackPulseAmplitudeScale(badgeLabel, stage);
        }

        private static string ResolveBadgeLabel(string title, string subtitle, string resolvedChallengeBadgeLabel = null)
        {
            if (!string.IsNullOrWhiteSpace(resolvedChallengeBadgeLabel))
            {
                return resolvedChallengeBadgeLabel;
            }

            if (ChallengeThreatPresentationResolver.TryResolveBadgeLabel(title, subtitle, out string challengeBadgeLabel))
            {
                return challengeBadgeLabel;
            }

            string combined = $"{title} {subtitle}";

            if (combined.Contains("비밀") && (combined.Contains("보상") || combined.Contains("은닉처") || combined.Contains("회수")))
            {
                return "비밀 보상";
            }

            if (combined.Contains("비밀"))
            {
                return "탐험 갱신";
            }

            if (combined.Contains("저주") || combined.Contains("제단") || combined.Contains("대가"))
            {
                if (combined.Contains("진귀") || combined.Contains("금단") || combined.Contains("유물") || combined.Contains("왕관"))
                {
                    return "저주 보물";
                }

                return "저주 의식";
            }

            if (((combined.Contains("도전") || combined.Contains("웨이브")) && (combined.Contains("엘리트") || combined.Contains("승격") || combined.Contains("압박")))
                || (combined.Contains("엘리트") && (combined.Contains("경보") || combined.Contains("압박") || combined.Contains("증원")))
                || (combined.Contains("승격") && combined.Contains("경보")))
            {
                return "엘리트 경보";
            }

            if (combined.Contains("도전") || combined.Contains("페이스"))
            {
                return "챌린지";
            }

            if (combined.Contains("챔피언"))
            {
                return "엘리트 위협";
            }

            if (combined.Contains("보스"))
            {
                return "위협 감지";
            }

            if (combined.Contains("클리어"))
            {
                return "전투 종료";
            }

            if (combined.Contains("보상"))
            {
                return "전리품";
            }

            return "이벤트";
        }

        private string ResolveSubtitleMarkup(string subtitle, string eyebrow)
        {
            if (string.IsNullOrWhiteSpace(eyebrow)
                || string.IsNullOrWhiteSpace(subtitle))
            {
                return subtitle;
            }

            Color eyebrowBaseColor = _isChallengeBaselineBanner ? challengeBaselineSubtitleEyebrowColor : challengeSubtitleEyebrowColor;
            Color detailBaseColor = _isChallengeBaselineBanner ? challengeBaselineSubtitleDetailColor : challengeSubtitleDetailColor;
            string eyebrowHex = ColorUtility.ToHtmlStringRGBA(Color.Lerp(eyebrowBaseColor, _accentColor, 0.18f));
            string detailHex = ColorUtility.ToHtmlStringRGBA(Color.Lerp(detailBaseColor, Color.Lerp(_accentColor, Color.white, 0.3f), 0.12f));
            return
                $"<size={challengeSubtitleEyebrowFontSize}><color=#{eyebrowHex}><b>{eyebrow}</b></color></size>\n" +
                $"<size={challengeSubtitleDetailFontSize}><color=#{detailHex}>{subtitle}</color></size>";
        }

        private Color ResolveSubtitleCardColor()
        {
            if (_isChallengePaceBanner)
            {
                return Color.Lerp(challengePaceSubtitleCardColor, _accentColor, 0.08f);
            }

            if (_isChallengeBaselineBanner)
            {
                return Color.Lerp(challengeBaselineSubtitleCardColor, _accentColor, 0.08f);
            }

            return Color.Lerp(new Color(0.13f, 0.17f, 0.24f, 0.96f), _accentColor, 0.12f);
        }

        private Color ResolveShadowColor()
        {
            if (_isEliteWarningBanner)
            {
                return new Color(0f, 0f, 0f, 0.42f);
            }

            if (_isChallengePaceBanner)
            {
                return new Color(0f, 0f, 0f, 0.38f);
            }

            if (_isChallengeBaselineBanner)
            {
                return new Color(0f, 0f, 0f, 0.36f);
            }

            return defaultBannerShadowColor;
        }

        private Color ResolveBackgroundColor()
        {
            if (_isChallengePaceBanner)
            {
                return Color.Lerp(defaultBannerBackgroundColor, challengePaceSubtitleCardColor, 0.24f);
            }

            if (_isChallengeBaselineBanner)
            {
                return Color.Lerp(defaultBannerBackgroundColor, challengeBaselineSubtitleCardColor, 0.2f);
            }

            if (_isEliteWarningBanner)
            {
                return Color.Lerp(new Color(0.09f, 0.06f, 0.08f, 0.98f), _accentColor, 0.16f);
            }

            return defaultBannerBackgroundColor;
        }

        private Color ResolveFrameColor()
        {
            if (_isEliteWarningBanner)
            {
                Color eliteFrameColor = Color.Lerp(Color.white, _accentColor, 0.28f);
                eliteFrameColor.a = 0.2f;
                return eliteFrameColor;
            }

            if (_isChallengePaceBanner || _isChallengeBaselineBanner)
            {
                Color challengeFrameColor = Color.Lerp(Color.white, _accentColor, 0.22f);
                challengeFrameColor.a = 0.18f;
                return challengeFrameColor;
            }

            Color frameColor = Color.Lerp(defaultBannerFrameColor, _accentColor, 0.12f);
            frameColor.a = Mathf.Max(defaultBannerFrameColor.a, 0.14f);
            return frameColor;
        }

        private Color ResolveProgressColor()
        {
            if (_isChallengePaceBanner)
            {
                return Color.Lerp(challengePaceProgressColor, _accentColor, 0.2f);
            }

            if (_isChallengeBaselineBanner)
            {
                return Color.Lerp(challengeBaselineProgressColor, _accentColor, 0.16f);
            }

            return _accentColor;
        }

        private Color ResolveBadgeColor()
        {
            if (_isChallengePaceBanner)
            {
                return Color.Lerp(challengePaceBadgeColor, _accentColor, 0.18f);
            }

            if (_isChallengeBaselineBanner)
            {
                return Color.Lerp(challengeBaselineBadgeColor, _accentColor, 0.14f);
            }

            if (_isEliteWarningBanner)
            {
                return Color.Lerp(eliteWarningBadgeColor, _accentColor, 0.24f);
            }

            return Color.Lerp(Color.white, _accentColor, 0.2f);
        }

        private Color ResolveTitleColor()
        {
            if (_isChallengePaceBanner)
            {
                return Color.Lerp(challengePaceTitleColor, _accentColor, 0.12f);
            }

            if (_isChallengeBaselineBanner)
            {
                return Color.Lerp(challengeBaselineTitleColor, _accentColor, 0.08f);
            }

            return Color.white;
        }

        private Image CreatePanelImage(string objectName, Vector2 anchoredPosition, Vector2 size, Color color)
        {
            GameObject imageObject = new(objectName);
            imageObject.transform.SetParent(rectTransform, false);
            RectTransform imageRect = imageObject.AddComponent<RectTransform>();
            imageRect.anchorMin = new Vector2(0.5f, 1f);
            imageRect.anchorMax = new Vector2(0.5f, 1f);
            imageRect.pivot = new Vector2(0.5f, 1f);
            imageRect.anchoredPosition = anchoredPosition;
            imageRect.sizeDelta = size;

            Image image = imageObject.AddComponent<Image>();
            image.raycastTarget = false;
            image.color = color;
            return image;
        }

        private Text CreateText(string objectName, int fontSize, Vector2 anchoredPosition, Vector2 size, TextAnchor alignment, Color color)
        {
            GameObject textObject = new(objectName);
            textObject.transform.SetParent(rectTransform, false);

            RectTransform textRect = textObject.AddComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0.5f, 1f);
            textRect.anchorMax = new Vector2(0.5f, 1f);
            textRect.pivot = new Vector2(0.5f, 1f);
            textRect.anchoredPosition = anchoredPosition;
            textRect.sizeDelta = size;

            Text text = textObject.AddComponent<Text>();
            LocalizedUiFontProvider.ApplyReadableDefaults(
                text,
                fontSize,
                alignment,
                FontStyle.Normal,
                false,
                HorizontalWrapMode.Wrap,
                VerticalWrapMode.Truncate,
                1.04f,
                true,
                Mathf.Max(14, fontSize - 8),
                fontSize);
            text.color = color;
            return text;
        }
    }
}
