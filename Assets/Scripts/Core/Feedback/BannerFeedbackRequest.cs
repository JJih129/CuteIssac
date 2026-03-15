using UnityEngine;
using CuteIssac.Room;

namespace CuteIssac.Core.Feedback
{
    public enum ChallengeBannerLayoutProfile
    {
        None = 0,
        Baseline = 1,
        Pace = 2,
        EliteWarning = 3
    }

    public readonly struct BannerFeedbackRequest
    {
        public BannerFeedbackRequest(
            string title,
            string subtitle,
            Color accentColor,
            float duration = 1.8f,
            bool hasChallengeMetadata = false,
            string challengeBadgeLabel = "",
            string challengeSubtitleEyebrow = "",
            ChallengeThreatStage challengeStage = ChallengeThreatStage.Baseline,
            ChallengeBannerLayoutProfile challengeLayoutProfile = ChallengeBannerLayoutProfile.None)
        {
            Title = title;
            Subtitle = subtitle;
            AccentColor = accentColor;
            Duration = Mathf.Max(0.25f, duration);
            HasChallengeMetadata = hasChallengeMetadata;
            ChallengeBadgeLabel = hasChallengeMetadata ? challengeBadgeLabel ?? string.Empty : string.Empty;
            ChallengeSubtitleEyebrow = hasChallengeMetadata ? challengeSubtitleEyebrow ?? string.Empty : string.Empty;
            ChallengeStage = challengeStage;
            ChallengeLayoutProfile = hasChallengeMetadata ? challengeLayoutProfile : ChallengeBannerLayoutProfile.None;
        }

        public string Title { get; }
        public string Subtitle { get; }
        public Color AccentColor { get; }
        public float Duration { get; }
        public bool HasChallengeMetadata { get; }
        public string ChallengeBadgeLabel { get; }
        public string ChallengeSubtitleEyebrow { get; }
        public ChallengeThreatStage ChallengeStage { get; }
        public ChallengeBannerLayoutProfile ChallengeLayoutProfile { get; }
    }
}
