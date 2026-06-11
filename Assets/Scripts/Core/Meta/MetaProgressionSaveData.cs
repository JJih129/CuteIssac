using System;
using System.Collections.Generic;

namespace CuteIssac.Core.Meta
{
    [Serializable]
    public sealed class MetaProgressionSaveData
    {
        public int TotalRuns;
        public int TotalWins;
        public int TotalDefeats;
        public int TotalAbandons;
        public int BestFloor;
        public int TotalRoomsCleared;
        public int TotalRoomsResolved;
        public int TotalBossRoomsCleared;
        public int TotalEnemyKills;
        public int CurrentWinStreak;
        public int BestWinStreak;
        public int NoHitCombatRoomClears;
        public int BossClearsWithoutBombs;
        public int TotalCoinsCollected;
        public int TotalKeysCollected;
        public int TotalBombsCollected;
        public float TotalRunSeconds;
        public List<string> DiscoveredItemIds = new();
        public List<string> SeenEnemyIds = new();
        public List<string> CompletedAchievementIds = new();
        public List<AchievementCompletionRecord> CompletedAchievements = new();
        public List<MetaProgressionCounterRecord> EnemyKillCounts = new();
        public List<MetaProgressionCounterRecord> RoomTypeClearCounts = new();
        public List<MetaProgressionCounterRecord> ChallengeClearRankCounts = new();
        public List<MetaProgressionCounterRecord> ChallengePressureTierCounts = new();
        public List<MetaProgressionCounterRecord> SpecialRewardOfferCounts = new();
        public List<MetaProgressionCounterRecord> SpecialDealOfferCounts = new();
        public List<MetaProgressionCounterRecord> SpecialDealPurchaseCounts = new();
        public List<MetaProgressionCounterRecord> SpecialDealDeclineCounts = new();
        public List<CharacterProgressionRecord> CharacterRecords = new();
    }

    [Serializable]
    public sealed class AchievementCompletionRecord
    {
        public string AchievementId;
        public string CompletedAtUtc;
    }

    [Serializable]
    public sealed class MetaProgressionCounterRecord
    {
        public string Id;
        public int Count;
    }

    [Serializable]
    public sealed class CharacterProgressionRecord
    {
        public string CharacterId;
        public int Runs;
        public int Wins;
        public int Defeats;
        public int BestFloor;
        public int BossKills;
        public int EnemyKills;
        public int CurrentWinStreak;
        public int BestWinStreak;
        public float TotalRunSeconds;
        public List<string> ClearMarks = new();
    }
}
