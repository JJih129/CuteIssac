using CuteIssac.Data.Item;
using UnityEngine;

namespace CuteIssac.Item
{
    /// <summary>
    /// Immutable snapshot passed from shop logic into UI presentation.
    /// </summary>
    public readonly struct ShopSlotState
    {
        public ShopSlotState(
            string displayName,
            string priceLabel,
            string statusLabel,
            Sprite icon,
            ShopCurrencyType currencyType,
            bool isVisible,
            bool canPurchase,
            bool isSold,
            bool isHighlighted)
        {
            DisplayName = displayName;
            PriceLabel = priceLabel;
            StatusLabel = statusLabel;
            Icon = icon;
            CurrencyType = currencyType;
            IsVisible = isVisible;
            CanPurchase = canPurchase;
            IsSold = isSold;
            IsHighlighted = isHighlighted;
        }

        public string DisplayName { get; }
        public string PriceLabel { get; }
        public string StatusLabel { get; }
        public Sprite Icon { get; }
        public ShopCurrencyType CurrencyType { get; }
        public bool IsVisible { get; }
        public bool CanPurchase { get; }
        public bool IsSold { get; }
        public bool IsHighlighted { get; }
    }
}
