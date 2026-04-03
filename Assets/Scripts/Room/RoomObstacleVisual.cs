using System.Collections.Generic;
using UnityEngine;

namespace CuteIssac.Room
{
    /// <summary>
    /// Presentation-only layer for room obstacles.
    /// When final art is missing, it builds readable procedural silhouettes per obstacle type.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RoomObstacleVisual : MonoBehaviour
    {
        [Header("Authored Renderers")]
        [SerializeField] private SpriteRenderer bodyRenderer;
        [SerializeField] private SpriteRenderer accentRenderer;

        [Header("Fallback Shape")]
        [SerializeField] private bool useProceduralVisual = true;
        [SerializeField] private Transform proceduralVisualRoot;
        [SerializeField] private int proceduralSortingOrder = 18;

        [Header("Reactive Feedback")]
        [SerializeField] private Color idleColor = Color.white;
        [SerializeField] private Color projectileHitColor = new(1f, 0.92f, 0.6f, 1f);
        [SerializeField] private Color hazardActiveColor = new(1f, 0.45f, 0.45f, 1f);
        [SerializeField] [Min(0f)] private float flashDuration = 0.08f;

        [Header("Rock Palette")]
        [SerializeField] private Color rockBodyColor = new(0.76f, 0.66f, 0.74f, 1f);
        [SerializeField] private Color rockCapColor = new(0.98f, 0.92f, 0.96f, 1f);
        [SerializeField] private Color rockDetailColor = new(0.56f, 0.42f, 0.52f, 1f);
        [SerializeField] private Color rockShadowColor = new(0.2f, 0.14f, 0.22f, 0.42f);

        [Header("Pit Palette")]
        [SerializeField] private Color pitRimColor = new(0.62f, 0.48f, 0.62f, 1f);
        [SerializeField] private Color pitLipColor = new(0.88f, 0.76f, 0.9f, 1f);
        [SerializeField] private Color pitVoidColor = new(0.08f, 0.06f, 0.1f, 0.96f);
        [SerializeField] private Color pitGlowColor = new(0.34f, 0.28f, 0.44f, 0.38f);

        [Header("Spike Palette")]
        [SerializeField] private Color spikeBaseColor = new(0.54f, 0.4f, 0.48f, 1f);
        [SerializeField] private Color spikeTeethColor = new(0.96f, 0.93f, 0.98f, 1f);
        [SerializeField] private Color spikeCoreColor = new(0.82f, 0.54f, 0.6f, 1f);
        [SerializeField] private Color spikeShadowColor = new(0.18f, 0.12f, 0.18f, 0.42f);

        private float _flashRemaining;
        private Color _currentFlashColor;
        private RoomObstacleController _obstacleController;
        private readonly List<SpriteRenderer> _proceduralRenderers = new();
        private readonly List<Color> _proceduralBaseColors = new();
        private static Sprite s_whiteSprite;
        private static Sprite s_circleSprite;

        private void Awake()
        {
            ResolveReferences();
            EnsureVisualBuilt();
            ApplyIdleVisualState();
        }

        private void OnEnable()
        {
            ResolveReferences();
            EnsureVisualBuilt();
            ApplyIdleVisualState();
        }

        private void Update()
        {
            if (_flashRemaining <= 0f)
            {
                return;
            }

            _flashRemaining -= Time.deltaTime;

            if (_flashRemaining <= 0f)
            {
                ApplyIdleVisualState();
            }
        }

        public void HandleProjectileImpact()
        {
            Flash(projectileHitColor);
        }

        public void HandleHazardTriggered()
        {
            Flash(hazardActiveColor);
        }

        private void Flash(Color color)
        {
            _currentFlashColor = color;
            _flashRemaining = flashDuration;
            ApplyColor(color);
        }

        private void EnsureVisualBuilt()
        {
            // Procedural preview generation during validation mutates prefab assets and leaks editor objects.
            if (!Application.isPlaying)
            {
                return;
            }

            if (!useProceduralVisual)
            {
                SetProceduralRootActive(false);
                SetAuthoredRenderersVisible(true);
                return;
            }

            EnsureProceduralRoot();
            RebuildProceduralVisual();
            SetAuthoredRenderersVisible(false);
            SetProceduralRootActive(true);
        }

        private void EnsureProceduralRoot()
        {
            if (proceduralVisualRoot != null)
            {
                return;
            }

            Transform existingRoot = transform.Find("ProceduralObstacleVisual");

            if (existingRoot != null)
            {
                proceduralVisualRoot = existingRoot;
                return;
            }

            GameObject rootObject = new("ProceduralObstacleVisual");
            rootObject.transform.SetParent(transform, false);
            rootObject.transform.localPosition = Vector3.zero;
            rootObject.transform.localRotation = Quaternion.identity;
            rootObject.transform.localScale = Vector3.one;
            rootObject.layer = gameObject.layer;
            proceduralVisualRoot = rootObject.transform;
        }

        private void RebuildProceduralVisual()
        {
            if (proceduralVisualRoot == null)
            {
                return;
            }

            ClearProceduralParts();

            switch (ResolveObstacleType())
            {
                case RoomObstacleType.Pit:
                    BuildPitVisual();
                    break;
                case RoomObstacleType.Spike:
                    BuildSpikeVisual();
                    break;
                default:
                    BuildRockVisual();
                    break;
            }
        }

        private void BuildRockVisual()
        {
            CreatePart("Shadow", GetCircleSprite(), new Vector2(0f, -0.28f), new Vector2(1.56f, 0.46f), rockShadowColor, -3);
            CreatePart("Body", GetCircleSprite(), new Vector2(0f, 0.02f), new Vector2(1.22f, 1.02f), rockBodyColor, 0);
            CreatePart("Cap", GetWhiteSprite(), new Vector2(0f, 0.36f), new Vector2(0.9f, 0.22f), rockCapColor, 1);
            CreatePart("CrackLeft", GetWhiteSprite(), new Vector2(-0.18f, 0.1f), new Vector2(0.06f, 0.42f), rockDetailColor, 2, 18f);
            CreatePart("CrackRight", GetWhiteSprite(), new Vector2(0.18f, -0.04f), new Vector2(0.06f, 0.34f), rockDetailColor, 2, -16f);
            CreatePart("PebbleLeft", GetCircleSprite(), new Vector2(-0.46f, -0.18f), new Vector2(0.22f, 0.18f), rockDetailColor, 1);
            CreatePart("PebbleRight", GetCircleSprite(), new Vector2(0.42f, -0.14f), new Vector2(0.18f, 0.14f), rockCapColor, 1);
        }

        private void BuildPitVisual()
        {
            CreatePart("Glow", GetCircleSprite(), new Vector2(0f, -0.02f), new Vector2(1.72f, 1.16f), pitGlowColor, -3);
            CreatePart("Rim", GetCircleSprite(), new Vector2(0f, 0f), new Vector2(1.46f, 1.02f), pitRimColor, -2);
            CreatePart("Lip", GetCircleSprite(), new Vector2(0f, 0.08f), new Vector2(1.12f, 0.74f), pitLipColor, -1);
            CreatePart("Void", GetCircleSprite(), new Vector2(0f, -0.02f), new Vector2(0.94f, 0.56f), pitVoidColor, 0);
            CreatePart("ShardLeft", GetWhiteSprite(), new Vector2(-0.48f, 0.18f), new Vector2(0.14f, 0.3f), pitRimColor, 1, -24f);
            CreatePart("ShardRight", GetWhiteSprite(), new Vector2(0.48f, 0.18f), new Vector2(0.14f, 0.3f), pitRimColor, 1, 24f);
        }

        private void BuildSpikeVisual()
        {
            CreatePart("Shadow", GetCircleSprite(), new Vector2(0f, -0.26f), new Vector2(1.5f, 0.42f), spikeShadowColor, -3);
            CreatePart("BasePlate", GetWhiteSprite(), new Vector2(0f, -0.18f), new Vector2(1.3f, 0.24f), spikeBaseColor, -1);
            CreatePart("BaseLip", GetWhiteSprite(), new Vector2(0f, -0.08f), new Vector2(1.08f, 0.1f), spikeCoreColor, 0);

            CreatePart("SpikeShadowA", GetWhiteSprite(), new Vector2(-0.4f, -0.02f), new Vector2(0.14f, 0.42f), spikeShadowColor, 1, 45f);
            CreatePart("SpikeShadowB", GetWhiteSprite(), new Vector2(-0.18f, 0.02f), new Vector2(0.16f, 0.5f), spikeShadowColor, 1, 45f);
            CreatePart("SpikeShadowC", GetWhiteSprite(), new Vector2(0f, 0.08f), new Vector2(0.18f, 0.62f), spikeShadowColor, 1, 45f);
            CreatePart("SpikeShadowD", GetWhiteSprite(), new Vector2(0.18f, 0.02f), new Vector2(0.16f, 0.5f), spikeShadowColor, 1, 45f);
            CreatePart("SpikeShadowE", GetWhiteSprite(), new Vector2(0.4f, -0.02f), new Vector2(0.14f, 0.42f), spikeShadowColor, 1, 45f);

            CreatePart("SpikeA", GetWhiteSprite(), new Vector2(-0.4f, 0f), new Vector2(0.12f, 0.38f), spikeTeethColor, 2, 45f);
            CreatePart("SpikeB", GetWhiteSprite(), new Vector2(-0.18f, 0.04f), new Vector2(0.14f, 0.46f), spikeTeethColor, 2, 45f);
            CreatePart("SpikeC", GetWhiteSprite(), new Vector2(0f, 0.12f), new Vector2(0.16f, 0.58f), spikeTeethColor, 3, 45f);
            CreatePart("SpikeD", GetWhiteSprite(), new Vector2(0.18f, 0.04f), new Vector2(0.14f, 0.46f), spikeTeethColor, 2, 45f);
            CreatePart("SpikeE", GetWhiteSprite(), new Vector2(0.4f, 0f), new Vector2(0.12f, 0.38f), spikeTeethColor, 2, 45f);
        }

        private SpriteRenderer CreatePart(
            string name,
            Sprite sprite,
            Vector2 localPosition,
            Vector2 localScale,
            Color color,
            int sortingOffset,
            float rotationZ = 0f)
        {
            GameObject child = new(name);
            child.transform.SetParent(proceduralVisualRoot, false);
            child.transform.localPosition = new Vector3(localPosition.x, localPosition.y, 0f);
            child.transform.localRotation = Quaternion.Euler(0f, 0f, rotationZ);
            child.transform.localScale = new Vector3(localScale.x, localScale.y, 1f);
            child.layer = gameObject.layer;

            SpriteRenderer renderer = child.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = ApplyIdleTint(color);

            SpriteRenderer referenceRenderer = bodyRenderer != null ? bodyRenderer : accentRenderer;

            if (referenceRenderer != null)
            {
                renderer.sortingLayerID = referenceRenderer.sortingLayerID;
                renderer.sortingOrder = referenceRenderer.sortingOrder + sortingOffset;
            }
            else
            {
                renderer.sortingOrder = proceduralSortingOrder + sortingOffset;
            }

            _proceduralRenderers.Add(renderer);
            _proceduralBaseColors.Add(ApplyIdleTint(color));
            return renderer;
        }

        private void ClearProceduralParts()
        {
            _proceduralRenderers.Clear();
            _proceduralBaseColors.Clear();

            if (proceduralVisualRoot == null)
            {
                return;
            }

            for (int childIndex = proceduralVisualRoot.childCount - 1; childIndex >= 0; childIndex--)
            {
                GameObject childObject = proceduralVisualRoot.GetChild(childIndex).gameObject;

                if (Application.isPlaying)
                {
                    Destroy(childObject);
                }
                else
                {
                    DestroyImmediate(childObject);
                }
            }
        }

        private void ApplyIdleVisualState()
        {
            if (useProceduralVisual && _proceduralRenderers.Count > 0)
            {
                for (int index = 0; index < _proceduralRenderers.Count; index++)
                {
                    SpriteRenderer renderer = _proceduralRenderers[index];

                    if (renderer == null)
                    {
                        continue;
                    }

                    renderer.color = index < _proceduralBaseColors.Count
                        ? _proceduralBaseColors[index]
                        : idleColor;
                }

                return;
            }

            ApplyColor(idleColor);
        }

        private void ApplyColor(Color color)
        {
            if (useProceduralVisual && _proceduralRenderers.Count > 0)
            {
                for (int index = 0; index < _proceduralRenderers.Count; index++)
                {
                    SpriteRenderer renderer = _proceduralRenderers[index];

                    if (renderer != null)
                    {
                        renderer.color = color;
                    }
                }

                return;
            }

            if (bodyRenderer != null)
            {
                bodyRenderer.color = color;
            }

            if (accentRenderer != null)
            {
                accentRenderer.color = color;
            }
        }

        private Color ApplyIdleTint(Color color)
        {
            return new Color(
                color.r * idleColor.r,
                color.g * idleColor.g,
                color.b * idleColor.b,
                color.a * idleColor.a);
        }

        private RoomObstacleType ResolveObstacleType()
        {
            return _obstacleController != null ? _obstacleController.ObstacleType : RoomObstacleType.Rock;
        }

        private void SetAuthoredRenderersVisible(bool visible)
        {
            if (bodyRenderer != null)
            {
                bodyRenderer.enabled = visible;
            }

            if (accentRenderer != null)
            {
                accentRenderer.enabled = visible;
            }
        }

        private void SetProceduralRootActive(bool active)
        {
            if (proceduralVisualRoot != null)
            {
                proceduralVisualRoot.gameObject.SetActive(active);
            }
        }

        private void ResolveReferences()
        {
            if (_obstacleController == null)
            {
                _obstacleController = GetComponent<RoomObstacleController>();
            }

            SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>(true);
            SpriteRenderer resolvedBody = null;
            SpriteRenderer resolvedAccent = null;

            for (int index = 0; index < renderers.Length; index++)
            {
                SpriteRenderer renderer = renderers[index];

                if (renderer == null)
                {
                    continue;
                }

                if (proceduralVisualRoot != null && renderer.transform.IsChildOf(proceduralVisualRoot))
                {
                    continue;
                }

                if (resolvedBody == null)
                {
                    resolvedBody = renderer;
                    continue;
                }

                if (resolvedAccent == null)
                {
                    resolvedAccent = renderer;
                    break;
                }
            }

            if (bodyRenderer == null)
            {
                bodyRenderer = resolvedBody;
            }

            if (accentRenderer == null)
            {
                accentRenderer = resolvedAccent;
            }
        }

        private static Sprite GetWhiteSprite()
        {
            if (s_whiteSprite == null)
            {
                Texture2D texture = new(1, 1, TextureFormat.RGBA32, false)
                {
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp
                };
                texture.SetPixel(0, 0, Color.white);
                texture.Apply();
                s_whiteSprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
            }

            return s_whiteSprite;
        }

        private static Sprite GetCircleSprite()
        {
            if (s_circleSprite == null)
            {
                const int size = 64;
                Texture2D texture = new(size, size, TextureFormat.RGBA32, false)
                {
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp
                };

                Vector2 center = new((size - 1) * 0.5f, (size - 1) * 0.5f);
                float radius = size * 0.5f;

                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        float distance = Vector2.Distance(new Vector2(x, y), center);
                        float alpha = Mathf.Clamp01(1f - ((distance - (radius - 1.5f)) / 1.5f));
                        texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                    }
                }

                texture.Apply();
                s_circleSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
            }

            return s_circleSprite;
        }

        private void Reset()
        {
            ResolveReferences();

            if (Application.isPlaying)
            {
                EnsureVisualBuilt();
                ApplyIdleVisualState();
            }
        }

        private void OnValidate()
        {
            ResolveReferences();

            if (Application.isPlaying)
            {
                EnsureVisualBuilt();
                ApplyIdleVisualState();
            }
        }
    }
}
