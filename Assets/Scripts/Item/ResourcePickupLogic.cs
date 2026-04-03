using CuteIssac.Player;
using UnityEngine;

namespace CuteIssac.Item
{
    /// <summary>
    /// Grants a simple inventory resource when collected.
    /// Coins, keys, and bombs all share this path so their prefabs stay presentation-driven.
    /// </summary>
    public sealed class ResourcePickupLogic : BasePickupLogic
    {
        [Header("Resource Reward")]
        [SerializeField] private ResourcePickupType resourceType;
        [SerializeField] [Min(1)] private int amount = 1;

        public ResourcePickupType ResourceType => resourceType;
        public int Amount => amount;

        public void Configure(ResourcePickupType nextResourceType, int nextAmount)
        {
            resourceType = nextResourceType;
            amount = Mathf.Max(1, nextAmount);
        }

        protected override bool TryCollect(PlayerInventory inventory, PlayerHealth health, PlayerItemManager itemManager)
        {
            if (inventory == null || amount <= 0)
            {
                return false;
            }

            switch (resourceType)
            {
                case ResourcePickupType.Coin:
                    inventory.AddCoins(amount);
                    return true;
                case ResourcePickupType.Key:
                    inventory.AddKeys(amount);
                    return true;
                case ResourcePickupType.Bomb:
                    inventory.AddBombs(amount);
                    return true;
                default:
                    return false;
            }
        }

        protected override string BuildPickupFeedbackLabel()
        {
            string resourceLabel = resourceType switch
            {
                ResourcePickupType.Coin => "COIN",
                ResourcePickupType.Key => "KEY",
                ResourcePickupType.Bomb => "BOMB",
                _ => "RESOURCE"
            };

            return amount > 1
                ? $"+{amount} {resourceLabel}"
                : $"+1 {resourceLabel}";
        }

        protected override Color ResolvePickupFeedbackColor()
        {
            return resourceType switch
            {
                ResourcePickupType.Coin => new Color(0.95f, 0.82f, 0.25f, 1f),
                ResourcePickupType.Key => new Color(0.72f, 0.86f, 1f, 1f),
                ResourcePickupType.Bomb => new Color(1f, 0.56f, 0.24f, 1f),
                _ => base.ResolvePickupFeedbackColor()
            };
        }
    }
}
