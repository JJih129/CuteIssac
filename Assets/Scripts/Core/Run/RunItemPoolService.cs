using System.Collections.Generic;
using CuteIssac.Data.Dungeon;
using CuteIssac.Data.Item;
using UnityEngine;

namespace CuteIssac.Core.Run
{
    [DisallowMultipleComponent]
    public sealed class RunItemPoolService : MonoBehaviour
    {
        [SerializeField] private RunManager runManager;
        [SerializeField] [Min(1)] private int recentOfferMemory = 8;
        [SerializeField] [Min(1)] private int recentCategoryMemory = 4;

        private readonly HashSet<string> _ownedItemIds = new();
        private readonly HashSet<string> _offeredItemIds = new();
        private readonly HashSet<string> _recentOfferedItemIds = new();
        private readonly Queue<string> _recentOfferedQueue = new();
        private readonly HashSet<ItemCategory> _recentOfferedCategories = new();
        private readonly Queue<ItemCategory> _recentOfferedCategoryQueue = new();
        private readonly Dictionary<ItemCategory, int> _recentOfferedCategoryCounts = new();
        private readonly HashSet<string> _unlockedItemKeys = new();

        public ItemPoolSelectionContext BuildSelectionContext(RoomType roomType, IReadOnlyCollection<string> excludedItemIds = null)
        {
            int floorIndex = runManager != null && runManager.CurrentContext.HasActiveRun
                ? runManager.CurrentContext.CurrentFloorIndex
                : 1;

            return new ItemPoolSelectionContext(
                roomType,
                floorIndex,
                excludedItemIds,
                _ownedItemIds,
                _offeredItemIds,
                _recentOfferedItemIds,
                _recentOfferedCategories,
                _unlockedItemKeys);
        }

        public void RegisterOffer(ItemData itemData)
        {
            if (itemData == null || string.IsNullOrWhiteSpace(itemData.ItemId))
            {
                return;
            }

            _offeredItemIds.Add(itemData.ItemId);
            _recentOfferedItemIds.Add(itemData.ItemId);
            _recentOfferedQueue.Enqueue(itemData.ItemId);
            RegisterRecentCategory(itemData.ItemCategory);

            while (_recentOfferedQueue.Count > Mathf.Max(1, recentOfferMemory))
            {
                string evictedItemId = _recentOfferedQueue.Dequeue();

                if (_recentOfferedQueue.Contains(evictedItemId))
                {
                    continue;
                }

                _recentOfferedItemIds.Remove(evictedItemId);
            }
        }

        public void RegisterAcquired(ItemData itemData)
        {
            if (itemData == null || string.IsNullOrWhiteSpace(itemData.ItemId))
            {
                return;
            }

            _ownedItemIds.Add(itemData.ItemId);
            RegisterOffer(itemData);
        }

        public void SyncOwnedItems(IReadOnlyList<ItemData> ownedItems)
        {
            _ownedItemIds.Clear();

            if (ownedItems == null)
            {
                return;
            }

            for (int index = 0; index < ownedItems.Count; index++)
            {
                ItemData itemData = ownedItems[index];

                if (itemData == null || string.IsNullOrWhiteSpace(itemData.ItemId))
                {
                    continue;
                }

                _ownedItemIds.Add(itemData.ItemId);
            }
        }

        public void GrantUnlock(string unlockKey)
        {
            if (!string.IsNullOrWhiteSpace(unlockKey))
            {
                _unlockedItemKeys.Add(unlockKey);
            }
        }

        private void Awake()
        {
            ResolveReferences();
        }

        private void OnEnable()
        {
            ResolveReferences();

            if (runManager != null)
            {
                runManager.RunStarted -= HandleRunStarted;
                runManager.RunStarted += HandleRunStarted;
            }
        }

        private void OnDisable()
        {
            if (runManager != null)
            {
                runManager.RunStarted -= HandleRunStarted;
            }
        }

        private void HandleRunStarted(RunContext context)
        {
            _ownedItemIds.Clear();
            _offeredItemIds.Clear();
            _recentOfferedItemIds.Clear();
            _recentOfferedQueue.Clear();
            _recentOfferedCategories.Clear();
            _recentOfferedCategoryQueue.Clear();
            _recentOfferedCategoryCounts.Clear();
        }

        private void ResolveReferences()
        {
            if (runManager == null)
            {
                runManager = GetComponent<RunManager>();
            }
        }

        private void RegisterRecentCategory(ItemCategory itemCategory)
        {
            _recentOfferedCategoryQueue.Enqueue(itemCategory);

            if (_recentOfferedCategoryCounts.TryGetValue(itemCategory, out int existingCount))
            {
                _recentOfferedCategoryCounts[itemCategory] = existingCount + 1;
            }
            else
            {
                _recentOfferedCategoryCounts[itemCategory] = 1;
            }

            _recentOfferedCategories.Add(itemCategory);

            while (_recentOfferedCategoryQueue.Count > Mathf.Max(1, recentCategoryMemory))
            {
                ItemCategory evictedCategory = _recentOfferedCategoryQueue.Dequeue();

                if (!_recentOfferedCategoryCounts.TryGetValue(evictedCategory, out int evictedCount))
                {
                    continue;
                }

                evictedCount--;
                if (evictedCount <= 0)
                {
                    _recentOfferedCategoryCounts.Remove(evictedCategory);
                    _recentOfferedCategories.Remove(evictedCategory);
                    continue;
                }

                _recentOfferedCategoryCounts[evictedCategory] = evictedCount;
            }
        }
    }
}
