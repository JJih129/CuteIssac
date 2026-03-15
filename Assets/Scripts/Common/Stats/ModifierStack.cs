using System.Collections.Generic;

namespace CuteIssac.Common.Stats
{
    /// <summary>
    /// Reusable container for grouped stat and projectile modifiers.
    /// Synergies and future authored effect bundles can build modifier output without coupling to PlayerStats internals.
    /// </summary>
    public sealed class ModifierStack
    {
        private readonly List<StatModifier> _statModifiers = new();
        private readonly List<ProjectileModifier> _projectileModifiers = new();

        public IReadOnlyList<StatModifier> StatModifiers => _statModifiers;
        public IReadOnlyList<ProjectileModifier> ProjectileModifiers => _projectileModifiers;

        public void Clear()
        {
            _statModifiers.Clear();
            _projectileModifiers.Clear();
        }

        public void Add(StatModifier modifier)
        {
            _statModifiers.Add(modifier);
        }

        public void Add(ProjectileModifier modifier)
        {
            _projectileModifiers.Add(modifier);
        }

        public void AddRange(ModifierStack other)
        {
            if (other == null)
            {
                return;
            }

            AddRange(other.StatModifiers);
            AddRange(other.ProjectileModifiers);
        }

        public void AddRange(IReadOnlyList<StatModifier> statModifiers)
        {
            if (statModifiers == null)
            {
                return;
            }

            for (int index = 0; index < statModifiers.Count; index++)
            {
                _statModifiers.Add(statModifiers[index]);
            }
        }

        public void AddRange(IReadOnlyList<ProjectileModifier> projectileModifiers)
        {
            if (projectileModifiers == null)
            {
                return;
            }

            for (int index = 0; index < projectileModifiers.Count; index++)
            {
                _projectileModifiers.Add(projectileModifiers[index]);
            }
        }
    }
}
