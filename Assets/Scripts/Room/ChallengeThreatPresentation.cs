using CuteIssac.Core.Audio;
using CuteIssac.Core.Feedback;
using CuteIssac.Data.Dungeon;
using CuteIssac.Dungeon;
using UnityEngine;

namespace CuteIssac.Room
{
    public enum ChallengeThreatStage
    {
        Baseline = 0,
        PromotionPressure = 1,
        EliteReinforcement = 2,
        ElitePressure = 3
    }

    public readonly struct ChallengeThreatPresentation
    {
        public ChallengeThreatPresentation(
            string badgeLabel,
            string bannerTitle,
            string headlineEyebrow,
            string headlineSegment,
            string detailEyebrow,
            string detailSegment,
            string compactTag,
            string floatingLabel,
            ChallengeThreatStage stage,
            Color accentColor,
            float bannerDuration)
        {
            BadgeLabel = badgeLabel;
            BannerTitle = bannerTitle;
            HeadlineEyebrow = headlineEyebrow;
            HeadlineSegment = headlineSegment;
            DetailEyebrow = detailEyebrow;
            DetailSegment = detailSegment;
            CompactTag = compactTag;
            FloatingLabel = floatingLabel;
            Stage = stage;
            AccentColor = accentColor;
            BannerDuration = bannerDuration;
            LayoutProfile = ChallengeThreatPresentationResolver.ResolveBannerLayoutProfile(badgeLabel);
        }

        public string BadgeLabel { get; }

        public string BannerTitle { get; }

        public string HeadlineEyebrow { get; }

        public string HeadlineSegment { get; }

        public string DetailEyebrow { get; }

        public string DetailSegment { get; }

        public string CompactTag { get; }

        public string FloatingLabel { get; }

        public ChallengeThreatStage Stage { get; }

        public Color AccentColor { get; }

        public float BannerDuration { get; }

        public ChallengeBannerLayoutProfile LayoutProfile { get; }
    }

    public readonly struct ChallengePaceBannerPresentation
    {
        public ChallengePaceBannerPresentation(
            string title,
            string subtitle,
            string badgeLabel,
            string subtitleEyebrow,
            string floatingLabel,
            ChallengeThreatStage stage,
            Color accentColor,
            float duration)
        {
            Title = title;
            Subtitle = subtitle;
            BadgeLabel = badgeLabel;
            SubtitleEyebrow = subtitleEyebrow;
            FloatingLabel = floatingLabel;
            Stage = stage;
            AccentColor = accentColor;
            Duration = duration;
            LayoutProfile = ChallengeThreatPresentationResolver.ResolveBannerLayoutProfile(badgeLabel);
        }

        public string Title { get; }

        public string Subtitle { get; }

        public string BadgeLabel { get; }

        public string SubtitleEyebrow { get; }

        public string FloatingLabel { get; }

        public ChallengeThreatStage Stage { get; }

        public Color AccentColor { get; }

        public float Duration { get; }

        public ChallengeBannerLayoutProfile LayoutProfile { get; }
    }

    public readonly struct ChallengeRoomStatusPresentation
    {
        public ChallengeRoomStatusPresentation(string badgeLabel, string headline, string detail, Color accentColor, string eyebrow = null, string compactTag = null, string detailEyebrow = null)
        {
            BadgeLabel = badgeLabel;
            Headline = headline;
            Detail = detail;
            AccentColor = accentColor;
            Eyebrow = eyebrow ?? string.Empty;
            CompactTag = compactTag ?? string.Empty;
            DetailEyebrow = detailEyebrow ?? string.Empty;
        }

        public string BadgeLabel { get; }

        public string Headline { get; }

        public string Detail { get; }

        public Color AccentColor { get; }

        public string Eyebrow { get; }

        public string CompactTag { get; }

        public string DetailEyebrow { get; }
    }

    public readonly struct ChallengeWaveIntermissionPresentation
    {
        public ChallengeWaveIntermissionPresentation(
            string floatingLabel,
            Color accentColor,
            float duration,
            GameAudioEventType audioEventType,
            float audioVolumeScale,
            float audioPitchScale)
        {
            FloatingLabel = floatingLabel;
            AccentColor = accentColor;
            Duration = duration;
            AudioEventType = audioEventType;
            AudioVolumeScale = audioVolumeScale;
            AudioPitchScale = audioPitchScale;
        }

        public string FloatingLabel { get; }
        public Color AccentColor { get; }
        public float Duration { get; }
        public GameAudioEventType AudioEventType { get; }
        public float AudioVolumeScale { get; }
        public float AudioPitchScale { get; }
    }

    public static class ChallengeThreatPresentationResolver
    {
        private const string ChallengeBadge = "Challenge";
        private const string PaceBadge = "Timer Pace";
        private const string EliteBadge = "Elite Warning";

        public static bool TryResolveStage(string title, string subtitle, out ChallengeThreatStage stage)
        {
            return TryResolveBannerCopy(title, subtitle, out _, out _, out stage);
        }

        public static bool TryResolveBannerCopy(
            string title,
            string subtitle,
            out string badgeLabel,
            out string subtitleEyebrow,
            out ChallengeThreatStage stage)
        {
            string combined = $"{title} {subtitle}";
            if (string.IsNullOrWhiteSpace(combined))
            {
                badgeLabel = string.Empty;
                subtitleEyebrow = string.Empty;
                stage = ChallengeThreatStage.Baseline;
                return false;
            }

            string normalized = combined.ToLowerInvariant();
            if (normalized.Contains("pace") || normalized.Contains("rank") || normalized.Contains("clear"))
            {
                badgeLabel = PaceBadge;
                subtitleEyebrow = normalized.Contains("danger") || normalized.Contains("warning") ? "Danger Window" : "Reward Pace";
                stage = normalized.Contains("danger") || normalized.Contains("warning") ? ChallengeThreatStage.PromotionPressure : ChallengeThreatStage.Baseline;
                return true;
            }

            if (normalized.Contains("elite") || normalized.Contains("reinforcement") || normalized.Contains("pressure"))
            {
                badgeLabel = EliteBadge;
                subtitleEyebrow = normalized.Contains("reinforcement") ? "Reinforcement" : "Pressure";
                stage = normalized.Contains("reinforcement") ? ChallengeThreatStage.EliteReinforcement : ChallengeThreatStage.ElitePressure;
                return true;
            }

            if (normalized.Contains("challenge") || normalized.Contains("wave"))
            {
                badgeLabel = ChallengeBadge;
                subtitleEyebrow = normalized.Contains("wave") ? "Wave Alert" : "Combat Trial";
                stage = ChallengeThreatStage.Baseline;
                return true;
            }

            badgeLabel = string.Empty;
            subtitleEyebrow = string.Empty;
            stage = ChallengeThreatStage.Baseline;
            return false;
        }

        public static bool TryResolveBadgeLabel(string title, string subtitle, out string badgeLabel)
        {
            return TryResolveBannerCopy(title, subtitle, out badgeLabel, out _, out _);
        }

        public static bool TryResolveBannerSubtitleEyebrow(string title, string subtitle, out string eyebrow)
        {
            return TryResolveBannerCopy(title, subtitle, out _, out eyebrow, out _);
        }

        public static ChallengeBannerLayoutProfile ResolveBannerLayoutProfile(string badgeLabel)
        {
            if (string.Equals(badgeLabel, PaceBadge))
            {
                return ChallengeBannerLayoutProfile.Pace;
            }

            if (string.Equals(badgeLabel, EliteBadge))
            {
                return ChallengeBannerLayoutProfile.EliteWarning;
            }

            if (string.Equals(badgeLabel, ChallengeBadge))
            {
                return ChallengeBannerLayoutProfile.Baseline;
            }

            return ChallengeBannerLayoutProfile.None;
        }

        public static float ResolveBannerPulseScale(ChallengeThreatStage stage)
        {
            return stage switch
            {
                ChallengeThreatStage.ElitePressure => 1.55f,
                ChallengeThreatStage.EliteReinforcement => 1.32f,
                ChallengeThreatStage.PromotionPressure => 1.18f,
                _ => 1f
            };
        }

        public static float ResolveBannerScaleBoost(ChallengeThreatStage stage)
        {
            return stage switch
            {
                ChallengeThreatStage.ElitePressure => 0.026f,
                ChallengeThreatStage.EliteReinforcement => 0.02f,
                ChallengeThreatStage.PromotionPressure => 0.014f,
                _ => 0.01f
            };
        }

        public static float ResolveBannerScaleBoost(string badgeLabel, ChallengeThreatStage stage)
        {
            float value = ResolveBannerScaleBoost(stage);
            return ResolveBannerLayoutProfile(badgeLabel) switch
            {
                ChallengeBannerLayoutProfile.Pace => value * 1.06f,
                ChallengeBannerLayoutProfile.Baseline => value * 0.88f,
                _ => value
            };
        }

        public static float ResolveBannerPulseCycles(ChallengeThreatStage stage)
        {
            return stage switch
            {
                ChallengeThreatStage.ElitePressure => 4.2f,
                ChallengeThreatStage.EliteReinforcement => 3.4f,
                ChallengeThreatStage.PromotionPressure => 2.7f,
                _ => 2.1f
            };
        }

        public static float ResolveBannerPulseCycles(string badgeLabel, ChallengeThreatStage stage)
        {
            float value = ResolveBannerPulseCycles(stage);
            return ResolveBannerLayoutProfile(badgeLabel) switch
            {
                ChallengeBannerLayoutProfile.Pace => value * 1.08f,
                ChallengeBannerLayoutProfile.Baseline => value * 0.86f,
                _ => value
            };
        }

        public static float ResolveBannerEntryOvershoot(ChallengeThreatStage stage)
        {
            return stage switch
            {
                ChallengeThreatStage.ElitePressure => 1.18f,
                ChallengeThreatStage.EliteReinforcement => 1.13f,
                ChallengeThreatStage.PromotionPressure => 1.08f,
                _ => 1.04f
            };
        }

        public static float ResolveBannerEntryOvershoot(string badgeLabel, ChallengeThreatStage stage)
        {
            float value = ResolveBannerEntryOvershoot(stage);
            return ResolveBannerLayoutProfile(badgeLabel) == ChallengeBannerLayoutProfile.Pace ? value * 1.03f : value;
        }

        public static float ResolveBannerEntryDropDistance(ChallengeThreatStage stage)
        {
            return stage switch
            {
                ChallengeThreatStage.ElitePressure => 72f,
                ChallengeThreatStage.EliteReinforcement => 60f,
                ChallengeThreatStage.PromotionPressure => 50f,
                _ => 42f
            };
        }

        public static float ResolveBannerEntryDropDistance(string badgeLabel, ChallengeThreatStage stage)
        {
            float value = ResolveBannerEntryDropDistance(stage);
            return ResolveBannerLayoutProfile(badgeLabel) == ChallengeBannerLayoutProfile.Baseline ? value * 0.82f : value;
        }

        public static float ResolveStatusThemeStrength(ChallengeThreatStage stage)
        {
            return stage switch
            {
                ChallengeThreatStage.ElitePressure => 1.25f,
                ChallengeThreatStage.EliteReinforcement => 1.16f,
                ChallengeThreatStage.PromotionPressure => 1.08f,
                _ => 0.98f
            };
        }

        public static float ResolveFeedbackPulseFrequencyScale(string badgeLabel, ChallengeThreatStage stage)
        {
            float value = stage switch
            {
                ChallengeThreatStage.ElitePressure => 1.35f,
                ChallengeThreatStage.EliteReinforcement => 1.22f,
                ChallengeThreatStage.PromotionPressure => 1.12f,
                _ => 1f
            };
            return ResolveBannerLayoutProfile(badgeLabel) == ChallengeBannerLayoutProfile.Pace ? value * 1.08f : value;
        }

        public static float ResolveFeedbackPulseAmplitudeScale(string badgeLabel, ChallengeThreatStage stage)
        {
            float value = stage switch
            {
                ChallengeThreatStage.ElitePressure => 1.34f,
                ChallengeThreatStage.EliteReinforcement => 1.18f,
                ChallengeThreatStage.PromotionPressure => 1.1f,
                _ => 1f
            };
            return ResolveBannerLayoutProfile(badgeLabel) == ChallengeBannerLayoutProfile.Baseline ? value * 0.9f : value;
        }

        public static float ResolveNodePulseScale(ChallengeThreatStage stage)
        {
            return stage switch
            {
                ChallengeThreatStage.ElitePressure => 1.28f,
                ChallengeThreatStage.EliteReinforcement => 1.18f,
                ChallengeThreatStage.PromotionPressure => 1.1f,
                _ => 1f
            };
        }

        public static float ResolveWarningFlashOpacity(ChallengeThreatStage stage)
        {
            return stage switch
            {
                ChallengeThreatStage.ElitePressure => 0.7f,
                ChallengeThreatStage.EliteReinforcement => 0.55f,
                ChallengeThreatStage.PromotionPressure => 0.42f,
                _ => 0.28f
            };
        }

        public static float ResolveWarningFlashOpacity(string badgeLabel, ChallengeThreatStage stage)
        {
            float value = ResolveWarningFlashOpacity(stage);
            return ResolveBannerLayoutProfile(badgeLabel) == ChallengeBannerLayoutProfile.Pace ? value * 0.86f : value;
        }

        public static float ResolveWarningFlashDuration(ChallengeThreatStage stage)
        {
            return stage switch
            {
                ChallengeThreatStage.ElitePressure => 0.7f,
                ChallengeThreatStage.EliteReinforcement => 0.56f,
                ChallengeThreatStage.PromotionPressure => 0.46f,
                _ => 0.34f
            };
        }

        public static int ResolveWarningFlashPulseCount(ChallengeThreatStage stage)
        {
            return stage switch
            {
                ChallengeThreatStage.ElitePressure => 4,
                ChallengeThreatStage.EliteReinforcement => 3,
                ChallengeThreatStage.PromotionPressure => 2,
                _ => 1
            };
        }

        public static float ResolveWarningFlashPulseStrength(ChallengeThreatStage stage)
        {
            return stage switch
            {
                ChallengeThreatStage.ElitePressure => 1.35f,
                ChallengeThreatStage.EliteReinforcement => 1.2f,
                ChallengeThreatStage.PromotionPressure => 1.08f,
                _ => 0.92f
            };
        }

        public static float ResolveWarningFlashPulseStrength(string badgeLabel, ChallengeThreatStage stage)
        {
            float value = ResolveWarningFlashPulseStrength(stage);
            return ResolveBannerLayoutProfile(badgeLabel) == ChallengeBannerLayoutProfile.Baseline ? value * 0.86f : value;
        }

        public static float ResolveWarningFlashFrequencyScale(ChallengeThreatStage stage)
        {
            return stage switch
            {
                ChallengeThreatStage.ElitePressure => 1.35f,
                ChallengeThreatStage.EliteReinforcement => 1.18f,
                ChallengeThreatStage.PromotionPressure => 1.08f,
                _ => 1f
            };
        }

        public static float ResolveWarningFlashFrequencyScale(string badgeLabel, ChallengeThreatStage stage)
        {
            float value = ResolveWarningFlashFrequencyScale(stage);
            return ResolveBannerLayoutProfile(badgeLabel) == ChallengeBannerLayoutProfile.Pace ? value * 1.08f : value;
        }

        public static float ResolveWarningFlashDecaySoftness(ChallengeThreatStage stage)
        {
            return stage switch
            {
                ChallengeThreatStage.ElitePressure => 0.64f,
                ChallengeThreatStage.EliteReinforcement => 0.72f,
                ChallengeThreatStage.PromotionPressure => 0.82f,
                _ => 0.9f
            };
        }

        public static float ResolveWarningFlashDecaySoftness(string badgeLabel, ChallengeThreatStage stage)
        {
            float value = ResolveWarningFlashDecaySoftness(stage);
            return ResolveBannerLayoutProfile(badgeLabel) == ChallengeBannerLayoutProfile.Baseline ? Mathf.Min(1f, value + 0.06f) : value;
        }

        public static Color ResolveWarningFlashColor(string badgeLabel, Color accentColor)
        {
            return ResolveBannerLayoutProfile(badgeLabel) switch
            {
                ChallengeBannerLayoutProfile.Pace => Color.Lerp(accentColor, new Color(1f, 0.84f, 0.3f, 1f), 0.35f),
                ChallengeBannerLayoutProfile.EliteWarning => Color.Lerp(accentColor, new Color(1f, 0.2f, 0.16f, 1f), 0.45f),
                _ => accentColor
            };
        }

        public static GameAudioEventType ResolveWarningAudioEventType(ChallengeThreatStage stage)
        {
            return stage switch
            {
                ChallengeThreatStage.ElitePressure => GameAudioEventType.BossAppeared,
                ChallengeThreatStage.EliteReinforcement => GameAudioEventType.EnemyDied,
                ChallengeThreatStage.PromotionPressure => GameAudioEventType.PlayerDamaged,
                _ => GameAudioEventType.RewardSpawned
            };
        }

        public static GameAudioEventType ResolveWarningAudioEventType(string badgeLabel, ChallengeThreatStage stage)
        {
            if (ResolveBannerLayoutProfile(badgeLabel) == ChallengeBannerLayoutProfile.Pace)
            {
                return GameAudioEventType.RewardSpawned;
            }

            return ResolveWarningAudioEventType(stage);
        }

        public static float ResolveWarningAudioVolumeScale(ChallengeThreatStage stage)
        {
            return stage switch
            {
                ChallengeThreatStage.ElitePressure => 0.76f,
                ChallengeThreatStage.EliteReinforcement => 0.66f,
                ChallengeThreatStage.PromotionPressure => 0.58f,
                _ => 0.44f
            };
        }

        public static float ResolveWarningAudioVolumeScale(string badgeLabel, ChallengeThreatStage stage)
        {
            float value = ResolveWarningAudioVolumeScale(stage);
            return ResolveBannerLayoutProfile(badgeLabel) == ChallengeBannerLayoutProfile.Baseline ? value * 0.82f : value;
        }

        public static float ResolveWarningAudioPitchScale(ChallengeThreatStage stage)
        {
            return stage switch
            {
                ChallengeThreatStage.ElitePressure => 0.92f,
                ChallengeThreatStage.EliteReinforcement => 0.98f,
                ChallengeThreatStage.PromotionPressure => 1.08f,
                _ => 1f
            };
        }

        public static float ResolveWarningAudioPitchScale(string badgeLabel, ChallengeThreatStage stage)
        {
            float value = ResolveWarningAudioPitchScale(stage);
            return ResolveBannerLayoutProfile(badgeLabel) == ChallengeBannerLayoutProfile.Pace ? value * 1.08f : value;
        }

        public static ChallengeRoomStatusPresentation BuildFallbackRoomStatusPresentation(RoomState roomState, bool hasRewardContent)
        {
            string headline = roomState switch
            {
                RoomState.Combat => "Challenge combat active",
                RoomState.Rewarded when hasRewardContent => "Challenge room cleared",
                _ => "Challenge room"
            };
            return new ChallengeRoomStatusPresentation(ChallengeBadge, headline, ResolveSharedChallengeFallbackDetailCopy(roomState, hasRewardContent), new Color(1f, 0.72f, 0.34f, 1f), "Combat Trial");
        }

        public static void BuildFallbackRoomStatus(RoomState roomState, bool hasRewardContent, out string headline, out string detail, out Color accentColor)
        {
            ChallengeRoomStatusPresentation presentation = BuildFallbackRoomStatusPresentation(roomState, hasRewardContent);
            headline = presentation.Headline;
            detail = presentation.Detail;
            accentColor = presentation.AccentColor;
        }

        public static ChallengeRoomStatusPresentation BuildProgressStatusPresentation(ChallengeClearRank liveRank, float elapsedSeconds, ChallengeRewardSettings challengeRewardSettings)
        {
            return new ChallengeRoomStatusPresentation(PaceBadge, $"Challenge {ResolveRankLabel(liveRank)} pace", ResolveSharedPaceDetailCopy(liveRank, challengeRewardSettings, elapsedSeconds), ResolvePaceBannerAccent(liveRank), ResolvePaceBannerEyebrow(ChallengeClearRank.None, liveRank));
        }

        public static void BuildProgressStatus(ChallengeClearRank liveRank, float elapsedSeconds, ChallengeRewardSettings challengeRewardSettings, out string headline, out string detail, out Color accentColor)
        {
            ChallengeRoomStatusPresentation presentation = BuildProgressStatusPresentation(liveRank, elapsedSeconds, challengeRewardSettings);
            headline = presentation.Headline;
            detail = presentation.Detail;
            accentColor = presentation.AccentColor;
        }

        public static ChallengeRoomStatusPresentation BuildCombatStatusPresentation(float elapsedSeconds, bool usePaceBadge = false)
        {
            return new ChallengeRoomStatusPresentation(usePaceBadge ? PaceBadge : ChallengeBadge, "Challenge combat", ResolveSharedChallengeCombatDetailCopy(elapsedSeconds), new Color(1f, 0.58f, 0.24f, 1f), usePaceBadge ? "Recovery Window" : "Combat Trial");
        }

        public static void BuildCombatStatus(float elapsedSeconds, out string headline, out string detail, out Color accentColor)
        {
            ChallengeRoomStatusPresentation presentation = BuildCombatStatusPresentation(elapsedSeconds);
            headline = presentation.Headline;
            detail = presentation.Detail;
            accentColor = presentation.AccentColor;
        }

        public static ChallengeRoomStatusPresentation BuildClearStatusPresentation(ChallengeClearRank clearRank, bool usePaceBadge = false)
        {
            return new ChallengeRoomStatusPresentation(usePaceBadge ? PaceBadge : ChallengeBadge, $"Challenge clear - {ResolveRankLabel(clearRank)}", ResolveSharedChallengeClearDetailCopy(clearRank), ResolvePaceBannerAccent(clearRank), usePaceBadge ? ResolvePaceBannerEyebrow(ChallengeClearRank.None, clearRank) : "Combat Trial");
        }

        public static void BuildClearStatus(ChallengeClearRank clearRank, out string headline, out string detail, out Color accentColor)
        {
            ChallengeRoomStatusPresentation presentation = BuildClearStatusPresentation(clearRank);
            headline = presentation.Headline;
            detail = presentation.Detail;
            accentColor = presentation.AccentColor;
        }

        public static string ResolvePaceBannerTitle(ChallengeClearRank previousRank, ChallengeClearRank currentRank)
        {
            if (currentRank > previousRank)
            {
                return "Challenge pace improved";
            }

            return "Challenge pace warning";
        }

        public static string ResolvePaceBannerSubtitle(ChallengeClearRank previousRank, ChallengeClearRank currentRank, float elapsedSeconds, ChallengeRewardSettings challengeRewardSettings)
        {
            return $"{ResolveRankLabel(currentRank)} pace at {FormatSeconds(elapsedSeconds)}";
        }

        public static string ResolvePaceBannerSubtitle(ChallengeClearRank currentRank, ChallengeRewardSettings challengeRewardSettings, float elapsedSeconds)
        {
            return ResolvePaceBannerSubtitle(ChallengeClearRank.None, currentRank, elapsedSeconds, challengeRewardSettings);
        }

        private static string ResolveSharedPaceDetailCopy(ChallengeClearRank liveRank, ChallengeRewardSettings challengeRewardSettings, float elapsedSeconds)
        {
            return $"Current pace: {ResolveRankLabel(liveRank)} / {FormatSeconds(elapsedSeconds)}";
        }

        private static string ResolveSharedChallengeFallbackDetailCopy(RoomState roomState, bool hasRewardContent)
        {
            if (roomState == RoomState.Combat)
            {
                return "Clear the challenge encounter.";
            }

            return hasRewardContent ? "Claim the challenge reward." : "Challenge room discovered.";
        }

        private static string ResolveSharedChallengeCombatDetailCopy(float elapsedSeconds)
        {
            return $"Challenge timer: {FormatSeconds(elapsedSeconds)}";
        }

        private static string ResolveSharedChallengeClearDetailCopy(ChallengeClearRank clearRank)
        {
            return $"Clear rank: {ResolveRankLabel(clearRank)}";
        }

        public static Color ResolvePaceBannerAccent(ChallengeClearRank currentRank)
        {
            return currentRank switch
            {
                ChallengeClearRank.S => new Color(1f, 0.84f, 0.34f, 1f),
                ChallengeClearRank.A => new Color(1f, 0.62f, 0.28f, 1f),
                ChallengeClearRank.B => new Color(0.92f, 0.42f, 0.2f, 1f),
                _ => new Color(1f, 0.72f, 0.34f, 1f)
            };
        }

        public static float ResolvePaceBannerDuration(ChallengeClearRank currentRank)
        {
            return currentRank switch
            {
                ChallengeClearRank.S => 1.45f,
                ChallengeClearRank.A => 1.25f,
                _ => 1.1f
            };
        }

        public static string ResolvePaceBannerEyebrow(ChallengeClearRank previousRank, ChallengeClearRank currentRank)
        {
            return currentRank switch
            {
                ChallengeClearRank.S => "Top Pace",
                ChallengeClearRank.A => "Recovery Window",
                ChallengeClearRank.B => "Danger Window",
                _ => "Challenge Pace"
            };
        }

        public static ChallengeThreatStage ResolvePaceBannerStage(ChallengeClearRank previousRank, ChallengeClearRank currentRank)
        {
            return currentRank >= ChallengeClearRank.A ? ChallengeThreatStage.Baseline : ChallengeThreatStage.PromotionPressure;
        }

        public static string ResolvePaceFloatingLabel(ChallengeClearRank previousRank, ChallengeClearRank currentRank)
        {
            return currentRank > previousRank ? "+Pace" : "Pace";
        }

        public static ChallengePaceBannerPresentation BuildPaceBannerPresentation(ChallengeClearRank previousRank, ChallengeClearRank currentRank, float elapsedSeconds, ChallengeRewardSettings challengeRewardSettings)
        {
            return new ChallengePaceBannerPresentation(
                ResolvePaceBannerTitle(previousRank, currentRank),
                ResolvePaceBannerSubtitle(previousRank, currentRank, elapsedSeconds, challengeRewardSettings),
                PaceBadge,
                ResolvePaceBannerEyebrow(previousRank, currentRank),
                ResolvePaceFloatingLabel(previousRank, currentRank),
                ResolvePaceBannerStage(previousRank, currentRank),
                ResolvePaceBannerAccent(currentRank),
                ResolvePaceBannerDuration(currentRank));
        }

        public static ChallengePaceBannerPresentation BuildPaceBannerPresentation(ChallengeClearRank previousRank, ChallengeClearRank currentRank, ChallengeRewardSettings challengeRewardSettings, float elapsedSeconds)
        {
            return BuildPaceBannerPresentation(previousRank, currentRank, elapsedSeconds, challengeRewardSettings);
        }

        public static ChallengeThreatPresentation Build(ChallengePressureTier pressureTier, int waveIndex, int waveCount, int enemyCount, int guaranteedChampionCount, Color accentColor, float bannerDuration)
        {
            ChallengeThreatStage stage = pressureTier switch
            {
                ChallengePressureTier.Deadly => ChallengeThreatStage.ElitePressure,
                ChallengePressureTier.Elite => ChallengeThreatStage.EliteReinforcement,
                ChallengePressureTier.Reinforced => ChallengeThreatStage.PromotionPressure,
                _ => ChallengeThreatStage.Baseline
            };
            string badge = stage >= ChallengeThreatStage.EliteReinforcement ? EliteBadge : ChallengeBadge;
            string wave = $"Wave {Mathf.Max(1, waveIndex)}/{Mathf.Max(1, waveCount)}";
            string elite = guaranteedChampionCount > 0 ? $"Elite x{guaranteedChampionCount}" : "Standard wave";
            return new ChallengeThreatPresentation(
                badge,
                stage == ChallengeThreatStage.Baseline ? "Challenge wave" : "Challenge pressure",
                wave,
                $"Enemies x{Mathf.Max(0, enemyCount)}",
                elite,
                ResolveSharedThreatDetailCopy(waveIndex, waveCount, enemyCount, guaranteedChampionCount),
                pressureTier.ToString(),
                stage == ChallengeThreatStage.Baseline ? "Wave" : "Warning",
                stage,
                accentColor,
                bannerDuration);
        }

        public static ChallengeThreatPresentation Build(int waveIndex, int waveCount, int enemyCount, int guaranteedChampionCount, float championChanceBonus)
        {
            ChallengePressureTier pressureTier = ResolvePressureTier(enemyCount, guaranteedChampionCount, championChanceBonus);
            Color accentColor = pressureTier switch
            {
                ChallengePressureTier.Deadly => new Color(1f, 0.22f, 0.16f, 1f),
                ChallengePressureTier.Elite => new Color(1f, 0.42f, 0.18f, 1f),
                ChallengePressureTier.Reinforced => new Color(1f, 0.62f, 0.26f, 1f),
                _ => new Color(1f, 0.72f, 0.34f, 1f)
            };
            return Build(pressureTier, waveIndex, waveCount, enemyCount, guaranteedChampionCount, accentColor, 1.35f);
        }

        public static ChallengeWaveIntermissionPresentation BuildWaveIntermission(int waveIndex, int waveCount, float duration)
        {
            return new ChallengeWaveIntermissionPresentation(
                $"Wave {Mathf.Max(1, waveIndex)}/{Mathf.Max(1, waveCount)}",
                new Color(1f, 0.72f, 0.34f, 1f),
                Mathf.Max(0.1f, duration),
                GameAudioEventType.RewardSpawned,
                0.45f,
                1f);
        }

        public static ChallengeWaveIntermissionPresentation BuildWaveIntermission(int clearedWave, int totalWaves, int nextEnemyCount, int nextGuaranteedChampionCount, float nextChampionChanceBonus)
        {
            ChallengePressureTier pressureTier = ResolvePressureTier(nextEnemyCount, nextGuaranteedChampionCount, nextChampionChanceBonus);
            Color accentColor = pressureTier >= ChallengePressureTier.Elite
                ? new Color(1f, 0.42f, 0.18f, 1f)
                : new Color(1f, 0.72f, 0.34f, 1f);
            return new ChallengeWaveIntermissionPresentation(
                $"Next wave {Mathf.Min(Mathf.Max(1, clearedWave + 1), Mathf.Max(1, totalWaves))}/{Mathf.Max(1, totalWaves)}",
                accentColor,
                0.9f,
                GameAudioEventType.RewardSpawned,
                0.45f,
                pressureTier >= ChallengePressureTier.Elite ? 1.08f : 1f);
        }

        private static ChallengePressureTier ResolvePressureTier(int enemyCount, int guaranteedChampionCount, float championChanceBonus)
        {
            if (guaranteedChampionCount > 0 && championChanceBonus >= 0.5f)
            {
                return ChallengePressureTier.Deadly;
            }

            if (guaranteedChampionCount > 0)
            {
                return ChallengePressureTier.Elite;
            }

            if (championChanceBonus > 0f || enemyCount >= 6)
            {
                return ChallengePressureTier.Reinforced;
            }

            return ChallengePressureTier.None;
        }

        private static string ResolveSharedThreatDetailCopy(int waveIndex, int waveCount, int enemyCount, int guaranteedChampionCount)
        {
            string elite = guaranteedChampionCount > 0 ? $", elite x{guaranteedChampionCount}" : string.Empty;
            return $"Wave {Mathf.Max(1, waveIndex)}/{Mathf.Max(1, waveCount)}, enemies x{Mathf.Max(0, enemyCount)}{elite}";
        }

        private static string ResolveRankLabel(ChallengeClearRank rank)
        {
            return rank switch
            {
                ChallengeClearRank.S => "S",
                ChallengeClearRank.A => "A",
                ChallengeClearRank.B => "B",
                _ => "-"
            };
        }

        private static string FormatSeconds(float seconds)
        {
            int totalSeconds = Mathf.Max(0, Mathf.RoundToInt(seconds));
            return $"{totalSeconds / 60:00}:{totalSeconds % 60:00}";
        }
    }
}
