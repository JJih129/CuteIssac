using System;
using System.Collections.Generic;
using CuteIssac.Room;
using UnityEngine;

namespace CuteIssac.Dungeon
{
    /// <summary>
    /// Runtime snapshot of one scene instantiation pass.
    /// Keeping the generated room references together makes cleanup and later systems easier to coordinate.
    /// </summary>
    [Serializable]
    public sealed class DungeonInstantiationResult
    {
        private readonly Dictionary<GridPosition, RoomController> _roomsByPosition = new();

        public DungeonInstantiationResult(DungeonMap dungeonMap, Transform root)
        {
            DungeonMap = dungeonMap;
            Root = root;
        }

        public DungeonMap DungeonMap { get; }
        public Transform Root { get; }
        public RoomController StartRoom { get; private set; }
        public IReadOnlyDictionary<GridPosition, RoomController> RoomsByPosition => _roomsByPosition;

        public void RegisterRoom(GridPosition position, RoomController roomController, bool isStartRoom)
        {
            if (roomController == null)
            {
                return;
            }

            _roomsByPosition[position] = roomController;

            if (isStartRoom)
            {
                StartRoom = roomController;
            }
        }

        public bool TryGetRoom(GridPosition position, out RoomController roomController)
        {
            return _roomsByPosition.TryGetValue(position, out roomController);
        }

        public RoomController[] BuildRoomArray()
        {
            RoomController[] rooms = new RoomController[_roomsByPosition.Count];
            int index = 0;

            foreach (KeyValuePair<GridPosition, RoomController> roomPair in _roomsByPosition)
            {
                rooms[index] = roomPair.Value;
                index++;
            }

            return rooms;
        }
    }
}
