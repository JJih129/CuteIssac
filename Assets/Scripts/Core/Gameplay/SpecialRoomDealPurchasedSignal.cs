using CuteIssac.Data.Dungeon;
using CuteIssac.Data.Item;
using CuteIssac.Item;
using UnityEngine;

namespace CuteIssac.Core.Gameplay
{
    public readonly struct SpecialRoomDealPurchasedSignal
    {
        public SpecialRoomDealPurchasedSignal(
            SpecialRoomDealType dealType,
            RoomType roomType,
            string ruleId,
            ShopItem shopItem,
            ItemData itemData,
            int price)
        {
            DealType = dealType;
            RoomType = roomType;
            RuleId = ruleId ?? string.Empty;
            ShopItem = shopItem;
            ItemData = itemData;
            Price = Mathf.Max(0, price);
        }

        public SpecialRoomDealType DealType { get; }
        public RoomType RoomType { get; }
        public string RuleId { get; }
        public ShopItem ShopItem { get; }
        public ItemData ItemData { get; }
        public int Price { get; }
        public bool IsValid => DealType != SpecialRoomDealType.None && ShopItem != null;
    }
}
