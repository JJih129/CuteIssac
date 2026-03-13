using System;
using System.Collections.Generic;
using CuteIssac.Data.Enemy;

namespace CuteIssac.Enemy
{
    /// <summary>
    /// Runtime enemy wave plan assigned to one generated room.
    /// This is kept separate from ScriptableObject data so generation can attach room-specific difficulty and composition without mutating authored assets.
    /// </summary>
    [Serializable]
    public sealed class EnemyWaveAssignment
    {
        private readonly List<EnemyWaveSpawnGroup> _spawnGroups = new();

        public EnemyWaveAssignment(string waveId, EnemyEncounterTier encounterTier, int distanceFromStart, int targetBudget)
        {
            WaveId = waveId;
            EncounterTier = encounterTier;
            DistanceFromStart = distanceFromStart;
            TargetBudget = targetBudget;
        }

        public string WaveId { get; }
        public EnemyEncounterTier EncounterTier { get; }
        public int DistanceFromStart { get; }
        public int TargetBudget { get; }
        public int TotalEnemyCount { get; private set; }
        public IReadOnlyList<EnemyWaveSpawnGroup> SpawnGroups => _spawnGroups;

        public void AddSpawn(EnemyController enemyPrefab, string enemyId, int count, int difficultyCost)
        {
            if (enemyPrefab == null || count <= 0)
            {
                return;
            }

            for (int i = 0; i < _spawnGroups.Count; i++)
            {
                if (_spawnGroups[i].EnemyPrefab == enemyPrefab)
                {
                    _spawnGroups[i].IncreaseCount(count);
                    TotalEnemyCount += count;
                    return;
                }
            }

            _spawnGroups.Add(new EnemyWaveSpawnGroup(enemyId, enemyPrefab, count, difficultyCost));
            TotalEnemyCount += count;
        }
    }

    /// <summary>
    /// Aggregated enemy count for one prefab inside a resolved wave.
    /// Later room spawners can iterate this list directly to instantiate enemies.
    /// </summary>
    [Serializable]
    public sealed class EnemyWaveSpawnGroup
    {
        public EnemyWaveSpawnGroup(string enemyId, EnemyController enemyPrefab, int count, int difficultyCost)
        {
            EnemyId = enemyId;
            EnemyPrefab = enemyPrefab;
            Count = count;
            DifficultyCost = difficultyCost;
        }

        public string EnemyId { get; }
        public EnemyController EnemyPrefab { get; }
        public int Count { get; private set; }
        public int DifficultyCost { get; }

        public void IncreaseCount(int additionalCount)
        {
            Count += additionalCount;
        }
    }
}
