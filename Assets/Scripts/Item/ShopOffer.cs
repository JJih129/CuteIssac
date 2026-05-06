using CuteIssac.Data.Item;
using UnityEngine;

namespace CuteIssac.Item
{
    /// <summary>
    /// Describes what a shop item grants when purchased.
    /// </summary>
    [System.Serializable]
    public struct ShopOffer
    {
        [SerializeField] private ShopOfferRewardType rewardType;
        [SerializeField] private ItemData passiveItem;
        [SerializeField] [Min(1)] private int resourceAmount;
        [SerializeField] [Min(0.5f)] private float healthAmount;
        [SerializeField] private GameObject pickupPrefabOverride;

        public ShopOfferRewardType RewardType => rewardType;
        public ItemData PassiveItem => passiveItem;
        public int ResourceAmount => Mathf.Max(1, resourceAmount);
        public float HealthAmount => Mathf.Max(0.5f, healthAmount);
        public GameObject PickupPrefabOverride => pickupPrefabOverride;

        public static ShopOffer CreatePassiveItemOffer(ItemData itemData)
        {
            ShopOffer offer = new ShopOffer
            {
                rewardType = ShopOfferRewardType.PassiveItem,
                passiveItem = itemData,
                resourceAmount = 1,
                healthAmount = 1f,
                pickupPrefabOverride = null
            };

            return offer;
        }

        public static ShopOffer CreateHealthOffer(float healthAmount)
        {
            return new ShopOffer
            {
                rewardType = ShopOfferRewardType.Health,
                passiveItem = null,
                resourceAmount = 1,
                healthAmount = Mathf.Max(0.5f, healthAmount),
                pickupPrefabOverride = null
            };
        }

        public static ShopOffer CreateAmmoOffer(int ammoAmount)
        {
            return new ShopOffer
            {
                rewardType = ShopOfferRewardType.Ammo,
                passiveItem = null,
                resourceAmount = Mathf.Max(1, ammoAmount),
                healthAmount = 1f,
                pickupPrefabOverride = null
            };
        }
    }
}
