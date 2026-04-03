using CuteIssac.Core.Gameplay;
using CuteIssac.Data.Item;

namespace CuteIssac.Player.ItemEffects
{
    public sealed class PlayerDamagedGameplayEffectTriggerBinder : IItemGameplayEffectTriggerBinder
    {
        public GameplayEventTriggerType TriggerType => GameplayEventTriggerType.PlayerDamaged;

        public bool TryBind(
            ItemGameplayEventEffect effect,
            ItemGameplayEffectBindingContext context)
        {
            if (effect == null || context.PlayerHealth == null || context.ApplyEffect == null)
            {
                return false;
            }

            void Handler(PlayerDamagedSignal signal)
            {
                if (signal.PlayerHealth != context.PlayerHealth)
                {
                    return;
                }

                context.ApplyEffect(effect, signal.Position);
            }

            GameplayRuntimeEvents.PlayerDamaged += Handler;
            context.RegisterUnbind?.Invoke(() => GameplayRuntimeEvents.PlayerDamaged -= Handler);
            return true;
        }
    }
}
