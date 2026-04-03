using System.Collections.Generic;
using CuteIssac.Data.Dungeon;
using CuteIssac.Data.Room;
using UnityEngine;

namespace CuteIssac.Room
{
    /// <summary>
    /// Runtime-built landmark placeholder for special room types.
    /// Theme data owns palette and prefab overrides, while this class provides a presentation-only fallback until authored art is swapped in.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RoomLandmarkVisual : MonoBehaviour
    {
        [Header("Motion")]
        [SerializeField] [Min(0f)] private float bobAmplitude = 0.05f;
        [SerializeField] [Min(0.1f)] private float bobSpeed = 1.8f;
        [SerializeField] [Min(0f)] private float pulseAmplitude = 0.035f;
        [SerializeField] [Min(0.1f)] private float pulseSpeed = 2.2f;
        [SerializeField] [Min(0f)] private float glowStrength = 0.18f;

        private static Sprite s_WhiteSprite;
        private static Sprite s_CircleSprite;

        private readonly List<SpriteRenderer> _accentRenderers = new();
        private readonly List<Color> _accentBaseColors = new();

        private RoomType _roomType;
        private RoomThemeData _theme;
        private bool _isConfigured;
        private float _phaseOffset;
        private Vector3 _baseLocalPosition;
        private Vector3 _baseLocalScale = Vector3.one;

        public void Configure(RoomType roomType, RoomThemeData theme)
        {
            _roomType = roomType;
            _theme = theme;
            Rebuild();
        }

        private void Awake()
        {
            _phaseOffset = Random.Range(0f, Mathf.PI * 2f);
        }

        private void OnEnable()
        {
            if (_isConfigured)
            {
                CacheBaseTransform();
            }
        }

        private void Update()
        {
            if (!_isConfigured)
            {
                return;
            }

            float bob = Mathf.Sin((Time.time * bobSpeed) + _phaseOffset) * bobAmplitude;
            float pulse = 1f + (Mathf.Sin((Time.time * pulseSpeed) + _phaseOffset) * pulseAmplitude);
            transform.localPosition = _baseLocalPosition + new Vector3(0f, bob, 0f);
            transform.localScale = _baseLocalScale * pulse;

            float glowPulse = 0.5f + (0.5f * Mathf.Sin((Time.time * (pulseSpeed * 1.18f)) + _phaseOffset));

            for (int index = 0; index < _accentRenderers.Count; index++)
            {
                SpriteRenderer renderer = _accentRenderers[index];

                if (renderer == null)
                {
                    continue;
                }

                Color baseColor = index < _accentBaseColors.Count ? _accentBaseColors[index] : renderer.color;
                float highlight = 1f + (glowPulse * glowStrength);
                renderer.color = new Color(
                    Mathf.Clamp01(baseColor.r * highlight),
                    Mathf.Clamp01(baseColor.g * highlight),
                    Mathf.Clamp01(baseColor.b * highlight),
                    baseColor.a);
            }
        }

        private void Rebuild()
        {
            ClearChildren();
            _accentRenderers.Clear();
            _accentBaseColors.Clear();

            ResolvePalette(out Color baseColor, out Color secondaryColor, out Color detailColor, out Color accentColor, out Color shadowColor);

            switch (_roomType)
            {
                case RoomType.Start:
                    BuildStartLandmark(baseColor, secondaryColor, accentColor, shadowColor);
                    break;
                case RoomType.Treasure:
                    BuildTreasureLandmark(baseColor, secondaryColor, detailColor, accentColor, shadowColor);
                    break;
                case RoomType.Shop:
                    BuildShopLandmark(baseColor, secondaryColor, detailColor, accentColor, shadowColor);
                    break;
                case RoomType.Boss:
                    BuildBossLandmark(baseColor, accentColor, shadowColor, 1.15f);
                    break;
                case RoomType.MiniBoss:
                    BuildBossLandmark(baseColor, accentColor, shadowColor, 0.92f);
                    break;
                case RoomType.Secret:
                    BuildSecretLandmark(baseColor, secondaryColor, accentColor, shadowColor);
                    break;
                case RoomType.Challenge:
                    BuildChallengeLandmark(baseColor, secondaryColor, accentColor, shadowColor);
                    break;
                case RoomType.Trap:
                    BuildTrapLandmark(baseColor, detailColor, accentColor, shadowColor);
                    break;
                case RoomType.Curse:
                    BuildCurseLandmark(baseColor, secondaryColor, accentColor, shadowColor);
                    break;
                default:
                    BuildTreasureLandmark(baseColor, secondaryColor, detailColor, accentColor, shadowColor);
                    break;
            }

            CacheBaseTransform();
            _isConfigured = true;
        }

        private void BuildStartLandmark(Color baseColor, Color secondaryColor, Color accentColor, Color shadowColor)
        {
            CreatePart("BaseShadow", GetCircleSprite(), new Vector2(0f, -0.16f), new Vector2(1.76f, 0.42f), shadowColor, 20);
            CreatePart("SigilOuter", GetCircleSprite(), new Vector2(0f, 0.02f), new Vector2(1.34f, 1.34f), baseColor, 21);
            CreateAccentPart("SigilCore", GetCircleSprite(), new Vector2(0f, 0.02f), new Vector2(0.64f, 0.64f), accentColor, 23);
            CreatePart("FlagLeft", GetWhiteSprite(), new Vector2(-0.64f, 0.44f), new Vector2(0.22f, 0.88f), secondaryColor, 22, 8f);
            CreatePart("FlagRight", GetWhiteSprite(), new Vector2(0.64f, 0.44f), new Vector2(0.22f, 0.88f), secondaryColor, 22, -8f);
        }

        private void BuildTreasureLandmark(Color baseColor, Color secondaryColor, Color detailColor, Color accentColor, Color shadowColor)
        {
            CreatePart("PlateShadow", GetCircleSprite(), new Vector2(0f, -0.22f), new Vector2(1.84f, 0.44f), shadowColor, 20);
            CreatePart("Pedestal", GetWhiteSprite(), new Vector2(0f, -0.02f), new Vector2(1.18f, 0.52f), baseColor, 21);
            CreatePart("Dome", GetCircleSprite(), new Vector2(0f, 0.38f), new Vector2(1.02f, 0.76f), secondaryColor, 22);
            CreateAccentPart("CandyCore", GetCircleSprite(), new Vector2(0f, 0.42f), new Vector2(0.32f, 0.32f), accentColor, 24);
            CreatePart("CandyLeft", GetCircleSprite(), new Vector2(-0.54f, 0.12f), new Vector2(0.24f, 0.24f), detailColor, 23);
            CreatePart("CandyRight", GetCircleSprite(), new Vector2(0.54f, 0.12f), new Vector2(0.24f, 0.24f), detailColor, 23);
        }

        private void BuildShopLandmark(Color baseColor, Color secondaryColor, Color detailColor, Color accentColor, Color shadowColor)
        {
            CreatePart("CounterShadow", GetCircleSprite(), new Vector2(0f, -0.2f), new Vector2(1.9f, 0.4f), shadowColor, 20);
            CreatePart("Counter", GetWhiteSprite(), new Vector2(0f, -0.02f), new Vector2(1.78f, 0.54f), secondaryColor, 21);
            CreatePart("AwningTop", GetWhiteSprite(), new Vector2(0f, 0.68f), new Vector2(1.72f, 0.24f), baseColor, 23);
            CreatePart("AwningStripeA", GetWhiteSprite(), new Vector2(-0.44f, 0.46f), new Vector2(0.36f, 0.48f), detailColor, 22);
            CreatePart("AwningStripeB", GetWhiteSprite(), new Vector2(0f, 0.46f), new Vector2(0.36f, 0.48f), baseColor, 22);
            CreatePart("AwningStripeC", GetWhiteSprite(), new Vector2(0.44f, 0.46f), new Vector2(0.36f, 0.48f), detailColor, 22);
            CreateAccentPart("CandyJar", GetCircleSprite(), new Vector2(0f, 0.12f), new Vector2(0.42f, 0.42f), accentColor, 24);
        }

        private void BuildBossLandmark(Color baseColor, Color accentColor, Color shadowColor, float crestScale)
        {
            CreatePart("GateShadow", GetCircleSprite(), new Vector2(0f, -0.22f), new Vector2(1.92f, 0.44f), shadowColor, 20);
            CreatePart("PillarLeft", GetWhiteSprite(), new Vector2(-0.66f, 0.18f), new Vector2(0.34f, 1.24f), shadowColor, 21);
            CreatePart("PillarRight", GetWhiteSprite(), new Vector2(0.66f, 0.18f), new Vector2(0.34f, 1.24f), shadowColor, 21);
            CreatePart("GateLintel", GetWhiteSprite(), new Vector2(0f, 0.76f), new Vector2(1.54f, 0.24f), baseColor, 22);
            CreatePart("GateVoid", GetWhiteSprite(), new Vector2(0f, 0.1f), new Vector2(0.82f, 0.82f), new Color(0.08f, 0.06f, 0.09f, 0.92f), 23);
            CreateAccentPart("Crest", GetCircleSprite(), new Vector2(0f, 0.82f), new Vector2(0.42f, 0.42f) * crestScale, accentColor, 24);
        }

        private void BuildSecretLandmark(Color baseColor, Color secondaryColor, Color accentColor, Color shadowColor)
        {
            CreatePart("JarShadow", GetCircleSprite(), new Vector2(0f, -0.18f), new Vector2(1.42f, 0.36f), shadowColor, 20);
            CreatePart("JarBody", GetCircleSprite(), new Vector2(0f, 0.18f), new Vector2(1.02f, 1.18f), baseColor, 21);
            CreatePart("JarCap", GetWhiteSprite(), new Vector2(0f, 0.78f), new Vector2(0.88f, 0.18f), secondaryColor, 22);
            CreateAccentPart("Seal", GetWhiteSprite(), new Vector2(0f, 0.26f), new Vector2(0.32f, 0.32f), accentColor, 23, 45f);
            CreatePart("SwirlLeft", GetCircleSprite(), new Vector2(-0.46f, 0.18f), new Vector2(0.22f, 0.22f), secondaryColor, 22);
            CreatePart("SwirlRight", GetCircleSprite(), new Vector2(0.46f, 0.18f), new Vector2(0.22f, 0.22f), secondaryColor, 22);
        }

        private void BuildChallengeLandmark(Color baseColor, Color secondaryColor, Color accentColor, Color shadowColor)
        {
            CreatePart("BoardShadow", GetCircleSprite(), new Vector2(0f, -0.18f), new Vector2(1.64f, 0.36f), shadowColor, 20);
            CreatePart("PostLeft", GetWhiteSprite(), new Vector2(-0.48f, 0.08f), new Vector2(0.16f, 1.1f), secondaryColor, 21, 10f);
            CreatePart("PostRight", GetWhiteSprite(), new Vector2(0.48f, 0.08f), new Vector2(0.16f, 1.1f), secondaryColor, 21, -10f);
            CreatePart("Board", GetWhiteSprite(), new Vector2(0f, 0.28f), new Vector2(1.18f, 0.78f), baseColor, 22);
            CreateAccentPart("Badge", GetWhiteSprite(), new Vector2(0f, 0.28f), new Vector2(0.32f, 0.32f), accentColor, 23, 45f);
        }

        private void BuildTrapLandmark(Color baseColor, Color detailColor, Color accentColor, Color shadowColor)
        {
            CreatePart("TrapShadow", GetCircleSprite(), new Vector2(0f, -0.18f), new Vector2(1.58f, 0.34f), shadowColor, 20);
            CreatePart("WarningPlate", GetWhiteSprite(), new Vector2(0f, 0.18f), new Vector2(1.02f, 1.02f), baseColor, 21, 45f);
            CreateAccentPart("CoreMark", GetWhiteSprite(), new Vector2(0f, 0.18f), new Vector2(0.2f, 0.72f), accentColor, 23);
            CreatePart("SpikeLeft", GetWhiteSprite(), new Vector2(-0.48f, -0.36f), new Vector2(0.18f, 0.42f), detailColor, 22, -12f);
            CreatePart("SpikeCenter", GetWhiteSprite(), new Vector2(0f, -0.38f), new Vector2(0.18f, 0.48f), detailColor, 22);
            CreatePart("SpikeRight", GetWhiteSprite(), new Vector2(0.48f, -0.36f), new Vector2(0.18f, 0.42f), detailColor, 22, 12f);
        }

        private void BuildCurseLandmark(Color baseColor, Color secondaryColor, Color accentColor, Color shadowColor)
        {
            CreatePart("AltarShadow", GetCircleSprite(), new Vector2(0f, -0.18f), new Vector2(1.7f, 0.38f), shadowColor, 20);
            CreatePart("Bowl", GetCircleSprite(), new Vector2(0f, 0.02f), new Vector2(1.02f, 0.68f), shadowColor, 21);
            CreatePart("Stand", GetWhiteSprite(), new Vector2(0f, -0.18f), new Vector2(0.42f, 0.46f), baseColor, 22);
            CreateAccentPart("FlameCore", GetCircleSprite(), new Vector2(0f, 0.46f), new Vector2(0.34f, 0.46f), accentColor, 24);
            CreatePart("CandleLeft", GetWhiteSprite(), new Vector2(-0.54f, 0.1f), new Vector2(0.12f, 0.46f), secondaryColor, 23);
            CreatePart("CandleRight", GetWhiteSprite(), new Vector2(0.54f, 0.1f), new Vector2(0.12f, 0.46f), secondaryColor, 23);
        }

        private void ResolvePalette(out Color baseColor, out Color secondaryColor, out Color detailColor, out Color accentColor, out Color shadowColor)
        {
            baseColor = _theme != null ? _theme.PrimaryAccentColor : new Color(0.97f, 0.78f, 0.84f, 1f);
            secondaryColor = _theme != null ? _theme.SecondaryAccentColor : new Color(0.74f, 0.92f, 0.86f, 1f);
            detailColor = _theme != null ? _theme.TertiaryAccentColor : new Color(0.95f, 0.88f, 0.56f, 1f);
            Color highlightColor = _theme != null ? _theme.HighlightAccentColor : new Color(0.99f, 0.95f, 0.92f, 1f);
            shadowColor = _theme != null ? _theme.ShadowAccentColor : new Color(0.43f, 0.28f, 0.26f, 1f);

            accentColor = _roomType switch
            {
                RoomType.Start => Color.Lerp(baseColor, highlightColor, 0.46f),
                RoomType.Treasure => Color.Lerp(detailColor, new Color(1f, 0.85f, 0.36f, 1f), 0.5f),
                RoomType.Shop => Color.Lerp(secondaryColor, new Color(0.44f, 0.88f, 0.74f, 1f), 0.38f),
                RoomType.Boss => Color.Lerp(shadowColor, new Color(0.96f, 0.34f, 0.42f, 1f), 0.72f),
                RoomType.MiniBoss => Color.Lerp(detailColor, new Color(0.98f, 0.54f, 0.24f, 1f), 0.58f),
                RoomType.Secret => Color.Lerp(baseColor, new Color(0.72f, 0.52f, 1f, 1f), 0.56f),
                RoomType.Challenge => Color.Lerp(detailColor, new Color(1f, 0.58f, 0.22f, 1f), 0.46f),
                RoomType.Trap => Color.Lerp(shadowColor, new Color(1f, 0.38f, 0.3f, 1f), 0.66f),
                RoomType.Curse => Color.Lerp(shadowColor, new Color(0.86f, 0.22f, 0.34f, 1f), 0.58f),
                _ => highlightColor
            };
        }

        private SpriteRenderer CreatePart(string name, Sprite sprite, Vector2 localPosition, Vector2 localScale, Color color, int sortingOrder, float rotationZ = 0f)
        {
            GameObject child = new(name);
            child.transform.SetParent(transform, false);
            child.transform.localPosition = new Vector3(localPosition.x, localPosition.y, 0f);
            child.transform.localRotation = Quaternion.Euler(0f, 0f, rotationZ);
            child.transform.localScale = new Vector3(localScale.x, localScale.y, 1f);
            child.layer = gameObject.layer;

            SpriteRenderer renderer = child.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
            return renderer;
        }

        private void CreateAccentPart(string name, Sprite sprite, Vector2 localPosition, Vector2 localScale, Color color, int sortingOrder, float rotationZ = 0f)
        {
            SpriteRenderer renderer = CreatePart(name, sprite, localPosition, localScale, color, sortingOrder, rotationZ);
            _accentRenderers.Add(renderer);
            _accentBaseColors.Add(color);
        }

        private void CacheBaseTransform()
        {
            _baseLocalPosition = transform.localPosition;
            _baseLocalScale = transform.localScale == Vector3.zero ? Vector3.one : transform.localScale;
        }

        private void ClearChildren()
        {
            for (int index = transform.childCount - 1; index >= 0; index--)
            {
                GameObject child = transform.GetChild(index).gameObject;

                if (Application.isPlaying)
                {
                    Destroy(child);
                }
                else
                {
                    DestroyImmediate(child);
                }
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

            const int size = 64;
            Texture2D texture = new(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                name = "RuntimeRoomLandmarkCircle"
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
            s_CircleSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
            return s_CircleSprite;
        }
    }
}
