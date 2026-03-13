using System;
using System.Collections.Generic;
using CuteIssac.Data.Dungeon;
using CuteIssac.Room;
using CuteIssac.UI;

namespace CuteIssac.Dungeon
{
    /// <summary>
    /// Tracks runtime exploration state for one instantiated dungeon.
    /// This class stays UI-agnostic so multiple minimap views or save systems can reuse the same room visit data.
    /// </summary>
    public sealed class DungeonExplorationTracker : IDisposable
    {
        private sealed class RoomExplorationRecord
        {
            public RoomExplorationRecord(GridPosition gridPosition, RoomType roomType, RoomController roomController)
            {
                GridPosition = gridPosition;
                RoomType = roomType;
                RoomController = roomController;
            }

            public GridPosition GridPosition { get; }
            public RoomType RoomType { get; }
            public RoomController RoomController { get; }
            public bool IsVisited { get; set; }
            public bool IsCurrent { get; set; }
            public bool IsCleared { get; set; }
        }

        private readonly Dictionary<GridPosition, RoomExplorationRecord> _recordsByPosition = new();
        private readonly Dictionary<RoomController, GridPosition> _positionByRoom = new();
        private RoomNavigationController _roomNavigationController;

        public event Action Changed;

        public bool HasData => _recordsByPosition.Count > 0;

        public void Initialize(DungeonInstantiationResult instantiationResult, RoomNavigationController roomNavigationController)
        {
            Reset();

            if (instantiationResult == null || instantiationResult.DungeonMap == null)
            {
                Changed?.Invoke();
                return;
            }

            _roomNavigationController = roomNavigationController;

            foreach (KeyValuePair<GridPosition, DungeonRoomNode> roomPair in instantiationResult.DungeonMap.RoomsByPosition)
            {
                if (!instantiationResult.TryGetRoom(roomPair.Key, out RoomController roomController) || roomController == null)
                {
                    continue;
                }

                RoomExplorationRecord record = new(roomPair.Key, roomPair.Value.RoomType, roomController);
                _recordsByPosition.Add(roomPair.Key, record);
                _positionByRoom.Add(roomController, roomPair.Key);
                roomController.RoomCleared += HandleRoomCleared;
            }

            if (_roomNavigationController != null)
            {
                _roomNavigationController.CurrentRoomChanged += HandleCurrentRoomChanged;
            }

            RoomController initialRoom = _roomNavigationController != null && _roomNavigationController.CurrentRoom != null
                ? _roomNavigationController.CurrentRoom
                : instantiationResult.StartRoom;

            if (initialRoom != null)
            {
                SetCurrentRoom(initialRoom);
            }

            Changed?.Invoke();
        }

        public void BuildViewData(List<MinimapRoomViewData> buffer)
        {
            if (buffer == null)
            {
                return;
            }

            buffer.Clear();

            foreach (RoomExplorationRecord record in _recordsByPosition.Values)
            {
                RoomExplorationState explorationState = RoomExplorationState.Unknown;

                if (record.IsCurrent)
                {
                    explorationState = RoomExplorationState.Current;
                }
                else if (record.IsCleared)
                {
                    explorationState = RoomExplorationState.Cleared;
                }
                else if (record.IsVisited)
                {
                    explorationState = RoomExplorationState.Visited;
                }

                buffer.Add(new MinimapRoomViewData(record.GridPosition, record.RoomType, explorationState, record.IsCleared));
            }
        }

        public void Reset()
        {
            if (_roomNavigationController != null)
            {
                _roomNavigationController.CurrentRoomChanged -= HandleCurrentRoomChanged;
                _roomNavigationController = null;
            }

            foreach (RoomExplorationRecord record in _recordsByPosition.Values)
            {
                if (record.RoomController != null)
                {
                    record.RoomController.RoomCleared -= HandleRoomCleared;
                }
            }

            _recordsByPosition.Clear();
            _positionByRoom.Clear();
        }

        public void Dispose()
        {
            Reset();
        }

        private void HandleCurrentRoomChanged(RoomController roomController)
        {
            SetCurrentRoom(roomController);
            Changed?.Invoke();
        }

        private void HandleRoomCleared(RoomController roomController)
        {
            if (roomController == null || !_positionByRoom.TryGetValue(roomController, out GridPosition gridPosition))
            {
                return;
            }

            if (_recordsByPosition.TryGetValue(gridPosition, out RoomExplorationRecord record))
            {
                record.IsVisited = true;
                record.IsCleared = true;
                Changed?.Invoke();
            }
        }

        private void SetCurrentRoom(RoomController roomController)
        {
            foreach (RoomExplorationRecord record in _recordsByPosition.Values)
            {
                record.IsCurrent = false;
            }

            if (roomController == null || !_positionByRoom.TryGetValue(roomController, out GridPosition gridPosition))
            {
                return;
            }

            if (_recordsByPosition.TryGetValue(gridPosition, out RoomExplorationRecord targetRecord))
            {
                targetRecord.IsVisited = true;
                targetRecord.IsCurrent = true;
            }
        }
    }
}
