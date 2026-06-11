using System.Collections.Generic;
using UnityEngine;

namespace CuteIssac.Core.Pooling
{
    [DisallowMultipleComponent]
    public sealed class PooledObject : MonoBehaviour
    {
        [SerializeField] private GameObject sourcePrefab;

        private readonly List<IPooledObjectLifecycle> _lifecycleCallbacks = new();
        private bool _hasScannedLifecycleCallbacks;

        public GameObject SourcePrefab => sourcePrefab;
        public bool IsInPool { get; private set; }

        public void AssignSourcePrefab(GameObject prefab)
        {
            sourcePrefab = prefab;
        }

        public void MarkSpawned()
        {
            IsInPool = false;
        }

        public void MarkReturned()
        {
            IsInPool = true;
        }

        public void NotifySpawned()
        {
            EnsureLifecycleCallbacks();

            for (int i = 0; i < _lifecycleCallbacks.Count; i++)
            {
                _lifecycleCallbacks[i]?.OnPoolSpawned();
            }
        }

        public void NotifyDespawned()
        {
            EnsureLifecycleCallbacks();

            for (int i = 0; i < _lifecycleCallbacks.Count; i++)
            {
                _lifecycleCallbacks[i]?.OnPoolDespawned();
            }
        }

        public void RefreshLifecycleCallbacks()
        {
            _hasScannedLifecycleCallbacks = false;
            EnsureLifecycleCallbacks();
        }

        private void EnsureLifecycleCallbacks()
        {
            if (_hasScannedLifecycleCallbacks)
            {
                return;
            }

            GetComponentsInChildren(true, _lifecycleCallbacks);
            _hasScannedLifecycleCallbacks = true;
        }
    }
}
