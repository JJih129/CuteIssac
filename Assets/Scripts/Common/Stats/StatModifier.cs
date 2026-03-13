using System;
using UnityEngine;

namespace CuteIssac.Common.Stats
{
    /// <summary>
    /// One authored stat change entry.
    /// Passive items expose a list of these so the stat system can stay data-driven instead of branching on item IDs.
    /// </summary>
    [Serializable]
    public struct StatModifier
    {
        [SerializeField] private PlayerStatType statType;
        [SerializeField] private StatModifierOperation operation;
        [SerializeField] private float value;

        public PlayerStatType StatType => statType;
        public StatModifierOperation Operation => operation;
        public float Value => value;
    }
}
