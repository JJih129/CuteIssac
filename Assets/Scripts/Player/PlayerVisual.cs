using CuteIssac.Combat;
using UnityEngine;

namespace CuteIssac.Player
{
    /// <summary>
    /// Presentation layer for the player prefab.
    /// Designers can swap sprites, animator, and effect anchors here without changing gameplay scripts.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerVisual : MonoBehaviour
    {
        private enum FacingMode
        {
            None = 0,
            FlipX = 1,
            RotateVisualRoot = 2
        }

        [Header("Logic References")]
        [Tooltip("Optional. Used to listen for damage and death signals without putting VFX code into PlayerHealth.")]
        [SerializeField] private PlayerHealth playerHealth;
        [Tooltip("Optional. When assigned, the projectile spawner will use the muzzle anchor below as its spawn origin.")]
        [SerializeField] private ProjectileSpawner projectileSpawner;

        [Header("Presentation Roots")]
        [Tooltip("Optional root for all presentation-only children. Safe to leave empty when the root object already represents the visual hierarchy.")]
        [SerializeField] private Transform visualRoot;
        [Tooltip("Optional transform that rotates or flips to face the current look direction.")]
        [SerializeField] private Transform facingRoot;
        [Tooltip("Optional extra root for shadow sprites or accessory visuals.")]
        [SerializeField] private Transform optionalShadowRoot;

        [Header("Renderer References")]
        [Tooltip("Main body sprite. Designers usually replace this sprite or its material first.")]
        [SerializeField] private SpriteRenderer bodySpriteRenderer;
        [Tooltip("Optional animator used only for presentation. Gameplay does not depend on it.")]
        [SerializeField] private Animator bodyAnimator;

        [Header("Effect Anchors")]
        [Tooltip("Projectile, muzzle flash, and future fire VFX should use this anchor.")]
        [SerializeField] private Transform muzzleAnchor;
        [Tooltip("Hit spark or flash effects should spawn from this anchor.")]
        [SerializeField] private Transform hitEffectAnchor;
        [Tooltip("Death burst or body-removal effects should spawn from this anchor.")]
        [SerializeField] private Transform deathEffectAnchor;

        [Header("Facing")]
        [SerializeField] private FacingMode facingMode = FacingMode.FlipX;
        [SerializeField] private bool useMoveDirectionWhenAimMissing = true;
        [SerializeField] private bool moveMuzzleAnchorWithAim = true;

        [Header("Feedback")]
        [SerializeField] private Color baseColor = Color.white;
        [SerializeField] private Color damagedColor = new(1f, 0.45f, 0.45f, 1f);
        [SerializeField] [Min(0f)] private float damagedFlashDuration = 0.08f;
        [SerializeField] private Color deadColor = new(1f, 1f, 1f, 0.55f);

        [Header("Optional Animator Parameters")]
        [SerializeField] private string moveXParameter = "MoveX";
        [SerializeField] private string moveYParameter = "MoveY";
        [SerializeField] private string speedParameter = "Speed";
        [SerializeField] private string aimXParameter = "AimX";
        [SerializeField] private string aimYParameter = "AimY";
        [SerializeField] private string firedTriggerParameter = "Fired";
        [SerializeField] private string damagedTriggerParameter = "Damaged";
        [SerializeField] private string deadBoolParameter = "Dead";

        public Transform MuzzleAnchor => muzzleAnchor;
        public Transform HitEffectAnchor => hitEffectAnchor != null ? hitEffectAnchor : transform;
        public Transform DeathEffectAnchor => deathEffectAnchor != null ? deathEffectAnchor : transform;
        public Transform OptionalShadowRoot => optionalShadowRoot;
        public SpriteRenderer BodySpriteRenderer => bodySpriteRenderer;
        public Animator BodyAnimator => bodyAnimator;

        private Vector2 _lastAimDirection = Vector2.right;
        private float _damagedFlashRemaining;
        private bool _warnedMissingBodyRenderer;
        private Vector3 _initialMuzzleLocalPosition;
        private bool _hasInitialMuzzleLocalPosition;

        private void Awake()
        {
            ResolveReferences();
            ApplySpawnOrigin();
            ApplyBodyColor(baseColor);
            CacheMuzzleAnchorLocalPosition();
            UpdateMuzzleAnchor(_lastAimDirection);
        }

        private void OnEnable()
        {
            ResolveReferences();

            if (playerHealth != null)
            {
                playerHealth.Damaged += HandleDamaged;
                playerHealth.Died += HandleDied;
            }
        }

        private void OnDisable()
        {
            if (playerHealth != null)
            {
                playerHealth.Damaged -= HandleDamaged;
                playerHealth.Died -= HandleDied;
            }
        }

        private void Update()
        {
            if (_damagedFlashRemaining <= 0f)
            {
                return;
            }

            _damagedFlashRemaining -= Time.deltaTime;

            if (_damagedFlashRemaining <= 0f && playerHealth != null && !playerHealth.IsDead)
            {
                ApplyBodyColor(baseColor);
            }
        }

        public void SetMoveInput(Vector2 moveInput)
        {
            if (!HasAimDirection() && useMoveDirectionWhenAimMissing && moveInput.sqrMagnitude > 0.0001f)
            {
                ApplyFacing(moveInput.normalized);
            }

            UpdateAnimatorMove(moveInput);
        }

        public void SetAimDirection(Vector2 aimDirection)
        {
            if (aimDirection.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            _lastAimDirection = aimDirection.normalized;
            ApplyFacing(_lastAimDirection);
            UpdateMuzzleAnchor(_lastAimDirection);
            UpdateAnimatorAim(_lastAimDirection);
        }

        public void HandleFired(Vector2 fireDirection)
        {
            SetAimDirection(fireDirection);
            SetAnimatorTrigger(firedTriggerParameter);
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

        private void ApplySpawnOrigin()
        {
            if (projectileSpawner != null && muzzleAnchor != null)
            {
                projectileSpawner.SetSpawnOrigin(muzzleAnchor);
            }
        }

        private void ApplyFacing(Vector2 facingDirection)
        {
            Transform target = facingRoot != null ? facingRoot : visualRoot;

            if (target == null)
            {
                return;
            }

            switch (facingMode)
            {
                case FacingMode.FlipX:
                    Vector3 localScale = target.localScale;

                    if (Mathf.Abs(facingDirection.x) > 0.0001f)
                    {
                        localScale.x = Mathf.Abs(localScale.x) * Mathf.Sign(facingDirection.x);
                        target.localScale = localScale;
                    }

                    break;
                case FacingMode.RotateVisualRoot:
                    float angle = Mathf.Atan2(facingDirection.y, facingDirection.x) * Mathf.Rad2Deg;
                    target.localRotation = Quaternion.Euler(0f, 0f, angle);
                    break;
            }
        }

        private void UpdateAnimatorMove(Vector2 moveInput)
        {
            if (bodyAnimator == null)
            {
                return;
            }

            SetAnimatorFloat(moveXParameter, moveInput.x);
            SetAnimatorFloat(moveYParameter, moveInput.y);
            SetAnimatorFloat(speedParameter, moveInput.sqrMagnitude);
        }

        private void UpdateAnimatorAim(Vector2 aimDirection)
        {
            if (bodyAnimator == null)
            {
                return;
            }

            SetAnimatorFloat(aimXParameter, aimDirection.x);
            SetAnimatorFloat(aimYParameter, aimDirection.y);
        }

        private void ResolveReferences()
        {
            if (playerHealth == null)
            {
                TryGetComponent(out playerHealth);
            }

            if (projectileSpawner == null)
            {
                TryGetComponent(out projectileSpawner);
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
        }

        private void CacheMuzzleAnchorLocalPosition()
        {
            if (muzzleAnchor == null || _hasInitialMuzzleLocalPosition)
            {
                return;
            }

            _initialMuzzleLocalPosition = muzzleAnchor.localPosition;
            _hasInitialMuzzleLocalPosition = true;
        }

        private void UpdateMuzzleAnchor(Vector2 aimDirection)
        {
            if (!moveMuzzleAnchorWithAim || muzzleAnchor == null)
            {
                return;
            }

            CacheMuzzleAnchorLocalPosition();

            if (!_hasInitialMuzzleLocalPosition || aimDirection.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            float radius = Mathf.Max(0.01f, new Vector2(_initialMuzzleLocalPosition.x, _initialMuzzleLocalPosition.y).magnitude);
            Vector2 normalizedAim = aimDirection.normalized;
            muzzleAnchor.localPosition = new Vector3(normalizedAim.x * radius, normalizedAim.y * radius, _initialMuzzleLocalPosition.z);
        }

        private bool HasAimDirection()
        {
            return _lastAimDirection.sqrMagnitude > 0.0001f;
        }

        private void ApplyBodyColor(Color color)
        {
            if (bodySpriteRenderer == null)
            {
                if (!_warnedMissingBodyRenderer)
                {
                    Debug.LogWarning("PlayerVisual has no body SpriteRenderer assigned. Visual feedback will be skipped.", this);
                    _warnedMissingBodyRenderer = true;
                }

                return;
            }

            bodySpriteRenderer.color = color;
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
            CacheMuzzleAnchorLocalPosition();
        }

        private void OnValidate()
        {
            ResolveReferences();
            CacheMuzzleAnchorLocalPosition();
        }
    }
}
