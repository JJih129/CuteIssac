using CuteIssac.Data.Item;
using UnityEngine;

namespace CuteIssac.Player.ItemEffects
{
    public sealed class RestoreHealthGameplayEffectExecutor : IItemGameplayEffectExecutor
    {
        private static readonly Color FeedbackColor = new(0.58f, 1f, 0.66f, 1f);

        public ItemGameplayEventEffectType EffectType => ItemGameplayEventEffectType.RestoreHealth;

        public bool TryExecute(
            ItemGameplayEventEffect effect,
            in ItemGameplayEffectExecutionContext context,
            Vector3 feedbackPosition)
        {
            if (effect == null || context.Health == null)
            {
                return false;
            }

            float amount = effect.HealAmount;

            if (amount <= 0f || !context.Health.RestoreHealth(amount))
            {
                return false;
            }

            context.RaiseFeedback?.Invoke(
                feedbackPosition,
                effect.ResolveFeedbackLabel($"+{amount:0.#}HP"),
                FeedbackColor);
            return true;
        }
    }
}
