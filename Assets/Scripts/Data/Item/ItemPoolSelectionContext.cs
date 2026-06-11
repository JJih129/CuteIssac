using System.Collections.Generic;
using CuteIssac.Data.Dungeon;

namespace CuteIssac.Data.Item
{
    public readonly struct ItemPoolSelectionContext
    {
        public ItemPoolSelectionContext(
            RoomType roomType,
            int floorIndex,
            IReadOnlyCollection<string> excludedItemIds,
            IReadOnlyCollection<string> ownedItemIds,
            IReadOnlyCollection<string> offeredItemIds,
            IReadOnlyCollection<string> recentItemIds,
            IReadOnlyCollection<ItemCategory> recentCategories,
            IReadOnlyCollection<string> unlockedItemKeys)
        {
            RoomType = roomType;
            FloorIndex = floorIndex;
            ExcludedItemIds = excludedItemIds;
            OwnedItemIds = ownedItemIds;
            OfferedItemIds = offeredItemIds;
            RecentItemIds = recentItemIds;
            RecentCategories = recentCategories;
            UnlockedItemKeys = unlockedItemKeys;
        }

        public RoomType RoomType { get; }
        public int FloorIndex { get; }
        public IReadOnlyCollection<string> ExcludedItemIds { get; }
        public IReadOnlyCollection<string> OwnedItemIds { get; }
        public IReadOnlyCollection<string> OfferedItemIds { get; }
        public IReadOnlyCollection<string> RecentItemIds { get; }
        public IReadOnlyCollection<ItemCategory> RecentCategories { get; }
        public IReadOnlyCollection<string> UnlockedItemKeys { get; }

        public bool IsExcluded(ItemData itemData)
        {
            return ContainsItemId(ExcludedItemIds, itemData);
        }

        public bool IsOwned(ItemData itemData)
        {
            return ContainsItemId(OwnedItemIds, itemData);
        }

        public bool WasOffered(ItemData itemData)
        {
            return ContainsItemId(OfferedItemIds, itemData);
        }

        public bool WasRecentlyOffered(ItemData itemData)
        {
            return ContainsItemId(RecentItemIds, itemData);
        }

        public bool WasRecentlyOfferedCategory(ItemData itemData)
        {
            if (itemData == null || RecentCategories == null)
            {
                return false;
            }

            if (RecentCategories is HashSet<ItemCategory> categoryHashSet)
            {
                return categoryHashSet.Contains(itemData.ItemCategory);
            }

            if (RecentCategories is ISet<ItemCategory> categorySet)
            {
                return categorySet.Contains(itemData.ItemCategory);
            }

            foreach (ItemCategory recentCategory in RecentCategories)
            {
                if (recentCategory == itemData.ItemCategory)
                {
                    return true;
                }
            }

            return false;
        }

        public bool IsUnlocked(ItemData itemData)
        {
            if (itemData == null)
            {
                return false;
            }

            if (itemData.UnlockedByDefault || string.IsNullOrWhiteSpace(itemData.UnlockKey))
            {
                return true;
            }

            if (UnlockedItemKeys == null)
            {
                return false;
            }

            if (UnlockedItemKeys is HashSet<string> unlockedHashSet)
            {
                if (unlockedHashSet.Contains(itemData.UnlockKey))
                {
                    return true;
                }
            }

            if (UnlockedItemKeys is ISet<string> unlockedSet)
            {
                if (unlockedSet.Contains(itemData.UnlockKey))
                {
                    return true;
                }
            }

            foreach (string unlockedItemKey in UnlockedItemKeys)
            {
                if (string.Equals(unlockedItemKey, itemData.UnlockKey, System.StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool ContainsItemId(IReadOnlyCollection<string> itemIds, ItemData itemData)
        {
            if (itemData == null || itemIds == null)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(itemData.ItemId))
            {
                return false;
            }

            if (itemIds is HashSet<string> itemHashSet)
            {
                if (itemHashSet.Contains(itemData.ItemId))
                {
                    return true;
                }
            }

            if (itemIds is ISet<string> itemSet)
            {
                if (itemSet.Contains(itemData.ItemId))
                {
                    return true;
                }
            }

            foreach (string itemId in itemIds)
            {
                if (string.Equals(itemId, itemData.ItemId, System.StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
