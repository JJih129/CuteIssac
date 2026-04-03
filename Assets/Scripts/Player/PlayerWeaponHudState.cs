using UnityEngine;

namespace CuteIssac.Player
{
    public readonly struct PlayerWeaponHudState
    {
        public PlayerWeaponHudState(
            bool hasWeapon,
            string displayName,
            string statusLabel,
            string ammoLabel,
            string detailLabel,
            string loadoutLabel,
            Sprite icon,
            Color accentColor,
            bool isReloading,
            float reloadNormalized,
            bool isCarouselOpen)
        {
            HasWeapon = hasWeapon;
            DisplayName = displayName ?? string.Empty;
            StatusLabel = statusLabel ?? string.Empty;
            AmmoLabel = ammoLabel ?? string.Empty;
            DetailLabel = detailLabel ?? string.Empty;
            LoadoutLabel = loadoutLabel ?? string.Empty;
            Icon = icon;
            AccentColor = accentColor.a > 0.01f
                ? accentColor
                : Color.white;
            IsReloading = isReloading;
            ReloadNormalized = Mathf.Clamp01(reloadNormalized);
            IsCarouselOpen = isCarouselOpen;
        }

        public bool HasWeapon { get; }
        public string DisplayName { get; }
        public string StatusLabel { get; }
        public string AmmoLabel { get; }
        public string DetailLabel { get; }
        public string LoadoutLabel { get; }
        public Sprite Icon { get; }
        public Color AccentColor { get; }
        public bool IsReloading { get; }
        public float ReloadNormalized { get; }
        public bool IsCarouselOpen { get; }
    }
}
