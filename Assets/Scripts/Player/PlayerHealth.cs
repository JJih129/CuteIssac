using System;
using CuteIssac.Common.Combat;
using CuteIssac.Core.Audio;
using CuteIssac.Core.Feedback;
using CuteIssac.Core.Gameplay;
using UnityEngine;

namespace CuteIssac.Player
{
    /// <summary>
    /// Owns player hit points and temporary invulnerability after taking damage.
    /// UI, run flow, and feedback systems should subscribe to the exposed events instead of embedding logic here.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerHealth : MonoBehaviour, IDamageable
    {
        [Header("Health")]
        [SerializeField] [Min(1f)] private float maxHealth = 6f;
        [SerializeField] [Min(0f)] private float startingHealth = -1f;
        [SerializeField] [Min(0f)] private float invulnerabilityDuration = 1f;
        [SerializeField] private PlayerStats playerStats;
        [SerializeField] private PlayerVisual playerVisual;

        public event Action<float, float> HealthChanged;
        public event Action Damaged;
        public event Action<DamageInfo> DamagedWithInfo;
        public event Action Died;

        public float BaseMaxHealth => maxHealth;
        public float MaxHealth => maxHealth + _runtimeMaxHealthBonus;
        public float CurrentHealth { get; private set; }
        public bool IsInvulnerable { get; private set; }
        public bool IsDead { get; private set; }
        public bool IsDebugInvulnerable { get; private set; }

        private float _invulnerabilityRemaining;
        private float _runtimeMaxHealthBonus;

        private void Awake()
        {
            CurrentHealth = startingHealth >= 0f
                ? Mathf.Min(MaxHealth, startingHealth)
                : MaxHealth;
        }

        private void OnEnable()
        {
            ResolveDependencies();

            if (playerStats != null)
            {
                playerStats.StatsRecalculated += HandleStatsRecalculated;
            }
        }

        private void Start()
        {
            if (playerStats != null)
            {
                HandleStatsRecalculated(playerStats.CurrentStats);
            }
        }

        private void OnDisable()
        {
            if (playerStats != null)
            {
                playerStats.StatsRecalculated -= HandleStatsRecalculated;
            }
        }

        private void Update()
        {
            if (!IsInvulnerable)
            {
                return;
            }

            _invulnerabilityRemaining -= Time.deltaTime;

            if (_invulnerabilityRemaining <= 0f)
            {
                IsInvulnerable = false;
                _invulnerabilityRemaining = 0f;
            }
        }

        public void ApplyDamage(in DamageInfo damageInfo)
        {
            ApplyDamageInternal(damageInfo, ignoreInvulnerability: false, grantInvulnerability: true);
        }

        public bool TrySpendHealth(float amount, Transform source = null, bool allowLethal = false)
        {
            if (IsDead)
            {
                return false;
            }

            float healthCost = Mathf.Max(0f, amount);

            if (healthCost <= 0f)
            {
                return true;
            }

            if (!allowLethal && CurrentHealth <= healthCost)
            {
                return false;
            }

            ApplyDamageInternal(
                new DamageInfo(healthCost, Vector2.zero, source != null ? source : transform),
                ignoreInvulnerability: true,
                grantInvulnerability: false);

            return true;
        }

        public void RestoreToFull()
        {
            IsDead = false;
            IsInvulnerable = false;
            _invulnerabilityRemaining = 0f;
            CurrentHealth = MaxHealth;
            HealthChanged?.Invoke(CurrentHealth, MaxHealth);
        }

        public void RestoreForRunResume(float currentHealth)
        {
            IsDead = false;
            IsInvulnerable = false;
            _invulnerabilityRemaining = 0f;
            CurrentHealth = currentHealth > 0f
                ? Mathf.Clamp(currentHealth, 1f, MaxHealth)
                : MaxHealth;
            HealthChanged?.Invoke(CurrentHealth, MaxHealth);
        }

        /// <summary>
        /// Restores health without reviving a dead player.
        /// Pickups can use this instead of talking to health internals directly.
        /// </summary>
        public bool RestoreHealth(float amount)
        {
            if (IsDead)
            {
                return false;
            }

            float clampedAmount = Mathf.Max(0f, amount);

            if (clampedAmount <= 0f || CurrentHealth >= MaxHealth)
            {
                return false;
            }

            CurrentHealth = Mathf.Min(MaxHealth, CurrentHealth + clampedAmount);
            HealthChanged?.Invoke(CurrentHealth, MaxHealth);
            return true;
        }

        public void SetRuntimeMaxHealthBonus(float bonus)
        {
            float clampedBonus = Mathf.Max(0f, bonus);

            if (Mathf.Approximately(_runtimeMaxHealthBonus, clampedBonus))
            {
                return;
            }

            float previousMaxHealth = MaxHealth;
            _runtimeMaxHealthBonus = clampedBonus;

            if (CurrentHealth > MaxHealth)
            {
                CurrentHealth = MaxHealth;
            }

            if (!Mathf.Approximately(previousMaxHealth, MaxHealth))
            {
                HealthChanged?.Invoke(CurrentHealth, MaxHealth);
            }
        }

        public void SetDebugInvulnerable(bool value)
        {
            IsDebugInvulnerable = value;
        }

        private void Die()
        {
            if (IsDead)
            {
                return;
            }

            IsDead = true;
            IsInvulnerable = false;
            _invulnerabilityRemaining = 0f;
            Died?.Invoke();
        }

        private void HandleStatsRecalculated(PlayerStatSnapshot snapshot)
        {
            SetRuntimeMaxHealthBonus(Mathf.Max(0f, snapshot.MaxHealth - maxHealth));
        }

        private void ResolveDependencies()
        {
            if (playerStats == null)
            {
                playerStats = GetComponent<PlayerStats>();
            }

            if (playerVisual == null)
            {
                playerVisual = GetComponent<PlayerVisual>();
            }
        }

        private void ApplyDamageInternal(in DamageInfo damageInfo, bool ignoreInvulnerability, bool grantInvulnerability)
        {
            if (IsDead || IsDebugInvulnerable)
            {
                return;
            }

            if (!ignoreInvulnerability && IsInvulnerable)
            {
                return;
            }

            float damageAmount = Mathf.Max(0f, damageInfo.Amount);

            if (damageAmount <= 0f)
            {
                return;
            }

            CurrentHealth = Mathf.Max(0f, CurrentHealth - damageAmount);
            DamagedWithInfo?.Invoke(damageInfo);
            Damaged?.Invoke();
            GameplayRuntimeEvents.RaisePlayerDamaged(new PlayerDamagedSignal(this, damageInfo, CurrentHealth, MaxHealth));
            GameplayFeedbackEvents.RaiseFloatingFeedback(new FloatingFeedbackRequest(
                ResolveDamageFeedbackPosition(in damageInfo),
                $"-{Mathf.CeilToInt(damageAmount)}",
                new Color(1f, 0.42f, 0.42f, 1f),
                0.58f,
                0.72f,
                1.24f,
                visualProfile: FloatingFeedbackVisualProfile.PlayerDamage));
            GameAudioEvents.Raise(GameAudioEventType.Hit, transform.position);
            HealthChanged?.Invoke(CurrentHealth, MaxHealth);

            if (CurrentHealth <= 0f)
            {
                Die();
                return;
            }

            if (grantInvulnerability && invulnerabilityDuration > 0f)
            {
                IsInvulnerable = true;
                _invulnerabilityRemaining = invulnerabilityDuration;
            }
        }

        private Vector3 ResolveDamageFeedbackPosition(in DamageInfo damageInfo)
        {
            Vector3 anchorPosition = playerVisual != null && playerVisual.HitEffectAnchor != null
                ? playerVisual.HitEffectAnchor.position
                : transform.position;
            Vector2 hitDirection = damageInfo.HitDirection.sqrMagnitude > 0.0001f
                ? damageInfo.HitDirection.normalized
                : Vector2.zero;
            return anchorPosition + new Vector3(hitDirection.x * 0.12f, 0.22f, 0f);
        }
    }
}
