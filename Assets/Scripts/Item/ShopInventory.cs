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

        public event System.Action<ShopItem, bool> PurchaseAttemptResolved;

        public ShopItem CurrentHighlightedItem => _highlightedItem;
        public bool CurrentHighlightedCanPurchase { get; private set; }

        public void ConfigureFromItemPool(ItemPoolData itemPool)
        {
            ResolveItemPoolService();
            _highlightedItem = null;
            CurrentHighlightedCanPurchase = false;
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
            CurrentHighlightedCanPurchase = _highlightedItem != null
                && _highlightedItem.CanPurchase(playerInventory, playerItemManager, playerHealth);

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
                CurrentHighlightedCanPurchase = false;
                return false;
            }

            ShopItem attemptedItem = _highlightedItem;
            bool purchased = _highlightedItem.TryPurchase(playerInventory, playerItemManager, playerHealth);
            _highlightedItem.RefreshView(true, playerInventory, playerItemManager, playerHealth);
            CurrentHighlightedCanPurchase = _highlightedItem.CanPurchase(playerInventory, playerItemManager, playerHealth);
            PurchaseAttemptResolved?.Invoke(attemptedItem, purchased);
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

        public void CollectAvailableShopTargets(List<Transform> targetBuffer)
        {
            if (targetBuffer == null)
            {
                return;
            }

            for (int i = 0; i < shopItems.Length; i++)
            {
                ShopItem shopItem = shopItems[i];

                if (shopItem == null || shopItem.IsSold || !shopItem.gameObject.activeInHierarchy)
                {
                    continue;
                }

                targetBuffer.Add(shopItem.transform);
            }
        }

        public bool TryResolvePreferredItem(string reasonTag, out ShopItem preferredItem)
        {
            preferredItem = null;
            float bestScore = float.NegativeInfinity;

            for (int i = 0; i < shopItems.Length; i++)
            {
                ShopItem shopItem = shopItems[i];

                if (shopItem == null || shopItem.IsSold || !shopItem.gameObject.activeInHierarchy || shopItem.ShopItemData == null)
                {
                    continue;
                }

                float score = ResolvePreferredItemScore(shopItem, reasonTag);
                if (score <= bestScore)
                {
                    continue;
                }

                bestScore = score;
                preferredItem = shopItem;
            }

            return preferredItem != null;
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

            if (itemData.IsWeaponRelic)
            {
                return 30;
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

        private float ResolvePreferredItemScore(ShopItem shopItem, string reasonTag)
        {
            if (shopItem == null || shopItem.ShopItemData == null)
            {
                return float.NegativeInfinity;
            }

            ShopOffer offer = shopItem.ShopItemData.Offer;
            float score = shopItem == _highlightedItem ? 3.5f : 0f;
            score += Mathf.Clamp(shopItem.Price, 0, 24) * 0.08f;

            switch (reasonTag)
            {
                case "PATCH HP":
                    score += offer.RewardType == ShopOfferRewardType.Health ? 18f : 0f;
                    break;
                case "LOOK FOR KEYS":
                    score += offer.RewardType == ShopOfferRewardType.Keys ? 18f : 0f;
                    break;
                case "RESTOCK BOMBS":
                    score += offer.RewardType == ShopOfferRewardType.Bombs ? 18f : 0f;
                    break;
                case "CASH OUT":
                    score += offer.RewardType == ShopOfferRewardType.PassiveItem ? 12f : 4f;
                    score += shopItem.Price * 0.32f;
                    break;
                case "CASH WINDOW":
                    score += offer.RewardType == ShopOfferRewardType.PassiveItem ? 14f : 5f;
                    score += shopItem.Price * 0.36f;
                    break;
                case "PRESS ADVANTAGE":
                    score += offer.RewardType == ShopOfferRewardType.PassiveItem ? 15f : 2.5f;
                    break;
                case "RECOVERY ONLINE":
                    score += offer.RewardType switch
                    {
                        ShopOfferRewardType.Health => 12f,
                        ShopOfferRewardType.PassiveItem => 8f,
                        _ => 2f
                    };
                    break;
                case "FILL SLOT":
                    score += offer.RewardType == ShopOfferRewardType.PassiveItem ? 16f : 0f;
                    break;
                case "SUPPLY RUN":
                    score += offer.RewardType switch
                    {
                        ShopOfferRewardType.Health => 10f,
                        ShopOfferRewardType.Keys => 9f,
                        ShopOfferRewardType.Bombs => 9f,
                        ShopOfferRewardType.Coins => 7f,
                        _ => 5f
                    };
                    break;
                default:
                    score += offer.RewardType == ShopOfferRewardType.PassiveItem ? 4f : 1.5f;
                    break;
            }

            return score;
        }
    }
}
