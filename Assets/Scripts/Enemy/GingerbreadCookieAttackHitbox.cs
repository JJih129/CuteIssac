using System.Collections.Generic;
using CuteIssac.Common.Combat;
using UnityEngine;

namespace CuteIssac.Enemy
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class GingerbreadCookieAttackHitbox : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private EnemyController enemyController;
        [SerializeField] private SpriteRenderer bodySpriteRenderer;
        [SerializeField] private BoxCollider2D hitboxCollider;

        [Header("Damage")]
        [SerializeField] [Min(0f)] private float damage = 1f;
        [SerializeField] [Min(0f)] private float knockbackForce = 1.6f;
        [SerializeField] private LayerMask targetMask = Physics2D.AllLayers;

        [Header("Shape")]
        [SerializeField] [Min(0f)] private float forwardExtension = 0.35f;
        [SerializeField] [Min(0f)] private float forwardOffset = 0.18f;

        private readonly HashSet<int> _damagedTargets = new();
        private readonly Collider2D[] _overlapBuffer = new Collider2D[12];
        private ContactFilter2D _contactFilter;
        private float _remainingActiveTime;
        private bool _isActive;

        public bool IsActive => _isActive;

        private void Awake()
        {
            ResolveReferences();
            BuildContactFilter();
            SetActiveState(false);
        }

        private void OnEnable()
        {
            ResolveReferences();
            BuildContactFilter();
            SetActiveState(false);
        }

        private void FixedUpdate()
        {
            if (!_isActive)
            {
                return;
            }

            _remainingActiveTime -= Time.fixedDeltaTime;

            if (_remainingActiveTime <= 0f)
            {
                SetActiveState(false);
                return;
            }

            ApplyOverlapDamage();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            TryDamage(other);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            TryDamage(other);
        }

        public void Activate(float activeDuration, float attackDamage, Vector2 attackDirection)
        {
            ResolveReferences();
            FitColliderToCurrentSprite(attackDirection);

            damage = Mathf.Max(0f, attackDamage);
            _remainingActiveTime = Mathf.Max(0f, activeDuration);
            _damagedTargets.Clear();

            SetActiveState(_remainingActiveTime > 0f && damage > 0f);

            if (_isActive)
            {
                ApplyOverlapDamage();
            }
        }

        public void Deactivate()
        {
            _remainingActiveTime = 0f;
            SetActiveState(false);
        }

        public void FitColliderToCurrentSprite()
        {
            Vector2 direction = enemyController != null
                ? enemyController.TargetPosition - enemyController.Position
                : Vector2.zero;

            FitColliderToCurrentSprite(direction);
        }

        public void FitColliderToCurrentSprite(Vector2 attackDirection)
        {
            ResolveReferences();

            if (hitboxCollider == null || bodySpriteRenderer == null || bodySpriteRenderer.sprite == null)
            {
                return;
            }

            Bounds bounds = bodySpriteRenderer.bounds;
            Vector2 localCenter = transform.InverseTransformPoint(bounds.center);
            Vector3 lossyScale = transform.lossyScale;
            float widthScale = Mathf.Abs(lossyScale.x) > Mathf.Epsilon ? Mathf.Abs(lossyScale.x) : 1f;
            float heightScale = Mathf.Abs(lossyScale.y) > Mathf.Epsilon ? Mathf.Abs(lossyScale.y) : 1f;
            Vector2 resolvedDirection = attackDirection.sqrMagnitude > 0.0001f
                ? attackDirection.normalized
                : Vector2.down;
            Vector2 localDirection = transform.InverseTransformVector(resolvedDirection).normalized;
            Vector2 extraSize = new(
                Mathf.Abs(localDirection.x) * forwardExtension,
                Mathf.Abs(localDirection.y) * forwardExtension);

            hitboxCollider.offset = localCenter + (localDirection * (forwardOffset + forwardExtension * 0.5f));
            hitboxCollider.size = new Vector2(
                Mathf.Max(0.01f, (bounds.size.x / widthScale) + extraSize.x),
                Mathf.Max(0.01f, (bounds.size.y / heightScale) + extraSize.y));
        }

        private void ApplyOverlapDamage()
        {
            if (hitboxCollider == null)
            {
                return;
            }

            int hitCount = hitboxCollider.Overlap(_contactFilter, _overlapBuffer);

            for (int i = 0; i < hitCount; i++)
            {
                TryDamage(_overlapBuffer[i]);
                _overlapBuffer[i] = null;
            }
        }

        private void TryDamage(Collider2D hitCollider)
        {
            if (!_isActive || hitCollider == null || enemyController == null || enemyController.EnemyHealth == null)
            {
                return;
            }

            if (enemyController.EnemyHealth.IsDead || enemyController.CurrentTarget == null)
            {
                return;
            }

            Transform target = enemyController.CurrentTarget;

            if (hitCollider.transform != target && !hitCollider.transform.IsChildOf(target))
            {
                return;
            }

            if (!DamageableResolver.TryResolveTarget(hitCollider, out DamageableResolver.ResolvedTarget resolvedTarget)
                || !resolvedTarget.IsPlayer)
            {
                return;
            }

            if (!_damagedTargets.Add(resolvedTarget.TargetId))
            {
                return;
            }

            Vector2 hitDirection = ((Vector2)hitCollider.bounds.center - (Vector2)transform.position).normalized;

            if (hitDirection.sqrMagnitude <= 0.0001f)
            {
                hitDirection = Vector2.down;
            }

            resolvedTarget.Damageable.ApplyDamage(new DamageInfo(damage, hitDirection, transform, knockbackForce));
        }

        private void SetActiveState(bool active)
        {
            _isActive = active;

            if (hitboxCollider != null)
            {
                hitboxCollider.enabled = active;
                hitboxCollider.isTrigger = true;
            }
        }

        private void ResolveReferences()
        {
            if (enemyController == null)
            {
                enemyController = GetComponentInParent<EnemyController>();
            }

            if (hitboxCollider == null)
            {
                hitboxCollider = GetComponent<BoxCollider2D>();
            }

            if (bodySpriteRenderer == null && enemyController != null && enemyController.EnemyVisual != null)
            {
                bodySpriteRenderer = enemyController.EnemyVisual.BodySpriteRenderer;
            }
        }

        private void BuildContactFilter()
        {
            _contactFilter = new ContactFilter2D
            {
                useLayerMask = true,
                useTriggers = true
            };
            _contactFilter.SetLayerMask(targetMask);
        }

        private void OnValidate()
        {
            ResolveReferences();
            BuildContactFilter();

            if (hitboxCollider != null)
            {
                hitboxCollider.isTrigger = true;
            }
        }
    }
}
