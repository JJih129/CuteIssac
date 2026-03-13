using UnityEngine;
using UnityEngine.UI;

namespace CuteIssac.UI
{
    /// <summary>
    /// Presentation-only boss health bar panel.
    /// Hide it by default and let future boss encounter systems show it explicitly.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BossHpPanelView : MonoBehaviour
    {
        [Header("Optional Root")]
        [Tooltip("Optional. Root object for the entire boss HP presentation.")]
        [SerializeField] private GameObject panelRoot;

        [Header("Skinnable Elements")]
        [Tooltip("Optional decorative background image.")]
        [SerializeField] private Image backgroundImage;
        [Tooltip("Optional fill image for boss HP skins that use Image.fillAmount.")]
        [SerializeField] private Image fillImage;
        [Tooltip("Optional fill rect for skins that scale a rectangular bar instead of using Image.fillAmount.")]
        [SerializeField] private RectTransform fillRect;
        [Tooltip("Optional slider for boss HP skins that prefer Slider-driven bars.")]
        [SerializeField] private Slider fillSlider;
        [Tooltip("Optional boss name label.")]
        [SerializeField] private Text bossNameText;

        private bool _warnedMissingPresentationRef;

        public void ConfigureDebugView(Text nameText, Image fill, Image background = null)
        {
            bossNameText = nameText;
            fillImage = fill;
            backgroundImage = background;
            panelRoot = nameText != null && nameText.transform.parent != null
                ? nameText.transform.parent.gameObject
                : panelRoot;
        }

        public void ShowBoss(string bossName, float normalizedHealth)
        {
            if (panelRoot != null)
            {
                panelRoot.SetActive(true);
            }

            if (bossNameText == null && fillImage == null && fillSlider == null && !_warnedMissingPresentationRef)
            {
                Debug.LogWarning("BossHpPanelView has no name or fill references assigned. The boss HUD can still reserve layout space, but it will not show meaningful data until references are connected.", this);
                _warnedMissingPresentationRef = true;
            }

            if (bossNameText != null)
            {
                bossNameText.text = bossName;
            }

            float clamped = Mathf.Clamp01(normalizedHealth);

            if (fillSlider != null)
            {
                fillSlider.value = clamped;
            }

            if (fillImage != null)
            {
                fillImage.fillAmount = clamped;
            }

            if (fillRect != null)
            {
                Vector3 scale = fillRect.localScale;
                scale.x = clamped;
                fillRect.localScale = scale;
            }
        }

        public void HideBoss()
        {
            if (panelRoot != null)
            {
                panelRoot.SetActive(false);
            }
        }
    }
}
