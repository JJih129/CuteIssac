using UnityEngine;

namespace CuteIssac.Item
{
    public enum ShopPriceLabelStyle
    {
        Compact,
        Ui
    }

    /// <summary>
    /// Centralizes shop price labels so world labels and UI slots use the same rule.
    /// </summary>
    public static class ShopPriceLabelFormatter
    {
        private const string DefaultCoinSuffix = "\uC6D0";

        public static string FormatPrice(
            int price,
            ShopCurrencyType currencyType,
            ShopPriceLabelStyle style = ShopPriceLabelStyle.Compact,
            string coinSuffixOverride = null)
        {
            int normalizedPrice = Mathf.Max(0, price);

            if (currencyType == ShopCurrencyType.Coins)
            {
                string suffix = string.IsNullOrWhiteSpace(coinSuffixOverride)
                    ? DefaultCoinSuffix
                    : coinSuffixOverride.Trim();
                return $"{normalizedPrice}{suffix}";
            }

            return style == ShopPriceLabelStyle.Ui
                ? $"{ResolveCurrencyLabel(currencyType)} {normalizedPrice}"
                : $"{normalizedPrice}{ResolveCompactSuffix(currencyType)}";
        }

        public static string ResolveCurrencyLabel(ShopCurrencyType currencyType)
        {
            return currencyType switch
            {
                ShopCurrencyType.Keys => "KEY",
                ShopCurrencyType.Bombs => "BOMB",
                ShopCurrencyType.Health => "HP",
                ShopCurrencyType.Coins => "COIN",
                _ => string.Empty
            };
        }

        public static string ResolveCompactSuffix(ShopCurrencyType currencyType)
        {
            return currencyType switch
            {
                ShopCurrencyType.Keys => "K",
                ShopCurrencyType.Bombs => "B",
                ShopCurrencyType.Health => "HP",
                ShopCurrencyType.Coins => DefaultCoinSuffix,
                _ => string.Empty
            };
        }
    }
}
