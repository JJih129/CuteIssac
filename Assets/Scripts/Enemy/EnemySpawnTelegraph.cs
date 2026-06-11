using System.Collections.Generic;
using UnityEngine;

namespace CuteIssac.Enemy
{
    /// <summary>
    /// Briefly hides a spawned enemy and shows a runtime telegraph marker before combat aggro begins.
    /// This keeps room registration immediate while giving the player a readable spawn warning window.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EnemySpawnTelegraph : MonoBehaviour
    {
        [SerializeField] [Min(0.05f)] private float revealDelay = 0.4f;
        [SerializeField] private Color telegraphColor = new(1f, 0.56f, 0.26f, 1f);
        [SerializeField] [Min(0.25f)] private float telegraphScale = 1.12f;
        [SerializeField] [Range(0.05f, 1f)] private float telegraphOpacity = 0.58f;
        [SerializeField] [Min(0.1f)] private float pulseSpeed = 5.4f;
        [SerializeField] private Vector2 localOffset = new(0f, -0.28f);

        private static Sprite s_WhiteSprite;
        private static Sprite s_CircleSprite;

        private readonly List<Renderer> _renderers = new();
        private readonly List<bool> _rendererEnabledStates = new();
        private readonly List<Collider2D> _colliders = new();
        private readonly List<bool> _colliderEnabledStates = new();

        private Transform _telegraphRoot;
        private SpriteRenderer _outerRingRenderer;
        private SpriteRenderer _innerPulseRenderer;
        private SpriteRenderer _crossbarHorizontalRenderer;
        private SpriteRenderer _crossbarVerticalRenderer;
        private float _remaining;
        private float _duration;
        private float _phaseOffset;
        private float _accentScaleMultiplier = 1f;
        private float _accentOpacityMultiplier = 1f;
        private bool _presentationHidden;
        private bool _isConfigured;

        private void Awake()
        {
            _phaseOffset = Random.Range(0f, Mathf.PI * 2f);
        }

        private void Update()
        {
            if (!_isConfigured)
            {
                return;
            }

            _remaining = Mathf.Max(0f, _remaining - Time.deltaTime);
            UpdateTelegraphVisual();

            if (_remaining <= 0f)
            {
                RevealNow();
            }
        }

        private void OnDisable()
        {
            CleanupImmediate();
        }

        private void OnDestroy()
        {
            CleanupImmediate();
        }

        public void Configure(
            float revealDelaySeconds,
            Color accentColor,
            float scale,
            float opacity,
            float pulseSpeedValue,
            Vector2 groundOffset)
        {
            CleanupImmediate();

            revealDelay = Mathf.Max(0.05f, revealDelaySeconds);
            telegraphColor = accentColor;
            telegraphScale = Mathf.Max(0.25f, scale);
            telegraphOpacity = Mathf.Clamp01(opacity);
            pulseSpeed = Mathf.Max(0.1f, pulseSpeedValue);
            localOffset = groundOffset;
            _duration = revealDelay;
            _remaining = revealDelay;
            _phaseOffset = Random.Range(0f, Mathf.PI * 2f);
            _accentScaleMultiplier = 1f;
            _accentOpacityMultiplier = 1f;

            CachePresentationTargets();
            BuildTelegraphVisual();
            HidePresentation();
            _isConfigured = true;
            enabled = true;
        }

        public void ApplyAccent(Color accentColor, float scaleMultiplier = 1.04f, float opacityMultiplier = 1.1f)
        {
            telegraphColor = accentColor;
            _accentScaleMultiplier = Mathf.Clamp(scaleMultiplier, 0.8f, 1.3f);
            _accentOpacityMultiplier = Mathf.Clamp(opacityMultiplier, 0.7f, 1.4f);
            UpdateTelegraphVisual();
        }

        public void RevealNow()
        {
            RestorePresentation();
            ClearTelegraphVisual();
            ClearCaches();
            _remaining = 0f;
            _duration = 0f;
            _isConfigured = false;

            enabled = false;
        }

        private void CachePresentationTargets()
        {
            _renderers.Clear();
            _rendererEnabledStates.Clear();
            _colliders.Clear();
            _colliderEnabledStates.Clear();

            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);

            for (int index = 0; index < renderers.Length; index++)
            {
                Renderer renderer = renderers[index];

                if (renderer == null)
                {
                    continue;
                }

                _renderers.Add(renderer);
                _rendererEnabledStates.Add(renderer.enabled);
            }

            Collider2D[] colliders = GetComponentsInChildren<Collider2D>(true);

            for (int index = 0; index < colliders.Length; index++)
            {
                Collider2D collider = colliders[index];

                if (collider == null)
                {
                    continue;
                }

                _colliders.Add(collider);
                _colliderEnabledStates.Add(collider.enabled);
            }
        }

        private void HidePresentation()
        {
            if (_presentationHidden)
            {
                return;
            }

            for (int index = 0; index < _renderers.Count; index++)
            {
                Renderer renderer = _renderers[index];

                if (renderer == null)
                {
                    continue;
                }

                renderer.enabled = false;
            }

            for (int index = 0; index < _colliders.Count; index++)
            {
                Collider2D collider = _colliders[index];

                if (collider == null)
                {
                    continue;
                }

                collider.enabled = false;
            }

            _presentationHidden = true;
        }

        private void RestorePresentation()
        {
            if (!_presentationHidden)
            {
                return;
            }

            for (int index = 0; index < _renderers.Count; index++)
            {
                Renderer renderer = _renderers[index];

                if (renderer == null)
                {
                    continue;
                }

                bool enabledState = index < _rendererEnabledStates.Count && _rendererEnabledStates[index];
                renderer.enabled = enabledState;
            }

            for (int index = 0; index < _colliders.Count; index++)
            {
                Collider2D collider = _colliders[index];

                if (collider == null)
                {
                    continue;
                }

                bool enabledState = index < _colliderEnabledStates.Count && _colliderEnabledStates[index];
                collider.enabled = enabledState;
            }

            _presentationHidden = false;
        }

        private void BuildTelegraphVisual()
        {
            EnsureTelegraphRoot();

            if (_telegraphRoot == null)
            {
                return;
            }

            _telegraphRoot.gameObject.SetActive(true);
            _telegraphRoot.gameObject.layer = gameObject.layer;
            _telegraphRoot.localPosition = new Vector3(localOffset.x, localOffset.y, 0f);
            _telegraphRoot.localScale = Vector3.one;
            float effectiveScale = telegraphScale * _accentScaleMultiplier;
            float effectiveOpacity = Mathf.Clamp01(telegraphOpacity * _accentOpacityMultiplier);

            ConfigureTelegraphPart(
                ref _outerRingRenderer,
                "OuterRing",
                GetCircleSprite(),
                Vector3.zero,
                new Vector3(effectiveScale * 1.4f, effectiveScale * 1.4f, 1f),
                new Color(telegraphColor.r, telegraphColor.g, telegraphColor.b, effectiveOpacity * 0.32f),
                1);

            ConfigureTelegraphPart(
                ref _innerPulseRenderer,
                "InnerPulse",
                GetCircleSprite(),
                Vector3.zero,
                new Vector3(effectiveScale * 0.78f, effectiveScale * 0.78f, 1f),
                new Color(telegraphColor.r, telegraphColor.g, telegraphColor.b, effectiveOpacity * 0.2f),
                2);

            ConfigureTelegraphPart(
                ref _crossbarHorizontalRenderer,
                "CrossbarHorizontal",
                GetWhiteSprite(),
                Vector3.zero,
                new Vector3(effectiveScale * 1.06f, effectiveScale * 0.1f, 1f),
                new Color(1f, 1f, 1f, effectiveOpacity * 0.72f),
                3);

            ConfigureTelegraphPart(
                ref _crossbarVerticalRenderer,
                "CrossbarVertical",
                GetWhiteSprite(),
                Vector3.zero,
                new Vector3(effectiveScale * 0.1f, effectiveScale * 1.06f, 1f),
                new Color(1f, 1f, 1f, effectiveOpacity * 0.72f),
                3);
        }

        private void EnsureTelegraphRoot()
        {
            if (_telegraphRoot != null)
            {
                return;
            }

            GameObject root = new("SpawnTelegraph");
            root.layer = gameObject.layer;
            root.transform.SetParent(transform, false);
            _telegraphRoot = root.transform;
        }

        private void ConfigureTelegraphPart(
            ref SpriteRenderer renderer,
            string name,
            Sprite sprite,
            Vector3 localPosition,
            Vector3 localScale,
            Color color,
            int sortingOrder)
        {
            if (renderer == null)
            {
                Transform childTransform = _telegraphRoot.Find(name);

                if (childTransform == null)
                {
                    GameObject child = new(name);
                    childTransform = child.transform;
                    childTransform.SetParent(_telegraphRoot, false);
                }

                renderer = childTransform.GetComponent<SpriteRenderer>();

                if (renderer == null)
                {
                    renderer = childTransform.gameObject.AddComponent<SpriteRenderer>();
                }
            }

            renderer.gameObject.SetActive(true);
            renderer.gameObject.layer = gameObject.layer;
            renderer.transform.localPosition = localPosition;
            renderer.transform.localScale = localScale;
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
        }

        private void UpdateTelegraphVisual()
        {
            if (_telegraphRoot == null)
            {
                return;
            }

            float normalizedProgress = _duration > 0.001f
                ? 1f - Mathf.Clamp01(_remaining / _duration)
                : 1f;
            float pulse = 0.5f + (0.5f * Mathf.Sin((Time.time * pulseSpeed) + _phaseOffset));
            float effectiveScale = telegraphScale * _accentScaleMultiplier;
            float effectiveOpacity = Mathf.Clamp01(telegraphOpacity * _accentOpacityMultiplier);
            float ringScale = Mathf.Lerp(0.72f, 1.08f, normalizedProgress);
            float innerScale = Mathf.Lerp(0.52f, 0.86f, pulse);
            float alphaBoost = Mathf.Lerp(0.7f, 1.1f, pulse);
            _telegraphRoot.localScale = Vector3.one * Mathf.Lerp(0.96f, 1.04f, pulse * 0.55f);

            if (_outerRingRenderer != null)
            {
                _outerRingRenderer.transform.localScale = new Vector3(effectiveScale * 1.4f * ringScale, effectiveScale * 1.4f * ringScale, 1f);
                _outerRingRenderer.color = new Color(
                    telegraphColor.r,
                    telegraphColor.g,
                    telegraphColor.b,
                    Mathf.Clamp01((effectiveOpacity * 0.24f) + (normalizedProgress * effectiveOpacity * 0.34f)));
            }

            if (_innerPulseRenderer != null)
            {
                _innerPulseRenderer.transform.localScale = new Vector3(effectiveScale * innerScale, effectiveScale * innerScale, 1f);
                _innerPulseRenderer.color = new Color(
                    telegraphColor.r,
                    telegraphColor.g,
                    telegraphColor.b,
                    Mathf.Clamp01((effectiveOpacity * 0.12f) + (effectiveOpacity * 0.18f * alphaBoost)));
            }

            if (_crossbarHorizontalRenderer != null)
            {
                _crossbarHorizontalRenderer.color = new Color(1f, 1f, 1f, Mathf.Clamp01(effectiveOpacity * 0.46f * alphaBoost));
            }

            if (_crossbarVerticalRenderer != null)
            {
                _crossbarVerticalRenderer.color = new Color(1f, 1f, 1f, Mathf.Clamp01(effectiveOpacity * 0.46f * alphaBoost));
            }
        }

        private void ClearTelegraphVisual()
        {
            if (_telegraphRoot == null)
            {
                return;
            }

            _telegraphRoot.gameObject.SetActive(false);
        }

        private void ClearCaches()
        {
            _renderers.Clear();
            _rendererEnabledStates.Clear();
            _colliders.Clear();
            _colliderEnabledStates.Clear();
            _presentationHidden = false;
        }

        private void CleanupImmediate()
        {
            RestorePresentation();
            ClearTelegraphVisual();
            ClearCaches();
            _remaining = 0f;
            _duration = 0f;
            _accentScaleMultiplier = 1f;
            _accentOpacityMultiplier = 1f;
            _isConfigured = false;
        }

        private static Sprite GetWhiteSprite()
        {
            if (s_WhiteSprite != null)
            {
                return s_WhiteSprite;
            }

            s_WhiteSprite = Sprite.Create(
                Texture2D.whiteTexture,
                new Rect(0f, 0f, Texture2D.whiteTexture.width, Texture2D.whiteTexture.height),
                new Vector2(0.5f, 0.5f),
                100f);
            return s_WhiteSprite;
        }

        private static Sprite GetCircleSprite()
        {
            if (s_CircleSprite != null)
            {
                return s_CircleSprite;
            }

            const int size = 64;
            Texture2D texture = new(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                name = "RuntimeEnemySpawnTelegraphCircle"
            };

            Vector2 center = new((size - 1) * 0.5f, (size - 1) * 0.5f);
            float radius = size * 0.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float normalizedDistance = Vector2.Distance(new Vector2(x, y), center) / radius;
                    float alpha = Mathf.Clamp01(1f - normalizedDistance);
                    alpha = Mathf.SmoothStep(0f, 1f, alpha);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply();
            s_CircleSprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f),
                100f);
            return s_CircleSprite;
        }
    }
}
