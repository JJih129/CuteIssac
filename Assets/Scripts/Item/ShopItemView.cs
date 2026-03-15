using CuteIssac.Data.Item;
using UnityEngine;

namespace CuteIssac.Item
{
    /// <summary>
    /// Presentation-only shop item view.
    /// Designers can swap sprites and highlight markers without changing purchase logic.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ShopItemView : MonoBehaviour
    {
        [Header("Visual References")]
        [SerializeField] private SpriteRenderer bodyRenderer;
        [SerializeField] private SpriteRenderer iconRenderer;
        [SerializeField] private SpriteRenderer highlightRenderer;
        [SerializeField] private SpriteRenderer soldOverlayRenderer;
        [SerializeField] private SpriteRenderer currencyMarkerRenderer;
        [SerializeField] private TextMesh nameText;
        [SerializeField] private TextMesh priceText;
        [SerializeField] private bool showWorldTextLabels;

        [Header("Colors")]
        [SerializeField] private Color availableBodyColor = new(0.94f, 0.94f, 0.94f, 1f);
        [SerializeField] private Color unaffordableBodyColor = new(0.62f, 0.62f, 0.62f, 1f);
        [SerializeField] private Color soldBodyColor = new(0.24f, 0.24f, 0.24f, 0.9f);
        [SerializeField] private Color highlightColor = new(1f, 1f, 1f, 0.85f);
        [SerializeField] private Color soldOverlayColor = new(0.2f, 0.2f, 0.2f, 0.55f);
        [SerializeField] private Color coinMarkerColor = new(1f, 0.85f, 0.25f, 1f);
        [SerializeField] private Color keyMarkerColor = new(0.7f, 0.9f, 1f, 1f);
        [SerializeField] private Color bombMarkerColor = new(1f, 0.55f, 0.2f, 1f);
        [SerializeField] [Min(0f)] private float highlightPulseSpeed = 6f;
        [SerializeField] [Range(0f, 1f)] private float highlightPulseAlphaFloor = 0.35f;
        [SerializeField] private Color purchaseFlashColor = new(1f, 0.92f, 0.46f, 1f);
        [SerializeField] [Min(0.05f)] private float purchaseFlashDuration = 0.28f;
        [SerializeField] [Min(1f)] private float purchaseScaleMultiplier = 1.12f;
        [SerializeField] [Min(0f)] private float purchaseScaleRecoverSpeed = 7.5f;

        private bool _isHighlighted;
        private float _purchaseFlashRemaining;
        private Vector3 _initialScale = Vector3.one;
        private bool _hasInitialScale;

        public void Present(ShopItemData shopItemData, bool canAfford, bool isHighlighted, bool isSold)
        {
            EnsureWorldLabelsState();
            _isHighlighted = isHighlighted && !isSold;
            ShopCurrencyType currencyType = shopItemData != null ? shopItemData.CurrencyType : ShopCurrencyType.Coins;

            if (bodyRenderer != null)
            {
                bodyRenderer.color = isSold
                    ? soldBodyColor
                    : (canAfford ? availableBodyColor : unaffordableBodyColor);
            }

            if (iconRenderer != null)
            {
                iconRenderer.sprite = shopItemData != null ? shopItemData.Icon : null;
                iconRenderer.enabled = iconRenderer.sprite != null;
                iconRenderer.color = isSold ? soldBodyColor : Color.white;
            }

            if (highlightRenderer != null)
            {
                highlightRenderer.gameObject.SetActive(_isHighlighted);
                highlightRenderer.color = highlightColor;
            }

            if (soldOverlayRenderer != null)
            {
                soldOverlayRenderer.gameObject.SetActive(isSold);
                soldOverlayRenderer.color = soldOverlayColor;
            }

            if (currencyMarkerRenderer != null)
            {
                currencyMarkerRenderer.color = currencyType switch
                {
                    ShopCurrencyType.Keys => keyMarkerColor,
                    ShopCurrencyType.Bombs => bombMarkerColor,
                    _ => coinMarkerColor
                };
            }

            if (nameText != null)
            {
                nameText.gameObject.SetActive(showWorldTextLabels);
                nameText.text = shopItemData != null ? shopItemData.DisplayName : string.Empty;
                nameText.color = isSold ? soldBodyColor : Color.white;
            }

            if (priceText != null)
            {
                priceText.gameObject.SetActive(showWorldTextLabels);
                priceText.text = shopItemData != null
                    ? $"{shopItemData.Price}{GetCurrencySuffix(currencyType)}"
                    : string.Empty;
                priceText.color = isSold
                    ? soldBodyColor
                    : (canAfford ? Color.white : unaffordableBodyColor);
            }
        }

        private void Update()
        {
            RecoverScale();

            if (_purchaseFlashRemaining > 0f)
            {
                _purchaseFlashRemaining -= Time.deltaTime;
                UpdatePurchaseFlash();
            }

            if (highlightRenderer == null || !_isHighlighted)
            {
                return;
            }

            Color nextColor = highlightColor;
            nextColor.a = Mathf.Lerp(highlightPulseAlphaFloor, highlightColor.a, 0.5f + (0.5f * Mathf.Sin(Time.time * highlightPulseSpeed)));
            highlightRenderer.color = nextColor;
        }

        public void PlayPurchaseSuccess()
        {
            CacheInitialScale();
            _purchaseFlashRemaining = purchaseFlashDuration;

            if (_hasInitialScale)
            {
                transform.localScale = _initialScale * purchaseScaleMultiplier;
            }

            if (highlightRenderer != null)
            {
                highlightRenderer.gameObject.SetActive(true);
                highlightRenderer.color = purchaseFlashColor;
            }

            if (soldOverlayRenderer != null)
            {
                soldOverlayRenderer.gameObject.SetActive(true);
            }
        }

        private void EnsureRuntimeLabels()
        {
            CacheInitialScale();

            if (nameText == null)
            {
                nameText = CreateRuntimeText("NameLabel", new Vector3(0f, 0.72f, 0f), 0.22f);
            }

            if (priceText == null)
            {
                priceText = CreateRuntimeText("PriceLabel", new Vector3(0f, -0.68f, 0f), 0.24f);
            }
        }

        private void EnsureWorldLabelsState()
        {
            if (showWorldTextLabels)
            {
                EnsureRuntimeLabels();
                return;
            }

            DisableAllWorldTextMeshes();
        }

        private void DisableAllWorldTextMeshes()
        {
            TextMesh[] textMeshes = GetComponentsInChildren<TextMesh>(true);
            for (int index = 0; index < textMeshes.Length; index++)
            {
                TextMesh candidate = textMeshes[index];
                if (candidate == null)
                {
                    continue;
                }

                candidate.text = string.Empty;
                candidate.gameObject.SetActive(false);
            }
        }

        private TextMesh CreateRuntimeText(string objectName, Vector3 localPosition, float characterSize)
        {
            TextMesh[] existingTexts = GetComponentsInChildren<TextMesh>(true);

            for (int i = 0; i < existingTexts.Length; i++)
            {
                TextMesh existingText = existingTexts[i];

                if (existingText != null && existingText.gameObject.name == objectName)
                {
                    return existingText;
                }
            }

            GameObject textObject = new(objectName);
            textObject.transform.SetParent(transform, false);
            textObject.transform.localPosition = localPosition;
            TextMesh textMesh = textObject.AddComponent<TextMesh>();
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.fontSize = 64;
            textMesh.characterSize = characterSize;
            textMesh.color = Color.white;
            CuteIssac.UI.LocalizedUiFontProvider.Apply(textMesh);
            return textMesh;
        }

        private static string GetCurrencySuffix(ShopCurrencyType currencyType)
        {
            return currencyType switch
            {
                ShopCurrencyType.Keys => "K",
                ShopCurrencyType.Bombs => "B",
                _ => "C"
            };
        }

        private void CacheInitialScale()
        {
            if (_hasInitialScale)
            {
                return;
            }

            _initialScale = transform.localScale;
            _hasInitialScale = true;
        }

        private void RecoverScale()
        {
            if (!_hasInitialScale)
            {
                return;
            }

            transform.localScale = Vector3.MoveTowards(
                transform.localScale,
                _initialScale,
                purchaseScaleRecoverSpeed * Time.deltaTime);
        }

        private void UpdatePurchaseFlash()
        {
            float duration = Mathf.Max(0.01f, purchaseFlashDuration);
            float normalized = Mathf.Clamp01(_purchaseFlashRemaining / duration);

            if (highlightRenderer != null)
            {
                Color flashColor = purchaseFlashColor;
                flashColor.a = Mathf.Lerp(0f, purchaseFlashColor.a, normalized);
                highlightRenderer.color = flashColor;
                highlightRenderer.gameObject.SetActive(normalized > 0.02f || _isHighlighted);
            }

            if (soldOverlayRenderer != null)
            {
                Color overlayColor = soldOverlayColor;
                overlayColor.a = Mathf.Max(soldOverlayColor.a, normalized * 0.75f);
                soldOverlayRenderer.color = overlayColor;
            }
        }
    }
}
