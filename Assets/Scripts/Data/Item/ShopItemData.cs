using CuteIssac.Item;
using UnityEngine;

namespace CuteIssac.Data.Item
{
    /// <summary>
    /// Authoring asset for a shop entry.
    /// The shop logic only consumes this data and stays independent from prefab names or room setup details.
    /// </summary>
    [CreateAssetMenu(fileName = "ShopItemData", menuName = "CuteIssac/Data/Item/Shop Item Data")]
    public sealed class ShopItemData : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string offerId = "shop_offer";
        [SerializeField] private string displayName = "Shop Offer";
        [SerializeField] [TextArea] private string description;
        [SerializeField] private Sprite icon;

        [Header("Price")]
        [SerializeField] [Min(1)] private int price = 5;
        [SerializeField] private ShopCurrencyType currencyType = ShopCurrencyType.Coins;

        [Header("Reward")]
        [SerializeField] private ShopDeliveryMode deliveryMode = ShopDeliveryMode.Immediate;
        [SerializeField] private ShopOffer offer;

        public string OfferId => offerId;
        public string DisplayName => displayName;
        public string Description => description;
        public Sprite Icon => icon;
        public int Price => Mathf.Max(1, price);
        public ShopCurrencyType CurrencyType => currencyType;
        public ShopDeliveryMode DeliveryMode => deliveryMode;
        public ShopOffer Offer => offer;

        public static ShopItemData CreateRuntimePassiveItemOffer(ItemData passiveItem, int runtimePrice, ShopCurrencyType runtimeCurrencyType)
        {
            if (passiveItem == null)
            {
                return null;
            }

            ShopItemData runtimeItemData = CreateInstance<ShopItemData>();
            runtimeItemData.name = $"RuntimeShop_{passiveItem.ItemId}";
            runtimeItemData.offerId = $"runtime_shop_{passiveItem.ItemId}";
            runtimeItemData.displayName = passiveItem.DisplayName;
            runtimeItemData.description = passiveItem.Description;
            runtimeItemData.icon = passiveItem.Icon;
            runtimeItemData.price = Mathf.Max(1, runtimePrice);
            runtimeItemData.currencyType = runtimeCurrencyType;
            runtimeItemData.deliveryMode = ShopDeliveryMode.Immediate;
            runtimeItemData.offer = ShopOffer.CreatePassiveItemOffer(passiveItem);
            return runtimeItemData;
        }
    }
}
