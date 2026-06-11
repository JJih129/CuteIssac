using System.Collections.Generic;
using UnityEngine;

namespace CuteIssac.Core.Meta
{
    [CreateAssetMenu(fileName = "MetaCollectionCatalog", menuName = "CuteIssac/Data/Meta/Collection Catalog")]
    public sealed class MetaCollectionCatalog : ScriptableObject
    {
        [SerializeField] private List<MetaCollectionEntry> entries = new();

        public IReadOnlyList<MetaCollectionEntry> Entries => entries;
    }
}
