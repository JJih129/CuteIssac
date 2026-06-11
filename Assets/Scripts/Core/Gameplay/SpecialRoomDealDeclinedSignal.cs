using CuteIssac.Data.Dungeon;

namespace CuteIssac.Core.Gameplay
{
    public readonly struct SpecialRoomDealDeclinedSignal
    {
        public SpecialRoomDealDeclinedSignal(SpecialRoomDealType dealType, RoomType roomType, string ruleId)
        {
            DealType = dealType;
            RoomType = roomType;
            RuleId = ruleId ?? string.Empty;
        }

        public SpecialRoomDealType DealType { get; }
        public RoomType RoomType { get; }
        public string RuleId { get; }
        public bool IsValid => DealType != SpecialRoomDealType.None;
    }
}
