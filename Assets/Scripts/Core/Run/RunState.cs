namespace CuteIssac.Core.Run
{
    /// <summary>
    /// Minimal run lifecycle states for the prototype entry flow.
    /// </summary>
    public enum RunState
    {
        Idle = 0,
        FrontEnd = 1,
        StartingRun = 2,
        InRun = 3,
        Paused = 4,
        Victory = 5,
        Defeat = 6
    }
}
