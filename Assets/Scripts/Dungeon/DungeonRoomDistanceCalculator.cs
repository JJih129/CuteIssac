using System.Collections.Generic;

namespace CuteIssac.Dungeon
{
    /// <summary>
    /// Computes shortest-path room distance from the start room.
    /// This pass is reusable for boss, treasure, and shop assignment because it writes stable graph distance onto each node.
    /// </summary>
    public sealed class DungeonRoomDistanceCalculator
    {
        public void CalculateDistances(DungeonMap dungeonMap, GridPosition startPosition)
        {
            if (dungeonMap == null)
            {
                return;
            }

            foreach (KeyValuePair<GridPosition, DungeonRoomNode> roomPair in dungeonMap.RoomsByPosition)
            {
                roomPair.Value.SetDistanceFromStart(-1);
            }

            if (!dungeonMap.TryGetRoom(startPosition, out DungeonRoomNode startRoom))
            {
                return;
            }

            Queue<DungeonRoomNode> frontier = new();
            startRoom.SetDistanceFromStart(0);
            frontier.Enqueue(startRoom);

            while (frontier.Count > 0)
            {
                DungeonRoomNode currentRoom = frontier.Dequeue();
                int nextDistance = currentRoom.DistanceFromStart + 1;

                for (int i = 0; i < currentRoom.Connections.Count; i++)
                {
                    RoomConnection connection = currentRoom.Connections[i];

                    if (!dungeonMap.TryGetRoom(connection.TargetPosition, out DungeonRoomNode nextRoom))
                    {
                        continue;
                    }

                    if (nextRoom.DistanceFromStart >= 0)
                    {
                        continue;
                    }

                    nextRoom.SetDistanceFromStart(nextDistance);
                    frontier.Enqueue(nextRoom);
                }
            }
        }
    }
}
