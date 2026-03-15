using CuteIssac.Player;
using UnityEngine;

namespace CuteIssac.Data.Item
{
    [CreateAssetMenu(menuName = "CuteIssac/Items/Active Effects/Heal", fileName = "ActiveHealEffect")]
    public sealed class ActiveHealEffectData : ActiveItemEffectData
    {
        [SerializeField]
        [Min(1)]
        private int healAmount = 2;

        public override bool TryApply(PlayerActiveItemController controller)
        {
            return controller != null && controller.TryRestoreHealth(healAmount);
        }
    }
}
