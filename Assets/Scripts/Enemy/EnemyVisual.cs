using UnityEngine;

namespace CuteIssac.Enemy
{
    /// <summary>
    /// Presentation layer for enemy prefabs.
    /// Replace sprites, animator, and effect anchors here without changing enemy gameplay logic.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EnemyVisual : MonoBehaviour
    {
        private enum FacingMode
        {
            None = 0,
            FlipX = 1,
            RotateVisualRoot = 2
        }

        [Header("Logic References")]
        [Tooltip("Optional. Used to subscribe to damage and death events without embedding visuals into EnemyHealth.")]
        [SerializeField] private EnemyHealth enemyHealth;

        [Header("Presentation Roots")]
        [Tooltip("Optional root that contains all artist-facing presentation children.")]
        [SerializeField] private Transform visualRoot;
        [Tooltip("Optional transform that flips or rotates with movement direction.")]
        [SerializeField] private Transform facingRoot;
        [Tooltip("Optional extra root for highlight or accessory visuals.")]
        [SerializeField] private Transform optionalHighlightRoot;
        [Tooltip("Optional extra root for shadow visuals.")]
        [SerializeField] private Transform optionalShadowRoot;

        [Header("Renderer References")]
        [Tooltip("Primary body sprite to swap when enemy art changes.")]
        [SerializeField] private SpriteRenderer bodySpriteRenderer;
        [Tooltip("Optional animator. Enemy logic does not require it.")]
        [SerializeField] private Animator bodyAnimator;
        [Tooltip("Optional separate renderer used for hit flash if the body sprite should stay untouched.")]
        [SerializeField] private SpriteRenderer hitFlashTarget;

        [Header("Effect Anchors")]
        [Tooltip("Optional anchor for melee or ranged attack effects.")]
        [SerializeField] private Transform attackEffectAnchor;
        [Tooltip("Optional anchor for death burst or dissolve effects.")]
        [SerializeField] private Transform deathEffectAnchor;

        [Header("Facing")]
        [SerializeField] private FacingMode facingMode = FacingMode.FlipX;

        [Header("Feedback")]
        [SerializeField] private Color baseColor = Color.white;
        [SerializeField] private Color damagedColor = new(1f, 0.48f, 0.48f, 1f);
        [SerializeField] [Min(0f)] private float damagedFlashDuration = 0.08f;
        [SerializeField] private Color deadColor = new(1f, 1f, 1f, 0.55f);

        [Header("Optional Animator Parameters")]
        [SerializeField] private string moveXParameter = "MoveX";
        [SerializeField] private string moveYParameter = "MoveY";
        [SerializeField] private string speedParameter = "Speed";
        [SerializeField] private string damagedTriggerParameter = "Damaged";
        [SerializeField] private string deadBoolParameter = "Dead";

        public Transform AttackEffectAnchor => attackEffectAnchor != null ? attackEffectAnchor : transform;
        public Transform DeathEffectAnchor => deathEffectAnchor != null ? deathEffectAnchor : transform;
        public Transform OptionalShadowRoot => optionalShadowRoot;
        public Transform OptionalHighlightRoot => optionalHighlightRoot;
        public SpriteRenderer BodySpriteRenderer => bodySpriteRenderer;
        public Animator BodyAnimator => bodyAnimator;

        private float _damagedFlashRemaining;
        private bool _warnedMissingRenderer;

        private void Awake()
        {
            ResolveReferences();
            ApplyBodyColor(baseColor);
        }

        private void OnEnable()
        {
            ResolveReferences();

            if (enemyHealth != null)
            {
                enemyHealth.Damaged += HandleDamaged;
                enemyHealth.Died += HandleDied;
            }
        }

        private void OnDisable()
        {
            if (enemyHealth != null)
            {
                enemyHealth.Damaged -= HandleDamaged;
                enemyHealth.Died -= HandleDied;
            }
        }

        private void Update()
        {
            if (_damagedFlashRemaining <= 0f)
            {
                return;
            }

            _damagedFlashRemaining -= Time.deltaTime;

            if (_damagedFlashRemaining <= 0f && enemyHealth != null && !enemyHealth.IsDead)
            {
                ApplyBodyColor(baseColor);
            }
        }

        public void SetMoveDirection(Vector2 moveDirection)
        {
            ApplyFacing(moveDirection);
            UpdateAnimatorMove(moveDirection);
        }

        public void HandleAttack()
        {
            // Reserved for future attack VFX/animation triggers.
        }

        public void HandleDamaged()
        {
            _damagedFlashRemaining = damagedFlashDuration;
            ApplyBodyColor(damagedColor);
            SetAnimatorTrigger(damagedTriggerParameter);
        }

        public void HandleDied()
        {
            _damagedFlashRemaining = 0f;
            ApplyBodyColor(deadColor);
            SetAnimatorBool(deadBoolParameter, true);
        }

        private void ResolveReferences()
        {
            if (enemyHealth == null)
            {
                TryGetComponent(out enemyHealth);
            }

            if (visualRoot == null)
            {
                visualRoot = transform;
            }

            if (facingRoot == null)
            {
                facingRoot = visualRoot;
            }

            if (bodySpriteRenderer == null)
            {
                bodySpriteRenderer = GetComponentInChildren<SpriteRenderer>(true);
            }

            if (bodyAnimator == null)
            {
                bodyAnimator = GetComponentInChildren<Animator>(true);
            }

            if (hitFlashTarget == null)
            {
                hitFlashTarget = bodySpriteRenderer;
            }
        }

        private void ApplyFacing(Vector2 moveDirection)
        {
            if (moveDirection.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            Transform target = facingRoot != null ? facingRoot : visualRoot;

            if (target == null)
            {
                return;
            }

            switch (facingMode)
            {
                case FacingMode.FlipX:
                    if (Mathf.Abs(moveDirection.x) > 0.0001f)
                    {
                        Vector3 localScale = target.localScale;
                        localScale.x = Mathf.Abs(localScale.x) * Mathf.Sign(moveDirection.x);
                        target.localScale = localScale;
                    }

                    break;
                case FacingMode.RotateVisualRoot:
                    float angle = Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg;
                    target.localRotation = Quaternion.Euler(0f, 0f, angle);
                    break;
            }
        }

        private void UpdateAnimatorMove(Vector2 moveDirection)
        {
            if (bodyAnimator == null)
            {
                return;
            }

            SetAnimatorFloat(moveXParameter, moveDirection.x);
            SetAnimatorFloat(moveYParameter, moveDirection.y);
            SetAnimatorFloat(speedParameter, moveDirection.sqrMagnitude);
        }

        private void ApplyBodyColor(Color color)
        {
            SpriteRenderer targetRenderer = hitFlashTarget != null ? hitFlashTarget : bodySpriteRenderer;

            if (targetRenderer == null)
            {
                if (!_warnedMissingRenderer)
                {
                    Debug.LogWarning("EnemyVisual has no SpriteRenderer assigned. Visual feedback will be skipped.", this);
                    _warnedMissingRenderer = true;
                }

                return;
            }

            targetRenderer.color = color;
        }

        private void SetAnimatorFloat(string parameterName, float value)
        {
            if (bodyAnimator == null || string.IsNullOrWhiteSpace(parameterName))
            {
                return;
            }

            bodyAnimator.SetFloat(parameterName, value);
        }

        private void SetAnimatorBool(string parameterName, bool value)
        {
            if (bodyAnimator == null || string.IsNullOrWhiteSpace(parameterName))
            {
                return;
            }

            bodyAnimator.SetBool(parameterName, value);
        }

        private void SetAnimatorTrigger(string parameterName)
        {
            if (bodyAnimator == null || string.IsNullOrWhiteSpace(parameterName))
            {
                return;
            }

            bodyAnimator.SetTrigger(parameterName);
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
