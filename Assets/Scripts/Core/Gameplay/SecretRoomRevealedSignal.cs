using CuteIssac.Dungeon;
using CuteIssac.Room;

namespace CuteIssac.Core.Gameplay
{
    public readonly struct SecretRoomRevealedSignal
    {
        public SecretRoomRevealedSignal(RoomController sourceRoom, RoomController secretRoom, RoomDirection revealDirection)
        {
            SourceRoom = sourceRoom;
            SecretRoom = secretRoom;
            RevealDirection = revealDirection;
        }

        public RoomController SourceRoom { get; }
        public RoomController SecretRoom { get; }
        public RoomDirection RevealDirection { get; }
    }
}
