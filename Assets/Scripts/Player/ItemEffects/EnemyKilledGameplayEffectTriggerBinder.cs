using CuteIssac.Core.Gameplay;
using CuteIssac.Data.Item;

namespace CuteIssac.Player.ItemEffects
{
    public sealed class EnemyKilledGameplayEffectTriggerBinder : IItemGameplayEffectTriggerBinder
    {
        public GameplayEventTriggerType TriggerType => GameplayEventTriggerType.EnemyKilled;

        public bool TryBind(
            ItemGameplayEventEffect effect,
            ItemGameplayEffectBindingContext context)
        {
            if (effect == null || context.ApplyEffect == null)
            {
                return false;
            }

            void Handler(EnemyKilledSignal signal)
            {
                if (effect.RequirePlayerSource && (context.IsOwnedByPlayer == null || !context.IsOwnedByPlayer(signal.Killer)))
                {
                    return;
                }

                context.ApplyEffect(effect, signal.Position);
            }

            GameplayRuntimeEvents.EnemyKilled += Handler;
            context.RegisterUnbind?.Invoke(() => GameplayRuntimeEvents.EnemyKilled -= Handler);
            return true;
        }
    }
}
