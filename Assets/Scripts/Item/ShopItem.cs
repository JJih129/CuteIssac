using CuteIssac.Core.Audio;
using CuteIssac.Core.Gameplay;
using CuteIssac.Core.Meta;
using CuteIssac.Core.Pooling;
using CuteIssac.Core.Spawning;
using CuteIssac.Data.Dungeon;
using CuteIssac.Data.Item;
using CuteIssac.Data.Visual;
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
        private RoomType _specialDealRoomType;
        private SpecialRoomDealType _specialDealType;
        private string _specialDealRuleId;

        public ShopItemData ShopItemData => shopItemData;
        public int Price => shopItemData != null ? shopItemData.Price : 0;
        public ShopCurrencyType CurrencyType => shopItemData != null ? shopItemData.CurrencyType : ShopCurrencyType.Coins;
        public bool IsSold => _isSold;
        public bool HasUnlockedOffer => shopItemData == null ||
            UnlockManager.IsUnlocked(shopItemData.UnlockKey, shopItemData.UnlockedByDefault);

        public void ConfigureShopItemData(ShopItemData runtimeShopItemData)
        {
            shopItemData = runtimeShopItemData;
            _isSold = false;
            ClearSpecialDealContext();
        }

        public void ConfigureSpecialDealContext(RoomType roomType, SpecialRoomDealType dealType, string ruleId)
        {
            _specialDealRoomType = roomType;
            _specialDealType = dealType;
            _specialDealRuleId = ruleId ?? string.Empty;
        }

        public void ConfigureRuntimeReferences(ShopItemView runtimeShopItemView, Transform runtimeRewardSpawnAnchor)
        {
            shopItemView = runtimeShopItemView;
            rewardSpawnAnchor = runtimeRewardSpawnAnchor != null ? runtimeRewardSpawnAnchor : transform;
        }

        public void ConfigureVisualProfile(ShopItemVisualProfile visualProfile)
        {
            if (shopItemView == null)
            {
                shopItemView = GetComponent<ShopItemView>();
            }

            shopItemView?.ConfigureVisualProfile(visualProfile);
        }

        public void ConfigureWorldPriceOnly()
        {
            if (shopItemView == null)
            {
                shopItemView = GetComponent<ShopItemView>();
            }

            shopItemView?.ConfigureWorldPriceOnly();
        }

        public bool CanPurchase(PlayerInventory playerInventory, PlayerItemManager playerItemManager, PlayerHealth playerHealth)
        {
            if (_isSold || shopItemData == null || playerInventory == null)
            {
                return false;
            }

            if (!HasUnlockedOffer)
            {
                return false;
            }

            if (!CanAfford(playerInventory, playerItemManager, playerHealth))
            {
                return false;
            }

            ShopOffer offer = shopItemData.Offer;

            return offer.RewardType switch
            {
                ShopOfferRewardType.PassiveItem => playerItemManager != null
                    && offer.PassiveItem != null
                    && !playerItemManager.OwnsItem(offer.PassiveItem),
                ShopOfferRewardType.Health => playerHealth != null && playerHealth.CurrentHealth < playerHealth.MaxHealth,
                ShopOfferRewardType.Ammo => CanReceiveAmmo(playerInventory, playerItemManager, playerHealth),
                ShopOfferRewardType.SpeedHeart => playerHealth != null && playerHealth.CanReceiveSpeedHeart(offer.ResourceAmount),
                ShopOfferRewardType.Coins => true,
                ShopOfferRewardType.Keys => true,
                ShopOfferRewardType.Bombs => true,
                _ => false
            };
        }

        public bool CanAfford(PlayerInventory playerInventory, PlayerItemManager playerItemManager = null, PlayerHealth playerHealth = null)
        {
            if (playerInventory == null || shopItemData == null)
            {
                return false;
            }

            int price = ResolveEffectivePrice(playerItemManager);

            if (price <= 0)
            {
                return true;
            }

            return shopItemData.CurrencyType switch
            {
                ShopCurrencyType.Keys => playerInventory.Keys >= price,
                ShopCurrencyType.Bombs => playerInventory.Bombs >= price,
                ShopCurrencyType.Health => playerHealth != null && playerHealth.CurrentHealth > price,
                _ => playerInventory.Coins >= price
            };
        }

        public bool TryPurchase(PlayerInventory playerInventory, PlayerItemManager playerItemManager, PlayerHealth playerHealth)
        {
            if (!CanPurchase(playerInventory, playerItemManager, playerHealth))
            {
                return false;
            }

            int price = ResolveEffectivePrice(playerItemManager);
            bool spent = price <= 0 || (shopItemData.CurrencyType switch
            {
                ShopCurrencyType.Keys => playerInventory.TrySpendKeys(price),
                ShopCurrencyType.Bombs => playerInventory.TrySpendBombs(price),
                ShopCurrencyType.Health => playerHealth != null && playerHealth.TrySpendHealth(price, transform, false),
                _ => playerInventory.TrySpendCoins(price)
            });

            if (!spent)
            {
                return false;
            }

            if (!TryDeliverReward(playerInventory, playerItemManager, playerHealth))
            {
                Refund(playerInventory, playerHealth, price);
                return false;
            }

            _isSold = true;
            RaiseSpecialDealPurchased(price);
            GameAudioEvents.Raise(GameAudioEventType.ShopPurchased, transform.position);
            RefreshView(false, playerInventory, playerItemManager, playerHealth);
            return true;
        }

        public void RefreshView(bool isHighlighted, PlayerInventory playerInventory, PlayerItemManager playerItemManager = null, PlayerHealth playerHealth = null)
        {
            if (shopItemView != null)
            {
                shopItemView.Present(
                    shopItemData,
                    ResolveEffectivePrice(playerItemManager),
                    CanPurchase(playerInventory, playerItemManager, playerHealth),
                    isHighlighted,
                    _isSold);
            }
        }

        public void PlayPurchaseSuccessFeedback()
        {
            shopItemView?.PlayPurchaseSuccess();
        }

        public void PlayPurchaseFailureFeedback()
        {
            shopItemView?.PlayPurchaseFailure();
        }

        public bool IsBuyerWithinInteractionBounds(Vector3 buyerPosition, float fallbackDistance, float boundsPadding, out float distanceSqr)
        {
            if (shopItemView == null)
            {
                shopItemView = GetComponent<ShopItemView>();
            }

            if (shopItemView != null && shopItemView.TryGetInteractionDistanceSqr(buyerPosition, out distanceSqr))
            {
                float paddingSqr = boundsPadding * boundsPadding;
                return distanceSqr <= paddingSqr;
            }

            distanceSqr = (transform.position - buyerPosition).sqrMagnitude;
            float fallbackDistanceSqr = fallbackDistance * fallbackDistance;
            return distanceSqr <= fallbackDistanceSqr;
        }

        public bool TryGetInteractionBounds(out Bounds bounds)
        {
            if (shopItemView == null)
            {
                shopItemView = GetComponent<ShopItemView>();
            }

            if (shopItemView != null && shopItemView.TryGetInteractionBounds(out bounds))
            {
                return true;
            }

            bounds = default;
            return false;
        }

        public ShopSlotState BuildSlotState(bool isHighlighted, PlayerInventory playerInventory, PlayerItemManager playerItemManager, PlayerHealth playerHealth)
        {
            bool isVisible = shopItemData != null;
            bool canPurchase = CanPurchase(playerInventory, playerItemManager, playerHealth);
            int effectivePrice = ResolveEffectivePrice(playerItemManager);
            string displayName = isVisible ? shopItemData.DisplayName : string.Empty;
            string basePriceLabel = isVisible
                ? ShopPriceLabelFormatter.FormatPrice(effectivePrice, shopItemData.CurrencyType, ShopPriceLabelStyle.Ui)
                : string.Empty;
            string priceLabel = isVisible ? ResolvePresentationPriceLabel(basePriceLabel, effectivePrice) : string.Empty;
            string statusLabel = _isSold
                ? "판매 완료"
                : (canPurchase ? "구매 가능" : ResolveUnavailableReason(playerInventory, playerItemManager, playerHealth));

            statusLabel = _isSold
                ? "Sold"
                : (canPurchase ? "Ready" : ResolveUnavailableReason(playerInventory, playerItemManager, playerHealth));

            return new ShopSlotState(
                displayName,
                priceLabel,
                statusLabel,
                isVisible ? shopItemData.ShopDisplaySprite : null,
                isVisible ? shopItemData.CurrencyType : ShopCurrencyType.Coins,
                isVisible,
                canPurchase,
                _isSold,
                isHighlighted,
                _specialDealType,
                ResolveDealLabel(_specialDealType));
        }

        private string ResolvePresentationPriceLabel(string fallbackPriceLabel, int effectivePrice)
        {
            if (_specialDealType == SpecialRoomDealType.None || shopItemData == null)
            {
                return fallbackPriceLabel;
            }

            return _specialDealType switch
            {
                SpecialRoomDealType.Devil => $"Blood price: {effectivePrice} HP",
                SpecialRoomDealType.Angel => $"Vow cost: {effectivePrice} HP",
                SpecialRoomDealType.BlackMarket => $"Black price: {fallbackPriceLabel}",
                _ => fallbackPriceLabel
            };
        }

        private static string ResolveDealLabel(SpecialRoomDealType dealType)
        {
            return dealType switch
            {
                SpecialRoomDealType.Devil => "Devil Deal",
                SpecialRoomDealType.Angel => "Angel Deal",
                SpecialRoomDealType.BlackMarket => "Black Market",
                _ => string.Empty
            };
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
                case ShopOfferRewardType.Ammo:
                    PlayerWeaponLoadout ammoLoadout = ResolveWeaponLoadout(playerInventory, playerHealth, playerItemManager);
                    return ammoLoadout != null && ammoLoadout.TryAddAmmo(offer.ResourceAmount);
                case ShopOfferRewardType.SpeedHeart:
                    return playerHealth != null && playerHealth.TryGrantSpeedHeart(
                        offer.ResourceAmount,
                        10f,
                        1.2f,
                        PlayerSpeedBuffState.DuplicateBuffPolicy.RefreshDuration,
                        RuntimeShopIconFactory.GetSpeedCandySprite(),
                        "Speed Heart");
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
                PrefabPoolService.EnsurePrewarmed(pickupPrefab, Mathf.Max(1, 1 + rewardPrewarmBufferCount));
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

        private void ClearSpecialDealContext()
        {
            _specialDealRoomType = RoomType.Normal;
            _specialDealType = SpecialRoomDealType.None;
            _specialDealRuleId = string.Empty;
        }

        private void RaiseSpecialDealPurchased(int price)
        {
            if (_specialDealType == SpecialRoomDealType.None)
            {
                return;
            }

            ItemData itemData = shopItemData != null ? shopItemData.Offer.PassiveItem : null;
            GameplayRuntimeEvents.RaiseSpecialRoomDealPurchased(new SpecialRoomDealPurchasedSignal(
                _specialDealType,
                _specialDealRoomType,
                _specialDealRuleId,
                this,
                itemData,
                price));
        }

        private void Refund(PlayerInventory playerInventory, PlayerHealth playerHealth, int price)
        {
            if (playerInventory == null || price <= 0)
            {
                return;
            }

            switch (shopItemData != null ? shopItemData.CurrencyType : ShopCurrencyType.Coins)
            {
                case ShopCurrencyType.Keys:
                    playerInventory.AddKeys(price);
                    break;
                case ShopCurrencyType.Bombs:
                    playerInventory.AddBombs(price);
                    break;
                case ShopCurrencyType.Health:
                    playerHealth?.RestoreHealth(price);
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

            string englishReason = ResolveUnavailableReasonEnglish(playerInventory, playerItemManager, playerHealth);
            if (!string.IsNullOrWhiteSpace(englishReason))
            {
                return englishReason;
            }

            if (!CanAfford(playerInventory, playerItemManager, playerHealth))
            {
                if (shopItemData.CurrencyType == ShopCurrencyType.Health)
                {
                    return "HP NEEDED";
                }

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
                ShopOfferRewardType.PassiveItem when playerItemManager != null && offer.PassiveItem != null && playerItemManager.OwnsItem(offer.PassiveItem) => "이미 보유",
                ShopOfferRewardType.Health when playerHealth != null && playerHealth.CurrentHealth >= playerHealth.MaxHealth => "체력 가득",
                ShopOfferRewardType.Ammo when ResolveWeaponLoadout(playerInventory, playerHealth, playerItemManager) == null => "무기 없음",
                ShopOfferRewardType.Ammo when !CanReceiveAmmo(playerInventory, playerItemManager, playerHealth) => "탄약 가득",
                ShopOfferRewardType.SpeedHeart when playerHealth != null && !playerHealth.CanReceiveSpeedHeart(offer.ResourceAmount) => "스피드 하트 가득",
                _ => "구매 불가"
            };
        }

        private string ResolveUnavailableReasonEnglish(PlayerInventory playerInventory, PlayerItemManager playerItemManager, PlayerHealth playerHealth)
        {
            if (shopItemData == null)
            {
                return string.Empty;
            }

            if (!HasUnlockedOffer)
            {
                return "Locked";
            }

            if (!CanAfford(playerInventory, playerItemManager, playerHealth))
            {
                return shopItemData.CurrencyType switch
                {
                    ShopCurrencyType.Keys => "Keys needed",
                    ShopCurrencyType.Bombs => "Bombs needed",
                    ShopCurrencyType.Health => "HP needed",
                    _ => "Coins needed"
                };
            }

            ShopOffer offer = shopItemData.Offer;
            return offer.RewardType switch
            {
                ShopOfferRewardType.PassiveItem when playerItemManager != null && offer.PassiveItem != null && playerItemManager.OwnsItem(offer.PassiveItem) => "Owned",
                ShopOfferRewardType.Health when playerHealth != null && playerHealth.CurrentHealth >= playerHealth.MaxHealth => "Full HP",
                ShopOfferRewardType.Ammo when ResolveWeaponLoadout(playerInventory, playerHealth, playerItemManager) == null => "No weapon",
                ShopOfferRewardType.Ammo when !CanReceiveAmmo(playerInventory, playerItemManager, playerHealth) => "Ammo full",
                ShopOfferRewardType.SpeedHeart when playerHealth != null && !playerHealth.CanReceiveSpeedHeart(offer.ResourceAmount) => "Speed full",
                _ => string.Empty
            };
        }

        private int ResolveEffectivePrice(PlayerItemManager playerItemManager)
        {
            if (shopItemData == null)
            {
                return 0;
            }

            if (playerItemManager == null)
            {
                return shopItemData.Price;
            }

            return playerItemManager.ResolveEffectiveShopPrice(shopItemData.Price, shopItemData.CurrencyType);
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

        private static bool CanReceiveAmmo(PlayerInventory playerInventory, PlayerItemManager playerItemManager, PlayerHealth playerHealth)
        {
            PlayerWeaponLoadout weaponLoadout = ResolveWeaponLoadout(playerInventory, playerHealth, playerItemManager);
            return weaponLoadout != null && weaponLoadout.CanReceiveAmmoPickup();
        }

        private static PlayerWeaponLoadout ResolveWeaponLoadout(PlayerInventory playerInventory, PlayerHealth playerHealth, PlayerItemManager playerItemManager)
        {
            if (PlayerRegistry.TryResolveActiveWeaponLoadoutFor(playerInventory, playerHealth, playerItemManager, out PlayerWeaponLoadout activeWeaponLoadout))
            {
                return activeWeaponLoadout;
            }

            if (playerItemManager != null && playerItemManager.TryGetComponent(out PlayerWeaponLoadout itemManagerLoadout))
            {
                return itemManagerLoadout;
            }

            if (playerInventory != null && playerInventory.TryGetComponent(out PlayerWeaponLoadout inventoryLoadout))
            {
                return inventoryLoadout;
            }

            if (playerHealth != null && playerHealth.TryGetComponent(out PlayerWeaponLoadout healthLoadout))
            {
                return healthLoadout;
            }

            return null;
        }
    }
}
