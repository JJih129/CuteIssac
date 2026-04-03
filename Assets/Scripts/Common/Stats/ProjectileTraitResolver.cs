using CuteIssac.Combat;

namespace CuteIssac.Common.Stats
{
    public static class ProjectileTraitResolver
    {
        public static void Apply(ProjectileModifier modifier, ref ProjectileTraitState traits)
        {
            switch (modifier.ModifierType)
            {
                case ProjectileModifierType.Explode:
                    traits.Flags |= ProjectileTraitFlags.Explosive;
                    traits.ExplosionStrength = ResolveStrength(traits.ExplosionStrength, modifier, 1f);
                    break;
                case ProjectileModifierType.Laser:
                    traits.Flags |= ProjectileTraitFlags.Laser;
                    traits.LaserStrength = ResolveStrength(traits.LaserStrength, modifier, 1f);
                    break;
                case ProjectileModifierType.Split:
                    traits.Flags |= ProjectileTraitFlags.Split;
                    traits.SplitStrength = ResolveStrength(traits.SplitStrength, modifier, 1f);
                    break;
                case ProjectileModifierType.Bounce:
                    traits.Flags |= ProjectileTraitFlags.Bounce;
                    traits.BounceStrength = ResolveStrength(traits.BounceStrength, modifier, 1f);
                    break;
                case ProjectileModifierType.Orbit:
                    traits.Flags |= ProjectileTraitFlags.Orbit;
                    traits.OrbitStrength = ResolveStrength(traits.OrbitStrength, modifier, 1f);
                    break;
                case ProjectileModifierType.Shield:
                    traits.Flags |= ProjectileTraitFlags.Shield;
                    traits.ShieldStrength = ResolveStrength(traits.ShieldStrength, modifier, 1f);
                    break;
                case ProjectileModifierType.Lifesteal:
                    traits.Flags |= ProjectileTraitFlags.Lifesteal;
                    traits.LifestealStrength = ResolveStrength(traits.LifestealStrength, modifier, 1f);
                    break;
            }
        }

        private static float ResolveStrength(float currentStrength, ProjectileModifier modifier, float defaultValue)
        {
            float value = UnityEngine.Mathf.Approximately(modifier.Value, 0f)
                ? defaultValue
                : modifier.Value;

            return modifier.Operation switch
            {
                StatModifierOperation.Add => currentStrength + value,
                StatModifierOperation.Multiply => currentStrength <= 0f ? value : currentStrength * value,
                StatModifierOperation.Override => value,
                _ => currentStrength
            };
        }
    }
}
