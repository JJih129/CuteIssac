using CuteIssac.Player;
using UnityEngine;

namespace CuteIssac.Item
{
    /// <summary>
    /// Grants limited pickup-owned max health. The cap prevents this pickup from scaling max health endlessly.
    /// </summary>
    public sealed class MaxHealthJellyPlusPickupLogic : BasePickupLogic
    {
        [Header("Max Health Jelly+")]
        [SerializeField] [Min(0.5f)] private float maxHealthIncreaseAmount = 1f;
        [SerializeField] [Min(0f)] private float maxAllowedIncrease = 2f;
        [SerializeField] private bool healGrantedAmount = true;
        [SerializeField] private bool logDebugMessages;

        public float MaxHealthIncreaseAmount => maxHealthIncreaseAmount;
        public float MaxAllowedIncrease => maxAllowedIncrease;
        public bool HealGrantedAmount => healGrantedAmount;

        protected override bool TryCollect(PlayerInventory inventory, PlayerHealth health, PlayerItemManager itemManager)
        {
            if (health == null)
            {
                return false;
            }

            bool granted = health.TryGrantPickupMaxHealthBonus(maxHealthIncreaseAmount, maxAllowedIncrease, healGrantedAmount);

            if (!granted && logDebugMessages)
            {
                Debug.Log("Max health jelly+ had no effect because the pickup max-health cap is already reached.", this);
            }

            return granted;
        }

        protected override string BuildPickupFeedbackLabel()
        {
            return healGrantedAmount ? "+MAX HP" : "MAX HP UP";
        }

        protected override Color ResolvePickupFeedbackColor()
        {
            return new Color(0.58f, 1f, 0.62f, 1f);
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            maxHealthIncreaseAmount = Mathf.Max(0.5f, maxHealthIncreaseAmount);
            maxAllowedIncrease = Mathf.Max(0f, maxAllowedIncrease);
        }
    }
}
