using CuteIssac.Player;
using UnityEngine;

namespace CuteIssac.Data.Item
{
    [CreateAssetMenu(menuName = "CuteIssac/Items/Active Effects/Bomb Rain", fileName = "ActiveBombRainEffect")]
    public sealed class ActiveBombRainEffectData : ActiveItemEffectData
    {
        [SerializeField] [Min(1)] private int bombCount = 4;
        [SerializeField] [Min(0f)] private float spawnRadius = 1.25f;

        public override bool TryApply(PlayerActiveItemController controller)
        {
            return controller != null && controller.TryTriggerBombRain(bombCount, spawnRadius);
        }
    }
}
