using System;
using CuteIssac.Common.Combat;
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

        public event Action<float, float> HealthChanged;
        public event Action Damaged;
        public event Action Died;

        public float MaxHealth => maxHealth;
        public float CurrentHealth { get; private set; }
        public bool IsInvulnerable { get; private set; }
        public bool IsDead { get; private set; }

        private float _invulnerabilityRemaining;

        private void Awake()
        {
            CurrentHealth = startingHealth >= 0f
                ? Mathf.Min(maxHealth, startingHealth)
                : maxHealth;
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
            if (IsDead || IsInvulnerable)
            {
                return;
            }

            float damageAmount = Mathf.Max(0f, damageInfo.Amount);

            if (damageAmount <= 0f)
            {
                return;
            }

            CurrentHealth = Mathf.Max(0f, CurrentHealth - damageAmount);
            Damaged?.Invoke();
            HealthChanged?.Invoke(CurrentHealth, maxHealth);

            if (CurrentHealth <= 0f)
            {
                Die();
                return;
            }

            if (invulnerabilityDuration > 0f)
            {
                IsInvulnerable = true;
                _invulnerabilityRemaining = invulnerabilityDuration;
            }
        }

        public void RestoreToFull()
        {
            IsDead = false;
            IsInvulnerable = false;
            _invulnerabilityRemaining = 0f;
            CurrentHealth = maxHealth;
            HealthChanged?.Invoke(CurrentHealth, maxHealth);
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

            if (clampedAmount <= 0f || CurrentHealth >= maxHealth)
            {
                return false;
            }

            CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + clampedAmount);
            HealthChanged?.Invoke(CurrentHealth, maxHealth);
            return true;
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
    }
}
