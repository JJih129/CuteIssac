using UnityEngine;

namespace CuteIssac.Enemy
{
    /// <summary>
    /// Lightweight top-down boss sprite animator. It swaps authored sprites directly so boss AI
    /// can keep using the existing pattern brain without AnimatorController asset churn.
    /// </summary>
    [DefaultExecutionOrder(1000)]
    [DisallowMultipleComponent]
    public sealed class BossSpriteAnimator : MonoBehaviour
    {
        private enum FacingDirection
        {
            Down = 0,
            Up = 1,
            Side = 2
        }

        [Header("References")]
        [SerializeField] private EnemyController enemyController;
        [SerializeField] private BossEnemyBrain bossEnemyBrain;
        [SerializeField] private BossVisual bossVisual;
        [SerializeField] private SpriteRenderer bodySpriteRenderer;

        [Header("Idle Sprites")]
        [SerializeField] private Sprite[] idleDownSprites = System.Array.Empty<Sprite>();
        [SerializeField] private Sprite[] idleUpSprites = System.Array.Empty<Sprite>();
        [SerializeField] private Sprite[] idleSideSprites = System.Array.Empty<Sprite>();

        [Header("Move Sprites")]
        [SerializeField] private Sprite[] moveDownSprites = System.Array.Empty<Sprite>();
        [SerializeField] private Sprite[] moveUpSprites = System.Array.Empty<Sprite>();
        [SerializeField] private Sprite[] moveSideSprites = System.Array.Empty<Sprite>();

        [Header("Attack Sprites")]
        [SerializeField] private Sprite[] attackDownSprites = System.Array.Empty<Sprite>();
        [SerializeField] private Sprite[] attackUpSprites = System.Array.Empty<Sprite>();
        [SerializeField] private Sprite[] attackSideSprites = System.Array.Empty<Sprite>();

        [Header("Timing")]
        [SerializeField] [Min(0.1f)] private float idleFramesPerSecond = 3f;
        [SerializeField] [Min(0.1f)] private float moveFramesPerSecond = 7f;
        [SerializeField] [Min(0.1f)] private float attackFramesPerSecond = 8f;
        [SerializeField] [Min(0f)] private float attackHoldDuration = 0.28f;
        [SerializeField] [Min(0f)] private float telegraphPoseMinimumDuration = 0.12f;

        [Header("Direction")]
        [SerializeField] [Min(0f)] private float movingThreshold = 0.015f;
        [SerializeField] [Range(0.5f, 2f)] private float verticalBias = 1.05f;
        [SerializeField] private bool flipSideSpriteForLeft = true;
        [SerializeField] private bool faceTargetDuringAttack = true;

        [Header("Frame Validation")]
        [SerializeField] private bool filterTinySlicedFrames = true;
        [SerializeField] [Min(1f)] private float minimumFrameWidth = 256f;
        [SerializeField] [Min(1f)] private float minimumFrameHeight = 256f;
        [SerializeField] private bool logFilteredFrames;

        private Vector2 _lastFacingDirection = Vector2.down;
        private float _attackPoseRemaining;
        private bool _telegraphPoseActive;
        private Sprite _lastAppliedSprite;
        private bool _hasSubscribed;
        private Sprite[] _cachedIdleDownSprites = System.Array.Empty<Sprite>();
        private Sprite[] _cachedIdleUpSprites = System.Array.Empty<Sprite>();
        private Sprite[] _cachedIdleSideSprites = System.Array.Empty<Sprite>();
        private Sprite[] _cachedMoveDownSprites = System.Array.Empty<Sprite>();
        private Sprite[] _cachedMoveUpSprites = System.Array.Empty<Sprite>();
        private Sprite[] _cachedMoveSideSprites = System.Array.Empty<Sprite>();
        private Sprite[] _cachedAttackDownSprites = System.Array.Empty<Sprite>();
        private Sprite[] _cachedAttackUpSprites = System.Array.Empty<Sprite>();
        private Sprite[] _cachedAttackSideSprites = System.Array.Empty<Sprite>();

        private void Awake()
        {
            ResolveReferences();
            RebuildFrameCache();
        }

        private void OnEnable()
        {
            ResolveReferences();
            RebuildFrameCache();
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
            _attackPoseRemaining = 0f;
            _telegraphPoseActive = false;
            _lastAppliedSprite = null;
        }

        private void LateUpdate()
        {
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

            // BossVisual/EnemyVisual can still touch the authored prototype renderer.
            // Keep the boss art renderer alive and apply the final sprite after those updates.
            if (!bodySpriteRenderer.gameObject.activeSelf)
            {
                bodySpriteRenderer.gameObject.SetActive(true);
            }

            if (!bodySpriteRenderer.enabled)
            {
                bodySpriteRenderer.enabled = true;
            }

            if (_attackPoseRemaining > 0f)
            {
                _attackPoseRemaining = Mathf.Max(0f, _attackPoseRemaining - Time.deltaTime);
            }

            Vector2 moveDirection = ResolveMoveDirection();
            Vector2 facingDirection = ResolveFacingDirection(moveDirection);
            bool isMoving = moveDirection.sqrMagnitude > movingThreshold * movingThreshold;
            bool isAttacking = _telegraphPoseActive || _attackPoseRemaining > 0f;
            FacingDirection facing = ResolveFacing(facingDirection);
            Sprite[] frames = ResolveFrames(facing, isAttacking, isMoving);

            if (frames == null || frames.Length == 0)
            {
                return;
            }

            float fps = isAttacking ? attackFramesPerSecond : isMoving ? moveFramesPerSecond : idleFramesPerSecond;
            int frameIndex = frames.Length == 1 ? 0 : Mathf.FloorToInt(Time.time * fps) % frames.Length;
            Sprite nextSprite = frames[frameIndex];

            if (nextSprite != null)
            {
                bodySpriteRenderer.sprite = nextSprite;
                _lastAppliedSprite = nextSprite;
            }

            if (facing == FacingDirection.Side && flipSideSpriteForLeft)
            {
                bodySpriteRenderer.flipX = facingDirection.x < 0f;
            }
            else if (facing != FacingDirection.Side)
            {
                bodySpriteRenderer.flipX = false;
            }
        }

        private Vector2 ResolveMoveDirection()
        {
            if (enemyController != null && enemyController.EnemyMovement != null)
            {
                return enemyController.EnemyMovement.CurrentMoveDirection;
            }

            return Vector2.zero;
        }

        private Vector2 ResolveFacingDirection(Vector2 moveDirection)
        {
            Vector2 direction = moveDirection;

            if ((_telegraphPoseActive || _attackPoseRemaining > 0f) && faceTargetDuringAttack && enemyController != null && enemyController.HasTarget)
            {
                direction = enemyController.TargetPosition - enemyController.Position;
            }

            if (direction.sqrMagnitude > 0.0001f)
            {
                _lastFacingDirection = direction.normalized;
            }

            return _lastFacingDirection;
        }

        private FacingDirection ResolveFacing(Vector2 direction)
        {
            if (Mathf.Abs(direction.y) >= Mathf.Abs(direction.x) * verticalBias)
            {
                return direction.y > 0f ? FacingDirection.Up : FacingDirection.Down;
            }

            return FacingDirection.Side;
        }

        private Sprite[] ResolveFrames(FacingDirection facing, bool isAttacking, bool isMoving)
        {
            if (isAttacking)
            {
                Sprite[] attackFrames = SelectDirectionalFrames(facing, _cachedAttackDownSprites, _cachedAttackUpSprites, _cachedAttackSideSprites);
                if (attackFrames != null && attackFrames.Length > 0)
                {
                    return attackFrames;
                }
            }

            if (isMoving)
            {
                Sprite[] moveFrames = SelectDirectionalFrames(facing, _cachedMoveDownSprites, _cachedMoveUpSprites, _cachedMoveSideSprites);
                if (moveFrames != null && moveFrames.Length > 0)
                {
                    return moveFrames;
                }
            }

            Sprite[] idleFrames = SelectDirectionalFrames(facing, _cachedIdleDownSprites, _cachedIdleUpSprites, _cachedIdleSideSprites);
            if (idleFrames != null && idleFrames.Length > 0)
            {
                return idleFrames;
            }

            return SelectFirstAvailable(
                _cachedIdleDownSprites,
                _cachedMoveDownSprites,
                _cachedIdleUpSprites,
                _cachedMoveUpSprites,
                _cachedIdleSideSprites,
                _cachedMoveSideSprites);
        }

        private static Sprite[] SelectDirectionalFrames(FacingDirection facing, Sprite[] down, Sprite[] up, Sprite[] side)
        {
            switch (facing)
            {
                case FacingDirection.Up:
                    return up != null && up.Length > 0 ? up : down;
                case FacingDirection.Side:
                    return side != null && side.Length > 0 ? side : down;
                default:
                    return down;
            }
        }

        private static Sprite[] SelectFirstAvailable(params Sprite[][] candidates)
        {
            for (int i = 0; i < candidates.Length; i++)
            {
                Sprite[] candidate = candidates[i];
                if (candidate != null && candidate.Length > 0)
                {
                    return candidate;
                }
            }

            return System.Array.Empty<Sprite>();
        }

        private void HandleTelegraphStarted(BossPatternType patternType)
        {
            _telegraphPoseActive = true;
            _attackPoseRemaining = Mathf.Max(_attackPoseRemaining, telegraphPoseMinimumDuration);
        }

        private void HandleTelegraphEnded()
        {
            _telegraphPoseActive = false;
        }

        private void HandleAttackEmphasized()
        {
            _attackPoseRemaining = Mathf.Max(_attackPoseRemaining, attackHoldDuration);
        }

        private void Subscribe()
        {
            if (_hasSubscribed)
            {
                return;
            }

            if (bossEnemyBrain != null)
            {
                bossEnemyBrain.TelegraphStarted += HandleTelegraphStarted;
                bossEnemyBrain.TelegraphEnded += HandleTelegraphEnded;
            }

            if (bossVisual != null)
            {
                bossVisual.AttackEmphasized += HandleAttackEmphasized;
            }

            _hasSubscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_hasSubscribed)
            {
                return;
            }

            if (bossEnemyBrain != null)
            {
                bossEnemyBrain.TelegraphStarted -= HandleTelegraphStarted;
                bossEnemyBrain.TelegraphEnded -= HandleTelegraphEnded;
            }

            if (bossVisual != null)
            {
                bossVisual.AttackEmphasized -= HandleAttackEmphasized;
            }

            _hasSubscribed = false;
        }

        private void ResolveReferences()
        {
            if (enemyController == null)
            {
                enemyController = GetComponent<EnemyController>();
            }

            if (bossEnemyBrain == null)
            {
                bossEnemyBrain = GetComponent<BossEnemyBrain>();
            }

            if (bossVisual == null)
            {
                bossVisual = GetComponent<BossVisual>();
            }

            if (bodySpriteRenderer == null)
            {
                EnemyVisual enemyVisual = bossVisual != null ? bossVisual.EnemyVisual : GetComponent<EnemyVisual>();
                bodySpriteRenderer = enemyVisual != null ? enemyVisual.BodySpriteRenderer : GetComponentInChildren<SpriteRenderer>(true);
            }
        }

        private void RebuildFrameCache()
        {
            _cachedIdleDownSprites = BuildFilteredFrames(idleDownSprites, nameof(idleDownSprites));
            _cachedIdleUpSprites = BuildFilteredFrames(idleUpSprites, nameof(idleUpSprites));
            _cachedIdleSideSprites = BuildFilteredFrames(idleSideSprites, nameof(idleSideSprites));
            _cachedMoveDownSprites = BuildFilteredFrames(moveDownSprites, nameof(moveDownSprites));
            _cachedMoveUpSprites = BuildFilteredFrames(moveUpSprites, nameof(moveUpSprites));
            _cachedMoveSideSprites = BuildFilteredFrames(moveSideSprites, nameof(moveSideSprites));
            _cachedAttackDownSprites = BuildFilteredFrames(attackDownSprites, nameof(attackDownSprites));
            _cachedAttackUpSprites = BuildFilteredFrames(attackUpSprites, nameof(attackUpSprites));
            _cachedAttackSideSprites = BuildFilteredFrames(attackSideSprites, nameof(attackSideSprites));
        }

        private Sprite[] BuildFilteredFrames(Sprite[] source, string fieldName)
        {
            if (source == null || source.Length == 0)
            {
                return System.Array.Empty<Sprite>();
            }

            if (!filterTinySlicedFrames)
            {
                return source;
            }

            int validCount = 0;
            for (int i = 0; i < source.Length; i++)
            {
                if (IsUsableFrame(source[i]))
                {
                    validCount++;
                }
            }

            if (validCount == source.Length)
            {
                return source;
            }

            if (validCount <= 0)
            {
                if (logFilteredFrames)
                {
                    Debug.LogWarning($"{nameof(BossSpriteAnimator)} filtered all frames from {fieldName}. Check sprite slicing on {name}.", this);
                }

                return System.Array.Empty<Sprite>();
            }

            Sprite[] filtered = new Sprite[validCount];
            int writeIndex = 0;
            for (int i = 0; i < source.Length; i++)
            {
                Sprite sprite = source[i];
                if (!IsUsableFrame(sprite))
                {
                    if (logFilteredFrames && sprite != null)
                    {
                        Rect rect = sprite.rect;
                        Debug.LogWarning($"{nameof(BossSpriteAnimator)} ignored tiny sliced frame '{sprite.name}' ({rect.width:0}x{rect.height:0}) in {fieldName}.", this);
                    }

                    continue;
                }

                filtered[writeIndex] = sprite;
                writeIndex++;
            }

            return filtered;
        }

        private bool IsUsableFrame(Sprite sprite)
        {
            if (sprite == null)
            {
                return false;
            }

            Rect rect = sprite.rect;
            return rect.width >= minimumFrameWidth && rect.height >= minimumFrameHeight;
        }

        private void Reset()
        {
            ResolveReferences();
            RebuildFrameCache();
        }

        private void OnValidate()
        {
            ResolveReferences();
            movingThreshold = Mathf.Max(0f, movingThreshold);
            idleFramesPerSecond = Mathf.Max(0.1f, idleFramesPerSecond);
            moveFramesPerSecond = Mathf.Max(0.1f, moveFramesPerSecond);
            attackFramesPerSecond = Mathf.Max(0.1f, attackFramesPerSecond);
            minimumFrameWidth = Mathf.Max(1f, minimumFrameWidth);
            minimumFrameHeight = Mathf.Max(1f, minimumFrameHeight);
            RebuildFrameCache();
        }
    }
}
