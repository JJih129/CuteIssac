using System;
using System.Collections.Generic;
using CuteIssac.Common.Stats;
using CuteIssac.Core.Gameplay;
using UnityEngine;

namespace CuteIssac.Player
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerStats))]
    public sealed class PlayerCombatMomentumController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerStats playerStats;
        [SerializeField] private PlayerHealth playerHealth;

        [Header("Momentum")]
        [SerializeField] [Range(0.4f, 1.4f)] private float durationScale = 0.94f;
        [SerializeField] [Min(0.2f)] private float minimumDuration = 0.8f;
        [SerializeField] [Min(0.2f)] private float maximumDuration = 3.1f;
        [SerializeField] [Min(0.4f)] private float maximumSustainedDuration = 4.25f;
        [SerializeField] [Range(1, 3)] private int maxChainCount = 3;
        [SerializeField] [Range(0f, 0.2f)] private float chainBonusPerStep = 0.05f;

        [Header("Base Buff")]
        [SerializeField] [Range(1f, 1.4f)] private float baseMoveSpeedMultiplier = 1.08f;
        [SerializeField] [Range(1f, 1.4f)] private float baseFireRateMultiplier = 1.08f;
        [SerializeField] [Range(1f, 1.4f)] private float baseDamageMultiplier = 1.06f;

        [Header("Formation Buff")]
        [SerializeField] [Range(1f, 1.4f)] private float escortMoveSpeedMultiplier = 1.06f;
        [SerializeField] [Range(1f, 1.4f)] private float escortDamageMultiplier = 1.04f;
        [SerializeField] [Range(1f, 1.4f)] private float crossfireFireRateMultiplier = 1.12f;
        [SerializeField] [Range(1f, 1.4f)] private float crossfireProjectileSpeedMultiplier = 1.08f;
        [SerializeField] [Range(1f, 1.4f)] private float siegeDamageMultiplier = 1.12f;
        [SerializeField] [Range(1f, 1.5f)] private float siegeKnockbackMultiplier = 1.16f;

        [Header("Damage Penalty")]
        [SerializeField] private bool trimMomentumOnDamage = true;
        [SerializeField] [Range(0f, 1f)] private float damageTrimRatio = 0.45f;

        private readonly List<StatModifier> _momentumStatModifiers = new();
        private float _momentumExpiresAt;
        private float _momentumWindowDuration;
        private int _chainCount;
        private string _activeFormationId = string.Empty;
        private Color _accentColor = Color.white;

        public event Action MomentumStateChanged;

        public bool IsMomentumActive => HasMomentum();
        public int ChainCount => _chainCount;
        public string ActiveFormationId => _activeFormationId;
        public Color AccentColor => _accentColor;
        public float RemainingDurationNormalized
        {
            get
            {
                if (!HasMomentum() || _momentumWindowDuration <= 0.01f)
                {
                    return 0f;
                }

                return Mathf.Clamp01((_momentumExpiresAt - Time.time) / _momentumWindowDuration);
            }
        }

        public float RemainingDurationSeconds => HasMomentum()
            ? Mathf.Max(0f, _momentumExpiresAt - Time.time)
            : 0f;

        private void Awake()
        {
            ResolveReferences();
            ClearMomentum();
        }

        private void OnEnable()
        {
            ResolveReferences();
            GameplayRuntimeEvents.FormationBroken += HandleFormationBroken;
            GameplayRuntimeEvents.PlayerDamaged += HandlePlayerDamaged;
        }

        private void OnDisable()
        {
            GameplayRuntimeEvents.FormationBroken -= HandleFormationBroken;
            GameplayRuntimeEvents.PlayerDamaged -= HandlePlayerDamaged;
            ClearMomentum();
        }

        private void Update()
        {
            if (_momentumExpiresAt <= 0f)
            {
                return;
            }

            if (Time.time < _momentumExpiresAt)
            {
                return;
            }

            ClearMomentum();
        }

        private void HandleFormationBroken(FormationBreakSignal signal)
        {
            if (playerStats == null || playerHealth == null || playerHealth.IsDead)
            {
                return;
            }

            float duration = Mathf.Clamp(signal.BreakDuration * durationScale, minimumDuration, maximumDuration);

            if (HasMomentum())
            {
                _chainCount = Mathf.Min(_chainCount + 1, Mathf.Max(1, maxChainCount));
            }
            else
            {
                _chainCount = 1;
            }

            _activeFormationId = signal.FormationId ?? string.Empty;
            _accentColor = signal.AccentColor;
            _momentumExpiresAt = Mathf.Max(_momentumExpiresAt, Time.time + duration);
            _momentumWindowDuration = Mathf.Max(_momentumExpiresAt - Time.time, duration);
            RebuildMomentumModifiers();
            RaiseMomentumStateChanged();
        }

        private void HandlePlayerDamaged(PlayerDamagedSignal signal)
        {
            if (!trimMomentumOnDamage
                || playerHealth == null
                || signal.PlayerHealth != playerHealth
                || !HasMomentum())
            {
                return;
            }

            float remainingDuration = _momentumExpiresAt - Time.time;

            if (remainingDuration <= 0.05f)
            {
                ClearMomentum();
                return;
            }

            _chainCount = Mathf.Min(_chainCount, 1);
            float trimmedDuration = remainingDuration * Mathf.Clamp01(damageTrimRatio);

            if (trimmedDuration <= 0.15f)
            {
                ClearMomentum();
                return;
            }

            _momentumExpiresAt = Time.time + trimmedDuration;
            _momentumWindowDuration = trimmedDuration;
            RebuildMomentumModifiers();
            RaiseMomentumStateChanged();
        }

        public float SustainMomentum(float extraDuration)
        {
            if (!HasMomentum() || extraDuration <= 0f)
            {
                return 0f;
            }

            float currentRemaining = Mathf.Max(0f, _momentumExpiresAt - Time.time);
            float sustainCap = Mathf.Max(currentRemaining, maximumSustainedDuration);
            float cappedRemaining = Mathf.Min(sustainCap, currentRemaining + Mathf.Max(0f, extraDuration));
            float appliedDuration = Mathf.Max(0f, cappedRemaining - currentRemaining);

            if (appliedDuration <= 0.01f)
            {
                return 0f;
            }

            _momentumExpiresAt = Time.time + cappedRemaining;
            _momentumWindowDuration = Mathf.Max(_momentumWindowDuration, cappedRemaining);
            RaiseMomentumStateChanged();
            return appliedDuration;
        }

        private void RebuildMomentumModifiers()
        {
            _momentumStatModifiers.Clear();

            if (!HasMomentum() || playerStats == null)
            {
                playerStats?.SetCombatMomentumRuntimeModifiers(null, null);
                return;
            }

            float chainScale = 1f + (Mathf.Max(0, _chainCount - 1) * Mathf.Max(0f, chainBonusPerStep));
            AddMultiplier(PlayerStatType.MoveSpeed, baseMoveSpeedMultiplier * chainScale);
            AddMultiplier(PlayerStatType.FireInterval, baseFireRateMultiplier * chainScale);
            AddMultiplier(PlayerStatType.Damage, baseDamageMultiplier * chainScale);

            switch (_activeFormationId)
            {
                case "escort":
                    AddMultiplier(PlayerStatType.MoveSpeed, escortMoveSpeedMultiplier * chainScale);
                    AddMultiplier(PlayerStatType.Damage, escortDamageMultiplier * chainScale);
                    break;
                case "crossfire":
                    AddMultiplier(PlayerStatType.FireInterval, crossfireFireRateMultiplier * chainScale);
                    AddMultiplier(PlayerStatType.ProjectileSpeed, crossfireProjectileSpeedMultiplier * chainScale);
                    break;
                case "siege":
                    AddMultiplier(PlayerStatType.Damage, siegeDamageMultiplier * chainScale);
                    AddMultiplier(PlayerStatType.Knockback, siegeKnockbackMultiplier * chainScale);
                    break;
            }

            playerStats.SetCombatMomentumRuntimeModifiers(_momentumStatModifiers, null);
        }

        private void ClearMomentum()
        {
            bool hadMomentum = _momentumExpiresAt > 0f || _chainCount > 0 || !string.IsNullOrWhiteSpace(_activeFormationId);
            _momentumExpiresAt = 0f;
            _momentumWindowDuration = 0f;
            _chainCount = 0;
            _activeFormationId = string.Empty;
            _accentColor = Color.white;
            _momentumStatModifiers.Clear();
            playerStats?.SetCombatMomentumRuntimeModifiers(null, null);

            if (hadMomentum)
            {
                RaiseMomentumStateChanged();
            }
        }

        private bool HasMomentum()
        {
            return _momentumExpiresAt > 0f && Time.time < _momentumExpiresAt;
        }

        private void AddMultiplier(PlayerStatType statType, float multiplier)
        {
            if (multiplier <= 0f)
            {
                return;
            }

            _momentumStatModifiers.Add(new StatModifier(
                statType,
                StatModifierOperation.Multiply,
                multiplier));
        }

        private void ResolveReferences()
        {
            if (playerStats == null)
            {
                playerStats = GetComponent<PlayerStats>();
            }

            if (playerHealth == null)
            {
                playerHealth = GetComponent<PlayerHealth>();
            }
        }

        private void RaiseMomentumStateChanged()
        {
            MomentumStateChanged?.Invoke();
        }
    }
}
