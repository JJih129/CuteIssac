using UnityEngine;

namespace CuteIssac.Room
{
    [DisallowMultipleComponent]
    public sealed class FloorExitVisual : MonoBehaviour
    {
        [Header("Optional References")]
        [SerializeField] private SpriteRenderer baseRenderer;
        [SerializeField] private SpriteRenderer glowRenderer;
        [SerializeField] private SpriteRenderer coreRenderer;
        [SerializeField] private SpriteRenderer promptRenderer;
        [SerializeField] private TextMesh promptLabelText;
        [SerializeField] private bool showWorldPromptLabel = true;

        [Header("Animation")]
        [SerializeField] [Min(0.1f)] private float pulseSpeed = 3.2f;
        [SerializeField] [Min(0f)] private float glowScaleAmplitude = 0.08f;
        [SerializeField] [Min(0f)] private float bobHeight = 0.08f;
        [SerializeField] [Min(0.1f)] private float bobSpeed = 2.4f;
        [SerializeField] [Min(0f)] private float coreRotationSpeed = 36f;

        private Color _accentColor = new(0.66f, 0.9f, 1f, 1f);
        private Vector3 _baseGlowScale = Vector3.one;
        private Vector3 _baseBaseScale = Vector3.one;
        private Vector3 _baseCoreScale = Vector3.one;
        private Vector3 _baseBaseLocalPosition = Vector3.zero;
        private Vector3 _baseGlowLocalPosition = Vector3.zero;
        private Vector3 _baseCoreLocalPosition = Vector3.zero;
        private static Sprite _portalSprite;
        private static Sprite _promptSprite;

        private void Awake()
        {
            ResolveReferences();
        }

        private void Update()
        {
            if (glowRenderer == null)
            {
                return;
            }

            float pulse = 0.5f + Mathf.Sin(Time.time * pulseSpeed) * 0.5f;
            float bobOffset = Mathf.Sin(Time.time * bobSpeed) * bobHeight;
            Color glowColor = _accentColor;
            glowColor.a = Mathf.Lerp(0.18f, 0.44f, pulse);
            glowRenderer.color = glowColor;
            glowRenderer.transform.localScale = _baseGlowScale * (1f + pulse * glowScaleAmplitude);
            glowRenderer.transform.localPosition = _baseGlowLocalPosition + new Vector3(0f, bobOffset, 0f);

            if (baseRenderer != null)
            {
                Color baseColor = _accentColor;
                baseColor.a = 0.86f;
                baseRenderer.color = baseColor;
                baseRenderer.transform.localScale = _baseBaseScale * Mathf.Lerp(0.96f, 1.08f, pulse);
                baseRenderer.transform.localPosition = _baseBaseLocalPosition + new Vector3(0f, bobOffset * 0.9f, 0f);
            }

            if (coreRenderer != null)
            {
                Color coreColor = Color.Lerp(Color.white, _accentColor, 0.35f);
                coreColor.a = 0.96f;
                coreRenderer.color = coreColor;
                coreRenderer.transform.localScale = _baseCoreScale * Mathf.Lerp(0.94f, 1.06f, pulse);
                coreRenderer.transform.localPosition = _baseCoreLocalPosition + new Vector3(0f, bobOffset * 1.1f, 0f);
                coreRenderer.transform.localRotation = Quaternion.Euler(0f, 0f, Time.time * coreRotationSpeed);
            }
        }

        public void Configure(int targetFloorIndex, Color accentColor)
        {
            ResolveReferences();
            _accentColor = accentColor;

            if (baseRenderer != null)
            {
                Color baseColor = accentColor * 0.92f;
                baseColor.a = 1f;
                baseRenderer.color = baseColor;
            }

            if (coreRenderer != null)
            {
                Color coreColor = Color.Lerp(Color.white, accentColor, 0.4f);
                coreColor.a = 0.96f;
                coreRenderer.color = coreColor;
            }

            if (promptRenderer != null)
            {
                promptRenderer.color = new Color(0.08f, 0.12f, 0.18f, 0.78f);
                promptRenderer.enabled = showWorldPromptLabel;
            }

            if (promptLabelText != null)
            {
                promptLabelText.text = $"E키로 다음 층 이동\n{targetFloorIndex}층 포탈";
                promptLabelText.color = Color.white;
                CuteIssac.UI.LocalizedUiFontProvider.Apply(promptLabelText);
                promptLabelText.gameObject.SetActive(showWorldPromptLabel);
            }

            SetPromptVisible(showWorldPromptLabel);
        }

        public void SetPromptVisible(bool visible)
        {
            if (!showWorldPromptLabel)
            {
                visible = false;
            }

            if (promptRenderer != null)
            {
                promptRenderer.enabled = visible;
            }

            if (promptLabelText != null)
            {
                promptLabelText.gameObject.SetActive(visible);
            }
        }

        public void SetWorldPromptEnabled(bool enabled)
        {
            showWorldPromptLabel = enabled;
            ResolveReferences();
        }

        public void PlayActivateFeedback()
        {
            if (baseRenderer != null)
            {
                Color activatedColor = Color.white;
                activatedColor.a = 1f;
                baseRenderer.color = activatedColor;
            }

            if (coreRenderer != null)
            {
                Color activatedCore = Color.white;
                activatedCore.a = 1f;
                coreRenderer.color = activatedCore;
            }
        }

        private void ResolveReferences()
        {
            EnsureFallbackVisuals();
            _baseGlowScale = glowRenderer != null ? glowRenderer.transform.localScale : Vector3.one;
            _baseBaseScale = baseRenderer != null ? baseRenderer.transform.localScale : Vector3.one;
            _baseCoreScale = coreRenderer != null ? coreRenderer.transform.localScale : Vector3.one;
            _baseBaseLocalPosition = baseRenderer != null ? baseRenderer.transform.localPosition : Vector3.zero;
            _baseGlowLocalPosition = glowRenderer != null ? glowRenderer.transform.localPosition : Vector3.zero;
            _baseCoreLocalPosition = coreRenderer != null ? coreRenderer.transform.localPosition : Vector3.zero;
        }

        private void EnsureFallbackVisuals()
        {
            Sprite portalSprite = GetPortalSprite();
            Sprite promptSprite = GetPromptSprite();

            if (baseRenderer == null)
            {
                baseRenderer = CreateRenderer("Base", portalSprite, new Vector2(0f, 0f), new Vector2(1.1f, 1.1f), 18);
            }

            if (glowRenderer == null)
            {
                glowRenderer = CreateRenderer("Glow", portalSprite, new Vector2(0f, 0f), new Vector2(1.75f, 1.75f), 17);
            }

            if (coreRenderer == null)
            {
                coreRenderer = CreateRenderer("Core", portalSprite, new Vector2(0f, 0.02f), new Vector2(0.52f, 0.52f), 19);
            }

            if (promptRenderer == null)
            {
                promptRenderer = CreateRenderer("Prompt", promptSprite, new Vector2(0f, 1.22f), new Vector2(2.2f, 0.32f), 21);
                Color promptColor = new(0.08f, 0.12f, 0.18f, 0.78f);
                promptRenderer.color = promptColor;
            }

            if (showWorldPromptLabel && promptLabelText == null)
            {
                GameObject promptTextObject = new("PromptLabel");
                promptTextObject.transform.SetParent(transform, false);
                promptTextObject.transform.localPosition = new Vector3(0f, 1.84f, 0f);
                promptLabelText = promptTextObject.AddComponent<TextMesh>();
                promptLabelText.anchor = TextAnchor.MiddleCenter;
                promptLabelText.alignment = TextAlignment.Center;
                promptLabelText.fontSize = 88;
                promptLabelText.characterSize = 0.16f;
                promptLabelText.color = new Color(1f, 0.98f, 0.9f, 1f);
                CuteIssac.UI.LocalizedUiFontProvider.Apply(promptLabelText);
                promptLabelText.gameObject.SetActive(true);
            }

            if (!showWorldPromptLabel && promptLabelText != null)
            {
                promptLabelText.text = string.Empty;
                promptLabelText.gameObject.SetActive(false);
            }
        }

        private SpriteRenderer CreateRenderer(string name, Sprite sprite, Vector2 localPosition, Vector2 localScale, int order)
        {
            GameObject rendererObject = new(name);
            rendererObject.transform.SetParent(transform, false);
            rendererObject.transform.localPosition = localPosition;
            rendererObject.transform.localScale = new Vector3(localScale.x, localScale.y, 1f);

            SpriteRenderer spriteRenderer = rendererObject.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = sprite;
            spriteRenderer.sortingOrder = order;
            spriteRenderer.drawMode = SpriteDrawMode.Simple;
            return spriteRenderer;
        }

        private static Sprite GetPortalSprite()
        {
            if (_portalSprite != null)
            {
                return _portalSprite;
            }

            const int size = 64;
            Texture2D texture = new(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                name = "RuntimeFloorExitPortal"
            };

            Vector2 center = new((size - 1) * 0.5f, (size - 1) * 0.5f);
            float maxRadius = size * 0.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), center) / maxRadius;
                    float alpha = Mathf.Clamp01(1f - distance);
                    alpha = Mathf.Pow(alpha, 2.2f);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply();
            _portalSprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f),
                100f);
            return _portalSprite;
        }

        private static Sprite GetPromptSprite()
        {
            if (_promptSprite != null)
            {
                return _promptSprite;
            }

            _promptSprite = Sprite.Create(
                Texture2D.whiteTexture,
                new Rect(0f, 0f, Texture2D.whiteTexture.width, Texture2D.whiteTexture.height),
                new Vector2(0.5f, 0.5f),
                100f);
            return _promptSprite;
        }
    }
}
