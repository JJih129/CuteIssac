namespace CuteIssac.Common.Stats
{
    /// <summary>
    /// Generic modifier operations used by passive item effects.
    /// Add and Multiply cover most early Isaac-like stat items, while Override keeps room for special items later.
    /// </summary>
    public enum StatModifierOperation
    {
        Add = 0,
        Multiply = 1,
        Override = 2
    }
}
