using System;
using System.Collections.Generic;
using CuteIssac.Data.Enemy;
using UnityEngine;

namespace CuteIssac.Data.Dungeon
{
    /// <summary>
    /// Designer-authored floor generation rules.
    /// A future DungeonGenerator should read this asset and decide how many rooms to place and which room pools can satisfy each type.
    /// </summary>
    [CreateAssetMenu(fileName = "FloorConfig", menuName = "CuteIssac/Data/Dungeon/Floor Config")]
    public sealed class FloorConfig : ScriptableObject
    {
        [SerializeField] [Min(1)] private int floorIndex = 1;
        [SerializeField] [Min(0)] private int minNormalRoomCount = 4;
        [SerializeField] [Min(0)] private int maxNormalRoomCount = 7;
        [SerializeField] [Min(1)] private int minimumBossDistanceFromStart = 3;
        [SerializeField] [Min(0)] private int treasureRoomCount = 1;
        [SerializeField] [Min(1)] private int minimumTreasureDistanceFromStart = 2;
        [SerializeField] [Min(0)] private int shopRoomCount = 1;
        [SerializeField] [Min(1)] private int minimumShopDistanceFromStart = 2;
        [SerializeField] [Min(0)] private int secretRoomCount = 0;
        [SerializeField] [Min(1)] private int minimumSecretDistanceFromStart = 2;
        [SerializeField] [Min(1)] private int minimumSecretAdjacentRoomCount = 2;
        [SerializeField] private RoomPoolEntry startRoomPool;
        [SerializeField] private RoomPoolEntry bossRoomPool;
        [SerializeField] private List<RoomPoolEntry> optionalRoomPools = new();
        [SerializeField] private List<RoomLayoutSet> sharedLayoutSets = new();
        [SerializeField] private EnemyPoolData enemyPool;
        [SerializeField] [Min(1)] private int normalRoomEnemyBudget = 4;
        [SerializeField] [Min(1)] private int eliteRoomEnemyBudget = 8;
        [SerializeField] [Min(1)] private int bossRoomEnemyBudget = 14;
        [SerializeField] [Min(0)] private int normalRoomDistanceBudgetBonusPerStep = 1;
        [SerializeField] [Min(0f)] private float encounterBudgetMultiplier = 1f;

        public int FloorIndex => floorIndex;
        public int MinNormalRoomCount => minNormalRoomCount;
        public int MaxNormalRoomCount => maxNormalRoomCount;
        public int MinimumBossDistanceFromStart => minimumBossDistanceFromStart;
        public int MinTotalRoomCount => minNormalRoomCount + 1;
        public int MaxTotalRoomCount => maxNormalRoomCount + 1;
        public int TreasureRoomCount => treasureRoomCount;
        public int MinimumTreasureDistanceFromStart => minimumTreasureDistanceFromStart;
        public int ShopRoomCount => shopRoomCount;
        public int MinimumShopDistanceFromStart => minimumShopDistanceFromStart;
        public int SecretRoomCount => secretRoomCount;
        public int MinimumSecretDistanceFromStart => minimumSecretDistanceFromStart;
        public int MinimumSecretAdjacentRoomCount => minimumSecretAdjacentRoomCount;
        public RoomPoolEntry StartRoomPool => startRoomPool;
        public RoomPoolEntry BossRoomPool => bossRoomPool;
        public IReadOnlyList<RoomPoolEntry> OptionalRoomPools => optionalRoomPools;
        public IReadOnlyList<RoomLayoutSet> SharedLayoutSets => sharedLayoutSets;
        public EnemyPoolData EnemyPool => enemyPool;
        public int NormalRoomEnemyBudget => normalRoomEnemyBudget;
        public int EliteRoomEnemyBudget => eliteRoomEnemyBudget;
        public int BossRoomEnemyBudget => bossRoomEnemyBudget;
        public int NormalRoomDistanceBudgetBonusPerStep => normalRoomDistanceBudgetBonusPerStep;
        public float EncounterBudgetMultiplier => encounterBudgetMultiplier;

        public bool TryGetRoomPool(RoomType roomType, out RoomPoolEntry roomPoolEntry)
        {
            if (startRoomPool != null && startRoomPool.RoomType == roomType)
            {
                roomPoolEntry = startRoomPool;
                return true;
            }

            if (bossRoomPool != null && bossRoomPool.RoomType == roomType)
            {
                roomPoolEntry = bossRoomPool;
                return true;
            }

            for (int i = 0; i < optionalRoomPools.Count; i++)
            {
                RoomPoolEntry candidate = optionalRoomPools[i];

                if (candidate != null && candidate.RoomType == roomType)
                {
                    roomPoolEntry = candidate;
                    return true;
                }
            }

            roomPoolEntry = null;
            return false;
        }

        public void CollectCandidateRooms(RoomType roomType, List<RoomData> results)
        {
            if (results == null)
            {
                return;
            }

            if (startRoomPool != null && startRoomPool.RoomType == roomType)
            {
                AddCandidates(startRoomPool, results);
            }

            if (bossRoomPool != null && bossRoomPool.RoomType == roomType)
            {
                AddCandidates(bossRoomPool, results);
            }

            for (int i = 0; i < optionalRoomPools.Count; i++)
            {
                RoomPoolEntry candidate = optionalRoomPools[i];

                if (candidate != null && candidate.RoomType == roomType)
                {
                    AddCandidates(candidate, results);
                }
            }
        }

        public void CollectCandidateLayouts(RoomType roomType, List<RoomLayoutData> results)
        {
            if (results == null)
            {
                return;
            }

            for (int i = 0; i < sharedLayoutSets.Count; i++)
            {
                RoomLayoutSet layoutSet = sharedLayoutSets[i];

                if (layoutSet == null)
                {
                    continue;
                }

                layoutSet.CollectLayouts(roomType, results);
            }
        }

        /// <summary>
        /// Exposes floor-specific enemy candidates without making room generation depend on a concrete pool layout.
        /// Future room encounter builders can request a tier and receive only entries valid for this floor.
        /// </summary>
        public void CollectEnemySpawnEntries(EnemyEncounterTier encounterTier, List<EnemySpawnEntry> results)
        {
            if (results == null || enemyPool == null)
            {
                return;
            }

            enemyPool.CollectEntries(encounterTier, floorIndex, results);
        }

        public int GetEnemyBudget(EnemyEncounterTier encounterTier)
        {
            int baseBudget = encounterTier switch
            {
                EnemyEncounterTier.Elite => eliteRoomEnemyBudget,
                EnemyEncounterTier.Boss => bossRoomEnemyBudget,
                _ => normalRoomEnemyBudget
            };

            return Mathf.Max(1, Mathf.RoundToInt(baseBudget * Mathf.Max(0f, encounterBudgetMultiplier)));
        }

        private static void AddCandidates(RoomPoolEntry roomPoolEntry, List<RoomData> results)
        {
            for (int i = 0; i < roomPoolEntry.CandidateRooms.Count; i++)
            {
                RoomData roomData = roomPoolEntry.CandidateRooms[i];

                if (roomData != null)
                {
                    results.Add(roomData);
                }
            }
        }

        [Serializable]
        public sealed class RoomPoolEntry
        {
            [SerializeField] private RoomType roomType = RoomType.Normal;
            [SerializeField] private List<RoomData> candidateRooms = new();
            [SerializeField] [Min(0)] private int minimumCount;
            [SerializeField] [Min(0)] private int maximumCount = 1;

            public RoomType RoomType => roomType;
            public IReadOnlyList<RoomData> CandidateRooms => candidateRooms;
            public int MinimumCount => minimumCount;
            public int MaximumCount => maximumCount;
        }
    }
}
