using System.Collections.Generic;
using UnityEngine;

namespace CuteIssac.Core.Pooling
{
    /// <summary>
    /// Lightweight prefab-keyed pooling service for frequently spawned gameplay objects.
    /// It is intentionally small so projectile/effect systems can opt in without depending on a huge global manager.
    /// </summary>
    public static class PrefabPoolService
    {
        private sealed class Pool
        {
            public Pool(GameObject prefab, Transform root)
            {
                Prefab = prefab;
                Root = root;
            }

            public GameObject Prefab { get; }
            public Transform Root { get; }
            public Queue<PooledObject> Available { get; } = new();
        }

        private static readonly Dictionary<GameObject, Pool> Pools = new();
        private static Transform _serviceRoot;

        public static void Prewarm(GameObject prefab, int count)
        {
            if (prefab == null || count <= 0)
            {
                return;
            }

            Pool pool = GetOrCreatePool(prefab);

            for (int index = 0; index < count; index++)
            {
                PooledObject pooledObject = CreatePooledInstance(pool);
                ReturnToPool(pool, pooledObject);
            }
        }

        public static T Spawn<T>(T prefab, Vector3 position, Quaternion rotation, Transform parent = null) where T : Component
        {
            if (prefab == null)
            {
                return null;
            }

            GameObject instance = Spawn(prefab.gameObject, position, rotation, parent);
            return instance != null ? instance.GetComponent<T>() : null;
        }

        public static GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            if (prefab == null)
            {
                return null;
            }

            Pool pool = GetOrCreatePool(prefab);
            PooledObject pooledObject = null;

            while (pool.Available.Count > 0 && pooledObject == null)
            {
                pooledObject = pool.Available.Dequeue();
            }

            if (pooledObject == null)
            {
                pooledObject = CreatePooledInstance(pool);
            }

            Transform transform = pooledObject.transform;
            if (transform is RectTransform rectTransform && prefab.transform is RectTransform prefabRectTransform && parent is RectTransform)
            {
                rectTransform.SetParent(parent, false);
                ResetRectTransform(rectTransform, prefabRectTransform);
            }
            else
            {
                transform.SetParent(parent, false);
                transform.SetPositionAndRotation(position, rotation);
            }

            pooledObject.MarkSpawned();
            pooledObject.gameObject.SetActive(true);
            return pooledObject.gameObject;
        }

        public static void Return(GameObject instance)
        {
            if (instance == null)
            {
                return;
            }

            PooledObject pooledObject = instance.GetComponent<PooledObject>();

            if (pooledObject == null || pooledObject.SourcePrefab == null)
            {
                Object.Destroy(instance);
                return;
            }

            Pool pool = GetOrCreatePool(pooledObject.SourcePrefab);
            ReturnToPool(pool, pooledObject);
        }

        public static void Return(Component instance)
        {
            if (instance != null)
            {
                Return(instance.gameObject);
            }
        }

        private static Pool GetOrCreatePool(GameObject prefab)
        {
            if (Pools.TryGetValue(prefab, out Pool pool))
            {
                return pool;
            }

            EnsureRoot();
            GameObject poolRootObject = new($"Pool_{prefab.name}");
            poolRootObject.transform.SetParent(_serviceRoot, false);
            pool = new Pool(prefab, poolRootObject.transform);
            Pools.Add(prefab, pool);
            return pool;
        }

        private static PooledObject CreatePooledInstance(Pool pool)
        {
            GameObject instance = Object.Instantiate(pool.Prefab, pool.Root);
            PooledObject pooledObject = instance.GetComponent<PooledObject>();

            if (pooledObject == null)
            {
                pooledObject = instance.AddComponent<PooledObject>();
            }

            pooledObject.AssignSourcePrefab(pool.Prefab);
            return pooledObject;
        }

        private static void ReturnToPool(Pool pool, PooledObject pooledObject)
        {
            if (pooledObject == null || pooledObject.IsInPool)
            {
                return;
            }

            pooledObject.MarkReturned();
            pooledObject.transform.SetParent(pool.Root, false);
            pooledObject.gameObject.SetActive(false);
            pool.Available.Enqueue(pooledObject);
        }

        private static void EnsureRoot()
        {
            if (_serviceRoot != null)
            {
                return;
            }

            GameObject rootObject = new("PrefabPoolService");
            Object.DontDestroyOnLoad(rootObject);
            _serviceRoot = rootObject.transform;
        }

        private static void ResetRectTransform(RectTransform target, RectTransform source)
        {
            target.anchorMin = source.anchorMin;
            target.anchorMax = source.anchorMax;
            target.anchoredPosition3D = source.anchoredPosition3D;
            target.sizeDelta = source.sizeDelta;
            target.pivot = source.pivot;
            target.localRotation = source.localRotation;
            target.localScale = source.localScale;
            target.offsetMin = source.offsetMin;
            target.offsetMax = source.offsetMax;
        }
    }
}
