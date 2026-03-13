namespace CuteIssac.Dungeon
{
    /// <summary>
    /// Shared helpers for cardinal room directions.
    /// Centralizing these conversions keeps graph generation readable and avoids duplicated switch statements.
    /// </summary>
    public static class RoomDirectionUtility
    {
        private static readonly RoomDirection[] AllDirections =
        {
            RoomDirection.Up,
            RoomDirection.Right,
            RoomDirection.Down,
            RoomDirection.Left
        };
        private static readonly System.Collections.Generic.IReadOnlyList<RoomDirection> DirectionList = AllDirections;

        public static System.Collections.Generic.IReadOnlyList<RoomDirection> Directions => DirectionList;

        public static GridPosition ToOffset(RoomDirection direction)
        {
            return direction switch
            {
                RoomDirection.Up => new GridPosition(0, 1),
                RoomDirection.Right => new GridPosition(1, 0),
                RoomDirection.Down => new GridPosition(0, -1),
                RoomDirection.Left => new GridPosition(-1, 0),
                _ => GridPosition.Zero
            };
        }

        public static RoomDirection Opposite(RoomDirection direction)
        {
            return direction switch
            {
                RoomDirection.Up => RoomDirection.Down,
                RoomDirection.Right => RoomDirection.Left,
                RoomDirection.Down => RoomDirection.Up,
                RoomDirection.Left => RoomDirection.Right,
                _ => RoomDirection.Up
            };
        }

        public static RoomDoorMask ToDoorMask(RoomDirection direction)
        {
            return direction switch
            {
                RoomDirection.Up => RoomDoorMask.Up,
                RoomDirection.Right => RoomDoorMask.Right,
                RoomDirection.Down => RoomDoorMask.Down,
                RoomDirection.Left => RoomDoorMask.Left,
                _ => RoomDoorMask.None
            };
        }

        public static RoomDoorMask ToDoorMask(System.Collections.Generic.IReadOnlyList<RoomConnection> connections)
        {
            RoomDoorMask mask = RoomDoorMask.None;

            if (connections == null)
            {
                return mask;
            }

            for (int i = 0; i < connections.Count; i++)
            {
                mask |= ToDoorMask(connections[i].Direction);
            }

            return mask;
        }
    }
}
