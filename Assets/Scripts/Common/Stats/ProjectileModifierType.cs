namespace CuteIssac.Common.Stats
{
    /// <summary>
    /// Projectile-specific authored values that passive items can modify without branching in combat code.
    /// </summary>
    public enum ProjectileModifierType
    {
        Speed = 0,
        Lifetime = 1,
        Scale = 2,
        MultiShot = 3,
        Pierce = 4,
        Homing = 5,
        Explode = 6,
        Laser = 7,
        Split = 8,
        Bounce = 9,
        Orbit = 10,
        Shield = 11,
        Lifesteal = 12
    }
}
