using UnityEngine;

namespace CuteIssac.Core.Pooling
{
    [DisallowMultipleComponent]
    public sealed class PooledObject : MonoBehaviour
    {
        [SerializeField] private GameObject sourcePrefab;

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
    }
}
