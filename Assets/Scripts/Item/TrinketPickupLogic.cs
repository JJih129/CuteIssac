using CuteIssac.Data.Item;
using CuteIssac.Player;
using UnityEngine;

namespace CuteIssac.Item
{
    /// <summary>
    /// Pickup path for trinket items.
    /// The player can hold exactly one trinket for now, and the latest pickup replaces the current one.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TrinketPickupLogic : BasePickupLogic
    {
        [Header("Trinket Reward")]
        [SerializeField] private ItemData trinketData;

        protected override void OnEnable()
        {
            base.OnEnable();
            ApplyPickupVisual();
        }

        public void ConfigureTrinket(ItemData configuredTrinket)
        {
            trinketData = configuredTrinket;
            ApplyPickupVisual();
        }

        protected override bool TryCollect(PlayerInventory inventory, PlayerHealth health, PlayerItemManager itemManager)
        {
            Component sourceComponent = itemManager != null
                ? itemManager
                : inventory != null
                    ? inventory
                    : (Component)health;

            PlayerItemManager resolvedItemManager = sourceComponent != null
                ? sourceComponent.GetComponent<PlayerItemManager>()
                : null;

            return resolvedItemManager != null && resolvedItemManager.AcquireTrinketItem(trinketData);
        }

        protected override string BuildPickupFeedbackLabel()
        {
            return trinketData != null
                ? trinketData.DisplayName.ToUpperInvariant()
                : base.BuildPickupFeedbackLabel();
        }

        private void ApplyPickupVisual()
        {
            PickupPlaceholderVisualResolver.Apply(PickupVisual, trinketData);
        }
    }
}
