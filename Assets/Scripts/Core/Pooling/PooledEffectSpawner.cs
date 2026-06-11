using UnityEngine;

namespace CuteIssac.Core.Pooling
{
    public static class PooledEffectSpawner
    {
        public static void Prewarm(GameObject prefab, int count)
        {
            if (prefab == null || count <= 0)
            {
                return;
            }

            PrefabPoolService.EnsurePrewarmed(prefab, count, PrepareEffectInstanceForPrewarm);
        }

        public static GameObject Spawn(
            GameObject prefab,
            Vector3 position,
            Quaternion rotation,
            Transform parent = null,
            float fallbackLifetime = 2f)
        {
            if (prefab == null)
            {
                return null;
            }

            GameObject instance = PrefabPoolService.Spawn(prefab, position, rotation, parent);
            if (instance == null)
            {
                return null;
            }

            PooledEffectAutoReturn autoReturn = PrepareEffectInstance(instance);
            autoReturn.Arm(fallbackLifetime);
            return instance;
        }

        private static PooledEffectAutoReturn PrepareEffectInstance(GameObject instance)
        {
            if (instance == null)
            {
                return null;
            }

            bool addedAutoReturn = false;

            if (!instance.TryGetComponent(out PooledEffectAutoReturn autoReturn))
            {
                autoReturn = instance.AddComponent<PooledEffectAutoReturn>();
                addedAutoReturn = true;
            }

            autoReturn.Prepare();

            if (addedAutoReturn && instance.TryGetComponent(out PooledObject pooledObject))
            {
                pooledObject.RefreshLifecycleCallbacks();
            }

            return autoReturn;
        }

        private static void PrepareEffectInstanceForPrewarm(GameObject instance)
        {
            PrepareEffectInstance(instance);
        }
    }
}
