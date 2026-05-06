using UnityEngine;

namespace CuteIssac.Enemy
{
    /// <summary>
    /// Optional adapter for Dubai Sticky Rabbit exploders.
    /// It drives horror visuals only during the exploder windup and exposes rabbit-specific charge tuning.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DubaiStickyRabbitExploderController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private EnemyHorrorPhaseController horrorPhaseController;

        [Header("Charge Rules")]
        [SerializeField] private bool stopWhileCharging = true;
        [SerializeField] private bool cancelChargeIfTargetLeavesRange = true;
        [SerializeField] [Min(1f)] private float cancelRangeMultiplier = 1.35f;

        [Header("Optional Overrides")]
        [SerializeField] private bool overrideWindupDuration = true;
        [SerializeField] [Min(0f)] private float windupDuration = 1f;
        [SerializeField] private bool overrideTriggerRange;
        [SerializeField] [Min(0f)] private float triggerRange = 2.5f;
        [SerializeField] private bool overrideExplosionRadius;
        [SerializeField] [Min(0f)] private float explosionRadius = 1.75f;
        [SerializeField] private bool overrideExplosionDamage;
        [SerializeField] [Min(0f)] private float explosionDamage = 1f;
        [SerializeField] private bool overrideExplosionKnockback;
        [SerializeField] [Min(0f)] private float explosionKnockback = 6f;

        public bool StopWhileCharging => stopWhileCharging;
        public bool CancelChargeIfTargetLeavesRange => cancelChargeIfTargetLeavesRange;
        public float CancelRangeMultiplier => Mathf.Max(1f, cancelRangeMultiplier);

        private void Awake()
        {
            ResolveReferences();
        }

        public float ResolveWindupDuration(float fallback)
        {
            return overrideWindupDuration ? Mathf.Max(0f, windupDuration) : fallback;
        }

        public float ResolveTriggerRange(float fallback)
        {
            return overrideTriggerRange ? Mathf.Max(0f, triggerRange) : fallback;
        }

        public float ResolveExplosionRadius(float fallback)
        {
            return overrideExplosionRadius ? Mathf.Max(0f, explosionRadius) : fallback;
        }

        public float ResolveExplosionDamage(float fallback)
        {
            return overrideExplosionDamage ? Mathf.Max(0f, explosionDamage) : fallback;
        }

        public float ResolveExplosionKnockback(float fallback)
        {
            return overrideExplosionKnockback ? Mathf.Max(0f, explosionKnockback) : fallback;
        }

        public void HandleChargeStarted()
        {
            horrorPhaseController?.EnterHorrorPhaseManual();
        }

        public void HandleChargeCancelled()
        {
            horrorPhaseController?.ExitHorrorPhaseManual();
        }

        public void HandleExploded()
        {
            // Explosion immediately kills the rabbit, so no visual reset is required here.
        }

        public void HandleResetForSpawn()
        {
            horrorPhaseController?.ResetPhase();
        }

        private void ResolveReferences()
        {
            if (horrorPhaseController == null)
            {
                horrorPhaseController = GetComponent<EnemyHorrorPhaseController>();
            }
        }

        private void Reset()
        {
            ResolveReferences();
        }

        private void OnValidate()
        {
            ResolveReferences();
            cancelRangeMultiplier = Mathf.Max(1f, cancelRangeMultiplier);
            windupDuration = Mathf.Max(0f, windupDuration);
            triggerRange = Mathf.Max(0f, triggerRange);
            explosionRadius = Mathf.Max(0f, explosionRadius);
            explosionDamage = Mathf.Max(0f, explosionDamage);
            explosionKnockback = Mathf.Max(0f, explosionKnockback);
        }
    }
}
