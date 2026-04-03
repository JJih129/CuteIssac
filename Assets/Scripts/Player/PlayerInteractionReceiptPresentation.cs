using CuteIssac.Core.Feedback;
using UnityEngine;

namespace CuteIssac.Player
{
    [DisallowMultipleComponent]
    public sealed class PlayerInteractionReceiptPresentation : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerVisual playerVisual;
        [SerializeField] private PlayerScreenFeedback playerScreenFeedback;

        [Header("Layout")]
        [SerializeField] private Vector3 rootOffset = new(0f, 0.08f, 0f);
        [SerializeField] [Min(0.01f)] private float bobAmplitude = 0.03f;
        [SerializeField] [Min(0.05f)] private float bobFrequency = 3.2f;
        [SerializeField] [Min(0.05f)] private float pulseFrequency = 6.4f;

        private static Sprite s_WhiteSprite;
        private static Sprite s_CircleSprite;

        private Transform _root;
        private SpriteRenderer _ringRenderer;
        private SpriteRenderer _flareRenderer;
        private SpriteRenderer _trailRenderer;
        private SpriteRenderer _coreRenderer;
        private SpriteRenderer _shardA;
        private SpriteRenderer _shardB;
        private Vector3 _baseLocalPosition;
        private Vector3 _ringBaseScale = Vector3.one;
        private Vector3 _flareBaseScale = Vector3.one;
        private Vector3 _trailBaseScale = Vector3.one;
        private Vector3 _coreBaseScale = Vector3.one;
        private Vector3 _shardABaseScale = Vector3.one;
        private Vector3 _shardBBaseScale = Vector3.one;
        private Color _accentColor = Color.white;
        private Vector2 _sourceDirection = Vector2.up;
        private float _visibility;
        private float _pulse;
        private float _linger;
        private float _phaseSeed;
        private bool _success;
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
            _visibility = Mathf.MoveTowards(_visibility, targetVisibility, Time.unscaledDeltaTime * (targetVisibility > 0f ? 8f : 10f));
            _pulse = Mathf.MoveTowards(_pulse, 0f, Time.unscaledDeltaTime * (_emphasize ? 2.1f : 3.2f));
            _linger = Mathf.MoveTowards(_linger, 0f, Time.unscaledDeltaTime * (_emphasize ? 1.2f : 1.8f));

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
            float emphasis = (_success ? 1f : 0.82f) + (_emphasize ? 0.22f : 0f) + (_pulse * 0.3f);
            Color liveAccent = ResolveLiveAccent();
            Color brightAccent = Color.Lerp(
                liveAccent,
                Color.white,
                (_success ? 0.34f : 0.18f) + (_emphasize ? 0.08f : 0f) + (_pulse * 0.12f));

            _root.localPosition = _baseLocalPosition + new Vector3(0f, bob, 0f);

            _ringRenderer.transform.localScale = _ringBaseScale * Mathf.Lerp(0.92f, 1.18f + (_pulse * 0.16f), pulse);
            _ringRenderer.color = new Color(liveAccent.r, liveAccent.g, liveAccent.b, _visibility * Mathf.Lerp(0.16f, 0.3f, pulse) * emphasis);

            _flareRenderer.transform.localScale = _flareBaseScale * Mathf.Lerp(0.9f, 1.24f + (_pulse * 0.14f), pulse);
            _flareRenderer.color = new Color(brightAccent.r, brightAccent.g, brightAccent.b, _visibility * Mathf.Lerp(0.1f, 0.22f, pulse) * emphasis);

            float trailLength = Mathf.Lerp(0.36f, 0.66f + (_emphasize ? 0.12f : 0f), pulse);
            float trailAngle = Mathf.Atan2(_sourceDirection.y, _sourceDirection.x) * Mathf.Rad2Deg;
            _trailRenderer.transform.localPosition = new Vector3(_sourceDirection.x, _sourceDirection.y, 0f) * (trailLength * 0.42f);
            _trailRenderer.transform.localRotation = Quaternion.Euler(0f, 0f, trailAngle);
            _trailRenderer.transform.localScale = new Vector3(trailLength, _trailBaseScale.y * Mathf.Lerp(0.82f, 1.14f, pulse), 1f);
            _trailRenderer.color = new Color(liveAccent.r, liveAccent.g, liveAccent.b, _visibility * Mathf.Lerp(0.12f, 0.26f, pulse) * emphasis);

            _coreRenderer.transform.localScale = _coreBaseScale * Mathf.Lerp(0.94f, 1.2f + (_pulse * 0.12f), pulse);
            _coreRenderer.color = new Color(brightAccent.r, brightAccent.g, brightAccent.b, _visibility * Mathf.Lerp(0.52f, 0.84f, pulse) * emphasis);

            Vector2 tangent = new(-_sourceDirection.y, _sourceDirection.x);
            UpdateShard(_shardA, _shardABaseScale, tangent, liveAccent, brightAccent, pulse, emphasis, 22f);
            UpdateShard(_shardB, _shardBBaseScale, -tangent, liveAccent, brightAccent, pulse, emphasis, -22f);
        }

        public void PlayReceipt(
            Vector3 sourcePosition,
            Color accentColor,
            string label,
            bool success,
            bool emphasize = false,
            string detailLabel = "",
            Color? detailColor = null)
        {
            ResolveReferences();
            EnsureVisuals();

            Vector2 offset = sourcePosition - transform.position;
            _sourceDirection = offset.sqrMagnitude > 0.0001f ? offset.normalized : Vector2.up;
            _accentColor = accentColor.a > 0.01f
                ? new Color(accentColor.r, accentColor.g, accentColor.b, 1f)
                : success
                    ? new Color(0.52f, 1f, 0.74f, 1f)
                    : new Color(1f, 0.58f, 0.46f, 1f);
            _success = success;
            _emphasize = emphasize;
            _pulse = 1f;
            _linger = emphasize ? 0.76f : success ? 0.58f : 0.42f;
            _visibility = 1f;

            if (!_root.gameObject.activeSelf)
            {
                _root.gameObject.SetActive(true);
            }

            float screenScale = success
                ? emphasize ? 0.18f : 0.12f
                : 0.06f;
            playerScreenFeedback?.PlayHitFeedback(screenScale);

            if (!string.IsNullOrWhiteSpace(label))
            {
                Color feedbackColor = Color.Lerp(_accentColor, Color.white, success ? 0.2f : 0.08f);
                Vector3 feedbackOffset = new(_sourceDirection.x * 0.18f, 0.94f + Mathf.Max(0f, _sourceDirection.y) * 0.14f, 0f);
                GameplayFeedbackEvents.RaiseFloatingFeedback(new FloatingFeedbackRequest(
                    transform.position + feedbackOffset,
                    label,
                    feedbackColor,
                    emphasize ? 0.74f : success ? 0.62f : 0.48f,
                    emphasize ? 0.62f : success ? 0.52f : 0.34f,
                    emphasize ? 1.08f : success ? 1f : 0.94f,
                    visualProfile: FloatingFeedbackVisualProfile.Momentum));
            }

            if (!string.IsNullOrWhiteSpace(detailLabel))
            {
                Color resolvedDetailColor = detailColor ?? Color.Lerp(_accentColor, Color.white, 0.1f);
                Vector3 detailOffset = new(-_sourceDirection.x * 0.1f, 0.62f - Mathf.Max(0f, _sourceDirection.y) * 0.08f, 0f);
                GameplayFeedbackEvents.RaiseFloatingFeedback(new FloatingFeedbackRequest(
                    transform.position + detailOffset,
                    detailLabel,
                    resolvedDetailColor,
                    success ? 0.56f : 0.5f,
                    success ? 0.42f : 0.3f,
                    success ? 0.92f : 0.88f,
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

            GameObject rootObject = new("PlayerInteractionReceipt");
            rootObject.layer = gameObject.layer;
            _root = rootObject.transform;
            _root.SetParent(transform, false);
            _baseLocalPosition = rootOffset;
            _root.localPosition = _baseLocalPosition;

            _ringRenderer = CreatePart(
                "ReceiptRing",
                GetCircleSprite(),
                new Vector3(0f, -0.22f, 0f),
                new Vector3(0.74f, 0.44f, 1f),
                new Color(1f, 1f, 1f, 0f),
                -3);
            _flareRenderer = CreatePart(
                "ReceiptFlare",
                GetCircleSprite(),
                new Vector3(0f, 0.14f, 0f),
                new Vector3(0.44f, 0.44f, 1f),
                new Color(1f, 1f, 1f, 0f),
                -1);
            _trailRenderer = CreatePart(
                "ReceiptTrail",
                GetWhiteSprite(),
                Vector3.zero,
                new Vector3(0.42f, 0.05f, 1f),
                new Color(1f, 1f, 1f, 0f),
                -2);
            _coreRenderer = CreatePart(
                "ReceiptCore",
                GetCircleSprite(),
                new Vector3(0f, 0.48f, 0f),
                new Vector3(0.16f, 0.16f, 1f),
                new Color(1f, 1f, 1f, 0f),
                1);
            _shardA = CreatePart(
                "ReceiptShardA",
                GetWhiteSprite(),
                new Vector3(-0.16f, 0.34f, 0f),
                new Vector3(0.14f, 0.04f, 1f),
                new Color(1f, 1f, 1f, 0f),
                0,
                22f);
            _shardB = CreatePart(
                "ReceiptShardB",
                GetWhiteSprite(),
                new Vector3(0.16f, 0.34f, 0f),
                new Vector3(0.14f, 0.04f, 1f),
                new Color(1f, 1f, 1f, 0f),
                0,
                -22f);

            _ringBaseScale = _ringRenderer.transform.localScale;
            _flareBaseScale = _flareRenderer.transform.localScale;
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
            Color liveAccent,
            Color brightAccent,
            float pulse,
            float emphasis,
            float rotationZ)
        {
            if (renderer == null)
            {
                return;
            }

            Vector3 localPosition = new(direction.x * 0.2f, 0.34f + (direction.y * 0.06f), 0f);
            renderer.transform.localPosition = localPosition;
            renderer.transform.localRotation = Quaternion.Euler(0f, 0f, rotationZ);
            renderer.transform.localScale = baseScale * Mathf.Lerp(0.92f, 1.22f, pulse);
            renderer.color = new Color(
                brightAccent.r,
                brightAccent.g,
                brightAccent.b,
                _visibility * Mathf.Lerp(0.3f, 0.74f, pulse) * emphasis);
        }

        private Color ResolveLiveAccent()
        {
            if (_success)
            {
                return Color.Lerp(_accentColor, new Color(0.52f, 1f, 0.74f, 1f), 0.3f);
            }

            return Color.Lerp(_accentColor, new Color(1f, 0.58f, 0.46f, 1f), 0.64f);
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
                name = "RuntimePlayerInteractionReceiptCircle"
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
