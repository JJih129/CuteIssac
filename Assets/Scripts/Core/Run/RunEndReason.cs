namespace CuteIssac.Core.Run
{
    /// <summary>
    /// Simple run termination reasons so UI or analytics systems can react without inferring from state.
    /// </summary>
    public enum RunEndReason
    {
        None = 0,
        Victory = 1,
        Defeat = 2,
        Abandoned = 3
    }
}
