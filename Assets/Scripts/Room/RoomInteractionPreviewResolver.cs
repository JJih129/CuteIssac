using System.Collections.Generic;
using System.Text;
using CuteIssac.Combat;
using CuteIssac.Common.Stats;
using CuteIssac.Data.Item;
using CuteIssac.Item;
using CuteIssac.Player;
using UnityEngine;

namespace CuteIssac.Room
{
    /// <summary>
    /// Resolves a concise player-side preview for the currently actionable room target.
    /// The preview stays short and judgment-heavy so the player can decide quickly.
    /// </summary>
    public static class RoomInteractionPreviewResolver
    {
        private static readonly ModifierStack ModifierStack = new();
        private static readonly List<string> SummaryChunks = new();

        public static bool TryResolve(
            Transform target,
            RoomController roomController,
            PlayerInventory playerInventory,
            PlayerItemManager playerItemManager,
            PlayerHealth playerHealth,
            PlayerStats playerStats,
            PlayerTrinketHolder playerTrinketHolder,
            PlayerActiveItemController playerActiveItemController,
            PlayerConsumableHolder playerConsumableHolder,
            out string title,
            out string detail,
            out Color accentColor,
            out string compareLabel,
            out Color compareColor,
            out bool emphasize)
        {
            title = string.Empty;
            detail = string.Empty;
            accentColor = RoomTraversalGuidanceController.ResolveRoomAccent(roomController != null
                ? roomController.RoomType
                : Data.Dungeon.RoomType.Normal);
            compareLabel = string.Empty;
            compareColor = accentColor;
            emphasize = false;

            if (target == null)
            {
                return false;
            }

            ShopItem shopItem = target.GetComponent<ShopItem>();
            if (shopItem == null)
            {
                shopItem = target.GetComponentInChildren<ShopItem>(true);
            }

            if (shopItem != null && shopItem.ShopItemData != null)
            {
                return TryResolveShopPreview(
                    shopItem,
                    playerInventory,
                    playerItemManager,
                    playerHealth,
                    playerStats,
                    out title,
                    out detail,
                    out accentColor,
                    out compareLabel,
                    out compareColor,
                    out emphasize);
            }

            BasePickupLogic pickupLogic = target.GetComponent<BasePickupLogic>();
            if (pickupLogic == null)
            {
                pickupLogic = target.GetComponentInChildren<BasePickupLogic>(true);
            }

            if (pickupLogic == null)
            {
                return false;
            }

            return TryResolvePickupPreview(
                pickupLogic,
                playerInventory,
                playerHealth,
                playerStats,
                playerTrinketHolder,
                playerActiveItemController,
                playerConsumableHolder,
                out title,
                out detail,
                out accentColor,
                out compareLabel,
                out compareColor,
                out emphasize);
        }

        public static bool TryResolvePriority(
            Transform target,
            RoomController roomController,
            PlayerInventory playerInventory,
            PlayerItemManager playerItemManager,
            PlayerHealth playerHealth,
            PlayerStats playerStats,
            PlayerTrinketHolder playerTrinketHolder,
            PlayerActiveItemController playerActiveItemController,
            PlayerConsumableHolder playerConsumableHolder,
            out float priorityScore,
            out string compareLabel,
            out Color compareColor)
        {
            priorityScore = float.NegativeInfinity;
            compareLabel = string.Empty;
            compareColor = Color.white;

            if (!TryResolve(
                    target,
                    roomController,
                    playerInventory,
                    playerItemManager,
                    playerHealth,
                    playerStats,
                    playerTrinketHolder,
                    playerActiveItemController,
                    playerConsumableHolder,
                    out _,
                    out _,
                    out _,
                    out compareLabel,
                    out compareColor,
                    out bool emphasize))
            {
                return false;
            }

            priorityScore = ResolvePreviewPriorityScore(target, compareLabel, emphasize);
            return true;
        }

        public static bool TryResolveOutcomePreview(
            Transform target,
            PlayerInventory playerInventory,
            PlayerItemManager playerItemManager,
            PlayerHealth playerHealth,
            PlayerStats playerStats,
            out string outcomeLabel,
            out Color outcomeColor)
        {
            outcomeLabel = string.Empty;
            outcomeColor = Color.white;

            if (target == null)
            {
                return false;
            }

            ShopItem shopItem = target.GetComponent<ShopItem>();
            if (shopItem == null)
            {
                shopItem = target.GetComponentInChildren<ShopItem>(true);
            }

            if (shopItem != null && shopItem.ShopItemData != null)
            {
                return TryResolveShopOutcomePreview(
                    shopItem,
                    playerInventory,
                    playerItemManager,
                    playerHealth,
                    playerStats,
                    out outcomeLabel,
                    out outcomeColor);
            }

            BasePickupLogic pickupLogic = target.GetComponent<BasePickupLogic>();
            if (pickupLogic == null)
            {
                pickupLogic = target.GetComponentInChildren<BasePickupLogic>(true);
            }

            if (pickupLogic == null)
            {
                return false;
            }

            return TryResolvePickupOutcomePreview(
                pickupLogic,
                playerHealth,
                playerInventory,
                playerStats,
                out outcomeLabel,
                out outcomeColor);
        }

        private static bool TryResolveShopPreview(
            ShopItem shopItem,
            PlayerInventory playerInventory,
            PlayerItemManager playerItemManager,
            PlayerHealth playerHealth,
            PlayerStats playerStats,
            out string title,
            out string detail,
            out Color accentColor,
            out string compareLabel,
            out Color compareColor,
            out bool emphasize)
        {
            title = string.Empty;
            detail = string.Empty;
            accentColor = ResolveShopRewardAccent(shopItem);
            compareLabel = string.Empty;
            compareColor = accentColor;
            emphasize = false;

            if (shopItem == null || shopItem.ShopItemData == null)
            {
                return false;
            }

            bool canPurchase = shopItem.CanPurchase(playerInventory, playerItemManager, playerHealth);
            title = shopItem.ShopItemData.DisplayName.ToUpperInvariant();
            detail = ResolveShopRewardDetail(shopItem);
            compareLabel = ResolveShopCompareLabel(shopItem, canPurchase, playerInventory, playerItemManager, playerHealth, playerStats);
            compareColor = canPurchase
                ? Color.Lerp(accentColor, Color.white, 0.2f)
                : ResolveUnavailableCompareColor(shopItem);

            if (!canPurchase)
            {
                accentColor = Color.Lerp(accentColor, new Color(1f, 0.44f, 0.42f, 1f), 0.42f);
            }

            emphasize = canPurchase && shopItem.Price >= 16;
            return true;
        }

        private static bool TryResolvePickupPreview(
            BasePickupLogic pickupLogic,
            PlayerInventory playerInventory,
            PlayerHealth playerHealth,
            PlayerStats playerStats,
            PlayerTrinketHolder playerTrinketHolder,
            PlayerActiveItemController playerActiveItemController,
            PlayerConsumableHolder playerConsumableHolder,
            out string title,
            out string detail,
            out Color accentColor,
            out string compareLabel,
            out Color compareColor,
            out bool emphasize)
        {
            title = ResolvePickupTitle(pickupLogic);
            detail = ResolvePickupDetail(pickupLogic);
            accentColor = NormalizeAccent(pickupLogic.PreviewFeedbackColor, new Color(0.72f, 0.85f, 1f, 1f));
            compareLabel = string.Empty;
            compareColor = accentColor;
            emphasize = pickupLogic is ItemPickupLogic
                || pickupLogic is ActiveItemPickupLogic
                || pickupLogic is TrinketPickupLogic;

            switch (pickupLogic)
            {
                case ItemPickupLogic itemPickup when itemPickup.ItemData != null:
                    accentColor = ResolveItemAccent(itemPickup.ItemData.Rarity);
                    compareLabel = ResolvePassiveItemCompareLabel(itemPickup.ItemData, playerStats);
                    compareColor = ResolvePassiveCompareColor(compareLabel, accentColor);
                    emphasize |= itemPickup.ItemData.Rarity >= ItemRarity.Rare;
                    return true;

                case TrinketPickupLogic trinketPickup when trinketPickup.TrinketData != null:
                    accentColor = ResolveItemAccent(trinketPickup.TrinketData.Rarity);
                    trinketPickup.TrinketData.BuildModifierStack(ModifierStack);
                    compareLabel = TryResolveProjectedProjectileProfileCompareLabel(ModifierStack.ProjectileModifiers, playerStats, out string projectedTrinketProfileLabel)
                        ? projectedTrinketProfileLabel
                        : ResolveTrinketCompareLabel(trinketPickup.TrinketData, playerTrinketHolder);
                    compareColor = compareLabel == "TRINKET SWAP"
                        ? new Color(1f, 0.76f, 0.34f, 1f)
                        : Color.Lerp(accentColor, Color.white, 0.18f);
                    emphasize = true;
                    return true;

                case ActiveItemPickupLogic activePickup when activePickup.ActiveItemData != null:
                    accentColor = new Color(0.58f, 0.9f, 1f, 1f);
                    compareLabel = ResolveActiveItemCompareLabel(activePickup.ActiveItemData, playerActiveItemController);
                    compareColor = compareLabel == "ACTIVE SWAP"
                        ? new Color(1f, 0.78f, 0.34f, 1f)
                        : Color.Lerp(accentColor, Color.white, 0.18f);
                    emphasize = true;
                    return true;

                case ConsumablePickupLogic consumablePickup when consumablePickup.ConsumableItemData != null:
                    accentColor = ResolveConsumableAccent(consumablePickup.ConsumableItemData);
                    compareLabel = ResolveConsumableCompareLabel(consumablePickup.ConsumableItemData, playerHealth, playerInventory, playerConsumableHolder, playerStats);
                    compareColor = ResolveConsumableCompareColor(compareLabel, accentColor);
                    emphasize = consumablePickup.ConsumableItemData.HasTimedEffect;
                    return true;

                case HeartPickupLogic heartPickup:
                    accentColor = new Color(1f, 0.48f, 0.58f, 1f);
                    compareLabel = ResolveHeartCompareLabel(heartPickup.HealAmount, playerHealth);
                    compareColor = compareLabel == "FULL HP"
                        ? new Color(1f, 0.46f, 0.42f, 1f)
                        : Color.Lerp(accentColor, Color.white, 0.18f);
                    return true;

                case ResourcePickupLogic resourcePickup:
                    accentColor = NormalizeAccent(resourcePickup.PreviewFeedbackColor, accentColor);
                    compareLabel = ResolveResourceCompareLabel(resourcePickup.ResourceType, resourcePickup.Amount, playerInventory, false);
                    compareColor = Color.Lerp(accentColor, Color.white, 0.14f);
                    return true;

                default:
                    compareLabel = "PICKUP READY";
                    compareColor = Color.Lerp(accentColor, Color.white, 0.12f);
                    return true;
            }
        }

        private static bool TryResolveShopOutcomePreview(
            ShopItem shopItem,
            PlayerInventory playerInventory,
            PlayerItemManager playerItemManager,
            PlayerHealth playerHealth,
            PlayerStats playerStats,
            out string outcomeLabel,
            out Color outcomeColor)
        {
            outcomeLabel = string.Empty;
            outcomeColor = ResolveShopRewardAccent(shopItem);

            if (shopItem == null || shopItem.ShopItemData == null)
            {
                return false;
            }

            bool canPurchase = shopItem.CanPurchase(playerInventory, playerItemManager, playerHealth);
            if (!canPurchase)
            {
                string compareLabel = ResolveShopCompareLabel(shopItem, false, playerInventory, playerItemManager, playerHealth, playerStats);
                outcomeLabel = compareLabel switch
                {
                    "COIN SHORT" or "KEY SHORT" or "BOMB SHORT" => BuildShopCostSummary(shopItem),
                    _ => compareLabel
                };
                outcomeColor = ResolveUnavailableCompareColor(shopItem);
                return !string.IsNullOrWhiteSpace(outcomeLabel);
            }

            ShopOffer offer = shopItem.ShopItemData.Offer;
            switch (offer.RewardType)
            {
                case ShopOfferRewardType.PassiveItem when offer.PassiveItem != null:
                    outcomeLabel = BuildPassiveOutcomeSummary(offer.PassiveItem, playerStats);
                    outcomeColor = ResolveItemAccent(offer.PassiveItem.Rarity);
                    return true;
                case ShopOfferRewardType.Health:
                    outcomeLabel = $"+{offer.HealthAmount:0.#} HP";
                    outcomeColor = new Color(1f, 0.54f, 0.62f, 1f);
                    return true;
                case ShopOfferRewardType.Ammo:
                    outcomeLabel = $"+{offer.ResourceAmount} AMMO";
                    outcomeColor = new Color(0.96f, 0.78f, 0.28f, 1f);
                    return true;
                case ShopOfferRewardType.Coins:
                    outcomeLabel = $"+{offer.ResourceAmount} COIN";
                    outcomeColor = new Color(0.98f, 0.86f, 0.34f, 1f);
                    return true;
                case ShopOfferRewardType.Keys:
                    outcomeLabel = $"+{offer.ResourceAmount} KEY";
                    outcomeColor = new Color(0.74f, 0.88f, 1f, 1f);
                    return true;
                case ShopOfferRewardType.Bombs:
                    outcomeLabel = $"+{offer.ResourceAmount} BOMB";
                    outcomeColor = new Color(1f, 0.6f, 0.3f, 1f);
                    return true;
                default:
                    outcomeLabel = BuildShopCostSummary(shopItem);
                    outcomeColor = ResolveCurrencyAccent(shopItem.CurrencyType);
                    return !string.IsNullOrWhiteSpace(outcomeLabel);
            }
        }

        private static bool TryResolvePickupOutcomePreview(
            BasePickupLogic pickupLogic,
            PlayerHealth playerHealth,
            PlayerInventory playerInventory,
            PlayerStats playerStats,
            out string outcomeLabel,
            out Color outcomeColor)
        {
            outcomeLabel = string.Empty;
            outcomeColor = pickupLogic != null && pickupLogic.PreviewFeedbackColor.a > 0.01f
                ? pickupLogic.PreviewFeedbackColor
                : new Color(0.72f, 0.85f, 1f, 1f);

            if (pickupLogic == null)
            {
                return false;
            }

            switch (pickupLogic)
            {
                case ItemPickupLogic itemPickup when itemPickup.ItemData != null:
                    outcomeLabel = BuildPassiveOutcomeSummary(itemPickup.ItemData, playerStats);
                    outcomeColor = ResolveItemAccent(itemPickup.ItemData.Rarity);
                    return true;
                case TrinketPickupLogic trinketPickup when trinketPickup.TrinketData != null:
                    outcomeLabel = BuildPassiveOutcomeSummary(trinketPickup.TrinketData, playerStats);
                    outcomeColor = ResolveItemAccent(trinketPickup.TrinketData.Rarity);
                    return true;
                case ActiveItemPickupLogic activePickup when activePickup.ActiveItemData != null:
                    outcomeLabel = BuildActiveOutcomeSummary(activePickup.ActiveItemData);
                    outcomeColor = new Color(0.58f, 0.9f, 1f, 1f);
                    return true;
                case ConsumablePickupLogic consumablePickup when consumablePickup.ConsumableItemData != null:
                    outcomeLabel = BuildConsumableOutcomeSummary(consumablePickup.ConsumableItemData, playerStats);
                    outcomeColor = ResolveConsumableAccent(consumablePickup.ConsumableItemData);
                    return true;
                case HeartPickupLogic heartPickup:
                    outcomeLabel = ResolveHeartCompareLabel(heartPickup.HealAmount, playerHealth) == "FULL HP"
                        ? "HP FULL"
                        : $"+{heartPickup.HealAmount:0.#} HP";
                    outcomeColor = new Color(1f, 0.54f, 0.62f, 1f);
                    return true;
                case ResourcePickupLogic resourcePickup:
                    outcomeLabel = resourcePickup.ResourceType switch
                    {
                        ResourcePickupType.Coin => $"+{resourcePickup.Amount} COIN",
                        ResourcePickupType.Key => $"+{resourcePickup.Amount} KEY",
                        ResourcePickupType.Bomb => $"+{resourcePickup.Amount} BOMB",
                        _ => ResolveResourceCompareLabel(resourcePickup.ResourceType, resourcePickup.Amount, playerInventory, false)
                    };
                    outcomeColor = NormalizeAccent(resourcePickup.PreviewFeedbackColor, outcomeColor);
                    return true;
                default:
                    return false;
            }
        }

        private static string ResolvePickupTitle(BasePickupLogic pickupLogic)
        {
            return pickupLogic switch
            {
                ItemPickupLogic itemPickup when itemPickup.ItemData != null => itemPickup.ItemData.DisplayName.ToUpperInvariant(),
                TrinketPickupLogic trinketPickup when trinketPickup.TrinketData != null => trinketPickup.TrinketData.DisplayName.ToUpperInvariant(),
                ActiveItemPickupLogic activePickup when activePickup.ActiveItemData != null => activePickup.ActiveItemData.DisplayName.ToUpperInvariant(),
                ConsumablePickupLogic consumablePickup when consumablePickup.ConsumableItemData != null => consumablePickup.ConsumableItemData.DisplayName.ToUpperInvariant(),
                _ => pickupLogic.PreviewFeedbackLabel
            };
        }

        private static string ResolvePickupDetail(BasePickupLogic pickupLogic)
        {
            switch (pickupLogic)
            {
                case ItemPickupLogic itemPickup when itemPickup.ItemData != null:
                    return BuildPassiveItemDetail(itemPickup.ItemData);
                case TrinketPickupLogic trinketPickup when trinketPickup.TrinketData != null:
                    return BuildPassiveItemDetail(trinketPickup.TrinketData);
                case ActiveItemPickupLogic activePickup when activePickup.ActiveItemData != null:
                    return BuildActiveItemDetail(activePickup.ActiveItemData);
                case ConsumablePickupLogic consumablePickup when consumablePickup.ConsumableItemData != null:
                    return BuildConsumableDetail(consumablePickup.ConsumableItemData);
                case HeartPickupLogic heartPickup:
                    return $"RESTORES {heartPickup.HealAmount:0.#} HP";
                case SpeedCandyPickupLogic speedHeartPickup:
                    return $"STORES {speedHeartPickup.SpeedHeartAmount} SPEED HEART";
                case ResourcePickupLogic resourcePickup:
                    return resourcePickup.ResourceType switch
                    {
                        ResourcePickupType.Coin => $"+{resourcePickup.Amount} COIN",
                        ResourcePickupType.Key => $"+{resourcePickup.Amount} KEY",
                        ResourcePickupType.Bomb => $"+{resourcePickup.Amount} BOMB",
                        _ => string.Empty
                    };
                default:
                    return string.Empty;
            }
        }

        private static string ResolveShopRewardDetail(ShopItem shopItem)
        {
            if (shopItem?.ShopItemData == null)
            {
                return string.Empty;
            }

            ShopOffer offer = shopItem.ShopItemData.Offer;
            return offer.RewardType switch
            {
                ShopOfferRewardType.PassiveItem when offer.PassiveItem != null => BuildPassiveItemDetail(offer.PassiveItem),
                ShopOfferRewardType.Health => $"+{offer.HealthAmount:0.#} HP",
                ShopOfferRewardType.Ammo => $"+{offer.ResourceAmount} AMMO",
                ShopOfferRewardType.Coins => $"+{offer.ResourceAmount} COIN CACHE",
                ShopOfferRewardType.Keys => $"+{offer.ResourceAmount} KEY CACHE",
                ShopOfferRewardType.Bombs => $"+{offer.ResourceAmount} BOMB CACHE",
                _ => NormalizeText(shopItem.ShopItemData.Description)
            };
        }

        private static string ResolveShopCompareLabel(
            ShopItem shopItem,
            bool canPurchase,
            PlayerInventory playerInventory,
            PlayerItemManager playerItemManager,
            PlayerHealth playerHealth,
            PlayerStats playerStats)
        {
            if (shopItem == null)
            {
                return string.Empty;
            }

            if (!canPurchase)
            {
                ShopSlotState state = shopItem.BuildSlotState(false, playerInventory, playerItemManager, playerHealth);
                return state.StatusLabel switch
                {
                    "코인 부족" => "COIN SHORT",
                    "열쇠 부족" => "KEY SHORT",
                    "폭탄 부족" => "BOMB SHORT",
                    "이미 보유" => "ALREADY OWNED",
                    "체력 가득" => "FULL HP",
                    "판매 완료" => "SOLD OUT",
                    _ => "SHOP LOCKED"
                };
            }

            ShopOffer offer = shopItem.ShopItemData.Offer;
            return offer.RewardType switch
            {
                ShopOfferRewardType.PassiveItem when offer.PassiveItem != null => ResolvePassiveItemCompareLabel(offer.PassiveItem, playerStats),
                ShopOfferRewardType.Health => ResolveHeartCompareLabel(offer.HealthAmount, playerHealth),
                ShopOfferRewardType.Ammo => ResolveAmmoCompareLabel(offer.ResourceAmount, playerInventory, playerItemManager, playerHealth),
                ShopOfferRewardType.Coins => ResolveResourceCompareLabel(ResourcePickupType.Coin, offer.ResourceAmount, playerInventory, true),
                ShopOfferRewardType.Keys => ResolveResourceCompareLabel(ResourcePickupType.Key, offer.ResourceAmount, playerInventory, true),
                ShopOfferRewardType.Bombs => ResolveResourceCompareLabel(ResourcePickupType.Bomb, offer.ResourceAmount, playerInventory, true),
                _ => "BUY READY"
            };
        }

        private static string BuildShopCostSummary(ShopItem shopItem)
        {
            if (shopItem == null)
            {
                return string.Empty;
            }

            string currencyLabel = shopItem.CurrencyType switch
            {
                ShopCurrencyType.Keys => "KEY",
                ShopCurrencyType.Bombs => "BOMB",
                _ => "COIN"
            };

            return $"{shopItem.Price} {currencyLabel}";
        }

        private static float ResolvePreviewPriorityScore(Transform target, string compareLabel, bool emphasize)
        {
            float score = compareLabel switch
            {
                "SOLD OUT" => -100f,
                "ALREADY OWNED" => -64f,
                "COIN SHORT" or "KEY SHORT" or "BOMB SHORT" or "SHOP LOCKED" => -34f,
                "NO WEAPON" => -30f,
                "FULL HP" => -24f,
                "AMMO FULL" => -22f,
                "STASH FULL" => -18f,
                "DMG SPIKE" => 30f,
                "SHOT SPIKE" => 29f,
                "EXTRA SHOT" => 28f,
                "PIERCE" or "HOMING" or "EXPLOSIVE" or "LASER SHIFT" or "SPLIT SHOT" or "RICOCHET" or "LIFESTEAL" => 27f,
                "CLUTCH HEAL" => 26f,
                "DMG UP" => 24f,
                "SHOT UP" => 23f,
                "HP SPIKE" => 22f,
                "FULL HEAL" => 21f,
                "HEAL NOW" => 19f,
                "KEY RELIEF" or "BOMB RELIEF" => 18f,
                "RESTOCK AMMO" or "AMMO NOW" => 17f,
                "ACTIVE OPEN" => 17f,
                "TRINKET OPEN" => 16f,
                "ACTIVE SWAP" => 15f,
                "TRINKET SWAP" => 14f,
                "SHOP FUEL" => 13f,
                "TEMP BUFF" => 12.5f,
                "BUY READY" => 11f,
                "INSTANT USE" or "STASH OPEN" => 9f,
                "COIN UP" or "KEY UP" or "BOMB UP" => 8f,
                "TRADE OFF" => 4f,
                _ when compareLabel.EndsWith("SPIKE", System.StringComparison.Ordinal) => 18f,
                _ when compareLabel.EndsWith("UP", System.StringComparison.Ordinal) => 12f,
                _ when compareLabel.EndsWith("EDGE", System.StringComparison.Ordinal) => 11f,
                _ when compareLabel.EndsWith("SHIFT", System.StringComparison.Ordinal) => 13f,
                _ when compareLabel.EndsWith("TECH", System.StringComparison.Ordinal) => 12.5f,
                _ when compareLabel.IndexOf("HEAL", System.StringComparison.Ordinal) >= 0 => 16f,
                _ => string.IsNullOrWhiteSpace(compareLabel) ? 6f : 10f
            };

            if (emphasize)
            {
                score += 1.4f;
            }

            score += ResolveIntrinsicTargetPriority(target);
            return score;
        }

        private static float ResolveIntrinsicTargetPriority(Transform target)
        {
            if (target == null)
            {
                return 0f;
            }

            ShopItem shopItem = target.GetComponent<ShopItem>();
            if (shopItem == null)
            {
                shopItem = target.GetComponentInChildren<ShopItem>(true);
            }

            if (shopItem != null && shopItem.ShopItemData != null)
            {
                float shopScore = Mathf.Clamp(shopItem.Price, 0, 24) * 0.18f;
                if (shopItem.ShopItemData.Offer.RewardType == ShopOfferRewardType.PassiveItem)
                {
                    shopScore += 1.75f;
                }

                return shopScore;
            }

            BasePickupLogic pickupLogic = target.GetComponent<BasePickupLogic>();
            if (pickupLogic == null)
            {
                pickupLogic = target.GetComponentInChildren<BasePickupLogic>(true);
            }

            return pickupLogic switch
            {
                ItemPickupLogic itemPickup when itemPickup.ItemData != null => ResolveRarityPriority(itemPickup.ItemData.Rarity),
                TrinketPickupLogic trinketPickup when trinketPickup.TrinketData != null => 1.8f + (ResolveRarityPriority(trinketPickup.TrinketData.Rarity) * 0.55f),
                ActiveItemPickupLogic => 3.5f,
                ConsumablePickupLogic consumablePickup when consumablePickup.ConsumableItemData != null && consumablePickup.ConsumableItemData.HasTimedEffect => 1.6f,
                HeartPickupLogic heartPickup => Mathf.Clamp(heartPickup.HealAmount, 0.5f, 4f) * 0.45f,
                SpeedCandyPickupLogic speedHeartPickup => Mathf.Clamp(speedHeartPickup.SpeedHeartAmount, 1, 3) * 1.25f,
                ResourcePickupLogic resourcePickup => Mathf.Clamp(resourcePickup.Amount, 1, 8) * 0.28f,
                _ => 0f
            };
        }

        private static float ResolveRarityPriority(ItemRarity rarity)
        {
            return rarity switch
            {
                ItemRarity.Uncommon => 1.4f,
                ItemRarity.Rare => 3f,
                ItemRarity.Legendary => 4.8f,
                ItemRarity.Relic => 5.8f,
                ItemRarity.Boss => 6.4f,
                _ => 0.5f
            };
        }

        private static Color ResolveUnavailableCompareColor(ShopItem shopItem)
        {
            Color currencyColor = shopItem != null
                ? ResolveCurrencyAccent(shopItem.CurrencyType)
                : new Color(1f, 0.46f, 0.42f, 1f);
            return Color.Lerp(currencyColor, new Color(1f, 0.42f, 0.42f, 1f), 0.55f);
        }

        private static string ResolvePassiveItemCompareLabel(ItemData itemData, PlayerStats playerStats)
        {
            if (itemData == null)
            {
                return string.Empty;
            }

            if (itemData.IsWeaponRelic)
            {
                return BuildWeaponCompareLabel(itemData);
            }

            itemData.BuildModifierStack(ModifierStack);
            if (TryResolveProjectedProjectileProfileCompareLabel(ModifierStack.ProjectileModifiers, playerStats, out string projectedProfileLabel))
            {
                return projectedProfileLabel;
            }

            if (TryResolveProjectileCompareLabel(ModifierStack.ProjectileModifiers, out string projectileLabel))
            {
                return projectileLabel;
            }

            if (TryResolveStatCompareLabel(ModifierStack.StatModifiers, playerStats, out string statLabel))
            {
                return statLabel;
            }

            return itemData.ItemCategory switch
            {
                ItemCategory.Damage => "DMG EDGE",
                ItemCategory.FireRate => "SHOT EDGE",
                ItemCategory.Movement => "SPEED EDGE",
                ItemCategory.Projectile => "SHOT SHIFT",
                ItemCategory.Defense => "HP EDGE",
                ItemCategory.Economy => "ECON SPIKE",
                ItemCategory.Luck => "LUCK UP",
                ItemCategory.Summon => "SUMMON TECH",
                ItemCategory.Orbital => "ORBITAL",
                ItemCategory.Laser => "LASER SHIFT",
                ItemCategory.Bomb => "BOMB TECH",
                _ => "BUILD SHIFT"
            };
        }

        private static bool TryResolveStatCompareLabel(
            IReadOnlyList<StatModifier> statModifiers,
            PlayerStats playerStats,
            out string compareLabel)
        {
            compareLabel = string.Empty;

            if (statModifiers == null || statModifiers.Count == 0)
            {
                return false;
            }

            float currentDamage = playerStats != null ? Mathf.Max(0.1f, playerStats.CurrentDamage) : 3f;
            float currentFireInterval = playerStats != null ? Mathf.Max(0.05f, playerStats.CurrentFireInterval) : 0.3f;
            float currentMaxHealth = playerStats != null ? Mathf.Max(1f, playerStats.CurrentMaxHealth) : 6f;
            float bestScore = float.MinValue;

            for (int index = 0; index < statModifiers.Count; index++)
            {
                StatModifier modifier = statModifiers[index];
                string candidate = string.Empty;
                float score = float.MinValue;

                switch (modifier.StatType)
                {
                    case PlayerStatType.Damage:
                    {
                        float normalized = ResolvePositiveModifierStrength(modifier, currentDamage, true);
                        if (normalized > 0f)
                        {
                            candidate = normalized >= 0.22f ? "DMG SPIKE" : "DMG UP";
                            score = 10f + normalized;
                        }

                        break;
                    }
                    case PlayerStatType.FireInterval:
                    {
                        float normalized = ResolveFireRateStrength(modifier, currentFireInterval);
                        if (normalized > 0f)
                        {
                            candidate = normalized >= 0.18f ? "SHOT SPIKE" : "SHOT UP";
                            score = 9f + normalized;
                        }

                        break;
                    }
                    case PlayerStatType.MaxHealth:
                    {
                        float normalized = ResolvePositiveModifierStrength(modifier, currentMaxHealth, true);
                        if (normalized > 0f)
                        {
                            candidate = normalized >= 0.28f ? "HP SPIKE" : "HP UP";
                            score = 8f + normalized;
                        }

                        break;
                    }
                    case PlayerStatType.ProjectileCount:
                    {
                        float normalized = ResolvePositiveModifierStrength(modifier, 1f, true);
                        if (normalized > 0f)
                        {
                            candidate = "EXTRA SHOT";
                            score = 7.5f + normalized;
                        }

                        break;
                    }
                    case PlayerStatType.MoveSpeed:
                    {
                        float normalized = ResolvePositiveModifierStrength(modifier, 5f, true);
                        if (normalized > 0f)
                        {
                            candidate = normalized >= 0.18f ? "SPEED SPIKE" : "SPEED UP";
                            score = 6f + normalized;
                        }

                        break;
                    }
                    case PlayerStatType.ProjectileSpeed:
                    {
                        float normalized = ResolvePositiveModifierStrength(modifier, 8.5f, true);
                        if (normalized > 0f)
                        {
                            candidate = "SHOT SPEED";
                            score = 5f + normalized;
                        }

                        break;
                    }
                    case PlayerStatType.Range:
                    {
                        float normalized = ResolvePositiveModifierStrength(modifier, 18f, true);
                        if (normalized > 0f)
                        {
                            candidate = "RANGE UP";
                            score = 4f + normalized;
                        }

                        break;
                    }
                    case PlayerStatType.Luck:
                    {
                        float normalized = ResolvePositiveModifierStrength(modifier, 1f, true);
                        if (normalized > 0f)
                        {
                            candidate = "LUCK UP";
                            score = 3f + normalized;
                        }

                        break;
                    }
                    case PlayerStatType.Knockback:
                    {
                        float normalized = ResolvePositiveModifierStrength(modifier, 2f, true);
                        if (normalized > 0f)
                        {
                            candidate = "KNOCK UP";
                            score = 2f + normalized;
                        }

                        break;
                    }
                }

                if (score > bestScore && !string.IsNullOrWhiteSpace(candidate))
                {
                    bestScore = score;
                    compareLabel = candidate;
                }
            }

            if (!string.IsNullOrWhiteSpace(compareLabel))
            {
                return true;
            }

            for (int index = 0; index < statModifiers.Count; index++)
            {
                StatModifier modifier = statModifiers[index];
                float strength = modifier.StatType == PlayerStatType.FireInterval
                    ? ResolveFireRateStrength(modifier, currentFireInterval)
                    : ResolvePositiveModifierStrength(modifier, 1f, true);
                if (strength < 0f)
                {
                    compareLabel = "TRADE OFF";
                    return true;
                }
            }

            return false;
        }

        private static float ResolveFireRateStrength(StatModifier modifier, float currentFireInterval)
        {
            float safeInterval = Mathf.Max(0.05f, currentFireInterval);

            return modifier.Operation switch
            {
                StatModifierOperation.Add => -modifier.Value / safeInterval,
                StatModifierOperation.Multiply => modifier.Value - 1f,
                StatModifierOperation.Override => (safeInterval - modifier.Value) / safeInterval,
                _ => 0f
            };
        }

        private static bool TryResolveProjectileCompareLabel(IReadOnlyList<ProjectileModifier> projectileModifiers, out string compareLabel)
        {
            compareLabel = string.Empty;

            if (projectileModifiers == null)
            {
                return false;
            }

            for (int index = 0; index < projectileModifiers.Count; index++)
            {
                ProjectileModifier modifier = projectileModifiers[index];
                compareLabel = modifier.ModifierType switch
                {
                    ProjectileModifierType.Pierce => "PIERCE",
                    ProjectileModifierType.Homing => "HOMING",
                    ProjectileModifierType.MultiShot => "EXTRA SHOT",
                    ProjectileModifierType.Explode => "EXPLOSIVE",
                    ProjectileModifierType.Laser => "LASER SHIFT",
                    ProjectileModifierType.Split => "SPLIT SHOT",
                    ProjectileModifierType.Bounce => "RICOCHET",
                    ProjectileModifierType.Orbit => "ORBITAL",
                    ProjectileModifierType.Shield => "SHIELD",
                    ProjectileModifierType.Lifesteal => "LIFESTEAL",
                    _ => string.Empty
                };

                if (!string.IsNullOrWhiteSpace(compareLabel))
                {
                    return true;
                }
            }

            return false;
        }

        private static string ResolveTrinketCompareLabel(ItemData trinketData, PlayerTrinketHolder playerTrinketHolder)
        {
            if (trinketData == null)
            {
                return string.Empty;
            }

            if (playerTrinketHolder != null
                && playerTrinketHolder.HasTrinket
                && playerTrinketHolder.EquippedTrinket != null
                && playerTrinketHolder.EquippedTrinket != trinketData)
            {
                return "TRINKET SWAP";
            }

            return "TRINKET OPEN";
        }

        private static string ResolveActiveItemCompareLabel(ActiveItemData activeItemData, PlayerActiveItemController playerActiveItemController)
        {
            if (activeItemData == null)
            {
                return string.Empty;
            }

            if (playerActiveItemController != null
                && playerActiveItemController.HasEquippedItem
                && playerActiveItemController.EquippedItem != null
                && playerActiveItemController.EquippedItem != activeItemData)
            {
                return "ACTIVE SWAP";
            }

            return "ACTIVE OPEN";
        }

        private static string ResolveConsumableCompareLabel(
            ConsumableItemData consumableItemData,
            PlayerHealth playerHealth,
            PlayerInventory playerInventory,
            PlayerConsumableHolder playerConsumableHolder,
            PlayerStats playerStats)
        {
            if (consumableItemData == null)
            {
                return string.Empty;
            }

            if (consumableItemData.PickupMode == ConsumablePickupMode.StoreInHolder)
            {
                return playerConsumableHolder != null && playerConsumableHolder.HeldConsumable != null
                    ? "STASH FULL"
                    : "STASH OPEN";
            }

            if (consumableItemData.HealAmount > 0f)
            {
                return ResolveHeartCompareLabel(consumableItemData.HealAmount, playerHealth);
            }

            if (consumableItemData.CoinGain > 0)
            {
                return ResolveResourceCompareLabel(ResourcePickupType.Coin, consumableItemData.CoinGain, playerInventory, false);
            }

            if (consumableItemData.KeyGain > 0)
            {
                return ResolveResourceCompareLabel(ResourcePickupType.Key, consumableItemData.KeyGain, playerInventory, false);
            }

            if (consumableItemData.BombGain > 0)
            {
                return ResolveResourceCompareLabel(ResourcePickupType.Bomb, consumableItemData.BombGain, playerInventory, false);
            }

            if (consumableItemData.HasTimedEffect)
            {
                if (TryResolveProjectedProjectileProfileCompareLabel(consumableItemData.TemporaryProjectileModifiers, playerStats, out string projectedProfileLabel))
                {
                    return projectedProfileLabel;
                }

                return "TEMP BUFF";
            }

            return "INSTANT USE";
        }

        private static Color ResolveConsumableCompareColor(string compareLabel, Color accentColor)
        {
            return compareLabel switch
            {
                "STASH FULL" => new Color(1f, 0.46f, 0.42f, 1f),
                "FULL HP" => new Color(1f, 0.46f, 0.42f, 1f),
                _ => Color.Lerp(accentColor, Color.white, 0.16f)
            };
        }

        private static string ResolveHeartCompareLabel(float healAmount, PlayerHealth playerHealth)
        {
            if (playerHealth == null)
            {
                return healAmount >= 2f ? "FULL HEAL" : "HEAL NOW";
            }

            float missingHealth = Mathf.Max(0f, playerHealth.MaxHealth - playerHealth.CurrentHealth);
            if (missingHealth <= 0.01f)
            {
                return "FULL HP";
            }

            if (healAmount >= missingHealth - 0.01f)
            {
                return "FULL HEAL";
            }

            if (playerHealth.CurrentHealth <= 2f)
            {
                return "CLUTCH HEAL";
            }

            return "HEAL NOW";
        }

        private static string ResolveAmmoCompareLabel(
            int ammoAmount,
            PlayerInventory playerInventory,
            PlayerItemManager playerItemManager,
            PlayerHealth playerHealth)
        {
            PlayerWeaponLoadout weaponLoadout = ResolveWeaponLoadout(playerInventory, playerHealth, playerItemManager);
            if (weaponLoadout == null || !weaponLoadout.HasWeapon)
            {
                return "NO WEAPON";
            }

            if (!weaponLoadout.CanReceiveAmmoPickup())
            {
                return "AMMO FULL";
            }

            return ammoAmount >= 8 ? "RESTOCK AMMO" : "AMMO NOW";
        }

        private static string ResolveResourceCompareLabel(
            ResourcePickupType resourceType,
            int amount,
            PlayerInventory playerInventory,
            bool fromShop)
        {
            switch (resourceType)
            {
                case ResourcePickupType.Key:
                    if (playerInventory != null && playerInventory.Keys <= 0)
                    {
                        return "KEY RELIEF";
                    }

                    return "KEY UP";
                case ResourcePickupType.Bomb:
                    if (playerInventory != null && playerInventory.Bombs <= 0)
                    {
                        return "BOMB RELIEF";
                    }

                    return "BOMB UP";
                case ResourcePickupType.Coin:
                default:
                    if (fromShop || (playerInventory != null && playerInventory.Coins < 5))
                    {
                        return "SHOP FUEL";
                    }

                    return amount >= 5 ? "ECON SPIKE" : "COIN UP";
            }
        }

        private static PlayerWeaponLoadout ResolveWeaponLoadout(
            PlayerInventory playerInventory,
            PlayerHealth playerHealth,
            PlayerItemManager playerItemManager)
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

        private static string BuildPassiveItemDetail(ItemData itemData)
        {
            if (itemData == null)
            {
                return string.Empty;
            }

            if (itemData.IsWeaponRelic)
            {
                return BuildWeaponDetail(itemData);
            }

            itemData.BuildModifierStack(ModifierStack);
            SummaryChunks.Clear();
            AppendStatSummaryChunks(ModifierStack.StatModifiers);
            AppendProjectileSummaryChunks(ModifierStack.ProjectileModifiers);

            if (SummaryChunks.Count > 0)
            {
                StringBuilder builder = new();
                for (int index = 0; index < SummaryChunks.Count && index < 2; index++)
                {
                    if (builder.Length > 0)
                    {
                        builder.Append("  ·  ");
                    }

                    builder.Append(SummaryChunks[index]);
                }

                return builder.ToString();
            }

            string description = NormalizeText(itemData.Description);
            if (!string.IsNullOrWhiteSpace(description))
            {
                return description;
            }

            string flavorText = NormalizeText(itemData.FlavorText);
            if (!string.IsNullOrWhiteSpace(flavorText))
            {
                return flavorText;
            }

            return itemData.ItemCategory switch
            {
                ItemCategory.Damage => "OFFENSE-LEANING PASSIVE",
                ItemCategory.FireRate => "FASTER FIRE RHYTHM",
                ItemCategory.Movement => "HIGHER MOVE TEMPO",
                ItemCategory.Projectile => "PROJECTILE BUILD SHIFT",
                ItemCategory.Defense => "SURVIVABILITY EDGE",
                ItemCategory.Economy => "RESOURCE POSITIVE PICK",
                _ => "BUILD-SHAPING PASSIVE"
            };
        }

        private static string BuildPassiveOutcomeSummary(ItemData itemData, PlayerStats playerStats)
        {
            if (itemData == null)
            {
                return string.Empty;
            }

            if (itemData.IsWeaponRelic)
            {
                return BuildWeaponOutcomeSummary(itemData);
            }

            itemData.BuildModifierStack(ModifierStack);
            SummaryChunks.Clear();
            bool hasProjectedProfile = TryResolveProjectedProjectileProfileOutcomeLabel(ModifierStack.ProjectileModifiers, playerStats, out string projectedProfileLabel);
            if (hasProjectedProfile)
            {
                SummaryChunks.Add(projectedProfileLabel);
            }

            AppendStatSummaryChunks(ModifierStack.StatModifiers);
            if (!hasProjectedProfile)
            {
                AppendProjectileSummaryChunks(ModifierStack.ProjectileModifiers);
            }

            if (SummaryChunks.Count > 0)
            {
                StringBuilder builder = new();
                for (int index = 0; index < SummaryChunks.Count && index < 2; index++)
                {
                    if (builder.Length > 0)
                    {
                        builder.Append(" / ");
                    }

                    builder.Append(SummaryChunks[index]);
                }

                return builder.ToString();
            }

            return itemData.ItemCategory switch
            {
                ItemCategory.Damage => "OFFENSE UP",
                ItemCategory.FireRate => "FIRE RHYTHM UP",
                ItemCategory.Movement => "MOVE FLOW UP",
                ItemCategory.Projectile => "SHOT PATTERN SHIFT",
                ItemCategory.Defense => "SURVIVE EDGE",
                ItemCategory.Economy => "RESOURCE EDGE",
                ItemCategory.Luck => "LUCK EDGE",
                ItemCategory.Summon => "SUMMON TECH",
                ItemCategory.Orbital => "ORBITAL",
                ItemCategory.Laser => "LASER SHIFT",
                ItemCategory.Bomb => "BOMB TECH",
                _ => "BUILD UPDATED"
            };
        }

        private static string BuildWeaponCompareLabel(ItemData itemData)
        {
            ItemWeaponProfile profile = itemData != null ? itemData.WeaponProfile : null;
            if (profile == null || !profile.IsValid)
            {
                return "ARMORY SHIFT";
            }

            if (profile.ShotsPerTrigger >= 5)
            {
                return "SHOTGUN SWAP";
            }

            if (profile.InfiniteReserveAmmo)
            {
                return "SIDEARM RELIC";
            }

            if (profile.AttackDefinition != null && profile.AttackDefinition.FireInterval <= 0.1f)
            {
                return "SPRAY SHIFT";
            }

            if (profile.MagazineCapacity <= 6 && profile.ReloadDuration >= 1.4f)
            {
                return "HEAVY RELIC";
            }

            return "ARMORY SHIFT";
        }

        private static string BuildWeaponDetail(ItemData itemData)
        {
            ItemWeaponProfile profile = itemData != null ? itemData.WeaponProfile : null;
            if (profile == null || !profile.IsValid)
            {
                return NormalizeText(itemData != null ? itemData.Description : string.Empty);
            }

            string reserveLabel = profile.InfiniteReserveAmmo
                ? "INF"
                : profile.StartingReserveAmmo.ToString();
            string motifLabel = !string.IsNullOrWhiteSpace(profile.FirearmMotif)
                ? NormalizeText(profile.FirearmMotif)
                : "Modern firearm relic";
            string pelletLabel = profile.ShotsPerTrigger > 1
                ? $" | {profile.ShotsPerTrigger} pellets"
                : string.Empty;
            return $"{motifLabel} | {profile.MagazineCapacity}/{reserveLabel} rounds | {profile.ReloadDuration:0.#}s reload{pelletLabel}";
        }

        private static string BuildWeaponOutcomeSummary(ItemData itemData)
        {
            ItemWeaponProfile profile = itemData != null ? itemData.WeaponProfile : null;
            if (profile == null || !profile.IsValid)
            {
                return "ARMED";
            }

            string reserveLabel = profile.InfiniteReserveAmmo
                ? "INF"
                : profile.StartingReserveAmmo.ToString();
            if (profile.ShotsPerTrigger > 1)
            {
                return $"{profile.ShotsPerTrigger} PELLETS | {profile.MagazineCapacity}/{reserveLabel}";
            }

            return $"ARMED | {profile.MagazineCapacity}/{reserveLabel}";
        }

        private static string BuildActiveItemDetail(ActiveItemData activeItemData)
        {
            if (activeItemData == null)
            {
                return string.Empty;
            }

            string description = NormalizeText(activeItemData.Description);
            if (!string.IsNullOrWhiteSpace(description))
            {
                return description;
            }

            return activeItemData.ChargeRule switch
            {
                ActiveItemChargeRule.RoomClear => $"{activeItemData.MaxCharge} CHARGE · ROOM FLOW",
                ActiveItemChargeRule.EnemyKill => $"{activeItemData.MaxCharge} CHARGE · KILL FLOW",
                _ => $"{activeItemData.MaxCharge} CHARGE ACTIVE"
            };
        }

        private static string BuildActiveOutcomeSummary(ActiveItemData activeItemData)
        {
            if (activeItemData == null)
            {
                return string.Empty;
            }

            return activeItemData.ChargeRule switch
            {
                ActiveItemChargeRule.RoomClear => $"{activeItemData.MaxCharge} CHARGE / ROOM FLOW",
                ActiveItemChargeRule.EnemyKill => $"{activeItemData.MaxCharge} CHARGE / KILL FLOW",
                _ => $"{activeItemData.MaxCharge} CHARGE"
            };
        }

        private static string BuildConsumableDetail(ConsumableItemData consumableItemData)
        {
            if (consumableItemData == null)
            {
                return string.Empty;
            }

            SummaryChunks.Clear();

            if (consumableItemData.HealAmount > 0f)
            {
                SummaryChunks.Add($"+{consumableItemData.HealAmount:0.#} HP");
            }

            if (consumableItemData.CoinGain > 0)
            {
                SummaryChunks.Add($"+{consumableItemData.CoinGain} COIN");
            }

            if (consumableItemData.KeyGain > 0)
            {
                SummaryChunks.Add($"+{consumableItemData.KeyGain} KEY");
            }

            if (consumableItemData.BombGain > 0)
            {
                SummaryChunks.Add($"+{consumableItemData.BombGain} BOMB");
            }

            if (consumableItemData.HasTimedEffect)
            {
                SummaryChunks.Add($"{consumableItemData.TemporaryEffectDuration:0.#}S BUFF");
            }

            if (SummaryChunks.Count > 0)
            {
                return string.Join("  ·  ", SummaryChunks);
            }

            return NormalizeText(consumableItemData.Description);
        }

        private static string BuildConsumableOutcomeSummary(ConsumableItemData consumableItemData, PlayerStats playerStats)
        {
            if (consumableItemData == null)
            {
                return string.Empty;
            }

            SummaryChunks.Clear();

            if (consumableItemData.HasTimedEffect
                && TryResolveProjectedProjectileProfileOutcomeLabel(consumableItemData.TemporaryProjectileModifiers, playerStats, out string projectedProfileLabel))
            {
                SummaryChunks.Add(projectedProfileLabel);
            }

            if (consumableItemData.HealAmount > 0f)
            {
                SummaryChunks.Add($"+{consumableItemData.HealAmount:0.#} HP");
            }

            if (consumableItemData.CoinGain > 0)
            {
                SummaryChunks.Add($"+{consumableItemData.CoinGain} COIN");
            }

            if (consumableItemData.KeyGain > 0)
            {
                SummaryChunks.Add($"+{consumableItemData.KeyGain} KEY");
            }

            if (consumableItemData.BombGain > 0)
            {
                SummaryChunks.Add($"+{consumableItemData.BombGain} BOMB");
            }

            if (consumableItemData.HasTimedEffect)
            {
                SummaryChunks.Add($"{consumableItemData.TemporaryEffectDuration:0.#}S BUFF");
            }

            if (SummaryChunks.Count > 0)
            {
                StringBuilder builder = new();
                for (int index = 0; index < SummaryChunks.Count && index < 2; index++)
                {
                    if (builder.Length > 0)
                    {
                        builder.Append(" / ");
                    }

                    builder.Append(SummaryChunks[index]);
                }

                return builder.ToString();
            }

            return "SUPPLY POP";
        }

        private static void AppendStatSummaryChunks(IReadOnlyList<StatModifier> statModifiers)
        {
            if (statModifiers == null)
            {
                return;
            }

            for (int index = 0; index < statModifiers.Count && SummaryChunks.Count < 3; index++)
            {
                StatModifier modifier = statModifiers[index];
                string statLabel = modifier.StatType switch
                {
                    PlayerStatType.Damage => "DMG",
                    PlayerStatType.FireInterval => "SHOT",
                    PlayerStatType.MoveSpeed => "SPD",
                    PlayerStatType.ProjectileSpeed => "SHOT SPD",
                    PlayerStatType.Range => "RANGE",
                    PlayerStatType.Luck => "LUCK",
                    PlayerStatType.ProjectileCount => "COUNT",
                    PlayerStatType.Knockback => "KNOCK",
                    PlayerStatType.MaxHealth => "HP",
                    _ => string.Empty
                };

                if (string.IsNullOrWhiteSpace(statLabel))
                {
                    continue;
                }

                float value = modifier.Value;
                string chunk = modifier.Operation switch
                {
                    StatModifierOperation.Add when modifier.StatType == PlayerStatType.FireInterval => $"{(value <= 0f ? "+" : "-")}{Mathf.Abs(value):0.##} {statLabel}",
                    StatModifierOperation.Add => $"{(value >= 0f ? "+" : string.Empty)}{value:0.##} {statLabel}",
                    StatModifierOperation.Multiply => $"{(value >= 1f ? "x" : "cut ")}{value:0.##} {statLabel}",
                    StatModifierOperation.Override => $"SET {statLabel}",
                    _ => string.Empty
                };

                if (!string.IsNullOrWhiteSpace(chunk))
                {
                    SummaryChunks.Add(chunk);
                }
            }
        }

        private static void AppendProjectileSummaryChunks(IReadOnlyList<ProjectileModifier> projectileModifiers)
        {
            if (projectileModifiers == null)
            {
                return;
            }

            for (int index = 0; index < projectileModifiers.Count && SummaryChunks.Count < 3; index++)
            {
                string chunk = projectileModifiers[index].ModifierType switch
                {
                    ProjectileModifierType.Pierce => "PIERCE",
                    ProjectileModifierType.Homing => "HOMING",
                    ProjectileModifierType.MultiShot => "MULTI",
                    ProjectileModifierType.Explode => "EXPLODE",
                    ProjectileModifierType.Laser => "LASER",
                    ProjectileModifierType.Split => "SPLIT",
                    ProjectileModifierType.Bounce => "BOUNCE",
                    ProjectileModifierType.Orbit => "ORBIT",
                    ProjectileModifierType.Shield => "SHIELD",
                    ProjectileModifierType.Lifesteal => "LIFESTEAL",
                    _ => string.Empty
                };

                if (!string.IsNullOrWhiteSpace(chunk))
                {
                    SummaryChunks.Add(chunk);
                }
            }
        }

        private static bool TryResolveProjectedProjectileProfileCompareLabel(
            IReadOnlyList<ProjectileModifier> projectileModifiers,
            PlayerStats playerStats,
            out string compareLabel)
        {
            compareLabel = string.Empty;

            if (!TryResolveProjectedProjectileProfile(projectileModifiers, playerStats, out string projectedLabel))
            {
                return false;
            }

            compareLabel = projectedLabel == "BASELINE"
                ? "SHOT RESET"
                : projectedLabel;
            return !string.IsNullOrWhiteSpace(compareLabel);
        }

        private static bool TryResolveProjectedProjectileProfileOutcomeLabel(
            IReadOnlyList<ProjectileModifier> projectileModifiers,
            PlayerStats playerStats,
            out string outcomeLabel)
        {
            outcomeLabel = string.Empty;

            if (!TryResolveProjectedProjectileProfile(projectileModifiers, playerStats, out string projectedLabel))
            {
                return false;
            }

            outcomeLabel = projectedLabel == "BASELINE"
                ? "SHOT: BASELINE"
                : $"SHOT: {projectedLabel}";
            return true;
        }

        private static bool TryResolveProjectedProjectileProfile(
            IReadOnlyList<ProjectileModifier> projectileModifiers,
            PlayerStats playerStats,
            out string profileLabel)
        {
            profileLabel = string.Empty;

            if (projectileModifiers == null || projectileModifiers.Count == 0)
            {
                return false;
            }

            ProjectileTraitState baseTraits = playerStats != null
                ? playerStats.CurrentProjectileTraits
                : ProjectileTraitState.Default;
            ProjectileTraitState projectedTraits = baseTraits;
            bool touchedTrait = false;

            for (int index = 0; index < projectileModifiers.Count; index++)
            {
                ProjectileModifier modifier = projectileModifiers[index];
                switch (modifier.ModifierType)
                {
                    case ProjectileModifierType.Explode:
                    case ProjectileModifierType.Laser:
                    case ProjectileModifierType.Split:
                    case ProjectileModifierType.Bounce:
                    case ProjectileModifierType.Orbit:
                    case ProjectileModifierType.Shield:
                    case ProjectileModifierType.Lifesteal:
                        ProjectileTraitResolver.Apply(modifier, ref projectedTraits);
                        touchedTrait = true;
                        break;
                }
            }

            if (!touchedTrait)
            {
                return false;
            }

            string baseLabel = ResolveProjectileTraitProfileLabel(baseTraits);
            string projectedLabel = ResolveProjectileTraitProfileLabel(projectedTraits);
            if (string.Equals(baseLabel, projectedLabel, System.StringComparison.Ordinal))
            {
                return false;
            }

            profileLabel = projectedLabel;
            return true;
        }

        private static string ResolveProjectileTraitProfileLabel(ProjectileTraitState traits)
        {
            string[] labels = new string[7];
            int count = 0;

            if (traits.IsLaser)
            {
                labels[count++] = "LASER";
            }

            if (traits.IsOrbiting)
            {
                labels[count++] = "ORBIT";
            }

            if (traits.IsShielded)
            {
                labels[count++] = "SHIELD";
            }

            if (traits.IsSplit)
            {
                labels[count++] = "SPLIT";
            }

            if (traits.IsExplosive)
            {
                labels[count++] = "BLAST";
            }

            if (traits.Has(ProjectileTraitFlags.Lifesteal) && traits.LifestealStrength > 0.01f)
            {
                labels[count++] = "LEECH";
            }

            if (traits.Has(ProjectileTraitFlags.Bounce) && traits.BounceStrength > 0.01f)
            {
                labels[count++] = "BOUNCE";
            }

            if (count == 0)
            {
                return "BASELINE";
            }

            if (count == 1)
            {
                return labels[0];
            }

            if (count == 2)
            {
                return $"{labels[0]} / {labels[1]}";
            }

            return $"{labels[0]} / {labels[1]} / +{count - 2}";
        }

        private static float ResolvePositiveModifierStrength(StatModifier modifier, float baseline, bool higherIsBetter)
        {
            float safeBaseline = Mathf.Max(0.05f, baseline);

            return modifier.Operation switch
            {
                StatModifierOperation.Add when higherIsBetter => modifier.Value / safeBaseline,
                StatModifierOperation.Add => -modifier.Value / safeBaseline,
                StatModifierOperation.Multiply when higherIsBetter => modifier.Value - 1f,
                StatModifierOperation.Multiply => 1f - modifier.Value,
                StatModifierOperation.Override when higherIsBetter => (modifier.Value - safeBaseline) / safeBaseline,
                StatModifierOperation.Override => (safeBaseline - modifier.Value) / safeBaseline,
                _ => 0f
            };
        }

        private static Color ResolvePassiveCompareColor(string compareLabel, Color accentColor)
        {
            return compareLabel switch
            {
                "TRADE OFF" => new Color(1f, 0.62f, 0.34f, 1f),
                _ => Color.Lerp(accentColor, Color.white, 0.18f)
            };
        }

        private static Color ResolveShopRewardAccent(ShopItem shopItem)
        {
            if (shopItem?.ShopItemData == null)
            {
                return RoomTraversalGuidanceController.ResolveRoomAccent(Data.Dungeon.RoomType.Shop);
            }

            ShopOffer offer = shopItem.ShopItemData.Offer;
            return offer.RewardType switch
            {
                ShopOfferRewardType.PassiveItem when offer.PassiveItem != null => ResolveItemAccent(offer.PassiveItem.Rarity),
                ShopOfferRewardType.Health => new Color(1f, 0.52f, 0.62f, 1f),
                ShopOfferRewardType.Ammo => new Color(0.96f, 0.78f, 0.28f, 1f),
                ShopOfferRewardType.Coins => new Color(0.96f, 0.84f, 0.28f, 1f),
                ShopOfferRewardType.Keys => new Color(0.74f, 0.88f, 1f, 1f),
                ShopOfferRewardType.Bombs => new Color(1f, 0.56f, 0.24f, 1f),
                _ => RoomTraversalGuidanceController.ResolveRoomAccent(Data.Dungeon.RoomType.Shop)
            };
        }

        private static Color ResolveCurrencyAccent(ShopCurrencyType currencyType)
        {
            return currencyType switch
            {
                ShopCurrencyType.Keys => new Color(0.74f, 0.88f, 1f, 1f),
                ShopCurrencyType.Bombs => new Color(1f, 0.56f, 0.24f, 1f),
                _ => new Color(0.96f, 0.84f, 0.28f, 1f)
            };
        }

        private static Color ResolveConsumableAccent(ConsumableItemData consumableItemData)
        {
            if (consumableItemData == null)
            {
                return new Color(0.74f, 0.88f, 1f, 1f);
            }

            if (consumableItemData.HealAmount > 0f)
            {
                return new Color(1f, 0.52f, 0.62f, 1f);
            }

            if (consumableItemData.CoinGain > 0)
            {
                return new Color(0.96f, 0.84f, 0.28f, 1f);
            }

            if (consumableItemData.KeyGain > 0)
            {
                return new Color(0.74f, 0.88f, 1f, 1f);
            }

            if (consumableItemData.BombGain > 0)
            {
                return new Color(1f, 0.56f, 0.24f, 1f);
            }

            if (consumableItemData.HasTimedEffect)
            {
                return new Color(0.58f, 0.92f, 1f, 1f);
            }

            return new Color(0.78f, 0.84f, 1f, 1f);
        }

        private static Color ResolveItemAccent(ItemRarity rarity)
        {
            return rarity switch
            {
                ItemRarity.Uncommon => new Color(0.52f, 1f, 0.68f, 1f),
                ItemRarity.Rare => new Color(1f, 0.84f, 0.42f, 1f),
                ItemRarity.Legendary => new Color(1f, 0.48f, 0.82f, 1f),
                ItemRarity.Relic => new Color(1f, 0.92f, 0.6f, 1f),
                ItemRarity.Boss => new Color(1f, 0.4f, 0.4f, 1f),
                _ => new Color(0.72f, 0.85f, 1f, 1f)
            };
        }

        private static Color NormalizeAccent(Color color, Color fallback)
        {
            return color.a > 0.01f
                ? new Color(color.r, color.g, color.b, 1f)
                : fallback;
        }

        private static string NormalizeText(string text)
        {
            return string.IsNullOrWhiteSpace(text)
                ? string.Empty
                : text.Trim();
        }
    }
}
