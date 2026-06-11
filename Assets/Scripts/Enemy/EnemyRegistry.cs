using System.Collections.Generic;

namespace CuteIssac.Enemy
{
    public static class EnemyRegistry
    {
        private static readonly List<EnemyHealth> ActiveEnemies = new(64);
        private static readonly Dictionary<int, int> ActiveEnemyIndicesById = new(64);

        public static int Count => ActiveEnemies.Count;

        public static EnemyHealth GetAt(int index)
        {
            return index >= 0 && index < ActiveEnemies.Count ? ActiveEnemies[index] : null;
        }

        public static void Register(EnemyHealth enemyHealth)
        {
            if (enemyHealth == null)
            {
                return;
            }

            int instanceId = enemyHealth.GetInstanceID();
            if (ActiveEnemyIndicesById.ContainsKey(instanceId))
            {
                return;
            }

            ActiveEnemyIndicesById.Add(instanceId, ActiveEnemies.Count);
            ActiveEnemies.Add(enemyHealth);
        }

        public static void Unregister(EnemyHealth enemyHealth)
        {
            if (enemyHealth == null)
            {
                return;
            }

            int instanceId = enemyHealth.GetInstanceID();
            if (!ActiveEnemyIndicesById.TryGetValue(instanceId, out int index))
            {
                return;
            }

            int lastIndex = ActiveEnemies.Count - 1;
            ActiveEnemyIndicesById.Remove(instanceId);

            if (index < 0 || index > lastIndex)
            {
                return;
            }

            if (index != lastIndex)
            {
                EnemyHealth movedEnemy = ActiveEnemies[lastIndex];
                ActiveEnemies[index] = movedEnemy;
                if (movedEnemy != null)
                {
                    ActiveEnemyIndicesById[movedEnemy.GetInstanceID()] = index;
                }
            }

            ActiveEnemies.RemoveAt(lastIndex);
        }
    }
}
