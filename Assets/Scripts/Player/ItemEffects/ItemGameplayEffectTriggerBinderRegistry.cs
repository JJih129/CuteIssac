using System.Collections.Generic;
using CuteIssac.Data.Item;

namespace CuteIssac.Player.ItemEffects
{
    internal static class ItemGameplayEffectTriggerBinderRegistry
    {
        private static readonly Dictionary<GameplayEventTriggerType, IItemGameplayEffectTriggerBinder> Binders = new()
        {
            { GameplayEventTriggerType.PlayerDamaged, new PlayerDamagedGameplayEffectTriggerBinder() },
            { GameplayEventTriggerType.EnemyKilled, new EnemyKilledGameplayEffectTriggerBinder() },
            { GameplayEventTriggerType.ProjectileFired, new ProjectileFiredGameplayEffectTriggerBinder() },
            { GameplayEventTriggerType.RoomCleared, new RoomClearedGameplayEffectTriggerBinder() }
        };

        public static bool TryBind(
            ItemGameplayEventEffect effect,
            ItemGameplayEffectBindingContext context)
        {
            if (effect == null)
            {
                return false;
            }

            return Binders.TryGetValue(effect.TriggerType, out IItemGameplayEffectTriggerBinder binder)
                && binder.TryBind(effect, context);
        }
    }
}
