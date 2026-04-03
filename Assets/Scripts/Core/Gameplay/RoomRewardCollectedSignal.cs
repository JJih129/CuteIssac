using CuteIssac.Data.Dungeon;
using CuteIssac.Room;
using UnityEngine;

namespace CuteIssac.Core.Gameplay
{
    public readonly struct RoomRewardCollectedSignal
    {
        public RoomRewardCollectedSignal(
            RoomController room,
            RoomType roomType,
            int remainingRewardCount = 0,
            bool isMomentumReward = false,
            bool isHighValueMomentumReward = false,
            Color momentumAccentColor = default)
        {
            Room = room;
            RoomType = roomType;
            RemainingRewardCount = Mathf.Max(0, remainingRewardCount);
            IsMomentumReward = isMomentumReward;
            IsHighValueMomentumReward = isMomentumReward && isHighValueMomentumReward;
            MomentumAccentColor = momentumAccentColor.a > 0.01f
                ? momentumAccentColor
                : new Color(1f, 0.84f, 0.36f, 1f);
        }

        public RoomController Room { get; }
        public RoomType RoomType { get; }
        public int RemainingRewardCount { get; }
        public bool IsMomentumReward { get; }
        public bool IsHighValueMomentumReward { get; }
        public Color MomentumAccentColor { get; }
        public bool IsFinalRewardCollection => RemainingRewardCount <= 0;
        public bool IsValid => Room != null;
    }
}
