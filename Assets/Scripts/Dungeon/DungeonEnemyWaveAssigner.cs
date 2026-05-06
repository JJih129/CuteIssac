using System.Collections.Generic;
using CuteIssac.Data.Dungeon;
using CuteIssac.Data.Enemy;
using CuteIssac.Enemy;
using UnityEngine;

namespace CuteIssac.Dungeon
{
    /// <summary>
    /// Assigns enemy wave plans to generated rooms.
    /// Room-authored overrides win first, then floor-authored wave presets, then the weighted floor pool fallback.
    /// </summary>
    public sealed class DungeonEnemyWaveAssigner
    {
        private readonly List<EnemySpawnEntry> _candidateBuffer = new();
        private readonly List<FloorConfig.EnemyWavePresetEntry> _wavePresetBuffer = new();

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
                {
                    int bossBudget = floorConfig.GetEnemyBudget(EnemyEncounterTier.Boss);

                    if (overrideWave != null)
                    {
                        return overrideWave.BuildAssignment(roomNode.DistanceFromStart, bossBudget);
                    }

                    if (TryBuildPresetWave(
                        floorConfig,
                        roomNode,
                        EnemyEncounterTier.Boss,
                        bossBudget,
                        out EnemyWaveAssignment bossPresetWave))
                    {
                        return bossPresetWave;
                    }

                    return BuildGeneratedEncounterWave(
                        floorConfig,
                        roomNode,
                        EnemyEncounterTier.Boss,
                        "generated-boss",
                        bossBudget);
                }
                case RoomType.MiniBoss:
                {
                    int eliteBudget = floorConfig.GetEnemyBudget(EnemyEncounterTier.Elite);

                    if (overrideWave != null)
                    {
                        return overrideWave.BuildAssignment(roomNode.DistanceFromStart, eliteBudget);
                    }

                    if (TryBuildPresetWave(
                        floorConfig,
                        roomNode,
                        EnemyEncounterTier.Elite,
                        eliteBudget,
                        out EnemyWaveAssignment elitePresetWave))
                    {
                        return elitePresetWave;
                    }

                    return BuildGeneratedEncounterWave(
                        floorConfig,
                        roomNode,
                        EnemyEncounterTier.Elite,
                        "generated-miniboss",
                        eliteBudget);
                }
                case RoomType.Challenge:
                {
                    int challengeBudget = floorConfig.GetEnemyBudget(EnemyEncounterTier.Elite);

                    if (overrideWave != null)
                    {
                        return overrideWave.BuildAssignment(roomNode.DistanceFromStart, challengeBudget);
                    }

                    if (TryBuildPresetWave(
                        floorConfig,
                        roomNode,
                        EnemyEncounterTier.Elite,
                        challengeBudget,
                        out EnemyWaveAssignment challengePresetWave))
                    {
                        return challengePresetWave;
                    }

                    return BuildGeneratedEncounterWave(
                        floorConfig,
                        roomNode,
                        EnemyEncounterTier.Elite,
                        "generated-challenge",
                        challengeBudget);
                }
                case RoomType.Normal:
                {
                    int normalBudget = ResolveNormalRoomBudget(floorConfig, roomNode);

                    if (overrideWave != null)
                    {
                        return overrideWave.BuildAssignment(roomNode.DistanceFromStart, normalBudget);
                    }

                    if (TryBuildPresetWave(
                        floorConfig,
                        roomNode,
                        EnemyEncounterTier.Normal,
                        normalBudget,
                        out EnemyWaveAssignment normalPresetWave))
                    {
                        return normalPresetWave;
                    }

                    return BuildGeneratedEncounterWave(
                        floorConfig,
                        roomNode,
                        EnemyEncounterTier.Normal,
                        "generated-normal",
                        normalBudget);
                }
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

        private bool TryBuildPresetWave(
            FloorConfig floorConfig,
            DungeonRoomNode roomNode,
            EnemyEncounterTier encounterTier,
            int targetBudget,
            out EnemyWaveAssignment waveAssignment)
        {
            waveAssignment = null;

            if (floorConfig == null || roomNode == null)
            {
                return false;
            }

            _wavePresetBuffer.Clear();
            floorConfig.CollectEnemyWavePresets(encounterTier, _wavePresetBuffer);

            if (_wavePresetBuffer.Count == 0)
            {
                return false;
            }

            FloorConfig.EnemyWavePresetEntry selectedPreset = SelectWeightedWavePreset(_wavePresetBuffer);

            if (selectedPreset?.WaveData == null)
            {
                return false;
            }

            waveAssignment = selectedPreset.WaveData.BuildAssignment(roomNode.DistanceFromStart, targetBudget);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log(
                $"Selected enemy wave preset '{selectedPreset.PresetId}' for {encounterTier} room at {roomNode.GridPosition} (floor {floorConfig.FloorIndex}, budget {targetBudget}).",
                floorConfig);
#endif

            return waveAssignment != null && waveAssignment.TotalEnemyCount > 0;
        }

        private static FloorConfig.EnemyWavePresetEntry SelectWeightedWavePreset(List<FloorConfig.EnemyWavePresetEntry> candidates)
        {
            int totalWeight = 0;

            for (int i = 0; i < candidates.Count; i++)
            {
                FloorConfig.EnemyWavePresetEntry entry = candidates[i];

                if (entry?.WaveData == null)
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

            for (int i = 0; i < candidates.Count; i++)
            {
                FloorConfig.EnemyWavePresetEntry entry = candidates[i];

                if (entry?.WaveData == null)
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
    }
}
