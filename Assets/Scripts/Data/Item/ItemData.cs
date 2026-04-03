using System.Collections.Generic;
using CuteIssac.Common.Stats;
using UnityEngine;
using UnityEngine.Serialization;

namespace CuteIssac.Data.Item
{
    /// <summary>
    /// Passive item authoring asset for the current prototype.
    /// This keeps pickup presentation and stat effects in data so inventory and stats can remain generic.
    /// </summary>
    [CreateAssetMenu(fileName = "ItemData", menuName = "CuteIssac/Data/Item/Item Data")]
    public sealed class ItemData : IsaacItemData
    {
        [SerializeField] private string itemId = "item";
        [SerializeField] private string displayName = "Passive Item";
        [SerializeField] [TextArea] private string description;
        [SerializeField] private Sprite icon;
        [SerializeField] private ItemRarity rarity = ItemRarity.Common;
        [SerializeField] private bool unlockedByDefault = true;
        [SerializeField] private string unlockKey;
        [FormerlySerializedAs("synergyTags")]
        [SerializeField] private List<ItemTag> itemTags = new();
        [SerializeField] private List<StatModifier> statModifiers = new();
        [SerializeField] private List<ProjectileModifier> projectileModifiers = new();
        [SerializeField] private List<ItemGameplayEventEffect> gameplayEventEffects = new();
        [SerializeField] private List<ItemShopPriceModifier> shopPriceModifiers = new();
        [SerializeField] private List<ItemDoorCostModifier> doorCostModifiers = new();
        [SerializeField] private List<ItemRoomRewardModifier> roomRewardModifiers = new();
        [SerializeField] private ItemWeaponProfile weaponProfile = new();
        private readonly List<StatModifier> _resolvedStatModifiers = new();
        private readonly List<ProjectileModifier> _resolvedProjectileModifiers = new();
        private readonly ModifierStack _modifierStack = new();

        public override string ItemId => itemId;
        public override string DisplayName => displayName;
        public override string Description => description;
        public override Sprite Icon => icon;
        public override ItemRarity Rarity => rarity;
        public override bool UnlockedByDefault => unlockedByDefault;
        public override string UnlockKey => unlockKey;
        public override IReadOnlyList<ItemTag> ItemTags => itemTags;
        public IReadOnlyList<ItemTag> SynergyTags => itemTags;
        public IReadOnlyList<StatModifier> StatModifiers => ResolveStatModifiers();
        public IReadOnlyList<ProjectileModifier> ProjectileModifiers => ResolveProjectileModifiers();
        public override IReadOnlyList<ItemGameplayEventEffect> GameplayEventEffects => gameplayEventEffects;
        public IReadOnlyList<ItemShopPriceModifier> ShopPriceModifiers => shopPriceModifiers;
        public IReadOnlyList<ItemDoorCostModifier> DoorCostModifiers => doorCostModifiers;
        public IReadOnlyList<ItemRoomRewardModifier> RoomRewardModifiers => roomRewardModifiers;
        public ItemWeaponProfile WeaponProfile => weaponProfile;
        public bool IsWeaponRelic => ItemType == ItemType.Weapon && weaponProfile != null && weaponProfile.IsValid;
        public string WeaponMotif => IsWeaponRelic ? weaponProfile.FirearmMotif : string.Empty;
        public string WeaponHudLabel => IsWeaponRelic && !string.IsNullOrWhiteSpace(weaponProfile.HudLabel)
            ? weaponProfile.HudLabel.Trim()
            : displayName;

        protected override void AppendToModifierStack(ModifierStack modifierStack)
        {
            base.AppendToModifierStack(modifierStack);
            modifierStack.AddRange(statModifiers);
            modifierStack.AddRange(projectileModifiers);
        }

        public string BuildWeaponAmmoSummary()
        {
            if (!IsWeaponRelic)
            {
                return string.Empty;
            }

            string reserveLabel = weaponProfile.InfiniteReserveAmmo
                ? "INF"
                : weaponProfile.StartingReserveAmmo.ToString();
            return $"{weaponProfile.MagazineCapacity}/{reserveLabel}";
        }

        public string BuildWeaponPickupSummary()
        {
            if (!IsWeaponRelic)
            {
                return string.Empty;
            }

            string reserveLabel = weaponProfile.InfiniteReserveAmmo
                ? "INF"
                : weaponProfile.StartingReserveAmmo.ToString();
            string motifLabel = !string.IsNullOrWhiteSpace(weaponProfile.FirearmMotif)
                ? weaponProfile.FirearmMotif.Trim()
                : "modern firearm relic";
            string pelletLabel = weaponProfile.ShotsPerTrigger > 1
                ? $" · {weaponProfile.ShotsPerTrigger} pellets"
                : string.Empty;
            return $"{weaponProfile.MagazineCapacity}/{reserveLabel} rounds · {weaponProfile.ReloadDuration:0.#}s reload{pelletLabel}\n{motifLabel}";
        }

        private IReadOnlyList<StatModifier> ResolveStatModifiers()
        {
            RebuildResolvedModifiers();
            return _resolvedStatModifiers;
        }

        private IReadOnlyList<ProjectileModifier> ResolveProjectileModifiers()
        {
            RebuildResolvedModifiers();
            return _resolvedProjectileModifiers;
        }

        private void RebuildResolvedModifiers()
        {
            _resolvedStatModifiers.Clear();
            _resolvedProjectileModifiers.Clear();
            BuildModifierStack(_modifierStack);
            _resolvedStatModifiers.AddRange(_modifierStack.StatModifiers);
            _resolvedProjectileModifiers.AddRange(_modifierStack.ProjectileModifiers);
        }
    }
}
