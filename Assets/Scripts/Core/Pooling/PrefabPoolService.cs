using System;
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
        public readonly struct PoolDebugSnapshot
        {
            public PoolDebugSnapshot(
                string prefabName,
                int totalCreated,
                int activeCount,
                int availableCount,
                int activeAvailableCount,
                Vector3 rootPosition,
                bool rootNearGameplayArea)
            {
                PrefabName = prefabName;
                TotalCreated = totalCreated;
                ActiveCount = activeCount;
                AvailableCount = availableCount;
                ActiveAvailableCount = activeAvailableCount;
                RootPosition = rootPosition;
                RootNearGameplayArea = rootNearGameplayArea;
            }

            public string PrefabName { get; }
            public int TotalCreated { get; }
            public int ActiveCount { get; }
            public int AvailableCount { get; }
            public int ActiveAvailableCount { get; }
            public Vector3 RootPosition { get; }
            public bool RootNearGameplayArea { get; }
            public bool HasWarning => RootNearGameplayArea || ActiveAvailableCount > 0;
        }

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
            public int TotalCreated { get; set; }
            public int ActiveCount { get; set; }
        }

        private static readonly Dictionary<GameObject, Pool> Pools = new();
        private static readonly Vector3 HiddenPoolRootPosition = new(10000f, 10000f, 0f);
        private const float GameplayAreaRootWarningDistance = 200f;
        private static Transform _serviceRoot;

        public static void Prewarm(GameObject prefab, int count)
        {
            Prewarm(prefab, count, null);
        }

        public static void Prewarm(GameObject prefab, int count, Action<GameObject> initializeInstance)
        {
            if (prefab == null || count <= 0)
            {
                return;
            }

            Pool pool = GetOrCreatePool(prefab);
            CreateAndReturn(pool, count, initializeInstance);
        }

        public static void EnsurePrewarmed(GameObject prefab, int desiredTotalCount)
        {
            EnsurePrewarmed(prefab, desiredTotalCount, null);
        }

        public static void EnsurePrewarmed(GameObject prefab, int desiredTotalCount, Action<GameObject> initializeInstance)
        {
            if (prefab == null || desiredTotalCount <= 0)
            {
                return;
            }

            Pool pool = GetOrCreatePool(prefab);
            int missingCount = Mathf.Max(0, desiredTotalCount - pool.TotalCreated);

            if (missingCount <= 0)
            {
                return;
            }

            CreateAndReturn(pool, missingCount, initializeInstance);
        }

        public static bool TryGetStats(GameObject prefab, out int totalCreated, out int activeCount, out int availableCount)
        {
            totalCreated = 0;
            activeCount = 0;
            availableCount = 0;

            if (prefab == null || !Pools.TryGetValue(prefab, out Pool pool))
            {
                return false;
            }

            totalCreated = pool.TotalCreated;
            activeCount = pool.ActiveCount;
            availableCount = pool.Available.Count;
            return true;
        }

        public static int GetDebugSnapshotCount()
        {
            return Pools.Count;
        }

        public static void CopyDebugSnapshots(List<PoolDebugSnapshot> results)
        {
            if (results == null)
            {
                return;
            }

            results.Clear();

            foreach (KeyValuePair<GameObject, Pool> pair in Pools)
            {
                Pool pool = pair.Value;
                if (pool == null)
                {
                    continue;
                }

                int activeAvailableCount = CountActiveAvailableInstances(pool);
                Vector3 rootPosition = pool.Root != null ? pool.Root.position : Vector3.zero;
                string prefabName = pool.Prefab != null ? pool.Prefab.name : "<missing prefab>";
                results.Add(new PoolDebugSnapshot(
                    prefabName,
                    pool.TotalCreated,
                    pool.ActiveCount,
                    pool.Available.Count,
                    activeAvailableCount,
                    rootPosition,
                    IsRootNearGameplayArea(rootPosition)));
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
            pool.ActiveCount++;
            pooledObject.gameObject.SetActive(true);
            pooledObject.NotifySpawned();
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
                UnityEngine.Object.Destroy(instance);
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
            poolRootObject.transform.localPosition = Vector3.zero;
            poolRootObject.transform.localRotation = Quaternion.identity;
            pool = new Pool(prefab, poolRootObject.transform);
            Pools.Add(prefab, pool);
            return pool;
        }

        private static void CreateAndReturn(Pool pool, int count, Action<GameObject> initializeInstance)
        {
            if (pool == null || count <= 0)
            {
                return;
            }

            for (int index = 0; index < count; index++)
            {
                PooledObject pooledObject = CreatePooledInstance(pool);
                initializeInstance?.Invoke(pooledObject.gameObject);
                ReturnToPool(pool, pooledObject);
            }
        }

        private static PooledObject CreatePooledInstance(Pool pool)
        {
            GameObject instance = UnityEngine.Object.Instantiate(pool.Prefab, pool.Root);
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.SetActive(false);
            PooledObject pooledObject = instance.GetComponent<PooledObject>();

            if (pooledObject == null)
            {
                pooledObject = instance.AddComponent<PooledObject>();
            }

            pooledObject.AssignSourcePrefab(pool.Prefab);
            pool.TotalCreated++;
            return pooledObject;
        }

        private static void ReturnToPool(Pool pool, PooledObject pooledObject)
        {
            if (pooledObject == null || pooledObject.IsInPool)
            {
                return;
            }

            pool.ActiveCount = Mathf.Max(0, pool.ActiveCount - 1);
            pooledObject.MarkReturned();
            pooledObject.gameObject.SetActive(false);
            pooledObject.transform.SetParent(pool.Root, false);
            pooledObject.transform.localPosition = Vector3.zero;
            pooledObject.transform.localRotation = Quaternion.identity;
            pooledObject.transform.localScale = pool.Prefab != null ? pool.Prefab.transform.localScale : Vector3.one;
            pooledObject.NotifyDespawned();
            pool.Available.Enqueue(pooledObject);
        }

        private static int CountActiveAvailableInstances(Pool pool)
        {
            if (pool == null)
            {
                return 0;
            }

            int activeCount = 0;
            foreach (PooledObject pooledObject in pool.Available)
            {
                if (pooledObject != null && pooledObject.gameObject.activeSelf)
                {
                    activeCount++;
                }
            }

            return activeCount;
        }

        private static bool IsRootNearGameplayArea(Vector3 rootPosition)
        {
            return Mathf.Abs(rootPosition.x) < GameplayAreaRootWarningDistance
                && Mathf.Abs(rootPosition.y) < GameplayAreaRootWarningDistance;
        }

        private static void EnsureRoot()
        {
            if (_serviceRoot != null)
            {
                _serviceRoot.position = HiddenPoolRootPosition;
                return;
            }

            GameObject rootObject = new("PrefabPoolService");
            rootObject.transform.position = HiddenPoolRootPosition;
            UnityEngine.Object.DontDestroyOnLoad(rootObject);
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
