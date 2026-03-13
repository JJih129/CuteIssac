using System.Collections.Generic;
using System.Text;

namespace CuteIssac.Dungeon
{
    /// <summary>
    /// Produces a deterministic text summary of a generated map.
    /// This is meant for fast debug logging before scene instantiation exists.
    /// </summary>
    public static class DungeonMapDebugFormatter
    {
        public static string Format(DungeonMap dungeonMap)
        {
            if (dungeonMap == null)
            {
                return "DungeonMap: <null>";
            }

            List<DungeonRoomNode> orderedRooms = new(dungeonMap.RoomCount);

            foreach (KeyValuePair<GridPosition, DungeonRoomNode> pair in dungeonMap.RoomsByPosition)
            {
                orderedRooms.Add(pair.Value);
            }

            orderedRooms.Sort(CompareRooms);

            StringBuilder builder = new();
            builder.AppendLine($"DungeonMap Floor={(dungeonMap.FloorConfig != null ? dungeonMap.FloorConfig.FloorIndex : 0)} Rooms={dungeonMap.RoomCount}");

            for (int i = 0; i < orderedRooms.Count; i++)
            {
                DungeonRoomNode room = orderedRooms[i];
                RoomDoorMask requiredDoors = RoomDirectionUtility.ToDoorMask(room.Connections);
                builder.Append($"- {room.RoomType} {room.GridPosition}");
                builder.Append($" dist={room.DistanceFromStart}");
                builder.Append($" degree={room.Connections.Count}");
                builder.Append($" doors={requiredDoors}");

                if (room.RoomData != null && !string.IsNullOrWhiteSpace(room.RoomData.RoomId))
                {
                    builder.Append($" id={room.RoomData.RoomId}");
                }

                if (room.ResolvedLayout != null && !string.IsNullOrWhiteSpace(room.ResolvedLayout.LayoutId))
                {
                    builder.Append($" layout={room.ResolvedLayout.LayoutId}");
                }

                if (room.AssignedEnemyWave != null)
                {
                    builder.Append($" wave={room.AssignedEnemyWave.WaveId}");
                    builder.Append($" enemies={room.AssignedEnemyWave.TotalEnemyCount}");
                    builder.Append($" budget={room.AssignedEnemyWave.TargetBudget}");
                }

                builder.Append(" links=");

                for (int j = 0; j < room.Connections.Count; j++)
                {
                    RoomConnection connection = room.Connections[j];
                    builder.Append(connection.Direction);
                    builder.Append("->");
                    builder.Append(connection.TargetPosition);

                    if (j < room.Connections.Count - 1)
                    {
                        builder.Append(", ");
                    }
                }

                builder.AppendLine();
            }

            return builder.ToString();
        }

        private static int CompareRooms(DungeonRoomNode left, DungeonRoomNode right)
        {
            int yCompare = right.GridPosition.Y.CompareTo(left.GridPosition.Y);

            if (yCompare != 0)
            {
                return yCompare;
            }

            return left.GridPosition.X.CompareTo(right.GridPosition.X);
        }
    }
}
