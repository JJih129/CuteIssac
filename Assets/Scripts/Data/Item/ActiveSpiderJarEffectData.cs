using CuteIssac.Core.Spawning;
using CuteIssac.Player;
using UnityEngine;

namespace CuteIssac.Data.Item
{
    [CreateAssetMenu(menuName = "CuteIssac/Items/Active Effects/Spider Jar", fileName = "ActiveSpiderJarEffect")]
    public sealed class ActiveSpiderJarEffectData : ActiveItemEffectData
    {
        [SerializeField] private PlayerSpiderMinionController spiderMinionPrefab;
        [SerializeField] [Min(1)] private int summonCount = 3;
        [SerializeField] [Min(0f)] private float summonRadius = 0.9f;
        [SerializeField] private SpawnReusePolicy reusePolicy = SpawnReusePolicy.Instantiate;

        public override bool TryApply(PlayerActiveItemController controller)
        {
            if (controller == null || spiderMinionPrefab == null || summonCount <= 0)
            {
                return false;
            }

            Vector3 center = controller.transform.position;
            bool spawnedAny = false;

            for (int index = 0; index < summonCount; index++)
            {
                float angle = summonCount == 1 ? 0f : (360f / summonCount) * index;
                float radians = angle * Mathf.Deg2Rad;
                Vector3 offset = new(Mathf.Cos(radians), Mathf.Sin(radians), 0f);
                Vector3 spawnPosition = center + (offset * summonRadius);
                PlayerSpiderMinionController minion = GameplaySpawnFactory.SpawnComponent(
                    spiderMinionPrefab,
                    spawnPosition,
                    Quaternion.identity,
                    null,
                    reusePolicy);

                if (minion == null)
                {
                    continue;
                }

                minion.Initialize(controller.transform);
                spawnedAny = true;
            }

            return spawnedAny;
        }
    }
}
