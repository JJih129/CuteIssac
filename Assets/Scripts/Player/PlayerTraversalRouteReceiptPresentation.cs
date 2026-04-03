using CuteIssac.Core.Feedback;
using CuteIssac.Data.Dungeon;
using UnityEngine;

namespace CuteIssac.Player
{
    [DisallowMultipleComponent]
    public sealed class PlayerTraversalRouteReceiptPresentation : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerVisual playerVisual;
        [SerializeField] private PlayerScreenFeedback playerScreenFeedback;

        [Header("Layout")]
        [SerializeField] private Vector3 rootOffset = new(0f, 0.18f, 0f);
        [SerializeField] [Min(0.01f)] private float bobAmplitude = 0.024f;
        [SerializeField] [Min(0.05f)] private float bobFrequency = 2.8f;
        [SerializeField] [Min(0.05f)] private float pulseFrequency = 5.4f;

        private static Sprite s_WhiteSprite;
        private static Sprite s_CircleSprite;

        private Transform _root;
        private SpriteRenderer _ringRenderer;
        private SpriteRenderer _trailRenderer;
        private SpriteRenderer _coreRenderer;
        private SpriteRenderer _shardA;
        private SpriteRenderer _shardB;
        private Vector3 _baseLocalPosition;
        private Vector3 _ringBaseScale = Vector3.one;
        private Vector3 _trailBaseScale = Vector3.one;
        private Vector3 _coreBaseScale = Vector3.one;
        private Vector3 _shardABaseScale = Vector3.one;
        private Vector3 _shardBBaseScale = Vector3.one;
        private Color _accentColor = Color.white;
        private Vector2 _travelDirection = Vector2.up;
        private float _visibility;
        private float _pulse;
        private float _linger;
        private float _phaseSeed;
        private float _roomWeight;

        private void Awake()
        {
            ResolveReferences();
            _phaseSeed = Random.Range(0f, Mathf.PI * 2f);
            EnsureVisuals();
            HideImmediate();
        }

        private void OnEnable()
        {
            ResolveReferences();
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

            float targetVisibility = (_linger > 0.001f || _pulse > 0.001f) ? 1f : 0f;
            _visibility = Mathf.MoveTowards(_visibility, targetVisibility, Time.unscaledDeltaTime * (targetVisibility > 0f ? 8f : 10f));
            _pulse = Mathf.MoveTowards(_pulse, 0f, Time.unscaledDeltaTime * 2.6f);
            _linger = Mathf.MoveTowards(_linger, 0f, Time.unscaledDeltaTime * 1.35f);

            if (_visibility <= 0.001f)
            {
                HideImmediate();
                return;
            }

            if (!_root.gameObject.activeSelf)
            {
                _root.gameObject.SetActive(true);
            }

            float pulse = 0.5f + (Mathf.Sin((Time.unscaledTime * pulseFrequency) + _phaseSeed) * 0.5f);
            float bob = Mathf.Sin((Time.unscaledTime * bobFrequency) + _phaseSeed) * bobAmplitude;
            float emphasis = 1f + (_pulse * 0.24f) + (_roomWeight * 0.12f);
            Color brightAccent = Color.Lerp(_accentColor, Color.white, 0.22f + (_pulse * 0.12f));
            Vector2 tangent = new(-_travelDirection.y, _travelDirection.x);

            _root.localPosition = _baseLocalPosition + new Vector3(0f, bob, 0f);

            _ringRenderer.transform.localScale = _ringBaseScale * Mathf.Lerp(0.9f, 1.18f + (_roomWeight * 0.14f), pulse);
            _ringRenderer.color = new Color(_accentColor.r, _accentColor.g, _accentColor.b, _visibility * Mathf.Lerp(0.18f, 0.34f, pulse) * emphasis);

            float trailAngle = Mathf.Atan2(_travelDirection.y, _travelDirection.x) * Mathf.Rad2Deg;
            _trailRenderer.transform.localPosition = new Vector3(_travelDirection.x, _travelDirection.y, 0f) * 0.18f;
            _trailRenderer.transform.localRotation = Quaternion.Euler(0f, 0f, trailAngle);
            _trailRenderer.transform.localScale = new Vector3(
                _trailBaseScale.x * Mathf.Lerp(0.92f, 1.24f, pulse),
                _trailBaseScale.y * Mathf.Lerp(0.88f, 1.18f + (_roomWeight * 0.12f), pulse),
                1f);
            _trailRenderer.color = new Color(brightAccent.r, brightAccent.g, brightAccent.b, _visibility * Mathf.Lerp(0.18f, 0.3f, pulse) * emphasis);

            _coreRenderer.transform.localScale = _coreBaseScale * Mathf.Lerp(0.94f, 1.22f + (_pulse * 0.12f), pulse);
            _coreRenderer.color = new Color(brightAccent.r, brightAccent.g, brightAccent.b, _visibility * Mathf.Lerp(0.48f, 0.78f, pulse) * emphasis);

            UpdateShard(_shardA, _shardABaseScale, tangent, brightAccent, pulse, emphasis, 26f);
            UpdateShard(_shardB, _shardBBaseScale, -tangent, brightAccent, pulse, emphasis, -26f);
        }

        public void PlayRouteReceipt(
            Vector2 travelDirection,
            Color accentColor,
            RoomType roomType,
            string headline,
            string detail,
            bool emphasize)
        {
            ResolveReferences();
            EnsureVisuals();

            _travelDirection = travelDirection.sqrMagnitude > 0.0001f
                ? travelDirection.normalized
                : Vector2.up;
            _accentColor = accentColor.a > 0.01f
                ? new Color(accentColor.r, accentColor.g, accentColor.b, 1f)
                : new Color(0.66f, 0.92f, 1f, 1f);
            _roomWeight = ResolveRoomWeight(roomType);
            _pulse = 1f;
            _linger = emphasize ? 0.84f : 0.58f;
            _visibility = 1f;

            if (!_root.gameObject.activeSelf)
            {
                _root.gameObject.SetActive(true);
            }

            playerScreenFeedback?.PlayHitFeedback(emphasize ? 0.1f : 0.06f);

            if (!string.IsNullOrWhiteSpace(headline))
            {
                GameplayFeedbackEvents.RaiseFloatingFeedback(new FloatingFeedbackRequest(
                    transform.position + new Vector3(_travelDirection.x * 0.14f, 1.04f, 0f),
                    headline,
                    Color.Lerp(_accentColor, Color.white, 0.18f),
                    emphasize ? 0.74f : 0.62f,
                    emphasize ? 0.6f : 0.48f,
                    emphasize ? 1.08f : 1f,
                    visualProfile: FloatingFeedbackVisualProfile.Momentum));
            }

            if (!string.IsNullOrWhiteSpace(detail))
            {
                GameplayFeedbackEvents.RaiseFloatingFeedback(new FloatingFeedbackRequest(
                    transform.position + new Vector3((-_travelDirection.x * 0.08f), 0.72f, 0f),
                    detail,
                    Color.Lerp(_accentColor, Color.white, 0.08f),
                    0.56f,
                    0.42f,
                    0.92f,
                    visualProfile: FloatingFeedbackVisualProfile.Momentum));
            }
        }

        private void ResolveReferences()
        {
            if (playerVisual == null)
            {
                TryGetComponent(out playerVisual);
            }

            if (playerScreenFeedback == null)
            {
                TryGetComponent(out playerScreenFeedback);
            }

            if (playerScreenFeedback == null)
            {
                playerScreenFeedback = gameObject.AddComponent<PlayerScreenFeedback>();
            }
        }

        private void EnsureVisuals()
        {
            if (_root != null)
            {
                return;
            }

            GameObject rootObject = new("PlayerTraversalRouteReceipt");
            rootObject.layer = gameObject.layer;
            _root = rootObject.transform;
            _root.SetParent(transform, false);
            _baseLocalPosition = rootOffset;
            _root.localPosition = _baseLocalPosition;

            _ringRenderer = CreatePart(
                "RouteRing",
                GetCircleSprite(),
                new Vector3(0f, -0.12f, 0f),
                new Vector3(0.62f, 0.38f, 1f),
                new Color(1f, 1f, 1f, 0f),
                -3);
            _trailRenderer = CreatePart(
                "RouteTrail",
                GetWhiteSprite(),
                new Vector3(0f, 0.12f, 0f),
                new Vector3(0.16f, 0.56f, 1f),
                new Color(1f, 1f, 1f, 0f),
                -2);
            _coreRenderer = CreatePart(
                "RouteCore",
                GetCircleSprite(),
                new Vector3(0f, 0.42f, 0f),
                new Vector3(0.16f, 0.16f, 1f),
                new Color(1f, 1f, 1f, 0f),
                1);
            _shardA = CreatePart(
                "RouteShardA",
                GetWhiteSprite(),
                new Vector3(-0.16f, 0.3f, 0f),
                new Vector3(0.16f, 0.04f, 1f),
                new Color(1f, 1f, 1f, 0f),
                0,
                26f);
            _shardB = CreatePart(
                "RouteShardB",
                GetWhiteSprite(),
                new Vector3(0.16f, 0.3f, 0f),
                new Vector3(0.16f, 0.04f, 1f),
                new Color(1f, 1f, 1f, 0f),
                0,
                -26f);

            _ringBaseScale = _ringRenderer.transform.localScale;
            _trailBaseScale = _trailRenderer.transform.localScale;
            _coreBaseScale = _coreRenderer.transform.localScale;
            _shardABaseScale = _shardA.transform.localScale;
            _shardBBaseScale = _shardB.transform.localScale;
            HideImmediate();
        }

        private SpriteRenderer CreatePart(
            string name,
            Sprite sprite,
            Vector3 localPosition,
            Vector3 localScale,
            Color color,
            int sortingOffset,
            float rotationZ = 0f)
        {
            GameObject child = new(name);
            child.layer = gameObject.layer;
            child.transform.SetParent(_root, false);
            child.transform.localPosition = localPosition;
            child.transform.localScale = localScale;
            child.transform.localRotation = Quaternion.Euler(0f, 0f, rotationZ);

            SpriteRenderer renderer = child.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = ResolveSortingOrder(sortingOffset);
            return renderer;
        }

        private void UpdateShard(
            SpriteRenderer renderer,
            Vector3 baseScale,
            Vector2 direction,
            Color accentColor,
            float pulse,
            float emphasis,
            float rotationZ)
        {
            if (renderer == null)
            {
                return;
            }

            renderer.transform.localPosition = new Vector3(direction.x * 0.18f, 0.28f + (direction.y * 0.04f), 0f);
            renderer.transform.localRotation = Quaternion.Euler(0f, 0f, rotationZ);
            renderer.transform.localScale = baseScale * Mathf.Lerp(0.94f, 1.2f, pulse);
            renderer.color = new Color(
                accentColor.r,
                accentColor.g,
                accentColor.b,
                _visibility * Mathf.Lerp(0.28f, 0.72f, pulse) * emphasis);
        }

        private int ResolveSortingOrder(int fallbackOffset)
        {
            SpriteRenderer bodyRenderer = playerVisual != null ? playerVisual.BodySpriteRenderer : null;
            return bodyRenderer != null
                ? bodyRenderer.sortingOrder + fallbackOffset
                : fallbackOffset;
        }

        private void HideImmediate()
        {
            _visibility = 0f;
            _pulse = 0f;
            _linger = 0f;

            if (_root != null)
            {
                _root.gameObject.SetActive(false);
            }
        }

        private static float ResolveRoomWeight(RoomType roomType)
        {
            return roomType switch
            {
                RoomType.Boss => 1f,
                RoomType.Treasure => 0.82f,
                RoomType.Secret => 0.76f,
                RoomType.MiniBoss => 0.72f,
                RoomType.Challenge => 0.68f,
                RoomType.Curse => 0.58f,
                RoomType.Trap => 0.54f,
                RoomType.Shop => 0.46f,
                _ => 0.2f
            };
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
                name = "RuntimePlayerTraversalRouteReceiptCircle"
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
