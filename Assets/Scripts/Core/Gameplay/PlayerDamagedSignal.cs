using CuteIssac.Common.Combat;
using CuteIssac.Player;
using UnityEngine;

namespace CuteIssac.Core.Gameplay
{
    public readonly struct PlayerDamagedSignal
    {
        public PlayerDamagedSignal(PlayerHealth playerHealth, in DamageInfo damageInfo, float currentHealth, float maxHealth)
        {
            PlayerHealth = playerHealth;
            DamageInfo = damageInfo;
            CurrentHealth = currentHealth;
            MaxHealth = maxHealth;
        }

        public PlayerHealth PlayerHealth { get; }
        public DamageInfo DamageInfo { get; }
        public float CurrentHealth { get; }
        public float MaxHealth { get; }
        public Vector3 Position => PlayerHealth != null ? PlayerHealth.transform.position : Vector3.zero;
    }
}
