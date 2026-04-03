using CuteIssac.Data.Item;
using UnityEngine;

namespace CuteIssac.Player.ItemEffects
{
    public sealed class ApplyTimedBuffGameplayEffectExecutor : IItemGameplayEffectExecutor
    {
        private static readonly Color FeedbackColor = new(0.48f, 0.9f, 1f, 1f);

        public ItemGameplayEventEffectType EffectType => ItemGameplayEventEffectType.ApplyTimedBuff;

        public bool TryExecute(
            ItemGameplayEventEffect effect,
            in ItemGameplayEffectExecutionContext context,
            Vector3 feedbackPosition)
        {
            if (effect == null || context.TryAddTimedModifier == null)
            {
                return false;
            }

            if (!context.TryAddTimedModifier(effect))
            {
                return false;
            }

            context.RaiseFeedback?.Invoke(
                feedbackPosition,
                effect.ResolveFeedbackLabel("SURGE"),
                FeedbackColor);
            return true;
        }
    }
}
