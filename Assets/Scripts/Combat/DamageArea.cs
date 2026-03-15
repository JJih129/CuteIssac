using System.Collections.Generic;
using CuteIssac.Common.Combat;
using UnityEngine;

namespace CuteIssac.Combat
{
    /// <summary>
    /// Applies an area hit once and notifies damageables and bomb-reactive targets.
    /// This keeps overlap logic out of BombController so other explosion-like gameplay can reuse it.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DamageArea : MonoBehaviour
    {
        [Header("Detection")]
        [SerializeField] private LayerMask overlapMask = Physics2D.AllLayers;
        [SerializeField] [Min(8)] private int initialBufferSize = 24;

        private readonly HashSet<int> _processedDamageables = new HashSet<int>();
        private readonly HashSet<int> _processedReactives = new HashSet<int>();
        private Collider2D[] _overlapBuffer;

        public void ApplyExplosion(in BombExplosionInfo explosionInfo, Collider2D ignoredCollider = null)
        {
            EnsureBuffer();
            _processedDamageables.Clear();
            _processedReactives.Clear();

            int hitCount = CollectHits(explosionInfo.Position, explosionInfo.Radius);

            for (int hitIndex = 0; hitIndex < hitCount; hitIndex++)
            {
                Collider2D hit = _overlapBuffer[hitIndex];

                if (hit == null || hit == ignoredCollider)
                {
                    continue;
                }

                TryDamage(hit, in explosionInfo);
                TryNotifyReactive(hit, in explosionInfo);
            }

            for (int clearIndex = 0; clearIndex < hitCount; clearIndex++)
            {
                _overlapBuffer[clearIndex] = null;
            }
        }

        private int CollectHits(Vector2 position, float radius)
        {
            ContactFilter2D contactFilter = BuildContactFilter();
            int hitCount = Physics2D.OverlapCircle(position, radius, contactFilter, _overlapBuffer);

            while (hitCount >= _overlapBuffer.Length)
            {
                _overlapBuffer = new Collider2D[_overlapBuffer.Length * 2];
                hitCount = Physics2D.OverlapCircle(position, radius, contactFilter, _overlapBuffer);
            }

            return hitCount;
        }

        private ContactFilter2D BuildContactFilter()
        {
            ContactFilter2D contactFilter = new ContactFilter2D
            {
                useLayerMask = true,
                useTriggers = true
            };
            contactFilter.SetLayerMask(overlapMask);
            return contactFilter;
        }

        private void TryDamage(Collider2D hit, in BombExplosionInfo explosionInfo)
        {
            if (!DamageableResolver.TryResolve(hit, out IDamageable damageable))
            {
                return;
            }

            int targetId = ResolveObjectId(hit, damageable);

            if (_processedDamageables.Contains(targetId))
            {
                return;
            }

            _processedDamageables.Add(targetId);

            Vector2 hitDirection = (Vector2)hit.bounds.center - explosionInfo.Position;

            if (hitDirection.sqrMagnitude <= 0.0001f)
            {
                hitDirection = Vector2.up;
            }

            damageable.ApplyDamage(new DamageInfo(
                explosionInfo.Damage,
                hitDirection.normalized,
                explosionInfo.Source,
                explosionInfo.KnockbackForce));
        }

        private void TryNotifyReactive(Collider2D hit, in BombExplosionInfo explosionInfo)
        {
            IBombReactive reactive = hit.GetComponentInParent<IBombReactive>();

            if (reactive == null)
            {
                return;
            }

            int targetId = ResolveObjectId(hit, reactive);

            if (_processedReactives.Contains(targetId))
            {
                return;
            }

            _processedReactives.Add(targetId);
            reactive.ReactToBomb(in explosionInfo);
        }

        private void EnsureBuffer()
        {
            int capacity = Mathf.Max(8, initialBufferSize);

            if (_overlapBuffer == null || _overlapBuffer.Length < capacity)
            {
                _overlapBuffer = new Collider2D[capacity];
            }
        }

        private static int ResolveObjectId(Collider2D fallbackCollider, object target)
        {
            if (target is Object unityObject)
            {
                return unityObject.GetInstanceID();
            }

            return fallbackCollider != null ? fallbackCollider.GetInstanceID() : 0;
        }

        private void OnValidate()
        {
            initialBufferSize = Mathf.Max(8, initialBufferSize);
        }
    }
}
