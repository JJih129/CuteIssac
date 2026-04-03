using System;

namespace CuteIssac.Combat
{
    [Serializable]
    public struct ProjectileTraitState
    {
        public ProjectileTraitFlags Flags;
        public float ExplosionStrength;
        public float LaserStrength;
        public float SplitStrength;
        public float BounceStrength;
        public float OrbitStrength;
        public float ShieldStrength;
        public float LifestealStrength;

        public bool Has(ProjectileTraitFlags flag)
        {
            return (Flags & flag) != 0;
        }

        public bool IsExplosive => Has(ProjectileTraitFlags.Explosive) && ExplosionStrength > 0.01f;
        public bool IsLaser => Has(ProjectileTraitFlags.Laser) && LaserStrength > 0.01f;
        public bool IsSplit => Has(ProjectileTraitFlags.Split) && SplitStrength > 0.01f;
        public bool IsOrbiting => Has(ProjectileTraitFlags.Orbit) && OrbitStrength > 0.01f;
        public bool IsShielded => Has(ProjectileTraitFlags.Shield) && ShieldStrength > 0.01f;

        public static ProjectileTraitState Default => default;
    }
}
