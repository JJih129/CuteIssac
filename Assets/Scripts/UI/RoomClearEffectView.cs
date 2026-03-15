using UnityEngine;
using UnityEngine.UI;
using CuteIssac.Core.Pooling;

namespace CuteIssac.UI
{
    [DisallowMultipleComponent]
    public sealed class RoomClearEffectView : MonoBehaviour, IUiModalDismissible
    {
        [SerializeField] private RectTransform rectTransform;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Image pulseImage;
        [SerializeField] [Min(0.05f)] private float duration = 0.5f;
        [SerializeField] private Vector2 startSize = new(160f, 42f);
        [SerializeField] private Vector2 endSize = new(880f, 160f);

        private float _elapsed;
        private Color _baseColor = Color.white;

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
            float normalized = Mathf.Clamp01(_elapsed / duration);
            float eased = 1f - Mathf.Pow(1f - normalized, 3f);

            if (rectTransform != null)
            {
                rectTransform.sizeDelta = Vector2.Lerp(startSize, endSize, eased);
            }

            if (pulseImage != null)
            {
                Color color = _baseColor;
                color.a *= 1f - normalized;
                pulseImage.color = color;
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f - normalized;
            }

            if (normalized >= 1f)
            {
                PrefabPoolService.Return(gameObject);
            }
        }

        public void Initialize(Color accentColor)
        {
            ResolveReferences();
            _elapsed = 0f;
            _baseColor = accentColor;
            rectTransform.sizeDelta = startSize;
            canvasGroup.alpha = 1f;
            pulseImage.color = _baseColor;
        }

        public void DismissForModal()
        {
            PrefabPoolService.Return(gameObject);
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

            if (pulseImage == null)
            {
                pulseImage = GetComponent<Image>();
            }

            if (pulseImage == null)
            {
                pulseImage = gameObject.AddComponent<Image>();
                pulseImage.raycastTarget = false;
            }
        }
    }
}
