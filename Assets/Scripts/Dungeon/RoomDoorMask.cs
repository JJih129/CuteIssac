using System;

namespace CuteIssac.Dungeon
{
    /// <summary>
    /// Bitmask describing which cardinal doorway openings a layout supports.
    /// Layout matching compares a room node's required connections with this mask.
    /// </summary>
    [Flags]
    public enum RoomDoorMask
    {
        None = 0,
        Up = 1 << 0,
        Right = 1 << 1,
        Down = 1 << 2,
        Left = 1 << 3
    }
}
