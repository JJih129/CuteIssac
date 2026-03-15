using CuteIssac.Core.Pooling;
using UnityEngine;

namespace CuteIssac.Combat
{
    /// <summary>
    /// Pooled one-shot explosion feedback. Designers can swap the sprite and timing without touching bomb logic.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ExplosionEffect : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Transform scaleRoot;

        [Header("Timing")]
        [SerializeField] [Min(0.05f)] private float lifetime = 0.32f;
        [SerializeField] [Min(0f)] private float startScale = 0.4f;
        [SerializeField] [Min(0f)] private float endScale = 1.85f;
        [SerializeField] private AnimationCurve alphaOverLifetime = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

        private Color _baseColor = Color.white;
        private Vector3 _baseScale = Vector3.one;
        private float _remainingLifetime;
        private float _configuredRadius = 1f;
        private bool _cachedDefaults;

        private void Awake()
        {
            CacheReferences();
        }

        private void OnEnable()
        {
            CacheReferences();
            _configuredRadius = 1f;
            _remainingLifetime = Mathf.Max(0.05f, lifetime);
            ApplyVisual(0f);
        }

        private void Update()
        {
            _remainingLifetime -= Time.deltaTime;
            float progress = 1f - Mathf.Clamp01(_remainingLifetime / Mathf.Max(0.05f, lifetime));
            ApplyVisual(progress);

            if (_remainingLifetime <= 0f)
            {
                PrefabPoolService.Return(gameObject);
            }
        }

        public void Configure(float worldRadius)
        {
            _configuredRadius = Mathf.Max(0.25f, worldRadius);
            ApplyVisual(0f);
        }

        private void ApplyVisual(float progress)
        {
            if (scaleRoot != null)
            {
                float scale = Mathf.Lerp(startScale, endScale, Mathf.Clamp01(progress)) * _configuredRadius;
                scaleRoot.localScale = _baseScale * scale;
            }

            if (spriteRenderer != null)
            {
                Color color = _baseColor;
                color.a = _baseColor.a * Mathf.Clamp01(alphaOverLifetime.Evaluate(Mathf.Clamp01(progress)));
                spriteRenderer.color = color;
            }
        }

        private void CacheReferences()
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }

            if (scaleRoot == null)
            {
                scaleRoot = transform;
            }

            if (_cachedDefaults)
            {
                return;
            }

            if (spriteRenderer != null)
            {
                _baseColor = spriteRenderer.color;
            }

            if (scaleRoot != null)
            {
                _baseScale = scaleRoot.localScale;
            }

            _cachedDefaults = true;
        }

        private void Reset()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            scaleRoot = transform;
        }

        private void OnValidate()
        {
            _cachedDefaults = false;
            CacheReferences();
        }
    }
}
