namespace CuteIssac.Data.Room
{
    /// <summary>
    /// High-level reward categories used by room reward tables.
    /// This stays descriptive so designers can understand the intent of each pickup entry in data.
    /// </summary>
    public enum RoomRewardType
    {
        Coin = 0,
        Key = 1,
        Bomb = 2,
        Heart = 3,
        PassiveItem = 4
    }
}
