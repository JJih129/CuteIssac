using CuteIssac.Data.Item;
using UnityEngine;

namespace CuteIssac.Player
{
    /// <summary>
    /// Coordinates passive item acquisition and stat recomputation.
    /// Keeping this separate from inventory avoids inventory turning into a gameplay orchestrator.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerItemManager : MonoBehaviour
    {
        [SerializeField] private PlayerInventory playerInventory;
        [SerializeField] private PlayerStats playerStats;

        [Header("Debug")]
        [SerializeField] private ItemData debugPickupItem;

        private void Awake()
        {
            ResolveDependencies();

            if (playerInventory != null)
            {
                playerInventory.InventoryChanged += HandleInventoryChanged;
            }
        }

        private void Start()
        {
            RecalculateStats();
        }

        private void OnDestroy()
        {
            if (playerInventory != null)
            {
                playerInventory.InventoryChanged -= HandleInventoryChanged;
            }
        }

        public bool AcquirePassiveItem(ItemData itemData)
        {
            ResolveDependencies();

            if (playerInventory == null || playerStats == null)
            {
                Debug.LogError("PlayerItemManager requires PlayerInventory and PlayerStats.", this);
                return false;
            }

            return playerInventory.AddPassiveItem(itemData);
        }

        [ContextMenu("Pickup Debug Item")]
        public void PickupDebugItem()
        {
            if (debugPickupItem == null)
            {
                Debug.LogWarning("PlayerItemManager debugPickupItem is not assigned.", this);
                return;
            }

            AcquirePassiveItem(debugPickupItem);
        }

        private void HandleInventoryChanged()
        {
            RecalculateStats();
        }

        private void RecalculateStats()
        {
            if (playerStats != null && playerInventory != null)
            {
                playerStats.Recalculate(playerInventory.PassiveItems);
            }
        }

        private void ResolveDependencies()
        {
            if (playerInventory == null)
            {
                playerInventory = GetComponent<PlayerInventory>();
            }

            if (playerStats == null)
            {
                playerStats = GetComponent<PlayerStats>();
            }
        }
    }
}
