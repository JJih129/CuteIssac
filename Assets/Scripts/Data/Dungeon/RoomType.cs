namespace CuteIssac.Data.Dungeon
{
    /// <summary>
    /// High-level gameplay classification for a room.
    /// Generation rules and reward logic will branch on this instead of hardcoding room ids.
    /// </summary>
    public enum RoomType
    {
        Start = 0,
        Normal = 1,
        Treasure = 2,
        Shop = 3,
        Boss = 4,
        Secret = 5,
        Challenge = 6
    }
}
