using CuteIssac.Data.Dungeon;
using CuteIssac.Dungeon;
using UnityEngine;

namespace CuteIssac.Room
{
    /// <summary>
    /// Runtime-only focus beat that points at the first meaningful interaction or pressure anchor after room arrival.
    /// It keeps entry direction readable even before authored room-type VFX are available.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RoomController))]
    public sealed class RoomEntryFocusBeatPresentation : MonoBehaviour
    {
        [Header("Timing")]
        [SerializeField] [Min(0.15f)] private float baseDuration = 0.92f;
        [SerializeField] [Min(0f)] private float guidedBonusDuration = 0.16f;

        [Header("Layout")]
        [SerializeField] [Min(0.4f)] private float defaultFocusRadius = 1.08f;
        [SerializeField] [Min(0.2f)] private float sweepLength = 1.34f;
        [SerializeField] [Min(0.1f)] private float guideLineMinLength = 0.42f;
        [SerializeField] [Min(0.1f)] private float guideLineMaxLength = 1.46f;
        [SerializeField] [Min(0.05f)] private float shardScale = 0.2f;

        private static Sprite s_WhiteSprite;
        private static Sprite s_CircleSprite;

        private RoomController _roomController;
        private Transform _focusRoot;
        private SpriteRenderer _ringRenderer;
        private SpriteRenderer _haloRenderer;
        private SpriteRenderer _coreRenderer;
        private SpriteRenderer _sweepRenderer;
        private SpriteRenderer _guideLineRenderer;
        private SpriteRenderer _shardA;
        private SpriteRenderer _shardB;
        private Vector3 _ringBaseScale = Vector3.one;
        private Vector3 _haloBaseScale = Vector3.one;
        private Vector3 _coreBaseScale = Vector3.one;
        private Vector3 _sweepBaseScale = Vector3.one;
        private Vector3 _guideLineBaseScale = Vector3.one;
        private Vector3 _shardABaseScale = Vector3.one;
        private Vector3 _shardBBaseScale = Vector3.one;
        private Vector3 _focusWorldPosition;
        private Color _accentColor = Color.white;
        private RoomType _roomType = RoomType.Normal;
        private float _startedAt = float.NegativeInfinity;
        private float _activeDuration;
        private float _focusRadius = 1f;
        private float _intensity = 1f;
        private float _roomWeight;
        private RoomDirection _arrivalDirection = RoomDirection.Down;

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
            if (_focusRoot == null || _activeDuration <= 0f)
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
            float resolvedRadius = Mathf.Max(defaultFocusRadius, _focusRadius) * Mathf.Lerp(0.96f, 1.14f, _intensity * 0.35f);
            Color brightAccent = Color.Lerp(_accentColor, Color.white, 0.36f);
            _focusRoot.localPosition = transform.InverseTransformPoint(_focusWorldPosition);

            _ringRenderer.transform.localScale = _ringBaseScale * resolvedRadius * Mathf.Lerp(0.76f, 1.22f, normalized);
            _ringRenderer.color = new Color(_accentColor.r, _accentColor.g, _accentColor.b, Mathf.Lerp(0.56f, 0f, normalized));

            _haloRenderer.transform.localScale = _haloBaseScale * resolvedRadius * Mathf.Lerp(0.34f, 1.08f, normalized);
            _haloRenderer.color = new Color(brightAccent.r, brightAccent.g, brightAccent.b, Mathf.Lerp(0.26f, 0f, normalized));

            _coreRenderer.transform.localScale = _coreBaseScale * Mathf.Lerp(0.88f, 1.34f, pulse);
            _coreRenderer.color = new Color(brightAccent.r, brightAccent.g, brightAccent.b, Mathf.Lerp(0.84f, 0f, normalized));

            float sweepStrength = Mathf.Lerp(1.24f + (_roomWeight * 0.32f), 0.14f, normalized);
            Vector2 inwardVector = ResolveDirectionVector(ResolveInwardDirection(_arrivalDirection));
            _sweepRenderer.transform.localRotation = Quaternion.Euler(0f, 0f, ResolveDirectionRotation(ResolveInwardDirection(_arrivalDirection)));
            _sweepRenderer.transform.localPosition = new Vector3(-inwardVector.x, -inwardVector.y, 0f) * (resolvedRadius * 0.18f);
            _sweepRenderer.transform.localScale = new Vector3(
                _sweepBaseScale.x * Mathf.Lerp(0.92f, 1.16f, pulse),
                _sweepBaseScale.y * sweepStrength,
                1f);
            _sweepRenderer.color = new Color(brightAccent.r, brightAccent.g, brightAccent.b, Mathf.Lerp(0.28f, 0f, normalized));

            UpdateGuideLine(brightAccent, fade, pulse);
            UpdateShard(_shardA, _shardABaseScale, brightAccent, pulse, fade);
            UpdateShard(_shardB, _shardBBaseScale, brightAccent, pulse, fade);
        }

        public void PlayBeat(
            RoomType roomType,
            Vector3 focusWorldPosition,
            Color accentColor,
            RoomDirection arrivalDirection,
            bool guided,
            float intensity = 1f,
            float focusRadius = 1f)
        {
            EnsureVisuals();
            _roomType = roomType;
            _focusWorldPosition = focusWorldPosition;
            _accentColor = ResolveAccent(accentColor);
            _arrivalDirection = arrivalDirection;
            _roomWeight = ResolveRoomWeight(roomType);
            _intensity = Mathf.Max(0.72f, intensity);
            _focusRadius = Mathf.Max(0.72f, focusRadius);
            _activeDuration = baseDuration + (_roomWeight * 0.18f) + (guided ? guidedBonusDuration : 0f);
            _startedAt = Time.unscaledTime;
            _focusRoot.localPosition = transform.InverseTransformPoint(_focusWorldPosition);
            ApplyShardLayout(roomType, _focusRadius);
            _focusRoot.gameObject.SetActive(true);
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
            if (_focusRoot != null)
            {
                return;
            }

            ResolveRoom();

            GameObject rootObject = new("RoomEntryFocusBeat");
            rootObject.layer = gameObject.layer;
            _focusRoot = rootObject.transform;
            _focusRoot.SetParent(transform, false);
            _focusRoot.localPosition = Vector3.zero;
            _focusRoot.localRotation = Quaternion.identity;
            _focusRoot.localScale = Vector3.one;

            _ringRenderer = CreatePart("Ring", GetCircleSprite(), Vector3.zero, Vector3.one, new Color(1f, 1f, 1f, 0f), 46);
            _haloRenderer = CreatePart("Halo", GetCircleSprite(), Vector3.zero, Vector3.one, new Color(1f, 1f, 1f, 0f), 44);
            _coreRenderer = CreatePart("Core", GetCircleSprite(), Vector3.zero, new Vector3(0.18f, 0.18f, 1f), new Color(1f, 1f, 1f, 0f), 48);
            _sweepRenderer = CreatePart("Sweep", GetWhiteSprite(), Vector3.zero, new Vector3(0.14f, sweepLength, 1f), new Color(1f, 1f, 1f, 0f), 43);
            _guideLineRenderer = CreatePart("GuideLine", GetWhiteSprite(), Vector3.zero, new Vector3(0.08f, guideLineMaxLength, 1f), new Color(1f, 1f, 1f, 0f), 42);
            _shardA = CreatePart("ShardA", GetWhiteSprite(), Vector3.zero, Vector3.one * shardScale, new Color(1f, 1f, 1f, 0f), 47);
            _shardB = CreatePart("ShardB", GetWhiteSprite(), Vector3.zero, Vector3.one * shardScale, new Color(1f, 1f, 1f, 0f), 47);

            _ringBaseScale = _ringRenderer.transform.localScale;
            _haloBaseScale = _haloRenderer.transform.localScale;
            _coreBaseScale = _coreRenderer.transform.localScale;
            _sweepBaseScale = _sweepRenderer.transform.localScale;
            _guideLineBaseScale = _guideLineRenderer.transform.localScale;
            _shardABaseScale = _shardA.transform.localScale;
            _shardBBaseScale = _shardB.transform.localScale;
            HideImmediate();
        }

        private SpriteRenderer CreatePart(string name, Sprite sprite, Vector3 localPosition, Vector3 localScale, Color color, int sortingOrder)
        {
            GameObject child = new(name);
            child.layer = gameObject.layer;
            child.transform.SetParent(_focusRoot, false);
            child.transform.localPosition = localPosition;
            child.transform.localRotation = Quaternion.identity;
            child.transform.localScale = localScale;

            SpriteRenderer renderer = child.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
            return renderer;
        }

        private void UpdateGuideLine(Color brightAccent, float fade, float pulse)
        {
            Vector2 guideDirection = ResolveGuideVector();
            float lineLength = ResolveGuideLineLength();
            _guideLineRenderer.transform.localRotation = Quaternion.Euler(0f, 0f, ResolveRotationFromVector(guideDirection));
            _guideLineRenderer.transform.localPosition = new Vector3(guideDirection.x, guideDirection.y, 0f) * (lineLength * 0.5f);
            _guideLineRenderer.transform.localScale = new Vector3(
                _guideLineBaseScale.x * Mathf.Lerp(0.86f, 1.14f, pulse),
                lineLength,
                1f);
            _guideLineRenderer.color = new Color(brightAccent.r, brightAccent.g, brightAccent.b, Mathf.Lerp(0.22f, 0f, 1f - fade));
        }

        private void ApplyShardLayout(RoomType roomType, float focusRadius)
        {
            float offset = Mathf.Max(0.36f, focusRadius * 0.52f);
            switch (roomType)
            {
                case RoomType.Boss:
                case RoomType.Challenge:
                    ConfigureShard(_shardA, new Vector3(-offset, 0f, 0f), new Vector3(shardScale * 0.7f, shardScale * 2f, 1f), 0f, true);
                    ConfigureShard(_shardB, new Vector3(offset, 0f, 0f), new Vector3(shardScale * 0.7f, shardScale * 2f, 1f), 0f, true);
                    break;
                case RoomType.Shop:
                case RoomType.Treasure:
                    ConfigureShard(_shardA, new Vector3(0f, offset, 0f), new Vector3(shardScale * 2f, shardScale * 0.68f, 1f), 0f, true);
                    ConfigureShard(_shardB, new Vector3(0f, -offset, 0f), new Vector3(shardScale * 2f, shardScale * 0.68f, 1f), 0f, true);
                    break;
                case RoomType.Secret:
                case RoomType.Curse:
                case RoomType.Trap:
                    ConfigureShard(_shardA, new Vector3(-offset * 0.72f, offset * 0.72f, 0f), new Vector3(shardScale * 0.82f, shardScale * 1.82f, 1f), 45f, true);
                    ConfigureShard(_shardB, new Vector3(offset * 0.72f, -offset * 0.72f, 0f), new Vector3(shardScale * 0.82f, shardScale * 1.82f, 1f), 45f, true);
                    break;
                case RoomType.MiniBoss:
                    ConfigureShard(_shardA, new Vector3(-offset * 0.82f, offset * 0.18f, 0f), new Vector3(shardScale * 0.82f, shardScale * 1.74f, 1f), 20f, true);
                    ConfigureShard(_shardB, new Vector3(offset * 0.82f, offset * 0.18f, 0f), new Vector3(shardScale * 0.82f, shardScale * 1.74f, 1f), -20f, true);
                    break;
                default:
                    ConfigureShard(_shardA, new Vector3(-offset * 0.66f, 0f, 0f), new Vector3(shardScale * 0.7f, shardScale * 1.6f, 1f), 0f, true);
                    ConfigureShard(_shardB, new Vector3(offset * 0.66f, 0f, 0f), new Vector3(shardScale * 0.7f, shardScale * 1.6f, 1f), 0f, true);
                    break;
            }
        }

        private static void ConfigureShard(SpriteRenderer renderer, Vector3 localPosition, Vector3 localScale, float rotationZ, bool enabled)
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

        private static void UpdateShard(SpriteRenderer renderer, Vector3 baseScale, Color brightAccent, float pulse, float fade)
        {
            if (renderer == null || !renderer.enabled)
            {
                return;
            }

            renderer.transform.localScale = baseScale * Mathf.Lerp(0.92f, 1.18f, pulse);
            renderer.color = new Color(brightAccent.r, brightAccent.g, brightAccent.b, Mathf.Lerp(0.82f, 0f, 1f - fade));
        }

        private Vector2 ResolveGuideVector()
        {
            if (_roomController == null)
            {
                return ResolveDirectionVector(ResolveInwardDirection(_arrivalDirection));
            }

            Vector2 guideVector = _roomController.CameraFocusPosition - _focusWorldPosition;
            return guideVector.sqrMagnitude > 0.01f
                ? guideVector.normalized
                : ResolveDirectionVector(ResolveInwardDirection(_arrivalDirection));
        }

        private float ResolveGuideLineLength()
        {
            if (_roomController == null)
            {
                return guideLineMinLength;
            }

            float roomDistance = Vector3.Distance(_roomController.CameraFocusPosition, _focusWorldPosition);
            return Mathf.Clamp(roomDistance * 0.42f, guideLineMinLength, guideLineMaxLength);
        }

        private void HideImmediate()
        {
            _activeDuration = 0f;

            if (_focusRoot != null)
            {
                _focusRoot.gameObject.SetActive(false);
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
                RoomType.Trap => 0.58f,
                RoomType.Shop => 0.48f,
                _ => 0.24f
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

        private static Vector2 ResolveDirectionVector(RoomDirection direction)
        {
            return direction switch
            {
                RoomDirection.Up => Vector2.up,
                RoomDirection.Right => Vector2.right,
                RoomDirection.Down => Vector2.down,
                RoomDirection.Left => Vector2.left,
                _ => Vector2.up
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

        private static float ResolveRotationFromVector(Vector2 direction)
        {
            if (direction.sqrMagnitude <= 0.0001f)
            {
                return 0f;
            }

            return (Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg) - 90f;
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
                name = "RuntimeRoomEntryFocusCircle"
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
