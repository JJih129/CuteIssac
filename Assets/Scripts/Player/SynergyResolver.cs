using System.Collections.Generic;
using CuteIssac.Common.Stats;
using CuteIssac.Data.Item;

namespace CuteIssac.Player
{
    /// <summary>
    /// Resolves active item synergies from the currently owned passive item list.
    /// The resolver is centralized so combat code never branches on specific item combinations.
    /// </summary>
    public static class SynergyResolver
    {
        private static readonly ItemSynergyDefinition[] FallbackDefinitions =
        {
            new(
                "scatter_breach",
                "Scatter Breach",
                new[] { ItemTag.MultiShot, ItemTag.Pierce },
                new[] { new StatModifier(PlayerStatType.Knockback, StatModifierOperation.Add, 1f) },
                new[]
                {
                    new ProjectileModifier(ProjectileModifierType.Pierce, StatModifierOperation.Add, 1f),
                    new ProjectileModifier(ProjectileModifierType.Scale, StatModifierOperation.Add, 0.2f)
                }),
            new(
                "seeker_rush",
                "Seeker Rush",
                new[] { ItemTag.Homing, ItemTag.SpeedUp },
                new[] { new StatModifier(PlayerStatType.Range, StatModifierOperation.Add, 4f) },
                new[]
                {
                    new ProjectileModifier(ProjectileModifierType.Homing, StatModifierOperation.Multiply, 1.5f),
                    new ProjectileModifier(ProjectileModifierType.Speed, StatModifierOperation.Multiply, 1.2f)
                }),
            new(
                "blood_rhythm",
                "Blood Tempo",
                new[] { ItemTag.DamageUp, ItemTag.FireRateUp },
                new[]
                {
                    new StatModifier(PlayerStatType.Damage, StatModifierOperation.Add, 1.5f),
                    new StatModifier(PlayerStatType.FireInterval, StatModifierOperation.Add, -0.05f),
                    new StatModifier(PlayerStatType.Knockback, StatModifierOperation.Add, 0.75f)
                },
                new[]
                {
                    new ProjectileModifier(ProjectileModifierType.Scale, StatModifierOperation.Add, 0.15f)
                })
        };

        public static void ResolveActiveSynergies(
            ItemSynergyCatalog catalog,
            IReadOnlyList<ItemData> passiveItems,
            List<ItemSynergyDefinition> activeSynergies,
            ModifierStack modifierStack)
        {
            activeSynergies.Clear();
            modifierStack.Clear();

            if (passiveItems == null || passiveItems.Count == 0)
            {
                return;
            }

            HashSet<ItemTag> ownedTags = new();

            for (int itemIndex = 0; itemIndex < passiveItems.Count; itemIndex++)
            {
                ItemData itemData = passiveItems[itemIndex];

                if (itemData == null)
                {
                    continue;
                }

                IReadOnlyList<ItemTag> tags = itemData.ItemTags;

                for (int tagIndex = 0; tagIndex < tags.Count; tagIndex++)
                {
                    ownedTags.Add(tags[tagIndex]);
                }
            }

            IReadOnlyList<ItemSynergyDefinition> definitions =
                catalog != null && catalog.Definitions != null && catalog.Definitions.Count > 0
                    ? catalog.Definitions
                    : FallbackDefinitions;

            for (int definitionIndex = 0; definitionIndex < definitions.Count; definitionIndex++)
            {
                ItemSynergyDefinition definition = definitions[definitionIndex];

                if (!Matches(definition, ownedTags))
                {
                    continue;
                }

                activeSynergies.Add(definition);

                modifierStack.AddRange(definition.StatModifiers);
                modifierStack.AddRange(definition.ProjectileModifiers);
            }
        }

        private static bool Matches(ItemSynergyDefinition definition, HashSet<ItemTag> ownedTags)
        {
            for (int tagIndex = 0; tagIndex < definition.RequiredTags.Count; tagIndex++)
            {
                if (!ownedTags.Contains(definition.RequiredTags[tagIndex]))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
