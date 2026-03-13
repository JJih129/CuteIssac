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
    }
}
