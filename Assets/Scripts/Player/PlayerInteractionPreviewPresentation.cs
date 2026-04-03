using CuteIssac.UI;
using UnityEngine;

namespace CuteIssac.Player
{
    [DisallowMultipleComponent]
    public sealed class PlayerInteractionPreviewPresentation : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerVisual playerVisual;

        [Header("Layout")]
        [SerializeField] private Vector3 rootOffset = new(0f, 1.34f, 0f);
        [SerializeField] [Min(0.01f)] private float bobAmplitude = 0.03f;
        [SerializeField] [Min(0.05f)] private float bobFrequency = 2.8f;
        [SerializeField] [Min(0.05f)] private float pulseFrequency = 4.8f;

        private static Sprite s_WhiteSprite;
        private static Sprite s_CircleSprite;

        private Transform _root;
        private SpriteRenderer _haloRenderer;
        private SpriteRenderer _underlineRenderer;
        private SpriteRenderer _accentRenderer;
        private SpriteRenderer _compareChipRenderer;
        private SpriteRenderer _outcomeChipRenderer;
        private TextMesh _titleText;
        private TextMesh _detailText;
        private TextMesh _compareText;
        private TextMesh _outcomeText;
        private Vector3 _baseLocalPosition;
        private Vector3 _haloBaseScale = Vector3.one;
        private Vector3 _underlineBaseScale = Vector3.one;
        private Vector3 _accentBaseScale = Vector3.one;
        private Vector3 _compareChipBaseScale = Vector3.one;
        private Vector3 _outcomeChipBaseScale = Vector3.one;
        private Color _accentColor = Color.white;
        private Color _compareColor = Color.white;
        private Color _outcomeColor = Color.white;
        private bool _strong;
        private bool _ready;
        private bool _emphasize;
        private bool _active;
        private float _visibility;
        private float _phaseSeed;

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

            float targetVisibility = _active ? 1f : 0f;
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

            float pulse = 0.5f + (Mathf.Sin((Time.unscaledTime * pulseFrequency) + _phaseSeed) * 0.5f);
            float bob = Mathf.Sin((Time.unscaledTime * bobFrequency) + _phaseSeed) * bobAmplitude;
            float emphasis = (_strong ? 1f : 0.82f) + (_ready ? 0.16f : 0f) + (_emphasize ? 0.18f : 0f);
            Color liveAccent = Color.Lerp(_accentColor, Color.white, (_ready ? 0.18f : 0.06f) + (_emphasize ? 0.08f : 0f));

            _root.localPosition = _baseLocalPosition + new Vector3(0f, bob, 0f);

            _haloRenderer.transform.localScale = _haloBaseScale * Mathf.Lerp(0.94f, 1.08f + (_ready ? 0.08f : 0f), pulse);
            _haloRenderer.color = new Color(liveAccent.r, liveAccent.g, liveAccent.b, _visibility * Mathf.Lerp(0.06f, 0.14f, pulse) * emphasis);

            _underlineRenderer.transform.localScale = _underlineBaseScale * Mathf.Lerp(0.96f, 1.04f + (_ready ? 0.08f : 0f), pulse);
            _underlineRenderer.color = new Color(liveAccent.r, liveAccent.g, liveAccent.b, _visibility * 0.88f * emphasis);

            _accentRenderer.transform.localScale = _accentBaseScale * Mathf.Lerp(0.92f, 1.1f + (_emphasize ? 0.08f : 0f), pulse);
            _accentRenderer.color = new Color(liveAccent.r, liveAccent.g, liveAccent.b, _visibility * Mathf.Lerp(0.26f, 0.54f, pulse) * emphasis);

            if (_compareChipRenderer != null && _compareChipRenderer.gameObject.activeSelf)
            {
                Color liveCompare = Color.Lerp(_compareColor, Color.white, (_ready ? 0.22f : 0.08f) + (_emphasize ? 0.08f : 0f));
                _compareChipRenderer.transform.localScale = _compareChipBaseScale * Mathf.Lerp(0.96f, 1.08f + (_ready ? 0.06f : 0f), pulse);
                _compareChipRenderer.color = new Color(liveCompare.r, liveCompare.g, liveCompare.b, _visibility * Mathf.Lerp(0.14f, 0.28f, pulse));

                if (_compareText != null && _compareText.gameObject.activeSelf)
                {
                    _compareText.color = new Color(1f, 1f, 1f, _visibility * 0.98f);

                    MeshRenderer compareRenderer = _compareText.GetComponent<MeshRenderer>();
                    if (compareRenderer != null)
                    {
                        compareRenderer.enabled = _visibility > 0.01f;
                    }
                }
            }

            if (_outcomeChipRenderer != null && _outcomeChipRenderer.gameObject.activeSelf)
            {
                Color liveOutcome = Color.Lerp(_outcomeColor, Color.white, (_ready ? 0.18f : 0.06f) + (_emphasize ? 0.06f : 0f));
                _outcomeChipRenderer.transform.localScale = _outcomeChipBaseScale * Mathf.Lerp(0.97f, 1.06f + (_ready ? 0.04f : 0f), pulse);
                _outcomeChipRenderer.color = new Color(liveOutcome.r, liveOutcome.g, liveOutcome.b, _visibility * Mathf.Lerp(0.12f, 0.24f, pulse));

                if (_outcomeText != null && _outcomeText.gameObject.activeSelf)
                {
                    _outcomeText.color = new Color(1f, 1f, 1f, _visibility * 0.92f);

                    MeshRenderer outcomeRenderer = _outcomeText.GetComponent<MeshRenderer>();
                    if (outcomeRenderer != null)
                    {
                        outcomeRenderer.enabled = _visibility > 0.01f;
                    }
                }
            }

            ApplyTextState(_titleText, liveAccent, true);
            ApplyTextState(_detailText, liveAccent, false);
        }

        public void SetPreview(
            string title,
            string detail,
            string compareLabel,
            string outcomeLabel,
            Color accentColor,
            Color compareColor,
            Color outcomeColor,
            bool strong,
            bool ready,
            bool emphasize)
        {
            EnsureVisuals();

            if (string.IsNullOrWhiteSpace(title))
            {
                ClearPreview();
                return;
            }

            _titleText.text = title;
            _titleText.gameObject.SetActive(true);

            _detailText.text = detail ?? string.Empty;
            _detailText.gameObject.SetActive(!string.IsNullOrWhiteSpace(_detailText.text));

            bool hasCompareLabel = !string.IsNullOrWhiteSpace(compareLabel);
            _compareText.text = hasCompareLabel ? compareLabel : string.Empty;
            _compareText.gameObject.SetActive(hasCompareLabel);
            _compareChipRenderer.gameObject.SetActive(hasCompareLabel);

            bool hasOutcomeLabel = !string.IsNullOrWhiteSpace(outcomeLabel);
            _outcomeText.text = hasOutcomeLabel ? outcomeLabel : string.Empty;
            _outcomeText.gameObject.SetActive(hasOutcomeLabel);
            _outcomeChipRenderer.gameObject.SetActive(hasOutcomeLabel);

            _accentColor = accentColor.a > 0.01f
                ? new Color(accentColor.r, accentColor.g, accentColor.b, 1f)
                : new Color(0.72f, 0.85f, 1f, 1f);
            _compareColor = compareColor.a > 0.01f
                ? new Color(compareColor.r, compareColor.g, compareColor.b, 1f)
                : Color.Lerp(_accentColor, Color.white, 0.16f);
            _outcomeColor = outcomeColor.a > 0.01f
                ? new Color(outcomeColor.r, outcomeColor.g, outcomeColor.b, 1f)
                : Color.Lerp(_accentColor, Color.white, 0.12f);
            _compareChipBaseScale = hasCompareLabel
                ? new Vector3(Mathf.Clamp(0.5f + (compareLabel.Length * 0.062f), 0.62f, 1.54f), 0.2f, 1f)
                : new Vector3(0.72f, 0.2f, 1f);
            _outcomeChipBaseScale = hasOutcomeLabel
                ? new Vector3(Mathf.Clamp(0.54f + (outcomeLabel.Length * 0.042f), 0.78f, 1.82f), 0.17f, 1f)
                : new Vector3(0.84f, 0.17f, 1f);
            _strong = strong;
            _ready = ready;
            _emphasize = emphasize;
            _active = true;

            if (!_root.gameObject.activeSelf)
            {
                _root.gameObject.SetActive(true);
            }
        }

        public void ClearPreview()
        {
            _active = false;
            _ready = false;
        }

        private void ResolveReferences()
        {
            if (playerVisual == null)
            {
                TryGetComponent(out playerVisual);
            }
        }

        private void EnsureVisuals()
        {
            if (_root != null)
            {
                return;
            }

            GameObject rootObject = new("PlayerInteractionPreview");
            rootObject.layer = gameObject.layer;
            _root = rootObject.transform;
            _root.SetParent(transform, false);
            _baseLocalPosition = rootOffset;
            _root.localPosition = _baseLocalPosition;

            _haloRenderer = CreateSpritePart(
                "PreviewHalo",
                GetCircleSprite(),
                new Vector3(0f, 0.08f, 0f),
                new Vector3(1.62f, 0.78f, 1f),
                new Color(1f, 1f, 1f, 0f),
                5);
            _underlineRenderer = CreateSpritePart(
                "PreviewUnderline",
                GetWhiteSprite(),
                new Vector3(0f, -0.26f, 0f),
                new Vector3(1.34f, 0.045f, 1f),
                new Color(1f, 1f, 1f, 0f),
                7);
            _accentRenderer = CreateSpritePart(
                "PreviewAccent",
                GetWhiteSprite(),
                new Vector3(-0.58f, 0.22f, 0f),
                new Vector3(0.08f, 0.38f, 1f),
                new Color(1f, 1f, 1f, 0f),
                8);
            _compareChipRenderer = CreateSpritePart(
                "PreviewCompareChip",
                GetWhiteSprite(),
                new Vector3(0f, 0.42f, 0f),
                new Vector3(0.72f, 0.2f, 1f),
                new Color(1f, 1f, 1f, 0f),
                9);
            _outcomeChipRenderer = CreateSpritePart(
                "PreviewOutcomeChip",
                GetWhiteSprite(),
                new Vector3(0f, -0.36f, 0f),
                new Vector3(0.84f, 0.17f, 1f),
                new Color(1f, 1f, 1f, 0f),
                8);

            _compareText = CreateTextPart("CompareText", new Vector3(0f, 0.42f, 0f), 34, 0.05f, true, 11);
            _titleText = CreateTextPart("TitleText", new Vector3(0f, 0.12f, 0f), 56, 0.08f, true, 10);
            _detailText = CreateTextPart("DetailText", new Vector3(0f, -0.12f, 0f), 42, 0.062f, false, 9);
            _outcomeText = CreateTextPart("OutcomeText", new Vector3(0f, -0.36f, 0f), 32, 0.048f, true, 10);

            _haloBaseScale = _haloRenderer.transform.localScale;
            _underlineBaseScale = _underlineRenderer.transform.localScale;
            _accentBaseScale = _accentRenderer.transform.localScale;
            _compareChipBaseScale = _compareChipRenderer.transform.localScale;
            _outcomeChipBaseScale = _outcomeChipRenderer.transform.localScale;
            HideImmediate();
        }

        private SpriteRenderer CreateSpritePart(
            string objectName,
            Sprite sprite,
            Vector3 localPosition,
            Vector3 localScale,
            Color color,
            int sortingOffset)
        {
            GameObject child = new(objectName);
            child.layer = gameObject.layer;
            child.transform.SetParent(_root, false);
            child.transform.localPosition = localPosition;
            child.transform.localScale = localScale;

            SpriteRenderer renderer = child.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            SpriteRenderer bodyRenderer = playerVisual != null ? playerVisual.BodySpriteRenderer : null;
            if (bodyRenderer != null)
            {
                renderer.sortingLayerID = bodyRenderer.sortingLayerID;
            }
            renderer.sortingOrder = ResolveSortingOrder(sortingOffset);
            return renderer;
        }

        private TextMesh CreateTextPart(
            string objectName,
            Vector3 localPosition,
            int fontSize,
            float characterSize,
            bool isTitle,
            int sortingOffset)
        {
            GameObject child = new(objectName);
            child.layer = gameObject.layer;
            child.transform.SetParent(_root, false);
            child.transform.localPosition = localPosition;

            TextMesh textMesh = child.AddComponent<TextMesh>();
            textMesh.text = string.Empty;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.fontSize = fontSize;
            textMesh.characterSize = characterSize;
            textMesh.fontStyle = isTitle ? FontStyle.Bold : FontStyle.Normal;
            textMesh.richText = false;
            LocalizedUiFontProvider.Apply(textMesh);

            MeshRenderer meshRenderer = textMesh.GetComponent<MeshRenderer>();
            if (meshRenderer != null)
            {
                SpriteRenderer bodyRenderer = playerVisual != null ? playerVisual.BodySpriteRenderer : null;
                if (bodyRenderer != null)
                {
                    meshRenderer.sortingLayerID = bodyRenderer.sortingLayerID;
                }
                meshRenderer.sortingOrder = ResolveSortingOrder(sortingOffset);
            }

            return textMesh;
        }

        private void ApplyTextState(TextMesh textMesh, Color accentColor, bool isTitle)
        {
            if (textMesh == null || !textMesh.gameObject.activeSelf)
            {
                return;
            }

            float alpha = _visibility * (isTitle ? 0.96f : 0.76f) * (_strong ? 1f : 0.88f);
            Color textColor = isTitle
                ? Color.Lerp(accentColor, Color.white, 0.46f)
                : Color.Lerp(accentColor, Color.white, 0.18f);
            textMesh.color = new Color(textColor.r, textColor.g, textColor.b, alpha);

            MeshRenderer meshRenderer = textMesh.GetComponent<MeshRenderer>();
            if (meshRenderer != null)
            {
                meshRenderer.enabled = alpha > 0.01f;
            }
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
                name = "RuntimePlayerInteractionPreviewCircle"
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
