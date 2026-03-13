using System;
using System.Collections.Generic;
using CuteIssac.Enemy;
using UnityEngine;

namespace CuteIssac.Data.Enemy
{
    /// <summary>
    /// Authored enemy wave asset.
    /// Boss rooms or hand-authored rooms can reference this directly, while generated normal rooms can still produce runtime wave assignments from floor pools.
    /// </summary>
    [CreateAssetMenu(fileName = "EnemyWaveData", menuName = "CuteIssac/Data/Enemy/Enemy Wave Data")]
    public sealed class EnemyWaveData : ScriptableObject
    {
        [SerializeField] private string waveId = "wave";
        [SerializeField] private EnemyEncounterTier encounterTier = EnemyEncounterTier.Normal;
        [SerializeField] private List<EnemyWaveEntry> entries = new();

        public string WaveId => waveId;
        public EnemyEncounterTier EncounterTier => encounterTier;
        public IReadOnlyList<EnemyWaveEntry> Entries => entries;

        public EnemyWaveAssignment BuildAssignment(int distanceFromStart, int targetBudget)
        {
            EnemyWaveAssignment assignment = new(waveId, encounterTier, distanceFromStart, targetBudget);

            for (int i = 0; i < entries.Count; i++)
            {
                EnemyWaveEntry entry = entries[i];

                if (entry != null)
                {
                    assignment.AddSpawn(entry.EnemyPrefab, entry.EnemyId, entry.Count, entry.DifficultyCost);
                }
            }

            return assignment;
        }
    }

    /// <summary>
    /// One enemy line inside an authored wave asset.
    /// This keeps designer-defined waves readable while still exposing cost metadata to future balancing tools.
    /// </summary>
    [Serializable]
    public sealed class EnemyWaveEntry
    {
        [SerializeField] private string enemyId = "enemy";
        [SerializeField] private EnemyController enemyPrefab;
        [SerializeField] [Min(1)] private int count = 1;
        [SerializeField] [Min(1)] private int difficultyCost = 1;

        public string EnemyId => enemyId;
        public EnemyController EnemyPrefab => enemyPrefab;
        public int Count => count;
        public int DifficultyCost => difficultyCost;
    }
}
