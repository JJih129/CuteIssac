using System.Collections.Generic;
using CuteIssac.Data.Dungeon;
using CuteIssac.Data.Enemy;
using CuteIssac.Enemy;
using UnityEngine;

namespace CuteIssac.Dungeon
{
    /// <summary>
    /// Assigns simple enemy wave plans to generated rooms.
    /// Normal rooms auto-build from the normal pool, while miniboss rooms auto-build from the elite pool.
    /// </summary>
    public sealed class DungeonEnemyWaveAssigner
    {
        private readonly List<EnemySpawnEntry> _candidateBuffer = new();

        public void Assign(DungeonMap dungeonMap)
        {
            if (dungeonMap == null || dungeonMap.FloorConfig == null)
            {
                return;
            }

            foreach (KeyValuePair<GridPosition, DungeonRoomNode> roomPair in dungeonMap.RoomsByPosition)
            {
                DungeonRoomNode roomNode = roomPair.Value;
                EnemyWaveAssignment assignedWave = BuildWaveForRoom(dungeonMap.FloorConfig, roomNode);
                roomNode.SetAssignedEnemyWave(assignedWave);
            }
        }

        private EnemyWaveAssignment BuildWaveForRoom(FloorConfig floorConfig, DungeonRoomNode roomNode)
        {
            if (roomNode == null)
            {
                return null;
            }

            EnemyWaveData overrideWave = roomNode.RoomData != null ? roomNode.RoomData.EnemyWaveOverride : null;

            switch (roomNode.RoomType)
            {
                case RoomType.Start:
                case RoomType.Treasure:
                case RoomType.Shop:
                case RoomType.Secret:
                case RoomType.Trap:
                case RoomType.Curse:
                    return null;
                case RoomType.Boss:
                    return overrideWave != null
                        ? overrideWave.BuildAssignment(roomNode.DistanceFromStart, floorConfig.GetEnemyBudget(EnemyEncounterTier.Boss))
                        : null;
                case RoomType.MiniBoss:
                    if (overrideWave != null)
                    {
                        return overrideWave.BuildAssignment(roomNode.DistanceFromStart, floorConfig.GetEnemyBudget(EnemyEncounterTier.Elite));
                    }

                    return BuildGeneratedEncounterWave(
                        floorConfig,
                        roomNode,
                        EnemyEncounterTier.Elite,
                        "generated-miniboss",
                        floorConfig.GetEnemyBudget(EnemyEncounterTier.Elite));
                case RoomType.Challenge:
                    if (overrideWave != null)
                    {
                        return overrideWave.BuildAssignment(roomNode.DistanceFromStart, floorConfig.GetEnemyBudget(EnemyEncounterTier.Elite));
                    }

                    return BuildGeneratedEncounterWave(
                        floorConfig,
                        roomNode,
                        EnemyEncounterTier.Elite,
                        "generated-challenge",
                        floorConfig.GetEnemyBudget(EnemyEncounterTier.Elite));
                case RoomType.Normal:
                    if (overrideWave != null)
                    {
                        return overrideWave.BuildAssignment(roomNode.DistanceFromStart, ResolveNormalRoomBudget(floorConfig, roomNode));
                    }

                    return BuildGeneratedEncounterWave(
                        floorConfig,
                        roomNode,
                        EnemyEncounterTier.Normal,
                        "generated-normal",
                        ResolveNormalRoomBudget(floorConfig, roomNode));
                default:
                    return null;
            }
        }

        private EnemyWaveAssignment BuildGeneratedEncounterWave(
            FloorConfig floorConfig,
            DungeonRoomNode roomNode,
            EnemyEncounterTier encounterTier,
            string assignmentPrefix,
            int targetBudget)
        {
            _candidateBuffer.Clear();
            floorConfig.CollectEnemySpawnEntries(encounterTier, _candidateBuffer);

            if (_candidateBuffer.Count == 0)
            {
                return null;
            }

            EnemyWaveAssignment assignment = new(
                $"{assignmentPrefix}-{roomNode.GridPosition.X}-{roomNode.GridPosition.Y}",
                encounterTier,
                roomNode.DistanceFromStart,
                targetBudget);

            int remainingBudget = targetBudget;
            int safetyCounter = 0;

            // Fill the room until the budget runs out. Distance from start increases the budget so farther rooms tend to get denser waves.
            while (remainingBudget > 0 && safetyCounter < 32)
            {
                EnemySpawnEntry selectedEntry = SelectWeightedEntry(remainingBudget);

                if (selectedEntry == null)
                {
                    if (assignment.TotalEnemyCount == 0)
                    {
                        selectedEntry = SelectCheapestEntry();
                    }

                    if (selectedEntry == null)
                    {
                        break;
                    }
                }

                assignment.AddSpawn(selectedEntry.EnemyPrefab, selectedEntry.EnemyId, 1, selectedEntry.DifficultyCost);
                remainingBudget -= Mathf.Max(1, selectedEntry.DifficultyCost);
                safetyCounter++;
            }

            return assignment.TotalEnemyCount > 0 ? assignment : null;
        }

        private int ResolveNormalRoomBudget(FloorConfig floorConfig, DungeonRoomNode roomNode)
        {
            int baseBudget = floorConfig.GetEnemyBudget(EnemyEncounterTier.Normal);
            int distanceBonus = Mathf.Max(0, roomNode.DistanceFromStart) * floorConfig.NormalRoomDistanceBudgetBonusPerStep;
            return Mathf.Max(1, baseBudget + distanceBonus);
        }

        private EnemySpawnEntry SelectWeightedEntry(int remainingBudget)
        {
            int totalWeight = 0;

            for (int i = 0; i < _candidateBuffer.Count; i++)
            {
                EnemySpawnEntry entry = _candidateBuffer[i];

                if (entry == null || entry.DifficultyCost > remainingBudget || entry.EnemyPrefab == null)
                {
                    continue;
                }

                totalWeight += Mathf.Max(1, entry.SelectionWeight);
            }

            if (totalWeight <= 0)
            {
                return null;
            }

            int roll = Random.Range(0, totalWeight);

            for (int i = 0; i < _candidateBuffer.Count; i++)
            {
                EnemySpawnEntry entry = _candidateBuffer[i];

                if (entry == null || entry.DifficultyCost > remainingBudget || entry.EnemyPrefab == null)
                {
                    continue;
                }

                roll -= Mathf.Max(1, entry.SelectionWeight);

                if (roll < 0)
                {
                    return entry;
                }
            }

            return null;
        }

        private EnemySpawnEntry SelectCheapestEntry()
        {
            EnemySpawnEntry cheapestEntry = null;
            int lowestCost = int.MaxValue;

            for (int i = 0; i < _candidateBuffer.Count; i++)
            {
                EnemySpawnEntry entry = _candidateBuffer[i];

                if (entry == null || entry.EnemyPrefab == null)
                {
                    continue;
                }

                if (entry.DifficultyCost < lowestCost)
                {
                    lowestCost = entry.DifficultyCost;
                    cheapestEntry = entry;
                }
            }

            return cheapestEntry;
        }
    }
}
