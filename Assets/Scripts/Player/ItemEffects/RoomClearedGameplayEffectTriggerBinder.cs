using CuteIssac.Core.Gameplay;
using CuteIssac.Data.Item;
using UnityEngine;

namespace CuteIssac.Player.ItemEffects
{
    public sealed class RoomClearedGameplayEffectTriggerBinder : IItemGameplayEffectTriggerBinder
    {
        public GameplayEventTriggerType TriggerType => GameplayEventTriggerType.RoomCleared;

        public bool TryBind(
            ItemGameplayEventEffect effect,
            ItemGameplayEffectBindingContext context)
        {
            if (effect == null || context.ApplyEffect == null)
            {
                return false;
            }

            void Handler(RoomClearSignal signal)
            {
                if (effect.RequireCombatEncounter && !signal.HadCombatEncounter)
                {
                    return;
                }

                Vector3 feedbackPosition = signal.Room != null
                    ? signal.Room.CameraFocusPosition
                    : context.PlayerRoot != null
                        ? context.PlayerRoot.position
                        : Vector3.zero;
                context.ApplyEffect(effect, feedbackPosition);
            }

            GameplayRuntimeEvents.RoomCleared += Handler;
            context.RegisterUnbind?.Invoke(() => GameplayRuntimeEvents.RoomCleared -= Handler);
            return true;
        }
    }
}
