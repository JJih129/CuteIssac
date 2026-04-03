using CuteIssac.Data.Item;
using UnityEngine;

namespace CuteIssac.Player.ItemEffects
{
    public sealed class AddKeysGameplayEffectExecutor : IItemGameplayEffectExecutor
    {
        private static readonly Color FeedbackColor = new(0.8f, 0.92f, 1f, 1f);

        public ItemGameplayEventEffectType EffectType => ItemGameplayEventEffectType.AddKeys;

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

            context.Inventory.AddKeys(amount);
            context.RaiseFeedback?.Invoke(
                feedbackPosition,
                effect.ResolveFeedbackLabel($"+{amount} KEY"),
                FeedbackColor);
            return true;
        }
    }
}
