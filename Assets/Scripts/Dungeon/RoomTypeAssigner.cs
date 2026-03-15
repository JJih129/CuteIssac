using System.Collections.Generic;
using CuteIssac.Core.Meta;
using CuteIssac.Data.Dungeon;
using UnityEngine;

namespace CuteIssac.Dungeon
{
    /// <summary>
    /// Applies Isaac-style special room rules after the base room tree is built.
    /// </summary>
    public sealed class RoomTypeAssigner
    {
        private readonly List<RoomData> _roomSelectionBuffer = new();
        private readonly List<DungeonRoomNode> _deadEndBuffer = new();
        private readonly List<DungeonRoomNode> _candidateBuffer = new();
        private readonly List<SecretRoomCandidate> _secretCandidateBuffer = new();
        private readonly HashSet<GridPosition> _mainRoutePositions = new();
        private readonly HashSet<GridPosition> _branchRoutePositions = new();

        public bool Assign(DungeonMap dungeonMap, out string failureReason)
        {
            failureReason = null;

            if (dungeonMap == null || dungeonMap.FloorConfig == null)
            {
                failureReason = "DungeonMap or FloorConfig was null.";
                return false;
            }

            CollectDeadEnds(dungeonMap, _deadEndBuffer);

            if (!TryAssignBossRoom(dungeonMap, out DungeonRoomNode bossRoom, out failureReason))
            {
                return false;
            }

            BuildMainRouteToStart(dungeonMap, bossRoom, _mainRoutePositions);
            BuildBranchRoute(dungeonMap, _mainRoutePositions, _branchRoutePositions);
            DungeonRoomNode bossApproachRoom = FindBossApproachRoom(dungeonMap, bossRoom);

            if (!TryAssignTreasureRooms(dungeonMap, _deadEndBuffer, _mainRoutePositions, _branchRoutePositions, bossApproachRoom, out failureReason))
            {
                return false;
            }

            if (!TryAssignShopRooms(dungeonMap, _deadEndBuffer, _mainRoutePositions, _branchRoutePositions, bossRoom.DistanceFromStart, bossApproachRoom, out failureReason))
            {
                return false;
            }

            if (!TryAssignChallengeRooms(dungeonMap, _mainRoutePositions, bossRoom.DistanceFromStart, bossApproachRoom, out failureReason))
            {
                return false;
            }

            if (!TryAssignTrapRooms(dungeonMap, _mainRoutePositions, bossRoom.DistanceFromStart, bossApproachRoom, out failureReason))
            {
                return false;
            }

            if (!TryAssignCurseRooms(dungeonMap, _deadEndBuffer, _mainRoutePositions, bossApproachRoom, out failureReason))
            {
                return false;
            }

            if (!TryAssignMiniBossRooms(dungeonMap, _deadEndBuffer, _mainRoutePositions, bossApproachRoom, out failureReason))
            {
                return false;
            }

            if (!TryAssignSecretRooms(dungeonMap, _mainRoutePositions, out failureReason))
            {
                return false;
            }

            return true;
        }

        private void CollectDeadEnds(DungeonMap dungeonMap, List<DungeonRoomNode> results)
        {
            results.Clear();

            foreach (KeyValuePair<GridPosition, DungeonRoomNode> roomPair in dungeonMap.RoomsByPosition)
            {
                DungeonRoomNode roomNode = roomPair.Value;

                if (roomNode.RoomType == RoomType.Start || roomNode.Connections.Count != 1)
                {
                    continue;
                }

                results.Add(roomNode);
            }
        }

        private bool TryAssignBossRoom(DungeonMap dungeonMap, out DungeonRoomNode bossRoom, out string failureReason)
        {
            bossRoom = null;
            failureReason = null;
            int minimumDistance = dungeonMap.FloorConfig.MinimumBossDistanceFromStart;

            CollectEligibleRooms(
                dungeonMap,
                roomNode => roomNode.RoomType == RoomType.Normal
                    && roomNode.Connections.Count == 1
                    && roomNode.DistanceFromStart >= minimumDistance
                    && !IsAdjacentToRestrictedSpecialRoom(dungeonMap, roomNode),
                _candidateBuffer);

            if (_candidateBuffer.Count == 0)
            {
                CollectEligibleRooms(
                    dungeonMap,
                    roomNode => roomNode.RoomType == RoomType.Normal
                        && roomNode.Connections.Count == 1
                        && !IsAdjacentToRestrictedSpecialRoom(dungeonMap, roomNode),
                    _candidateBuffer);
            }

            SortByDistanceDescending(_candidateBuffer);

            if (_candidateBuffer.Count == 0)
            {
                failureReason = "No dead-end room was available for boss placement.";
                return false;
            }

            bossRoom = _candidateBuffer[0];
            bossRoom.ApplyGeneratedMetadata(RoomType.Boss, RoomDataSelector.SelectWeighted(dungeonMap.FloorConfig, RoomType.Boss, _roomSelectionBuffer));

            if (bossRoom.RoomData == null)
            {
                failureReason = "Boss RoomData pool was empty.";
                return false;
            }

            return true;
        }

        private bool TryAssignTreasureRooms(
            DungeonMap dungeonMap,
            List<DungeonRoomNode> deadEnds,
            HashSet<GridPosition> mainRoute,
            HashSet<GridPosition> branchRoute,
            DungeonRoomNode bossApproachRoom,
            out string failureReason)
        {
            failureReason = null;
            int treasureRoomCount = ResolveUnlockedRoomCount(RoomType.Treasure, dungeonMap.FloorConfig.TreasureRoomCount);

            for (int i = 0; i < treasureRoomCount; i++)
            {
                DungeonRoomNode selectedRoom = SelectTreasureCandidate(dungeonMap, deadEnds, mainRoute, branchRoute, bossApproachRoom);

                if (selectedRoom == null)
                {
                    Debug.LogWarning("RoomTypeAssigner skipped remaining treasure room slots because no eligible branch dead-end room was available.");
                    break;
                }

                selectedRoom.ApplyGeneratedMetadata(RoomType.Treasure, RoomDataSelector.SelectWeighted(dungeonMap.FloorConfig, RoomType.Treasure, _roomSelectionBuffer));

                if (selectedRoom.RoomData == null)
                {
                    failureReason = "Treasure RoomData pool was empty.";
                    return false;
                }
            }

            return true;
        }

        private DungeonRoomNode SelectTreasureCandidate(
            DungeonMap dungeonMap,
            List<DungeonRoomNode> deadEnds,
            HashSet<GridPosition> mainRoute,
            HashSet<GridPosition> branchRoute,
            DungeonRoomNode bossApproachRoom)
        {
            _candidateBuffer.Clear();
            int minimumDistance = dungeonMap.FloorConfig.MinimumTreasureDistanceFromStart;

            CollectLeafCandidates(
                deadEnds,
                roomNode => IsTreasureCandidate(dungeonMap, roomNode, branchRoute, minimumDistance, bossApproachRoom),
                _candidateBuffer);

            SortByDistanceDescending(_candidateBuffer);
            return _candidateBuffer.Count > 0 ? _candidateBuffer[0] : null;
        }

        private bool TryAssignChallengeRooms(
            DungeonMap dungeonMap,
            HashSet<GridPosition> mainRoute,
            int bossDistance,
            DungeonRoomNode bossApproachRoom,
            out string failureReason)
        {
            failureReason = null;
            int challengeRoomCount = ResolveUnlockedRoomCount(RoomType.Challenge, dungeonMap.FloorConfig.ChallengeRoomCount);

            for (int i = 0; i < challengeRoomCount; i++)
            {
                DungeonRoomNode selectedRoom = SelectChallengeCandidate(dungeonMap, mainRoute, bossDistance, bossApproachRoom);

                if (selectedRoom == null)
                {
                    Debug.LogWarning("RoomTypeAssigner skipped remaining challenge room slots because no eligible room was available.");
                    break;
                }

                selectedRoom.ApplyGeneratedMetadata(RoomType.Challenge, RoomDataSelector.SelectWeighted(dungeonMap.FloorConfig, RoomType.Challenge, _roomSelectionBuffer));

                if (selectedRoom.RoomData == null)
                {
                    failureReason = "Challenge RoomData pool was empty.";
                    return false;
                }
            }

            return true;
        }

        private DungeonRoomNode SelectChallengeCandidate(DungeonMap dungeonMap, HashSet<GridPosition> mainRoute, int bossDistance, DungeonRoomNode bossApproachRoom)
        {
            _candidateBuffer.Clear();
            int minimumDistance = dungeonMap.FloorConfig.MinimumChallengeDistanceFromStart;

            CollectEligibleRooms(
                dungeonMap,
                roomNode => roomNode.RoomType == RoomType.Normal
                    && roomNode.DistanceFromStart >= minimumDistance
                    && roomNode.DistanceFromStart < bossDistance
                    && !mainRoute.Contains(roomNode.GridPosition)
                    && roomNode.Connections.Count >= 2
                    && !IsBossApproachRoom(roomNode, bossApproachRoom)
                    && !IsAdjacentToRestrictedSpecialRoom(dungeonMap, roomNode),
                _candidateBuffer);

            if (_candidateBuffer.Count == 0)
            {
                CollectEligibleRooms(
                    dungeonMap,
                    roomNode => roomNode.RoomType == RoomType.Normal
                        && roomNode.DistanceFromStart >= minimumDistance
                        && !mainRoute.Contains(roomNode.GridPosition)
                        && !IsBossApproachRoom(roomNode, bossApproachRoom)
                        && !IsAdjacentToRestrictedSpecialRoom(dungeonMap, roomNode),
                    _candidateBuffer);
            }

            if (_candidateBuffer.Count == 0)
            {
                CollectEligibleRooms(
                    dungeonMap,
                    roomNode => roomNode.RoomType == RoomType.Normal
                        && roomNode.DistanceFromStart >= minimumDistance
                        && !IsAdjacentToRestrictedSpecialRoom(dungeonMap, roomNode),
                    _candidateBuffer);
            }

            SortByDistanceDescending(_candidateBuffer);
            return _candidateBuffer.Count > 0 ? _candidateBuffer[0] : null;
        }

        private bool TryAssignTrapRooms(
            DungeonMap dungeonMap,
            HashSet<GridPosition> mainRoute,
            int bossDistance,
            DungeonRoomNode bossApproachRoom,
            out string failureReason)
        {
            failureReason = null;
            int trapRoomCount = ResolveUnlockedRoomCount(RoomType.Trap, dungeonMap.FloorConfig.TrapRoomCount);

            for (int i = 0; i < trapRoomCount; i++)
            {
                DungeonRoomNode selectedRoom = SelectTrapCandidate(dungeonMap, mainRoute, bossDistance, bossApproachRoom);

                if (selectedRoom == null)
                {
                    Debug.LogWarning("RoomTypeAssigner skipped remaining trap room slots because no eligible room was available.");
                    break;
                }

                selectedRoom.ApplyGeneratedMetadata(RoomType.Trap, RoomDataSelector.SelectWeighted(dungeonMap.FloorConfig, RoomType.Trap, _roomSelectionBuffer));

                if (selectedRoom.RoomData == null)
                {
                    failureReason = "Trap RoomData pool was empty.";
                    return false;
                }
            }

            return true;
        }

        private DungeonRoomNode SelectTrapCandidate(DungeonMap dungeonMap, HashSet<GridPosition> mainRoute, int bossDistance, DungeonRoomNode bossApproachRoom)
        {
            _candidateBuffer.Clear();
            int minimumDistance = dungeonMap.FloorConfig.MinimumTrapDistanceFromStart;

            CollectEligibleRooms(
                dungeonMap,
                roomNode => roomNode.RoomType == RoomType.Normal
                    && roomNode.DistanceFromStart >= minimumDistance
                    && roomNode.DistanceFromStart < bossDistance
                    && !mainRoute.Contains(roomNode.GridPosition)
                    && roomNode.Connections.Count >= 2
                    && !IsBossApproachRoom(roomNode, bossApproachRoom)
                    && !IsAdjacentToRestrictedSpecialRoom(dungeonMap, roomNode),
                _candidateBuffer);

            if (_candidateBuffer.Count == 0)
            {
                CollectEligibleRooms(
                    dungeonMap,
                    roomNode => roomNode.RoomType == RoomType.Normal
                        && roomNode.DistanceFromStart >= minimumDistance
                        && !mainRoute.Contains(roomNode.GridPosition)
                        && !IsBossApproachRoom(roomNode, bossApproachRoom)
                        && !IsAdjacentToRestrictedSpecialRoom(dungeonMap, roomNode),
                    _candidateBuffer);
            }

            if (_candidateBuffer.Count == 0)
            {
                CollectEligibleRooms(
                    dungeonMap,
                    roomNode => roomNode.RoomType == RoomType.Normal
                        && roomNode.DistanceFromStart >= minimumDistance
                        && !IsAdjacentToRestrictedSpecialRoom(dungeonMap, roomNode),
                    _candidateBuffer);
            }

            SortByDistanceDescending(_candidateBuffer);
            return _candidateBuffer.Count > 0 ? _candidateBuffer[0] : null;
        }

        private bool TryAssignCurseRooms(
            DungeonMap dungeonMap,
            List<DungeonRoomNode> deadEnds,
            HashSet<GridPosition> mainRoute,
            DungeonRoomNode bossApproachRoom,
            out string failureReason)
        {
            failureReason = null;
            int curseRoomCount = ResolveUnlockedRoomCount(RoomType.Curse, dungeonMap.FloorConfig.CurseRoomCount);

            for (int i = 0; i < curseRoomCount; i++)
            {
                DungeonRoomNode selectedRoom = SelectCurseCandidate(dungeonMap, deadEnds, mainRoute, bossApproachRoom);

                if (selectedRoom == null)
                {
                    Debug.LogWarning("RoomTypeAssigner skipped remaining curse room slots because no eligible dead-end room was available.");
                    break;
                }

                selectedRoom.ApplyGeneratedMetadata(RoomType.Curse, RoomDataSelector.SelectWeighted(dungeonMap.FloorConfig, RoomType.Curse, _roomSelectionBuffer));

                if (selectedRoom.RoomData == null)
                {
                    failureReason = "Curse RoomData pool was empty.";
                    return false;
                }
            }

            return true;
        }

        private DungeonRoomNode SelectCurseCandidate(DungeonMap dungeonMap, List<DungeonRoomNode> deadEnds, HashSet<GridPosition> mainRoute, DungeonRoomNode bossApproachRoom)
        {
            _candidateBuffer.Clear();
            int minimumDistance = dungeonMap.FloorConfig.MinimumCurseDistanceFromStart;

            for (int i = 0; i < deadEnds.Count; i++)
            {
                DungeonRoomNode roomNode = deadEnds[i];

                if (roomNode.RoomType != RoomType.Normal || roomNode.DistanceFromStart < minimumDistance)
                {
                    continue;
                }

                if (IsAdjacentToRestrictedSpecialRoom(dungeonMap, roomNode) || IsBossApproachRoom(roomNode, bossApproachRoom))
                {
                    continue;
                }

                if (!mainRoute.Contains(roomNode.GridPosition))
                {
                    _candidateBuffer.Add(roomNode);
                }
            }

            if (_candidateBuffer.Count == 0)
            {
                for (int i = 0; i < deadEnds.Count; i++)
                {
                    DungeonRoomNode roomNode = deadEnds[i];

                    if (roomNode.RoomType == RoomType.Normal
                        && roomNode.DistanceFromStart >= minimumDistance
                        && !IsBossApproachRoom(roomNode, bossApproachRoom)
                        && !IsAdjacentToRestrictedSpecialRoom(dungeonMap, roomNode))
                    {
                        _candidateBuffer.Add(roomNode);
                    }
                }
            }

            SortByDistanceDescending(_candidateBuffer);
            return _candidateBuffer.Count > 0 ? _candidateBuffer[0] : null;
        }

        private bool TryAssignMiniBossRooms(
            DungeonMap dungeonMap,
            List<DungeonRoomNode> deadEnds,
            HashSet<GridPosition> mainRoute,
            DungeonRoomNode bossApproachRoom,
            out string failureReason)
        {
            failureReason = null;
            int miniBossRoomCount = ResolveUnlockedRoomCount(RoomType.MiniBoss, dungeonMap.FloorConfig.MiniBossRoomCount);

            for (int i = 0; i < miniBossRoomCount; i++)
            {
                DungeonRoomNode selectedRoom = SelectMiniBossCandidate(dungeonMap, deadEnds, mainRoute, bossApproachRoom);

                if (selectedRoom == null)
                {
                    Debug.LogWarning("RoomTypeAssigner skipped remaining miniboss room slots because no eligible room was available.");
                    break;
                }

                selectedRoom.ApplyGeneratedMetadata(RoomType.MiniBoss, RoomDataSelector.SelectWeighted(dungeonMap.FloorConfig, RoomType.MiniBoss, _roomSelectionBuffer));

                if (selectedRoom.RoomData == null)
                {
                    failureReason = "MiniBoss RoomData pool was empty.";
                    return false;
                }
            }

            return true;
        }

        private DungeonRoomNode SelectMiniBossCandidate(DungeonMap dungeonMap, List<DungeonRoomNode> deadEnds, HashSet<GridPosition> mainRoute, DungeonRoomNode bossApproachRoom)
        {
            _candidateBuffer.Clear();
            int minimumDistance = dungeonMap.FloorConfig.MinimumMiniBossDistanceFromStart;

            for (int i = 0; i < deadEnds.Count; i++)
            {
                DungeonRoomNode roomNode = deadEnds[i];

                if (roomNode.RoomType != RoomType.Normal || roomNode.DistanceFromStart < minimumDistance)
                {
                    continue;
                }

                if (IsAdjacentToRestrictedSpecialRoom(dungeonMap, roomNode) || IsBossApproachRoom(roomNode, bossApproachRoom))
                {
                    continue;
                }

                if (!mainRoute.Contains(roomNode.GridPosition))
                {
                    _candidateBuffer.Add(roomNode);
                }
            }

            if (_candidateBuffer.Count == 0)
            {
                CollectEligibleRooms(
                    dungeonMap,
                    roomNode => roomNode.RoomType == RoomType.Normal
                        && roomNode.DistanceFromStart >= minimumDistance
                        && !mainRoute.Contains(roomNode.GridPosition)
                        && !IsBossApproachRoom(roomNode, bossApproachRoom)
                        && !IsAdjacentToRestrictedSpecialRoom(dungeonMap, roomNode),
                    _candidateBuffer);
            }

            if (_candidateBuffer.Count == 0)
            {
                CollectEligibleRooms(
                    dungeonMap,
                    roomNode => roomNode.RoomType == RoomType.Normal
                        && roomNode.DistanceFromStart >= minimumDistance
                        && !IsAdjacentToRestrictedSpecialRoom(dungeonMap, roomNode),
                    _candidateBuffer);
            }

            SortByDistanceDescending(_candidateBuffer);
            return _candidateBuffer.Count > 0 ? _candidateBuffer[0] : null;
        }

        private bool TryAssignShopRooms(
            DungeonMap dungeonMap,
            List<DungeonRoomNode> deadEnds,
            HashSet<GridPosition> mainRoute,
            HashSet<GridPosition> branchRoute,
            int bossDistance,
            DungeonRoomNode bossApproachRoom,
            out string failureReason)
        {
            failureReason = null;
            int shopRoomCount = ResolveUnlockedRoomCount(RoomType.Shop, dungeonMap.FloorConfig.ShopRoomCount);

            for (int i = 0; i < shopRoomCount; i++)
            {
                DungeonRoomNode selectedRoom = SelectShopCandidate(dungeonMap, deadEnds, mainRoute, branchRoute, bossDistance, bossApproachRoom);

                if (selectedRoom == null)
                {
                    Debug.LogWarning("RoomTypeAssigner skipped remaining shop room slots because no eligible branch dead-end room was available.");
                    break;
                }

                selectedRoom.ApplyGeneratedMetadata(RoomType.Shop, RoomDataSelector.SelectWeighted(dungeonMap.FloorConfig, RoomType.Shop, _roomSelectionBuffer));

                if (selectedRoom.RoomData == null)
                {
                    failureReason = "Shop RoomData pool was empty.";
                    return false;
                }
            }

            return true;
        }

        private DungeonRoomNode SelectShopCandidate(
            DungeonMap dungeonMap,
            List<DungeonRoomNode> deadEnds,
            HashSet<GridPosition> mainRoute,
            HashSet<GridPosition> branchRoute,
            int bossDistance,
            DungeonRoomNode bossApproachRoom)
        {
            _candidateBuffer.Clear();
            int minimumDistance = Mathf.Max(2, dungeonMap.FloorConfig.MinimumShopDistanceFromStart);
            int targetDistance = Mathf.Max(minimumDistance, Mathf.RoundToInt(bossDistance * 0.5f));

            CollectLeafCandidates(
                deadEnds,
                roomNode => IsShopLeafCandidate(dungeonMap, roomNode, branchRoute, minimumDistance, bossDistance, bossApproachRoom),
                _candidateBuffer);

            _candidateBuffer.Sort((left, right) => CompareShopCandidates(left, right, mainRoute, targetDistance));

            return _candidateBuffer.Count > 0 ? _candidateBuffer[0] : null;
        }

        private static bool IsAdjacentToRestrictedSpecialRoom(DungeonMap dungeonMap, DungeonRoomNode roomNode)
        {
            if (dungeonMap == null || roomNode == null)
            {
                return false;
            }

            foreach (RoomDirection direction in RoomDirectionUtility.Directions)
            {
                GridPosition neighborPosition = roomNode.GridPosition + RoomDirectionUtility.ToOffset(direction);
                if (!dungeonMap.TryGetRoom(neighborPosition, out DungeonRoomNode neighborRoom))
                {
                    continue;
                }

                if (IsRestrictedSpecialRoomType(neighborRoom.RoomType))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsRestrictedSpecialRoomType(RoomType roomType)
        {
            switch (roomType)
            {
                case RoomType.Boss:
                case RoomType.Treasure:
                case RoomType.Shop:
                case RoomType.Challenge:
                case RoomType.MiniBoss:
                case RoomType.Trap:
                case RoomType.Curse:
                    return true;
                default:
                    return false;
            }
        }

        private bool TryAssignSecretRooms(DungeonMap dungeonMap, HashSet<GridPosition> mainRoute, out string failureReason)
        {
            failureReason = null;
            int secretRoomCount = ResolveUnlockedRoomCount(RoomType.Secret, dungeonMap.FloorConfig.SecretRoomCount);

            for (int i = 0; i < secretRoomCount; i++)
            {
                SecretRoomCandidate candidate = SelectSecretCandidate(dungeonMap, mainRoute, requireDensePlacement: true);

                if (!candidate.IsValid)
                {
                    candidate = SelectSecretCandidate(dungeonMap, mainRoute, requireDensePlacement: false);
                }

                if (!candidate.IsValid)
                {
                    Debug.LogWarning("RoomTypeAssigner could not place a secret room for this floor. Continuing without the remaining secret room slots.");
                    break;
                }

                RoomData secretRoomData = RoomDataSelector.SelectWeighted(dungeonMap.FloorConfig, RoomType.Secret, _roomSelectionBuffer);

                if (secretRoomData == null)
                {
                    failureReason = "Secret RoomData pool was empty.";
                    return false;
                }

                DungeonRoomNode secretRoomNode = new(RoomType.Secret, secretRoomData, candidate.Position);

                if (!dungeonMap.TryAddRoom(secretRoomNode))
                {
                    failureReason = $"Secret room position {candidate.Position} was already occupied.";
                    return false;
                }

                for (int neighborIndex = 0; neighborIndex < candidate.Neighbors.Count; neighborIndex++)
                {
                    SecretNeighbor neighbor = candidate.Neighbors[neighborIndex];
                    dungeonMap.ConnectRooms(candidate.Position, neighbor.DirectionFromSecret, neighbor.Room.GridPosition);
                    dungeonMap.ConnectRooms(neighbor.Room.GridPosition, RoomDirectionUtility.Opposite(neighbor.DirectionFromSecret), candidate.Position);
                }

                secretRoomNode.SetDistanceFromStart(candidate.DistanceFromStart);
            }

            return true;
        }

        private SecretRoomCandidate SelectSecretCandidate(DungeonMap dungeonMap, HashSet<GridPosition> mainRoute, bool requireDensePlacement)
        {
            _secretCandidateBuffer.Clear();
            Vector2 dungeonCenter = CalculateDungeonCenter(dungeonMap);
            float preferredDistance = CalculatePreferredSecretDistance(dungeonMap);

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

                    if (TryBuildSecretCandidate(
                        dungeonMap,
                        candidatePosition,
                        mainRoute,
                        dungeonCenter,
                        preferredDistance,
                        requireDensePlacement,
                        out SecretRoomCandidate candidate))
                    {
                        _secretCandidateBuffer.Add(candidate);
                    }
                }
            }

            if (_secretCandidateBuffer.Count == 0)
            {
                return default;
            }

            _secretCandidateBuffer.Sort(CompareSecretCandidates);

            return _secretCandidateBuffer[0];
        }

        private bool TryBuildSecretCandidate(
            DungeonMap dungeonMap,
            GridPosition candidatePosition,
            HashSet<GridPosition> mainRoute,
            Vector2 dungeonCenter,
            float preferredDistance,
            bool requireDensePlacement,
            out SecretRoomCandidate candidate)
        {
            int minimumNeighborCount = requireDensePlacement
                ? Mathf.Clamp(dungeonMap.FloorConfig.MinimumSecretAdjacentRoomCount, 3, 4)
                : 2;
            int minimumDistance = dungeonMap.FloorConfig.MinimumSecretDistanceFromStart;
            List<SecretNeighbor> neighbors = new(4);
            int bestDistance = int.MaxValue;
            int normalNeighborCount = 0;
            int branchNeighborCount = 0;
            int specialNeighborCount = 0;

            foreach (RoomDirection direction in RoomDirectionUtility.Directions)
            {
                GridPosition neighborPosition = candidatePosition + RoomDirectionUtility.ToOffset(direction);

                if (!dungeonMap.TryGetRoom(neighborPosition, out DungeonRoomNode neighborRoom))
                {
                    continue;
                }

                if (IsDisallowedSecretNeighbor(neighborRoom.RoomType))
                {
                    candidate = default;
                    return false;
                }

                neighbors.Add(new SecretNeighbor(neighborRoom, RoomDirectionUtility.Opposite(direction)));
                bestDistance = Mathf.Min(bestDistance, neighborRoom.DistanceFromStart + 1);

                if (neighborRoom.RoomType == RoomType.Normal)
                {
                    normalNeighborCount++;
                }
                else
                {
                    specialNeighborCount++;
                }

                if (mainRoute == null || !mainRoute.Contains(neighborRoom.GridPosition))
                {
                    branchNeighborCount++;
                }
            }

            if (neighbors.Count < minimumNeighborCount
                || neighbors.Count > 4
                || bestDistance < minimumDistance
                || normalNeighborCount < 2)
            {
                candidate = default;
                return false;
            }

            float centerOffset = Vector2.SqrMagnitude(new Vector2(candidatePosition.X, candidatePosition.Y) - dungeonCenter);
            int preferredDistanceDelta = Mathf.Abs(bestDistance - Mathf.RoundToInt(preferredDistance));
            candidate = new SecretRoomCandidate(
                candidatePosition,
                bestDistance,
                neighbors,
                normalNeighborCount,
                branchNeighborCount,
                specialNeighborCount,
                preferredDistanceDelta,
                centerOffset);
            return true;
        }

        private static int CompareSecretCandidates(SecretRoomCandidate left, SecretRoomCandidate right)
        {
            int neighborCompare = right.NeighborCount.CompareTo(left.NeighborCount);
            if (neighborCompare != 0)
            {
                return neighborCompare;
            }

            int normalCompare = right.NormalNeighborCount.CompareTo(left.NormalNeighborCount);
            if (normalCompare != 0)
            {
                return normalCompare;
            }

            int branchCompare = right.BranchNeighborCount.CompareTo(left.BranchNeighborCount);
            if (branchCompare != 0)
            {
                return branchCompare;
            }

            int specialCompare = left.SpecialNeighborCount.CompareTo(right.SpecialNeighborCount);
            if (specialCompare != 0)
            {
                return specialCompare;
            }

            int preferredDepthCompare = left.PreferredDistanceDelta.CompareTo(right.PreferredDistanceDelta);
            if (preferredDepthCompare != 0)
            {
                return preferredDepthCompare;
            }

            int centerCompare = left.CenterOffset.CompareTo(right.CenterOffset);
            if (centerCompare != 0)
            {
                return centerCompare;
            }

            return left.DistanceFromStart.CompareTo(right.DistanceFromStart);
        }

        private static Vector2 CalculateDungeonCenter(DungeonMap dungeonMap)
        {
            if (dungeonMap == null)
            {
                return Vector2.zero;
            }

            int count = 0;
            Vector2 sum = Vector2.zero;

            foreach (KeyValuePair<GridPosition, DungeonRoomNode> roomPair in dungeonMap.RoomsByPosition)
            {
                DungeonRoomNode roomNode = roomPair.Value;
                if (roomNode == null || roomNode.RoomType == RoomType.Secret)
                {
                    continue;
                }

                sum += new Vector2(roomNode.GridPosition.X, roomNode.GridPosition.Y);
                count++;
            }

            return count > 0 ? sum / count : Vector2.zero;
        }

        private static float CalculatePreferredSecretDistance(DungeonMap dungeonMap)
        {
            if (dungeonMap == null)
            {
                return 0f;
            }

            int count = 0;
            int totalDistance = 0;

            foreach (KeyValuePair<GridPosition, DungeonRoomNode> roomPair in dungeonMap.RoomsByPosition)
            {
                DungeonRoomNode roomNode = roomPair.Value;
                if (roomNode == null || roomNode.RoomType == RoomType.Start || roomNode.RoomType == RoomType.Secret)
                {
                    continue;
                }

                totalDistance += Mathf.Max(0, roomNode.DistanceFromStart);
                count++;
            }

            return count > 0 ? (float)totalDistance / count : 0f;
        }

        private static bool IsDisallowedSecretNeighbor(RoomType roomType)
        {
            switch (roomType)
            {
                case RoomType.Start:
                case RoomType.Boss:
                case RoomType.Treasure:
                case RoomType.Shop:
                case RoomType.Secret:
                    return true;
                default:
                    return false;
            }
        }

        private static void BuildMainRouteToStart(DungeonMap dungeonMap, DungeonRoomNode bossRoom, HashSet<GridPosition> results)
        {
            results.Clear();

            if (dungeonMap == null || bossRoom == null)
            {
                return;
            }

            DungeonRoomNode currentRoom = bossRoom;
            results.Add(currentRoom.GridPosition);

            while (currentRoom.DistanceFromStart > 0)
            {
                DungeonRoomNode previousRoom = null;

                for (int i = 0; i < currentRoom.Connections.Count; i++)
                {
                    if (!dungeonMap.TryGetRoom(currentRoom.Connections[i].TargetPosition, out DungeonRoomNode neighbor))
                    {
                        continue;
                    }

                    if (neighbor.DistanceFromStart == currentRoom.DistanceFromStart - 1)
                    {
                        previousRoom = neighbor;
                        break;
                    }
                }

                if (previousRoom == null)
                {
                    break;
                }

                currentRoom = previousRoom;
                results.Add(currentRoom.GridPosition);
            }
        }

        private static void BuildBranchRoute(DungeonMap dungeonMap, HashSet<GridPosition> mainRoute, HashSet<GridPosition> results)
        {
            results.Clear();

            if (dungeonMap == null)
            {
                return;
            }

            foreach (KeyValuePair<GridPosition, DungeonRoomNode> roomPair in dungeonMap.RoomsByPosition)
            {
                if (!mainRoute.Contains(roomPair.Key))
                {
                    results.Add(roomPair.Key);
                }
            }
        }

        private static DungeonRoomNode FindBossApproachRoom(DungeonMap dungeonMap, DungeonRoomNode bossRoom)
        {
            if (dungeonMap == null || bossRoom == null)
            {
                return null;
            }

            for (int i = 0; i < bossRoom.Connections.Count; i++)
            {
                RoomConnection connection = bossRoom.Connections[i];

                if (!dungeonMap.TryGetRoom(connection.TargetPosition, out DungeonRoomNode neighbor))
                {
                    continue;
                }

                if (neighbor.DistanceFromStart == bossRoom.DistanceFromStart - 1)
                {
                    return neighbor;
                }
            }

            return null;
        }

        private static void CollectEligibleRooms(DungeonMap dungeonMap, System.Predicate<DungeonRoomNode> predicate, List<DungeonRoomNode> results)
        {
            results.Clear();

            foreach (KeyValuePair<GridPosition, DungeonRoomNode> roomPair in dungeonMap.RoomsByPosition)
            {
                DungeonRoomNode roomNode = roomPair.Value;

                if (predicate != null && predicate(roomNode))
                {
                    results.Add(roomNode);
                }
            }
        }

        private static void CollectLeafCandidates(List<DungeonRoomNode> deadEnds, System.Predicate<DungeonRoomNode> predicate, List<DungeonRoomNode> results)
        {
            results.Clear();

            for (int i = 0; i < deadEnds.Count; i++)
            {
                DungeonRoomNode roomNode = deadEnds[i];
                if (predicate == null || predicate(roomNode))
                {
                    results.Add(roomNode);
                }
            }
        }

        private static void SortByDistanceDescending(List<DungeonRoomNode> rooms)
        {
            rooms.Sort((left, right) =>
            {
                int distanceCompare = right.DistanceFromStart.CompareTo(left.DistanceFromStart);

                if (distanceCompare != 0)
                {
                    return distanceCompare;
                }

                return left.GridPosition.GetHashCode().CompareTo(right.GridPosition.GetHashCode());
            });
        }

        private static int CompareShopCandidates(DungeonRoomNode left, DungeonRoomNode right, HashSet<GridPosition> mainRoute, int targetDistance)
        {
            bool leftIsLeaf = left.Connections.Count == 1;
            bool rightIsLeaf = right.Connections.Count == 1;
            if (leftIsLeaf != rightIsLeaf)
            {
                return rightIsLeaf.CompareTo(leftIsLeaf);
            }

            bool leftOffMainRoute = !mainRoute.Contains(left.GridPosition);
            bool rightOffMainRoute = !mainRoute.Contains(right.GridPosition);
            if (leftOffMainRoute != rightOffMainRoute)
            {
                return rightOffMainRoute.CompareTo(leftOffMainRoute);
            }

            int leftScore = Mathf.Abs(left.DistanceFromStart - targetDistance);
            int rightScore = Mathf.Abs(right.DistanceFromStart - targetDistance);
            int scoreCompare = leftScore.CompareTo(rightScore);
            if (scoreCompare != 0)
            {
                return scoreCompare;
            }

            return right.DistanceFromStart.CompareTo(left.DistanceFromStart);
        }

        private static bool IsStartAdjacentRoom(DungeonRoomNode roomNode)
        {
            return roomNode != null && roomNode.DistanceFromStart <= 1;
        }

        private static bool IsBossApproachRoom(DungeonRoomNode roomNode, DungeonRoomNode bossApproachRoom)
        {
            return roomNode != null
                && bossApproachRoom != null
                && roomNode.GridPosition.Equals(bossApproachRoom.GridPosition);
        }

        private static bool IsTreasureCandidate(
            DungeonMap dungeonMap,
            DungeonRoomNode roomNode,
            HashSet<GridPosition> branchRoute,
            int minimumDistance,
            DungeonRoomNode bossApproachRoom)
        {
            return roomNode.RoomType == RoomType.Normal
                && roomNode.DistanceFromStart >= minimumDistance
                && !IsStartAdjacentRoom(roomNode)
                && !IsBossApproachRoom(roomNode, bossApproachRoom)
                && branchRoute.Contains(roomNode.GridPosition)
                && !IsAdjacentToRestrictedSpecialRoom(dungeonMap, roomNode);
        }

        private static bool IsShopLeafCandidate(
            DungeonMap dungeonMap,
            DungeonRoomNode roomNode,
            HashSet<GridPosition> branchRoute,
            int minimumDistance,
            int bossDistance,
            DungeonRoomNode bossApproachRoom)
        {
            return roomNode.RoomType == RoomType.Normal
                && roomNode.DistanceFromStart >= minimumDistance
                && roomNode.DistanceFromStart < bossDistance
                && !IsStartAdjacentRoom(roomNode)
                && !IsBossApproachRoom(roomNode, bossApproachRoom)
                && branchRoute.Contains(roomNode.GridPosition)
                && !IsAdjacentToRestrictedSpecialRoom(dungeonMap, roomNode);
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

        private static int ResolveUnlockedRoomCount(RoomType roomType, int configuredCount)
        {
            if (configuredCount <= 0)
            {
                return 0;
            }

            return UnlockManager.IsRoomTypeUnlocked(roomType) ? configuredCount : 0;
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
            public SecretRoomCandidate(
                GridPosition position,
                int distanceFromStart,
                List<SecretNeighbor> neighbors,
                int normalNeighborCount,
                int branchNeighborCount,
                int specialNeighborCount,
                int preferredDistanceDelta,
                float centerOffset)
            {
                Position = position;
                DistanceFromStart = distanceFromStart;
                Neighbors = neighbors;
                NormalNeighborCount = normalNeighborCount;
                BranchNeighborCount = branchNeighborCount;
                SpecialNeighborCount = specialNeighborCount;
                PreferredDistanceDelta = preferredDistanceDelta;
                CenterOffset = centerOffset;
            }

            public GridPosition Position { get; }
            public int DistanceFromStart { get; }
            public List<SecretNeighbor> Neighbors { get; }
            public int NeighborCount => Neighbors != null ? Neighbors.Count : 0;
            public int NormalNeighborCount { get; }
            public int BranchNeighborCount { get; }
            public int SpecialNeighborCount { get; }
            public int PreferredDistanceDelta { get; }
            public float CenterOffset { get; }
            public bool IsValid => Neighbors != null;
        }
    }
}
