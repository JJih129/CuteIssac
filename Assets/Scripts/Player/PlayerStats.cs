using System;
using System.Collections.Generic;
using CuteIssac.Common.Stats;
using CuteIssac.Data.Item;
using UnityEngine;

namespace CuteIssac.Player
{
    /// <summary>
    /// Calculates final player stats from base combat/movement data plus passive item modifiers.
    /// Gameplay systems read from this component so item effects remain centralized and deterministic.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerStats : MonoBehaviour, IPlayerMoveSpeedProvider
    {
        [Header("Base Sources")]
        [SerializeField] private PlayerMovement playerMovement;
        [SerializeField] private PlayerCombat playerCombat;

        [Header("Fallback Base Stats")]
        [SerializeField] [Min(0f)] private float fallbackMoveSpeed = 5f;
        [SerializeField] [Min(0f)] private float fallbackDamage = 3f;
        [SerializeField] [Min(0.01f)] private float fallbackFireInterval = 0.3f;

        public event Action<PlayerStatSnapshot> StatsRecalculated;

        public PlayerStatSnapshot CurrentStats { get; private set; }
        public float CurrentMoveSpeed => CurrentStats.MoveSpeed;
        public float CurrentDamage => CurrentStats.Damage;
        public float CurrentFireInterval => CurrentStats.FireInterval;

        private void Awake()
        {
            ResolveDependencies();
            Recalculate(null);
        }

        [ContextMenu("Recalculate Base Stats")]
        public void RecalculateBaseStats()
        {
            Recalculate(null);
        }

        /// <summary>
        /// Rebuilds the final stat snapshot from authored base values and currently owned passive items.
        /// This is called by the item manager whenever inventory changes.
        /// </summary>
        public void Recalculate(IReadOnlyList<ItemData> passiveItems)
        {
            ResolveDependencies();

            PlayerStatSnapshot baseStats = ResolveBaseStats();
            CurrentStats = ApplyModifiers(baseStats, passiveItems);
            StatsRecalculated?.Invoke(CurrentStats);
        }

        private void ResolveDependencies()
        {
            if (playerMovement == null)
            {
                playerMovement = GetComponent<PlayerMovement>();
            }

            if (playerCombat == null)
            {
                playerCombat = GetComponent<PlayerCombat>();
            }
        }

        private PlayerStatSnapshot ResolveBaseStats()
        {
            float moveSpeed = playerMovement != null ? playerMovement.BaseMoveSpeed : fallbackMoveSpeed;
            float damage = fallbackDamage;
            float fireInterval = fallbackFireInterval;

            if (playerCombat != null && playerCombat.AttackDefinition != null)
            {
                fireInterval = playerCombat.AttackDefinition.FireInterval;

                if (playerCombat.AttackDefinition.ProjectileDefinition != null)
                {
                    damage = playerCombat.AttackDefinition.ProjectileDefinition.Damage;
                }
            }

            return new PlayerStatSnapshot(moveSpeed, damage, fireInterval);
        }

        private static PlayerStatSnapshot ApplyModifiers(PlayerStatSnapshot baseStats, IReadOnlyList<ItemData> passiveItems)
        {
            StatAccumulator damage = StatAccumulator.Create();
            StatAccumulator moveSpeed = StatAccumulator.Create();
            StatAccumulator fireInterval = StatAccumulator.Create();

            if (passiveItems != null)
            {
                for (int itemIndex = 0; itemIndex < passiveItems.Count; itemIndex++)
                {
                    ItemData itemData = passiveItems[itemIndex];

                    if (itemData == null)
                    {
                        continue;
                    }

                    IReadOnlyList<StatModifier> modifiers = itemData.StatModifiers;

                    for (int modifierIndex = 0; modifierIndex < modifiers.Count; modifierIndex++)
                    {
                        ApplyModifier(modifiers[modifierIndex], ref damage, ref moveSpeed, ref fireInterval);
                    }
                }
            }

            return new PlayerStatSnapshot(
                moveSpeed.Apply(baseStats.MoveSpeed, 0f),
                damage.Apply(baseStats.Damage, 0f),
                fireInterval.Apply(baseStats.FireInterval, 0.01f));
        }

        private static void ApplyModifier(
            StatModifier modifier,
            ref StatAccumulator damage,
            ref StatAccumulator moveSpeed,
            ref StatAccumulator fireInterval)
        {
            switch (modifier.StatType)
            {
                case PlayerStatType.Damage:
                    damage.Apply(modifier);
                    break;
                case PlayerStatType.MoveSpeed:
                    moveSpeed.Apply(modifier);
                    break;
                case PlayerStatType.FireInterval:
                    fireInterval.Apply(modifier);
                    break;
            }
        }

        /// <summary>
        /// Small reusable accumulator so stat recomputation stays order-stable and allocation-free.
        /// </summary>
        private struct StatAccumulator
        {
            private float _additive;
            private float _multiplier;
            private bool _hasOverride;
            private float _overrideValue;

            public static StatAccumulator Create()
            {
                return new StatAccumulator
                {
                    _multiplier = 1f
                };
            }

            public void Apply(StatModifier modifier)
            {
                switch (modifier.Operation)
                {
                    case StatModifierOperation.Add:
                        _additive += modifier.Value;
                        break;
                    case StatModifierOperation.Multiply:
                        _multiplier *= modifier.Value;
                        break;
                    case StatModifierOperation.Override:
                        _hasOverride = true;
                        _overrideValue = modifier.Value;
                        break;
                }
            }

            public float Apply(float baseValue, float minimumValue)
            {
                float resolved = _hasOverride
                    ? _overrideValue
                    : (baseValue + _additive) * _multiplier;

                return Mathf.Max(minimumValue, resolved);
            }
        }
    }
}
