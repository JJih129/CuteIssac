using CuteIssac.Item;
using UnityEngine;
using UnityEngine.UI;

namespace CuteIssac.UI
{
    [DisallowMultipleComponent]
    public sealed class SpeedBuffStatusPanelView : MonoBehaviour
    {
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Image frameImage;
        [SerializeField] private Image iconImage;
        [SerializeField] private Image fillImage;
        [SerializeField] private Text labelText;
        [SerializeField] private Color frameColor = new(0.08f, 0.16f, 0.2f, 0.72f);
        [SerializeField] private Color fillColor = new(0.42f, 0.82f, 1f, 0.82f);

        public static SpeedBuffStatusPanelView CreateRuntime(Transform parent)
        {
            GameObject root = new("SpeedBuffStatusPanel", typeof(RectTransform));
            root.transform.SetParent(parent, false);

            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0f, 1f);
            rootRect.anchorMax = new Vector2(0f, 1f);
            rootRect.pivot = new Vector2(0f, 1f);
            rootRect.anchoredPosition = new Vector2(16f, -96f);
            rootRect.sizeDelta = new Vector2(172f, 44f);

            SpeedBuffStatusPanelView view = root.AddComponent<SpeedBuffStatusPanelView>();

            Image frame = CreateImage("Frame", root.transform, Vector2.zero, rootRect.sizeDelta, new Color(0.08f, 0.16f, 0.2f, 0.72f));
            Image fill = CreateImage("Fill", root.transform, new Vector2(50f, -31f), new Vector2(112f, 6f), new Color(0.42f, 0.82f, 1f, 0.82f));
            Image icon = CreateImage("Icon", root.transform, new Vector2(10f, -8f), new Vector2(30f, 30f), Color.white);
            Text label = CreateText("Label", root.transform, new Vector2(48f, -8f), new Vector2(118f, 22f));

            view.panelRoot = root;
            view.frameImage = frame;
            view.iconImage = icon;
            view.fillImage = fill;
            view.labelText = label;
            view.Hide();
            return view;
        }

        public void Present(string displayName, Sprite icon, float normalizedRemaining)
        {
            if (panelRoot != null)
            {
                panelRoot.SetActive(true);
            }

            if (frameImage != null)
            {
                frameImage.color = frameColor;
            }

            if (iconImage != null)
            {
                iconImage.enabled = true;
                iconImage.sprite = icon != null ? icon : RuntimeShopIconFactory.GetSpeedCandySprite();
                iconImage.color = Color.white;
            }

            if (fillImage != null)
            {
                fillImage.color = fillColor;
                fillImage.fillAmount = Mathf.Clamp01(normalizedRemaining);
            }

            if (labelText != null)
            {
                labelText.text = string.IsNullOrWhiteSpace(displayName) ? "스피드 하트" : displayName;
            }
        }

        public void Hide()
        {
            if (panelRoot != null)
            {
                panelRoot.SetActive(false);
            }
        }

        private static Image CreateImage(string name, Transform parent, Vector2 anchoredPosition, Vector2 size, Color color)
        {
            GameObject obj = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            obj.transform.SetParent(parent, false);
            RectTransform rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            Image image = obj.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            if (name == "Fill")
            {
                image.type = Image.Type.Filled;
                image.fillMethod = Image.FillMethod.Horizontal;
                image.fillOrigin = 0;
            }

            return image;
        }

        private static Text CreateText(string name, Transform parent, Vector2 anchoredPosition, Vector2 size)
        {
            GameObject obj = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            obj.transform.SetParent(parent, false);
            RectTransform rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            Text text = obj.GetComponent<Text>();
            text.font = LocalizedUiFontProvider.GetFont();
            text.fontSize = 13;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleLeft;
            text.color = Color.white;
            text.raycastTarget = false;
            return text;
        }
    }
}
