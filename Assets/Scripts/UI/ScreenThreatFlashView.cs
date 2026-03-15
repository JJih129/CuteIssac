using CuteIssac.Core.Pooling;
using UnityEngine;
using UnityEngine.UI;

namespace CuteIssac.UI
{
    [DisallowMultipleComponent]
    public sealed class ScreenThreatFlashView : MonoBehaviour, IUiModalDismissible
    {
        [SerializeField] private RectTransform rectTransform;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Image flashImage;
        [SerializeField] [Min(0.05f)] private float defaultDuration = 0.32f;
        [SerializeField] [Range(1, 4)] private int defaultPulseCount = 1;
        [SerializeField] [Range(0f, 1f)] private float defaultPulseStrength = 0.2f;
        [SerializeField] [Min(0.2f)] private float defaultPulseFrequencyScale = 1f;
        [SerializeField] [Range(0f, 1f)] private float defaultDecaySoftness = 0.36f;

        private float _elapsed;
        private float _duration = 0.32f;
        private float _targetOpacity = 0.1f;
        private Color _flashColor;
        private int _pulseCount = 1;
        private float _pulseStrength = 0.2f;
        private float _pulseFrequencyScale = 1f;
        private float _decaySoftness = 0.36f;

        private void Awake()
        {
            ResolveReferences();
        }

        private void OnEnable()
        {
            UiModalDismissRegistry.Register(this);
        }

        private void OnDisable()
        {
            UiModalDismissRegistry.Unregister(this);
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;
            float normalized = Mathf.Clamp01(_elapsed / Mathf.Max(0.05f, _duration));
            float fade = EvaluateFade(normalized);
            float pulse = EvaluatePulse(normalized);

            if (canvasGroup != null)
            {
                canvasGroup.alpha = fade * pulse;
            }

            if (flashImage != null)
            {
                Color color = _flashColor;
                color.a = _targetOpacity * fade * pulse;
                flashImage.color = color;
            }

            if (normalized >= 1f)
            {
                PrefabPoolService.Return(gameObject);
            }
        }

        public void Initialize(
            Color flashColor,
            float opacity,
            float duration,
            int pulseCount,
            float pulseStrength,
            float pulseFrequencyScale,
            float decaySoftness)
        {
            ResolveReferences();
            _elapsed = 0f;
            _duration = Mathf.Max(0.05f, duration > 0f ? duration : defaultDuration);
            _targetOpacity = Mathf.Clamp01(opacity);
            _flashColor = flashColor;
            _pulseCount = Mathf.Max(1, pulseCount > 0 ? pulseCount : defaultPulseCount);
            _pulseStrength = Mathf.Clamp01(pulseStrength > 0f ? pulseStrength : defaultPulseStrength);
            _pulseFrequencyScale = Mathf.Max(0.2f, pulseFrequencyScale > 0f ? pulseFrequencyScale : defaultPulseFrequencyScale);
            _decaySoftness = Mathf.Clamp01(decaySoftness > 0f ? decaySoftness : defaultDecaySoftness);

            if (rectTransform != null)
            {
                rectTransform.anchorMin = Vector2.zero;
                rectTransform.anchorMax = Vector2.one;
                rectTransform.offsetMin = Vector2.zero;
                rectTransform.offsetMax = Vector2.zero;
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
            }

            if (flashImage != null)
            {
                Color color = _flashColor;
                color.a = _targetOpacity;
                flashImage.color = color;
            }
        }

        public void DismissForModal()
        {
            PrefabPoolService.Return(gameObject);
        }

        private float EvaluateFade(float normalized)
        {
            float fadePower = Mathf.Lerp(1.55f, 0.82f, _decaySoftness);
            return Mathf.Pow(1f - normalized, fadePower);
        }

        private float EvaluatePulse(float normalized)
        {
            float envelope = 1f - normalized * 0.32f;
            float oscillation = 0.5f + 0.5f * Mathf.Cos(normalized * _pulseCount * _pulseFrequencyScale * Mathf.PI * 2f);
            float crest = Mathf.Pow(oscillation, Mathf.Lerp(1.3f, 0.78f, _pulseStrength));
            float pulseBias = Mathf.Lerp(1f, 0.7f + (0.3f * crest), _pulseStrength);
            return Mathf.Clamp01(envelope * pulseBias);
        }

        private void ResolveReferences()
        {
            if (rectTransform == null)
            {
                rectTransform = transform as RectTransform;
            }

            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
            }

            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            if (flashImage == null)
            {
                flashImage = GetComponent<Image>();
            }

            if (flashImage == null)
            {
                flashImage = gameObject.AddComponent<Image>();
                flashImage.raycastTarget = false;
            }
        }
    }
}
