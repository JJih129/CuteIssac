using CuteIssac.Common.Stats;
using CuteIssac.Player;
using UnityEngine;

namespace CuteIssac.Data.Item
{
    [CreateAssetMenu(menuName = "CuteIssac/Items/Active Effects/Battle Focus", fileName = "ActiveBattleFocusEffect")]
    public sealed class ActiveBattleFocusEffectData : ActiveItemEffectData
    {
        [SerializeField]
        [Min(0.1f)]
        private float durationSeconds = 8f;

        [SerializeField]
        private StatModifier[] temporaryStatModifiers =
        {
            new(PlayerStatType.Damage, StatModifierOperation.Add, 2f),
        };

        [SerializeField]
        private ProjectileModifier[] temporaryProjectileModifiers;

        public override bool TryApply(PlayerActiveItemController controller)
        {
            return controller != null
                && controller.TryApplyTimedEffect(durationSeconds, temporaryStatModifiers, temporaryProjectileModifiers);
        }

        public override bool TryRestore(PlayerActiveItemController controller, float remainingSeconds)
        {
            return controller != null
                && controller.TryRestoreTimedEffect(remainingSeconds, durationSeconds, temporaryStatModifiers, temporaryProjectileModifiers);
        }
    }
}
