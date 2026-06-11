using CuteIssac.Player;
using UnityEngine;

namespace CuteIssac.Item
{
    [DisallowMultipleComponent]
    public sealed class AmmoPickupLogic : BasePickupLogic
    {
        private const int MaxCachedFeedbackAmount = 32;
        private static readonly string[] s_ammoFeedbackLabels = BuildFeedbackLabelCache();

        [Header("Ammo Reward")]
        [SerializeField] [Min(1)] private int ammoAmount = 8;
        [SerializeField] private bool restockEquippedWeapon = true;

        public int AmmoAmount => ammoAmount;

        public void Configure(int amount)
        {
            ammoAmount = Mathf.Max(1, amount);
            InvalidatePickupFeedbackCache();
        }

        public void Configure(int amount, bool restockEquipped)
        {
            ammoAmount = Mathf.Max(1, amount);
            restockEquippedWeapon = restockEquipped;
            InvalidatePickupFeedbackCache();
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
                if (weaponLoadout.TryRestockEquippedWeaponFromPickup())
                {
                    return true;
                }

                return weaponLoadout.TryAddAmmo(ammoAmount);
            }

            return weaponLoadout.TryAddAmmo(ammoAmount);
        }

        protected override string BuildPickupFeedbackLabel()
        {
            if (restockEquippedWeapon)
            {
                return "AMMO FULL";
            }

            if (ammoAmount > 0 && ammoAmount <= MaxCachedFeedbackAmount)
            {
                return s_ammoFeedbackLabels[ammoAmount];
            }

            return string.Concat("+", Mathf.Max(1, ammoAmount).ToString(), " AMMO");
        }

        protected override Color ResolvePickupFeedbackColor()
        {
            return new Color(0.94f, 0.76f, 0.26f, 1f);
        }

        private static PlayerWeaponLoadout ResolveWeaponLoadout(PlayerInventory inventory, PlayerHealth health, PlayerItemManager itemManager)
        {
            if (PlayerRegistry.TryResolveActiveWeaponLoadoutFor(inventory, health, itemManager, out PlayerWeaponLoadout activeWeaponLoadout))
            {
                return activeWeaponLoadout;
            }

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

        private static string[] BuildFeedbackLabelCache()
        {
            string[] labels = new string[MaxCachedFeedbackAmount + 1];

            for (int index = 1; index < labels.Length; index++)
            {
                labels[index] = string.Concat("+", index.ToString(), " AMMO");
            }

            return labels;
        }
    }
}
