using CuteIssac.Data.Enemy;
using CuteIssac.Combat;
using CuteIssac.Core.Feedback;
using UnityEngine;

namespace CuteIssac.Enemy
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(EnemyController))]
    [RequireComponent(typeof(EnemyMovement))]
    [RequireComponent(typeof(EnemyHealth))]
    [RequireComponent(typeof(EnemyVisual))]
    public sealed class ChampionEnemyModifier : MonoBehaviour
    {
        [SerializeField] private EnemyController enemyController;
        [SerializeField] private EnemyMovement enemyMovement;
        [SerializeField] private EnemyHealth enemyHealth;
        [SerializeField] private EnemyVisual enemyVisual;
        [SerializeField] private DamageArea damageArea;
        [SerializeField] private Collider2D ownerCollider;

        [Header("Volatile Champion")]
        [SerializeField] [Min(0.1f)] private float volatileExplosionRadius = 1.8f;
        [SerializeField] [Min(0f)] private float volatileExplosionDamageMultiplier = 1.35f;
        [SerializeField] [Min(0f)] private float volatileExplosionKnockback = 6f;
        [SerializeField] private Color volatileExplosionFeedbackColor = new(1f, 0.46f, 0.34f, 1f);

        [Header("Swift Champion")]
        [SerializeField] [Min(0.1f)] private float swiftBurstInterval = 2.4f;
        [SerializeField] [Min(0.05f)] private float swiftBurstDuration = 0.55f;
        [SerializeField] [Min(1f)] private float swiftBurstSpeedMultiplier = 1.5f;
        [SerializeField] [Min(0f)] private float swiftBurstMinimumMoveInput = 0.2f;
        [SerializeField] private Color swiftBurstFeedbackColor = new(0.42f, 0.9f, 1f, 1f);

        [Header("Bulwark Champion")]
        [SerializeField] [Min(0.1f)] private float bulwarkGuardInterval = 2.9f;
        [SerializeField] [Min(0.05f)] private float bulwarkGuardDuration = 0.85f;
        [SerializeField] [Range(0f, 1f)] private float bulwarkGuardDamageMultiplier = 0.35f;
        [SerializeField] [Min(0f)] private float bulwarkGuardFeedbackCooldown = 0.22f;
        [SerializeField] private Color bulwarkGuardFeedbackColor = new(1f, 0.78f, 0.35f, 1f);

        public bool IsChampion { get; private set; }
        public string VariantId { get; private set; }
        public string VariantLabel { get; private set; }
        public Color VariantAccentColor { get; private set; } = Color.white;
        public bool IsBulwarkGuardActive => IsBulwarkChampion() && _bulwarkGuardRemaining > 0f;

        private bool _hasBaselineState;
        private float _baselineMoveSpeed;
        private float _baselineBaseMaxHealth;
        private float _baselineContactDamage;
        private bool _volatileDeathBurstConsumed;
        private float _swiftBurstCooldownRemaining;
        private float _swiftBurstRemaining;
        private float _bulwarkGuardCooldownRemaining;
        private float _bulwarkGuardRemaining;
        private float _bulwarkFeedbackCooldownRemaining;

        private void Awake()
        {
            ResolveReferences();
            SubscribeHealthEvents();
        }

        private void OnDisable()
        {
            if (Application.isPlaying)
            {
                ClearChampionState();
            }
        }

        private void OnDestroy()
        {
            UnsubscribeHealthEvents();
        }

        private void Update()
        {
            UpdateSwiftBurst(Time.deltaTime);
            UpdateBulwarkGuard(Time.deltaTime);
        }

        public void PrepareForSpawn()
        {
            ResolveReferences();

            if (!_hasBaselineState)
            {
                CaptureBaselineState();
            }

            _volatileDeathBurstConsumed = false;
            _swiftBurstCooldownRemaining = 0f;
            _swiftBurstRemaining = 0f;
            _bulwarkGuardCooldownRemaining = 0f;
            _bulwarkGuardRemaining = 0f;
            _bulwarkFeedbackCooldownRemaining = 0f;
            enemyMovement?.SetExternalSpeedMultiplier(1f);
            ClearChampionState();
        }

        public void ApplyChampion(ChampionEnemyProfile.VariantSettings variant)
        {
            if (variant == null)
            {
                return;
            }

            PrepareForSpawn();
            IsChampion = true;
            VariantId = variant.VariantId;
            VariantLabel = variant.DisplayName;
            VariantAccentColor = variant.AccentColor;

            enemyMovement?.SetBaseMoveSpeed(_baselineMoveSpeed * variant.MoveSpeedMultiplier);
            enemyHealth?.SetMaxHealth(_baselineBaseMaxHealth * variant.MaxHealthMultiplier);
            enemyController?.SetContactDamage(_baselineContactDamage * variant.ContactDamageMultiplier);
            enemyVisual?.ApplyChampionPresentation(variant.VariantId, variant.AccentColor, variant.ColorBlend, variant.VisualScaleMultiplier);
        }

        public void ClearChampionState()
        {
            if (!_hasBaselineState)
            {
                return;
            }

            IsChampion = false;
            VariantId = string.Empty;
            VariantLabel = string.Empty;
            VariantAccentColor = Color.white;
            _swiftBurstCooldownRemaining = 0f;
            _swiftBurstRemaining = 0f;
            _bulwarkGuardCooldownRemaining = 0f;
            _bulwarkGuardRemaining = 0f;
            _bulwarkFeedbackCooldownRemaining = 0f;
            enemyMovement?.SetBaseMoveSpeed(_baselineMoveSpeed);
            enemyMovement?.SetExternalSpeedMultiplier(1f);
            enemyHealth?.SetMaxHealth(_baselineBaseMaxHealth);
            enemyController?.SetContactDamage(_baselineContactDamage);
            enemyVisual?.ResetChampionPresentation();
            enemyVisual?.SetChampionGuardState(false, VariantAccentColor);
        }

        public float ModifyIncomingDamage(float rawDamage, in Common.Combat.DamageInfo damageInfo)
        {
            float clampedDamage = Mathf.Max(0f, rawDamage);

            if (!IsChampion || clampedDamage <= 0f || enemyHealth == null || enemyHealth.IsDead)
            {
                return clampedDamage;
            }

            if (!IsBulwarkChampion() || _bulwarkGuardRemaining <= 0f)
            {
                return clampedDamage;
            }

            float reducedDamage = clampedDamage * bulwarkGuardDamageMultiplier;
            float preventedDamage = clampedDamage - reducedDamage;

            if (_bulwarkFeedbackCooldownRemaining <= 0f && preventedDamage > 0.01f)
            {
                _bulwarkFeedbackCooldownRemaining = bulwarkGuardFeedbackCooldown;
                GameplayFeedbackEvents.RaiseFloatingFeedback(new FloatingFeedbackRequest(
                    transform.position + (Vector3.up * 1.05f),
                    "GUARD",
                    Color.Lerp(bulwarkGuardFeedbackColor, VariantAccentColor, 0.62f),
                    0.42f,
                    0.48f,
                    0.72f,
                    visualProfile: FloatingFeedbackVisualProfile.EventLabel));
            }

            return Mathf.Max(0f, reducedDamage);
        }

        private void CaptureBaselineState()
        {
            _baselineMoveSpeed = enemyMovement != null ? enemyMovement.MoveSpeed : 0f;
            _baselineBaseMaxHealth = enemyHealth != null ? enemyHealth.BaseMaxHealth : 1f;
            _baselineContactDamage = enemyController != null ? enemyController.ContactDamage : 0f;
            _hasBaselineState = true;
        }

        private void ResolveReferences()
        {
            if (enemyController == null)
            {
                enemyController = GetComponent<EnemyController>();
            }

            if (enemyMovement == null)
            {
                enemyMovement = GetComponent<EnemyMovement>();
            }

            if (enemyHealth == null)
            {
                enemyHealth = GetComponent<EnemyHealth>();
            }

            if (enemyVisual == null)
            {
                enemyVisual = GetComponent<EnemyVisual>();
            }

            if (damageArea == null)
            {
                damageArea = GetComponent<DamageArea>();
            }

            if (ownerCollider == null)
            {
                ownerCollider = GetComponent<Collider2D>();
            }
        }

        private void SubscribeHealthEvents()
        {
            if (enemyHealth == null)
            {
                return;
            }

            enemyHealth.Died -= HandleEnemyDied;
            enemyHealth.Died += HandleEnemyDied;
        }

        private void UnsubscribeHealthEvents()
        {
            if (enemyHealth == null)
            {
                return;
            }

            enemyHealth.Died -= HandleEnemyDied;
        }

        private void HandleEnemyDied()
        {
            if (!IsChampion || _volatileDeathBurstConsumed || !IsVolatileChampion())
            {
                return;
            }

            _volatileDeathBurstConsumed = true;
            TriggerVolatileDeathBurst();
        }

        private void TriggerVolatileDeathBurst()
        {
            if (damageArea == null)
            {
                damageArea = gameObject.AddComponent<DamageArea>();
            }

            float explosionDamage = Mathf.Max(1f, _baselineContactDamage * volatileExplosionDamageMultiplier);
            BombExplosionInfo explosionInfo = new(
                transform.position,
                volatileExplosionRadius,
                explosionDamage,
                volatileExplosionKnockback,
                transform);
            damageArea.ApplyExplosion(in explosionInfo, ownerCollider);

            GameplayFeedbackEvents.RaiseFloatingFeedback(new FloatingFeedbackRequest(
                transform.position + (Vector3.up * 1.05f),
                "VOLATILE",
                Color.Lerp(volatileExplosionFeedbackColor, VariantAccentColor, 0.6f),
                0.62f,
                0.76f,
                1.15f,
                visualProfile: FloatingFeedbackVisualProfile.EventLabel));
        }

        private void UpdateSwiftBurst(float deltaTime)
        {
            if (!IsChampion || enemyMovement == null || enemyHealth == null || enemyHealth.IsDead)
            {
                return;
            }

            if (!IsSwiftChampion())
            {
                enemyMovement.SetExternalSpeedMultiplier(1f);
                return;
            }

            if (_swiftBurstRemaining > 0f)
            {
                _swiftBurstRemaining = Mathf.Max(0f, _swiftBurstRemaining - deltaTime);
                enemyMovement.SetExternalSpeedMultiplier(swiftBurstSpeedMultiplier);

                if (_swiftBurstRemaining <= 0f)
                {
                    enemyMovement.SetExternalSpeedMultiplier(1f);
                    _swiftBurstCooldownRemaining = swiftBurstInterval;
                }

                return;
            }

            enemyMovement.SetExternalSpeedMultiplier(1f);
            _swiftBurstCooldownRemaining = Mathf.Max(0f, _swiftBurstCooldownRemaining - deltaTime);

            if (_swiftBurstCooldownRemaining > 0f)
            {
                return;
            }

            if (enemyMovement.CurrentMoveDirection.sqrMagnitude < (swiftBurstMinimumMoveInput * swiftBurstMinimumMoveInput))
            {
                return;
            }

            _swiftBurstRemaining = swiftBurstDuration;
            enemyMovement.SetExternalSpeedMultiplier(swiftBurstSpeedMultiplier);
            GameplayFeedbackEvents.RaiseFloatingFeedback(new FloatingFeedbackRequest(
                transform.position + (Vector3.up * 1.05f),
                "SWIFT",
                Color.Lerp(swiftBurstFeedbackColor, VariantAccentColor, 0.7f),
                0.42f,
                0.48f,
                0.7f,
                visualProfile: FloatingFeedbackVisualProfile.EventLabel));
        }

        private void UpdateBulwarkGuard(float deltaTime)
        {
            if (!IsChampion || enemyHealth == null || enemyHealth.IsDead)
            {
                return;
            }

            _bulwarkFeedbackCooldownRemaining = Mathf.Max(0f, _bulwarkFeedbackCooldownRemaining - deltaTime);

            if (!IsBulwarkChampion())
            {
                _bulwarkGuardRemaining = 0f;
                enemyVisual?.SetChampionGuardState(false, VariantAccentColor);
                return;
            }

            if (_bulwarkGuardRemaining > 0f)
            {
                _bulwarkGuardRemaining = Mathf.Max(0f, _bulwarkGuardRemaining - deltaTime);
                enemyVisual?.SetChampionGuardState(true, VariantAccentColor);

                if (_bulwarkGuardRemaining <= 0f)
                {
                    _bulwarkGuardCooldownRemaining = bulwarkGuardInterval;
                    enemyVisual?.SetChampionGuardState(false, VariantAccentColor);
                }

                return;
            }

            enemyVisual?.SetChampionGuardState(false, VariantAccentColor);
            _bulwarkGuardCooldownRemaining = Mathf.Max(0f, _bulwarkGuardCooldownRemaining - deltaTime);

            if (_bulwarkGuardCooldownRemaining > 0f)
            {
                return;
            }

            _bulwarkGuardRemaining = bulwarkGuardDuration;
            enemyVisual?.SetChampionGuardState(true, VariantAccentColor);
        }

        private bool IsVolatileChampion()
        {
            return string.Equals(VariantId, "explosive", System.StringComparison.OrdinalIgnoreCase);
        }

        private bool IsSwiftChampion()
        {
            return string.Equals(VariantId, "fast", System.StringComparison.OrdinalIgnoreCase);
        }

        private bool IsBulwarkChampion()
        {
            return string.Equals(VariantId, "tank", System.StringComparison.OrdinalIgnoreCase);
        }

        private void Reset()
        {
            ResolveReferences();
            SubscribeHealthEvents();
        }

        private void OnValidate()
        {
            ResolveReferences();
        }
    }
}
