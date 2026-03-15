namespace CuteIssac.Core.Run
{
    /// <summary>
    /// Defines how the current unresolved room should be restored when resuming a saved run.
    /// </summary>
    public enum RunResumeCurrentRoomPolicy
    {
        RestartEncounter = 0,
        ReturnToStartRoom = 1,
        ResumeAtSavedPosition = 2
    }
}
