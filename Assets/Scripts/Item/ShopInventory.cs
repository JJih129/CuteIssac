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
        [SerializeField] [Min(0)] private int weaponOfferPrice = 30;
        [SerializeField] [Min(1)] private int randomItemOfferPriceMin = 12;
        [SerializeField] [Min(1)] private int randomItemOfferPriceMax = 24;
        [SerializeField] [Min(0.5f)] private float healthOfferAmount = 2f;
        [SerializeField] [Min(0)] private int healthOfferPrice = 5;
        [SerializeField] [Min(1)] private int ammoOfferAmount = 8;
        [SerializeField] [Min(0)] private int ammoOfferPrice = 5;
        [SerializeField] [Min(1)] private int minimumRuntimeShopSlots = 3;
        [SerializeField] [Min(0.1f)] private float shopSlotSpacing = 1.32f;
        [SerializeField] private Vector3 shopSlotCenterLocalPosition = new(0f, -1.05f, 0f);
        [SerializeField] private bool createRuntimeShopkeeper = true;
        [SerializeField] private Vector3 runtimeShopkeeperLocalPosition = new(0f, 1.16f, 0f);

        private ShopItem _highlightedItem;
        private Transform _runtimeContentRoot;
        private GameObject _runtimeShopkeeper;
        private readonly HashSet<string> _selectedItemIds = new();
        private readonly List<ShopSlotState> _slotStateBuffer = new List<ShopSlotState>();

        public event System.Action<ShopItem, bool> PurchaseAttemptResolved;

        public ShopItem CurrentHighlightedItem => _highlightedItem;
        public bool CurrentHighlightedCanPurchase { get; private set; }

        public void ConfigureFromItemPool(ItemPoolData itemPool)
        {
            ResolveItemPoolService();
            EnsureRuntimeShopPresentation();
            _highlightedItem = null;
            CurrentHighlightedCanPurchase = false;
            _selectedItemIds.Clear();

            if (shopItems == null || shopItems.Length == 0)
            {
                return;
            }

            int configuredItemSlots = ConfigureRandomItemSlots(itemPool);

            if (configuredItemSlots < 1)
            {
                ConfigureWeaponSlot(itemPool, 0);
                configuredItemSlots = 1;
            }

            if (configuredItemSlots < 2)
            {
                ConfigureHealthSlot(1);
                configuredItemSlots = 2;
            }

            if (configuredItemSlots < 3)
            {
                ConfigureAmmoSlot(2);
                configuredItemSlots = 3;
            }

            for (int i = configuredItemSlots; i < shopItems.Length; i++)
            {
                ClearShopSlot(shopItems[i]);
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

        private void ClearConfiguredItems()
        {
            for (int i = 0; i < shopItems.Length; i++)
            {
                ClearShopSlot(shopItems[i]);
            }
        }

        private void ResolveItemPoolService()
        {
            if (runItemPoolService == null)
            {
                runItemPoolService = FindFirstObjectByType<RunItemPoolService>(FindObjectsInactive.Exclude);
            }
        }

        private void EnsureRuntimeShopPresentation()
        {
            EnsureMinimumShopSlots();
            AlignShopSlots(Mathf.Max(1, minimumRuntimeShopSlots));
            EnsureRuntimeShopkeeper();
        }

        private void EnsureMinimumShopSlots()
        {
            int targetSlotCount = Mathf.Max(1, minimumRuntimeShopSlots);

            if (shopItems != null && shopItems.Length >= targetSlotCount)
            {
                return;
            }

            int existingCount = shopItems != null ? shopItems.Length : 0;
            ShopItem[] nextItems = new ShopItem[targetSlotCount];

            for (int i = 0; i < existingCount; i++)
            {
                nextItems[i] = shopItems[i];
            }

            for (int i = existingCount; i < targetSlotCount; i++)
            {
                nextItems[i] = CreateRuntimeShopSlot(i, targetSlotCount);
            }

            shopItems = nextItems;
            AlignShopSlots(targetSlotCount);
        }

        private ShopItem CreateRuntimeShopSlot(int slotIndex, int slotCount)
        {
            Transform root = ResolveRuntimeContentRoot();
            GameObject slotObject = new($"RuntimeShopItem{slotIndex + 1}");
            slotObject.transform.SetParent(root, false);
            slotObject.transform.localPosition = ResolveRuntimeSlotLocalPosition(slotIndex, slotCount);
            slotObject.transform.localScale = new Vector3(0.48f, 0.62f, 1f);

            SpriteRenderer bodyRenderer = slotObject.AddComponent<SpriteRenderer>();
            bodyRenderer.sprite = RuntimeShopIconFactory.GetShopItemBodySprite();
            bodyRenderer.sortingOrder = 18;

            ShopItemView itemView = slotObject.AddComponent<ShopItemView>();

            SpriteRenderer highlightRenderer = CreateRuntimeChildRenderer(slotObject.transform, "Highlight", RuntimeShopIconFactory.GetShopItemBodySprite(), 17, new Vector3(1.25f, 1.25f, 1f));
            SpriteRenderer soldRenderer = CreateRuntimeChildRenderer(slotObject.transform, "SoldOverlay", RuntimeShopIconFactory.GetShopItemBodySprite(), 19, new Vector3(1.05f, 1.05f, 1f));
            SpriteRenderer currencyRenderer = CreateRuntimeChildRenderer(slotObject.transform, "CurrencyMarker", RuntimeShopIconFactory.GetShopItemBodySprite(), 20, new Vector3(0.36f, 0.18f, 1f));
            currencyRenderer.transform.localPosition = new Vector3(0f, -0.82f, 0f);

            itemView.ConfigureRuntimeReferences(bodyRenderer, bodyRenderer, highlightRenderer, soldRenderer, currencyRenderer, true);

            ShopItem shopItem = slotObject.AddComponent<ShopItem>();
            shopItem.ConfigureRuntimeReferences(itemView, slotObject.transform);
            return shopItem;
        }

        private Transform ResolveRuntimeContentRoot()
        {
            if (_runtimeContentRoot != null)
            {
                return _runtimeContentRoot;
            }

            Transform existingRoot = transform.Find("RuntimeShopContent");
            if (existingRoot != null)
            {
                _runtimeContentRoot = existingRoot;
                return _runtimeContentRoot;
            }

            GameObject rootObject = new("RuntimeShopContent");
            rootObject.transform.SetParent(transform, false);
            _runtimeContentRoot = rootObject.transform;
            return _runtimeContentRoot;
        }

        private static SpriteRenderer CreateRuntimeChildRenderer(Transform parent, string objectName, Sprite sprite, int sortingOrder, Vector3 localScale)
        {
            GameObject child = new(objectName);
            child.transform.SetParent(parent, false);
            child.transform.localScale = localScale;

            SpriteRenderer renderer = child.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = sortingOrder;
            renderer.gameObject.SetActive(false);
            return renderer;
        }

        private Vector3 ResolveRuntimeSlotLocalPosition(int slotIndex, int slotCount)
        {
            float spacing = Mathf.Max(0.1f, shopSlotSpacing);
            float centeredIndex = slotIndex - ((slotCount - 1) * 0.5f);
            return shopSlotCenterLocalPosition + new Vector3(centeredIndex * spacing, 0f, 0f);
        }

        private void AlignShopSlots(int slotCount)
        {
            if (shopItems == null)
            {
                return;
            }

            int count = Mathf.Min(slotCount, shopItems.Length);
            for (int i = 0; i < count; i++)
            {
                if (shopItems[i] != null)
                {
                    shopItems[i].transform.localPosition = ResolveRuntimeSlotLocalPosition(i, count);
                    shopItems[i].ConfigureWorldPriceOnly();
                }
            }
        }

        private void EnsureRuntimeShopkeeper()
        {
            if (!createRuntimeShopkeeper || _runtimeShopkeeper != null || transform.Find("ShopkeeperNpc") != null)
            {
                return;
            }

            GameObject npcObject = new("ShopkeeperNpc");
            npcObject.transform.SetParent(transform, false);
            npcObject.transform.localPosition = runtimeShopkeeperLocalPosition;
            npcObject.transform.localScale = new Vector3(0.62f, 0.62f, 1f);

            SpriteRenderer renderer = npcObject.AddComponent<SpriteRenderer>();
            renderer.sprite = RuntimeShopIconFactory.GetShopkeeperSprite();
            renderer.sortingOrder = 16;
            _runtimeShopkeeper = npcObject;
        }

        private int ConfigureRandomItemSlots(ItemPoolData itemPool)
        {
            if (itemPool == null || shopItems == null || shopItems.Length == 0)
            {
                return 0;
            }

            int configuredCount = 0;
            int targetCount = Mathf.Min(3, shopItems.Length);

            for (int slotIndex = 0; slotIndex < targetCount; slotIndex++)
            {
                if (!TryGetShopItem(slotIndex, out ShopItem shopItem))
                {
                    continue;
                }

                if (!TrySelectShopItem(itemPool, out ItemData selectedItem))
                {
                    ClearShopSlot(shopItem);
                    continue;
                }

                int price = ResolveRandomItemOfferPrice(selectedItem);
                ShopItemData runtimeShopItemData = ShopItemData.CreateRuntimePassiveItemOffer(
                    selectedItem,
                    price,
                    ShopCurrencyType.Coins);

                ApplyRuntimeShopItem(shopItem, runtimeShopItemData);

                if (runtimeShopItemData != null)
                {
                    configuredCount++;
                    _selectedItemIds.Add(selectedItem.ItemId);
                    runItemPoolService?.RegisterOffer(selectedItem);
                }
            }

            return configuredCount;
        }

        private void ConfigureWeaponSlot(ItemPoolData itemPool, int slotIndex)
        {
            if (!TryGetShopItem(slotIndex, out ShopItem shopItem))
            {
                return;
            }

            if (itemPool == null || !TrySelectWeaponItem(itemPool, out ItemData selectedWeapon))
            {
                ClearShopSlot(shopItem);
                return;
            }

            ShopItemData runtimeShopItemData = ShopItemData.CreateRuntimePassiveItemOffer(
                selectedWeapon,
                weaponOfferPrice,
                ShopCurrencyType.Coins);

            ApplyRuntimeShopItem(shopItem, runtimeShopItemData);

            if (runtimeShopItemData != null)
            {
                _selectedItemIds.Add(selectedWeapon.ItemId);
                runItemPoolService?.RegisterOffer(selectedWeapon);
            }
        }

        private void ConfigureHealthSlot(int slotIndex)
        {
            if (!TryGetShopItem(slotIndex, out ShopItem shopItem))
            {
                return;
            }

            ShopItemData runtimeShopItemData = ShopItemData.CreateRuntimeHealthOffer(
                healthOfferAmount,
                healthOfferPrice,
                ShopCurrencyType.Coins);

            ApplyRuntimeShopItem(shopItem, runtimeShopItemData);
        }

        private void ConfigureAmmoSlot(int slotIndex)
        {
            if (!TryGetShopItem(slotIndex, out ShopItem shopItem))
            {
                return;
            }

            ShopItemData runtimeShopItemData = ShopItemData.CreateRuntimeAmmoOffer(
                ammoOfferAmount,
                ammoOfferPrice,
                ShopCurrencyType.Coins);

            ApplyRuntimeShopItem(shopItem, runtimeShopItemData);
        }

        private bool TrySelectWeaponItem(ItemPoolData itemPool, out ItemData selectedItem)
        {
            ItemPoolSelectionContext selectionContext = runItemPoolService != null
                ? runItemPoolService.BuildSelectionContext(RoomType.Shop, _selectedItemIds)
                : new ItemPoolSelectionContext(RoomType.Shop, 1, _selectedItemIds, null, null, null, null, null);

            return itemPool.TrySelectRandomWeaponItem(selectionContext, out selectedItem);
        }

        private bool TrySelectShopItem(ItemPoolData itemPool, out ItemData selectedItem)
        {
            ItemPoolSelectionContext selectionContext = runItemPoolService != null
                ? runItemPoolService.BuildSelectionContext(RoomType.Shop, _selectedItemIds)
                : new ItemPoolSelectionContext(RoomType.Shop, 1, _selectedItemIds, null, null, null, null, null);

            return itemPool.TrySelectRandomItem(selectionContext, out selectedItem);
        }

        private int ResolveRandomItemOfferPrice(ItemData itemData)
        {
            int minPrice = Mathf.Max(1, randomItemOfferPriceMin);
            int maxPrice = Mathf.Max(minPrice, randomItemOfferPriceMax);
            float rarityBonus = itemData != null ? (int)itemData.Rarity * 2f : 0f;
            return Mathf.Clamp(Mathf.RoundToInt(Random.Range(minPrice, maxPrice + 1) + rarityBonus), minPrice, maxPrice + 6);
        }

        private bool TryGetShopItem(int slotIndex, out ShopItem shopItem)
        {
            if (shopItems != null && slotIndex >= 0 && slotIndex < shopItems.Length)
            {
                shopItem = shopItems[slotIndex];
                return shopItem != null;
            }

            shopItem = null;
            return false;
        }

        private static void ApplyRuntimeShopItem(ShopItem shopItem, ShopItemData runtimeShopItemData)
        {
            if (shopItem == null)
            {
                return;
            }

            shopItem.gameObject.SetActive(runtimeShopItemData != null);
            shopItem.ConfigureShopItemData(runtimeShopItemData);
        }

        private static void ClearShopSlot(ShopItem shopItem)
        {
            if (shopItem == null)
            {
                return;
            }

            shopItem.ConfigureShopItemData(null);
            shopItem.gameObject.SetActive(false);
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
                case "RESTOCK AMMO":
                case "AMMO NOW":
                    score += offer.RewardType == ShopOfferRewardType.Ammo ? 18f : 0f;
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
                        ShopOfferRewardType.Ammo => 10f,
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
