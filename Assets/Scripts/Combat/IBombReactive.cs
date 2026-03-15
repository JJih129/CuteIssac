namespace CuteIssac.Combat
{
    /// <summary>
    /// Optional bomb interaction hook for secret walls, breakable props, and future room devices.
    /// BombController only depends on this contract, not on concrete wall implementations.
    /// </summary>
    public interface IBombReactive
    {
        void ReactToBomb(in BombExplosionInfo explosionInfo);
    }
}
