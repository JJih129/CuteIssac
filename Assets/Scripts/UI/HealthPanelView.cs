using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CuteIssac.UI
{
    /// <summary>
    /// Pure presentation component for player health.
    /// Assign a text label and optionally a heart slot template plus sprites to swap the visual skin later without touching gameplay logic.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HealthPanelView : MonoBehaviour
    {
        [Header("Optional Root")]
        [Tooltip("Optional. Hide or replace the whole panel by swapping this root object.")]
        [SerializeField] private GameObject panelRoot;

        [Header("Value Label")]
        [Tooltip("Optional. Displays a simple numeric HP label.")]
        [SerializeField] private Text healthValueText;

        [Header("Heart Slots")]
        [Tooltip("Parent transform that receives generated heart slot instances.")]
        [SerializeField] private RectTransform heartSlotParent;
        [Tooltip("Template image used to generate heart slots. It can be any skinned UI element with an Image component.")]
        [SerializeField] private Image heartSlotTemplate;
        [Tooltip("Spacing used when the panel lays out heart slots without a separate layout group.")]
        [SerializeField] [Min(0f)] private float slotSpacing = 6f;
        [Tooltip("Optional sprite shown for filled hearts.")]
        [SerializeField] private Sprite filledHeartSprite;
        [Tooltip("Optional sprite shown for empty hearts.")]
        [SerializeField] private Sprite emptyHeartSprite;
        [Tooltip("Fallback tint used when no filled sprite is assigned.")]
        [SerializeField] private Color filledHeartColor = new(0.95f, 0.3f, 0.3f, 1f);
        [Tooltip("Fallback tint used when no empty sprite is assigned.")]
        [SerializeField] private Color emptyHeartColor = new(0.3f, 0.3f, 0.3f, 0.8f);

        private readonly List<Image> _runtimeSlots = new();
        private bool _warnedMissingSlotSetup;

        public void ConfigureDebugView(Text valueText, RectTransform slotParent, Image slotTemplate)
        {
            healthValueText = valueText;
            heartSlotParent = slotParent;
            heartSlotTemplate = slotTemplate;
        }

        public void SetHealth(float currentHealth, float maxHealth)
        {
            if (panelRoot != null && !panelRoot.activeSelf)
            {
                panelRoot.SetActive(true);
            }

            if (healthValueText != null)
            {
                healthValueText.text = $"HP {Mathf.CeilToInt(currentHealth)}/{Mathf.CeilToInt(maxHealth)}";
            }

            int totalSlots = Mathf.Max(0, Mathf.CeilToInt(Mathf.Max(0f, maxHealth)));
            int filledSlots = Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(0f, currentHealth)), 0, totalSlots);

            if (heartSlotParent == null || heartSlotTemplate == null)
            {
                if (!_warnedMissingSlotSetup)
                {
                    Debug.LogWarning("HealthPanelView is missing heartSlotParent or heartSlotTemplate. The panel will fall back to text-only health display.", this);
                    _warnedMissingSlotSetup = true;
                }

                return;
            }

            EnsureSlotCount(totalSlots);

            for (int i = 0; i < _runtimeSlots.Count; i++)
            {
                Image slotImage = _runtimeSlots[i];
                bool isFilled = i < filledSlots;

                if (slotImage == null)
                {
                    continue;
                }

                slotImage.gameObject.SetActive(true);

                if (isFilled && filledHeartSprite != null)
                {
                    slotImage.sprite = filledHeartSprite;
                }
                else if (!isFilled && emptyHeartSprite != null)
                {
                    slotImage.sprite = emptyHeartSprite;
                }

                slotImage.color = isFilled ? filledHeartColor : emptyHeartColor;
            }
        }

        private void EnsureSlotCount(int desiredCount)
        {
            while (_runtimeSlots.Count < desiredCount)
            {
                Image newSlot = Instantiate(heartSlotTemplate, heartSlotParent);
                newSlot.gameObject.name = "HeartSlot";
                newSlot.gameObject.SetActive(true);
                _runtimeSlots.Add(newSlot);
            }

            for (int i = 0; i < _runtimeSlots.Count; i++)
            {
                bool shouldBeVisible = i < desiredCount;

                if (_runtimeSlots[i] != null)
                {
                    LayoutSlot(_runtimeSlots[i], i);
                    _runtimeSlots[i].gameObject.SetActive(shouldBeVisible);
                }
            }
        }

        private void LayoutSlot(Image slotImage, int index)
        {
            RectTransform templateRect = heartSlotTemplate.rectTransform;
            RectTransform slotRect = slotImage.rectTransform;

            slotRect.anchorMin = templateRect.anchorMin;
            slotRect.anchorMax = templateRect.anchorMax;
            slotRect.pivot = templateRect.pivot;
            slotRect.sizeDelta = templateRect.sizeDelta;
            slotRect.localScale = templateRect.localScale;

            float width = templateRect.rect.width > 0f ? templateRect.rect.width : templateRect.sizeDelta.x;
            float x = templateRect.anchoredPosition.x + (index * (width + slotSpacing));
            slotRect.anchoredPosition = new Vector2(x, templateRect.anchoredPosition.y);
        }
    }
}
