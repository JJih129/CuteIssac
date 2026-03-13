using UnityEngine;
using UnityEngine.UI;

namespace CuteIssac.UI
{
    /// <summary>
    /// Presentation-only view for one minimap room node.
    /// Designers can replace the node prefab visuals later as long as these references stay wired.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MinimapNodeView : MonoBehaviour
    {
        [SerializeField] private RectTransform rootRect;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image iconImage;
        [SerializeField] private Image currentHighlightImage;
        [SerializeField] private Image clearedMarkerImage;

        public RectTransform RootRect => rootRect != null ? rootRect : (RectTransform)transform;

        public void ConfigureFallback(RectTransform rectTransform, Image background, Image icon, Image currentHighlight, Image clearedMarker)
        {
            rootRect = rectTransform;
            backgroundImage = background;
            iconImage = icon;
            currentHighlightImage = currentHighlight;
            clearedMarkerImage = clearedMarker;
        }

        public void SetAnchoredPosition(Vector2 anchoredPosition)
        {
            RootRect.anchoredPosition = anchoredPosition;
        }

        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }

        public void Present(
            Vector2 nodeSize,
            Color backgroundColor,
            Color iconColor,
            Sprite iconSprite,
            bool showCurrentHighlight,
            Color currentHighlightColor,
            bool showClearedMarker,
            Color clearedMarkerColor)
        {
            RootRect.sizeDelta = nodeSize;

            if (backgroundImage != null)
            {
                backgroundImage.color = backgroundColor;
            }

            if (iconImage != null)
            {
                iconImage.enabled = true;
                iconImage.sprite = iconSprite;
                iconImage.color = iconColor;
            }

            if (currentHighlightImage != null)
            {
                currentHighlightImage.gameObject.SetActive(showCurrentHighlight);
                currentHighlightImage.color = currentHighlightColor;
            }

            if (clearedMarkerImage != null)
            {
                clearedMarkerImage.gameObject.SetActive(showClearedMarker);
                clearedMarkerImage.color = clearedMarkerColor;
            }
        }
    }
}
