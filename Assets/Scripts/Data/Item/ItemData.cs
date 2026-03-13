using System.Collections.Generic;
using CuteIssac.Common.Stats;
using UnityEngine;

namespace CuteIssac.Data.Item
{
    /// <summary>
    /// Passive item authoring asset for the current prototype.
    /// This keeps pickup presentation and stat effects in data so inventory and stats can remain generic.
    /// </summary>
    [CreateAssetMenu(fileName = "ItemData", menuName = "CuteIssac/Data/Item/Item Data")]
    public sealed class ItemData : ScriptableObject
    {
        [SerializeField] private string itemId = "item";
        [SerializeField] private string displayName = "Passive Item";
        [SerializeField] [TextArea] private string description;
        [SerializeField] private Sprite icon;
        [SerializeField] private List<StatModifier> statModifiers = new();

        public string ItemId => itemId;
        public string DisplayName => displayName;
        public string Description => description;
        public Sprite Icon => icon;
        public IReadOnlyList<StatModifier> StatModifiers => statModifiers;
    }
}
