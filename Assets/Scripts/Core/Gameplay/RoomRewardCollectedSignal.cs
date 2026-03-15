using CuteIssac.Data.Dungeon;
using CuteIssac.Room;

namespace CuteIssac.Core.Gameplay
{
    public readonly struct RoomRewardCollectedSignal
    {
        public RoomRewardCollectedSignal(RoomController room, RoomType roomType)
        {
            Room = room;
            RoomType = roomType;
        }

        public RoomController Room { get; }
        public RoomType RoomType { get; }
        public bool IsValid => Room != null;
    }
}
