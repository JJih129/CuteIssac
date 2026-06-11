using CuteIssac.Data.Dungeon;
using CuteIssac.Room;
using UnityEngine;

namespace CuteIssac.Core.Gameplay
{
    public readonly struct SpecialRoomDealOfferedSignal
    {
        public SpecialRoomDealOfferedSignal(
            RoomController room,
            SpecialRoomDealType dealType,
            RoomType roomType,
            string ruleId,
            int offerCount)
        {
            Room = room;
            DealType = dealType;
            RoomType = roomType;
            RuleId = ruleId ?? string.Empty;
            OfferCount = Mathf.Max(0, offerCount);
        }

        public RoomController Room { get; }
        public SpecialRoomDealType DealType { get; }
        public RoomType RoomType { get; }
        public string RuleId { get; }
        public int OfferCount { get; }
        public bool IsValid => DealType != SpecialRoomDealType.None && OfferCount > 0;
    }
}
