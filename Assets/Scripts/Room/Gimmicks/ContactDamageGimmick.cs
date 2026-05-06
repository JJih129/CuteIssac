using System.Collections.Generic;
using CuteIssac.Player;
using UnityEngine;

namespace CuteIssac.Room.Gimmicks
{
    /// <summary>
    /// Reusable player contact hazard with a per-player tick cooldown to prevent damage every physics callback.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ContactDamageGimmick : CandyGimmickBase
    {
        [Header("Contact Damage")]
        [SerializeField] [Min(0f)] private float damage = 1f;
        [SerializeField] [Min(0.05f)] private float damageCooldown = 0.5f;
        [SerializeField] [Min(0f)] private float knockbackForce = 4f;
        [SerializeField] private bool forceContactColliderAsTrigger = true;

        private readonly Dictionary<int, float> _nextDamageTimes = new();

        public float Damage => damage;
        public float DamageCooldown => damageCooldown;
        public float KnockbackForce => knockbackForce;

        protected override void Awake()
        {
            base.Awake();
            ConfigureCollider();
        }

        private void OnDisable()
        {
            _nextDamageTimes.Clear();
        }

        protected override void OnPlayerContact(Collider2D playerCollider, PlayerHealth playerHealth)
        {
            int targetId = playerHealth.GetInstanceID();

            if (_nextDamageTimes.TryGetValue(targetId, out float nextDamageTime) && Time.time < nextDamageTime)
            {
                return;
            }

            if (TryApplyDamageToPlayer(playerHealth, playerCollider, damage, knockbackForce))
            {
                _nextDamageTimes[targetId] = Time.time + damageCooldown;
            }
        }

        public void ConfigureContactDamage(float damageAmount, float cooldown, float knockback)
        {
            damage = Mathf.Max(0f, damageAmount);
            damageCooldown = Mathf.Max(0.05f, cooldown);
            knockbackForce = Mathf.Max(0f, knockback);
            _nextDamageTimes.Clear();
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            damage = Mathf.Max(0f, damage);
            damageCooldown = Mathf.Max(0.05f, damageCooldown);
            knockbackForce = Mathf.Max(0f, knockbackForce);
            ConfigureCollider();
        }

        private void ConfigureCollider()
        {
            if (forceContactColliderAsTrigger && ContactCollider != null)
            {
                ContactCollider.isTrigger = true;
            }
        }
    }
}
