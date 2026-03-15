using CuteIssac.Data.Visual;
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

        [Header("Visual Set")]
        [Tooltip("Optional visual asset set. Swap this in the inspector to replace pickup art without touching logic.")]
        [SerializeField] private PickupVisualSet visualSet;

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
        private bool _hasCapturedDefaultState;
        private Sprite _defaultBodySprite;
        private RuntimeAnimatorController _defaultAnimatorController;
        private Color _defaultBaseColor;
        private Color _defaultCollectedColor;
        private bool _hasRuntimeVisualOverride;
        private Sprite _runtimeBodySprite;
        private RuntimeAnimatorController _runtimeAnimatorController;
        private Color _runtimeBaseColor;
        private Color _runtimeCollectedColor;

        private void Awake()
        {
            ResolveReferences();
            ApplyConfiguredVisualSet();
            CaptureFloatingBasePosition();
            ResetPresentation();
        }

        private void OnEnable()
        {
            ResolveReferences();
            ClearRuntimeVisualOverride();
            ApplyConfiguredVisualSet();
            CaptureFloatingBasePosition();
            ResetPresentation();
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

            if (visualRoot != null)
            {
                visualRoot.gameObject.SetActive(false);
            }
        }

        public void ResetPresentation()
        {
            _collected = false;

            Transform floatTarget = floatingRoot != null ? floatingRoot : visualRoot;

            if (floatTarget != null)
            {
                if (visualRoot != null)
                {
                    visualRoot.gameObject.SetActive(true);
                }

                floatTarget.localPosition = _floatingLocalBasePosition;
            }

            ApplyBodyColor(baseColor);
        }

        public void ApplyVisualSet(PickupVisualSet nextVisualSet)
        {
            visualSet = nextVisualSet;
            ApplyConfiguredVisualSet();
            ResetPresentation();
        }

        public void ApplyRuntimeVisual(Sprite bodySprite, Color runtimeBaseColor, Color runtimeCollectedColor, RuntimeAnimatorController runtimeAnimatorController = null)
        {
            ResolveReferences();
            _hasRuntimeVisualOverride = true;
            _runtimeBodySprite = bodySprite;
            _runtimeAnimatorController = runtimeAnimatorController;
            _runtimeBaseColor = runtimeBaseColor;
            _runtimeCollectedColor = runtimeCollectedColor;
            ApplyConfiguredVisualSet();
            ResetPresentation();
        }

        public void ClearRuntimeVisualOverride()
        {
            _hasRuntimeVisualOverride = false;
            _runtimeBodySprite = null;
            _runtimeAnimatorController = null;

            if (_hasCapturedDefaultState)
            {
                if (bodySpriteRenderer != null)
                {
                    bodySpriteRenderer.sprite = _defaultBodySprite;
                }

                if (bodyAnimator != null)
                {
                    bodyAnimator.runtimeAnimatorController = _defaultAnimatorController;
                }

                baseColor = _defaultBaseColor;
                collectedColor = _defaultCollectedColor;
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

        private void ApplyConfiguredVisualSet()
        {
            if (_hasRuntimeVisualOverride)
            {
                if (bodySpriteRenderer != null && _runtimeBodySprite != null)
                {
                    bodySpriteRenderer.sprite = _runtimeBodySprite;
                }

                if (bodyAnimator != null)
                {
                    bodyAnimator.runtimeAnimatorController = _runtimeAnimatorController;
                }

                baseColor = _runtimeBaseColor;
                collectedColor = _runtimeCollectedColor;
                return;
            }

            if (visualSet != null)
            {
                if (bodySpriteRenderer != null && visualSet.BodySprite != null)
                {
                    bodySpriteRenderer.sprite = visualSet.BodySprite;
                }

                if (bodyAnimator != null && visualSet.AnimatorController != null)
                {
                    bodyAnimator.runtimeAnimatorController = visualSet.AnimatorController;
                }

                baseColor = visualSet.BaseColor;
                collectedColor = visualSet.CollectedColor;
            }

            CaptureDefaultState();
        }

        private void CaptureDefaultState()
        {
            if (_hasRuntimeVisualOverride)
            {
                return;
            }

            _defaultBodySprite = bodySpriteRenderer != null ? bodySpriteRenderer.sprite : null;
            _defaultAnimatorController = bodyAnimator != null ? bodyAnimator.runtimeAnimatorController : null;
            _defaultBaseColor = baseColor;
            _defaultCollectedColor = collectedColor;
            _hasCapturedDefaultState = true;
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

        private void CaptureFloatingBasePosition()
        {
            _floatingLocalBasePosition = (floatingRoot != null ? floatingRoot : visualRoot)?.localPosition ?? Vector3.zero;
        }

        private void Reset()
        {
            ResolveReferences();
        }

        private void OnValidate()
        {
            ResolveReferences();
            ApplyConfiguredVisualSet();
        }
    }
}
