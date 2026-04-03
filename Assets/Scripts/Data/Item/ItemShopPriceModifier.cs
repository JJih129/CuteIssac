using System;
using CuteIssac.Item;
using UnityEngine;

namespace CuteIssac.Data.Item
{
    [Serializable]
    public sealed class ItemShopPriceModifier
    {
        [SerializeField] private ShopCurrencyType currencyType = ShopCurrencyType.Coins;
        [SerializeField] [Min(0)] private int flatDiscount = 1;

        public ShopCurrencyType CurrencyType => currencyType;
        public int FlatDiscount => Mathf.Max(0, flatDiscount);

        public bool Supports(ShopCurrencyType requestedCurrencyType)
        {
            return requestedCurrencyType == currencyType;
        }
    }
}
