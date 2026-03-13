using System;
using System.Collections.Generic;
using CuteIssac.Data.Dungeon;

namespace CuteIssac.Dungeon
{
    /// <summary>
    /// Runtime container for one generated floor layout.
    /// DungeonGenerator should output this object, and scene builders/navigation systems can consume it afterward.
    /// </summary>
    [Serializable]
    public sealed class DungeonMap
    {
        private readonly Dictionary<GridPosition, DungeonRoomNode> _roomsByPosition = new();

        public DungeonMap(FloorConfig floorConfig)
        {
            FloorConfig = floorConfig;
        }

        public FloorConfig FloorConfig { get; }
        public int RoomCount => _roomsByPosition.Count;
        public IReadOnlyDictionary<GridPosition, DungeonRoomNode> RoomsByPosition => _roomsByPosition;

        public bool TryAddRoom(DungeonRoomNode roomNode)
        {
            if (roomNode == null || _roomsByPosition.ContainsKey(roomNode.GridPosition))
            {
                return false;
            }

            _roomsByPosition.Add(roomNode.GridPosition, roomNode);
            return true;
        }

        public bool TryGetRoom(GridPosition gridPosition, out DungeonRoomNode roomNode)
        {
            return _roomsByPosition.TryGetValue(gridPosition, out roomNode);
        }

        public bool ContainsRoom(GridPosition gridPosition)
        {
            return _roomsByPosition.ContainsKey(gridPosition);
        }

        public void ConnectRooms(GridPosition from, RoomDirection direction, GridPosition to)
        {
            if (!_roomsByPosition.TryGetValue(from, out DungeonRoomNode fromRoom))
            {
                return;
            }

            if (!_roomsByPosition.ContainsKey(to))
            {
                return;
            }

            fromRoom.AddConnection(new RoomConnection(direction, to));
        }
    }
}
