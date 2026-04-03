using CuteIssac.Data.Dungeon;
using UnityEngine;

namespace CuteIssac.Room
{
    /// <summary>
    /// Runtime-only proximity affordance for currently actionable room targets.
    /// This layer stays separate from entry beats so live interaction emphasis can be swapped independently later.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RoomInteractionAffordancePresentation : MonoBehaviour
    {
        [Header("Layout")]
        [SerializeField] private Vector3 rootOffset = new(0f, 0.02f, 0f);
        [SerializeField] [Min(0.01f)] private float bobAmplitude = 0.03f;
        [SerializeField] [Min(0.05f)] private float bobFrequency = 2.8f;
        [SerializeField] [Min(0.05f)] private float pulseFrequency = 4.8f;
        [SerializeField] [Min(0.05f)] private float beamHeight = 0.46f;

        private static Sprite s_WhiteSprite;
        private static Sprite s_CircleSprite;

        private Transform _root;
        private SpriteRenderer _ringRenderer;
        private SpriteRenderer _haloRenderer;
        private SpriteRenderer _beamRenderer;
        private SpriteRenderer _coreRenderer;
        private SpriteRenderer _markerA;
        private SpriteRenderer _markerB;
        private Vector3 _baseLocalPosition;
        private Vector3 _ringBaseScale = Vector3.one;
        private Vector3 _haloBaseScale = Vector3.one;
        private Vector3 _beamBaseScale = Vector3.one;
        private Vector3 _coreBaseScale = Vector3.one;
        private Vector3 _markerABaseScale = Vector3.one;
        private Vector3 _markerBBaseScale = Vector3.one;
        private RoomType _roomType = RoomType.Normal;
        private Color _accentColor = Color.white;
        private bool _strong;
        private bool _ready;
        private bool _active;
        private float _radius = 0.48f;
        private float _visibility;
        private float _commitPulse;
        private float _resolutionPulse;
        private Color _resolutionColor = Color.white;

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
            if (_root == null)
            {
                return;
            }

            float targetVisibility = _active ? 1f : 0f;
            _visibility = Sanitize01(Mathf.MoveTowards(Sanitize01(_visibility), targetVisibility, Time.unscaledDeltaTime * (_active ? 6f : 8f)));
            _commitPulse = Sanitize01(Mathf.MoveTowards(Sanitize01(_commitPulse), 0f, Time.unscaledDeltaTime * 3.8f));
            _resolutionPulse = Sanitize01(Mathf.MoveTowards(Sanitize01(_resolutionPulse), 0f, Time.unscaledDeltaTime * 2.9f));

            if (_visibility <= 0.001f)
            {
                HideImmediate();
                return;
            }

            if (!_root.gameObject.activeSelf)
            {
                _root.gameObject.SetActive(true);
            }

            float pulse = Sanitize01(0.5f + (Mathf.Sin(Time.unscaledTime * SanitizePositive(pulseFrequency, 4.8f, 0.05f, 20f)) * 0.5f));
            float bob = SanitizeSigned(Mathf.Sin(Time.unscaledTime * SanitizePositive(bobFrequency, 2.8f, 0.05f, 20f)) * SanitizePositive(bobAmplitude, 0.03f, 0f, 1f), 0f, 1f);
            float readyBonus = _ready ? 0.24f : 0f;
            float emphasis = SanitizePositive((_strong ? 1f : 0.78f) + readyBonus + (_commitPulse * 0.28f) + (_resolutionPulse * 0.34f), 1f, 0.1f, 3f);
            Color liveAccent = Color.Lerp(_accentColor, _resolutionColor, _resolutionPulse);
            Color brightAccent = Color.Lerp(liveAccent, Color.white, (_strong ? 0.42f : 0.26f) + (_ready ? 0.1f : 0f) + (_commitPulse * 0.14f) + (_resolutionPulse * 0.1f));

            _root.localPosition = SanitizePosition(_baseLocalPosition + new Vector3(0f, bob, 0f), _baseLocalPosition);

            _ringRenderer.transform.localScale = SanitizeScale(_ringBaseScale * Mathf.Lerp(0.94f, 1.18f + (_visibility * 0.08f) + (_commitPulse * 0.18f), pulse), _ringBaseScale);
            _ringRenderer.color = new Color(liveAccent.r, liveAccent.g, liveAccent.b, _visibility * Mathf.Lerp(0.18f, 0.34f, pulse) * emphasis);

            _haloRenderer.transform.localScale = SanitizeScale(_haloBaseScale * Mathf.Lerp(0.88f, 1.28f + (_commitPulse * 0.12f), pulse), _haloBaseScale);
            _haloRenderer.color = new Color(brightAccent.r, brightAccent.g, brightAccent.b, _visibility * Mathf.Lerp(0.08f, 0.16f, pulse) * emphasis);

            _beamRenderer.transform.localScale = SanitizeScale(new Vector3(
                _beamBaseScale.x,
                _beamBaseScale.y * Mathf.Lerp(0.9f, (_strong ? 1.28f : 1.08f) + (_ready ? 0.18f : 0f) + (_commitPulse * 0.22f), pulse),
                1f), _beamBaseScale);
            _beamRenderer.color = new Color(brightAccent.r, brightAccent.g, brightAccent.b, _visibility * Mathf.Lerp(0.12f, 0.24f, pulse) * emphasis);

            _coreRenderer.transform.localScale = SanitizeScale(_coreBaseScale * Mathf.Lerp(0.94f, (_strong ? 1.24f : 1.1f) + (_ready ? 0.16f : 0f) + (_commitPulse * 0.26f), pulse), _coreBaseScale);
            _coreRenderer.color = new Color(brightAccent.r, brightAccent.g, brightAccent.b, _visibility * Mathf.Lerp(0.62f, 0.92f, pulse) * emphasis);

            UpdateMarker(_markerA, _markerABaseScale, brightAccent, pulse, emphasis);
            UpdateMarker(_markerB, _markerBBaseScale, brightAccent, pulse, emphasis);
        }

        public void SetAffordance(RoomType roomType, Color accentColor, bool strong, bool ready, float radius)
        {
            EnsureVisuals();
            _roomType = roomType;
            _accentColor = ResolveAccent(accentColor);
            _strong = strong;
            _ready = ready;
            _radius = SanitizePositive(radius, 0.48f, 0.28f, 4f);
            _active = true;
            ApplyLayout();

            if (!_root.gameObject.activeSelf)
            {
                _root.gameObject.SetActive(true);
            }
        }

        public void TriggerCommitPulse(float intensity = 1f)
        {
            _commitPulse = Mathf.Max(_commitPulse, Sanitize01(intensity));
        }

        public void TriggerResolutionFeedback(bool success, float intensity = 1f)
        {
            EnsureVisuals();
            float safeIntensity = Sanitize01(intensity);
            _resolutionColor = success
                ? new Color(0.52f, 1f, 0.74f, 1f)
                : new Color(1f, 0.52f, 0.44f, 1f);
            _resolutionPulse = Mathf.Max(_resolutionPulse, safeIntensity);
            _commitPulse = Mathf.Max(_commitPulse, safeIntensity * (success ? 0.82f : 0.56f));
            _visibility = Mathf.Max(_visibility, success ? 0.88f : 0.72f);

            if (_root != null && !_root.gameObject.activeSelf)
            {
                _root.gameObject.SetActive(true);
            }
        }

        public void ClearAffordance()
        {
            _active = false;
            _ready = false;
        }

        private void EnsureVisuals()
        {
            if (_root != null)
            {
                return;
            }

            GameObject rootObject = new("RoomInteractionAffordance");
            rootObject.layer = gameObject.layer;
            _root = rootObject.transform;
            _root.SetParent(transform, false);
            _baseLocalPosition = rootOffset;
            _root.localPosition = _baseLocalPosition;

            _ringRenderer = CreatePart("Ring", GetCircleSprite(), Vector3.zero, Vector3.one, new Color(1f, 1f, 1f, 0f), 58);
            _haloRenderer = CreatePart("Halo", GetCircleSprite(), Vector3.zero, Vector3.one, new Color(1f, 1f, 1f, 0f), 56);
            _beamRenderer = CreatePart("Beam", GetWhiteSprite(), new Vector3(0f, beamHeight * 0.5f, 0f), new Vector3(0.04f, beamHeight, 1f), new Color(1f, 1f, 1f, 0f), 55);
            _coreRenderer = CreatePart("Core", GetCircleSprite(), new Vector3(0f, beamHeight, 0f), new Vector3(0.12f, 0.12f, 1f), new Color(1f, 1f, 1f, 0f), 59);
            _markerA = CreatePart("MarkerA", GetWhiteSprite(), Vector3.zero, new Vector3(0.16f, 0.04f, 1f), new Color(1f, 1f, 1f, 0f), 60);
            _markerB = CreatePart("MarkerB", GetWhiteSprite(), Vector3.zero, new Vector3(0.16f, 0.04f, 1f), new Color(1f, 1f, 1f, 0f), 60);

            _ringBaseScale = _ringRenderer.transform.localScale;
            _haloBaseScale = _haloRenderer.transform.localScale;
            _beamBaseScale = _beamRenderer.transform.localScale;
            _coreBaseScale = _coreRenderer.transform.localScale;
            _markerABaseScale = _markerA.transform.localScale;
            _markerBBaseScale = _markerB.transform.localScale;
            HideImmediate();
        }

        private SpriteRenderer CreatePart(string name, Sprite sprite, Vector3 localPosition, Vector3 localScale, Color color, int sortingOrder)
        {
            GameObject child = new(name);
            child.layer = gameObject.layer;
            child.transform.SetParent(_root, false);
            child.transform.localPosition = SanitizePosition(localPosition, Vector3.zero);
            child.transform.localRotation = Quaternion.identity;
            child.transform.localScale = SanitizeScale(localScale, Vector3.one);

            SpriteRenderer renderer = child.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
            return renderer;
        }

        private void ApplyLayout()
        {
            float safeRadius = SanitizePositive(_radius, 0.48f, 0.28f, 4f);
            float safeBeamHeight = SanitizePositive(beamHeight, 0.46f, 0.05f, 4f);
            float lateralOffset = Mathf.Max(0.18f, safeRadius * 0.54f);
            _ringRenderer.transform.localScale = SanitizeScale(new Vector3(safeRadius, safeRadius, 1f), Vector3.one);
            _haloRenderer.transform.localScale = SanitizeScale(new Vector3(safeRadius * 1.26f, safeRadius * 1.26f, 1f), Vector3.one);
            _beamRenderer.transform.localPosition = SanitizePosition(new Vector3(0f, safeBeamHeight * 0.5f, 0f), new Vector3(0f, 0.23f, 0f));
            _beamRenderer.transform.localScale = SanitizeScale(new Vector3(
                _strong ? 0.05f : 0.04f,
                _strong ? safeBeamHeight * 1.18f : safeBeamHeight,
                1f), new Vector3(0.04f, safeBeamHeight, 1f));
            _coreRenderer.transform.localPosition = SanitizePosition(new Vector3(0f, (_strong ? safeBeamHeight * 1.1f : safeBeamHeight) + (_ready ? 0.06f : 0f), 0f), new Vector3(0f, safeBeamHeight, 0f));

            switch (_roomType)
            {
                case RoomType.Shop:
                case RoomType.Treasure:
                    ConfigureMarker(_markerA, new Vector3(-lateralOffset, safeBeamHeight * 0.9f, 0f), new Vector3(0.18f, 0.05f, 1f), 22f, true);
                    ConfigureMarker(_markerB, new Vector3(lateralOffset, safeBeamHeight * 0.9f, 0f), new Vector3(0.18f, 0.05f, 1f), -22f, true);
                    break;
                default:
                    ConfigureMarker(_markerA, new Vector3(-lateralOffset, safeBeamHeight * 0.76f, 0f), new Vector3(0.14f, 0.04f, 1f), 0f, true);
                    ConfigureMarker(_markerB, new Vector3(lateralOffset, safeBeamHeight * 0.76f, 0f), new Vector3(0.14f, 0.04f, 1f), 0f, true);
                    break;
            }

            _ringBaseScale = SanitizeScale(_ringRenderer.transform.localScale, Vector3.one);
            _haloBaseScale = SanitizeScale(_haloRenderer.transform.localScale, Vector3.one);
            _beamBaseScale = SanitizeScale(_beamRenderer.transform.localScale, new Vector3(0.04f, safeBeamHeight, 1f));
            _coreBaseScale = SanitizeScale(_coreRenderer.transform.localScale, new Vector3(0.12f, 0.12f, 1f));
            _markerABaseScale = SanitizeScale(_markerA.transform.localScale, new Vector3(0.16f, 0.04f, 1f));
            _markerBBaseScale = SanitizeScale(_markerB.transform.localScale, new Vector3(0.16f, 0.04f, 1f));
        }

        private static void ConfigureMarker(SpriteRenderer renderer, Vector3 localPosition, Vector3 localScale, float rotationZ, bool enabled)
        {
            if (renderer == null)
            {
                return;
            }

            renderer.enabled = enabled;
            renderer.transform.localPosition = SanitizePosition(localPosition, Vector3.zero);
            renderer.transform.localScale = SanitizeScale(localScale, new Vector3(0.14f, 0.04f, 1f));
            renderer.transform.localRotation = Quaternion.Euler(0f, 0f, rotationZ);
        }

        private static void UpdateMarker(SpriteRenderer renderer, Vector3 baseScale, Color color, float pulse, float emphasis)
        {
            if (renderer == null || !renderer.enabled)
            {
                return;
            }

            renderer.transform.localScale = SanitizeScale(baseScale * Mathf.Lerp(0.94f, 1.18f, pulse), baseScale);
            renderer.color = new Color(color.r, color.g, color.b, Mathf.Lerp(0.44f, 0.8f, pulse) * emphasis);
        }

        private static float Sanitize01(float value)
        {
            return IsFinite(value) ? Mathf.Clamp01(value) : 0f;
        }

        private static float SanitizePositive(float value, float fallback, float min, float max)
        {
            if (!IsFinite(value))
            {
                return fallback;
            }

            return Mathf.Clamp(value, min, max);
        }

        private static float SanitizeSigned(float value, float fallback, float maxMagnitude)
        {
            if (!IsFinite(value))
            {
                return fallback;
            }

            return Mathf.Clamp(value, -maxMagnitude, maxMagnitude);
        }

        private static Vector3 SanitizePosition(Vector3 value, Vector3 fallback)
        {
            if (!IsFinite(value))
            {
                return fallback;
            }

            return new Vector3(
                Mathf.Clamp(value.x, -32f, 32f),
                Mathf.Clamp(value.y, -32f, 32f),
                Mathf.Clamp(value.z, -32f, 32f));
        }

        private static Vector3 SanitizeScale(Vector3 value, Vector3 fallback)
        {
            Vector3 safeFallback = IsFinite(fallback)
                ? new Vector3(
                    Mathf.Clamp(Mathf.Abs(fallback.x), 0.01f, 8f),
                    Mathf.Clamp(Mathf.Abs(fallback.y), 0.01f, 8f),
                    1f)
                : Vector3.one;

            if (!IsFinite(value))
            {
                return safeFallback;
            }

            return new Vector3(
                Mathf.Clamp(Mathf.Abs(value.x), 0.01f, 8f),
                Mathf.Clamp(Mathf.Abs(value.y), 0.01f, 8f),
                1f);
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private static bool IsFinite(Vector3 value)
        {
            return IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);
        }

        private void HideImmediate()
        {
            _visibility = 0f;
            _commitPulse = 0f;
            _resolutionPulse = 0f;
            _resolutionColor = Color.white;

            if (_root != null)
            {
                _root.gameObject.SetActive(false);
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
                name = "RuntimeRoomInteractionAffordanceCircle"
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
