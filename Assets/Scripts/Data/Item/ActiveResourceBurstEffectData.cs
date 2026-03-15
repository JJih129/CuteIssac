using CuteIssac.Player;
using UnityEngine;

namespace CuteIssac.Data.Item
{
    [CreateAssetMenu(menuName = "CuteIssac/Items/Active Effects/Resource Burst", fileName = "ActiveResourceBurstEffect")]
    public sealed class ActiveResourceBurstEffectData : ActiveItemEffectData
    {
        [SerializeField] [Min(0)] private int coinAmount;
        [SerializeField] [Min(0)] private int keyAmount;
        [SerializeField] [Min(0)] private int bombAmount;

        public override bool TryApply(PlayerActiveItemController controller)
        {
            if (controller == null)
            {
                return false;
            }

            bool applied = false;

            if (coinAmount > 0)
            {
                applied |= controller.TryGrantCoins(coinAmount);
            }

            if (keyAmount > 0)
            {
                applied |= controller.TryGrantKeys(keyAmount);
            }

            if (bombAmount > 0)
            {
                applied |= controller.TryGrantBombs(bombAmount);
            }

            return applied;
        }
    }
}
