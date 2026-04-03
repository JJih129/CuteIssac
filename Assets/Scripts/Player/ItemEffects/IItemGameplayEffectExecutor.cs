using CuteIssac.Data.Item;
using UnityEngine;

namespace CuteIssac.Player.ItemEffects
{
    public interface IItemGameplayEffectExecutor
    {
        ItemGameplayEventEffectType EffectType { get; }

        bool TryExecute(
            ItemGameplayEventEffect effect,
            in ItemGameplayEffectExecutionContext context,
            Vector3 feedbackPosition);
    }
}
