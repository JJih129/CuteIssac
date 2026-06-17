using CuteIssac.Player;
using UnityEngine;
using UnityEngine.UI;

namespace CuteIssac.UI
{
    [DisallowMultipleComponent]
    public sealed class WeaponPanelView : MonoBehaviour
    {
        [Header("Optional Root")]
        [SerializeField] private GameObject panelRoot;

        [Header("Skinnable Elements")]
        [SerializeField] private Image frameImage;
        [SerializeField] private Image iconBackdropImage;
        [SerializeField] private Image iconImage;
        [SerializeField] private Image statusBackdropImage;
        [SerializeField] private Image ammoBackdropImage;
        [SerializeField] private Image reloadTrackImage;
        [SerializeField] private Image reloadFillImage;
        [SerializeField] private Sprite fallbackWeaponIcon;
        [SerializeField] private Text titleText;
        [SerializeField] private Text statusText;
        [SerializeField] private Text ammoText;
        [SerializeField] private Text detailText;
        [SerializeField] private Text loadoutText;

        [Header("Minimal Layout")]
        [Tooltip("Shows only the weapon slot, current weapon icon, ammo text, and optional reload bar.")]
        [SerializeField] private bool useMinimalLayout = true;
        [SerializeField] private bool showReloadBarInMinimalLayout = true;
        [SerializeField] private Color minimalAmmoTextColor = Color.white;

        [Header("Styling")]
        [SerializeField] private Color panelTint = new(0.03f, 0.06f, 0.1f, 0.9f);
        [SerializeField] private Color iconBackdropTint = new(0.07f, 0.12f, 0.18f, 0.97f);
        [SerializeField] private Color chipBackdropTint = new(0.09f, 0.15f, 0.22f, 0.96f);
        [SerializeField] private Color ammoBackdropTint = new(0.07f, 0.11f, 0.17f, 0.97f);
        [SerializeField] private Color placeholderTextColor = new(1f, 1f, 1f, 0.82f);
        [SerializeField] private Color detailTextColor = new(0.9f, 0.95f, 1f, 0.96f);
        [SerializeField] private Color loadoutTextColor = new(0.82f, 0.9f, 1f, 0.92f);
        [SerializeField] private Color reloadTrackTint = new(0.16f, 0.24f, 0.34f, 0.94f);
        [SerializeField] private Color authoredTitleTextColor = new(0.24f, 0.15f, 0.09f, 1f);
        [SerializeField] private Color authoredPrimaryTextColor = new(0.30f, 0.19f, 0.12f, 1f);
        [SerializeField] private Color authoredSecondaryTextColor = new(0.42f, 0.29f, 0.19f, 0.98f);
        [SerializeField] private Color authoredMutedTextColor = new(0.52f, 0.38f, 0.27f, 0.94f);

        public bool UsesMinimalLayout => useMinimalLayout;

        private void Awake()
        {
            EnsureTextSetup();
            ApplyMinimalElementVisibility();
        }

        private void OnValidate()
        {
            ApplyMinimalElementVisibility();
        }

        public void ConfigureRuntimeView(
            GameObject root,
            Image frame,
            Image iconBackdrop,
            Image icon,
            Image statusBackdrop,
            Image ammoBackdrop,
            Image reloadTrack,
            Image reloadFill,
            Text title,
            Text status,
            Text ammo,
            Text detail,
            Text loadout)
        {
            panelRoot = root;
            frameImage = frame;
            iconBackdropImage = iconBackdrop;
            iconImage = icon;
            statusBackdropImage = statusBackdrop;
            ammoBackdropImage = ammoBackdrop;
            reloadTrackImage = reloadTrack;
            reloadFillImage = reloadFill;
            titleText = title;
            statusText = status;
            ammoText = ammo;
            detailText = detail;
            loadoutText = loadout;
            EnsureTextSetup();
            ApplyMinimalElementVisibility();
        }

        public void HidePanel()
        {
            if (panelRoot != null)
            {
                panelRoot.SetActive(false);
            }
        }

        public void ShowPlaceholder()
        {
            EnsureTextSetup();

            if (panelRoot != null)
            {
                panelRoot.SetActive(true);
            }

            if (frameImage != null)
            {
                frameImage.gameObject.SetActive(!useMinimalLayout || iconBackdropImage == null);
                frameImage.color = ResolveImageColor(frameImage, panelTint);
            }

            if (iconBackdropImage != null)
            {
                iconBackdropImage.gameObject.SetActive(true);
                iconBackdropImage.color = ResolveImageColor(iconBackdropImage, iconBackdropTint);
            }

            if (iconImage != null)
            {
                iconImage.enabled = fallbackWeaponIcon != null;
                iconImage.sprite = fallbackWeaponIcon;
                iconImage.color = Color.white;
            }

            if (statusBackdropImage != null)
            {
                statusBackdropImage.gameObject.SetActive(!useMinimalLayout);
                statusBackdropImage.color = ResolveImageColor(statusBackdropImage, chipBackdropTint);
            }

            if (ammoBackdropImage != null)
            {
                ammoBackdropImage.gameObject.SetActive(true);
                ammoBackdropImage.enabled = !useMinimalLayout;
                ammoBackdropImage.color = ResolveImageColor(ammoBackdropImage, ammoBackdropTint);
            }

            if (titleText != null)
            {
                titleText.gameObject.SetActive(!useMinimalLayout);
                titleText.text = "WEAPON RELIC";
                titleText.color = HasAuthoredSkin ? authoredTitleTextColor : placeholderTextColor;
            }

            if (statusText != null)
            {
                statusText.gameObject.SetActive(!useMinimalLayout);
                statusText.text = "NO LOADOUT";
                statusText.color = HasAuthoredSkin ? authoredPrimaryTextColor : placeholderTextColor;
            }

            if (ammoText != null)
            {
                ammoText.gameObject.SetActive(true);
                ammoText.text = "--/--";
                ammoText.color = useMinimalLayout
                    ? minimalAmmoTextColor
                    : HasAuthoredSkin
                        ? authoredPrimaryTextColor
                        : placeholderTextColor;
            }

            if (detailText != null)
            {
                detailText.gameObject.SetActive(!useMinimalLayout);
                detailText.text = "Modern firearm relics will appear here.";
                detailText.color = HasAuthoredSkin ? authoredSecondaryTextColor : placeholderTextColor;
            }

            if (loadoutText != null)
            {
                loadoutText.gameObject.SetActive(!useMinimalLayout);
                loadoutText.text = "LOADOUT 0/0";
                loadoutText.color = HasAuthoredSkin ? authoredMutedTextColor : placeholderTextColor;
            }

            if (reloadTrackImage != null)
            {
                reloadTrackImage.gameObject.SetActive(!useMinimalLayout || showReloadBarInMinimalLayout);
                reloadTrackImage.color = ResolveImageColor(reloadTrackImage, reloadTrackTint);
            }

            if (reloadFillImage != null)
            {
                reloadFillImage.gameObject.SetActive(!useMinimalLayout || showReloadBarInMinimalLayout);
                reloadFillImage.fillAmount = 0f;
                reloadFillImage.color = ResolveImageColor(reloadFillImage, reloadTrackTint);
            }
        }

        public void SetWeapon(PlayerWeaponHudState state)
        {
            EnsureTextSetup();
            ApplyLayoutMode(state.IsCarouselOpen);

            if (!state.HasWeapon)
            {
                HidePanel();
                return;
            }

            if (panelRoot != null)
            {
                panelRoot.SetActive(true);
            }

            Color accentColor = state.AccentColor.a > 0.01f
                ? state.AccentColor
                : Color.white;

            if (frameImage != null)
            {
                frameImage.gameObject.SetActive(!useMinimalLayout || iconBackdropImage == null);
                frameImage.color = ResolveImageColor(frameImage, Color.Lerp(panelTint, accentColor, 0.14f));
            }

            if (iconBackdropImage != null)
            {
                iconBackdropImage.gameObject.SetActive(true);
                iconBackdropImage.color = ResolveImageColor(iconBackdropImage, Color.Lerp(iconBackdropTint, accentColor, 0.18f));
            }

            if (statusBackdropImage != null)
            {
                statusBackdropImage.gameObject.SetActive(!useMinimalLayout);
                statusBackdropImage.color = ResolveImageColor(statusBackdropImage, Color.Lerp(chipBackdropTint, accentColor, 0.24f));
            }

            if (ammoBackdropImage != null)
            {
                ammoBackdropImage.gameObject.SetActive(true);
                ammoBackdropImage.enabled = !useMinimalLayout;
                ammoBackdropImage.color = ResolveImageColor(ammoBackdropImage, Color.Lerp(ammoBackdropTint, accentColor, 0.12f));
            }

            if (iconImage != null)
            {
                Sprite resolvedIcon = state.Icon != null ? state.Icon : fallbackWeaponIcon;
                iconImage.enabled = resolvedIcon != null;
                iconImage.sprite = resolvedIcon;
                iconImage.color = Color.white;
            }

            if (titleText != null)
            {
                titleText.gameObject.SetActive(!useMinimalLayout);
                titleText.text = state.DisplayName;
                titleText.color = HasAuthoredSkin
                    ? authoredTitleTextColor
                    : accentColor;
            }

            if (statusText != null)
            {
                statusText.gameObject.SetActive(!useMinimalLayout);
                statusText.text = state.StatusLabel;
                statusText.color = HasAuthoredSkin
                    ? authoredPrimaryTextColor
                    : state.IsReloading
                        ? Color.Lerp(accentColor, Color.white, 0.28f)
                        : Color.Lerp(detailTextColor, accentColor, 0.18f);
            }

            if (ammoText != null)
            {
                ammoText.gameObject.SetActive(true);
                ammoText.text = state.AmmoLabel;
                ammoText.color = useMinimalLayout
                    ? minimalAmmoTextColor
                    : HasAuthoredSkin
                        ? authoredPrimaryTextColor
                        : Color.Lerp(accentColor, Color.white, 0.24f);
            }

            if (detailText != null)
            {
                detailText.gameObject.SetActive(!useMinimalLayout);
                detailText.text = state.DetailLabel;
                detailText.color = HasAuthoredSkin ? authoredSecondaryTextColor : detailTextColor;
            }

            if (loadoutText != null)
            {
                loadoutText.gameObject.SetActive(!useMinimalLayout);
                loadoutText.text = state.LoadoutLabel;
                loadoutText.color = HasAuthoredSkin ? authoredMutedTextColor : loadoutTextColor;
            }

            if (reloadTrackImage != null)
            {
                reloadTrackImage.gameObject.SetActive(!useMinimalLayout || (showReloadBarInMinimalLayout && state.IsReloading));
                reloadTrackImage.color = ResolveImageColor(reloadTrackImage, Color.Lerp(reloadTrackTint, accentColor, 0.16f));
            }

            if (reloadFillImage != null)
            {
                reloadFillImage.gameObject.SetActive(!useMinimalLayout || (showReloadBarInMinimalLayout && state.IsReloading));
                reloadFillImage.fillAmount = state.IsReloading
                    ? Mathf.Clamp01(state.ReloadNormalized)
                    : 0f;
                reloadFillImage.color = ResolveImageColor(reloadFillImage, Color.Lerp(reloadTrackTint, accentColor, 0.62f));
            }
        }

        private static Color ResolveImageColor(Image image, Color fallbackColor)
        {
            return image != null && image.sprite != null
                ? Color.white
                : fallbackColor;
        }

        private bool HasAuthoredSkin => frameImage != null && frameImage.sprite != null;

        private void ApplyLayoutMode(bool carouselOpen)
        {
            if (useMinimalLayout)
            {
                if (ammoText != null)
                {
                    ammoText.alignment = TextAnchor.MiddleCenter;
                    ammoText.fontSize = 17;
                }

                return;
            }

            bool authoredSkin = HasAuthoredSkin;

            if (titleText != null)
            {
                titleText.alignment = authoredSkin || carouselOpen ? TextAnchor.MiddleCenter : TextAnchor.UpperLeft;
                titleText.fontSize = authoredSkin ? 23 : carouselOpen ? 30 : 24;
            }

            if (statusText != null)
            {
                statusText.alignment = TextAnchor.MiddleCenter;
                statusText.fontSize = authoredSkin ? 18 : carouselOpen ? 17 : 18;
            }

            if (ammoText != null)
            {
                ammoText.alignment = carouselOpen ? TextAnchor.MiddleCenter : TextAnchor.MiddleLeft;
                ammoText.fontSize = authoredSkin ? 46 : carouselOpen ? 38 : 44;
            }

            if (detailText != null)
            {
                detailText.alignment = authoredSkin || carouselOpen ? TextAnchor.MiddleCenter : TextAnchor.UpperLeft;
                detailText.fontSize = authoredSkin ? 15 : carouselOpen ? 18 : 16;
            }

            if (loadoutText != null)
            {
                loadoutText.alignment = authoredSkin || carouselOpen ? TextAnchor.MiddleCenter : TextAnchor.UpperLeft;
                loadoutText.fontSize = authoredSkin ? 12 : carouselOpen ? 16 : 14;
            }
        }

        private void EnsureTextSetup()
        {
            LocalizedUiFontProvider.ApplyReadableDefaults(titleText, 24, TextAnchor.UpperLeft, FontStyle.Bold);
            LocalizedUiFontProvider.ApplyReadableDefaults(statusText, 18, TextAnchor.MiddleCenter, FontStyle.Bold);
            LocalizedUiFontProvider.ApplyReadableDefaults(ammoText, 44, TextAnchor.MiddleLeft, FontStyle.Bold);
            LocalizedUiFontProvider.ApplyReadableDefaults(detailText, 16, TextAnchor.UpperLeft);
            LocalizedUiFontProvider.ApplyReadableDefaults(loadoutText, 14, TextAnchor.UpperLeft);
            ConfigureBestFit(titleText, 14, 23);
            ConfigureBestFit(statusText, 12, 18);
            ConfigureBestFit(ammoText, 28, 46);
            ConfigureBestFit(detailText, 10, 15);
            ConfigureBestFit(loadoutText, 8, 12);
        }

        private void ApplyMinimalElementVisibility()
        {
            if (!useMinimalLayout)
            {
                return;
            }

            SetActiveIfNotNull(frameImage, iconBackdropImage == null);
            SetActiveIfNotNull(iconBackdropImage, true);
            SetActiveIfNotNull(iconImage, true);
            SetActiveIfNotNull(statusBackdropImage, false);
            SetImageEnabledIfNotNull(ammoBackdropImage, false);
            SetActiveIfNotNull(titleText, false);
            SetActiveIfNotNull(statusText, false);
            SetActiveIfNotNull(ammoText, true);
            SetActiveIfNotNull(detailText, false);
            SetActiveIfNotNull(loadoutText, false);
            SetActiveIfNotNull(reloadTrackImage, showReloadBarInMinimalLayout);
            SetActiveIfNotNull(reloadFillImage, showReloadBarInMinimalLayout);
        }

        private static void SetActiveIfNotNull(Graphic graphic, bool active)
        {
            if (graphic != null)
            {
                graphic.gameObject.SetActive(active);
            }
        }

        private static void SetImageEnabledIfNotNull(Image image, bool enabled)
        {
            if (image != null)
            {
                image.gameObject.SetActive(true);
                image.enabled = enabled;
            }
        }

        private static void ConfigureBestFit(Text text, int minSize, int maxSize)
        {
            if (text == null)
            {
                return;
            }

            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = minSize;
            text.resizeTextMaxSize = maxSize;
        }
    }
}
