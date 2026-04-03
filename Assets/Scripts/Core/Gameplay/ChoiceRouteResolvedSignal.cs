using CuteIssac.Room;
using UnityEngine;

namespace CuteIssac.Core.Gameplay
{
    public readonly struct ChoiceRouteResolvedSignal
    {
        public ChoiceRouteResolvedSignal(
            RoomController room,
            string headline,
            string detail,
            string compareLabel,
            string intentReasonTag,
            Color accentColor,
            bool heldRoute,
            float strength,
            bool emphasize,
            string carryHeadline = "",
            string carryDetail = "",
            bool carryHeld = false)
        {
            Room = room;
            Headline = headline ?? string.Empty;
            Detail = detail ?? string.Empty;
            CompareLabel = compareLabel ?? string.Empty;
            IntentReasonTag = intentReasonTag ?? string.Empty;
            AccentColor = accentColor.a > 0.01f
                ? accentColor
                : Color.white;
            HeldRoute = heldRoute;
            Strength = Mathf.Clamp01(strength);
            Emphasize = emphasize;
            CarryHeadline = carryHeadline ?? string.Empty;
            CarryDetail = carryDetail ?? string.Empty;
            CarryHeld = carryHeld;
        }

        public RoomController Room { get; }
        public string Headline { get; }
        public string Detail { get; }
        public string CompareLabel { get; }
        public string IntentReasonTag { get; }
        public Color AccentColor { get; }
        public bool HeldRoute { get; }
        public float Strength { get; }
        public bool Emphasize { get; }
        public string CarryHeadline { get; }
        public string CarryDetail { get; }
        public bool CarryHeld { get; }
        public bool HasCompareLabel => !string.IsNullOrWhiteSpace(CompareLabel);
        public bool HasIntent => !string.IsNullOrWhiteSpace(IntentReasonTag);
        public bool HasDetail => !string.IsNullOrWhiteSpace(Detail);
        public bool HasCarryClosure => !string.IsNullOrWhiteSpace(CarryHeadline);
        public bool HasCarryDetail => !string.IsNullOrWhiteSpace(CarryDetail);
        public bool IsValid => Room != null && !string.IsNullOrWhiteSpace(Headline);
    }
}
