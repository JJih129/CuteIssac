using System;
using System.Collections.Generic;
using CuteIssac.Common.Input;
using CuteIssac.Common.Stats;
using CuteIssac.Core.Gameplay;
using CuteIssac.Data.Item;
using CuteIssac.Enemy;
using UnityEngine;

namespace CuteIssac.Player
{
    /// <summary>
    /// Owns the currently equipped active item, charge progress, use requests, and temporary runtime buffs.
    /// UI and pickups talk to this component instead of touching player combat/stats directly.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerActiveItemController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerHealth playerHealth;
        [SerializeField] private PlayerInventory playerInventory;
        [SerializeField] private PlayerStats playerStats;
        [SerializeField] private PlayerBombController playerBombController;
        [SerializeField] private PlayerMovement playerMovement;
        [SerializeField] private MonoBehaviour inputReaderSource;

        [Header("Starting Slot")]
        [SerializeField] private ActiveItemData startingActiveItem;

        public event Action<PlayerActiveItemSlotState> ActiveItemStateChanged;

        public ActiveItemData EquippedItem { get; private set; }
        public ActiveItemData TimedEffectSourceItem => _timedEffectSourceItem;
        public float TimedEffectRemainingSeconds => Mathf.Max(0f, _timedEffectRemaining);

        private readonly List<StatModifier> _runtimeStatModifiers = new();
        private readonly List<ProjectileModifier> _runtimeProjectileModifiers = new();
        private IPlayerInputReader _inputReader;
        private int _currentCharge;
        private float _timedEffectRemaining;
        private float _timedEffectDuration;
        private ActiveItemData _timedEffectSourceItem;

        public bool HasEquippedItem => EquippedItem != null;

        private void Awake()
        {
            ResolveDependencies();

            if (startingActiveItem != null)
            {
                EquipActiveItem(startingActiveItem);
            }
            else
            {
                NotifyStateChanged();
            }
        }

        private void OnEnable()
        {
            GameplayRuntimeEvents.RoomCleared += HandleRoomCleared;
            GameplayRuntimeEvents.EnemyDied += HandleEnemyDied;
        }

        private void OnDisable()
        {
            GameplayRuntimeEvents.RoomCleared -= HandleRoomCleared;
            GameplayRuntimeEvents.EnemyDied -= HandleEnemyDied;
        }

        private void Update()
        {
            if (_timedEffectRemaining > 0f)
            {
                _timedEffectRemaining = Mathf.Max(0f, _timedEffectRemaining - Time.deltaTime);

                if (_timedEffectRemaining <= 0f)
                {
                    ClearTimedEffect();
                }
                else
                {
                    NotifyStateChanged();
                }
            }

            if (_inputReader != null && _inputReader.ReadState().ActiveItemPressed)
            {
                TryUseEquippedItem();
            }
        }

        public void EquipActiveItem(ActiveItemData activeItemData)
        {
            EquippedItem = activeItemData;
            _currentCharge = activeItemData != null && activeItemData.StartFullyCharged
                ? activeItemData.MaxCharge
                : 0;
            NotifyStateChanged();
        }

        public void RestoreForRunResume(ActiveItemData activeItemData, int currentCharge)
        {
            EquippedItem = activeItemData;
            _currentCharge = activeItemData != null
                ? Mathf.Clamp(currentCharge, 0, activeItemData.MaxCharge)
                : 0;
            ClearTimedEffect();
            NotifyStateChanged();
        }

        public bool TryUseEquippedItem()
        {
            if (EquippedItem == null || EquippedItem.Effect == null)
            {
                return false;
            }

            if (_currentCharge < EquippedItem.MaxCharge)
            {
                return false;
            }

            if (!EquippedItem.Effect.TryApply(this))
            {
                return false;
            }

            _currentCharge = 0;
            NotifyStateChanged();
            return true;
        }

        public bool TryRestoreHealth(float amount)
        {
            return playerHealth != null && playerHealth.RestoreHealth(amount);
        }

        public bool TryGrantCoins(int amount)
        {
            if (playerInventory == null || amount <= 0)
            {
                return false;
            }

            playerInventory.AddCoins(amount);
            return true;
        }

        public bool TryGrantKeys(int amount)
        {
            if (playerInventory == null || amount <= 0)
            {
                return false;
            }

            playerInventory.AddKeys(amount);
            return true;
        }

        public bool TryGrantBombs(int amount)
        {
            if (playerInventory == null || amount <= 0)
            {
                return false;
            }

            playerInventory.AddBombs(amount);
            return true;
        }

        public bool TryTriggerBombRain(int bombCount, float spawnRadius)
        {
            return playerBombController != null && playerBombController.TrySpawnBombBurst(bombCount, spawnRadius);
        }

        public bool TryWarpToPosition(Vector3 worldPosition)
        {
            playerMovement?.Stop();
            transform.position = worldPosition;
            return true;
        }

        public bool TryApplyTimedEffect(
            float durationSeconds,
            IReadOnlyList<StatModifier> statModifiers,
            IReadOnlyList<ProjectileModifier> projectileModifiers)
        {
            _timedEffectSourceItem = EquippedItem;
            return ApplyTimedEffectInternal(durationSeconds, durationSeconds, statModifiers, projectileModifiers);
        }

        public bool TryRestoreTimedEffect(
            float remainingSeconds,
            float durationSeconds,
            IReadOnlyList<StatModifier> statModifiers,
            IReadOnlyList<ProjectileModifier> projectileModifiers)
        {
            _timedEffectSourceItem = EquippedItem;
            return ApplyTimedEffectInternal(remainingSeconds, durationSeconds, statModifiers, projectileModifiers);
        }

        public bool TryRestoreTimedEffectFromItem(ActiveItemData sourceItem, float remainingSeconds)
        {
            if (sourceItem == null || sourceItem.Effect == null || remainingSeconds <= 0f)
            {
                return false;
            }

            bool restored = sourceItem.Effect.TryRestore(this, remainingSeconds);

            if (restored)
            {
                _timedEffectSourceItem = sourceItem;
            }

            return restored;
        }

        private bool ApplyTimedEffectInternal(
            float remainingSeconds,
            float durationSeconds,
            IReadOnlyList<StatModifier> statModifiers,
            IReadOnlyList<ProjectileModifier> projectileModifiers)
        {
            if (playerStats == null || durationSeconds <= 0f)
            {
                return false;
            }

            _runtimeStatModifiers.Clear();
            _runtimeProjectileModifiers.Clear();

            CopyStatModifiers(statModifiers, _runtimeStatModifiers);
            CopyProjectileModifiers(projectileModifiers, _runtimeProjectileModifiers);

            if (_runtimeStatModifiers.Count == 0 && _runtimeProjectileModifiers.Count == 0)
            {
                return false;
            }

            _timedEffectDuration = durationSeconds;
            _timedEffectRemaining = Mathf.Clamp(remainingSeconds, 0f, durationSeconds);
            playerStats.SetActiveItemRuntimeModifiers(_runtimeStatModifiers, _runtimeProjectileModifiers);
            NotifyStateChanged();
            return true;
        }

        public PlayerActiveItemSlotState BuildSlotState()
        {
            bool hasTimedEffect = _timedEffectRemaining > 0f;
            float normalizedRemaining = hasTimedEffect && _timedEffectDuration > 0f
                ? _timedEffectRemaining / _timedEffectDuration
                : 0f;

            return new PlayerActiveItemSlotState(
                EquippedItem,
                _currentCharge,
                EquippedItem != null ? EquippedItem.MaxCharge : 0,
                EquippedItem != null && _currentCharge >= EquippedItem.MaxCharge,
                hasTimedEffect,
                normalizedRemaining);
        }

        private void HandleRoomCleared(RoomClearSignal signal)
        {
            if (!signal.HadCombatEncounter || EquippedItem == null || EquippedItem.ChargeRule != ActiveItemChargeRule.RoomClear)
            {
                return;
            }

            AddCharge(EquippedItem.ChargePerRoomClear);
        }

        private void HandleEnemyDied(EnemyHealth enemyHealth)
        {
            if (EquippedItem == null || EquippedItem.ChargeRule != ActiveItemChargeRule.EnemyKill)
            {
                return;
            }

            AddCharge(EquippedItem.ChargePerEnemyKill);
        }

        private void AddCharge(int amount)
        {
            if (EquippedItem == null || amount <= 0)
            {
                return;
            }

            int nextCharge = Mathf.Clamp(_currentCharge + amount, 0, EquippedItem.MaxCharge);
            if (nextCharge == _currentCharge)
            {
                return;
            }

            _currentCharge = nextCharge;
            NotifyStateChanged();
        }

        private void ClearTimedEffect()
        {
            _runtimeStatModifiers.Clear();
            _runtimeProjectileModifiers.Clear();
            _timedEffectDuration = 0f;
            _timedEffectRemaining = 0f;
            _timedEffectSourceItem = null;

            if (playerStats != null)
            {
                playerStats.SetActiveItemRuntimeModifiers(_runtimeStatModifiers, _runtimeProjectileModifiers);
            }

            NotifyStateChanged();
        }

        private void NotifyStateChanged()
        {
            ActiveItemStateChanged?.Invoke(BuildSlotState());
        }

        private void ResolveDependencies()
        {
            if (playerHealth == null)
            {
                playerHealth = GetComponent<PlayerHealth>();
            }

            if (playerInventory == null)
            {
                playerInventory = GetComponent<PlayerInventory>();
            }

            if (playerStats == null)
            {
                playerStats = GetComponent<PlayerStats>();
            }

            if (playerBombController == null)
            {
                playerBombController = GetComponent<PlayerBombController>();
            }

            if (playerMovement == null)
            {
                playerMovement = GetComponent<PlayerMovement>();
            }

            if (inputReaderSource is IPlayerInputReader serializedReader)
            {
                _inputReader = serializedReader;
                return;
            }

            MonoBehaviour[] sceneBehaviours = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            for (int i = 0; i < sceneBehaviours.Length; i++)
            {
                if (sceneBehaviours[i] is IPlayerInputReader sceneReader)
                {
                    _inputReader = sceneReader;
                    return;
                }
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
    }
}
