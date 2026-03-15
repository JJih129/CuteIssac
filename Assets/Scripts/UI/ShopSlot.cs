using CuteIssac.Item;
using UnityEngine;
using UnityEngine.UI;

namespace CuteIssac.UI
{
    /// <summary>
    /// Presentation-only shop slot entry used by ShopPanelView.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ShopSlot : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image accentBarImage;
        [SerializeField] private Image highlightFrameImage;
        [SerializeField] private Image iconFrameImage;
        [SerializeField] private Image iconImage;
        [SerializeField] private Image currencyBadgeImage;
        [SerializeField] private Image priceChipImage;
        [SerializeField] private Image statusChipImage;
        [SerializeField] private Text nameText;
        [SerializeField] private Text priceText;
        [SerializeField] private Text statusText;

        [Header("Colors")]
        [SerializeField] private Color availableColor = new(0.31f, 0.25f, 0.18f, 0.94f);
        [SerializeField] private Color unavailableColor = new(0.25f, 0.22f, 0.2f, 0.9f);
        [SerializeField] private Color highlightedColor = new(0.48f, 0.35f, 0.16f, 0.97f);
        [SerializeField] private Color soldColor = new(0.22f, 0.2f, 0.18f, 0.84f);
        [SerializeField] private Color iconFrameColor = new(0.17f, 0.12f, 0.08f, 0.92f);
        [SerializeField] private Color accentBarColor = new(0.95f, 0.79f, 0.35f, 1f);
        [SerializeField] private Color highlightFrameColor = new(1f, 0.92f, 0.64f, 0.92f);
        [SerializeField] private Color availablePriceColor = new(0.26f, 0.18f, 0.1f, 1f);
        [SerializeField] private Color unavailablePriceColor = new(0.7f, 0.4f, 0.34f, 1f);
        [SerializeField] private Color soldPriceColor = new(0.56f, 0.56f, 0.56f, 1f);
        [SerializeField] private Color availablePriceChipColor = new(1f, 0.9f, 0.58f, 0.94f);
        [SerializeField] private Color unavailablePriceChipColor = new(0.62f, 0.46f, 0.4f, 0.9f);
        [SerializeField] private Color soldPriceChipColor = new(0.34f, 0.34f, 0.34f, 0.86f);
        [SerializeField] private Color availableStatusChipColor = new(0.28f, 0.5f, 0.28f, 0.92f);
        [SerializeField] private Color unavailableStatusChipColor = new(0.55f, 0.28f, 0.22f, 0.9f);
        [SerializeField] private Color soldStatusChipColor = new(0.34f, 0.34f, 0.34f, 0.86f);
        [SerializeField] private Color coinBadgeColor = new(1f, 0.84f, 0.25f, 1f);
        [SerializeField] private Color keyBadgeColor = new(0.7f, 0.9f, 1f, 1f);
        [SerializeField] private Color bombBadgeColor = new(1f, 0.55f, 0.2f, 1f);
        [SerializeField] private Color availableStatusColor = new(0.88f, 1f, 0.9f, 1f);
        [SerializeField] private Color unavailableStatusColor = new(1f, 0.82f, 0.78f, 1f);
        [SerializeField] private Color soldStatusColor = new(0.82f, 0.82f, 0.82f, 1f);
        [SerializeField] private Color soldNameColor = new(0.72f, 0.72f, 0.72f, 1f);
        [SerializeField] private Color normalNameColor = new(0.97f, 0.94f, 0.88f, 1f);
        [SerializeField] private Color highlightedIconFrameColor = new(1f, 0.88f, 0.54f, 1f);
        [SerializeField] private Color purchasableIconFrameColor = new(0.66f, 0.88f, 0.62f, 0.98f);
        [SerializeField] private Color unavailableIconFrameColor = new(0.68f, 0.44f, 0.36f, 0.92f);

        public void ConfigureRuntimeView(
            GameObject rootObject,
            Image background,
            Image accentBar,
            Image highlightFrame,
            Image iconFrame,
            Image icon,
            Image currencyBadge,
            Image priceChip,
            Image statusChip,
            Text name,
            Text price,
            Text status)
        {
            root = rootObject;
            backgroundImage = background;
            accentBarImage = accentBar;
            highlightFrameImage = highlightFrame;
            iconFrameImage = iconFrame;
            iconImage = icon;
            currencyBadgeImage = currencyBadge;
            priceChipImage = priceChip;
            statusChipImage = statusChip;
            nameText = name;
            priceText = price;
            statusText = status;
        }

        public void Present(ShopSlotState slotState)
        {
            EnsureFallbackVisuals();

            if (root != null)
            {
                root.SetActive(slotState.IsVisible);
            }

            if (!slotState.IsVisible)
            {
                return;
            }

            if (backgroundImage != null)
            {
                backgroundImage.color = slotState.IsSold
                    ? soldColor
                    : (slotState.IsHighlighted ? highlightedColor : (slotState.CanPurchase ? availableColor : unavailableColor));
            }

            if (accentBarImage != null)
            {
                accentBarImage.color = slotState.IsHighlighted ? highlightFrameColor : accentBarColor;
            }

            if (highlightFrameImage != null)
            {
                highlightFrameImage.enabled = slotState.IsHighlighted && !slotState.IsSold;
                highlightFrameImage.color = highlightFrameColor;
            }

            if (iconFrameImage != null)
            {
                iconFrameImage.color = ResolveIconFrameColor(slotState);
            }

            if (iconImage != null)
            {
                iconImage.sprite = slotState.Icon;
                iconImage.enabled = slotState.Icon != null;
                iconImage.color = slotState.IsSold ? new Color(1f, 1f, 1f, 0.35f) : Color.white;
            }

            if (currencyBadgeImage != null)
            {
                currencyBadgeImage.color = ResolveCurrencyAccent(slotState.CurrencyType);
            }

            if (nameText != null)
            {
                nameText.supportRichText = false;
                nameText.text = slotState.DisplayName;
                nameText.color = slotState.IsSold ? soldNameColor : normalNameColor;
            }

            Color priceColor = slotState.IsSold
                ? soldPriceColor
                : (slotState.CanPurchase ? availablePriceColor : unavailablePriceColor);

            if (priceText != null)
            {
                priceText.supportRichText = false;
                priceText.text = slotState.PriceLabel;
                priceText.color = priceColor;
            }

            if (priceChipImage != null)
            {
                priceChipImage.color = slotState.IsSold
                    ? soldPriceChipColor
                    : (slotState.CanPurchase ? availablePriceChipColor : unavailablePriceChipColor);
            }

            if (statusText != null)
            {
                statusText.supportRichText = false;
                statusText.text = slotState.IsSold ? "판매 완료" : slotState.StatusLabel;
                statusText.color = slotState.IsSold
                    ? soldStatusColor
                    : (slotState.CanPurchase ? availableStatusColor : unavailableStatusColor);
            }

            if (statusChipImage != null)
            {
                statusChipImage.color = slotState.IsSold
                    ? soldStatusChipColor
                    : (slotState.CanPurchase ? availableStatusChipColor : unavailableStatusChipColor);
            }
        }

        private void EnsureFallbackVisuals()
        {
            if (root == null)
            {
                root = gameObject;
            }
        }

        private Color ResolveIconFrameColor(ShopSlotState slotState)
        {
            if (slotState.IsSold)
            {
                return soldColor;
            }

            if (slotState.IsHighlighted)
            {
                return highlightedIconFrameColor;
            }

            if (slotState.CanPurchase)
            {
                return purchasableIconFrameColor;
            }

            return unavailableIconFrameColor;
        }

        private Color ResolveCurrencyAccent(ShopCurrencyType currencyType)
        {
            return currencyType switch
            {
                ShopCurrencyType.Keys => keyBadgeColor,
                ShopCurrencyType.Bombs => bombBadgeColor,
                _ => coinBadgeColor
            };
        }
    }
}
