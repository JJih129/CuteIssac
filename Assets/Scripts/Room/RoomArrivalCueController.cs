using System.Collections.Generic;
using CuteIssac.Core.Feedback;
using CuteIssac.Data.Dungeon;
using CuteIssac.Dungeon;
using UnityEngine;

namespace CuteIssac.Room
{
    /// <summary>
    /// Orchestrates room-type-specific arrival cues so traversal and room identity stay decoupled from authored VFX.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RoomArrivalCueController : MonoBehaviour
    {
        [SerializeField] private RoomNavigationController roomNavigationController;
        [SerializeField] [Min(0f)] private float cueRepeatCooldown = 0.08f;

        private readonly HashSet<int> _announcedRoomIds = new();
        private float _lastCueTimestamp = float.NegativeInfinity;

        public void ConfigureRuntime(RoomNavigationController navigation)
        {
            roomNavigationController = navigation;
        }

        public void PresentArrivalCue(RoomController room, RoomDirection arrivalDirection, bool guidedRoute, bool carriedPlan = false, string carryLabel = "")
        {
            if (room == null || Time.unscaledTime - _lastCueTimestamp < cueRepeatCooldown)
            {
                return;
            }

            CueProfile profile = BuildCueProfile(room.RoomType, guidedRoute, carriedPlan);
            if (!profile.ShowWorldCue)
            {
                return;
            }

            RoomArrivalCuePresentation presentation = EnsurePresentation(room);
            presentation?.PlayCue(room.RoomType, profile.AccentColor, arrivalDirection, guidedRoute, profile.WorldCueIntensity);

            bool firstAnnouncement = _announcedRoomIds.Add(room.GetInstanceID());
            if (firstAnnouncement && profile.ShowBanner)
            {
                GameplayFeedbackEvents.RaiseBannerFeedback(new BannerFeedbackRequest(
                    profile.Title,
                    profile.Subtitle,
                    profile.AccentColor,
                    profile.BannerDuration));
            }

            if (firstAnnouncement && profile.FlashOpacity > 0f)
            {
                GameplayFeedbackEvents.RaiseThreatFlash(new ThreatFlashRequest(
                    profile.AccentColor,
                    profile.FlashOpacity,
                    profile.FlashDuration,
                    profile.FlashPulseCount,
                    profile.FlashPulseStrength,
                    profile.FlashPulseFrequencyScale,
                    0.42f));
            }

            if (carriedPlan && !string.IsNullOrWhiteSpace(carryLabel))
            {
                GameplayFeedbackEvents.RaiseFloatingFeedback(new FloatingFeedbackRequest(
                    room.CameraFocusPosition + Vector3.up * 0.54f,
                    carryLabel,
                    Color.Lerp(profile.AccentColor, Color.white, 0.26f),
                    0.62f,
                    0.4f,
                    0.96f,
                    visualProfile: FloatingFeedbackVisualProfile.Momentum));
            }

            _lastCueTimestamp = Time.unscaledTime;
        }

        private static RoomArrivalCuePresentation EnsurePresentation(RoomController room)
        {
            if (room == null)
            {
                return null;
            }

            RoomArrivalCuePresentation presentation = room.GetComponent<RoomArrivalCuePresentation>();
            if (presentation == null)
            {
                presentation = room.gameObject.AddComponent<RoomArrivalCuePresentation>();
            }

            return presentation;
        }

        private static CueProfile BuildCueProfile(RoomType roomType, bool guidedRoute, bool carriedPlan)
        {
            float guidedIntensityBonus = guidedRoute ? 0.12f : 0f;
            float planCarryBonus = carriedPlan ? 0.1f : 0f;
            return roomType switch
            {
                RoomType.Treasure => new CueProfile(
                    ResolveArrivalAccent(roomType),
                    "TREASURE ROOM",
                    "Reward cache online",
                    true,
                    true,
                    1.78f,
                    0.14f,
                    0.34f,
                    1,
                    0.14f,
                    1f,
                    1.06f + guidedIntensityBonus + planCarryBonus),
                RoomType.Shop => new CueProfile(
                    ResolveArrivalAccent(roomType),
                    "SHOP ROOM",
                    "Trader stock is live",
                    true,
                    true,
                    1.72f,
                    0.1f,
                    0.3f,
                    1,
                    0.12f,
                    1f,
                    1.02f + guidedIntensityBonus + planCarryBonus),
                RoomType.Boss => new CueProfile(
                    ResolveArrivalAccent(roomType),
                    "BOSS CHAMBER",
                    "Arena pressure expected",
                    true,
                    true,
                    2f,
                    0.32f,
                    0.62f,
                    2,
                    0.26f,
                    1.06f,
                    1.22f + guidedIntensityBonus + planCarryBonus),
                RoomType.Secret => new CueProfile(
                    ResolveArrivalAccent(roomType),
                    "SECRET ROOM",
                    "Hidden route confirmed",
                    true,
                    true,
                    1.76f,
                    0.18f,
                    0.4f,
                    2,
                    0.18f,
                    1.02f,
                    1.14f + guidedIntensityBonus + planCarryBonus),
                RoomType.Challenge => new CueProfile(
                    ResolveArrivalAccent(roomType),
                    "CHALLENGE ROOM",
                    "Paced pressure ahead",
                    true,
                    true,
                    1.9f,
                    0.24f,
                    0.48f,
                    2,
                    0.22f,
                    1.04f,
                    1.16f + guidedIntensityBonus + planCarryBonus),
                RoomType.MiniBoss => new CueProfile(
                    ResolveArrivalAccent(roomType),
                    "ELITE DEN",
                    "Champion pressure ahead",
                    true,
                    true,
                    1.86f,
                    0.26f,
                    0.52f,
                    2,
                    0.22f,
                    1.04f,
                    1.16f + guidedIntensityBonus + planCarryBonus),
                RoomType.Trap => new CueProfile(
                    ResolveArrivalAccent(roomType),
                    "TRAP ROOM",
                    "Hazard lane armed",
                    true,
                    true,
                    1.68f,
                    0.18f,
                    0.38f,
                    2,
                    0.18f,
                    1f,
                    1.08f + guidedIntensityBonus + planCarryBonus),
                RoomType.Curse => new CueProfile(
                    ResolveArrivalAccent(roomType),
                    "CURSE ROOM",
                    "Blood price may apply",
                    true,
                    true,
                    1.82f,
                    0.22f,
                    0.44f,
                    2,
                    0.2f,
                    1.02f,
                    1.12f + guidedIntensityBonus + planCarryBonus),
                RoomType.Start => new CueProfile(
                    ResolveArrivalAccent(roomType),
                    "START ROOM",
                    "Route is open",
                    false,
                    true,
                    1.4f,
                    0f,
                    0f,
                    1,
                    0f,
                    1f,
                    0.92f + planCarryBonus),
                _ => new CueProfile(
                    ResolveArrivalAccent(roomType),
                    "NEXT SECTOR",
                    "Push deeper",
                    false,
                    guidedRoute,
                    1.3f,
                    0f,
                    0f,
                    1,
                    0f,
                    1f,
                    (guidedRoute ? 0.94f : 0.84f) + planCarryBonus)
            };
        }

        private static Color ResolveArrivalAccent(RoomType roomType)
        {
            return roomType switch
            {
                RoomType.Start => new Color(0.72f, 0.94f, 1f, 1f),
                RoomType.Treasure => new Color(1f, 0.84f, 0.28f, 1f),
                RoomType.Shop => new Color(0.36f, 0.95f, 0.84f, 1f),
                RoomType.Boss => new Color(1f, 0.4f, 0.34f, 1f),
                RoomType.Secret => new Color(0.84f, 0.58f, 1f, 1f),
                RoomType.Challenge => new Color(1f, 0.64f, 0.24f, 1f),
                RoomType.MiniBoss => new Color(0.96f, 0.52f, 0.82f, 1f),
                RoomType.Trap => new Color(1f, 0.48f, 0.34f, 1f),
                RoomType.Curse => new Color(0.86f, 0.36f, 0.68f, 1f),
                _ => new Color(0.66f, 0.92f, 1f, 1f)
            };
        }

        private readonly struct CueProfile
        {
            public CueProfile(
                Color accentColor,
                string title,
                string subtitle,
                bool showBanner,
                bool showWorldCue,
                float bannerDuration,
                float flashOpacity,
                float flashDuration,
                int flashPulseCount,
                float flashPulseStrength,
                float flashPulseFrequencyScale,
                float worldCueIntensity)
            {
                AccentColor = accentColor;
                Title = title;
                Subtitle = subtitle;
                ShowBanner = showBanner;
                ShowWorldCue = showWorldCue;
                BannerDuration = bannerDuration;
                FlashOpacity = flashOpacity;
                FlashDuration = flashDuration;
                FlashPulseCount = flashPulseCount;
                FlashPulseStrength = flashPulseStrength;
                FlashPulseFrequencyScale = flashPulseFrequencyScale;
                WorldCueIntensity = worldCueIntensity;
            }

            public Color AccentColor { get; }
            public string Title { get; }
            public string Subtitle { get; }
            public bool ShowBanner { get; }
            public bool ShowWorldCue { get; }
            public float BannerDuration { get; }
            public float FlashOpacity { get; }
            public float FlashDuration { get; }
            public int FlashPulseCount { get; }
            public float FlashPulseStrength { get; }
            public float FlashPulseFrequencyScale { get; }
            public float WorldCueIntensity { get; }
        }
    }
}
