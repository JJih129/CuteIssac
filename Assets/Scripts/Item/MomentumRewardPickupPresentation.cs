using CuteIssac.Core.Feedback;
using CuteIssac.Core.Scene;
using CuteIssac.Player;
using UnityEngine;
using System.Collections.Generic;

namespace CuteIssac.Item
{
    /// <summary>
    /// Runtime-only marker layer for momentum payout pickups so art can be swapped later without touching reward logic.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BasePickupLogic))]
    public sealed class MomentumRewardPickupPresentation : MonoBehaviour
    {
        [Header("Motion")]
        [SerializeField] [Min(0f)] private float bobAmplitude = 0.05f;
        [SerializeField] [Min(0f)] private float bobFrequency = 2.8f;
        [SerializeField] [Min(0f)] private float pulseFrequency = 4.6f;

        [Header("Pickup Flow")]
        [SerializeField] [Min(0f)] private float attractRadius = 1.75f;
        [SerializeField] [Min(0f)] private float attractSpeed = 4.25f;
        [SerializeField] [Min(0f)] private float highValueAttractBoost = 1.2f;
        [SerializeField] [Min(0f)] private float sweepAttractRadius = 3.4f;
        [SerializeField] [Min(0f)] private float sweepDuration = 1.1f;
        [SerializeField] [Min(0f)] private float sweepSpeedBonus = 2.4f;
        [SerializeField] [Min(0f)] private float chainTriggerRadius = 2.85f;
        [SerializeField] [Min(0f)] private float targetRefreshInterval = 0.6f;

        [Header("Layout")]
        [SerializeField] private Vector3 markerOffset = new(0f, 0.16f, 0f);

        private static Sprite s_WhiteSprite;
        private static Sprite s_CircleSprite;
        private static readonly List<MomentumRewardPickupPresentation> s_ActivePresentations = new();

        [SerializeField] private BasePickupLogic pickupLogic;
        [SerializeField] private PickupVisual pickupVisual;
        [SerializeField] private RoomRewardPickupTracker roomRewardPickupTracker;
        [Tooltip("씬에 배치된 GameplaySceneContext입니다. 런타임 생성 시 비어 있으면 Active Context를 사용합니다.")]
        [SerializeField] private GameplaySceneContext sceneContext;

        private Transform _markerRoot;
        private SpriteRenderer _ringRenderer;
        private SpriteRenderer _coreRenderer;
        private SpriteRenderer _beamRenderer;
        private SpriteRenderer _underlineRenderer;
        private Vector3 _rootBaseLocalPosition;
        private Vector3 _ringBaseScale = Vector3.one;
        private Vector3 _coreBaseScale = Vector3.one;
        private Vector3 _beamBaseScale = Vector3.one;
        private Vector3 _underlineBaseScale = Vector3.one;
        private Color _accentColor = new(1f, 0.84f, 0.36f, 1f);
        private int _sequenceIndex;
        private bool _isHighValue;
        private bool _isConfigured;
        private float _animationSeed;
        private string _feedbackLabel = string.Empty;
        private PlayerHealth _playerHealth;
        private Transform _playerTarget;
        private float _nextTargetRefreshTime;
        private float _sweepExpiresAt;
        private float _proximityPulse;

        private void Awake()
        {
            ResolveReferences();

            if (pickupLogic != null)
            {
                pickupLogic.Collected -= HandleCollected;
                pickupLogic.Collected += HandleCollected;
            }

            EnsureRuntimeMarker();
            SetMarkerVisible(false);
        }

        private void OnEnable()
        {
            ResolveReferences();
            EnsureRuntimeMarker();
            _isConfigured = false;
            _sweepExpiresAt = 0f;
            _proximityPulse = 0f;
            SetMarkerVisible(false);

            if (!s_ActivePresentations.Contains(this))
            {
                s_ActivePresentations.Add(this);
            }
        }

        private void OnDisable()
        {
            _isConfigured = false;
            _sweepExpiresAt = 0f;
            _proximityPulse = 0f;
            SetMarkerVisible(false);
            s_ActivePresentations.Remove(this);
        }

        private void OnDestroy()
        {
            if (pickupLogic != null)
            {
                pickupLogic.Collected -= HandleCollected;
            }
        }

        private void Update()
        {
            if (!_isConfigured || _markerRoot == null)
            {
                return;
            }

            float time = Time.time + _animationSeed;
            float bob = Mathf.Sin(time * bobFrequency) * bobAmplitude;
            float pulse = 0.5f + (Mathf.Sin(time * pulseFrequency) * 0.5f);
            RefreshPlayerTargetIfNeeded();
            UpdatePickupFlow();
            _markerRoot.localPosition = _rootBaseLocalPosition + new Vector3(0f, bob, 0f);
            ApplyVisualState(Mathf.Lerp(pulse, 1f, _proximityPulse));
        }

        public void Configure(Color accentColor, bool isHighValue, int sequenceIndex)
        {
            ResolveReferences();
            EnsureRuntimeMarker();

            _accentColor = accentColor.a > 0.01f
                ? new Color(accentColor.r, accentColor.g, accentColor.b, 1f)
                : new Color(1f, 0.84f, 0.36f, 1f);
            _isHighValue = isHighValue;
            _sequenceIndex = Mathf.Max(0, sequenceIndex);
            _animationSeed = _sequenceIndex * 0.23f;
            _feedbackLabel = isHighValue ? "FLOW ITEM" : "FLOW CACHE";
            _isConfigured = true;
            _sweepExpiresAt = 0f;
            _proximityPulse = 0f;
            ApplyVisualState(0.5f);
            SetMarkerVisible(true);
        }

        private void HandleCollected(BasePickupLogic _)
        {
            if (!_isConfigured)
            {
                return;
            }

            Vector3 feedbackPosition = pickupVisual != null && pickupVisual.PickupEffectAnchor != null
                ? pickupVisual.PickupEffectAnchor.position
                : transform.position;
            Color feedbackColor = Color.Lerp(_accentColor, Color.white, 0.18f);

            GameplayFeedbackEvents.RaiseFloatingFeedback(new FloatingFeedbackRequest(
                feedbackPosition + new Vector3(0f, 0.16f, 0f),
                _feedbackLabel,
                feedbackColor,
                0.62f,
                0.58f,
                _isHighValue ? 1.1f : 1.02f,
                visualProfile: FloatingFeedbackVisualProfile.Momentum));

            if (_isHighValue)
            {
                GameplayFeedbackEvents.RaiseThreatFlash(new ThreatFlashRequest(
                    _accentColor,
                    0.14f,
                    0.26f,
                    pulseCount: 2,
                    pulseStrength: 0.22f,
                    pulseFrequencyScale: 1.08f,
                    decaySoftness: 0.28f));
            }

            TriggerSweepCollection();
            _isConfigured = false;
            SetMarkerVisible(false);
        }

        private void ResolveReferences()
        {
            if (pickupLogic == null)
            {
                TryGetComponent(out pickupLogic);
            }

            if (pickupVisual == null)
            {
                TryGetComponent(out pickupVisual);
            }

            if (roomRewardPickupTracker == null)
            {
                TryGetComponent(out roomRewardPickupTracker);
            }
        }

        private void EnsureRuntimeMarker()
        {
            Transform anchor = ResolveAnchor();

            if (_markerRoot != null)
            {
                if (_markerRoot.parent != anchor)
                {
                    _markerRoot.SetParent(anchor, false);
                }

                _rootBaseLocalPosition = markerOffset;
                return;
            }

            GameObject root = new("MomentumRewardMarker");
            root.layer = gameObject.layer;
            _markerRoot = root.transform;
            _markerRoot.SetParent(anchor, false);
            _rootBaseLocalPosition = markerOffset;
            _markerRoot.localPosition = _rootBaseLocalPosition;

            _ringRenderer = CreatePart(
                "PulseRing",
                GetCircleSprite(),
                Vector3.zero,
                new Vector3(0.62f, 0.62f, 1f),
                new Color(_accentColor.r, _accentColor.g, _accentColor.b, 0.24f),
                4);
            _coreRenderer = CreatePart(
                "FlowCore",
                GetCircleSprite(),
                Vector3.zero,
                new Vector3(0.18f, 0.18f, 1f),
                Color.Lerp(_accentColor, Color.white, 0.42f),
                6);
            _beamRenderer = CreatePart(
                "FlowBeam",
                GetWhiteSprite(),
                new Vector3(0f, 0.32f, 0f),
                new Vector3(0.06f, 0.42f, 1f),
                new Color(_accentColor.r, _accentColor.g, _accentColor.b, 0.28f),
                3);
            _underlineRenderer = CreatePart(
                "FlowUnderline",
                GetWhiteSprite(),
                new Vector3(0f, -0.18f, 0f),
                new Vector3(0.26f, 0.04f, 1f),
                new Color(_accentColor.r, _accentColor.g, _accentColor.b, 0.72f),
                5);

            _ringBaseScale = _ringRenderer.transform.localScale;
            _coreBaseScale = _coreRenderer.transform.localScale;
            _beamBaseScale = _beamRenderer.transform.localScale;
            _underlineBaseScale = _underlineRenderer.transform.localScale;
        }

        private Transform ResolveAnchor()
        {
            if (pickupVisual != null)
            {
                if (pickupVisual.OptionalHighlightRoot != null)
                {
                    return pickupVisual.OptionalHighlightRoot;
                }

                if (pickupVisual.PickupEffectAnchor != null)
                {
                    return pickupVisual.PickupEffectAnchor;
                }
            }

            return transform;
        }

        private void RefreshPlayerTargetIfNeeded()
        {
            if (_playerTarget != null && _playerTarget.gameObject.activeInHierarchy)
            {
                return;
            }

            if (Time.time < _nextTargetRefreshTime)
            {
                return;
            }

            _nextTargetRefreshTime = Time.time + Mathf.Max(0.1f, targetRefreshInterval);

            if (_playerHealth == null)
            {
                _playerHealth = ResolvePlayerHealth();
            }

            _playerTarget = _playerHealth != null ? _playerHealth.transform : null;
        }

        private PlayerHealth ResolvePlayerHealth()
        {
            if (sceneContext == null)
            {
                sceneContext = GameplaySceneContext.Active;
            }

            if (sceneContext != null)
            {
                sceneContext.ResolveMissingReferences();

                if (sceneContext.PlayerHealth != null)
                {
                    return sceneContext.PlayerHealth;
                }
            }

            return FindFirstObjectByType<PlayerHealth>(FindObjectsInactive.Exclude);
        }

        private void UpdatePickupFlow()
        {
            if (_playerTarget == null)
            {
                _proximityPulse = Mathf.MoveTowards(_proximityPulse, 0f, Time.deltaTime * 2f);
                return;
            }

            Vector3 offset = _playerTarget.position - transform.position;
            float distance = offset.magnitude;
            bool isSweepActive = Time.time <= _sweepExpiresAt;
            float effectiveRadius = attractRadius;
            float effectiveSpeed = attractSpeed * (_isHighValue ? 1f + highValueAttractBoost : 1f);

            if (isSweepActive)
            {
                effectiveRadius = Mathf.Max(effectiveRadius, sweepAttractRadius);
                effectiveSpeed += sweepSpeedBonus;
            }

            if (distance > effectiveRadius || distance <= 0.01f)
            {
                _proximityPulse = Mathf.MoveTowards(_proximityPulse, 0f, Time.deltaTime * 2.4f);
                return;
            }

            float proximity = 1f - Mathf.Clamp01(distance / Mathf.Max(0.01f, effectiveRadius));
            float speedMultiplier = Mathf.Lerp(0.78f, 1.52f, proximity);
            Vector3 direction = offset / distance;
            transform.position += direction * (effectiveSpeed * speedMultiplier * Time.deltaTime);
            _proximityPulse = Mathf.MoveTowards(_proximityPulse, Mathf.Lerp(0.18f, 0.82f, proximity), Time.deltaTime * 5.2f);
        }

        private SpriteRenderer CreatePart(
            string name,
            Sprite sprite,
            Vector3 localPosition,
            Vector3 localScale,
            Color color,
            int sortingOffset,
            float rotationZ = 0f)
        {
            GameObject child = new(name);
            child.layer = gameObject.layer;
            child.transform.SetParent(_markerRoot, false);
            child.transform.localPosition = localPosition;
            child.transform.localScale = localScale;
            child.transform.localRotation = Quaternion.Euler(0f, 0f, rotationZ);

            SpriteRenderer renderer = child.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;

            if (pickupVisual != null && pickupVisual.BodySpriteRenderer != null)
            {
                renderer.sortingLayerID = pickupVisual.BodySpriteRenderer.sortingLayerID;
                renderer.sortingOrder = pickupVisual.BodySpriteRenderer.sortingOrder + sortingOffset;
            }
            else
            {
                renderer.sortingOrder = sortingOffset;
            }

            return renderer;
        }

        private void ApplyVisualState(float pulse)
        {
            if (_ringRenderer == null || _coreRenderer == null || _beamRenderer == null || _underlineRenderer == null)
            {
                return;
            }

            float normalizedPulse = Mathf.Clamp01(pulse);
            float ringScaleMultiplier = _isHighValue
                ? Mathf.Lerp(1.05f, 1.28f, normalizedPulse)
                : Mathf.Lerp(0.9f, 1.1f, normalizedPulse);
            float coreScaleMultiplier = _isHighValue
                ? Mathf.Lerp(1.04f, 1.18f, normalizedPulse)
                : Mathf.Lerp(0.94f, 1.08f, normalizedPulse);
            float beamScaleMultiplier = _isHighValue
                ? Mathf.Lerp(1f, 1.26f, normalizedPulse)
                : Mathf.Lerp(0.86f, 1.04f, normalizedPulse);

            _ringRenderer.transform.localScale = _ringBaseScale * ringScaleMultiplier;
            _coreRenderer.transform.localScale = _coreBaseScale * coreScaleMultiplier;
            _beamRenderer.transform.localScale = new Vector3(
                _beamBaseScale.x,
                _beamBaseScale.y * beamScaleMultiplier,
                _beamBaseScale.z);
            _underlineRenderer.transform.localScale = _underlineBaseScale * Mathf.Lerp(0.92f, 1.08f, normalizedPulse);

            Color ringColor = new(
                _accentColor.r,
                _accentColor.g,
                _accentColor.b,
                _isHighValue
                    ? Mathf.Lerp(0.24f, 0.46f, normalizedPulse)
                    : Mathf.Lerp(0.16f, 0.3f, normalizedPulse));
            Color beamColor = new(
                _accentColor.r,
                _accentColor.g,
                _accentColor.b,
                _isHighValue
                    ? Mathf.Lerp(0.22f, 0.38f, normalizedPulse)
                    : Mathf.Lerp(0.12f, 0.24f, normalizedPulse));
            Color underlineColor = new(
                _accentColor.r,
                _accentColor.g,
                _accentColor.b,
                _isHighValue
                    ? Mathf.Lerp(0.62f, 0.84f, normalizedPulse)
                    : Mathf.Lerp(0.44f, 0.68f, normalizedPulse));

            _ringRenderer.color = ringColor;
            _beamRenderer.color = beamColor;
            _underlineRenderer.color = underlineColor;
            _coreRenderer.color = Color.Lerp(_accentColor, Color.white, _isHighValue ? 0.52f : 0.34f);
        }

        private void SetMarkerVisible(bool visible)
        {
            if (_markerRoot == null)
            {
                return;
            }

            _markerRoot.gameObject.SetActive(visible);

            if (visible)
            {
                _markerRoot.localPosition = _rootBaseLocalPosition;
            }
        }

        private void TriggerSweepCollection()
        {
            RoomRewardPickupTracker sourceTracker = roomRewardPickupTracker;

            if (sourceTracker == null || !sourceTracker.TracksRoomReward || sourceTracker.SourceRoom == null)
            {
                return;
            }

            Vector3 origin = transform.position;
            float sweepRadius = Mathf.Max(chainTriggerRadius, _isHighValue ? chainTriggerRadius * 1.24f : chainTriggerRadius);

            for (int i = 0; i < s_ActivePresentations.Count; i++)
            {
                MomentumRewardPickupPresentation candidate = s_ActivePresentations[i];

                if (candidate == null
                    || candidate == this
                    || !candidate._isConfigured
                    || candidate.roomRewardPickupTracker == null
                    || !candidate.roomRewardPickupTracker.TracksRoomReward
                    || candidate.roomRewardPickupTracker.SourceRoom != sourceTracker.SourceRoom)
                {
                    continue;
                }

                if ((candidate.transform.position - origin).sqrMagnitude > sweepRadius * sweepRadius)
                {
                    continue;
                }

                candidate.PrimeSweep(_isHighValue);
            }
        }

        private void PrimeSweep(bool fromHighValueSource)
        {
            if (!_isConfigured)
            {
                return;
            }

            float duration = fromHighValueSource ? sweepDuration * 1.2f : sweepDuration;
            _sweepExpiresAt = Mathf.Max(_sweepExpiresAt, Time.time + duration);
            _proximityPulse = Mathf.Max(_proximityPulse, fromHighValueSource ? 0.62f : 0.38f);
        }

        private static Sprite GetWhiteSprite()
        {
            if (s_WhiteSprite != null)
            {
                return s_WhiteSprite;
            }

            s_WhiteSprite = Sprite.Create(
                Texture2D.whiteTexture,
                new Rect(0f, 0f, Texture2D.whiteTexture.width, Texture2D.whiteTexture.height),
                new Vector2(0.5f, 0.5f),
                100f);
            return s_WhiteSprite;
        }

        private static Sprite GetCircleSprite()
        {
            if (s_CircleSprite != null)
            {
                return s_CircleSprite;
            }

            const int size = 48;
            Texture2D texture = new(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                name = "RuntimeMomentumRewardCircle"
            };

            Vector2 center = new((size - 1) * 0.5f, (size - 1) * 0.5f);
            float radius = size * 0.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float normalizedDistance = Vector2.Distance(new Vector2(x, y), center) / radius;
                    float alpha = Mathf.Clamp01(1f - normalizedDistance);
                    alpha = Mathf.SmoothStep(0f, 1f, alpha);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply();
            s_CircleSprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f),
                100f);
            return s_CircleSprite;
        }
    }
}
