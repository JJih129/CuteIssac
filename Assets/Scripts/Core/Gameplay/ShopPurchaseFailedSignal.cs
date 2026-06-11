using CuteIssac.Item;
using UnityEngine;

namespace CuteIssac.Core.Gameplay
{
    public readonly struct ShopPurchaseFailedSignal
    {
        public ShopPurchaseFailedSignal(ShopCurrencyType currencyType, string reasonLabel, Vector3 worldPosition)
        {
            CurrencyType = currencyType;
            ReasonLabel = reasonLabel ?? string.Empty;
            WorldPosition = worldPosition;
        }

        public ShopCurrencyType CurrencyType { get; }
        public string ReasonLabel { get; }
        public Vector3 WorldPosition { get; }
    }
}
