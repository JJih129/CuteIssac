using System.Collections.Generic;
using CuteIssac.Data.Dungeon;
using UnityEngine;

namespace CuteIssac.Dungeon
{
    /// <summary>
    /// Resolves a concrete layout prefab for each generated room node.
    /// Resolution is driven by room type and required door directions so later scene instantiation can use the selected layout directly.
    /// </summary>
    public sealed class RoomLayoutResolver
    {
        private readonly List<RoomLayoutData> _candidateBuffer = new();
        private readonly List<RoomLayoutData> _exactMatchBuffer = new();
        private readonly List<RoomLayoutData> _supersetMatchBuffer = new();

        public void ResolveAllLayouts(DungeonMap dungeonMap)
        {
            if (dungeonMap == null)
            {
                return;
            }

            foreach (KeyValuePair<GridPosition, DungeonRoomNode> roomPair in dungeonMap.RoomsByPosition)
            {
                DungeonRoomNode roomNode = roomPair.Value;
                RoomLayoutData resolvedLayout = ResolveLayout(dungeonMap.FloorConfig, roomNode);
                roomNode.SetResolvedLayout(resolvedLayout);
            }
        }

        public RoomLayoutData ResolveLayout(FloorConfig floorConfig, DungeonRoomNode roomNode)
        {
            if (roomNode == null)
            {
                return null;
            }

            _candidateBuffer.Clear();
            RoomDoorMask requiredDoors = RoomDirectionUtility.ToDoorMask(roomNode.Connections);
            roomNode.RoomData?.CollectLocalLayouts(_candidateBuffer);

            RoomLayoutData localLayout = SelectBestCompatibleLayout(_candidateBuffer, roomNode.RoomType, requiredDoors);

            if (localLayout != null)
            {
                return localLayout;
            }

            _candidateBuffer.Clear();
            floorConfig?.CollectCandidateLayouts(roomNode.RoomType, _candidateBuffer);

            RoomLayoutData sharedLayout = SelectBestCompatibleLayout(_candidateBuffer, roomNode.RoomType, requiredDoors);

            if (sharedLayout == null)
            {
                Debug.LogWarning(
                    $"RoomLayoutResolver could not find a compatible layout for {roomNode.RoomType} at {roomNode.GridPosition} " +
                    $"with required doors {requiredDoors}.",
                    roomNode.RoomData);
            }

            return sharedLayout;
        }

        private RoomLayoutData SelectBestCompatibleLayout(List<RoomLayoutData> candidates, RoomType roomType, RoomDoorMask requiredDoors)
        {
            _exactMatchBuffer.Clear();
            _supersetMatchBuffer.Clear();

            for (int i = 0; i < candidates.Count; i++)
            {
                RoomLayoutData layout = candidates[i];

                if (layout == null || !layout.IsCompatible(roomType, requiredDoors))
                {
                    continue;
                }

                if (layout.SupportedDoorMask == requiredDoors)
                {
                    _exactMatchBuffer.Add(layout);
                }
                else
                {
                    _supersetMatchBuffer.Add(layout);
                }
            }

            if (_exactMatchBuffer.Count > 0)
            {
                return SelectWeightedLayout(_exactMatchBuffer);
            }

            if (_supersetMatchBuffer.Count > 0)
            {
                return SelectWeightedLayout(_supersetMatchBuffer);
            }

            return null;
        }

        private static RoomLayoutData SelectWeightedLayout(List<RoomLayoutData> candidates)
        {
            int totalWeight = 0;

            for (int i = 0; i < candidates.Count; i++)
            {
                totalWeight += Mathf.Max(1, candidates[i].SelectionWeight);
            }

            int roll = Random.Range(0, totalWeight);

            for (int i = 0; i < candidates.Count; i++)
            {
                roll -= Mathf.Max(1, candidates[i].SelectionWeight);

                if (roll < 0)
                {
                    return candidates[i];
                }
            }

            return candidates.Count > 0 ? candidates[0] : null;
        }
    }
}
