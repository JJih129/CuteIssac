using UnityEngine;

namespace CuteIssac.Enemy
{
    /// <summary>
    /// Drives Pudding Jellyfish idle/move/attack sprites and releases the projectile on the authored attack frame.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(EnemyController))]
    [DefaultExecutionOrder(120)]
    public sealed class PuddingJellyfishSpriteAnimator : MonoBehaviour, IEnemyRangedAttackPresentation
    {
        [Header("References")]
        [SerializeField] private EnemyController enemyController;
        [SerializeField] private EnemyVisual enemyVisual;
        [SerializeField] private SpriteRenderer bodySpriteRenderer;

        [Header("Sprites")]
        [SerializeField] private Sprite idleSprite;
        [SerializeField] private Sprite moveSprite;
        [SerializeField] private Sprite attackFrame1;
        [SerializeField] private Sprite attackFrame2;
        [SerializeField] private Sprite attackFrame3;
        [SerializeField] private Sprite attackFrame4;

        [Header("Timing")]
        [SerializeField] [Min(0.02f)] private float attackFrameInterval = 0.09f;
        [SerializeField] [Min(0f)] private float attackEndHold = 0.08f;
        [SerializeField] [Min(0f)] private float movingThreshold = 0.02f;
        [SerializeField] [Range(0, 3)] private int projectileFireFrameIndex = 2;

        private Vector2 _queuedAimDirection = Vector2.right;
        private EnemyCombat _queuedEnemyCombat;
        private float _attackTimer;
        private int _attackFrameIndex;
        private bool _attackActive;
        private bool _projectileFired;
        private Sprite _lastAppliedSprite;

        public bool IsAttackPresentationActive => _attackActive;

        private void Awake()
        {
            ResolveReferences();
        }

        private void OnEnable()
        {
            ResolveReferences();
            ResetAttackState();
            ApplyLocomotionSprite(force: true);
        }

        private void OnDisable()
        {
            ResetAttackState();
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

            if (_attackActive)
            {
                TickAttackAnimation();
                return;
            }

            ApplyLocomotionSprite(force: false);
        }

        private void OnValidate()
        {
            ResolveReferences();
            projectileFireFrameIndex = Mathf.Clamp(projectileFireFrameIndex, 0, 3);
        }

        public bool TryPlayAttack(Vector2 aimDirection, EnemyCombat enemyCombat)
        {
            if (_attackActive || enemyCombat == null || !enemyCombat.CanFire)
            {
                return false;
            }

            _queuedAimDirection = aimDirection.sqrMagnitude > 0.0001f ? aimDirection.normalized : Vector2.right;
            _queuedEnemyCombat = enemyCombat;
            _attackTimer = 0f;
            _attackFrameIndex = 0;
            _projectileFired = false;
            _attackActive = true;
            ApplyAttackFrame();
            return true;
        }

        private void TickAttackAnimation()
        {
            _attackTimer += Time.deltaTime;

            if (_attackFrameIndex < 3)
            {
                while (_attackTimer >= attackFrameInterval && _attackFrameIndex < 3)
                {
                    _attackTimer -= attackFrameInterval;
                    _attackFrameIndex++;
                    ApplyAttackFrame();
                }
            }
            else if (_attackTimer >= attackFrameInterval + attackEndHold)
            {
                ResetAttackState();
                ApplyLocomotionSprite(force: true);
            }
        }

        private void ApplyAttackFrame()
        {
            Sprite nextSprite = _attackFrameIndex switch
            {
                0 => attackFrame1,
                1 => attackFrame2,
                2 => attackFrame3,
                _ => attackFrame4
            };

            ApplySprite(nextSprite != null ? nextSprite : ResolveFallbackSprite());

            if (!_projectileFired && _attackFrameIndex >= projectileFireFrameIndex)
            {
                _projectileFired = true;
                _queuedEnemyCombat?.Fire(_queuedAimDirection);
                enemyVisual?.HandleAttack();
            }
        }

        private void ApplyLocomotionSprite(bool force)
        {
            Vector2 moveDirection = enemyController != null && enemyController.EnemyMovement != null
                ? enemyController.EnemyMovement.CurrentMoveDirection
                : Vector2.zero;

            Sprite nextSprite = moveDirection.sqrMagnitude > movingThreshold * movingThreshold
                ? moveSprite != null ? moveSprite : idleSprite
                : idleSprite != null ? idleSprite : moveSprite;

            ApplySprite(nextSprite, force);
        }

        private void ApplySprite(Sprite nextSprite, bool force = false)
        {
            if (bodySpriteRenderer == null || nextSprite == null)
            {
                return;
            }

            if (force || _lastAppliedSprite != nextSprite)
            {
                bodySpriteRenderer.sprite = nextSprite;
                _lastAppliedSprite = nextSprite;
            }
        }

        private Sprite ResolveFallbackSprite()
        {
            if (idleSprite != null)
            {
                return idleSprite;
            }

            if (moveSprite != null)
            {
                return moveSprite;
            }

            if (attackFrame1 != null)
            {
                return attackFrame1;
            }

            return bodySpriteRenderer != null ? bodySpriteRenderer.sprite : null;
        }

        private void ResetAttackState()
        {
            _attackActive = false;
            _projectileFired = false;
            _queuedEnemyCombat = null;
            _attackTimer = 0f;
            _attackFrameIndex = 0;
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

            if (bodySpriteRenderer == null)
            {
                bodySpriteRenderer = enemyVisual != null && enemyVisual.BodySpriteRenderer != null
                    ? enemyVisual.BodySpriteRenderer
                    : GetComponentInChildren<SpriteRenderer>(true);
            }
        }
    }
}
