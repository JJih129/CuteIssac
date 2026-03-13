using System.Collections.Generic;
using UnityEngine;

namespace CuteIssac.Data.Enemy
{
    /// <summary>
    /// Floor-facing enemy pool asset.
    /// It separates authored enemy availability from runtime spawn logic so later systems can build room waves without hardcoding per-floor enemy lists.
    /// </summary>
    [CreateAssetMenu(fileName = "EnemyPoolData", menuName = "CuteIssac/Data/Enemy/Enemy Pool Data")]
    public sealed class EnemyPoolData : ScriptableObject
    {
        [SerializeField] private string poolId = "enemy-pool";
        [SerializeField] private List<EnemySpawnEntry> normalEnemies = new();
        [SerializeField] private List<EnemySpawnEntry> eliteEnemies = new();
        [SerializeField] private List<EnemySpawnEntry> bossEnemies = new();

        public string PoolId => poolId;
        public IReadOnlyList<EnemySpawnEntry> NormalEnemies => normalEnemies;
        public IReadOnlyList<EnemySpawnEntry> EliteEnemies => eliteEnemies;
        public IReadOnlyList<EnemySpawnEntry> BossEnemies => bossEnemies;

        /// <summary>
        /// Copies entries that are valid for the requested floor into a caller-owned buffer.
        /// A shared buffer pattern keeps future generation code allocation-light.
        /// </summary>
        public void CollectEntries(EnemyEncounterTier encounterTier, int floorIndex, List<EnemySpawnEntry> results)
        {
            if (results == null)
            {
                return;
            }

            List<EnemySpawnEntry> source = GetSourceList(encounterTier);

            for (int i = 0; i < source.Count; i++)
            {
                EnemySpawnEntry entry = source[i];

                if (entry != null && entry.IsAvailableForFloor(floorIndex))
                {
                    results.Add(entry);
                }
            }
        }

        public bool HasEntries(EnemyEncounterTier encounterTier, int floorIndex)
        {
            List<EnemySpawnEntry> source = GetSourceList(encounterTier);

            for (int i = 0; i < source.Count; i++)
            {
                EnemySpawnEntry entry = source[i];

                if (entry != null && entry.IsAvailableForFloor(floorIndex))
                {
                    return true;
                }
            }

            return false;
        }

        private List<EnemySpawnEntry> GetSourceList(EnemyEncounterTier encounterTier)
        {
            return encounterTier switch
            {
                EnemyEncounterTier.Elite => eliteEnemies,
                EnemyEncounterTier.Boss => bossEnemies,
                _ => normalEnemies
            };
        }
    }
}
