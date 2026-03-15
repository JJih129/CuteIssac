using System.Collections.Generic;
using CuteIssac.Data.Run;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace CuteIssac.UI
{
    /// <summary>
    /// Minimal build selection overlay used before a run starts.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class StartingBuildPanelView : MonoBehaviour
    {
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private RectTransform contentCardRoot;
        [SerializeField] private Text titleText;
        [SerializeField] private Text subtitleText;
        [SerializeField] private RectTransform optionsRoot;

        [Header("Theme")]
        [SerializeField] private Color overlayColor = new(0.08f, 0.05f, 0.03f, 0.72f);
        [SerializeField] private Color contentCardColor = new(0.88f, 0.82f, 0.72f, 0.985f);
        [SerializeField] private Color contentCardEdgeColor = new(0.24f, 0.15f, 0.08f, 1f);
        [SerializeField] private Color contentCardShadowColor = new(0.07f, 0.04f, 0.02f, 0.44f);
        [SerializeField] private Color titleColor = new(0.21f, 0.12f, 0.07f, 1f);
        [SerializeField] private Color subtitleColor = new(0.32f, 0.2f, 0.12f, 0.94f);
        [SerializeField] private Color optionCardColor = new(0.9f, 0.84f, 0.72f, 0.97f);
        [SerializeField] private Color optionSelectedColor = new(0.8f, 0.67f, 0.38f, 0.995f);
        [SerializeField] private Color optionHoverColor = new(0.95f, 0.89f, 0.76f, 1f);
        [SerializeField] private Color optionPressedColor = new(0.84f, 0.76f, 0.62f, 1f);
        [SerializeField] private Color optionAccentColor = new(0.5f, 0.3f, 0.14f, 0.95f);
        [SerializeField] private Color optionSelectedAccentColor = new(0.98f, 0.82f, 0.37f, 1f);
        [SerializeField] private Color optionFrameColor = new(0.28f, 0.16f, 0.06f, 0.95f);
        [SerializeField] private Color optionDescriptionColor = new(0.3f, 0.19f, 0.11f, 0.92f);
        [SerializeField] private Color optionLoadoutColor = new(0.54f, 0.33f, 0.12f, 0.98f);
        [SerializeField] private Color optionStatusColor = new(0.34f, 0.21f, 0.1f, 0.95f);
        [SerializeField] private Color optionSelectedStatusColor = new(0.19f, 0.1f, 0.03f, 1f);
        [SerializeField] private Color optionIconFallbackColor = new(0.28f, 0.18f, 0.1f, 0.14f);

        private readonly List<StartingBuildOptionView> _runtimeOptions = new();

        public void Present(IReadOnlyList<StartingBuildData> builds, StartingBuildData selectedBuild, UnityAction<StartingBuildData> onSelected)
        {
            EnsureFallbackVisuals();

            if (panelRoot != null)
            {
                panelRoot.SetActive(true);
            }

            ApplyTheme();

            if (titleText != null)
            {
                titleText.supportRichText = false;
                titleText.text = "런 준비\n시작 빌드 선택";
            }

            if (subtitleText != null)
            {
                subtitleText.supportRichText = false;
                subtitleText.text = "이번 런의 출발 구성을 고르세요.\n자원, 시작 아이템, 운영 성향이 카드별로 다릅니다.";
            }

            int desiredCount = builds != null ? builds.Count : 0;
            EnsureOptionCount(desiredCount);

            for (int index = 0; index < _runtimeOptions.Count; index++)
            {
                StartingBuildData build = index < desiredCount ? builds[index] : null;
                StartingBuildOptionView optionView = _runtimeOptions[index];
                optionView.Present(
                    build,
                    build == selectedBuild,
                    build == null || onSelected == null ? null : (() => onSelected.Invoke(build)));
            }
        }

        public void Hide()
        {
            if (panelRoot != null)
            {
                panelRoot.SetActive(false);
            }
        }

        public static StartingBuildPanelView CreateRuntime(Canvas canvas)
        {
            if (canvas == null)
            {
                return null;
            }

            GameObject rootObject = new("StartingBuildPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(StartingBuildPanelView));
            rootObject.transform.SetParent(canvas.transform, false);

            RectTransform rootRect = rootObject.GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            StartingBuildPanelView panelView = rootObject.GetComponent<StartingBuildPanelView>();
            Image rootImage = rootObject.GetComponent<Image>();
            rootImage.color = panelView.overlayColor;

            panelView.panelRoot = rootObject;
            panelView.BuildFallbackVisuals(rootRect);
            panelView.ApplyTheme();
            return panelView;
        }

        private void EnsureFallbackVisuals()
        {
            if (panelRoot == null)
            {
                panelRoot = gameObject;
            }

            RectTransform rootRect = panelRoot.transform as RectTransform;
            if (rootRect != null && (titleText == null || subtitleText == null || optionsRoot == null))
            {
                BuildFallbackVisuals(rootRect);
            }
        }

        private void ApplyTheme()
        {
            if (panelRoot != null && panelRoot.TryGetComponent(out Image panelImage))
            {
                panelImage.color = overlayColor;
            }

            if (contentCardRoot != null && contentCardRoot.TryGetComponent(out Image contentCardImage))
            {
                contentCardImage.color = contentCardColor;
            }

            if (titleText != null)
            {
                titleText.color = titleColor;
            }

            if (subtitleText != null)
            {
                subtitleText.color = subtitleColor;
            }
        }

        private void BuildFallbackVisuals(RectTransform rootRect)
        {
            if (contentCardRoot == null)
            {
                GameObject shadowObject = new("ContentShadow", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                shadowObject.transform.SetParent(rootRect, false);
                RectTransform shadowRect = shadowObject.GetComponent<RectTransform>();
                shadowRect.anchorMin = new Vector2(0.5f, 0.5f);
                shadowRect.anchorMax = new Vector2(0.5f, 0.5f);
                shadowRect.pivot = new Vector2(0.5f, 0.5f);
                shadowRect.anchoredPosition = new Vector2(10f, -12f);
                shadowRect.sizeDelta = new Vector2(1220f, 760f);
                Image shadowImage = shadowObject.GetComponent<Image>();
                shadowImage.color = contentCardShadowColor;
                shadowImage.raycastTarget = false;

                GameObject contentObject = new("ContentCard", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                contentObject.transform.SetParent(rootRect, false);
                contentCardRoot = contentObject.GetComponent<RectTransform>();
                contentCardRoot.anchorMin = new Vector2(0.5f, 0.5f);
                contentCardRoot.anchorMax = new Vector2(0.5f, 0.5f);
                contentCardRoot.pivot = new Vector2(0.5f, 0.5f);
                contentCardRoot.anchoredPosition = new Vector2(0f, -12f);
                contentCardRoot.sizeDelta = new Vector2(1220f, 760f);

                Image contentImage = contentObject.GetComponent<Image>();
                contentImage.color = contentCardColor;

                GameObject edgeObject = new("ContentEdge", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                edgeObject.transform.SetParent(contentCardRoot, false);
                RectTransform edgeRect = edgeObject.GetComponent<RectTransform>();
                edgeRect.anchorMin = Vector2.zero;
                edgeRect.anchorMax = Vector2.one;
                edgeRect.offsetMin = new Vector2(10f, 10f);
                edgeRect.offsetMax = new Vector2(-10f, -10f);
                Image edgeImage = edgeObject.GetComponent<Image>();
                edgeImage.color = contentCardEdgeColor;
                edgeImage.raycastTarget = false;

                GameObject surfaceObject = new("ContentSurface", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                surfaceObject.transform.SetParent(contentCardRoot, false);
                RectTransform surfaceRect = surfaceObject.GetComponent<RectTransform>();
                surfaceRect.anchorMin = Vector2.zero;
                surfaceRect.anchorMax = Vector2.one;
                surfaceRect.offsetMin = new Vector2(14f, 14f);
                surfaceRect.offsetMax = new Vector2(-14f, -14f);
                Image surfaceImage = surfaceObject.GetComponent<Image>();
                surfaceImage.color = contentCardColor;
                surfaceImage.raycastTarget = false;
            }

            RectTransform contentRect = contentCardRoot != null ? contentCardRoot : rootRect;

            if (titleText == null)
            {
                titleText = CreateText("Title", contentRect, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -54f), new Vector2(880f, 76f), 36, FontStyle.Bold, TextAnchor.MiddleCenter);
            }

            if (subtitleText == null)
            {
                subtitleText = CreateText("Subtitle", contentRect, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -138f), new Vector2(980f, 66f), 22, FontStyle.Normal, TextAnchor.MiddleCenter);
                subtitleText.color = subtitleColor;
            }

            if (optionsRoot == null)
            {
                GameObject optionsObject = new("OptionsRoot", typeof(RectTransform));
                optionsObject.transform.SetParent(contentRect, false);
                optionsRoot = optionsObject.GetComponent<RectTransform>();
                optionsRoot.anchorMin = new Vector2(0.5f, 0.5f);
                optionsRoot.anchorMax = new Vector2(0.5f, 0.5f);
                optionsRoot.pivot = new Vector2(0.5f, 0.5f);
                optionsRoot.anchoredPosition = new Vector2(0f, -64f);
                optionsRoot.sizeDelta = new Vector2(1040f, 572f);

                VerticalLayoutGroup optionsLayout = optionsObject.AddComponent<VerticalLayoutGroup>();
                optionsLayout.spacing = 16f;
                optionsLayout.padding = new RectOffset(0, 0, 0, 0);
                optionsLayout.childAlignment = TextAnchor.UpperCenter;
                optionsLayout.childControlWidth = true;
                optionsLayout.childControlHeight = false;
                optionsLayout.childForceExpandWidth = true;
                optionsLayout.childForceExpandHeight = false;

                ContentSizeFitter optionsFitter = optionsObject.AddComponent<ContentSizeFitter>();
                optionsFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            }
        }

        private void EnsureOptionCount(int desiredCount)
        {
            while (_runtimeOptions.Count < desiredCount)
            {
                _runtimeOptions.Add(CreateRuntimeOption(_runtimeOptions.Count));
            }
        }

        private StartingBuildOptionView CreateRuntimeOption(int index)
        {
            GameObject optionObject = new($"BuildOption{index + 1}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(StartingBuildOptionView));
            optionObject.transform.SetParent(optionsRoot, false);

            RectTransform optionRect = optionObject.GetComponent<RectTransform>();
            optionRect.anchorMin = new Vector2(0.5f, 1f);
            optionRect.anchorMax = new Vector2(0.5f, 1f);
            optionRect.pivot = new Vector2(0.5f, 1f);
            optionRect.anchoredPosition = Vector2.zero;
            optionRect.sizeDelta = new Vector2(980f, 184f);

            LayoutElement layoutElement = optionObject.AddComponent<LayoutElement>();
            layoutElement.minHeight = 184f;
            layoutElement.preferredHeight = 184f;
            layoutElement.flexibleWidth = 1f;

            Image backgroundImage = optionObject.GetComponent<Image>();
            backgroundImage.color = optionCardColor;

            Image selectionFrame = CreateImage("SelectionFrame", optionRect, new Vector2(0f, 0f), Vector2.zero, Vector2.one, Vector2.zero, optionFrameColor);
            RectTransform selectionFrameRect = selectionFrame.rectTransform;
            selectionFrameRect.offsetMin = new Vector2(6f, 6f);
            selectionFrameRect.offsetMax = new Vector2(-6f, -6f);
            selectionFrame.enabled = false;

            Image accentBar = CreateImage("AccentBar", optionRect, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(18f, 168f), optionAccentColor);
            RectTransform accentRect = accentBar.rectTransform;
            accentRect.pivot = new Vector2(0f, 1f);

            Button button = optionObject.GetComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = optionHoverColor;
            colors.pressedColor = optionPressedColor;
            colors.selectedColor = optionHoverColor;
            button.colors = colors;

            Image iconFrame = CreateImage("IconFrame", optionRect, new Vector2(42f, -28f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(108f, 108f), new Color(0.2f, 0.12f, 0.05f, 0.16f));
            iconFrame.rectTransform.pivot = new Vector2(0f, 1f);

            Image iconImage = CreateImage("Icon", optionRect, new Vector2(48f, -34f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(96f, 96f), optionIconFallbackColor);
            Text nameText = CreateText("Name", optionRect, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(170f, -16f), new Vector2(-252f, 62f), 24, FontStyle.Bold, TextAnchor.UpperLeft);
            Text descriptionText = CreateText("Description", optionRect, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(170f, -82f), new Vector2(-252f, 48f), 16, FontStyle.Normal, TextAnchor.UpperLeft);
            descriptionText.color = optionDescriptionColor;
            Text loadoutText = CreateText("Loadout", optionRect, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(170f, -134f), new Vector2(-252f, 34f), 16, FontStyle.Bold, TextAnchor.UpperLeft);
            loadoutText.color = optionLoadoutColor;
            Text statusText = CreateText("Status", optionRect, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-24f, 0f), new Vector2(188f, 92f), 16, FontStyle.Bold, TextAnchor.MiddleRight);
            statusText.color = optionStatusColor;

            StartingBuildOptionView optionView = optionObject.GetComponent<StartingBuildOptionView>();
            optionView.Configure(button, backgroundImage, accentBar, selectionFrame, iconImage, nameText, descriptionText, loadoutText, statusText);
            optionView.SetTheme(
                optionCardColor,
                optionSelectedColor,
                optionAccentColor,
                optionSelectedAccentColor,
                optionFrameColor,
                titleColor,
                optionDescriptionColor,
                optionLoadoutColor,
                optionStatusColor,
                optionSelectedStatusColor,
                optionIconFallbackColor);
            return optionView;
        }

        private static Text CreateText(string objectName, RectTransform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 size, int fontSize, FontStyle fontStyle, TextAnchor alignment)
        {
            GameObject textObject = new(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            textObject.transform.SetParent(parent, false);

            RectTransform rectTransform = textObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.pivot = pivot;
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.sizeDelta = size;

            Text text = textObject.GetComponent<Text>();
            LocalizedUiFontProvider.ApplyReadableDefaults(
                text,
                fontSize,
                alignment,
                fontStyle,
                false,
                HorizontalWrapMode.Wrap,
                VerticalWrapMode.Truncate,
                1.08f);
            text.color = Color.white;
            return text;
        }

        private static Image CreateImage(string objectName, RectTransform parent, Vector2 anchoredPosition, Vector2 anchorMin, Vector2 anchorMax, Vector2 size, Color color)
        {
            GameObject imageObject = new(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            imageObject.transform.SetParent(parent, false);

            RectTransform rectTransform = imageObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.pivot = new Vector2(0f, 1f);
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.sizeDelta = size;

            Image image = imageObject.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }
    }
}
