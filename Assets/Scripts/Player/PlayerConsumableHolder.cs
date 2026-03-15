using System;
using System.Collections.Generic;
using CuteIssac.Common.Stats;
using CuteIssac.Data.Item;
using UnityEngine;

namespace CuteIssac.Player
{
    /// <summary>
    /// Stores a single consumable item slot and applies consumable effects when used.
    /// Passive inventory remains separate so pickup ownership and one-shot item logic do not get mixed.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerConsumableHolder : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerInventory playerInventory;
        [SerializeField] private PlayerHealth playerHealth;
        [SerializeField] private PlayerStats playerStats;

        [Header("Starting Slot")]
        [SerializeField] private ConsumableItemData startingConsumable;

        public event Action<PlayerConsumableSlotState> ConsumableStateChanged;

        public ConsumableItemData HeldConsumable { get; private set; }
        public ConsumableItemData TimedEffectSourceConsumable => _timedEffectSourceConsumable;
        public float TimedEffectRemainingSeconds => Mathf.Max(0f, _timedEffectRemaining);

        private readonly List<StatModifier> _runtimeStatModifiers = new();
        private readonly List<ProjectileModifier> _runtimeProjectileModifiers = new();
        private float _timedEffectRemaining;
        private float _timedEffectDuration;
        private ConsumableItemData _timedEffectSourceConsumable;

        private void Awake()
        {
            ResolveDependencies();
            HeldConsumable = startingConsumable;
            NotifyConsumableStateChanged();
        }

        private void Update()
        {
            if (_timedEffectRemaining <= 0f)
            {
                return;
            }

            _timedEffectRemaining = Mathf.Max(0f, _timedEffectRemaining - Time.deltaTime);

            if (_timedEffectRemaining <= 0f)
            {
                ClearTimedEffect();
            }
            else
            {
                NotifyConsumableStateChanged();
            }
        }

        public bool TryAcquireConsumable(ConsumableItemData consumableItemData)
        {
            ResolveDependencies();

            if (consumableItemData == null || !consumableItemData.HasAnyEffect)
            {
                return false;
            }

            if (consumableItemData.PickupMode == ConsumablePickupMode.UseImmediately)
            {
                return TryApplyConsumable(consumableItemData);
            }

            if (HeldConsumable != null)
            {
                return false;
            }

            HeldConsumable = consumableItemData;
            NotifyConsumableStateChanged();
            return true;
        }

        public bool TryUseHeldConsumable()
        {
            if (HeldConsumable == null)
            {
                return false;
            }

            ConsumableItemData consumableToUse = HeldConsumable;

            if (!TryApplyConsumable(consumableToUse))
            {
                return false;
            }

            HeldConsumable = null;
            NotifyConsumableStateChanged();
            return true;
        }

        public void RestoreForRunResume(ConsumableItemData consumableItemData)
        {
            ResolveDependencies();
            ClearTimedEffect();
            HeldConsumable = consumableItemData;
            NotifyConsumableStateChanged();
        }

        public bool TryRestoreTimedEffect(ConsumableItemData consumableItemData, float remainingSeconds)
        {
            ResolveDependencies();

            if (playerStats == null || consumableItemData == null || !consumableItemData.HasTimedEffect || remainingSeconds <= 0f)
            {
                return false;
            }

            _timedEffectSourceConsumable = consumableItemData;
            ApplyTimedEffect(consumableItemData, remainingSeconds);
            return true;
        }

        public PlayerConsumableSlotState BuildSlotState()
        {
            bool hasTimedEffect = _timedEffectRemaining > 0f;
            float normalizedRemaining = hasTimedEffect && _timedEffectDuration > 0f
                ? _timedEffectRemaining / _timedEffectDuration
                : 0f;

            if (HeldConsumable != null)
            {
                return new PlayerConsumableSlotState(
                    true,
                    HeldConsumable.DisplayName,
                    HeldConsumable.Icon,
                    hasTimedEffect,
                    normalizedRemaining);
            }

            return new PlayerConsumableSlotState(
                false,
                string.Empty,
                null,
                hasTimedEffect,
                normalizedRemaining);
        }

        private bool TryApplyConsumable(ConsumableItemData consumableItemData)
        {
            ResolveDependencies();

            bool appliedAnyEffect = false;

            if (consumableItemData.HealAmount > 0f && playerHealth != null)
            {
                appliedAnyEffect |= playerHealth.RestoreHealth(consumableItemData.HealAmount);
            }

            if (consumableItemData.CoinGain > 0 && playerInventory != null)
            {
                playerInventory.AddCoins(consumableItemData.CoinGain);
                appliedAnyEffect = true;
            }

            if (consumableItemData.KeyGain > 0 && playerInventory != null)
            {
                playerInventory.AddKeys(consumableItemData.KeyGain);
                appliedAnyEffect = true;
            }

            if (consumableItemData.BombGain > 0 && playerInventory != null)
            {
                playerInventory.AddBombs(consumableItemData.BombGain);
                appliedAnyEffect = true;
            }

            if (consumableItemData.HasTimedEffect && playerStats != null)
            {
                _timedEffectSourceConsumable = consumableItemData;
                ApplyTimedEffect(consumableItemData, consumableItemData.TemporaryEffectDuration);
                appliedAnyEffect = true;
            }

            return appliedAnyEffect;
        }

        private void ApplyTimedEffect(ConsumableItemData consumableItemData, float remainingSeconds)
        {
            _runtimeStatModifiers.Clear();
            _runtimeProjectileModifiers.Clear();

            CopyStatModifiers(consumableItemData.TemporaryStatModifiers, _runtimeStatModifiers);
            CopyProjectileModifiers(consumableItemData.TemporaryProjectileModifiers, _runtimeProjectileModifiers);

            _timedEffectDuration = consumableItemData.TemporaryEffectDuration;
            _timedEffectRemaining = Mathf.Clamp(remainingSeconds, 0f, consumableItemData.TemporaryEffectDuration);
            playerStats.SetConsumableRuntimeModifiers(_runtimeStatModifiers, _runtimeProjectileModifiers);
            NotifyConsumableStateChanged();
        }

        private void ClearTimedEffect()
        {
            _runtimeStatModifiers.Clear();
            _runtimeProjectileModifiers.Clear();
            _timedEffectDuration = 0f;
            _timedEffectRemaining = 0f;
            _timedEffectSourceConsumable = null;

            if (playerStats != null)
            {
                playerStats.SetConsumableRuntimeModifiers(_runtimeStatModifiers, _runtimeProjectileModifiers);
            }

            NotifyConsumableStateChanged();
        }

        private void NotifyConsumableStateChanged()
        {
            ConsumableStateChanged?.Invoke(BuildSlotState());
        }

        private void ResolveDependencies()
        {
            if (playerInventory == null)
            {
                playerInventory = GetComponent<PlayerInventory>();
            }

            if (playerHealth == null)
            {
                playerHealth = GetComponent<PlayerHealth>();
            }

            if (playerStats == null)
            {
                playerStats = GetComponent<PlayerStats>();
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

        private void Reset()
        {
            ResolveDependencies();
        }

        private void OnValidate()
        {
            ResolveDependencies();
        }
    }
}
