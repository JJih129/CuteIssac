using System.Collections.Generic;
using CuteIssac.Core.Run;
using CuteIssac.Data.Dungeon;
using CuteIssac.Data.Item;
using CuteIssac.Player;
using UnityEngine;

namespace CuteIssac.Item
{
    /// <summary>
    /// Keeps shop stock grouped together and exposes purchase operations without leaking UI or player input details.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ShopInventory : MonoBehaviour
    {
        [SerializeField] private ShopItem[] shopItems = System.Array.Empty<ShopItem>();
        [SerializeField] private RunItemPoolService runItemPoolService;

        private ShopItem _highlightedItem;
        private readonly HashSet<string> _selectedItemIds = new();
        private readonly List<ShopSlotState> _slotStateBuffer = new List<ShopSlotState>();

        public void ConfigureFromItemPool(ItemPoolData itemPool)
        {
            ResolveItemPoolService();
            _highlightedItem = null;
            if (itemPool == null)
            {
                ClearConfiguredItems();
                return;
            }

            _selectedItemIds.Clear();

            for (int i = 0; i < shopItems.Length; i++)
            {
                ShopItem shopItem = shopItems[i];

                if (shopItem == null)
                {
                    continue;
                }

                ItemPoolSelectionContext selectionContext = runItemPoolService != null
                    ? runItemPoolService.BuildSelectionContext(RoomType.Shop, _selectedItemIds)
                    : new ItemPoolSelectionContext(RoomType.Shop, 1, _selectedItemIds, null, null, null, null, null);

                if (!itemPool.TrySelectRandomItem(selectionContext, out ItemData selectedItem))
                {
                    shopItem.ConfigureShopItemData(null);
                    shopItem.gameObject.SetActive(false);
                    continue;
                }

                ShopItemData runtimeShopItemData = ShopItemData.CreateRuntimePassiveItemOffer(
                    selectedItem,
                    ResolveRuntimePrice(selectedItem),
                    ShopCurrencyType.Coins);

                shopItem.gameObject.SetActive(runtimeShopItemData != null);
                shopItem.ConfigureShopItemData(runtimeShopItemData);

                if (runtimeShopItemData != null)
                {
                    _selectedItemIds.Add(selectedItem.ItemId);
                    runItemPoolService?.RegisterOffer(selectedItem);
                }
            }
        }

        public ShopItem GetClosestAvailableItem(
            Vector3 buyerPosition,
            float maxDistance,
            PlayerInventory playerInventory,
            PlayerItemManager playerItemManager,
            PlayerHealth playerHealth)
        {
            ShopItem closestItem = null;
            float closestDistanceSqr = maxDistance * maxDistance;

            for (int i = 0; i < shopItems.Length; i++)
            {
                ShopItem shopItem = shopItems[i];

                if (shopItem == null || shopItem.IsSold)
                {
                    continue;
                }

                float distanceSqr = (shopItem.transform.position - buyerPosition).sqrMagnitude;

                if (distanceSqr <= closestDistanceSqr)
                {
                    closestDistanceSqr = distanceSqr;
                    closestItem = shopItem;
                }

                shopItem.RefreshView(false, playerInventory, playerItemManager, playerHealth);
            }

            return closestItem;
        }

        public void SetHighlightedItem(ShopItem highlightedItem, PlayerInventory playerInventory, PlayerItemManager playerItemManager, PlayerHealth playerHealth)
        {
            _highlightedItem = highlightedItem;

            for (int i = 0; i < shopItems.Length; i++)
            {
                ShopItem shopItem = shopItems[i];

                if (shopItem != null)
                {
                    shopItem.RefreshView(shopItem == _highlightedItem, playerInventory, playerItemManager, playerHealth);
                }
            }
        }

        public bool TryPurchaseHighlighted(PlayerInventory playerInventory, PlayerItemManager playerItemManager, PlayerHealth playerHealth)
        {
            if (_highlightedItem == null)
            {
                return false;
            }

            bool purchased = _highlightedItem.TryPurchase(playerInventory, playerItemManager, playerHealth);
            _highlightedItem.RefreshView(true, playerInventory, playerItemManager, playerHealth);
            return purchased;
        }

        public IReadOnlyList<ShopSlotState> BuildSlotStates(PlayerInventory playerInventory, PlayerItemManager playerItemManager, PlayerHealth playerHealth)
        {
            _slotStateBuffer.Clear();

            for (int i = 0; i < shopItems.Length; i++)
            {
                ShopItem shopItem = shopItems[i];

                if (shopItem == null)
                {
                    continue;
                }

                _slotStateBuffer.Add(shopItem.BuildSlotState(shopItem == _highlightedItem, playerInventory, playerItemManager, playerHealth));
            }

            return _slotStateBuffer;
        }

        private void Reset()
        {
            shopItems = GetComponentsInChildren<ShopItem>(true);
        }

        private void OnValidate()
        {
            if (shopItems == null || shopItems.Length == 0)
            {
                shopItems = GetComponentsInChildren<ShopItem>(true);
            }

            ResolveItemPoolService();
        }

        private static int ResolveRuntimePrice(ItemData itemData)
        {
            if (itemData == null)
            {
                return 5;
            }

            return itemData.Rarity switch
            {
                ItemRarity.Uncommon => 8,
                ItemRarity.Rare => 12,
                ItemRarity.Legendary => 16,
                ItemRarity.Relic => 20,
                ItemRarity.Boss => 24,
                _ => 5
            };
        }

        private void ClearConfiguredItems()
        {
            for (int i = 0; i < shopItems.Length; i++)
            {
                ShopItem shopItem = shopItems[i];

                if (shopItem == null)
                {
                    continue;
                }

                shopItem.ConfigureShopItemData(null);
                shopItem.gameObject.SetActive(false);
            }
        }

        private void ResolveItemPoolService()
        {
            if (runItemPoolService == null)
            {
                runItemPoolService = FindFirstObjectByType<RunItemPoolService>(FindObjectsInactive.Exclude);
            }
        }
    }
}
