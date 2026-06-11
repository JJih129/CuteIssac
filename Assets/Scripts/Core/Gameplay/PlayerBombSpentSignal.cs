using CuteIssac.Player;
using UnityEngine;

namespace CuteIssac.Core.Gameplay
{
    public readonly struct PlayerBombSpentSignal
    {
        public PlayerBombSpentSignal(PlayerInventory inventory, int amount)
        {
            Inventory = inventory;
            Amount = Mathf.Max(0, amount);
        }

        public PlayerInventory Inventory { get; }
        public int Amount { get; }
    }
}
