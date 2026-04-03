using System;
using CuteIssac.Data.Item;
using UnityEngine;

namespace CuteIssac.Player.ItemEffects
{
    public readonly struct ItemGameplayEffectExecutionContext
    {
        public ItemGameplayEffectExecutionContext(
            PlayerInventory inventory,
            PlayerStats stats,
            PlayerHealth health,
            PlayerActiveItemController activeItemController,
            Func<ItemGameplayEventEffect, bool> tryAddTimedModifier,
            Action<Vector3, string, Color> raiseFeedback)
        {
            Inventory = inventory;
            Stats = stats;
            Health = health;
            ActiveItemController = activeItemController;
            TryAddTimedModifier = tryAddTimedModifier;
            RaiseFeedback = raiseFeedback;
        }

        public PlayerInventory Inventory { get; }
        public PlayerStats Stats { get; }
        public PlayerHealth Health { get; }
        public PlayerActiveItemController ActiveItemController { get; }
        public Func<ItemGameplayEventEffect, bool> TryAddTimedModifier { get; }
        public Action<Vector3, string, Color> RaiseFeedback { get; }
    }
}
