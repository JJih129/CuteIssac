using System.Collections.Generic;
using CuteIssac.Item;
using CuteIssac.Player;
using UnityEngine;
using UnityEngine.UI;

namespace CuteIssac.UI
{
    /// <summary>
    /// Presentation-only shop overlay. It renders slot snapshots and resource labels, but knows nothing about purchase logic.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ShopPanelView : MonoBehaviour
    {
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Image hintCardImage;
        [SerializeField] private Image selectionCardImage;
        [SerializeField] private Image selectionStatusCardImage;
        [SerializeField] private Image resourceCardImage;
        [SerializeField] private Text titleText;
        [SerializeField] private Text hintText;
        [SerializeField] private Text selectionText;
        [SerializeField] private Text selectionStatusText;
        [SerializeField] private Text resourceText;
        [SerializeField] private Image coinResourceChipImage;
        [SerializeField] private Image keyResourceChipImage;
        [SerializeField] private Image bombResourceChipImage;
        [SerializeField] private Text coinResourceValueText;
        [SerializeField] private Text keyResourceValueText;
        [SerializeField] private Text bombResourceValueText;
        [SerializeField] private RectTransform slotsRoot;
        [SerializeField] private ShopSlot slotTemplate;

        [Header("Fallback")]
        [SerializeField] [Min(2)] private int fallbackSlotCount = 3;
        [SerializeField] private Vector2 fallbackPanelSize = new(472f, 520f);
        [SerializeField] private Vector2 fallbackPanelOffset = new(-18f, 0f);
        [SerializeField] [Min(16)] private int fallbackTitleFontSize = 30;
        [SerializeField] [Min(12)] private int fallbackHintFontSize = 16;
        [SerializeField] [Min(12)] private int fallbackResourceFontSize = 16;
        [SerializeField] [Min(14)] private int fallbackSelectionFontSize = 20;
        [SerializeField] [Min(12)] private int fallbackSelectionStatusFontSize = 16;
        [SerializeField] [Min(40f)] private float fallbackSlotHeight = 74f;
        [SerializeField] [Min(4f)] private float fallbackSlotSpacing = 10f;
        [SerializeField] [Min(14)] private int fallbackSlotNameFontSize = 19;
        [SerializeField] [Min(14)] private int fallbackSlotPriceFontSize = 18;
        [SerializeField] [Min(12)] private int fallbackSlotStatusFontSize = 15;

        [Header("Theme")]
        [SerializeField] private Color panelColor = new(0.9f, 0.85f, 0.76f, 0.94f);
        [SerializeField] private Color slotColor = new(0.31f, 0.25f, 0.18f, 0.94f);
        [SerializeField] private Color titleColor = new(0.18f, 0.1f, 0.06f, 1f);
        [SerializeField] private Color hintColor = new(0.28f, 0.18f, 0.1f, 0.92f);
        [SerializeField] private Color idleSelectionTitleColor = new(0.2f, 0.13f, 0.08f, 1f);
        [SerializeField] private Color idleSelectionStatusColor = new(0.52f, 0.28f, 0.2f, 1f);
        [SerializeField] private Color resourceColor = new(0.24f, 0.15f, 0.1f, 0.96f);
        [SerializeField] private Color hintCardColor = new(1f, 1f, 1f, 0.08f);
        [SerializeField] private Color selectionCardColor = new(1f, 0.96f, 0.88f, 0.1f);
        [SerializeField] private Color selectionStatusCardColor = new(1f, 0.94f, 0.84f, 0.08f);
        [SerializeField] private Color resourceCardColor = new(1f, 1f, 1f, 0.08f);
        [SerializeField] private Color resourceChipColor = new(0.22f, 0.16f, 0.1f, 0.92f);
        [SerializeField] private Color coinChipAccentColor = new(1f, 0.84f, 0.25f, 1f);
        [SerializeField] private Color keyChipAccentColor = new(0.72f, 0.88f, 1f, 1f);
        [SerializeField] private Color bombChipAccentColor = new(1f, 0.6f, 0.32f, 1f);
        [SerializeField] private Color slotIconFallbackColor = new(1f, 1f, 1f, 0.16f);
        [SerializeField] private Color slotAccentBarColor = new(1f, 0.84f, 0.25f, 1f);
        [SerializeField] private Color slotHighlightFrameColor = new(1f, 0.92f, 0.64f, 0.92f);
        [SerializeField] private Color slotIconFrameColor = new(0.17f, 0.12f, 0.08f, 0.92f);
        [SerializeField] private Color slotBadgeColor = new(1f, 0.84f, 0.25f, 1f);
        [SerializeField] private Color slotNameColor = new(0.97f, 0.94f, 0.88f, 1f);
        [SerializeField] private Color slotPriceColor = new(1f, 0.9f, 0.52f, 1f);
        [SerializeField] private Color slotStatusColor = new(0.86f, 0.8f, 0.7f, 1f);
        [SerializeField] private Color soldColor = new(0.72f, 0.72f, 0.76f, 1f);
        [SerializeField] private Color affordableColor = new(0.56f, 0.86f, 0.58f, 1f);
        [SerializeField] private Color coinShortageColor = new(0.92f, 0.7f, 0.24f, 1f);
        [SerializeField] private Color keyShortageColor = new(0.48f, 0.82f, 0.96f, 1f);
        [SerializeField] private Color bombShortageColor = new(0.92f, 0.4f, 0.38f, 1f);
        [SerializeField] private Color ownedColor = new(0.72f, 0.64f, 0.9f, 1f);
        [SerializeField] private Color healthMaxColor = new(0.92f, 0.42f, 0.56f, 1f);
        [SerializeField] private Color defaultWarningColor = new(0.74f, 0.42f, 0.32f, 1f);
        [SerializeField] private Color selectionValueColor = new(1f, 0.93f, 0.72f, 1f);
        [SerializeField] private Color resourceValueColor = new(0.98f, 0.95f, 0.84f, 1f);

        private readonly List<ShopSlot> _runtimeSlots = new();

        public void Present(IReadOnlyList<ShopSlotState> slotStates, PlayerResourceSnapshot resources)
        {
            EnsureFallbackVisuals();

            if (panelRoot != null)
            {
                panelRoot.SetActive(true);
            }

            ApplyTheme();

            if (titleText != null)
            {
                titleText.text = "상점";
            }

            if (hintText != null)
            {
                hintText.text = "E 또는 Shift로 선택 상품을 구매합니다";
            }

            if (resourceText != null)
            {
                resourceText.text = "보유 재화";
            }

            UpdateResourceChip(coinResourceChipImage, coinResourceValueText, coinChipAccentColor, "코인", resources.Coins);
            UpdateResourceChip(keyResourceChipImage, keyResourceValueText, keyChipAccentColor, "열쇠", resources.Keys);
            UpdateResourceChip(bombResourceChipImage, bombResourceValueText, bombChipAccentColor, "폭탄", resources.Bombs);

            PresentSelectionSummary(slotStates);

            int desiredSlots = Mathf.Max(fallbackSlotCount, slotStates != null ? slotStates.Count : 0);
            EnsureSlotCount(desiredSlots);

            for (int index = 0; index < _runtimeSlots.Count; index++)
            {
                ShopSlotState state = index < (slotStates != null ? slotStates.Count : 0)
                    ? slotStates[index]
                    : default;
                _runtimeSlots[index].Present(state);
            }
        }

        public void Hide()
        {
            if (panelRoot != null)
            {
                panelRoot.SetActive(false);
            }
        }

        public static ShopPanelView CreateRuntime(Canvas canvas)
        {
            if (canvas == null)
            {
                return null;
            }

            GameObject rootObject = new("ShopPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(ShopPanelView));
            rootObject.transform.SetParent(canvas.transform, false);
            ShopPanelView panelView = rootObject.GetComponent<ShopPanelView>();
            RectTransform rectTransform = rootObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(1f, 0.5f);
            rectTransform.anchorMax = new Vector2(1f, 0.5f);
            rectTransform.pivot = new Vector2(1f, 0.5f);
            rectTransform.anchoredPosition = panelView.fallbackPanelOffset;
            rectTransform.sizeDelta = panelView.fallbackPanelSize;

            Image background = rootObject.GetComponent<Image>();
            background.color = panelView.panelColor;

            panelView.panelRoot = rootObject;
            panelView.BuildFallbackVisuals(rectTransform);
            panelView.ApplyTheme();
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
            if (rootRect != null && (titleText == null || hintText == null || selectionText == null || selectionStatusText == null || resourceText == null || slotsRoot == null))
            {
                BuildFallbackVisuals(rootRect);
            }
        }

        private void ApplyTheme()
        {
            if (panelRoot != null && panelRoot.TryGetComponent(out Image panelImage))
            {
                panelImage.color = panelColor;
            }

            if (hintCardImage != null)
            {
                hintCardImage.color = hintCardColor;
            }

            if (selectionCardImage != null)
            {
                selectionCardImage.color = selectionCardColor;
            }

            if (selectionStatusCardImage != null)
            {
                selectionStatusCardImage.color = selectionStatusCardColor;
            }

            if (resourceCardImage != null)
            {
                resourceCardImage.color = resourceCardColor;
            }

            if (titleText != null)
            {
                titleText.color = titleColor;
                titleText.supportRichText = false;
            }

            if (hintText != null)
            {
                hintText.color = hintColor;
                hintText.supportRichText = false;
            }

            if (selectionText != null)
            {
                selectionText.supportRichText = false;
            }

            if (selectionStatusText != null)
            {
                selectionStatusText.supportRichText = false;
            }

            if (resourceText != null)
            {
                resourceText.color = resourceColor;
                resourceText.supportRichText = false;
            }
        }

        private void BuildFallbackVisuals(RectTransform rootRect)
        {
            if (titleText == null)
            {
                titleText = CreateText("Title", rootRect, new Vector2(24f, -18f), new Vector2(220f, 34f), TextAnchor.UpperLeft, fallbackTitleFontSize, FontStyle.Bold);
                titleText.text = "상점";
            }

            if (hintCardImage == null)
            {
                hintCardImage = CreatePanelCard("HintCard", rootRect, new Vector2(20f, -60f), new Vector2(432f, 42f), hintCardColor);
            }

            if (hintText == null)
            {
                hintText = CreateText("Hint", rootRect, new Vector2(32f, -69f), new Vector2(392f, 24f), TextAnchor.UpperLeft, fallbackHintFontSize, FontStyle.Normal);
            }

            if (selectionCardImage == null)
            {
                selectionCardImage = CreatePanelCard("SelectionCard", rootRect, new Vector2(20f, -114f), new Vector2(432f, 58f), selectionCardColor);
            }

            if (selectionText == null)
            {
                selectionText = CreateText("Selection", rootRect, new Vector2(32f, -124f), new Vector2(392f, 24f), TextAnchor.UpperLeft, fallbackSelectionFontSize, FontStyle.Bold);
            }

            if (selectionStatusCardImage == null)
            {
                selectionStatusCardImage = CreatePanelCard("SelectionStatusCard", rootRect, new Vector2(20f, -178f), new Vector2(432f, 42f), selectionStatusCardColor);
            }

            if (selectionStatusText == null)
            {
                selectionStatusText = CreateText("SelectionStatus", rootRect, new Vector2(32f, -188f), new Vector2(392f, 22f), TextAnchor.UpperLeft, fallbackSelectionStatusFontSize, FontStyle.Normal);
            }

            if (resourceCardImage == null)
            {
                resourceCardImage = CreatePanelCard("ResourceCard", rootRect, new Vector2(20f, -228f), new Vector2(432f, 44f), resourceCardColor);
            }

            if (resourceText == null)
            {
                resourceText = CreateText("Resources", rootRect, new Vector2(32f, -236f), new Vector2(92f, 20f), TextAnchor.UpperLeft, fallbackResourceFontSize, FontStyle.Bold);
            }

            if (coinResourceChipImage == null)
            {
                coinResourceChipImage = CreatePanelCard("CoinResourceChip", rootRect, new Vector2(132f, -234f), new Vector2(96f, 28f), resourceChipColor);
                coinResourceValueText = CreateText("CoinResourceValue", rootRect, new Vector2(144f, -239f), new Vector2(72f, 18f), TextAnchor.MiddleLeft, fallbackResourceFontSize - 1, FontStyle.Bold);
            }

            if (keyResourceChipImage == null)
            {
                keyResourceChipImage = CreatePanelCard("KeyResourceChip", rootRect, new Vector2(236f, -234f), new Vector2(96f, 28f), resourceChipColor);
                keyResourceValueText = CreateText("KeyResourceValue", rootRect, new Vector2(248f, -239f), new Vector2(72f, 18f), TextAnchor.MiddleLeft, fallbackResourceFontSize - 1, FontStyle.Bold);
            }

            if (bombResourceChipImage == null)
            {
                bombResourceChipImage = CreatePanelCard("BombResourceChip", rootRect, new Vector2(340f, -234f), new Vector2(96f, 28f), resourceChipColor);
                bombResourceValueText = CreateText("BombResourceValue", rootRect, new Vector2(352f, -239f), new Vector2(72f, 18f), TextAnchor.MiddleLeft, fallbackResourceFontSize - 1, FontStyle.Bold);
            }

            if (slotsRoot == null)
            {
                GameObject slotsObject = new("SlotsRoot", typeof(RectTransform));
                slotsObject.transform.SetParent(rootRect, false);
                slotsRoot = slotsObject.GetComponent<RectTransform>();
                slotsRoot.anchorMin = new Vector2(0f, 0f);
                slotsRoot.anchorMax = new Vector2(1f, 0f);
                slotsRoot.pivot = new Vector2(0.5f, 0f);
                slotsRoot.anchoredPosition = new Vector2(0f, 18f);
                slotsRoot.sizeDelta = new Vector2(-40f, fallbackPanelSize.y - 286f);

                VerticalLayoutGroup slotLayout = slotsObject.AddComponent<VerticalLayoutGroup>();
                slotLayout.padding = new RectOffset(0, 0, 4, 0);
                slotLayout.spacing = Mathf.RoundToInt(fallbackSlotSpacing);
                slotLayout.childAlignment = TextAnchor.UpperCenter;
                slotLayout.childControlWidth = true;
                slotLayout.childControlHeight = false;
                slotLayout.childForceExpandWidth = true;
                slotLayout.childForceExpandHeight = false;
            }
        }

        private void PresentSelectionSummary(IReadOnlyList<ShopSlotState> slotStates)
        {
            ShopSlotState focusedState = default;
            bool hasFocusedState = false;

            if (slotStates != null)
            {
                for (int index = 0; index < slotStates.Count; index++)
                {
                    if (!slotStates[index].IsHighlighted)
                    {
                        continue;
                    }

                    focusedState = slotStates[index];
                    hasFocusedState = true;
                    break;
                }
            }

            if (selectionText != null)
            {
                selectionText.text = hasFocusedState
                    ? BuildSelectionTitle(focusedState)
                    : "선택 상품 · 없음";
                selectionText.color = hasFocusedState
                    ? ResolveSelectionTitleColor(focusedState)
                    : idleSelectionTitleColor;
            }

            if (selectionStatusText != null)
            {
                selectionStatusText.text = hasFocusedState
                    ? BuildSelectionStatus(focusedState)
                    : "가까이 가면 자동으로 선택됩니다";
                selectionStatusText.color = hasFocusedState
                    ? ResolveStatusColor(focusedState.StatusLabel, focusedState.CanPurchase, focusedState.IsSold)
                    : idleSelectionStatusColor;
            }
        }

        private string BuildSelectionTitle(ShopSlotState state)
        {
            return $"선택 상품 · {state.DisplayName}";
        }

        private string BuildSelectionStatus(ShopSlotState state)
        {
            return $"{state.PriceLabel} · {state.StatusLabel}";
        }

        private void UpdateResourceChip(Image chipImage, Text valueText, Color accentColor, string label, int value)
        {
            if (chipImage != null)
            {
                chipImage.color = Color.Lerp(resourceChipColor, accentColor, 0.18f);
            }

            if (valueText != null)
            {
                valueText.supportRichText = false;
                valueText.color = resourceValueColor;
                valueText.text = $"{label} {value}";
            }
        }

        private Color ResolveSelectionTitleColor(ShopSlotState state)
        {
            if (state.IsSold)
            {
                return soldColor;
            }

            if (state.CanPurchase)
            {
                return selectionValueColor;
            }

            return ResolveStatusColor(state.StatusLabel, false, false);
        }

        private Color ResolveStatusColor(string statusLabel, bool canPurchase, bool isSold)
        {
            if (isSold)
            {
                return soldColor;
            }

            if (canPurchase)
            {
                return affordableColor;
            }

            return statusLabel switch
            {
                "코인 부족" => coinShortageColor,
                "열쇠 부족" => keyShortageColor,
                "폭탄 부족" => bombShortageColor,
                "이미 보유" => ownedColor,
                "체력 가득" => healthMaxColor,
                _ => defaultWarningColor
            };
        }

        private void EnsureSlotCount(int desiredSlots)
        {
            if (slotsRoot == null)
            {
                return;
            }

            while (_runtimeSlots.Count < desiredSlots)
            {
                _runtimeSlots.Add(CreateFallbackSlot(_runtimeSlots.Count));
            }
        }

        private ShopSlot CreateFallbackSlot(int slotIndex)
        {
            GameObject slotObject = new($"Slot{slotIndex + 1}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(ShopSlot));
            slotObject.transform.SetParent(slotsRoot, false);

            RectTransform slotRect = slotObject.GetComponent<RectTransform>();
            slotRect.anchorMin = new Vector2(0.5f, 1f);
            slotRect.anchorMax = new Vector2(0.5f, 1f);
            slotRect.pivot = new Vector2(0.5f, 1f);
            slotRect.anchoredPosition = Vector2.zero;
            slotRect.sizeDelta = new Vector2(0f, fallbackSlotHeight);

            LayoutElement layoutElement = slotObject.AddComponent<LayoutElement>();
            layoutElement.minHeight = fallbackSlotHeight;
            layoutElement.preferredHeight = fallbackSlotHeight;
            layoutElement.flexibleWidth = 1f;

            Image background = slotObject.GetComponent<Image>();
            background.color = slotColor;

            ShopSlot slotView = slotObject.GetComponent<ShopSlot>();
            BindFallbackSlotVisuals(slotRect, slotView);
            return slotView;
        }

        private void BindFallbackSlotVisuals(RectTransform slotRect, ShopSlot slotView)
        {
            Image backgroundImage = slotRect.GetComponent<Image>();
            Image accentBarImage = CreateImage("AccentBar", slotRect, new Vector2(0f, 0f), new Vector2(4f, fallbackSlotHeight), slotAccentBarColor);
            accentBarImage.rectTransform.anchorMin = new Vector2(0f, 0f);
            accentBarImage.rectTransform.anchorMax = new Vector2(0f, 1f);
            accentBarImage.rectTransform.pivot = new Vector2(0f, 0.5f);
            accentBarImage.rectTransform.anchoredPosition = Vector2.zero;

            Image highlightFrameImage = CreateStretchImage("HighlightFrame", slotRect, new Vector2(2f, 2f), new Vector2(-2f, -2f), slotHighlightFrameColor);
            Image iconFrameImage = CreateImage("IconFrame", slotRect, new Vector2(14f, -10f), new Vector2(52f, 52f), slotIconFrameColor);
            Image iconImage = CreateImage("Icon", slotRect, new Vector2(18f, -14f), new Vector2(44f, 44f), slotIconFallbackColor);
            Image badgeImage = CreateImage("Badge", slotRect, new Vector2(70f, -16f), new Vector2(10f, 10f), slotBadgeColor);
            Image priceChipImage = CreateImage("PriceChip", slotRect, new Vector2(-14f, -10f), new Vector2(110f, 28f), slotPriceColor, true);
            Image statusChipImage = CreateImage("StatusChip", slotRect, new Vector2(-14f, -44f), new Vector2(110f, 22f), slotStatusColor, true);
            Text name = CreateText("Name", slotRect, new Vector2(84f, -12f), new Vector2(218f, 24f), TextAnchor.UpperLeft, fallbackSlotNameFontSize, FontStyle.Bold);
            Text price = CreateText("Price", slotRect, new Vector2(-14f, -10f), new Vector2(110f, 28f), TextAnchor.MiddleCenter, fallbackSlotPriceFontSize, FontStyle.Bold, true);
            Text status = CreateText("Status", slotRect, new Vector2(-14f, -44f), new Vector2(110f, 22f), TextAnchor.MiddleCenter, fallbackSlotStatusFontSize, FontStyle.Bold, true);

            name.color = slotNameColor;
            price.color = slotPriceColor;
            status.color = slotStatusColor;

            slotView.ConfigureRuntimeView(slotRect.gameObject, backgroundImage, accentBarImage, highlightFrameImage, iconFrameImage, iconImage, badgeImage, priceChipImage, statusChipImage, name, price, status);
        }

        private static Text CreateText(string name, RectTransform parent, Vector2 anchoredPosition, Vector2 size, TextAnchor anchor, int fontSize, FontStyle style, bool anchorRight = false)
        {
            GameObject textObject = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            textObject.transform.SetParent(parent, false);
            RectTransform rectTransform = textObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = anchorRight ? new Vector2(1f, 1f) : new Vector2(0f, 1f);
            rectTransform.anchorMax = anchorRight ? new Vector2(1f, 1f) : new Vector2(0f, 1f);
            rectTransform.pivot = anchorRight ? new Vector2(1f, 1f) : new Vector2(0f, 1f);
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.sizeDelta = size;

            Text text = textObject.GetComponent<Text>();
            LocalizedUiFontProvider.ApplyReadableDefaults(
                text,
                fontSize,
                anchor,
                style,
                false,
                HorizontalWrapMode.Wrap,
                VerticalWrapMode.Truncate,
                1.06f);
            text.color = Color.white;
            return text;
        }

        private static Image CreateImage(string name, RectTransform parent, Vector2 anchoredPosition, Vector2 size, Color color, bool anchorRight = false)
        {
            GameObject imageObject = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            imageObject.transform.SetParent(parent, false);
            RectTransform rectTransform = imageObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = anchorRight ? new Vector2(1f, 1f) : new Vector2(0f, 1f);
            rectTransform.anchorMax = anchorRight ? new Vector2(1f, 1f) : new Vector2(0f, 1f);
            rectTransform.pivot = anchorRight ? new Vector2(1f, 1f) : new Vector2(0f, 1f);
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.sizeDelta = size;

            Image image = imageObject.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static Image CreateStretchImage(string name, RectTransform parent, Vector2 offsetMin, Vector2 offsetMax, Color color)
        {
            GameObject imageObject = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            imageObject.transform.SetParent(parent, false);

            RectTransform rectTransform = imageObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.offsetMin = offsetMin;
            rectTransform.offsetMax = offsetMax;

            Image image = imageObject.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static Image CreatePanelCard(string name, RectTransform parent, Vector2 anchoredPosition, Vector2 size, Color color)
        {
            GameObject imageObject = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            imageObject.transform.SetParent(parent, false);

            RectTransform rectTransform = imageObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0f, 1f);
            rectTransform.anchorMax = new Vector2(0f, 1f);
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
