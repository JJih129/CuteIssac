using CuteIssac.Player;
using UnityEngine;

namespace CuteIssac.Item
{
    /// <summary>
    /// Shared trigger-based pickup flow.
    /// Derived classes only decide what the pickup grants when a player overlap is detected.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider2D))]
    public abstract class BasePickupLogic : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Collider2D pickupTrigger;
        [SerializeField] private PickupVisual pickupVisual;

        [Header("Pickup Behaviour")]
        [SerializeField] private bool destroyOnCollected = true;
        [SerializeField] [Min(0f)] private float destroyDelay = 0f;

        private bool _isCollected;

        protected PickupVisual PickupVisual => pickupVisual;

        protected virtual void Awake()
        {
            ResolveReferences();
            ConfigureTriggerCollider();
        }

        protected virtual void OnTriggerEnter2D(Collider2D other)
        {
            TryCollect(other);
        }

        protected virtual void OnTriggerStay2D(Collider2D other)
        {
            TryCollect(other);
        }

        protected bool TryResolveCollector(
            Collider2D other,
            out PlayerInventory playerInventory,
            out PlayerHealth playerHealth,
            out PlayerItemManager playerItemManager)
        {
            playerInventory = null;
            playerHealth = null;
            playerItemManager = null;

            if (other == null)
            {
                return false;
            }

            playerInventory = other.GetComponentInParent<PlayerInventory>();
            playerHealth = other.GetComponentInParent<PlayerHealth>();
            playerItemManager = other.GetComponentInParent<PlayerItemManager>();

            return playerInventory != null || playerHealth != null || playerItemManager != null;
        }

        /// <summary>
        /// Pickups should never physically block the collector.
        /// We ignore all colliders on the collector root so the pickup remains overlap-only even if inspector physics settings drift.
        /// </summary>
        protected void IgnoreCollectorCollisions(Collider2D other)
        {
            if (pickupTrigger == null || other == null)
            {
                return;
            }

            Collider2D[] collectorColliders = other.GetComponentsInParent<Collider2D>(true);

            for (int i = 0; i < collectorColliders.Length; i++)
            {
                Collider2D collectorCollider = collectorColliders[i];

                if (collectorCollider != null && collectorCollider != pickupTrigger)
                {
                    Physics2D.IgnoreCollision(pickupTrigger, collectorCollider, true);
                }
            }
        }

        protected void CompleteCollection()
        {
            if (_isCollected)
            {
                return;
            }

            _isCollected = true;
            pickupVisual?.HandleCollected();

            if (pickupTrigger != null)
            {
                pickupTrigger.enabled = false;
            }

            if (destroyOnCollected)
            {
                Destroy(gameObject, destroyDelay);
            }
        }

        private void TryCollect(Collider2D other)
        {
            if (_isCollected || !enabled)
            {
                return;
            }

            if (!TryResolveCollector(other, out PlayerInventory inventory, out PlayerHealth health, out PlayerItemManager itemManager))
            {
                return;
            }

            IgnoreCollectorCollisions(other);

            if (TryCollect(inventory, health, itemManager))
            {
                CompleteCollection();
            }
        }

        protected abstract bool TryCollect(PlayerInventory inventory, PlayerHealth health, PlayerItemManager itemManager);

        private void ResolveReferences()
        {
            if (pickupTrigger == null)
            {
                TryGetComponent(out pickupTrigger);
            }

            if (pickupVisual == null)
            {
                TryGetComponent(out pickupVisual);
            }
        }

        private void ConfigureTriggerCollider()
        {
            if (pickupTrigger != null)
            {
                pickupTrigger.isTrigger = true;
            }
        }

        protected virtual void Reset()
        {
            ResolveReferences();
            ConfigureTriggerCollider();
        }

        protected virtual void OnValidate()
        {
            ResolveReferences();
            ConfigureTriggerCollider();
        }
    }
}
