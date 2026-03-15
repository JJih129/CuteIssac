using CuteIssac.Data.Run;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace CuteIssac.UI
{
    /// <summary>
    /// Presentation-only button for one starting build option.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class StartingBuildOptionView : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image accentBarImage;
        [SerializeField] private Image selectionFrameImage;
        [SerializeField] private Image iconImage;
        [SerializeField] private Text nameText;
        [SerializeField] private Text descriptionText;
        [SerializeField] private Text loadoutText;
        [SerializeField] private Text statusText;

        [Header("Theme")]
        [SerializeField] private Color idleCardColor = new(0.9f, 0.84f, 0.72f, 0.97f);
        [SerializeField] private Color selectedCardColor = new(0.74f, 0.57f, 0.28f, 0.98f);
        [SerializeField] private Color idleAccentColor = new(0.48f, 0.3f, 0.15f, 0.9f);
        [SerializeField] private Color selectedAccentColor = new(0.98f, 0.82f, 0.37f, 1f);
        [SerializeField] private Color selectionFrameColor = new(0.28f, 0.16f, 0.06f, 0.9f);
        [SerializeField] private Color titleColor = new(0.21f, 0.12f, 0.07f, 1f);
        [SerializeField] private Color descriptionColor = new(0.3f, 0.19f, 0.11f, 0.92f);
        [SerializeField] private Color loadoutColor = new(0.54f, 0.33f, 0.12f, 0.98f);
        [SerializeField] private Color statusColor = new(0.34f, 0.21f, 0.1f, 0.95f);
        [SerializeField] private Color selectedStatusColor = new(0.19f, 0.1f, 0.03f, 1f);
        [SerializeField] private Color iconFallbackColor = new(0.28f, 0.18f, 0.1f, 0.14f);
        [SerializeField] private Color coinAccentColor = new(1f, 0.84f, 0.25f, 1f);
        [SerializeField] private Color keyAccentColor = new(0.72f, 0.88f, 1f, 1f);
        [SerializeField] private Color bombAccentColor = new(1f, 0.6f, 0.32f, 1f);
        [SerializeField] private Color itemAccentColor = new(0.74f, 0.92f, 0.64f, 1f);

        public void Configure(
            Button runtimeButton,
            Image runtimeBackground,
            Image runtimeAccentBar,
            Image runtimeSelectionFrame,
            Image runtimeIcon,
            Text runtimeName,
            Text runtimeDescription,
            Text runtimeLoadout,
            Text runtimeStatus)
        {
            button = runtimeButton;
            backgroundImage = runtimeBackground;
            accentBarImage = runtimeAccentBar;
            selectionFrameImage = runtimeSelectionFrame;
            iconImage = runtimeIcon;
            nameText = runtimeName;
            descriptionText = runtimeDescription;
            loadoutText = runtimeLoadout;
            statusText = runtimeStatus;
        }

        public void SetTheme(
            Color idleBackground,
            Color selectedBackground,
            Color runtimeIdleAccent,
            Color runtimeSelectedAccent,
            Color runtimeSelectionFrame,
            Color runtimeTitleColor,
            Color runtimeDescriptionColor,
            Color runtimeLoadoutColor,
            Color runtimeStatusColor,
            Color runtimeSelectedStatusColor,
            Color runtimeIconFallbackColor)
        {
            idleCardColor = idleBackground;
            selectedCardColor = selectedBackground;
            idleAccentColor = runtimeIdleAccent;
            selectedAccentColor = runtimeSelectedAccent;
            selectionFrameColor = runtimeSelectionFrame;
            titleColor = runtimeTitleColor;
            descriptionColor = runtimeDescriptionColor;
            loadoutColor = runtimeLoadoutColor;
            statusColor = runtimeStatusColor;
            selectedStatusColor = runtimeSelectedStatusColor;
            iconFallbackColor = runtimeIconFallbackColor;
        }

        public void Present(StartingBuildData buildData, bool isSelected, UnityAction onSelected)
        {
            if (buildData == null)
            {
                gameObject.SetActive(false);
                return;
            }

            gameObject.SetActive(true);

            if (nameText != null)
            {
                nameText.supportRichText = false;
                nameText.text =
                    $"{(isSelected ? "선택된 빌드" : "시작 빌드")}\n" +
                    $"{buildData.DisplayName}";
                nameText.color = titleColor;
            }

            if (descriptionText != null)
            {
                descriptionText.supportRichText = false;
                descriptionText.text = $"특징\n{buildData.Description}";
                descriptionText.color = descriptionColor;
            }

            if (loadoutText != null)
            {
                int passiveItemCount = buildData.StartingPassiveItems != null ? buildData.StartingPassiveItems.Count : 0;
                int displayedStartingKeys = Mathf.Max(1, buildData.StartingKeys);
                loadoutText.supportRichText = false;
                loadoutText.text =
                    $"시작 자원  코인 {buildData.StartingCoins}  열쇠 {displayedStartingKeys}  폭탄 {buildData.StartingBombs}\n" +
                    $"로드아웃  패시브 {passiveItemCount}";
                loadoutText.color = loadoutColor;
            }

            if (statusText != null)
            {
                statusText.supportRichText = false;
                statusText.text = isSelected
                    ? "선택 상태\n현재 선택됨"
                    : "선택 상태\n클릭하여 시작";
                statusText.color = isSelected ? selectedStatusColor : statusColor;
            }

            if (iconImage != null)
            {
                iconImage.sprite = buildData.Icon;
                iconImage.enabled = true;
                iconImage.color = buildData.Icon != null ? Color.white : iconFallbackColor;
            }

            if (backgroundImage != null)
            {
                backgroundImage.color = isSelected ? selectedCardColor : idleCardColor;
            }

            if (accentBarImage != null)
            {
                accentBarImage.color = isSelected ? selectedAccentColor : idleAccentColor;
            }

            if (selectionFrameImage != null)
            {
                selectionFrameImage.enabled = isSelected;
                selectionFrameImage.color = selectionFrameColor;
            }

            if (button != null)
            {
                button.onClick.RemoveAllListeners();

                if (onSelected != null)
                {
                    button.onClick.AddListener(onSelected);
                }
            }
        }
    }
}
