using CuteIssac.Data.Dungeon;
using CuteIssac.Room;

namespace CuteIssac.Core.Gameplay
{
    public readonly struct RoomResolvedSignal
    {
        public RoomResolvedSignal(RoomController room, RoomType roomType, bool hadCombatEncounter)
        {
            Room = room;
            RoomType = roomType;
            HadCombatEncounter = hadCombatEncounter;
        }

        public RoomController Room { get; }
        public RoomType RoomType { get; }
        public bool HadCombatEncounter { get; }
    }
}
