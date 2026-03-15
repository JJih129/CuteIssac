using System.Collections.Generic;
using CuteIssac.Data.Dungeon;
using UnityEngine;

namespace CuteIssac.Dungeon
{
    /// <summary>
    /// Builds an Isaac-style room tree on a 2D grid.
    /// The graph is expanded with BFS-style frontier growth and a strict adjacency rule so rooms form branches instead of blobs.
    /// </summary>
    public sealed class RoomGraphBuilder
    {
        private readonly List<RoomData> _roomSelectionBuffer = new();
        private readonly DungeonRoomDistanceCalculator _distanceCalculator = new();
        private readonly RoomTypeAssigner _roomTypeAssigner = new();
        private readonly DungeonEnemyWaveAssigner _enemyWaveAssigner = new();
        private readonly RoomLayoutResolver _layoutResolver = new();
        private readonly List<GridPosition> _frontierRefillBuffer = new();

        public DungeonMap Build(FloorConfig floorConfig, int seed)
        {
            if (floorConfig == null)
            {
                Debug.LogError("RoomGraphBuilder requires a FloorConfig.");
                return null;
            }

            Random.State previousRandomState = Random.state;

            try
            {
                return BuildInternal(floorConfig, seed);
            }
            finally
            {
                Random.state = previousRandomState;
            }
        }

        private DungeonMap BuildInternal(FloorConfig floorConfig, int seed)
        {
            for (int attemptIndex = 0; attemptIndex < floorConfig.MaxGenerationAttempts; attemptIndex++)
            {
                int attemptSeed = seed + (attemptIndex * 104729);
                Random.InitState(attemptSeed);
                DungeonMap dungeonMap = new(floorConfig, seed);
                int targetNormalRoomCount = ResolveTargetNormalRoomCount(floorConfig);
                RoomData startRoomData = SelectRoomData(floorConfig, RoomType.Start);

                if (startRoomData == null)
                {
                    Debug.LogError("RoomGraphBuilder could not find a Start room definition.");
                    return null;
                }

                dungeonMap.TryAddRoom(new DungeonRoomNode(RoomType.Start, startRoomData, GridPosition.Zero));

                if (!BuildNormalRoomTree(dungeonMap, floorConfig, targetNormalRoomCount, out string graphFailureReason))
                {
                    Debug.LogWarning($"RoomGraphBuilder attempt {attemptIndex + 1}/{floorConfig.MaxGenerationAttempts} failed during graph build: {graphFailureReason}");
                    continue;
                }

                _distanceCalculator.CalculateDistances(dungeonMap, GridPosition.Zero);

                if (!_roomTypeAssigner.Assign(dungeonMap, out string typeFailureReason))
                {
                    Debug.LogWarning($"RoomGraphBuilder attempt {attemptIndex + 1}/{floorConfig.MaxGenerationAttempts} failed during room type assignment: {typeFailureReason}");
                    continue;
                }

                _distanceCalculator.CalculateDistances(dungeonMap, GridPosition.Zero);
                _enemyWaveAssigner.Assign(dungeonMap);

                if (!_layoutResolver.TryResolveAllLayouts(dungeonMap, out string layoutFailureReason))
                {
                    Debug.LogWarning($"RoomGraphBuilder attempt {attemptIndex + 1}/{floorConfig.MaxGenerationAttempts} failed during layout resolution: {layoutFailureReason}");
                    continue;
                }

                return dungeonMap;
            }

            Debug.LogError($"RoomGraphBuilder failed to generate a valid dungeon after {floorConfig.MaxGenerationAttempts} attempts for seed {seed}.");
            return null;
        }

        private bool BuildNormalRoomTree(DungeonMap dungeonMap, FloorConfig floorConfig, int targetNormalRoomCount, out string failureReason)
        {
            failureReason = null;
            Queue<GridPosition> frontier = new();
            HashSet<GridPosition> queuedPositions = new();
            frontier.Enqueue(GridPosition.Zero);
            queuedPositions.Add(GridPosition.Zero);
            int normalRoomsCreated = 0;

            while (normalRoomsCreated < targetNormalRoomCount)
            {
                if (frontier.Count == 0)
                {
                    RefillFrontier(dungeonMap, frontier, queuedPositions);

                    if (frontier.Count == 0)
                    {
                        failureReason = $"Expansion frontier exhausted at {normalRoomsCreated}/{targetNormalRoomCount} normal rooms.";
                        return false;
                    }
                }

                GridPosition sourcePosition = frontier.Dequeue();
                queuedPositions.Remove(sourcePosition);

                if (!dungeonMap.TryGetRoom(sourcePosition, out DungeonRoomNode sourceRoom))
                {
                    continue;
                }

                List<RoomDirection> validDirections = GatherValidExpansionDirections(dungeonMap, sourcePosition);

                if (validDirections.Count == 0)
                {
                    continue;
                }

                ShuffleDirections(validDirections);
                int expansionCount = DetermineExpansionCount(sourceRoom, validDirections.Count, floorConfig);

                for (int directionIndex = 0; directionIndex < expansionCount && normalRoomsCreated < targetNormalRoomCount; directionIndex++)
                {
                    RoomDirection direction = validDirections[directionIndex];
                    GridPosition nextPosition = sourcePosition + RoomDirectionUtility.ToOffset(direction);
                    RoomData normalRoomData = SelectRoomData(floorConfig, RoomType.Normal);

                    if (normalRoomData == null)
                    {
                        failureReason = "Normal room pool was empty.";
                        return false;
                    }

                    DungeonRoomNode nextRoomNode = new(RoomType.Normal, normalRoomData, nextPosition);

                    if (!dungeonMap.TryAddRoom(nextRoomNode))
                    {
                        continue;
                    }

                    dungeonMap.ConnectRooms(sourcePosition, direction, nextPosition);
                    dungeonMap.ConnectRooms(nextPosition, RoomDirectionUtility.Opposite(direction), sourcePosition);

                    if (queuedPositions.Add(nextPosition))
                    {
                        frontier.Enqueue(nextPosition);
                    }

                    normalRoomsCreated++;
                }
            }

            return true;
        }

        private int ResolveTargetNormalRoomCount(FloorConfig floorConfig)
        {
            int min = Mathf.Max(0, floorConfig.MinNormalRoomCount);
            int max = Mathf.Max(min, floorConfig.MaxNormalRoomCount);
            return Random.Range(min, max + 1);
        }

        private List<RoomDirection> GatherValidExpansionDirections(DungeonMap dungeonMap, GridPosition sourcePosition)
        {
            List<RoomDirection> validDirections = new();

            foreach (RoomDirection direction in RoomDirectionUtility.Directions)
            {
                GridPosition candidatePosition = sourcePosition + RoomDirectionUtility.ToOffset(direction);

                if (IsValidExpansionCandidate(dungeonMap, candidatePosition))
                {
                    validDirections.Add(direction);
                }
            }

            return validDirections;
        }

        private bool IsValidExpansionCandidate(DungeonMap dungeonMap, GridPosition candidatePosition)
        {
            if (dungeonMap.ContainsRoom(candidatePosition))
            {
                return false;
            }

            int adjacentRoomCount = 0;

            foreach (RoomDirection direction in RoomDirectionUtility.Directions)
            {
                GridPosition neighborPosition = candidatePosition + RoomDirectionUtility.ToOffset(direction);

                if (!dungeonMap.ContainsRoom(neighborPosition))
                {
                    continue;
                }

                adjacentRoomCount++;

                if (adjacentRoomCount >= 2)
                {
                    return false;
                }
            }

            return adjacentRoomCount == 1;
        }

        private RoomData SelectRoomData(FloorConfig floorConfig, RoomType roomType)
        {
            return RoomDataSelector.SelectWeighted(floorConfig, roomType, _roomSelectionBuffer);
        }

        private static int DetermineExpansionCount(DungeonRoomNode sourceRoom, int validDirectionCount, FloorConfig floorConfig)
        {
            int expansionCount = 1;

            if (sourceRoom != null && sourceRoom.RoomType == RoomType.Start)
            {
                expansionCount = floorConfig.StartRoomInitialBranchCount;
            }
            else if (validDirectionCount > 1 && Random.value < floorConfig.AdditionalBranchChance)
            {
                expansionCount++;
            }

            return Mathf.Clamp(expansionCount, 1, validDirectionCount);
        }

        private void RefillFrontier(DungeonMap dungeonMap, Queue<GridPosition> frontier, HashSet<GridPosition> queuedPositions)
        {
            _frontierRefillBuffer.Clear();

            foreach (KeyValuePair<GridPosition, DungeonRoomNode> roomPair in dungeonMap.RoomsByPosition)
            {
                GridPosition position = roomPair.Key;

                if (queuedPositions.Contains(position))
                {
                    continue;
                }

                if (GatherValidExpansionDirections(dungeonMap, position).Count > 0)
                {
                    _frontierRefillBuffer.Add(position);
                }
            }

            ShufflePositions(_frontierRefillBuffer);

            for (int i = 0; i < _frontierRefillBuffer.Count; i++)
            {
                GridPosition position = _frontierRefillBuffer[i];

                if (queuedPositions.Add(position))
                {
                    frontier.Enqueue(position);
                }
            }
        }

        private static void ShuffleDirections(List<RoomDirection> directions)
        {
            for (int i = directions.Count - 1; i > 0; i--)
            {
                int swapIndex = Random.Range(0, i + 1);
                (directions[i], directions[swapIndex]) = (directions[swapIndex], directions[i]);
            }
        }

        private static void ShufflePositions(List<GridPosition> positions)
        {
            for (int i = positions.Count - 1; i > 0; i--)
            {
                int swapIndex = Random.Range(0, i + 1);
                (positions[i], positions[swapIndex]) = (positions[swapIndex], positions[i]);
            }
        }
    }
}
