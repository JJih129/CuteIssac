using System;
using CuteIssac.Data.Enemy;
using UnityEngine;

namespace CuteIssac.Enemy
{
    /// <summary>
    /// Dedicated boss brain with two clearly different patterns:
    /// radial burst fire and telegraphed charge.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BossEnemyBrain : EnemyBrain
    {
        public event Action<BossPatternType> TelegraphStarted;
        public event Action TelegraphEnded;
        public event Action<bool> PhaseTransitionStateChanged;

        private enum BossBrainState
        {
            Neutral = 0,
            BurstTelegraph = 1,
            VolleyTelegraph = 2,
            VolleyFiring = 3,
            ChargeTelegraph = 4,
            Charging = 5,
            SweepTelegraph = 6,
            SweepFiring = 7,
            SpiralTelegraph = 8,
            SpiralFiring = 9,
            FanTelegraph = 10,
            FanFiring = 11,
            ShockwaveTelegraph = 12,
            ShockwaveFiring = 13,
            CrossfireTelegraph = 14,
            CrossfireFiring = 15,
            PhaseTransition = 16
        }

        [Header("References")]
        [SerializeField] private EnemyCombat enemyCombat;
        [SerializeField] private BossVisual bossVisual;
        [SerializeField] private BossPhaseProfile bossPhaseProfile;

        [Header("Movement")]
        [SerializeField] [Min(0.5f)] private float preferredOrbitRange = 4.4f;
        [SerializeField] [Range(0f, 1f)] private float orbitBlend = 0.65f;

        [Header("Burst Pattern")]
        [SerializeField] [Min(0.1f)] private float burstCooldown = 2.4f;
        [SerializeField] [Min(0.05f)] private float burstTelegraphDuration = 0.45f;
        [SerializeField] [Min(4)] private int burstProjectileCount = 8;

        [Header("Volley Pattern")]
        [SerializeField] [Min(0.1f)] private float volleyCooldown = 4f;
        [SerializeField] [Min(0.05f)] private float volleyTelegraphDuration = 0.4f;
        [SerializeField] [Min(1)] private int volleySalvoCount = 3;
        [SerializeField] [Min(1)] private int volleyProjectilesPerSalvo = 3;
        [SerializeField] [Min(0f)] private float volleySpreadAngle = 22f;
        [SerializeField] [Min(0.05f)] private float volleyShotInterval = 0.18f;
        [SerializeField] [Min(0f)] private float volleyMoveBlend = 0.35f;

        [Header("Charge Pattern")]
        [SerializeField] [Min(0.1f)] private float chargeCooldown = 3.2f;
        [SerializeField] [Min(0.05f)] private float chargeTelegraphDuration = 0.55f;
        [SerializeField] [Min(0.05f)] private float chargeDuration = 0.55f;
        [SerializeField] [Min(1f)] private float chargeSpeedMultiplier = 3.8f;

        [Header("Sweep Pattern")]
        [SerializeField] [Min(0.1f)] private float sweepCooldown = 4.8f;
        [SerializeField] [Min(0.05f)] private float sweepTelegraphDuration = 0.5f;
        [SerializeField] [Min(1)] private int sweepSalvoCount = 5;
        [SerializeField] [Min(1)] private int sweepProjectilesPerSalvo = 2;
        [SerializeField] [Min(0f)] private float sweepArcAngle = 110f;
        [SerializeField] [Min(0f)] private float sweepSpreadAngle = 10f;
        [SerializeField] [Min(0.05f)] private float sweepShotInterval = 0.16f;
        [SerializeField] [Min(0f)] private float sweepMoveBlend = 0.18f;

        [Header("Spiral Pattern")]
        [SerializeField] [Min(0.1f)] private float spiralCooldown = 5.4f;
        [SerializeField] [Min(0.05f)] private float spiralTelegraphDuration = 0.58f;
        [SerializeField] [Min(2)] private int spiralSalvoCount = 4;
        [SerializeField] [Min(4)] private int spiralProjectilesPerSalvo = 10;
        [SerializeField] [Min(0.05f)] private float spiralShotInterval = 0.18f;
        [SerializeField] [Min(5f)] private float spiralTurnAngle = 24f;
        [SerializeField] [Range(0f, 1f)] private float spiralMoveBlend = 0.14f;

        [Header("Fan Pattern")]
        [SerializeField] [Min(0.1f)] private float fanCooldown = 5.1f;
        [SerializeField] [Min(0.05f)] private float fanTelegraphDuration = 0.52f;
        [SerializeField] [Min(2)] private int fanWaveCount = 3;
        [SerializeField] [Min(3)] private int fanProjectilesPerWave = 7;
        [SerializeField] [Min(0f)] private float fanSpreadAngle = 96f;
        [SerializeField] [Min(0f)] private float fanTurnAngle = 16f;
        [SerializeField] [Min(0.05f)] private float fanShotInterval = 0.2f;
        [SerializeField] [Range(0f, 1f)] private float fanMoveBlend = 0.08f;

        [Header("Shockwave Pattern")]
        [SerializeField] [Min(0.1f)] private float shockwaveCooldown = 6f;
        [SerializeField] [Min(0.05f)] private float shockwaveTelegraphDuration = 0.68f;
        [SerializeField] [Min(2)] private int shockwavePulseCount = 3;
        [SerializeField] [Min(4)] private int shockwaveProjectilesPerPulse = 12;
        [SerializeField] [Min(0.05f)] private float shockwaveShotInterval = 0.18f;
        [SerializeField] [Min(0f)] private float shockwaveAngleOffsetStep = 9f;

        [Header("Crossfire Pattern")]
        [SerializeField] [Min(0.1f)] private float crossfireCooldown = 5.6f;
        [SerializeField] [Min(0.05f)] private float crossfireTelegraphDuration = 0.62f;
        [SerializeField] [Min(2)] private int crossfireBurstCount = 3;
        [SerializeField] [Min(0.05f)] private float crossfireShotInterval = 0.18f;
        [SerializeField] [Min(0f)] private float crossfireAngleStep = 18f;
        [SerializeField] [Range(0f, 1f)] private float crossfireMoveBlend = 0.12f;

        [Header("Enrage")]
        [SerializeField] [Range(0.5f, 2f)] private float enragedCooldownMultiplier = 0.72f;
        [SerializeField] [Range(1f, 2.5f)] private float enragedBurstCountMultiplier = 1.5f;
        [SerializeField] [Range(1f, 2.5f)] private float enragedChargeSpeedMultiplier = 1.35f;
        [SerializeField] [Range(1f, 2f)] private float enragedVolleyCountMultiplier = 1.34f;

        private BossBrainState _state;
        private float _stateTimer;
        private float _burstCooldownRemaining;
        private float _volleyCooldownRemaining;
        private float _chargeCooldownRemaining;
        private float _sweepCooldownRemaining;
        private float _spiralCooldownRemaining;
        private float _fanCooldownRemaining;
        private float _shockwaveCooldownRemaining;
        private float _crossfireCooldownRemaining;
        private Vector2 _chargeDirection = Vector2.right;
        private int _patternCycleIndex;
        private bool _isEnraged;
        private BossPhaseType _currentPhase = BossPhaseType.PhaseOne;
        private BossPhaseProfile.BossPhaseDefinition _currentPhaseDefinition;
        private Vector2 _cachedAimDirection = Vector2.right;
        private int _volleyShotsRemaining;
        private int _sweepShotsRemaining;
        private int _spiralShotsRemaining;
        private int _fanShotsRemaining;
        private int _shockwaveShotsRemaining;
        private int _crossfireShotsRemaining;
        private BossPatternType? _telegraphedPattern;
        private float _phaseTransitionRemaining;
        private bool _isPhaseTransitionActive;
        private float _spiralCurrentAngle;
        private float _fanCurrentAngle;
        private float _shockwaveCurrentAngle;
        private float _crossfireCurrentAngle;

        protected override void HandleInitialized()
        {
            if (enemyCombat == null)
            {
                enemyCombat = GetComponent<EnemyCombat>();
            }

            if (bossVisual == null)
            {
                bossVisual = GetComponent<BossVisual>();
            }

            HandleResetState();
        }

        protected override void HandleResetState()
        {
            _state = BossBrainState.Neutral;
            _stateTimer = 0f;
            _burstCooldownRemaining = 0f;
            _volleyCooldownRemaining = 0f;
            _chargeCooldownRemaining = 0f;
            _sweepCooldownRemaining = 0f;
            _spiralCooldownRemaining = 0f;
            _fanCooldownRemaining = 0f;
            _shockwaveCooldownRemaining = 0f;
            _crossfireCooldownRemaining = 0f;
            _chargeDirection = Vector2.right;
            _patternCycleIndex = 0;
            _cachedAimDirection = Vector2.right;
            _volleyShotsRemaining = 0;
            _sweepShotsRemaining = 0;
            _spiralShotsRemaining = 0;
            _fanShotsRemaining = 0;
            _shockwaveShotsRemaining = 0;
            _crossfireShotsRemaining = 0;
            _telegraphedPattern = null;
            _phaseTransitionRemaining = 0f;
            _isPhaseTransitionActive = false;
            _spiralCurrentAngle = 0f;
            _fanCurrentAngle = 0f;
            _shockwaveCurrentAngle = 0f;
            _crossfireCurrentAngle = 0f;
            Controller.SetMoveSpeedMultiplier(1f);
            Controller.StopMovement();
            ClearTelegraph();
        }

        public void SetEnraged(bool enraged)
        {
            _isEnraged = enraged;
        }

        public void SetPhaseProfile(BossPhaseProfile profile)
        {
            bossPhaseProfile = profile;
            RefreshPhaseDefinition();
        }

        public void SetPhase(BossPhaseType phase)
        {
            ApplyPhase(phase, resetPatternCycle: true);
            _phaseTransitionRemaining = 0f;
            _stateTimer = 0f;
            _state = BossBrainState.Neutral;
            Controller.SetMoveSpeedMultiplier(1f);
            Controller.StopMovement();
            ClearTelegraph();
            SetPhaseTransitionState(false);
        }

        public void BeginPhaseTransition(BossPhaseType phase, float duration)
        {
            ApplyPhase(phase, resetPatternCycle: true);
            _stateTimer = Mathf.Max(0f, duration);
            _phaseTransitionRemaining = _stateTimer;
            Controller.SetMoveSpeedMultiplier(1f);
            Controller.StopMovement();
            ClearTelegraph();

            if (_phaseTransitionRemaining <= 0f)
            {
                _state = BossBrainState.Neutral;
                SetPhaseTransitionState(false);
                return;
            }

            _state = BossBrainState.PhaseTransition;
            SetPhaseTransitionState(true);
        }

        public BossPatternType? CurrentTelegraphedPattern => _telegraphedPattern;
        public bool IsPhaseTransitioning => _state == BossBrainState.PhaseTransition;

        public override void TickBrain(float fixedDeltaTime)
        {
            _burstCooldownRemaining = Mathf.Max(0f, _burstCooldownRemaining - fixedDeltaTime);
            _volleyCooldownRemaining = Mathf.Max(0f, _volleyCooldownRemaining - fixedDeltaTime);
            _chargeCooldownRemaining = Mathf.Max(0f, _chargeCooldownRemaining - fixedDeltaTime);
            _sweepCooldownRemaining = Mathf.Max(0f, _sweepCooldownRemaining - fixedDeltaTime);
            _spiralCooldownRemaining = Mathf.Max(0f, _spiralCooldownRemaining - fixedDeltaTime);
            _fanCooldownRemaining = Mathf.Max(0f, _fanCooldownRemaining - fixedDeltaTime);
            _shockwaveCooldownRemaining = Mathf.Max(0f, _shockwaveCooldownRemaining - fixedDeltaTime);
            _crossfireCooldownRemaining = Mathf.Max(0f, _crossfireCooldownRemaining - fixedDeltaTime);

            if (_state == BossBrainState.PhaseTransition)
            {
                TickPhaseTransition(fixedDeltaTime);
                return;
            }

            if (!Controller.HasTarget)
            {
                Controller.StopMovement();
                Controller.SetMoveSpeedMultiplier(1f);
                return;
            }

            Vector2 toTarget = Controller.TargetPosition - Controller.Position;

            if (toTarget.sqrMagnitude <= 0.0001f)
            {
                Controller.StopMovement();
                Controller.SetMoveSpeedMultiplier(1f);
                return;
            }

            Vector2 aimDirection = toTarget.normalized;
            _cachedAimDirection = aimDirection;

            switch (_state)
            {
                case BossBrainState.BurstTelegraph:
                    TickBurstTelegraph(fixedDeltaTime);
                    return;
                case BossBrainState.VolleyTelegraph:
                    TickVolleyTelegraph(fixedDeltaTime);
                    return;
                case BossBrainState.VolleyFiring:
                    TickVolleyFiring(fixedDeltaTime);
                    return;
                case BossBrainState.ChargeTelegraph:
                    TickChargeTelegraph(fixedDeltaTime);
                    return;
                case BossBrainState.Charging:
                    TickCharging(fixedDeltaTime);
                    return;
                case BossBrainState.SweepTelegraph:
                    TickSweepTelegraph(fixedDeltaTime);
                    return;
                case BossBrainState.SweepFiring:
                    TickSweepFiring(fixedDeltaTime);
                    return;
                case BossBrainState.SpiralTelegraph:
                    TickSpiralTelegraph(fixedDeltaTime);
                    return;
                case BossBrainState.SpiralFiring:
                    TickSpiralFiring(fixedDeltaTime);
                    return;
                case BossBrainState.FanTelegraph:
                    TickFanTelegraph(fixedDeltaTime);
                    return;
                case BossBrainState.FanFiring:
                    TickFanFiring(fixedDeltaTime);
                    return;
                case BossBrainState.ShockwaveTelegraph:
                    TickShockwaveTelegraph(fixedDeltaTime);
                    return;
                case BossBrainState.ShockwaveFiring:
                    TickShockwaveFiring(fixedDeltaTime);
                    return;
                case BossBrainState.CrossfireTelegraph:
                    TickCrossfireTelegraph(fixedDeltaTime);
                    return;
                case BossBrainState.CrossfireFiring:
                    TickCrossfireFiring(fixedDeltaTime);
                    return;
                case BossBrainState.PhaseTransition:
                    TickPhaseTransition(fixedDeltaTime);
                    return;
                default:
                    TickNeutral(aimDirection);
                    return;
            }
        }

        private void TickPhaseTransition(float fixedDeltaTime)
        {
            _phaseTransitionRemaining = Mathf.Max(0f, _phaseTransitionRemaining - fixedDeltaTime);
            _stateTimer = _phaseTransitionRemaining;
            Controller.SetMoveSpeedMultiplier(1f);
            Controller.StopMovement();

            if (_phaseTransitionRemaining > 0f)
            {
                return;
            }

            _state = BossBrainState.Neutral;
            _stateTimer = 0f;
            SetPhaseTransitionState(false);
        }

        private void TickNeutral(Vector2 aimDirection)
        {
            Vector2 orbitDirection = ResolveOrbitDirection(aimDirection);
            Controller.SetMoveSpeedMultiplier(1f);
            Controller.SetDesiredMoveDirection(orbitDirection);
            BossPatternType nextPattern = ResolveNextPattern();

            if (nextPattern == BossPatternType.Burst && _burstCooldownRemaining <= 0f && enemyCombat != null && enemyCombat.CanFire)
            {
                StartBurstTelegraph();
                return;
            }

            if (nextPattern == BossPatternType.Volley && _volleyCooldownRemaining <= 0f && enemyCombat != null && enemyCombat.CanFire)
            {
                StartVolleyTelegraph();
                return;
            }

            if (nextPattern == BossPatternType.Charge && _chargeCooldownRemaining <= 0f)
            {
                StartChargeTelegraph(aimDirection);
                return;
            }

            if (nextPattern == BossPatternType.Sweep && _sweepCooldownRemaining <= 0f && enemyCombat != null && enemyCombat.CanFire)
            {
                StartSweepTelegraph();
                return;
            }

            if (nextPattern == BossPatternType.Spiral && _spiralCooldownRemaining <= 0f && enemyCombat != null && enemyCombat.CanFire)
            {
                StartSpiralTelegraph();
                return;
            }

            if (nextPattern == BossPatternType.Fan && _fanCooldownRemaining <= 0f && enemyCombat != null && enemyCombat.CanFire)
            {
                StartFanTelegraph();
                return;
            }

            if (nextPattern == BossPatternType.Shockwave && _shockwaveCooldownRemaining <= 0f && enemyCombat != null && enemyCombat.CanFire)
            {
                StartShockwaveTelegraph();
                return;
            }

            if (nextPattern == BossPatternType.Crossfire && _crossfireCooldownRemaining <= 0f && enemyCombat != null && enemyCombat.CanFire)
            {
                StartCrossfireTelegraph();
            }
        }

        private Vector2 ResolveOrbitDirection(Vector2 aimDirection)
        {
            float distance = Vector2.Distance(Controller.Position, Controller.TargetPosition);
            Vector2 radialDirection = aimDirection;

            if (distance < preferredOrbitRange - 0.4f)
            {
                radialDirection = -aimDirection;
            }
            else if (distance <= preferredOrbitRange + 0.8f)
            {
                radialDirection = Vector2.zero;
            }

            Vector2 tangent = new Vector2(-aimDirection.y, aimDirection.x);
            Vector2 moveDirection = (tangent * orbitBlend) + radialDirection;
            return moveDirection.sqrMagnitude > 0.0001f ? moveDirection.normalized : tangent.normalized;
        }

        private void StartBurstTelegraph()
        {
            _state = BossBrainState.BurstTelegraph;
            _stateTimer = GetBurstTelegraphDuration();
            Controller.StopMovement();
            SetTelegraph(BossPatternType.Burst);
        }

        private void TickBurstTelegraph(float fixedDeltaTime)
        {
            _stateTimer -= fixedDeltaTime;
            Controller.StopMovement();

            if (_stateTimer > 0f)
            {
                return;
            }

            FireBurst();
            _burstCooldownRemaining = GetBurstCooldown();
            AdvancePatternCycle();
            _state = BossBrainState.Neutral;
            ClearTelegraph();
        }

        private void FireBurst()
        {
            if (enemyCombat == null || !enemyCombat.CanFire)
            {
                return;
            }

            int projectileCount = Mathf.Max(4, Mathf.RoundToInt(burstProjectileCount * GetBurstCountMultiplier()));
            float angleStep = 360f / projectileCount;

            for (int i = 0; i < projectileCount; i++)
            {
                float angle = angleStep * i;
                Vector2 direction = Quaternion.Euler(0f, 0f, angle) * Vector2.right;
                enemyCombat.Fire(direction);
            }

            bossVisual?.HandleAttack();
        }

        private void StartVolleyTelegraph()
        {
            _state = BossBrainState.VolleyTelegraph;
            _stateTimer = GetVolleyTelegraphDuration();
            _volleyShotsRemaining = Mathf.Max(1, Mathf.RoundToInt(volleySalvoCount * GetVolleyCountMultiplier()));
            Controller.SetMoveSpeedMultiplier(1f);
            Controller.StopMovement();
            SetTelegraph(BossPatternType.Volley);
        }

        private void TickVolleyTelegraph(float fixedDeltaTime)
        {
            _stateTimer -= fixedDeltaTime;
            Controller.StopMovement();

            if (_stateTimer > 0f)
            {
                return;
            }

            _state = BossBrainState.VolleyFiring;
            _stateTimer = 0f;
        }

        private void TickVolleyFiring(float fixedDeltaTime)
        {
            Vector2 strafeDirection = new Vector2(-_cachedAimDirection.y, _cachedAimDirection.x);
            Controller.SetMoveSpeedMultiplier(1f);
            Controller.SetDesiredMoveDirection((_cachedAimDirection * (1f - volleyMoveBlend)) + (strafeDirection * volleyMoveBlend));

            _stateTimer -= fixedDeltaTime;

            if (_stateTimer > 0f)
            {
                return;
            }

            FireVolleySalvo(_cachedAimDirection);
            _volleyShotsRemaining--;

            if (_volleyShotsRemaining <= 0)
            {
                _volleyCooldownRemaining = GetVolleyCooldown();
                AdvancePatternCycle();
                _state = BossBrainState.Neutral;
                ClearTelegraph();
                return;
            }

            _stateTimer = volleyShotInterval;
        }

        private void FireVolleySalvo(Vector2 aimDirection)
        {
            if (enemyCombat == null || !enemyCombat.CanFire)
            {
                return;
            }

            int projectileCount = Mathf.Max(1, volleyProjectilesPerSalvo);

            if (projectileCount == 1)
            {
                enemyCombat.Fire(aimDirection);
                bossVisual?.HandleAttack();
                return;
            }

            float halfSpread = volleySpreadAngle * 0.5f;

            for (int i = 0; i < projectileCount; i++)
            {
                float normalized = projectileCount == 1 ? 0.5f : i / (float)(projectileCount - 1);
                float angle = Mathf.Lerp(-halfSpread, halfSpread, normalized);
                Vector2 shotDirection = Quaternion.Euler(0f, 0f, angle) * aimDirection;
                enemyCombat.Fire(shotDirection.normalized);
            }

            bossVisual?.HandleAttack();
        }

        private void StartChargeTelegraph(Vector2 aimDirection)
        {
            _state = BossBrainState.ChargeTelegraph;
            _stateTimer = GetChargeTelegraphDuration();
            _chargeDirection = aimDirection;
            Controller.StopMovement();
            SetTelegraph(BossPatternType.Charge);
        }

        private void TickChargeTelegraph(float fixedDeltaTime)
        {
            _stateTimer -= fixedDeltaTime;
            Controller.StopMovement();

            if (_stateTimer > 0f)
            {
                return;
            }

            _state = BossBrainState.Charging;
            _stateTimer = chargeDuration;
            Controller.SetMoveSpeedMultiplier(GetChargeSpeedMultiplier());
            bossVisual?.HandleAttack();
        }

        private void TickCharging(float fixedDeltaTime)
        {
            _stateTimer -= fixedDeltaTime;
            Controller.SetMoveSpeedMultiplier(GetChargeSpeedMultiplier());
            Controller.SetDesiredMoveDirection(_chargeDirection);

            if (_stateTimer > 0f)
            {
                return;
            }

            Controller.SetMoveSpeedMultiplier(1f);
            _chargeCooldownRemaining = GetChargeCooldown();
            AdvancePatternCycle();
            _state = BossBrainState.Neutral;
            ClearTelegraph();
        }

        private void StartSweepTelegraph()
        {
            _state = BossBrainState.SweepTelegraph;
            _stateTimer = GetSweepTelegraphDuration();
            _sweepShotsRemaining = Mathf.Max(1, Mathf.RoundToInt(sweepSalvoCount * GetSweepCountMultiplier()));
            Controller.SetMoveSpeedMultiplier(1f);
            Controller.StopMovement();
            SetTelegraph(BossPatternType.Sweep);
        }

        private void StartSpiralTelegraph()
        {
            _state = BossBrainState.SpiralTelegraph;
            _stateTimer = GetSpiralTelegraphDuration();
            _spiralShotsRemaining = Mathf.Max(2, Mathf.RoundToInt(spiralSalvoCount * GetSpiralCountMultiplier()));
            _spiralCurrentAngle = Vector2.SignedAngle(Vector2.right, _cachedAimDirection);
            Controller.SetMoveSpeedMultiplier(1f);
            Controller.StopMovement();
            SetTelegraph(BossPatternType.Spiral);
        }

        private void StartFanTelegraph()
        {
            _state = BossBrainState.FanTelegraph;
            _stateTimer = GetFanTelegraphDuration();
            _fanShotsRemaining = Mathf.Max(2, Mathf.RoundToInt(fanWaveCount * GetFanCountMultiplier()));
            _fanCurrentAngle = Vector2.SignedAngle(Vector2.right, _cachedAimDirection);
            Controller.SetMoveSpeedMultiplier(1f);
            Controller.StopMovement();
            SetTelegraph(BossPatternType.Fan);
        }

        private void StartShockwaveTelegraph()
        {
            _state = BossBrainState.ShockwaveTelegraph;
            _stateTimer = GetShockwaveTelegraphDuration();
            _shockwaveShotsRemaining = Mathf.Max(2, Mathf.RoundToInt(shockwavePulseCount * GetShockwaveCountMultiplier()));
            _shockwaveCurrentAngle = 0f;
            Controller.SetMoveSpeedMultiplier(1f);
            Controller.StopMovement();
            SetTelegraph(BossPatternType.Shockwave);
        }

        private void StartCrossfireTelegraph()
        {
            _state = BossBrainState.CrossfireTelegraph;
            _stateTimer = GetCrossfireTelegraphDuration();
            _crossfireShotsRemaining = Mathf.Max(2, Mathf.RoundToInt(crossfireBurstCount * GetCrossfireCountMultiplier()));
            _crossfireCurrentAngle = Vector2.SignedAngle(Vector2.right, _cachedAimDirection);
            Controller.SetMoveSpeedMultiplier(1f);
            Controller.StopMovement();
            SetTelegraph(BossPatternType.Crossfire);
        }

        private void TickSweepTelegraph(float fixedDeltaTime)
        {
            _stateTimer -= fixedDeltaTime;
            Controller.StopMovement();

            if (_stateTimer > 0f)
            {
                return;
            }

            _state = BossBrainState.SweepFiring;
            _stateTimer = 0f;
        }

        private void TickSpiralTelegraph(float fixedDeltaTime)
        {
            _stateTimer -= fixedDeltaTime;
            Controller.StopMovement();

            if (_stateTimer > 0f)
            {
                return;
            }

            _state = BossBrainState.SpiralFiring;
            _stateTimer = 0f;
        }

        private void TickFanTelegraph(float fixedDeltaTime)
        {
            _stateTimer -= fixedDeltaTime;
            Controller.StopMovement();

            if (_stateTimer > 0f)
            {
                return;
            }

            _state = BossBrainState.FanFiring;
            _stateTimer = 0f;
        }

        private void TickShockwaveTelegraph(float fixedDeltaTime)
        {
            _stateTimer -= fixedDeltaTime;
            Controller.StopMovement();

            if (_stateTimer > 0f)
            {
                return;
            }

            _state = BossBrainState.ShockwaveFiring;
            _stateTimer = 0f;
        }

        private void TickCrossfireTelegraph(float fixedDeltaTime)
        {
            _stateTimer -= fixedDeltaTime;
            Controller.StopMovement();

            if (_stateTimer > 0f)
            {
                return;
            }

            _state = BossBrainState.CrossfireFiring;
            _stateTimer = 0f;
        }

        private void TickSweepFiring(float fixedDeltaTime)
        {
            Vector2 strafeDirection = new Vector2(_cachedAimDirection.y, -_cachedAimDirection.x);
            Controller.SetMoveSpeedMultiplier(1f);
            Controller.SetDesiredMoveDirection((_cachedAimDirection * (1f - sweepMoveBlend)) + (strafeDirection * sweepMoveBlend));

            _stateTimer -= fixedDeltaTime;

            if (_stateTimer > 0f)
            {
                return;
            }

            FireSweepSalvo(_cachedAimDirection);
            _sweepShotsRemaining--;

            if (_sweepShotsRemaining <= 0)
            {
                _sweepCooldownRemaining = GetSweepCooldown();
                AdvancePatternCycle();
                _state = BossBrainState.Neutral;
                ClearTelegraph();
                return;
            }

            _stateTimer = sweepShotInterval;
        }

        private void TickSpiralFiring(float fixedDeltaTime)
        {
            Vector2 strafeDirection = new Vector2(-_cachedAimDirection.y, _cachedAimDirection.x);
            Controller.SetMoveSpeedMultiplier(1f);
            Controller.SetDesiredMoveDirection((_cachedAimDirection * (1f - spiralMoveBlend)) + (strafeDirection * spiralMoveBlend));

            _stateTimer -= fixedDeltaTime;

            if (_stateTimer > 0f)
            {
                return;
            }

            FireSpiralSalvo();
            _spiralShotsRemaining--;

            if (_spiralShotsRemaining <= 0)
            {
                _spiralCooldownRemaining = GetSpiralCooldown();
                AdvancePatternCycle();
                _state = BossBrainState.Neutral;
                ClearTelegraph();
                return;
            }

            _stateTimer = spiralShotInterval;
        }

        private void TickFanFiring(float fixedDeltaTime)
        {
            Vector2 retreatDirection = -_cachedAimDirection;
            Controller.SetMoveSpeedMultiplier(1f);
            Controller.SetDesiredMoveDirection((_cachedAimDirection * (1f - fanMoveBlend)) + (retreatDirection * fanMoveBlend));

            _stateTimer -= fixedDeltaTime;

            if (_stateTimer > 0f)
            {
                return;
            }

            FireFanWave();
            _fanShotsRemaining--;

            if (_fanShotsRemaining <= 0)
            {
                _fanCooldownRemaining = GetFanCooldown();
                AdvancePatternCycle();
                _state = BossBrainState.Neutral;
                ClearTelegraph();
                return;
            }

            _stateTimer = fanShotInterval;
        }

        private void TickShockwaveFiring(float fixedDeltaTime)
        {
            Controller.SetMoveSpeedMultiplier(1f);
            Controller.StopMovement();

            _stateTimer -= fixedDeltaTime;

            if (_stateTimer > 0f)
            {
                return;
            }

            FireShockwavePulse();
            _shockwaveShotsRemaining--;

            if (_shockwaveShotsRemaining <= 0)
            {
                _shockwaveCooldownRemaining = GetShockwaveCooldown();
                AdvancePatternCycle();
                _state = BossBrainState.Neutral;
                ClearTelegraph();
                return;
            }

            _stateTimer = shockwaveShotInterval;
        }

        private void TickCrossfireFiring(float fixedDeltaTime)
        {
            Vector2 orbitDirection = new Vector2(-_cachedAimDirection.y, _cachedAimDirection.x);
            Controller.SetMoveSpeedMultiplier(1f);
            Controller.SetDesiredMoveDirection((_cachedAimDirection * (1f - crossfireMoveBlend)) + (orbitDirection * crossfireMoveBlend));

            _stateTimer -= fixedDeltaTime;

            if (_stateTimer > 0f)
            {
                return;
            }

            FireCrossfireBurst();
            _crossfireShotsRemaining--;

            if (_crossfireShotsRemaining <= 0)
            {
                _crossfireCooldownRemaining = GetCrossfireCooldown();
                AdvancePatternCycle();
                _state = BossBrainState.Neutral;
                ClearTelegraph();
                return;
            }

            _stateTimer = crossfireShotInterval;
        }

        private void FireSweepSalvo(Vector2 aimDirection)
        {
            if (enemyCombat == null || !enemyCombat.CanFire)
            {
                return;
            }

            float normalizedIndex = _sweepShotsRemaining <= 1
                ? 1f
                : 1f - ((_sweepShotsRemaining - 1f) / Mathf.Max(1f, (sweepSalvoCount * GetSweepCountMultiplier()) - 1f));
            float sweepAngle = Mathf.Lerp(-sweepArcAngle * 0.5f, sweepArcAngle * 0.5f, normalizedIndex);
            Vector2 centerDirection = (Quaternion.Euler(0f, 0f, sweepAngle) * aimDirection).normalized;
            int projectileCount = Mathf.Max(1, sweepProjectilesPerSalvo);

            if (projectileCount == 1)
            {
                enemyCombat.Fire(centerDirection);
                bossVisual?.HandleAttack();
                return;
            }

            float halfSpread = sweepSpreadAngle * 0.5f;

            for (int i = 0; i < projectileCount; i++)
            {
                float spreadT = projectileCount == 1 ? 0.5f : i / (float)(projectileCount - 1);
                float spreadAngle = Mathf.Lerp(-halfSpread, halfSpread, spreadT);
                Vector2 shotDirection = (Quaternion.Euler(0f, 0f, spreadAngle) * centerDirection).normalized;
                enemyCombat.Fire(shotDirection);
            }

            bossVisual?.HandleAttack();
        }

        private void FireSpiralSalvo()
        {
            if (enemyCombat == null || !enemyCombat.CanFire)
            {
                return;
            }

            int projectileCount = Mathf.Max(4, spiralProjectilesPerSalvo);
            float angleStep = 360f / projectileCount;

            for (int i = 0; i < projectileCount; i++)
            {
                float angle = _spiralCurrentAngle + (angleStep * i);
                Vector2 direction = Quaternion.Euler(0f, 0f, angle) * Vector2.right;
                enemyCombat.Fire(direction.normalized);
            }

            _spiralCurrentAngle += spiralTurnAngle;
            bossVisual?.HandleAttack();
        }

        private void FireFanWave()
        {
            if (enemyCombat == null || !enemyCombat.CanFire)
            {
                return;
            }

            int projectileCount = Mathf.Max(3, fanProjectilesPerWave);
            float halfSpread = fanSpreadAngle * 0.5f;

            for (int i = 0; i < projectileCount; i++)
            {
                float t = projectileCount == 1 ? 0.5f : i / (float)(projectileCount - 1);
                float angle = Mathf.Lerp(-halfSpread, halfSpread, t) + _fanCurrentAngle;
                Vector2 direction = (Quaternion.Euler(0f, 0f, angle) * Vector2.right).normalized;
                enemyCombat.Fire(direction);
            }

            _fanCurrentAngle += fanTurnAngle;
            bossVisual?.HandleAttack();
        }

        private void FireShockwavePulse()
        {
            if (enemyCombat == null || !enemyCombat.CanFire)
            {
                return;
            }

            int projectileCount = Mathf.Max(6, shockwaveProjectilesPerPulse);
            float angleStep = 360f / projectileCount;

            for (int i = 0; i < projectileCount; i++)
            {
                float angle = _shockwaveCurrentAngle + (angleStep * i);
                Vector2 direction = (Quaternion.Euler(0f, 0f, angle) * Vector2.right).normalized;
                enemyCombat.Fire(direction);
            }

            _shockwaveCurrentAngle += shockwaveAngleOffsetStep;
            bossVisual?.HandleAttack();
        }

        private void FireCrossfireBurst()
        {
            if (enemyCombat == null || !enemyCombat.CanFire)
            {
                return;
            }

            Vector2 baseDirection = (Quaternion.Euler(0f, 0f, _crossfireCurrentAngle) * Vector2.right).normalized;
            FireCrossfireAxis(baseDirection);
            FireCrossfireAxis(new Vector2(-baseDirection.y, baseDirection.x));
            _crossfireCurrentAngle += crossfireAngleStep;
            bossVisual?.HandleAttack();
        }

        private void FireCrossfireAxis(Vector2 direction)
        {
            enemyCombat.Fire(direction.normalized);
            enemyCombat.Fire((-direction).normalized);
        }

        private float GetBurstCooldown()
        {
            float cooldown = burstCooldown * GetPhaseMultiplier(_currentPhaseDefinition != null ? _currentPhaseDefinition.BurstCooldownMultiplier : 1f);
            return _isEnraged ? cooldown * enragedCooldownMultiplier : cooldown;
        }

        private float GetChargeCooldown()
        {
            float cooldown = chargeCooldown * GetPhaseMultiplier(_currentPhaseDefinition != null ? _currentPhaseDefinition.ChargeCooldownMultiplier : 1f);
            return _isEnraged ? cooldown * enragedCooldownMultiplier : cooldown;
        }

        private float GetSweepCooldown()
        {
            float cooldown = sweepCooldown * GetPhaseMultiplier(_currentPhaseDefinition != null ? _currentPhaseDefinition.SweepCooldownMultiplier : 1f);
            return _isEnraged ? cooldown * enragedCooldownMultiplier : cooldown;
        }

        private float GetSpiralCooldown()
        {
            float cooldown = spiralCooldown * GetPhaseMultiplier(_currentPhaseDefinition != null ? _currentPhaseDefinition.SpiralCooldownMultiplier : 1f);
            return _isEnraged ? cooldown * enragedCooldownMultiplier : cooldown;
        }

        private float GetFanCooldown()
        {
            float cooldown = fanCooldown * GetPhaseMultiplier(_currentPhaseDefinition != null ? _currentPhaseDefinition.FanCooldownMultiplier : 1f);
            return _isEnraged ? cooldown * enragedCooldownMultiplier : cooldown;
        }

        private float GetShockwaveCooldown()
        {
            float cooldown = shockwaveCooldown * GetPhaseMultiplier(_currentPhaseDefinition != null ? _currentPhaseDefinition.ShockwaveCooldownMultiplier : 1f);
            return _isEnraged ? cooldown * enragedCooldownMultiplier : cooldown;
        }

        private float GetCrossfireCooldown()
        {
            float cooldown = crossfireCooldown * GetPhaseMultiplier(_currentPhaseDefinition != null ? _currentPhaseDefinition.CrossfireCooldownMultiplier : 1f);
            return _isEnraged ? cooldown * enragedCooldownMultiplier : cooldown;
        }

        private float GetVolleyCooldown()
        {
            float cooldown = volleyCooldown * GetPhaseMultiplier(_currentPhaseDefinition != null ? _currentPhaseDefinition.VolleyCooldownMultiplier : 1f);
            return _isEnraged ? cooldown * enragedCooldownMultiplier : cooldown;
        }

        private float GetChargeSpeedMultiplier()
        {
            float multiplier = chargeSpeedMultiplier * GetPhaseMultiplier(_currentPhaseDefinition != null ? _currentPhaseDefinition.ChargeSpeedMultiplier : 1f);
            return _isEnraged ? multiplier * enragedChargeSpeedMultiplier : multiplier;
        }

        private BossPatternType ResolveNextPattern()
        {
            if (_currentPhaseDefinition?.PatternCycle != null && _currentPhaseDefinition.PatternCycle.Length > 0)
            {
                BossPatternType[] phasePatternCycle = _currentPhaseDefinition.PatternCycle;
                return phasePatternCycle[_patternCycleIndex % phasePatternCycle.Length];
            }

            BossPatternType[] patternCycle = _currentPhase switch
            {
                BossPhaseType.PhaseTwo => new[] { BossPatternType.Burst, BossPatternType.Volley, BossPatternType.Spiral, BossPatternType.Fan, BossPatternType.Shockwave, BossPatternType.Charge },
                BossPhaseType.PhaseThree => new[] { BossPatternType.Burst, BossPatternType.Spiral, BossPatternType.Crossfire, BossPatternType.Fan, BossPatternType.Shockwave, BossPatternType.Sweep, BossPatternType.Charge, BossPatternType.Volley },
                _ => new[] { BossPatternType.Burst, BossPatternType.Charge }
            };

            return patternCycle[_patternCycleIndex % patternCycle.Length];
        }

        private void AdvancePatternCycle()
        {
            _patternCycleIndex++;
        }

        private float GetBurstTelegraphDuration()
        {
            float duration = burstTelegraphDuration * GetPhaseMultiplier(_currentPhaseDefinition != null ? _currentPhaseDefinition.BurstTelegraphMultiplier : 1f);
            return _isEnraged ? duration * 0.8f : duration;
        }

        private float GetVolleyTelegraphDuration()
        {
            float duration = volleyTelegraphDuration * GetPhaseMultiplier(_currentPhaseDefinition != null ? _currentPhaseDefinition.VolleyTelegraphMultiplier : 1f);
            return _isEnraged ? duration * 0.75f : duration;
        }

        private float GetChargeTelegraphDuration()
        {
            float duration = chargeTelegraphDuration * GetPhaseMultiplier(_currentPhaseDefinition != null ? _currentPhaseDefinition.ChargeTelegraphMultiplier : 1f);
            return _isEnraged ? duration * 0.8f : duration;
        }

        private float GetSweepTelegraphDuration()
        {
            float duration = sweepTelegraphDuration * GetPhaseMultiplier(_currentPhaseDefinition != null ? _currentPhaseDefinition.SweepTelegraphMultiplier : 1f);
            return _isEnraged ? duration * 0.78f : duration;
        }

        private float GetSpiralTelegraphDuration()
        {
            float duration = spiralTelegraphDuration * GetPhaseMultiplier(_currentPhaseDefinition != null ? _currentPhaseDefinition.SpiralTelegraphMultiplier : 1f);
            return _isEnraged ? duration * 0.76f : duration;
        }

        private float GetFanTelegraphDuration()
        {
            float duration = fanTelegraphDuration * GetPhaseMultiplier(_currentPhaseDefinition != null ? _currentPhaseDefinition.FanTelegraphMultiplier : 1f);
            return _isEnraged ? duration * 0.78f : duration;
        }

        private float GetShockwaveTelegraphDuration()
        {
            float duration = shockwaveTelegraphDuration * GetPhaseMultiplier(_currentPhaseDefinition != null ? _currentPhaseDefinition.ShockwaveTelegraphMultiplier : 1f);
            return _isEnraged ? duration * 0.8f : duration;
        }

        private float GetCrossfireTelegraphDuration()
        {
            float duration = crossfireTelegraphDuration * GetPhaseMultiplier(_currentPhaseDefinition != null ? _currentPhaseDefinition.CrossfireTelegraphMultiplier : 1f);
            return _isEnraged ? duration * 0.8f : duration;
        }

        private float GetBurstCountMultiplier()
        {
            float multiplier = GetPhaseMultiplier(_currentPhaseDefinition != null ? _currentPhaseDefinition.BurstCountMultiplier : 1f);
            return _isEnraged ? multiplier * enragedBurstCountMultiplier : multiplier;
        }

        private float GetVolleyCountMultiplier()
        {
            float multiplier = GetPhaseMultiplier(_currentPhaseDefinition != null ? _currentPhaseDefinition.VolleyCountMultiplier : 1f);
            return _isEnraged ? multiplier * enragedVolleyCountMultiplier : multiplier;
        }

        private float GetSweepCountMultiplier()
        {
            float multiplier = GetPhaseMultiplier(_currentPhaseDefinition != null ? _currentPhaseDefinition.SweepCountMultiplier : 1f);
            return _isEnraged ? multiplier * enragedVolleyCountMultiplier : multiplier;
        }

        private float GetSpiralCountMultiplier()
        {
            float multiplier = GetPhaseMultiplier(_currentPhaseDefinition != null ? _currentPhaseDefinition.SpiralCountMultiplier : 1f);
            return _isEnraged ? multiplier * enragedBurstCountMultiplier : multiplier;
        }

        private float GetFanCountMultiplier()
        {
            float multiplier = GetPhaseMultiplier(_currentPhaseDefinition != null ? _currentPhaseDefinition.FanCountMultiplier : 1f);
            return _isEnraged ? multiplier * enragedVolleyCountMultiplier : multiplier;
        }

        private float GetShockwaveCountMultiplier()
        {
            float multiplier = GetPhaseMultiplier(_currentPhaseDefinition != null ? _currentPhaseDefinition.ShockwaveCountMultiplier : 1f);
            return _isEnraged ? multiplier * enragedBurstCountMultiplier : multiplier;
        }

        private float GetCrossfireCountMultiplier()
        {
            float multiplier = GetPhaseMultiplier(_currentPhaseDefinition != null ? _currentPhaseDefinition.CrossfireCountMultiplier : 1f);
            return _isEnraged ? multiplier * enragedVolleyCountMultiplier : multiplier;
        }

        private void RefreshPhaseDefinition()
        {
            if (bossPhaseProfile != null && bossPhaseProfile.TryGetDefinition(_currentPhase, out BossPhaseProfile.BossPhaseDefinition definition))
            {
                _currentPhaseDefinition = definition;
                return;
            }

            _currentPhaseDefinition = null;
        }

        private void ApplyPhase(BossPhaseType phase, bool resetPatternCycle)
        {
            _currentPhase = phase;
            RefreshPhaseDefinition();
            _volleyShotsRemaining = 0;
            _sweepShotsRemaining = 0;
            _spiralShotsRemaining = 0;
            _fanShotsRemaining = 0;
            _shockwaveShotsRemaining = 0;
            _crossfireShotsRemaining = 0;

            if (resetPatternCycle)
            {
                _patternCycleIndex = 0;
            }
        }

        private static float GetPhaseMultiplier(float multiplier)
        {
            return Mathf.Max(0.2f, multiplier);
        }

        private void SetTelegraph(BossPatternType patternType)
        {
            _telegraphedPattern = patternType;
            bossVisual?.SetTelegraphActive(true, patternType);
            TelegraphStarted?.Invoke(patternType);
        }

        private void SetPhaseTransitionState(bool active)
        {
            if (_isPhaseTransitionActive == active)
            {
                return;
            }

            _isPhaseTransitionActive = active;
            PhaseTransitionStateChanged?.Invoke(active);
        }

        private void ClearTelegraph()
        {
            if (_telegraphedPattern.HasValue)
            {
                bossVisual?.SetTelegraphActive(false, _telegraphedPattern.Value);
                _telegraphedPattern = null;
                TelegraphEnded?.Invoke();
                return;
            }

            bossVisual?.SetTelegraphActive(false, BossPatternType.Burst);
            bossVisual?.SetTelegraphActive(false, BossPatternType.Volley);
            bossVisual?.SetTelegraphActive(false, BossPatternType.Charge);
            bossVisual?.SetTelegraphActive(false, BossPatternType.Sweep);
            bossVisual?.SetTelegraphActive(false, BossPatternType.Spiral);
            bossVisual?.SetTelegraphActive(false, BossPatternType.Fan);
            bossVisual?.SetTelegraphActive(false, BossPatternType.Shockwave);
            bossVisual?.SetTelegraphActive(false, BossPatternType.Crossfire);
        }

        private void Reset()
        {
            enemyCombat = GetComponent<EnemyCombat>();
            bossVisual = GetComponent<BossVisual>();
        }

        private void OnValidate()
        {
            if (enemyCombat == null)
            {
                enemyCombat = GetComponent<EnemyCombat>();
            }

            if (bossVisual == null)
            {
                bossVisual = GetComponent<BossVisual>();
            }
        }
    }
}
