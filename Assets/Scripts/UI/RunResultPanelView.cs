using CuteIssac.Core.Run;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace CuteIssac.UI
{
    [DisallowMultipleComponent]
    public sealed class RunResultPanelView : MonoBehaviour
    {
        [Header("Optional Root")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private RectTransform cardRoot;

        [Header("Skinnable Elements")]
        [SerializeField] private Image dimBackgroundImage;
        [SerializeField] private Image frameImage;
        [SerializeField] private Image summarySectionImage;
        [SerializeField] private Image detailSectionImage;
        [SerializeField] private Image actionSectionImage;
        [SerializeField] private Text resultBadgeText;
        [SerializeField] private Text titleText;
        [SerializeField] private Text subtitleText;
        [SerializeField] private Text summaryHeaderText;
        [SerializeField] private Text detailHeaderText;
        [SerializeField] private Text actionHeaderText;
        [SerializeField] private Text itemsValueText;
        [SerializeField] private Text roomsValueText;
        [SerializeField] private Text floorValueText;
        [SerializeField] private Text resolvedRoomsValueText;
        [SerializeField] private Text coinsValueText;
        [SerializeField] private Text keysValueText;
        [SerializeField] private Text bombsValueText;
        [SerializeField] private Text actionHintText;
        [SerializeField] private Button restartButton;
        [SerializeField] private Text restartButtonText;

        [Header("Theme")]
        [SerializeField] private Color dimColor = new(0f, 0f, 0f, 0.84f);
        [SerializeField] private Color panelColor = new(0.92f, 0.86f, 0.74f, 0.985f);
        [SerializeField] private Color sectionColor = new(1f, 1f, 1f, 0.08f);
        [SerializeField] private Color detailSectionColor = new(0.22f, 0.19f, 0.15f, 0.12f);
        [SerializeField] private Color victoryAccentColor = new(0.42f, 0.78f, 1f, 0.96f);
        [SerializeField] private Color defeatAccentColor = new(0.96f, 0.42f, 0.34f, 0.96f);
        [SerializeField] private Color titleColor = new(0.17f, 0.12f, 0.08f, 1f);
        [SerializeField] private Color subtitleColor = new(0.3f, 0.23f, 0.16f, 0.92f);
        [SerializeField] private Color sectionHeaderColor = new(0.31f, 0.23f, 0.16f, 0.96f);
        [SerializeField] private Color statLabelColor = new(0.46f, 0.58f, 0.7f, 0.95f);
        [SerializeField] private Color statValueColor = new(0.16f, 0.12f, 0.08f, 1f);
        [SerializeField] private Color hintColor = new(0.3f, 0.23f, 0.17f, 0.78f);
        [SerializeField] private Color buttonColor = new(0.23f, 0.18f, 0.13f, 0.94f);
        [SerializeField] private Color buttonTextColor = new(0.97f, 0.94f, 0.86f, 1f);
        [SerializeField] private Color selectedButtonColor = new(0.38f, 0.49f, 0.62f, 0.98f);
        [SerializeField] private Color selectedButtonTextColor = new(1f, 0.98f, 0.92f, 1f);
        [SerializeField] [Min(1f)] private float selectedButtonScale = 1.03f;

        public Button RestartButton => restartButton;

        public void EnsureRuntimeBaseline(RectTransform parent, Font font)
        {
            if (font == null)
            {
                font = LocalizedUiFontProvider.GetFont();
            }

            panelRoot = panelRoot != null ? panelRoot : gameObject;
            RectTransform rootRect = panelRoot.transform as RectTransform;

            if (rootRect == null)
            {
                return;
            }

            if (parent != null && rootRect.parent != parent)
            {
                rootRect.SetParent(parent, false);
            }

            StretchToParent(rootRect);
            EnsureCanvasRenderer(panelRoot);

            if (ShouldRebuildFallbackLayout())
            {
                ClearChildren(rootRect);
                ClearSerializedRuntimeReferences();
            }

            dimBackgroundImage = EnsureFullScreenImage(panelRoot, dimBackgroundImage, "Dimmer", dimColor, true);
            cardRoot = EnsureCardRoot(rootRect);

            RectTransform headerSection = EnsureSectionRoot(cardRoot, "HeaderSection", out _, null, 0f, new RectOffset(0, 0, 0, 0), 176f);
            resultBadgeText = EnsureText(headerSection, "ResultBadge", resultBadgeText, font, 22, FontStyle.Bold, TextAnchor.MiddleCenter, 28f);
            titleText = EnsureText(headerSection, "Title", titleText, font, 54, FontStyle.Bold, TextAnchor.MiddleCenter, 72f, true);
            subtitleText = EnsureText(headerSection, "Subtitle", subtitleText, font, 28, FontStyle.Normal, TextAnchor.UpperCenter, 62f, true);
            subtitleText.lineSpacing = 1.12f;

            RectTransform summarySection = EnsureSectionRoot(cardRoot, "SummarySection", out summarySectionImage, sectionColor, 18f, new RectOffset(24, 24, 22, 22), 214f);
            summaryHeaderText = EnsureText(summarySection, "SummaryHeader", summaryHeaderText, font, 24, FontStyle.Bold, TextAnchor.MiddleLeft, 28f);
            RectTransform summaryGridRoot = EnsureGridRoot(summarySection, "SummaryGrid", 3, new RectOffset(0, 0, 16, 14), new Vector2(270f, 126f), 1);
            itemsValueText = EnsureStatCard(summaryGridRoot, "ItemsCard", "\uD68D\uB4DD \uC544\uC774\uD15C", itemsValueText, font, 18, 40);
            roomsValueText = EnsureStatCard(summaryGridRoot, "RoomsCard", "\uD074\uB9AC\uC5B4 \uBC29", roomsValueText, font, 18, 40);
            floorValueText = EnsureStatCard(summaryGridRoot, "FloorCard", "\uB3C4\uB2EC \uCE35", floorValueText, font, 18, 40);

            RectTransform detailSection = EnsureSectionRoot(cardRoot, "DetailSection", out detailSectionImage, detailSectionColor, 16f, new RectOffset(24, 24, 20, 20), 198f);
            detailHeaderText = EnsureText(detailSection, "DetailHeader", detailHeaderText, font, 22, FontStyle.Bold, TextAnchor.MiddleLeft, 26f);
            RectTransform detailGridRoot = EnsureGridRoot(detailSection, "DetailGrid", 4, new RectOffset(0, 0, 14, 8), new Vector2(194f, 112f), 1);
            resolvedRoomsValueText = EnsureStatCard(detailGridRoot, "ResolvedRoomsCard", "\uD574\uACB0 \uBC29", resolvedRoomsValueText, font, 18, 34);
            coinsValueText = EnsureStatCard(detailGridRoot, "CoinsCard", "\uCF54\uC778", coinsValueText, font, 18, 34);
            keysValueText = EnsureStatCard(detailGridRoot, "KeysCard", "\uC5F4\uC1E0", keysValueText, font, 18, 34);
            bombsValueText = EnsureStatCard(detailGridRoot, "BombsCard", "\uD3ED\uD0C4", bombsValueText, font, 18, 34);

            RectTransform actionSection = EnsureSectionRoot(cardRoot, "ActionSection", out actionSectionImage, new Color(1f, 1f, 1f, 0.06f), 14f, new RectOffset(24, 24, 22, 24), 188f);
            actionHeaderText = EnsureText(actionSection, "ActionHeader", actionHeaderText, font, 22, FontStyle.Bold, TextAnchor.MiddleCenter, 26f);
            actionHintText = EnsureText(actionSection, "ActionHint", actionHintText, font, 24, FontStyle.Normal, TextAnchor.UpperCenter, 56f, true);
            actionHintText.lineSpacing = 1.08f;
            restartButton = EnsureButton(actionSection, "RestartButton", restartButton, ref restartButtonText, font);

            ApplyTextDefaults(font);
            Hide();
        }

        public void ShowSummary(RunResultSummary summary, string actionHint)
        {
            if (summary == null)
            {
                Hide();
                return;
            }

            panelRoot?.SetActive(true);

            Color accentColor = summary.EndReason == RunEndReason.Defeat
                ? defeatAccentColor
                : victoryAccentColor;

            if (dimBackgroundImage != null)
            {
                dimBackgroundImage.color = dimColor;
            }

            if (frameImage != null)
            {
                frameImage.color = panelColor;
            }

            if (summarySectionImage != null)
            {
                summarySectionImage.color = sectionColor;
            }

            if (detailSectionImage != null)
            {
                detailSectionImage.color = detailSectionColor;
            }

            if (actionSectionImage != null)
            {
                actionSectionImage.color = new Color(accentColor.r, accentColor.g, accentColor.b, 0.12f);
            }

            ApplyAccentBorder(accentColor);

            SetText(resultBadgeText, BuildBadgeText(summary.EndReason), Color.Lerp(buttonTextColor, Color.white, 0.18f));
            SetText(titleText, summary.Title, titleColor);
            SetText(subtitleText, summary.Subtitle, subtitleColor);
            SetText(summaryHeaderText, "\uACB0\uACFC \uC694\uC57D", sectionHeaderColor);
            SetText(detailHeaderText, "\uC138\uBD80 \uAE30\uB85D", sectionHeaderColor);
            SetText(actionHeaderText, "\uB2E4\uC74C \uD589\uB3D9", sectionHeaderColor);
            SetText(actionHintText, string.IsNullOrWhiteSpace(actionHint)
                ? "\uC900\uBE44\uAC00 \uB418\uBA74 \uC0C8 \uB7F0\uC744 \uC2DC\uC791\uD558\uC138\uC694."
                : actionHint, hintColor);

            SetStatValue(itemsValueText, summary.CollectedItemCount);
            SetStatValue(roomsValueText, summary.ClearedRoomCount);
            SetStatValue(floorValueText, summary.ReachedFloor);
            SetStatValue(resolvedRoomsValueText, summary.ResolvedRoomCount);
            SetStatValue(coinsValueText, summary.Coins);
            SetStatValue(keysValueText, summary.Keys);
            SetStatValue(bombsValueText, summary.Bombs);

            if (restartButtonText != null)
            {
                restartButtonText.text = "\uB2E4\uC2DC \uC2DC\uC791";
                restartButtonText.color = buttonTextColor;
            }

            if (restartButton != null)
            {
                if (restartButton.targetGraphic is Image buttonImage)
                {
                    buttonImage.color = buttonColor;
                }

                ColorBlock colors = restartButton.colors;
                colors.normalColor = buttonColor;
                colors.highlightedColor = new Color(0.31f, 0.25f, 0.19f, 0.98f);
                colors.pressedColor = new Color(0.17f, 0.13f, 0.1f, 0.98f);
                colors.selectedColor = selectedButtonColor;
                colors.disabledColor = new Color(0.2f, 0.2f, 0.2f, 0.65f);
                restartButton.colors = colors;
            }

            RefreshSelectionState(false);
        }

        public void Hide()
        {
            if (panelRoot != null)
            {
                panelRoot.SetActive(false);
            }
        }

        public void BindRestartAction(UnityAction restartAction)
        {
            if (restartButton == null)
            {
                return;
            }

            restartButton.onClick.RemoveAllListeners();

            if (restartAction != null)
            {
                restartButton.onClick.AddListener(restartAction);
            }
        }

        public void RefreshSelectionState(bool selected)
        {
            if (restartButton == null)
            {
                return;
            }

            if (restartButton.targetGraphic is Image buttonImage)
            {
                buttonImage.color = selected ? selectedButtonColor : buttonColor;
            }

            RectTransform restartRect = restartButton.transform as RectTransform;
            if (restartRect != null)
            {
                restartRect.localScale = selected ? Vector3.one * selectedButtonScale : Vector3.one;
            }

            if (restartButtonText != null)
            {
                restartButtonText.color = selected ? selectedButtonTextColor : buttonTextColor;
            }
        }

        private RectTransform EnsureCardRoot(RectTransform rootRect)
        {
            GameObject cardObject = frameImage != null ? frameImage.gameObject : FindNamedChild(rootRect, "RunResultCard");

            if (cardObject == null)
            {
                cardObject = new GameObject("RunResultCard", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(VerticalLayoutGroup));
                cardObject.transform.SetParent(rootRect, false);
            }

            RectTransform rect = cardObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(1020f, 760f);
            rect.localScale = Vector3.one;

            frameImage = cardObject.GetComponent<Image>() ?? cardObject.AddComponent<Image>();
            frameImage.color = panelColor;
            frameImage.raycastTarget = true;

            VerticalLayoutGroup layout = cardObject.GetComponent<VerticalLayoutGroup>() ?? cardObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(42, 42, 34, 34);
            layout.spacing = 22f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            return rect;
        }

        private RectTransform EnsureSectionRoot(
            RectTransform parent,
            string name,
            out Image sectionImage,
            Color? backgroundColor,
            float spacing,
            RectOffset padding,
            float preferredHeight)
        {
            GameObject sectionObject = FindNamedChild(parent, name);

            if (sectionObject == null)
            {
                sectionObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(VerticalLayoutGroup), typeof(LayoutElement));
                sectionObject.transform.SetParent(parent, false);
            }

            RectTransform rect = sectionObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.localScale = Vector3.one;

            VerticalLayoutGroup layout = sectionObject.GetComponent<VerticalLayoutGroup>() ?? sectionObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = padding;
            layout.spacing = spacing;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            LayoutElement layoutElement = sectionObject.GetComponent<LayoutElement>() ?? sectionObject.AddComponent<LayoutElement>();
            layoutElement.flexibleWidth = 1f;
            layoutElement.minHeight = preferredHeight;
            layoutElement.preferredHeight = preferredHeight;

            if (backgroundColor.HasValue)
            {
                sectionImage = sectionObject.GetComponent<Image>() ?? sectionObject.AddComponent<Image>();
                sectionImage.color = backgroundColor.Value;
                sectionImage.raycastTarget = false;
            }
            else
            {
                sectionImage = null;
            }

            return rect;
        }

        private RectTransform EnsureGridRoot(
            RectTransform parent,
            string name,
            int columns,
            RectOffset padding,
            Vector2 cellSize,
            int rows)
        {
            GameObject gridObject = FindNamedChild(parent, name);

            if (gridObject == null)
            {
                gridObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(GridLayoutGroup), typeof(LayoutElement));
                gridObject.transform.SetParent(parent, false);
            }

            RectTransform rect = gridObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.localScale = Vector3.one;

            GridLayoutGroup grid = gridObject.GetComponent<GridLayoutGroup>() ?? gridObject.AddComponent<GridLayoutGroup>();
            grid.startAxis = GridLayoutGroup.Axis.Horizontal;
            grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = columns;
            grid.cellSize = cellSize;
            grid.spacing = new Vector2(16f, 14f);
            grid.padding = padding;
            grid.childAlignment = TextAnchor.UpperCenter;

            LayoutElement layoutElement = gridObject.GetComponent<LayoutElement>() ?? gridObject.AddComponent<LayoutElement>();
            float height = padding.top + padding.bottom + (rows * cellSize.y) + ((rows - 1) * grid.spacing.y);
            layoutElement.flexibleWidth = 1f;
            layoutElement.minHeight = height;
            layoutElement.preferredHeight = height;
            return rect;
        }

        private Text EnsureStatCard(RectTransform gridRoot, string name, string label, Text existingValueText, Font font, int labelFontSize, int valueFontSize)
        {
            GameObject cardObject = existingValueText != null ? existingValueText.transform.parent?.gameObject : FindNamedChild(gridRoot, name);

            if (cardObject == null)
            {
                cardObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(VerticalLayoutGroup), typeof(LayoutElement));
                cardObject.transform.SetParent(gridRoot, false);
            }

            RectTransform rect = cardObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.localScale = Vector3.one;

            Image image = cardObject.GetComponent<Image>() ?? cardObject.AddComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0.08f);
            image.raycastTarget = false;

            VerticalLayoutGroup layout = cardObject.GetComponent<VerticalLayoutGroup>() ?? cardObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(14, 14, 14, 14);
            layout.spacing = 6f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            LayoutElement element = cardObject.GetComponent<LayoutElement>() ?? cardObject.AddComponent<LayoutElement>();
            element.preferredHeight = 118f;
            element.flexibleWidth = 1f;

            Text labelText = EnsureText(rect, "Label", FindNamedText(rect, "Label"), font, labelFontSize, FontStyle.Bold, TextAnchor.MiddleCenter, 26f);
            labelText.text = label;
            labelText.color = statLabelColor;
            labelText.lineSpacing = 1f;

            Text valueText = EnsureText(rect, "Value", existingValueText, font, valueFontSize, FontStyle.Bold, TextAnchor.MiddleCenter, 44f);
            valueText.color = statValueColor;
            valueText.lineSpacing = 1f;
            return valueText;
        }

        private Button EnsureButton(RectTransform parent, string name, Button existing, ref Text label, Font font)
        {
            GameObject buttonObject = existing != null ? existing.gameObject : FindNamedChild(parent, name);

            if (buttonObject == null)
            {
                buttonObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(LayoutElement));
                buttonObject.transform.SetParent(parent, false);
            }

            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.localScale = Vector3.one;
            rect.sizeDelta = new Vector2(360f, 86f);

            LayoutElement layoutElement = buttonObject.GetComponent<LayoutElement>() ?? buttonObject.AddComponent<LayoutElement>();
            layoutElement.minHeight = 86f;
            layoutElement.preferredHeight = 86f;
            layoutElement.preferredWidth = 360f;
            layoutElement.flexibleWidth = 0f;

            Image image = buttonObject.GetComponent<Image>() ?? buttonObject.AddComponent<Image>();
            image.color = buttonColor;
            image.raycastTarget = true;

            Button button = buttonObject.GetComponent<Button>() ?? buttonObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.ColorTint;

            label = EnsureText(rect, "Label", label, font, 32, FontStyle.Bold, TextAnchor.MiddleCenter, 40f);
            label.text = "\uB2E4\uC2DC \uC2DC\uC791";
            label.color = buttonTextColor;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 22;
            label.resizeTextMaxSize = 32;

            RectTransform labelRect = label.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.pivot = new Vector2(0.5f, 0.5f);
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            return button;
        }

        private Text EnsureText(
            RectTransform parent,
            string name,
            Text existing,
            Font font,
            int fontSize,
            FontStyle fontStyle,
            TextAnchor alignment,
            float preferredHeight,
            bool allowBestFit = false)
        {
            GameObject textObject = existing != null ? existing.gameObject : FindNamedChild(parent, name);

            if (textObject == null)
            {
                textObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text), typeof(LayoutElement));
                textObject.transform.SetParent(parent, false);
            }

            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.localScale = Vector3.one;
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(0f, preferredHeight);

            LayoutElement layoutElement = textObject.GetComponent<LayoutElement>() ?? textObject.AddComponent<LayoutElement>();
            layoutElement.preferredHeight = preferredHeight;
            layoutElement.flexibleWidth = 1f;

            Text text = textObject.GetComponent<Text>() ?? textObject.AddComponent<Text>();
            LocalizedUiFontProvider.Apply(text);
            text.font = font;
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.alignment = alignment;
            text.supportRichText = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.resizeTextForBestFit = allowBestFit;
            text.resizeTextMinSize = Mathf.Max(18, fontSize - 12);
            text.resizeTextMaxSize = fontSize;
            text.alignByGeometry = true;
            text.raycastTarget = false;
            return text;
        }

        private void ApplyTextDefaults(Font font)
        {
            Text[] texts =
            {
                resultBadgeText,
                titleText,
                subtitleText,
                summaryHeaderText,
                detailHeaderText,
                actionHeaderText,
                itemsValueText,
                roomsValueText,
                floorValueText,
                resolvedRoomsValueText,
                coinsValueText,
                keysValueText,
                bombsValueText,
                actionHintText,
                restartButtonText
            };

            for (int index = 0; index < texts.Length; index++)
            {
                Text text = texts[index];

                if (text == null)
                {
                    continue;
                }

                LocalizedUiFontProvider.Apply(text);
                text.font = font;
                text.supportRichText = false;
                text.horizontalOverflow = HorizontalWrapMode.Wrap;
                text.verticalOverflow = VerticalWrapMode.Truncate;
                text.alignByGeometry = true;
            }
        }

        private void SetText(Text text, string value, Color color)
        {
            if (text == null)
            {
                return;
            }

            text.text = value;
            text.color = color;
        }

        private void SetStatValue(Text valueText, int value)
        {
            if (valueText == null)
            {
                return;
            }

            valueText.text = value.ToString();
            valueText.color = statValueColor;
        }

        private void ApplyAccentBorder(Color accentColor)
        {
            if (frameImage == null)
            {
                return;
            }

            Outline outline = frameImage.GetComponent<Outline>() ?? frameImage.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(accentColor.r, accentColor.g, accentColor.b, 0.82f);
            outline.effectDistance = new Vector2(3f, -3f);
            outline.useGraphicAlpha = true;
        }

        private static string BuildBadgeText(RunEndReason endReason)
        {
            return endReason switch
            {
                RunEndReason.Victory => "\uC2B9\uB9AC \uAE30\uB85D",
                RunEndReason.Defeat => "\uD328\uBC30 \uBD84\uC11D",
                RunEndReason.Abandoned => "\uB7F0 \uC911\uB2E8",
                _ => "\uB7F0 \uACB0\uACFC"
            };
        }

        private static void StretchToParent(RectTransform rectTransform)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            rectTransform.localScale = Vector3.one;
        }

        private static void EnsureCanvasRenderer(GameObject target)
        {
            if (target.GetComponent<CanvasRenderer>() == null)
            {
                target.AddComponent<CanvasRenderer>();
            }
        }

        private static Image EnsureFullScreenImage(GameObject owner, Image existing, string name, Color color, bool raycastTarget)
        {
            GameObject imageObject = existing != null ? existing.gameObject : FindNamedChild(owner.transform, name);

            if (imageObject == null)
            {
                imageObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                imageObject.transform.SetParent(owner.transform, false);
            }

            RectTransform rect = imageObject.GetComponent<RectTransform>();
            StretchToParent(rect);

            Image image = imageObject.GetComponent<Image>() ?? imageObject.AddComponent<Image>();
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

        private static Text FindNamedText(Transform parent, string name)
        {
            GameObject child = FindNamedChild(parent, name);
            return child != null ? child.GetComponent<Text>() : null;
        }

        private bool ShouldRebuildFallbackLayout()
        {
            return dimBackgroundImage == null
                && frameImage == null
                && summarySectionImage == null
                && detailSectionImage == null
                && actionSectionImage == null
                && resultBadgeText == null
                && titleText == null
                && restartButton == null;
        }

        private void ClearSerializedRuntimeReferences()
        {
            cardRoot = null;
            dimBackgroundImage = null;
            frameImage = null;
            summarySectionImage = null;
            detailSectionImage = null;
            actionSectionImage = null;
            resultBadgeText = null;
            titleText = null;
            subtitleText = null;
            summaryHeaderText = null;
            detailHeaderText = null;
            actionHeaderText = null;
            itemsValueText = null;
            roomsValueText = null;
            floorValueText = null;
            resolvedRoomsValueText = null;
            coinsValueText = null;
            keysValueText = null;
            bombsValueText = null;
            actionHintText = null;
            restartButton = null;
            restartButtonText = null;
        }

        private static void ClearChildren(RectTransform root)
        {
            if (root == null)
            {
                return;
            }

            for (int index = root.childCount - 1; index >= 0; index--)
            {
                Transform child = root.GetChild(index);

                if (Application.isPlaying)
                {
                    Destroy(child.gameObject);
                }
                else
                {
                    DestroyImmediate(child.gameObject);
                }
            }
        }
    }
}
