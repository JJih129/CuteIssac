using CuteIssac.Enemy;
using CuteIssac.Player;
using UnityEngine;

namespace CuteIssac.Data.Item
{
    [CreateAssetMenu(menuName = "CuteIssac/Items/Active Effects/Freeze Room", fileName = "ActiveFreezeRoomEffect")]
    public sealed class ActiveFreezeRoomEffectData : ActiveItemEffectData
    {
        [SerializeField] [Min(0.1f)] private float freezeDurationSeconds = 2.75f;

        public override bool TryApply(PlayerActiveItemController controller)
        {
            EnemyController[] enemies = Object.FindObjectsByType<EnemyController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            bool applied = false;

            for (int index = 0; index < enemies.Length; index++)
            {
                EnemyController enemy = enemies[index];

                if (enemy == null || enemy.EnemyHealth == null || enemy.EnemyHealth.IsDead)
                {
                    continue;
                }

                enemy.ApplyFreeze(freezeDurationSeconds);
                applied = true;
            }

            return applied;
        }
    }
}
