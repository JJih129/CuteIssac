using System;

namespace CuteIssac.Core.Run
{
    /// <summary>
    /// Lightweight presentation-ready run summary.
    /// UI can render this without depending on gameplay components directly.
    /// </summary>
    [Serializable]
    public sealed class RunResultSummary
    {
        public RunResultSummary(
            string title,
            string subtitle,
            int collectedItemCount,
            int clearedRoomCount,
            int resolvedRoomCount,
            int coins,
            int keys,
            int bombs,
            int enemyKillCount,
            int reachedFloor,
            RunEndReason endReason,
            bool isHardMode = false,
            string characterName = "",
            float runSeconds = 0f,
            int totalRuns = 0,
            int totalWins = 0,
            int totalDefeats = 0,
            int bestFloor = 0,
            int currentWinStreak = 0,
            int bestWinStreak = 0,
            string newUnlocksText = "",
            string newClearMarksText = "",
            string acquiredItemsText = "",
            string newAchievementsText = "",
            string challengeSummaryText = "")
        {
            Title = title ?? string.Empty;
            Subtitle = subtitle ?? string.Empty;
            CollectedItemCount = Math.Max(0, collectedItemCount);
            ClearedRoomCount = Math.Max(0, clearedRoomCount);
            ResolvedRoomCount = Math.Max(0, resolvedRoomCount);
            Coins = Math.Max(0, coins);
            Keys = Math.Max(0, keys);
            Bombs = Math.Max(0, bombs);
            EnemyKillCount = Math.Max(0, enemyKillCount);
            ReachedFloor = Math.Max(1, reachedFloor);
            EndReason = endReason;
            IsHardMode = isHardMode;
            CharacterName = characterName ?? string.Empty;
            RunSeconds = Math.Max(0f, runSeconds);
            TotalRuns = Math.Max(0, totalRuns);
            TotalWins = Math.Max(0, totalWins);
            TotalDefeats = Math.Max(0, totalDefeats);
            BestFloor = Math.Max(0, bestFloor);
            CurrentWinStreak = Math.Max(0, currentWinStreak);
            BestWinStreak = Math.Max(CurrentWinStreak, bestWinStreak);
            NewUnlocksText = newUnlocksText ?? string.Empty;
            NewClearMarksText = newClearMarksText ?? string.Empty;
            AcquiredItemsText = acquiredItemsText ?? string.Empty;
            NewAchievementsText = newAchievementsText ?? string.Empty;
            ChallengeSummaryText = challengeSummaryText ?? string.Empty;
        }

        public string Title { get; }
        public string Subtitle { get; }
        public int CollectedItemCount { get; }
        public int ClearedRoomCount { get; }
        public int ResolvedRoomCount { get; }
        public int Coins { get; }
        public int Keys { get; }
        public int Bombs { get; }
        public int EnemyKillCount { get; }
        public int ReachedFloor { get; }
        public RunEndReason EndReason { get; }
        public bool IsHardMode { get; }
        public string CharacterName { get; }
        public float RunSeconds { get; }
        public int TotalRuns { get; }
        public int TotalWins { get; }
        public int TotalDefeats { get; }
        public int BestFloor { get; }
        public int CurrentWinStreak { get; }
        public int BestWinStreak { get; }
        public string NewUnlocksText { get; }
        public string NewClearMarksText { get; }
        public string AcquiredItemsText { get; }
        public string NewAchievementsText { get; }
        public string ChallengeSummaryText { get; }
    }
}
