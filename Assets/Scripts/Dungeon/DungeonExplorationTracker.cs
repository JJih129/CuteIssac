using System;
using System.Collections.Generic;
using CuteIssac.Core.Gameplay;
using CuteIssac.Data.Dungeon;
using CuteIssac.Room;
using CuteIssac.UI;

namespace CuteIssac.Dungeon
{
    /// <summary>
    /// Tracks exploration state for the generated dungeon independently from any specific minimap skin.
    /// UI layers consume immutable snapshots so room logic never needs to know about canvas objects.
    /// </summary>
    public sealed class DungeonExplorationTracker : IDisposable
    {
        private sealed class RoomExplorationRecord
        {
            public RoomExplorationRecord(DungeonRoomNode roomNode, RoomController roomController)
            {
                RoomNode = roomNode;
                RoomController = roomController;
                ExplorationState = RoomExplorationState.Hidden;
            }

            public DungeonRoomNode RoomNode { get; }
            public RoomController RoomController { get; }
            public RoomExplorationState ExplorationState { get; set; }
            public bool IsCurrent { get; set; }
            public bool IsCleared { get; set; }
            public bool HasRewardContent { get; set; }
            public bool HasCollectedRewardContent { get; set; }
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

                RoomExplorationRecord record = new(roomPair.Value, roomController);
                if (roomPair.Value.RoomType != RoomType.Secret)
                {
                    record.ExplorationState = RoomExplorationState.Discovered;
                }

                _recordsByPosition.Add(roomPair.Key, record);
                _positionByRoom.Add(roomController, roomPair.Key);
            }

            GameplayRuntimeEvents.RoomResolved += HandleRoomResolved;
            GameplayRuntimeEvents.RoomRewardPhaseCompleted += HandleRoomRewardPhaseCompleted;
            GameplayRuntimeEvents.RoomRewardCollected += HandleRoomRewardCollected;
            GameplayRuntimeEvents.SecretRoomRevealed += HandleSecretRoomRevealed;

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
                buffer.Add(new MinimapRoomViewData(
                    record.RoomNode.GridPosition,
                    record.RoomNode.RoomType,
                    ResolveVisibleState(record),
                    ShouldDisplayConnection(record, RoomDirection.Up),
                    ShouldDisplayConnection(record, RoomDirection.Down),
                    ShouldDisplayConnection(record, RoomDirection.Left),
                    ShouldDisplayConnection(record, RoomDirection.Right),
                    IsVisibleSecretConnection(record, RoomDirection.Up),
                    IsVisibleSecretConnection(record, RoomDirection.Down),
                    IsVisibleSecretConnection(record, RoomDirection.Left),
                    IsVisibleSecretConnection(record, RoomDirection.Right),
                    record.IsCleared,
                    record.HasRewardContent,
                    record.HasCollectedRewardContent));
            }
        }

        public void BuildSaveData(List<RoomExplorationSaveRecord> buffer)
        {
            if (buffer == null)
            {
                return;
            }

            buffer.Clear();

            foreach (RoomExplorationRecord record in _recordsByPosition.Values)
            {
                buffer.Add(new RoomExplorationSaveRecord(
                    record.RoomNode.GridPosition.X,
                    record.RoomNode.GridPosition.Y,
                    record.RoomNode.RoomType,
                    ResolveVisibleState(record),
                    record.IsCleared,
                    record.HasRewardContent,
                    record.HasCollectedRewardContent));
            }
        }

        public void ApplySaveData(IReadOnlyList<RoomExplorationSaveRecord> records)
        {
            if (records == null || records.Count == 0)
            {
                return;
            }

            for (int index = 0; index < records.Count; index++)
            {
                RoomExplorationSaveRecord saveRecord = records[index];
                GridPosition gridPosition = new(saveRecord.GridX, saveRecord.GridY);

                if (!_recordsByPosition.TryGetValue(gridPosition, out RoomExplorationRecord runtimeRecord))
                {
                    continue;
                }

                RoomExplorationState restoredState = saveRecord.ExplorationState == RoomExplorationState.Current
                    ? saveRecord.IsCleared ? RoomExplorationState.Cleared : RoomExplorationState.Visited
                    : saveRecord.ExplorationState;
                PromoteExploration(runtimeRecord, restoredState);
                runtimeRecord.IsCleared = runtimeRecord.IsCleared
                    || saveRecord.IsCleared
                    || saveRecord.ExplorationState == RoomExplorationState.Cleared;
                if (runtimeRecord.IsCleared)
                {
                    PromoteExploration(runtimeRecord, RoomExplorationState.Cleared);
                }

                runtimeRecord.HasRewardContent = saveRecord.HasRewardContent;
                runtimeRecord.HasCollectedRewardContent = saveRecord.HasRewardContent && saveRecord.HasCollectedRewardContent;
            }

            if (_roomNavigationController != null && _roomNavigationController.CurrentRoom != null)
            {
                SetCurrentRoom(_roomNavigationController.CurrentRoom);
            }

            Changed?.Invoke();
        }

        public bool TryRevealRoom(GridPosition gridPosition)
        {
            if (!_recordsByPosition.TryGetValue(gridPosition, out RoomExplorationRecord record))
            {
                return false;
            }

            PromoteExploration(record, RoomExplorationState.Discovered);
            Changed?.Invoke();
            return true;
        }

        public void Reset()
        {
            if (_roomNavigationController != null)
            {
                _roomNavigationController.CurrentRoomChanged -= HandleCurrentRoomChanged;
                _roomNavigationController = null;
            }

            GameplayRuntimeEvents.RoomResolved -= HandleRoomResolved;
            GameplayRuntimeEvents.RoomRewardPhaseCompleted -= HandleRoomRewardPhaseCompleted;
            GameplayRuntimeEvents.RoomRewardCollected -= HandleRoomRewardCollected;
            GameplayRuntimeEvents.SecretRoomRevealed -= HandleSecretRoomRevealed;

            _recordsByPosition.Clear();
            _positionByRoom.Clear();
        }

        public void Dispose()
        {
            Reset();
        }

        private static RoomExplorationState ResolveVisibleState(RoomExplorationRecord record)
        {
            if (record == null)
            {
                return RoomExplorationState.Hidden;
            }

            if (record.IsCurrent)
            {
                return RoomExplorationState.Current;
            }

            if (record.IsCleared)
            {
                return RoomExplorationState.Cleared;
            }

            return record.ExplorationState;
        }

        private static void PromoteExploration(RoomExplorationRecord record, RoomExplorationState nextState)
        {
            if (record == null || nextState == RoomExplorationState.Current)
            {
                return;
            }

            if ((int)nextState > (int)record.ExplorationState)
            {
                record.ExplorationState = nextState;
            }
        }

        private bool ShouldDisplayConnection(RoomExplorationRecord record, RoomDirection direction)
        {
            if (record == null || !record.RoomNode.HasConnection(direction))
            {
                return false;
            }

            if (!IsVisibleOnMinimap(record))
            {
                return false;
            }

            GridPosition targetPosition = record.RoomNode.GridPosition + RoomDirectionUtility.ToOffset(direction);

            if (!_recordsByPosition.TryGetValue(targetPosition, out RoomExplorationRecord adjacentRecord))
            {
                return false;
            }

            if (!IsVisibleOnMinimap(adjacentRecord))
            {
                return false;
            }

            if (record.RoomNode.RoomType == RoomType.Treasure || adjacentRecord.RoomNode.RoomType == RoomType.Treasure)
            {
                return false;
            }

            return true;
        }

        private bool IsVisibleSecretConnection(RoomExplorationRecord record, RoomDirection direction)
        {
            if (!ShouldDisplayConnection(record, direction))
            {
                return false;
            }

            GridPosition targetPosition = record.RoomNode.GridPosition + RoomDirectionUtility.ToOffset(direction);
            return _recordsByPosition.TryGetValue(targetPosition, out RoomExplorationRecord adjacentRecord)
                && adjacentRecord.RoomNode.RoomType == RoomType.Secret;
        }

        private static bool IsVisibleOnMinimap(RoomExplorationRecord record)
        {
            return ResolveVisibleState(record) != RoomExplorationState.Hidden;
        }

        private void HandleCurrentRoomChanged(RoomController roomController)
        {
            SetCurrentRoom(roomController);
            Changed?.Invoke();
        }

        private void HandleRoomResolved(RoomResolvedSignal signal)
        {
            RoomController roomController = signal.Room;

            if (roomController == null || !_positionByRoom.TryGetValue(roomController, out GridPosition gridPosition))
            {
                return;
            }

            if (_recordsByPosition.TryGetValue(gridPosition, out RoomExplorationRecord record))
            {
                record.IsCleared = true;
                PromoteExploration(record, RoomExplorationState.Cleared);
                Changed?.Invoke();
            }
        }

        private void HandleRoomRewardPhaseCompleted(RoomRewardPhaseSignal signal)
        {
            RoomController roomController = signal.Room;

            if (roomController == null || !_positionByRoom.TryGetValue(roomController, out GridPosition gridPosition))
            {
                return;
            }

            if (_recordsByPosition.TryGetValue(gridPosition, out RoomExplorationRecord record))
            {
                record.HasRewardContent = signal.HasRewards;
                if (!signal.HasRewards)
                {
                    record.HasCollectedRewardContent = false;
                }

                Changed?.Invoke();
            }
        }

        private void HandleRoomRewardCollected(RoomRewardCollectedSignal signal)
        {
            RoomController roomController = signal.Room;

            if (roomController == null || !_positionByRoom.TryGetValue(roomController, out GridPosition gridPosition))
            {
                return;
            }

            if (_recordsByPosition.TryGetValue(gridPosition, out RoomExplorationRecord record))
            {
                record.HasCollectedRewardContent = record.HasRewardContent;
                Changed?.Invoke();
            }
        }

        private void HandleSecretRoomRevealed(SecretRoomRevealedSignal signal)
        {
            RoomController secretRoom = signal.SecretRoom;

            if (secretRoom == null || !_positionByRoom.TryGetValue(secretRoom, out GridPosition gridPosition))
            {
                return;
            }

            if (_recordsByPosition.TryGetValue(gridPosition, out RoomExplorationRecord record))
            {
                PromoteExploration(record, RoomExplorationState.Discovered);
                Changed?.Invoke();
            }
        }

        private void SetCurrentRoom(RoomController roomController)
        {
            foreach (RoomExplorationRecord record in _recordsByPosition.Values)
            {
                if (!record.IsCurrent)
                {
                    continue;
                }

                record.IsCurrent = false;
                PromoteExploration(record, record.IsCleared ? RoomExplorationState.Cleared : RoomExplorationState.Visited);
            }

            if (roomController == null || !_positionByRoom.TryGetValue(roomController, out GridPosition gridPosition))
            {
                return;
            }

            if (!_recordsByPosition.TryGetValue(gridPosition, out RoomExplorationRecord targetRecord))
            {
                return;
            }

            PromoteExploration(targetRecord, targetRecord.IsCleared ? RoomExplorationState.Cleared : RoomExplorationState.Visited);
            targetRecord.IsCurrent = true;
            RevealAdjacentRooms(targetRecord.RoomNode);
        }

        private void RevealAdjacentRooms(DungeonRoomNode currentRoomNode)
        {
            IReadOnlyList<RoomConnection> connections = currentRoomNode.Connections;

            for (int index = 0; index < connections.Count; index++)
            {
                GridPosition targetPosition = connections[index].TargetPosition;

                if (!_recordsByPosition.TryGetValue(targetPosition, out RoomExplorationRecord adjacentRecord))
                {
                    continue;
                }

                if (adjacentRecord.RoomNode.RoomType == RoomType.Secret)
                {
                    continue;
                }

                PromoteExploration(adjacentRecord, RoomExplorationState.Discovered);
            }
        }
    }
}
