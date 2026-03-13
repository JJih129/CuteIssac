namespace CuteIssac.Player
{
    /// <summary>
    /// Optional extension point for future stat systems. Implement this when move speed should come from runtime stats.
    /// </summary>
    public interface IPlayerMoveSpeedProvider
    {
        float CurrentMoveSpeed { get; }
    }
}
