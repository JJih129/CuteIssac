using System;
using UnityEngine;

namespace CuteIssac.Player
{
    /// <summary>
    /// Read-only snapshot for HUD or future save systems that need to inspect the active consumable slot.
    /// </summary>
    [Serializable]
    public readonly struct PlayerConsumableSlotState
    {
        public PlayerConsumableSlotState(
            bool hasConsumable,
            string displayName,
            Sprite icon,
            bool hasActiveTimedEffect,
            float timedEffectNormalizedRemaining)
        {
            HasConsumable = hasConsumable;
            DisplayName = displayName;
            Icon = icon;
            HasActiveTimedEffect = hasActiveTimedEffect;
            TimedEffectNormalizedRemaining = Mathf.Clamp01(timedEffectNormalizedRemaining);
        }

        public bool HasConsumable { get; }
        public string DisplayName { get; }
        public Sprite Icon { get; }
        public bool HasActiveTimedEffect { get; }
        public float TimedEffectNormalizedRemaining { get; }
    }
}
