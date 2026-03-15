using System.Collections.Generic;
using CuteIssac.Data.Dungeon;
using UnityEngine;

namespace CuteIssac.Dungeon
{
    internal static class RoomDataSelector
    {
        public static RoomData SelectWeighted(FloorConfig floorConfig, RoomType roomType, List<RoomData> buffer)
        {
            if (floorConfig == null || buffer == null)
            {
                return null;
            }

            buffer.Clear();
            floorConfig.CollectCandidateRooms(roomType, buffer);

            if (buffer.Count == 0)
            {
                return null;
            }

            int totalWeight = 0;

            for (int i = 0; i < buffer.Count; i++)
            {
                RoomData roomData = buffer[i];
                totalWeight += Mathf.Max(1, roomData != null ? roomData.GenerationWeight : 1);
            }

            int roll = Random.Range(0, Mathf.Max(1, totalWeight));

            for (int i = 0; i < buffer.Count; i++)
            {
                RoomData roomData = buffer[i];
                roll -= Mathf.Max(1, roomData != null ? roomData.GenerationWeight : 1);

                if (roll < 0)
                {
                    return roomData;
                }
            }

            return buffer[0];
        }
    }
}
