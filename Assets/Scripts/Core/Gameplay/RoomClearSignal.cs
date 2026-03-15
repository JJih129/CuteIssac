using CuteIssac.Room;

namespace CuteIssac.Core.Gameplay
{
    public readonly struct RoomClearSignal
    {
        public RoomClearSignal(RoomController room, bool hadCombatEncounter)
        {
            Room = room;
            HadCombatEncounter = hadCombatEncounter;
        }

        public RoomController Room { get; }
        public bool HadCombatEncounter { get; }
    }
}
