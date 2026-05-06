using UnityEngine;

namespace CuteIssac.Enemy
{
    /// <summary>
    /// Sprite-only animator for Macaron Cat's slow chase and fast chase states.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(EnemyController))]
    [DefaultExecutionOrder(110)]
    public sealed class MacaronChaserSpriteAnimator : MonoBehaviour
    {
        private enum MovementPose
        {
            Idle = 0,
            WalkUp = 1,
            WalkRight = 2,
            WalkDown = 3,
            FastChase = 4,
            Recovery = 5
        }

        [Header("References")]
        [SerializeField] private EnemyController enemyController;
        [SerializeField] private EnemyVisual enemyVisual;
        [SerializeField] private MacaronChaserBrain macaronBrain;
        [SerializeField] private SpriteRenderer bodySpriteRenderer;

        [Header("Movement Frames")]
        [SerializeField] private Sprite walkUpFrame1;
        [SerializeField] private Sprite walkUpFrame2;
        [SerializeField] private Sprite walkRightFrame1;
        [SerializeField] private Sprite walkRightFrame2;
        [SerializeField] private Sprite walkDownFrame1;
        [SerializeField] private Sprite walkDownFrame2;

        [Header("Fast Chase Frames")]
        [SerializeField] private Sprite fastChaseSprite;
        [SerializeField] private Sprite recoverySprite;

        [Header("Timing")]
        [SerializeField] [Min(0.02f)] private float slowWalkFrameInterval = 0.16f;
        [SerializeField] [Min(0.02f)] private float fastWalkFrameInterval = 0.08f;
        [SerializeField] [Min(0f)] private float movingThreshold = 0.02f;
        [SerializeField] [Range(0.5f, 2f)] private float verticalBias = 1.05f;

        private MovementPose _currentPose;
        private float _walkFrameTimer;
        private bool _walkFrameToggle;
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
            MovementPose nextPose = ResolvePose();

            if (forceReset || nextPose != _currentPose)
            {
                _currentPose = nextPose;
                _walkFrameTimer = 0f;
                _walkFrameToggle = false;
            }

            ApplyFacing(nextPose);
            ApplySprite(ResolveSpriteForPose(nextPose));
        }

        private MovementPose ResolvePose()
        {
            if (macaronBrain != null)
            {
                if (macaronBrain.IsFastChasing)
                {
                    SyncBrainFacing();
                    return MovementPose.FastChase;
                }

                if (macaronBrain.IsRecovering)
                {
                    SyncBrainFacing();
                    return MovementPose.Recovery;
                }
            }

            Vector2 moveDirection = enemyController != null && enemyController.EnemyMovement != null
                ? enemyController.EnemyMovement.CurrentMoveDirection
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
                case MovementPose.FastChase:
                    return fastChaseSprite != null ? fastChaseSprite : ResolveDirectionalWalkSprite(useFastTiming: true);
                case MovementPose.Recovery:
                    return recoverySprite != null ? recoverySprite : ResolveDirectionalWalkSprite(useFastTiming: false);
                case MovementPose.WalkUp:
                case MovementPose.WalkRight:
                case MovementPose.WalkDown:
                    return ResolveDirectionalWalkSprite(useFastTiming: false);
                case MovementPose.Idle:
                default:
                    return ResolveIdleSprite();
            }
        }

        private Sprite ResolveDirectionalWalkSprite(bool useFastTiming)
        {
            MovementPose facingPose = ResolveFacingPose(_lastFacingDirection);

            switch (facingPose)
            {
                case MovementPose.WalkUp:
                    return ResolveWalkSprite(walkUpFrame1, walkUpFrame2, useFastTiming);
                case MovementPose.WalkRight:
                    return ResolveWalkSprite(walkRightFrame1, walkRightFrame2, useFastTiming);
                case MovementPose.WalkDown:
                default:
                    return ResolveWalkSprite(walkDownFrame1, walkDownFrame2, useFastTiming);
            }
        }

        private Sprite ResolveIdleSprite()
        {
            MovementPose facingPose = ResolveFacingPose(_lastFacingDirection);

            switch (facingPose)
            {
                case MovementPose.WalkUp:
                    return walkUpFrame1 != null ? walkUpFrame1 : ResolveFallbackSprite();
                case MovementPose.WalkRight:
                    return walkRightFrame1 != null ? walkRightFrame1 : ResolveFallbackSprite();
                case MovementPose.WalkDown:
                default:
                    return walkDownFrame1 != null ? walkDownFrame1 : ResolveFallbackSprite();
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

        private Sprite ResolveWalkSprite(Sprite frameA, Sprite frameB, bool useFastTiming)
        {
            float interval = useFastTiming ? fastWalkFrameInterval : slowWalkFrameInterval;

            if (interval > 0f)
            {
                _walkFrameTimer += Time.deltaTime;

                while (_walkFrameTimer >= interval)
                {
                    _walkFrameTimer -= interval;
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

            if (fastChaseSprite != null)
            {
                return fastChaseSprite;
            }

            return recoverySprite;
        }

        private void SyncBrainFacing()
        {
            if (macaronBrain == null)
            {
                return;
            }

            Vector2 direction = macaronBrain.LastChaseDirection;

            if (direction.sqrMagnitude > 0.0001f)
            {
                _lastFacingDirection = direction.normalized;
            }
        }

        private void ApplyFacing(MovementPose pose)
        {
            if (bodySpriteRenderer == null)
            {
                return;
            }

            bool shouldFlip = false;

            if (pose == MovementPose.FastChase || pose == MovementPose.Recovery)
            {
                shouldFlip = _lastFacingDirection.x < 0f;
            }
            else if (pose == MovementPose.WalkRight
                || (pose == MovementPose.Idle && ResolveFacingPose(_lastFacingDirection) == MovementPose.WalkRight))
            {
                shouldFlip = _lastFacingDirection.x < 0f;
            }

            bodySpriteRenderer.flipX = shouldFlip;
        }

        private void ApplySprite(Sprite sprite)
        {
            if (bodySpriteRenderer == null || sprite == null || _lastAppliedSprite == sprite)
            {
                return;
            }

            bodySpriteRenderer.sprite = sprite;
            _lastAppliedSprite = sprite;
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

            if (macaronBrain == null)
            {
                TryGetComponent(out macaronBrain);
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
