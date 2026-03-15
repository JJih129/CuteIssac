using System;
using System.Collections.Generic;
using CuteIssac.Data.Item;
using UnityEngine;

namespace CuteIssac.Player
{
    /// <summary>
    /// Stores passive items owned by the player.
    /// Inventory does not recalculate stats itself so ownership and stat application stay decoupled.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerInventory : MonoBehaviour
    {
        [Header("Starting Resources")]
        [SerializeField] [Min(0)] private int startingCoins;
        [SerializeField] [Min(0)] private int startingKeys = 1;
        [SerializeField] [Min(0)] private int startingBombs;

        [Header("Passive Items")]
        [SerializeField] private List<ItemData> startingPassiveItems = new();

        public event Action InventoryChanged;
        public event Action<PlayerResourceSnapshot> ResourcesChanged;

        public IReadOnlyList<ItemData> PassiveItems => _passiveItems;
        public int Coins { get; private set; }
        public int Keys { get; private set; }
        public int Bombs { get; private set; }
        public PlayerResourceSnapshot Resources => new(Coins, Keys, Bombs);

        private readonly List<ItemData> _passiveItems = new();

        private void Awake()
        {
            Coins = Mathf.Max(0, startingCoins);
            Keys = Mathf.Max(0, startingKeys);
            Bombs = Mathf.Max(0, startingBombs);
            InitializeStartingItems();
        }

        public bool AddPassiveItem(ItemData itemData)
        {
            if (itemData == null || _passiveItems.Contains(itemData))
            {
                return false;
            }

            _passiveItems.Add(itemData);
            InventoryChanged?.Invoke();
            return true;
        }

        public bool Contains(ItemData itemData)
        {
            return itemData != null && _passiveItems.Contains(itemData);
        }

        public void ApplyStartingLoadout(int coins, int keys, int bombs, IReadOnlyList<ItemData> passiveItems)
        {
            Coins = Mathf.Max(0, coins);
            Keys = Mathf.Max(0, keys);
            Bombs = Mathf.Max(0, bombs);
            _passiveItems.Clear();

            if (passiveItems != null)
            {
                for (int index = 0; index < passiveItems.Count; index++)
                {
                    ItemData itemData = passiveItems[index];

                    if (itemData != null && !_passiveItems.Contains(itemData))
                    {
                        _passiveItems.Add(itemData);
                    }
                }
            }

            InventoryChanged?.Invoke();
            NotifyResourcesChanged();
        }

        public void AddCoins(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            Coins += amount;
            NotifyResourcesChanged();
        }

        public void AddKeys(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            Keys += amount;
            NotifyResourcesChanged();
        }

        public void AddBombs(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            Bombs += amount;
            NotifyResourcesChanged();
        }

        public bool TrySpendCoins(int amount)
        {
            if (amount <= 0 || Coins < amount)
            {
                return false;
            }

            Coins -= amount;
            NotifyResourcesChanged();
            return true;
        }

        public bool TrySpendKeys(int amount)
        {
            if (amount <= 0 || Keys < amount)
            {
                return false;
            }

            Keys -= amount;
            NotifyResourcesChanged();
            return true;
        }

        public bool TrySpendBombs(int amount)
        {
            if (amount <= 0 || Bombs < amount)
            {
                return false;
            }

            Bombs -= amount;
            NotifyResourcesChanged();
            return true;
        }

        private void InitializeStartingItems()
        {
            for (int i = 0; i < startingPassiveItems.Count; i++)
            {
                ItemData itemData = startingPassiveItems[i];

                if (itemData != null && !_passiveItems.Contains(itemData))
                {
                    _passiveItems.Add(itemData);
                }
            }
        }

        private void NotifyResourcesChanged()
        {
            ResourcesChanged?.Invoke(Resources);
        }
    }
}
