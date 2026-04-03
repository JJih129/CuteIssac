using CuteIssac.Data.Dungeon;
using UnityEngine;

namespace CuteIssac.Room
{
    /// <summary>
    /// Runtime-only ranking chip for multi-choice room targets.
    /// Treasure and shop options can be upgraded independently from the active affordance layer.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RoomChoiceRecommendationPresentation : MonoBehaviour
    {
        [Header("Layout")]
        [SerializeField] private Vector3 rootOffset = new(0f, 0.84f, 0f);
        [SerializeField] [Min(0.01f)] private float bobAmplitude = 0.025f;
        [SerializeField] [Min(0.05f)] private float bobFrequency = 2.2f;
        [SerializeField] [Min(0.05f)] private float pulseFrequency = 3.8f;

        private static Sprite s_WhiteSprite;
        private static Sprite s_CircleSprite;

        private Transform _root;
        private SpriteRenderer _pulseRenderer;
        private SpriteRenderer _laneRenderer;
        private SpriteRenderer _chipRenderer;
        private SpriteRenderer _accentRenderer;
        private SpriteRenderer _markerA;
        private SpriteRenderer _markerB;
        private TextMesh _labelText;
        private Vector3 _baseLocalPosition;
        private Vector3 _pulseBaseScale = Vector3.one;
        private Vector3 _laneBaseScale = Vector3.one;
        private Vector3 _chipBaseScale = Vector3.one;
        private Vector3 _accentBaseScale = Vector3.one;
        private Vector3 _markerABaseScale = Vector3.one;
        private Vector3 _markerBBaseScale = Vector3.one;
        private RoomType _roomType = RoomType.Normal;
        private Color _accentColor = Color.white;
        private string _label = string.Empty;
        private bool _active;
        private bool _primary;
        private bool _blocked;
        private bool _routeSynced;
        private bool _focused;
        private bool _commitReady;
        private bool _suppressed;
        private float _laneStrength;
        private float _suppressionStrength;
        private float _visibility;

        private void Awake()
        {
            EnsureVisuals();
            HideImmediate();
        }

        private void OnEnable()
        {
            EnsureVisuals();
        }

        private void OnDisable()
        {
            HideImmediate();
        }

        private void Update()
        {
            if (_root == null)
            {
                return;
            }

            float targetVisibility = _active
                ? Mathf.Lerp(1f, 0.46f, _suppressionStrength)
                : 0f;
            _visibility = Mathf.MoveTowards(_visibility, targetVisibility, Time.unscaledDeltaTime * (_active ? 7f : 9f));

            if (_visibility <= 0.001f)
            {
                HideImmediate();
                return;
            }

            if (!_root.gameObject.activeSelf)
            {
                _root.gameObject.SetActive(true);
            }

            float pulse = 0.5f + (Mathf.Sin(Time.unscaledTime * pulseFrequency) * 0.5f);
            float bob = Mathf.Sin(Time.unscaledTime * bobFrequency) * bobAmplitude;
            float emphasis = (_primary ? 1f : 0.78f)
                + (_routeSynced ? 0.18f : 0f)
                + (_focused ? 0.12f : 0f)
                + (_commitReady ? 0.18f : 0f)
                + (_laneStrength * 0.2f);
            Color accent = ResolveLiveAccent();
            Color brightAccent = Color.Lerp(
                accent,
                Color.white,
                (_primary ? 0.3f : 0.16f)
                + (_routeSynced ? 0.08f : 0f)
                + (_focused ? 0.08f : 0f)
                + (_commitReady ? 0.12f : 0f)
                + (_laneStrength * 0.08f));
            float pulseScaleBoost = (_routeSynced ? 0.08f : 0f) + (_laneStrength * 0.06f);
            float focusScaleBoost = _focused ? 0.08f : 0f;
            float commitScaleBoost = _commitReady ? 0.14f : 0f;
            float suppressionScale = Mathf.Lerp(1f, 0.84f, _suppressionStrength);
            float suppressDrop = _suppressionStrength * 0.045f;

            _root.localPosition = _baseLocalPosition + new Vector3(0f, bob - suppressDrop, 0f);

            _pulseRenderer.transform.localScale = _pulseBaseScale * (Mathf.Lerp(0.94f, (_primary ? 1.18f : 1.08f) + pulseScaleBoost + focusScaleBoost + commitScaleBoost, pulse) * suppressionScale);
            _pulseRenderer.color = new Color(accent.r, accent.g, accent.b, _visibility * Mathf.Lerp(_primary ? 0.16f : 0.08f, (_primary ? 0.28f : 0.14f) + (_routeSynced ? 0.08f : 0f) + (_focused ? 0.08f : 0f) + (_commitReady ? 0.12f : 0f), pulse));

            bool showLane = _routeSynced && !_blocked;
            _laneRenderer.enabled = showLane;
            if (showLane)
            {
                _laneRenderer.transform.localScale = Vector3.Scale(_laneBaseScale, new Vector3(
                    Mathf.Lerp(0.94f, 1.08f + (_focused ? 0.08f : 0f) + (_commitReady ? 0.16f : 0f) + (_laneStrength * 0.16f), pulse) * suppressionScale,
                    Mathf.Lerp(0.88f, 1.12f + (_commitReady ? 0.18f : 0f) + (_laneStrength * 0.18f), pulse),
                    1f));
                _laneRenderer.color = new Color(
                    brightAccent.r,
                    brightAccent.g,
                    brightAccent.b,
                    _visibility * (Mathf.Lerp(0.14f, 0.08f, _suppressionStrength) + (_focused ? 0.08f : 0f) + (_commitReady ? 0.12f : 0f) + (_laneStrength * 0.1f)));
            }

            _chipRenderer.transform.localScale = _chipBaseScale * (Mathf.Lerp(0.98f, (_primary ? 1.06f : 1.03f) + (focusScaleBoost * 0.8f) + (commitScaleBoost * 0.85f), pulse) * suppressionScale);
            _chipRenderer.color = new Color(accent.r, accent.g, accent.b, _visibility * (_blocked ? 0.18f : _primary ? 0.28f : 0.2f) * emphasis);

            _accentRenderer.transform.localScale = _accentBaseScale * (Mathf.Lerp(0.94f, (_primary ? 1.14f : 1.06f) + pulseScaleBoost + (focusScaleBoost * 0.6f) + (commitScaleBoost * 0.75f), pulse) * suppressionScale);
            _accentRenderer.color = new Color(brightAccent.r, brightAccent.g, brightAccent.b, _visibility * ((_primary ? 0.9f : 0.72f) + (_routeSynced ? 0.08f : 0f) + (_focused ? 0.08f : 0f)));

            UpdateMarker(_markerA, _markerABaseScale, brightAccent, pulse, emphasis);
            UpdateMarker(_markerB, _markerBBaseScale, brightAccent, pulse, emphasis);
            ApplyTextState(brightAccent);
        }

        public void SetRecommendation(RoomType roomType, Color accentColor, string label, bool primary, bool blocked, bool routeSynced = false, bool focused = false, bool commitReady = false, bool suppressed = false, float laneStrength = 0f, float suppressionStrength = 0f)
        {
            EnsureVisuals();
            _roomType = roomType;
            _accentColor = NormalizeAccent(accentColor, blocked);
            _label = string.IsNullOrWhiteSpace(label)
                ? blocked ? "LOCKED" : primary ? "BEST PICK" : "ALT PICK"
                : label;
            _primary = primary;
            _blocked = blocked;
            _routeSynced = routeSynced;
            _focused = focused;
            _commitReady = commitReady;
            _suppressed = suppressed;
            _laneStrength = Mathf.Clamp01(routeSynced && !blocked ? laneStrength : 0f);
            _suppressionStrength = Mathf.Clamp01(suppressed ? suppressionStrength : 0f);
            _active = true;

            _labelText.text = _label;
            _labelText.gameObject.SetActive(true);

            ApplyLayout();
            if (!_root.gameObject.activeSelf)
            {
                _root.gameObject.SetActive(true);
            }
        }

        public void ClearRecommendation()
        {
            _active = false;
        }

        private void EnsureVisuals()
        {
            if (_root != null)
            {
                return;
            }

            GameObject rootObject = new("RoomChoiceRecommendation");
            rootObject.layer = gameObject.layer;
            _root = rootObject.transform;
            _root.SetParent(transform, false);
            _baseLocalPosition = rootOffset;
            _root.localPosition = _baseLocalPosition;

            _pulseRenderer = CreateSpritePart(
                "Pulse",
                GetCircleSprite(),
                new Vector3(0f, -0.02f, 0f),
                new Vector3(1.12f, 0.54f, 1f),
                new Color(1f, 1f, 1f, 0f),
                64);
            _laneRenderer = CreateSpritePart(
                "Lane",
                GetWhiteSprite(),
                new Vector3(0f, -0.06f, 0f),
                new Vector3(1.28f, 0.05f, 1f),
                new Color(1f, 1f, 1f, 0f),
                65);
            _chipRenderer = CreateSpritePart(
                "Chip",
                GetWhiteSprite(),
                Vector3.zero,
                new Vector3(0.84f, 0.18f, 1f),
                new Color(1f, 1f, 1f, 0f),
                66);
            _accentRenderer = CreateSpritePart(
                "Accent",
                GetWhiteSprite(),
                new Vector3(-0.34f, 0f, 0f),
                new Vector3(0.05f, 0.24f, 1f),
                new Color(1f, 1f, 1f, 0f),
                67);
            _markerA = CreateSpritePart(
                "MarkerA",
                GetWhiteSprite(),
                new Vector3(-0.48f, 0.18f, 0f),
                new Vector3(0.14f, 0.04f, 1f),
                new Color(1f, 1f, 1f, 0f),
                68);
            _markerB = CreateSpritePart(
                "MarkerB",
                GetWhiteSprite(),
                new Vector3(0.48f, 0.18f, 0f),
                new Vector3(0.14f, 0.04f, 1f),
                new Color(1f, 1f, 1f, 0f),
                68);
            _labelText = CreateTextPart("Label", Vector3.zero, 34, 0.045f, 69);

            _pulseBaseScale = _pulseRenderer.transform.localScale;
            _laneBaseScale = _laneRenderer.transform.localScale;
            _chipBaseScale = _chipRenderer.transform.localScale;
            _accentBaseScale = _accentRenderer.transform.localScale;
            _markerABaseScale = _markerA.transform.localScale;
            _markerBBaseScale = _markerB.transform.localScale;
            HideImmediate();
        }

        private SpriteRenderer CreateSpritePart(
            string objectName,
            Sprite sprite,
            Vector3 localPosition,
            Vector3 localScale,
            Color color,
            int sortingOrder)
        {
            GameObject child = new(objectName);
            child.layer = gameObject.layer;
            child.transform.SetParent(_root, false);
            child.transform.localPosition = localPosition;
            child.transform.localScale = localScale;
            child.transform.localRotation = Quaternion.identity;

            SpriteRenderer renderer = child.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
            return renderer;
        }

        private TextMesh CreateTextPart(string objectName, Vector3 localPosition, int fontSize, float characterSize, int sortingOrder)
        {
            GameObject child = new(objectName);
            child.layer = gameObject.layer;
            child.transform.SetParent(_root, false);
            child.transform.localPosition = localPosition;
            child.transform.localScale = Vector3.one;

            TextMesh textMesh = child.AddComponent<TextMesh>();
            textMesh.text = string.Empty;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.fontSize = fontSize;
            textMesh.characterSize = characterSize;
            textMesh.fontStyle = FontStyle.Bold;
            textMesh.richText = false;

            MeshRenderer meshRenderer = textMesh.GetComponent<MeshRenderer>();
            if (meshRenderer != null)
            {
                meshRenderer.sortingOrder = sortingOrder;
            }

            return textMesh;
        }

        private void ApplyLayout()
        {
            float chipWidth = Mathf.Clamp(0.54f + (_label.Length * 0.06f), 0.78f, 1.7f);
            float heightBoost = _commitReady ? 0.08f : _focused ? 0.04f : 0f;
            float widthBoost = (_routeSynced ? 0.08f : 0f) + (_laneStrength * 0.08f);
            _chipRenderer.transform.localScale = new Vector3(chipWidth + widthBoost, (_primary ? 0.24f : 0.2f) + heightBoost, 1f);
            _pulseRenderer.transform.localScale = new Vector3((chipWidth * 1.18f) + (widthBoost * 1.1f), (_primary ? 0.62f : 0.5f) + (heightBoost * 1.8f), 1f);
            _laneRenderer.transform.localPosition = new Vector3(0f, (-0.06f) - (_commitReady ? 0.02f : 0f), 0f);
            _laneRenderer.transform.localScale = new Vector3((chipWidth * (_routeSynced ? 1.12f + (_laneStrength * 0.1f) : 0.94f)) + widthBoost, (_commitReady ? 0.07f : _focused ? 0.06f : 0.05f) + (_laneStrength * 0.02f), 1f);
            _accentRenderer.transform.localPosition = new Vector3((-chipWidth * 0.5f) + 0.08f, 0f, 0f);
            _accentRenderer.transform.localScale = new Vector3(0.06f, (_primary ? 0.28f : 0.22f) + heightBoost, 1f);

            float edgeOffset = (chipWidth * 0.5f) + 0.08f;
            switch (_roomType)
            {
                case RoomType.Shop:
                case RoomType.Treasure:
                    ConfigureMarker(_markerA, new Vector3(-edgeOffset, 0.18f, 0f), new Vector3(0.16f, 0.04f, 1f), 18f, true);
                    ConfigureMarker(_markerB, new Vector3(edgeOffset, 0.18f, 0f), new Vector3(0.16f, 0.04f, 1f), -18f, true);
                    break;
                default:
                    ConfigureMarker(_markerA, new Vector3(-edgeOffset, 0.16f, 0f), new Vector3(0.14f, 0.04f, 1f), 0f, true);
                    ConfigureMarker(_markerB, new Vector3(edgeOffset, 0.16f, 0f), new Vector3(0.14f, 0.04f, 1f), 0f, true);
                    break;
            }

            _chipBaseScale = _chipRenderer.transform.localScale;
            _pulseBaseScale = _pulseRenderer.transform.localScale;
            _laneBaseScale = _laneRenderer.transform.localScale;
            _accentBaseScale = _accentRenderer.transform.localScale;
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

        private void ApplyTextState(Color accentColor)
        {
            if (_labelText == null || !_labelText.gameObject.activeSelf)
            {
                return;
            }

            float alpha = _visibility * ((_primary ? 0.98f : 0.84f) + (_focused ? 0.08f : 0f) + (_commitReady ? 0.08f : 0f));
            alpha *= Mathf.Lerp(1f, 0.74f, _suppressionStrength);
            Color textColor = _blocked
                ? Color.Lerp(accentColor, Color.white, 0.24f)
                : Color.Lerp(accentColor, Color.white, 0.42f + (_routeSynced ? 0.08f : 0f) + (_commitReady ? 0.08f : 0f) + (_laneStrength * 0.06f));
            if (_suppressed)
            {
                textColor = Color.Lerp(textColor, new Color(0.72f, 0.78f, 0.86f, 1f), Mathf.Lerp(0.26f, 0.4f, _suppressionStrength));
            }
            _labelText.color = new Color(textColor.r, textColor.g, textColor.b, alpha);

            MeshRenderer meshRenderer = _labelText.GetComponent<MeshRenderer>();
            if (meshRenderer != null)
            {
                meshRenderer.enabled = alpha > 0.01f;
            }
        }

        private static void UpdateMarker(SpriteRenderer renderer, Vector3 baseScale, Color color, float pulse, float emphasis)
        {
            if (renderer == null || !renderer.enabled)
            {
                return;
            }

            renderer.transform.localScale = baseScale * Mathf.Lerp(0.94f, 1.18f, pulse);
            renderer.color = new Color(color.r, color.g, color.b, Mathf.Lerp(0.26f, 0.58f, pulse) * emphasis);
        }

        private Color ResolveLiveAccent()
        {
            if (_blocked)
            {
                return Color.Lerp(_accentColor, new Color(1f, 0.44f, 0.42f, 1f), 0.38f);
            }

            return _routeSynced
                ? Color.Lerp(_accentColor, Color.white, _commitReady ? 0.22f : _focused ? 0.14f : 0.08f)
                : _accentColor;
        }

        private static Color NormalizeAccent(Color accentColor, bool blocked)
        {
            Color color = accentColor.a > 0.01f
                ? new Color(accentColor.r, accentColor.g, accentColor.b, 1f)
                : new Color(0.66f, 0.92f, 1f, 1f);

            return blocked
                ? Color.Lerp(color, new Color(1f, 0.52f, 0.44f, 1f), 0.26f)
                : color;
        }

        private void HideImmediate()
        {
            _visibility = 0f;
            _suppressed = false;
            _laneStrength = 0f;
            _suppressionStrength = 0f;

            if (_root != null)
            {
                _root.gameObject.SetActive(false);
            }
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
                name = "RuntimeRoomChoiceRecommendationCircle"
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
