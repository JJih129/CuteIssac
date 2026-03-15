using CuteIssac.Data.Item;
using UnityEngine;

namespace CuteIssac.Player
{
    public readonly struct PlayerActiveItemSlotState
    {
        public PlayerActiveItemSlotState(
            ActiveItemData activeItemData,
            int currentCharge,
            int maxCharge,
            bool canUse,
            bool hasTimedEffect,
            float timedEffectNormalizedRemaining)
        {
            ActiveItemData = activeItemData;
            CurrentCharge = currentCharge;
            MaxCharge = maxCharge;
            CanUse = canUse;
            HasTimedEffect = hasTimedEffect;
            TimedEffectNormalizedRemaining = timedEffectNormalizedRemaining;
        }

        public ActiveItemData ActiveItemData { get; }
        public int CurrentCharge { get; }
        public int MaxCharge { get; }
        public bool CanUse { get; }
        public bool HasTimedEffect { get; }
        public float TimedEffectNormalizedRemaining { get; }

        public bool HasItem => ActiveItemData != null;
        public string DisplayName => ActiveItemData != null ? ActiveItemData.DisplayName : string.Empty;
        public Sprite Icon => ActiveItemData != null ? ActiveItemData.Icon : null;
        public float ChargeNormalized => MaxCharge > 0 ? Mathf.Clamp01((float)CurrentCharge / MaxCharge) : 0f;
    }
}
