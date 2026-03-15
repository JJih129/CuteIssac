using System.Collections.Generic;
using CuteIssac.Data.Dungeon;
using UnityEngine;

namespace CuteIssac.Data.Item
{
    /// <summary>
    /// Weighted passive-item pool used by treasure, shop, and boss reward content.
    /// Duplicate filtering is optional so future systems can extend the selection policy without changing callers.
    /// </summary>
    [CreateAssetMenu(fileName = "ItemPoolData", menuName = "CuteIssac/Data/Item/Item Pool Data")]
    public sealed class ItemPoolData : ScriptableObject
    {
        [SerializeField] private string poolId = "item_pool";
        [SerializeField] private ItemPoolType poolType = ItemPoolType.TreasurePool;
        [SerializeField] private bool preventDuplicateSelections = true;
        [SerializeField] private bool requireUnlockedItems = true;
        [SerializeField] [Range(0f, 1f)] private float ownedDuplicateWeightMultiplier = 0.2f;
        [SerializeField] [Range(0f, 1f)] private float offeredDuplicateWeightMultiplier = 0.45f;
        [SerializeField] [Range(0f, 1f)] private float recentDuplicateWeightMultiplier = 0.15f;
        [SerializeField] [Range(0f, 1f)] private float recentCategoryWeightMultiplier = 0.58f;
        [SerializeField] private List<ItemPoolRarityWeight> rarityWeights = new();
        [SerializeField] private List<ItemPoolEntry> entries = new();

        private readonly List<ItemPoolEntry> _candidateBuffer = new();
        private readonly List<float> _candidateWeightBuffer = new();

        public string PoolId => poolId;
        public ItemPoolType PoolType => poolType;
        public bool PreventDuplicateSelections => preventDuplicateSelections;
        public bool RequireUnlockedItems => requireUnlockedItems;
        public float OwnedDuplicateWeightMultiplier => ownedDuplicateWeightMultiplier;
        public float OfferedDuplicateWeightMultiplier => offeredDuplicateWeightMultiplier;
        public float RecentDuplicateWeightMultiplier => recentDuplicateWeightMultiplier;
        public float RecentCategoryWeightMultiplier => recentCategoryWeightMultiplier;
        public IReadOnlyList<ItemPoolRarityWeight> RarityWeights => rarityWeights;
        public IReadOnlyList<ItemPoolEntry> Entries => entries;

        public bool TrySelectRandomItem(HashSet<string> excludedItemIds, out ItemData selectedItem)
        {
            return TrySelectRandomItem(new ItemPoolSelectionContext(
                RoomType.Normal,
                1,
                excludedItemIds,
                null,
                null,
                null,
                null,
                null), out selectedItem);
        }

        public bool TrySelectRandomItem(ItemPoolSelectionContext selectionContext, out ItemData selectedItem)
        {
            _candidateBuffer.Clear();
            _candidateWeightBuffer.Clear();

            for (int i = 0; i < entries.Count; i++)
            {
                ItemPoolEntry entry = entries[i];

                if (!entry.IsAvailableFor(selectionContext.RoomType, selectionContext.FloorIndex))
                {
                    continue;
                }

                if (preventDuplicateSelections && selectionContext.IsExcluded(entry.ItemData))
                {
                    continue;
                }

                if (requireUnlockedItems && !selectionContext.IsUnlocked(entry.ItemData))
                {
                    continue;
                }

                float effectiveWeight = ResolveEffectiveWeight(entry, selectionContext);

                if (effectiveWeight <= 0f)
                {
                    continue;
                }

                _candidateBuffer.Add(entry);
                _candidateWeightBuffer.Add(effectiveWeight);
            }

            int selectedIndex = SelectWeightedIndex(_candidateWeightBuffer);

            if (selectedIndex < 0)
            {
                selectedItem = null;
                return false;
            }

            selectedItem = _candidateBuffer[selectedIndex].ItemData;
            return selectedItem != null;
        }

        private float ResolveEffectiveWeight(ItemPoolEntry entry, ItemPoolSelectionContext selectionContext)
        {
            float effectiveWeight = entry.Weight * ResolveRarityWeight(entry.ItemData != null ? entry.ItemData.Rarity : ItemRarity.Common);

            if (selectionContext.IsOwned(entry.ItemData))
            {
                effectiveWeight *= ownedDuplicateWeightMultiplier;
            }

            if (selectionContext.WasOffered(entry.ItemData))
            {
                effectiveWeight *= offeredDuplicateWeightMultiplier;
            }

            if (selectionContext.WasRecentlyOffered(entry.ItemData))
            {
                effectiveWeight *= recentDuplicateWeightMultiplier;
            }

            if (selectionContext.WasRecentlyOfferedCategory(entry.ItemData))
            {
                effectiveWeight *= recentCategoryWeightMultiplier;
            }

            return effectiveWeight;
        }

        private float ResolveRarityWeight(ItemRarity rarity)
        {
            for (int i = 0; i < rarityWeights.Count; i++)
            {
                if (rarityWeights[i].Rarity == rarity)
                {
                    return rarityWeights[i].WeightMultiplier;
                }
            }

            return 1f;
        }

        private static int SelectWeightedIndex(List<float> candidateWeights)
        {
            float totalWeight = 0f;

            for (int i = 0; i < candidateWeights.Count; i++)
            {
                totalWeight += candidateWeights[i];
            }

            if (totalWeight <= 0f)
            {
                return -1;
            }

            float threshold = Random.value * totalWeight;

            for (int i = 0; i < candidateWeights.Count; i++)
            {
                threshold -= candidateWeights[i];

                if (threshold <= 0f)
                {
                    return i;
                }
            }

            return candidateWeights.Count - 1;
        }
    }
}
