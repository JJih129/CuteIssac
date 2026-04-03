using CuteIssac.Core.Gameplay;
using CuteIssac.Data.Item;

namespace CuteIssac.Player.ItemEffects
{
    public sealed class ProjectileFiredGameplayEffectTriggerBinder : IItemGameplayEffectTriggerBinder
    {
        public GameplayEventTriggerType TriggerType => GameplayEventTriggerType.ProjectileFired;

        public bool TryBind(
            ItemGameplayEventEffect effect,
            ItemGameplayEffectBindingContext context)
        {
            if (effect == null || context.ApplyEffect == null)
            {
                return false;
            }

            void Handler(ProjectileFiredSignal signal)
            {
                if (effect.RequirePlayerSource && (context.IsOwnedByPlayer == null || !context.IsOwnedByPlayer(signal.Source)))
                {
                    return;
                }

                context.ApplyEffect(effect, signal.Origin);
            }

            GameplayRuntimeEvents.ProjectileFired += Handler;
            context.RegisterUnbind?.Invoke(() => GameplayRuntimeEvents.ProjectileFired -= Handler);
            return true;
        }
    }
}
