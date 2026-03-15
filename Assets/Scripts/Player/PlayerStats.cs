using System;
using System.Collections.Generic;
using CuteIssac.Common.Stats;
using CuteIssac.Data.Combat;
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
        [SerializeField] private ItemSynergyCatalog synergyCatalog;

        [Header("Fallback Base Stats")]
        [SerializeField] [Min(0f)] private float fallbackMoveSpeed = 5f;
        [SerializeField] [Min(0f)] private float fallbackDamage = 3f;
        [SerializeField] [Min(0.01f)] private float fallbackFireInterval = 0.3f;
        [SerializeField] [Min(0f)] private float fallbackProjectileSpeed = 12f;
        [SerializeField] [Min(0.5f)] private float fallbackRange = 18f;
        [SerializeField] [Min(0.05f)] private float fallbackProjectileScale = 1f;
        [SerializeField] private float fallbackLuck;
        [SerializeField] [Min(1f)] private float fallbackProjectileCount = 1f;
        [SerializeField] [Min(0f)] private float fallbackKnockback = 2f;
        [SerializeField] [Min(0f)] private float fallbackProjectilePierce;
        [SerializeField] [Min(0f)] private float fallbackHomingStrength;
        [SerializeField] [Min(1f)] private float fallbackMaxHealth = 6f;

        [Header("Fire Rate Conversion")]
        [SerializeField] [Min(0.1f)] private float fireIntervalModifierToShotsPerSecondScale = 2.4f;
        [SerializeField] [Min(0.1f)] private float minimumShotsPerSecond = 1f;
        [SerializeField] [Min(0.5f)] private float maximumShotsPerSecond = 8f;

        public event Action<PlayerStatSnapshot> StatsRecalculated;

        public PlayerStatSnapshot CurrentStats { get; private set; }
        public float CurrentMaxHealth => CurrentStats.MaxHealth;
        public float CurrentMoveSpeed => CurrentStats.MoveSpeed;
        public float CurrentDamage => CurrentStats.Damage;
        public float CurrentFireInterval => CurrentStats.FireInterval;
        public float CurrentProjectileSpeed => CurrentStats.ProjectileSpeed;
        public float CurrentRange => CurrentStats.Range;
        public float CurrentProjectileLifetime => CurrentStats.ProjectileLifetime;
        public float CurrentProjectileScale => CurrentStats.ProjectileScale;
        public float CurrentLuck => CurrentStats.Luck;
        public float CurrentProjectileCount => CurrentStats.ProjectileCount;
        public float CurrentKnockback => CurrentStats.Knockback;
        public float CurrentProjectilePierce => CurrentStats.ProjectilePierce;
        public float CurrentHomingStrength => CurrentStats.HomingStrength;
        public IReadOnlyList<string> ActiveSynergyNames => _activeSynergyNames;

        private readonly List<ItemData> _cachedPassiveItems = new();
        private readonly List<StatModifier> _runtimeConsumableStatModifiers = new();
        private readonly List<ProjectileModifier> _runtimeConsumableProjectileModifiers = new();
        private readonly List<StatModifier> _runtimeStartingBuildStatModifiers = new();
        private readonly List<ProjectileModifier> _runtimeStartingBuildProjectileModifiers = new();
        private readonly List<StatModifier> _runtimeActiveItemStatModifiers = new();
        private readonly List<ProjectileModifier> _runtimeActiveItemProjectileModifiers = new();
        private readonly List<StatModifier> _runtimeEventStatModifiers = new();
        private readonly List<ProjectileModifier> _runtimeEventProjectileModifiers = new();
        private readonly List<ItemSynergyDefinition> _activeSynergies = new();
        private readonly ModifierStack _resolvedSynergyModifierStack = new();
        private readonly List<string> _activeSynergyNames = new();
        private readonly ModifierStack _resolvedItemModifierStack = new();

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

        [ContextMenu("Log Current Stats")]
        public void LogCurrentStats()
        {
            string synergySummary = _activeSynergyNames.Count > 0
                ? string.Join(", ", _activeSynergyNames)
                : "none";

            Debug.Log(
                $"PlayerStats dmg={CurrentDamage:0.##} fireInt={CurrentFireInterval:0.###} move={CurrentMoveSpeed:0.##} " +
                $"projSpeed={CurrentProjectileSpeed:0.##} range={CurrentRange:0.##} luck={CurrentLuck:0.##} " +
                $"projCount={CurrentProjectileCount:0.##} knockback={CurrentKnockback:0.##} " +
                $"pierce={CurrentProjectilePierce:0.##} homing={CurrentHomingStrength:0.##} synergies={synergySummary}",
                this);
        }

        /// <summary>
        /// Rebuilds the final stat snapshot from authored base values and currently owned passive items.
        /// This is called by the item manager whenever inventory changes.
        /// </summary>
        public void Recalculate(IReadOnlyList<ItemData> passiveItems)
        {
            CachePassiveItems(passiveItems);
            RebuildCurrentStats();
        }

        /// <summary>
        /// Applies temporary runtime modifiers from consumables without mixing those items into passive inventory ownership.
        /// </summary>
        public void SetConsumableRuntimeModifiers(
            IReadOnlyList<StatModifier> statModifiers,
            IReadOnlyList<ProjectileModifier> projectileModifiers)
        {
            _runtimeConsumableStatModifiers.Clear();
            _runtimeConsumableProjectileModifiers.Clear();

            CopyStatModifiers(statModifiers, _runtimeConsumableStatModifiers);
            CopyProjectileModifiers(projectileModifiers, _runtimeConsumableProjectileModifiers);
            RebuildCurrentStats();
        }

        public void SetStartingBuildModifiers(
            IReadOnlyList<StatModifier> statModifiers,
            IReadOnlyList<ProjectileModifier> projectileModifiers)
        {
            _runtimeStartingBuildStatModifiers.Clear();
            _runtimeStartingBuildProjectileModifiers.Clear();

            CopyStatModifiers(statModifiers, _runtimeStartingBuildStatModifiers);
            CopyProjectileModifiers(projectileModifiers, _runtimeStartingBuildProjectileModifiers);
            RebuildCurrentStats();
        }

        public void SetActiveItemRuntimeModifiers(
            IReadOnlyList<StatModifier> statModifiers,
            IReadOnlyList<ProjectileModifier> projectileModifiers)
        {
            _runtimeActiveItemStatModifiers.Clear();
            _runtimeActiveItemProjectileModifiers.Clear();

            CopyStatModifiers(statModifiers, _runtimeActiveItemStatModifiers);
            CopyProjectileModifiers(projectileModifiers, _runtimeActiveItemProjectileModifiers);
            RebuildCurrentStats();
        }

        public void SetEventRuntimeModifiers(
            IReadOnlyList<StatModifier> statModifiers,
            IReadOnlyList<ProjectileModifier> projectileModifiers)
        {
            _runtimeEventStatModifiers.Clear();
            _runtimeEventProjectileModifiers.Clear();

            CopyStatModifiers(statModifiers, _runtimeEventStatModifiers);
            CopyProjectileModifiers(projectileModifiers, _runtimeEventProjectileModifiers);
            RebuildCurrentStats();
        }

        private void RebuildCurrentStats()
        {
            ResolveDependencies();
            ResolveSynergies();
            PlayerStatSnapshot baseStats = ResolveBaseStats();
            CurrentStats = ApplyModifiers(
                baseStats,
                _cachedPassiveItems,
                _resolvedSynergyModifierStack.StatModifiers,
                _resolvedSynergyModifierStack.ProjectileModifiers,
                _runtimeStartingBuildStatModifiers,
                _runtimeStartingBuildProjectileModifiers,
                _runtimeConsumableStatModifiers,
                _runtimeConsumableProjectileModifiers,
                _runtimeActiveItemStatModifiers,
                _runtimeActiveItemProjectileModifiers,
                _runtimeEventStatModifiers,
                _runtimeEventProjectileModifiers);
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
            PlayerHealth playerHealth = GetComponent<PlayerHealth>();
            float moveSpeed = playerMovement != null ? playerMovement.BaseMoveSpeed : fallbackMoveSpeed;
            float damage = fallbackDamage;
            float fireInterval = fallbackFireInterval;
            float projectileSpeed = fallbackProjectileSpeed;
            float range = fallbackRange;
            float projectileScale = fallbackProjectileScale;
            float luck = fallbackLuck;
            float projectileCount = fallbackProjectileCount;
            float knockback = fallbackKnockback;
            float projectilePierce = fallbackProjectilePierce;
            float homingStrength = fallbackHomingStrength;
            float maxHealth = playerHealth != null ? playerHealth.BaseMaxHealth : fallbackMaxHealth;

            if (playerCombat != null && playerCombat.AttackDefinition != null)
            {
                fireInterval = playerCombat.AttackDefinition.FireInterval;

                if (playerCombat.AttackDefinition.ProjectileDefinition != null)
                {
                    ProjectileDefinition projectileDefinition = playerCombat.AttackDefinition.ProjectileDefinition;
                    damage = projectileDefinition.Damage;
                    projectileSpeed = projectileDefinition.Speed;
                    range = projectileDefinition.Speed * projectileDefinition.Lifetime;
                    projectileScale = projectileDefinition.Scale;
                }
            }

            float projectileLifetime = ResolveProjectileLifetime(projectileSpeed, range);

            return new PlayerStatSnapshot(
                maxHealth,
                moveSpeed,
                damage,
                fireInterval,
                projectileSpeed,
                range,
                projectileLifetime,
                projectileScale,
                luck,
                projectileCount,
                knockback,
                projectilePierce,
                homingStrength);
        }

        private PlayerStatSnapshot ApplyModifiers(
            PlayerStatSnapshot baseStats,
            IReadOnlyList<ItemData> passiveItems,
            IReadOnlyList<StatModifier> synergyStatModifiers,
            IReadOnlyList<ProjectileModifier> synergyProjectileModifiers,
            IReadOnlyList<StatModifier> runtimeStartingBuildStatModifiers,
            IReadOnlyList<ProjectileModifier> runtimeStartingBuildProjectileModifiers,
            IReadOnlyList<StatModifier> runtimeStatModifiers,
            IReadOnlyList<ProjectileModifier> runtimeProjectileModifiers,
            IReadOnlyList<StatModifier> runtimeActiveItemStatModifiers,
            IReadOnlyList<ProjectileModifier> runtimeActiveItemProjectileModifiers,
            IReadOnlyList<StatModifier> runtimeEventStatModifiers,
            IReadOnlyList<ProjectileModifier> runtimeEventProjectileModifiers)
        {
            StatAccumulator damage = StatAccumulator.Create();
            StatAccumulator moveSpeed = StatAccumulator.Create();
            FireRateAccumulator fireRate = FireRateAccumulator.Create();
            StatAccumulator projectileSpeed = StatAccumulator.Create();
            StatAccumulator range = StatAccumulator.Create();
            StatAccumulator projectileLifetime = StatAccumulator.Create();
            StatAccumulator projectileScale = StatAccumulator.Create();
            StatAccumulator luck = StatAccumulator.Create();
            StatAccumulator projectileCount = StatAccumulator.Create();
            StatAccumulator knockback = StatAccumulator.Create();
            StatAccumulator projectilePierce = StatAccumulator.Create();
            StatAccumulator homingStrength = StatAccumulator.Create();
            StatAccumulator maxHealth = StatAccumulator.Create();

            if (passiveItems != null)
            {
                for (int itemIndex = 0; itemIndex < passiveItems.Count; itemIndex++)
                {
                    ItemData itemData = passiveItems[itemIndex];

                    if (itemData == null)
                    {
                        continue;
                    }

                    itemData.BuildModifierStack(_resolvedItemModifierStack);
                    IReadOnlyList<StatModifier> modifiers = _resolvedItemModifierStack.StatModifiers;
                    IReadOnlyList<ProjectileModifier> projectileModifiers = _resolvedItemModifierStack.ProjectileModifiers;

                    for (int modifierIndex = 0; modifierIndex < modifiers.Count; modifierIndex++)
                    {
                        ApplyModifier(
                            modifiers[modifierIndex],
                            ref damage,
                            ref moveSpeed,
                            ref fireRate,
                            ref projectileSpeed,
                            ref range,
                            ref luck,
                            ref projectileCount,
                            ref knockback,
                            ref maxHealth);
                    }

                    for (int modifierIndex = 0; modifierIndex < projectileModifiers.Count; modifierIndex++)
                    {
                        ApplyProjectileModifier(
                            projectileModifiers[modifierIndex],
                            ref projectileSpeed,
                            ref range,
                            ref projectileLifetime,
                            ref projectileScale,
                            ref projectileCount,
                            ref projectilePierce,
                            ref homingStrength);
                    }
                }
            }

            if (runtimeStartingBuildStatModifiers != null)
            {
                for (int modifierIndex = 0; modifierIndex < runtimeStartingBuildStatModifiers.Count; modifierIndex++)
                {
                    ApplyModifier(
                        runtimeStartingBuildStatModifiers[modifierIndex],
                        ref damage,
                        ref moveSpeed,
                        ref fireRate,
                        ref projectileSpeed,
                        ref range,
                        ref luck,
                        ref projectileCount,
                        ref knockback,
                        ref maxHealth);
                }
            }

            if (runtimeStartingBuildProjectileModifiers != null)
            {
                for (int modifierIndex = 0; modifierIndex < runtimeStartingBuildProjectileModifiers.Count; modifierIndex++)
                {
                    ApplyProjectileModifier(
                        runtimeStartingBuildProjectileModifiers[modifierIndex],
                        ref projectileSpeed,
                        ref range,
                        ref projectileLifetime,
                        ref projectileScale,
                        ref projectileCount,
                        ref projectilePierce,
                        ref homingStrength);
                }
            }

            if (runtimeStatModifiers != null)
            {
                for (int modifierIndex = 0; modifierIndex < runtimeStatModifiers.Count; modifierIndex++)
                {
                    ApplyModifier(
                        runtimeStatModifiers[modifierIndex],
                        ref damage,
                        ref moveSpeed,
                        ref fireRate,
                        ref projectileSpeed,
                        ref range,
                        ref luck,
                        ref projectileCount,
                        ref knockback,
                        ref maxHealth);
                }
            }

            if (synergyStatModifiers != null)
            {
                for (int modifierIndex = 0; modifierIndex < synergyStatModifiers.Count; modifierIndex++)
                {
                    ApplyModifier(
                        synergyStatModifiers[modifierIndex],
                        ref damage,
                        ref moveSpeed,
                        ref fireRate,
                        ref projectileSpeed,
                        ref range,
                        ref luck,
                        ref projectileCount,
                        ref knockback,
                        ref maxHealth);
                }
            }

            if (runtimeProjectileModifiers != null)
            {
                for (int modifierIndex = 0; modifierIndex < runtimeProjectileModifiers.Count; modifierIndex++)
                {
                    ApplyProjectileModifier(
                        runtimeProjectileModifiers[modifierIndex],
                        ref projectileSpeed,
                        ref range,
                        ref projectileLifetime,
                        ref projectileScale,
                        ref projectileCount,
                        ref projectilePierce,
                        ref homingStrength);
                }
            }

            if (synergyProjectileModifiers != null)
            {
                for (int modifierIndex = 0; modifierIndex < synergyProjectileModifiers.Count; modifierIndex++)
                {
                    ApplyProjectileModifier(
                        synergyProjectileModifiers[modifierIndex],
                        ref projectileSpeed,
                        ref range,
                        ref projectileLifetime,
                        ref projectileScale,
                        ref projectileCount,
                        ref projectilePierce,
                        ref homingStrength);
                }
            }

            if (runtimeActiveItemStatModifiers != null)
            {
                for (int modifierIndex = 0; modifierIndex < runtimeActiveItemStatModifiers.Count; modifierIndex++)
                {
                    ApplyModifier(
                        runtimeActiveItemStatModifiers[modifierIndex],
                        ref damage,
                        ref moveSpeed,
                        ref fireRate,
                        ref projectileSpeed,
                        ref range,
                        ref luck,
                        ref projectileCount,
                        ref knockback,
                        ref maxHealth);
                }
            }

            if (runtimeActiveItemProjectileModifiers != null)
            {
                for (int modifierIndex = 0; modifierIndex < runtimeActiveItemProjectileModifiers.Count; modifierIndex++)
                {
                    ApplyProjectileModifier(
                        runtimeActiveItemProjectileModifiers[modifierIndex],
                        ref projectileSpeed,
                        ref range,
                        ref projectileLifetime,
                        ref projectileScale,
                        ref projectileCount,
                        ref projectilePierce,
                        ref homingStrength);
                }
            }

            if (runtimeEventStatModifiers != null)
            {
                for (int modifierIndex = 0; modifierIndex < runtimeEventStatModifiers.Count; modifierIndex++)
                {
                    ApplyModifier(
                        runtimeEventStatModifiers[modifierIndex],
                        ref damage,
                        ref moveSpeed,
                        ref fireRate,
                        ref projectileSpeed,
                        ref range,
                        ref luck,
                        ref projectileCount,
                        ref knockback,
                        ref maxHealth);
                }
            }

            if (runtimeEventProjectileModifiers != null)
            {
                for (int modifierIndex = 0; modifierIndex < runtimeEventProjectileModifiers.Count; modifierIndex++)
                {
                    ApplyProjectileModifier(
                        runtimeEventProjectileModifiers[modifierIndex],
                        ref projectileSpeed,
                        ref range,
                        ref projectileLifetime,
                        ref projectileScale,
                        ref projectileCount,
                        ref projectilePierce,
                        ref homingStrength);
                }
            }

            float resolvedProjectileSpeed = projectileSpeed.Apply(baseStats.ProjectileSpeed, 0f);
            float resolvedRange = range.Apply(baseStats.Range, 0.5f);
            float resolvedProjectileLifetime = projectileLifetime.Apply(baseStats.ProjectileLifetime, 0.05f);
            resolvedRange = Mathf.Max(resolvedRange, resolvedProjectileSpeed * resolvedProjectileLifetime);
            resolvedProjectileLifetime = ResolveProjectileLifetime(resolvedProjectileSpeed, resolvedRange);

            return new PlayerStatSnapshot(
                maxHealth.Apply(baseStats.MaxHealth, 1f),
                moveSpeed.Apply(baseStats.MoveSpeed, 0f),
                damage.Apply(baseStats.Damage, 0f),
                ResolveFireInterval(baseStats.FireInterval, fireRate),
                resolvedProjectileSpeed,
                resolvedRange,
                resolvedProjectileLifetime,
                projectileScale.Apply(baseStats.ProjectileScale, 0.05f),
                luck.Apply(baseStats.Luck, -10f),
                projectileCount.Apply(baseStats.ProjectileCount, 1f),
                knockback.Apply(baseStats.Knockback, 0f),
                projectilePierce.Apply(baseStats.ProjectilePierce, 0f),
                homingStrength.Apply(baseStats.HomingStrength, 0f));
        }

        private void CachePassiveItems(IReadOnlyList<ItemData> passiveItems)
        {
            _cachedPassiveItems.Clear();

            if (passiveItems == null)
            {
                return;
            }

            for (int i = 0; i < passiveItems.Count; i++)
            {
                if (passiveItems[i] != null)
                {
                    _cachedPassiveItems.Add(passiveItems[i]);
                }
            }
        }

        private void ResolveSynergies()
        {
            _activeSynergies.Clear();
            _resolvedSynergyModifierStack.Clear();
            _activeSynergyNames.Clear();

            SynergyResolver.ResolveActiveSynergies(
                synergyCatalog,
                _cachedPassiveItems,
                _activeSynergies,
                _resolvedSynergyModifierStack);

            for (int synergyIndex = 0; synergyIndex < _activeSynergies.Count; synergyIndex++)
            {
                _activeSynergyNames.Add(_activeSynergies[synergyIndex].DisplayName);
            }
        }

        private static void CopyStatModifiers(IReadOnlyList<StatModifier> source, List<StatModifier> destination)
        {
            if (source == null)
            {
                return;
            }

            for (int i = 0; i < source.Count; i++)
            {
                destination.Add(source[i]);
            }
        }

        private static void CopyProjectileModifiers(IReadOnlyList<ProjectileModifier> source, List<ProjectileModifier> destination)
        {
            if (source == null)
            {
                return;
            }

            for (int i = 0; i < source.Count; i++)
            {
                destination.Add(source[i]);
            }
        }

        private static void ApplyModifier(
            StatModifier modifier,
            ref StatAccumulator damage,
            ref StatAccumulator moveSpeed,
            ref FireRateAccumulator fireRate,
            ref StatAccumulator projectileSpeed,
            ref StatAccumulator range,
            ref StatAccumulator luck,
            ref StatAccumulator projectileCount,
            ref StatAccumulator knockback,
            ref StatAccumulator maxHealth)
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
                    fireRate.Apply(modifier);
                    break;
                case PlayerStatType.ProjectileSpeed:
                    projectileSpeed.Apply(modifier);
                    break;
                case PlayerStatType.Range:
                    range.Apply(modifier);
                    break;
                case PlayerStatType.Luck:
                    luck.Apply(modifier);
                    break;
                case PlayerStatType.ProjectileCount:
                    projectileCount.Apply(modifier);
                    break;
                case PlayerStatType.Knockback:
                    knockback.Apply(modifier);
                    break;
                case PlayerStatType.MaxHealth:
                    maxHealth.Apply(modifier);
                    break;
            }
        }

        private static void ApplyProjectileModifier(
            ProjectileModifier modifier,
            ref StatAccumulator projectileSpeed,
            ref StatAccumulator range,
            ref StatAccumulator projectileLifetime,
            ref StatAccumulator projectileScale,
            ref StatAccumulator projectileCount,
            ref StatAccumulator projectilePierce,
            ref StatAccumulator homingStrength)
        {
            switch (modifier.ModifierType)
            {
                case ProjectileModifierType.Speed:
                    projectileSpeed.Apply(modifier.Operation, modifier.Value);
                    break;
                case ProjectileModifierType.Lifetime:
                    range.Apply(modifier.Operation, modifier.Value);
                    projectileLifetime.Apply(modifier.Operation, modifier.Value);
                    break;
                case ProjectileModifierType.Scale:
                    projectileScale.Apply(modifier.Operation, modifier.Value);
                    break;
                case ProjectileModifierType.MultiShot:
                    projectileCount.Apply(modifier.Operation, modifier.Value);
                    break;
                case ProjectileModifierType.Pierce:
                    projectilePierce.Apply(modifier.Operation, modifier.Value);
                    break;
                case ProjectileModifierType.Homing:
                    homingStrength.Apply(modifier.Operation, modifier.Value);
                    break;
                case ProjectileModifierType.Explode:
                case ProjectileModifierType.Laser:
                case ProjectileModifierType.Split:
                case ProjectileModifierType.Bounce:
                case ProjectileModifierType.Orbit:
                case ProjectileModifierType.Shield:
                case ProjectileModifierType.Lifesteal:
                    break;
            }
        }

        private static float ResolveProjectileLifetime(float projectileSpeed, float range)
        {
            return projectileSpeed > 0.01f
                ? Mathf.Max(0.05f, range / projectileSpeed)
                : 0.05f;
        }

        private float ResolveFireInterval(float baseFireInterval, FireRateAccumulator fireRate)
        {
            float safeBaseFireInterval = Mathf.Max(0.01f, baseFireInterval);
            float baseShotsPerSecond = 1f / safeBaseFireInterval;
            float resolvedShotsPerSecond = fireRate.Resolve(
                baseShotsPerSecond,
                safeBaseFireInterval,
                fireIntervalModifierToShotsPerSecondScale,
                minimumShotsPerSecond,
                maximumShotsPerSecond);

            return 1f / Mathf.Max(0.01f, resolvedShotsPerSecond);
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
                Apply(modifier.Operation, modifier.Value);
            }

            public void Apply(StatModifierOperation operation, float value)
            {
                switch (operation)
                {
                    case StatModifierOperation.Add:
                        _additive += value;
                        break;
                    case StatModifierOperation.Multiply:
                        _multiplier *= value;
                        break;
                    case StatModifierOperation.Override:
                        _hasOverride = true;
                        _overrideValue = value;
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

        /// <summary>
        /// Fire interval authored data is treated as "tears-like" fire-rate change instead of raw seconds removed.
        /// This keeps existing item data compatible while preventing interval values from collapsing to 0.01.
        /// </summary>
        private struct FireRateAccumulator
        {
            private float _additiveIntervalDelta;
            private float _shotsPerSecondMultiplier;
            private bool _hasOverride;
            private float _overrideFireInterval;

            public static FireRateAccumulator Create()
            {
                return new FireRateAccumulator
                {
                    _shotsPerSecondMultiplier = 1f
                };
            }

            public void Apply(StatModifier modifier)
            {
                switch (modifier.Operation)
                {
                    case StatModifierOperation.Add:
                        _additiveIntervalDelta += modifier.Value;
                        break;
                    case StatModifierOperation.Multiply:
                        _shotsPerSecondMultiplier *= modifier.Value;
                        break;
                    case StatModifierOperation.Override:
                        _hasOverride = true;
                        _overrideFireInterval = modifier.Value;
                        break;
                }
            }

            public float Resolve(
                float baseShotsPerSecond,
                float baseFireInterval,
                float intervalModifierScale,
                float minimumShotsPerSecond,
                float maximumShotsPerSecond)
            {
                if (_hasOverride)
                {
                    float safeOverrideInterval = Mathf.Max(0.01f, _overrideFireInterval);
                    return 1f / safeOverrideInterval;
                }

                float fireRateDelta = -_additiveIntervalDelta * intervalModifierScale;
                float resolvedShotsPerSecond = (baseShotsPerSecond + fireRateDelta) * _shotsPerSecondMultiplier;
                float fallbackShotsPerSecond = 1f / Mathf.Max(0.01f, baseFireInterval);
                float minShotsPerSecond = Mathf.Max(0.1f, minimumShotsPerSecond);
                float maxShotsPerSecond = Mathf.Max(minShotsPerSecond, maximumShotsPerSecond);

                if (!float.IsFinite(resolvedShotsPerSecond))
                {
                    resolvedShotsPerSecond = fallbackShotsPerSecond;
                }

                return Mathf.Clamp(resolvedShotsPerSecond, minShotsPerSecond, maxShotsPerSecond);
            }
        }
    }
}
