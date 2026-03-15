using System;
using UnityEngine;

namespace CuteIssac.Common.Stats
{
    /// <summary>
    /// One authored projectile behavior change entry.
    /// These are resolved in PlayerStats and then applied through the projectile spawn request.
    /// </summary>
    [Serializable]
    public struct ProjectileModifier
    {
        [SerializeField] private ProjectileModifierType modifierType;
        [SerializeField] private StatModifierOperation operation;
        [SerializeField] private float value;

        public ProjectileModifier(ProjectileModifierType modifierType, StatModifierOperation operation, float value)
        {
            this.modifierType = modifierType;
            this.operation = operation;
            this.value = value;
        }

        public ProjectileModifierType ModifierType => modifierType;
        public StatModifierOperation Operation => operation;
        public float Value => value;
    }
}
