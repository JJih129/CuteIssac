using CuteIssac.Player;
using UnityEngine;

namespace CuteIssac.Item
{
    /// <summary>
    /// Grants a small number of damage-blocking shield charges to the player.
    /// </summary>
    public sealed class ShieldCandyPickupLogic : BasePickupLogic
    {
        [Header("Shield Candy")]
        [SerializeField] [Min(1)] private int shieldAmount = 1;
        [SerializeField] [Min(1)] private int maxShieldCount = 1;
        [SerializeField] private bool addMissingStateComponent = true;
        [SerializeField] private bool logDebugMessages;

        public int ShieldAmount => shieldAmount;
        public int MaxShieldCount => maxShieldCount;

        protected override bool TryCollect(PlayerInventory inventory, PlayerHealth health, PlayerItemManager itemManager)
        {
            if (health == null || !TryResolveShieldState(health, out PlayerShieldState shieldState))
            {
                return false;
            }

            shieldState.SetMaxShieldCount(maxShieldCount);
            bool granted = shieldState.TryGrantShield(shieldAmount);

            if (!granted && logDebugMessages)
            {
                Debug.Log("Shield candy had no effect because shield charges are already full.", this);
            }

            return granted;
        }

        protected override string BuildPickupFeedbackLabel()
        {
            return "+SHIELD";
        }

        protected override Color ResolvePickupFeedbackColor()
        {
            return new Color(0.72f, 0.92f, 1f, 1f);
        }

        private bool TryResolveShieldState(PlayerHealth health, out PlayerShieldState shieldState)
        {
            shieldState = health.GetComponent<PlayerShieldState>();

            if (shieldState != null)
            {
                return true;
            }

            if (!addMissingStateComponent)
            {
                return false;
            }

            shieldState = health.gameObject.AddComponent<PlayerShieldState>();
            return shieldState != null;
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            shieldAmount = Mathf.Max(1, shieldAmount);
            maxShieldCount = Mathf.Max(1, maxShieldCount);
        }
    }
}
