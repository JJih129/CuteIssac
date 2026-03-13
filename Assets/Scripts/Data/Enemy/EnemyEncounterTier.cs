namespace CuteIssac.Data.Enemy
{
    /// <summary>
    /// High-level enemy bucket used by floor and room generation.
    /// Keeping this separate from concrete enemy prefabs lets later systems ask for "normal", "elite", or "boss" candidates first.
    /// </summary>
    public enum EnemyEncounterTier
    {
        Normal = 0,
        Elite = 1,
        Boss = 2
    }
}
