namespace CuteIssac.Room
{
    /// <summary>
    /// Runtime room lifecycle states.
    /// These stay coarse enough for current gameplay while separating entry, combat resolution, and reward completion.
    /// </summary>
    public enum RoomState
    {
        Idle = 0,
        Entered = 1,
        Combat = 2,
        Resolved = 3,
        Rewarded = 4
    }
}
