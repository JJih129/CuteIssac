using System.Collections.Generic;
using UnityEngine;

namespace CuteIssac.Data.Item
{
    [CreateAssetMenu(fileName = "ItemSynergyCatalog", menuName = "CuteIssac/Data/Item/Item Synergy Catalog")]
    public sealed class ItemSynergyCatalog : ScriptableObject
    {
        [SerializeField] private List<ItemSynergyDefinition> definitions = new();

        public IReadOnlyList<ItemSynergyDefinition> Definitions => definitions;
    }
}
