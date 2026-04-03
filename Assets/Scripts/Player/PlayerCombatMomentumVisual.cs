using System.Collections.Generic;
using UnityEngine;

namespace CuteIssac.Player
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerCombatMomentumController))]
    public sealed class PlayerCombatMomentumVisual : MonoBehaviour
    {
        [SerializeField] private PlayerCombatMomentumController momentumController;
        [SerializeField] private PlayerVisual playerVisual;
        [SerializeField] private Vector3 auraLocalOffset = new(0f, -0.34f, 0f);
        [SerializeField] [Min(0.4f)] private float auraRadius = 1.08f;
        [SerializeField] [Min(0.2f)] private float ringThickness = 0.18f;
        [SerializeField] [Min(0f)] private float bobAmplitude = 0.04f;
        [SerializeField] [Min(0f)] private float bobSpeed = 2.1f;
        [SerializeField] [Min(0f)] private float pulseSpeed = 5.6f;
        [SerializeField] [Range(0f, 1f)] private float auraOpacity = 0.24f;
        [SerializeField] [Range(0f, 1f)] private float ringOpacity = 0.72f;

        private static Sprite s_CircleSprite;
        private static Sprite s_WhiteSprite;

        private readonly List<SpriteRenderer> _chainShardRenderers = new();
        private Transform _visualRoot;
        private SpriteRenderer _auraRenderer;
        private SpriteRenderer _ringRenderer;
        private float _phaseOffset;

        private void Awake()
        {
            ResolveReferences();
            _phaseOffset = Random.Range(0f, Mathf.PI * 2f);
        }

        private void OnEnable()
        {
            ResolveReferences();
        }

        private void OnDisable()
        {
            SetVisualActive(false);
        }

        private void Update()
        {
            if (momentumController == null || !momentumController.IsMomentumActive)
            {
                SetVisualActive(false);
                return;
            }

            EnsureVisual();
            UpdateVisual();
        }

        private void ResolveReferences()
        {
            if (momentumController == null)
            {
                momentumController = GetComponent<PlayerCombatMomentumController>();
            }

            if (playerVisual == null)
            {
                playerVisual = GetComponent<PlayerVisual>();
            }
        }

        private void EnsureVisual()
        {
            if (_visualRoot != null)
            {
                SetVisualActive(true);
                return;
            }

            GameObject root = new("MomentumVisual");
            root.layer = gameObject.layer;
            root.transform.SetParent(transform, false);
            _visualRoot = root.transform;

            _auraRenderer = CreatePart(
                "Aura",
                GetCircleSprite(),
                auraLocalOffset,
                new Vector3(auraRadius, auraRadius * 0.68f, 1f),
                new Color(1f, 1f, 1f, auraOpacity),
                -3);

            _ringRenderer = CreatePart(
                "Ring",
                GetCircleSprite(),
                auraLocalOffset,
                new Vector3(auraRadius * 0.86f, ringThickness, 1f),
                new Color(1f, 1f, 1f, ringOpacity),
                -2);

            for (int shardIndex = 0; shardIndex < 3; shardIndex++)
            {
                _chainShardRenderers.Add(CreatePart(
                    $"ChainShard{shardIndex + 1}",
                    GetWhiteSprite(),
                    auraLocalOffset,
                    new Vector3(0.12f, 0.12f, 1f),
                    new Color(1f, 1f, 1f, 0f),
                    -1,
                    45f));
            }
        }

        private void UpdateVisual()
        {
            if (_visualRoot == null)
            {
                return;
            }

            float pulseTime = (Time.time * pulseSpeed) + _phaseOffset;
            float pulse = 0.5f + (0.5f * Mathf.Sin(pulseTime));
            float bob = Mathf.Sin((Time.time * bobSpeed) + _phaseOffset) * bobAmplitude;
            Color accentColor = momentumController.AccentColor;
            float remaining = momentumController.RemainingDurationNormalized;
            int chainCount = Mathf.Max(1, momentumController.ChainCount);
            float chainIntensity = 1f + ((chainCount - 1) * 0.08f);

            _visualRoot.localPosition = new Vector3(0f, bob, 0f);

            if (_auraRenderer != null)
            {
                _auraRenderer.transform.localScale = new Vector3(
                    auraRadius * Mathf.Lerp(0.94f, 1.12f, pulse) * chainIntensity,
                    auraRadius * 0.68f * Mathf.Lerp(0.94f, 1.08f, pulse) * chainIntensity,
                    1f);
                _auraRenderer.color = new Color(
                    accentColor.r,
                    accentColor.g,
                    accentColor.b,
                    Mathf.Lerp(auraOpacity * 0.5f, auraOpacity * 1.25f, pulse) * Mathf.Lerp(0.55f, 1f, remaining));
            }

            if (_ringRenderer != null)
            {
                _ringRenderer.transform.localScale = new Vector3(
                    auraRadius * Mathf.Lerp(0.78f, 0.94f, remaining) * chainIntensity,
                    ringThickness * Mathf.Lerp(0.88f, 1.18f, pulse),
                    1f);
                _ringRenderer.color = new Color(
                    Mathf.Lerp(accentColor.r, 1f, 0.24f),
                    Mathf.Lerp(accentColor.g, 1f, 0.24f),
                    Mathf.Lerp(accentColor.b, 1f, 0.24f),
                    Mathf.Lerp(ringOpacity * 0.44f, ringOpacity, pulse));
            }

            for (int shardIndex = 0; shardIndex < _chainShardRenderers.Count; shardIndex++)
            {
                SpriteRenderer shardRenderer = _chainShardRenderers[shardIndex];

                if (shardRenderer == null)
                {
                    continue;
                }

                bool active = shardIndex < chainCount;
                shardRenderer.enabled = active;

                if (!active)
                {
                    continue;
                }

                float angle = ((Time.time * (56f + (shardIndex * 9f))) + _phaseOffset + (shardIndex * 2.094f)) * Mathf.Deg2Rad;
                float orbitRadius = auraRadius * (0.42f + (shardIndex * 0.08f));
                Vector3 localPosition = auraLocalOffset + new Vector3(Mathf.Cos(angle) * orbitRadius, (Mathf.Sin(angle) * orbitRadius * 0.32f) + 0.24f, 0f);
                shardRenderer.transform.localPosition = localPosition;
                shardRenderer.transform.localScale = Vector3.one * Mathf.Lerp(0.1f, 0.16f, pulse);
                shardRenderer.color = new Color(
                    Mathf.Lerp(accentColor.r, 1f, 0.32f),
                    Mathf.Lerp(accentColor.g, 1f, 0.32f),
                    Mathf.Lerp(accentColor.b, 1f, 0.32f),
                    Mathf.Lerp(0.42f, 0.92f, pulse));
            }
        }

        private void SetVisualActive(bool active)
        {
            if (_visualRoot != null)
            {
                _visualRoot.gameObject.SetActive(active);
            }
        }

        private SpriteRenderer CreatePart(string name, Sprite sprite, Vector3 localPosition, Vector3 localScale, Color color, int sortingOrder, float rotationZ = 0f)
        {
            GameObject child = new(name);
            child.layer = gameObject.layer;
            child.transform.SetParent(_visualRoot, false);
            child.transform.localPosition = localPosition;
            child.transform.localScale = localScale;
            child.transform.localRotation = Quaternion.Euler(0f, 0f, rotationZ);

            SpriteRenderer renderer = child.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = ResolveSortingOrder(sortingOrder);
            return renderer;
        }

        private int ResolveSortingOrder(int fallbackOrder)
        {
            SpriteRenderer bodyRenderer = playerVisual != null ? playerVisual.BodySpriteRenderer : null;
            return bodyRenderer != null
                ? bodyRenderer.sortingOrder + fallbackOrder
                : fallbackOrder;
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

            const int size = 48;
            Texture2D texture = new(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                name = "RuntimePlayerMomentumCircle"
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
