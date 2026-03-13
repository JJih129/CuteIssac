using UnityEngine;
using UnityEngine.UI;

namespace CuteIssac.UI
{
    /// <summary>
    /// Presentation-only active item slot.
    /// The current prototype reserves the layout and placeholder state so a skinned active item system can plug in later.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ActiveItemPanelView : MonoBehaviour
    {
        [Header("Optional Root")]
        [Tooltip("Optional. Root object for the entire active item panel.")]
        [SerializeField] private GameObject panelRoot;

        [Header("Skinnable Elements")]
        [Tooltip("Optional frame image for the active item slot.")]
        [SerializeField] private Image frameImage;
        [Tooltip("Optional icon image for the active item itself.")]
        [SerializeField] private Image iconImage;
        [Tooltip("Optional slider used by skins that prefer a fill bar for charge.")]
        [SerializeField] private Slider chargeSlider;
        [Tooltip("Optional image fill used by skins that prefer a radial or horizontal charge overlay.")]
        [SerializeField] private Image chargeFillImage;
        [Tooltip("Optional fallback label. Useful when the slot has no icon yet.")]
        [SerializeField] private Text labelText;

        private bool _warnedMissingPresentationRef;

        public void ConfigureDebugView(Text label, Image frame = null)
        {
            labelText = label;
            frameImage = frame;
            panelRoot = label != null && label.transform.parent != null
                ? label.transform.parent.gameObject
                : panelRoot;
        }

        public void ShowPlaceholder()
        {
            if (panelRoot != null)
            {
                panelRoot.SetActive(true);
            }

            if (labelText == null && iconImage == null && chargeSlider == null && chargeFillImage == null && !_warnedMissingPresentationRef)
            {
                Debug.LogWarning("ActiveItemPanelView has no skinnable references assigned. The placeholder slot will stay logical-only until at least one view reference is connected.", this);
                _warnedMissingPresentationRef = true;
            }

            if (iconImage != null)
            {
                iconImage.enabled = false;
            }

            if (labelText != null)
            {
                labelText.text = "ACTIVE --";
            }

            if (chargeSlider != null)
            {
                chargeSlider.value = 0f;
            }

            if (chargeFillImage != null)
            {
                chargeFillImage.fillAmount = 0f;
            }
        }
    }
}
