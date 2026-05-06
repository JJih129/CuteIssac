using System;
using CuteIssac.Common.Combat;
using UnityEngine;
using UnityEngine.Events;

namespace CuteIssac.Player
{
    /// <summary>
    /// Stores player damage-blocking shield charges separately from health so damage rules stay centralized.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerShieldState : MonoBehaviour
    {
        [Header("Shield")]
        [SerializeField] [Min(1)] private int maxShieldCount = 1;
        [SerializeField] private bool logDebugMessages;

        [Header("Hooks")]
        [SerializeField] private UnityEvent shieldGranted;
        [SerializeField] private UnityEvent damageBlocked;
        [SerializeField] private UnityEvent shieldCountChanged;

        public event Action<int> ShieldCountChanged;
        public event Action<DamageInfo> DamageBlocked;

        public int ShieldCount { get; private set; }
        public int MaxShieldCount => Mathf.Max(1, maxShieldCount);
        public bool HasShield => ShieldCount > 0;

        public void SetMaxShieldCount(int value)
        {
            maxShieldCount = Mathf.Max(1, value);

            if (ShieldCount > MaxShieldCount)
            {
                ShieldCount = MaxShieldCount;
                NotifyShieldCountChanged();
            }
        }

        public bool TryGrantShield(int amount)
        {
            int resolvedAmount = Mathf.Max(0, amount);

            if (resolvedAmount <= 0 || ShieldCount >= MaxShieldCount)
            {
                return false;
            }

            ShieldCount = Mathf.Min(MaxShieldCount, ShieldCount + resolvedAmount);
            shieldGranted?.Invoke();
            NotifyShieldCountChanged();

            if (logDebugMessages)
            {
                Debug.Log($"Shield granted. Current={ShieldCount}/{MaxShieldCount}", this);
            }

            return true;
        }

        public bool TryBlockDamage(in DamageInfo damageInfo)
        {
            if (ShieldCount <= 0 || damageInfo.Amount <= 0f)
            {
                return false;
            }

            ShieldCount = Mathf.Max(0, ShieldCount - 1);
            damageBlocked?.Invoke();
            DamageBlocked?.Invoke(damageInfo);
            NotifyShieldCountChanged();

            if (logDebugMessages)
            {
                Debug.Log("Shield blocked incoming damage.", this);
            }

            return true;
        }

        private void NotifyShieldCountChanged()
        {
            shieldCountChanged?.Invoke();
            ShieldCountChanged?.Invoke(ShieldCount);
        }

        private void OnValidate()
        {
            maxShieldCount = Mathf.Max(1, maxShieldCount);
        }
    }
}
