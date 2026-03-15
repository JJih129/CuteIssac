using System.Collections.Generic;
using UnityEngine;
using CuteIssac.Core.Pooling;

namespace CuteIssac.Combat
{
    /// <summary>
    /// Presentation-only projectile view.
    /// Designers can swap sprite, trail, hit effect, and destroy effect references without changing gameplay logic.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ProjectileVisual : MonoBehaviour
    {
        private static readonly HashSet<GameObject> PrewarmedEffectPrefabs = new();

        [Header("Visual References")]
        [Tooltip("Optional sprite renderer for the projectile body.")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [Tooltip("Optional trail renderer used while the projectile is flying.")]
        [SerializeField] private TrailRenderer trailRenderer;

        [Header("Effect Prefabs")]
        [Tooltip("Optional impact effect spawned when the projectile hits a damageable target such as an enemy.")]
        [SerializeField] private GameObject damageableHitEffectPrefab;
        [Tooltip("Optional impact effect spawned when the projectile hits a solid collider such as a wall.")]
        [SerializeField] private GameObject solidHitEffectPrefab;
        [Tooltip("Optional effect spawned when the projectile disappears for any reason.")]
        [SerializeField] private GameObject destroyEffectPrefab;

        [Header("Optional Anchors")]
        [Tooltip("Optional anchor used as the default hit effect spawn point. Falls back to this transform when empty.")]
        [SerializeField] private Transform hitEffectAnchor;
        [Tooltip("Optional anchor used as the default destroy effect spawn point. Falls back to this transform when empty.")]
        [SerializeField] private Transform destroyEffectAnchor;

        [Header("Behavior")]
        [Tooltip("When enabled, the sprite flips based on travel direction. Leave disabled for rotation-driven sprites.")]
        [SerializeField] private bool flipSpriteByDirection;

        [Header("Pooling")]
        [Tooltip("One-time prewarm count for impact and destroy VFX used by this projectile view.")]
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
            HandleImpact(impactType, effectPosition);
            SpawnEffect(destroyEffectPrefab, destroyEffectAnchor, effectPosition);
        }

        public void HandleImpact(ProjectileImpactType impactType, Vector3 effectPosition)
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
                Debug.LogWarning("ProjectileVisual has no sprite, trail, or effect references assigned. The projectile still works, but it will be almost invisible.", this);
                _warnedMissingVisuals = true;
            }
        }
    }
}
