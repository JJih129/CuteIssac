using UnityEngine;

namespace CuteIssac.Enemy
{
    /// <summary>
    /// Drives the macaron dasher sprite sequence without an Animator.
    /// Keeps the body renderer as the single animated surface so color flashes still work.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(EnemyController))]
    [DefaultExecutionOrder(100)]
    public sealed class DasherEnemySpriteAnimator : MonoBehaviour
    {
        private enum MovementPose
        {
            Idle = 0,
            WalkUp = 1,
            WalkRight = 2,
            WalkDown = 3,
            Dash = 4,
            Landing = 5
        }

        [Header("Movement Frames")]
        [SerializeField] private Sprite idleDownFrame;
        [SerializeField] private Sprite idleUpFrame;
        [SerializeField] private Sprite idleRightFrame;
        [SerializeField] private Sprite walkUpFrame1;
        [SerializeField] private Sprite walkUpFrame2;
        [SerializeField] private Sprite walkRightFrame1;
        [SerializeField] private Sprite walkRightFrame2;
        [SerializeField] private Sprite walkDownFrame1;
        [SerializeField] private Sprite walkDownFrame2;

        [Header("Dash Frames")]
        [SerializeField] private Sprite dashSprite;
        [SerializeField] private Sprite landingSprite;

        [Header("Timing")]
        [SerializeField] [Min(0.02f)] private float walkFrameInterval = 0.12f;
        [SerializeField] [Min(0f)] private float movingThreshold = 0.02f;
        [SerializeField] [Range(0.5f, 2f)] private float verticalBias = 1.05f;

        [Header("Facing")]
        [Tooltip("Enable when side-facing source sprites are authored looking left instead of right.")]
        [SerializeField] private bool invertHorizontalFacing;

        private EnemyController _enemyController;
        private DasherEnemyBrain _dasherBrain;
        private EnemyVisual _enemyVisual;
        private SpriteRenderer _bodySpriteRenderer;

        private MovementPose _currentPose = MovementPose.Idle;
        private float _walkFrameTimer;
        private bool _walkFrameToggle;
        private Sprite _lastWalkSprite;
        private Sprite _lastAppliedSprite;
        private Vector2 _lastFacingDirection = Vector2.down;

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

            if (_bodySpriteRenderer == null)
            {
                return;
            }

            if (_enemyController != null && _enemyController.EnemyHealth != null && _enemyController.EnemyHealth.IsDead)
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

        private void ResolveReferences()
        {
            if (_enemyController == null)
            {
                TryGetComponent(out _enemyController);
            }

            if (_dasherBrain == null)
            {
                TryGetComponent(out _dasherBrain);
            }

            if (_enemyVisual == null)
            {
                TryGetComponent(out _enemyVisual);
            }

            if (_bodySpriteRenderer == null)
            {
                if (_enemyVisual != null && _enemyVisual.BodySpriteRenderer != null)
                {
                    _bodySpriteRenderer = _enemyVisual.BodySpriteRenderer;
                }
                else
                {
                    _bodySpriteRenderer = GetComponentInChildren<SpriteRenderer>(true);
                }
            }
        }

        private void RefreshSprite(bool forceReset)
        {
            MovementPose nextPose = ResolvePose();

            if (forceReset || nextPose != _currentPose)
            {
                _currentPose = nextPose;
                _walkFrameTimer = 0f;
                _walkFrameToggle = false;
            }

            ApplyFacing(nextPose);
            Sprite nextSprite = ResolveSpriteForPose(nextPose);
            ApplySprite(nextSprite);
        }

        private MovementPose ResolvePose()
        {
            if (_dasherBrain != null)
            {
                if (_dasherBrain.IsDashingWindup || _dasherBrain.IsDashing)
                {
                    SyncDashFacing();
                    return MovementPose.Dash;
                }

                if (_dasherBrain.IsDashingRecovery)
                {
                    SyncDashFacing();
                    return MovementPose.Landing;
                }
            }

            Vector2 moveDirection = _enemyController != null && _enemyController.EnemyMovement != null
                ? _enemyController.EnemyMovement.CurrentMoveDirection
                : Vector2.zero;

            if (moveDirection.sqrMagnitude <= movingThreshold * movingThreshold)
            {
                return MovementPose.Idle;
            }

            _lastFacingDirection = moveDirection.normalized;

            if (Mathf.Abs(moveDirection.y) >= Mathf.Abs(moveDirection.x) * verticalBias)
            {
                return moveDirection.y > 0f ? MovementPose.WalkUp : MovementPose.WalkDown;
            }

            return MovementPose.WalkRight;
        }

        private Sprite ResolveSpriteForPose(MovementPose pose)
        {
            switch (pose)
            {
                case MovementPose.Dash:
                    return dashSprite != null
                        ? dashSprite
                        : landingSprite != null
                            ? landingSprite
                            : _lastWalkSprite != null
                                ? _lastWalkSprite
                                : ResolveFallbackSprite();
                case MovementPose.Landing:
                    return landingSprite != null
                        ? landingSprite
                        : dashSprite != null
                            ? dashSprite
                            : _lastWalkSprite != null
                                ? _lastWalkSprite
                                : ResolveFallbackSprite();
                case MovementPose.WalkUp:
                    return ResolveWalkSprite(walkUpFrame1, walkUpFrame2);
                case MovementPose.WalkRight:
                    return ResolveWalkSprite(walkRightFrame1, walkRightFrame2);
                case MovementPose.WalkDown:
                    return ResolveWalkSprite(walkDownFrame1, walkDownFrame2);
                case MovementPose.Idle:
                default:
                    return ResolveIdleSprite();
            }
        }

        private Sprite ResolveIdleSprite()
        {
            MovementPose idlePose = ResolveFacingPose(_lastFacingDirection);

            switch (idlePose)
            {
                case MovementPose.WalkUp:
                    return idleUpFrame != null
                        ? idleUpFrame
                        : walkUpFrame1 != null
                            ? walkUpFrame1
                            : ResolveFallbackSprite();
                case MovementPose.WalkRight:
                    return idleRightFrame != null
                        ? idleRightFrame
                        : walkRightFrame1 != null
                            ? walkRightFrame1
                            : ResolveFallbackSprite();
                case MovementPose.WalkDown:
                default:
                    return idleDownFrame != null
                        ? idleDownFrame
                        : walkDownFrame1 != null
                            ? walkDownFrame1
                            : ResolveFallbackSprite();
            }
        }

        private MovementPose ResolveFacingPose(Vector2 direction)
        {
            if (Mathf.Abs(direction.y) >= Mathf.Abs(direction.x) * verticalBias)
            {
                return direction.y > 0f ? MovementPose.WalkUp : MovementPose.WalkDown;
            }

            return MovementPose.WalkRight;
        }

        private Sprite ResolveWalkSprite(Sprite frameA, Sprite frameB)
        {
            if (frameA == null && frameB == null)
            {
                return _lastWalkSprite != null
                    ? _lastWalkSprite
                    : (_lastAppliedSprite != null ? _lastAppliedSprite : ResolveFallbackSprite());
            }

            if (walkFrameInterval > 0f)
            {
                _walkFrameTimer += Time.deltaTime;

                while (_walkFrameTimer >= walkFrameInterval)
                {
                    _walkFrameTimer -= walkFrameInterval;
                    _walkFrameToggle = !_walkFrameToggle;
                }
            }

            Sprite resolvedSprite = _walkFrameToggle
                ? frameB != null ? frameB : frameA
                : frameA != null ? frameA : frameB;

            if (resolvedSprite != null)
            {
                _lastWalkSprite = resolvedSprite;
                return resolvedSprite;
            }

            return _lastWalkSprite != null ? _lastWalkSprite : ResolveFallbackSprite();
        }

        private Sprite ResolveFallbackSprite()
        {
            if (walkDownFrame1 != null)
            {
                return walkDownFrame1;
            }

            if (walkRightFrame1 != null)
            {
                return walkRightFrame1;
            }

            if (walkUpFrame1 != null)
            {
                return walkUpFrame1;
            }

            if (dashSprite != null)
            {
                return dashSprite;
            }

            if (landingSprite != null)
            {
                return landingSprite;
            }

            if (walkDownFrame2 != null)
            {
                return walkDownFrame2;
            }

            if (walkRightFrame2 != null)
            {
                return walkRightFrame2;
            }

            return walkUpFrame2;
        }

        private void SyncDashFacing()
        {
            if (_enemyVisual == null || _dasherBrain == null)
            {
                return;
            }

            Vector2 dashDirection = _dasherBrain.DashDirection;

            if (dashDirection.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            _lastFacingDirection = dashDirection.normalized;
            _enemyVisual.SetMoveDirection(dashDirection);
        }

        private void ApplyFacing(MovementPose pose)
        {
            if (_bodySpriteRenderer == null)
            {
                return;
            }

            bool shouldFlip = false;
            bool usesHorizontalFacing = false;

            if (pose == MovementPose.Dash || pose == MovementPose.Landing)
            {
                usesHorizontalFacing = _dasherBrain != null && Mathf.Abs(_dasherBrain.DashDirection.x) > 0.001f;
                shouldFlip = _dasherBrain != null
                    && _dasherBrain.DashDirection.x < 0f;
            }
            else if (pose == MovementPose.WalkRight
                || (pose == MovementPose.Idle && ResolveFacingPose(_lastFacingDirection) == MovementPose.WalkRight))
            {
                usesHorizontalFacing = Mathf.Abs(_lastFacingDirection.x) > 0.001f;
                shouldFlip = _lastFacingDirection.x < 0f;
            }

            if (invertHorizontalFacing && usesHorizontalFacing)
            {
                shouldFlip = !shouldFlip;
            }

            if (_bodySpriteRenderer.flipX != shouldFlip)
            {
                _bodySpriteRenderer.flipX = shouldFlip;
            }
        }

        private void ApplySprite(Sprite nextSprite)
        {
            if (_bodySpriteRenderer == null || nextSprite == null)
            {
                return;
            }

            if (_bodySpriteRenderer.sprite != nextSprite)
            {
                _bodySpriteRenderer.sprite = nextSprite;
            }

            _lastAppliedSprite = nextSprite;
        }
    }
}
