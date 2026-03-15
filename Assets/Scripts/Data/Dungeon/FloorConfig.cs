using System;
using System.Collections.Generic;
using CuteIssac.Data.Enemy;
using CuteIssac.Data.Item;
using CuteIssac.Data.Room;
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
        [SerializeField] [Min(1)] private int maxGenerationAttempts = 8;
        [SerializeField] [Min(1)] private int startRoomInitialBranchCount = 2;
        [SerializeField] [Range(0f, 1f)] private float additionalBranchChance = 0.35f;
        [SerializeField] [Min(1)] private int minimumBossDistanceFromStart = 3;
        [SerializeField] [Min(0)] private int treasureRoomCount = 1;
        [SerializeField] [Min(1)] private int minimumTreasureDistanceFromStart = 2;
        [SerializeField] [Min(0)] private int challengeRoomCount;
        [SerializeField] [Min(1)] private int minimumChallengeDistanceFromStart = 2;
        [SerializeField] [Min(0)] private int trapRoomCount;
        [SerializeField] [Min(1)] private int minimumTrapDistanceFromStart = 2;
        [SerializeField] [Min(0)] private int curseRoomCount;
        [SerializeField] [Min(1)] private int minimumCurseDistanceFromStart = 2;
        [SerializeField] [Min(0)] private int shopRoomCount = 1;
        [SerializeField] [Min(1)] private int minimumShopDistanceFromStart = 2;
        [SerializeField] [Min(0)] private int miniBossRoomCount;
        [SerializeField] [Min(1)] private int minimumMiniBossDistanceFromStart = 2;
        [SerializeField] [Min(0)] private int secretRoomCount = 0;
        [SerializeField] [Min(1)] private int minimumSecretDistanceFromStart = 2;
        [SerializeField] [Min(3)] private int minimumSecretAdjacentRoomCount = 3;
        [SerializeField] private RoomPoolEntry startRoomPool;
        [SerializeField] private RoomPoolEntry bossRoomPool;
        [SerializeField] private List<RoomPoolEntry> optionalRoomPools = new();
        [SerializeField] private List<RoomLayoutSet> sharedLayoutSets = new();
        [SerializeField] private EnemyPoolData enemyPool;
        [SerializeField] private RoomThemeData roomTheme;
        [SerializeField] [Min(1)] private int normalRoomEnemyBudget = 4;
        [SerializeField] [Min(1)] private int eliteRoomEnemyBudget = 8;
        [SerializeField] [Min(1)] private int bossRoomEnemyBudget = 14;
        [SerializeField] [Min(0)] private int normalRoomDistanceBudgetBonusPerStep = 1;
        [SerializeField] [Min(0f)] private float encounterBudgetMultiplier = 1f;
        [SerializeField] private EncounterPacingSettings encounterPacing = new();
        [SerializeField] private EncounterPacingSettings challengeRoomEncounterPacing = EncounterPacingSettings.CreateChallengeDefault();
        [SerializeField] private ChallengeRewardSettings challengeRewardSettings = ChallengeRewardSettings.CreateDefault();
        [SerializeField] private SecretRoomRewardSettings secretRoomRewardSettings = SecretRoomRewardSettings.CreateDefault();
        [SerializeField] private RoomRewardTable normalRoomRewardPool;
        [SerializeField] private RoomRewardTable bossRoomRewardPool;
        [SerializeField] private RoomRewardTable treasureRoomRewardPool;
        [SerializeField] private RoomRewardTable shopRoomRewardPool;
        [SerializeField] private RoomRewardTable secretRoomRewardPool;
        [SerializeField] private RoomRewardTable curseRoomRewardPool;
        [SerializeField] private ItemPoolData treasureRoomItemPool;
        [SerializeField] private ItemPoolData challengeRoomItemPool;
        [SerializeField] private ItemPoolData shopRoomItemPool;
        [SerializeField] private ItemPoolData bossRewardItemPool;
        [SerializeField] private ItemPoolData secretRoomItemPool;
        [SerializeField] private ItemPoolData curseRoomItemPool;

        public int FloorIndex => floorIndex;
        public int MinNormalRoomCount => minNormalRoomCount;
        public int MaxNormalRoomCount => maxNormalRoomCount;
        public int MaxGenerationAttempts => Mathf.Max(1, maxGenerationAttempts);
        public int StartRoomInitialBranchCount => Mathf.Clamp(startRoomInitialBranchCount <= 0 ? 2 : startRoomInitialBranchCount, 1, 4);
        public float AdditionalBranchChance => Mathf.Clamp01(additionalBranchChance);
        public int MinimumBossDistanceFromStart => minimumBossDistanceFromStart;
        public int MinTotalRoomCount => minNormalRoomCount + 1;
        public int MaxTotalRoomCount => maxNormalRoomCount + 1;
        public int TreasureRoomCount => treasureRoomCount;
        public int MinimumTreasureDistanceFromStart => minimumTreasureDistanceFromStart;
        public int ChallengeRoomCount => challengeRoomCount;
        public int MinimumChallengeDistanceFromStart => minimumChallengeDistanceFromStart;
        public int TrapRoomCount => trapRoomCount;
        public int MinimumTrapDistanceFromStart => minimumTrapDistanceFromStart;
        public int CurseRoomCount => curseRoomCount;
        public int MinimumCurseDistanceFromStart => minimumCurseDistanceFromStart;
        public int ShopRoomCount => shopRoomCount;
        public int MinimumShopDistanceFromStart => minimumShopDistanceFromStart;
        public int MiniBossRoomCount => miniBossRoomCount;
        public int MinimumMiniBossDistanceFromStart => minimumMiniBossDistanceFromStart;
        public int SecretRoomCount => secretRoomCount;
        public int MinimumSecretDistanceFromStart => minimumSecretDistanceFromStart;
        public int MinimumSecretAdjacentRoomCount => minimumSecretAdjacentRoomCount;
        public RoomPoolEntry StartRoomPool => startRoomPool;
        public RoomPoolEntry BossRoomPool => bossRoomPool;
        public IReadOnlyList<RoomPoolEntry> OptionalRoomPools => optionalRoomPools;
        public IReadOnlyList<RoomLayoutSet> SharedLayoutSets => sharedLayoutSets;
        public EnemyPoolData EnemyPool => enemyPool;
        public RoomThemeData RoomTheme => roomTheme;
        public int NormalRoomEnemyBudget => normalRoomEnemyBudget;
        public int EliteRoomEnemyBudget => eliteRoomEnemyBudget;
        public int BossRoomEnemyBudget => bossRoomEnemyBudget;
        public int NormalRoomDistanceBudgetBonusPerStep => normalRoomDistanceBudgetBonusPerStep;
        public float EncounterBudgetMultiplier => encounterBudgetMultiplier;
        public EncounterPacingSettings EncounterPacing => encounterPacing;
        public EncounterPacingSettings ChallengeRoomEncounterPacing => challengeRoomEncounterPacing;
        public ChallengeRewardSettings ChallengeRewardSettings => challengeRewardSettings ?? ChallengeRewardSettings.CreateDefault();
        public SecretRoomRewardSettings SecretRoomRewardSettings => secretRoomRewardSettings ?? SecretRoomRewardSettings.CreateDefault();
        public RoomRewardTable NormalRoomRewardPool => normalRoomRewardPool;
        public RoomRewardTable BossRoomRewardPool => bossRoomRewardPool;
        public RoomRewardTable TreasureRoomRewardPool => treasureRoomRewardPool;
        public RoomRewardTable ShopRoomRewardPool => shopRoomRewardPool;
        public RoomRewardTable SecretRoomRewardPool => secretRoomRewardPool;
        public RoomRewardTable CurseRoomRewardPool => curseRoomRewardPool;
        public ItemPoolData TreasureRoomItemPool => treasureRoomItemPool;
        public ItemPoolData ChallengeRoomItemPool => challengeRoomItemPool;
        public ItemPoolData ShopRoomItemPool => shopRoomItemPool;
        public ItemPoolData BossRewardItemPool => bossRewardItemPool;
        public ItemPoolData SecretRoomItemPool => secretRoomItemPool;
        public ItemPoolData CurseRoomItemPool => curseRoomItemPool;

        public bool TryGetRoomPool(RoomType roomType, out RoomPoolEntry roomPoolEntry)
        {
            if (TryGetExactRoomPool(roomType, out roomPoolEntry))
            {
                return true;
            }

            if (roomType == RoomType.Curse)
            {
                return TryGetExactRoomPool(RoomType.Trap, out roomPoolEntry)
                    || TryGetExactRoomPool(RoomType.Normal, out roomPoolEntry);
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

            int initialCount = results.Count;
            CollectExactCandidateRooms(roomType, results);

            if (roomType == RoomType.Curse && results.Count == initialCount)
            {
                CollectExactCandidateRooms(RoomType.Trap, results);

                if (results.Count == initialCount)
                {
                    CollectExactCandidateRooms(RoomType.Normal, results);
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

        public EncounterPacingSettings GetEncounterPacing(RoomType roomType)
        {
            if (roomType == RoomType.Challenge)
            {
                return challengeRoomEncounterPacing ?? encounterPacing;
            }

            return encounterPacing;
        }

        /// <summary>
        /// Exposes floor-specific reward pools so generated rooms can differ by floor without hardcoding reward tables in prefabs.
        /// </summary>
        public RoomRewardTable GetRewardPool(RoomType roomType)
        {
            return roomType switch
            {
                RoomType.Boss => bossRoomRewardPool,
                RoomType.MiniBoss => bossRoomRewardPool,
                RoomType.Treasure => treasureRoomRewardPool,
                RoomType.Shop => shopRoomRewardPool,
                RoomType.Secret => secretRoomRewardPool,
                RoomType.Curse => curseRoomRewardPool != null ? curseRoomRewardPool : normalRoomRewardPool,
                RoomType.Normal or RoomType.Challenge or RoomType.Trap => normalRoomRewardPool,
                _ => null
            };
        }

        /// <summary>
        /// Exposes floor-specific item pools so treasure/shop/boss rooms can feel different per floor
        /// without hardcoding room content inside prefabs or global catalogs.
        /// </summary>
        public ItemPoolData GetItemPool(RoomType roomType)
        {
            return roomType switch
            {
                RoomType.Treasure => treasureRoomItemPool,
                RoomType.Challenge => challengeRoomItemPool != null ? challengeRoomItemPool : treasureRoomItemPool,
                RoomType.Shop => shopRoomItemPool,
                RoomType.Boss => bossRewardItemPool,
                RoomType.Secret => secretRoomItemPool,
                RoomType.Curse => curseRoomItemPool != null ? curseRoomItemPool : treasureRoomItemPool,
                _ => null
            };
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

        private bool TryGetExactRoomPool(RoomType roomType, out RoomPoolEntry roomPoolEntry)
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

        private void CollectExactCandidateRooms(RoomType roomType, List<RoomData> results)
        {
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
