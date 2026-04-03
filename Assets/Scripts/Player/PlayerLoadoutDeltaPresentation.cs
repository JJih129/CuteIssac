using UnityEngine;

namespace CuteIssac.Player
{
    [DisallowMultipleComponent]
    public sealed class PlayerLoadoutDeltaPresentation : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerVisual playerVisual;
        [SerializeField] private PlayerScreenFeedback playerScreenFeedback;

        [Header("Layout")]
        [SerializeField] private Vector3 rootOffset = new(0f, 0.1f, 0f);
        [SerializeField] [Min(0.01f)] private float bobAmplitude = 0.02f;
        [SerializeField] [Min(0.05f)] private float bobFrequency = 2.6f;
        [SerializeField] [Min(0.05f)] private float pulseFrequency = 5.2f;

        private static Sprite s_WhiteSprite;
        private static Sprite s_CircleSprite;

        private Transform _root;
        private SpriteRenderer _ringRenderer;
        private SpriteRenderer _crestRenderer;
        private SpriteRenderer _trailLeftRenderer;
        private SpriteRenderer _trailRightRenderer;
        private SpriteRenderer _coreRenderer;
        private Vector3 _baseLocalPosition;
        private Vector3 _ringBaseScale = Vector3.one;
        private Vector3 _crestBaseScale = Vector3.one;
        private Vector3 _trailLeftBaseScale = Vector3.one;
        private Vector3 _trailRightBaseScale = Vector3.one;
        private Vector3 _coreBaseScale = Vector3.one;
        private Color _accentColor = Color.white;
        private float _visibility;
        private float _pulse;
        private float _linger;
        private float _phaseSeed;
        private bool _emphasize;

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
            _visibility = Mathf.MoveTowards(_visibility, targetVisibility, Time.unscaledDeltaTime * (targetVisibility > 0f ? 7.8f : 9.6f));
            _pulse = Mathf.MoveTowards(_pulse, 0f, Time.unscaledDeltaTime * (_emphasize ? 2f : 3f));
            _linger = Mathf.MoveTowards(_linger, 0f, Time.unscaledDeltaTime * (_emphasize ? 1f : 1.6f));

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
            float emphasis = 1f + (_emphasize ? 0.22f : 0f) + (_pulse * 0.34f);
            Color liveAccent = Color.Lerp(_accentColor, Color.white, 0.12f + (_emphasize ? 0.08f : 0f) + (_pulse * 0.14f));
            Color brightAccent = Color.Lerp(liveAccent, Color.white, 0.3f + (_pulse * 0.12f));

            _root.localPosition = _baseLocalPosition + new Vector3(0f, bob, 0f);

            _ringRenderer.transform.localScale = _ringBaseScale * Mathf.Lerp(0.92f, 1.16f + (_pulse * 0.12f), pulse);
            _ringRenderer.color = new Color(liveAccent.r, liveAccent.g, liveAccent.b, _visibility * Mathf.Lerp(0.14f, 0.28f, pulse) * emphasis);

            _crestRenderer.transform.localScale = _crestBaseScale * Mathf.Lerp(0.9f, 1.18f + (_pulse * 0.16f), pulse);
            _crestRenderer.color = new Color(brightAccent.r, brightAccent.g, brightAccent.b, _visibility * Mathf.Lerp(0.18f, 0.34f, pulse) * emphasis);

            _trailLeftRenderer.transform.localScale = _trailLeftBaseScale * Mathf.Lerp(0.92f, 1.14f, pulse);
            _trailLeftRenderer.color = new Color(liveAccent.r, liveAccent.g, liveAccent.b, _visibility * Mathf.Lerp(0.12f, 0.24f, pulse) * emphasis);

            _trailRightRenderer.transform.localScale = _trailRightBaseScale * Mathf.Lerp(0.92f, 1.14f, pulse);
            _trailRightRenderer.color = new Color(liveAccent.r, liveAccent.g, liveAccent.b, _visibility * Mathf.Lerp(0.12f, 0.24f, pulse) * emphasis);

            _coreRenderer.transform.localScale = _coreBaseScale * Mathf.Lerp(0.94f, 1.26f + (_pulse * 0.1f), pulse);
            _coreRenderer.color = new Color(brightAccent.r, brightAccent.g, brightAccent.b, _visibility * Mathf.Lerp(0.42f, 0.7f, pulse) * emphasis);
        }

        public void PlayDelta(Color accentColor, bool emphasize)
        {
            ResolveReferences();
            EnsureVisuals();

            _accentColor = accentColor.a > 0.01f
                ? new Color(accentColor.r, accentColor.g, accentColor.b, 1f)
                : new Color(0.72f, 0.85f, 1f, 1f);
            _emphasize = emphasize;
            _pulse = 1f;
            _linger = emphasize ? 0.78f : 0.56f;
            _visibility = 1f;

            if (!_root.gameObject.activeSelf)
            {
                _root.gameObject.SetActive(true);
            }

            playerScreenFeedback?.PlayHitFeedback(emphasize ? 0.14f : 0.08f);
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

            GameObject rootObject = new("PlayerLoadoutDelta");
            rootObject.layer = gameObject.layer;
            _root = rootObject.transform;
            _root.SetParent(transform, false);
            _baseLocalPosition = rootOffset;
            _root.localPosition = _baseLocalPosition;

            _ringRenderer = CreatePart(
                "DeltaRing",
                GetCircleSprite(),
                new Vector3(0f, -0.16f, 0f),
                new Vector3(0.86f, 0.48f, 1f),
                new Color(1f, 1f, 1f, 0f),
                -4);
            _crestRenderer = CreatePart(
                "DeltaCrest",
                GetCircleSprite(),
                new Vector3(0f, 0.42f, 0f),
                new Vector3(0.44f, 0.28f, 1f),
                new Color(1f, 1f, 1f, 0f),
                1);
            _trailLeftRenderer = CreatePart(
                "DeltaTrailLeft",
                GetWhiteSprite(),
                new Vector3(-0.22f, 0.1f, 0f),
                new Vector3(0.22f, 0.045f, 1f),
                new Color(1f, 1f, 1f, 0f),
                -1,
                28f);
            _trailRightRenderer = CreatePart(
                "DeltaTrailRight",
                GetWhiteSprite(),
                new Vector3(0.22f, 0.1f, 0f),
                new Vector3(0.22f, 0.045f, 1f),
                new Color(1f, 1f, 1f, 0f),
                -1,
                -28f);
            _coreRenderer = CreatePart(
                "DeltaCore",
                GetCircleSprite(),
                new Vector3(0f, 0.18f, 0f),
                new Vector3(0.16f, 0.16f, 1f),
                new Color(1f, 1f, 1f, 0f),
                2);

            _ringBaseScale = _ringRenderer.transform.localScale;
            _crestBaseScale = _crestRenderer.transform.localScale;
            _trailLeftBaseScale = _trailLeftRenderer.transform.localScale;
            _trailRightBaseScale = _trailRightRenderer.transform.localScale;
            _coreBaseScale = _coreRenderer.transform.localScale;
            HideImmediate();
        }

        private SpriteRenderer CreatePart(
            string objectName,
            Sprite sprite,
            Vector3 localPosition,
            Vector3 localScale,
            Color color,
            int sortingOffset,
            float rotationZ = 0f)
        {
            GameObject child = new(objectName);
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
                name = "RuntimePlayerLoadoutDeltaCircle"
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
