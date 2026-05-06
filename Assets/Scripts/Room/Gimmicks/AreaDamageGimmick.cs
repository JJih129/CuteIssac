using System.Collections.Generic;
using CuteIssac.Common.Combat;
using CuteIssac.Player;
using UnityEngine;
using UnityEngine.Events;

namespace CuteIssac.Room.Gimmicks
{
    /// <summary>
    /// One-shot radius damage helper for apple bombs and other explosion-like gimmicks.
    /// Uses Collider2D NonAlloc queries and reusable buffers to avoid runtime allocations.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class AreaDamageGimmick : CandyGimmickBase
    {
        [Header("Area Damage")]
        [SerializeField] [Min(0.1f)] private float explosionRadius = 1.75f;
        [SerializeField] [Min(0f)] private float damage = 1f;
        [SerializeField] [Min(0f)] private float knockbackForce = 6f;
        [SerializeField] private bool playerOnly = true;

        [Header("Detection")]
        [SerializeField] private LayerMask overlapMask = Physics2D.AllLayers;
        [SerializeField] [Min(8)] private int initialBufferSize = 16;

        [Header("Hooks")]
        [SerializeField] private UnityEvent areaDamageApplied;

        private readonly HashSet<int> _processedDamageables = new();
        private Collider2D[] _overlapBuffer;

        public float ExplosionRadius => explosionRadius;
        public float Damage => damage;
        public float KnockbackForce => knockbackForce;

        public void ConfigureAreaDamage(float radius, float damageAmount, float knockback, bool restrictToPlayerOnly)
        {
            explosionRadius = Mathf.Max(0.1f, radius);
            damage = Mathf.Max(0f, damageAmount);
            knockbackForce = Mathf.Max(0f, knockback);
            playerOnly = restrictToPlayerOnly;
        }

        [ContextMenu("Apply Area Damage")]
        public void ApplyAreaDamage()
        {
            if (!IsGimmickActive)
            {
                return;
            }

            EnsureBuffer();
            _processedDamageables.Clear();

            ContactFilter2D contactFilter = BuildContactFilter();
            Vector2 origin = transform.position;
            int hitCount = Physics2D.OverlapCircle(origin, explosionRadius, contactFilter, _overlapBuffer);

            while (hitCount >= _overlapBuffer.Length)
            {
                _overlapBuffer = new Collider2D[_overlapBuffer.Length * 2];
                hitCount = Physics2D.OverlapCircle(origin, explosionRadius, contactFilter, _overlapBuffer);
            }

            for (int i = 0; i < hitCount; i++)
            {
                TryDamage(_overlapBuffer[i], origin);
            }

            for (int i = 0; i < hitCount; i++)
            {
                _overlapBuffer[i] = null;
            }

            areaDamageApplied?.Invoke();
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            explosionRadius = Mathf.Max(0.1f, explosionRadius);
            damage = Mathf.Max(0f, damage);
            knockbackForce = Mathf.Max(0f, knockbackForce);
            initialBufferSize = Mathf.Max(8, initialBufferSize);
        }

        private void TryDamage(Collider2D hit, Vector2 origin)
        {
            if (hit == null)
            {
                return;
            }

            if (playerOnly && hit.GetComponentInParent<PlayerHealth>() == null)
            {
                return;
            }

            if (!DamageableResolver.TryResolve(hit, out IDamageable damageable))
            {
                return;
            }

            int targetId = ResolveDamageableId(hit, damageable);

            if (_processedDamageables.Contains(targetId))
            {
                return;
            }

            _processedDamageables.Add(targetId);
            Vector2 hitDirection = (Vector2)hit.bounds.center - origin;

            if (hitDirection.sqrMagnitude <= 0.0001f)
            {
                hitDirection = Vector2.up;
            }

            damageable.ApplyDamage(new DamageInfo(damage, hitDirection.normalized, transform, knockbackForce));
        }

        private ContactFilter2D BuildContactFilter()
        {
            ContactFilter2D contactFilter = new()
            {
                useLayerMask = true,
                useTriggers = true
            };
            contactFilter.SetLayerMask(overlapMask);
            return contactFilter;
        }

        private void EnsureBuffer()
        {
            int capacity = Mathf.Max(8, initialBufferSize);

            if (_overlapBuffer == null || _overlapBuffer.Length < capacity)
            {
                _overlapBuffer = new Collider2D[capacity];
            }
        }

        private static int ResolveDamageableId(Collider2D fallbackCollider, IDamageable damageable)
        {
            if (damageable is Object unityObject)
            {
                return unityObject.GetInstanceID();
            }

            return fallbackCollider != null ? fallbackCollider.GetInstanceID() : 0;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.36f, 0.12f, 0.86f);
            Gizmos.DrawWireSphere(transform.position, explosionRadius);
        }
    }
}
