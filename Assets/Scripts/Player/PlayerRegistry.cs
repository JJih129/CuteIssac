using UnityEngine;

namespace CuteIssac.Player
{
    public static class PlayerRegistry
    {
        public static PlayerController ActiveController { get; private set; }
        public static PlayerHealth ActiveHealth { get; private set; }
        public static PlayerWeaponLoadout ActiveWeaponLoadout { get; private set; }
        public static PlayerInventory ActiveInventory { get; private set; }
        public static PlayerItemManager ActiveItemManager { get; private set; }

        public static void Register(PlayerController playerController)
        {
            if (playerController != null)
            {
                ActiveController = playerController;
            }
        }

        public static void Register(PlayerHealth playerHealth)
        {
            if (playerHealth != null)
            {
                ActiveHealth = playerHealth;
            }
        }

        public static void Register(PlayerWeaponLoadout weaponLoadout)
        {
            if (weaponLoadout != null)
            {
                ActiveWeaponLoadout = weaponLoadout;
            }
        }

        public static void Register(PlayerInventory inventory)
        {
            if (inventory != null)
            {
                ActiveInventory = inventory;
            }
        }

        public static void Register(PlayerItemManager itemManager)
        {
            if (itemManager != null)
            {
                ActiveItemManager = itemManager;
            }
        }

        public static void Unregister(PlayerController playerController)
        {
            if (ActiveController == playerController)
            {
                ActiveController = null;
            }
        }

        public static void Unregister(PlayerHealth playerHealth)
        {
            if (ActiveHealth == playerHealth)
            {
                ActiveHealth = null;
            }
        }

        public static void Unregister(PlayerWeaponLoadout weaponLoadout)
        {
            if (ActiveWeaponLoadout == weaponLoadout)
            {
                ActiveWeaponLoadout = null;
            }
        }

        public static void Unregister(PlayerInventory inventory)
        {
            if (ActiveInventory == inventory)
            {
                ActiveInventory = null;
            }
        }

        public static void Unregister(PlayerItemManager itemManager)
        {
            if (ActiveItemManager == itemManager)
            {
                ActiveItemManager = null;
            }
        }

        public static bool TryResolveActiveWeaponLoadoutFor(
            Component firstComponent,
            Component secondComponent,
            Component thirdComponent,
            out PlayerWeaponLoadout weaponLoadout)
        {
            weaponLoadout = null;

            PlayerWeaponLoadout activeWeaponLoadout = ActiveWeaponLoadout;
            if (activeWeaponLoadout == null)
            {
                return false;
            }

            Transform activeTransform = activeWeaponLoadout.transform;
            if (!IsComponentUnderTransform(firstComponent, activeTransform)
                && !IsComponentUnderTransform(secondComponent, activeTransform)
                && !IsComponentUnderTransform(thirdComponent, activeTransform))
            {
                return false;
            }

            weaponLoadout = activeWeaponLoadout;
            return true;
        }

        public static bool IsComponentUnderTransform(Component component, Transform root)
        {
            if (component == null || root == null)
            {
                return false;
            }

            Transform componentTransform = component.transform;
            return componentTransform == root || componentTransform.IsChildOf(root);
        }
    }
}
