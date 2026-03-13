using System.Collections.Generic;
using CuteIssac.Data.Dungeon;
using UnityEngine;

namespace CuteIssac.Dungeon
{
    /// <summary>
    /// Builds the first-pass dungeon graph for a floor.
    /// This stage only places the start room and expands outward with normal rooms on unique grid coordinates.
    /// </summary>
    public sealed class RoomGraphBuilder
    {
        private readonly List<RoomData> _roomSelectionBuffer = new();
        private readonly DungeonRoomDistanceCalculator _distanceCalculator = new();
        private readonly DungeonBossRoomAssigner _bossRoomAssigner = new();
        private readonly DungeonSpecialRoomAssigner _specialRoomAssigner = new();
        private readonly DungeonEnemyWaveAssigner _enemyWaveAssigner = new();
        private readonly RoomLayoutResolver _layoutResolver = new();

        public DungeonMap Build(FloorConfig floorConfig, int seed)
        {
            if (floorConfig == null)
            {
                Debug.LogError("RoomGraphBuilder requires a FloorConfig.");
                return null;
            }

            Random.State previousRandomState = Random.state;
            Random.InitState(seed);

            try
            {
                return BuildInternal(floorConfig);
            }
            finally
            {
                Random.state = previousRandomState;
            }
        }

        private DungeonMap BuildInternal(FloorConfig floorConfig)
        {
            DungeonMap dungeonMap = new(floorConfig);
            int targetNormalRoomCount = ResolveTargetNormalRoomCount(floorConfig);
            RoomData startRoomData = SelectRoomData(floorConfig, RoomType.Start);

            dungeonMap.TryAddRoom(new DungeonRoomNode(RoomType.Start, startRoomData, GridPosition.Zero));

            List<GridPosition> frontier = new(targetNormalRoomCount + 1)
            {
                GridPosition.Zero
            };

            int normalRoomsCreated = 0;

            while (normalRoomsCreated < targetNormalRoomCount)
            {
                if (!TryCreateNormalRoom(dungeonMap, floorConfig, frontier))
                {
                    Debug.LogWarning($"RoomGraphBuilder stopped early. Requested {targetNormalRoomCount} normal rooms, created {normalRoomsCreated}.");
                    break;
                }

                normalRoomsCreated++;
            }

            _distanceCalculator.CalculateDistances(dungeonMap, GridPosition.Zero);
            _bossRoomAssigner.TryAssignBossRoom(dungeonMap);
            _specialRoomAssigner.Assign(dungeonMap);
            _distanceCalculator.CalculateDistances(dungeonMap, GridPosition.Zero);
            _enemyWaveAssigner.Assign(dungeonMap);
            _layoutResolver.ResolveAllLayouts(dungeonMap);

            return dungeonMap;
        }

        private bool TryCreateNormalRoom(DungeonMap dungeonMap, FloorConfig floorConfig, List<GridPosition> frontier)
        {
            while (frontier.Count > 0)
            {
                int frontierIndex = Random.Range(0, frontier.Count);
                GridPosition sourcePosition = frontier[frontierIndex];
                int openNeighborCount = CountOpenNeighborSlots(dungeonMap, sourcePosition);

                if (openNeighborCount == 0)
                {
                    RemoveAtSwapBack(frontier, frontierIndex);
                    continue;
                }

                RoomDirection chosenDirection = SelectOpenDirection(dungeonMap, sourcePosition, openNeighborCount);
                GridPosition nextPosition = sourcePosition + RoomDirectionUtility.ToOffset(chosenDirection);
                RoomData normalRoomData = SelectRoomData(floorConfig, RoomType.Normal);
                DungeonRoomNode nextRoomNode = new(RoomType.Normal, normalRoomData, nextPosition);

                if (!dungeonMap.TryAddRoom(nextRoomNode))
                {
                    continue;
                }

                dungeonMap.ConnectRooms(sourcePosition, chosenDirection, nextPosition);
                dungeonMap.ConnectRooms(nextPosition, RoomDirectionUtility.Opposite(chosenDirection), sourcePosition);
                frontier.Add(nextPosition);

                if (CountOpenNeighborSlots(dungeonMap, sourcePosition) == 0)
                {
                    RemoveAtSwapBack(frontier, frontierIndex);
                }

                return true;
            }

            return false;
        }

        private int ResolveTargetNormalRoomCount(FloorConfig floorConfig)
        {
            int min = Mathf.Max(0, floorConfig.MinNormalRoomCount);
            int max = Mathf.Max(min, floorConfig.MaxNormalRoomCount);
            return Random.Range(min, max + 1);
        }

        private RoomDirection SelectOpenDirection(DungeonMap dungeonMap, GridPosition sourcePosition, int openNeighborCount)
        {
            int selectedOpenIndex = Random.Range(0, openNeighborCount);
            int currentOpenIndex = 0;

            foreach (RoomDirection direction in RoomDirectionUtility.Directions)
            {
                GridPosition candidatePosition = sourcePosition + RoomDirectionUtility.ToOffset(direction);

                if (dungeonMap.ContainsRoom(candidatePosition))
                {
                    continue;
                }

                if (currentOpenIndex == selectedOpenIndex)
                {
                    return direction;
                }

                currentOpenIndex++;
            }

            return RoomDirection.Up;
        }

        private int CountOpenNeighborSlots(DungeonMap dungeonMap, GridPosition sourcePosition)
        {
            int openNeighborCount = 0;

            foreach (RoomDirection direction in RoomDirectionUtility.Directions)
            {
                GridPosition candidatePosition = sourcePosition + RoomDirectionUtility.ToOffset(direction);

                if (!dungeonMap.ContainsRoom(candidatePosition))
                {
                    openNeighborCount++;
                }
            }

            return openNeighborCount;
        }

        private RoomData SelectRoomData(FloorConfig floorConfig, RoomType roomType)
        {
            _roomSelectionBuffer.Clear();
            floorConfig.CollectCandidateRooms(roomType, _roomSelectionBuffer);

            if (_roomSelectionBuffer.Count == 0)
            {
                return null;
            }

            int selectedIndex = Random.Range(0, _roomSelectionBuffer.Count);
            return _roomSelectionBuffer[selectedIndex];
        }

        private static void RemoveAtSwapBack(List<GridPosition> frontier, int index)
        {
            int lastIndex = frontier.Count - 1;
            frontier[index] = frontier[lastIndex];
            frontier.RemoveAt(lastIndex);
        }
    }
}
