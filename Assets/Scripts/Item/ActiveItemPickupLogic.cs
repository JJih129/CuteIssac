using CuteIssac.Data.Item;
using CuteIssac.Player;
using UnityEngine;

namespace CuteIssac.Item
{
    [DisallowMultipleComponent]
    public sealed class ActiveItemPickupLogic : BasePickupLogic
    {
        [SerializeField] private ActiveItemData activeItemData;

        protected override void OnEnable()
        {
            base.OnEnable();
            ApplyPickupVisual();
        }

        public void ConfigureActiveItem(ActiveItemData configuredActiveItem)
        {
            activeItemData = configuredActiveItem;
            ApplyPickupVisual();
        }

        protected override bool TryCollect(PlayerInventory inventory, PlayerHealth health, PlayerItemManager itemManager)
        {
            if (activeItemData == null)
            {
                return false;
            }

            Component sourceComponent = inventory != null
                ? inventory
                : (health != null ? (Component)health : itemManager);
            PlayerActiveItemController activeItemController = sourceComponent != null
                ? sourceComponent.GetComponent<PlayerActiveItemController>()
                : null;

            if (activeItemController == null)
            {
                return false;
            }

            activeItemController.EquipActiveItem(activeItemData);
            return true;
        }

        protected override string BuildPickupFeedbackLabel()
        {
            return activeItemData != null
                ? activeItemData.DisplayName.ToUpperInvariant()
                : base.BuildPickupFeedbackLabel();
        }

        private void ApplyPickupVisual()
        {
            PickupPlaceholderVisualResolver.Apply(PickupVisual, activeItemData);
        }
    }
}
