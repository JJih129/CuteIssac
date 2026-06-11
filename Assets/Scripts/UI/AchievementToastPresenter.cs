using System.Collections;
using System.Collections.Generic;
using CuteIssac.Core.Meta;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CuteIssac.UI
{
    [DisallowMultipleComponent]
    public sealed class AchievementToastPresenter : MonoBehaviour
    {
        private const string RuntimeObjectName = "RuntimeAchievementToastPresenter";
        private const float VisibleSeconds = 2.8f;
        private const float SlideSeconds = 0.18f;

        private static AchievementToastPresenter s_instance;

        private readonly Queue<AchievementData> _queue = new();
        private MetaProgressionManager _boundProgressionManager;
        private RectTransform _panel;
        private CanvasGroup _canvasGroup;
        private Text _titleText;
        private Text _detailText;
        private Coroutine _routine;
        private float _nextBindAttemptRealtime;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureRuntimePresenter()
        {
            if (s_instance != null)
            {
                s_instance.BindToProgressionManager();
                return;
            }

            AchievementToastPresenter existing = FindFirstObjectByType<AchievementToastPresenter>(FindObjectsInactive.Include);

            if (existing != null)
            {
                s_instance = existing;
                s_instance.BindToProgressionManager();
                return;
            }

            GameObject presenterObject = new(RuntimeObjectName);
            s_instance = presenterObject.AddComponent<AchievementToastPresenter>();
        }

        private void Awake()
        {
            if (s_instance != null && s_instance != this)
            {
                Destroy(gameObject);
                return;
            }

            s_instance = this;
            DontDestroyOnLoad(gameObject);
            BuildToastUi();
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            SceneManager.sceneLoaded += HandleSceneLoaded;
            BindToProgressionManager();
        }

        private void OnDestroy()
        {
            if (s_instance == this)
            {
                s_instance = null;
            }

            SceneManager.sceneLoaded -= HandleSceneLoaded;
            UnbindProgressionManager();
        }

        private void Update()
        {
            if (_boundProgressionManager != null || Time.unscaledTime < _nextBindAttemptRealtime)
            {
                return;
            }

            _nextBindAttemptRealtime = Time.unscaledTime + 0.5f;
            BindToProgressionManager();
        }

        private void HandleSceneLoaded(Scene _, LoadSceneMode __)
        {
            BindToProgressionManager();
        }

        private void BindToProgressionManager()
        {
            MetaProgressionManager manager = FindFirstObjectByType<MetaProgressionManager>(FindObjectsInactive.Exclude);

            if (_boundProgressionManager == manager)
            {
                return;
            }

            UnbindProgressionManager();
            _boundProgressionManager = manager;

            if (_boundProgressionManager != null)
            {
                _boundProgressionManager.AchievementsUnlocked -= HandleAchievementsUnlocked;
                _boundProgressionManager.AchievementsUnlocked += HandleAchievementsUnlocked;
            }
        }

        private void UnbindProgressionManager()
        {
            if (_boundProgressionManager == null)
            {
                return;
            }

            _boundProgressionManager.AchievementsUnlocked -= HandleAchievementsUnlocked;
            _boundProgressionManager = null;
        }

        private void HandleAchievementsUnlocked(IReadOnlyList<AchievementData> achievements)
        {
            if (achievements == null)
            {
                return;
            }

            for (int index = 0; index < achievements.Count; index++)
            {
                if (achievements[index] != null)
                {
                    _queue.Enqueue(achievements[index]);
                }
            }

            if (_routine == null && _queue.Count > 0)
            {
                _routine = StartCoroutine(ShowQueuedToasts());
            }
        }

        private IEnumerator ShowQueuedToasts()
        {
            while (_queue.Count > 0)
            {
                AchievementData achievement = _queue.Dequeue();
                ShowAchievement(achievement);

                float elapsed = 0f;
                while (elapsed < SlideSeconds)
                {
                    elapsed += Time.unscaledDeltaTime;
                    ApplyVisibility(Mathf.Clamp01(elapsed / SlideSeconds));
                    yield return null;
                }

                elapsed = 0f;
                while (elapsed < VisibleSeconds)
                {
                    elapsed += Time.unscaledDeltaTime;
                    yield return null;
                }

                elapsed = 0f;
                while (elapsed < SlideSeconds)
                {
                    elapsed += Time.unscaledDeltaTime;
                    ApplyVisibility(1f - Mathf.Clamp01(elapsed / SlideSeconds));
                    yield return null;
                }

                ApplyVisibility(0f);
            }

            _routine = null;
        }

        private void ShowAchievement(AchievementData achievement)
        {
            if (achievement == null)
            {
                return;
            }

            _titleText.text = string.IsNullOrWhiteSpace(achievement.DisplayName)
                ? "Achievement Unlocked"
                : achievement.DisplayName;
            _detailText.text = string.IsNullOrWhiteSpace(achievement.Description)
                ? "New achievement completed."
                : achievement.Description;
        }

        private void ApplyVisibility(float value)
        {
            if (_canvasGroup == null || _panel == null)
            {
                return;
            }

            _canvasGroup.alpha = value;
            _panel.anchoredPosition = new Vector2(0f, Mathf.Lerp(96f, -28f, value));
        }

        private void BuildToastUi()
        {
            if (_panel != null)
            {
                return;
            }

            GameObject canvasObject = new("AchievementToastCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 6100;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            GameObject panelObject = new("AchievementToast", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
            panelObject.transform.SetParent(canvasObject.transform, false);
            _panel = panelObject.GetComponent<RectTransform>();
            _panel.anchorMin = new Vector2(0.5f, 1f);
            _panel.anchorMax = new Vector2(0.5f, 1f);
            _panel.pivot = new Vector2(0.5f, 1f);
            _panel.sizeDelta = new Vector2(560f, 104f);
            _panel.anchoredPosition = new Vector2(0f, 96f);

            Image panelImage = panelObject.GetComponent<Image>();
            panelImage.color = new Color(0.09f, 0.11f, 0.13f, 0.94f);

            _canvasGroup = panelObject.GetComponent<CanvasGroup>();
            _canvasGroup.alpha = 0f;
            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.interactable = false;

            _titleText = CreateText("Title", _panel, 24, FontStyle.Bold, new Vector2(24f, 56f), new Vector2(-24f, -14f));
            _detailText = CreateText("Detail", _panel, 17, FontStyle.Normal, new Vector2(24f, 16f), new Vector2(-24f, -52f));
        }

        private static Text CreateText(string name, Transform parent, int fontSize, FontStyle style, Vector2 offsetMin, Vector2 offsetMax)
        {
            GameObject textObject = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            textObject.transform.SetParent(parent, false);

            RectTransform rectTransform = textObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = offsetMin;
            rectTransform.offsetMax = offsetMax;

            Text text = textObject.GetComponent<Text>();
            text.color = Color.white;
            LocalizedUiFontProvider.ApplyReadableDefaults(
                text,
                fontSize,
                TextAnchor.UpperLeft,
                style,
                horizontalOverflow: HorizontalWrapMode.Wrap,
                verticalOverflow: VerticalWrapMode.Truncate);
            return text;
        }
    }
}
