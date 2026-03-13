using System;
using CuteIssac.Common.Combat;
using UnityEngine;

namespace CuteIssac.Enemy
{
    /// <summary>
    /// Reusable health component for enemies.
    /// Implements IDamageable so projectiles and future hazards can damage enemies through one common path.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EnemyHealth : MonoBehaviour, IDamageable
    {
        [Header("Health")]
        [SerializeField] [Min(1f)] private float maxHealth = 10f;
        [SerializeField] private bool destroyOnDeath = true;

        public event Action Damaged;
        public event Action Died;
        public event Action<EnemyHealth> DiedWithSource;

        public float MaxHealth => maxHealth;
        public float CurrentHealth { get; private set; }
        public bool IsDead { get; private set; }

        private void Awake()
        {
            CurrentHealth = maxHealth;
        }

        public void ApplyDamage(in DamageInfo damageInfo)
        {
            if (IsDead)
            {
                return;
            }

            CurrentHealth = Mathf.Max(0f, CurrentHealth - Mathf.Max(0f, damageInfo.Amount));
            Damaged?.Invoke();

            if (CurrentHealth <= 0f)
            {
                Die();
            }
        }

        public void RestoreToFull()
        {
            IsDead = false;
            CurrentHealth = maxHealth;
        }

        private void Die()
        {
            if (IsDead)
            {
                return;
            }

            IsDead = true;
            DiedWithSource?.Invoke(this);
            Died?.Invoke();

            if (destroyOnDeath)
            {
                Destroy(gameObject);
            }
        }
    }
}
