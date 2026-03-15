using System;
using CuteIssac.Core.Meta;
using CuteIssac.Enemy;
using UnityEngine;

namespace CuteIssac.Data.Enemy
{
    /// <summary>
    /// One authored enemy candidate inside a pool.
    /// Future wave builders can use the tier, weight, unlock floor, and budget cost without knowing anything about a specific room implementation.
    /// </summary>
    [Serializable]
    public sealed class EnemySpawnEntry
    {
        [SerializeField] private string enemyId = "enemy";
        [SerializeField] private EnemyController enemyPrefab;
        [SerializeField] private EnemyEncounterTier encounterTier = EnemyEncounterTier.Normal;
        [SerializeField] [Min(0)] private int selectionWeight = 1;
        [SerializeField] [Min(1)] private int difficultyCost = 1;
        [SerializeField] [Min(1)] private int unlockFloor = 1;
        [SerializeField] private bool unlockedByDefault = true;
        [SerializeField] private string unlockKey;

        public string EnemyId => enemyId;
        public EnemyController EnemyPrefab => enemyPrefab;
        public EnemyEncounterTier EncounterTier => encounterTier;
        public int SelectionWeight => selectionWeight;
        public int DifficultyCost => difficultyCost;
        public int UnlockFloor => unlockFloor;
        public bool UnlockedByDefault => unlockedByDefault;
        public string UnlockKey => unlockKey;

        public bool IsAvailableForFloor(int floorIndex)
        {
            return enemyPrefab != null
                && floorIndex >= unlockFloor
                && UnlockManager.IsUnlocked(unlockKey, unlockedByDefault);
        }
    }
}
