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

        [Header("Outline")]
        [Tooltip("Adds a darker silhouette behind the projectile to improve readability on bright stage art.")]
        [SerializeField] private bool useRuntimeOutline = true;
        [SerializeField] private Color outlineColor = new(0.05f, 0.05f, 0.08f, 0.96f);
        [SerializeField] [Min(1f)] private float outlineScaleMultiplier = 1.28f;
        [SerializeField] private int outlineSortingOffset = -1;

        [Header("Pooling")]
        [Tooltip("One-time prewarm count for impact and destroy VFX used by this projectile view.")]
        [SerializeField] [Min(0)] private int effectPrewarmCount = 2;

        [Header("Trait Overlay")]
        [SerializeField] private bool useRuntimeTraitOverlay = true;
        [SerializeField] [Min(1f)] private float auraScaleMultiplier = 1.9f;
        [SerializeField] [Min(1f)] private float coreScaleMultiplier = 1.18f;
        [SerializeField] [Min(0.1f)] private float traitPulseSpeed = 7.5f;

        private bool _warnedMissingVisuals;
        private SpriteRenderer _outlineRenderer;
        private SpriteRenderer _traitAuraRenderer;
        private SpriteRenderer _traitCoreRenderer;
        private ProjectileTraitState _traits;
        private OpeningCadenceVolleyRole _openingCadenceRole;
        private float _openingCadenceRoleWeight;
        private float _spawnTime;
        private bool _hasCachedDefaultPresentation;
        private Color _baseSpriteColor = Color.white;
        private Color _baseTrailStartColor = Color.white;
        private Color _baseTrailEndColor = Color.white;
        private float _baseTrailStartWidth;
        private float _baseTrailEndWidth;

        private void Awake()
        {
            ResolveReferences();
            TryPrewarmEffects();
            CacheDefaultPresentation();
            SyncOutlineRenderer();
            PrepareTraitRenderers();
        }

        public void HandleInitialized(Vector2 direction)
        {
            HandleInitialized(direction, ProjectileTraitState.Default);
        }

        public void HandleInitialized(Vector2 direction, ProjectileTraitState traits)
        {
            HandleInitialized(direction, traits, OpeningCadenceVolleyRole.None, 0f);
        }

        public void HandleInitialized(Vector2 direction, ProjectileTraitState traits, OpeningCadenceVolleyRole openingCadenceRole, float openingCadenceRoleWeight)
        {
            TryPrewarmEffects();
            CacheDefaultPresentation();
            _traits = traits;
            _openingCadenceRole = openingCadenceRole;
            _openingCadenceRoleWeight = Mathf.Clamp01(openingCadenceRoleWeight);
            _spawnTime = Time.time;

            if (trailRenderer != null)
            {
                trailRenderer.Clear();
            }

            if (flipSpriteByDirection && spriteRenderer != null)
            {
                spriteRenderer.flipX = direction.x < 0f;
            }

            ApplyTraitPresentation();
            SyncOutlineRenderer();

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
            ResolveReferences();
        }

        private void OnValidate()
        {
            ResolveReferences();

            if (Application.isPlaying)
            {
                CacheDefaultPresentation();
                ApplyTraitPresentation();
                SyncOutlineRenderer();
            }
        }

        private void ResolveReferences()
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

        private void Update()
        {
            if (!useRuntimeTraitOverlay || !_HasVisibleTrait())
            {
                return;
            }

            UpdateTraitRenderers();
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
            if (effectPrefab == null)
            {
                return;
            }

            PrefabPoolService.EnsurePrewarmed(effectPrefab, effectPrewarmCount);
        }

        private void PrepareTraitRenderers()
        {
            if (!useRuntimeTraitOverlay || spriteRenderer == null || spriteRenderer.sprite == null)
            {
                return;
            }

            EnsureTraitRenderer(ref _traitAuraRenderer, "RuntimeTraitAura", -2);
            EnsureTraitRenderer(ref _traitCoreRenderer, "RuntimeTraitCore", -1);
            _traitAuraRenderer.enabled = false;
            _traitCoreRenderer.enabled = false;
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

        private void CacheDefaultPresentation()
        {
            if (_hasCachedDefaultPresentation)
            {
                return;
            }

            if (spriteRenderer != null)
            {
                _baseSpriteColor = spriteRenderer.color;
            }

            if (trailRenderer != null)
            {
                _baseTrailStartColor = trailRenderer.startColor;
                _baseTrailEndColor = trailRenderer.endColor;
                _baseTrailStartWidth = trailRenderer.startWidth;
                _baseTrailEndWidth = trailRenderer.endWidth;
            }

            _hasCachedDefaultPresentation = true;
        }

        private void ApplyTraitPresentation()
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.color = ResolveTintedSpriteColor();
            }

            if (trailRenderer != null)
            {
                Color accent = ResolveTraitAccent();
                float trailBoost = ResolveTrailBoost();
                trailRenderer.startColor = Color.Lerp(_baseTrailStartColor, accent, 0.45f);
                trailRenderer.endColor = Color.Lerp(_baseTrailEndColor, accent * new Color(1f, 1f, 1f, 0.65f), 0.52f);
                trailRenderer.startWidth = Mathf.Max(0.01f, _baseTrailStartWidth * trailBoost);
                trailRenderer.endWidth = Mathf.Max(0.005f, _baseTrailEndWidth * Mathf.Lerp(1f, trailBoost, 0.72f));
            }

            SyncTraitRenderers(forceRefresh: true);
        }

        private void UpdateTraitRenderers()
        {
            if (_traitAuraRenderer == null && _traitCoreRenderer == null)
            {
                return;
            }

            float pulse = 0.5f + (0.5f * Mathf.Sin((Time.time - _spawnTime) * traitPulseSpeed));
            Color accent = ResolveTraitAccent();
            float roleScaleBoost = ResolveOpeningCadenceRoleScaleBoost();
            float auraAlpha = Mathf.Lerp(0.14f, 0.3f, pulse);
            float coreAlpha = Mathf.Lerp(0.22f, 0.42f, 1f - pulse);
            float auraScale = Mathf.Lerp(auraScaleMultiplier * 0.92f, auraScaleMultiplier * 1.08f, pulse) * roleScaleBoost;
            float coreScale = Mathf.Lerp(coreScaleMultiplier * 0.94f, coreScaleMultiplier * 1.04f, 1f - pulse) * roleScaleBoost;

            if (_traitAuraRenderer != null)
            {
                _traitAuraRenderer.enabled = true;
                _traitAuraRenderer.color = new Color(accent.r, accent.g, accent.b, auraAlpha);
                _traitAuraRenderer.transform.localScale = new Vector3(auraScale, auraScale, 1f);
                _traitAuraRenderer.flipX = spriteRenderer != null && spriteRenderer.flipX;
                _traitAuraRenderer.flipY = spriteRenderer != null && spriteRenderer.flipY;
            }

            if (_traitCoreRenderer != null)
            {
                _traitCoreRenderer.enabled = true;
                _traitCoreRenderer.color = new Color(accent.r, accent.g, accent.b, coreAlpha);
                _traitCoreRenderer.transform.localScale = new Vector3(coreScale, coreScale, 1f);
                _traitCoreRenderer.flipX = spriteRenderer != null && spriteRenderer.flipX;
                _traitCoreRenderer.flipY = spriteRenderer != null && spriteRenderer.flipY;
            }
        }

        private void SyncTraitRenderers(bool forceRefresh = false)
        {
            if (!useRuntimeTraitOverlay || !_HasVisibleTrait() || spriteRenderer == null || spriteRenderer.sprite == null)
            {
                if (_traitAuraRenderer != null)
                {
                    _traitAuraRenderer.enabled = false;
                }

                if (_traitCoreRenderer != null)
                {
                    _traitCoreRenderer.enabled = false;
                }

                return;
            }

            EnsureTraitRenderer(ref _traitAuraRenderer, "RuntimeTraitAura", -2);
            EnsureTraitRenderer(ref _traitCoreRenderer, "RuntimeTraitCore", -1);

            if (forceRefresh)
            {
                _traitAuraRenderer.sprite = spriteRenderer.sprite;
                _traitAuraRenderer.sortingLayerID = spriteRenderer.sortingLayerID;
                _traitAuraRenderer.sortingOrder = spriteRenderer.sortingOrder - 2;
                _traitAuraRenderer.maskInteraction = spriteRenderer.maskInteraction;

                _traitCoreRenderer.sprite = spriteRenderer.sprite;
                _traitCoreRenderer.sortingLayerID = spriteRenderer.sortingLayerID;
                _traitCoreRenderer.sortingOrder = spriteRenderer.sortingOrder - 1;
                _traitCoreRenderer.maskInteraction = spriteRenderer.maskInteraction;
            }

            UpdateTraitRenderers();
        }

        private void EnsureTraitRenderer(ref SpriteRenderer renderer, string childName, int sortingOffset)
        {
            if (renderer != null)
            {
                return;
            }

            Transform childTransform = transform.Find(childName);
            if (childTransform == null)
            {
                GameObject childObject = new GameObject(childName);
                childTransform = childObject.transform;
                childTransform.SetParent(transform, false);
            }

            renderer = childTransform.GetComponent<SpriteRenderer>();
            if (renderer == null)
            {
                renderer = childTransform.gameObject.AddComponent<SpriteRenderer>();
            }

            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.allowOcclusionWhenDynamic = false;
            renderer.sortingLayerID = spriteRenderer != null ? spriteRenderer.sortingLayerID : renderer.sortingLayerID;
            renderer.sortingOrder = (spriteRenderer != null ? spriteRenderer.sortingOrder : 0) + sortingOffset;
        }

        private Color ResolveTintedSpriteColor()
        {
            if (!_HasVisibleTrait())
            {
                return _baseSpriteColor;
            }

            return Color.Lerp(_baseSpriteColor, ResolveTraitAccent(), Mathf.Lerp(0.2f, 0.32f, _openingCadenceRoleWeight));
        }

        private float ResolveTrailBoost()
        {
            float boost = 1f;

            if (_traits.IsLaser)
            {
                boost = Mathf.Max(boost, 1.35f);
            }

            if (_traits.IsShielded)
            {
                boost = Mathf.Max(boost, 1.18f);
            }

            if (_traits.IsOrbiting)
            {
                boost = Mathf.Max(boost, 1.12f);
            }

            switch (_openingCadenceRole)
            {
                case OpeningCadenceVolleyRole.Core:
                    boost = Mathf.Max(boost, Mathf.Lerp(1f, 1.12f, _openingCadenceRoleWeight));
                    break;
                case OpeningCadenceVolleyRole.Flank:
                    boost = Mathf.Max(boost, Mathf.Lerp(1f, 1.18f, _openingCadenceRoleWeight));
                    break;
                case OpeningCadenceVolleyRole.Edge:
                    boost = Mathf.Max(boost, Mathf.Lerp(1f, 1.24f, _openingCadenceRoleWeight));
                    break;
            }

            return boost;
        }

        private Color ResolveTraitAccent()
        {
            Color traitAccent = _baseSpriteColor;
            if (_traits.IsOrbiting)
            {
                traitAccent = new Color(1f, 0.92f, 0.42f, 1f);
            }
            else if (_traits.IsShielded)
            {
                traitAccent = new Color(0.42f, 0.92f, 1f, 1f);
            }
            else if (_traits.IsLaser)
            {
                traitAccent = new Color(1f, 0.34f, 0.34f, 1f);
            }
            else if (_traits.IsExplosive)
            {
                traitAccent = new Color(1f, 0.62f, 0.26f, 1f);
            }
            else if (_traits.Has(ProjectileTraitFlags.Lifesteal))
            {
                traitAccent = new Color(0.48f, 1f, 0.62f, 1f);
            }
            else if (_traits.IsSplit)
            {
                traitAccent = new Color(1f, 0.72f, 0.98f, 1f);
            }
            else if (_traits.Has(ProjectileTraitFlags.Bounce))
            {
                traitAccent = new Color(0.95f, 0.95f, 1f, 1f);
            }

            return Color.Lerp(traitAccent, ResolveOpeningCadenceRoleAccent(), Mathf.Lerp(0f, 0.34f, _openingCadenceRoleWeight));
        }

        private Color ResolveOpeningCadenceRoleAccent()
        {
            return _openingCadenceRole switch
            {
                OpeningCadenceVolleyRole.Core => new Color(1f, 0.96f, 0.72f, 1f),
                OpeningCadenceVolleyRole.Flank => new Color(1f, 0.76f, 0.94f, 1f),
                OpeningCadenceVolleyRole.Edge => new Color(0.72f, 0.9f, 1f, 1f),
                _ => _baseSpriteColor
            };
        }

        private float ResolveOpeningCadenceRoleScaleBoost()
        {
            return _openingCadenceRole switch
            {
                OpeningCadenceVolleyRole.Core => Mathf.Lerp(1f, 1.08f, _openingCadenceRoleWeight),
                OpeningCadenceVolleyRole.Flank => Mathf.Lerp(1f, 1.12f, _openingCadenceRoleWeight),
                OpeningCadenceVolleyRole.Edge => Mathf.Lerp(1f, 1.16f, _openingCadenceRoleWeight),
                _ => 1f
            };
        }

        private bool _HasVisibleTrait()
        {
            return _traits.IsExplosive
                || _traits.IsLaser
                || _traits.IsSplit
                || _traits.IsShielded
                || _traits.IsOrbiting
                || _traits.Has(ProjectileTraitFlags.Bounce)
                || _traits.Has(ProjectileTraitFlags.Lifesteal)
                || (_openingCadenceRole != OpeningCadenceVolleyRole.None && _openingCadenceRoleWeight > 0.01f);
        }

        private void SyncOutlineRenderer()
        {
            if (!useRuntimeOutline || spriteRenderer == null || spriteRenderer.sprite == null)
            {
                if (_outlineRenderer != null)
                {
                    _outlineRenderer.enabled = false;
                }

                return;
            }

            EnsureOutlineRenderer();

            Transform outlineTransform = _outlineRenderer.transform;
            outlineTransform.localPosition = Vector3.zero;
            outlineTransform.localRotation = Quaternion.identity;
            outlineTransform.localScale = new Vector3(outlineScaleMultiplier, outlineScaleMultiplier, 1f);

            _outlineRenderer.enabled = spriteRenderer.enabled;
            _outlineRenderer.sprite = spriteRenderer.sprite;
            _outlineRenderer.color = outlineColor;
            _outlineRenderer.flipX = spriteRenderer.flipX;
            _outlineRenderer.flipY = spriteRenderer.flipY;
            _outlineRenderer.drawMode = spriteRenderer.drawMode;
            _outlineRenderer.size = spriteRenderer.size;
            _outlineRenderer.maskInteraction = spriteRenderer.maskInteraction;
            _outlineRenderer.sortingLayerID = spriteRenderer.sortingLayerID;
            _outlineRenderer.sortingOrder = spriteRenderer.sortingOrder + outlineSortingOffset;
        }

        private void EnsureOutlineRenderer()
        {
            if (_outlineRenderer != null)
            {
                return;
            }

            Transform outlineTransform = transform.Find("RuntimeOutline");

            if (outlineTransform == null)
            {
                GameObject outlineObject = new("RuntimeOutline");
                outlineTransform = outlineObject.transform;
                outlineTransform.SetParent(transform, false);
            }

            _outlineRenderer = outlineTransform.GetComponent<SpriteRenderer>();

            if (_outlineRenderer == null)
            {
                _outlineRenderer = outlineTransform.gameObject.AddComponent<SpriteRenderer>();
            }

            _outlineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _outlineRenderer.receiveShadows = false;
            _outlineRenderer.allowOcclusionWhenDynamic = false;
        }
    }
}
