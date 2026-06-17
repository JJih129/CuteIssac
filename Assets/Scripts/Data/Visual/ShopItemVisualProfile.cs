using UnityEngine;

namespace CuteIssac.Data.Visual
{
    [CreateAssetMenu(fileName = "DefaultShopItemVisualProfile", menuName = "CuteIssac/Visual/Shop Item Visual Profile")]
    public sealed class ShopItemVisualProfile : ScriptableObject
    {
        private const string DefaultResourcesPath = "Visual/DefaultShopItemVisualProfile";

        private static ShopItemVisualProfile s_defaultProfile;
        private static bool s_defaultProfileLoaded;

        [Header("World Price Label")]
        [SerializeField] private Vector3 priceLabelLocalPosition = new(0f, -0.9f, -0.01f);
        [SerializeField] private Vector3 priceShadowLocalOffset = new(0.04f, -0.045f, 0.02f);
        [SerializeField] [Min(0.01f)] private float priceLabelCharacterSize = 0.3f;
        [SerializeField] private string coinPriceSuffix = "\uC6D0";

        [Header("State Colors")]
        [SerializeField] private Color availableBodyColor = new(0.94f, 0.94f, 0.94f, 1f);
        [SerializeField] private Color unaffordableBodyColor = new(0.62f, 0.62f, 0.62f, 1f);
        [SerializeField] private Color soldBodyColor = new(0.24f, 0.24f, 0.24f, 0.9f);
        [SerializeField] private Color highlightColor = new(1f, 1f, 1f, 0.85f);
        [SerializeField] private Color soldOverlayColor = new(0.2f, 0.2f, 0.2f, 0.55f);

        [Header("Currency Colors")]
        [SerializeField] private Color coinMarkerColor = new(1f, 0.85f, 0.25f, 1f);
        [SerializeField] private Color keyMarkerColor = new(0.7f, 0.9f, 1f, 1f);
        [SerializeField] private Color bombMarkerColor = new(1f, 0.55f, 0.2f, 1f);
        [SerializeField] private Color healthMarkerColor = new(1f, 0.28f, 0.42f, 1f);

        [Header("Price Text Colors")]
        [SerializeField] private Color availablePriceTextColor = new(1f, 0.98f, 0.78f, 1f);
        [SerializeField] private Color unavailablePriceTextColor = new(1f, 0.42f, 0.34f, 1f);
        [SerializeField] private Color soldPriceTextColor = new(0.66f, 0.66f, 0.66f, 0.92f);
        [SerializeField] private Color worldTextShadowColor = new(0.08f, 0.04f, 0.03f, 0.82f);

        [Header("Feedback")]
        [SerializeField] [Min(0f)] private float highlightPulseSpeed = 6f;
        [SerializeField] [Range(0f, 1f)] private float highlightPulseAlphaFloor = 0.35f;
        [SerializeField] private Color purchaseFlashColor = new(1f, 0.92f, 0.46f, 1f);
        [SerializeField] [Min(0.05f)] private float purchaseFlashDuration = 0.28f;
        [SerializeField] private Color purchaseFailureFlashColor = new(1f, 0.24f, 0.18f, 0.95f);
        [SerializeField] [Min(0.05f)] private float purchaseFailureFlashDuration = 0.22f;
        [SerializeField] [Min(1f)] private float purchaseScaleMultiplier = 1.12f;
        [SerializeField] [Min(0f)] private float purchaseScaleRecoverSpeed = 7.5f;

        [Header("Offer Scale")]
        [SerializeField] [Min(1f)] private float weaponBodyScaleMultiplier = 1.4f;
        [SerializeField] [Min(1f)] private float weaponIconScaleMultiplier = 2.8f;
        [SerializeField] [Min(1f)] private float weaponHighlightScaleMultiplier = 1.55f;

        public Vector3 PriceLabelLocalPosition => priceLabelLocalPosition;
        public Vector3 PriceShadowLocalOffset => priceShadowLocalOffset;
        public float PriceLabelCharacterSize => Mathf.Max(0.01f, priceLabelCharacterSize);
        public string CoinPriceSuffix => string.IsNullOrWhiteSpace(coinPriceSuffix) ? "\uC6D0" : coinPriceSuffix;
        public Color AvailableBodyColor => availableBodyColor;
        public Color UnaffordableBodyColor => unaffordableBodyColor;
        public Color SoldBodyColor => soldBodyColor;
        public Color HighlightColor => highlightColor;
        public Color SoldOverlayColor => soldOverlayColor;
        public Color CoinMarkerColor => coinMarkerColor;
        public Color KeyMarkerColor => keyMarkerColor;
        public Color BombMarkerColor => bombMarkerColor;
        public Color HealthMarkerColor => healthMarkerColor;
        public Color AvailablePriceTextColor => availablePriceTextColor;
        public Color UnavailablePriceTextColor => unavailablePriceTextColor;
        public Color SoldPriceTextColor => soldPriceTextColor;
        public Color WorldTextShadowColor => worldTextShadowColor;
        public float HighlightPulseSpeed => Mathf.Max(0f, highlightPulseSpeed);
        public float HighlightPulseAlphaFloor => Mathf.Clamp01(highlightPulseAlphaFloor);
        public Color PurchaseFlashColor => purchaseFlashColor;
        public float PurchaseFlashDuration => Mathf.Max(0.05f, purchaseFlashDuration);
        public Color PurchaseFailureFlashColor => purchaseFailureFlashColor;
        public float PurchaseFailureFlashDuration => Mathf.Max(0.05f, purchaseFailureFlashDuration);
        public float PurchaseScaleMultiplier => Mathf.Max(1f, purchaseScaleMultiplier);
        public float PurchaseScaleRecoverSpeed => Mathf.Max(0f, purchaseScaleRecoverSpeed);
        public float WeaponBodyScaleMultiplier => Mathf.Max(1f, weaponBodyScaleMultiplier);
        public float WeaponIconScaleMultiplier => Mathf.Max(1f, weaponIconScaleMultiplier);
        public float WeaponHighlightScaleMultiplier => Mathf.Max(1f, weaponHighlightScaleMultiplier);

        public static ShopItemVisualProfile Default
        {
            get
            {
                if (!s_defaultProfileLoaded)
                {
                    s_defaultProfile = Resources.Load<ShopItemVisualProfile>(DefaultResourcesPath);
                    s_defaultProfileLoaded = true;
                }

                return s_defaultProfile;
            }
        }

        public static ShopItemVisualProfile Resolve(ShopItemVisualProfile profile)
        {
            return profile != null ? profile : Default;
        }
    }
}
