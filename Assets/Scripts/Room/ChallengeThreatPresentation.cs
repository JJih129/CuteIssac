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

            if (TryResolvePaceBannerCopy(combined, out badgeLabel, out subtitleEyebrow, out stage))
            {
                return true;
            }

            if (combined.Contains("엘리트 압박 경보") || combined.Contains("엘리트 압박"))
            {
                badgeLabel = "엘리트 경보";
                subtitleEyebrow = "확정 위협";
                stage = ChallengeThreatStage.ElitePressure;
                return true;
            }

            if (combined.Contains("엘리트 증원 경보") || combined.Contains("엘리트 증원"))
            {
                badgeLabel = "엘리트 경보";
                subtitleEyebrow = "증원 경보";
                stage = ChallengeThreatStage.EliteReinforcement;
                return true;
            }

            if (combined.Contains("승격 압박 경보") || combined.Contains("승격 압박"))
            {
                badgeLabel = "엘리트 경보";
                subtitleEyebrow = "압박 경보";
                stage = ChallengeThreatStage.PromotionPressure;
                return true;
            }

            if (combined.Contains("도전 웨이브") || combined.Contains("승격 +"))
            {
                badgeLabel = "챌린지";
                subtitleEyebrow = combined.Contains("승격 +") ? "승격 예고" : "웨이브 예고";
                stage = ChallengeThreatStage.Baseline;
                return true;
            }

            badgeLabel = string.Empty;
            subtitleEyebrow = string.Empty;
            stage = ChallengeThreatStage.Baseline;
            return false;
        }

        private static bool TryResolvePaceBannerCopy(
            string combined,
            out string badgeLabel,
            out string subtitleEyebrow,
            out ChallengeThreatStage stage)
        {
            if (combined.Contains("도전 페이스 상승") || combined.Contains("S 보상 구간 유지"))
            {
                badgeLabel = "도전 페이스";
                subtitleEyebrow = "최상 구간";
                stage = ChallengeThreatStage.Baseline;
                return true;
            }

            if (combined.Contains("도전 페이스 회복") || combined.Contains("도전 전투 갱신"))
            {
                badgeLabel = "도전 페이스";
                subtitleEyebrow = "회복 구간";
                stage = ChallengeThreatStage.Baseline;
                return true;
            }

            if (combined.Contains("도전 페이스 하락"))
            {
                badgeLabel = "도전 페이스";
                subtitleEyebrow = "속도 경고";
                stage = ChallengeThreatStage.PromotionPressure;
                return true;
            }

            if (combined.Contains("도전 페이스 경고") || combined.Contains("도전 전투 경고"))
            {
                badgeLabel = "도전 페이스";
                subtitleEyebrow = "위험 구간";
                stage = ChallengeThreatStage.PromotionPressure;
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
            if (string.Equals(badgeLabel, "도전 페이스"))
            {
                return ChallengeBannerLayoutProfile.Pace;
            }

            if (string.Equals(badgeLabel, "엘리트 경보"))
            {
                return ChallengeBannerLayoutProfile.EliteWarning;
            }

            if (string.Equals(badgeLabel, "챌린지"))
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
            float baseBoost = ResolveBannerScaleBoost(stage);
            if (string.Equals(badgeLabel, "챌린지"))
            {
                return baseBoost * 0.88f;
            }

            if (string.Equals(badgeLabel, "도전 페이스"))
            {
                return baseBoost * 1.06f;
            }

            return baseBoost;
        }

        public static float ResolveBannerPulseCycles(ChallengeThreatStage stage)
        {
            return stage switch
            {
                ChallengeThreatStage.ElitePressure => 4.2f,
                ChallengeThreatStage.EliteReinforcement => 3.4f,
                ChallengeThreatStage.PromotionPressure => 2.6f,
                _ => 2f
            };
        }

        public static float ResolveBannerPulseCycles(string badgeLabel, ChallengeThreatStage stage)
        {
            float baseCycles = ResolveBannerPulseCycles(stage);
            if (string.Equals(badgeLabel, "챌린지"))
            {
                return Mathf.Max(1.6f, baseCycles * 0.88f);
            }

            if (string.Equals(badgeLabel, "도전 페이스"))
            {
                return baseCycles * 1.08f;
            }

            return baseCycles;
        }

        public static float ResolveBannerEntryOvershoot(ChallengeThreatStage stage)
        {
            return stage switch
            {
                ChallengeThreatStage.ElitePressure => 0.072f,
                ChallengeThreatStage.EliteReinforcement => 0.054f,
                ChallengeThreatStage.PromotionPressure => 0.038f,
                _ => 0.022f
            };
        }

        public static float ResolveBannerEntryOvershoot(string badgeLabel, ChallengeThreatStage stage)
        {
            float baseOvershoot = ResolveBannerEntryOvershoot(stage);
            if (string.Equals(badgeLabel, "챌린지"))
            {
                return baseOvershoot * 0.84f;
            }

            if (string.Equals(badgeLabel, "도전 페이스"))
            {
                return baseOvershoot * 1.04f;
            }

            return baseOvershoot;
        }

        public static float ResolveBannerEntryDropDistance(ChallengeThreatStage stage)
        {
            return stage switch
            {
                ChallengeThreatStage.ElitePressure => 34f,
                ChallengeThreatStage.EliteReinforcement => 26f,
                ChallengeThreatStage.PromotionPressure => 18f,
                _ => 12f
            };
        }

        public static float ResolveBannerEntryDropDistance(string badgeLabel, ChallengeThreatStage stage)
        {
            float baseDropDistance = ResolveBannerEntryDropDistance(stage);
            if (string.Equals(badgeLabel, "챌린지"))
            {
                return baseDropDistance * 0.82f;
            }

            if (string.Equals(badgeLabel, "도전 페이스"))
            {
                return baseDropDistance * 0.92f;
            }

            return baseDropDistance;
        }

        public static float ResolveStatusThemeStrength(ChallengeThreatStage stage)
        {
            return stage switch
            {
                ChallengeThreatStage.ElitePressure => 1.25f,
                ChallengeThreatStage.EliteReinforcement => 1.12f,
                ChallengeThreatStage.PromotionPressure => 1.04f,
                _ => 0.92f
            };
        }

        public static float ResolveFeedbackPulseFrequencyScale(string badgeLabel, ChallengeThreatStage stage)
        {
            float baseFrequency = stage switch
            {
                ChallengeThreatStage.ElitePressure => 1.24f,
                ChallengeThreatStage.EliteReinforcement => 1.12f,
                ChallengeThreatStage.PromotionPressure => 1.02f,
                _ => 0.94f
            };

            if (string.Equals(badgeLabel, "챌린지"))
            {
                return baseFrequency * 0.94f;
            }

            if (string.Equals(badgeLabel, "도전 페이스"))
            {
                return baseFrequency * 1.08f;
            }

            return baseFrequency;
        }

        public static float ResolveFeedbackPulseAmplitudeScale(string badgeLabel, ChallengeThreatStage stage)
        {
            float baseAmplitude = stage switch
            {
                ChallengeThreatStage.ElitePressure => 1.28f,
                ChallengeThreatStage.EliteReinforcement => 1.16f,
                ChallengeThreatStage.PromotionPressure => 1.06f,
                _ => 0.94f
            };

            if (string.Equals(badgeLabel, "챌린지"))
            {
                return baseAmplitude * 0.92f;
            }

            if (string.Equals(badgeLabel, "도전 페이스"))
            {
                return baseAmplitude * 1.04f;
            }

            return baseAmplitude;
        }

        public static float ResolveNodePulseScale(ChallengeThreatStage stage)
        {
            return stage switch
            {
                ChallengeThreatStage.ElitePressure => 1.3f,
                ChallengeThreatStage.EliteReinforcement => 1.16f,
                ChallengeThreatStage.PromotionPressure => 1.08f,
                _ => 1f
            };
        }

        public static float ResolveWarningFlashOpacity(ChallengeThreatStage stage)
        {
            return stage switch
            {
                ChallengeThreatStage.ElitePressure => 0.22f,
                ChallengeThreatStage.EliteReinforcement => 0.16f,
                ChallengeThreatStage.PromotionPressure => 0.11f,
                _ => 0.07f
            };
        }

        public static float ResolveWarningFlashOpacity(string badgeLabel, ChallengeThreatStage stage)
        {
            float baseOpacity = ResolveWarningFlashOpacity(stage);
            if (string.Equals(badgeLabel, "챌린지"))
            {
                return baseOpacity * 0.82f;
            }

            if (string.Equals(badgeLabel, "도전 페이스"))
            {
                return baseOpacity * 0.9f;
            }

            return baseOpacity;
        }

        public static float ResolveWarningFlashDuration(ChallengeThreatStage stage)
        {
            return stage switch
            {
                ChallengeThreatStage.ElitePressure => 0.44f,
                ChallengeThreatStage.EliteReinforcement => 0.38f,
                ChallengeThreatStage.PromotionPressure => 0.32f,
                _ => 0.26f
            };
        }

        public static int ResolveWarningFlashPulseCount(ChallengeThreatStage stage)
        {
            return stage switch
            {
                ChallengeThreatStage.ElitePressure => 3,
                ChallengeThreatStage.EliteReinforcement => 2,
                ChallengeThreatStage.PromotionPressure => 2,
                _ => 1
            };
        }

        public static float ResolveWarningFlashPulseStrength(ChallengeThreatStage stage)
        {
            return stage switch
            {
                ChallengeThreatStage.ElitePressure => 0.62f,
                ChallengeThreatStage.EliteReinforcement => 0.46f,
                ChallengeThreatStage.PromotionPressure => 0.3f,
                _ => 0.18f
            };
        }

        public static float ResolveWarningFlashPulseStrength(string badgeLabel, ChallengeThreatStage stage)
        {
            float baseStrength = ResolveWarningFlashPulseStrength(stage);
            if (string.Equals(badgeLabel, "챌린지"))
            {
                return baseStrength * 0.84f;
            }

            if (string.Equals(badgeLabel, "도전 페이스"))
            {
                return baseStrength * 0.92f;
            }

            return baseStrength;
        }

        public static float ResolveWarningFlashFrequencyScale(ChallengeThreatStage stage)
        {
            return stage switch
            {
                ChallengeThreatStage.ElitePressure => 1.2f,
                ChallengeThreatStage.EliteReinforcement => 1.08f,
                ChallengeThreatStage.PromotionPressure => 0.96f,
                _ => 0.88f
            };
        }

        public static float ResolveWarningFlashFrequencyScale(string badgeLabel, ChallengeThreatStage stage)
        {
            float baseFrequency = ResolveWarningFlashFrequencyScale(stage);
            if (string.Equals(badgeLabel, "챌린지"))
            {
                return baseFrequency * 0.94f;
            }

            if (string.Equals(badgeLabel, "도전 페이스"))
            {
                return baseFrequency * 1.04f;
            }

            return baseFrequency;
        }

        public static float ResolveWarningFlashDecaySoftness(ChallengeThreatStage stage)
        {
            return stage switch
            {
                ChallengeThreatStage.ElitePressure => 0.22f,
                ChallengeThreatStage.EliteReinforcement => 0.3f,
                ChallengeThreatStage.PromotionPressure => 0.42f,
                _ => 0.56f
            };
        }

        public static float ResolveWarningFlashDecaySoftness(string badgeLabel, ChallengeThreatStage stage)
        {
            float baseSoftness = ResolveWarningFlashDecaySoftness(stage);
            if (string.Equals(badgeLabel, "챌린지"))
            {
                return Mathf.Clamp01(baseSoftness + 0.12f);
            }

            if (string.Equals(badgeLabel, "도전 페이스"))
            {
                return Mathf.Clamp01(baseSoftness + 0.06f);
            }

            return baseSoftness;
        }

        public static Color ResolveWarningFlashColor(string badgeLabel, Color accentColor)
        {
            if (string.Equals(badgeLabel, "챌린지"))
            {
                Color baseColor = new(1f, 0.76f, 0.3f, 1f);
                return Color.Lerp(baseColor, accentColor, 0.18f);
            }

            if (string.Equals(badgeLabel, "도전 페이스"))
            {
                Color baseColor = new(1f, 0.84f, 0.36f, 1f);
                return Color.Lerp(baseColor, accentColor, 0.24f);
            }

            if (string.Equals(badgeLabel, "엘리트 경보"))
            {
                Color baseColor = new(1f, 0.46f, 0.18f, 1f);
                return Color.Lerp(baseColor, accentColor, 0.42f);
            }

            return accentColor;
        }

        public static GameAudioEventType ResolveWarningAudioEventType(ChallengeThreatStage stage)
        {
            return stage switch
            {
                ChallengeThreatStage.ElitePressure => GameAudioEventType.BossAppeared,
                ChallengeThreatStage.EliteReinforcement => GameAudioEventType.BossAppeared,
                ChallengeThreatStage.PromotionPressure => GameAudioEventType.RewardSpawned,
                _ => GameAudioEventType.RewardSpawned
            };
        }

        public static GameAudioEventType ResolveWarningAudioEventType(string badgeLabel, ChallengeThreatStage stage)
        {
            if (string.Equals(badgeLabel, "도전 페이스"))
            {
                return GameAudioEventType.RewardSpawned;
            }

            if (string.Equals(badgeLabel, "챌린지"))
            {
                return stage >= ChallengeThreatStage.PromotionPressure
                    ? GameAudioEventType.RewardSpawned
                    : GameAudioEventType.ItemCollected;
            }

            return ResolveWarningAudioEventType(stage);
        }

        public static float ResolveWarningAudioVolumeScale(ChallengeThreatStage stage)
        {
            return stage switch
            {
                ChallengeThreatStage.ElitePressure => 0.9f,
                ChallengeThreatStage.EliteReinforcement => 0.72f,
                ChallengeThreatStage.PromotionPressure => 0.58f,
                _ => 0.44f
            };
        }

        public static float ResolveWarningAudioVolumeScale(string badgeLabel, ChallengeThreatStage stage)
        {
            float baseVolume = ResolveWarningAudioVolumeScale(stage);
            if (string.Equals(badgeLabel, "챌린지"))
            {
                return baseVolume * 0.82f;
            }

            if (string.Equals(badgeLabel, "도전 페이스"))
            {
                return baseVolume * 0.9f;
            }

            return baseVolume;
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
            float basePitch = ResolveWarningAudioPitchScale(stage);
            if (string.Equals(badgeLabel, "챌린지"))
            {
                return basePitch * 1.04f;
            }

            if (string.Equals(badgeLabel, "도전 페이스"))
            {
                return basePitch * 1.08f;
            }

            return basePitch;
        }

        public static ChallengeRoomStatusPresentation BuildFallbackRoomStatusPresentation(
            RoomState roomState,
            bool hasRewardContent)
        {
            string detail = ResolveSharedChallengeFallbackDetailCopy(roomState, hasRewardContent);
            switch (roomState)
            {
                case RoomState.Combat:
                    return new ChallengeRoomStatusPresentation(
                        "챌린지",
                        "\uB3C4\uC804 \uC804\uD22C \uC9C4\uD589 \uC911",
                        detail,
                        new Color(1f, 0.58f, 0.24f, 1f),
                        "梨뚮┛吏");
                case RoomState.Rewarded when hasRewardContent:
                    return new ChallengeRoomStatusPresentation(
                        "챌린지",
                        "\uB3C4\uC804\uBC29 \uD074\uB9AC\uC5B4",
                        detail,
                        new Color(1f, 0.82f, 0.3f, 1f),
                        "梨뚮┛吏");
                default:
                    return new ChallengeRoomStatusPresentation(
                        "챌린지",
                        "\uB3C4\uC804\uBC29",
                        detail,
                        new Color(1f, 0.72f, 0.34f, 1f),
                        "梨뚮┛吏");
            }
        }

        public static void BuildFallbackRoomStatus(
            RoomState roomState,
            bool hasRewardContent,
            out string headline,
            out string detail,
            out Color accentColor)
        {
            ChallengeRoomStatusPresentation presentation = BuildFallbackRoomStatusPresentation(roomState, hasRewardContent);
            headline = presentation.Headline;
            detail = presentation.Detail;
            accentColor = presentation.AccentColor;
        }

        public static ChallengeRoomStatusPresentation BuildProgressStatusPresentation(
            ChallengeClearRank liveRank,
            float elapsedSeconds,
            ChallengeRewardSettings challengeRewardSettings)
        {
            string detail = ResolveSharedPaceDetailCopy(liveRank, challengeRewardSettings, elapsedSeconds);
            switch (liveRank)
            {
                case ChallengeClearRank.S:
                    return new ChallengeRoomStatusPresentation(
                        "도전 페이스",
                        "\uB3C4\uC804 S \uD398\uC774\uC2A4",
                        detail,
                        new Color(1f, 0.8f, 0.28f, 1f),
                        "理쒖긽 援ш컙");
                case ChallengeClearRank.A:
                    return new ChallengeRoomStatusPresentation(
                        "도전 페이스",
                        "\uB3C4\uC804 A \uD398\uC774\uC2A4",
                        detail,
                        new Color(1f, 0.58f, 0.24f, 1f),
                        "회복 구간");
                default:
                    return new ChallengeRoomStatusPresentation(
                        "도전 페이스",
                        "\uB3C4\uC804 B \uD398\uC774\uC2A4",
                        detail,
                        new Color(0.92f, 0.42f, 0.2f, 1f),
                        "위험 구간");
            }
        }

        public static void BuildProgressStatus(
            ChallengeClearRank liveRank,
            float elapsedSeconds,
            ChallengeRewardSettings challengeRewardSettings,
            out string headline,
            out string detail,
            out Color accentColor)
        {
            ChallengeRoomStatusPresentation presentation = BuildProgressStatusPresentation(liveRank, elapsedSeconds, challengeRewardSettings);
            headline = presentation.Headline;
            detail = presentation.Detail;
            accentColor = presentation.AccentColor;
        }

        public static ChallengeRoomStatusPresentation BuildCombatStatusPresentation(
            float elapsedSeconds,
            bool usePaceBadge = false)
        {
            string detail = ResolveSharedChallengeCombatDetailCopy(elapsedSeconds);
            return new ChallengeRoomStatusPresentation(
                usePaceBadge ? "도전 페이스" : "챌린지",
                "\uB3C4\uC804 \uC804\uD22C",
                detail,
                new Color(1f, 0.58f, 0.24f, 1f),
                usePaceBadge ? "회복 구간" : "梨뚮┛吏");
        }

        public static void BuildCombatStatus(
            float elapsedSeconds,
            out string headline,
            out string detail,
            out Color accentColor)
        {
            ChallengeRoomStatusPresentation presentation = BuildCombatStatusPresentation(elapsedSeconds);
            headline = presentation.Headline;
            detail = presentation.Detail;
            accentColor = presentation.AccentColor;
        }

        public static ChallengeRoomStatusPresentation BuildClearStatusPresentation(
            ChallengeClearRank clearRank,
            bool usePaceBadge = false)
        {
            string headline = clearRank switch
            {
                ChallengeClearRank.S => "\uB3C4\uC804\uBC29 \uD074\uB9AC\uC5B4 \u00b7 S",
                ChallengeClearRank.A => "\uB3C4\uC804\uBC29 \uD074\uB9AC\uC5B4 \u00b7 A",
                ChallengeClearRank.B => "\uB3C4\uC804\uBC29 \uD074\uB9AC\uC5B4 \u00b7 B",
                _ => "\uB3C4\uC804\uBC29 \uD074\uB9AC\uC5B4"
            };

            string detail = ResolveSharedChallengeClearDetailCopy(clearRank);

            Color accentColor = clearRank == ChallengeClearRank.S
                ? new Color(1f, 0.84f, 0.34f, 1f)
                : new Color(1f, 0.72f, 0.34f, 1f);
            return new ChallengeRoomStatusPresentation(
                usePaceBadge ? "도전 페이스" : "챌린지",
                headline,
                detail,
                accentColor,
                usePaceBadge
                    ? clearRank == ChallengeClearRank.S ? "理쒖긽 援ш컙" : clearRank == ChallengeClearRank.A ? "회복 구간" : "위험 구간"
                    : "梨뚮┛吏");
        }

        public static void BuildClearStatus(
            ChallengeClearRank clearRank,
            out string headline,
            out string detail,
            out Color accentColor)
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
                return currentRank switch
                {
                    ChallengeClearRank.S => "\uB3C4\uC804 \uD398\uC774\uC2A4 \uC0C1\uC2B9",
                    ChallengeClearRank.A => "\uB3C4\uC804 \uD398\uC774\uC2A4 \uD68C\uBCF5",
                    _ => "\uB3C4\uC804 \uC804\uD22C \uAC31\uC2E0"
                };
            }

            return currentRank switch
            {
                ChallengeClearRank.A => "\uB3C4\uC804 \uD398\uC774\uC2A4 \uD558\uB77D",
                ChallengeClearRank.B => "\uB3C4\uC804 \uD398\uC774\uC2A4 \uACBD\uACE0",
                _ => "\uB3C4\uC804 \uC804\uD22C \uACBD\uACE0"
            };
        }

        public static string ResolvePaceBannerSubtitle(
            ChallengeClearRank currentRank,
            ChallengeRewardSettings challengeRewardSettings,
            float elapsedSeconds)
        {
            return ResolveSharedPaceDetailCopy(currentRank, challengeRewardSettings, elapsedSeconds);
        }

        private static string ResolveSharedPaceDetailCopy(
            ChallengeClearRank currentRank,
            ChallengeRewardSettings challengeRewardSettings,
            float elapsedSeconds)
        {
            return currentRank switch
            {
                ChallengeClearRank.S => $"S \uBCF4\uC0C1 \uAD6C\uAC04 \uC720\uC9C0 \u00b7 \uC804\uD22C \uC2DC\uAC04 {elapsedSeconds:0.0}s",
                ChallengeClearRank.A => $"S \uBAA9\uD45C \uCD08\uACFC \u00b7 A \uBAA9\uD45C {challengeRewardSettings.ARankTimeSeconds:0}s\uAE4C\uC9C0 \uCD94\uAC00 \uBCF4\uC0C1",
                _ => $"A \uBAA9\uD45C \uCD08\uACFC \u00b7 \uC804\uD22C \uC2DC\uAC04 {elapsedSeconds:0.0}s / \uAE30\uBCF8 \uBCF4\uC0C1 \uAD6C\uAC04"
            };
        }

        private static string ResolveSharedChallengeFallbackDetailCopy(RoomState roomState, bool hasRewardContent)
        {
            return roomState switch
            {
                RoomState.Combat => "\uACC4\uC18D \uC774\uB3D9\uD558\uBA70 \uBE48\uD2C8 \uACBD\uB85C\uB97C \uB9CC\uB4DC\uC138\uC694.",
                RoomState.Rewarded when hasRewardContent => "\uBC29 \uC548\uC5D0 \uBCF4\uC0C1\uC774 \uC5F4\uB824 \uC788\uC2B5\uB2C8\uB2E4.",
                _ => "\uC804\uD22C\uAC00 \uC2DC\uC791\uB418\uBA74 \uAC15\uD55C \uC555\uBC15\uC774 \uB4E4\uC5B4\uC635\uB2C8\uB2E4."
            };
        }

        private static string ResolveSharedChallengeCombatDetailCopy(float elapsedSeconds)
        {
            return $"\uC804\uD22C {elapsedSeconds:0.0}s \u00b7 \uACC4\uC18D \uBC84\uD2F0\uC138\uC694";
        }

        private static string ResolveSharedChallengeClearDetailCopy(ChallengeClearRank clearRank)
        {
            return clearRank switch
            {
                ChallengeClearRank.S => "\uCD5C\uC0C1\uC704 \uBCF4\uC0C1\uC744 \uD655\uBCF4\uD588\uC2B5\uB2C8\uB2E4",
                ChallengeClearRank.A => "\uCD94\uAC00 \uBCF4\uC0C1\uC744 \uD655\uBCF4\uD588\uC2B5\uB2C8\uB2E4",
                _ => "\uBCF4\uC0C1\uC774 \uC5F4\uB824 \uC788\uC2B5\uB2C8\uB2E4"
            };
        }

        public static Color ResolvePaceBannerAccent(ChallengeClearRank currentRank)
        {
            return currentRank switch
            {
                ChallengeClearRank.S => new Color(1f, 0.8f, 0.28f, 1f),
                ChallengeClearRank.A => new Color(1f, 0.58f, 0.24f, 1f),
                _ => new Color(0.92f, 0.42f, 0.2f, 1f)
            };
        }

        public static float ResolvePaceBannerDuration(ChallengeClearRank currentRank)
        {
            return currentRank switch
            {
                ChallengeClearRank.S => 1.65f,
                ChallengeClearRank.A => 1.55f,
                _ => 1.5f
            };
        }

        public static string ResolvePaceBannerEyebrow(ChallengeClearRank previousRank, ChallengeClearRank currentRank)
        {
            if (currentRank > previousRank)
            {
                return currentRank switch
                {
                    ChallengeClearRank.S => "최상 구간",
                    ChallengeClearRank.A => "회복 구간",
                    _ => "회복 구간"
                };
            }

            return currentRank switch
            {
                ChallengeClearRank.A => "속도 경고",
                ChallengeClearRank.B => "위험 구간",
                _ => "위험 구간"
            };
        }

        public static ChallengeThreatStage ResolvePaceBannerStage(ChallengeClearRank previousRank, ChallengeClearRank currentRank)
        {
            return currentRank > previousRank
                ? ChallengeThreatStage.Baseline
                : ChallengeThreatStage.PromotionPressure;
        }

        public static string ResolvePaceFloatingLabel(ChallengeClearRank previousRank, ChallengeClearRank currentRank)
        {
            if (currentRank > previousRank)
            {
                return currentRank switch
                {
                    ChallengeClearRank.S => "최상 유지",
                    ChallengeClearRank.A => "페이스 회복",
                    _ => "전열 회복"
                };
            }

            return currentRank switch
            {
                ChallengeClearRank.A => "속도 저하",
                ChallengeClearRank.B => "위험 구간",
                _ => "전투 경고"
            };
        }

        public static ChallengePaceBannerPresentation BuildPaceBannerPresentation(
            ChallengeClearRank previousRank,
            ChallengeClearRank currentRank,
            ChallengeRewardSettings challengeRewardSettings,
            float elapsedSeconds)
        {
            return new ChallengePaceBannerPresentation(
                ResolvePaceBannerTitle(previousRank, currentRank),
                ResolvePaceBannerSubtitle(currentRank, challengeRewardSettings, elapsedSeconds),
                "도전 페이스",
                ResolvePaceBannerEyebrow(previousRank, currentRank),
                ResolvePaceFloatingLabel(previousRank, currentRank),
                ResolvePaceBannerStage(previousRank, currentRank),
                ResolvePaceBannerAccent(currentRank),
                ResolvePaceBannerDuration(currentRank));
        }

        public static ChallengeThreatPresentation Build(
            int currentWave,
            int totalWaves,
            int enemyCount,
            int guaranteedChampionCount,
            float championChanceBonus)
        {
            string waveSegment = totalWaves > 1
                ? $"웨이브 {currentWave}/{totalWaves}"
                : $"웨이브 {currentWave}";

            if (guaranteedChampionCount >= 2)
            {
                return new ChallengeThreatPresentation(
                    "엘리트 경보",
                    "엘리트 압박 경보",
                    "엘리트 경보",
                    $"W{currentWave} 엘리트 압박",
                    "확정 위협",
                    ResolveSharedThreatDetailCopy(
                        ChallengeThreatStage.ElitePressure,
                        waveSegment,
                        enemyCount,
                        guaranteedChampionCount,
                        championChanceBonus),
                    $"엘{guaranteedChampionCount}+",
                    $"엘리트 {guaranteedChampionCount}+",
                    ChallengeThreatStage.ElitePressure,
                    new Color(1f, 0.38f, 0.2f, 1f),
                    2.1f);
            }

            if (guaranteedChampionCount >= 1)
            {
                return new ChallengeThreatPresentation(
                    "엘리트 경보",
                    "엘리트 증원 경보",
                    "엘리트 경보",
                    $"W{currentWave} 엘리트 증원",
                    "증원 경보",
                    ResolveSharedThreatDetailCopy(
                        ChallengeThreatStage.EliteReinforcement,
                        waveSegment,
                        enemyCount,
                        guaranteedChampionCount,
                        championChanceBonus),
                    $"엘{guaranteedChampionCount}+",
                    $"엘리트 {guaranteedChampionCount}+",
                    ChallengeThreatStage.EliteReinforcement,
                    new Color(1f, 0.54f, 0.24f, 1f),
                    1.95f);
            }

            if (championChanceBonus >= 0.18f)
            {
                string bonusLabel = $"+{championChanceBonus * 100f:0}%";
                return new ChallengeThreatPresentation(
                    "엘리트 경보",
                    "승격 압박 경보",
                    "승격 경보",
                    $"W{currentWave} 승격 압박",
                    "압박 경보",
                    ResolveSharedThreatDetailCopy(
                        ChallengeThreatStage.PromotionPressure,
                        waveSegment,
                        enemyCount,
                        guaranteedChampionCount,
                        championChanceBonus),
                    bonusLabel,
                    $"승격 {bonusLabel}",
                    ChallengeThreatStage.PromotionPressure,
                    new Color(1f, 0.48f, 0.18f, 1f),
                    1.8f);
            }

            if (championChanceBonus > 0f)
            {
                string bonusLabel = $"+{championChanceBonus * 100f:0}%";
                return new ChallengeThreatPresentation(
                    "챌린지",
                    $"도전 웨이브 {currentWave}",
                    "도전 현황",
                    $"W{currentWave} {bonusLabel}",
                    "승격 예고",
                    ResolveSharedThreatDetailCopy(
                        ChallengeThreatStage.Baseline,
                        waveSegment,
                        enemyCount,
                        guaranteedChampionCount,
                        championChanceBonus),
                    bonusLabel,
                    $"승격 {bonusLabel}",
                    ChallengeThreatStage.Baseline,
                    new Color(0.94f, 0.38f, 0.18f, 1f),
                    1.65f);
            }

            return new ChallengeThreatPresentation(
                "챌린지",
                $"도전 웨이브 {currentWave}",
                "도전 현황",
                $"W{currentWave} 적{enemyCount}",
                "웨이브 예고",
                ResolveSharedThreatDetailCopy(
                    ChallengeThreatStage.Baseline,
                    waveSegment,
                    enemyCount,
                    guaranteedChampionCount,
                    championChanceBonus),
                $"적{enemyCount}",
                "추가 증원",
                ChallengeThreatStage.Baseline,
                new Color(0.94f, 0.38f, 0.18f, 1f),
                1.65f);
        }

        public static ChallengeWaveIntermissionPresentation BuildWaveIntermission(
            int clearedWave,
            int totalWaves,
            int nextEnemyCount,
            int nextGuaranteedChampionCount,
            float nextChampionChanceBonus)
        {
            string floatingLabel;
            Color accentColor;
            float duration;

            if (nextGuaranteedChampionCount >= 2)
            {
                floatingLabel = $"W{clearedWave} 확보 · 엘리트 경계";
                accentColor = new Color(1f, 0.78f, 0.38f, 1f);
                duration = 0.98f;
            }
            else if (nextGuaranteedChampionCount >= 1 || nextChampionChanceBonus >= 0.18f)
            {
                floatingLabel = $"W{clearedWave} 확보 · 압박 상승";
                accentColor = new Color(1f, 0.84f, 0.42f, 1f);
                duration = 0.92f;
            }
            else if (clearedWave < totalWaves)
            {
                floatingLabel = $"W{clearedWave} 확보 · 다음 {nextEnemyCount}";
                accentColor = new Color(0.72f, 1f, 0.82f, 1f);
                duration = 0.86f;
            }
            else
            {
                floatingLabel = $"W{clearedWave} 확보";
                accentColor = new Color(0.72f, 1f, 0.82f, 1f);
                duration = 0.82f;
            }

            return new ChallengeWaveIntermissionPresentation(
                floatingLabel,
                accentColor,
                duration,
                GameAudioEventType.ItemCollected,
                0.34f,
                1.08f);
        }

        private static string ResolveSharedThreatDetailCopy(
            ChallengeThreatStage stage,
            string waveSegment,
            int enemyCount,
            int guaranteedChampionCount,
            float championChanceBonus)
        {
            return stage switch
            {
                ChallengeThreatStage.ElitePressure => $"{waveSegment} · 적 {enemyCount} · 엘리트 {Mathf.Max(2, guaranteedChampionCount)}+ 확정",
                ChallengeThreatStage.EliteReinforcement => $"{waveSegment} · 적 {enemyCount} · 엘리트 {Mathf.Max(1, guaranteedChampionCount)}+ 보장",
                ChallengeThreatStage.PromotionPressure => $"{waveSegment} · 적 {enemyCount} · 승격 +{championChanceBonus * 100f:0}% · 압박 높음",
                _ when championChanceBonus > 0f => $"{waveSegment} · 적 {enemyCount} · 승격 +{championChanceBonus * 100f:0}%",
                _ => $"{waveSegment} · 적 {enemyCount} · 압박 증가"
            };
        }
    }
}
