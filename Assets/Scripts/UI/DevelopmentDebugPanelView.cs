using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace CuteIssac.UI
{
    /// <summary>
    /// Development-only panel view. It knows how to render buttons and labels, but not what game logic those buttons trigger.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DevelopmentDebugPanelView : MonoBehaviour
    {
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Text titleText;
        [SerializeField] private Text subtitleText;
        [SerializeField] private Text detailsText;
        [SerializeField] private RectTransform buttonsRoot;

        private readonly List<Button> _runtimeButtons = new();

        public bool IsVisible => panelRoot != null && panelRoot.activeSelf;

        public void Present(IReadOnlyList<DebugPanelButtonModel> buttons, string subtitle, string details = null)
        {
            EnsureFallbackVisuals();

            if (panelRoot != null)
            {
                panelRoot.SetActive(true);
            }

            if (titleText != null)
            {
                titleText.text = "개발 디버그";
            }

            if (subtitleText != null)
            {
                subtitleText.text = subtitle;
            }

            if (detailsText != null)
            {
                detailsText.text = string.IsNullOrWhiteSpace(details) ? "디버그 스냅샷 없음" : details;
            }

            int desiredCount = buttons != null ? buttons.Count : 0;
            EnsureButtonCount(desiredCount);

            for (int index = 0; index < _runtimeButtons.Count; index++)
            {
                Button button = _runtimeButtons[index];
                bool active = index < desiredCount && buttons[index] != null;
                button.gameObject.SetActive(active);

                if (!active)
                {
                    continue;
                }

                DebugPanelButtonModel model = buttons[index];
                Text labelText = button.GetComponentInChildren<Text>();

                if (labelText != null)
                {
                    labelText.text = model.Label;
                }

                Image background = button.GetComponent<Image>();

                if (background != null)
                {
                    background.color = model.AccentColor;
                }

                button.onClick.RemoveAllListeners();

                if (model.OnPressed != null)
                {
                    button.onClick.AddListener(model.OnPressed);
                }
            }
        }

        public void Hide()
        {
            if (panelRoot != null)
            {
                panelRoot.SetActive(false);
            }
        }

        public void Toggle()
        {
            if (IsVisible)
            {
                Hide();
                return;
            }

            if (panelRoot != null)
            {
                panelRoot.SetActive(true);
            }
        }

        public static DevelopmentDebugPanelView CreateRuntime(Canvas canvas)
        {
            if (canvas == null)
            {
                return null;
            }

            GameObject rootObject = new("DevelopmentDebugPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(DevelopmentDebugPanelView));
            rootObject.transform.SetParent(canvas.transform, false);

            RectTransform rootRect = rootObject.GetComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0f, 1f);
            rootRect.anchorMax = new Vector2(0f, 1f);
            rootRect.pivot = new Vector2(0f, 1f);
            rootRect.anchoredPosition = new Vector2(24f, -24f);
            rootRect.sizeDelta = new Vector2(460f, 820f);

            Image background = rootObject.GetComponent<Image>();
            background.color = new Color(0.04f, 0.06f, 0.1f, 0.92f);

            DevelopmentDebugPanelView panelView = rootObject.GetComponent<DevelopmentDebugPanelView>();
            panelView.panelRoot = rootObject;
            panelView.BuildFallbackVisuals(rootRect);
            panelView.Hide();
            return panelView;
        }

        private void EnsureFallbackVisuals()
        {
            if (panelRoot == null)
            {
                panelRoot = gameObject;
            }

            RectTransform rootRect = panelRoot.transform as RectTransform;

            if (rootRect != null && (titleText == null || subtitleText == null || detailsText == null || buttonsRoot == null))
            {
                BuildFallbackVisuals(rootRect);
            }
        }

        private void BuildFallbackVisuals(RectTransform rootRect)
        {
            if (titleText == null)
            {
                titleText = CreateText("Title", rootRect, new Vector2(16f, -16f), new Vector2(340f, 36f), 28, FontStyle.Bold);
            }

            if (subtitleText == null)
            {
                subtitleText = CreateText("Subtitle", rootRect, new Vector2(16f, -56f), new Vector2(404f, 54f), 17, FontStyle.Normal);
                subtitleText.color = new Color(0.82f, 0.88f, 0.94f, 0.94f);
            }

            if (detailsText == null)
            {
                detailsText = CreateText("Details", rootRect, new Vector2(16f, -122f), new Vector2(412f, 220f), 15, FontStyle.Normal);
                detailsText.color = new Color(0.8f, 0.9f, 1f, 0.92f);
                detailsText.alignment = TextAnchor.UpperLeft;
            }

            if (buttonsRoot == null)
            {
                GameObject buttonsObject = new("ButtonsRoot", typeof(RectTransform));
                buttonsObject.transform.SetParent(rootRect, false);
                buttonsRoot = buttonsObject.GetComponent<RectTransform>();
                buttonsRoot.anchorMin = new Vector2(0f, 1f);
                buttonsRoot.anchorMax = new Vector2(0f, 1f);
                buttonsRoot.pivot = new Vector2(0f, 1f);
                buttonsRoot.anchoredPosition = new Vector2(16f, -360f);
                buttonsRoot.sizeDelta = new Vector2(412f, 420f);
            }
        }

        private void EnsureButtonCount(int desiredCount)
        {
            while (_runtimeButtons.Count < desiredCount)
            {
                _runtimeButtons.Add(CreateRuntimeButton(_runtimeButtons.Count));
            }
        }

        private Button CreateRuntimeButton(int index)
        {
            GameObject buttonObject = new($"DebugButton{index + 1}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(buttonsRoot, false);

            RectTransform rectTransform = buttonObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0f, 1f);
            rectTransform.anchorMax = new Vector2(0f, 1f);
            rectTransform.pivot = new Vector2(0f, 1f);
            rectTransform.anchoredPosition = new Vector2(0f, -(index * 48f));
            rectTransform.sizeDelta = new Vector2(412f, 40f);

            Image background = buttonObject.GetComponent<Image>();
            background.color = new Color(0.16f, 0.22f, 0.3f, 0.96f);

            Button button = buttonObject.GetComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.98f);
            colors.pressedColor = new Color(0.9f, 0.9f, 0.9f, 0.98f);
            button.colors = colors;

            Text label = CreateText("Label", rectTransform, new Vector2(12f, -8f), new Vector2(388f, 26f), 16, FontStyle.Bold);
            label.alignment = TextAnchor.MiddleLeft;

            return button;
        }

        private static Text CreateText(string objectName, RectTransform parent, Vector2 anchoredPosition, Vector2 size, int fontSize, FontStyle fontStyle)
        {
            GameObject textObject = new(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            textObject.transform.SetParent(parent, false);

            RectTransform rectTransform = textObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0f, 1f);
            rectTransform.anchorMax = new Vector2(0f, 1f);
            rectTransform.pivot = new Vector2(0f, 1f);
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.sizeDelta = size;

            Text text = textObject.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.color = Color.white;
            text.alignment = TextAnchor.UpperLeft;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }
    }

    public sealed class DebugPanelButtonModel
    {
        public DebugPanelButtonModel(string label, Color accentColor, UnityAction onPressed)
        {
            Label = label;
            AccentColor = accentColor;
            OnPressed = onPressed;
        }

        public string Label { get; }
        public Color AccentColor { get; }
        public UnityAction OnPressed { get; }
    }
}
