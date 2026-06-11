using CuteIssac.Core.Run;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CuteIssac.UI
{
    /// <summary>
    /// Runtime-built title menu so the title scene stays lightweight and does not depend on authored UI prefabs.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TitleMenuController : MonoBehaviour
    {
        private const string TitleSceneName = "TitleScene";
        private const string GameplaySceneName = "SampleScene";
        private const string MasterVolumePrefKey = "settings.master_volume";
        private const string FullscreenPrefKey = "settings.fullscreen";

        private static bool s_IsSceneHooked;

        private RectTransform _mainPanel;
        private RectTransform _settingsPanel;
        private Text _volumeValueText;
        private Text _fullscreenValueText;
        private float _masterVolume = 0.85f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InitializeAfterSceneLoad()
        {
            if (!s_IsSceneHooked)
            {
                SceneManager.sceneLoaded -= HandleSceneLoaded;
                SceneManager.sceneLoaded += HandleSceneLoaded;
                s_IsSceneHooked = true;
            }

            TryCreateForScene(SceneManager.GetActiveScene());
        }

        private static void HandleSceneLoaded(Scene scene, LoadSceneMode _)
        {
            TryCreateForScene(scene);
        }

        private static void TryCreateForScene(Scene scene)
        {
            if (!scene.IsValid() || scene.name != TitleSceneName)
            {
                return;
            }

            if (FindFirstObjectByType<TitleMenuController>(FindObjectsInactive.Include) != null)
            {
                return;
            }

            GameObject controllerObject = new("TitleMenuController");
            controllerObject.AddComponent<TitleMenuController>();
        }

        private void Awake()
        {
            Time.timeScale = 1f;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            ApplySavedSettings();
            BuildMenu();
            ShowMainMenu();
        }

        private void StartGame()
        {
            RunLaunchRequest.RequestNewRunFromFirstFloor();
            SceneManager.LoadScene(GameplaySceneName, LoadSceneMode.Single);
        }

        private void ShowMainMenu()
        {
            if (_mainPanel != null)
            {
                _mainPanel.gameObject.SetActive(true);
            }

            if (_settingsPanel != null)
            {
                _settingsPanel.gameObject.SetActive(false);
            }
        }

        private void ShowSettings()
        {
            if (_mainPanel != null)
            {
                _mainPanel.gameObject.SetActive(false);
            }

            if (_settingsPanel != null)
            {
                _settingsPanel.gameObject.SetActive(true);
            }

            RefreshSettingsLabels();
        }

        private void AdjustVolume(float delta)
        {
            _masterVolume = Mathf.Clamp01(_masterVolume + delta);
            AudioListener.volume = _masterVolume;
            PlayerPrefs.SetFloat(MasterVolumePrefKey, _masterVolume);
            PlayerPrefs.Save();
            RefreshSettingsLabels();
        }

        private void ToggleFullscreen()
        {
            Screen.fullScreen = !Screen.fullScreen;
            PlayerPrefs.SetInt(FullscreenPrefKey, Screen.fullScreen ? 1 : 0);
            PlayerPrefs.Save();
            RefreshSettingsLabels();
        }

        private void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void ApplySavedSettings()
        {
            _masterVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(MasterVolumePrefKey, 0.85f));
            AudioListener.volume = _masterVolume;

            if (PlayerPrefs.HasKey(FullscreenPrefKey))
            {
                Screen.fullScreen = PlayerPrefs.GetInt(FullscreenPrefKey, Screen.fullScreen ? 1 : 0) != 0;
            }
        }

        private void BuildMenu()
        {
            InputSystemEventSystemBootstrap.EnsureReady();

            Canvas canvas = CreateCanvas();
            CreateBackground(canvas.transform);

            RectTransform shell = CreatePanel("TitleShell", canvas.transform, new Color(0.18f, 0.11f, 0.1f, 0.86f));
            Stretch(shell, new Vector2(0.22f, 0.18f), new Vector2(0.78f, 0.82f), Vector2.zero, Vector2.zero);

            Text title = CreateText("Title", shell, "CANDY VILLAGE", 58, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(1f, 0.82f, 0.34f, 1f));
            Anchor(title.rectTransform, new Vector2(0.08f, 0.72f), new Vector2(0.92f, 0.94f), Vector2.zero, Vector2.zero);

            Text subtitle = CreateText("Subtitle", shell, "달콤한 마을 아래에서 살아남기", 24, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0.94f, 0.9f, 0.82f, 0.92f));
            Anchor(subtitle.rectTransform, new Vector2(0.08f, 0.64f), new Vector2(0.92f, 0.73f), Vector2.zero, Vector2.zero);

            _mainPanel = CreatePanel("MainMenu", shell, new Color(0f, 0f, 0f, 0f));
            Stretch(_mainPanel, new Vector2(0.18f, 0.16f), new Vector2(0.82f, 0.6f), Vector2.zero, Vector2.zero);
            CreateButton(_mainPanel, "게임 시작", 0.68f, StartGame);
            CreateButton(_mainPanel, "설정", 0.43f, ShowSettings);
            CreateButton(_mainPanel, "끝내기", 0.18f, QuitGame);

            _settingsPanel = CreatePanel("SettingsMenu", shell, new Color(0f, 0f, 0f, 0f));
            Stretch(_settingsPanel, new Vector2(0.12f, 0.12f), new Vector2(0.88f, 0.62f), Vector2.zero, Vector2.zero);
            CreateText("SettingsTitle", _settingsPanel, "설정", 34, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(1f, 0.86f, 0.46f, 1f));
            Anchor(_settingsPanel.Find("SettingsTitle").GetComponent<RectTransform>(), new Vector2(0f, 0.78f), new Vector2(1f, 0.98f), Vector2.zero, Vector2.zero);

            _volumeValueText = CreateText("VolumeValue", _settingsPanel, string.Empty, 23, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
            Anchor(_volumeValueText.rectTransform, new Vector2(0f, 0.62f), new Vector2(1f, 0.76f), Vector2.zero, Vector2.zero);
            CreateSmallButton(_settingsPanel, "볼륨 -", new Vector2(0.08f, 0.46f), new Vector2(0.46f, 0.6f), () => AdjustVolume(-0.1f));
            CreateSmallButton(_settingsPanel, "볼륨 +", new Vector2(0.54f, 0.46f), new Vector2(0.92f, 0.6f), () => AdjustVolume(0.1f));

            _fullscreenValueText = CreateText("FullscreenValue", _settingsPanel, string.Empty, 23, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
            Anchor(_fullscreenValueText.rectTransform, new Vector2(0f, 0.3f), new Vector2(1f, 0.43f), Vector2.zero, Vector2.zero);
            CreateSmallButton(_settingsPanel, "전체 화면 변경", new Vector2(0.08f, 0.14f), new Vector2(0.92f, 0.28f), ToggleFullscreen);
            CreateSmallButton(_settingsPanel, "뒤로", new Vector2(0.08f, -0.04f), new Vector2(0.92f, 0.1f), ShowMainMenu);

            RefreshSettingsLabels();
        }

        private void RefreshSettingsLabels()
        {
            if (_volumeValueText != null)
            {
                _volumeValueText.text = $"마스터 볼륨: {Mathf.RoundToInt(_masterVolume * 100f)}%";
            }

            if (_fullscreenValueText != null)
            {
                _fullscreenValueText.text = $"전체 화면: {(Screen.fullScreen ? "켬" : "끔")}";
            }
        }

        private static Canvas CreateCanvas()
        {
            GameObject canvasObject = new("TitleCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 5000;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            return canvas;
        }

        private static void CreateBackground(Transform parent)
        {
            RectTransform background = CreatePanel("CandyBackdrop", parent, new Color(0.42f, 0.18f, 0.15f, 1f));
            Stretch(background, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            Color[] stripeColors =
            {
                new(1f, 0.58f, 0.62f, 0.18f),
                new(0.7f, 1f, 0.72f, 0.16f),
                new(1f, 0.9f, 0.44f, 0.16f),
                new(0.58f, 0.82f, 1f, 0.16f)
            };

            for (int index = 0; index < 18; index++)
            {
                RectTransform stripe = CreatePanel($"CandyStripe{index:00}", background, stripeColors[index % stripeColors.Length]);
                float x = (index - 4) / 14f;
                Anchor(stripe, new Vector2(x, -0.1f), new Vector2(x + 0.065f, 1.1f), Vector2.zero, Vector2.zero);
                stripe.localRotation = Quaternion.Euler(0f, 0f, -18f);
            }
        }

        private static Button CreateButton(RectTransform parent, string label, float normalizedY, UnityEngine.Events.UnityAction onClick)
        {
            return CreateSmallButton(parent, label, new Vector2(0.12f, normalizedY - 0.09f), new Vector2(0.88f, normalizedY + 0.08f), onClick);
        }

        private static Button CreateSmallButton(RectTransform parent, string label, Vector2 anchorMin, Vector2 anchorMax, UnityEngine.Events.UnityAction onClick)
        {
            GameObject buttonObject = new(label, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);

            RectTransform rectTransform = buttonObject.GetComponent<RectTransform>();
            Anchor(rectTransform, anchorMin, anchorMax, Vector2.zero, Vector2.zero);

            Image image = buttonObject.GetComponent<Image>();
            image.color = new Color(0.78f, 0.2f, 0.18f, 0.92f);

            Button button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(onClick);

            ColorBlock colors = button.colors;
            colors.normalColor = new Color(0.78f, 0.2f, 0.18f, 0.92f);
            colors.highlightedColor = new Color(1f, 0.46f, 0.24f, 1f);
            colors.pressedColor = new Color(0.48f, 0.1f, 0.1f, 1f);
            colors.selectedColor = colors.highlightedColor;
            button.colors = colors;

            Text text = CreateText("Label", rectTransform, label, 28, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
            Stretch(text.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            return button;
        }

        private static Text CreateText(
            string name,
            Transform parent,
            string value,
            int fontSize,
            FontStyle style,
            TextAnchor alignment,
            Color color)
        {
            GameObject textObject = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            textObject.transform.SetParent(parent, false);

            Text text = textObject.GetComponent<Text>();
            text.text = value;
            text.color = color;
            LocalizedUiFontProvider.ApplyReadableDefaults(
                text,
                fontSize,
                alignment,
                style,
                horizontalOverflow: HorizontalWrapMode.Overflow,
                verticalOverflow: VerticalWrapMode.Overflow);
            return text;
        }

        private static RectTransform CreatePanel(string name, Transform parent, Color color)
        {
            GameObject panelObject = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panelObject.transform.SetParent(parent, false);

            Image image = panelObject.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return panelObject.GetComponent<RectTransform>();
        }

        private static void Stretch(RectTransform rectTransform, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            Anchor(rectTransform, anchorMin, anchorMax, offsetMin, offsetMax);
        }

        private static void Anchor(RectTransform rectTransform, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            if (rectTransform == null)
            {
                return;
            }

            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.offsetMin = offsetMin;
            rectTransform.offsetMax = offsetMax;
        }
    }
}
