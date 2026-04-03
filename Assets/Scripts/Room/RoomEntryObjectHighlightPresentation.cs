using CuteIssac.Data.Dungeon;
using UnityEngine;

namespace CuteIssac.Room
{
    /// <summary>
    /// Runtime-only object highlight for actionable room targets such as pickups, shop slots, landmarks, or arena cores.
    /// It sits on the target object so later authored VFX can replace only this layer.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RoomEntryObjectHighlightPresentation : MonoBehaviour
    {
        [Header("Layout")]
        [SerializeField] private Vector3 rootOffset = new(0f, 0.04f, 0f);
        [SerializeField] [Min(0.05f)] private float crownHeight = 0.58f;
        [SerializeField] [Min(0.05f)] private float pulseFrequency = 4.2f;
        [SerializeField] [Min(0.01f)] private float bobAmplitude = 0.04f;
        [SerializeField] [Min(0.05f)] private float bobFrequency = 2.4f;

        private static Sprite s_WhiteSprite;
        private static Sprite s_CircleSprite;

        private Transform _highlightRoot;
        private SpriteRenderer _ringRenderer;
        private SpriteRenderer _haloRenderer;
        private SpriteRenderer _coreRenderer;
        private SpriteRenderer _beamRenderer;
        private SpriteRenderer _markerA;
        private SpriteRenderer _markerB;
        private Vector3 _baseLocalPosition;
        private Vector3 _ringBaseScale = Vector3.one;
        private Vector3 _haloBaseScale = Vector3.one;
        private Vector3 _coreBaseScale = Vector3.one;
        private Vector3 _beamBaseScale = Vector3.one;
        private Vector3 _markerABaseScale = Vector3.one;
        private Vector3 _markerBBaseScale = Vector3.one;
        private RoomType _roomType = RoomType.Normal;
        private Color _accentColor = Color.white;
        private float _startedAt = float.NegativeInfinity;
        private float _duration;
        private float _radius = 0.56f;
        private bool _primary;

        private void Awake()
        {
            EnsureVisuals();
            HideImmediate();
        }

        private void OnEnable()
        {
            EnsureVisuals();
        }

        private void Update()
        {
            if (_highlightRoot == null || _duration <= 0f)
            {
                return;
            }

            float elapsed = Time.unscaledTime - _startedAt;
            if (elapsed < 0f || elapsed > _duration)
            {
                HideImmediate();
                return;
            }

            float normalized = Mathf.Clamp01(elapsed / _duration);
            float pulse = 0.5f + (Mathf.Sin(Time.unscaledTime * pulseFrequency) * 0.5f);
            float bob = Mathf.Sin(Time.unscaledTime * bobFrequency) * bobAmplitude;
            float fade = 1f - normalized;
            Color brightAccent = Color.Lerp(_accentColor, Color.white, _primary ? 0.38f : 0.26f);

            _highlightRoot.localPosition = _baseLocalPosition + new Vector3(0f, bob, 0f);

            _ringRenderer.transform.localScale = _ringBaseScale * Mathf.Lerp(0.92f, 1.16f, pulse);
            _ringRenderer.color = new Color(_accentColor.r, _accentColor.g, _accentColor.b, Mathf.Lerp(_primary ? 0.4f : 0.28f, 0f, normalized));

            _haloRenderer.transform.localScale = _haloBaseScale * Mathf.Lerp(0.84f, 1.24f, pulse);
            _haloRenderer.color = new Color(brightAccent.r, brightAccent.g, brightAccent.b, Mathf.Lerp(_primary ? 0.2f : 0.14f, 0f, normalized));

            _coreRenderer.transform.localScale = _coreBaseScale * Mathf.Lerp(0.92f, 1.18f, pulse);
            _coreRenderer.color = new Color(brightAccent.r, brightAccent.g, brightAccent.b, Mathf.Lerp(_primary ? 0.94f : 0.72f, 0f, 1f - fade));

            _beamRenderer.transform.localScale = new Vector3(
                _beamBaseScale.x,
                _beamBaseScale.y * Mathf.Lerp(0.88f, _primary ? 1.32f : 1.14f, pulse),
                1f);
            _beamRenderer.color = new Color(brightAccent.r, brightAccent.g, brightAccent.b, Mathf.Lerp(_primary ? 0.26f : 0.18f, 0f, normalized));

            UpdateMarker(_markerA, _markerABaseScale, brightAccent, pulse, fade);
            UpdateMarker(_markerB, _markerBBaseScale, brightAccent, pulse, fade);
        }

        public void PlayHighlight(RoomType roomType, Color accentColor, bool primary, bool guidedRoute, float duration, float radius)
        {
            EnsureVisuals();
            _roomType = roomType;
            _accentColor = ResolveAccent(accentColor);
            _primary = primary;
            _duration = Mathf.Max(0.2f, guidedRoute ? duration + 0.12f : duration);
            _radius = Mathf.Max(0.36f, radius);
            _startedAt = Time.unscaledTime;
            ApplyLayout(roomType, _radius, primary);
            _highlightRoot.gameObject.SetActive(true);
        }

        private void EnsureVisuals()
        {
            if (_highlightRoot != null)
            {
                return;
            }

            GameObject root = new("RoomEntryObjectHighlight");
            root.layer = gameObject.layer;
            _highlightRoot = root.transform;
            _highlightRoot.SetParent(transform, false);
            _baseLocalPosition = rootOffset;
            _highlightRoot.localPosition = _baseLocalPosition;
            _highlightRoot.localRotation = Quaternion.identity;
            _highlightRoot.localScale = Vector3.one;

            _ringRenderer = CreatePart("Ring", GetCircleSprite(), Vector3.zero, Vector3.one, new Color(1f, 1f, 1f, 0f), 52);
            _haloRenderer = CreatePart("Halo", GetCircleSprite(), Vector3.zero, Vector3.one, new Color(1f, 1f, 1f, 0f), 50);
            _coreRenderer = CreatePart("Core", GetCircleSprite(), Vector3.up * crownHeight, new Vector3(0.14f, 0.14f, 1f), new Color(1f, 1f, 1f, 0f), 54);
            _beamRenderer = CreatePart("Beam", GetWhiteSprite(), new Vector3(0f, crownHeight * 0.46f, 0f), new Vector3(0.05f, crownHeight, 1f), new Color(1f, 1f, 1f, 0f), 49);
            _markerA = CreatePart("MarkerA", GetWhiteSprite(), Vector3.zero, new Vector3(0.18f, 0.05f, 1f), new Color(1f, 1f, 1f, 0f), 53);
            _markerB = CreatePart("MarkerB", GetWhiteSprite(), Vector3.zero, new Vector3(0.18f, 0.05f, 1f), new Color(1f, 1f, 1f, 0f), 53);

            _ringBaseScale = _ringRenderer.transform.localScale;
            _haloBaseScale = _haloRenderer.transform.localScale;
            _coreBaseScale = _coreRenderer.transform.localScale;
            _beamBaseScale = _beamRenderer.transform.localScale;
            _markerABaseScale = _markerA.transform.localScale;
            _markerBBaseScale = _markerB.transform.localScale;
            HideImmediate();
        }

        private SpriteRenderer CreatePart(string name, Sprite sprite, Vector3 localPosition, Vector3 localScale, Color color, int sortingOrder)
        {
            GameObject child = new(name);
            child.layer = gameObject.layer;
            child.transform.SetParent(_highlightRoot, false);
            child.transform.localPosition = localPosition;
            child.transform.localRotation = Quaternion.identity;
            child.transform.localScale = localScale;

            SpriteRenderer renderer = child.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
            return renderer;
        }

        private void ApplyLayout(RoomType roomType, float radius, bool primary)
        {
            _highlightRoot.localPosition = _baseLocalPosition;
            _ringRenderer.transform.localScale = new Vector3(radius, radius, 1f);
            _haloRenderer.transform.localScale = new Vector3(radius * 1.28f, radius * 1.28f, 1f);
            _coreRenderer.transform.localPosition = new Vector3(0f, crownHeight + (primary ? 0.06f : 0f), 0f);
            _beamRenderer.transform.localPosition = new Vector3(0f, crownHeight * 0.46f, 0f);
            _beamRenderer.transform.localScale = new Vector3(primary ? 0.06f : 0.05f, crownHeight + (primary ? 0.12f : 0f), 1f);

            float lateralOffset = Mathf.Max(0.22f, radius * 0.56f);
            switch (roomType)
            {
                case RoomType.Treasure:
                case RoomType.Shop:
                    ConfigureMarker(_markerA, new Vector3(-lateralOffset, crownHeight * 0.82f, 0f), new Vector3(0.2f, 0.06f, 1f), 18f, true);
                    ConfigureMarker(_markerB, new Vector3(lateralOffset, crownHeight * 0.82f, 0f), new Vector3(0.2f, 0.06f, 1f), -18f, true);
                    break;
                case RoomType.Boss:
                case RoomType.Challenge:
                case RoomType.MiniBoss:
                    ConfigureMarker(_markerA, new Vector3(-lateralOffset, 0f, 0f), new Vector3(0.08f, crownHeight * 1.08f, 1f), 0f, true);
                    ConfigureMarker(_markerB, new Vector3(lateralOffset, 0f, 0f), new Vector3(0.08f, crownHeight * 1.08f, 1f), 0f, true);
                    break;
                case RoomType.Secret:
                case RoomType.Curse:
                case RoomType.Trap:
                    ConfigureMarker(_markerA, new Vector3(-lateralOffset * 0.82f, crownHeight * 0.52f, 0f), new Vector3(0.08f, crownHeight * 0.78f, 1f), 40f, true);
                    ConfigureMarker(_markerB, new Vector3(lateralOffset * 0.82f, crownHeight * 0.52f, 0f), new Vector3(0.08f, crownHeight * 0.78f, 1f), -40f, true);
                    break;
                default:
                    ConfigureMarker(_markerA, new Vector3(-lateralOffset, crownHeight * 0.72f, 0f), new Vector3(0.16f, 0.05f, 1f), 0f, true);
                    ConfigureMarker(_markerB, new Vector3(lateralOffset, crownHeight * 0.72f, 0f), new Vector3(0.16f, 0.05f, 1f), 0f, true);
                    break;
            }

            _ringBaseScale = _ringRenderer.transform.localScale;
            _haloBaseScale = _haloRenderer.transform.localScale;
            _coreBaseScale = _coreRenderer.transform.localScale;
            _beamBaseScale = _beamRenderer.transform.localScale;
            _markerABaseScale = _markerA.transform.localScale;
            _markerBBaseScale = _markerB.transform.localScale;
        }

        private static void ConfigureMarker(SpriteRenderer renderer, Vector3 localPosition, Vector3 localScale, float rotationZ, bool enabled)
        {
            if (renderer == null)
            {
                return;
            }

            renderer.enabled = enabled;
            renderer.transform.localPosition = localPosition;
            renderer.transform.localScale = localScale;
            renderer.transform.localRotation = Quaternion.Euler(0f, 0f, rotationZ);
        }

        private static void UpdateMarker(SpriteRenderer renderer, Vector3 baseScale, Color color, float pulse, float fade)
        {
            if (renderer == null || !renderer.enabled)
            {
                return;
            }

            renderer.transform.localScale = baseScale * Mathf.Lerp(0.94f, 1.18f, pulse);
            renderer.color = new Color(color.r, color.g, color.b, Mathf.Lerp(0.92f, 0f, 1f - fade));
        }

        private void HideImmediate()
        {
            _duration = 0f;

            if (_highlightRoot != null)
            {
                _highlightRoot.gameObject.SetActive(false);
            }
        }

        private static Color ResolveAccent(Color accentColor)
        {
            return accentColor.a > 0.01f
                ? new Color(accentColor.r, accentColor.g, accentColor.b, 1f)
                : new Color(0.66f, 0.92f, 1f, 1f);
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
                name = "RuntimeRoomEntryObjectCircle"
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
