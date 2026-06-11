using CuteIssac.Common.Input;
using CuteIssac.Core.Feedback;
using CuteIssac.Core.Gameplay;
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
        [SerializeField] private bool showShopPanel;
        [SerializeField] [Min(0f)] private float triggerBoundsPadding = 0.55f;
        [SerializeField] [Min(0.05f)] private float triggerBoundsRefreshInterval = 0.5f;

        [Header("Behavior")]
        [SerializeField] [Min(0.25f)] private float purchaseDistance = 2.2f;
        [SerializeField] private bool autoPurchaseOnContact = true;
        [SerializeField] [Min(0.05f)] private float autoPurchaseDistance = 0.72f;
        [SerializeField] [Min(0f)] private float autoPurchaseContactPadding = 0.18f;
        [SerializeField] [Min(0.05f)] private float autoPurchaseCooldown = 0.35f;
        [SerializeField] [Min(0.05f)] private float failedAutoPurchaseCooldown = 0.85f;
        [SerializeField] [Min(0f)] private float autoPurchaseActivationDelay = 0.18f;
        [SerializeField] private Color purchaseSuccessColor = new(0.48f, 1f, 0.72f, 1f);
        [SerializeField] private Color purchaseFailureColor = new(1f, 0.62f, 0.48f, 1f);

        private IPlayerInputReader _inputReader;
        private PlayerInventory _currentPlayerInventory;
        private PlayerItemManager _currentPlayerItemManager;
        private PlayerHealth _currentPlayerHealth;
        private Transform _currentPlayerTransform;
        private float _nextAutoPurchaseTime;
        private float _nextTriggerBoundsRefreshTime;

        private void Awake()
        {
            ResolveReferences();
            FitInteractionTriggerToShopItems();
        }

        private void Update()
        {
            if (Time.unscaledTime >= _nextTriggerBoundsRefreshTime)
            {
                FitInteractionTriggerToShopItems();
                _nextTriggerBoundsRefreshTime = Time.unscaledTime + triggerBoundsRefreshInterval;
            }

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

            if (highlightedItem == null)
            {
                return;
            }

            if (TryAutoPurchaseOnContact(highlightedItem))
            {
                return;
            }

            if (_inputReader != null && _inputReader.ReadState().ActiveItemPressed)
            {
                TryPurchaseHighlighted(highlightedItem);
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

        private void FitInteractionTriggerToShopItems()
        {
            if (shopInventory == null || interactionTrigger is not BoxCollider2D boxCollider)
            {
                return;
            }

            if (!shopInventory.TryGetShopItemVisualBounds(out Bounds worldBounds))
            {
                return;
            }

            if (!TryConvertWorldBoundsToLocal(worldBounds, transform, out Bounds localBounds))
            {
                return;
            }

            float padding = Mathf.Max(0f, triggerBoundsPadding);
            boxCollider.offset = new Vector2(localBounds.center.x, localBounds.center.y);
            boxCollider.size = new Vector2(
                Mathf.Max(0.05f, localBounds.size.x + (padding * 2f)),
                Mathf.Max(0.05f, localBounds.size.y + (padding * 2f)));
            boxCollider.isTrigger = true;
        }

        private static bool TryConvertWorldBoundsToLocal(Bounds worldBounds, Transform localRoot, out Bounds localBounds)
        {
            localBounds = default;

            if (localRoot == null)
            {
                return false;
            }

            Vector3 min = worldBounds.min;
            Vector3 max = worldBounds.max;
            Vector3 center = worldBounds.center;
            Vector3 localA = localRoot.InverseTransformPoint(new Vector3(min.x, min.y, center.z));
            Vector3 localB = localRoot.InverseTransformPoint(new Vector3(min.x, max.y, center.z));
            Vector3 localC = localRoot.InverseTransformPoint(new Vector3(max.x, min.y, center.z));
            Vector3 localD = localRoot.InverseTransformPoint(new Vector3(max.x, max.y, center.z));

            localBounds = new Bounds(localA, Vector3.zero);
            localBounds.Encapsulate(localB);
            localBounds.Encapsulate(localC);
            localBounds.Encapsulate(localD);
            return true;
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
            bool resolvedActiveCollector = TryResolveActiveCollector(
                other,
                out PlayerInventory playerInventory,
                out PlayerItemManager playerItemManager,
                out PlayerHealth playerHealth,
                out Transform playerTransform);

            if (!resolvedActiveCollector || playerInventory == null || playerItemManager == null)
            {
                playerInventory = other.GetComponentInParent<PlayerInventory>();
                playerItemManager = other.GetComponentInParent<PlayerItemManager>();
                playerHealth = other.GetComponentInParent<PlayerHealth>();
                playerTransform = playerInventory != null ? playerInventory.transform : null;
            }

            if (playerInventory == null || playerItemManager == null)
            {
                return;
            }

            if (_currentPlayerTransform != playerTransform)
            {
                _nextAutoPurchaseTime = Time.unscaledTime + autoPurchaseActivationDelay;
            }

            _currentPlayerInventory = playerInventory;
            _currentPlayerItemManager = playerItemManager;
            _currentPlayerHealth = playerHealth;
            _currentPlayerTransform = playerTransform;
        }

        private static bool TryResolveActiveCollector(
            Collider2D other,
            out PlayerInventory playerInventory,
            out PlayerItemManager playerItemManager,
            out PlayerHealth playerHealth,
            out Transform playerTransform)
        {
            playerInventory = null;
            playerItemManager = null;
            playerHealth = null;
            playerTransform = null;

            PlayerController activeController = PlayerRegistry.ActiveController;
            if (activeController == null || !PlayerRegistry.IsComponentUnderTransform(other, activeController.transform))
            {
                return false;
            }

            playerTransform = activeController.transform;
            playerInventory = PlayerRegistry.IsComponentUnderTransform(PlayerRegistry.ActiveInventory, playerTransform)
                ? PlayerRegistry.ActiveInventory
                : activeController.GetComponent<PlayerInventory>();
            playerItemManager = PlayerRegistry.IsComponentUnderTransform(PlayerRegistry.ActiveItemManager, playerTransform)
                ? PlayerRegistry.ActiveItemManager
                : activeController.GetComponent<PlayerItemManager>();
            playerHealth = PlayerRegistry.IsComponentUnderTransform(PlayerRegistry.ActiveHealth, playerTransform)
                ? PlayerRegistry.ActiveHealth
                : activeController.GetComponent<PlayerHealth>();

            return playerInventory != null || playerItemManager != null || playerHealth != null;
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
            if (TryBuildReadablePrompt(slotState, out string readablePrompt))
            {
                SetPromptText(readablePrompt);
                return;
            }
            SetPromptText(slotState.CanPurchase
                ? $"구매 {slotState.DisplayName} · {slotState.PriceLabel}"
                : $"{slotState.StatusLabel} · {slotState.PriceLabel}");
        }

        private void PresentPanel()
        {
            bool shouldShowPanel = shopPanelView != null
                && showShopPanel
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
                if (promptTextMesh != null)
                {
                    promptTextMesh.anchor = TextAnchor.MiddleCenter;
                    promptTextMesh.alignment = TextAlignment.Center;
                    promptTextMesh.richText = false;
                    promptTextMesh.fontSize = 44;
                    promptTextMesh.characterSize = 0.08f;
                }

                return;
            }

            GameObject textObject = new("PromptLabel");
            textObject.transform.SetParent(promptRoot.transform, false);
            textObject.transform.localPosition = new Vector3(0f, 1.34f, 0f);
            promptTextMesh = textObject.AddComponent<TextMesh>();
            promptTextMesh.anchor = TextAnchor.MiddleCenter;
            promptTextMesh.alignment = TextAlignment.Center;
            promptTextMesh.richText = false;
            promptTextMesh.fontSize = 44;
            promptTextMesh.characterSize = 0.08f;
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

        private bool TryAutoPurchaseOnContact(ShopItem highlightedItem)
        {
            if (!autoPurchaseOnContact || highlightedItem == null || _currentPlayerTransform == null)
            {
                return false;
            }

            float now = Time.unscaledTime;
            if (now < _nextAutoPurchaseTime)
            {
                return false;
            }

            if (!highlightedItem.IsBuyerWithinInteractionBounds(
                    _currentPlayerTransform.position,
                    autoPurchaseDistance,
                    autoPurchaseContactPadding,
                    out _))
            {
                return false;
            }

            bool canPurchase = highlightedItem.CanPurchase(_currentPlayerInventory, _currentPlayerItemManager, _currentPlayerHealth);
            _nextAutoPurchaseTime = now + (canPurchase ? autoPurchaseCooldown : failedAutoPurchaseCooldown);
            TryPurchaseHighlighted(highlightedItem);
            return true;
        }

        private void TryPurchaseHighlighted(ShopItem highlightedItem)
        {
            ShopSlotState highlightedSlotState = highlightedItem.BuildSlotState(true, _currentPlayerInventory, _currentPlayerItemManager, _currentPlayerHealth);
            bool purchased = shopInventory.TryPurchaseHighlighted(_currentPlayerInventory, _currentPlayerItemManager, _currentPlayerHealth);

            if (purchased)
            {
                highlightedItem.PlayPurchaseSuccessFeedback();
            }
            else
            {
                highlightedItem.PlayPurchaseFailureFeedback();
            }

            PresentPurchaseFeedback(highlightedSlotState, purchased);
            PresentPanel();
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
                    $"{slotState.DisplayName} · {slotState.PriceLabel}",
                    purchaseSuccessColor,
                    1.2f));
                return;
            }

            GameplayFeedbackEvents.RaiseBannerFeedback(new BannerFeedbackRequest(
                "구매 실패",
                $"{slotState.StatusLabel} · {slotState.DisplayName}",
                purchaseFailureColor,
                1f));

            if (IsResourceShortage(slotState.StatusLabel))
            {
                GameplayRuntimeEvents.RaiseShopPurchaseFailed(new ShopPurchaseFailedSignal(
                    slotState.CurrencyType,
                    slotState.StatusLabel,
                    transform.position));
            }
        }

        private static bool IsResourceShortage(string statusLabel)
        {
            return string.Equals(statusLabel, "코인 부족")
                || string.Equals(statusLabel, "열쇠 부족")
                || string.Equals(statusLabel, "폭탄 부족");
        }

        private static bool TryBuildReadablePrompt(ShopSlotState slotState, out string prompt)
        {
            prompt = slotState.CanPurchase
                ? $"구매 {slotState.DisplayName} · {slotState.PriceLabel}"
                : $"{slotState.StatusLabel} · {slotState.PriceLabel}";
            return slotState.IsVisible;
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
