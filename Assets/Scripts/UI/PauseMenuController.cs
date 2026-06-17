using CuteIssac.Core.Run;
using CuteIssac.Core.Scene;
using CuteIssac.Core.Settings;
using CuteIssac.Player;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace CuteIssac.UI
{
    /// <summary>
    /// Handles pause state, Escape-key toggling, and menu button actions.
    /// The actual visuals live in PauseMenuView.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PauseMenuController : MonoBehaviour
    {
        private static readonly Vector2Int[] ResolutionPresets =
        {
            new(1280, 720),
            new(1600, 900),
            new(1920, 1080),
            new(2560, 1440)
        };

        [Header("Optional References")]
        [Tooltip("씬에 배치된 GameplaySceneContext입니다. 비워두면 Active Context를 먼저 사용하고, 마지막에만 씬 검색으로 보정합니다.")]
        [SerializeField] private GameplaySceneContext sceneContext;
        [SerializeField] private PauseMenuView pauseMenuView;
        [SerializeField] private PlayerStats playerStats;
        [SerializeField] private RunManager runManager;
        [SerializeField] private GameOptionsService gameOptionsService;
        [SerializeField] private RectTransform safeAreaRoot;

        [Header("Behavior")]
        [SerializeField] private Key fallbackToggleKey = Key.Escape;
        [SerializeField] private bool pauseAudioListener = true;

        private bool _isPaused;
        private float _cachedTimeScale = 1f;
        private Button _settingsButton;
        private Button _resumeButton;
        private Button _quitButton;
        private Button _lastSelectedButton;
        private bool _isSettingsOpen;
        private PauseSettingsTarget _settingsTarget = PauseSettingsTarget.Master;

        private void Awake()
        {
            ResolveReferences();
            pauseMenuView?.Hide();
        }

        private void OnEnable()
        {
            ResolveReferences();
            BindButtons();
        }

        private void OnDisable()
        {
            UnbindButtons();

            if (_isPaused)
            {
                ApplyPause(false);
            }
        }

        private void Update()
        {
            if (!WasPausePressedThisFrame())
            {
                EnsureMenuSelection();
                RefreshMenuSelectionVisuals();
                return;
            }

            if (_isPaused && _isSettingsOpen)
            {
                ShowOverviewPanel();
                EnsureMenuSelection();
                RefreshMenuSelectionVisuals();
                return;
            }

            TogglePause();
            EnsureMenuSelection();
            RefreshMenuSelectionVisuals();
        }

        public void ConfigureRuntime(PauseMenuView runtimePauseMenuView, PlayerStats runtimePlayerStats, RunManager runtimeRunManager, RectTransform runtimeSafeAreaRoot)
        {
            pauseMenuView = runtimePauseMenuView;
            playerStats = runtimePlayerStats;
            runManager = runtimeRunManager;
            safeAreaRoot = runtimeSafeAreaRoot;
            ResolveReferences();
            BindButtons();
            pauseMenuView?.Hide();
        }

        public void TogglePause()
        {
            ApplyPause(!_isPaused);
        }

        private void ResolveReferences()
        {
            ResolveReferencesFromSceneContext();

            if (playerStats == null)
            {
                playerStats = FindFirstObjectByType<PlayerStats>(FindObjectsInactive.Exclude);
            }

            if (runManager == null)
            {
                runManager = FindFirstObjectByType<RunManager>(FindObjectsInactive.Exclude);
            }

            if (gameOptionsService == null)
            {
                gameOptionsService = FindFirstObjectByType<GameOptionsService>(FindObjectsInactive.Exclude);
            }

            if (safeAreaRoot == null)
            {
                if (transform is RectTransform rootRect && rootRect.childCount > 0 && rootRect.GetChild(0) is RectTransform childRect)
                {
                    safeAreaRoot = childRect;
                }
                else
                {
                    safeAreaRoot = transform as RectTransform;
                }
            }

            if (pauseMenuView == null)
            {
                pauseMenuView = CreateFallbackPauseMenuView();
            }

            pauseMenuView?.EnsureRuntimeBaseline(safeAreaRoot);
        }

        private void ResolveReferencesFromSceneContext()
        {
            if (sceneContext == null)
            {
                sceneContext = GameplaySceneContext.Active;
            }

            if (sceneContext == null)
            {
                return;
            }

            sceneContext.ResolveMissingReferences();
            playerStats ??= sceneContext.PlayerStats;
            runManager ??= sceneContext.RunManager;
            gameOptionsService ??= sceneContext.GameOptionsService;
        }

        private void BindButtons()
        {
            UnbindButtons();

            if (pauseMenuView == null)
            {
                return;
            }

            _settingsButton = pauseMenuView.SettingsButton;
            _resumeButton = pauseMenuView.ResumeButton;
            _quitButton = pauseMenuView.QuitButton;
            _lastSelectedButton = ResolveDefaultSelection();

            if (_resumeButton != null)
            {
                _resumeButton.onClick.AddListener(HandleResumeClicked);
            }

            if (_settingsButton != null)
            {
                _settingsButton.onClick.AddListener(HandleSettingsClicked);
            }

            if (_quitButton != null)
            {
                _quitButton.onClick.AddListener(HandleQuitClicked);
            }
        }

        private void UnbindButtons()
        {
            if (_resumeButton != null)
            {
                _resumeButton.onClick.RemoveListener(HandleResumeClicked);
            }

            if (_settingsButton != null)
            {
                _settingsButton.onClick.RemoveListener(HandleSettingsClicked);
            }

            if (_quitButton != null)
            {
                _quitButton.onClick.RemoveListener(HandleQuitClicked);
            }

            _settingsButton = null;
            _resumeButton = null;
            _quitButton = null;
            _lastSelectedButton = null;
        }

        private void HandleResumeClicked()
        {
            if (_isSettingsOpen)
            {
                CycleSettingsTarget();
                return;
            }

            ApplyPause(false);
        }

        private void HandleSettingsClicked()
        {
            if (_isSettingsOpen)
            {
                AdjustSelectedOption(-0.1f);
                return;
            }

            ShowSettingsPanel();
        }

        private void HandleQuitClicked()
        {
            if (_isSettingsOpen)
            {
                AdjustSelectedOption(0.1f);
                return;
            }

            ApplyPause(false);

            if (runManager != null)
            {
                runManager.AbortRun();
            }

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void ApplyPause(bool paused)
        {
            if (_isPaused == paused)
            {
                return;
            }

            _isPaused = paused;
            runManager?.SetPaused(paused);

            if (paused)
            {
                _isSettingsOpen = false;
                _settingsTarget = PauseSettingsTarget.Master;
                _cachedTimeScale = Time.timeScale > 0f ? Time.timeScale : 1f;
                Time.timeScale = 0f;

                if (pauseAudioListener)
                {
                    AudioListener.pause = true;
                }

                ShowOverviewPanel();

                if (_resumeButton != null && EventSystem.current != null)
                {
                    EventSystem.current.SetSelectedGameObject(_resumeButton.gameObject);
                }

                _lastSelectedButton = ResolveDefaultSelection();

                RefreshMenuSelectionVisuals();

                return;
            }

            _isSettingsOpen = false;
            _settingsTarget = PauseSettingsTarget.Master;
            Time.timeScale = Mathf.Max(0.01f, _cachedTimeScale);

            if (pauseAudioListener)
            {
                AudioListener.pause = false;
            }

            pauseMenuView?.Hide();
        }

        private void ShowOverviewPanel()
        {
            _isSettingsOpen = false;
            _settingsTarget = PauseSettingsTarget.Master;
            pauseMenuView?.Show(playerStats != null ? playerStats.CurrentStats : default);
            _lastSelectedButton = _resumeButton != null && _resumeButton.interactable
                ? _resumeButton
                : ResolveDefaultSelection();
        }

        private void ShowSettingsPanel()
        {
            _isSettingsOpen = true;
            pauseMenuView?.ShowSettings(ResolveCurrentOptions(), ResolveSettingsTargetLabel());
            _lastSelectedButton = _resumeButton != null && _resumeButton.interactable
                ? _resumeButton
                : ResolveDefaultSelection();

            if (_lastSelectedButton != null && EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(_lastSelectedButton.gameObject);
            }
        }

        private GameOptionsData ResolveCurrentOptions()
        {
            return gameOptionsService != null
                ? gameOptionsService.Export()
                : new GameOptionsData { MasterVolume = AudioListener.volume, Fullscreen = Screen.fullScreen };
        }

        private void CycleSettingsTarget()
        {
            _settingsTarget = _settingsTarget switch
            {
                PauseSettingsTarget.Master => PauseSettingsTarget.Music,
                PauseSettingsTarget.Music => PauseSettingsTarget.Sfx,
                PauseSettingsTarget.Sfx => PauseSettingsTarget.Resolution,
                PauseSettingsTarget.Resolution => PauseSettingsTarget.UiScale,
                PauseSettingsTarget.UiScale => PauseSettingsTarget.Fullscreen,
                PauseSettingsTarget.Fullscreen => PauseSettingsTarget.CameraShake,
                PauseSettingsTarget.CameraShake => PauseSettingsTarget.DamageNumbers,
                PauseSettingsTarget.DamageNumbers => PauseSettingsTarget.ReduceFlashes,
                PauseSettingsTarget.ReduceFlashes => PauseSettingsTarget.HighContrast,
                PauseSettingsTarget.HighContrast => PauseSettingsTarget.ColorAssist,
                _ => PauseSettingsTarget.Master
            };

            pauseMenuView?.ShowSettings(ResolveCurrentOptions(), ResolveSettingsTargetLabel());
            RefreshMenuSelectionVisuals();
        }

        private void AdjustSelectedOption(float delta)
        {
            GameOptionsData nextOptions = ResolveCurrentOptions();

            switch (_settingsTarget)
            {
                case PauseSettingsTarget.Music:
                    nextOptions.MusicVolume = Mathf.Clamp01(nextOptions.MusicVolume + delta);
                    break;
                case PauseSettingsTarget.Sfx:
                    nextOptions.SfxVolume = Mathf.Clamp01(nextOptions.SfxVolume + delta);
                    break;
                case PauseSettingsTarget.Resolution:
                    CycleResolution(nextOptions, delta);
                    break;
                case PauseSettingsTarget.UiScale:
                    nextOptions.UiScale = Mathf.Clamp(nextOptions.UiScale + Mathf.Sign(delta) * 0.1f, 0.75f, 1.5f);
                    break;
                case PauseSettingsTarget.Fullscreen:
                    nextOptions.Fullscreen = !nextOptions.Fullscreen;
                    break;
                case PauseSettingsTarget.CameraShake:
                    nextOptions.CameraShakeEnabled = !nextOptions.CameraShakeEnabled;
                    break;
                case PauseSettingsTarget.DamageNumbers:
                    nextOptions.DamageNumbersEnabled = !nextOptions.DamageNumbersEnabled;
                    break;
                case PauseSettingsTarget.ReduceFlashes:
                    nextOptions.ReduceFlashes = !nextOptions.ReduceFlashes;
                    break;
                case PauseSettingsTarget.HighContrast:
                    nextOptions.HighContrastUi = !nextOptions.HighContrastUi;
                    break;
                case PauseSettingsTarget.ColorAssist:
                    nextOptions.ColorBlindAssist = !nextOptions.ColorBlindAssist;
                    break;
                default:
                    nextOptions.MasterVolume = Mathf.Clamp01(nextOptions.MasterVolume + delta);
                    break;
            }

            if (gameOptionsService != null)
            {
                gameOptionsService.Import(nextOptions);
            }
            else
            {
                AudioListener.volume = nextOptions.MasterVolume;
            }

            pauseMenuView?.ShowSettings(nextOptions, ResolveSettingsTargetLabel());
            RefreshMenuSelectionVisuals();
        }

        private string ResolveSettingsTargetLabel()
        {
            return _settingsTarget switch
            {
                PauseSettingsTarget.Music => "Music Volume",
                PauseSettingsTarget.Sfx => "SFX Volume",
                PauseSettingsTarget.Resolution => "Resolution",
                PauseSettingsTarget.UiScale => "UI Scale",
                PauseSettingsTarget.Fullscreen => "Fullscreen",
                PauseSettingsTarget.CameraShake => "Camera Shake",
                PauseSettingsTarget.DamageNumbers => "Damage Numbers",
                PauseSettingsTarget.ReduceFlashes => "Reduce Flashes",
                PauseSettingsTarget.HighContrast => "High Contrast",
                PauseSettingsTarget.ColorAssist => "Color Assist",
                _ => "Master Volume"
            };
        }

        private static void CycleResolution(GameOptionsData options, float delta)
        {
            if (options == null || ResolutionPresets.Length == 0)
            {
                return;
            }

            int direction = delta < 0f ? -1 : 1;
            int currentIndex = ResolveClosestResolutionPresetIndex(options.ResolutionWidth, options.ResolutionHeight);
            int nextIndex = (currentIndex + direction + ResolutionPresets.Length) % ResolutionPresets.Length;
            Vector2Int preset = ResolutionPresets[nextIndex];
            options.ResolutionWidth = preset.x;
            options.ResolutionHeight = preset.y;
        }

        private static int ResolveClosestResolutionPresetIndex(int width, int height)
        {
            int bestIndex = 0;
            int bestScore = int.MaxValue;

            for (int index = 0; index < ResolutionPresets.Length; index++)
            {
                Vector2Int preset = ResolutionPresets[index];
                int score = Mathf.Abs(preset.x - width) + Mathf.Abs(preset.y - height);
                if (score >= bestScore)
                {
                    continue;
                }

                bestScore = score;
                bestIndex = index;
            }

            return bestIndex;
        }

        private void RefreshMenuSelectionVisuals()
        {
            if (!_isPaused || pauseMenuView == null)
            {
                return;
            }

            pauseMenuView.RefreshSelectionState(ResolveSelectedButton());
        }

        private Button ResolveSelectedButton()
        {
            if (EventSystem.current?.currentSelectedGameObject == null)
            {
                return _lastSelectedButton != null && _lastSelectedButton.interactable
                    ? _lastSelectedButton
                    : ResolveDefaultSelection();
            }

            Button selectedButton = EventSystem.current.currentSelectedGameObject.GetComponentInParent<Button>();

            if (selectedButton != null && selectedButton.interactable)
            {
                _lastSelectedButton = selectedButton;
                return selectedButton;
            }

            return _lastSelectedButton != null && _lastSelectedButton.interactable
                ? _lastSelectedButton
                : ResolveDefaultSelection();
        }

        private void EnsureMenuSelection()
        {
            if (!_isPaused || EventSystem.current == null)
            {
                return;
            }

            if (EventSystem.current.currentSelectedGameObject != null &&
                EventSystem.current.currentSelectedGameObject.GetComponentInParent<Button>() != null)
            {
                return;
            }

            Button fallbackButton = _lastSelectedButton != null && _lastSelectedButton.interactable
                ? _lastSelectedButton
                : ResolveDefaultSelection();

            if (fallbackButton != null)
            {
                EventSystem.current.SetSelectedGameObject(fallbackButton.gameObject);
                _lastSelectedButton = fallbackButton;
            }
        }

        private Button ResolveDefaultSelection()
        {
            if (_resumeButton != null && _resumeButton.interactable)
            {
                return _resumeButton;
            }

            if (_quitButton != null && _quitButton.interactable)
            {
                return _quitButton;
            }

            if (_settingsButton != null && _settingsButton.interactable)
            {
                return _settingsButton;
            }

            return null;
        }

        private bool WasPausePressedThisFrame()
        {
            Keyboard keyboard = Keyboard.current;

            if (keyboard == null)
            {
                return false;
            }

            return fallbackToggleKey switch
            {
                Key.Pause => keyboard.pauseKey.wasPressedThisFrame,
                Key.Backquote => keyboard.backquoteKey.wasPressedThisFrame,
                _ => keyboard.escapeKey.wasPressedThisFrame,
            };
        }

        private PauseMenuView CreateFallbackPauseMenuView()
        {
            if (safeAreaRoot == null)
            {
                return null;
            }

            GameObject overlayObject = new("PauseMenuOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(PauseMenuView));
            RectTransform overlayRect = overlayObject.GetComponent<RectTransform>();
            overlayRect.SetParent(safeAreaRoot, false);
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;

            Image overlayImage = overlayObject.GetComponent<Image>();
            overlayImage.color = new Color(0f, 0f, 0f, 0.58f);
            overlayImage.raycastTarget = true;

            GameObject panelObject = new("PausePanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            RectTransform panelRect = panelObject.GetComponent<RectTransform>();
            panelRect.SetParent(overlayRect, false);
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.sizeDelta = new Vector2(820f, 820f);

            Image panelImage = panelObject.GetComponent<Image>();
            panelImage.color = new Color(0.9f, 0.82f, 0.68f, 0.98f);
            panelImage.raycastTarget = true;

            Outline panelOutline = panelObject.AddComponent<Outline>();
            panelOutline.effectColor = new Color(0.34f, 0.24f, 0.16f, 0.65f);
            panelOutline.effectDistance = new Vector2(3f, -3f);

            Image accentStrip = CreatePanelBlock("AccentStrip", panelRect, new Vector2(0f, -18f), new Vector2(730f, 12f), new Color(0.42f, 0.56f, 0.7f, 0.88f));
            Image statsSection = CreatePanelBlock("StatsSection", panelRect, new Vector2(0f, -180f), new Vector2(700f, 344f), new Color(1f, 1f, 1f, 0.09f));
            Image actionSection = CreatePanelBlock("ActionSection", panelRect, new Vector2(0f, -604f), new Vector2(700f, 196f), new Color(1f, 1f, 1f, 0.09f));

            Text badgeText = CreateText("PauseBadge", panelRect, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -44f), new Vector2(320f, 28f), 20, FontStyle.Bold, TextAnchor.MiddleCenter);
            badgeText.text = "메뉴 열림";
            badgeText.color = new Color(0.94f, 0.9f, 0.82f, 1f);

            Text titleText = CreateText("PauseTitle", panelRect, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -92f), new Vector2(560f, 96f), 58, FontStyle.Bold, TextAnchor.MiddleCenter);
            titleText.supportRichText = true;
            titleText.text = "<size=18>런 일시정지</size>\n<size=52><b>잠시 멈춤</b></size>";

            Text statsHeader = CreateText("StatsHeader", panelRect, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -170f), new Vector2(260f, 42f), 24, FontStyle.Bold, TextAnchor.MiddleCenter);
            statsHeader.supportRichText = true;
            statsHeader.text = "<size=18>현재 상태</size>\n<b>스탯 요약</b>";
            statsHeader.color = new Color(0.29f, 0.2f, 0.11f, 1f);

            Text[] statTexts = new Text[5];
            for (int index = 0; index < statTexts.Length; index++)
            {
                statTexts[index] = CreateText(
                    $"Stat_{index}",
                    panelRect,
                    new Vector2(0.5f, 1f),
                    new Vector2(0.5f, 1f),
                    new Vector2(0.5f, 1f),
                    new Vector2(0f, -236f - (index * 56f)),
                    new Vector2(560f, 48f),
                    28,
                    FontStyle.Bold,
                    TextAnchor.MiddleCenter);
                statTexts[index].supportRichText = true;
                statTexts[index].color = new Color(0.2f, 0.16f, 0.12f, 1f);
            }

            Text actionHeader = CreateText("ActionHeader", panelRect, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -560f), new Vector2(280f, 42f), 24, FontStyle.Bold, TextAnchor.MiddleCenter);
            actionHeader.supportRichText = true;
            actionHeader.text = "<size=18>다음 행동</size>\n<b>메뉴 선택</b>";
            actionHeader.color = new Color(0.22f, 0.18f, 0.14f, 1f);

            Button settingsButton = CreateButton(panelRect, "SettingsButton", new Vector2(-230f, -662f), out Text settingsButtonText);
            Button resumeButton = CreateButton(panelRect, "ResumeButton", new Vector2(0f, -662f), out Text resumeButtonText);
            Button quitButton = CreateButton(panelRect, "QuitButton", new Vector2(230f, -662f), out Text quitButtonText);

            PauseMenuView view = overlayObject.GetComponent<PauseMenuView>();
            view.ConfigureRuntimeView(
                overlayObject,
                overlayImage,
                panelImage,
                statsSection,
                actionSection,
                badgeText,
                titleText,
                statsHeader,
                actionHeader,
                statTexts,
                settingsButton,
                resumeButton,
                quitButton,
                settingsButtonText,
                resumeButtonText,
                quitButtonText);
            view.Hide();
            return view;
        }

        private static Image CreatePanelBlock(string name, RectTransform parent, Vector2 anchoredPosition, Vector2 size, Color color)
        {
            GameObject blockObject = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            RectTransform blockRect = blockObject.GetComponent<RectTransform>();
            blockRect.SetParent(parent, false);
            blockRect.anchorMin = new Vector2(0.5f, 1f);
            blockRect.anchorMax = new Vector2(0.5f, 1f);
            blockRect.pivot = new Vector2(0.5f, 1f);
            blockRect.anchoredPosition = anchoredPosition;
            blockRect.sizeDelta = size;

            Image blockImage = blockObject.GetComponent<Image>();
            blockImage.color = color;
            blockImage.raycastTarget = false;
            return blockImage;
        }

        private static Button CreateButton(RectTransform parent, string name, Vector2 anchoredPosition, out Text labelText)
        {
            GameObject buttonObject = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
            buttonRect.SetParent(parent, false);
            buttonRect.anchorMin = new Vector2(0.5f, 1f);
            buttonRect.anchorMax = new Vector2(0.5f, 1f);
            buttonRect.pivot = new Vector2(0.5f, 1f);
            buttonRect.anchoredPosition = anchoredPosition;
            buttonRect.sizeDelta = new Vector2(206f, 108f);

            Image buttonImage = buttonObject.GetComponent<Image>();
            buttonImage.color = new Color(1f, 1f, 1f, 0.08f);

            Button button = buttonObject.GetComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = new Color(1f, 1f, 1f, 0.08f);
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.14f);
            colors.pressedColor = new Color(1f, 1f, 1f, 0.22f);
            colors.selectedColor = new Color(1f, 1f, 1f, 0.16f);
            colors.disabledColor = new Color(1f, 1f, 1f, 0.04f);
            button.colors = colors;

            labelText = CreateText("Label", buttonRect, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, 26, FontStyle.Bold, TextAnchor.MiddleCenter);
            labelText.supportRichText = true;
            labelText.color = new Color(0.22f, 0.18f, 0.14f, 1f);
            return button;
        }

        private static Text CreateText(
            string name,
            RectTransform parent,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 anchoredPosition,
            Vector2 sizeDelta,
            int fontSize,
            FontStyle fontStyle,
            TextAnchor alignment)
        {
            GameObject textObject = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.SetParent(parent, false);
            textRect.anchorMin = anchorMin;
            textRect.anchorMax = anchorMax;
            textRect.pivot = pivot;
            textRect.anchoredPosition = anchoredPosition;
            textRect.sizeDelta = sizeDelta;

            Text text = textObject.GetComponent<Text>();
            LocalizedUiFontProvider.Apply(text);
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            text.color = Color.white;
            return text;
        }

        private enum PauseSettingsTarget
        {
            Master,
            Music,
            Sfx,
            Resolution,
            UiScale,
            Fullscreen,
            CameraShake,
            DamageNumbers,
            ReduceFlashes,
            HighContrast,
            ColorAssist
        }
    }
}
