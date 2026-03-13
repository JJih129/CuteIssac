using System;
using System.Collections.Generic;
using CuteIssac.Data.Dungeon;
using CuteIssac.Enemy;

namespace CuteIssac.Dungeon
{
    /// <summary>
    /// Runtime room node created by a generator.
    /// It references authoring data while storing generated position and connection state for the current floor.
    /// </summary>
    [Serializable]
    public sealed class DungeonRoomNode
    {
        private readonly List<RoomConnection> _connections = new();
        private RoomData _roomData;
        private RoomType _roomType;
        private RoomLayoutData _resolvedLayout;
        private EnemyWaveAssignment _assignedEnemyWave;

        public DungeonRoomNode(RoomData roomData, GridPosition gridPosition)
            : this(roomData != null ? roomData.RoomType : RoomType.Normal, roomData, gridPosition)
        {
        }

        public DungeonRoomNode(RoomType roomType, RoomData roomData, GridPosition gridPosition)
        {
            _roomType = roomType;
            _roomData = roomData;
            GridPosition = gridPosition;
        }

        public RoomData RoomData => _roomData;
        public RoomLayoutData ResolvedLayout => _resolvedLayout;
        public EnemyWaveAssignment AssignedEnemyWave => _assignedEnemyWave;
        public GridPosition GridPosition { get; }
        public RoomType RoomType => _roomType;
        public IReadOnlyList<RoomConnection> Connections => _connections;
        public int DistanceFromStart { get; private set; } = -1;

        public bool HasConnection(RoomDirection direction)
        {
            for (int i = 0; i < _connections.Count; i++)
            {
                if (_connections[i].Direction == direction)
                {
                    return true;
                }
            }

            return false;
        }

        public void AddConnection(RoomConnection connection)
        {
            if (HasConnection(connection.Direction))
            {
                return;
            }

            _connections.Add(connection);
        }

        public void SetDistanceFromStart(int distanceFromStart)
        {
            DistanceFromStart = distanceFromStart;
        }

        /// <summary>
        /// Allows post-processing passes to retag a generated room without rebuilding the graph.
        /// Boss, treasure, and shop assignment should all go through this path.
        /// </summary>
        public void ApplyGeneratedMetadata(RoomType roomType, RoomData roomData)
        {
            _roomType = roomType;
            _roomData = roomData;
            _resolvedLayout = null;
            _assignedEnemyWave = null;
        }

        public void SetResolvedLayout(RoomLayoutData roomLayoutData)
        {
            _resolvedLayout = roomLayoutData;
        }

        public void SetAssignedEnemyWave(EnemyWaveAssignment enemyWaveAssignment)
        {
            _assignedEnemyWave = enemyWaveAssignment;
        }
    }
}
