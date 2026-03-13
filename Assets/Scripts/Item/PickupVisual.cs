using UnityEngine;

namespace CuteIssac.Item
{
    /// <summary>
    /// Presentation layer for world pickups.
    /// Designers can swap sprites, animator, floating roots, and effect anchors here without changing pickup logic.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PickupVisual : MonoBehaviour
    {
        [Header("Presentation Roots")]
        [Tooltip("Optional root for the full visible hierarchy.")]
        [SerializeField] private Transform visualRoot;
        [Tooltip("Optional root that receives the bobbing animation.")]
        [SerializeField] private Transform floatingRoot;
        [Tooltip("Optional root for shadow visuals.")]
        [SerializeField] private Transform optionalShadowRoot;
        [Tooltip("Optional root for highlight visuals.")]
        [SerializeField] private Transform optionalHighlightRoot;
        [Tooltip("Optional extra root reserved for animator-driven child graphics.")]
        [SerializeField] private Transform optionalAnimatorRoot;

        [Header("Renderer References")]
        [Tooltip("Primary pickup sprite. Swap this when changing pickup art.")]
        [SerializeField] private SpriteRenderer bodySpriteRenderer;
        [Tooltip("Optional animator for idle, hover, or collect animations.")]
        [SerializeField] private Animator bodyAnimator;

        [Header("Effect Anchors")]
        [Tooltip("Optional anchor for a collect burst or sparkle effect.")]
        [SerializeField] private Transform pickupEffectAnchor;

        [Header("Floating Motion")]
        [SerializeField] private bool enableFloatMotion = true;
        [SerializeField] [Min(0f)] private float floatAmplitude = 0.08f;
        [SerializeField] [Min(0f)] private float floatFrequency = 2f;

        [Header("Feedback")]
        [SerializeField] private Color baseColor = Color.white;
        [SerializeField] private Color collectedColor = new(1f, 1f, 1f, 0.35f);
        [SerializeField] private string collectedTriggerParameter = "Collected";

        public Transform PickupEffectAnchor => pickupEffectAnchor != null ? pickupEffectAnchor : transform;
        public Transform OptionalShadowRoot => optionalShadowRoot;
        public Transform OptionalHighlightRoot => optionalHighlightRoot;
        public Transform OptionalAnimatorRoot => optionalAnimatorRoot;
        public SpriteRenderer BodySpriteRenderer => bodySpriteRenderer;
        public Animator BodyAnimator => bodyAnimator;

        private Vector3 _floatingLocalBasePosition;
        private bool _warnedMissingRenderer;
        private bool _collected;

        private void Awake()
        {
            ResolveReferences();
            _floatingLocalBasePosition = (floatingRoot != null ? floatingRoot : visualRoot)?.localPosition ?? Vector3.zero;
            ApplyBodyColor(baseColor);
        }

        private void Update()
        {
            if (_collected || !enableFloatMotion)
            {
                return;
            }

            Transform floatTarget = floatingRoot != null ? floatingRoot : visualRoot;

            if (floatTarget == null)
            {
                return;
            }

            float bobOffset = Mathf.Sin(Time.time * floatFrequency) * floatAmplitude;
            floatTarget.localPosition = _floatingLocalBasePosition + new Vector3(0f, bobOffset, 0f);
        }

        public void HandleCollected()
        {
            if (_collected)
            {
                return;
            }

            _collected = true;
            ApplyBodyColor(collectedColor);

            if (bodyAnimator != null && !string.IsNullOrWhiteSpace(collectedTriggerParameter))
            {
                bodyAnimator.SetTrigger(collectedTriggerParameter);
            }
        }

        private void ResolveReferences()
        {
            if (visualRoot == null)
            {
                visualRoot = transform;
            }

            if (floatingRoot == null)
            {
                floatingRoot = visualRoot;
            }

            if (bodySpriteRenderer == null)
            {
                bodySpriteRenderer = GetComponentInChildren<SpriteRenderer>(true);
            }

            if (bodyAnimator == null)
            {
                bodyAnimator = GetComponentInChildren<Animator>(true);
            }
        }

        private void ApplyBodyColor(Color color)
        {
            if (bodySpriteRenderer == null)
            {
                if (!_warnedMissingRenderer)
                {
                    Debug.LogWarning("PickupVisual has no SpriteRenderer assigned. Visual feedback will be skipped.", this);
                    _warnedMissingRenderer = true;
                }

                return;
            }

            bodySpriteRenderer.color = color;
        }

        private void Reset()
        {
            ResolveReferences();
        }

        private void OnValidate()
        {
            ResolveReferences();
        }
    }
}
