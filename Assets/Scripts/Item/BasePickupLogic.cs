using System;
using CuteIssac.Core.Audio;
using CuteIssac.Core.Feedback;
using CuteIssac.Core.Pooling;
using CuteIssac.Data.Visual;
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

        [Header("Pickup Range Indicator")]
        [SerializeField] [Min(12)] private int pickupRangeIndicatorSegments = 48;
        [SerializeField] [Min(0.005f)] private float pickupRangeIndicatorWidth = 0.025f;
        [SerializeField] [Min(0f)] private float pickupRangeIndicatorPadding = 0.03f;
        [Tooltip("비워두면 Resources/Sorting/DefaultSortingOrderProfile 기준을 사용합니다. 픽업 표시선이 픽업 본체와 같은 계층 기준을 따르게 합니다.")]
        [SerializeField] private SortingOrderProfile sortingOrderProfile;
        [SerializeField] private int pickupRangeIndicatorSortingOrder = 9;
        [SerializeField] private Color pickupRangeIndicatorColor = new(1f, 0.86f, 0.34f, 0.66f);

        private bool _isCollected;
        private bool _collectorCollisionsIgnored;
        private bool _hasCachedPickupFeedbackLabel;
        private string _cachedPickupFeedbackLabel;
        private Coroutine _releaseCoroutine;
        private LineRenderer _pickupRangeIndicator;
        private static Material s_RangeIndicatorMaterial;
        private static PlayerController s_cachedCollectorController;
        private static PlayerInventory s_cachedCollectorInventory;
        private static PlayerHealth s_cachedCollectorHealth;
        private static PlayerItemManager s_cachedCollectorItemManager;
        private static Collider2D[] s_cachedCollectorColliders;

        public event Action<BasePickupLogic> Collected;

        protected PickupVisual PickupVisual => pickupVisual;
        public string PreviewFeedbackLabel => ResolvePickupFeedbackLabel();
        public Color PreviewFeedbackColor => ResolvePickupFeedbackColor();

        protected virtual void Awake()
        {
            ResolveReferences();
            ConfigureTriggerCollider();
            ConfigurePickupRangeIndicator();
        }

        protected virtual void OnEnable()
        {
            _isCollected = false;
            _collectorCollisionsIgnored = false;
            InvalidatePickupFeedbackCache();

            if (_releaseCoroutine != null)
            {
                StopCoroutine(_releaseCoroutine);
                _releaseCoroutine = null;
            }

            if (pickupTrigger != null)
            {
                pickupTrigger.enabled = true;
            }

            RefreshPickupRangeIndicator();
            pickupVisual?.ResetPresentation();
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

            if (TryResolveActiveCollector(other, out playerInventory, out playerHealth, out playerItemManager))
            {
                return true;
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
            if (_collectorCollisionsIgnored || pickupTrigger == null || other == null)
            {
                return;
            }

            if (TryResolveActiveCollectorColliders(other, out Collider2D[] cachedCollectorColliders))
            {
                for (int i = 0; i < cachedCollectorColliders.Length; i++)
                {
                    Collider2D collectorCollider = cachedCollectorColliders[i];

                    if (collectorCollider != null && collectorCollider != pickupTrigger)
                    {
                        Physics2D.IgnoreCollision(pickupTrigger, collectorCollider, true);
                    }
                }

                _collectorCollisionsIgnored = true;
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

            _collectorCollisionsIgnored = true;
        }

        protected void CompleteCollection()
        {
            if (_isCollected)
            {
                return;
            }

            _isCollected = true;
            GameplayFeedbackEvents.RaiseFloatingFeedback(new FloatingFeedbackRequest(
                ResolvePickupFeedbackPosition(),
                ResolvePickupFeedbackLabel(),
                ResolvePickupFeedbackColor(),
                0.7f,
                0.75f,
                1.08f,
                visualProfile: FloatingFeedbackVisualProfile.Pickup));
            Collected?.Invoke(this);
            pickupVisual?.HandleCollected();
            GameAudioEvents.Raise(GameAudioEventType.ItemCollected, transform.position);

            if (pickupTrigger != null)
            {
                pickupTrigger.enabled = false;
            }

            SetPickupRangeIndicatorActive(false);

            if (destroyOnCollected)
            {
                ReleasePickup(destroyDelay, playCollectedVisual: false);
            }
        }

        public void ReleasePickup(float delay = 0f, bool playCollectedVisual = true)
        {
            if (_releaseCoroutine != null)
            {
                StopCoroutine(_releaseCoroutine);
                _releaseCoroutine = null;
            }

            if (pickupTrigger != null)
            {
                pickupTrigger.enabled = false;
            }

            SetPickupRangeIndicatorActive(false);

            if (playCollectedVisual)
            {
                pickupVisual?.HandleCollected();
            }

            float releaseDelay = Mathf.Max(0f, delay);

            if (releaseDelay <= 0f)
            {
                PrefabPoolService.Return(gameObject);
                return;
            }

            _releaseCoroutine = StartCoroutine(ReleaseAfterDelay(releaseDelay));
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

        protected void InvalidatePickupFeedbackCache()
        {
            _hasCachedPickupFeedbackLabel = false;
            _cachedPickupFeedbackLabel = null;
        }

        private string ResolvePickupFeedbackLabel()
        {
            if (_hasCachedPickupFeedbackLabel)
            {
                return _cachedPickupFeedbackLabel;
            }

            _cachedPickupFeedbackLabel = BuildPickupFeedbackLabel();
            _hasCachedPickupFeedbackLabel = true;
            return _cachedPickupFeedbackLabel;
        }

        private static bool TryResolveActiveCollector(
            Collider2D other,
            out PlayerInventory playerInventory,
            out PlayerHealth playerHealth,
            out PlayerItemManager playerItemManager)
        {
            playerInventory = null;
            playerHealth = null;
            playerItemManager = null;

            PlayerController activeController = PlayerRegistry.ActiveController;
            if (activeController == null || other == null || !IsColliderUnderActiveCollector(other, activeController))
            {
                return false;
            }

            RefreshActiveCollectorCache(activeController);
            playerInventory = s_cachedCollectorInventory;
            playerHealth = s_cachedCollectorHealth;
            playerItemManager = s_cachedCollectorItemManager;
            return playerInventory != null || playerHealth != null || playerItemManager != null;
        }

        private static bool TryResolveActiveCollectorColliders(Collider2D other, out Collider2D[] collectorColliders)
        {
            collectorColliders = null;

            PlayerController activeController = PlayerRegistry.ActiveController;
            if (activeController == null || other == null || !IsColliderUnderActiveCollector(other, activeController))
            {
                return false;
            }

            RefreshActiveCollectorCache(activeController);
            collectorColliders = s_cachedCollectorColliders;
            return collectorColliders != null && collectorColliders.Length > 0;
        }

        private static bool IsColliderUnderActiveCollector(Collider2D other, PlayerController activeController)
        {
            if (other == null || activeController == null)
            {
                return false;
            }

            Transform collectorRoot = activeController.transform;
            Transform otherTransform = other.transform;
            return otherTransform == collectorRoot || otherTransform.IsChildOf(collectorRoot);
        }

        private static void RefreshActiveCollectorCache(PlayerController activeController)
        {
            if (activeController == null)
            {
                s_cachedCollectorController = null;
                s_cachedCollectorInventory = null;
                s_cachedCollectorHealth = null;
                s_cachedCollectorItemManager = null;
                s_cachedCollectorColliders = null;
                return;
            }

            if (s_cachedCollectorController != activeController)
            {
                s_cachedCollectorController = activeController;
                s_cachedCollectorInventory = null;
                s_cachedCollectorHealth = null;
                s_cachedCollectorItemManager = null;
                s_cachedCollectorColliders = activeController.GetComponentsInChildren<Collider2D>(true);
            }

            if (s_cachedCollectorInventory == null)
            {
                PlayerInventory activeInventory = PlayerRegistry.ActiveInventory;
                s_cachedCollectorInventory = PlayerRegistry.IsComponentUnderTransform(activeInventory, activeController.transform)
                    ? activeInventory
                    : activeController.GetComponent<PlayerInventory>();
            }

            if (s_cachedCollectorHealth == null)
            {
                PlayerHealth activeHealth = PlayerRegistry.ActiveHealth;
                s_cachedCollectorHealth = PlayerRegistry.IsComponentUnderTransform(activeHealth, activeController.transform)
                    ? activeHealth
                    : activeController.GetComponent<PlayerHealth>();
            }

            if (s_cachedCollectorItemManager == null)
            {
                PlayerItemManager activeItemManager = PlayerRegistry.ActiveItemManager;
                s_cachedCollectorItemManager = PlayerRegistry.IsComponentUnderTransform(activeItemManager, activeController.transform)
                    ? activeItemManager
                    : activeController.GetComponent<PlayerItemManager>();
            }
        }

        protected virtual string BuildPickupFeedbackLabel()
        {
            return gameObject.name
                .Replace("(Clone)", string.Empty)
                .Replace("Pickup", string.Empty)
                .Trim()
                .ToUpperInvariant();
        }

        protected virtual Color ResolvePickupFeedbackColor()
        {
            return new Color(0.48f, 1f, 0.72f, 1f);
        }

        protected virtual bool ShouldShowPickupRangeIndicator()
        {
            return false;
        }

        protected virtual Color ResolvePickupRangeIndicatorColor()
        {
            return pickupRangeIndicatorColor;
        }

        protected void RefreshPickupRangeIndicator()
        {
            ConfigurePickupRangeIndicator();
            if (_pickupRangeIndicator == null)
            {
                return;
            }

            bool shouldShow = ShouldShowPickupRangeIndicator() && pickupTrigger != null && pickupTrigger.enabled && !_isCollected;
            _pickupRangeIndicator.gameObject.SetActive(shouldShow);
            if (!shouldShow)
            {
                return;
            }

            Color indicatorColor = ResolvePickupRangeIndicatorColor();
            _pickupRangeIndicator.startColor = indicatorColor;
            _pickupRangeIndicator.endColor = indicatorColor;
        }

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

        private Vector3 ResolvePickupFeedbackPosition()
        {
            Vector3 anchorPosition = pickupVisual != null && pickupVisual.PickupEffectAnchor != null
                ? pickupVisual.PickupEffectAnchor.position
                : transform.position;
            return anchorPosition + new Vector3(0.06f, 0.18f, 0f);
        }

        private void ConfigureTriggerCollider()
        {
            if (pickupTrigger != null)
            {
                pickupTrigger.isTrigger = true;
            }
        }

        private void ConfigurePickupRangeIndicator()
        {
            if (!ShouldShowPickupRangeIndicator())
            {
                SetPickupRangeIndicatorActive(false);
                return;
            }

            if (pickupTrigger == null)
            {
                return;
            }

            CircleCollider2D circleCollider = pickupTrigger as CircleCollider2D;
            if (circleCollider == null)
            {
                SetPickupRangeIndicatorActive(false);
                return;
            }

            if (_pickupRangeIndicator == null)
            {
                Transform existingIndicator = transform.Find("PickupRangeIndicator");
                if (existingIndicator != null)
                {
                    _pickupRangeIndicator = existingIndicator.GetComponent<LineRenderer>();
                }

                if (_pickupRangeIndicator == null)
                {
                    GameObject indicatorObject = new("PickupRangeIndicator");
                    indicatorObject.transform.SetParent(transform, false);
                    indicatorObject.transform.localPosition = Vector3.zero;
                    _pickupRangeIndicator = indicatorObject.AddComponent<LineRenderer>();
                }
            }

            _pickupRangeIndicator.sharedMaterial = ResolveRangeIndicatorMaterial();
            _pickupRangeIndicator.useWorldSpace = false;
            _pickupRangeIndicator.loop = true;
            _pickupRangeIndicator.textureMode = LineTextureMode.Stretch;
            _pickupRangeIndicator.numCapVertices = 2;
            _pickupRangeIndicator.numCornerVertices = 2;
            _pickupRangeIndicator.widthMultiplier = pickupRangeIndicatorWidth;
            _pickupRangeIndicator.sortingOrder = ResolvePickupRangeIndicatorSortingOrder();

            int segmentCount = Mathf.Max(12, pickupRangeIndicatorSegments);
            float radius = Mathf.Max(0.01f, circleCollider.radius + pickupRangeIndicatorPadding);
            Vector2 offset = circleCollider.offset;
            _pickupRangeIndicator.positionCount = segmentCount;

            for (int index = 0; index < segmentCount; index++)
            {
                float angle = (index / (float)segmentCount) * Mathf.PI * 2f;
                _pickupRangeIndicator.SetPosition(
                    index,
                    new Vector3(
                        offset.x + Mathf.Cos(angle) * radius,
                        offset.y + Mathf.Sin(angle) * radius,
                        -0.01f));
            }
        }

        private void SetPickupRangeIndicatorActive(bool active)
        {
            if (_pickupRangeIndicator != null)
            {
                _pickupRangeIndicator.gameObject.SetActive(active);
            }
        }

        private int ResolvePickupRangeIndicatorSortingOrder()
        {
            return SortingOrderProfile.ResolvePickupOrder(sortingOrderProfile, pickupRangeIndicatorSortingOrder);
        }

        private static Material ResolveRangeIndicatorMaterial()
        {
            if (s_RangeIndicatorMaterial != null)
            {
                return s_RangeIndicatorMaterial;
            }

            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null)
            {
                shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            }

            s_RangeIndicatorMaterial = new Material(shader)
            {
                name = "RuntimePickupRangeIndicatorMaterial",
                hideFlags = HideFlags.HideAndDontSave
            };
            return s_RangeIndicatorMaterial;
        }

        protected virtual void Reset()
        {
            InvalidatePickupFeedbackCache();
            ResolveReferences();
            ConfigureTriggerCollider();
        }

        protected virtual void OnValidate()
        {
            InvalidatePickupFeedbackCache();
            ResolveReferences();
            ConfigureTriggerCollider();
        }

        private System.Collections.IEnumerator ReleaseAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            _releaseCoroutine = null;
            PrefabPoolService.Return(gameObject);
        }
    }
}
