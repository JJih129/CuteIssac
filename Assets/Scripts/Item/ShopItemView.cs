using CuteIssac.Data.Item;
using CuteIssac.Data.Visual;
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
        private const int NameTextSortingOrder = 38;
        private const int NameShadowSortingOrder = 37;
        private const int PriceTextSortingOrder = 42;
        private const int PriceShadowSortingOrder = 41;

        [Header("Visual References")]
        [SerializeField] private SpriteRenderer bodyRenderer;
        [SerializeField] private SpriteRenderer iconRenderer;
        [SerializeField] private SpriteRenderer highlightRenderer;
        [SerializeField] private SpriteRenderer soldOverlayRenderer;
        [SerializeField] private SpriteRenderer currencyMarkerRenderer;
        [SerializeField] private TextMesh nameText;
        [SerializeField] private TextMesh priceText;
        [SerializeField] private TextMesh nameShadowText;
        [SerializeField] private TextMesh priceShadowText;
        [SerializeField] private bool showWorldTextLabels = true;
        [SerializeField] private bool showWorldNameLabel;
        [SerializeField] private bool showCurrencyMarker;

        [Header("Visual Profile")]
        [SerializeField] private ShopItemVisualProfile visualProfile;

        [Header("World Price Label")]
        [Tooltip("Local position of the world price label under the shop item.")]
        [SerializeField] private Vector3 priceLabelLocalPosition = new(0f, -0.9f, -0.01f);
        [Tooltip("Local offset for the world price label shadow.")]
        [SerializeField] private Vector3 priceShadowLocalOffset = new(0.04f, -0.045f, 0.02f);
        [Tooltip("Character size for the world price label.")]
        [SerializeField] [Min(0.01f)] private float priceLabelCharacterSize = 0.3f;
        [Tooltip("Suffix appended after coin prices. Default example: 5 won.")]
        [SerializeField] private string coinPriceSuffix = "\uC6D0";

        [Header("Colors")]
        [SerializeField] private Color availableBodyColor = new(0.94f, 0.94f, 0.94f, 1f);
        [SerializeField] private Color unaffordableBodyColor = new(0.62f, 0.62f, 0.62f, 1f);
        [SerializeField] private Color soldBodyColor = new(0.24f, 0.24f, 0.24f, 0.9f);
        [SerializeField] private Color highlightColor = new(1f, 1f, 1f, 0.85f);
        [SerializeField] private Color soldOverlayColor = new(0.2f, 0.2f, 0.2f, 0.55f);
        [SerializeField] private Color coinMarkerColor = new(1f, 0.85f, 0.25f, 1f);
        [SerializeField] private Color keyMarkerColor = new(0.7f, 0.9f, 1f, 1f);
        [SerializeField] private Color bombMarkerColor = new(1f, 0.55f, 0.2f, 1f);
        [SerializeField] private Color healthMarkerColor = new(1f, 0.28f, 0.42f, 1f);
        [SerializeField] private Color availablePriceTextColor = new(1f, 0.98f, 0.78f, 1f);
        [SerializeField] private Color unavailablePriceTextColor = new(1f, 0.42f, 0.34f, 1f);
        [SerializeField] private Color soldPriceTextColor = new(0.66f, 0.66f, 0.66f, 0.92f);
        [SerializeField] private Color worldTextShadowColor = new(0.08f, 0.04f, 0.03f, 0.82f);
        [SerializeField] [Min(0f)] private float highlightPulseSpeed = 6f;
        [SerializeField] [Range(0f, 1f)] private float highlightPulseAlphaFloor = 0.35f;
        [SerializeField] private Color purchaseFlashColor = new(1f, 0.92f, 0.46f, 1f);
        [SerializeField] [Min(0.05f)] private float purchaseFlashDuration = 0.28f;
        [SerializeField] private Color purchaseFailureFlashColor = new(1f, 0.24f, 0.18f, 0.95f);
        [SerializeField] [Min(0.05f)] private float purchaseFailureFlashDuration = 0.22f;
        [SerializeField] [Min(1f)] private float purchaseScaleMultiplier = 1.12f;
        [SerializeField] [Min(0f)] private float purchaseScaleRecoverSpeed = 7.5f;
        [SerializeField] [Min(1f)] private float weaponBodyScaleMultiplier = 1.4f;
        [SerializeField] [Min(1f)] private float weaponIconScaleMultiplier = 2.8f;
        [SerializeField] [Min(1f)] private float weaponHighlightScaleMultiplier = 1.55f;

        private bool _isHighlighted;
        private float _purchaseFlashRemaining;
        private float _activeFlashDuration;
        private Color _activeFlashColor;
        private bool _showSoldOverlayDuringFlash;
        private Vector3 _initialScale = Vector3.one;
        private bool _hasInitialScale;
        private Vector3 _baseBodyScale = Vector3.one;
        private Vector3 _baseIconScale = Vector3.one;
        private Vector3 _baseHighlightScale = Vector3.one;
        private Vector3 _baseSoldOverlayScale = Vector3.one;
        private Vector3 _baseCurrencyMarkerScale = Vector3.one;
        private bool _hasCapturedVisualScales;
        private ShopItemVisualProfile _appliedVisualProfile;

        public void ConfigureVisualProfile(ShopItemVisualProfile profile)
        {
            visualProfile = profile;
            ApplyVisualProfileIfAvailable();
            _hasCapturedVisualScales = false;
        }

        public void Present(ShopItemData shopItemData, int effectivePrice, bool canAfford, bool isHighlighted, bool isSold)
        {
            ApplyVisualProfileIfAvailable();
            EnsureWorldLabelsState();
            _isHighlighted = isHighlighted && !isSold;
            ShopCurrencyType currencyType = shopItemData != null ? shopItemData.CurrencyType : ShopCurrencyType.Coins;
            bool isWeaponOffer = shopItemData != null
                && shopItemData.Offer.RewardType == ShopOfferRewardType.PassiveItem
                && shopItemData.Offer.PassiveItem != null
                && shopItemData.Offer.PassiveItem.IsWeaponRelic;
            ApplyScaleProfile(isWeaponOffer);

            if (bodyRenderer != null)
            {
                bodyRenderer.color = isSold
                    ? soldBodyColor
                    : (canAfford ? availableBodyColor : unaffordableBodyColor);
            }

            if (iconRenderer != null)
            {
                iconRenderer.sprite = shopItemData != null ? shopItemData.ShopDisplaySprite : null;
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
                currencyMarkerRenderer.gameObject.SetActive(showCurrencyMarker && shopItemData != null && !isSold);
                currencyMarkerRenderer.color = currencyType switch
                {
                    ShopCurrencyType.Keys => keyMarkerColor,
                    ShopCurrencyType.Bombs => bombMarkerColor,
                    ShopCurrencyType.Health => healthMarkerColor,
                    _ => coinMarkerColor
                };
            }

            string displayName = shopItemData != null ? shopItemData.DisplayName : string.Empty;
            bool showName = showWorldTextLabels && showWorldNameLabel && !string.IsNullOrEmpty(displayName);
            PresentWorldText(nameText, nameShadowText, showName, displayName, isSold ? soldBodyColor : Color.white);

            string priceValue = shopItemData != null ? (isSold ? "SOLD" : BuildWorldPriceText(effectivePrice, currencyType)) : string.Empty;
            Color priceColor = isSold ? soldPriceTextColor : (canAfford ? availablePriceTextColor : unavailablePriceTextColor);
            PresentWorldText(priceText, priceShadowText, showWorldTextLabels && !string.IsNullOrEmpty(priceValue), priceValue, priceColor);
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
            BeginFeedbackFlash(purchaseFlashColor, purchaseFlashDuration, purchaseScaleMultiplier, true);
        }

        public void PlayPurchaseFailure()
        {
            BeginFeedbackFlash(purchaseFailureFlashColor, purchaseFailureFlashDuration, 1f, false);
        }

        public void ConfigureRuntimeReferences(
            SpriteRenderer body,
            SpriteRenderer icon,
            SpriteRenderer highlight,
            SpriteRenderer soldOverlay,
            SpriteRenderer currencyMarker,
            bool showLabels)
        {
            bodyRenderer = body;
            iconRenderer = icon;
            highlightRenderer = highlight;
            soldOverlayRenderer = soldOverlay;
            currencyMarkerRenderer = currencyMarker;
            showWorldTextLabels = showLabels;
            ApplyVisualProfileIfAvailable();
            _hasCapturedVisualScales = false;
            CacheInitialScale();
            EnsureWorldLabelsState();
        }

        public void ConfigureWorldPriceOnly()
        {
            showWorldTextLabels = true;
            showWorldNameLabel = false;
            showCurrencyMarker = false;
            ApplyVisualProfileIfAvailable();
            EnsureWorldLabelsState();
        }

        public bool TryGetInteractionDistanceSqr(Vector3 worldPosition, out float distanceSqr)
        {
            distanceSqr = 0f;

            if (!TryGetInteractionBounds(out Bounds bounds))
            {
                return false;
            }

            worldPosition.z = bounds.center.z;
            distanceSqr = bounds.SqrDistance(worldPosition);
            return true;
        }

        public bool TryGetInteractionBounds(out Bounds bounds)
        {
            bounds = default;
            bool hasBounds = false;

            TryEncapsulateRendererBounds(bodyRenderer, ref bounds, ref hasBounds);
            TryEncapsulateRendererBounds(iconRenderer, ref bounds, ref hasBounds);

            return hasBounds;
        }

        private void EnsureRuntimeLabels()
        {
            CacheInitialScale();

            if (nameText == null)
            {
                nameText = CreateRuntimeText("NameLabel", new Vector3(0f, 0.72f, 0f), 0.22f, NameTextSortingOrder);
            }

            if (nameShadowText == null)
            {
                nameShadowText = CreateRuntimeText("NameShadow", new Vector3(0.035f, 0.685f, 0.01f), 0.22f, NameShadowSortingOrder);
            }

            if (priceText == null)
            {
                priceText = CreateRuntimeText("PriceLabel", priceLabelLocalPosition, priceLabelCharacterSize, PriceTextSortingOrder);
            }

            ConfigureRuntimeTextLayout(priceText, priceLabelLocalPosition, priceLabelCharacterSize, PriceTextSortingOrder, FontStyle.Bold);

            if (priceShadowText == null)
            {
                priceShadowText = CreateRuntimeText("PriceShadow", priceLabelLocalPosition + priceShadowLocalOffset, priceLabelCharacterSize, PriceShadowSortingOrder);
            }

            ConfigureRuntimeTextLayout(priceShadowText, priceLabelLocalPosition + priceShadowLocalOffset, priceLabelCharacterSize, PriceShadowSortingOrder, FontStyle.Bold);
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

        private TextMesh CreateRuntimeText(string objectName, Vector3 localPosition, float characterSize, int sortingOrder)
        {
            TextMesh[] existingTexts = GetComponentsInChildren<TextMesh>(true);

            for (int i = 0; i < existingTexts.Length; i++)
            {
                TextMesh existingText = existingTexts[i];

                if (existingText != null && existingText.gameObject.name == objectName)
                {
                    ConfigureTextRenderer(existingText, sortingOrder);
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
            ConfigureTextRenderer(textMesh, sortingOrder);
            return textMesh;
        }

        private void ConfigureRuntimeTextLayout(TextMesh textMesh, Vector3 localPosition, float characterSize, int sortingOrder, FontStyle fontStyle)
        {
            if (textMesh == null)
            {
                return;
            }

            textMesh.transform.localPosition = localPosition;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.fontSize = 64;
            textMesh.characterSize = Mathf.Max(0.01f, characterSize);
            textMesh.fontStyle = fontStyle;
            CuteIssac.UI.LocalizedUiFontProvider.Apply(textMesh);
            ConfigureTextRenderer(textMesh, sortingOrder);
        }

        private void PresentWorldText(TextMesh text, TextMesh shadow, bool visible, string value, Color color)
        {
            if (text != null)
            {
                text.gameObject.SetActive(visible);
                text.text = visible ? value : string.Empty;
                text.color = color;
                SetTextRendererEnabled(text, visible);
            }

            if (shadow != null)
            {
                shadow.gameObject.SetActive(visible);
                shadow.text = visible ? value : string.Empty;
                shadow.color = worldTextShadowColor;
                SetTextRendererEnabled(shadow, visible);
            }
        }

        private void ConfigureTextRenderer(TextMesh textMesh, int sortingOrder)
        {
            if (textMesh == null)
            {
                return;
            }

            MeshRenderer meshRenderer = textMesh.GetComponent<MeshRenderer>();
            if (meshRenderer == null)
            {
                return;
            }

            if (bodyRenderer != null)
            {
                meshRenderer.sortingLayerID = bodyRenderer.sortingLayerID;
            }

            meshRenderer.sortingOrder = sortingOrder;

            if (textMesh.font != null)
            {
                meshRenderer.sharedMaterial = textMesh.font.material;
            }
        }

        private static void SetTextRendererEnabled(TextMesh textMesh, bool enabled)
        {
            MeshRenderer meshRenderer = textMesh != null ? textMesh.GetComponent<MeshRenderer>() : null;
            if (meshRenderer != null)
            {
                meshRenderer.enabled = enabled;
            }
        }

        private static void TryEncapsulateRendererBounds(SpriteRenderer renderer, ref Bounds bounds, ref bool hasBounds)
        {
            if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy || renderer.sprite == null)
            {
                return;
            }

            if (!hasBounds)
            {
                bounds = renderer.bounds;
                hasBounds = true;
                return;
            }

            bounds.Encapsulate(renderer.bounds);
        }

        private string BuildWorldPriceText(int effectivePrice, ShopCurrencyType currencyType)
        {
            return ShopPriceLabelFormatter.FormatPrice(
                effectivePrice,
                currencyType,
                ShopPriceLabelStyle.Compact,
                coinPriceSuffix);
        }

        private void ApplyVisualProfileIfAvailable()
        {
            ShopItemVisualProfile resolvedProfile = ShopItemVisualProfile.Resolve(visualProfile);
            if (resolvedProfile == null || _appliedVisualProfile == resolvedProfile)
            {
                return;
            }

            _appliedVisualProfile = resolvedProfile;
            priceLabelLocalPosition = resolvedProfile.PriceLabelLocalPosition;
            priceShadowLocalOffset = resolvedProfile.PriceShadowLocalOffset;
            priceLabelCharacterSize = resolvedProfile.PriceLabelCharacterSize;
            coinPriceSuffix = resolvedProfile.CoinPriceSuffix;
            availableBodyColor = resolvedProfile.AvailableBodyColor;
            unaffordableBodyColor = resolvedProfile.UnaffordableBodyColor;
            soldBodyColor = resolvedProfile.SoldBodyColor;
            highlightColor = resolvedProfile.HighlightColor;
            soldOverlayColor = resolvedProfile.SoldOverlayColor;
            coinMarkerColor = resolvedProfile.CoinMarkerColor;
            keyMarkerColor = resolvedProfile.KeyMarkerColor;
            bombMarkerColor = resolvedProfile.BombMarkerColor;
            healthMarkerColor = resolvedProfile.HealthMarkerColor;
            availablePriceTextColor = resolvedProfile.AvailablePriceTextColor;
            unavailablePriceTextColor = resolvedProfile.UnavailablePriceTextColor;
            soldPriceTextColor = resolvedProfile.SoldPriceTextColor;
            worldTextShadowColor = resolvedProfile.WorldTextShadowColor;
            highlightPulseSpeed = resolvedProfile.HighlightPulseSpeed;
            highlightPulseAlphaFloor = resolvedProfile.HighlightPulseAlphaFloor;
            purchaseFlashColor = resolvedProfile.PurchaseFlashColor;
            purchaseFlashDuration = resolvedProfile.PurchaseFlashDuration;
            purchaseFailureFlashColor = resolvedProfile.PurchaseFailureFlashColor;
            purchaseFailureFlashDuration = resolvedProfile.PurchaseFailureFlashDuration;
            purchaseScaleMultiplier = resolvedProfile.PurchaseScaleMultiplier;
            purchaseScaleRecoverSpeed = resolvedProfile.PurchaseScaleRecoverSpeed;
            weaponBodyScaleMultiplier = resolvedProfile.WeaponBodyScaleMultiplier;
            weaponIconScaleMultiplier = resolvedProfile.WeaponIconScaleMultiplier;
            weaponHighlightScaleMultiplier = resolvedProfile.WeaponHighlightScaleMultiplier;
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

        private void ApplyScaleProfile(bool isWeaponOffer)
        {
            CaptureVisualScales();

            float bodyMultiplier = isWeaponOffer ? Mathf.Max(1f, weaponBodyScaleMultiplier) : 1f;
            float iconMultiplier = isWeaponOffer ? Mathf.Max(1f, weaponIconScaleMultiplier) : 1f;
            float highlightMultiplier = isWeaponOffer ? Mathf.Max(1f, weaponHighlightScaleMultiplier) : 1f;

            if (bodyRenderer != null)
            {
                bodyRenderer.transform.localScale = _baseBodyScale * bodyMultiplier;
            }

            if (iconRenderer != null)
            {
                iconRenderer.transform.localScale = _baseIconScale * iconMultiplier;
            }

            if (highlightRenderer != null)
            {
                highlightRenderer.transform.localScale = _baseHighlightScale * highlightMultiplier;
            }

            if (soldOverlayRenderer != null)
            {
                soldOverlayRenderer.transform.localScale = _baseSoldOverlayScale * bodyMultiplier;
            }

            if (currencyMarkerRenderer != null)
            {
                currencyMarkerRenderer.transform.localScale = _baseCurrencyMarkerScale * Mathf.Lerp(1f, 1.22f, isWeaponOffer ? 1f : 0f);
            }
        }

        private void CaptureVisualScales()
        {
            if (_hasCapturedVisualScales)
            {
                return;
            }

            _baseBodyScale = bodyRenderer != null ? bodyRenderer.transform.localScale : Vector3.one;
            _baseIconScale = iconRenderer != null ? iconRenderer.transform.localScale : Vector3.one;
            _baseHighlightScale = highlightRenderer != null ? highlightRenderer.transform.localScale : Vector3.one;
            _baseSoldOverlayScale = soldOverlayRenderer != null ? soldOverlayRenderer.transform.localScale : Vector3.one;
            _baseCurrencyMarkerScale = currencyMarkerRenderer != null ? currencyMarkerRenderer.transform.localScale : Vector3.one;
            _hasCapturedVisualScales = true;
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
            float duration = Mathf.Max(0.01f, _activeFlashDuration);
            float normalized = Mathf.Clamp01(_purchaseFlashRemaining / duration);

            if (highlightRenderer != null)
            {
                Color flashColor = _activeFlashColor;
                flashColor.a = Mathf.Lerp(0f, _activeFlashColor.a, normalized);
                highlightRenderer.color = flashColor;
                highlightRenderer.gameObject.SetActive(normalized > 0.02f || _isHighlighted);
            }

            if (soldOverlayRenderer != null && _showSoldOverlayDuringFlash)
            {
                Color overlayColor = soldOverlayColor;
                overlayColor.a = Mathf.Max(soldOverlayColor.a, normalized * 0.75f);
                soldOverlayRenderer.color = overlayColor;
            }
        }

        private void BeginFeedbackFlash(Color flashColor, float duration, float scaleMultiplier, bool showSoldOverlay)
        {
            CacheInitialScale();
            _activeFlashColor = flashColor;
            _activeFlashDuration = Mathf.Max(0.05f, duration);
            _showSoldOverlayDuringFlash = showSoldOverlay;
            _purchaseFlashRemaining = _activeFlashDuration;

            if (_hasInitialScale)
            {
                transform.localScale = _initialScale * Mathf.Max(1f, scaleMultiplier);
            }

            if (highlightRenderer != null)
            {
                highlightRenderer.gameObject.SetActive(true);
                highlightRenderer.color = flashColor;
            }

            if (soldOverlayRenderer != null && showSoldOverlay)
            {
                soldOverlayRenderer.gameObject.SetActive(true);
            }
        }
    }
}
