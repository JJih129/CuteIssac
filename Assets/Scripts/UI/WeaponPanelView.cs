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
        [SerializeField] private Text titleText;
        [SerializeField] private Text statusText;
        [SerializeField] private Text ammoText;
        [SerializeField] private Text detailText;
        [SerializeField] private Text loadoutText;

        [Header("Styling")]
        [SerializeField] private Color panelTint = new(0.03f, 0.06f, 0.1f, 0.9f);
        [SerializeField] private Color iconBackdropTint = new(0.07f, 0.12f, 0.18f, 0.97f);
        [SerializeField] private Color chipBackdropTint = new(0.09f, 0.15f, 0.22f, 0.96f);
        [SerializeField] private Color ammoBackdropTint = new(0.07f, 0.11f, 0.17f, 0.97f);
        [SerializeField] private Color placeholderTextColor = new(1f, 1f, 1f, 0.82f);
        [SerializeField] private Color detailTextColor = new(0.9f, 0.95f, 1f, 0.96f);
        [SerializeField] private Color loadoutTextColor = new(0.82f, 0.9f, 1f, 0.92f);
        [SerializeField] private Color reloadTrackTint = new(0.16f, 0.24f, 0.34f, 0.94f);

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
                frameImage.color = panelTint;
            }

            if (iconBackdropImage != null)
            {
                iconBackdropImage.color = iconBackdropTint;
            }

            if (iconImage != null)
            {
                iconImage.enabled = false;
                iconImage.sprite = null;
            }

            if (statusBackdropImage != null)
            {
                statusBackdropImage.color = chipBackdropTint;
            }

            if (ammoBackdropImage != null)
            {
                ammoBackdropImage.color = ammoBackdropTint;
            }

            if (titleText != null)
            {
                titleText.text = "WEAPON RELIC";
                titleText.color = placeholderTextColor;
            }

            if (statusText != null)
            {
                statusText.text = "NO LOADOUT";
                statusText.color = placeholderTextColor;
            }

            if (ammoText != null)
            {
                ammoText.text = "--/--";
                ammoText.color = placeholderTextColor;
            }

            if (detailText != null)
            {
                detailText.text = "Modern firearm relics will appear here.";
                detailText.color = placeholderTextColor;
            }

            if (loadoutText != null)
            {
                loadoutText.text = "LOADOUT 0/0";
                loadoutText.color = placeholderTextColor;
            }

            if (reloadTrackImage != null)
            {
                reloadTrackImage.color = reloadTrackTint;
            }

            if (reloadFillImage != null)
            {
                reloadFillImage.fillAmount = 0f;
                reloadFillImage.color = reloadTrackTint;
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
                frameImage.color = Color.Lerp(panelTint, accentColor, 0.14f);
            }

            if (iconBackdropImage != null)
            {
                iconBackdropImage.color = Color.Lerp(iconBackdropTint, accentColor, 0.18f);
            }

            if (statusBackdropImage != null)
            {
                statusBackdropImage.color = Color.Lerp(chipBackdropTint, accentColor, 0.24f);
            }

            if (ammoBackdropImage != null)
            {
                ammoBackdropImage.color = Color.Lerp(ammoBackdropTint, accentColor, 0.12f);
            }

            if (iconImage != null)
            {
                iconImage.enabled = state.Icon != null;
                iconImage.sprite = state.Icon;
                iconImage.color = Color.white;
            }

            if (titleText != null)
            {
                titleText.text = state.DisplayName;
                titleText.color = accentColor;
            }

            if (statusText != null)
            {
                statusText.text = state.StatusLabel;
                statusText.color = state.IsReloading
                    ? Color.Lerp(accentColor, Color.white, 0.28f)
                    : Color.Lerp(detailTextColor, accentColor, 0.18f);
            }

            if (ammoText != null)
            {
                ammoText.text = state.AmmoLabel;
                ammoText.color = Color.Lerp(accentColor, Color.white, 0.24f);
            }

            if (detailText != null)
            {
                detailText.text = state.DetailLabel;
                detailText.color = detailTextColor;
            }

            if (loadoutText != null)
            {
                loadoutText.text = state.LoadoutLabel;
                loadoutText.color = loadoutTextColor;
            }

            if (reloadTrackImage != null)
            {
                reloadTrackImage.color = Color.Lerp(reloadTrackTint, accentColor, 0.16f);
            }

            if (reloadFillImage != null)
            {
                reloadFillImage.fillAmount = state.IsReloading
                    ? Mathf.Clamp01(state.ReloadNormalized)
                    : 0f;
                reloadFillImage.color = Color.Lerp(reloadTrackTint, accentColor, 0.62f);
            }
        }

        private void ApplyLayoutMode(bool carouselOpen)
        {
            if (titleText != null)
            {
                titleText.alignment = carouselOpen ? TextAnchor.UpperCenter : TextAnchor.UpperLeft;
                titleText.fontSize = carouselOpen ? 34 : 30;
            }

            if (statusText != null)
            {
                statusText.alignment = TextAnchor.MiddleCenter;
                statusText.fontSize = carouselOpen ? 18 : 20;
            }

            if (ammoText != null)
            {
                ammoText.alignment = carouselOpen ? TextAnchor.MiddleCenter : TextAnchor.MiddleLeft;
                ammoText.fontSize = carouselOpen ? 40 : 52;
            }

            if (detailText != null)
            {
                detailText.alignment = carouselOpen ? TextAnchor.UpperCenter : TextAnchor.UpperLeft;
                detailText.fontSize = carouselOpen ? 20 : 18;
            }

            if (loadoutText != null)
            {
                loadoutText.alignment = carouselOpen ? TextAnchor.UpperCenter : TextAnchor.UpperLeft;
                loadoutText.fontSize = carouselOpen ? 18 : 16;
            }
        }

        private void EnsureTextSetup()
        {
            LocalizedUiFontProvider.ApplyReadableDefaults(titleText, 30, TextAnchor.UpperLeft, FontStyle.Bold);
            LocalizedUiFontProvider.ApplyReadableDefaults(statusText, 20, TextAnchor.MiddleCenter, FontStyle.Bold);
            LocalizedUiFontProvider.ApplyReadableDefaults(ammoText, 52, TextAnchor.MiddleLeft, FontStyle.Bold);
            LocalizedUiFontProvider.ApplyReadableDefaults(detailText, 18, TextAnchor.UpperLeft);
            LocalizedUiFontProvider.ApplyReadableDefaults(loadoutText, 16, TextAnchor.UpperLeft);
        }
    }
}
