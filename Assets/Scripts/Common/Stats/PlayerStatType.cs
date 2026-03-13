namespace CuteIssac.Common.Stats
{
    /// <summary>
    /// Supported player-facing stats for the first passive item pass.
    /// New item effects should expand this enum instead of adding item-specific conditionals in gameplay code.
    /// </summary>
    public enum PlayerStatType
    {
        Damage = 0,
        MoveSpeed = 1,
        FireInterval = 2
    }
}
