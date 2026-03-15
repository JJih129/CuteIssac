namespace CuteIssac.Data.Item
{
    /// <summary>
    /// Lightweight semantic tags used by the synergy resolver.
    /// Item effects still live in modifiers, while tags only describe combination hooks.
    /// </summary>
    public enum ItemTag
    {
        SpeedUp = 0,
        DamageUp = 1,
        FireRateUp = 2,
        MultiShot = 3,
        Pierce = 4,
        Homing = 5
    }
}
