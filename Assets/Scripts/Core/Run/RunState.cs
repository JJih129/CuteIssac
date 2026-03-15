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
        TransitioningFloor = 4,
        Paused = 5,
        Victory = 6,
        Defeat = 7
    }
}
