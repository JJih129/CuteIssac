using System.Collections.Generic;
using CuteIssac.Data.Dungeon;
using UnityEngine;

namespace CuteIssac.Dungeon
{
    /// <summary>
    /// Assigns non-boss special rooms after the base graph is built.
    /// Treasure and shop rooms replace eligible normal rooms, while secret rooms occupy empty coordinates with strong neighbor support.
    /// </summary>
    public sealed class DungeonSpecialRoomAssigner
    {
        private readonly List<DungeonRoomNode> _roomCandidateBuffer = new();
        private readonly List<SecretRoomCandidate> _secretCandidateBuffer = new();
        private readonly List<RoomData> _roomDataSelectionBuffer = new();

        public void Assign(DungeonMap dungeonMap)
        {
            if (dungeonMap == null || dungeonMap.FloorConfig == null)
            {
                return;
            }

            AssignReplacementRooms(
                dungeonMap,
                RoomType.Treasure,
                dungeonMap.FloorConfig.TreasureRoomCount,
                dungeonMap.FloorConfig.MinimumTreasureDistanceFromStart);

            AssignReplacementRooms(
                dungeonMap,
                RoomType.Shop,
                dungeonMap.FloorConfig.ShopRoomCount,
                dungeonMap.FloorConfig.MinimumShopDistanceFromStart);

            AssignSecretRooms(dungeonMap);
        }

        private void AssignReplacementRooms(DungeonMap dungeonMap, RoomType targetType, int count, int minimumDistance)
        {
            for (int i = 0; i < count; i++)
            {
                DungeonRoomNode selectedRoom = SelectReplacementCandidate(dungeonMap, minimumDistance);

                if (selectedRoom == null)
                {
                    break;
                }

                selectedRoom.ApplyGeneratedMetadata(targetType, SelectRoomData(dungeonMap.FloorConfig, targetType));
            }
        }

        private DungeonRoomNode SelectReplacementCandidate(DungeonMap dungeonMap, int minimumDistance)
        {
            _roomCandidateBuffer.Clear();

            foreach (KeyValuePair<GridPosition, DungeonRoomNode> roomPair in dungeonMap.RoomsByPosition)
            {
                DungeonRoomNode roomNode = roomPair.Value;

                if (roomNode.RoomType != RoomType.Normal)
                {
                    continue;
                }

                if (roomNode.DistanceFromStart < minimumDistance)
                {
                    continue;
                }

                _roomCandidateBuffer.Add(roomNode);
            }

            if (_roomCandidateBuffer.Count == 0)
            {
                foreach (KeyValuePair<GridPosition, DungeonRoomNode> roomPair in dungeonMap.RoomsByPosition)
                {
                    DungeonRoomNode roomNode = roomPair.Value;

                    if (roomNode.RoomType == RoomType.Normal)
                    {
                        _roomCandidateBuffer.Add(roomNode);
                    }
                }
            }

            if (_roomCandidateBuffer.Count == 0)
            {
                return null;
            }

            _roomCandidateBuffer.Sort(CompareRoomCandidates);
            return _roomCandidateBuffer[0];
        }

        private void AssignSecretRooms(DungeonMap dungeonMap)
        {
            int secretRoomCount = dungeonMap.FloorConfig.SecretRoomCount;

            for (int i = 0; i < secretRoomCount; i++)
            {
                SecretRoomCandidate candidate = SelectSecretCandidate(dungeonMap);

                if (!candidate.IsValid)
                {
                    break;
                }

                DungeonRoomNode secretRoomNode = new(RoomType.Secret, SelectRoomData(dungeonMap.FloorConfig, RoomType.Secret), candidate.Position);

                if (!dungeonMap.TryAddRoom(secretRoomNode))
                {
                    continue;
                }

                for (int neighborIndex = 0; neighborIndex < candidate.Neighbors.Count; neighborIndex++)
                {
                    SecretNeighbor neighbor = candidate.Neighbors[neighborIndex];
                    dungeonMap.ConnectRooms(candidate.Position, neighbor.DirectionFromSecret, neighbor.Room.GridPosition);
                    dungeonMap.ConnectRooms(neighbor.Room.GridPosition, RoomDirectionUtility.Opposite(neighbor.DirectionFromSecret), candidate.Position);
                }

                secretRoomNode.SetDistanceFromStart(candidate.DistanceFromStart);
            }
        }

        private SecretRoomCandidate SelectSecretCandidate(DungeonMap dungeonMap)
        {
            _secretCandidateBuffer.Clear();

            foreach (KeyValuePair<GridPosition, DungeonRoomNode> roomPair in dungeonMap.RoomsByPosition)
            {
                GridPosition sourcePosition = roomPair.Key;

                foreach (RoomDirection direction in RoomDirectionUtility.Directions)
                {
                    GridPosition candidatePosition = sourcePosition + RoomDirectionUtility.ToOffset(direction);

                    if (dungeonMap.ContainsRoom(candidatePosition) || ContainsSecretCandidate(candidatePosition))
                    {
                        continue;
                    }

                    if (!TryBuildSecretCandidate(dungeonMap, candidatePosition, out SecretRoomCandidate candidate))
                    {
                        continue;
                    }

                    _secretCandidateBuffer.Add(candidate);
                }
            }

            if (_secretCandidateBuffer.Count == 0)
            {
                return default;
            }

            _secretCandidateBuffer.Sort(CompareSecretCandidates);
            return _secretCandidateBuffer[0];
        }

        private bool TryBuildSecretCandidate(DungeonMap dungeonMap, GridPosition candidatePosition, out SecretRoomCandidate candidate)
        {
            int minimumNeighborCount = dungeonMap.FloorConfig.MinimumSecretAdjacentRoomCount;
            int minimumDistance = dungeonMap.FloorConfig.MinimumSecretDistanceFromStart;
            List<SecretNeighbor> neighbors = new(4);
            int bestDistance = int.MaxValue;

            foreach (RoomDirection direction in RoomDirectionUtility.Directions)
            {
                GridPosition neighborPosition = candidatePosition + RoomDirectionUtility.ToOffset(direction);

                if (!dungeonMap.TryGetRoom(neighborPosition, out DungeonRoomNode neighborRoom))
                {
                    continue;
                }

                if (neighborRoom.RoomType == RoomType.Start || neighborRoom.RoomType == RoomType.Boss || neighborRoom.RoomType == RoomType.Secret)
                {
                    continue;
                }

                neighbors.Add(new SecretNeighbor(neighborRoom, RoomDirectionUtility.Opposite(direction)));
                bestDistance = Mathf.Min(bestDistance, neighborRoom.DistanceFromStart + 1);
            }

            if (neighbors.Count < minimumNeighborCount || bestDistance < minimumDistance)
            {
                candidate = default;
                return false;
            }

            candidate = new SecretRoomCandidate(candidatePosition, bestDistance, neighbors);
            return true;
        }

        private RoomData SelectRoomData(FloorConfig floorConfig, RoomType roomType)
        {
            _roomDataSelectionBuffer.Clear();
            floorConfig.CollectCandidateRooms(roomType, _roomDataSelectionBuffer);

            if (_roomDataSelectionBuffer.Count == 0)
            {
                return null;
            }

            int selectedIndex = Random.Range(0, _roomDataSelectionBuffer.Count);
            return _roomDataSelectionBuffer[selectedIndex];
        }

        private bool ContainsSecretCandidate(GridPosition candidatePosition)
        {
            for (int i = 0; i < _secretCandidateBuffer.Count; i++)
            {
                if (_secretCandidateBuffer[i].Position.Equals(candidatePosition))
                {
                    return true;
                }
            }

            return false;
        }

        private static int CompareRoomCandidates(DungeonRoomNode left, DungeonRoomNode right)
        {
            int distanceCompare = right.DistanceFromStart.CompareTo(left.DistanceFromStart);

            if (distanceCompare != 0)
            {
                return distanceCompare;
            }

            return left.GridPosition.GetHashCode().CompareTo(right.GridPosition.GetHashCode());
        }

        private static int CompareSecretCandidates(SecretRoomCandidate left, SecretRoomCandidate right)
        {
            int neighborCompare = right.Neighbors.Count.CompareTo(left.Neighbors.Count);

            if (neighborCompare != 0)
            {
                return neighborCompare;
            }

            int distanceCompare = right.DistanceFromStart.CompareTo(left.DistanceFromStart);

            if (distanceCompare != 0)
            {
                return distanceCompare;
            }

            return left.Position.GetHashCode().CompareTo(right.Position.GetHashCode());
        }

        private readonly struct SecretNeighbor
        {
            public SecretNeighbor(DungeonRoomNode room, RoomDirection directionFromSecret)
            {
                Room = room;
                DirectionFromSecret = directionFromSecret;
            }

            public DungeonRoomNode Room { get; }
            public RoomDirection DirectionFromSecret { get; }
        }

        private readonly struct SecretRoomCandidate
        {
            public SecretRoomCandidate(GridPosition position, int distanceFromStart, List<SecretNeighbor> neighbors)
            {
                Position = position;
                DistanceFromStart = distanceFromStart;
                Neighbors = neighbors;
            }

            public GridPosition Position { get; }
            public int DistanceFromStart { get; }
            public List<SecretNeighbor> Neighbors { get; }
            public bool IsValid => Neighbors != null;
        }
    }
}
