using CuteIssac.Data.Dungeon;

namespace CuteIssac.Dungeon
{
    /// <summary>
    /// Promotes one generated room to the boss room after graph construction.
    /// The selection rule is intentionally simple for now: choose the farthest non-start room that satisfies the floor's minimum distance.
    /// </summary>
    public sealed class DungeonBossRoomAssigner
    {
        public bool TryAssignBossRoom(DungeonMap dungeonMap)
        {
            if (dungeonMap == null || dungeonMap.FloorConfig == null)
            {
                return false;
            }

            DungeonRoomNode selectedRoom = null;
            int minimumDistance = dungeonMap.FloorConfig.MinimumBossDistanceFromStart;

            foreach (var roomPair in dungeonMap.RoomsByPosition)
            {
                DungeonRoomNode roomNode = roomPair.Value;

                if (roomNode.RoomType == RoomType.Start || roomNode.DistanceFromStart < minimumDistance)
                {
                    continue;
                }

                if (selectedRoom == null || roomNode.DistanceFromStart > selectedRoom.DistanceFromStart)
                {
                    selectedRoom = roomNode;
                }
            }

            if (selectedRoom == null)
            {
                selectedRoom = FindFallbackBossRoom(dungeonMap);
            }

            if (selectedRoom == null)
            {
                return false;
            }

            RoomData bossRoomData = SelectBossRoomData(dungeonMap.FloorConfig);
            selectedRoom.ApplyGeneratedMetadata(RoomType.Boss, bossRoomData);
            return true;
        }

        private static DungeonRoomNode FindFallbackBossRoom(DungeonMap dungeonMap)
        {
            DungeonRoomNode selectedRoom = null;

            foreach (var roomPair in dungeonMap.RoomsByPosition)
            {
                DungeonRoomNode roomNode = roomPair.Value;

                if (roomNode.RoomType == RoomType.Start)
                {
                    continue;
                }

                if (selectedRoom == null || roomNode.DistanceFromStart > selectedRoom.DistanceFromStart)
                {
                    selectedRoom = roomNode;
                }
            }

            return selectedRoom;
        }

        private static RoomData SelectBossRoomData(FloorConfig floorConfig)
        {
            if (!floorConfig.TryGetRoomPool(RoomType.Boss, out FloorConfig.RoomPoolEntry bossPool) || bossPool.CandidateRooms.Count == 0)
            {
                return null;
            }

            int selectedIndex = UnityEngine.Random.Range(0, bossPool.CandidateRooms.Count);
            return bossPool.CandidateRooms[selectedIndex];
        }
    }
}
