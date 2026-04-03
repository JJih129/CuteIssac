using CuteIssac.Player;
using UnityEngine;

namespace CuteIssac.Item
{
    [DisallowMultipleComponent]
    public sealed class AmmoPickupLogic : BasePickupLogic
    {
        [Header("Ammo Reward")]
        [SerializeField] [Min(1)] private int ammoAmount = 8;
        [SerializeField] private bool restockEquippedWeapon = true;

        public int AmmoAmount => ammoAmount;

        public void Configure(int amount)
        {
            ammoAmount = Mathf.Max(1, amount);
        }

        public void Configure(int amount, bool restockEquipped)
        {
            ammoAmount = Mathf.Max(1, amount);
            restockEquippedWeapon = restockEquipped;
        }

        protected override bool TryCollect(PlayerInventory inventory, PlayerHealth health, PlayerItemManager itemManager)
        {
            PlayerWeaponLoadout weaponLoadout = ResolveWeaponLoadout(inventory, health, itemManager);
            if (weaponLoadout == null)
            {
                return false;
            }

            if (restockEquippedWeapon)
            {
                return weaponLoadout.TryRestockEquippedWeaponFromPickup();
            }

            return weaponLoadout.TryAddAmmo(ammoAmount);
        }

        protected override string BuildPickupFeedbackLabel()
        {
            if (restockEquippedWeapon)
            {
                return "AMMO FULL";
            }

            return ammoAmount > 1
                ? $"+{ammoAmount} AMMO"
                : "+1 AMMO";
        }

        protected override Color ResolvePickupFeedbackColor()
        {
            return new Color(0.94f, 0.76f, 0.26f, 1f);
        }

        private static PlayerWeaponLoadout ResolveWeaponLoadout(PlayerInventory inventory, PlayerHealth health, PlayerItemManager itemManager)
        {
            if (itemManager != null && itemManager.TryGetComponent(out PlayerWeaponLoadout itemManagerLoadout))
            {
                return itemManagerLoadout;
            }

            if (inventory != null && inventory.TryGetComponent(out PlayerWeaponLoadout inventoryLoadout))
            {
                return inventoryLoadout;
            }

            if (health != null && health.TryGetComponent(out PlayerWeaponLoadout healthLoadout))
            {
                return healthLoadout;
            }

            return null;
        }
    }
}
