using CuteIssac.Data.Item;

namespace CuteIssac.Player.ItemEffects
{
    public interface IItemGameplayEffectTriggerBinder
    {
        GameplayEventTriggerType TriggerType { get; }

        bool TryBind(
            ItemGameplayEventEffect effect,
            ItemGameplayEffectBindingContext context);
    }
}
