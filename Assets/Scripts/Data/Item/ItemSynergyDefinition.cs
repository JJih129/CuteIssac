using System;
using System.Collections.Generic;
using CuteIssac.Common.Stats;
using UnityEngine;

namespace CuteIssac.Data.Item
{
    /// <summary>
    /// Central synergy rule data.
    /// This can later be moved to ScriptableObject assets without changing PlayerStats or combat code.
    /// </summary>
    [Serializable]
    public sealed class ItemSynergyDefinition
    {
        [SerializeField] private string synergyId = "synergy";
        [SerializeField] private string displayName = "Item Synergy";
        [SerializeField] private List<ItemTag> requiredTags = new();
        [SerializeField] private List<StatModifier> statModifiers = new();
        [SerializeField] private List<ProjectileModifier> projectileModifiers = new();

        public ItemSynergyDefinition()
        {
        }

        public ItemSynergyDefinition(
            string synergyId,
            string displayName,
            IReadOnlyList<ItemTag> requiredTags,
            IReadOnlyList<StatModifier> statModifiers,
            IReadOnlyList<ProjectileModifier> projectileModifiers)
        {
            this.synergyId = synergyId;
            this.displayName = displayName;
            this.requiredTags = requiredTags != null ? new List<ItemTag>(requiredTags) : new List<ItemTag>();
            this.statModifiers = statModifiers != null ? new List<StatModifier>(statModifiers) : new List<StatModifier>();
            this.projectileModifiers = projectileModifiers != null ? new List<ProjectileModifier>(projectileModifiers) : new List<ProjectileModifier>();
        }

        public string SynergyId => synergyId;
        public string DisplayName => displayName;
        public IReadOnlyList<ItemTag> RequiredTags => requiredTags;
        public IReadOnlyList<StatModifier> StatModifiers => statModifiers;
        public IReadOnlyList<ProjectileModifier> ProjectileModifiers => projectileModifiers;
    }
}
