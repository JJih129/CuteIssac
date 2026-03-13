using CuteIssac.Data.Item;
using CuteIssac.Player;
using UnityEngine;

namespace CuteIssac.Item
{
    /// <summary>
    /// Grants a passive item through PlayerItemManager so stat recomputation stays centralized.
    /// </summary>
    public sealed class ItemPickupLogic : BasePickupLogic
    {
        [Header("Item Reward")]
        [SerializeField] private ItemData itemData;

        protected override bool TryCollect(PlayerInventory inventory, PlayerHealth health, PlayerItemManager itemManager)
        {
            return itemManager != null && itemManager.AcquirePassiveItem(itemData);
        }
    }
}
