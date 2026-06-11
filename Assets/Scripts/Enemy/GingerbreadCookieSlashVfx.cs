using UnityEngine;

namespace CuteIssac.Enemy
{
    [DisallowMultipleComponent]
    public sealed class GingerbreadCookieSlashVfx : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private SpriteRenderer bodySpriteRenderer;

        [Header("Look")]
        [SerializeField] private Color coreColor = new(1f, 0.96f, 0.82f, 0.95f);
        [SerializeField] private Color edgeColor = new(1f, 0.2f, 0.28f, 0.55f);
        [SerializeField] [Min(0.1f)] private float slashWidth = 1.2f;
        [SerializeField] [Min(0.1f)] private float slashHeight = 0.58f;
        [SerializeField] [Min(0f)] private float forwardOffset = 0.72f;
        [SerializeField] [Min(0f)] private float verticalLift = 0.08f;
        [SerializeField] [Min(0.02f)] private float lifetime = 0.22f;
        [SerializeField] [Min(0f)] private float startScale = 0.68f;
        [SerializeField] [Min(0f)] private float endScale = 1.22f;

        private const int TextureSize = 96;
        private static Sprite _slashSprite;

        private SpriteRenderer _coreRenderer;
        private SpriteRenderer _edgeRenderer;
        private float _remaining;
        private float _duration;
        private Vector2 _playDirection = Vector2.right;
        private Vector3 _baseLocalScale = Vector3.one;

        private void Awake()
        {
            EnsureRenderers();
            Hide();
        }

        private void LateUpdate()
        {
            if (_remaining <= 0f)
            {
                Hide();
                return;
            }

            _remaining = Mathf.Max(0f, _remaining - Time.deltaTime);
            float progress = _duration > Mathf.Epsilon ? 1f - (_remaining / _duration) : 1f;
            float alpha = Mathf.Sin(progress * Mathf.PI);
            float scale = Mathf.Lerp(startScale, endScale, progress);

            ApplyPose(scale);
            SetAlpha(alpha);

            if (_remaining <= 0f)
            {
                Hide();
            }
        }

        public void Play(Vector2 direction)
        {
            EnsureRenderers();

            _playDirection = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.down;
            _duration = Mathf.Max(0.02f, lifetime);
            _remaining = _duration;
            _baseLocalScale = new Vector3(slashWidth, slashHeight, 1f);

            if (_coreRenderer != null)
            {
                _coreRenderer.enabled = true;
            }

            if (_edgeRenderer != null)
            {
                _edgeRenderer.enabled = true;
            }

            ApplyPose(startScale);
            SetAlpha(1f);
        }

        private void EnsureRenderers()
        {
            if (_slashSprite == null)
            {
                _slashSprite = CreateSlashSprite();
            }

            if (_coreRenderer == null)
            {
                _coreRenderer = CreateRenderer("SlashCore", coreColor, 28);
            }

            if (_edgeRenderer == null)
            {
                _edgeRenderer = CreateRenderer("SlashEdge", edgeColor, 27);
            }
        }

        private SpriteRenderer CreateRenderer(string objectName, Color color, int sortingOrder)
        {
            GameObject child = new(objectName);
            child.transform.SetParent(transform, false);
            SpriteRenderer renderer = child.AddComponent<SpriteRenderer>();
            renderer.sprite = _slashSprite;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
            return renderer;
        }

        private void ApplyPose(float scale)
        {
            Vector3 direction = new(_playDirection.x, _playDirection.y, 0f);
            Vector3 perpendicular = new(-_playDirection.y, _playDirection.x, 0f);
            Vector3 localPosition = (direction * forwardOffset) + (perpendicular * verticalLift);
            float angle = Mathf.Atan2(_playDirection.y, _playDirection.x) * Mathf.Rad2Deg;

            if (_coreRenderer != null)
            {
                Transform coreTransform = _coreRenderer.transform;
                coreTransform.localPosition = localPosition;
                coreTransform.localRotation = Quaternion.Euler(0f, 0f, angle - 22f);
                coreTransform.localScale = _baseLocalScale * scale;
            }

            if (_edgeRenderer != null)
            {
                Transform edgeTransform = _edgeRenderer.transform;
                edgeTransform.localPosition = localPosition + (Vector3)(perpendicular * 0.02f);
                edgeTransform.localRotation = Quaternion.Euler(0f, 0f, angle - 22f);
                edgeTransform.localScale = _baseLocalScale * (scale * 1.14f);
            }
        }

        private void SetAlpha(float alpha)
        {
            if (_coreRenderer != null)
            {
                Color color = coreColor;
                color.a *= alpha;
                _coreRenderer.color = color;
            }

            if (_edgeRenderer != null)
            {
                Color color = edgeColor;
                color.a *= alpha * 0.85f;
                _edgeRenderer.color = color;
            }
        }

        private void Hide()
        {
            if (_coreRenderer != null)
            {
                _coreRenderer.enabled = false;
            }

            if (_edgeRenderer != null)
            {
                _edgeRenderer.enabled = false;
            }
        }

        private static Sprite CreateSlashSprite()
        {
            Texture2D texture = new(TextureSize, TextureSize, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            for (int y = 0; y < TextureSize; y++)
            {
                for (int x = 0; x < TextureSize; x++)
                {
                    Vector2 uv = new(
                        (x + 0.5f) / TextureSize * 2f - 1f,
                        (y + 0.5f) / TextureSize * 2f - 1f);
                    float angle = Mathf.Atan2(uv.y, uv.x) * Mathf.Rad2Deg;
                    float radius = uv.magnitude;
                    float angleWindow = Mathf.InverseLerp(-72f, 72f, angle);
                    float inAngle = angleWindow > 0f && angleWindow < 1f ? 1f : 0f;
                    float band = 1f - Mathf.Abs(radius - 0.66f) / 0.16f;
                    float taper = Mathf.Sin(angleWindow * Mathf.PI);
                    float alpha = Mathf.Clamp01(band) * Mathf.Clamp01(taper) * inAngle;
                    alpha = Mathf.SmoothStep(0f, 1f, alpha);

                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, TextureSize, TextureSize), new Vector2(0.5f, 0.5f), TextureSize);
        }

        private void OnValidate()
        {
            slashWidth = Mathf.Max(0.1f, slashWidth);
            slashHeight = Mathf.Max(0.1f, slashHeight);
            lifetime = Mathf.Max(0.02f, lifetime);
            startScale = Mathf.Max(0f, startScale);
            endScale = Mathf.Max(0f, endScale);
        }
    }
}
