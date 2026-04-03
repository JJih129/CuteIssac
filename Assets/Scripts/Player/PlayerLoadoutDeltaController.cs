using System.Collections.Generic;
using CuteIssac.Combat;
using CuteIssac.Common.Stats;
using CuteIssac.Core.Feedback;
using CuteIssac.Core.Gameplay;
using CuteIssac.Data.Item;
using UnityEngine;

namespace CuteIssac.Player
{
    [DisallowMultipleComponent]
    public sealed class PlayerLoadoutDeltaController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerStats playerStats;
        [SerializeField] private PlayerInventory playerInventory;
        [SerializeField] private PlayerHealth playerHealth;
        [SerializeField] private PlayerItemManager playerItemManager;
        [SerializeField] private PlayerTrinketHolder playerTrinketHolder;
        [SerializeField] private PlayerActiveItemController playerActiveItemController;
        [SerializeField] private PlayerConsumableHolder playerConsumableHolder;
        [SerializeField] private PlayerLoadoutDeltaPresentation loadoutDeltaPresentation;

        [Header("Feedback")]
        [SerializeField] private Vector3 headerOffset = new(0f, 1.12f, 0f);
        [SerializeField] private Vector3 firstDetailOffset = new(-0.14f, 0.84f, 0f);
        [SerializeField] private Vector3 secondDetailOffset = new(0.16f, 0.68f, 0f);
        [SerializeField] private Vector3 thirdDetailOffset = new(0f, 0.52f, 0f);
        [SerializeField] [Min(1)] private int maxDetailLines = 3;

        private readonly List<DeltaEntry> _deltaBuffer = new();
        private readonly ModifierStack _modifierStack = new();
        private PlayerStatSnapshot _cachedStats;
        private ProjectileTraitState _cachedProjectileTraits;
        private float _cachedHealth;
        private int _cachedCoins;
        private int _cachedKeys;
        private int _cachedBombs;
        private ItemData _cachedTrinket;
        private ActiveItemData _cachedActiveItem;
        private ConsumableItemData _cachedHeldConsumable;
        private bool _cacheReady;
        private bool _suppressNextConsumableSlotFeedback;
        private bool _hasProjectileProfileDelta;
        private int _projectileProfileChangeCount;
        private string _projectileProfileLabel = string.Empty;
        private Color _projectileProfileAccent = Color.white;

        private void Awake()
        {
            ResolveReferences();
        }

        private void Start()
        {
            CacheCurrentState();
        }

        private void OnEnable()
        {
            ResolveReferences();

            if (playerItemManager != null)
            {
                playerItemManager.PassiveItemAcquired += HandlePassiveItemAcquired;
            }

            if (playerTrinketHolder != null)
            {
                playerTrinketHolder.TrinketChanged += HandleTrinketChanged;
            }

            if (playerActiveItemController != null)
            {
                playerActiveItemController.ActiveItemStateChanged += HandleActiveItemStateChanged;
            }

            if (playerConsumableHolder != null)
            {
                playerConsumableHolder.ConsumableStateChanged += HandleConsumableStateChanged;
                playerConsumableHolder.ConsumableApplied += HandleConsumableApplied;
            }
        }

        private void OnDisable()
        {
            if (playerItemManager != null)
            {
                playerItemManager.PassiveItemAcquired -= HandlePassiveItemAcquired;
            }

            if (playerTrinketHolder != null)
            {
                playerTrinketHolder.TrinketChanged -= HandleTrinketChanged;
            }

            if (playerActiveItemController != null)
            {
                playerActiveItemController.ActiveItemStateChanged -= HandleActiveItemStateChanged;
            }

            if (playerConsumableHolder != null)
            {
                playerConsumableHolder.ConsumableStateChanged -= HandleConsumableStateChanged;
                playerConsumableHolder.ConsumableApplied -= HandleConsumableApplied;
            }
        }

        private void HandlePassiveItemAcquired(ItemData itemData)
        {
            if (!_cacheReady)
            {
                CacheCurrentState();
                return;
            }

            if (itemData != null)
            {
                CollectCurrentStateDeltaLines();
                AppendItemTraitDeltaLines(itemData);
                EnsureMinimumDeltaLine(itemData.ItemCategory switch
                {
                    ItemCategory.Damage => "OFFENSE UP",
                    ItemCategory.FireRate => "FIRE RHYTHM UP",
                    ItemCategory.Movement => "MOVE FLOW UP",
                    ItemCategory.Projectile => "SHOT PATTERN SHIFT",
                    ItemCategory.Defense => "SURVIVE EDGE",
                    _ => "BUILD UPDATED"
                }, ResolveItemAccent(itemData.Rarity));
                EmitDelta("LOADOUT SHIFT", ResolveItemAccent(itemData.Rarity), itemData.Rarity >= ItemRarity.Rare);
            }

            CacheCurrentState();
        }

        private void HandleTrinketChanged()
        {
            if (!_cacheReady)
            {
                CacheCurrentState();
                return;
            }

            ItemData currentTrinket = playerTrinketHolder != null ? playerTrinketHolder.EquippedTrinket : null;
            if (currentTrinket == _cachedTrinket)
            {
                CacheCurrentState();
                return;
            }

            if (currentTrinket != null)
            {
                CollectCurrentStateDeltaLines();
                AppendItemTraitDeltaLines(currentTrinket);
                EnsureMinimumDeltaLine(
                    _cachedTrinket != null ? "TRINKET SLOT SHIFT" : "TRINKET ONLINE",
                    ResolveItemAccent(currentTrinket.Rarity));
                EmitDelta(
                    _cachedTrinket != null ? "TRINKET SWAP" : "TRINKET ONLINE",
                    ResolveItemAccent(currentTrinket.Rarity),
                    true);
            }

            CacheCurrentState();
        }

        private void HandleActiveItemStateChanged(PlayerActiveItemSlotState state)
        {
            if (!_cacheReady)
            {
                CacheCurrentState();
                return;
            }

            ActiveItemData currentActiveItem = state.ActiveItemData;
            if (currentActiveItem == _cachedActiveItem)
            {
                CacheCurrentState();
                return;
            }

            if (currentActiveItem != null)
            {
                _deltaBuffer.Clear();
                AddDeltaCandidate(
                    $"{currentActiveItem.MaxCharge} CHARGE",
                    new Color(0.7f, 0.92f, 1f, 1f),
                    4f);
                AddDeltaCandidate(
                    ResolveActiveChargeRuleLabel(currentActiveItem),
                    new Color(0.54f, 0.82f, 1f, 1f),
                    3f);
                EmitDelta(
                    _cachedActiveItem != null ? "ACTIVE SWAP" : "ACTIVE READY",
                    new Color(0.58f, 0.9f, 1f, 1f),
                    true);
            }

            CacheCurrentState();
        }

        private void HandleConsumableStateChanged(PlayerConsumableSlotState state)
        {
            if (!_cacheReady)
            {
                CacheCurrentState();
                return;
            }

            ConsumableItemData currentHeldConsumable = playerConsumableHolder != null ? playerConsumableHolder.HeldConsumable : null;
            if (_suppressNextConsumableSlotFeedback)
            {
                _suppressNextConsumableSlotFeedback = false;
                CacheCurrentState();
                return;
            }

            if (currentHeldConsumable == _cachedHeldConsumable)
            {
                CacheCurrentState();
                return;
            }

            if (currentHeldConsumable != null)
            {
                _deltaBuffer.Clear();
                AppendConsumableDescriptorLines(currentHeldConsumable);
                EnsureMinimumDeltaLine("USE LATER", ResolveConsumableAccent(currentHeldConsumable));
                EmitDelta("STASH READY", ResolveConsumableAccent(currentHeldConsumable), currentHeldConsumable.HasTimedEffect);
            }

            CacheCurrentState();
        }

        private void HandleConsumableApplied(ConsumableItemData consumableItemData)
        {
            if (!_cacheReady)
            {
                CacheCurrentState();
                return;
            }

            if (consumableItemData == null)
            {
                CacheCurrentState();
                return;
            }

            CollectCurrentStateDeltaLines();
            AppendConsumableModifierDeltaLines(consumableItemData);
            EnsureMinimumDeltaLine(
                consumableItemData.HasTimedEffect ? "BUFF ONLINE" : "SUPPLY POP",
                ResolveConsumableAccent(consumableItemData));
            EmitDelta(
                ResolveConsumableHeader(consumableItemData),
                ResolveConsumableAccent(consumableItemData),
                consumableItemData.HasTimedEffect);
            _suppressNextConsumableSlotFeedback = true;
            CacheCurrentState();
        }

        private void CollectCurrentStateDeltaLines()
        {
            _deltaBuffer.Clear();
            _hasProjectileProfileDelta = false;
            _projectileProfileChangeCount = 0;
            _projectileProfileLabel = string.Empty;
            _projectileProfileAccent = Color.white;

            PlayerStatSnapshot currentStats = playerStats != null ? playerStats.CurrentStats : _cachedStats;
            ProjectileTraitState currentProjectileTraits = playerStats != null ? playerStats.CurrentProjectileTraits : _cachedProjectileTraits;
            float currentHealth = playerHealth != null ? playerHealth.CurrentHealth : _cachedHealth;
            int currentCoins = playerInventory != null ? playerInventory.Coins : _cachedCoins;
            int currentKeys = playerInventory != null ? playerInventory.Keys : _cachedKeys;
            int currentBombs = playerInventory != null ? playerInventory.Bombs : _cachedBombs;

            AddSignedStatDelta(currentStats.Damage - _cachedStats.Damage, "DMG", new Color(1f, 0.5f, 0.42f, 1f), 3.8f, 0.05f);
            AddSignedStatDelta(ResolveShotsPerSecond(currentStats.FireInterval) - ResolveShotsPerSecond(_cachedStats.FireInterval), "SHOT", new Color(1f, 0.76f, 0.38f, 1f), 3.4f, 0.05f);
            AddSignedStatDelta(currentStats.MoveSpeed - _cachedStats.MoveSpeed, "SPD", new Color(0.48f, 1f, 0.72f, 1f), 2.8f, 0.05f);
            AddSignedStatDelta(currentStats.MaxHealth - _cachedStats.MaxHealth, "HP CAP", new Color(1f, 0.58f, 0.68f, 1f), 3.2f, 0.05f);
            AddSignedStatDelta(currentHealth - _cachedHealth, "HP", new Color(1f, 0.54f, 0.62f, 1f), 3.6f, 0.05f);
            AddSignedStatDelta(currentStats.ProjectileCount - _cachedStats.ProjectileCount, "SHOT COUNT", new Color(0.72f, 0.9f, 1f, 1f), 3.1f, 0.05f);
            AddSignedStatDelta(currentStats.ProjectilePierce - _cachedStats.ProjectilePierce, "PIERCE", new Color(0.66f, 0.9f, 1f, 1f), 2.7f, 0.05f);
            AddSignedStatDelta(currentStats.HomingStrength - _cachedStats.HomingStrength, "HOMING", new Color(0.72f, 0.84f, 1f, 1f), 2.5f, 0.05f);
            AddSignedStatDelta(currentStats.Range - _cachedStats.Range, "RANGE", new Color(0.58f, 0.84f, 1f, 1f), 2.1f, 0.25f);
            AddSignedStatDelta(currentStats.Luck - _cachedStats.Luck, "LUCK", new Color(0.74f, 1f, 0.62f, 1f), 1.8f, 0.05f);

            AddSignedStatDelta(currentCoins - _cachedCoins, "COIN", new Color(0.98f, 0.86f, 0.34f, 1f), 3f, 0.5f);
            AddSignedStatDelta(currentKeys - _cachedKeys, "KEY", new Color(0.74f, 0.88f, 1f, 1f), 2.7f, 0.5f);
            AddSignedStatDelta(currentBombs - _cachedBombs, "BOMB", new Color(1f, 0.6f, 0.3f, 1f), 2.7f, 0.5f);

            AppendProjectileTraitStateDeltaLines(_cachedProjectileTraits, currentProjectileTraits);
        }

        private void AppendProjectileTraitStateDeltaLines(ProjectileTraitState previousTraits, ProjectileTraitState currentTraits)
        {
            int changeCount = 0;
            Color dominantAccent = new(0.78f, 0.9f, 1f, 1f);

            changeCount += AppendProjectileTraitStateDeltaLine(
                previousTraits,
                currentTraits,
                ProjectileTraitFlags.Laser,
                "LASER",
                new Color(1f, 0.48f, 0.52f, 1f),
                ref dominantAccent);

            changeCount += AppendProjectileTraitStateDeltaLine(
                previousTraits,
                currentTraits,
                ProjectileTraitFlags.Orbit,
                "ORBIT",
                new Color(1f, 0.84f, 0.42f, 1f),
                ref dominantAccent);

            changeCount += AppendProjectileTraitStateDeltaLine(
                previousTraits,
                currentTraits,
                ProjectileTraitFlags.Shield,
                "SHIELD",
                new Color(0.46f, 0.92f, 1f, 1f),
                ref dominantAccent);

            changeCount += AppendProjectileTraitStateDeltaLine(
                previousTraits,
                currentTraits,
                ProjectileTraitFlags.Split,
                "SPLIT",
                new Color(1f, 0.62f, 0.82f, 1f),
                ref dominantAccent);

            changeCount += AppendProjectileTraitStateDeltaLine(
                previousTraits,
                currentTraits,
                ProjectileTraitFlags.Explosive,
                "BLAST",
                new Color(1f, 0.66f, 0.36f, 1f),
                ref dominantAccent);

            changeCount += AppendProjectileTraitStateDeltaLine(
                previousTraits,
                currentTraits,
                ProjectileTraitFlags.Lifesteal,
                "LEECH",
                new Color(0.56f, 1f, 0.62f, 1f),
                ref dominantAccent);

            changeCount += AppendProjectileTraitStateDeltaLine(
                previousTraits,
                currentTraits,
                ProjectileTraitFlags.Bounce,
                "BOUNCE",
                new Color(0.86f, 0.92f, 1f, 1f),
                ref dominantAccent);

            if (changeCount > 1)
            {
                AddDeltaCandidate("SHOT PROFILE SHIFT", dominantAccent, 4.6f + (changeCount * 0.08f));
            }

            if (changeCount > 0)
            {
                _hasProjectileProfileDelta = true;
                _projectileProfileChangeCount = changeCount;
                _projectileProfileLabel = ResolveProjectileTraitProfileLabel(currentTraits);
                _projectileProfileAccent = dominantAccent;

                AddDeltaCandidate(
                    _projectileProfileLabel == "BASELINE" ? "SHOT RESET" : _projectileProfileLabel,
                    dominantAccent,
                    5.05f + (changeCount * 0.12f));
            }
        }

        private int AppendProjectileTraitStateDeltaLine(
            ProjectileTraitState previousTraits,
            ProjectileTraitState currentTraits,
            ProjectileTraitFlags flag,
            string label,
            Color accentColor,
            ref Color dominantAccent)
        {
            bool hadTrait = HasActiveTrait(previousTraits, flag);
            bool hasTrait = HasActiveTrait(currentTraits, flag);
            float previousStrength = ResolveProjectileTraitStrength(previousTraits, flag);
            float currentStrength = ResolveProjectileTraitStrength(currentTraits, flag);

            if (hadTrait != hasTrait)
            {
                AddDeltaCandidate(
                    hasTrait ? $"{label} ONLINE" : $"{label} LOST",
                    accentColor,
                    hasTrait ? 3.55f + currentStrength : 3.15f + previousStrength);
                dominantAccent = accentColor;
                return 1;
            }

            if (hasTrait && Mathf.Abs(currentStrength - previousStrength) > 0.34f)
            {
                AddDeltaCandidate(
                    currentStrength > previousStrength ? $"{label} AMPED" : $"{label} THINNED",
                    accentColor,
                    2.95f + Mathf.Abs(currentStrength - previousStrength));
                dominantAccent = accentColor;
                return 1;
            }

            return 0;
        }

        private void AppendItemTraitDeltaLines(ItemData itemData)
        {
            if (itemData == null)
            {
                return;
            }

            itemData.BuildModifierStack(_modifierStack);
            AppendProjectileTraitLines(_modifierStack.ProjectileModifiers, ResolveItemAccent(itemData.Rarity));
        }

        private void AppendConsumableModifierDeltaLines(ConsumableItemData consumableItemData)
        {
            if (consumableItemData == null)
            {
                return;
            }

            AppendProjectileTraitLines(consumableItemData.TemporaryProjectileModifiers, ResolveConsumableAccent(consumableItemData));
        }

        private void AppendProjectileTraitLines(IReadOnlyList<ProjectileModifier> projectileModifiers, Color accentColor)
        {
            if (projectileModifiers == null)
            {
                return;
            }

            for (int index = 0; index < projectileModifiers.Count; index++)
            {
                string traitLabel = projectileModifiers[index].ModifierType switch
                {
                    ProjectileModifierType.Explode => "EXPLOSIVE",
                    ProjectileModifierType.Laser => "LASER SHIFT",
                    ProjectileModifierType.Split => "SPLIT SHOT",
                    ProjectileModifierType.Bounce => "RICOCHET",
                    ProjectileModifierType.Orbit => "ORBITAL",
                    ProjectileModifierType.Shield => "SHIELD",
                    ProjectileModifierType.Lifesteal => "LIFESTEAL",
                    _ => string.Empty
                };

                if (!string.IsNullOrWhiteSpace(traitLabel))
                {
                    AddDeltaCandidate(traitLabel, accentColor, 2.4f - (index * 0.1f));
                }
            }
        }

        private void AppendConsumableDescriptorLines(ConsumableItemData consumableItemData)
        {
            if (consumableItemData == null)
            {
                return;
            }

            if (consumableItemData.HealAmount > 0f)
            {
                AddDeltaCandidate($"+{consumableItemData.HealAmount:0.#} HP", new Color(1f, 0.54f, 0.62f, 1f), 3.1f);
            }

            if (consumableItemData.CoinGain > 0)
            {
                AddDeltaCandidate($"+{consumableItemData.CoinGain} COIN", new Color(0.98f, 0.86f, 0.34f, 1f), 2.7f);
            }

            if (consumableItemData.KeyGain > 0)
            {
                AddDeltaCandidate($"+{consumableItemData.KeyGain} KEY", new Color(0.74f, 0.88f, 1f, 1f), 2.7f);
            }

            if (consumableItemData.BombGain > 0)
            {
                AddDeltaCandidate($"+{consumableItemData.BombGain} BOMB", new Color(1f, 0.6f, 0.3f, 1f), 2.7f);
            }

            if (consumableItemData.HasTimedEffect)
            {
                AddDeltaCandidate($"{consumableItemData.TemporaryEffectDuration:0.#}S BUFF", new Color(0.58f, 0.92f, 1f, 1f), 3.2f);
                AppendConsumableModifierDeltaLines(consumableItemData);
            }
        }

        private void AddSignedStatDelta(float delta, string suffix, Color baseColor, float baseScore, float threshold)
        {
            if (Mathf.Abs(delta) < threshold)
            {
                return;
            }

            Color deltaColor = delta >= 0f
                ? baseColor
                : Color.Lerp(baseColor, new Color(1f, 0.44f, 0.42f, 1f), 0.56f);
            AddDeltaCandidate(
                $"{(delta >= 0f ? "+" : string.Empty)}{delta:0.##} {suffix}",
                deltaColor,
                baseScore + Mathf.Abs(delta));
        }

        private void EnsureMinimumDeltaLine(string fallbackLabel, Color accentColor)
        {
            if (_deltaBuffer.Count > 0 || string.IsNullOrWhiteSpace(fallbackLabel))
            {
                return;
            }

            AddDeltaCandidate(fallbackLabel, accentColor, 1f);
        }

        private void AddDeltaCandidate(string text, Color color, float score)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            _deltaBuffer.Add(new DeltaEntry(text, color, score));
        }

        private void EmitDelta(string header, Color accentColor, bool emphasize)
        {
            if (string.IsNullOrWhiteSpace(header))
            {
                return;
            }

            string resolvedHeader = header;
            Color resolvedAccent = accentColor;
            bool resolvedEmphasize = emphasize;

            if (_hasProjectileProfileDelta)
            {
                resolvedHeader = ResolveProjectileProfileHeader(header);
                resolvedAccent = Color.Lerp(accentColor, _projectileProfileAccent, 0.58f);
                resolvedEmphasize = true;
            }

            ResolveReferences();
            EnsurePresentation().PlayDelta(resolvedAccent, resolvedEmphasize);

            _deltaBuffer.Sort((left, right) => right.Score.CompareTo(left.Score));
            if (_deltaBuffer.Count > maxDetailLines)
            {
                _deltaBuffer.RemoveRange(maxDetailLines, _deltaBuffer.Count - maxDetailLines);
            }

            StartCoroutine(RaisePlayerLoadoutDeltaDeferred(new PlayerLoadoutDeltaSignal(
                transform.position,
                resolvedHeader,
                BuildDeltaSummary(),
                resolvedAccent,
                resolvedEmphasize)));

            GameplayFeedbackEvents.RaiseFloatingFeedback(new FloatingFeedbackRequest(
                transform.position + headerOffset,
                resolvedHeader,
                Color.Lerp(resolvedAccent, Color.white, 0.18f),
                resolvedEmphasize ? 0.7f : 0.58f,
                resolvedEmphasize ? 0.52f : 0.44f,
                resolvedEmphasize ? 1.08f : 1f,
                visualProfile: FloatingFeedbackVisualProfile.Momentum));

            for (int index = 0; index < _deltaBuffer.Count; index++)
            {
                Vector3 offset = index switch
                {
                    0 => firstDetailOffset,
                    1 => secondDetailOffset,
                    _ => thirdDetailOffset
                };

                DeltaEntry entry = _deltaBuffer[index];
                GameplayFeedbackEvents.RaiseFloatingFeedback(new FloatingFeedbackRequest(
                    transform.position + offset,
                    entry.Text,
                    Color.Lerp(entry.Color, Color.white, 0.08f),
                    0.62f - (index * 0.06f),
                    0.38f - (index * 0.06f),
                    0.92f,
                    visualProfile: FloatingFeedbackVisualProfile.Momentum));
            }

            _deltaBuffer.Clear();
            _hasProjectileProfileDelta = false;
            _projectileProfileChangeCount = 0;
            _projectileProfileLabel = string.Empty;
            _projectileProfileAccent = Color.white;
        }

        private string BuildDeltaSummary()
        {
            if (_deltaBuffer.Count == 0)
            {
                return string.Empty;
            }

            string summary = string.Empty;

            for (int index = 0; index < _deltaBuffer.Count; index++)
            {
                if (index > 0)
                {
                    summary += " / ";
                }

                summary += _deltaBuffer[index].Text;
            }

            return summary;
        }

        private System.Collections.IEnumerator RaisePlayerLoadoutDeltaDeferred(PlayerLoadoutDeltaSignal signal)
        {
            yield return null;
            GameplayRuntimeEvents.RaisePlayerLoadoutDelta(signal);
        }

        private PlayerLoadoutDeltaPresentation EnsurePresentation()
        {
            if (loadoutDeltaPresentation == null)
            {
                loadoutDeltaPresentation = GetComponent<PlayerLoadoutDeltaPresentation>();
            }

            if (loadoutDeltaPresentation == null)
            {
                loadoutDeltaPresentation = gameObject.AddComponent<PlayerLoadoutDeltaPresentation>();
            }

            return loadoutDeltaPresentation;
        }

        private void ResolveReferences()
        {
            if (playerStats == null)
            {
                playerStats = GetComponent<PlayerStats>();
            }

            if (playerInventory == null)
            {
                playerInventory = GetComponent<PlayerInventory>();
            }

            if (playerHealth == null)
            {
                playerHealth = GetComponent<PlayerHealth>();
            }

            if (playerItemManager == null)
            {
                playerItemManager = GetComponent<PlayerItemManager>();
            }

            if (playerTrinketHolder == null)
            {
                playerTrinketHolder = GetComponent<PlayerTrinketHolder>();
            }

            if (playerActiveItemController == null)
            {
                playerActiveItemController = GetComponent<PlayerActiveItemController>();
            }

            if (playerConsumableHolder == null)
            {
                playerConsumableHolder = GetComponent<PlayerConsumableHolder>();
            }

            if (loadoutDeltaPresentation == null)
            {
                loadoutDeltaPresentation = GetComponent<PlayerLoadoutDeltaPresentation>();
            }
        }

        private void CacheCurrentState()
        {
            ResolveReferences();

            if (playerStats != null)
            {
                _cachedStats = playerStats.CurrentStats;
                _cachedProjectileTraits = playerStats.CurrentProjectileTraits;
            }

            _cachedHealth = playerHealth != null ? playerHealth.CurrentHealth : 0f;
            _cachedCoins = playerInventory != null ? playerInventory.Coins : 0;
            _cachedKeys = playerInventory != null ? playerInventory.Keys : 0;
            _cachedBombs = playerInventory != null ? playerInventory.Bombs : 0;
            _cachedTrinket = playerTrinketHolder != null ? playerTrinketHolder.EquippedTrinket : null;
            _cachedActiveItem = playerActiveItemController != null ? playerActiveItemController.EquippedItem : null;
            _cachedHeldConsumable = playerConsumableHolder != null ? playerConsumableHolder.HeldConsumable : null;
            _cacheReady = true;
        }

        private static float ResolveShotsPerSecond(float fireInterval)
        {
            return fireInterval > 0.01f ? 1f / fireInterval : 0f;
        }

        private string ResolveProjectileProfileHeader(string fallbackHeader)
        {
            if (!_hasProjectileProfileDelta)
            {
                return fallbackHeader;
            }

            if (string.Equals(_projectileProfileLabel, "BASELINE", System.StringComparison.Ordinal))
            {
                return "SHOT RESET";
            }

            return _projectileProfileChangeCount > 1
                ? "SHOT PROFILE"
                : "SHOT TUNE";
        }

        private static string ResolveProjectileTraitProfileLabel(ProjectileTraitState traits)
        {
            string[] labels = new string[7];
            int count = 0;

            if (traits.IsLaser)
            {
                labels[count++] = "LASER";
            }

            if (traits.IsOrbiting)
            {
                labels[count++] = "ORBIT";
            }

            if (traits.IsShielded)
            {
                labels[count++] = "SHIELD";
            }

            if (traits.IsSplit)
            {
                labels[count++] = "SPLIT";
            }

            if (traits.IsExplosive)
            {
                labels[count++] = "BLAST";
            }

            if (traits.Has(ProjectileTraitFlags.Lifesteal) && traits.LifestealStrength > 0.01f)
            {
                labels[count++] = "LEECH";
            }

            if (traits.Has(ProjectileTraitFlags.Bounce) && traits.BounceStrength > 0.01f)
            {
                labels[count++] = "BOUNCE";
            }

            if (count == 0)
            {
                return "BASELINE";
            }

            if (count == 1)
            {
                return labels[0];
            }

            if (count == 2)
            {
                return $"{labels[0]} / {labels[1]}";
            }

            return $"{labels[0]} / {labels[1]} / +{count - 2}";
        }

        private static bool HasActiveTrait(ProjectileTraitState traits, ProjectileTraitFlags flag)
        {
            return flag switch
            {
                ProjectileTraitFlags.Explosive => traits.IsExplosive,
                ProjectileTraitFlags.Laser => traits.IsLaser,
                ProjectileTraitFlags.Split => traits.IsSplit,
                ProjectileTraitFlags.Orbit => traits.IsOrbiting,
                ProjectileTraitFlags.Shield => traits.IsShielded,
                ProjectileTraitFlags.Bounce => traits.Has(ProjectileTraitFlags.Bounce) && traits.BounceStrength > 0.01f,
                ProjectileTraitFlags.Lifesteal => traits.Has(ProjectileTraitFlags.Lifesteal) && traits.LifestealStrength > 0.01f,
                _ => false
            };
        }

        private static float ResolveProjectileTraitStrength(ProjectileTraitState traits, ProjectileTraitFlags flag)
        {
            return flag switch
            {
                ProjectileTraitFlags.Explosive => traits.ExplosionStrength,
                ProjectileTraitFlags.Laser => traits.LaserStrength,
                ProjectileTraitFlags.Split => traits.SplitStrength,
                ProjectileTraitFlags.Bounce => traits.BounceStrength,
                ProjectileTraitFlags.Orbit => traits.OrbitStrength,
                ProjectileTraitFlags.Shield => traits.ShieldStrength,
                ProjectileTraitFlags.Lifesteal => traits.LifestealStrength,
                _ => 0f
            };
        }

        private static string ResolveActiveChargeRuleLabel(ActiveItemData activeItemData)
        {
            if (activeItemData == null)
            {
                return string.Empty;
            }

            return activeItemData.ChargeRule switch
            {
                ActiveItemChargeRule.RoomClear => "ROOM CHARGE",
                ActiveItemChargeRule.EnemyKill => "KILL CHARGE",
                _ => "ACTIVE SLOT"
            };
        }

        private static string ResolveConsumableHeader(ConsumableItemData consumableItemData)
        {
            if (consumableItemData == null)
            {
                return "SUPPLY GAIN";
            }

            if (consumableItemData.HasTimedEffect)
            {
                return "BUFF ONLINE";
            }

            if (consumableItemData.HealAmount > 0f)
            {
                return "RECOVERY";
            }

            return "SUPPLY GAIN";
        }

        private static Color ResolveConsumableAccent(ConsumableItemData consumableItemData)
        {
            if (consumableItemData == null)
            {
                return new Color(0.78f, 0.84f, 1f, 1f);
            }

            if (consumableItemData.HealAmount > 0f)
            {
                return new Color(1f, 0.54f, 0.62f, 1f);
            }

            if (consumableItemData.CoinGain > 0)
            {
                return new Color(0.98f, 0.86f, 0.34f, 1f);
            }

            if (consumableItemData.KeyGain > 0)
            {
                return new Color(0.74f, 0.88f, 1f, 1f);
            }

            if (consumableItemData.BombGain > 0)
            {
                return new Color(1f, 0.6f, 0.3f, 1f);
            }

            if (consumableItemData.HasTimedEffect)
            {
                return new Color(0.58f, 0.92f, 1f, 1f);
            }

            return new Color(0.78f, 0.84f, 1f, 1f);
        }

        private static Color ResolveItemAccent(ItemRarity rarity)
        {
            return rarity switch
            {
                ItemRarity.Uncommon => new Color(0.52f, 1f, 0.68f, 1f),
                ItemRarity.Rare => new Color(1f, 0.84f, 0.42f, 1f),
                ItemRarity.Legendary => new Color(1f, 0.48f, 0.82f, 1f),
                ItemRarity.Relic => new Color(1f, 0.92f, 0.6f, 1f),
                ItemRarity.Boss => new Color(1f, 0.4f, 0.4f, 1f),
                _ => new Color(0.72f, 0.85f, 1f, 1f)
            };
        }

        private readonly struct DeltaEntry
        {
            public DeltaEntry(string text, Color color, float score)
            {
                Text = text;
                Color = color;
                Score = score;
            }

            public string Text { get; }
            public Color Color { get; }
            public float Score { get; }
        }
    }
}
