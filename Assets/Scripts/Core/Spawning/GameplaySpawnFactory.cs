using CuteIssac.Core.Pooling;
using UnityEngine;

namespace CuteIssac.Core.Spawning
{
    public static class GameplaySpawnFactory
    {
        public static GameObject SpawnGameObject(
            GameObject prefab,
            Vector3 position,
            Quaternion rotation,
            Transform parent = null,
            SpawnReusePolicy reusePolicy = SpawnReusePolicy.Instantiate)
        {
            if (prefab == null)
            {
                return null;
            }

            GameObject instance = SpawnInternal(prefab, position, rotation, parent, reusePolicy);
            GameplaySpawnTelemetry.RecordSpawn(prefab, reusePolicy, instance != null);
            return instance;
        }

        public static T SpawnComponent<T>(
            T prefab,
            Vector3 position,
            Quaternion rotation,
            Transform parent = null,
            SpawnReusePolicy reusePolicy = SpawnReusePolicy.Instantiate)
            where T : Component
        {
            if (prefab == null)
            {
                return null;
            }

            GameObject instanceObject = SpawnInternal(prefab.gameObject, position, rotation, parent, reusePolicy);
            GameplaySpawnTelemetry.RecordSpawn(prefab.gameObject, reusePolicy, instanceObject != null);

            if (instanceObject == null)
            {
                return null;
            }

            T instance = instanceObject.GetComponent<T>();

            if (instance != null)
            {
                return instance;
            }

            GameplaySpawnTelemetry.RecordValidationFailure(prefab.gameObject);
            UnityEngine.Debug.LogError(
                $"GameplaySpawnFactory spawned prefab '{prefab.name}' without required component '{typeof(T).Name}'.",
                instanceObject);
            PrefabPoolService.Return(instanceObject);
            return null;
        }

        private static GameObject SpawnInternal(
            GameObject prefab,
            Vector3 position,
            Quaternion rotation,
            Transform parent,
            SpawnReusePolicy reusePolicy)
        {
            if (reusePolicy == SpawnReusePolicy.Pooled)
            {
                return PrefabPoolService.Spawn(prefab, position, rotation, parent);
            }

            GameObject instance = Object.Instantiate(prefab, position, rotation);

            if (instance != null && parent != null)
            {
                instance.transform.SetParent(parent, true);
            }

            return instance;
        }
    }
}
