using UnityEngine;
using CuteIssac.Room;

namespace CuteIssac.Core.Feedback
{
    public enum FloatingFeedbackVisualProfile
    {
        Default = 0,
        EnemyDamage = 1,
        PlayerDamage = 2,
        Pickup = 3,
        EventLabel = 4
    }

    public readonly struct FloatingFeedbackRequest
    {
        public FloatingFeedbackRequest(
            Vector3 worldPosition,
            string text,
            Color textColor,
            float lifetime = 0.6f,
            float riseDistance = 0.75f,
            float startScale = 1.15f,
            bool hasChallengeMetadata = false,
            string challengeBadgeLabel = "",
            ChallengeThreatStage challengeStage = ChallengeThreatStage.Baseline,
            ChallengeBannerLayoutProfile challengeLayoutProfile = ChallengeBannerLayoutProfile.None,
            FloatingFeedbackVisualProfile visualProfile = FloatingFeedbackVisualProfile.Default)
        {
            WorldPosition = worldPosition;
            Text = text;
            TextColor = textColor;
            Lifetime = Mathf.Max(0.1f, lifetime);
            RiseDistance = Mathf.Max(0f, riseDistance);
            StartScale = Mathf.Max(0.1f, startScale);
            VisualProfile = visualProfile;
            HasChallengeMetadata = hasChallengeMetadata;
            ChallengeBadgeLabel = hasChallengeMetadata ? challengeBadgeLabel ?? string.Empty : string.Empty;
            ChallengeStage = challengeStage;
            ChallengeLayoutProfile = hasChallengeMetadata ? challengeLayoutProfile : ChallengeBannerLayoutProfile.None;
        }

        public Vector3 WorldPosition { get; }
        public string Text { get; }
        public Color TextColor { get; }
        public float Lifetime { get; }
        public float RiseDistance { get; }
        public float StartScale { get; }
        public FloatingFeedbackVisualProfile VisualProfile { get; }
        public bool HasChallengeMetadata { get; }
        public string ChallengeBadgeLabel { get; }
        public ChallengeThreatStage ChallengeStage { get; }
        public ChallengeBannerLayoutProfile ChallengeLayoutProfile { get; }
    }
}
