using CuteIssac.Core.Audio;
using CuteIssac.Core.Pooling;
using CuteIssac.Core.Spawning;
using CuteIssac.Data.Item;
using CuteIssac.Player;
using UnityEngine;

namespace CuteIssac.Item
{
    /// <summary>
    /// Runtime shop slot.
    /// The slot owns purchase state while the sellable content stays in ShopItemData.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ShopItem : MonoBehaviour
    {
        [Header("Item Data")]
        [SerializeField] private ShopItemData shopItemData;
        [SerializeField] private Transform rewardSpawnAnchor;
        [SerializeField] private SpawnReusePolicy rewardSpawnReusePolicy = SpawnReusePolicy.Pooled;
        [SerializeField] [Min(0)] private int rewardPrewarmBufferCount = 1;

        [Header("Presentation")]
        [SerializeField] private ShopItemView shopItemView;

        private bool _isSold;

        public ShopItemData ShopItemData => shopItemData;
        public int Price => shopItemData != null ? shopItemData.Price : 0;
        public ShopCurrencyType CurrencyType => shopItemData != null ? shopItemData.CurrencyType : ShopCurrencyType.Coins;
        public bool IsSold => _isSold;

        public void ConfigureShopItemData(ShopItemData runtimeShopItemData)
        {
            shopItemData = runtimeShopItemData;
            _isSold = false;
        }

        public bool CanPurchase(PlayerInventory playerInventory, PlayerItemManager playerItemManager, PlayerHealth playerHealth)
        {
            if (_isSold || shopItemData == null || playerInventory == null)
            {
                return false;
            }

            if (!CanAfford(playerInventory))
            {
                return false;
            }

            ShopOffer offer = shopItemData.Offer;

            return offer.RewardType switch
            {
                ShopOfferRewardType.PassiveItem => playerItemManager != null
                    && offer.PassiveItem != null
                    && !playerInventory.Contains(offer.PassiveItem),
                ShopOfferRewardType.Health => playerHealth != null && playerHealth.CurrentHealth < playerHealth.MaxHealth,
                ShopOfferRewardType.Coins => true,
                ShopOfferRewardType.Keys => true,
                ShopOfferRewardType.Bombs => true,
                _ => false
            };
        }

        public bool CanAfford(PlayerInventory playerInventory)
        {
            if (playerInventory == null || shopItemData == null)
            {
                return false;
            }

            int price = shopItemData.Price;

            return shopItemData.CurrencyType switch
            {
                ShopCurrencyType.Keys => playerInventory.Keys >= price,
                ShopCurrencyType.Bombs => playerInventory.Bombs >= price,
                _ => playerInventory.Coins >= price
            };
        }

        public bool TryPurchase(PlayerInventory playerInventory, PlayerItemManager playerItemManager, PlayerHealth playerHealth)
        {
            if (!CanPurchase(playerInventory, playerItemManager, playerHealth))
            {
                return false;
            }

            int price = shopItemData.Price;

            bool spent = shopItemData.CurrencyType switch
            {
                ShopCurrencyType.Keys => playerInventory.TrySpendKeys(price),
                ShopCurrencyType.Bombs => playerInventory.TrySpendBombs(price),
                _ => playerInventory.TrySpendCoins(price)
            };

            if (!spent)
            {
                return false;
            }

            if (!TryDeliverReward(playerInventory, playerItemManager, playerHealth))
            {
                Refund(playerInventory);
                return false;
            }

            _isSold = true;
            GameAudioEvents.Raise(GameAudioEventType.ShopPurchased, transform.position);
            RefreshView(false, playerInventory);
            return true;
        }

        public void RefreshView(bool isHighlighted, PlayerInventory playerInventory, PlayerItemManager playerItemManager = null, PlayerHealth playerHealth = null)
        {
            if (shopItemView != null)
            {
                shopItemView.Present(shopItemData, CanPurchase(playerInventory, playerItemManager, playerHealth), isHighlighted, _isSold);
            }
        }

        public void PlayPurchaseSuccessFeedback()
        {
            shopItemView?.PlayPurchaseSuccess();
        }

        public ShopSlotState BuildSlotState(bool isHighlighted, PlayerInventory playerInventory, PlayerItemManager playerItemManager, PlayerHealth playerHealth)
        {
            bool isVisible = shopItemData != null;
            bool canPurchase = CanPurchase(playerInventory, playerItemManager, playerHealth);
            string displayName = isVisible ? shopItemData.DisplayName : string.Empty;
            string priceLabel = isVisible ? $"{GetCurrencyLabel(shopItemData.CurrencyType)} {shopItemData.Price}" : string.Empty;
            string statusLabel = _isSold
                ? "판매 완료"
                : (canPurchase ? "구매 가능" : ResolveUnavailableReason(playerInventory, playerItemManager, playerHealth));

            return new ShopSlotState(
                displayName,
                priceLabel,
                statusLabel,
                isVisible ? shopItemData.Icon : null,
                isVisible ? shopItemData.CurrencyType : ShopCurrencyType.Coins,
                isVisible,
                canPurchase,
                _isSold,
                isHighlighted);
        }

        private bool TryDeliverReward(PlayerInventory playerInventory, PlayerItemManager playerItemManager, PlayerHealth playerHealth)
        {
            if (shopItemData == null)
            {
                return false;
            }

            if (shopItemData.DeliveryMode == ShopDeliveryMode.SpawnPickup)
            {
                return TrySpawnPickupReward();
            }

            ShopOffer offer = shopItemData.Offer;

            switch (offer.RewardType)
            {
                case ShopOfferRewardType.PassiveItem:
                    return playerItemManager != null
                        && offer.PassiveItem != null
                        && playerItemManager.AcquirePassiveItem(offer.PassiveItem);
                case ShopOfferRewardType.Health:
                    return playerHealth != null && playerHealth.RestoreHealth(offer.HealthAmount);
                case ShopOfferRewardType.Keys:
                    playerInventory.AddKeys(offer.ResourceAmount);
                    return true;
                case ShopOfferRewardType.Bombs:
                    playerInventory.AddBombs(offer.ResourceAmount);
                    return true;
                case ShopOfferRewardType.Coins:
                    playerInventory.AddCoins(offer.ResourceAmount);
                    return true;
                default:
                    return false;
            }
        }

        private bool TrySpawnPickupReward()
        {
            if (shopItemData == null)
            {
                return false;
            }

            GameObject pickupPrefab = shopItemData.Offer.PickupPrefabOverride;

            if (pickupPrefab == null)
            {
                return false;
            }

            if (rewardSpawnReusePolicy == SpawnReusePolicy.Pooled)
            {
                PrefabPoolService.Prewarm(
                    pickupPrefab,
                    Mathf.Max(1, 1 + rewardPrewarmBufferCount));
            }

            Vector3 spawnPosition = rewardSpawnAnchor != null ? rewardSpawnAnchor.position : transform.position;
            Quaternion spawnRotation = rewardSpawnAnchor != null ? rewardSpawnAnchor.rotation : Quaternion.identity;
            GameObject rewardObject = GameplaySpawnFactory.SpawnGameObject(
                pickupPrefab,
                spawnPosition,
                spawnRotation,
                null,
                rewardSpawnReusePolicy);
            return rewardObject != null;
        }

        private void Refund(PlayerInventory playerInventory)
        {
            if (playerInventory == null)
            {
                return;
            }

            int price = shopItemData != null ? shopItemData.Price : 0;

            switch (shopItemData != null ? shopItemData.CurrencyType : ShopCurrencyType.Coins)
            {
                case ShopCurrencyType.Keys:
                    playerInventory.AddKeys(price);
                    break;
                case ShopCurrencyType.Bombs:
                    playerInventory.AddBombs(price);
                    break;
                default:
                    playerInventory.AddCoins(price);
                    break;
            }
        }

        private string ResolveUnavailableReason(PlayerInventory playerInventory, PlayerItemManager playerItemManager, PlayerHealth playerHealth)
        {
            if (shopItemData == null)
            {
                return string.Empty;
            }

            if (!CanAfford(playerInventory))
            {
                return shopItemData.CurrencyType switch
                {
                    ShopCurrencyType.Keys => "열쇠 부족",
                    ShopCurrencyType.Bombs => "폭탄 부족",
                    _ => "코인 부족"
                };
            }

            ShopOffer offer = shopItemData.Offer;

            return offer.RewardType switch
            {
                ShopOfferRewardType.PassiveItem when playerInventory != null && offer.PassiveItem != null && playerInventory.Contains(offer.PassiveItem) => "이미 보유",
                ShopOfferRewardType.Health when playerHealth != null && playerHealth.CurrentHealth >= playerHealth.MaxHealth => "체력 가득",
                _ => "구매 불가"
            };
        }

        private static string GetCurrencyLabel(ShopCurrencyType currencyType)
        {
            return currencyType switch
            {
                ShopCurrencyType.Keys => "열쇠",
                ShopCurrencyType.Bombs => "폭탄",
                _ => "코인"
            };
        }

        private void Reset()
        {
            shopItemView = GetComponent<ShopItemView>();
            rewardSpawnAnchor = transform;
        }

        private void OnValidate()
        {
            if (shopItemView == null)
            {
                shopItemView = GetComponent<ShopItemView>();
            }

            if (rewardSpawnAnchor == null)
            {
                rewardSpawnAnchor = transform;
            }
        }
    }
}
