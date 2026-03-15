using CuteIssac.Combat;
using UnityEngine;

namespace CuteIssac.Environment
{
    /// <summary>
    /// Generic bomb-reactive object used for breakable props and secret wall reveals.
    /// It exposes only data references so visuals and colliders can be swapped in the inspector.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BombReactiveObject : MonoBehaviour, IBombReactive
    {
        [Header("Behavior")]
        [SerializeField] private BombReactiveMode reactiveMode = BombReactiveMode.Breakable;
        [SerializeField] private bool triggerOnce = true;
        [SerializeField] private bool destroyRootAfterTrigger;
        [SerializeField] [Min(0f)] private float destroyDelay;

        [Header("Targets")]
        [SerializeField] private Collider2D[] collidersToDisable = new Collider2D[0];
        [SerializeField] private Renderer[] renderersToHide = new Renderer[0];
        [SerializeField] private GameObject[] objectsToHide = new GameObject[0];
        [SerializeField] private GameObject[] objectsToShow = new GameObject[0];

        private bool _hasTriggered;

        public void ReactToBomb(in BombExplosionInfo explosionInfo)
        {
            if (_hasTriggered && triggerOnce)
            {
                return;
            }

            _hasTriggered = true;

            for (int index = 0; index < collidersToDisable.Length; index++)
            {
                if (collidersToDisable[index] != null)
                {
                    collidersToDisable[index].enabled = false;
                }
            }

            for (int index = 0; index < renderersToHide.Length; index++)
            {
                if (renderersToHide[index] != null)
                {
                    renderersToHide[index].enabled = false;
                }
            }

            for (int index = 0; index < objectsToHide.Length; index++)
            {
                if (objectsToHide[index] != null)
                {
                    objectsToHide[index].SetActive(false);
                }
            }

            for (int index = 0; index < objectsToShow.Length; index++)
            {
                if (objectsToShow[index] != null)
                {
                    objectsToShow[index].SetActive(true);
                }
            }

            if (destroyRootAfterTrigger && reactiveMode == BombReactiveMode.Breakable)
            {
                Destroy(gameObject, destroyDelay);
            }
        }
    }
}
