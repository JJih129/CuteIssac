using System.Collections.Generic;

namespace CuteIssac.Enemy
{
    public static class EnemyRegistry
    {
        private static readonly List<EnemyHealth> ActiveEnemies = new(64);
        private static readonly HashSet<int> ActiveEnemyIds = new();

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
            if (!ActiveEnemyIds.Add(instanceId))
            {
                return;
            }

            ActiveEnemies.Add(enemyHealth);
        }

        public static void Unregister(EnemyHealth enemyHealth)
        {
            if (enemyHealth == null)
            {
                return;
            }

            int instanceId = enemyHealth.GetInstanceID();
            if (!ActiveEnemyIds.Remove(instanceId))
            {
                return;
            }

            for (int i = ActiveEnemies.Count - 1; i >= 0; i--)
            {
                if (ActiveEnemies[i] != enemyHealth)
                {
                    continue;
                }

                int lastIndex = ActiveEnemies.Count - 1;
                ActiveEnemies[i] = ActiveEnemies[lastIndex];
                ActiveEnemies.RemoveAt(lastIndex);
                return;
            }
        }
    }
}
