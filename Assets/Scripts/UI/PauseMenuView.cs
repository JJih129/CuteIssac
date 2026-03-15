using CuteIssac.Player;
using UnityEngine;
using UnityEngine.UI;

namespace CuteIssac.UI
{
    /// <summary>
    /// Presentation-only pause menu. It renders pause stats and menu buttons,
    /// while input and game-state changes stay in the controller.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PauseMenuView : MonoBehaviour
    {
        [Header("Optional Root")]
        [SerializeField] private GameObject overlayRoot;
        [SerializeField] private Image dimmerImage;
        [SerializeField] private Image panelImage;
        [SerializeField] private Image statsSectionImage;
        [SerializeField] private Image actionSectionImage;

        [Header("Texts")]
        [SerializeField] private Text badgeText;
        [SerializeField] private Text titleText;
        [SerializeField] private Text statsHeaderText;
        [SerializeField] private Text actionHeaderText;
        [SerializeField] private Text[] statValueTexts;

        [Header("Buttons")]
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button quitButton;
        [SerializeField] private Text settingsButtonText;
        [SerializeField] private Text resumeButtonText;
        [SerializeField] private Text quitButtonText;

        [Header("Theme")]
        [SerializeField] private Color dimmerColor = new(0f, 0f, 0f, 0.58f);
        [SerializeField] private Color panelColor = new(0.9f, 0.82f, 0.68f, 0.98f);
        [SerializeField] private Color titleColor = new(0.18f, 0.14f, 0.1f, 1f);
        [SerializeField] private Color statLabelColor = new(0.34f, 0.25f, 0.17f, 0.95f);
        [SerializeField] private Color statValueColor = new(0.18f, 0.14f, 0.1f, 1f);
        [SerializeField] private Color enabledButtonTextColor = new(0.22f, 0.18f, 0.14f, 1f);
        [SerializeField] private Color disabledButtonTextColor = new(0.22f, 0.18f, 0.14f, 0.6f);
        [SerializeField] private Color buttonBaseColor = new(1f, 1f, 1f, 0.08f);
        [SerializeField] private Color selectedButtonColor = new(0.42f, 0.56f, 0.7f, 0.24f);
        [SerializeField] private Color selectedButtonTextColor = new(0.14f, 0.18f, 0.24f, 1f);
        [SerializeField] [Min(1f)] private float selectedButtonScale = 1.035f;

        public Button SettingsButton => settingsButton;
        public Button ResumeButton => resumeButton;
        public Button QuitButton => quitButton;

        public void ConfigureRuntimeView(
            GameObject runtimeOverlayRoot,
            Image runtimeDimmerImage,
            Image runtimePanelImage,
            Image runtimeStatsSectionImage,
            Image runtimeActionSectionImage,
            Text runtimeBadgeText,
            Text runtimeTitleText,
            Text runtimeStatsHeaderText,
            Text runtimeActionHeaderText,
            Text[] runtimeStatValueTexts,
            Button runtimeSettingsButton,
            Button runtimeResumeButton,
            Button runtimeQuitButton,
            Text runtimeSettingsButtonText,
            Text runtimeResumeButtonText,
            Text runtimeQuitButtonText)
        {
            overlayRoot = runtimeOverlayRoot;
            dimmerImage = runtimeDimmerImage;
            panelImage = runtimePanelImage;
            statsSectionImage = runtimeStatsSectionImage;
            actionSectionImage = runtimeActionSectionImage;
            badgeText = runtimeBadgeText;
            titleText = runtimeTitleText;
            statsHeaderText = runtimeStatsHeaderText;
            actionHeaderText = runtimeActionHeaderText;
            statValueTexts = runtimeStatValueTexts;
            settingsButton = runtimeSettingsButton;
            resumeButton = runtimeResumeButton;
            quitButton = runtimeQuitButton;
            settingsButtonText = runtimeSettingsButtonText;
            resumeButtonText = runtimeResumeButtonText;
            quitButtonText = runtimeQuitButtonText;
        }

        public void EnsureRuntimeBaseline(RectTransform safeAreaRoot)
        {
            overlayRoot = overlayRoot != null ? overlayRoot : gameObject;

            RectTransform overlayRect = overlayRoot.transform as RectTransform;

            if (overlayRect == null)
            {
                return;
            }

            if (safeAreaRoot != null && overlayRect.parent != safeAreaRoot)
            {
                overlayRect.SetParent(safeAreaRoot, false);
            }

            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;
            overlayRect.localScale = Vector3.one;

            EnsureCanvasRenderer(overlayRoot);

            dimmerImage = EnsureImage(
                overlayRoot,
                nameof(dimmerImage),
                dimmerImage,
                "PauseMenuOverlayImage",
                overlayRect,
                new Color(0f, 0f, 0f, 0.58f),
                true);

            RectTransform panelRect = EnsurePanelRoot(overlayRect);

            statsSectionImage = EnsurePanelBlock(
                panelRect,
                "StatsSection",
                statsSectionImage,
                new Vector2(0f, -180f),
                new Vector2(700f, 344f),
                new Color(1f, 1f, 1f, 0.1f));

            actionSectionImage = EnsurePanelBlock(
                panelRect,
                "ActionSection",
                actionSectionImage,
                new Vector2(0f, -604f),
                new Vector2(700f, 196f),
                new Color(0.42f, 0.56f, 0.7f, 0.12f));

            badgeText = EnsureText(
                panelRect,
                "PauseBadge",
                badgeText,
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, -44f),
                new Vector2(320f, 28f),
                20,
                FontStyle.Bold,
                TextAnchor.MiddleCenter,
                "<b>메뉴 열림</b>",
                new Color(0.94f, 0.9f, 0.82f, 1f));

            titleText = EnsureText(
                panelRect,
                "PauseTitle",
                titleText,
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, -92f),
                new Vector2(560f, 96f),
                58,
                FontStyle.Bold,
                TextAnchor.MiddleCenter,
                "<size=18>런 일시정지</size>\n<size=52><b>잠시 멈춤</b></size>",
                titleColor);

            statsHeaderText = EnsureText(
                panelRect,
                "StatsHeader",
                statsHeaderText,
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, -170f),
                new Vector2(260f, 42f),
                24,
                FontStyle.Bold,
                TextAnchor.MiddleCenter,
                "<size=18>현재 상태</size>\n<b>스탯 요약</b>",
                titleColor);

            actionHeaderText = EnsureText(
                panelRect,
                "ActionHeader",
                actionHeaderText,
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, -560f),
                new Vector2(280f, 42f),
                24,
                FontStyle.Bold,
                TextAnchor.MiddleCenter,
                "<size=18>다음 행동</size>\n<b>메뉴 선택</b>",
                titleColor);

            statValueTexts = EnsureStatTexts(panelRect);

            settingsButton = EnsureButton(
                panelRect,
                "SettingsButton",
                settingsButton,
                ref settingsButtonText,
                new Vector2(-230f, -662f),
                "<size=15>메뉴</size>\n<b>설정</b>\n<size=13>준비 중</size>",
                disabledButtonTextColor);

            resumeButton = EnsureButton(
                panelRect,
                "ResumeButton",
                resumeButton,
                ref resumeButtonText,
                new Vector2(0f, -662f),
                "<size=15>메뉴</size>\n<b>게임 재개</b>\n<size=13>즉시 복귀</size>",
                enabledButtonTextColor);

            quitButton = EnsureButton(
                panelRect,
                "QuitButton",
                quitButton,
                ref quitButtonText,
                new Vector2(230f, -662f),
                "<size=15>메뉴</size>\n<b>게임 종료</b>\n<size=13>현재 런 중단</size>",
                enabledButtonTextColor);
        }

        public void Show(PlayerStatSnapshot snapshot)
        {
            if (overlayRoot != null)
            {
                overlayRoot.SetActive(true);
            }

            if (dimmerImage != null)
            {
                dimmerImage.color = dimmerColor;
            }

            if (panelImage != null)
            {
                panelImage.color = panelColor;
            }

            if (statsSectionImage != null)
            {
                statsSectionImage.color = new Color(1f, 1f, 1f, 0.1f);
            }

            if (actionSectionImage != null)
            {
                actionSectionImage.color = new Color(0.42f, 0.56f, 0.7f, 0.12f);
            }

            if (badgeText != null)
            {
                badgeText.supportRichText = true;
                badgeText.text = "<b>메뉴 열림</b>";
                badgeText.color = new Color(0.94f, 0.9f, 0.82f, 1f);
            }

            if (titleText != null)
            {
                titleText.supportRichText = true;
                titleText.text = "<size=18>런 일시정지</size>\n<size=52><b>잠시 멈춤</b></size>";
                titleText.color = titleColor;
            }

            if (statsHeaderText != null)
            {
                statsHeaderText.supportRichText = true;
                statsHeaderText.text = "<size=18>현재 상태</size>\n<b>스탯 요약</b>";
                statsHeaderText.color = titleColor;
            }

            if (actionHeaderText != null)
            {
                actionHeaderText.supportRichText = true;
                actionHeaderText.text = "<size=18>다음 행동</size>\n<b>메뉴 선택</b>";
                actionHeaderText.color = titleColor;
            }

            SetStatLine(0, "공격력", snapshot.Damage);
            SetStatLine(1, "연사", ResolveShotsPerSecond(snapshot.FireInterval));
            SetStatLine(2, "이동속도", snapshot.MoveSpeed);
            SetStatLine(3, "탄속", snapshot.ProjectileSpeed);
            SetStatLine(4, "행운", snapshot.Luck);

            if (settingsButton != null)
            {
                settingsButton.interactable = false;
            }

            if (settingsButtonText != null)
            {
                settingsButtonText.supportRichText = true;
                settingsButtonText.text = "<size=15>메뉴</size>\n<b>설정</b>\n<size=13>준비 중</size>";
                settingsButtonText.color = disabledButtonTextColor;
            }

            if (resumeButtonText != null)
            {
                resumeButtonText.supportRichText = true;
                resumeButtonText.text = "<size=15>메뉴</size>\n<b>게임 재개</b>\n<size=13>즉시 복귀</size>";
                resumeButtonText.color = enabledButtonTextColor;
            }

            if (quitButtonText != null)
            {
                quitButtonText.supportRichText = true;
                quitButtonText.text = "<size=15>메뉴</size>\n<b>게임 종료</b>\n<size=13>현재 런 중단</size>";
                quitButtonText.color = enabledButtonTextColor;
            }

            ApplyButtonTheme(settingsButton, false);
            ApplyButtonTheme(resumeButton, true);
            ApplyButtonTheme(quitButton, true);
            RefreshSelectionState(resumeButton);
        }

        public void Hide()
        {
            if (overlayRoot != null)
            {
                overlayRoot.SetActive(false);
            }
        }

        public void RefreshSelectionState(Button selectedButton)
        {
            RefreshButtonVisual(settingsButton, settingsButtonText, false, selectedButton == settingsButton);
            RefreshButtonVisual(resumeButton, resumeButtonText, resumeButton != null && resumeButton.interactable, selectedButton == resumeButton);
            RefreshButtonVisual(quitButton, quitButtonText, quitButton != null && quitButton.interactable, selectedButton == quitButton);
        }

        private void SetStatLine(int index, string label, float value)
        {
            if (statValueTexts == null || index < 0 || index >= statValueTexts.Length)
            {
                return;
            }

            Text statText = statValueTexts[index];

            if (statText != null)
            {
                statText.supportRichText = true;
                statText.text =
                    $"<size=14><color=#{ColorUtility.ToHtmlStringRGBA(statLabelColor)}>현재 {label}</color></size>\n" +
                    $"<size=30><b><color=#{ColorUtility.ToHtmlStringRGBA(statValueColor)}>{value:0.0}</color></b></size>";
                statText.color = statValueColor;
            }
        }

        private void ApplyButtonTheme(Button button, bool interactable)
        {
            if (button == null)
            {
                return;
            }

            if (button.targetGraphic is Image buttonImage)
            {
                buttonImage.color = buttonBaseColor;
            }

            ColorBlock colors = button.colors;
            colors.normalColor = buttonBaseColor;
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.14f);
            colors.pressedColor = new Color(1f, 1f, 1f, 0.22f);
            colors.selectedColor = new Color(1f, 1f, 1f, 0.16f);
            colors.disabledColor = new Color(1f, 1f, 1f, 0.04f);
            button.colors = colors;
            button.interactable = interactable;
        }

        private void RefreshButtonVisual(Button button, Text label, bool interactable, bool selected)
        {
            if (button == null)
            {
                return;
            }

            if (button.targetGraphic is Image buttonImage)
            {
                buttonImage.color = !interactable
                    ? new Color(1f, 1f, 1f, 0.04f)
                    : selected
                        ? selectedButtonColor
                        : buttonBaseColor;
            }

            RectTransform buttonRect = button.transform as RectTransform;

            if (buttonRect != null)
            {
                buttonRect.localScale = selected && interactable
                    ? Vector3.one * selectedButtonScale
                    : Vector3.one;
            }

            if (label != null)
            {
                label.color = !interactable
                    ? disabledButtonTextColor
                    : selected
                        ? selectedButtonTextColor
                        : enabledButtonTextColor;
            }
        }

        private static float ResolveShotsPerSecond(float fireInterval)
        {
            return fireInterval > 0.001f
                ? 1f / fireInterval
                : 0f;
        }

        private RectTransform EnsurePanelRoot(RectTransform overlayRect)
        {
            GameObject panelObject = panelImage != null ? panelImage.gameObject : FindNamedChild(overlayRect, "PausePanel");

            if (panelObject == null)
            {
                panelObject = new GameObject("PausePanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                panelObject.transform.SetParent(overlayRect, false);
            }

            RectTransform panelRect = panelObject.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.sizeDelta = new Vector2(820f, 820f);
            panelRect.localScale = Vector3.one;

            EnsureCanvasRenderer(panelObject);
            panelImage = panelObject.GetComponent<Image>();

            if (panelImage == null)
            {
                panelImage = panelObject.AddComponent<Image>();
            }

            panelImage.color = panelColor;
            panelImage.raycastTarget = true;
            return panelRect;
        }

        private Image EnsurePanelBlock(RectTransform parent, string name, Image existing, Vector2 anchoredPosition, Vector2 sizeDelta, Color color)
        {
            GameObject blockObject = existing != null ? existing.gameObject : FindNamedChild(parent, name);

            if (blockObject == null)
            {
                blockObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                blockObject.transform.SetParent(parent, false);
            }

            RectTransform blockRect = blockObject.GetComponent<RectTransform>();
            blockRect.anchorMin = new Vector2(0.5f, 1f);
            blockRect.anchorMax = new Vector2(0.5f, 1f);
            blockRect.pivot = new Vector2(0.5f, 1f);
            blockRect.anchoredPosition = anchoredPosition;
            blockRect.sizeDelta = sizeDelta;
            blockRect.localScale = Vector3.one;

            EnsureCanvasRenderer(blockObject);
            Image image = blockObject.GetComponent<Image>();

            if (image == null)
            {
                image = blockObject.AddComponent<Image>();
            }

            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private Text[] EnsureStatTexts(RectTransform panelRect)
        {
            Text[] resolved = new Text[5];
            string[] labels = { "현재 공격력", "현재 연사", "현재 이동속도", "현재 탄속", "현재 행운" };

            for (int index = 0; index < resolved.Length; index++)
            {
                Text existing = statValueTexts != null && index < statValueTexts.Length
                    ? statValueTexts[index]
                    : null;

                resolved[index] = EnsureText(
                    panelRect,
                    $"Stat_{index}",
                    existing,
                    new Vector2(0.5f, 1f),
                    new Vector2(0.5f, 1f),
                    new Vector2(0.5f, 1f),
                    new Vector2(0f, -236f - (index * 56f)),
                    new Vector2(560f, 48f),
                    28,
                    FontStyle.Bold,
                    TextAnchor.MiddleCenter,
                    $"<size=14><color=#{ColorUtility.ToHtmlStringRGBA(statLabelColor)}>{labels[index]}</color></size>\n<size=30><b><color=#{ColorUtility.ToHtmlStringRGBA(statValueColor)}>0.0</color></b></size>",
                    statValueColor);
            }

            return resolved;
        }

        private Button EnsureButton(
            RectTransform parent,
            string name,
            Button existing,
            ref Text label,
            Vector2 anchoredPosition,
            string labelText,
            Color labelColor)
        {
            GameObject buttonObject = existing != null ? existing.gameObject : FindNamedChild(parent, name);

            if (buttonObject == null)
            {
                buttonObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
                buttonObject.transform.SetParent(parent, false);
            }

            RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0.5f, 1f);
            buttonRect.anchorMax = new Vector2(0.5f, 1f);
            buttonRect.pivot = new Vector2(0.5f, 1f);
            buttonRect.anchoredPosition = anchoredPosition;
            buttonRect.sizeDelta = new Vector2(206f, 108f);
            buttonRect.localScale = Vector3.one;

            EnsureCanvasRenderer(buttonObject);

            Image buttonImage = buttonObject.GetComponent<Image>();

            if (buttonImage == null)
            {
                buttonImage = buttonObject.AddComponent<Image>();
            }

            buttonImage.color = buttonBaseColor;
            buttonImage.raycastTarget = true;

            Button button = buttonObject.GetComponent<Button>();

            if (button == null)
            {
                button = buttonObject.AddComponent<Button>();
            }

            ColorBlock colors = button.colors;
            colors.normalColor = buttonBaseColor;
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.14f);
            colors.pressedColor = new Color(1f, 1f, 1f, 0.22f);
            colors.selectedColor = new Color(1f, 1f, 1f, 0.16f);
            colors.disabledColor = new Color(1f, 1f, 1f, 0.04f);
            button.colors = colors;
            button.targetGraphic = buttonImage;
            button.transition = Selectable.Transition.ColorTint;

            label = EnsureText(
                buttonRect,
                "Label",
                label,
                Vector2.zero,
                Vector2.one,
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                Vector2.zero,
                26,
                FontStyle.Bold,
                TextAnchor.MiddleCenter,
                labelText,
                labelColor);

            return button;
        }

        private Text EnsureText(
            RectTransform parent,
            string name,
            Text existing,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 anchoredPosition,
            Vector2 sizeDelta,
            int fontSize,
            FontStyle fontStyle,
            TextAnchor alignment,
            string textValue,
            Color color)
        {
            GameObject textObject = existing != null ? existing.gameObject : FindNamedChild(parent, name);

            if (textObject == null)
            {
                textObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
                textObject.transform.SetParent(parent, false);
            }

            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = anchorMin;
            textRect.anchorMax = anchorMax;
            textRect.pivot = pivot;
            textRect.anchoredPosition = anchoredPosition;
            textRect.sizeDelta = sizeDelta;
            textRect.localScale = Vector3.one;

            EnsureCanvasRenderer(textObject);

            Text text = textObject.GetComponent<Text>();

            if (text == null)
            {
                text = textObject.AddComponent<Text>();
            }

            LocalizedUiFontProvider.Apply(text);
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.alignment = alignment;
            text.supportRichText = true;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            text.color = color;
            text.text = textValue;
            return text;
        }

        private Image EnsureImage(
            GameObject owner,
            string childName,
            Image existing,
            string fallbackName,
            RectTransform fallbackParent,
            Color color,
            bool raycastTarget)
        {
            GameObject imageObject = existing != null ? existing.gameObject : FindNamedChild(owner.transform, childName);
            imageObject ??= FindNamedChild(owner.transform, fallbackName);
            imageObject ??= owner;

            EnsureCanvasRenderer(imageObject);

            Image image = imageObject.GetComponent<Image>();

            if (image == null)
            {
                image = imageObject.AddComponent<Image>();
            }

            if (imageObject != owner)
            {
                RectTransform rect = imageObject.GetComponent<RectTransform>();

                if (rect != null && fallbackParent != null)
                {
                    rect.SetParent(fallbackParent, false);
                }
            }

            image.color = color;
            image.raycastTarget = raycastTarget;
            return image;
        }

        private static GameObject FindNamedChild(Transform parent, string name)
        {
            if (parent == null)
            {
                return null;
            }

            for (int index = 0; index < parent.childCount; index++)
            {
                Transform child = parent.GetChild(index);

                if (child.name == name)
                {
                    return child.gameObject;
                }
            }

            return null;
        }

        private static void EnsureCanvasRenderer(GameObject gameObject)
        {
            if (gameObject.GetComponent<CanvasRenderer>() == null)
            {
                gameObject.AddComponent<CanvasRenderer>();
            }
        }
    }
}
