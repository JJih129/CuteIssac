using CuteIssac.Data.Item;
using CuteIssac.Player;
using UnityEngine;

namespace CuteIssac.Item
{
    /// <summary>
    /// Pickup path for consumable items. The consumable decides whether it is used immediately or stored in the active slot.
    /// </summary>
    public sealed class ConsumablePickupLogic : BasePickupLogic
    {
        [Header("Consumable Reward")]
        [SerializeField] private ConsumableItemData consumableItemData;

        protected override void OnEnable()
        {
            base.OnEnable();
            ApplyPickupVisual();
        }

        public void ConfigureConsumable(ConsumableItemData configuredConsumable)
        {
            consumableItemData = configuredConsumable;
            ApplyPickupVisual();
        }

        protected override bool TryCollect(PlayerInventory inventory, PlayerHealth health, PlayerItemManager itemManager)
        {
            Component sourceComponent = inventory != null
                ? inventory
                : (health != null ? (Component)health : itemManager);
            PlayerConsumableHolder consumableHolder = sourceComponent != null
                ? sourceComponent.GetComponent<PlayerConsumableHolder>()
                : null;

            return consumableHolder != null && consumableHolder.TryAcquireConsumable(consumableItemData);
        }

        protected override string BuildPickupFeedbackLabel()
        {
            return consumableItemData != null
                ? consumableItemData.DisplayName.ToUpperInvariant()
                : base.BuildPickupFeedbackLabel();
        }

        private void ApplyPickupVisual()
        {
            PickupPlaceholderVisualResolver.Apply(PickupVisual, consumableItemData);
        }
    }
}
