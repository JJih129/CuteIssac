using CuteIssac.Data.Dungeon;
using CuteIssac.Dungeon;
using UnityEngine;

namespace CuteIssac.Room
{
    /// <summary>
    /// Runtime-only room arrival pulse that can later be replaced by authored VFX without changing navigation flow.
    /// It gives each room type a readable landing signature on first arrival and a lighter re-entry beat afterward.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RoomController))]
    public sealed class RoomArrivalCuePresentation : MonoBehaviour
    {
        [Header("Timing")]
        [SerializeField] [Min(0.1f)] private float baseDuration = 0.48f;
        [SerializeField] [Min(0.1f)] private float guidedBonusDuration = 0.12f;

        [Header("Layout")]
        [SerializeField] [Min(0f)] private float ringRadius = 1.68f;
        [SerializeField] [Min(0f)] private float trailLength = 1.12f;
        [SerializeField] [Min(0f)] private float glyphScale = 0.42f;

        private static Sprite s_WhiteSprite;
        private static Sprite s_CircleSprite;

        private RoomController _roomController;
        private Transform _cueRoot;
        private SpriteRenderer _ringRenderer;
        private SpriteRenderer _coreRenderer;
        private SpriteRenderer _trailRenderer;
        private SpriteRenderer _glyphA;
        private SpriteRenderer _glyphB;
        private SpriteRenderer _glyphC;
        private Vector3 _ringBaseScale = Vector3.one;
        private Vector3 _coreBaseScale = Vector3.one;
        private Vector3 _trailBaseScale = Vector3.one;
        private Vector3 _glyphABaseScale = Vector3.one;
        private Vector3 _glyphBBaseScale = Vector3.one;
        private Vector3 _glyphCBaseScale = Vector3.one;
        private Color _accentColor = Color.white;
        private RoomType _activeRoomType = RoomType.Normal;
        private float _startedAt = float.NegativeInfinity;
        private float _activeDuration;
        private float _roomWeight;
        private float _intensity = 1f;

        private void Awake()
        {
            ResolveRoom();
            EnsureVisuals();
            HideImmediate();
        }

        private void OnEnable()
        {
            ResolveRoom();
            EnsureVisuals();
        }

        private void Update()
        {
            if (_cueRoot == null || _activeDuration <= 0f)
            {
                return;
            }

            float elapsed = Time.unscaledTime - _startedAt;
            if (elapsed < 0f || elapsed > _activeDuration)
            {
                HideImmediate();
                return;
            }

            float normalized = Mathf.Clamp01(elapsed / _activeDuration);
            float pulse = Mathf.Sin(normalized * Mathf.PI);
            float fade = 1f - normalized;
            float resolvedIntensity = _intensity * (1f + (_roomWeight * 0.24f));
            Color brightAccent = Color.Lerp(_accentColor, Color.white, 0.34f);
            Vector3 cueLocalPosition = ResolveCueLocalPosition();
            _cueRoot.localPosition = cueLocalPosition;

            _ringRenderer.transform.localScale = _ringBaseScale * Mathf.Lerp(0.56f, 1.18f * resolvedIntensity, normalized);
            _ringRenderer.color = new Color(_accentColor.r, _accentColor.g, _accentColor.b, Mathf.Lerp(0.52f, 0f, normalized));

            _coreRenderer.transform.localScale = _coreBaseScale * Mathf.Lerp(0.72f, 1.34f, pulse);
            _coreRenderer.color = new Color(brightAccent.r, brightAccent.g, brightAccent.b, Mathf.Lerp(0.76f, 0f, normalized));

            _trailRenderer.transform.localScale = new Vector3(
                _trailBaseScale.x * Mathf.Lerp(0.82f, 1.18f, pulse),
                _trailBaseScale.y * Mathf.Lerp(1.22f * resolvedIntensity, 0.18f, normalized),
                1f);
            _trailRenderer.transform.localPosition = new Vector3(0f, -Mathf.Lerp(0.08f, trailLength * 0.32f, fade), 0f);
            _trailRenderer.color = new Color(brightAccent.r, brightAccent.g, brightAccent.b, Mathf.Lerp(0.32f, 0f, normalized));

            UpdateGlyphRenderer(_glyphA, _glyphABaseScale, pulse, fade, brightAccent);
            UpdateGlyphRenderer(_glyphB, _glyphBBaseScale, pulse, fade, brightAccent);
            UpdateGlyphRenderer(_glyphC, _glyphCBaseScale, pulse, fade, brightAccent);
        }

        public void PlayCue(RoomType roomType, Color accentColor, RoomDirection arrivalDirection, bool guided, float intensity = 1f)
        {
            EnsureVisuals();
            _activeRoomType = roomType;
            _accentColor = ResolveAccent(accentColor);
            _roomWeight = ResolveRoomWeight(roomType);
            _intensity = Mathf.Max(0.65f, intensity);
            _activeDuration = baseDuration + (_roomWeight * 0.14f) + (guided ? guidedBonusDuration : 0f);
            _startedAt = Time.unscaledTime;
            _cueRoot.localPosition = ResolveCueLocalPosition();
            _trailRenderer.transform.localRotation = Quaternion.Euler(0f, 0f, ResolveDirectionRotation(ResolveInwardDirection(arrivalDirection)));
            ApplyGlyphLayout(roomType);
            _cueRoot.gameObject.SetActive(true);
        }

        private void ResolveRoom()
        {
            if (_roomController == null)
            {
                _roomController = GetComponent<RoomController>();
            }
        }

        private void EnsureVisuals()
        {
            if (_cueRoot != null)
            {
                return;
            }

            ResolveRoom();

            GameObject rootObject = new("RoomArrivalCue");
            rootObject.layer = gameObject.layer;
            _cueRoot = rootObject.transform;
            _cueRoot.SetParent(transform, false);
            _cueRoot.localPosition = ResolveCueLocalPosition();
            _cueRoot.localRotation = Quaternion.identity;
            _cueRoot.localScale = Vector3.one;

            _ringRenderer = CreatePart("Ring", GetCircleSprite(), Vector3.zero, new Vector3(ringRadius, ringRadius, 1f), new Color(1f, 1f, 1f, 0f), 38);
            _coreRenderer = CreatePart("Core", GetCircleSprite(), Vector3.zero, new Vector3(0.22f, 0.22f, 1f), new Color(1f, 1f, 1f, 0f), 41);
            _trailRenderer = CreatePart("Trail", GetWhiteSprite(), new Vector3(0f, -0.18f, 0f), new Vector3(0.12f, trailLength, 1f), new Color(1f, 1f, 1f, 0f), 37);
            _glyphA = CreatePart("GlyphA", GetWhiteSprite(), Vector3.zero, Vector3.one * glyphScale, new Color(1f, 1f, 1f, 0f), 40);
            _glyphB = CreatePart("GlyphB", GetWhiteSprite(), Vector3.zero, Vector3.one * glyphScale, new Color(1f, 1f, 1f, 0f), 40);
            _glyphC = CreatePart("GlyphC", GetWhiteSprite(), Vector3.zero, Vector3.one * glyphScale, new Color(1f, 1f, 1f, 0f), 40);

            _ringBaseScale = _ringRenderer.transform.localScale;
            _coreBaseScale = _coreRenderer.transform.localScale;
            _trailBaseScale = _trailRenderer.transform.localScale;
            _glyphABaseScale = _glyphA.transform.localScale;
            _glyphBBaseScale = _glyphB.transform.localScale;
            _glyphCBaseScale = _glyphC.transform.localScale;
            HideImmediate();
        }

        private SpriteRenderer CreatePart(string name, Sprite sprite, Vector3 localPosition, Vector3 localScale, Color color, int sortingOrder)
        {
            GameObject child = new(name);
            child.layer = gameObject.layer;
            child.transform.SetParent(_cueRoot, false);
            child.transform.localPosition = localPosition;
            child.transform.localRotation = Quaternion.identity;
            child.transform.localScale = localScale;

            SpriteRenderer renderer = child.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
            return renderer;
        }

        private void ApplyGlyphLayout(RoomType roomType)
        {
            ConfigureGlyph(_glyphA, Vector3.zero, Vector3.zero, false);
            ConfigureGlyph(_glyphB, Vector3.zero, Vector3.zero, false);
            ConfigureGlyph(_glyphC, Vector3.zero, Vector3.zero, false);

            switch (roomType)
            {
                case RoomType.Treasure:
                    ConfigureGlyph(_glyphA, Vector3.zero, new Vector3(glyphScale * 0.44f, glyphScale * 0.44f, 1f), true, 45f);
                    ConfigureGlyph(_glyphB, new Vector3(0f, glyphScale * 0.38f, 0f), new Vector3(glyphScale * 0.12f, glyphScale * 0.52f, 1f), true);
                    ConfigureGlyph(_glyphC, new Vector3(0f, glyphScale * 0.18f, 0f), new Vector3(glyphScale * 0.52f, glyphScale * 0.12f, 1f), true);
                    break;
                case RoomType.Shop:
                    ConfigureGlyph(_glyphA, new Vector3(-glyphScale * 0.18f, 0f, 0f), new Vector3(glyphScale * 0.14f, glyphScale * 0.64f, 1f), true);
                    ConfigureGlyph(_glyphB, new Vector3(glyphScale * 0.18f, 0f, 0f), new Vector3(glyphScale * 0.14f, glyphScale * 0.64f, 1f), true);
                    ConfigureGlyph(_glyphC, Vector3.zero, new Vector3(glyphScale * 0.56f, glyphScale * 0.14f, 1f), true);
                    break;
                case RoomType.Boss:
                    ConfigureGlyph(_glyphA, Vector3.zero, new Vector3(glyphScale * 0.16f, glyphScale * 0.72f, 1f), true);
                    ConfigureGlyph(_glyphB, Vector3.zero, new Vector3(glyphScale * 0.6f, glyphScale * 0.14f, 1f), true);
                    ConfigureGlyph(_glyphC, new Vector3(0f, glyphScale * 0.32f, 0f), new Vector3(glyphScale * 0.36f, glyphScale * 0.12f, 1f), true, 45f);
                    break;
                case RoomType.Secret:
                    ConfigureGlyph(_glyphA, new Vector3(-glyphScale * 0.14f, 0f, 0f), new Vector3(glyphScale * 0.14f, glyphScale * 0.64f, 1f), true, 28f);
                    ConfigureGlyph(_glyphB, new Vector3(glyphScale * 0.14f, 0f, 0f), new Vector3(glyphScale * 0.14f, glyphScale * 0.64f, 1f), true, -28f);
                    ConfigureGlyph(_glyphC, Vector3.zero, new Vector3(glyphScale * 0.18f, glyphScale * 0.18f, 1f), true, 45f);
                    break;
                case RoomType.Challenge:
                    ConfigureGlyph(_glyphA, Vector3.zero, new Vector3(glyphScale * 0.16f, glyphScale * 0.72f, 1f), true);
                    ConfigureGlyph(_glyphB, Vector3.zero, new Vector3(glyphScale * 0.72f, glyphScale * 0.16f, 1f), true);
                    ConfigureGlyph(_glyphC, Vector3.zero, new Vector3(glyphScale * 0.56f, glyphScale * 0.12f, 1f), true, 45f);
                    break;
                case RoomType.MiniBoss:
                    ConfigureGlyph(_glyphA, new Vector3(-glyphScale * 0.08f, glyphScale * 0.08f, 0f), new Vector3(glyphScale * 0.42f, glyphScale * 0.12f, 1f), true, 40f);
                    ConfigureGlyph(_glyphB, new Vector3(glyphScale * 0.08f, glyphScale * 0.08f, 0f), new Vector3(glyphScale * 0.42f, glyphScale * 0.12f, 1f), true, -40f);
                    ConfigureGlyph(_glyphC, new Vector3(0f, -glyphScale * 0.18f, 0f), new Vector3(glyphScale * 0.18f, glyphScale * 0.48f, 1f), true);
                    break;
                case RoomType.Trap:
                    ConfigureGlyph(_glyphA, Vector3.zero, new Vector3(glyphScale * 0.16f, glyphScale * 0.72f, 1f), true, 45f);
                    ConfigureGlyph(_glyphB, Vector3.zero, new Vector3(glyphScale * 0.16f, glyphScale * 0.72f, 1f), true, -45f);
                    ConfigureGlyph(_glyphC, Vector3.zero, new Vector3(glyphScale * 0.18f, glyphScale * 0.18f, 1f), true);
                    break;
                case RoomType.Curse:
                    ConfigureGlyph(_glyphA, new Vector3(0f, glyphScale * 0.08f, 0f), new Vector3(glyphScale * 0.18f, glyphScale * 0.72f, 1f), true);
                    ConfigureGlyph(_glyphB, new Vector3(-glyphScale * 0.12f, -glyphScale * 0.12f, 0f), new Vector3(glyphScale * 0.38f, glyphScale * 0.12f, 1f), true, 32f);
                    ConfigureGlyph(_glyphC, new Vector3(glyphScale * 0.12f, -glyphScale * 0.22f, 0f), new Vector3(glyphScale * 0.2f, glyphScale * 0.2f, 1f), true, 45f);
                    break;
                case RoomType.Start:
                    ConfigureGlyph(_glyphA, new Vector3(0f, glyphScale * 0.06f, 0f), new Vector3(glyphScale * 0.16f, glyphScale * 0.56f, 1f), true);
                    ConfigureGlyph(_glyphB, new Vector3(-glyphScale * 0.12f, glyphScale * 0.16f, 0f), new Vector3(glyphScale * 0.32f, glyphScale * 0.12f, 1f), true, 32f);
                    ConfigureGlyph(_glyphC, new Vector3(glyphScale * 0.12f, glyphScale * 0.16f, 0f), new Vector3(glyphScale * 0.32f, glyphScale * 0.12f, 1f), true, -32f);
                    break;
                default:
                    ConfigureGlyph(_glyphA, Vector3.zero, new Vector3(glyphScale * 0.22f, glyphScale * 0.22f, 1f), true, 45f);
                    break;
            }
        }

        private static void ConfigureGlyph(SpriteRenderer renderer, Vector3 localPosition, Vector3 localScale, bool enabled, float rotationZ = 0f)
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

        private static void UpdateGlyphRenderer(SpriteRenderer renderer, Vector3 baseScale, float pulse, float fade, Color brightAccent)
        {
            if (renderer == null || !renderer.enabled)
            {
                return;
            }

            renderer.transform.localScale = baseScale * Mathf.Lerp(0.94f, 1.16f, pulse);
            renderer.color = new Color(brightAccent.r, brightAccent.g, brightAccent.b, Mathf.Lerp(0.9f, 0f, 1f - fade));
        }

        private Vector3 ResolveCueLocalPosition()
        {
            if (_roomController == null)
            {
                return Vector3.zero;
            }

            return transform.InverseTransformPoint(_roomController.CameraFocusPosition);
        }

        private void HideImmediate()
        {
            _activeDuration = 0f;

            if (_cueRoot != null)
            {
                _cueRoot.gameObject.SetActive(false);
            }
        }

        private static float ResolveRoomWeight(RoomType roomType)
        {
            return roomType switch
            {
                RoomType.Boss => 1f,
                RoomType.Treasure => 0.82f,
                RoomType.Secret => 0.78f,
                RoomType.MiniBoss => 0.76f,
                RoomType.Challenge => 0.72f,
                RoomType.Curse => 0.62f,
                RoomType.Trap => 0.56f,
                RoomType.Shop => 0.48f,
                RoomType.Start => 0.24f,
                _ => 0.12f
            };
        }

        private static RoomDirection ResolveInwardDirection(RoomDirection doorDirection)
        {
            return doorDirection switch
            {
                RoomDirection.Up => RoomDirection.Down,
                RoomDirection.Right => RoomDirection.Left,
                RoomDirection.Down => RoomDirection.Up,
                RoomDirection.Left => RoomDirection.Right,
                _ => RoomDirection.Down
            };
        }

        private static float ResolveDirectionRotation(RoomDirection direction)
        {
            return direction switch
            {
                RoomDirection.Up => 0f,
                RoomDirection.Right => -90f,
                RoomDirection.Down => 180f,
                RoomDirection.Left => 90f,
                _ => 0f
            };
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
                name = "RuntimeRoomArrivalCueCircle"
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
