using System.Collections.Generic;
using UnityEngine;
using CuteIssac.Core.Pooling;

namespace CuteIssac.Combat
{
    /// <summary>
    /// Presentation-only enemy projectile view.
    /// Swappable sprite, trail, hit, and destroy effects live here.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EnemyProjectileVisual : MonoBehaviour
    {
        private static readonly HashSet<GameObject> PrewarmedEffectPrefabs = new();

        [Header("Visual References")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private TrailRenderer trailRenderer;

        [Header("Effect Prefabs")]
        [SerializeField] private GameObject damageableHitEffectPrefab;
        [SerializeField] private GameObject solidHitEffectPrefab;
        [SerializeField] private GameObject destroyEffectPrefab;

        [Header("Optional Anchors")]
        [SerializeField] private Transform hitEffectAnchor;
        [SerializeField] private Transform destroyEffectAnchor;

        [Header("Behavior")]
        [SerializeField] private bool flipSpriteByDirection;

        [Header("Pooling")]
        [Tooltip("One-time prewarm count for enemy projectile hit and destroy VFX.")]
        [SerializeField] [Min(0)] private int effectPrewarmCount = 2;

        private bool _warnedMissingVisuals;

        public void HandleInitialized(Vector2 direction)
        {
            TryPrewarmEffects();

            if (trailRenderer != null)
            {
                trailRenderer.Clear();
            }

            if (flipSpriteByDirection && spriteRenderer != null)
            {
                spriteRenderer.flipX = direction.x < 0f;
            }

            WarnIfFullyUnassigned();
        }

        public void HandleDespawn(ProjectileImpactType impactType, Vector3 effectPosition)
        {
            GameObject impactEffectPrefab = impactType switch
            {
                ProjectileImpactType.Damageable => damageableHitEffectPrefab,
                ProjectileImpactType.Solid => solidHitEffectPrefab,
                _ => null
            };

            if (impactEffectPrefab != null)
            {
                SpawnEffect(impactEffectPrefab, hitEffectAnchor, effectPosition);
            }

            SpawnEffect(destroyEffectPrefab, destroyEffectAnchor, effectPosition);
        }

        private void SpawnEffect(GameObject effectPrefab, Transform anchor, Vector3 fallbackPosition)
        {
            if (effectPrefab == null)
            {
                return;
            }

            Vector3 spawnPosition = anchor != null ? anchor.position : fallbackPosition;
            Quaternion spawnRotation = anchor != null ? anchor.rotation : Quaternion.identity;
            PrefabPoolService.Spawn(effectPrefab, spawnPosition, spawnRotation);
        }

        private void TryPrewarmEffects()
        {
            if (effectPrewarmCount <= 0)
            {
                return;
            }

            TryPrewarmEffect(damageableHitEffectPrefab);
            TryPrewarmEffect(solidHitEffectPrefab);
            TryPrewarmEffect(destroyEffectPrefab);
        }

        private void TryPrewarmEffect(GameObject effectPrefab)
        {
            if (effectPrefab == null || !PrewarmedEffectPrefabs.Add(effectPrefab))
            {
                return;
            }

            PrefabPoolService.Prewarm(effectPrefab, effectPrewarmCount);
        }

        private void WarnIfFullyUnassigned()
        {
            if (_warnedMissingVisuals)
            {
                return;
            }

            if (spriteRenderer == null &&
                trailRenderer == null &&
                damageableHitEffectPrefab == null &&
                solidHitEffectPrefab == null &&
                destroyEffectPrefab == null)
            {
                Debug.LogWarning("EnemyProjectileVisual has no sprite, trail, or effect references assigned.", this);
                _warnedMissingVisuals = true;
            }
        }

        private void Reset()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            trailRenderer = GetComponent<TrailRenderer>();
        }

        private void OnValidate()
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }

            if (trailRenderer == null)
            {
                trailRenderer = GetComponent<TrailRenderer>();
            }
        }
    }
}
