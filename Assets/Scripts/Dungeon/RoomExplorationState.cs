namespace CuteIssac.Dungeon
{
    /// <summary>
    /// Primary exploration state for one room on the minimap.
    /// Higher numeric values always mean a stronger reveal state so promotion logic stays deterministic.
    /// </summary>
    public enum RoomExplorationState
    {
        Hidden = 0,
        Discovered = 1,
        Visited = 2,
        Cleared = 3,
        Current = 4
    }
}
