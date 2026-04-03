using CuteIssac.Data.Item;
using UnityEngine;

namespace CuteIssac.Player.ItemEffects
{
    public sealed class AddBombsGameplayEffectExecutor : IItemGameplayEffectExecutor
    {
        private static readonly Color FeedbackColor = new(1f, 0.74f, 0.44f, 1f);

        public ItemGameplayEventEffectType EffectType => ItemGameplayEventEffectType.AddBombs;

        public bool TryExecute(
            ItemGameplayEventEffect effect,
            in ItemGameplayEffectExecutionContext context,
            Vector3 feedbackPosition)
        {
            if (effect == null || context.Inventory == null)
            {
                return false;
            }

            int amount = effect.ResourceAmount;

            if (amount <= 0)
            {
                return false;
            }

            context.Inventory.AddBombs(amount);
            context.RaiseFeedback?.Invoke(
                feedbackPosition,
                effect.ResolveFeedbackLabel($"+{amount} BOMB"),
                FeedbackColor);
            return true;
        }
    }
}
