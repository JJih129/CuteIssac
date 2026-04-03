using System.Collections.Generic;
using CuteIssac.Data.Item;
using UnityEngine;

namespace CuteIssac.Player.ItemEffects
{
    internal static class ItemGameplayEffectExecutorRegistry
    {
        private static readonly Dictionary<ItemGameplayEventEffectType, IItemGameplayEffectExecutor> Executors = new()
        {
            { ItemGameplayEventEffectType.AddCoins, new AddCoinsGameplayEffectExecutor() },
            { ItemGameplayEventEffectType.ApplyTimedBuff, new ApplyTimedBuffGameplayEffectExecutor() },
            { ItemGameplayEventEffectType.AddKeys, new AddKeysGameplayEffectExecutor() },
            { ItemGameplayEventEffectType.AddBombs, new AddBombsGameplayEffectExecutor() },
            { ItemGameplayEventEffectType.RestoreHealth, new RestoreHealthGameplayEffectExecutor() },
            { ItemGameplayEventEffectType.AddActiveCharge, new AddActiveChargeGameplayEffectExecutor() },
            { ItemGameplayEventEffectType.GrantInvulnerability, new GrantInvulnerabilityGameplayEffectExecutor() }
        };

        public static bool TryExecute(
            ItemGameplayEventEffect effect,
            in ItemGameplayEffectExecutionContext context,
            Vector3 feedbackPosition)
        {
            if (effect == null)
            {
                return false;
            }

            return Executors.TryGetValue(effect.EffectType, out IItemGameplayEffectExecutor executor)
                && executor.TryExecute(effect, context, feedbackPosition);
        }
    }
}
