namespace CuteIssac.Data.Unlock
{
    /// <summary>
    /// High-level trigger types for persistent meta unlocks.
    /// </summary>
    public enum UnlockConditionType
    {
        BossKill = 0,
        ReachFloor = 1,
        AcquireItem = 2,
        CumulativeEnemyKillCount = 3,
        CharacterClearMark = 4,
        ItemDiscovered = 5,
        RoomTypeClearCount = 6,
        TotalRuns = 7,
        TotalWins = 8,
        TotalEnemyKills = 9,
        CurrentWinStreak = 10,
        BestWinStreak = 11,
        AchievementCompleted = 12
    }
}
