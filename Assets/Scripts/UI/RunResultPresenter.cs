using CuteIssac.Core.Run;
using CuteIssac.Dungeon;
using CuteIssac.Player;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CuteIssac.UI
{
    /// <summary>
    /// Binds RunManager lifecycle events to a skinnable result screen.
    /// The presenter owns fallback UI creation so scenes can stay lightweight until a final prefab is authored.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RunResultPresenter : MonoBehaviour
    {
        private const string ResultModalScopeId = "RunResult";
        private const string RuntimePanelName = "RuntimeRunResultPanel";

        [Header("References")]
        [SerializeField] private RunManager runManager;
        [SerializeField] private PlayerInventory playerInventory;
        [SerializeField] private PlayerActiveItemController playerActiveItemController;
        [SerializeField] private PlayerConsumableHolder playerConsumableHolder;
        [SerializeField] private Canvas overlayCanvas;
        [SerializeField] private RunResultPanelView runResultPanelView;

        [Header("Behavior")]
        [SerializeField] private bool pauseGameplayWhenResultIsVisible = true;
        [SerializeField] [Min(10)] private int resultCanvasSortingOrder = 240;
        [SerializeField] private string resultActionHint = "\uACB0\uACFC\uB97C \uD655\uC778\uD55C \uB4A4 \uC900\uBE44\uB418\uBA74 \uB2E4\uC2DC \uC2DC\uC791\uD558\uC138\uC694.";

        private bool _appliedTimeScalePause;

        private void Awake()
        {
            ResolveReferences();
            EnsureView();
            EnsureEventSystem();
            SetResultModalActive(false);
            runResultPanelView?.Hide();
        }

        private void OnEnable()
        {
            ResolveReferences();
            EnsureView();
            EnsureEventSystem();

            if (runManager != null)
            {
                runManager.RunStarted -= HandleRunStarted;
                runManager.RunStarted += HandleRunStarted;
                runManager.RunEnded -= HandleRunEnded;
                runManager.RunEnded += HandleRunEnded;
            }
        }

        private void OnDisable()
        {
            if (runManager != null)
            {
                runManager.RunStarted -= HandleRunStarted;
                runManager.RunEnded -= HandleRunEnded;
            }

            SetResultModalActive(false);
            RestoreTimeScale();
        }

        private void OnValidate()
        {
            ResolveReferences();
        }

        private void HandleRunStarted(RunContext _)
        {
            UiModalState.ResetAll();
            WorldTextModalSuppressor.SetSuppressed(false);
            SetResultModalActive(false);
            RestoreTimeScale();
            runResultPanelView?.Hide();
        }

        private void HandleRunEnded(RunContext context, RunEndReason endReason)
        {
            EnsureView();

            if (runResultPanelView == null)
            {
                return;
            }

            SetResultModalActive(true);
            runResultPanelView.ShowSummary(BuildSummary(context, endReason), resultActionHint);
            runResultPanelView.BindRestartAction(RestartRun);
            FocusRestartButton();

            if (pauseGameplayWhenResultIsVisible)
            {
                Time.timeScale = 0f;
                _appliedTimeScalePause = true;
            }
        }

        private RunResultSummary BuildSummary(RunContext context, RunEndReason endReason)
        {
            int passiveItemCount = playerInventory != null ? playerInventory.PassiveItems.Count : 0;
            int activeItemCount = playerActiveItemController != null && playerActiveItemController.HasEquippedItem ? 1 : 0;
            int consumableItemCount = playerConsumableHolder != null && playerConsumableHolder.HeldConsumable != null ? 1 : 0;
            int totalItemCount = passiveItemCount + activeItemCount + consumableItemCount;

            string title = endReason switch
            {
                RunEndReason.Victory => "\uC2B9\uB9AC",
                RunEndReason.Defeat => "\uD328\uBC30",
                RunEndReason.Abandoned => "\uB7F0 \uC885\uB8CC",
                _ => "\uB7F0 \uACB0\uACFC"
            };

            string subtitle = endReason switch
            {
                RunEndReason.Victory => "\uBCF4\uC2A4\uB97C \uCC98\uCE58\uD588\uC2B5\uB2C8\uB2E4.",
                RunEndReason.Defeat => "\uC804\uD22C \uC911 \uD638\uD761\uC774 \uB04A\uACBC\uC2B5\uB2C8\uB2E4.",
                RunEndReason.Abandoned => "\uB7F0\uC744 \uC911\uB2E8\uD588\uC2B5\uB2C8\uB2E4.",
                _ => "\uACB0\uACFC \uC694\uC57D\uC744 \uBD88\uB7EC\uC624\uC9C0 \uBABB\uD588\uC2B5\uB2C8\uB2E4."
            };

            return new RunResultSummary(
                title,
                subtitle,
                totalItemCount,
                context != null ? context.TotalClearedRoomCount : 0,
                context != null ? context.TotalResolvedRoomCount : 0,
                playerInventory != null ? playerInventory.Coins : 0,
                playerInventory != null ? playerInventory.Keys : 0,
                playerInventory != null ? playerInventory.Bombs : 0,
                context != null ? context.CurrentFloorIndex : 1,
                endReason);
        }

        private void ResolveReferences()
        {
            if (runManager == null)
            {
                runManager = GetComponent<RunManager>();
            }

            if (playerInventory == null)
            {
                playerInventory = FindFirstObjectByType<PlayerInventory>(FindObjectsInactive.Exclude);
            }

            if (playerActiveItemController == null)
            {
                playerActiveItemController = FindFirstObjectByType<PlayerActiveItemController>(FindObjectsInactive.Exclude);
            }

            if (playerConsumableHolder == null)
            {
                playerConsumableHolder = FindFirstObjectByType<PlayerConsumableHolder>(FindObjectsInactive.Exclude);
            }

            if (overlayCanvas == null)
            {
                overlayCanvas = runResultPanelView != null
                    ? runResultPanelView.GetComponentInParent<Canvas>(true)
                    : null;
            }

            if (runResultPanelView == null && overlayCanvas != null)
            {
                RunResultPanelView[] candidates = overlayCanvas.GetComponentsInChildren<RunResultPanelView>(true);
                for (int index = 0; index < candidates.Length; index++)
                {
                    if (candidates[index] != null && candidates[index].gameObject.name == RuntimePanelName)
                    {
                        runResultPanelView = candidates[index];
                        break;
                    }
                }
            }
        }

        private void EnsureView()
        {
            if (overlayCanvas == null)
            {
                overlayCanvas = CreateFallbackCanvas();
            }

            if (overlayCanvas == null)
            {
                return;
            }

            NormalizeOverlayCanvas(overlayCanvas);

            RunResultPanelView runtimeView = FindRuntimePanelView();

            if (runtimeView != null)
            {
                runResultPanelView = runtimeView;
            }
            else
            {
                DestroyLegacyRuntimePanels();

                GameObject panelObject = new(RuntimePanelName);
                RectTransform panelRect = panelObject.AddComponent<RectTransform>();
                panelRect.SetParent(overlayCanvas.transform, false);
                panelRect.anchorMin = Vector2.zero;
                panelRect.anchorMax = Vector2.one;
                panelRect.offsetMin = Vector2.zero;
                panelRect.offsetMax = Vector2.zero;
                panelObject.AddComponent<CanvasRenderer>();
                runResultPanelView = panelObject.AddComponent<RunResultPanelView>();
            }

            Font fallbackFont = ResolveFallbackFont();
            runResultPanelView.EnsureRuntimeBaseline(overlayCanvas.transform as RectTransform, fallbackFont);
            runResultPanelView.Hide();
        }

        private RunResultPanelView FindRuntimePanelView()
        {
            if (overlayCanvas == null)
            {
                return null;
            }

            RunResultPanelView[] candidates = overlayCanvas.GetComponentsInChildren<RunResultPanelView>(true);
            for (int index = 0; index < candidates.Length; index++)
            {
                RunResultPanelView candidate = candidates[index];
                if (candidate != null && candidate.gameObject.name == RuntimePanelName)
                {
                    return candidate;
                }
            }

            return null;
        }

        private void DestroyLegacyRuntimePanels()
        {
            if (overlayCanvas == null)
            {
                return;
            }

            RunResultPanelView[] candidates = overlayCanvas.GetComponentsInChildren<RunResultPanelView>(true);
            for (int index = 0; index < candidates.Length; index++)
            {
                RunResultPanelView candidate = candidates[index];
                if (candidate == null || candidate.gameObject.name == RuntimePanelName)
                {
                    continue;
                }

                if (Application.isPlaying)
                {
                    Destroy(candidate.gameObject);
                }
                else
                {
                    DestroyImmediate(candidate.gameObject);
                }
            }
        }

        private void RestoreTimeScale()
        {
            if (!_appliedTimeScalePause)
            {
                return;
            }

            Time.timeScale = 1f;
            _appliedTimeScalePause = false;
        }

        private static Text CreateLabeledValue(Transform parent, Font font, string label, Vector2 anchoredPosition)
        {
            Image statCard = CreateBlock(parent, $"{label}_Card", anchoredPosition, new Vector2(220f, 124f), new Color(1f, 1f, 1f, 0.08f));

            Text labelText = CreateText(statCard.transform, $"{label}_Label", font, 20, TextAnchor.MiddleCenter, new Vector2(0f, 34f), new Vector2(184f, 28f), FontStyle.Bold);
            labelText.color = new Color(0.42f, 0.56f, 0.7f, 0.92f);
            labelText.text = label;

            Text valueText = CreateText(statCard.transform, $"{label}_Value", font, 40, TextAnchor.MiddleCenter, new Vector2(0f, -12f), new Vector2(184f, 58f), FontStyle.Bold);
            valueText.color = new Color(0.18f, 0.14f, 0.1f, 1f);
            return valueText;
        }

        private static Image CreateBlock(Transform parent, string name, Vector2 anchoredPosition, Vector2 size, Color color)
        {
            GameObject blockObject = new(name);
            RectTransform rectTransform = blockObject.AddComponent<RectTransform>();
            rectTransform.SetParent(parent, false);
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.sizeDelta = size;

            Image image = blockObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static Text CreateText(Transform parent, string name, Font font, int fontSize, TextAnchor alignment, Vector2 anchoredPosition, Vector2 size, FontStyle fontStyle)
        {
            GameObject textObject = new(name);
            RectTransform rectTransform = textObject.AddComponent<RectTransform>();
            rectTransform.SetParent(parent, false);
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.sizeDelta = size;

            Text text = textObject.AddComponent<Text>();
            text.font = font;
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.alignment = alignment;
            text.color = Color.white;
            text.raycastTarget = false;
            return text;
        }

        private static Button CreateButton(Transform parent, Font font, string name, string label, Vector2 anchoredPosition, Vector2 size, out Text labelText)
        {
            GameObject buttonObject = new(name);
            RectTransform rectTransform = buttonObject.AddComponent<RectTransform>();
            rectTransform.SetParent(parent, false);
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.sizeDelta = size;

            Image image = buttonObject.AddComponent<Image>();
            image.color = new Color(0.22f, 0.18f, 0.14f, 0.92f);

            Button button = buttonObject.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = image.color;
            colors.highlightedColor = new Color(0.32f, 0.26f, 0.2f, 0.96f);
            colors.pressedColor = new Color(0.16f, 0.12f, 0.09f, 0.96f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(0.2f, 0.2f, 0.2f, 0.75f);
            button.colors = colors;

            labelText = CreateText(rectTransform, "Label", font, 28, TextAnchor.MiddleCenter, Vector2.zero, size, FontStyle.Bold);
            labelText.supportRichText = true;
            labelText.text = label;
            labelText.color = new Color(0.94f, 0.9f, 0.82f, 1f);
            return button;
        }

        private Font ResolveFallbackFont()
        {
            Text existingText = FindFirstObjectByType<Text>(FindObjectsInactive.Include);

            if (existingText != null && existingText.font != null)
            {
                return existingText.font;
            }

            return LocalizedUiFontProvider.GetFont();
        }

        private static Canvas CreateFallbackCanvas()
        {
            GameObject canvasObject = new("RunResultCanvas");
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            canvasObject.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        private void NormalizeOverlayCanvas(Canvas canvas)
        {
            if (canvas == null)
            {
                return;
            }

            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = resultCanvasSortingOrder;

            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();

            if (scaler == null)
            {
                scaler = canvas.gameObject.AddComponent<CanvasScaler>();
            }

            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            if (canvas.GetComponent<GraphicRaycaster>() == null)
            {
                canvas.gameObject.AddComponent<GraphicRaycaster>();
            }
        }

        private void SetResultModalActive(bool active)
        {
            UiModalState.SetScopeActive(ResultModalScopeId, active);
            WorldTextModalSuppressor.SetSuppressed(UiModalState.IsGameplayModalActive);
        }

        private void EnsureEventSystem()
        {
            InputSystemEventSystemBootstrap.EnsureReady();
        }

        private void FocusRestartButton()
        {
            if (runResultPanelView?.RestartButton == null)
            {
                return;
            }

            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(runResultPanelView.RestartButton.gameObject);
            }

            runResultPanelView.RefreshSelectionState(true);
        }

        private void RestartRun()
        {
            UiModalState.ResetAll();
            WorldTextModalSuppressor.SetSuppressed(false);
            SetResultModalActive(false);
            RestoreTimeScale();
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }
}
