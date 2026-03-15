using System;
using CuteIssac.Data.Item;
using UnityEngine;

namespace CuteIssac.Player
{
    /// <summary>
    /// Owns the currently equipped trinket.
    /// Trinkets stay separate from passive inventory so they can use their own pickup and save rules later,
    /// while still contributing their modifiers through PlayerItemManager.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerTrinketHolder : MonoBehaviour
    {
        [SerializeField] private ItemData startingTrinket;

        public event Action TrinketChanged;

        public ItemData EquippedTrinket { get; private set; }
        public bool HasTrinket => EquippedTrinket != null;

        private void Awake()
        {
            if (startingTrinket != null && startingTrinket.ItemType == ItemType.Trinket)
            {
                EquippedTrinket = startingTrinket;
            }
        }

        public bool TryEquipTrinket(ItemData itemData)
        {
            if (itemData == null || itemData.ItemType != ItemType.Trinket || EquippedTrinket == itemData)
            {
                return false;
            }

            EquippedTrinket = itemData;
            TrinketChanged?.Invoke();
            return true;
        }

        public void RestoreForRunResume(ItemData itemData)
        {
            EquippedTrinket = itemData != null && itemData.ItemType == ItemType.Trinket
                ? itemData
                : null;
            TrinketChanged?.Invoke();
        }
    }
}
