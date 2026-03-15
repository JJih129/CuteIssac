using CuteIssac.Player;
using UnityEngine;

namespace CuteIssac.Data.Item
{
    [CreateAssetMenu(menuName = "CuteIssac/Items/Active Effects/Coin Burst", fileName = "ActiveCoinBurstEffect")]
    public sealed class ActiveCoinBurstEffectData : ActiveItemEffectData
    {
        [SerializeField]
        [Min(1)]
        private int coinAmount = 6;

        public override bool TryApply(PlayerActiveItemController controller)
        {
            return controller != null && controller.TryGrantCoins(coinAmount);
        }
    }
}
