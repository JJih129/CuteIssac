using System;
using CuteIssac.Common.Combat;
using UnityEngine;
using UnityEngine.Events;

namespace CuteIssac.Room.Gimmicks
{
    /// <summary>
    /// Damageable prop base for candy gimmicks such as worms, lollipops, webs, cookie heads, and skull turrets.
    /// Bomb explosions already damage IDamageable targets, so this intentionally does not implement IBombReactive.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BreakableGimmick : CandyGimmickBase, IDamageable
    {
        [Header("Breakable")]
        [SerializeField] [Min(1f)] private float maxHealth = 1f;
        [SerializeField] private bool resetHealthOnEnable = true;
        [SerializeField] private bool destroyGameObjectOnBroken = true;
        [SerializeField] private bool deactivateCollidersOnBroken = true;
        [SerializeField] private Collider2D[] collidersToDisable;

        [Header("Hooks")]
        [SerializeField] private UnityEvent damaged;
        [SerializeField] private UnityEvent broken;

        public event Action<BreakableGimmick> Broken;

        public float MaxHealth => maxHealth;
        public float CurrentHealth { get; private set; }
        public bool IsBroken { get; private set; }

        protected override void Awake()
        {
            base.Awake();
            ResetHealth();
        }

        private void OnEnable()
        {
            if (resetHealthOnEnable)
            {
                ResetHealth();
            }
        }

        public void ApplyDamage(in DamageInfo damageInfo)
        {
            if (!IsGimmickActive || IsBroken)
            {
                return;
            }

            float damageAmount = Mathf.Max(0f, damageInfo.Amount);

            if (damageAmount <= 0f)
            {
                return;
            }

            CurrentHealth = Mathf.Max(0f, CurrentHealth - damageAmount);
            damaged?.Invoke();

            if (LogDebugMessages)
            {
                Debug.Log($"{name} damaged. Current={CurrentHealth:0.##}/{MaxHealth:0.##}", this);
            }

            if (CurrentHealth <= 0f)
            {
                Break();
            }
        }

        public void ConfigureHealth(float configuredMaxHealth, bool resetCurrentHealth)
        {
            maxHealth = Mathf.Max(1f, configuredMaxHealth);

            if (resetCurrentHealth)
            {
                ResetHealth();
            }
        }

        public void ConfigureResetHealthOnEnable(bool resetOnEnable)
        {
            resetHealthOnEnable = resetOnEnable;
        }

        public void ConfigureBreakBehaviour(bool destroyOnBroken)
        {
            destroyGameObjectOnBroken = destroyOnBroken;
        }

        [ContextMenu("Break")]
        public void Break()
        {
            if (IsBroken)
            {
                return;
            }

            IsBroken = true;
            SetCollidersEnabled(false);
            broken?.Invoke();
            Broken?.Invoke(this);

            if (destroyGameObjectOnBroken)
            {
                Destroy(gameObject);
            }
            else
            {
                SetActive(false);
            }
        }

        public void ResetHealth()
        {
            IsBroken = false;
            CurrentHealth = maxHealth;
            SetActive(true);
            SetCollidersEnabled(true);
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            maxHealth = Mathf.Max(1f, maxHealth);
        }

        private void SetCollidersEnabled(bool enabled)
        {
            if (!deactivateCollidersOnBroken && !enabled)
            {
                return;
            }

            if (collidersToDisable == null || collidersToDisable.Length == 0)
            {
                Collider2D fallbackCollider = ContactCollider;

                if (fallbackCollider != null)
                {
                    fallbackCollider.enabled = enabled;
                }

                return;
            }

            for (int i = 0; i < collidersToDisable.Length; i++)
            {
                if (collidersToDisable[i] != null)
                {
                    collidersToDisable[i].enabled = enabled;
                }
            }
        }
    }
}
