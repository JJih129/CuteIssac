using CuteIssac.Data.Item;
using UnityEngine;

namespace CuteIssac.Player.ItemEffects
{
    public sealed class AddCoinsGameplayEffectExecutor : IItemGameplayEffectExecutor
    {
        private static readonly Color FeedbackColor = new(1f, 0.92f, 0.4f, 1f);

        public ItemGameplayEventEffectType EffectType => ItemGameplayEventEffectType.AddCoins;

        public bool TryExecute(
            ItemGameplayEventEffect effect,
            in ItemGameplayEffectExecutionContext context,
            Vector3 feedbackPosition)
        {
            if (effect == null || context.Inventory == null)
            {
                return false;
            }

            int amount = effect.CoinAmount;

            if (amount <= 0)
            {
                return false;
            }

            context.Inventory.AddCoins(amount);
            context.RaiseFeedback?.Invoke(
                feedbackPosition,
                effect.ResolveFeedbackLabel($"+{amount}C"),
                FeedbackColor);
            return true;
        }
    }
}
