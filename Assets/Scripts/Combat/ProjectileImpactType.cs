namespace CuteIssac.Combat
{
    /// <summary>
    /// Distinguishes what kind of collision ended the projectile so presentation can react differently.
    /// </summary>
    public enum ProjectileImpactType
    {
        None = 0,
        Damageable = 1,
        Solid = 2
    }
}
