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

            string runtimeDescription = passiveItem.IsWeaponRelic && string.IsNullOrWhiteSpace(passiveItem.Description)
                ? passiveItem.BuildWeaponPickupSummary()
                : passiveItem.Description;
            return CreateRuntimeOffer(
                $"RuntimeShop_{passiveItem.ItemId}",
                $"runtime_shop_{passiveItem.ItemId}",
                passiveItem.DisplayName,
                runtimeDescription,
                passiveItem.Icon,
                runtimePrice,
                runtimeCurrencyType,
                ShopDeliveryMode.Immediate,
                ShopOffer.CreatePassiveItemOffer(passiveItem));
        }

        public static ShopItemData CreateRuntimeHealthOffer(float healthAmount, int runtimePrice, ShopCurrencyType runtimeCurrencyType)
        {
            return CreateRuntimeOffer(
                "RuntimeShop_Health",
                $"runtime_shop_health_{Mathf.Max(0.5f, healthAmount):0.#}",
                "Heart Cache",
                $"+{Mathf.Max(0.5f, healthAmount):0.#} HP",
                RuntimeShopIconFactory.GetHeartSprite(),
                runtimePrice,
                runtimeCurrencyType,
                ShopDeliveryMode.Immediate,
                ShopOffer.CreateHealthOffer(healthAmount));
        }

        public static ShopItemData CreateRuntimeAmmoOffer(int ammoAmount, int runtimePrice, ShopCurrencyType runtimeCurrencyType)
        {
            return CreateRuntimeOffer(
                "RuntimeShop_Ammo",
                $"runtime_shop_ammo_{Mathf.Max(1, ammoAmount)}",
                "Ammo Cache",
                $"+{Mathf.Max(1, ammoAmount)} AMMO",
                RuntimeShopIconFactory.GetAmmoSprite(),
                runtimePrice,
                runtimeCurrencyType,
                ShopDeliveryMode.Immediate,
                ShopOffer.CreateAmmoOffer(ammoAmount));
        }

        public static ShopItemData CreateRuntimeSpeedHeartOffer(int speedHeartAmount, int runtimePrice, ShopCurrencyType runtimeCurrencyType)
        {
            return CreateRuntimeOffer(
                "RuntimeShop_SpeedHeart",
                $"runtime_shop_speed_heart_{Mathf.Max(1, speedHeartAmount)}",
                "Speed Heart",
                $"Stores {Mathf.Max(1, speedHeartAmount)} hit-trigger speed buff",
                RuntimeShopIconFactory.GetSpeedCandySprite(),
                runtimePrice,
                runtimeCurrencyType,
                ShopDeliveryMode.Immediate,
                ShopOffer.CreateSpeedHeartOffer(speedHeartAmount));
        }

        private static ShopItemData CreateRuntimeOffer(
            string runtimeName,
            string runtimeOfferId,
            string displayName,
            string description,
            Sprite icon,
            int runtimePrice,
            ShopCurrencyType runtimeCurrencyType,
            ShopDeliveryMode runtimeDeliveryMode,
            ShopOffer runtimeOffer)
        {
            ShopItemData runtimeItemData = CreateInstance<ShopItemData>();
            runtimeItemData.name = runtimeName;
            runtimeItemData.offerId = runtimeOfferId;
            runtimeItemData.displayName = displayName;
            runtimeItemData.description = description;
            runtimeItemData.icon = icon;
            runtimeItemData.price = Mathf.Max(1, runtimePrice);
            runtimeItemData.currencyType = runtimeCurrencyType;
            runtimeItemData.deliveryMode = runtimeDeliveryMode;
            runtimeItemData.offer = runtimeOffer;
            return runtimeItemData;
        }
    }
}
