namespace CuteIssac.Dungeon
{
    /// <summary>
    /// Primary exploration state for one room on the minimap.
    /// Cleared is kept separate from visited so the UI can emphasize rooms already finished in combat.
    /// </summary>
    public enum RoomExplorationState
    {
        Unknown = 0,
        Visited = 1,
        Current = 2,
        Cleared = 3
    }
}
