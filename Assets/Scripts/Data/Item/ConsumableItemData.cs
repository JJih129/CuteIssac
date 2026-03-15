using System.Collections.Generic;
using CuteIssac.Common.Stats;
using UnityEngine;

namespace CuteIssac.Data.Item
{
    /// <summary>
    /// Authored data for consumable items.
    /// These stay separate from passive items so pickup, storage, and one-shot usage rules can evolve independently.
    /// </summary>
    [CreateAssetMenu(fileName = "ConsumableItemData", menuName = "CuteIssac/Data/Item/Consumable Item Data")]
    public sealed class ConsumableItemData : ScriptableObject
    {
        [SerializeField] private string itemId = "consumable";
        [SerializeField] private string displayName = "Consumable";
        [SerializeField] [TextArea] private string description;
        [SerializeField] private Sprite icon;
        [SerializeField] private ConsumablePickupMode pickupMode = ConsumablePickupMode.StoreInHolder;
        [SerializeField] [Min(0f)] private float healAmount;
        [SerializeField] [Min(0)] private int coinGain;
        [SerializeField] [Min(0)] private int keyGain;
        [SerializeField] [Min(0)] private int bombGain;
        [SerializeField] [Min(0f)] private float temporaryEffectDuration;
        [SerializeField] private List<StatModifier> temporaryStatModifiers = new();
        [SerializeField] private List<ProjectileModifier> temporaryProjectileModifiers = new();

        public string ItemId => itemId;
        public string DisplayName => displayName;
        public string Description => description;
        public Sprite Icon => icon;
        public ConsumablePickupMode PickupMode => pickupMode;
        public float HealAmount => healAmount;
        public int CoinGain => coinGain;
        public int KeyGain => keyGain;
        public int BombGain => bombGain;
        public float TemporaryEffectDuration => temporaryEffectDuration;
        public IReadOnlyList<StatModifier> TemporaryStatModifiers => temporaryStatModifiers;
        public IReadOnlyList<ProjectileModifier> TemporaryProjectileModifiers => temporaryProjectileModifiers;

        public bool HasTimedEffect =>
            temporaryEffectDuration > 0f &&
            (temporaryStatModifiers.Count > 0 || temporaryProjectileModifiers.Count > 0);

        public bool HasAnyEffect => healAmount > 0f || coinGain > 0 || keyGain > 0 || bombGain > 0 || HasTimedEffect;
    }
}
