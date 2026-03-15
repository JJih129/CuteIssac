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
                ResourcePickupType.Coin => "코인",
                ResourcePickupType.Key => "열쇠",
                ResourcePickupType.Bomb => "폭탄",
                _ => "자원"
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
