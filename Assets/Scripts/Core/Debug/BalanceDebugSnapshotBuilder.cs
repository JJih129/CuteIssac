using System.Collections.Generic;
using System.Text;
using CuteIssac.Core.Run;
using CuteIssac.Data.Balance;
using CuteIssac.Data.Dungeon;
using CuteIssac.Data.Enemy;
using CuteIssac.Data.Item;
using CuteIssac.Data.Room;
using CuteIssac.Dungeon;
using CuteIssac.Room;
using UnityEngine;

namespace CuteIssac.Core.Debug
{
    internal static class BalanceDebugSnapshotBuilder
    {
        public static string BuildSnapshot(BalanceConfig balanceConfig, RunManager runManager, RoomNavigationController roomNavigationController)
        {
            StringBuilder builder = new();
            int floorIndex = ResolveFloorIndex(runManager);
            RoomController currentRoom = roomNavigationController != null ? roomNavigationController.CurrentRoom : null;

            builder.Append("밸런스 ");
            builder.Append(balanceConfig != null ? balanceConfig.ConfigId : "없음");
            builder.AppendLine();

            if (!TryResolveFloorConfig(balanceConfig, runManager, floorIndex, out FloorConfig floorConfig))
            {
                builder.Append("층 ").Append(floorIndex).Append(": 층 설정 없음");
                return builder.ToString();
            }

            builder.Append("층 ").Append(floorConfig.FloorIndex).Append(" · ").Append(floorConfig.name).AppendLine();

            if (currentRoom != null)
            {
                builder.Append("방 ").Append(currentRoom.RoomId).Append(" · ").Append(currentRoom.State).AppendLine();
            }

            builder.Append("예산 일반/정예/보스 ")
                .Append(floorConfig.NormalRoomEnemyBudget).Append("/")
                .Append(floorConfig.EliteRoomEnemyBudget).Append("/")
                .Append(floorConfig.BossRoomEnemyBudget)
                .Append("  +거리 ").Append(floorConfig.NormalRoomDistanceBudgetBonusPerStep)
                .Append("  x").Append(floorConfig.EncounterBudgetMultiplier.ToString("0.00"))
                .AppendLine();

            AppendEncounterPacingSummary(builder, floorConfig.EncounterPacing);

            builder.Append("생성 방 ").Append(floorConfig.MinNormalRoomCount).Append("-").Append(floorConfig.MaxNormalRoomCount)
                .Append("  갈래 ").Append(floorConfig.StartRoomInitialBranchCount)
                .Append("  추가 ").Append(Mathf.RoundToInt(floorConfig.AdditionalBranchChance * 100f)).Append("%")
                .AppendLine();

            AppendEnemyPoolSummary(builder, floorConfig.EnemyPool, floorConfig.FloorIndex);
            AppendRewardSummary(builder, "보상", floorConfig);
            AppendItemPoolSummary(builder, "아이템", RoomType.Treasure, floorConfig.TreasureRoomItemPool);
            AppendItemPoolSummary(builder, "아이템", RoomType.Shop, floorConfig.ShopRoomItemPool);
            AppendItemPoolSummary(builder, "아이템", RoomType.Boss, floorConfig.BossRewardItemPool);
            AppendItemPoolSummary(builder, "아이템", RoomType.Secret, floorConfig.SecretRoomItemPool);

            if (floorConfig.RoomTheme != null)
            {
                builder.Append("테마 ").Append(floorConfig.RoomTheme.name).AppendLine();
            }

            return builder.ToString().TrimEnd();
        }

        private static int ResolveFloorIndex(RunManager runManager)
        {
            if (runManager != null && runManager.CurrentContext.HasActiveRun)
            {
                return runManager.CurrentContext.CurrentFloorIndex;
            }

            return runManager != null && runManager.Configuration != null
                ? runManager.Configuration.StartingFloorIndex
                : 1;
        }

        private static bool TryResolveFloorConfig(BalanceConfig balanceConfig, RunManager runManager, int floorIndex, out FloorConfig floorConfig)
        {
            floorConfig = null;

            if (runManager != null && runManager.TryGetFloorConfig(floorIndex, out floorConfig))
            {
                return true;
            }

            return balanceConfig != null && balanceConfig.TryGetFloorConfig(floorIndex, out floorConfig);
        }

        private static void AppendEnemyPoolSummary(StringBuilder builder, EnemyPoolData enemyPool, int floorIndex)
        {
            if (enemyPool == null)
            {
                builder.AppendLine("적 풀 없음");
                return;
            }

            builder.Append("적 ").Append(enemyPool.PoolId)
                .Append("  일반:").Append(CountAvailableEntries(enemyPool.NormalEnemies, floorIndex))
                .Append(" 정예:").Append(CountAvailableEntries(enemyPool.EliteEnemies, floorIndex))
                .Append(" 보스:").Append(CountAvailableEntries(enemyPool.BossEnemies, floorIndex))
                .AppendLine();
        }

        private static void AppendEncounterPacingSummary(StringBuilder builder, EncounterPacingSettings pacing)
        {
            if (pacing == null)
            {
                builder.AppendLine("전투 템포 없음");
                return;
            }

            builder.Append("전투 템포 지연 ")
                .Append(pacing.EncounterStartAggroDelay.ToString("0.00"))
                .Append("+").Append(pacing.EncounterStartAggroDelayJitter.ToString("0.00"))
                .Append("  플레이어 ").Append(pacing.MinimumDistanceFromPlayer.ToString("0.0"))
                .Append("  분산 ").Append(pacing.PreferredSpawnSeparation.ToString("0.0"))
                .Append("  첫 공격+ ").Append(pacing.FirstAttackDelayBonus.ToString("0.00"))
                .Append("  예고 x").Append(pacing.TelegraphDurationMultiplier.ToString("0.00"))
                .Append("  샘플 ").Append(pacing.RoomCandidateSamples)
                .AppendLine();
        }

        private static void AppendRewardSummary(StringBuilder builder, string label, FloorConfig floorConfig)
        {
            AppendSingleRewardSummary(builder, label, "일반", floorConfig.NormalRoomRewardPool);
            AppendSingleRewardSummary(builder, label, "보스", floorConfig.BossRoomRewardPool);
            AppendSingleRewardSummary(builder, label, "보물", floorConfig.TreasureRoomRewardPool);
            AppendSingleRewardSummary(builder, label, "상점", floorConfig.ShopRoomRewardPool);
            AppendSingleRewardSummary(builder, label, "비밀", floorConfig.SecretRoomRewardPool);
        }

        private static void AppendSingleRewardSummary(StringBuilder builder, string label, string shortCode, RoomRewardTable rewardTable)
        {
            builder.Append(label).Append(" ").Append(shortCode).Append(" ");

            if (rewardTable == null)
            {
                builder.AppendLine("없음");
                return;
            }

            builder.Append(rewardTable.name)
                .Append("  선택 ").Append(rewardTable.MinimumRewardSelections).Append("-").Append(rewardTable.MaximumRewardSelections)
                .Append("  엔트리 ").Append(CountValidRewardEntries(rewardTable.RewardEntries))
                .Append(rewardTable.AllowDuplicateSelections ? "  중복 허용" : "  중복 없음")
                .AppendLine();
        }

        private static void AppendItemPoolSummary(StringBuilder builder, string label, RoomType roomType, ItemPoolData itemPool)
        {
            builder.Append(label).Append(" ").Append(roomType).Append(" ");

            if (itemPool == null)
            {
                builder.AppendLine("없음");
                return;
            }

            builder.Append(itemPool.PoolId)
                .Append("  엔트리 ").Append(CountValidItemEntries(itemPool.Entries))
                .Append("  중복보유 ").Append(itemPool.OwnedDuplicateWeightMultiplier.ToString("0.00"))
                .Append("  최근중복 ").Append(itemPool.RecentDuplicateWeightMultiplier.ToString("0.00"))
                .Append("  희귀도 ").Append(BuildRaritySummary(itemPool.RarityWeights))
                .AppendLine();
        }

        private static int CountAvailableEntries(IReadOnlyList<EnemySpawnEntry> entries, int floorIndex)
        {
            int count = 0;

            if (entries == null)
            {
                return count;
            }

            for (int index = 0; index < entries.Count; index++)
            {
                EnemySpawnEntry entry = entries[index];

                if (entry != null && entry.IsAvailableForFloor(floorIndex))
                {
                    count++;
                }
            }

            return count;
        }

        private static int CountValidRewardEntries(IReadOnlyList<RoomRewardEntry> entries)
        {
            int count = 0;

            if (entries == null)
            {
                return count;
            }

            for (int index = 0; index < entries.Count; index++)
            {
                if (entries[index].IsValid)
                {
                    count++;
                }
            }

            return count;
        }

        private static int CountValidItemEntries(IReadOnlyList<ItemPoolEntry> entries)
        {
            int count = 0;

            if (entries == null)
            {
                return count;
            }

            for (int index = 0; index < entries.Count; index++)
            {
                if (entries[index].IsValid)
                {
                    count++;
                }
            }

            return count;
        }

        private static string BuildRaritySummary(IReadOnlyList<ItemPoolRarityWeight> rarityWeights)
        {
            if (rarityWeights == null || rarityWeights.Count == 0)
            {
                return "-";
            }

            StringBuilder builder = new();

            for (int index = 0; index < rarityWeights.Count; index++)
            {
                ItemPoolRarityWeight rarityWeight = rarityWeights[index];

                if (index > 0)
                {
                    builder.Append("/");
                }

                builder.Append(ShortenRarity(rarityWeight.Rarity))
                    .Append(":")
                    .Append(rarityWeight.WeightMultiplier.ToString("0.##"));
            }

            return builder.ToString();
        }

        private static string ShortenRarity(ItemRarity rarity)
        {
            return rarity switch
            {
                ItemRarity.Common => "일반",
                ItemRarity.Uncommon => "고급",
                ItemRarity.Rare => "희귀",
                ItemRarity.Legendary => "전설",
                ItemRarity.Relic => "유물",
                ItemRarity.Boss => "보스",
                _ => rarity.ToString()
            };
        }
    }
}
