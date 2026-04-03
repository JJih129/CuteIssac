using System;
using CuteIssac.Data.Item;
using UnityEngine;

namespace CuteIssac.Player.ItemEffects
{
    public readonly struct ItemGameplayEffectBindingContext
    {
        public ItemGameplayEffectBindingContext(
            PlayerHealth playerHealth,
            Transform playerRoot,
            Func<Transform, bool> isOwnedByPlayer,
            Action<ItemGameplayEventEffect, Vector3> applyEffect,
            Action<Action> registerUnbind)
        {
            PlayerHealth = playerHealth;
            PlayerRoot = playerRoot;
            IsOwnedByPlayer = isOwnedByPlayer;
            ApplyEffect = applyEffect;
            RegisterUnbind = registerUnbind;
        }

        public PlayerHealth PlayerHealth { get; }
        public Transform PlayerRoot { get; }
        public Func<Transform, bool> IsOwnedByPlayer { get; }
        public Action<ItemGameplayEventEffect, Vector3> ApplyEffect { get; }
        public Action<Action> RegisterUnbind { get; }
    }
}
