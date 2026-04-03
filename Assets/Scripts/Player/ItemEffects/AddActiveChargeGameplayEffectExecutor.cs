using CuteIssac.Data.Item;
using UnityEngine;

namespace CuteIssac.Player.ItemEffects
{
    public sealed class AddActiveChargeGameplayEffectExecutor : IItemGameplayEffectExecutor
    {
        private static readonly Color FeedbackColor = new(0.62f, 0.88f, 1f, 1f);

        public ItemGameplayEventEffectType EffectType => ItemGameplayEventEffectType.AddActiveCharge;

        public bool TryExecute(
            ItemGameplayEventEffect effect,
            in ItemGameplayEffectExecutionContext context,
            Vector3 feedbackPosition)
        {
            if (effect == null || context.ActiveItemController == null)
            {
                return false;
            }

            int amount = effect.ActiveChargeAmount;

            if (amount <= 0 || !context.ActiveItemController.TryAddCharge(amount))
            {
                return false;
            }

            context.RaiseFeedback?.Invoke(
                feedbackPosition,
                effect.ResolveFeedbackLabel($"+{amount} CHARGE"),
                FeedbackColor);
            return true;
        }
    }
}
