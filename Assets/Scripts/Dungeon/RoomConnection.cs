using System;

namespace CuteIssac.Dungeon
{
    /// <summary>
    /// Runtime graph edge between two room positions.
    /// This is intentionally lightweight so the generator can build and mutate connections cheaply.
    /// </summary>
    [Serializable]
    public readonly struct RoomConnection
    {
        public RoomConnection(RoomDirection direction, GridPosition targetPosition)
        {
            Direction = direction;
            TargetPosition = targetPosition;
        }

        public RoomDirection Direction { get; }
        public GridPosition TargetPosition { get; }
    }
}
