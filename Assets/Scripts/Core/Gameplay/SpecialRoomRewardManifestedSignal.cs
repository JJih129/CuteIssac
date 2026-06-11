using CuteIssac.Data.Dungeon;
using CuteIssac.Data.Item;
using CuteIssac.Room;

namespace CuteIssac.Core.Gameplay
{
    public readonly struct SpecialRoomRewardManifestedSignal
    {
        public SpecialRoomRewardManifestedSignal(
            RoomController room,
            RoomType roomType,
            string ruleId,
            SpecialRoomDealType dealType,
            ItemData itemData)
        {
            Room = room;
            RoomType = roomType;
            RuleId = ruleId ?? string.Empty;
            DealType = dealType;
            ItemData = itemData;
        }

        public RoomController Room { get; }
        public RoomType RoomType { get; }
        public string RuleId { get; }
        public SpecialRoomDealType DealType { get; }
        public ItemData ItemData { get; }
        public ItemRarity Rarity => ItemData != null ? ItemData.Rarity : ItemRarity.Common;
        public bool IsDealReward => DealType != SpecialRoomDealType.None;
        public bool IsValid => Room != null && ItemData != null;
    }
}
