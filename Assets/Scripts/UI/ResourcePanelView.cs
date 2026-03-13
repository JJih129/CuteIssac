using CuteIssac.Player;
using UnityEngine;
using UnityEngine.UI;

namespace CuteIssac.UI
{
    /// <summary>
    /// Presentation-only panel for the three core Isaac-like resources.
    /// Each icon, value label, and background can be replaced independently in the inspector.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ResourcePanelView : MonoBehaviour
    {
        [Header("Optional Root")]
        [Tooltip("Optional. Root object to hide or swap when replacing the whole resource panel prefab.")]
        [SerializeField] private GameObject panelRoot;

        [Header("Coin")]
        [Tooltip("Optional background image for the coin slot.")]
        [SerializeField] private Image coinBackgroundImage;
        [Tooltip("Optional icon image for the coin slot.")]
        [SerializeField] private Image coinIconImage;
        [Tooltip("Numeric label for the player's coin count.")]
        [SerializeField] private Text coinValueText;

        [Header("Key")]
        [Tooltip("Optional background image for the key slot.")]
        [SerializeField] private Image keyBackgroundImage;
        [Tooltip("Optional icon image for the key slot.")]
        [SerializeField] private Image keyIconImage;
        [Tooltip("Numeric label for the player's key count.")]
        [SerializeField] private Text keyValueText;

        [Header("Bomb")]
        [Tooltip("Optional background image for the bomb slot.")]
        [SerializeField] private Image bombBackgroundImage;
        [Tooltip("Optional icon image for the bomb slot.")]
        [SerializeField] private Image bombIconImage;
        [Tooltip("Numeric label for the player's bomb count.")]
        [SerializeField] private Text bombValueText;

        private bool _warnedMissingValueText;

        public void ConfigureDebugView(
            Text coinText,
            Text keyText,
            Text bombText,
            Image coinBackground = null,
            Image keyBackground = null,
            Image bombBackground = null,
            Image coinIcon = null,
            Image keyIcon = null,
            Image bombIcon = null)
        {
            coinValueText = coinText;
            keyValueText = keyText;
            bombValueText = bombText;
            coinBackgroundImage = coinBackground;
            keyBackgroundImage = keyBackground;
            bombBackgroundImage = bombBackground;
            coinIconImage = coinIcon;
            keyIconImage = keyIcon;
            bombIconImage = bombIcon;
        }

        public void SetResources(PlayerResourceSnapshot resources)
        {
            if (panelRoot != null && !panelRoot.activeSelf)
            {
                panelRoot.SetActive(true);
            }

            if ((coinValueText == null || keyValueText == null || bombValueText == null) && !_warnedMissingValueText)
            {
                Debug.LogWarning("ResourcePanelView is missing one or more value text references. Missing fields will simply not update until they are assigned.", this);
                _warnedMissingValueText = true;
            }

            if (coinValueText != null)
            {
                coinValueText.text = resources.Coins.ToString();
            }

            if (keyValueText != null)
            {
                keyValueText.text = resources.Keys.ToString();
            }

            if (bombValueText != null)
            {
                bombValueText.text = resources.Bombs.ToString();
            }
        }
    }
}
