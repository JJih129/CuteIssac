using System;

namespace CuteIssac.Combat
{
    [Flags]
    public enum ProjectileTraitFlags
    {
        None = 0,
        Explosive = 1 << 0,
        Laser = 1 << 1,
        Split = 1 << 2,
        Bounce = 1 << 3,
        Orbit = 1 << 4,
        Shield = 1 << 5,
        Lifesteal = 1 << 6
    }
}
