using UnityEngine;
using UnityEngine.UI;

namespace CuteIssac.UI
{
    /// <summary>
    /// Presentation-only view for one minimap room node.
    /// The same gameplay state can be reskinned by swapping images and sprites in the node prefab.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MinimapNodeView : MonoBehaviour
    {
        public readonly struct MinimapNodePresentation
        {
            public MinimapNodePresentation(
                Vector2 nodeSize,
                float connectionLength,
                float connectionThickness,
                Color frameColor,
                Color fillColor,
                bool pulseNode,
                Color nodePulseColor,
                float nodePulseSpeed,
                float nodePulseTintStrength,
                float nodePulseScaleAmplitude,
                Color connectionColor,
                Color secretConnectionColor,
                Sprite connectionSprite,
                Sprite specialIconSprite,
                Color specialIconColor,
                bool showCurrentMarker,
                Color currentMarkerColor,
                bool pulseCurrentMarker,
                bool showClearedMark,
                Sprite clearedMarkSprite,
                Color clearedMarkColor,
                bool showRewardMarker,
                Sprite rewardMarkSprite,
                Color rewardMarkColor,
                bool pulseRewardMarker,
                bool showUpConnection,
                bool showUpSecretConnection,
                bool showDownConnection,
                bool showDownSecretConnection,
                bool showLeftConnection,
                bool showLeftSecretConnection,
                bool showRightConnection,
                bool showRightSecretConnection)
            {
                NodeSize = nodeSize;
                ConnectionLength = connectionLength;
                ConnectionThickness = connectionThickness;
                FrameColor = frameColor;
                FillColor = fillColor;
                PulseNode = pulseNode;
                NodePulseColor = nodePulseColor;
                NodePulseSpeed = nodePulseSpeed;
                NodePulseTintStrength = nodePulseTintStrength;
                NodePulseScaleAmplitude = nodePulseScaleAmplitude;
                ConnectionColor = connectionColor;
                SecretConnectionColor = secretConnectionColor;
                ConnectionSprite = connectionSprite;
                SpecialIconSprite = specialIconSprite;
                SpecialIconColor = specialIconColor;
                ShowCurrentMarker = showCurrentMarker;
                CurrentMarkerColor = currentMarkerColor;
                PulseCurrentMarker = pulseCurrentMarker;
                ShowClearedMark = showClearedMark;
                ClearedMarkSprite = clearedMarkSprite;
                ClearedMarkColor = clearedMarkColor;
                ShowRewardMarker = showRewardMarker;
                RewardMarkSprite = rewardMarkSprite;
                RewardMarkColor = rewardMarkColor;
                PulseRewardMarker = pulseRewardMarker;
                ShowUpConnection = showUpConnection;
                ShowUpSecretConnection = showUpSecretConnection;
                ShowDownConnection = showDownConnection;
                ShowDownSecretConnection = showDownSecretConnection;
                ShowLeftConnection = showLeftConnection;
                ShowLeftSecretConnection = showLeftSecretConnection;
                ShowRightConnection = showRightConnection;
                ShowRightSecretConnection = showRightSecretConnection;
            }

            public Vector2 NodeSize { get; }
            public float ConnectionLength { get; }
            public float ConnectionThickness { get; }
            public Color FrameColor { get; }
            public Color FillColor { get; }
            public bool PulseNode { get; }
            public Color NodePulseColor { get; }
            public float NodePulseSpeed { get; }
            public float NodePulseTintStrength { get; }
            public float NodePulseScaleAmplitude { get; }
            public Color ConnectionColor { get; }
            public Color SecretConnectionColor { get; }
            public Sprite ConnectionSprite { get; }
            public Sprite SpecialIconSprite { get; }
            public Color SpecialIconColor { get; }
            public bool ShowCurrentMarker { get; }
            public Color CurrentMarkerColor { get; }
            public bool PulseCurrentMarker { get; }
            public bool ShowClearedMark { get; }
            public Sprite ClearedMarkSprite { get; }
            public Color ClearedMarkColor { get; }
            public bool ShowRewardMarker { get; }
            public Sprite RewardMarkSprite { get; }
            public Color RewardMarkColor { get; }
            public bool PulseRewardMarker { get; }
            public bool ShowUpConnection { get; }
            public bool ShowUpSecretConnection { get; }
            public bool ShowDownConnection { get; }
            public bool ShowDownSecretConnection { get; }
            public bool ShowLeftConnection { get; }
            public bool ShowLeftSecretConnection { get; }
            public bool ShowRightConnection { get; }
            public bool ShowRightSecretConnection { get; }
        }

        [Header("Structure")]
        [SerializeField] private RectTransform rootRect;
        [SerializeField] private Image roomBaseImage;
        [SerializeField] private Image roomFillImage;
        [SerializeField] private Image currentMarkerImage;
        [SerializeField] private Image specialIconImage;
        [SerializeField] private Image clearedMarkImage;
        [SerializeField] private Image rewardMarkerImage;
        [SerializeField] private Image upConnectionImage;
        [SerializeField] private Image downConnectionImage;
        [SerializeField] private Image leftConnectionImage;
        [SerializeField] private Image rightConnectionImage;

        [Header("Pulse")]
        [SerializeField] [Min(0f)] private float currentPulseSpeed = 3.1f;
        [SerializeField] [Min(0f)] private float currentPulseAlphaAmplitude = 0.2f;
        [SerializeField] [Min(1f)] private float currentPulseScaleAmplitude = 1.08f;
        [SerializeField] [Min(0f)] private float nodePulseSpeed = 2.4f;
        [SerializeField] [Range(0f, 0.35f)] private float nodePulseTintStrength = 0.18f;
        [SerializeField] [Min(1f)] private float nodePulseScaleAmplitude = 1.05f;
        [SerializeField] [Min(0f)] private float rewardPulseSpeed = 3.8f;
        [SerializeField] [Min(0f)] private float rewardPulseAlphaAmplitude = 0.28f;
        [SerializeField] [Min(1f)] private float rewardPulseScaleAmplitude = 1.14f;

        private bool _pulseNode;
        private Color _roomBaseBaseColor = Color.white;
        private Color _roomFillBaseColor = Color.white;
        private Color _nodePulseColor = Color.white;
        private float _nodePulseSpeedRuntime;
        private float _nodePulseTintStrengthRuntime;
        private float _nodePulseScaleAmplitudeRuntime = 1.05f;
        private bool _pulseCurrentMarker;
        private Color _currentMarkerBaseColor = Color.white;
        private bool _pulseRewardMarker;
        private Color _rewardMarkerBaseColor = Color.white;

        public RectTransform RootRect => rootRect != null ? rootRect : (RectTransform)transform;

        private void Update()
        {
            if (roomBaseImage == null || roomFillImage == null || !gameObject.activeInHierarchy)
            {
                _pulseNode = false;
            }
            else if (!_pulseNode || nodePulseSpeed <= 0f)
            {
                roomBaseImage.color = _roomBaseBaseColor;
                roomFillImage.color = _roomFillBaseColor;
                RootRect.localScale = Vector3.one;
            }
            else
            {
                float resolvedPulseSpeed = _nodePulseSpeedRuntime > 0f ? _nodePulseSpeedRuntime : nodePulseSpeed;
                float resolvedTintStrength = _nodePulseTintStrengthRuntime > 0f ? _nodePulseTintStrengthRuntime : nodePulseTintStrength;
                float resolvedScaleAmplitude = _nodePulseScaleAmplitudeRuntime > 1f ? _nodePulseScaleAmplitudeRuntime : nodePulseScaleAmplitude;
                float nodeWave = (Mathf.Sin(Time.unscaledTime * resolvedPulseSpeed) + 1f) * 0.5f;
                float tintBlend = Mathf.Lerp(0.04f, resolvedTintStrength, nodeWave);
                float nodeScale = Mathf.Lerp(1f, resolvedScaleAmplitude, nodeWave);
                roomBaseImage.color = Color.Lerp(_roomBaseBaseColor, _nodePulseColor, tintBlend);
                roomFillImage.color = Color.Lerp(_roomFillBaseColor, _nodePulseColor, tintBlend * 0.72f);
                RootRect.localScale = new Vector3(nodeScale, nodeScale, 1f);
            }

            if (currentMarkerImage == null || !currentMarkerImage.gameObject.activeSelf)
            {
                _pulseCurrentMarker = false;
            }
            else if (!_pulseCurrentMarker || currentPulseSpeed <= 0f)
            {
                currentMarkerImage.color = _currentMarkerBaseColor;
                currentMarkerImage.rectTransform.localScale = Vector3.one;
            }
            else
            {
                float wave = (Mathf.Sin(Time.unscaledTime * currentPulseSpeed) + 1f) * 0.5f;
                float alpha = Mathf.Clamp01(_currentMarkerBaseColor.a - currentPulseAlphaAmplitude + (wave * currentPulseAlphaAmplitude));
                float scale = Mathf.Lerp(1f, currentPulseScaleAmplitude, wave);
                currentMarkerImage.color = new Color(
                    _currentMarkerBaseColor.r,
                    _currentMarkerBaseColor.g,
                    _currentMarkerBaseColor.b,
                    alpha);
                currentMarkerImage.rectTransform.localScale = new Vector3(scale, scale, 1f);
            }

            if (rewardMarkerImage == null || !rewardMarkerImage.gameObject.activeSelf)
            {
                _pulseRewardMarker = false;
                return;
            }

            if (!_pulseRewardMarker || rewardPulseSpeed <= 0f)
            {
                rewardMarkerImage.color = _rewardMarkerBaseColor;
                rewardMarkerImage.rectTransform.localScale = Vector3.one;
                return;
            }

            float rewardWave = (Mathf.Sin(Time.unscaledTime * rewardPulseSpeed) + 1f) * 0.5f;
            float rewardAlpha = Mathf.Clamp01(_rewardMarkerBaseColor.a - rewardPulseAlphaAmplitude + (rewardWave * rewardPulseAlphaAmplitude));
            float rewardScale = Mathf.Lerp(1f, rewardPulseScaleAmplitude, rewardWave);
            rewardMarkerImage.color = new Color(
                _rewardMarkerBaseColor.r,
                _rewardMarkerBaseColor.g,
                _rewardMarkerBaseColor.b,
                rewardAlpha);
            rewardMarkerImage.rectTransform.localScale = new Vector3(rewardScale, rewardScale, 1f);
        }

        public void ConfigureFallback(
            RectTransform rectTransform,
            Image baseImage,
            Image fillImage,
            Image currentMarker,
            Image specialIcon,
            Image clearedMark,
            Image rewardMarker,
            Image upConnection,
            Image downConnection,
            Image leftConnection,
            Image rightConnection)
        {
            rootRect = rectTransform;
            roomBaseImage = baseImage;
            roomFillImage = fillImage;
            currentMarkerImage = currentMarker;
            specialIconImage = specialIcon;
            clearedMarkImage = clearedMark;
            rewardMarkerImage = rewardMarker;
            upConnectionImage = upConnection;
            downConnectionImage = downConnection;
            leftConnectionImage = leftConnection;
            rightConnectionImage = rightConnection;
        }

        public void SetAnchoredPosition(Vector2 anchoredPosition)
        {
            RootRect.anchoredPosition = anchoredPosition;
        }

        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }

        public void Present(MinimapNodePresentation presentation)
        {
            RootRect.sizeDelta = presentation.NodeSize;

            _roomBaseBaseColor = presentation.FrameColor;
            _roomFillBaseColor = presentation.FillColor;
            _pulseNode = presentation.PulseNode;
            _nodePulseColor = presentation.NodePulseColor;
            _nodePulseSpeedRuntime = presentation.NodePulseSpeed;
            _nodePulseTintStrengthRuntime = presentation.NodePulseTintStrength;
            _nodePulseScaleAmplitudeRuntime = presentation.NodePulseScaleAmplitude;
            SetImage(roomBaseImage, true, null, presentation.FrameColor);
            SetImage(roomFillImage, true, null, presentation.FillColor);

            bool showSpecialIcon = presentation.SpecialIconSprite != null;
            SetImage(specialIconImage, showSpecialIcon, presentation.SpecialIconSprite, presentation.SpecialIconColor);

            _currentMarkerBaseColor = presentation.CurrentMarkerColor;
            _pulseCurrentMarker = presentation.ShowCurrentMarker && presentation.PulseCurrentMarker;
            SetImage(currentMarkerImage, presentation.ShowCurrentMarker, null, presentation.CurrentMarkerColor);

            SetImage(clearedMarkImage, presentation.ShowClearedMark, presentation.ClearedMarkSprite, presentation.ClearedMarkColor);
            _rewardMarkerBaseColor = presentation.RewardMarkColor;
            _pulseRewardMarker = presentation.ShowRewardMarker && presentation.PulseRewardMarker;
            SetImage(rewardMarkerImage, presentation.ShowRewardMarker, presentation.RewardMarkSprite, presentation.RewardMarkColor);

            PresentConnection(upConnectionImage, presentation.ShowUpConnection, presentation.ShowUpSecretConnection, presentation.ConnectionSprite, presentation.ConnectionColor, presentation.SecretConnectionColor, presentation.ConnectionThickness, presentation.ConnectionLength, true);
            PresentConnection(downConnectionImage, presentation.ShowDownConnection, presentation.ShowDownSecretConnection, presentation.ConnectionSprite, presentation.ConnectionColor, presentation.SecretConnectionColor, presentation.ConnectionThickness, presentation.ConnectionLength, true);
            PresentConnection(leftConnectionImage, presentation.ShowLeftConnection, presentation.ShowLeftSecretConnection, presentation.ConnectionSprite, presentation.ConnectionColor, presentation.SecretConnectionColor, presentation.ConnectionThickness, presentation.ConnectionLength, false);
            PresentConnection(rightConnectionImage, presentation.ShowRightConnection, presentation.ShowRightSecretConnection, presentation.ConnectionSprite, presentation.ConnectionColor, presentation.SecretConnectionColor, presentation.ConnectionThickness, presentation.ConnectionLength, false);
        }

        private static void SetImage(Image image, bool visible, Sprite sprite, Color color)
        {
            if (image == null)
            {
                return;
            }

            image.gameObject.SetActive(visible);

            if (!visible)
            {
                return;
            }

            image.sprite = sprite;
            image.color = color;
        }

        private static void PresentConnection(
            Image image,
            bool visible,
            bool secretConnection,
            Sprite sprite,
            Color color,
            Color secretColor,
            float thickness,
            float length,
            bool vertical)
        {
            if (image == null)
            {
                return;
            }

            image.gameObject.SetActive(visible);

            if (!visible)
            {
                return;
            }

            image.sprite = sprite;
            image.color = secretConnection ? secretColor : color;
            RectTransform rectTransform = image.rectTransform;
            float resolvedThickness = secretConnection ? thickness * 1.35f : thickness;
            rectTransform.sizeDelta = vertical
                ? new Vector2(resolvedThickness, length)
                : new Vector2(length, resolvedThickness);
        }
    }
}
