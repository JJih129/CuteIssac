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

        protected override void AppendToModifierStack(ModifierStack modifierStack)
        {
            base.AppendToModifierStack(modifierStack);
            modifierStack.AddRange(statModifiers);
            modifierStack.AddRange(projectileModifiers);
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
