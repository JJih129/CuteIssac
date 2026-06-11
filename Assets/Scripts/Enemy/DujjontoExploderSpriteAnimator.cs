using System;
using UnityEngine;

namespace CuteIssac.Enemy
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(EnemyController))]
    [DefaultExecutionOrder(120)]
    public sealed class DujjontoExploderSpriteAnimator : MonoBehaviour
    {
        private enum DujjontoPose
        {
            Down = 0,
            Side = 1,
            Up = 2
        }

        [Header("References")]
        [SerializeField] private EnemyController enemyController;
        [SerializeField] private EnemyVisual enemyVisual;
        [SerializeField] private ExploderEnemyBrain exploderBrain;
        [SerializeField] private SpriteRenderer bodySpriteRenderer;

        [Header("Movement Frames")]
        [SerializeField] private Sprite[] walkDownFrames = Array.Empty<Sprite>();
        [SerializeField] private Sprite[] walkSideFrames = Array.Empty<Sprite>();
        [SerializeField] private Sprite[] walkUpFrames = Array.Empty<Sprite>();

        [Header("Explosion Frames")]
        [SerializeField] private Sprite[] explosionFrontFrames = Array.Empty<Sprite>();
        [SerializeField] private Sprite[] explosionSideFrames = Array.Empty<Sprite>();

        [Header("Timing")]
        [SerializeField] [Min(0.02f)] private float walkFrameInterval = 0.1f;
        [SerializeField] [Min(0.02f)] private float explosionFrameInterval = 0.08f;
        [SerializeField] [Min(0f)] private float movingThreshold = 0.02f;
        [SerializeField] [Range(0.5f, 2f)] private float verticalBias = 1.05f;
        [SerializeField] [Min(0.1f)] private float movementWorldHeight = 0.72f;
        [SerializeField] [Min(0.1f)] private float windupWorldHeight = 0.88f;
        [SerializeField] [Min(0f)] private float minimumExplosionFrameWidthPixels = 96f;
        [SerializeField] [Min(0f)] private float minimumExplosionFrameHeightPixels = 96f;

        private DujjontoPose _currentPose = DujjontoPose.Down;
        private float _frameTimer;
        private int _frameIndex;
        private bool _wasWindingUp;
        private Sprite _lastAppliedSprite;
        private Vector2 _lastFacingDirection = Vector2.down;
        private Vector3 _baseSpriteLocalScale = Vector3.one;
        private Vector3 _baseSpriteLocalPosition;
        private bool _hasScaleReference;

        private void Awake()
        {
            ResolveReferences();
        }

        private void OnEnable()
        {
            ResolveReferences();
            ResetFrameState();
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
            walkFrameInterval = Mathf.Max(0.02f, walkFrameInterval);
            explosionFrameInterval = Mathf.Max(0.02f, explosionFrameInterval);
            movingThreshold = Mathf.Max(0f, movingThreshold);
            movementWorldHeight = Mathf.Max(0.1f, movementWorldHeight);
            windupWorldHeight = Mathf.Max(0.1f, windupWorldHeight);
            minimumExplosionFrameWidthPixels = Mathf.Max(0f, minimumExplosionFrameWidthPixels);
            minimumExplosionFrameHeightPixels = Mathf.Max(0f, minimumExplosionFrameHeightPixels);
        }

        private void RefreshSprite(bool forceReset)
        {
            bool isWindingUp = exploderBrain != null && exploderBrain.IsWindingUp;
            DujjontoPose nextPose = ResolvePose(isWindingUp);

            if (forceReset || nextPose != _currentPose || isWindingUp != _wasWindingUp)
            {
                _currentPose = nextPose;
                _wasWindingUp = isWindingUp;
                ResetFrameState();
            }

            AdvanceFrame(isWindingUp);
            ApplyFacing(nextPose);
            ApplySprite(isWindingUp ? ResolveExplosionSprite(nextPose) : ResolveWalkSprite(nextPose), isWindingUp);
        }

        private DujjontoPose ResolvePose(bool isWindingUp)
        {
            Vector2 moveDirection = enemyController != null && enemyController.EnemyMovement != null
                ? enemyController.EnemyMovement.CurrentMoveDirection
                : Vector2.zero;

            if (!isWindingUp && moveDirection.sqrMagnitude > movingThreshold * movingThreshold)
            {
                _lastFacingDirection = moveDirection.normalized;
            }

            return ResolveFacingPose(_lastFacingDirection);
        }

        private DujjontoPose ResolveFacingPose(Vector2 direction)
        {
            if (Mathf.Abs(direction.y) >= Mathf.Abs(direction.x) * verticalBias)
            {
                return direction.y > 0f ? DujjontoPose.Up : DujjontoPose.Down;
            }

            return DujjontoPose.Side;
        }

        private void AdvanceFrame(bool isWindingUp)
        {
            Sprite[] frames = isWindingUp ? ResolveExplosionFrames(_currentPose) : ResolveWalkFrames(_currentPose);
            int frameCount = frames != null ? frames.Length : 0;

            if (frameCount <= 1)
            {
                _frameIndex = 0;
                return;
            }

            float interval = isWindingUp ? explosionFrameInterval : walkFrameInterval;
            _frameTimer += Time.deltaTime;

            while (_frameTimer >= interval)
            {
                _frameTimer -= interval;
                _frameIndex = (_frameIndex + 1) % frameCount;
            }
        }

        private Sprite ResolveWalkSprite(DujjontoPose pose)
        {
            return ResolveFrame(ResolveWalkFrames(pose), false);
        }

        private Sprite ResolveExplosionSprite(DujjontoPose pose)
        {
            return ResolveFrame(ResolveExplosionFrames(pose), true);
        }

        private Sprite[] ResolveWalkFrames(DujjontoPose pose)
        {
            return pose switch
            {
                DujjontoPose.Up => walkUpFrames,
                DujjontoPose.Side => walkSideFrames,
                _ => walkDownFrames
            };
        }

        private Sprite[] ResolveExplosionFrames(DujjontoPose pose)
        {
            return pose == DujjontoPose.Side && explosionSideFrames != null && explosionSideFrames.Length > 0
                ? explosionSideFrames
                : explosionFrontFrames;
        }

        private Sprite ResolveFrame(Sprite[] frames, bool isWindingUp)
        {
            if (frames != null && frames.Length > 0)
            {
                int index = Mathf.Clamp(_frameIndex, 0, frames.Length - 1);
                Sprite sprite = frames[index];

                if (IsUsableFrame(sprite, isWindingUp))
                {
                    return sprite;
                }

                for (int offset = 1; offset < frames.Length; offset++)
                {
                    int candidateIndex = (_frameIndex + offset) % frames.Length;
                    if (IsUsableFrame(frames[candidateIndex], isWindingUp))
                    {
                        return frames[candidateIndex];
                    }
                }
            }

            return ResolveFallbackSprite();
        }

        private Sprite ResolveFallbackSprite()
        {
            Sprite sprite = FirstSprite(walkDownFrames);
            if (sprite != null)
            {
                return sprite;
            }

            sprite = FirstSprite(walkSideFrames);
            if (sprite != null)
            {
                return sprite;
            }

            sprite = FirstSprite(walkUpFrames);
            if (sprite != null)
            {
                return sprite;
            }

            sprite = FirstSprite(explosionFrontFrames);
            if (sprite != null)
            {
                return sprite;
            }

            sprite = FirstSprite(explosionSideFrames);
            return sprite != null ? sprite : bodySpriteRenderer != null ? bodySpriteRenderer.sprite : null;
        }

        private static Sprite FirstSprite(Sprite[] frames)
        {
            if (frames == null)
            {
                return null;
            }

            for (int i = 0; i < frames.Length; i++)
            {
                if (frames[i] != null)
                {
                    return frames[i];
                }
            }

            return null;
        }

        private void ApplyFacing(DujjontoPose pose)
        {
            if (bodySpriteRenderer == null)
            {
                return;
            }

            bodySpriteRenderer.flipX = pose == DujjontoPose.Side && _lastFacingDirection.x < -0.01f;
        }

        private bool IsUsableFrame(Sprite sprite, bool isWindingUp)
        {
            if (sprite == null)
            {
                return false;
            }

            return !isWindingUp
                || (sprite.rect.width >= minimumExplosionFrameWidthPixels
                    && sprite.rect.height >= minimumExplosionFrameHeightPixels);
        }

        private void ApplySprite(Sprite sprite, bool isWindingUp)
        {
            if (bodySpriteRenderer == null || sprite == null)
            {
                return;
            }

            if (_lastAppliedSprite != sprite)
            {
                bodySpriteRenderer.sprite = sprite;
                _lastAppliedSprite = sprite;
            }

            ApplySpriteSizeNormalization(sprite, isWindingUp);
        }

        private void ApplySpriteSizeNormalization(Sprite sprite, bool isWindingUp)
        {
            CacheScaleReference();

            Bounds spriteBounds = sprite != null ? sprite.bounds : new Bounds(Vector3.zero, Vector3.one);
            Vector2 spriteSize = spriteBounds.size;

            if (spriteSize.x <= Mathf.Epsilon
                || spriteSize.y <= Mathf.Epsilon
                || Mathf.Abs(_baseSpriteLocalScale.y) <= Mathf.Epsilon)
            {
                bodySpriteRenderer.transform.localScale = _baseSpriteLocalScale;
                bodySpriteRenderer.transform.localPosition = _baseSpriteLocalPosition;
                return;
            }

            float targetWorldHeight = isWindingUp ? windupWorldHeight : movementWorldHeight;
            float scaleMultiplier = targetWorldHeight / (spriteSize.y * Mathf.Abs(_baseSpriteLocalScale.y));
            Vector3 localScale = new(
                _baseSpriteLocalScale.x * scaleMultiplier,
                _baseSpriteLocalScale.y * scaleMultiplier,
                _baseSpriteLocalScale.z);
            bodySpriteRenderer.transform.localScale = localScale;

            float pivotCenterX = bodySpriteRenderer.flipX ? -spriteBounds.center.x : spriteBounds.center.x;
            bodySpriteRenderer.transform.localPosition = _baseSpriteLocalPosition - new Vector3(
                pivotCenterX * localScale.x,
                spriteBounds.center.y * localScale.y,
                0f);
        }

        private void CacheScaleReference()
        {
            if (_hasScaleReference || bodySpriteRenderer == null)
            {
                return;
            }

            _baseSpriteLocalScale = bodySpriteRenderer.transform.localScale;
            _baseSpriteLocalPosition = bodySpriteRenderer.transform.localPosition;
            _hasScaleReference = true;
        }

        private void ResetFrameState()
        {
            _frameTimer = 0f;
            _frameIndex = 0;
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

            if (exploderBrain == null)
            {
                TryGetComponent(out exploderBrain);
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
