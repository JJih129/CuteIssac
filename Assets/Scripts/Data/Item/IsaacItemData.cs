using System;
using System.Collections.Generic;
using CuteIssac.Common.Stats;
using UnityEngine;

namespace CuteIssac.Data.Item
{
    public abstract class IsaacItemData : ScriptableObject
    {
        [Header("Isaac Item Identity")]
        [SerializeField] private ItemType itemType = ItemType.Passive;
        [SerializeField] private ItemCategory itemCategory = ItemCategory.Utility;
        [SerializeField] [TextArea] private string flavorText;
        [SerializeField] private List<ItemPoolType> sourcePools = new();
        [SerializeField] private List<ItemEventFlag> eventFlags = new();

        [Header("Flat Stat Modifiers")]
        [SerializeField] private float damageModifier;
        [SerializeField] private float tearsModifier;
        [SerializeField] private float speedModifier;
        [SerializeField] private float rangeModifier;
        [SerializeField] private float shotSpeedModifier;
        [SerializeField] private float luckModifier;
        [SerializeField] private float maxHealthModifier;

        [Header("Projectile Flags")]
        [SerializeField] private List<ProjectileFlag> projectileFlags = new();

        public abstract string ItemId { get; }
        public abstract string DisplayName { get; }
        public abstract string Description { get; }
        public abstract Sprite Icon { get; }
        public abstract ItemRarity Rarity { get; }
        public abstract bool UnlockedByDefault { get; }
        public abstract string UnlockKey { get; }

        public ItemType ItemType => itemType;
        public ItemCategory ItemCategory => itemCategory;
        public string FlavorText => flavorText;
        public IReadOnlyList<ItemPoolType> SourcePools => sourcePools;
        public IReadOnlyList<ItemEventFlag> EventFlags => eventFlags;
        public virtual IReadOnlyList<ItemTag> ItemTags => Array.Empty<ItemTag>();
        public virtual IReadOnlyList<ItemGameplayEventEffect> GameplayEventEffects => Array.Empty<ItemGameplayEventEffect>();

        public void BuildModifierStack(ModifierStack modifierStack)
        {
            if (modifierStack == null)
            {
                return;
            }

            modifierStack.Clear();
            AppendToModifierStack(modifierStack);
        }

        protected virtual void AppendToModifierStack(ModifierStack modifierStack)
        {
            TryAddStatModifier(modifierStack, PlayerStatType.Damage, damageModifier);
            TryAddStatModifier(modifierStack, PlayerStatType.FireInterval, -tearsModifier);
            TryAddStatModifier(modifierStack, PlayerStatType.MoveSpeed, speedModifier);
            TryAddStatModifier(modifierStack, PlayerStatType.Range, rangeModifier);
            TryAddStatModifier(modifierStack, PlayerStatType.ProjectileSpeed, shotSpeedModifier);
            TryAddStatModifier(modifierStack, PlayerStatType.Luck, luckModifier);
            TryAddStatModifier(modifierStack, PlayerStatType.MaxHealth, maxHealthModifier);

            for (int index = 0; index < projectileFlags.Count; index++)
            {
                switch (projectileFlags[index])
                {
                    case ProjectileFlag.Pierce:
                        modifierStack.Add(new ProjectileModifier(ProjectileModifierType.Pierce, StatModifierOperation.Add, 1f));
                        break;
                    case ProjectileFlag.Homing:
                        modifierStack.Add(new ProjectileModifier(ProjectileModifierType.Homing, StatModifierOperation.Add, 1f));
                        break;
                    case ProjectileFlag.MultiShot:
                        modifierStack.Add(new ProjectileModifier(ProjectileModifierType.MultiShot, StatModifierOperation.Add, 1f));
                        break;
                    case ProjectileFlag.Explode:
                        modifierStack.Add(new ProjectileModifier(ProjectileModifierType.Explode, StatModifierOperation.Add, 1f));
                        break;
                    case ProjectileFlag.Laser:
                        modifierStack.Add(new ProjectileModifier(ProjectileModifierType.Laser, StatModifierOperation.Add, 1f));
                        break;
                    case ProjectileFlag.Split:
                        modifierStack.Add(new ProjectileModifier(ProjectileModifierType.Split, StatModifierOperation.Add, 1f));
                        break;
                    case ProjectileFlag.Bounce:
                        modifierStack.Add(new ProjectileModifier(ProjectileModifierType.Bounce, StatModifierOperation.Add, 1f));
                        break;
                    case ProjectileFlag.Orbit:
                        modifierStack.Add(new ProjectileModifier(ProjectileModifierType.Orbit, StatModifierOperation.Add, 1f));
                        break;
                    case ProjectileFlag.Shield:
                        modifierStack.Add(new ProjectileModifier(ProjectileModifierType.Shield, StatModifierOperation.Add, 1f));
                        break;
                    case ProjectileFlag.Lifesteal:
                        modifierStack.Add(new ProjectileModifier(ProjectileModifierType.Lifesteal, StatModifierOperation.Add, 1f));
                        break;
                }
            }
        }

        private static void TryAddStatModifier(ModifierStack modifierStack, PlayerStatType statType, float value)
        {
            if (Mathf.Abs(value) <= 0.0001f)
            {
                return;
            }

            modifierStack.Add(new StatModifier(statType, StatModifierOperation.Add, value));
        }
    }
}
