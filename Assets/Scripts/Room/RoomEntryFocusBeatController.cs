using System.Collections.Generic;
using CuteIssac.Core.Feedback;
using CuteIssac.Data.Dungeon;
using CuteIssac.Dungeon;
using UnityEngine;

namespace CuteIssac.Room
{
    /// <summary>
    /// Chooses a room-type-specific focus anchor right after traversal so the first actionable point reads immediately.
    /// The resolver stays data-agnostic and only asks existing room systems for anchors they already own.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RoomEntryFocusBeatController : MonoBehaviour
    {
        [SerializeField] private RoomNavigationController roomNavigationController;
        [SerializeField] [Min(0f)] private float beatRepeatCooldown = 0.08f;
        [SerializeField] [Min(0.1f)] private float focusLabelLifetime = 0.64f;
        [SerializeField] [Min(0f)] private float focusLabelRiseDistance = 0.46f;

        private readonly HashSet<int> _announcedRoomIds = new();
        private readonly List<Transform> _highlightTargetBuffer = new();
        private float _lastBeatTimestamp = float.NegativeInfinity;

        public void ConfigureRuntime(RoomNavigationController navigationController)
        {
            roomNavigationController = navigationController;
        }

        public void PresentEntryFocusBeat(RoomController room, RoomDirection arrivalDirection, bool guidedRoute, string routeReasonTag = "", bool carriedPlan = false, string carryLabel = "")
        {
            if (room == null || Time.unscaledTime - _lastBeatTimestamp < beatRepeatCooldown)
            {
                return;
            }

            FocusBeatProfile profile = BuildProfile(room.RoomType, guidedRoute, carriedPlan);
            if (!profile.Enabled)
            {
                return;
            }

            bool firstAnnouncement = _announcedRoomIds.Add(room.GetInstanceID());
            RoomInteractionAffordanceController affordanceController = EnsureAffordanceController(room);
            affordanceController?.ConfigureRuntime(room);

            Transform preferredHighlightTarget = null;
            string arrivalAssistCompareLabel = routeReasonTag;
            Color arrivalAssistCompareColor = profile.AccentColor;

            if (guidedRoute)
            {
                Transform hintedTarget = ResolvePreferredHighlightTarget(room, routeReasonTag);
                if (affordanceController != null
                    && affordanceController.TryResolveGuidedArrivalAssistTarget(
                        hintedTarget,
                        routeReasonTag,
                        out Transform resolvedTarget,
                        out string resolvedCompareLabel,
                        out Color resolvedCompareColor))
                {
                    preferredHighlightTarget = resolvedTarget;
                    arrivalAssistCompareLabel = resolvedCompareLabel;
                    arrivalAssistCompareColor = resolvedCompareColor;
                }
                else
                {
                    preferredHighlightTarget = hintedTarget;
                }
            }

            string focusAnnouncementLabel = ResolveFocusAnnouncementLabel(profile.FocusLabel, guidedRoute, arrivalAssistCompareLabel);
            Color focusAnnouncementColor = IsShotProfileCompareLabel(arrivalAssistCompareLabel)
                ? arrivalAssistCompareColor
                : profile.AccentColor;

            bool resolvedFocusTarget = TryResolveFocusTarget(room, out Vector3 focusPosition, out float focusRadius);
            if (!resolvedFocusTarget)
            {
                focusPosition = room.CameraFocusPosition;
                focusRadius = profile.DefaultRadius;
            }

            RoomEntryFocusBeatPresentation presentation = EnsurePresentation(room);
            presentation?.PlayBeat(
                room.RoomType,
                focusPosition,
                profile.AccentColor,
                arrivalDirection,
                guidedRoute,
                profile.WorldIntensity,
                Mathf.Max(profile.DefaultRadius, focusRadius));

            if (firstAnnouncement && !string.IsNullOrWhiteSpace(focusAnnouncementLabel))
            {
                GameplayFeedbackEvents.RaiseFloatingFeedback(new FloatingFeedbackRequest(
                    focusPosition + Vector3.up * 0.48f,
                    focusAnnouncementLabel,
                    Color.Lerp(focusAnnouncementColor, Color.white, 0.24f),
                    focusLabelLifetime + profile.LabelLifetimeBonus,
                    focusLabelRiseDistance + profile.LabelRiseBonus,
                    1.02f + profile.LabelScaleBonus,
                    visualProfile: FloatingFeedbackVisualProfile.Momentum));
            }

            if (carriedPlan && !string.IsNullOrWhiteSpace(carryLabel))
            {
                GameplayFeedbackEvents.RaiseFloatingFeedback(new FloatingFeedbackRequest(
                    focusPosition + Vector3.up * 0.72f,
                    carryLabel,
                    Color.Lerp(profile.AccentColor, Color.white, 0.24f),
                    0.62f + profile.LabelLifetimeBonus,
                    0.38f + profile.LabelRiseBonus,
                    0.94f + profile.LabelScaleBonus,
                    visualProfile: FloatingFeedbackVisualProfile.Momentum));
            }

            if (firstAnnouncement || guidedRoute)
            {
                PresentInteractionHighlights(room, profile, guidedRoute, preferredHighlightTarget);
            }

            if (guidedRoute && affordanceController != null && preferredHighlightTarget != null)
            {
                affordanceController.PrimeArrivalAssist(
                    preferredHighlightTarget,
                    arrivalAssistCompareLabel,
                    profile.AccentColor,
                    arrivalAssistCompareColor,
                    ResolveArrivalAssistDuration(profile.RoomType, guidedRoute),
                    routeReasonTag,
                    carriedPlan ? carryLabel : string.Empty);
            }

            _lastBeatTimestamp = Time.unscaledTime;
        }

        private static string ResolveFocusAnnouncementLabel(string fallbackLabel, bool guidedRoute, string compareLabel)
        {
            if (guidedRoute && IsShotProfileCompareLabel(compareLabel))
            {
                return compareLabel;
            }

            return fallbackLabel;
        }

        private static bool IsShotProfileCompareLabel(string compareLabel)
        {
            if (string.IsNullOrWhiteSpace(compareLabel))
            {
                return false;
            }

            return compareLabel == "SHOT RESET"
                || compareLabel == "BASELINE"
                || compareLabel.Contains("LASER")
                || compareLabel.Contains("ORBIT")
                || compareLabel.Contains("SHIELD")
                || compareLabel.Contains("SPLIT")
                || compareLabel.Contains("BLAST")
                || compareLabel.Contains("LEECH")
                || compareLabel.Contains("BOUNCE");
        }

        private static RoomEntryFocusBeatPresentation EnsurePresentation(RoomController room)
        {
            if (room == null)
            {
                return null;
            }

            RoomEntryFocusBeatPresentation presentation = room.GetComponent<RoomEntryFocusBeatPresentation>();
            if (presentation == null)
            {
                presentation = room.gameObject.AddComponent<RoomEntryFocusBeatPresentation>();
            }

            return presentation;
        }

        private static RoomInteractionAffordanceController EnsureAffordanceController(RoomController room)
        {
            if (room == null)
            {
                return null;
            }

            RoomInteractionAffordanceController controller = room.GetComponent<RoomInteractionAffordanceController>();
            if (controller == null)
            {
                controller = room.gameObject.AddComponent<RoomInteractionAffordanceController>();
            }

            return controller;
        }

        private static bool TryResolveFocusTarget(RoomController room, out Vector3 focusPosition, out float focusRadius)
        {
            if (room == null)
            {
                focusPosition = Vector3.zero;
                focusRadius = 1f;
                return false;
            }

            RoomType roomType = room.RoomType;
            RoomTypeContentController contentController = room.GetComponent<RoomTypeContentController>();
            TreasureRoomSpawner treasureRoomSpawner = room.GetComponent<TreasureRoomSpawner>();
            RoomThemeController roomThemeController = room.GetComponent<RoomThemeController>();

            switch (roomType)
            {
                case RoomType.Treasure:
                    if (treasureRoomSpawner != null && treasureRoomSpawner.TryResolveTreasureFocusTarget(out focusPosition, out focusRadius))
                    {
                        return true;
                    }

                    if (contentController != null && contentController.TryResolveContentFocusTarget(out focusPosition, out focusRadius))
                    {
                        return true;
                    }

                    break;
                case RoomType.Shop:
                case RoomType.Curse:
                    if (contentController != null && contentController.TryResolveContentFocusTarget(out focusPosition, out focusRadius))
                    {
                        return true;
                    }

                    break;
                case RoomType.Boss:
                case RoomType.MiniBoss:
                case RoomType.Challenge:
                    if (contentController != null && contentController.TryResolveCombatSetpieceFocusTarget(out focusPosition, out focusRadius))
                    {
                        return true;
                    }

                    break;
                case RoomType.Trap:
                    TrapRoomHazardContent trapRoomHazardContent = room.GetComponentInChildren<TrapRoomHazardContent>(true);
                    if (trapRoomHazardContent != null && trapRoomHazardContent.TryResolveHazardFocusTarget(out focusPosition, out focusRadius))
                    {
                        return true;
                    }

                    if (contentController != null && contentController.TryResolveContentFocusTarget(out focusPosition, out focusRadius))
                    {
                        return true;
                    }

                    break;
                case RoomType.Secret:
                    if (roomThemeController != null && roomThemeController.TryResolveLandmarkFocusTarget(out focusPosition, out focusRadius))
                    {
                        return true;
                    }

                    if (contentController != null && contentController.TryResolveContentFocusTarget(out focusPosition, out focusRadius))
                    {
                        return true;
                    }

                    break;
            }

            if (roomThemeController != null && roomThemeController.TryResolveLandmarkFocusTarget(out focusPosition, out focusRadius))
            {
                return true;
            }

            focusPosition = room.CameraFocusPosition;
            focusRadius = 1f;
            return false;
        }

        private void PresentInteractionHighlights(RoomController room, FocusBeatProfile profile, bool guidedRoute, Transform preferredHighlightTarget)
        {
            if (room == null)
            {
                return;
            }

            _highlightTargetBuffer.Clear();
            CollectHighlightTargets(room, _highlightTargetBuffer);

            if (_highlightTargetBuffer.Count == 0)
            {
                return;
            }

            float baseRadius = ResolveHighlightRadius(profile.RoomType, _highlightTargetBuffer.Count);
            bool hasPreferredTarget = preferredHighlightTarget != null && _highlightTargetBuffer.Contains(preferredHighlightTarget);

            for (int i = 0; i < _highlightTargetBuffer.Count; i++)
            {
                Transform target = _highlightTargetBuffer[i];

                if (target == null)
                {
                    continue;
                }

                RoomEntryObjectHighlightPresentation presentation = target.GetComponent<RoomEntryObjectHighlightPresentation>();
                if (presentation == null)
                {
                    presentation = target.gameObject.AddComponent<RoomEntryObjectHighlightPresentation>();
                }

                presentation.PlayHighlight(
                    profile.RoomType,
                    profile.AccentColor,
                    hasPreferredTarget ? target == preferredHighlightTarget : i == 0,
                    guidedRoute,
                    ResolveHighlightDuration(profile.RoomType, guidedRoute),
                    baseRadius + (i * 0.05f));
            }
        }

        private static Transform ResolvePreferredHighlightTarget(RoomController room, string routeReasonTag)
        {
            if (room == null)
            {
                return null;
            }

            RoomTypeContentController contentController = room.GetComponent<RoomTypeContentController>();
            if (contentController != null
                && contentController.TryResolvePreferredEntryHighlightTarget(routeReasonTag, out Transform preferredTarget))
            {
                return preferredTarget;
            }

            return null;
        }

        private static void CollectHighlightTargets(RoomController room, List<Transform> targetBuffer)
        {
            if (room == null || targetBuffer == null)
            {
                return;
            }

            targetBuffer.Clear();
            RoomType roomType = room.RoomType;
            RoomTypeContentController contentController = room.GetComponent<RoomTypeContentController>();
            TreasureRoomSpawner treasureRoomSpawner = room.GetComponent<TreasureRoomSpawner>();
            RoomThemeController roomThemeController = room.GetComponent<RoomThemeController>();

            switch (roomType)
            {
                case RoomType.Treasure:
                    treasureRoomSpawner?.CollectTreasureChoiceTargets(targetBuffer);
                    if (targetBuffer.Count == 0)
                    {
                        contentController?.CollectEntryHighlightTargets(targetBuffer);
                    }

                    break;
                case RoomType.Shop:
                case RoomType.Curse:
                    contentController?.CollectEntryHighlightTargets(targetBuffer);
                    break;
                case RoomType.Boss:
                case RoomType.MiniBoss:
                case RoomType.Challenge:
                    if (contentController != null && contentController.TryGetCombatSetpieceTransform(out Transform setpieceTransform) && setpieceTransform != null)
                    {
                        targetBuffer.Add(setpieceTransform);
                    }

                    break;
                case RoomType.Trap:
                    TrapRoomHazardContent trapRoomHazardContent = room.GetComponentInChildren<TrapRoomHazardContent>(true);
                    trapRoomHazardContent?.CollectHazardTargets(targetBuffer);
                    if (targetBuffer.Count == 0)
                    {
                        contentController?.CollectEntryHighlightTargets(targetBuffer);
                    }

                    break;
                case RoomType.Secret:
                    roomThemeController?.CollectLandmarkTargets(targetBuffer);
                    if (targetBuffer.Count == 0)
                    {
                        contentController?.CollectEntryHighlightTargets(targetBuffer);
                    }

                    break;
            }

            if (targetBuffer.Count == 0)
            {
                roomThemeController?.CollectLandmarkTargets(targetBuffer);
            }
        }

        private static FocusBeatProfile BuildProfile(RoomType roomType, bool guidedRoute, bool carriedPlan)
        {
            float guidedIntensityBonus = guidedRoute ? 0.08f : 0f;
            float planCarryBonus = carriedPlan ? 0.08f : 0f;
            return roomType switch
            {
                RoomType.Treasure => new FocusBeatProfile(
                    true,
                    roomType,
                    RoomTraversalGuidanceController.ResolveRoomAccent(roomType),
                    "CACHE HERE",
                    1.02f + guidedIntensityBonus + planCarryBonus,
                    1.2f,
                    0.08f,
                    0.04f,
                    0.06f),
                RoomType.Shop => new FocusBeatProfile(
                    true,
                    roomType,
                    RoomTraversalGuidanceController.ResolveRoomAccent(roomType),
                    "STOCK HERE",
                    0.98f + guidedIntensityBonus + planCarryBonus,
                    1.08f,
                    0.04f,
                    0f,
                    0.04f),
                RoomType.Boss => new FocusBeatProfile(
                    true,
                    roomType,
                    RoomTraversalGuidanceController.ResolveRoomAccent(roomType),
                    "ARENA CORE",
                    1.18f + guidedIntensityBonus + planCarryBonus,
                    1.42f,
                    0.12f,
                    0.06f,
                    0.08f),
                RoomType.Secret => new FocusBeatProfile(
                    true,
                    roomType,
                    RoomTraversalGuidanceController.ResolveRoomAccent(roomType),
                    "HIDDEN CACHE",
                    1.06f + guidedIntensityBonus + planCarryBonus,
                    1.14f,
                    0.08f,
                    0.04f,
                    0.06f),
                RoomType.Challenge => new FocusBeatProfile(
                    true,
                    roomType,
                    RoomTraversalGuidanceController.ResolveRoomAccent(roomType),
                    "PRESSURE CORE",
                    1.12f + guidedIntensityBonus + planCarryBonus,
                    1.26f,
                    0.08f,
                    0.04f,
                    0.06f),
                RoomType.MiniBoss => new FocusBeatProfile(
                    true,
                    roomType,
                    RoomTraversalGuidanceController.ResolveRoomAccent(roomType),
                    "ELITE CORE",
                    1.12f + guidedIntensityBonus + planCarryBonus,
                    1.28f,
                    0.08f,
                    0.04f,
                    0.06f),
                RoomType.Trap => new FocusBeatProfile(
                    true,
                    roomType,
                    RoomTraversalGuidanceController.ResolveRoomAccent(roomType),
                    "HAZARD LANE",
                    1f + guidedIntensityBonus + planCarryBonus,
                    1.2f,
                    0.04f,
                    0.02f,
                    0.04f),
                RoomType.Curse => new FocusBeatProfile(
                    true,
                    roomType,
                    RoomTraversalGuidanceController.ResolveRoomAccent(roomType),
                    "CURSED OFFER",
                    1.04f + guidedIntensityBonus + planCarryBonus,
                    1.18f,
                    0.06f,
                    0.04f,
                    0.06f),
                _ => new FocusBeatProfile(
                    false,
                    roomType,
                    RoomTraversalGuidanceController.ResolveRoomAccent(roomType),
                    string.Empty,
                    (guidedRoute ? 0.92f : 0.82f) + planCarryBonus,
                    1f,
                    0f,
                    0f,
                    0f)
            };
        }

        private static float ResolveHighlightDuration(RoomType roomType, bool guidedRoute)
        {
            float duration = roomType switch
            {
                RoomType.Boss => 1.48f,
                RoomType.MiniBoss => 1.34f,
                RoomType.Challenge => 1.3f,
                RoomType.Treasure => 1.2f,
                RoomType.Shop => 1.16f,
                RoomType.Secret => 1.24f,
                RoomType.Curse => 1.22f,
                RoomType.Trap => 1.12f,
                _ => 1f
            };

            return guidedRoute ? duration + 0.14f : duration;
        }

        private static float ResolveHighlightRadius(RoomType roomType, int targetCount)
        {
            float baseRadius = roomType switch
            {
                RoomType.Boss => 0.78f,
                RoomType.MiniBoss => 0.74f,
                RoomType.Challenge => 0.72f,
                RoomType.Treasure => 0.58f,
                RoomType.Shop => 0.54f,
                RoomType.Secret => 0.6f,
                RoomType.Curse => 0.58f,
                RoomType.Trap => 0.62f,
                _ => 0.5f
            };

            return baseRadius + (Mathf.Clamp(targetCount, 1, 4) * 0.03f);
        }

        private static float ResolveArrivalAssistDuration(RoomType roomType, bool guidedRoute)
        {
            float duration = roomType switch
            {
                RoomType.Treasure => 1.26f,
                RoomType.Shop => 1.2f,
                RoomType.Secret => 1.12f,
                RoomType.Curse => 1.12f,
                _ => 1f
            };

            return guidedRoute ? duration + 0.14f : duration;
        }

        private readonly struct FocusBeatProfile
        {
            public FocusBeatProfile(
                bool enabled,
                RoomType roomType,
                Color accentColor,
                string focusLabel,
                float worldIntensity,
                float defaultRadius,
                float labelLifetimeBonus,
                float labelRiseBonus,
                float labelScaleBonus)
            {
                Enabled = enabled;
                RoomType = roomType;
                AccentColor = accentColor;
                FocusLabel = focusLabel;
                WorldIntensity = worldIntensity;
                DefaultRadius = defaultRadius;
                LabelLifetimeBonus = labelLifetimeBonus;
                LabelRiseBonus = labelRiseBonus;
                LabelScaleBonus = labelScaleBonus;
            }

            public bool Enabled { get; }
            public RoomType RoomType { get; }
            public Color AccentColor { get; }
            public string FocusLabel { get; }
            public float WorldIntensity { get; }
            public float DefaultRadius { get; }
            public float LabelLifetimeBonus { get; }
            public float LabelRiseBonus { get; }
            public float LabelScaleBonus { get; }
        }
    }
}
