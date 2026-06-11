using System;
using System.Collections.Generic;
using System.Text;
using CuteIssac.Common.Stats;
using CuteIssac.Core.Feedback;
using CuteIssac.Core.Gameplay;
using CuteIssac.Core.Run;
using CuteIssac.Data.Dungeon;
using CuteIssac.Data.Item;
using CuteIssac.Item;
using CuteIssac.Player.ItemEffects;
using UnityEngine;

namespace CuteIssac.Player
{
    /// <summary>
    /// Coordinates passive item acquisition, stat recomputation, and passive event-effect bindings.
    /// Inventory stays as ownership data while this component turns owned items into runtime gameplay behavior.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerItemManager : MonoBehaviour
    {
        [SerializeField] private PlayerInventory playerInventory;
        [SerializeField] private PlayerTrinketHolder playerTrinketHolder;
        [SerializeField] private PlayerStats playerStats;
        [SerializeField] private PlayerHealth playerHealth;
        [SerializeField] private PlayerActiveItemController playerActiveItemController;
        [SerializeField] private PlayerWeaponLoadout playerWeaponLoadout;
        [SerializeField] private RunItemPoolService runItemPoolService;

        [Header("Debug")]
        [SerializeField] private ItemData debugPickupItem;

        public event Action<ItemData> PassiveItemAcquired;

        private readonly List<Action> _eventUnbindActions = new();
        private readonly List<TimedEventModifierInstance> _timedEventModifiers = new();
        private readonly List<StatModifier> _resolvedTimedEventStatModifiers = new();
        private readonly List<ProjectileModifier> _resolvedTimedEventProjectileModifiers = new();
        private readonly Dictionary<ItemGameplayEventEffect, float> _eventEffectNextTriggerTimes = new();
        private readonly ModifierStack _pickupPreviewModifierStack = new();
        private readonly List<ItemData> _ownedItemsBuffer = new();
        private readonly HashSet<string> _ownedItemIdBuffer = new(StringComparer.Ordinal);

        private void Awake()
        {
            ResolveDependencies();

            if (playerInventory != null)
            {
                playerInventory.InventoryChanged += HandleInventoryChanged;
            }

            if (playerTrinketHolder != null)
            {
                playerTrinketHolder.TrinketChanged += HandleTrinketChanged;
            }
        }

        private void Start()
        {
            RecalculateStats();
            runItemPoolService?.SyncOwnedItems(_ownedItemsBuffer);
            RebuildEventEffectBindings();
        }

        private void OnEnable()
        {
            PlayerRegistry.Register(this);
        }

        private void OnDisable()
        {
            PlayerRegistry.Unregister(this);
        }

        private void Update()
        {
            if (_timedEventModifiers.Count == 0)
            {
                return;
            }

            bool changed = false;

            for (int index = _timedEventModifiers.Count - 1; index >= 0; index--)
            {
                TimedEventModifierInstance instance = _timedEventModifiers[index];
                instance.RemainingDuration = Mathf.Max(0f, instance.RemainingDuration - Time.deltaTime);

                if (instance.RemainingDuration <= 0f)
                {
                    _timedEventModifiers.RemoveAt(index);
                    changed = true;
                    continue;
                }

                _timedEventModifiers[index] = instance;
            }

            if (changed)
            {
                RebuildTimedEventModifiers();
            }
        }

        private void OnDestroy()
        {
            PlayerRegistry.Unregister(this);

            if (playerInventory != null)
            {
                playerInventory.InventoryChanged -= HandleInventoryChanged;
            }

            if (playerTrinketHolder != null)
            {
                playerTrinketHolder.TrinketChanged -= HandleTrinketChanged;
            }

            ClearEventEffectBindings();
            _eventEffectNextTriggerTimes.Clear();
        }

        public bool AcquirePassiveItem(ItemData itemData)
        {
            ResolveDependencies();

            if (itemData != null && itemData.IsWeaponRelic)
            {
                return AcquireWeaponItem(itemData);
            }

            if (playerInventory == null || playerStats == null)
            {
                Debug.LogError("PlayerItemManager requires PlayerInventory and PlayerStats.", this);
                return false;
            }

            bool added = playerInventory.AddPassiveItem(itemData);

            if (added)
            {
                runItemPoolService?.RegisterAcquired(itemData);
                RaisePickupBanner(itemData);
                PassiveItemAcquired?.Invoke(itemData);
            }

            return added;
        }

        public bool AcquireWeaponItem(ItemData itemData)
        {
            ResolveDependencies();

            if (itemData == null || !itemData.IsWeaponRelic || playerWeaponLoadout == null)
            {
                return false;
            }

            bool acquired = playerWeaponLoadout.TryAcquireWeapon(itemData);
            if (!acquired)
            {
                return false;
            }

            RecalculateStats();
            runItemPoolService?.SyncOwnedItems(_ownedItemsBuffer);
            RebuildEventEffectBindings();
            runItemPoolService?.RegisterAcquired(itemData);
            RaisePickupBanner(itemData);
            return true;
        }

        public bool AcquireTrinketItem(ItemData itemData)
        {
            ResolveDependencies();

            if (itemData == null || itemData.ItemType != ItemType.Trinket || playerStats == null)
            {
                return false;
            }

            if (playerTrinketHolder == null)
            {
                playerTrinketHolder = GetComponent<PlayerTrinketHolder>();

                if (playerTrinketHolder == null)
                {
                    playerTrinketHolder = gameObject.AddComponent<PlayerTrinketHolder>();
                }

                playerTrinketHolder.TrinketChanged -= HandleTrinketChanged;
                playerTrinketHolder.TrinketChanged += HandleTrinketChanged;
            }

            bool equipped = playerTrinketHolder.TryEquipTrinket(itemData);

            if (equipped)
            {
                RaisePickupBanner(itemData);
            }

            return equipped;
        }

        [ContextMenu("Pickup Debug Item")]
        public void PickupDebugItem()
        {
            if (debugPickupItem == null)
            {
                Debug.LogWarning("PlayerItemManager debugPickupItem is not assigned.", this);
                return;
            }

            AcquirePassiveItem(debugPickupItem);
        }

        private void HandleInventoryChanged()
        {
            RecalculateStats();
            runItemPoolService?.SyncOwnedItems(_ownedItemsBuffer);
            RebuildEventEffectBindings();
        }

        private void HandleTrinketChanged()
        {
            RecalculateStats();
            runItemPoolService?.SyncOwnedItems(_ownedItemsBuffer);
            RebuildEventEffectBindings();
        }

        private void RecalculateStats()
        {
            if (playerStats != null)
            {
                BuildOwnedItemsBuffer();
                playerStats.Recalculate(_ownedItemsBuffer);
            }
        }

        private void ResolveDependencies()
        {
            if (playerInventory == null)
            {
                playerInventory = GetComponent<PlayerInventory>();
            }

            if (playerStats == null)
            {
                playerStats = GetComponent<PlayerStats>();
            }

            if (playerTrinketHolder == null)
            {
                playerTrinketHolder = GetComponent<PlayerTrinketHolder>();
            }

            if (playerHealth == null)
            {
                playerHealth = GetComponent<PlayerHealth>();
            }

            if (playerActiveItemController == null)
            {
                playerActiveItemController = GetComponent<PlayerActiveItemController>();
            }

            if (playerWeaponLoadout == null)
            {
                playerWeaponLoadout = GetComponent<PlayerWeaponLoadout>();
            }

            if (runItemPoolService == null)
            {
                runItemPoolService = FindFirstObjectByType<RunItemPoolService>(FindObjectsInactive.Exclude);
            }
        }

        private void RebuildEventEffectBindings()
        {
            ClearEventEffectBindings();

            if (playerInventory == null)
            {
                return;
            }

            BuildOwnedItemsBuffer();

            for (int itemIndex = 0; itemIndex < _ownedItemsBuffer.Count; itemIndex++)
            {
                ItemData itemData = _ownedItemsBuffer[itemIndex];

                if (itemData == null)
                {
                    continue;
                }

                IReadOnlyList<ItemGameplayEventEffect> gameplayEventEffects = itemData.GameplayEventEffects;

                for (int effectIndex = 0; effectIndex < gameplayEventEffects.Count; effectIndex++)
                {
                    ItemGameplayEventEffect effect = gameplayEventEffects[effectIndex];

                    if (effect == null)
                    {
                        continue;
                    }

                    BindItemGameplayEffect(effect);
                }
            }
        }

        private void BuildOwnedItemsBuffer()
        {
            _ownedItemsBuffer.Clear();
            _ownedItemIdBuffer.Clear();

            if (playerInventory != null)
            {
                IReadOnlyList<ItemData> passiveItems = playerInventory.PassiveItems;

                for (int index = 0; index < passiveItems.Count; index++)
                {
                    TryAddOwnedItemToBuffer(passiveItems[index]);
                }
            }

            AppendOwnedWeaponItemsToBuffer();

            if (playerTrinketHolder != null)
            {
                TryAddOwnedItemToBuffer(playerTrinketHolder.EquippedTrinket);
            }
        }

        private void AppendOwnedWeaponItemsToBuffer()
        {
            if (playerWeaponLoadout == null)
            {
                return;
            }

            playerWeaponLoadout.AppendOwnedWeaponItems(_ownedItemsBuffer, _ownedItemIdBuffer);
        }

        private bool TryAddOwnedItemToBuffer(ItemData itemData)
        {
            if (!TryRegisterOwnedItemId(itemData))
            {
                return false;
            }

            _ownedItemsBuffer.Add(itemData);
            return true;
        }

        private bool TryRegisterOwnedItemId(ItemData itemData)
        {
            if (itemData == null)
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(itemData.ItemId))
            {
                return _ownedItemIdBuffer.Add(itemData.ItemId);
            }

            return !_ownedItemsBuffer.Contains(itemData);
        }

        private void BindItemGameplayEffect(ItemGameplayEventEffect effect)
        {
            if (effect == null)
            {
                return;
            }

            ItemGameplayEffectTriggerBinderRegistry.TryBind(
                effect,
                BuildGameplayEffectBindingContext());
        }

        private void ApplyGameplayEventEffect(ItemGameplayEventEffect effect, Vector3 feedbackPosition)
        {
            if (effect == null)
            {
                return;
            }

            if (!CanTriggerGameplayEventEffect(effect))
            {
                return;
            }

            bool executed = ItemGameplayEffectExecutorRegistry.TryExecute(
                effect,
                BuildGameplayEffectExecutionContext(),
                feedbackPosition);

            if (executed)
            {
                CommitGameplayEventEffectTrigger(effect);
            }
        }

        private ItemGameplayEffectExecutionContext BuildGameplayEffectExecutionContext()
        {
            return new ItemGameplayEffectExecutionContext(
                playerInventory,
                playerStats,
                playerHealth,
                playerActiveItemController,
                TryAddTimedModifier,
                RaiseEffectFeedback);
        }

        private ItemGameplayEffectBindingContext BuildGameplayEffectBindingContext()
        {
            return new ItemGameplayEffectBindingContext(
                playerHealth,
                transform,
                IsOwnedByPlayer,
                ApplyGameplayEventEffect,
                RegisterEventEffectUnbindAction);
        }

        private void RegisterEventEffectUnbindAction(Action unbindAction)
        {
            if (unbindAction != null)
            {
                _eventUnbindActions.Add(unbindAction);
            }
        }

        private bool TryAddTimedModifier(ItemGameplayEventEffect effect)
        {
            if (playerStats == null)
            {
                return false;
            }

            bool hasStats = effect.StatModifiers != null && effect.StatModifiers.Count > 0;
            bool hasProjectiles = effect.ProjectileModifiers != null && effect.ProjectileModifiers.Count > 0;

            if (!hasStats && !hasProjectiles)
            {
                return false;
            }

            TimedEventModifierInstance instance = new TimedEventModifierInstance(effect.TimedEffectDuration);
            CopyStatModifiers(effect.StatModifiers, instance.StatModifiers);
            CopyProjectileModifiers(effect.ProjectileModifiers, instance.ProjectileModifiers);
            _timedEventModifiers.Add(instance);
            RebuildTimedEventModifiers();
            return true;
        }

        private void RebuildTimedEventModifiers()
        {
            _resolvedTimedEventStatModifiers.Clear();
            _resolvedTimedEventProjectileModifiers.Clear();

            for (int index = 0; index < _timedEventModifiers.Count; index++)
            {
                TimedEventModifierInstance instance = _timedEventModifiers[index];
                CopyStatModifiers(instance.StatModifiers, _resolvedTimedEventStatModifiers);
                CopyProjectileModifiers(instance.ProjectileModifiers, _resolvedTimedEventProjectileModifiers);
            }

            playerStats?.SetEventRuntimeModifiers(_resolvedTimedEventStatModifiers, _resolvedTimedEventProjectileModifiers);
        }

        private void ClearEventEffectBindings()
        {
            for (int index = 0; index < _eventUnbindActions.Count; index++)
            {
                _eventUnbindActions[index]?.Invoke();
            }

            _eventUnbindActions.Clear();
        }

        private bool IsOwnedByPlayer(Transform source)
        {
            return source != null && (source == transform || source.IsChildOf(transform));
        }

        private static string ResolveFeedbackLabel(ItemGameplayEventEffect effect, string fallback)
        {
            return effect != null
                ? effect.ResolveFeedbackLabel(fallback)
                : FloatingFeedbackLabelUtility.NormalizeEventLabel(string.Empty, fallback);
        }

        private void RaisePickupBanner(ItemData itemData)
        {
            if (itemData == null)
            {
                return;
            }

            itemData.BuildModifierStack(_pickupPreviewModifierStack);
            string statSummary = BuildPickupStatSummary(_pickupPreviewModifierStack);
            string gameplaySummary = BuildPickupGameplayEffectSummary(itemData.GameplayEventEffects);
            string economySummary = BuildPickupEconomyModifierSummary(itemData.ShopPriceModifiers, itemData.DoorCostModifiers);
            string rewardSummary = BuildPickupRoomRewardModifierSummary(itemData.RoomRewardModifiers);
            string modifierSummary = MergePickupSummary(statSummary, gameplaySummary);
            modifierSummary = MergePickupSummary(modifierSummary, economySummary);
            modifierSummary = MergePickupSummary(modifierSummary, rewardSummary);
            if (string.IsNullOrWhiteSpace(modifierSummary) && itemData.IsWeaponRelic)
            {
                string reserveLabel = itemData.WeaponProfile.InfiniteReserveAmmo
                    ? "INF"
                    : itemData.WeaponProfile.StartingReserveAmmo.ToString();
                string pelletLabel = itemData.WeaponProfile.ShotsPerTrigger > 1
                    ? $" | {itemData.WeaponProfile.ShotsPerTrigger} pellets"
                    : string.Empty;
                modifierSummary = $"{itemData.WeaponProfile.MagazineCapacity}/{reserveLabel} rounds | {itemData.WeaponProfile.ReloadDuration:0.#}s reload{pelletLabel}";
            }
            string flavorLine = ResolvePickupFlavorLine(itemData);
            string subtitle = string.IsNullOrWhiteSpace(modifierSummary)
                ? flavorLine
                : string.IsNullOrWhiteSpace(flavorLine)
                    ? modifierSummary
                    : $"{modifierSummary}\n{flavorLine}";

            GameplayFeedbackEvents.RaiseBannerFeedback(new BannerFeedbackRequest(
                itemData.DisplayName,
                subtitle,
                ResolvePickupAccentColor(itemData.Rarity),
                2.4f));
        }

        private static string BuildPickupStatSummary(ModifierStack modifierStack)
        {
            if (modifierStack == null)
            {
                return string.Empty;
            }

            StringBuilder builder = new();
            AppendStatSummary(builder, modifierStack.StatModifiers);
            AppendProjectileSummary(builder, modifierStack.ProjectileModifiers);
            return builder.ToString();
        }

        private static string BuildPickupGameplayEffectSummary(IReadOnlyList<ItemGameplayEventEffect> gameplayEventEffects)
        {
            if (gameplayEventEffects == null || gameplayEventEffects.Count == 0)
            {
                return string.Empty;
            }

            StringBuilder builder = new();

            for (int index = 0; index < gameplayEventEffects.Count; index++)
            {
                ItemGameplayEventEffect effect = gameplayEventEffects[index];
                string chunk = ResolveGameplayEffectSummaryChunk(effect);

                if (string.IsNullOrWhiteSpace(chunk))
                {
                    continue;
                }

                AppendSummaryChunk(builder, chunk);
            }

            return builder.ToString();
        }

        public int ResolveShopPriceDiscount(ShopCurrencyType currencyType)
        {
            BuildOwnedItemsBuffer();

            int totalDiscount = 0;

            for (int itemIndex = 0; itemIndex < _ownedItemsBuffer.Count; itemIndex++)
            {
                ItemData itemData = _ownedItemsBuffer[itemIndex];
                if (itemData == null || itemData.ShopPriceModifiers == null)
                {
                    continue;
                }

                for (int modifierIndex = 0; modifierIndex < itemData.ShopPriceModifiers.Count; modifierIndex++)
                {
                    ItemShopPriceModifier modifier = itemData.ShopPriceModifiers[modifierIndex];
                    if (modifier == null || !modifier.Supports(currencyType))
                    {
                        continue;
                    }

                    totalDiscount += modifier.FlatDiscount;
                }
            }

            return Mathf.Max(0, totalDiscount);
        }

        public int ResolveEffectiveShopPrice(int basePrice, ShopCurrencyType currencyType)
        {
            return Mathf.Max(0, Mathf.Max(0, basePrice) - ResolveShopPriceDiscount(currencyType));
        }

        public bool OwnsItem(ItemData itemData)
        {
            if (itemData == null)
            {
                return false;
            }

            if (itemData.IsWeaponRelic)
            {
                return playerWeaponLoadout != null && playerWeaponLoadout.OwnsWeaponItem(itemData);
            }

            if (playerInventory != null)
            {
                IReadOnlyList<ItemData> passiveItems = playerInventory.PassiveItems;
                for (int index = 0; index < passiveItems.Count; index++)
                {
                    ItemData ownedItem = passiveItems[index];
                    if (ownedItem == null)
                    {
                        continue;
                    }

                    if (ReferenceEquals(ownedItem, itemData) || ownedItem.ItemId == itemData.ItemId)
                    {
                        return true;
                    }
                }
            }

            return playerTrinketHolder != null
                && playerTrinketHolder.EquippedTrinket != null
                && (ReferenceEquals(playerTrinketHolder.EquippedTrinket, itemData)
                    || playerTrinketHolder.EquippedTrinket.ItemId == itemData.ItemId);
        }

        public int ResolveDoorKeyCostReduction(RoomType roomType)
        {
            BuildOwnedItemsBuffer();

            int totalReduction = 0;

            for (int itemIndex = 0; itemIndex < _ownedItemsBuffer.Count; itemIndex++)
            {
                ItemData itemData = _ownedItemsBuffer[itemIndex];
                if (itemData == null || itemData.DoorCostModifiers == null)
                {
                    continue;
                }

                for (int modifierIndex = 0; modifierIndex < itemData.DoorCostModifiers.Count; modifierIndex++)
                {
                    ItemDoorCostModifier modifier = itemData.DoorCostModifiers[modifierIndex];
                    if (modifier == null || !modifier.Supports(roomType))
                    {
                        continue;
                    }

                    totalReduction += modifier.KeyDiscount;
                }
            }

            return Mathf.Max(0, totalReduction);
        }

        public float ResolveDoorHealthCostReduction(RoomType roomType)
        {
            BuildOwnedItemsBuffer();

            float totalReduction = 0f;

            for (int itemIndex = 0; itemIndex < _ownedItemsBuffer.Count; itemIndex++)
            {
                ItemData itemData = _ownedItemsBuffer[itemIndex];
                if (itemData == null || itemData.DoorCostModifiers == null)
                {
                    continue;
                }

                for (int modifierIndex = 0; modifierIndex < itemData.DoorCostModifiers.Count; modifierIndex++)
                {
                    ItemDoorCostModifier modifier = itemData.DoorCostModifiers[modifierIndex];
                    if (modifier == null || !modifier.Supports(roomType))
                    {
                        continue;
                    }

                    totalReduction += modifier.HealthDiscount;
                }
            }

            return Mathf.Max(0f, totalReduction);
        }

        public int ResolveEffectiveDoorKeyCost(int baseCost, RoomType roomType)
        {
            return Mathf.Max(0, Mathf.Max(0, baseCost) - ResolveDoorKeyCostReduction(roomType));
        }

        public float ResolveEffectiveDoorHealthCost(float baseCost, RoomType roomType)
        {
            return Mathf.Max(0f, Mathf.Max(0f, baseCost) - ResolveDoorHealthCostReduction(roomType));
        }

        public bool TryGetRoomRewardBonus(
            RoomType roomType,
            bool allowNonCombatResolve,
            out int bonusRewardSelections,
            out int bonusItemRolls,
            out string title,
            out string subtitle,
            out Color accentColor)
        {
            bonusRewardSelections = 0;
            bonusItemRolls = 0;
            title = string.Empty;
            subtitle = string.Empty;
            accentColor = Color.white;

            BuildOwnedItemsBuffer();

            ItemData strongestContributor = null;
            int contributorCount = 0;

            for (int itemIndex = 0; itemIndex < _ownedItemsBuffer.Count; itemIndex++)
            {
                ItemData itemData = _ownedItemsBuffer[itemIndex];
                if (itemData == null || itemData.RoomRewardModifiers == null)
                {
                    continue;
                }

                bool itemContributed = false;

                for (int modifierIndex = 0; modifierIndex < itemData.RoomRewardModifiers.Count; modifierIndex++)
                {
                    ItemRoomRewardModifier modifier = itemData.RoomRewardModifiers[modifierIndex];
                    if (modifier == null || !modifier.Supports(roomType, allowNonCombatResolve))
                    {
                        continue;
                    }

                    int rewardSelections = modifier.BonusRewardSelections;
                    int itemRolls = modifier.BonusItemRolls;

                    if (rewardSelections <= 0 && itemRolls <= 0)
                    {
                        continue;
                    }

                    bonusRewardSelections += rewardSelections;
                    bonusItemRolls += itemRolls;
                    itemContributed = true;
                }

                if (!itemContributed)
                {
                    continue;
                }

                contributorCount++;

                if (strongestContributor == null || itemData.Rarity > strongestContributor.Rarity)
                {
                    strongestContributor = itemData;
                }
            }

            if (bonusRewardSelections <= 0 && bonusItemRolls <= 0)
            {
                return false;
            }

            accentColor = strongestContributor != null
                ? ResolvePickupAccentColor(strongestContributor.Rarity)
                : Color.white;
            title = contributorCount == 1 && strongestContributor != null
                ? $"{strongestContributor.DisplayName} CACHE"
                : "ITEM CACHE BONUS";
            subtitle = BuildRoomRewardBonusSubtitle(
                roomType,
                bonusRewardSelections,
                bonusItemRolls,
                allowNonCombatResolve);
            return true;
        }

        private static string ResolveGameplayEffectSummaryChunk(ItemGameplayEventEffect effect)
        {
            if (effect == null)
            {
                return string.Empty;
            }

            string triggerLabel = effect.TriggerType switch
            {
                GameplayEventTriggerType.PlayerDamaged => "ON HIT",
                GameplayEventTriggerType.EnemyKilled => "ON KILL",
                GameplayEventTriggerType.ProjectileFired => "ON SHOT",
                GameplayEventTriggerType.RoomCleared => "ON CLEAR",
                _ => string.Empty
            };

            string effectLabel = effect.EffectType switch
            {
                ItemGameplayEventEffectType.AddCoins when effect.CoinAmount > 0 => $"+{effect.CoinAmount} COIN",
                ItemGameplayEventEffectType.AddKeys when effect.ResourceAmount > 0 => $"+{effect.ResourceAmount} KEY",
                ItemGameplayEventEffectType.AddBombs when effect.ResourceAmount > 0 => $"+{effect.ResourceAmount} BOMB",
                ItemGameplayEventEffectType.RestoreHealth when effect.HealAmount > 0f => $"+{effect.HealAmount:0.#} HP",
                ItemGameplayEventEffectType.AddActiveCharge when effect.ActiveChargeAmount > 0 => $"+{effect.ActiveChargeAmount} CHARGE",
                ItemGameplayEventEffectType.GrantInvulnerability when effect.InvulnerabilityDuration > 0f => $"{effect.InvulnerabilityDuration:0.#}S SHIELD",
                ItemGameplayEventEffectType.ApplyTimedBuff => "SURGE",
                _ => string.Empty
            };

            if (string.IsNullOrWhiteSpace(triggerLabel))
            {
                return effectLabel;
            }

            if (string.IsNullOrWhiteSpace(effectLabel))
            {
                string triggerDescriptor = ResolveGameplayEffectProcDescriptor(effect);
                return string.IsNullOrWhiteSpace(triggerDescriptor)
                    ? triggerLabel
                    : $"{triggerLabel} {triggerDescriptor}";
            }

            string summary = $"{triggerLabel} {effectLabel}";
            string procDescriptor = ResolveGameplayEffectProcDescriptor(effect);
            return string.IsNullOrWhiteSpace(procDescriptor)
                ? summary
                : $"{triggerLabel} {procDescriptor} {effectLabel}";
        }

        private bool CanTriggerGameplayEventEffect(ItemGameplayEventEffect effect)
        {
            if (effect == null)
            {
                return false;
            }

            if (_eventEffectNextTriggerTimes.TryGetValue(effect, out float nextTriggerTime)
                && Time.time < nextTriggerTime)
            {
                return false;
            }

            float triggerChance = effect.ResolveEffectiveTriggerChance(playerStats != null ? playerStats.CurrentLuck : 0f);
            if (triggerChance <= 0f)
            {
                return false;
            }

            return triggerChance >= 0.999f || UnityEngine.Random.value <= triggerChance;
        }

        private void CommitGameplayEventEffectTrigger(ItemGameplayEventEffect effect)
        {
            if (effect == null)
            {
                return;
            }

            float cooldown = effect.InternalCooldown;
            if (cooldown <= 0.01f)
            {
                return;
            }

            _eventEffectNextTriggerTimes[effect] = Time.time + cooldown;
        }

        private static string ResolveGameplayEffectProcDescriptor(ItemGameplayEventEffect effect)
        {
            if (effect == null)
            {
                return string.Empty;
            }

            StringBuilder builder = new();
            float triggerChance = effect.TriggerChance;
            if (triggerChance < 0.999f)
            {
                builder.Append(Mathf.RoundToInt(triggerChance * 100f)).Append('%');

                if (effect.LuckBonusChancePerPoint > 0.0001f)
                {
                    builder.Append("+LCK");
                }
            }

            float cooldown = effect.InternalCooldown;
            if (cooldown > 0.01f)
            {
                if (builder.Length > 0)
                {
                    builder.Append(" / ");
                }

                builder.Append(cooldown.ToString("0.#")).Append("S CD");
            }

            return builder.ToString();
        }

        private static string BuildPickupRoomRewardModifierSummary(IReadOnlyList<ItemRoomRewardModifier> roomRewardModifiers)
        {
            if (roomRewardModifiers == null || roomRewardModifiers.Count == 0)
            {
                return string.Empty;
            }

            StringBuilder builder = new();

            for (int index = 0; index < roomRewardModifiers.Count; index++)
            {
                string chunk = ResolveRoomRewardModifierSummaryChunk(roomRewardModifiers[index]);
                if (string.IsNullOrWhiteSpace(chunk))
                {
                    continue;
                }

                AppendSummaryChunk(builder, chunk);
            }

            return builder.ToString();
        }

        private static string BuildPickupEconomyModifierSummary(
            IReadOnlyList<ItemShopPriceModifier> shopPriceModifiers,
            IReadOnlyList<ItemDoorCostModifier> doorCostModifiers)
        {
            StringBuilder builder = new();

            if (shopPriceModifiers != null)
            {
                for (int index = 0; index < shopPriceModifiers.Count; index++)
                {
                    string chunk = ResolveShopPriceModifierSummaryChunk(shopPriceModifiers[index]);
                    if (string.IsNullOrWhiteSpace(chunk))
                    {
                        continue;
                    }

                    AppendSummaryChunk(builder, chunk);
                }
            }

            if (doorCostModifiers != null)
            {
                for (int index = 0; index < doorCostModifiers.Count; index++)
                {
                    string chunk = ResolveDoorCostModifierSummaryChunk(doorCostModifiers[index]);
                    if (string.IsNullOrWhiteSpace(chunk))
                    {
                        continue;
                    }

                    AppendSummaryChunk(builder, chunk);
                }
            }

            return builder.ToString();
        }

        private static string ResolveRoomRewardModifierSummaryChunk(ItemRoomRewardModifier modifier)
        {
            if (modifier == null)
            {
                return string.Empty;
            }

            int bonusRewardSelections = modifier.BonusRewardSelections;
            int bonusItemRolls = modifier.BonusItemRolls;

            if (bonusRewardSelections <= 0 && bonusItemRolls <= 0)
            {
                return string.Empty;
            }

            string roomLabel = ResolveRoomRewardSummaryLabel(modifier.SupportedRoomTypes, modifier.AllowOnNonCombatResolve);
            string rewardLabel = bonusRewardSelections > 0 ? $"+{bonusRewardSelections} REWARD" : string.Empty;
            string itemLabel = bonusItemRolls > 0 ? $"+{bonusItemRolls} ITEM" : string.Empty;

            if (!string.IsNullOrWhiteSpace(rewardLabel) && !string.IsNullOrWhiteSpace(itemLabel))
            {
                return $"{roomLabel} {rewardLabel} / {itemLabel}";
            }

            return $"{roomLabel} {rewardLabel}{itemLabel}".Trim();
        }

        private static string ResolveShopPriceModifierSummaryChunk(ItemShopPriceModifier modifier)
        {
            if (modifier == null || modifier.FlatDiscount <= 0)
            {
                return string.Empty;
            }

            return $"{ResolveCurrencyLabel(modifier.CurrencyType)} SHOP -{modifier.FlatDiscount}";
        }

        private static string ResolveDoorCostModifierSummaryChunk(ItemDoorCostModifier modifier)
        {
            if (modifier == null)
            {
                return string.Empty;
            }

            string roomLabel = modifier.SupportedRoomTypes != null && modifier.SupportedRoomTypes.Count == 1
                ? $"{ResolveRoomLabel(modifier.SupportedRoomTypes[0])} DOOR"
                : "DOOR";
            string keySegment = modifier.KeyDiscount > 0 ? $"-{modifier.KeyDiscount} KEY" : string.Empty;
            string healthSegment = modifier.HealthDiscount > 0f ? $"-{modifier.HealthDiscount:0.#} HP" : string.Empty;

            if (string.IsNullOrWhiteSpace(keySegment) && string.IsNullOrWhiteSpace(healthSegment))
            {
                return string.Empty;
            }

            if (!string.IsNullOrWhiteSpace(keySegment) && !string.IsNullOrWhiteSpace(healthSegment))
            {
                return $"{roomLabel} {keySegment} / {healthSegment}";
            }

            return $"{roomLabel} {keySegment}{healthSegment}".Trim();
        }

        private static string MergePickupSummary(string primarySummary, string secondarySummary)
        {
            if (string.IsNullOrWhiteSpace(primarySummary))
            {
                return secondarySummary ?? string.Empty;
            }

            if (string.IsNullOrWhiteSpace(secondarySummary))
            {
                return primarySummary;
            }

            return $"{primarySummary}\n{secondarySummary}";
        }

        private static string BuildRoomRewardBonusSubtitle(
            RoomType roomType,
            int bonusRewardSelections,
            int bonusItemRolls,
            bool allowNonCombatResolve)
        {
            string roomLabel = allowNonCombatResolve
                ? $"{ResolveRoomLabel(roomType)} CACHE"
                : $"{ResolveRoomLabel(roomType)} CLEAR";
            string rewardSegment = bonusRewardSelections > 0 ? $"+REWARD {bonusRewardSelections}" : string.Empty;
            string itemSegment = bonusItemRolls > 0 ? $"+ITEM {bonusItemRolls}" : string.Empty;

            if (!string.IsNullOrWhiteSpace(rewardSegment) && !string.IsNullOrWhiteSpace(itemSegment))
            {
                return $"{roomLabel} / {rewardSegment} / {itemSegment}";
            }

            if (!string.IsNullOrWhiteSpace(rewardSegment))
            {
                return $"{roomLabel} / {rewardSegment}";
            }

            return $"{roomLabel} / {itemSegment}";
        }

        private static string ResolveRoomRewardSummaryLabel(IReadOnlyList<RoomType> supportedRoomTypes, bool allowOnNonCombatResolve)
        {
            if (supportedRoomTypes != null && supportedRoomTypes.Count == 1)
            {
                return ResolveRoomLabel(supportedRoomTypes[0]);
            }

            return allowOnNonCombatResolve ? "CACHE" : "CLEAR";
        }

        private static string ResolveRoomLabel(RoomType roomType)
        {
            return roomType switch
            {
                RoomType.Boss => "BOSS",
                RoomType.MiniBoss => "ELITE",
                RoomType.Secret => "SECRET",
                RoomType.Challenge => "CHALLENGE",
                RoomType.Curse => "CURSE",
                RoomType.Trap => "TRAP",
                RoomType.Shop => "SHOP",
                RoomType.Treasure => "TREASURE",
                _ => "CLEAR"
            };
        }

        private static string ResolveCurrencyLabel(ShopCurrencyType currencyType)
        {
            return currencyType switch
            {
                ShopCurrencyType.Keys => "KEY",
                ShopCurrencyType.Bombs => "BOMB",
                _ => "COIN"
            };
        }

        private static void AppendStatSummary(StringBuilder builder, IReadOnlyList<StatModifier> statModifiers)
        {
            if (statModifiers == null)
            {
                return;
            }

            for (int index = 0; index < statModifiers.Count; index++)
            {
                StatModifier modifier = statModifiers[index];
                string label = modifier.StatType switch
                {
                    PlayerStatType.Damage => "공격력",
                    PlayerStatType.FireInterval => "연사",
                    PlayerStatType.MoveSpeed => "이동속도",
                    PlayerStatType.ProjectileSpeed => "탄속",
                    PlayerStatType.Range => "사거리",
                    PlayerStatType.Luck => "행운",
                    PlayerStatType.MaxHealth => "최대 체력",
                    PlayerStatType.ProjectileCount => "탄 수",
                    PlayerStatType.Knockback => "넉백",
                    _ => string.Empty
                };

                if (string.IsNullOrWhiteSpace(label))
                {
                    continue;
                }

                float displayValue = modifier.StatType == PlayerStatType.FireInterval
                    ? -modifier.Value
                    : modifier.Value;

                if (Mathf.Abs(displayValue) <= 0.0001f)
                {
                    continue;
                }

                AppendSummaryChunk(builder, $"{(displayValue >= 0f ? "+" : string.Empty)}{displayValue:0.##} {label}");
            }
        }

        private static void AppendProjectileSummary(StringBuilder builder, IReadOnlyList<ProjectileModifier> projectileModifiers)
        {
            if (projectileModifiers == null)
            {
                return;
            }

            for (int index = 0; index < projectileModifiers.Count; index++)
            {
                string label = projectileModifiers[index].ModifierType switch
                {
                    ProjectileModifierType.Pierce => "관통",
                    ProjectileModifierType.Homing => "유도",
                    ProjectileModifierType.MultiShot => "다중 발사",
                    ProjectileModifierType.Explode => "폭발 탄환",
                    ProjectileModifierType.Laser => "레이저 변환",
                    ProjectileModifierType.Split => "분열 탄환",
                    ProjectileModifierType.Bounce => "반사 탄환",
                    ProjectileModifierType.Orbit => "오비탈",
                    ProjectileModifierType.Shield => "보호막",
                    ProjectileModifierType.Lifesteal => "흡혈",
                    ProjectileModifierType.Scale => "탄 크기",
                    ProjectileModifierType.Speed => "탄속 보정",
                    ProjectileModifierType.Lifetime => "지속시간 보정",
                    _ => string.Empty
                };

                if (string.IsNullOrWhiteSpace(label))
                {
                    continue;
                }

                AppendSummaryChunk(builder, label);
            }
        }

        private static void AppendSummaryChunk(StringBuilder builder, string chunk)
        {
            if (string.IsNullOrWhiteSpace(chunk))
            {
                return;
            }

            if (builder.Length > 0)
            {
                builder.Append("  ·  ");
            }

            builder.Append(chunk);
        }

        private static string ResolvePickupFlavorLine(ItemData itemData)
        {
            if (itemData == null)
            {
                return string.Empty;
            }

            if (!string.IsNullOrWhiteSpace(itemData.Description))
            {
                return itemData.Description.Trim();
            }

            if (!string.IsNullOrWhiteSpace(itemData.FlavorText))
            {
                return itemData.FlavorText.Trim();
            }

            if (itemData.IsWeaponRelic && !string.IsNullOrWhiteSpace(itemData.WeaponMotif))
            {
                return itemData.WeaponMotif.Trim();
            }

            return itemData.ItemCategory switch
            {
                ItemCategory.Damage => "손에 쥐는 순간, 위험한 확신이 든다.",
                ItemCategory.FireRate => "이제 손이 생각보다 더 빠르게 반응한다.",
                ItemCategory.Movement => "발끝이 가벼워졌다.",
                ItemCategory.Projectile => "탄도부터 달라질 예감이다.",
                ItemCategory.Defense => "조금은 덜 아플지도 모른다.",
                ItemCategory.Economy => "동전 냄새가 진하게 밴 물건이다.",
                ItemCategory.Utility => "분명 쓸모는 있는데, 어디에 쓸까?",
                ItemCategory.Summon => "혼자라는 느낌이 조금 옅어진다.",
                ItemCategory.Orbital => "주변을 맴도는 건 대개 좋은 징조가 아니다.",
                ItemCategory.Laser => "이건 누가 봐도 평범한 눈물이 아니다.",
                ItemCategory.Bomb => "좋은 소식이다. 크게 터질 것이다.",
                ItemCategory.Luck => "이쯤 되면 우연도 실력이다.",
                _ => "누구의 물건이었을까?"
            };
        }

        private static Color ResolvePickupAccentColor(ItemRarity rarity)
        {
            return rarity switch
            {
                ItemRarity.Common => new Color(0.72f, 0.85f, 1f, 1f),
                ItemRarity.Uncommon => new Color(0.52f, 1f, 0.68f, 1f),
                ItemRarity.Rare => new Color(1f, 0.84f, 0.42f, 1f),
                ItemRarity.Legendary => new Color(1f, 0.48f, 0.82f, 1f),
                ItemRarity.Relic => new Color(1f, 0.92f, 0.6f, 1f),
                ItemRarity.Boss => new Color(1f, 0.4f, 0.4f, 1f),
                _ => Color.white
            };
        }

        private static void RaiseEffectFeedback(Vector3 position, string label, Color color)
        {
            GameplayFeedbackEvents.RaiseFloatingFeedback(new FloatingFeedbackRequest(
                position + Vector3.up * 0.8f,
                label,
                color,
                0.68f,
                0.72f,
                1.08f,
                visualProfile: FloatingFeedbackVisualProfile.EventLabel));
        }

        private static void CopyStatModifiers(IReadOnlyList<StatModifier> source, List<StatModifier> destination)
        {
            if (source == null)
            {
                return;
            }

            for (int index = 0; index < source.Count; index++)
            {
                destination.Add(source[index]);
            }
        }

        private static void CopyProjectileModifiers(IReadOnlyList<ProjectileModifier> source, List<ProjectileModifier> destination)
        {
            if (source == null)
            {
                return;
            }

            for (int index = 0; index < source.Count; index++)
            {
                destination.Add(source[index]);
            }
        }

        private sealed class TimedEventModifierInstance
        {
            public TimedEventModifierInstance(float duration)
            {
                RemainingDuration = duration;
            }

            public float RemainingDuration { get; set; }
            public List<StatModifier> StatModifiers { get; } = new();
            public List<ProjectileModifier> ProjectileModifiers { get; } = new();
        }
    }
}
