using UnityEngine;

namespace CuteIssac.Enemy
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(EnemyController))]
    [DefaultExecutionOrder(110)]
    public sealed class GingerbreadCookieSpriteAnimator : MonoBehaviour
    {
        private enum CookiePose
        {
            Idle = 0,
            WalkUp = 1,
            WalkSide = 2,
            WalkDown = 3,
            Attack = 4
        }

        [Header("References")]
        [SerializeField] private EnemyController enemyController;
        [SerializeField] private EnemyVisual enemyVisual;
        [SerializeField] private GingerbreadCookieAttackBrain cookieBrain;
        [SerializeField] private GingerbreadCookieAttackHitbox attackHitbox;
        [SerializeField] private SpriteRenderer bodySpriteRenderer;

        [Header("Idle Frames")]
        [SerializeField] private Sprite idleDownSprite;
        [SerializeField] private Sprite idleUpSprite;

        [Header("Movement Frames")]
        [SerializeField] private Sprite walkDownFrame1;
        [SerializeField] private Sprite walkDownFrame2;
        [SerializeField] private Sprite walkUpFrame1;
        [SerializeField] private Sprite walkUpFrame2;
        [SerializeField] private Sprite walkSideFrame1;
        [SerializeField] private Sprite walkSideFrame2;

        [Header("Attack")]
        [SerializeField] private Sprite attackSprite;

        [Header("Timing")]
        [SerializeField] [Min(0.02f)] private float walkFrameInterval = 0.15f;
        [SerializeField] [Min(0f)] private float movingThreshold = 0.02f;
        [SerializeField] [Range(0.5f, 2f)] private float verticalBias = 1.05f;

        private CookiePose _currentPose;
        private float _walkFrameTimer;
        private bool _walkFrameToggle;
        private Sprite _lastAppliedSprite;
        private Vector2 _lastFacingDirection = Vector2.down;
        private Vector3 _baseSpriteLocalScale = Vector3.one;
        private Vector2 _referenceSpriteWorldSize = Vector2.one;
        private bool _hasScaleReference;

        private void Awake()
        {
            ResolveReferences();
        }

        private void OnEnable()
        {
            ResolveReferences();
            RefreshSprite(forceReset: true);
        }

        private void LateUpdate()
        {
            ResolveReferences();

            if (bodySpriteRenderer == null)
            {
                return;
            }

            if (enemyController != null
                && enemyController.EnemyHealth != null
                && enemyController.EnemyHealth.IsDead)
            {
                return;
            }

            RefreshSprite(forceReset: false);
        }

        private void OnValidate()
        {
            ResolveReferences();
            RefreshSprite(forceReset: true);
        }

        private void RefreshSprite(bool forceReset)
        {
            CookiePose nextPose = ResolvePose();

            if (forceReset || nextPose != _currentPose)
            {
                _currentPose = nextPose;
                _walkFrameTimer = 0f;
                _walkFrameToggle = false;
            }

            ApplyFacing(nextPose);
            ApplySprite(ResolveSpriteForPose(nextPose));
        }

        private CookiePose ResolvePose()
        {
            if (cookieBrain != null && cookieBrain.IsAttacking)
            {
                SyncBrainFacing();
                return CookiePose.Attack;
            }

            Vector2 moveDirection = enemyController != null && enemyController.EnemyMovement != null
                ? enemyController.EnemyMovement.CurrentMoveDirection
                : Vector2.zero;

            if (moveDirection.sqrMagnitude <= movingThreshold * movingThreshold)
            {
                return CookiePose.Idle;
            }

            _lastFacingDirection = moveDirection.normalized;

            if (Mathf.Abs(moveDirection.y) >= Mathf.Abs(moveDirection.x) * verticalBias)
            {
                return moveDirection.y > 0f ? CookiePose.WalkUp : CookiePose.WalkDown;
            }

            return CookiePose.WalkSide;
        }

        private Sprite ResolveSpriteForPose(CookiePose pose)
        {
            switch (pose)
            {
                case CookiePose.Attack:
                    return attackSprite != null ? attackSprite : ResolveDirectionalIdleSprite();
                case CookiePose.WalkUp:
                    return ResolveWalkSprite(walkUpFrame1, walkUpFrame2);
                case CookiePose.WalkSide:
                    return ResolveWalkSprite(walkSideFrame1, walkSideFrame2);
                case CookiePose.WalkDown:
                    return ResolveWalkSprite(walkDownFrame1, walkDownFrame2);
                case CookiePose.Idle:
                default:
                    return ResolveDirectionalIdleSprite();
            }
        }

        private Sprite ResolveDirectionalIdleSprite()
        {
            CookiePose facingPose = ResolveFacingPose(_lastFacingDirection);

            switch (facingPose)
            {
                case CookiePose.WalkUp:
                    return idleUpSprite != null ? idleUpSprite : ResolveFallbackSprite();
                case CookiePose.WalkSide:
                    return walkSideFrame1 != null ? walkSideFrame1 : ResolveFallbackSprite();
                case CookiePose.WalkDown:
                default:
                    return idleDownSprite != null ? idleDownSprite : ResolveFallbackSprite();
            }
        }

        private CookiePose ResolveFacingPose(Vector2 direction)
        {
            if (Mathf.Abs(direction.y) >= Mathf.Abs(direction.x) * verticalBias)
            {
                return direction.y > 0f ? CookiePose.WalkUp : CookiePose.WalkDown;
            }

            return CookiePose.WalkSide;
        }

        private Sprite ResolveWalkSprite(Sprite frameA, Sprite frameB)
        {
            if (walkFrameInterval > 0f)
            {
                _walkFrameTimer += Time.deltaTime;

                while (_walkFrameTimer >= walkFrameInterval)
                {
                    _walkFrameTimer -= walkFrameInterval;
                    _walkFrameToggle = !_walkFrameToggle;
                }
            }

            Sprite resolved = _walkFrameToggle
                ? frameB != null ? frameB : frameA
                : frameA != null ? frameA : frameB;

            return resolved != null ? resolved : ResolveFallbackSprite();
        }

        private Sprite ResolveFallbackSprite()
        {
            if (idleDownSprite != null)
            {
                return idleDownSprite;
            }

            if (walkDownFrame1 != null)
            {
                return walkDownFrame1;
            }

            if (walkSideFrame1 != null)
            {
                return walkSideFrame1;
            }

            if (walkUpFrame1 != null)
            {
                return walkUpFrame1;
            }

            return attackSprite;
        }

        private void SyncBrainFacing()
        {
            if (cookieBrain == null)
            {
                return;
            }

            Vector2 direction = cookieBrain.LastAimDirection;

            if (direction.sqrMagnitude > 0.0001f)
            {
                _lastFacingDirection = direction.normalized;
            }
        }

        private void ApplyFacing(CookiePose pose)
        {
            if (bodySpriteRenderer == null)
            {
                return;
            }

            bool sideFacing = pose == CookiePose.WalkSide
                || pose == CookiePose.Attack
                || (pose == CookiePose.Idle && ResolveFacingPose(_lastFacingDirection) == CookiePose.WalkSide);

            bodySpriteRenderer.flipX = sideFacing && _lastFacingDirection.x > 0f;
        }

        private void ApplySprite(Sprite sprite)
        {
            if (bodySpriteRenderer == null || sprite == null || _lastAppliedSprite == sprite)
            {
                return;
            }

            bodySpriteRenderer.sprite = sprite;
            ApplySpriteSizeNormalization(sprite);
            _lastAppliedSprite = sprite;

            if (attackHitbox != null && attackHitbox.IsActive)
            {
                attackHitbox.FitColliderToCurrentSprite();
            }
        }

        private void ApplySpriteSizeNormalization(Sprite sprite)
        {
            if (bodySpriteRenderer == null || sprite == null)
            {
                return;
            }

            CacheScaleReference();

            Vector2 spriteSize = sprite.bounds.size;

            if (spriteSize.x <= Mathf.Epsilon
                || spriteSize.y <= Mathf.Epsilon
                || _referenceSpriteWorldSize.x <= Mathf.Epsilon
                || _referenceSpriteWorldSize.y <= Mathf.Epsilon)
            {
                bodySpriteRenderer.transform.localScale = _baseSpriteLocalScale;
                return;
            }

            float scaleMultiplier = _referenceSpriteWorldSize.y / spriteSize.y;

            bodySpriteRenderer.transform.localScale = new Vector3(
                _baseSpriteLocalScale.x * scaleMultiplier,
                _baseSpriteLocalScale.y * scaleMultiplier,
                _baseSpriteLocalScale.z);
        }

        private void CacheScaleReference()
        {
            if (_hasScaleReference || bodySpriteRenderer == null)
            {
                return;
            }

            Sprite referenceSprite = idleDownSprite != null ? idleDownSprite : ResolveFallbackSprite();
            _baseSpriteLocalScale = bodySpriteRenderer.transform.localScale;
            _referenceSpriteWorldSize = referenceSprite != null ? referenceSprite.bounds.size : Vector2.one;
            _hasScaleReference = true;
        }

        private void ResolveReferences()
        {
            if (enemyController == null)
            {
                TryGetComponent(out enemyController);
            }

            if (enemyVisual == null)
            {
                TryGetComponent(out enemyVisual);
            }

            if (cookieBrain == null)
            {
                TryGetComponent(out cookieBrain);
            }

            if (attackHitbox == null)
            {
                attackHitbox = GetComponentInChildren<GingerbreadCookieAttackHitbox>(true);
            }

            if (bodySpriteRenderer == null)
            {
                bodySpriteRenderer = enemyVisual != null && enemyVisual.BodySpriteRenderer != null
                    ? enemyVisual.BodySpriteRenderer
                    : GetComponentInChildren<SpriteRenderer>(true);
            }
        }
    }
}
