namespace CuteIssac.Room
{
    /// <summary>
    /// Defines how a hidden secret-room entrance becomes available.
    /// Only BombOnly is implemented now; other modes remain explicit extension points.
    /// </summary>
    public enum SecretDoorRevealMode
    {
        BombOnly = 0,
        ExternalCondition = 1,
        StartsRevealed = 2
    }
}
