using System;
using System.Collections.Generic;
using System.Text;
using CuteIssac.Common.Stats;
using CuteIssac.Core.Feedback;
using CuteIssac.Core.Gameplay;
using CuteIssac.Core.Run;
using CuteIssac.Data.Item;
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
        [SerializeField] private RunItemPoolService runItemPoolService;

        [Header("Debug")]
        [SerializeField] private ItemData debugPickupItem;

        public event Action<ItemData> PassiveItemAcquired;

        private readonly List<Action> _eventUnbindActions = new();
        private readonly List<TimedEventModifierInstance> _timedEventModifiers = new();
        private readonly List<StatModifier> _resolvedTimedEventStatModifiers = new();
        private readonly List<ProjectileModifier> _resolvedTimedEventProjectileModifiers = new();
        private readonly ModifierStack _pickupPreviewModifierStack = new();
        private readonly List<ItemData> _ownedItemsBuffer = new();

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
            runItemPoolService?.SyncOwnedItems(playerInventory != null ? playerInventory.PassiveItems : null);
            RebuildEventEffectBindings();
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
            if (playerInventory != null)
            {
                playerInventory.InventoryChanged -= HandleInventoryChanged;
            }

            if (playerTrinketHolder != null)
            {
                playerTrinketHolder.TrinketChanged -= HandleTrinketChanged;
            }

            ClearEventEffectBindings();
        }

        public bool AcquirePassiveItem(ItemData itemData)
        {
            ResolveDependencies();

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
            runItemPoolService?.SyncOwnedItems(playerInventory != null ? playerInventory.PassiveItems : null);
            RebuildEventEffectBindings();
        }

        private void HandleTrinketChanged()
        {
            RecalculateStats();
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

            if (playerInventory != null)
            {
                IReadOnlyList<ItemData> passiveItems = playerInventory.PassiveItems;

                for (int index = 0; index < passiveItems.Count; index++)
                {
                    ItemData itemData = passiveItems[index];

                    if (itemData != null && !_ownedItemsBuffer.Contains(itemData))
                    {
                        _ownedItemsBuffer.Add(itemData);
                    }
                }
            }

            if (playerTrinketHolder != null && playerTrinketHolder.EquippedTrinket != null && !_ownedItemsBuffer.Contains(playerTrinketHolder.EquippedTrinket))
            {
                _ownedItemsBuffer.Add(playerTrinketHolder.EquippedTrinket);
            }
        }

        private void BindItemGameplayEffect(ItemGameplayEventEffect effect)
        {
            switch (effect.TriggerType)
            {
                case GameplayEventTriggerType.PlayerDamaged:
                {
                    void Handler(PlayerDamagedSignal signal)
                    {
                        if (signal.PlayerHealth != playerHealth)
                        {
                            return;
                        }

                        ApplyGameplayEventEffect(effect, signal.Position);
                    }

                    GameplayRuntimeEvents.PlayerDamaged += Handler;
                    _eventUnbindActions.Add(() => GameplayRuntimeEvents.PlayerDamaged -= Handler);
                    break;
                }

                case GameplayEventTriggerType.EnemyKilled:
                {
                    void Handler(EnemyKilledSignal signal)
                    {
                        if (effect.RequirePlayerSource && !IsOwnedByPlayer(signal.Killer))
                        {
                            return;
                        }

                        ApplyGameplayEventEffect(effect, signal.Position);
                    }

                    GameplayRuntimeEvents.EnemyKilled += Handler;
                    _eventUnbindActions.Add(() => GameplayRuntimeEvents.EnemyKilled -= Handler);
                    break;
                }

                case GameplayEventTriggerType.ProjectileFired:
                {
                    void Handler(ProjectileFiredSignal signal)
                    {
                        if (effect.RequirePlayerSource && !IsOwnedByPlayer(signal.Source))
                        {
                            return;
                        }

                        ApplyGameplayEventEffect(effect, signal.Origin);
                    }

                    GameplayRuntimeEvents.ProjectileFired += Handler;
                    _eventUnbindActions.Add(() => GameplayRuntimeEvents.ProjectileFired -= Handler);
                    break;
                }

                case GameplayEventTriggerType.RoomCleared:
                {
                    void Handler(RoomClearSignal signal)
                    {
                        if (effect.RequireCombatEncounter && !signal.HadCombatEncounter)
                        {
                            return;
                        }

                        Vector3 feedbackPosition = signal.Room != null
                            ? signal.Room.CameraFocusPosition
                            : transform.position;
                        ApplyGameplayEventEffect(effect, feedbackPosition);
                    }

                    GameplayRuntimeEvents.RoomCleared += Handler;
                    _eventUnbindActions.Add(() => GameplayRuntimeEvents.RoomCleared -= Handler);
                    break;
                }
            }
        }

        private void ApplyGameplayEventEffect(ItemGameplayEventEffect effect, Vector3 feedbackPosition)
        {
            switch (effect.EffectType)
            {
                case ItemGameplayEventEffectType.AddCoins:
                    if (playerInventory == null || effect.CoinAmount <= 0)
                    {
                        return;
                    }

                    playerInventory.AddCoins(effect.CoinAmount);
                    RaiseEffectFeedback(feedbackPosition, ResolveFeedbackLabel(effect, $"+{effect.CoinAmount}C"), new Color(1f, 0.92f, 0.4f, 1f));
                    break;

                case ItemGameplayEventEffectType.ApplyTimedBuff:
                    if (!TryAddTimedModifier(effect))
                    {
                        return;
                    }

                    RaiseEffectFeedback(feedbackPosition, ResolveFeedbackLabel(effect, "SURGE"), new Color(0.48f, 0.9f, 1f, 1f));
                    break;
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
            string flavorLine = ResolvePickupFlavorLine(itemData);
            string subtitle = string.IsNullOrWhiteSpace(statSummary)
                ? flavorLine
                : string.IsNullOrWhiteSpace(flavorLine)
                    ? statSummary
                    : $"{statSummary}\n{flavorLine}";

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
