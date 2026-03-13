namespace CuteIssac.Common.Input
{
    /// <summary>
    /// Backend-agnostic contract for gameplay input. Player code should depend on this instead of Input System APIs.
    /// </summary>
    public interface IPlayerInputReader
    {
        PlayerGameplayInputState ReadState();
    }
}
