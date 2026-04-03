using UnityEngine;

namespace CuteIssac.Core.Gameplay
{
    public readonly struct PlayerInteractionOutcomeSignal
    {
        public PlayerInteractionOutcomeSignal(
            Vector3 position,
            string headline,
            string detail,
            Color accentColor,
            bool emphasize)
        {
            Position = position;
            Headline = headline ?? string.Empty;
            Detail = detail ?? string.Empty;
            AccentColor = accentColor.a > 0.01f
                ? accentColor
                : Color.white;
            Emphasize = emphasize;
        }

        public Vector3 Position { get; }
        public string Headline { get; }
        public string Detail { get; }
        public Color AccentColor { get; }
        public bool Emphasize { get; }
        public bool HasDetail => !string.IsNullOrWhiteSpace(Detail);
        public bool IsValid => !string.IsNullOrWhiteSpace(Headline);
    }
}
