using CuteIssac.Dungeon;
using CuteIssac.Player;
using UnityEngine;

namespace CuteIssac.Room
{
    public enum TraversalDoorGuidanceStyle
    {
        Guided = 0,
        GuidedClean = 1,
        GuidedRisk = 2,
        RiskWarning = 3,
        Portal = 4
    }

    /// <summary>
    /// Runtime-only traversal marker so exit guidance can be swapped to authored VFX later without changing flow logic.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TraversalGuidanceBeacon : MonoBehaviour
    {
        [SerializeField] private Vector3 markerOffset = new(0f, 1.02f, 0f);
        [SerializeField] private Vector3 thresholdOffset = new(0f, 0.42f, 0f);
        [SerializeField] [Min(0f)] private float bobAmplitude = 0.08f;
        [SerializeField] [Min(0.05f)] private float bobFrequency = 2.2f;
        [SerializeField] [Min(0.05f)] private float pulseFrequency = 3.6f;
        [SerializeField] [Min(0.5f)] private float proximityNearDistance = 1.95f;
        [SerializeField] [Min(1f)] private float proximityFarDistance = 6.2f;
        [SerializeField] [Range(0.05f, 1f)] private float entryFlareTriggerThreshold = 0.82f;
        [SerializeField] [Min(0.05f)] private float entryFlareDecaySpeed = 2.7f;
        [SerializeField] [Min(0f)] private float entryFlareCooldown = 0.42f;
        [SerializeField] [Range(0.1f, 1f)] private float commitCueTriggerThreshold = 0.94f;
        [SerializeField] [Min(0.05f)] private float commitCueDecaySpeed = 4.8f;
        [SerializeField] [Min(0f)] private float commitCueCooldown = 0.34f;

        private static readonly Color WarningAccent = new(1f, 0.34f, 0.24f, 1f);
        private static Sprite s_WhiteSprite;
        private static Sprite s_CircleSprite;

        private Transform _markerRoot;
        private Transform _arrowRoot;
        private Transform _thresholdRoot;
        private SpriteRenderer _ringRenderer;
        private SpriteRenderer _coreRenderer;
        private SpriteRenderer _beamRenderer;
        private SpriteRenderer _chevronA;
        private SpriteRenderer _chevronB;
        private SpriteRenderer _thresholdBandRenderer;
        private SpriteRenderer _thresholdGlowRenderer;
        private SpriteRenderer _thresholdTickA;
        private SpriteRenderer _thresholdTickB;
        private SpriteRenderer _thresholdTickC;
        private SpriteRenderer _thresholdFlareRenderer;
        private Vector3 _baseLocalPosition;
        private Vector3 _thresholdBaseLocalPosition;
        private Vector3 _thresholdCurrentLocalPosition;
        private Vector3 _ringBaseScale = Vector3.one;
        private Vector3 _beamBaseScale = Vector3.one;
        private Vector3 _coreBaseScale = Vector3.one;
        private Vector3 _thresholdBandBaseScale = Vector3.one;
        private Vector3 _thresholdGlowBaseScale = Vector3.one;
        private Vector3 _thresholdFlareBaseScale = Vector3.one;
        private Color _accentColor = new(1f, 0.84f, 0.36f, 1f);
        private float _expiresAt;
        private float _proximityFactor;
        private float _entryFlare;
        private float _commitCue;
        private float _commitAcceptPulse;
        private float _commitRejectPulse;
        private float _lastEntryFlareTimestamp = float.NegativeInfinity;
        private float _lastCommitCueTimestamp = float.NegativeInfinity;
        private bool _active;
        private bool _wasInsideEntryFlareRange;
        private bool _wasInsideCommitCueRange;
        private TraversalDoorGuidanceStyle _style;
        private Transform _playerTransform;

        private void Awake()
        {
            EnsureMarker();
            Hide();
        }

        private void OnEnable()
        {
            EnsureMarker();
            TryResolvePlayerTransform();
        }

        private void Update()
        {
            if (!_active)
            {
                return;
            }

            if (_expiresAt > 0f && Time.unscaledTime >= _expiresAt)
            {
                Hide();
                return;
            }

            UpdatePlayerProximityState();
            float pulse = 0.5f + (Mathf.Sin(Time.unscaledTime * pulseFrequency) * 0.5f);
            float bob = Mathf.Sin(Time.unscaledTime * bobFrequency) * bobAmplitude;
            UpdateMarkerVisual(pulse, bob);
            UpdateThresholdVisual(pulse);
        }

        public void ShowDoorGuidance(Color accentColor, RoomDirection direction, float duration, TraversalDoorGuidanceStyle style = TraversalDoorGuidanceStyle.Guided)
        {
            EnsureMarker();
            _accentColor = ResolveAccentColor(accentColor);
            _style = style is TraversalDoorGuidanceStyle.Portal
                ? TraversalDoorGuidanceStyle.Guided
                : style;
            _expiresAt = duration > 0f ? Time.unscaledTime + duration : 0f;
            ConfigureDoorDirection(direction);
            SetActive(true);
        }

        public void ShowDoorRiskWarning(Color accentColor, RoomDirection direction, float duration)
        {
            EnsureMarker();
            _accentColor = ResolveAccentColor(accentColor);
            _style = TraversalDoorGuidanceStyle.RiskWarning;
            _expiresAt = duration > 0f ? Time.unscaledTime + duration : 0f;
            ConfigureDoorDirection(direction);
            SetActive(true);
        }

        public void ShowPortalGuidance(Color accentColor, float duration = 0f)
        {
            EnsureMarker();
            _accentColor = ResolveAccentColor(accentColor);
            _style = TraversalDoorGuidanceStyle.Portal;
            _expiresAt = duration > 0f ? Time.unscaledTime + duration : 0f;
            _arrowRoot.localRotation = Quaternion.identity;
            _thresholdRoot.localRotation = Quaternion.identity;
            _thresholdCurrentLocalPosition = _thresholdBaseLocalPosition;
            _thresholdRoot.localPosition = _thresholdCurrentLocalPosition;
            SetActive(true);
        }

        public void PlayCommitAccepted()
        {
            _commitAcceptPulse = 1f;
            _commitRejectPulse = 0f;
            _commitCue = Mathf.Max(_commitCue, 1f);

            if (ShouldTriggerEntryFlare())
            {
                _entryFlare = Mathf.Max(_entryFlare, 1f);
            }
        }

        public void PlayCommitRejected()
        {
            _commitRejectPulse = 1f;
            _commitAcceptPulse = 0f;
            _commitCue = Mathf.Max(_commitCue, 0.92f);
            _entryFlare = Mathf.Min(_entryFlare, 0.12f);
        }

        public void Hide()
        {
            _active = false;
            _expiresAt = 0f;
            _style = TraversalDoorGuidanceStyle.Guided;
            _proximityFactor = 0f;
            _entryFlare = 0f;
            _commitCue = 0f;
            _commitAcceptPulse = 0f;
            _commitRejectPulse = 0f;
            _wasInsideEntryFlareRange = false;
            _wasInsideCommitCueRange = false;

            if (_markerRoot != null)
            {
                _markerRoot.gameObject.SetActive(false);
            }

            if (_thresholdRoot != null)
            {
                _thresholdRoot.gameObject.SetActive(false);
            }
        }

        private void EnsureMarker()
        {
            if (_markerRoot != null)
            {
                return;
            }

            GameObject markerRootObject = new("TraversalGuidanceBeacon");
            markerRootObject.layer = gameObject.layer;
            _markerRoot = markerRootObject.transform;
            _markerRoot.SetParent(transform, false);
            _baseLocalPosition = markerOffset;
            _markerRoot.localPosition = _baseLocalPosition;

            _ringRenderer = CreatePart("Ring", _markerRoot, GetCircleSprite(), Vector3.zero, new Vector3(0.58f, 0.58f, 1f), new Color(1f, 1f, 1f, 0.22f), 10);
            _coreRenderer = CreatePart("Core", _markerRoot, GetCircleSprite(), Vector3.zero, new Vector3(0.16f, 0.16f, 1f), Color.white, 12);
            _beamRenderer = CreatePart("Beam", _markerRoot, GetWhiteSprite(), new Vector3(0f, 0.26f, 0f), new Vector3(0.06f, 0.44f, 1f), new Color(1f, 1f, 1f, 0.2f), 9);

            GameObject arrowRootObject = new("ArrowRoot");
            arrowRootObject.layer = gameObject.layer;
            _arrowRoot = arrowRootObject.transform;
            _arrowRoot.SetParent(_markerRoot, false);

            _chevronA = CreatePart("ChevronA", _arrowRoot, GetWhiteSprite(), new Vector3(0f, 0.18f, 0f), new Vector3(0.18f, 0.05f, 1f), Color.white, 11, 36f);
            _chevronB = CreatePart("ChevronB", _arrowRoot, GetWhiteSprite(), new Vector3(0f, 0.06f, 0f), new Vector3(0.14f, 0.045f, 1f), Color.white, 11, 36f);

            GameObject thresholdRootObject = new("ThresholdRoot");
            thresholdRootObject.layer = gameObject.layer;
            _thresholdRoot = thresholdRootObject.transform;
            _thresholdRoot.SetParent(transform, false);
            _thresholdBaseLocalPosition = thresholdOffset;
            _thresholdCurrentLocalPosition = _thresholdBaseLocalPosition;
            _thresholdRoot.localPosition = _thresholdCurrentLocalPosition;

            _thresholdGlowRenderer = CreatePart("ThresholdGlow", _thresholdRoot, GetCircleSprite(), Vector3.zero, new Vector3(0.88f, 0.28f, 1f), new Color(1f, 1f, 1f, 0.16f), 7);
            _thresholdBandRenderer = CreatePart("ThresholdBand", _thresholdRoot, GetWhiteSprite(), Vector3.zero, new Vector3(0.82f, 0.08f, 1f), new Color(1f, 1f, 1f, 0.26f), 8);
            _thresholdTickA = CreatePart("ThresholdTickA", _thresholdRoot, GetWhiteSprite(), new Vector3(-0.22f, 0.1f, 0f), new Vector3(0.14f, 0.04f, 1f), new Color(1f, 1f, 1f, 0.42f), 9, 34f);
            _thresholdTickB = CreatePart("ThresholdTickB", _thresholdRoot, GetWhiteSprite(), new Vector3(0f, 0.1f, 0f), new Vector3(0.14f, 0.04f, 1f), new Color(1f, 1f, 1f, 0.42f), 9, 34f);
            _thresholdTickC = CreatePart("ThresholdTickC", _thresholdRoot, GetWhiteSprite(), new Vector3(0.22f, 0.1f, 0f), new Vector3(0.14f, 0.04f, 1f), new Color(1f, 1f, 1f, 0.42f), 9, 34f);
            _thresholdFlareRenderer = CreatePart("ThresholdFlare", _thresholdRoot, GetCircleSprite(), Vector3.zero, new Vector3(0.42f, 0.16f, 1f), new Color(1f, 1f, 1f, 0f), 10);

            _ringBaseScale = _ringRenderer.transform.localScale;
            _beamBaseScale = _beamRenderer.transform.localScale;
            _coreBaseScale = _coreRenderer.transform.localScale;
            _thresholdBandBaseScale = _thresholdBandRenderer.transform.localScale;
            _thresholdGlowBaseScale = _thresholdGlowRenderer.transform.localScale;
            _thresholdFlareBaseScale = _thresholdFlareRenderer.transform.localScale;
            Hide();
        }

        private void SetActive(bool active)
        {
            _active = active;

            if (!active)
            {
                Hide();
                return;
            }

            bool showMarker = _style != TraversalDoorGuidanceStyle.RiskWarning;
            bool showThreshold = _style != TraversalDoorGuidanceStyle.Portal;

            if (_markerRoot != null)
            {
                _markerRoot.gameObject.SetActive(showMarker);
                _markerRoot.localPosition = _baseLocalPosition;
            }

            if (_thresholdRoot != null)
            {
                _thresholdRoot.gameObject.SetActive(showThreshold);
                _thresholdRoot.localPosition = _thresholdCurrentLocalPosition;
            }
        }

        private void ConfigureDoorDirection(RoomDirection direction)
        {
            if (_arrowRoot == null || _thresholdRoot == null)
            {
                return;
            }

            _arrowRoot.localRotation = Quaternion.Euler(0f, 0f, ResolveDirectionRotation(direction));
            _thresholdRoot.localRotation = Quaternion.Euler(0f, 0f, ResolveThresholdRotation(direction));
            _thresholdCurrentLocalPosition = ResolveThresholdOffset(direction);
            _thresholdRoot.localPosition = _thresholdCurrentLocalPosition;
        }

        private void UpdatePlayerProximityState()
        {
            if (_style == TraversalDoorGuidanceStyle.Portal)
            {
                _proximityFactor = Mathf.MoveTowards(_proximityFactor, 0f, Time.unscaledDeltaTime * 4.4f);
                _entryFlare = Mathf.MoveTowards(_entryFlare, 0f, Time.unscaledDeltaTime * entryFlareDecaySpeed);
                _commitCue = Mathf.MoveTowards(_commitCue, 0f, Time.unscaledDeltaTime * commitCueDecaySpeed);
                _commitAcceptPulse = Mathf.MoveTowards(_commitAcceptPulse, 0f, Time.unscaledDeltaTime * (commitCueDecaySpeed * 1.2f));
                _commitRejectPulse = Mathf.MoveTowards(_commitRejectPulse, 0f, Time.unscaledDeltaTime * (commitCueDecaySpeed * 1.45f));
                _wasInsideEntryFlareRange = false;
                _wasInsideCommitCueRange = false;
                return;
            }

            TryResolvePlayerTransform();

            float targetProximity = 0f;
            if (_playerTransform != null)
            {
                float resolvedFarDistance = Mathf.Max(proximityNearDistance + 0.01f, proximityFarDistance);
                float distance = Vector2.Distance(_playerTransform.position, transform.position);
                targetProximity = 1f - Mathf.InverseLerp(proximityNearDistance, resolvedFarDistance, distance);
            }

            _proximityFactor = Mathf.MoveTowards(
                _proximityFactor,
                Mathf.Clamp01(targetProximity),
                Time.unscaledDeltaTime * (targetProximity > _proximityFactor ? 5.2f : 3.6f));

            bool insideEntryFlareRange = _proximityFactor >= entryFlareTriggerThreshold;
            if (insideEntryFlareRange
                && !_wasInsideEntryFlareRange
                && ShouldTriggerEntryFlare()
                && Time.unscaledTime - _lastEntryFlareTimestamp >= entryFlareCooldown)
            {
                _entryFlare = 1f;
                _lastEntryFlareTimestamp = Time.unscaledTime;
            }

            bool insideCommitCueRange = _proximityFactor >= commitCueTriggerThreshold;
            if (insideCommitCueRange
                && !_wasInsideCommitCueRange
                && ShouldTriggerCommitCue()
                && Time.unscaledTime - _lastCommitCueTimestamp >= commitCueCooldown)
            {
                _commitCue = 1f;
                _lastCommitCueTimestamp = Time.unscaledTime;

                if (ShouldTriggerEntryFlare())
                {
                    _entryFlare = Mathf.Max(_entryFlare, 0.82f);
                }
            }

            _wasInsideEntryFlareRange = insideEntryFlareRange;
            _wasInsideCommitCueRange = insideCommitCueRange;
            _entryFlare = Mathf.MoveTowards(_entryFlare, 0f, Time.unscaledDeltaTime * entryFlareDecaySpeed);
            _commitCue = Mathf.MoveTowards(_commitCue, 0f, Time.unscaledDeltaTime * commitCueDecaySpeed);
            _commitAcceptPulse = Mathf.MoveTowards(_commitAcceptPulse, 0f, Time.unscaledDeltaTime * (commitCueDecaySpeed * 1.2f));
            _commitRejectPulse = Mathf.MoveTowards(_commitRejectPulse, 0f, Time.unscaledDeltaTime * (commitCueDecaySpeed * 1.45f));
        }

        private bool ShouldTriggerEntryFlare()
        {
            return _style == TraversalDoorGuidanceStyle.Guided
                || _style == TraversalDoorGuidanceStyle.GuidedClean;
        }

        private bool ShouldTriggerCommitCue()
        {
            return _style != TraversalDoorGuidanceStyle.Portal;
        }

        private void TryResolvePlayerTransform()
        {
            if (_playerTransform != null)
            {
                return;
            }

            PlayerController playerController = PlayerRegistry.ActiveController != null
                ? PlayerRegistry.ActiveController
                : FindFirstObjectByType<PlayerController>(FindObjectsInactive.Exclude);
            _playerTransform = playerController != null ? playerController.transform : null;
        }

        private void UpdateMarkerVisual(float pulse, float bob)
        {
            if (_markerRoot == null || !_markerRoot.gameObject.activeSelf)
            {
                return;
            }

            _markerRoot.localPosition = _baseLocalPosition + new Vector3(0f, bob, 0f);

            bool isPortal = _style == TraversalDoorGuidanceStyle.Portal;
            bool isRisk = _style == TraversalDoorGuidanceStyle.GuidedRisk;
            bool isClean = _style == TraversalDoorGuidanceStyle.GuidedClean;
            float proximityBoost = _proximityFactor;
            float flareBoost = _entryFlare;
            float commitBoost = _commitCue;
            float acceptBoost = _commitAcceptPulse;
            float rejectBoost = _commitRejectPulse;
            Color markerAccent = ResolveOutcomeAccent(ResolveStyleAccent(), acceptBoost, rejectBoost);
            Color brightAccent = Color.Lerp(markerAccent, Color.white, isPortal ? 0.34f : isRisk ? 0.28f : 0.22f);

            _ringRenderer.transform.localScale = _ringBaseScale * Mathf.Lerp(
                isClean ? 0.98f : 0.92f,
                isRisk
                    ? 1.2f + (proximityBoost * 0.12f) + (commitBoost * 0.1f) - (rejectBoost * 0.08f)
                    : 1.14f + (flareBoost * 0.18f) + (commitBoost * 0.24f) + (acceptBoost * 0.16f) - (rejectBoost * 0.06f),
                pulse);
            _coreRenderer.transform.localScale = _coreBaseScale * Mathf.Lerp(
                0.94f,
                isRisk
                    ? 1.12f + (proximityBoost * 0.08f) + (commitBoost * 0.08f) - (rejectBoost * 0.04f)
                    : 1.08f + (flareBoost * 0.24f) + (commitBoost * 0.28f) + (acceptBoost * 0.18f),
                pulse);
            _beamRenderer.transform.localScale = new Vector3(
                _beamBaseScale.x,
                _beamBaseScale.y * (isPortal
                    ? Mathf.Lerp(0.96f, 1.28f, pulse)
                    : Mathf.Lerp(
                        isClean ? 0.88f : 0.82f,
                        isRisk
                            ? 1.14f + (proximityBoost * 0.16f) + (commitBoost * 0.1f) - (rejectBoost * 0.08f)
                            : 1.06f + (flareBoost * 0.22f) + (commitBoost * 0.3f) + (acceptBoost * 0.18f),
                        pulse)),
                _beamBaseScale.z);

            _ringRenderer.color = new Color(
                markerAccent.r,
                markerAccent.g,
                markerAccent.b,
                Mathf.Lerp(isRisk ? 0.26f : 0.18f, (isRisk ? 0.44f : 0.34f) + (proximityBoost * 0.12f) + (flareBoost * 0.1f) + (commitBoost * 0.14f) + (acceptBoost * 0.08f) + (rejectBoost * 0.12f), pulse));
            _coreRenderer.color = Color.Lerp(markerAccent, Color.white, isPortal ? 0.54f : isRisk ? 0.42f + (proximityBoost * 0.12f) + (commitBoost * 0.08f) + (rejectBoost * 0.06f) : 0.36f + (flareBoost * 0.18f) + (commitBoost * 0.24f) + (acceptBoost * 0.2f));
            _beamRenderer.color = new Color(
                brightAccent.r,
                brightAccent.g,
                brightAccent.b,
                isPortal
                    ? Mathf.Lerp(0.24f, 0.46f, pulse)
                    : Mathf.Lerp(
                        isRisk ? 0.24f : 0.16f,
                        (isRisk ? 0.34f : 0.28f) + (proximityBoost * 0.12f) + (flareBoost * 0.1f) + (commitBoost * 0.16f) + (acceptBoost * 0.12f) + (rejectBoost * 0.14f),
                        pulse));

            bool showChevrons = !isPortal;
            _chevronA.enabled = showChevrons;
            _chevronB.enabled = showChevrons;

            if (showChevrons)
            {
                _chevronA.color = new Color(brightAccent.r, brightAccent.g, brightAccent.b, Mathf.Lerp(isRisk ? 0.7f : 0.64f, (isRisk ? 0.98f : 0.92f) + (proximityBoost * 0.1f) + (commitBoost * 0.12f) + (acceptBoost * 0.1f), pulse));
                _chevronB.color = new Color(brightAccent.r, brightAccent.g, brightAccent.b, Mathf.Lerp(isRisk ? 0.58f : 0.48f, (isRisk ? 0.88f : 0.82f) + (proximityBoost * 0.08f) + (commitBoost * 0.1f) + (acceptBoost * 0.08f), pulse));
            }
        }

        private void UpdateThresholdVisual(float pulse)
        {
            if (_thresholdRoot == null || !_thresholdRoot.gameObject.activeSelf)
            {
                return;
            }

            Color thresholdAccent = ResolveStyleAccent();
            bool isRisk = _style == TraversalDoorGuidanceStyle.GuidedRisk || _style == TraversalDoorGuidanceStyle.RiskWarning;
            bool riskOnly = _style == TraversalDoorGuidanceStyle.RiskWarning;
            bool isClean = _style == TraversalDoorGuidanceStyle.GuidedClean;
            float proximityBoost = _proximityFactor;
            float flareBoost = _entryFlare;
            float commitBoost = _commitCue;
            float acceptBoost = _commitAcceptPulse;
            float rejectBoost = _commitRejectPulse;
            float scalePulse = isRisk
                ? Mathf.Lerp(0.96f, 1.08f + (proximityBoost * 0.08f) + (commitBoost * 0.12f) - (rejectBoost * 0.08f), pulse)
                : isClean
                    ? Mathf.Lerp(0.98f, 1.04f + (flareBoost * 0.08f) + (commitBoost * 0.16f) + (acceptBoost * 0.14f), pulse)
                    : Mathf.Lerp(0.97f, 1.03f + (flareBoost * 0.05f) + (commitBoost * 0.12f) + (acceptBoost * 0.08f), pulse);

            _thresholdGlowRenderer.transform.localScale = _thresholdGlowBaseScale * Mathf.Lerp(
                scalePulse,
                scalePulse * (isRisk
                    ? 1.12f + (proximityBoost * 0.14f) + (commitBoost * 0.18f) + (rejectBoost * 0.12f)
                    : 1.08f + (flareBoost * 0.18f) + (commitBoost * 0.24f) + (acceptBoost * 0.18f)),
                isRisk ? pulse : pulse * 0.6f);
            _thresholdBandRenderer.transform.localScale = Vector3.Scale(
                _thresholdBandBaseScale,
                new Vector3(
                    scalePulse * (isRisk
                        ? 1f + (commitBoost * 0.1f) - (rejectBoost * 0.06f)
                        : 1f + (commitBoost * 0.16f) + (acceptBoost * 0.12f)),
                    isRisk
                        ? Mathf.Lerp(1f, 0.76f - (rejectBoost * 0.12f), commitBoost)
                        : Mathf.Lerp(1f, 1.18f + (acceptBoost * 0.08f), commitBoost),
                    1f));
            _thresholdRoot.localScale = Vector3.one * (riskOnly
                ? Mathf.Lerp(0.98f, 1.04f + (proximityBoost * 0.08f) + (commitBoost * 0.12f) + (rejectBoost * 0.08f), pulse)
                : 1f + (flareBoost * 0.05f) + (commitBoost * 0.08f) + (acceptBoost * 0.06f));

            _thresholdGlowRenderer.color = new Color(
                thresholdAccent.r,
                thresholdAccent.g,
                thresholdAccent.b,
                Mathf.Lerp(
                    riskOnly ? 0.1f : 0.08f,
                    (isRisk ? 0.32f : 0.22f) + (proximityBoost * 0.16f) + (flareBoost * 0.12f) + (commitBoost * 0.18f) + (acceptBoost * 0.08f) + (rejectBoost * 0.16f),
                    pulse));

            _thresholdBandRenderer.color = new Color(
                thresholdAccent.r,
                thresholdAccent.g,
                thresholdAccent.b,
                Mathf.Lerp(
                    isRisk ? 0.34f : isClean ? 0.28f : 0.22f,
                    (isRisk ? 0.88f : isClean ? 0.64f : 0.48f) + (proximityBoost * 0.16f) + (flareBoost * 0.18f) + (commitBoost * 0.2f) + (acceptBoost * 0.14f) + (rejectBoost * 0.18f),
                    pulse));

            bool showTicks = isRisk || riskOnly || _style == TraversalDoorGuidanceStyle.Guided;
            SetThresholdTickVisual(_thresholdTickA, thresholdAccent, showTicks ? Mathf.Lerp(riskOnly ? 0.42f : 0.28f, (isRisk ? 0.92f : 0.56f) + (proximityBoost * 0.18f) + (commitBoost * 0.22f) + (rejectBoost * 0.12f), pulse) : 0f, 1f + (commitBoost * 0.18f) + (acceptBoost * 0.06f));
            SetThresholdTickVisual(_thresholdTickB, thresholdAccent, showTicks ? Mathf.Lerp(riskOnly ? 0.48f : 0.32f, (isRisk ? 1f : 0.64f) + (proximityBoost * 0.18f) + (commitBoost * 0.26f) + (rejectBoost * 0.12f), pulse) : 0f, 1.08f + (commitBoost * 0.22f) + (acceptBoost * 0.08f));
            SetThresholdTickVisual(_thresholdTickC, thresholdAccent, showTicks ? Mathf.Lerp(riskOnly ? 0.42f : 0.28f, (isRisk ? 0.92f : 0.56f) + (proximityBoost * 0.18f) + (commitBoost * 0.22f) + (rejectBoost * 0.12f), pulse) : 0f, 1f + (commitBoost * 0.18f) + (acceptBoost * 0.06f));

            if (_thresholdFlareRenderer != null)
            {
                float flareAlpha = ShouldTriggerEntryFlare()
                    ? flareBoost * Mathf.Lerp(0.28f, 0.82f + (commitBoost * 0.12f) + (acceptBoost * 0.14f), Mathf.Max(proximityBoost, pulse))
                    : isRisk
                        ? Mathf.Max(commitBoost, rejectBoost) * Mathf.Lerp(0.12f, 0.36f + (rejectBoost * 0.16f), Mathf.Max(proximityBoost, pulse))
                        : 0f;
                _thresholdFlareRenderer.enabled = flareAlpha > 0.01f;
                _thresholdFlareRenderer.transform.localScale = Vector3.Scale(
                    _thresholdFlareBaseScale,
                    new Vector3(
                        Mathf.Lerp(1f, isRisk ? 1.48f - (rejectBoost * 0.12f) : 2.1f + (acceptBoost * 0.16f), Mathf.Max(flareBoost, commitBoost, acceptBoost, rejectBoost)),
                        Mathf.Lerp(1f, isRisk ? 1.18f - (rejectBoost * 0.08f) : 1.7f + (acceptBoost * 0.12f), Mathf.Max(flareBoost, commitBoost, acceptBoost, rejectBoost)),
                        1f));
                _thresholdFlareRenderer.color = new Color(
                    thresholdAccent.r,
                    thresholdAccent.g,
                    thresholdAccent.b,
                    flareAlpha);
            }
        }

        private static void SetThresholdTickVisual(SpriteRenderer renderer, Color accentColor, float alpha, float scaleMultiplier)
        {
            if (renderer == null)
            {
                return;
            }

            renderer.enabled = alpha > 0.01f;
            renderer.color = new Color(accentColor.r, accentColor.g, accentColor.b, alpha);
            renderer.transform.localScale = new Vector3(0.14f * scaleMultiplier, 0.04f * scaleMultiplier, 1f);
        }

        private Color ResolveStyleAccent()
        {
            return _style switch
            {
                TraversalDoorGuidanceStyle.GuidedClean => Color.Lerp(_accentColor, Color.white, 0.14f),
                TraversalDoorGuidanceStyle.GuidedRisk => Color.Lerp(_accentColor, WarningAccent, 0.68f),
                TraversalDoorGuidanceStyle.RiskWarning => WarningAccent,
                TraversalDoorGuidanceStyle.Portal => _accentColor,
                _ => _accentColor
            };
        }

        private static Color ResolveOutcomeAccent(Color baseAccent, float acceptBoost, float rejectBoost)
        {
            Color acceptedAccent = Color.Lerp(baseAccent, Color.white, Mathf.Clamp01(acceptBoost * 0.34f));
            return Color.Lerp(acceptedAccent, WarningAccent, Mathf.Clamp01(rejectBoost * 0.72f));
        }

        private SpriteRenderer CreatePart(
            string name,
            Transform parent,
            Sprite sprite,
            Vector3 localPosition,
            Vector3 localScale,
            Color color,
            int sortingOrder,
            float rotationZ = 0f)
        {
            GameObject child = new(name);
            child.layer = gameObject.layer;
            child.transform.SetParent(parent, false);
            child.transform.localPosition = localPosition;
            child.transform.localScale = localScale;
            child.transform.localRotation = Quaternion.Euler(0f, 0f, rotationZ);

            SpriteRenderer renderer = child.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
            return renderer;
        }

        private static Vector3 ResolveThresholdOffset(RoomDirection direction)
        {
            return direction switch
            {
                RoomDirection.Up => new Vector3(0f, 0.48f, 0f),
                RoomDirection.Right => new Vector3(0.62f, 0f, 0f),
                RoomDirection.Down => new Vector3(0f, -0.42f, 0f),
                RoomDirection.Left => new Vector3(-0.62f, 0f, 0f),
                _ => Vector3.zero
            };
        }

        private static float ResolveThresholdRotation(RoomDirection direction)
        {
            return direction switch
            {
                RoomDirection.Left => 90f,
                RoomDirection.Right => 90f,
                _ => 0f
            };
        }

        private static float ResolveDirectionRotation(RoomDirection direction)
        {
            return direction switch
            {
                RoomDirection.Up => 0f,
                RoomDirection.Right => -90f,
                RoomDirection.Down => 180f,
                RoomDirection.Left => 90f,
                _ => 0f
            };
        }

        private static Color ResolveAccentColor(Color accentColor)
        {
            return accentColor.a > 0.01f
                ? new Color(accentColor.r, accentColor.g, accentColor.b, 1f)
                : new Color(1f, 0.84f, 0.36f, 1f);
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
                name = "RuntimeTraversalGuidanceCircle"
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
