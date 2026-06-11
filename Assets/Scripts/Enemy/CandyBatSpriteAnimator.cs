using UnityEngine;

namespace CuteIssac.Enemy
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(EnemyController))]
    [DefaultExecutionOrder(110)]
    public sealed class CandyBatSpriteAnimator : MonoBehaviour
    {
        private enum BatPose
        {
            Front = 0,
            Side = 1,
            Up = 2
        }

        [Header("References")]
        [SerializeField] private EnemyController enemyController;
        [SerializeField] private EnemyVisual enemyVisual;
        [SerializeField] private CandyBatShooterBrain batBrain;
        [SerializeField] private SpriteRenderer bodySpriteRenderer;

        [Header("Front Flight")]
        [SerializeField] private Sprite frontFrame1;
        [SerializeField] private Sprite frontFrame2;

        [Header("Side Flight")]
        [SerializeField] private Sprite sideFrame1;
        [SerializeField] private Sprite sideFrame2;

        [Header("Up Flight")]
        [SerializeField] private Sprite upFrame1;
        [SerializeField] private Sprite upFrame2;

        [Header("Timing")]
        [SerializeField] [Min(0.02f)] private float flightFrameInterval = 0.14f;
        [SerializeField] [Min(0f)] private float movingThreshold = 0.02f;
        [SerializeField] [Range(0.5f, 2f)] private float verticalBias = 1.05f;

        private BatPose _currentPose;
        private float _frameTimer;
        private bool _frameToggle;
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
            RefreshSprite(true);
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

            RefreshSprite(false);
        }

        private void OnValidate()
        {
            ResolveReferences();
            _hasScaleReference = false;
            _lastAppliedSprite = null;
        }

        private void RefreshSprite(bool forceReset)
        {
            BatPose nextPose = ResolvePose();

            if (forceReset || nextPose != _currentPose)
            {
                _currentPose = nextPose;
                _frameTimer = 0f;
                _frameToggle = false;
            }

            AdvanceFlightFrame();
            ApplyFacing(nextPose);
            ApplySprite(ResolveSpriteForPose(nextPose));
        }

        private BatPose ResolvePose()
        {
            if (batBrain != null && batBrain.IsChargingShot)
            {
                SyncBrainFacing();
                return ResolveFacingPose(_lastFacingDirection);
            }

            Vector2 moveDirection = enemyController != null && enemyController.EnemyMovement != null
                ? enemyController.EnemyMovement.CurrentMoveDirection
                : Vector2.zero;

            if (moveDirection.sqrMagnitude > movingThreshold * movingThreshold)
            {
                _lastFacingDirection = moveDirection.normalized;
            }

            return ResolveFacingPose(_lastFacingDirection);
        }

        private BatPose ResolveFacingPose(Vector2 direction)
        {
            if (direction.y > 0f && Mathf.Abs(direction.y) >= Mathf.Abs(direction.x) * verticalBias)
            {
                return BatPose.Up;
            }

            if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y) * 0.78f)
            {
                return BatPose.Side;
            }

            return BatPose.Front;
        }

        private void AdvanceFlightFrame()
        {
            if (flightFrameInterval <= 0f)
            {
                return;
            }

            _frameTimer += Time.deltaTime;

            while (_frameTimer >= flightFrameInterval)
            {
                _frameTimer -= flightFrameInterval;
                _frameToggle = !_frameToggle;
            }
        }

        private Sprite ResolveSpriteForPose(BatPose pose)
        {
            return pose switch
            {
                BatPose.Up => ResolveFrame(upFrame1, upFrame2),
                BatPose.Side => ResolveFrame(sideFrame1, sideFrame2),
                _ => ResolveFrame(frontFrame1, frontFrame2)
            };
        }

        private Sprite ResolveFrame(Sprite frameA, Sprite frameB)
        {
            Sprite resolved = _frameToggle
                ? frameB != null ? frameB : frameA
                : frameA != null ? frameA : frameB;

            return resolved != null ? resolved : ResolveFallbackSprite();
        }

        private Sprite ResolveFallbackSprite()
        {
            if (frontFrame1 != null)
            {
                return frontFrame1;
            }

            if (frontFrame2 != null)
            {
                return frontFrame2;
            }

            if (sideFrame1 != null)
            {
                return sideFrame1;
            }

            if (upFrame1 != null)
            {
                return upFrame1;
            }

            return sideFrame2 != null ? sideFrame2 : upFrame2;
        }

        private void SyncBrainFacing()
        {
            if (batBrain == null)
            {
                return;
            }

            Vector2 direction = batBrain.LastAimDirection;

            if (direction.sqrMagnitude > 0.0001f)
            {
                _lastFacingDirection = direction.normalized;
            }
        }

        private void ApplyFacing(BatPose pose)
        {
            if (bodySpriteRenderer == null)
            {
                return;
            }

            bodySpriteRenderer.flipX = pose == BatPose.Side && _lastFacingDirection.x < -0.01f;
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

            Sprite referenceSprite = frontFrame1 != null ? frontFrame1 : ResolveFallbackSprite();
            _baseSpriteLocalScale = bodySpriteRenderer.transform.localScale;
            _referenceSpriteWorldSize = referenceSprite != null ? (Vector2)referenceSprite.bounds.size : Vector2.one;
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

            if (batBrain == null)
            {
                TryGetComponent(out batBrain);
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
