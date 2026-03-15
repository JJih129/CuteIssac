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

            foreach (string unlockedItemKey in UnlockedItemKeys)
            {
                if (unlockedItemKey == itemData.UnlockKey)
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

            foreach (string itemId in itemIds)
            {
                if (itemId == itemData.ItemId)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
