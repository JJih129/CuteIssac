using CuteIssac.Combat;
using CuteIssac.Common.Combat;
using CuteIssac.Data.Visual;
using UnityEngine;
using UnityEngine.Serialization;

namespace CuteIssac.Player
{
    /// <summary>
    /// Presentation layer for the player prefab.
    /// Designers can swap sprites, animator, and effect anchors here without changing gameplay scripts.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerVisual : MonoBehaviour
    {
        private const string HitFlashShaderName = "CuteIssac/SpriteWhiteFlash";

        private enum FacingMode
        {
            None = 0,
            FlipX = 1,
            RotateVisualRoot = 2
        }

        [Header("Logic References")]
        [Tooltip("Optional. Used to listen for damage and death signals without putting VFX code into PlayerHealth.")]
        [SerializeField] private PlayerHealth playerHealth;
        [Tooltip("Optional. Used for hit knockback without moving combat logic into the visual component.")]
        [SerializeField] private PlayerMovement playerMovement;
        [Tooltip("Optional. When assigned, the projectile spawner will use the muzzle anchor below as its spawn origin.")]
        [SerializeField] private ProjectileSpawner projectileSpawner;
        [Tooltip("Optional. Handles camera or screen-space hit feedback when the player takes damage.")]
        [SerializeField] private PlayerScreenFeedback screenFeedback;

        [Header("Presentation Roots")]
        [Tooltip("Optional root for all presentation-only children. Safe to leave empty when the root object already represents the visual hierarchy.")]
        [SerializeField] private Transform visualRoot;
        [Tooltip("Optional root that receives hit punch offsets. When empty, the visual root is used if safe.")]
        [SerializeField] private Transform hitFeedbackRoot;
        [Tooltip("Optional transform that rotates or flips to face the current look direction.")]
        [SerializeField] private Transform facingRoot;
        [Tooltip("Optional extra root for shadow sprites or accessory visuals.")]
        [SerializeField] private Transform optionalShadowRoot;

        [Header("Renderer References")]
        [Tooltip("Main body sprite. Designers usually replace this sprite or its material first.")]
        [SerializeField] private SpriteRenderer bodySpriteRenderer;
        [Tooltip("Optional animator used only for presentation. Gameplay does not depend on it.")]
        [SerializeField] private Animator bodyAnimator;

        [Header("Visual Set")]
        [Tooltip("Optional visual asset set. Swap this in the inspector to replace prototype player art without touching logic.")]
        [SerializeField] private PlayerVisualSet visualSet;

        [Header("Fallback Sprite Animation")]
        [Tooltip("Used when no animator controller is assigned. The body sprite swaps through these frames while moving.")]
        [SerializeField] private bool useSpriteSequenceAnimation = true;
        [SerializeField] private Sprite idleBodySprite;
        [SerializeField] private Sprite[] walkLeftBodySprites;
        [Tooltip("Optional sprite shown briefly when the player takes damage.")]
        [SerializeField] private Sprite hitBodySprite;
        [SerializeField, Min(0f)] private float hitSpriteDuration = 0.12f;
        [SerializeField] [Min(1f)] private float walkAnimationFramesPerSecond = 10f;
        [SerializeField] [Min(0f)] private float walkAnimationMoveThreshold = 0.08f;
        [SerializeField] private bool animateVerticalMovementWithWalkCycle = true;

        [Header("Effect Anchors")]
        [Tooltip("Projectile, muzzle flash, and future fire VFX should use this anchor.")]
        [SerializeField] private Transform muzzleAnchor;
        [Tooltip("Hit spark or flash effects should spawn from this anchor.")]
        [SerializeField] private Transform hitEffectAnchor;
        [Tooltip("Death burst or body-removal effects should spawn from this anchor.")]
        [SerializeField] private Transform deathEffectAnchor;

        [Header("Facing")]
        [SerializeField] private FacingMode facingMode = FacingMode.FlipX;
        [SerializeField] private bool bodySpriteFacesRightByDefault = true;
        [SerializeField] private bool useMoveDirectionWhenAimMissing = true;
        [SerializeField] private bool moveMuzzleAnchorWithAim = true;

        [Header("Feedback")]
        [SerializeField] private Color baseColor = Color.white;
        [SerializeField] private Color hitFlashColor = new(1f, 1f, 1f, 1f);
        [SerializeField] private Color damagedColor = new(1f, 0.45f, 0.45f, 1f);
        [SerializeField, FormerlySerializedAs("damagedFlashDuration"), Min(0f)] private float hitFlashBlinkInterval = 0.1f;
        [SerializeField] [Min(0f)] private float hitKnockbackImpulse = 4.75f;
        [SerializeField] [Min(0f)] private float hitVisualPunchDistance = 0.14f;
        [SerializeField] [Min(0f)] private float hitVisualRecoverSpeed = 1.4f;
        [SerializeField] [Min(0f)] private float screenFeedbackScale = 1f;
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
        private float _hitFlashRemaining;
        private float _hitFlashTotalDuration;
        private bool _warnedMissingBodyRenderer;
        private Vector3 _initialMuzzleLocalPosition;
        private bool _hasInitialMuzzleLocalPosition;
        private Vector3 _initialHitFeedbackLocalPosition;
        private bool _hasInitialHitFeedbackLocalPosition;
        private Vector3 _currentHitVisualOffset;
        private Vector2 _lastMoveInput;
        private float _walkAnimationTime;
        private bool _wasWalkAnimationActive;
        private float _hitSpriteRemaining;
        private SpriteRenderer _hitPoseOverlayRenderer;
        private SpriteRenderer _hitFlashOverlayRenderer;

        private static Shader s_hitFlashShader;
        private static Material s_hitFlashMaterial;

        private void Awake()
        {
            ResolveReferences();
            if (GetComponent<PlayerHeldWeaponVisual>() == null)
            {
                gameObject.AddComponent<PlayerHeldWeaponVisual>();
            }
            ApplyConfiguredVisualSet();
            ApplySpawnOrigin();
            ApplyBodyColor(baseColor);
            CacheMuzzleAnchorLocalPosition();
            CacheHitFeedbackLocalPosition();
            UpdateMuzzleAnchor(_lastAimDirection);
            RefreshSpriteSequenceFrame(resetAnimationTime: true);
        }

        private void OnEnable()
        {
            ResolveReferences();
            ApplyConfiguredVisualSet();
            ApplyBodyColor(baseColor);
            _hitSpriteRemaining = 0f;
            _hitFlashRemaining = 0f;
            ClearHitFlashOverlay();

            if (playerHealth != null)
            {
                playerHealth.InvulnerabilityGranted += HandleInvulnerabilityGranted;
                playerHealth.DamagedWithInfo += HandleDamaged;
                playerHealth.Died += HandleDied;
            }

            RefreshSpriteSequenceFrame(resetAnimationTime: true);
        }

        private void OnDisable()
        {
            if (playerHealth != null)
            {
                playerHealth.InvulnerabilityGranted -= HandleInvulnerabilityGranted;
                playerHealth.DamagedWithInfo -= HandleDamaged;
                playerHealth.Died -= HandleDied;
            }

            _hitSpriteRemaining = 0f;
            _hitFlashRemaining = 0f;
            ClearHitPoseOverlay();
            ClearHitFlashOverlay();
            ResetHitFeedbackRootPosition();
        }

        private void Update()
        {
            UpdateSpriteSequenceAnimation();
            UpdateHitVisualOffset();
            UpdateHitSpriteAnimation();
            UpdateHitFlashBlink();
        }

        public void SetMoveInput(Vector2 moveInput)
        {
            _lastMoveInput = moveInput;

            bool usedHorizontalMoveFacing = ShouldUseSpriteSequenceAnimation()
                && Mathf.Abs(moveInput.x) > walkAnimationMoveThreshold;

            if (usedHorizontalMoveFacing)
            {
                ApplyFacing(new Vector2(moveInput.x, 0f));
            }
            else if (!HasAimDirection() && useMoveDirectionWhenAimMissing && moveInput.sqrMagnitude > 0.0001f)
            {
                ApplyFacing(moveInput.normalized);
            }

            UpdateAnimatorMove(moveInput);
            RefreshSpriteSequenceFrame(resetAnimationTime: false);
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

        public void HandleDamaged(DamageInfo damageInfo)
        {
            bool canUseHitSprite = hitBodySprite != null && !HasAnimatorDrivenSpriteAnimation();
            _hitSpriteRemaining = canUseHitSprite
                ? Mathf.Max(_hitSpriteRemaining, hitSpriteDuration)
                : 0f;
            if (canUseHitSprite)
            {
                EnsureHitPoseOverlay();
                SyncHitPoseOverlaySprite(hitBodySprite);
                SetHitPoseOverlayVisible(true);
                ApplyAnimatedBodySprite(hitBodySprite);
            }

            ApplyBodyColor(baseColor);
            SetAnimatorTrigger(damagedTriggerParameter);

            Vector2 hitDirection = ResolveHitDirection(damageInfo.HitDirection);

            if (playerMovement != null && hitKnockbackImpulse > 0f)
            {
                playerMovement.ApplyImpulse(hitDirection * hitKnockbackImpulse);
            }

            ApplyHitVisualPunch(hitDirection);

            if (screenFeedback != null && screenFeedbackScale > 0f)
            {
                screenFeedback.PlayHitFeedback(screenFeedbackScale);
            }
        }

        public void HandleDied()
        {
            _hitFlashRemaining = 0f;
            _hitSpriteRemaining = 0f;
            ClearHitPoseOverlay();
            ClearHitFlashOverlay();
            ApplyBodyColor(deadColor);
            SetAnimatorBool(deadBoolParameter, true);
            _currentHitVisualOffset = Vector3.zero;
            ResetHitFeedbackRootPosition();
        }

        private void HandleInvulnerabilityGranted(float duration)
        {
            if (playerHealth == null || playerHealth.IsDead)
            {
                return;
            }

            _hitFlashTotalDuration = Mathf.Max(0f, duration);
            _hitFlashRemaining = _hitFlashTotalDuration;
            _hitSpriteRemaining = Mathf.Max(_hitSpriteRemaining, _hitFlashTotalDuration);

            if (_hitFlashRemaining <= 0f)
            {
                ClearHitFlashOverlay();
                return;
            }

            ApplyBodyColor(baseColor);
            SyncHitFlashOverlaySprite();
            SetHitFlashOverlayVisible(true);
        }

        private void UpdateHitFlashBlink()
        {
            if (_hitFlashRemaining <= 0f)
            {
                ClearHitFlashOverlay();
                return;
            }

            if (playerHealth == null || playerHealth.IsDead || !playerHealth.IsInvulnerable)
            {
                _hitFlashRemaining = 0f;
                ClearHitFlashOverlay();
                return;
            }

            _hitFlashRemaining = Mathf.Max(0f, _hitFlashRemaining - Time.deltaTime);

            if (_hitFlashRemaining <= 0f)
            {
                ClearHitFlashOverlay();
                return;
            }

            SyncHitFlashOverlaySprite();

            bool visible = hitFlashBlinkInterval <= 0f
                || ((int)((_hitFlashTotalDuration - _hitFlashRemaining) / hitFlashBlinkInterval) & 1) == 0;
            SetHitFlashOverlayVisible(visible);
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
                        float horizontalSign = Mathf.Sign(facingDirection.x);
                        if (!bodySpriteFacesRightByDefault)
                        {
                            horizontalSign *= -1f;
                        }

                        localScale.x = Mathf.Abs(localScale.x) * horizontalSign;
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

            if (playerMovement == null)
            {
                TryGetComponent(out playerMovement);
            }

            if (projectileSpawner == null)
            {
                TryGetComponent(out projectileSpawner);
            }

            if (screenFeedback == null)
            {
                TryGetComponent(out screenFeedback);
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

            if (hitFeedbackRoot == null)
            {
                hitFeedbackRoot = visualRoot != transform
                    ? visualRoot
                    : facingRoot != null
                        ? facingRoot
                        : bodySpriteRenderer != null
                            ? bodySpriteRenderer.transform
                            : null;
            }
        }

        public void ApplyVisualSet(PlayerVisualSet nextVisualSet)
        {
            visualSet = nextVisualSet;
            ApplyConfiguredVisualSet();
        }

        private void ApplyConfiguredVisualSet()
        {
            if (visualSet == null)
            {
                RefreshSpriteSequenceFrame(resetAnimationTime: true);
                return;
            }

            if (bodySpriteRenderer != null && visualSet.BodySprite != null)
            {
                bodySpriteRenderer.sprite = visualSet.BodySprite;
            }

            if (bodyAnimator != null && visualSet.AnimatorController != null)
            {
                bodyAnimator.runtimeAnimatorController = visualSet.AnimatorController;
            }

            if (optionalShadowRoot != null &&
                optionalShadowRoot.TryGetComponent(out SpriteRenderer shadowRenderer))
            {
                if (visualSet.ShadowSprite != null)
                {
                    shadowRenderer.sprite = visualSet.ShadowSprite;
                }

                shadowRenderer.color = visualSet.ShadowColor;
            }

            baseColor = visualSet.BaseColor;
            if (visualSet.HitSprite != null)
            {
                hitBodySprite = visualSet.HitSprite;
            }

            hitFlashColor = visualSet.HitFlashColor;
            damagedColor = visualSet.DamagedColor;
            deadColor = visualSet.DeadColor;
            RefreshSpriteSequenceFrame(resetAnimationTime: true);
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

        private void CacheHitFeedbackLocalPosition()
        {
            if (hitFeedbackRoot == null || _hasInitialHitFeedbackLocalPosition)
            {
                return;
            }

            _initialHitFeedbackLocalPosition = hitFeedbackRoot.localPosition;
            _hasInitialHitFeedbackLocalPosition = true;
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

        private Vector2 ResolveHitDirection(Vector2 hitDirection)
        {
            if (hitDirection.sqrMagnitude > 0.0001f)
            {
                return hitDirection.normalized;
            }

            if (_lastAimDirection.sqrMagnitude > 0.0001f)
            {
                return -_lastAimDirection.normalized;
            }

            return Vector2.down;
        }

        private void ApplyHitVisualPunch(Vector2 hitDirection)
        {
            CacheHitFeedbackLocalPosition();

            if (!_hasInitialHitFeedbackLocalPosition || hitFeedbackRoot == null || hitVisualPunchDistance <= 0f)
            {
                return;
            }

            _currentHitVisualOffset = (Vector3)(hitDirection.normalized * hitVisualPunchDistance);
            hitFeedbackRoot.localPosition = _initialHitFeedbackLocalPosition + _currentHitVisualOffset;
        }

        private void UpdateHitVisualOffset()
        {
            if (!_hasInitialHitFeedbackLocalPosition || hitFeedbackRoot == null || _currentHitVisualOffset.sqrMagnitude <= 0.000001f)
            {
                return;
            }

            _currentHitVisualOffset = Vector3.MoveTowards(
                _currentHitVisualOffset,
                Vector3.zero,
                hitVisualRecoverSpeed * Time.deltaTime);

            hitFeedbackRoot.localPosition = _initialHitFeedbackLocalPosition + _currentHitVisualOffset;

            if (_currentHitVisualOffset.sqrMagnitude <= 0.000001f)
            {
                _currentHitVisualOffset = Vector3.zero;
                ResetHitFeedbackRootPosition();
            }
        }

        private void ResetHitFeedbackRootPosition()
        {
            if (_hasInitialHitFeedbackLocalPosition && hitFeedbackRoot != null)
            {
                hitFeedbackRoot.localPosition = _initialHitFeedbackLocalPosition;
            }
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

        private void EnsureHitFlashOverlay()
        {
            if (_hitFlashOverlayRenderer != null || bodySpriteRenderer == null)
            {
                return;
            }

            Transform bodyTransform = bodySpriteRenderer.transform;
            GameObject overlayObject = new GameObject("HitFlashOverlay");
            overlayObject.transform.SetParent(bodyTransform.parent != null ? bodyTransform.parent : bodyTransform, false);
            overlayObject.transform.localPosition = bodyTransform.parent != null ? bodyTransform.localPosition : Vector3.zero;
            overlayObject.transform.localRotation = bodyTransform.parent != null ? bodyTransform.localRotation : Quaternion.identity;
            overlayObject.transform.localScale = bodyTransform.parent != null ? bodyTransform.localScale : Vector3.one;

            _hitFlashOverlayRenderer = overlayObject.AddComponent<SpriteRenderer>();
            Material hitFlashMaterial = GetHitFlashMaterial();
            if (hitFlashMaterial != null)
            {
                _hitFlashOverlayRenderer.sharedMaterial = hitFlashMaterial;
            }

            _hitFlashOverlayRenderer.sortingLayerID = bodySpriteRenderer.sortingLayerID;
            _hitFlashOverlayRenderer.sortingOrder = bodySpriteRenderer.sortingOrder + 2;
            _hitFlashOverlayRenderer.enabled = false;
            _hitFlashOverlayRenderer.color = Color.white;
            SyncHitFlashOverlaySprite();
        }

        private void EnsureHitPoseOverlay()
        {
            if (_hitPoseOverlayRenderer != null || bodySpriteRenderer == null || hitBodySprite == null)
            {
                return;
            }

            Transform bodyTransform = bodySpriteRenderer.transform;
            GameObject overlayObject = new GameObject("HitPoseOverlay");
            overlayObject.transform.SetParent(bodyTransform.parent != null ? bodyTransform.parent : bodyTransform, false);
            overlayObject.transform.localPosition = bodyTransform.parent != null ? bodyTransform.localPosition : Vector3.zero;
            overlayObject.transform.localRotation = bodyTransform.parent != null ? bodyTransform.localRotation : Quaternion.identity;
            overlayObject.transform.localScale = bodyTransform.parent != null ? bodyTransform.localScale : Vector3.one;

            _hitPoseOverlayRenderer = overlayObject.AddComponent<SpriteRenderer>();
            _hitPoseOverlayRenderer.sortingLayerID = bodySpriteRenderer.sortingLayerID;
            _hitPoseOverlayRenderer.sortingOrder = bodySpriteRenderer.sortingOrder + 1;
            _hitPoseOverlayRenderer.enabled = false;
            _hitPoseOverlayRenderer.color = Color.white;
            SyncHitPoseOverlaySprite(hitBodySprite);
        }

        private static Material GetHitFlashMaterial()
        {
            if (s_hitFlashMaterial != null)
            {
                return s_hitFlashMaterial;
            }

            if (s_hitFlashShader == null)
            {
                s_hitFlashShader = Shader.Find(HitFlashShaderName);
            }

            if (s_hitFlashShader == null)
            {
                return null;
            }

            s_hitFlashMaterial = new Material(s_hitFlashShader)
            {
                name = "CuteIssac Sprite White Flash (Runtime)",
                hideFlags = HideFlags.HideAndDontSave
            };

            return s_hitFlashMaterial;
        }

        private void SyncHitFlashOverlaySprite()
        {
            if (_hitFlashOverlayRenderer == null || bodySpriteRenderer == null)
            {
                return;
            }

            Transform bodyTransform = bodySpriteRenderer.transform;
            Transform overlayTransform = _hitFlashOverlayRenderer.transform;
            if (bodyTransform.parent != null)
            {
                overlayTransform.localPosition = bodyTransform.localPosition;
                overlayTransform.localRotation = bodyTransform.localRotation;
                overlayTransform.localScale = bodyTransform.localScale;
            }
            else
            {
                overlayTransform.localPosition = Vector3.zero;
                overlayTransform.localRotation = Quaternion.identity;
                overlayTransform.localScale = Vector3.one;
            }

            Sprite sprite = GetHitFlashSourceSprite();
            if (sprite == null)
            {
                _hitFlashOverlayRenderer.sprite = null;
                _hitFlashOverlayRenderer.enabled = false;
                return;
            }

            if (_hitFlashOverlayRenderer.sprite != sprite)
            {
                _hitFlashOverlayRenderer.sprite = sprite;
            }

            _hitFlashOverlayRenderer.sortingLayerID = bodySpriteRenderer.sortingLayerID;
            _hitFlashOverlayRenderer.sortingOrder = bodySpriteRenderer.sortingOrder + 2;
            _hitFlashOverlayRenderer.flipX = bodySpriteRenderer.flipX;
            _hitFlashOverlayRenderer.flipY = bodySpriteRenderer.flipY;
            _hitFlashOverlayRenderer.maskInteraction = bodySpriteRenderer.maskInteraction;
            _hitFlashOverlayRenderer.spriteSortPoint = bodySpriteRenderer.spriteSortPoint;
            _hitFlashOverlayRenderer.color = Color.white;
        }

        private Sprite GetHitFlashSourceSprite()
        {
            if (_hitPoseOverlayRenderer != null && _hitPoseOverlayRenderer.enabled && _hitPoseOverlayRenderer.sprite != null)
            {
                return _hitPoseOverlayRenderer.sprite;
            }

            if (_hitSpriteRemaining > 0f && hitBodySprite != null)
            {
                return hitBodySprite;
            }

            return bodySpriteRenderer != null ? bodySpriteRenderer.sprite : null;
        }

        private void SyncHitPoseOverlaySprite(Sprite sprite)
        {
            if (_hitPoseOverlayRenderer == null || bodySpriteRenderer == null)
            {
                return;
            }

            Transform bodyTransform = bodySpriteRenderer.transform;
            Transform overlayTransform = _hitPoseOverlayRenderer.transform;
            if (bodyTransform.parent != null)
            {
                overlayTransform.localPosition = bodyTransform.localPosition;
                overlayTransform.localRotation = bodyTransform.localRotation;
                overlayTransform.localScale = bodyTransform.localScale;
            }
            else
            {
                overlayTransform.localPosition = Vector3.zero;
                overlayTransform.localRotation = Quaternion.identity;
                overlayTransform.localScale = Vector3.one;
            }

            if (_hitPoseOverlayRenderer.sprite != sprite)
            {
                _hitPoseOverlayRenderer.sprite = sprite;
            }

            _hitPoseOverlayRenderer.sortingLayerID = bodySpriteRenderer.sortingLayerID;
            _hitPoseOverlayRenderer.sortingOrder = bodySpriteRenderer.sortingOrder + 1;
            _hitPoseOverlayRenderer.flipX = bodySpriteRenderer.flipX;
            _hitPoseOverlayRenderer.flipY = bodySpriteRenderer.flipY;
            _hitPoseOverlayRenderer.maskInteraction = bodySpriteRenderer.maskInteraction;
            _hitPoseOverlayRenderer.spriteSortPoint = bodySpriteRenderer.spriteSortPoint;
            _hitPoseOverlayRenderer.color = Color.white;
        }

        private void SetHitPoseOverlayVisible(bool visible)
        {
            if (visible)
            {
                EnsureHitPoseOverlay();
            }

            if (_hitPoseOverlayRenderer != null)
            {
                _hitPoseOverlayRenderer.enabled = visible && _hitPoseOverlayRenderer.sprite != null;
            }
        }

        private void ClearHitPoseOverlay()
        {
            if (_hitPoseOverlayRenderer != null)
            {
                _hitPoseOverlayRenderer.enabled = false;
            }
        }

        private void SetHitFlashOverlayVisible(bool visible)
        {
            if (visible)
            {
                EnsureHitFlashOverlay();
                SyncHitFlashOverlaySprite();
            }

            if (_hitFlashOverlayRenderer != null)
            {
                _hitFlashOverlayRenderer.enabled = visible && _hitFlashOverlayRenderer.sprite != null;
            }
        }

        private void ClearHitFlashOverlay()
        {
            if (_hitFlashOverlayRenderer != null)
            {
                _hitFlashOverlayRenderer.enabled = false;
            }
        }

        private void UpdateSpriteSequenceAnimation()
        {
            if (IsHitSpriteActive())
            {
                return;
            }

            if (!ShouldUseSpriteSequenceAnimation())
            {
                return;
            }

            if (!ShouldPlayWalkAnimation())
            {
                if (_wasWalkAnimationActive)
                {
                    RefreshSpriteSequenceFrame(resetAnimationTime: true);
                }

                return;
            }

            _wasWalkAnimationActive = true;
            _walkAnimationTime += Time.deltaTime * Mathf.Max(1f, walkAnimationFramesPerSecond);
            ApplyAnimatedBodySprite(ResolveCurrentWalkSprite());
        }

        private void RefreshSpriteSequenceFrame(bool resetAnimationTime)
        {
            if (IsHitSpriteActive())
            {
                return;
            }

            if (HasAnimatorDrivenSpriteAnimation())
            {
                return;
            }

            if (!ShouldUseSpriteSequenceAnimation())
            {
                ApplyAnimatedBodySprite(ResolveRestoredBodySprite());
                return;
            }

            if (resetAnimationTime)
            {
                _walkAnimationTime = 0f;
            }

            if (ShouldPlayWalkAnimation())
            {
                _wasWalkAnimationActive = true;
                ApplyAnimatedBodySprite(ResolveCurrentWalkSprite());
                return;
            }

            _wasWalkAnimationActive = false;
            ApplyAnimatedBodySprite(ResolveIdleSprite());
        }

        private bool ShouldUseSpriteSequenceAnimation()
        {
            bool hasAnimatorController = HasAnimatorDrivenSpriteAnimation();
            return useSpriteSequenceAnimation
                && bodySpriteRenderer != null
                && !hasAnimatorController
                && (idleBodySprite != null || (walkLeftBodySprites != null && walkLeftBodySprites.Length > 0));
        }

        private bool HasAnimatorDrivenSpriteAnimation()
        {
            return bodyAnimator != null && bodyAnimator.runtimeAnimatorController != null;
        }

        private bool ShouldPlayWalkAnimation()
        {
            if (walkLeftBodySprites == null || walkLeftBodySprites.Length == 0)
            {
                return false;
            }

            float threshold = Mathf.Max(0f, walkAnimationMoveThreshold);
            if (_lastMoveInput.sqrMagnitude <= threshold * threshold)
            {
                return false;
            }

            return animateVerticalMovementWithWalkCycle || Mathf.Abs(_lastMoveInput.x) > threshold;
        }

        private Sprite ResolveIdleSprite()
        {
            if (idleBodySprite != null)
            {
                return idleBodySprite;
            }

            if (walkLeftBodySprites != null)
            {
                for (int index = 0; index < walkLeftBodySprites.Length; index++)
                {
                    if (walkLeftBodySprites[index] != null)
                    {
                        return walkLeftBodySprites[index];
                    }
                }
            }

            return null;
        }

        private Sprite ResolveRestoredBodySprite()
        {
            if (visualSet != null && visualSet.BodySprite != null)
            {
                return visualSet.BodySprite;
            }

            return ResolveIdleSprite();
        }

        private Sprite ResolveCurrentWalkSprite()
        {
            if (walkLeftBodySprites == null || walkLeftBodySprites.Length == 0)
            {
                return ResolveIdleSprite();
            }

            int frameIndex = Mathf.Abs(Mathf.FloorToInt(_walkAnimationTime));
            frameIndex %= walkLeftBodySprites.Length;

            Sprite resolvedSprite = walkLeftBodySprites[frameIndex];
            return resolvedSprite != null ? resolvedSprite : ResolveIdleSprite();
        }

        private void ApplyAnimatedBodySprite(Sprite sprite)
        {
            if (bodySpriteRenderer == null || sprite == null || bodySpriteRenderer.sprite == sprite)
            {
                return;
            }

            bodySpriteRenderer.sprite = sprite;
            if (_hitFlashRemaining > 0f)
            {
                SyncHitFlashOverlaySprite();
            }
        }

        private void UpdateHitSpriteAnimation()
        {
            if (_hitSpriteRemaining <= 0f)
            {
                ClearHitPoseOverlay();
                return;
            }

            _hitSpriteRemaining = Mathf.Max(0f, _hitSpriteRemaining - Time.deltaTime);

            if (hitBodySprite != null)
            {
                EnsureHitPoseOverlay();
                SyncHitPoseOverlaySprite(hitBodySprite);
                SetHitPoseOverlayVisible(true);
            }

            if (_hitSpriteRemaining <= 0f && (playerHealth == null || !playerHealth.IsDead))
            {
                ClearHitPoseOverlay();
                RefreshSpriteSequenceFrame(resetAnimationTime: false);
            }
        }

        private bool IsHitSpriteActive()
        {
            return _hitSpriteRemaining > 0f && hitBodySprite != null;
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
            CacheHitFeedbackLocalPosition();
        }

        private void OnValidate()
        {
            ResolveReferences();
            ApplyConfiguredVisualSet();
            CacheMuzzleAnchorLocalPosition();
            CacheHitFeedbackLocalPosition();
            RefreshSpriteSequenceFrame(resetAnimationTime: true);
        }
    }
}
