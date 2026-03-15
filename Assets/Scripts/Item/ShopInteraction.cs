using CuteIssac.Common.Input;
using CuteIssac.Core.Feedback;
using CuteIssac.Player;
using CuteIssac.UI;
using UnityEngine;
using UnityEngine.UI;

namespace CuteIssac.Item
{
    /// <summary>
    /// Handles player proximity and purchase input for one shop room placeholder.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ShopInteraction : MonoBehaviour
    {
        private const string ShopModalScopeId = "ShopPanel";

        [Header("References")]
        [SerializeField] private ShopInventory shopInventory;
        [SerializeField] private Collider2D interactionTrigger;
        [SerializeField] private MonoBehaviour inputReaderSource;
        [SerializeField] private GameObject promptRoot;
        [SerializeField] private ShopPanelView shopPanelView;
        [SerializeField] private Text promptText;
        [SerializeField] private TextMesh promptTextMesh;
        [SerializeField] private bool showWorldPromptText;

        [Header("Behavior")]
        [SerializeField] [Min(0.25f)] private float purchaseDistance = 2.2f;
        [SerializeField] private Color purchaseSuccessColor = new(0.48f, 1f, 0.72f, 1f);
        [SerializeField] private Color purchaseFailureColor = new(1f, 0.62f, 0.48f, 1f);

        private IPlayerInputReader _inputReader;
        private PlayerInventory _currentPlayerInventory;
        private PlayerItemManager _currentPlayerItemManager;
        private PlayerHealth _currentPlayerHealth;
        private Transform _currentPlayerTransform;

        private void Awake()
        {
            ResolveReferences();
        }

        private void Update()
        {
            if (_currentPlayerTransform == null || shopInventory == null)
            {
                SetPromptVisible(false);
                shopPanelView?.Hide();
                UiModalState.SetScopeActive(ShopModalScopeId, false);
                return;
            }

            if (_inputReader == null)
            {
                ResolveInputReader();
            }

            ShopItem highlightedItem = shopInventory.GetClosestAvailableItem(
                _currentPlayerTransform.position,
                purchaseDistance,
                _currentPlayerInventory,
                _currentPlayerItemManager,
                _currentPlayerHealth);

            shopInventory.SetHighlightedItem(highlightedItem, _currentPlayerInventory, _currentPlayerItemManager, _currentPlayerHealth);
            UpdatePrompt(highlightedItem);
            SetPromptVisible(highlightedItem != null);
            PresentPanel();

            if (highlightedItem == null || _inputReader == null)
            {
                return;
            }

            if (_inputReader.ReadState().ActiveItemPressed)
            {
                ShopSlotState highlightedSlotState = highlightedItem.BuildSlotState(true, _currentPlayerInventory, _currentPlayerItemManager, _currentPlayerHealth);
                bool purchased = shopInventory.TryPurchaseHighlighted(_currentPlayerInventory, _currentPlayerItemManager, _currentPlayerHealth);

                if (purchased)
                {
                    highlightedItem.PlayPurchaseSuccessFeedback();
                }

                PresentPurchaseFeedback(highlightedSlotState, purchased);
                PresentPanel();
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            TryBindCollector(other);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            TryBindCollector(other);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (_currentPlayerTransform == null || other.transform.root != _currentPlayerTransform)
            {
                return;
            }

            ClearCollector();
        }

        private void OnDisable()
        {
            UiModalState.SetScopeActive(ShopModalScopeId, false);
            shopPanelView?.Hide();
        }

        private void ResolveReferences()
        {
            if (shopInventory == null)
            {
                shopInventory = GetComponent<ShopInventory>();
            }

            if (interactionTrigger == null)
            {
                interactionTrigger = GetComponent<Collider2D>();
            }

            ResolveInputReader();
            ResolveShopPanelView();
        }

        private void ResolveInputReader()
        {
            if (inputReaderSource == null)
            {
                inputReaderSource = FindFirstObjectByType<CuteIssac.Core.Input.InputSystemPlayerInputReader>(FindObjectsInactive.Exclude);
            }

            _inputReader = inputReaderSource as IPlayerInputReader;
        }

        private void TryBindCollector(Collider2D other)
        {
            PlayerInventory playerInventory = other.GetComponentInParent<PlayerInventory>();
            PlayerItemManager playerItemManager = other.GetComponentInParent<PlayerItemManager>();
            PlayerHealth playerHealth = other.GetComponentInParent<PlayerHealth>();

            if (playerInventory == null || playerItemManager == null)
            {
                return;
            }

            _currentPlayerInventory = playerInventory;
            _currentPlayerItemManager = playerItemManager;
            _currentPlayerHealth = playerHealth;
            _currentPlayerTransform = playerInventory.transform;
        }

        private void ClearCollector()
        {
            _currentPlayerInventory = null;
            _currentPlayerItemManager = null;
            _currentPlayerHealth = null;
            _currentPlayerTransform = null;
            shopInventory?.SetHighlightedItem(null, null, null, null);
            UpdatePrompt(null);
            SetPromptVisible(false);
            shopPanelView?.Hide();
            UiModalState.SetScopeActive(ShopModalScopeId, false);
        }

        private void SetPromptVisible(bool visible)
        {
            if (!showWorldPromptText)
            {
                visible = false;
            }

            if (promptRoot != null)
            {
                promptRoot.SetActive(visible);
            }
        }

        private void UpdatePrompt(ShopItem highlightedItem)
        {
            ResolvePromptTextReferences();

            if (highlightedItem == null)
            {
                SetPromptText(string.Empty);
                return;
            }

            ShopSlotState slotState = highlightedItem.BuildSlotState(true, _currentPlayerInventory, _currentPlayerItemManager, _currentPlayerHealth);
            SetPromptText(slotState.CanPurchase
                ? $"구매 {slotState.DisplayName} · {slotState.PriceLabel}"
                : $"{slotState.StatusLabel} · {slotState.PriceLabel}");
        }

        private void PresentPanel()
        {
            bool shouldShowPanel = shopPanelView != null
                && _currentPlayerInventory != null
                && shopInventory != null;

            UiModalState.SetScopeActive(ShopModalScopeId, shouldShowPanel);

            if (!shouldShowPanel)
            {
                shopPanelView?.Hide();
                return;
            }

            shopPanelView.Present(
                shopInventory.BuildSlotStates(_currentPlayerInventory, _currentPlayerItemManager, _currentPlayerHealth),
                _currentPlayerInventory.Resources);
        }

        private void ResolveShopPanelView()
        {
            if (shopPanelView != null)
            {
                return;
            }

            shopPanelView = FindFirstObjectByType<ShopPanelView>(FindObjectsInactive.Include);

            if (shopPanelView != null)
            {
                return;
            }

            Canvas canvas = FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);

            if (canvas != null)
            {
                shopPanelView = ShopPanelView.CreateRuntime(canvas);
            }
        }

        private void ResolvePromptTextReferences()
        {
            if (!showWorldPromptText)
            {
                if (promptText != null)
                {
                    promptText.text = string.Empty;
                    promptText.gameObject.SetActive(false);
                }

                if (promptTextMesh != null)
                {
                    promptTextMesh.text = string.Empty;
                    promptTextMesh.gameObject.SetActive(false);
                }

                return;
            }

            if (promptRoot == null)
            {
                return;
            }

            if (promptText == null)
            {
                promptText = promptRoot.GetComponentInChildren<Text>(true);
            }

            if (promptTextMesh == null)
            {
                promptTextMesh = promptRoot.GetComponentInChildren<TextMesh>(true);
            }

            if (promptText != null || promptTextMesh != null)
            {
                LocalizedUiFontProvider.Apply(promptText);
                LocalizedUiFontProvider.Apply(promptTextMesh);
                return;
            }

            GameObject textObject = new("PromptLabel");
            textObject.transform.SetParent(promptRoot.transform, false);
            textObject.transform.localPosition = new Vector3(0f, 1.34f, 0f);
            promptTextMesh = textObject.AddComponent<TextMesh>();
            promptTextMesh.anchor = TextAnchor.MiddleCenter;
            promptTextMesh.alignment = TextAlignment.Center;
            promptTextMesh.fontSize = 92;
            promptTextMesh.characterSize = 0.24f;
            promptTextMesh.color = new Color(1f, 0.97f, 0.76f, 1f);
            LocalizedUiFontProvider.Apply(promptTextMesh);
        }

        private void SetPromptText(string value)
        {
            if (promptText != null)
            {
                promptText.text = value;
            }

            if (promptTextMesh != null)
            {
                promptTextMesh.text = value;
            }
        }

        private void PresentPurchaseFeedback(ShopSlotState slotState, bool purchased)
        {
            if (!slotState.IsVisible)
            {
                return;
            }

            if (purchased)
            {
                GameplayFeedbackEvents.RaiseBannerFeedback(new BannerFeedbackRequest(
                    "구매 완료",
                    $"{slotState.DisplayName}  {slotState.PriceLabel}",
                    purchaseSuccessColor,
                    1.2f));
                return;
            }

            GameplayFeedbackEvents.RaiseBannerFeedback(new BannerFeedbackRequest(
                "구매 불가",
                $"{slotState.StatusLabel}  {slotState.DisplayName}",
                purchaseFailureColor,
                1f));
        }

        private void Reset()
        {
            shopInventory = GetComponent<ShopInventory>();
            interactionTrigger = GetComponent<Collider2D>();
        }

        private void OnValidate()
        {
            if (shopInventory == null)
            {
                shopInventory = GetComponent<ShopInventory>();
            }

            if (interactionTrigger == null)
            {
                interactionTrigger = GetComponent<Collider2D>();
            }

            if (promptRoot == null)
            {
                return;
            }

            if (promptText == null)
            {
                promptText = promptRoot.GetComponentInChildren<Text>(true);
            }

            if (promptTextMesh == null)
            {
                promptTextMesh = promptRoot.GetComponentInChildren<TextMesh>(true);
            }
        }
    }
}
