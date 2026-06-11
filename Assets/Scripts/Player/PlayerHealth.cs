using System;
using CuteIssac.Common.Combat;
using CuteIssac.Core.Audio;
using CuteIssac.Core.Feedback;
using CuteIssac.Core.Gameplay;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CuteIssac.Player
{
    /// <summary>
    /// Owns player hit points and temporary invulnerability after taking damage.
    /// UI, run flow, and feedback systems should subscribe to the exposed events instead of embedding logic here.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerHealth : MonoBehaviour, IDamageable
    {
        [Header("Health")]
        [SerializeField] [Min(1f)] private float maxHealth = 6f;
        [SerializeField] [Min(0f)] private float startingHealth = -1f;
        [SerializeField] [Min(0f)] private float invulnerabilityDuration = 1f;
        [SerializeField] private bool enableDevelopmentHotkeys = true;
        [SerializeField] private PlayerStats playerStats;
        [SerializeField] private PlayerVisual playerVisual;
        [SerializeField] private PlayerShieldState playerShieldState;
        [SerializeField] private PlayerSpeedBuffState speedBuffState;

        [Header("Speed Heart")]
        [SerializeField] [Min(0)] private int maxSpeedHeartCount = 3;
        [SerializeField] [Min(0.05f)] private float defaultSpeedHeartBuffDuration = 10f;
        [SerializeField] [Min(0.05f)] private float defaultSpeedHeartMoveSpeedMultiplier = 1.2f;
        [SerializeField] private PlayerSpeedBuffState.DuplicateBuffPolicy defaultSpeedHeartDuplicatePolicy = PlayerSpeedBuffState.DuplicateBuffPolicy.RefreshDuration;

        public event Action<float, float> HealthChanged;
        public event Action<int> SpeedHeartChanged;
        public event Action Damaged;
        public event Action<DamageInfo> DamagedWithInfo;
        public event Action<float> InvulnerabilityGranted;
        public event Action Died;

        public float BaseMaxHealth => maxHealth;
        public float MaxHealth => maxHealth + _runtimeMaxHealthBonus + _pickupMaxHealthBonus;
        public float PickupMaxHealthBonus => _pickupMaxHealthBonus;
        public float CurrentHealth { get; private set; }
        public bool IsInvulnerable { get; private set; }
        public bool IsDead { get; private set; }
        public bool IsDebugInvulnerable { get; private set; }
        public int SpeedHeartCount => _speedHeartCount;
        public int MaxSpeedHeartCount => Mathf.Max(0, maxSpeedHeartCount);

        private float _invulnerabilityRemaining;
        private float _runtimeMaxHealthBonus;
        private float _pickupMaxHealthBonus;
        private float _routeBreakthroughDamageMultiplier = 1f;
        private float _routeBreakthroughInvulnerabilityBonus;
        private int _speedHeartCount;
        private float _speedHeartBuffDuration;
        private float _speedHeartMoveSpeedMultiplier;
        private PlayerSpeedBuffState.DuplicateBuffPolicy _speedHeartDuplicatePolicy;
        private Sprite _speedHeartIcon;
        private string _speedHeartDisplayName = "스피드 하트";

        private void Awake()
        {
            _speedHeartDuplicatePolicy = defaultSpeedHeartDuplicatePolicy;
            CurrentHealth = startingHealth >= 0f
                ? Mathf.Min(MaxHealth, startingHealth)
                : MaxHealth;
        }

        private void OnEnable()
        {
            PlayerRegistry.Register(this);
            ResolveDependencies();

            if (playerStats != null)
            {
                playerStats.StatsRecalculated += HandleStatsRecalculated;
            }
        }

        private void Start()
        {
            if (playerStats != null)
            {
                HandleStatsRecalculated(playerStats.CurrentStats);
            }
        }

        private void OnDisable()
        {
            PlayerRegistry.Unregister(this);

            if (playerStats != null)
            {
                playerStats.StatsRecalculated -= HandleStatsRecalculated;
            }
        }

        private void OnDestroy()
        {
            PlayerRegistry.Unregister(this);
        }

        private void Update()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            HandleDevelopmentHotkeys();
#endif

            if (!IsInvulnerable)
            {
                return;
            }

            _invulnerabilityRemaining -= Time.deltaTime;

            if (_invulnerabilityRemaining <= 0f)
            {
                IsInvulnerable = false;
                _invulnerabilityRemaining = 0f;
            }
        }

        public void ApplyDamage(in DamageInfo damageInfo)
        {
            ApplyDamageInternal(damageInfo, ignoreInvulnerability: false, grantInvulnerability: true);
        }

        public bool TrySpendHealth(float amount, Transform source = null, bool allowLethal = false)
        {
            if (IsDead)
            {
                return false;
            }

            float healthCost = Mathf.Max(0f, amount);

            if (healthCost <= 0f)
            {
                return true;
            }

            if (!allowLethal && CurrentHealth <= healthCost)
            {
                return false;
            }

            ApplyDamageInternal(
                new DamageInfo(healthCost, Vector2.zero, source != null ? source : transform),
                ignoreInvulnerability: true,
                grantInvulnerability: false);

            return true;
        }

        public void RestoreToFull()
        {
            IsDead = false;
            IsInvulnerable = false;
            _invulnerabilityRemaining = 0f;
            CurrentHealth = MaxHealth;
            HealthChanged?.Invoke(CurrentHealth, MaxHealth);
        }

        public void RestoreForRunResume(float currentHealth)
        {
            IsDead = false;
            IsInvulnerable = false;
            _invulnerabilityRemaining = 0f;
            CurrentHealth = currentHealth > 0f
                ? Mathf.Clamp(currentHealth, 1f, MaxHealth)
                : MaxHealth;
            HealthChanged?.Invoke(CurrentHealth, MaxHealth);
        }

        /// <summary>
        /// Restores health without reviving a dead player.
        /// Pickups can use this instead of talking to health internals directly.
        /// </summary>
        public bool RestoreHealth(float amount)
        {
            if (IsDead)
            {
                return false;
            }

            float clampedAmount = Mathf.Max(0f, amount);

            if (clampedAmount <= 0f || CurrentHealth >= MaxHealth)
            {
                return false;
            }

            CurrentHealth = Mathf.Min(MaxHealth, CurrentHealth + clampedAmount);
            HealthChanged?.Invoke(CurrentHealth, MaxHealth);
            return true;
        }

        public bool TryGrantTemporaryInvulnerability(float duration)
        {
            if (IsDead)
            {
                return false;
            }

            bool granted = GrantInvulnerabilityInternal(duration);
            if (granted)
            {
                InvulnerabilityGranted?.Invoke(Mathf.Max(0f, duration));
            }

            return granted;
        }

        public bool TryGrantSpeedHeart(
            int amount,
            float buffDuration,
            float moveSpeedMultiplier,
            PlayerSpeedBuffState.DuplicateBuffPolicy duplicatePolicy,
            Sprite statusIcon,
            string displayName)
        {
            if (IsDead)
            {
                return false;
            }

            int resolvedAmount = Mathf.Max(0, amount);
            int resolvedMax = MaxSpeedHeartCount;
            int available = Mathf.Max(0, resolvedMax - _speedHeartCount);
            int granted = Mathf.Min(resolvedAmount, available);

            if (granted <= 0)
            {
                return false;
            }

            _speedHeartCount += granted;
            _speedHeartBuffDuration = Mathf.Max(0.05f, buffDuration);
            _speedHeartMoveSpeedMultiplier = Mathf.Max(0.05f, moveSpeedMultiplier);
            _speedHeartDuplicatePolicy = duplicatePolicy;
            _speedHeartIcon = statusIcon;
            _speedHeartDisplayName = string.IsNullOrWhiteSpace(displayName) ? "스피드 하트" : displayName;
            SpeedHeartChanged?.Invoke(_speedHeartCount);
            HealthChanged?.Invoke(CurrentHealth, MaxHealth);
            return true;
        }

        public bool CanReceiveSpeedHeart(int amount = 1)
        {
            return !IsDead && amount > 0 && _speedHeartCount < MaxSpeedHeartCount;
        }

        /// <summary>
        /// Adds pickup-owned max health without being overwritten by PlayerStats recalculation.
        /// </summary>
        public bool TryGrantPickupMaxHealthBonus(float amount, float maxAllowedBonus, bool healGrantedAmount)
        {
            if (IsDead)
            {
                return false;
            }

            float resolvedAmount = Mathf.Max(0f, amount);
            float resolvedLimit = Mathf.Max(0f, maxAllowedBonus);
            float remainingBonus = Mathf.Max(0f, resolvedLimit - _pickupMaxHealthBonus);
            float appliedBonus = Mathf.Min(resolvedAmount, remainingBonus);

            if (appliedBonus <= 0f)
            {
                return false;
            }

            _pickupMaxHealthBonus += appliedBonus;

            if (healGrantedAmount)
            {
                CurrentHealth = Mathf.Min(MaxHealth, CurrentHealth + appliedBonus);
            }

            HealthChanged?.Invoke(CurrentHealth, MaxHealth);
            return true;
        }

        public void SetRuntimeMaxHealthBonus(float bonus)
        {
            float clampedBonus = Mathf.Max(0f, bonus);

            if (Mathf.Approximately(_runtimeMaxHealthBonus, clampedBonus))
            {
                return;
            }

            float previousMaxHealth = MaxHealth;
            _runtimeMaxHealthBonus = clampedBonus;

            if (CurrentHealth > MaxHealth)
            {
                CurrentHealth = MaxHealth;
            }

            if (!Mathf.Approximately(previousMaxHealth, MaxHealth))
            {
                HealthChanged?.Invoke(CurrentHealth, MaxHealth);
            }
        }

        public void SetDebugInvulnerable(bool value)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            IsDebugInvulnerable = value;
#else
            IsDebugInvulnerable = false;
#endif
        }

        public void SetRouteBreakthroughProtection(float damageMultiplier, float invulnerabilityBonus)
        {
            _routeBreakthroughDamageMultiplier = Mathf.Clamp(damageMultiplier, 0.1f, 1f);
            _routeBreakthroughInvulnerabilityBonus = Mathf.Max(0f, invulnerabilityBonus);
        }

        private void Die()
        {
            if (IsDead)
            {
                return;
            }

            IsDead = true;
            IsInvulnerable = false;
            _invulnerabilityRemaining = 0f;
            Died?.Invoke();
        }

        private void HandleStatsRecalculated(PlayerStatSnapshot snapshot)
        {
            SetRuntimeMaxHealthBonus(Mathf.Max(0f, snapshot.MaxHealth - maxHealth));
        }

        private void ResolveDependencies()
        {
            if (playerStats == null)
            {
                playerStats = GetComponent<PlayerStats>();
            }

            if (playerVisual == null)
            {
                playerVisual = GetComponent<PlayerVisual>();
            }

            if (playerShieldState == null)
            {
                playerShieldState = GetComponent<PlayerShieldState>();
            }

            if (speedBuffState == null)
            {
                speedBuffState = GetComponent<PlayerSpeedBuffState>();
            }
        }

        private void HandleDevelopmentHotkeys()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (!enableDevelopmentHotkeys)
            {
                return;
            }

            Keyboard keyboard = Keyboard.current;

            if (keyboard == null)
            {
                return;
            }

            if ((keyboard.digit1Key != null && keyboard.digit1Key.wasPressedThisFrame)
                || (keyboard.numpad1Key != null && keyboard.numpad1Key.wasPressedThisFrame))
            {
                RestoreToFull();
            }
#endif
        }

        private void ApplyDamageInternal(in DamageInfo damageInfo, bool ignoreInvulnerability, bool grantInvulnerability)
        {
            if (IsDead)
            {
                return;
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (IsDebugInvulnerable)
            {
                return;
            }
#endif

            if (!ignoreInvulnerability && IsInvulnerable)
            {
                return;
            }

            if (!ignoreInvulnerability && playerShieldState == null)
            {
                playerShieldState = GetComponent<PlayerShieldState>();
            }

            if (!ignoreInvulnerability && playerShieldState != null && playerShieldState.TryBlockDamage(in damageInfo))
            {
                return;
            }

            float damageAmount = Mathf.Max(0f, damageInfo.Amount * _routeBreakthroughDamageMultiplier);

            if (damageAmount <= 0f)
            {
                return;
            }

            if (!ignoreInvulnerability && TryConsumeSpeedHeart(in damageInfo, grantInvulnerability))
            {
                return;
            }

            DamageInfo resolvedDamageInfo = new(damageAmount, damageInfo.HitDirection, damageInfo.Source, damageInfo.KnockbackForce);
            CurrentHealth = Mathf.Max(0f, CurrentHealth - damageAmount);

            float resolvedInvulnerabilityDuration = invulnerabilityDuration + _routeBreakthroughInvulnerabilityBonus;
            if (grantInvulnerability && resolvedInvulnerabilityDuration > 0f)
            {
                GrantInvulnerabilityInternal(resolvedInvulnerabilityDuration);
                InvulnerabilityGranted?.Invoke(resolvedInvulnerabilityDuration);
            }

            DamagedWithInfo?.Invoke(resolvedDamageInfo);
            Damaged?.Invoke();
            GameplayRuntimeEvents.RaisePlayerDamaged(new PlayerDamagedSignal(this, resolvedDamageInfo, CurrentHealth, MaxHealth));
            GameplayFeedbackEvents.RaiseFloatingFeedback(new FloatingFeedbackRequest(
                ResolveDamageFeedbackPosition(in resolvedDamageInfo),
                FloatingFeedbackTextCache.GetNegativeCeil(damageAmount),
                new Color(1f, 0.42f, 0.42f, 1f),
                0.58f,
                0.72f,
                1.24f,
                visualProfile: FloatingFeedbackVisualProfile.PlayerDamage));
            GameAudioEvents.Raise(GameAudioEventType.PlayerDamaged, transform.position, false);
            HealthChanged?.Invoke(CurrentHealth, MaxHealth);

            if (CurrentHealth <= 0f)
            {
                Die();
                return;
            }
        }

        private bool GrantInvulnerabilityInternal(float duration)
        {
            float resolvedDuration = Mathf.Max(0f, duration);

            if (resolvedDuration <= 0f)
            {
                return false;
            }

            bool wasInvulnerable = IsInvulnerable;
            float previousRemaining = _invulnerabilityRemaining;
            IsInvulnerable = true;
            _invulnerabilityRemaining = Mathf.Max(_invulnerabilityRemaining, resolvedDuration);
            return !wasInvulnerable || _invulnerabilityRemaining > previousRemaining + 0.001f;
        }

        private bool TryConsumeSpeedHeart(in DamageInfo damageInfo, bool grantInvulnerability)
        {
            if (_speedHeartCount <= 0)
            {
                return false;
            }

            _speedHeartCount--;
            SpeedHeartChanged?.Invoke(_speedHeartCount);
            HealthChanged?.Invoke(CurrentHealth, MaxHealth);

            ResolveSpeedBuffState();
            speedBuffState?.TryApplyBuff(
                ResolveSpeedHeartBuffDuration(),
                ResolveSpeedHeartMoveSpeedMultiplier(),
                _speedHeartDuplicatePolicy,
                _speedHeartIcon,
                _speedHeartDisplayName);

            float resolvedInvulnerabilityDuration = invulnerabilityDuration + _routeBreakthroughInvulnerabilityBonus;
            if (grantInvulnerability && resolvedInvulnerabilityDuration > 0f)
            {
                GrantInvulnerabilityInternal(resolvedInvulnerabilityDuration);
                InvulnerabilityGranted?.Invoke(resolvedInvulnerabilityDuration);
            }

            DamageInfo blockedDamageInfo = new(0f, damageInfo.HitDirection, damageInfo.Source, damageInfo.KnockbackForce);
            DamagedWithInfo?.Invoke(blockedDamageInfo);
            Damaged?.Invoke();
            GameplayFeedbackEvents.RaiseFloatingFeedback(new FloatingFeedbackRequest(
                ResolveDamageFeedbackPosition(in blockedDamageInfo),
                "SPEED HEART",
                new Color(0.42f, 0.82f, 1f, 1f),
                0.62f,
                0.72f,
                1.16f,
                visualProfile: FloatingFeedbackVisualProfile.Pickup));
            GameAudioEvents.Raise(GameAudioEventType.PlayerDamaged, transform.position, false, 0.82f, 1.08f);
            return true;
        }

        private void ResolveSpeedBuffState()
        {
            if (speedBuffState != null)
            {
                return;
            }

            speedBuffState = GetComponent<PlayerSpeedBuffState>();
            if (speedBuffState == null)
            {
                speedBuffState = gameObject.AddComponent<PlayerSpeedBuffState>();
            }
        }

        private float ResolveSpeedHeartBuffDuration()
        {
            return _speedHeartBuffDuration > 0f
                ? _speedHeartBuffDuration
                : defaultSpeedHeartBuffDuration;
        }

        private float ResolveSpeedHeartMoveSpeedMultiplier()
        {
            return _speedHeartMoveSpeedMultiplier > 0f
                ? _speedHeartMoveSpeedMultiplier
                : defaultSpeedHeartMoveSpeedMultiplier;
        }

        private Vector3 ResolveDamageFeedbackPosition(in DamageInfo damageInfo)
        {
            Vector3 anchorPosition = playerVisual != null && playerVisual.HitEffectAnchor != null
                ? playerVisual.HitEffectAnchor.position
                : transform.position;
            Vector2 hitDirection = damageInfo.HitDirection.sqrMagnitude > 0.0001f
                ? damageInfo.HitDirection.normalized
                : Vector2.zero;
            return anchorPosition + new Vector3(hitDirection.x * 0.12f, 0.22f, 0f);
        }
    }
}
