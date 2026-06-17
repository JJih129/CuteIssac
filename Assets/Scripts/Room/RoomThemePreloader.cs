using System.Collections.Generic;
using CuteIssac.Core.Pooling;
using CuteIssac.Data.Dungeon;
using CuteIssac.Data.Enemy;
using CuteIssac.Data.Room;
using CuteIssac.Data.Run;
using UnityEngine;

namespace CuteIssac.Room
{
    /// <summary>
    /// 런 시작 전에 층별 방/문 테마 프리팹을 풀에 올려 첫 방 진입 시 스파이크를 줄입니다.
    /// 적 프리팹 선로딩과 같은 방향으로, 실제 배치는 RoomThemeController가 담당합니다.
    /// </summary>
    public static class RoomThemePreloader
    {
        private static readonly Dictionary<GameObject, int> s_prewarmedPrefabCounts = new();
        private static readonly List<EnemySpawnEntry> s_enemyEntryBuffer = new();
        private static readonly RoomType[] s_rewardPrewarmRoomTypes =
        {
            RoomType.Normal,
            RoomType.Challenge,
            RoomType.Trap,
            RoomType.Treasure,
            RoomType.Shop,
            RoomType.Secret,
            RoomType.Curse,
            RoomType.MiniBoss,
            RoomType.Boss
        };

        public static void PrewarmRunConfiguration(
            RunConfiguration runConfiguration,
            int roomVisualPrewarmCount,
            int doorVisualPrewarmCount,
            int decorationPrewarmCount)
        {
            if (runConfiguration == null || !Application.isPlaying)
            {
                return;
            }

            IReadOnlyList<StageProfile> stageSequence = runConfiguration.StageSequence;

            if (stageSequence != null)
            {
                for (int index = 0; index < stageSequence.Count; index++)
                {
                    PrewarmStageProfile(
                        stageSequence[index],
                        roomVisualPrewarmCount,
                        doorVisualPrewarmCount,
                        decorationPrewarmCount);
                }
            }

            IReadOnlyList<FloorConfig> floorSequence = runConfiguration.FloorSequence;

            if (floorSequence == null)
            {
                return;
            }

            for (int index = 0; index < floorSequence.Count; index++)
            {
                FloorConfig floorConfig = floorSequence[index];

                if (floorConfig == null)
                {
                    continue;
                }

                PrewarmRoomTheme(
                    floorConfig.RoomTheme,
                    roomVisualPrewarmCount,
                    doorVisualPrewarmCount,
                    decorationPrewarmCount);
            }
        }

        private static void PrewarmStageProfile(
            StageProfile stageProfile,
            int fallbackRoomVisualPrewarmCount,
            int fallbackDoorVisualPrewarmCount,
            int fallbackDecorationPrewarmCount)
        {
            if (stageProfile == null)
            {
                return;
            }

            StagePrewarmProfile prewarmProfile = stageProfile.PrewarmProfile;

            PrewarmRoomTheme(
                stageProfile.RoomTheme,
                Mathf.Max(fallbackRoomVisualPrewarmCount, prewarmProfile.RoomVisualPrewarmCount),
                Mathf.Max(fallbackDoorVisualPrewarmCount, prewarmProfile.DoorVisualPrewarmCount),
                Mathf.Max(fallbackDecorationPrewarmCount, prewarmProfile.DecorationPrewarmCount));
            PrewarmEnemyPool(stageProfile, prewarmProfile.EnemyPrefabPrewarmCount);
            PrewarmRewardPools(stageProfile, prewarmProfile.RewardPickupPrewarmCount);
        }

        private static void PrewarmRoomTheme(
            RoomThemeData roomTheme,
            int roomVisualPrewarmCount,
            int doorVisualPrewarmCount,
            int decorationPrewarmCount)
        {
            if (roomTheme == null)
            {
                return;
            }

            PrewarmPrefab(roomTheme.RoomVisualPrefab, roomVisualPrewarmCount);
            PrewarmPrefab(roomTheme.FloorVisualPrefab, roomVisualPrewarmCount);
            PrewarmPrefab(roomTheme.WallVisualPrefab, roomVisualPrewarmCount);
            PrewarmPrefab(roomTheme.DoorVisualPrefab, doorVisualPrewarmCount);

            IReadOnlyList<GameObject> decorationPrefabs = roomTheme.DecorationPrefabs;

            if (decorationPrefabs == null)
            {
                return;
            }

            for (int index = 0; index < decorationPrefabs.Count; index++)
            {
                PrewarmPrefab(decorationPrefabs[index], decorationPrewarmCount);
            }
        }

        private static void PrewarmPrefab(GameObject prefab, int count)
        {
            if (prefab == null || count <= 0)
            {
                return;
            }

            if (s_prewarmedPrefabCounts.TryGetValue(prefab, out int previousCount) && previousCount >= count)
            {
                return;
            }

            PrefabPoolService.EnsurePrewarmed(prefab, count);
            s_prewarmedPrefabCounts[prefab] = count;
        }

        private static void PrewarmEnemyPool(StageProfile stageProfile, int enemyPrewarmCount)
        {
            if (stageProfile == null || enemyPrewarmCount <= 0)
            {
                return;
            }

            EnemyPoolData enemyPool = stageProfile.EnemyPool;
            if (enemyPool == null)
            {
                return;
            }

            PrewarmEnemyEntries(enemyPool, EnemyEncounterTier.Normal, stageProfile.FloorIndex, enemyPrewarmCount);
            PrewarmEnemyEntries(enemyPool, EnemyEncounterTier.Elite, stageProfile.FloorIndex, enemyPrewarmCount);
            PrewarmEnemyEntries(enemyPool, EnemyEncounterTier.Boss, stageProfile.FloorIndex, enemyPrewarmCount);
        }

        private static void PrewarmEnemyEntries(
            EnemyPoolData enemyPool,
            EnemyEncounterTier encounterTier,
            int floorIndex,
            int enemyPrewarmCount)
        {
            s_enemyEntryBuffer.Clear();
            enemyPool.CollectEntries(encounterTier, floorIndex, s_enemyEntryBuffer);

            for (int index = 0; index < s_enemyEntryBuffer.Count; index++)
            {
                EnemySpawnEntry entry = s_enemyEntryBuffer[index];
                if (entry?.EnemyPrefab == null)
                {
                    continue;
                }

                PrewarmPrefab(entry.EnemyPrefab.gameObject, enemyPrewarmCount);
            }
        }

        private static void PrewarmRewardPools(StageProfile stageProfile, int rewardPickupPrewarmCount)
        {
            if (stageProfile == null || rewardPickupPrewarmCount <= 0)
            {
                return;
            }

            for (int roomTypeIndex = 0; roomTypeIndex < s_rewardPrewarmRoomTypes.Length; roomTypeIndex++)
            {
                RoomRewardTable rewardTable = stageProfile.GetRewardPool(s_rewardPrewarmRoomTypes[roomTypeIndex]);
                if (rewardTable == null)
                {
                    continue;
                }

                IReadOnlyList<RoomRewardEntry> entries = rewardTable.RewardEntries;
                for (int entryIndex = 0; entryIndex < entries.Count; entryIndex++)
                {
                    RoomRewardEntry entry = entries[entryIndex];
                    if (!entry.IsValid)
                    {
                        continue;
                    }

                    PrewarmPrefab(entry.PickupPrefab, Mathf.Max(rewardPickupPrewarmCount, entry.Quantity));
                }
            }
        }
    }
}
