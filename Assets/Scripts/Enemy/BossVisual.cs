using UnityEngine;

namespace CuteIssac.Enemy
{
    /// <summary>
    /// Boss-specific presentation layer.
    /// EnemyVisual still owns the baseline sprite feedback; this component adds telegraphs, aura, and pattern emphasis.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BossVisual : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Base enemy visual used by the boss root.")]
        [SerializeField] private EnemyVisual enemyVisual;
        [Tooltip("Optional renderer used for phase aura or boss emphasis.")]
        [SerializeField] private SpriteRenderer auraRenderer;
        [Tooltip("Optional renderer used for attack telegraphs.")]
        [SerializeField] private SpriteRenderer telegraphRenderer;

        [Header("Colors")]
        [SerializeField] private Color idleAuraColor = new(1f, 0.35f, 0.2f, 0.15f);
        [SerializeField] private Color phaseTwoAuraColor = new(1f, 0.5f, 0.18f, 0.2f);
        [SerializeField] private Color phaseThreeAuraColor = new(1f, 0.16f, 0.16f, 0.28f);
        [SerializeField] private Color enragedAuraColor = new(1f, 0.08f, 0.08f, 0.35f);
        [SerializeField] private Color burstTelegraphColor = new(1f, 0.87f, 0.22f, 0.55f);
        [SerializeField] private Color chargeTelegraphColor = new(1f, 0.18f, 0.18f, 0.62f);
        [SerializeField] private Color volleyTelegraphColor = new(0.45f, 0.92f, 1f, 0.56f);
        [SerializeField] private Color sweepTelegraphColor = new(0.8f, 0.46f, 1f, 0.58f);
        [SerializeField] private Color spiralTelegraphColor = new(0.54f, 1f, 0.42f, 0.62f);
        [SerializeField] private Color fanTelegraphColor = new(1f, 0.68f, 0.24f, 0.62f);
        [SerializeField] private Color shockwaveTelegraphColor = new(1f, 0.4f, 0.18f, 0.66f);
        [SerializeField] private Color crossfireTelegraphColor = new(1f, 0.52f, 0.3f, 0.68f);

        [Header("Scales")]
        [SerializeField] private Vector3 idleAuraScale = new(1.4f, 1.4f, 1f);
        [SerializeField] private Vector3 phaseTwoAuraScale = new(1.55f, 1.55f, 1f);
        [SerializeField] private Vector3 phaseThreeAuraScale = new(1.72f, 1.72f, 1f);
        [SerializeField] private Vector3 enragedAuraScale = new(1.7f, 1.7f, 1f);
        [SerializeField] private Vector3 burstTelegraphScale = new(1.55f, 1.55f, 1f);
        [SerializeField] private Vector3 chargeTelegraphScale = new(1.9f, 0.8f, 1f);
        [SerializeField] private Vector3 volleyTelegraphScale = new(2.2f, 1.15f, 1f);
        [SerializeField] private Vector3 sweepTelegraphScale = new(2.45f, 0.95f, 1f);
        [SerializeField] private Vector3 spiralTelegraphScale = new(1.85f, 1.85f, 1f);
        [SerializeField] private Vector3 fanTelegraphScale = new(2.5f, 1.1f, 1f);
        [SerializeField] private Vector3 shockwaveTelegraphScale = new(2.05f, 2.05f, 1f);
        [SerializeField] private Vector3 crossfireTelegraphScale = new(2.3f, 0.92f, 1f);

        [Header("Telegraph Motion")]
        [SerializeField] [Min(0f)] private float burstPulseSpeed = 8f;
        [SerializeField] [Min(0f)] private float burstScalePulse = 0.16f;
        [SerializeField] [Min(0f)] private float chargePulseSpeed = 11f;
        [SerializeField] [Min(0f)] private float chargeStretchPulse = 0.24f;
        [SerializeField] [Min(0f)] private float volleyPulseSpeed = 5.5f;
        [SerializeField] [Min(0f)] private float volleyRotateSpeed = 48f;
        [SerializeField] [Min(0f)] private float volleyWidthPulse = 0.2f;
        [SerializeField] [Min(0f)] private float sweepPulseSpeed = 6.2f;
        [SerializeField] [Min(0f)] private float sweepRotateSpeed = 76f;
        [SerializeField] [Min(0f)] private float sweepWidthPulse = 0.26f;
        [SerializeField] [Min(0f)] private float spiralPulseSpeed = 7.2f;
        [SerializeField] [Min(0f)] private float spiralRotateSpeed = 132f;
        [SerializeField] [Min(0f)] private float spiralScalePulse = 0.22f;
        [SerializeField] [Min(0f)] private float fanPulseSpeed = 6.4f;
        [SerializeField] [Min(0f)] private float fanWidthPulse = 0.24f;
        [SerializeField] [Min(0f)] private float fanRotateSpeed = 28f;
        [SerializeField] [Min(0f)] private float shockwavePulseSpeed = 5.8f;
        [SerializeField] [Min(0f)] private float shockwaveScalePulse = 0.28f;
        [SerializeField] [Min(0f)] private float crossfirePulseSpeed = 7.4f;
        [SerializeField] [Min(0f)] private float crossfireWidthPulse = 0.22f;
        [SerializeField] [Min(0f)] private float crossfireRotateSpeed = 42f;

        [Header("Phase Transition")]
        [SerializeField] private Color phaseTransitionAuraColor = new(1f, 0.88f, 0.32f, 0.48f);
        [SerializeField] [Min(0f)] private float phaseTransitionPulseSpeed = 8.5f;
        [SerializeField] [Min(0f)] private float phaseTransitionAuraScalePulse = 0.18f;
        [SerializeField] [Min(0f)] private float phaseTransitionAlphaBoost = 0.18f;

        public EnemyVisual EnemyVisual => enemyVisual;
        public BossPhaseType CurrentPhase { get; private set; } = BossPhaseType.PhaseOne;
        public bool IsEnraged { get; private set; }

        private BossPatternType _currentTelegraphPattern = BossPatternType.Burst;
        private bool _telegraphActive;
        private Color _currentTelegraphColor;
        private Vector3 _currentTelegraphScale = Vector3.one;
        private Quaternion _telegraphBaseRotation = Quaternion.identity;
        private float _phaseTransitionRemaining;
        private float _phaseTransitionDuration;

        private void Awake()
        {
            ResolveReferences();
            SetPhase(BossPhaseType.PhaseOne);
            SetEnraged(false);
            SetTelegraphActive(false, BossPatternType.Burst);
        }

        private void Update()
        {
            UpdatePhaseTransitionMotion();
            UpdateTelegraphMotion();
        }

        public void HandleAttack()
        {
            if (telegraphRenderer != null && telegraphRenderer.gameObject.activeSelf)
            {
                Color boosted = telegraphRenderer.color;
                boosted.a = Mathf.Min(0.82f, boosted.a + 0.18f);
                telegraphRenderer.color = boosted;
            }
        }

        public void HandleDamaged()
        {
            if (auraRenderer != null)
            {
                auraRenderer.color = new Color(auraRenderer.color.r, auraRenderer.color.g, auraRenderer.color.b, Mathf.Min(0.5f, auraRenderer.color.a + 0.08f));
            }
        }

        public void HandleDied()
        {
            SetTelegraphActive(false, BossPatternType.Burst);
            _phaseTransitionRemaining = 0f;
            _phaseTransitionDuration = 0f;

            if (auraRenderer != null)
            {
                auraRenderer.color = new Color(auraRenderer.color.r, auraRenderer.color.g, auraRenderer.color.b, 0.12f);
            }
        }

        public void BeginPhaseTransition(float duration)
        {
            _phaseTransitionDuration = Mathf.Max(0.01f, duration);
            _phaseTransitionRemaining = Mathf.Max(0f, duration);

            if (_phaseTransitionRemaining <= 0f)
            {
                RefreshAuraState();
            }
        }

        public void SetPhase(BossPhaseType phase)
        {
            CurrentPhase = phase;
            RefreshAuraState();
        }

        public void SetEnraged(bool enraged)
        {
            IsEnraged = enraged;
            RefreshAuraState();
        }

        public void SetTelegraphActive(bool active, BossPatternType patternType)
        {
            if (telegraphRenderer == null)
            {
                return;
            }

            _telegraphActive = active;
            telegraphRenderer.gameObject.SetActive(active);

            if (!active)
            {
                telegraphRenderer.transform.localScale = _currentTelegraphScale;
                telegraphRenderer.transform.localRotation = _telegraphBaseRotation;
                return;
            }

            _currentTelegraphPattern = patternType;

            switch (patternType)
            {
                case BossPatternType.Charge:
                    _currentTelegraphColor = chargeTelegraphColor;
                    _currentTelegraphScale = chargeTelegraphScale;
                    break;
                case BossPatternType.Volley:
                    _currentTelegraphColor = volleyTelegraphColor;
                    _currentTelegraphScale = volleyTelegraphScale;
                    break;
                case BossPatternType.Sweep:
                    _currentTelegraphColor = sweepTelegraphColor;
                    _currentTelegraphScale = sweepTelegraphScale;
                    break;
                case BossPatternType.Spiral:
                    _currentTelegraphColor = spiralTelegraphColor;
                    _currentTelegraphScale = spiralTelegraphScale;
                    break;
                case BossPatternType.Fan:
                    _currentTelegraphColor = fanTelegraphColor;
                    _currentTelegraphScale = fanTelegraphScale;
                    break;
                case BossPatternType.Shockwave:
                    _currentTelegraphColor = shockwaveTelegraphColor;
                    _currentTelegraphScale = shockwaveTelegraphScale;
                    break;
                case BossPatternType.Crossfire:
                    _currentTelegraphColor = crossfireTelegraphColor;
                    _currentTelegraphScale = crossfireTelegraphScale;
                    break;
                default:
                    _currentTelegraphColor = burstTelegraphColor;
                    _currentTelegraphScale = burstTelegraphScale;
                    break;
            }

            _telegraphBaseRotation = Quaternion.identity;
            telegraphRenderer.color = _currentTelegraphColor;
            telegraphRenderer.transform.localScale = _currentTelegraphScale;
            telegraphRenderer.transform.localRotation = _telegraphBaseRotation;
        }

        private void RefreshAuraState()
        {
            if (auraRenderer == null)
            {
                return;
            }

            ResolveBaseAuraState(out Color baseAuraColor, out Vector3 baseAuraScale);

            auraRenderer.color = IsEnraged ? enragedAuraColor : baseAuraColor;
            auraRenderer.transform.localScale = IsEnraged ? enragedAuraScale : baseAuraScale;
            auraRenderer.gameObject.SetActive(true);
        }

        private void ResolveReferences()
        {
            if (enemyVisual == null)
            {
                enemyVisual = GetComponent<EnemyVisual>();
            }
        }

        private void UpdateTelegraphMotion()
        {
            if (!_telegraphActive || telegraphRenderer == null || !telegraphRenderer.gameObject.activeSelf)
            {
                return;
            }

            switch (_currentTelegraphPattern)
            {
                case BossPatternType.Charge:
                    AnimateChargeTelegraph();
                    break;
                case BossPatternType.Volley:
                    AnimateVolleyTelegraph();
                    break;
                case BossPatternType.Sweep:
                    AnimateSweepTelegraph();
                    break;
                case BossPatternType.Spiral:
                    AnimateSpiralTelegraph();
                    break;
                case BossPatternType.Fan:
                    AnimateFanTelegraph();
                    break;
                case BossPatternType.Shockwave:
                    AnimateShockwaveTelegraph();
                    break;
                case BossPatternType.Crossfire:
                    AnimateCrossfireTelegraph();
                    break;
                default:
                    AnimateBurstTelegraph();
                    break;
            }
        }

        private void AnimateBurstTelegraph()
        {
            float pulse = 0.5f + (0.5f * Mathf.Sin(Time.time * burstPulseSpeed));
            float scaleMultiplier = 1f + (burstScalePulse * pulse);
            telegraphRenderer.transform.localScale = _currentTelegraphScale * scaleMultiplier;

            Color color = _currentTelegraphColor;
            color.a = Mathf.Lerp(_currentTelegraphColor.a * 0.62f, _currentTelegraphColor.a, pulse);
            telegraphRenderer.color = color;
            telegraphRenderer.transform.localRotation = _telegraphBaseRotation;
        }

        private void AnimateChargeTelegraph()
        {
            float pulse = 0.5f + (0.5f * Mathf.Sin(Time.time * chargePulseSpeed));
            Vector3 scale = _currentTelegraphScale;
            scale.x *= 1f + (chargeStretchPulse * pulse);
            scale.y *= 1f - (chargeStretchPulse * 0.25f * pulse);
            telegraphRenderer.transform.localScale = scale;

            Color color = _currentTelegraphColor;
            color.a = Mathf.Lerp(_currentTelegraphColor.a * 0.45f, _currentTelegraphColor.a, pulse);
            telegraphRenderer.color = color;
            telegraphRenderer.transform.localRotation = _telegraphBaseRotation;
        }

        private void AnimateVolleyTelegraph()
        {
            float pulse = 0.5f + (0.5f * Mathf.Sin(Time.time * volleyPulseSpeed));
            Vector3 scale = _currentTelegraphScale;
            scale.x *= 1f + (volleyWidthPulse * pulse);
            scale.y *= 1f - (volleyWidthPulse * 0.18f * pulse);
            telegraphRenderer.transform.localScale = scale;

            Color color = _currentTelegraphColor;
            color.a = Mathf.Lerp(_currentTelegraphColor.a * 0.58f, _currentTelegraphColor.a, pulse);
            telegraphRenderer.color = color;
            telegraphRenderer.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.time * (volleyPulseSpeed * 0.6f)) * volleyRotateSpeed);
        }

        private void AnimateSweepTelegraph()
        {
            float pulse = 0.5f + (0.5f * Mathf.Sin(Time.time * sweepPulseSpeed));
            Vector3 scale = _currentTelegraphScale;
            scale.x *= 1f + (sweepWidthPulse * pulse);
            scale.y *= 1f - (sweepWidthPulse * 0.22f * pulse);
            telegraphRenderer.transform.localScale = scale;

            Color color = _currentTelegraphColor;
            color.a = Mathf.Lerp(_currentTelegraphColor.a * 0.54f, _currentTelegraphColor.a, pulse);
            telegraphRenderer.color = color;
            telegraphRenderer.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.time * (sweepPulseSpeed * 0.45f)) * sweepRotateSpeed);
        }

        private void AnimateSpiralTelegraph()
        {
            float pulse = 0.5f + (0.5f * Mathf.Sin(Time.time * spiralPulseSpeed));
            float scaleMultiplier = 1f + (spiralScalePulse * pulse);
            telegraphRenderer.transform.localScale = _currentTelegraphScale * scaleMultiplier;

            Color color = _currentTelegraphColor;
            color.a = Mathf.Lerp(_currentTelegraphColor.a * 0.5f, _currentTelegraphColor.a, pulse);
            telegraphRenderer.color = color;
            telegraphRenderer.transform.localRotation = Quaternion.Euler(0f, 0f, Time.time * spiralRotateSpeed);
        }

        private void AnimateFanTelegraph()
        {
            float pulse = 0.5f + (0.5f * Mathf.Sin(Time.time * fanPulseSpeed));
            Vector3 scale = _currentTelegraphScale;
            scale.x *= 1f + (fanWidthPulse * pulse);
            scale.y *= 1f - (fanWidthPulse * 0.16f * pulse);
            telegraphRenderer.transform.localScale = scale;

            Color color = _currentTelegraphColor;
            color.a = Mathf.Lerp(_currentTelegraphColor.a * 0.52f, _currentTelegraphColor.a, pulse);
            telegraphRenderer.color = color;
            telegraphRenderer.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.time * (fanPulseSpeed * 0.35f)) * fanRotateSpeed);
        }

        private void AnimateShockwaveTelegraph()
        {
            float pulse = 0.5f + (0.5f * Mathf.Sin(Time.time * shockwavePulseSpeed));
            float scaleMultiplier = 1f + (shockwaveScalePulse * pulse);
            telegraphRenderer.transform.localScale = _currentTelegraphScale * scaleMultiplier;

            Color color = _currentTelegraphColor;
            color.a = Mathf.Lerp(_currentTelegraphColor.a * 0.46f, _currentTelegraphColor.a, pulse);
            telegraphRenderer.color = color;
            telegraphRenderer.transform.localRotation = _telegraphBaseRotation;
        }

        private void AnimateCrossfireTelegraph()
        {
            float pulse = 0.5f + (0.5f * Mathf.Sin(Time.time * crossfirePulseSpeed));
            Vector3 scale = _currentTelegraphScale;
            scale.x *= 1f + (crossfireWidthPulse * pulse);
            scale.y *= 1f - (crossfireWidthPulse * 0.2f * pulse);
            telegraphRenderer.transform.localScale = scale;

            Color color = _currentTelegraphColor;
            color.a = Mathf.Lerp(_currentTelegraphColor.a * 0.5f, _currentTelegraphColor.a, pulse);
            telegraphRenderer.color = color;
            telegraphRenderer.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.time * (crossfirePulseSpeed * 0.42f)) * crossfireRotateSpeed);
        }

        private void UpdatePhaseTransitionMotion()
        {
            if (auraRenderer == null || _phaseTransitionRemaining <= 0f)
            {
                return;
            }

            _phaseTransitionRemaining = Mathf.Max(0f, _phaseTransitionRemaining - Time.deltaTime);
            ResolveBaseAuraState(out Color baseAuraColor, out Vector3 baseAuraScale);
            float remainingRatio = _phaseTransitionDuration > 0.0001f
                ? Mathf.Clamp01(_phaseTransitionRemaining / _phaseTransitionDuration)
                : 0f;
            float pulse = 0.5f + (0.5f * Mathf.Sin(Time.time * phaseTransitionPulseSpeed));
            float intensity = Mathf.Lerp(0.3f, 1f, remainingRatio) * pulse;
            Color transitionColor = Color.Lerp(baseAuraColor, phaseTransitionAuraColor, intensity);
            transitionColor.a = Mathf.Min(0.9f, baseAuraColor.a + (phaseTransitionAlphaBoost * (0.3f + intensity)));
            auraRenderer.color = transitionColor;
            auraRenderer.transform.localScale = baseAuraScale * (1f + (phaseTransitionAuraScalePulse * intensity));

            if (_phaseTransitionRemaining <= 0f)
            {
                RefreshAuraState();
            }
        }

        private void ResolveBaseAuraState(out Color baseAuraColor, out Vector3 baseAuraScale)
        {
            baseAuraColor = CurrentPhase switch
            {
                BossPhaseType.PhaseTwo => phaseTwoAuraColor,
                BossPhaseType.PhaseThree => phaseThreeAuraColor,
                _ => idleAuraColor
            };
            baseAuraScale = CurrentPhase switch
            {
                BossPhaseType.PhaseTwo => phaseTwoAuraScale,
                BossPhaseType.PhaseThree => phaseThreeAuraScale,
                _ => idleAuraScale
            };
        }

        private void Reset()
        {
            ResolveReferences();
        }

        private void OnValidate()
        {
            ResolveReferences();
        }
    }
}
