using UnityEngine;

namespace CuteIssac.Core.Pooling
{
    public static class PooledEffectSpawner
    {
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

            if (!instance.TryGetComponent(out PooledEffectAutoReturn autoReturn))
            {
                autoReturn = instance.AddComponent<PooledEffectAutoReturn>();
            }

            if (instance.TryGetComponent(out PooledObject pooledObject))
            {
                pooledObject.RefreshLifecycleCallbacks();
            }

            autoReturn.Arm(fallbackLifetime);
            return instance;
        }
    }
}
