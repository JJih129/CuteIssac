using System.Collections.Generic;
using CuteIssac.Combat;
using CuteIssac.Common.Stats;
using CuteIssac.Core.Gameplay;
using CuteIssac.Data.Dungeon;
using CuteIssac.Dungeon;
using CuteIssac.Enemy;
using CuteIssac.Room;
using UnityEngine;

namespace CuteIssac.Player
{
    /// <summary>
    /// Converts a locked or pivoted route plan into a short opening buff when the player enters the next combat room.
    /// This keeps route planning tied to actual combat tempo instead of ending at UI.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerStats))]
    [RequireComponent(typeof(PlayerHealth))]
    public sealed class PlayerRoutePlanCarryController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerStats playerStats;
        [SerializeField] private PlayerHealth playerHealth;
        [SerializeField] private PlayerInteractionReceiptPresentation playerReceiptPresentation;
        [SerializeField] private PlayerCombatMomentumController momentumController;
        [SerializeField] private CombatOpeningTargetHintController openingTargetHintController;
        [SerializeField] private RoomNavigationController roomNavigationController;

        [Header("Pending Carry")]
        [SerializeField] [Min(1f)] private float pendingCarryDuration = 18f;

        [Header("Opening Window")]
        [SerializeField] [Min(0.25f)] private float baseOpeningDuration = 3.8f;
        [SerializeField] [Min(0f)] private float heldPlanDurationBonus = 0.8f;
        [SerializeField] [Min(0f)] private float eliteOpeningDurationBonus = 0.4f;
        [SerializeField] [Min(0f)] private float bossOpeningDurationBonus = 0.65f;
        [SerializeField] private bool breakOpeningOnDamage = true;
        [SerializeField] [Range(0.5f, 1f)] private float pivotPlanStrengthScale = 0.72f;

        [Header("Opening Conversion")]
        [SerializeField] [Min(0.4f)] private float maximumSustainedOpeningDuration = 5.6f;
        [SerializeField] [Min(0f)] private float baseExecutionSustain = 0.18f;
        [SerializeField] [Min(0f)] private float hintedTargetSustainBonus = 0.18f;
        [SerializeField] [Min(0f)] private float focusExecutionSustainBonus = 0.1f;
        [SerializeField] [Min(0f)] private float criticalExecutionSustainBonus = 0.22f;
        [SerializeField] [Min(0f)] private float eliteExecutionSustainBonus = 0.16f;
        [SerializeField] [Min(0f)] private float bossExecutionSustainBonus = 0.32f;
        [SerializeField] [Min(0f)] private float momentumConversionScale = 0.35f;

        [Header("Opening Collapse")]
        [SerializeField] [Min(0f)] private float openingCollapseFreezeDuration = 0.18f;
        [SerializeField] [Min(0f)] private float openingCollapseHintedBonus = 0.08f;
        [SerializeField] [Min(0f)] private float openingCollapseCriticalBonus = 0.1f;
        [SerializeField] [Min(0f)] private float openingCollapseEliteBonus = 0.08f;
        [SerializeField] [Min(0f)] private float openingCollapseBossBonus = 0.16f;
        [SerializeField] [Min(0.1f)] private float openingCollapseFeedbackDuration = 1.2f;

        [Header("Breakthrough Chain")]
        [SerializeField] [Min(0f)] private float breakthroughChainOpeningBonus = 0.32f;
        [SerializeField] [Min(0f)] private float breakthroughChainMomentumBonus = 0.22f;
        [SerializeField] [Min(0.1f)] private float breakthroughChainFeedbackDuration = 1.15f;

        [Header("Breakthrough Pressure")]
        [SerializeField] [Min(0.02f)] private float breakthroughPressureUpdateInterval = 0.08f;
        [SerializeField] [Range(0.1f, 1f)] private float breakthroughPressureSpeedMultiplier = 0.72f;
        [SerializeField] [Range(0.1f, 1f)] private float breakthroughChainPressureSpeedMultiplier = 0.58f;
        [SerializeField] [Range(0.1f, 1f)] private float breakthroughBossPressureSpeedMultiplier = 0.82f;
        [SerializeField] [Range(0.1f, 1f)] private float breakthroughPressureContactMultiplier = 0.68f;
        [SerializeField] [Range(0.1f, 1f)] private float breakthroughChainPressureContactMultiplier = 0.48f;
        [SerializeField] [Range(0.1f, 1f)] private float breakthroughBossPressureContactMultiplier = 0.84f;
        [SerializeField] [Min(0f)] private float breakthroughPressureLanePadding = 0.18f;
        [SerializeField] [Min(0f)] private float breakthroughPressurePushImpulse = 0.22f;
        [SerializeField] [Min(0f)] private float breakthroughChainPressurePushImpulse = 0.34f;
        [SerializeField] [Min(0f)] private float breakthroughBossPressurePushImpulse = 0.14f;
        [SerializeField] [Range(0f, 1f)] private float breakthroughPressurePushForwardBias = 0.16f;

        [Header("Breakthrough Drive")]
        [SerializeField] [Range(1f, 1.3f)] private float breakthroughDriveMoveSpeedMultiplier = 1.08f;
        [SerializeField] [Range(1f, 1.24f)] private float breakthroughDriveFireRateMultiplier = 1.05f;
        [SerializeField] [Range(1f, 1.28f)] private float breakthroughDriveProjectileSpeedMultiplier = 1.08f;
        [SerializeField] [Range(1f, 1.3f)] private float breakthroughDriveKnockbackMultiplier = 1.1f;
        [SerializeField] [Range(0f, 0.25f)] private float breakthroughChainDriveBonus = 0.08f;
        [SerializeField] [Range(0.1f, 1f)] private float breakthroughDriveDamageMultiplier = 0.74f;
        [SerializeField] [Range(0.1f, 1f)] private float breakthroughChainDriveDamageMultiplier = 0.58f;
        [SerializeField] [Min(0f)] private float breakthroughDriveInvulnerabilityBonus = 0.12f;
        [SerializeField] [Min(0f)] private float breakthroughChainDriveInvulnerabilityBonus = 0.22f;
        [SerializeField] [Min(0f)] private float breakthroughDrivePierceBonus = 1f;
        [SerializeField] [Min(0f)] private float breakthroughChainDrivePierceBonus = 2f;
        [SerializeField] [Min(0f)] private float breakthroughDriveScaleBonus = 0.12f;
        [SerializeField] [Min(0f)] private float breakthroughChainDriveScaleBonus = 0.2f;

        [Header("Breakthrough Aim Assist")]
        [SerializeField] [Range(0f, 30f)] private float breakthroughDriveAimAssistDegrees = 12f;
        [SerializeField] [Range(0f, 35f)] private float breakthroughChainDriveAimAssistDegrees = 20f;
        [SerializeField] [Range(0f, 1f)] private float breakthroughDriveAimAssistStrength = 0.26f;
        [SerializeField] [Range(0f, 1f)] private float breakthroughChainDriveAimAssistStrength = 0.42f;

        [Header("Impact Hold")]
        [SerializeField] [Range(1f, 1.18f)] private float impactHoldMoveSpeedMultiplier = 1.04f;
        [SerializeField] [Range(1f, 1.22f)] private float impactHoldFireRateMultiplier = 1.06f;
        [SerializeField] [Range(1f, 1.24f)] private float impactHoldProjectileSpeedMultiplier = 1.08f;
        [SerializeField] [Range(1f, 1.24f)] private float impactHoldKnockbackMultiplier = 1.08f;
        [SerializeField] [Range(0f, 0.16f)] private float impactHoldChainBonus = 0.06f;
        [SerializeField] [Range(0.1f, 1f)] private float impactHoldDamageMultiplier = 0.88f;
        [SerializeField] [Range(0.1f, 1f)] private float impactChainHoldDamageMultiplier = 0.76f;
        [SerializeField] [Min(0f)] private float impactHoldInvulnerabilityBonus = 0.08f;
        [SerializeField] [Min(0f)] private float impactChainHoldInvulnerabilityBonus = 0.16f;
        [SerializeField] [Min(0f)] private float impactHoldPierceBonus = 1f;
        [SerializeField] [Min(0f)] private float impactChainHoldPierceBonus = 2f;
        [SerializeField] [Min(0f)] private float impactHoldScaleBonus = 0.08f;
        [SerializeField] [Min(0f)] private float impactChainHoldScaleBonus = 0.14f;
        [SerializeField] [Range(0f, 20f)] private float impactHoldAimAssistDegreesBonus = 6f;
        [SerializeField] [Range(0f, 1f)] private float impactHoldAimAssistStrengthBonus = 0.14f;
        [SerializeField] [Range(1f, 1.18f)] private float impactPreferredSecureDriveMultiplier = 1.06f;
        [SerializeField] [Range(0.1f, 1f)] private float impactPreferredSecureDamageMultiplier = 0.9f;
        [SerializeField] [Min(0f)] private float impactPreferredSecureInvulnerabilityBonus = 0.06f;
        [SerializeField] [Min(0f)] private float impactPreferredSecurePierceBonus = 1f;
        [SerializeField] [Min(0f)] private float impactPreferredSecureScaleBonus = 0.08f;
        [SerializeField] [Range(1f, 1.18f)] private float impactRecentPreferredHitDriveMultiplier = 1.08f;
        [SerializeField] [Range(0.1f, 1f)] private float impactRecentPreferredHitDamageMultiplier = 0.88f;
        [SerializeField] [Min(0f)] private float impactRecentPreferredHitInvulnerabilityBonus = 0.08f;
        [SerializeField] [Min(0f)] private float impactRecentPreferredHitPierceBonus = 1f;
        [SerializeField] [Min(0f)] private float impactRecentPreferredHitScaleBonus = 0.1f;

        [Header("Impact Secure")]
        [SerializeField] [Min(0f)] private float impactSecureOpeningBonus = 0.16f;
        [SerializeField] [Min(0f)] private float impactChainSecureOpeningBonus = 0.28f;
        [SerializeField] [Min(0f)] private float impactSecureMomentumBonus = 0.08f;
        [SerializeField] [Min(0f)] private float impactChainSecureMomentumBonus = 0.16f;
        [SerializeField] [Min(0f)] private float impactSecureProjectileClearRadius = 0.92f;
        [SerializeField] [Min(0f)] private float impactChainSecureProjectileClearRadius = 1.28f;
        [SerializeField] [Min(0f)] private float impactSecureHazardClearRadius = 0.98f;
        [SerializeField] [Min(0f)] private float impactChainSecureHazardClearRadius = 1.3f;
        [SerializeField] [Range(1f, 1.4f)] private float impactSecureReliefRadiusScale = 1.14f;
        [SerializeField] [Min(0.1f)] private float impactSecureReliefDuration = 0.72f;
        [SerializeField] [Min(0.1f)] private float impactChainSecureReliefDuration = 1.04f;
        [SerializeField] [Min(0.1f)] private float impactSecureFeedbackDuration = 0.92f;

        [Header("Impact Secure Hold")]
        [SerializeField] [Min(0.1f)] private float impactSecureHoldDuration = 1.45f;
        [SerializeField] [Min(0.1f)] private float impactChainSecureHoldDuration = 2.05f;
        [SerializeField] [Min(0.02f)] private float impactSecurePulseInterval = 0.32f;
        [SerializeField] [Min(0.02f)] private float impactChainSecurePulseInterval = 0.22f;
        [SerializeField] [Min(0f)] private float impactSecurePulseOpeningBonus = 0.04f;
        [SerializeField] [Min(0f)] private float impactChainSecurePulseOpeningBonus = 0.08f;
        [SerializeField] [Min(0f)] private float impactSecurePulseProjectileClearRadius = 0.72f;
        [SerializeField] [Min(0f)] private float impactChainSecurePulseProjectileClearRadius = 1.04f;
        [SerializeField] [Min(0f)] private float impactSecurePulseHazardClearRadius = 0.76f;
        [SerializeField] [Min(0f)] private float impactChainSecurePulseHazardClearRadius = 1.08f;
        [SerializeField] [Min(0.1f)] private float impactSecurePulseReliefDuration = 0.34f;
        [SerializeField] [Min(0.1f)] private float impactChainSecurePulseReliefDuration = 0.48f;
        [SerializeField] [Min(0.1f)] private float impactSecurePulseFeedbackDuration = 0.52f;
        [SerializeField] [Range(0f, 0.3f)] private float impactPreferredSecureOpeningScaleBonus = 0.12f;
        [SerializeField] [Range(0f, 0.35f)] private float impactPreferredSecurePulseScaleBonus = 0.16f;
        [SerializeField] [Range(0f, 0.35f)] private float impactPreferredSecureRadiusScaleBonus = 0.18f;

        [Header("Breakthrough Projectile Pressure")]
        [SerializeField] [Range(0.1f, 1f)] private float breakthroughProjectileSpeedMultiplier = 0.72f;
        [SerializeField] [Range(0.1f, 1f)] private float breakthroughChainProjectileSpeedMultiplier = 0.46f;
        [SerializeField] [Range(0f, 1f)] private float breakthroughProjectileLateralBias = 0.24f;
        [SerializeField] [Range(0f, 1f)] private float breakthroughChainProjectileLateralBias = 0.42f;
        [SerializeField] [Min(0f)] private float breakthroughProjectileLanePadding = 0.12f;
        [SerializeField] [Min(0f)] private float breakthroughProjectileClearRadius = 0.72f;
        [SerializeField] [Range(0f, 0.5f)] private float breakthroughProjectileChainClearProgressWindow = 0.18f;

        [Header("Breakthrough Hit Pulse")]
        [SerializeField] [Min(0f)] private float breakthroughHitSustain = 0.08f;
        [SerializeField] [Min(0f)] private float breakthroughChainHitSustain = 0.14f;
        [SerializeField] [Min(0f)] private float breakthroughHitProjectileClearRadius = 0.92f;
        [SerializeField] [Min(0f)] private float breakthroughChainProjectileClearRadius = 1.26f;
        [SerializeField] [Min(0f)] private float breakthroughHitHazardClearRadius = 0.96f;
        [SerializeField] [Min(0f)] private float breakthroughChainHazardClearRadius = 1.24f;
        [SerializeField] [Min(0.1f)] private float breakthroughHitImpactReliefDuration = 0.58f;
        [SerializeField] [Min(0.1f)] private float breakthroughChainImpactReliefDuration = 0.92f;
        [SerializeField] [Min(0.02f)] private float breakthroughHitPulseCooldown = 0.22f;
        [SerializeField] [Min(0.1f)] private float breakthroughHitFeedbackDuration = 0.86f;

        [Header("Preferred Secure Hit Pulse")]
        [SerializeField] [Min(0f)] private float preferredImpactHitSustainBonus = 0.08f;
        [SerializeField] [Range(1f, 1.6f)] private float preferredImpactHitProjectileClearScale = 1.18f;
        [SerializeField] [Range(1f, 1.6f)] private float preferredImpactHitHazardClearScale = 1.24f;
        [SerializeField] [Range(1f, 1.6f)] private float preferredImpactHitReliefScale = 1.16f;
        [SerializeField] [Range(0f, 0.98f)] private float preferredImpactHitAlignmentThreshold = 0.32f;
        [SerializeField] [Range(0f, 0.5f)] private float preferredImpactHitAnchorBlend = 0.24f;

        [Header("Breakthrough Sweep")]
        [SerializeField] [Min(0f)] private float breakthroughSweepImpulse = 4.2f;
        [SerializeField] [Min(0f)] private float breakthroughSweepBossImpulse = 2.4f;
        [SerializeField] [Range(0f, 1f)] private float breakthroughSweepForwardBias = 0.28f;
        [SerializeField] [Min(0f)] private float breakthroughSweepHazardPadding = 0.22f;

        [Header("Advantage Opener")]
        [SerializeField] [Range(1f, 1.4f)] private float advantageDamageMultiplier = 1.12f;
        [SerializeField] [Range(1f, 1.4f)] private float advantageFireRateMultiplier = 1.1f;
        [SerializeField] [Range(1f, 1.4f)] private float advantageKnockbackMultiplier = 1.08f;

        [Header("Recovery Opener")]
        [SerializeField] [Range(1f, 1.4f)] private float recoveryMoveSpeedMultiplier = 1.12f;
        [SerializeField] [Range(1f, 1.4f)] private float recoveryFireRateMultiplier = 1.05f;
        [SerializeField] [Range(1f, 1.4f)] private float recoveryKnockbackMultiplier = 1.08f;

        [Header("Supply Opener")]
        [SerializeField] [Range(1f, 1.4f)] private float supplyMoveSpeedMultiplier = 1.08f;
        [SerializeField] [Range(1f, 1.4f)] private float supplyProjectileSpeedMultiplier = 1.12f;
        [SerializeField] [Range(1f, 1.4f)] private float supplyRangeMultiplier = 1.08f;

        [Header("Build Opener")]
        [SerializeField] [Range(1f, 1.4f)] private float buildDamageMultiplier = 1.08f;
        [SerializeField] [Range(1f, 1.4f)] private float buildProjectileSpeedMultiplier = 1.08f;
        [SerializeField] [Range(1f, 1.4f)] private float buildRangeMultiplier = 1.06f;

        [Header("Clean Opener")]
        [SerializeField] [Range(1f, 1.4f)] private float cleanMoveSpeedMultiplier = 1.1f;
        [SerializeField] [Range(1f, 1.4f)] private float cleanFireRateMultiplier = 1.06f;

        [Header("Opening Shot Profile")]
        [SerializeField] [Min(0f)] private float openingShotProfileTraitBonus = 0.8f;
        [SerializeField] [Min(0f)] private float openingShotProfileScaleBonus = 0.12f;
        [SerializeField] [Min(0f)] private float openingShotProfileSpeedBonus = 0.14f;
        [SerializeField] [Min(0f)] private float openingShotProfilePierceBonus = 1f;
        [SerializeField] [Min(0f)] private float openingShotProfileMultiShotBonus = 1f;

        [Header("Opening Cadence")]
        [SerializeField] [Min(1)] private int openingCadenceBurstLimit = 3;
        [SerializeField] [Min(0f)] private float openingCadenceFirstBurstSustain = 0.06f;
        [SerializeField] [Min(0f)] private float openingCadenceRepeatBurstSustain = 0.03f;
        [SerializeField] [Min(0.1f)] private float openingCadenceFeedbackDuration = 0.78f;
        [SerializeField] [Min(0f)] private float openingCadenceTraitBonus = 0.45f;
        [SerializeField] [Range(1f, 1.25f)] private float openingCadenceDamageMultiplier = 1.06f;
        [SerializeField] [Range(1f, 1.25f)] private float openingCadenceSpeedMultiplier = 1.08f;
        [SerializeField] [Min(0f)] private float openingCadenceScaleBonus = 0.06f;
        [SerializeField] [Min(0f)] private float openingCadencePierceBonus = 1f;
        [SerializeField] [Range(0.5f, 1f)] private float openingCadenceFastIntervalMultiplier = 0.82f;
        [SerializeField] [Range(0.5f, 1f)] private float openingCadenceMediumIntervalMultiplier = 0.88f;
        [SerializeField] [Range(0.5f, 1f)] private float openingCadenceHeavyIntervalMultiplier = 0.94f;
        [SerializeField] [Range(0.5f, 1f)] private float openingCadenceResetIntervalMultiplier = 0.9f;
        [SerializeField] [Range(0.5f, 1f)] private float openingCadenceFinaleIntervalMultiplier = 0.76f;
        [SerializeField] [Range(0f, 0.16f)] private float openingCadenceIntervalRelaxPerBurst = 0.05f;
        [SerializeField] [Range(0f, 1f)] private float openingCadenceLaserSpreadCompression = 0.44f;
        [SerializeField] [Range(1f, 2f)] private float openingCadenceSplitSpreadExpansion = 1.35f;
        [SerializeField] [Range(0f, 1f)] private float openingCadenceOrbitSpreadCompression = 0.76f;
        [SerializeField] [Range(0f, 1f)] private float openingCadenceShieldSpreadCompression = 0.68f;
        [SerializeField] [Range(1f, 2f)] private float openingCadenceBounceSpreadExpansion = 1.28f;
        [SerializeField] [Range(1f, 2f)] private float openingCadenceBlastSpreadExpansion = 1.16f;
        [SerializeField] [Range(0f, 1f)] private float openingCadenceLeechSpreadCompression = 0.62f;
        [SerializeField] [Range(0f, 30f)] private float openingCadenceBaseSpreadDegrees = 9f;
        [SerializeField] [Range(0f, 12f)] private float openingCadenceOrbitVolleyTwistDegrees = 5f;
        [SerializeField] [Range(0f, 10f)] private float openingCadenceBounceVolleyTwistDegrees = 4f;
        [SerializeField] [Min(0)] private int openingCadenceLightVolleyBonusShots = 1;
        [SerializeField] [Min(0)] private int openingCadenceMediumVolleyBonusShots = 2;
        [SerializeField] [Min(0)] private int openingCadenceHeavyVolleyBonusShots = 1;

        [Header("Cadence Role Follow-Up")]
        [SerializeField] [Min(1)] private int recentCadenceRoleBurstLimit = 2;
        [SerializeField] [Min(0f)] private float recentCadenceRoleTraitBonus = 0.38f;
        [SerializeField] [Range(1f, 1.24f)] private float recentCadenceRoleDamageMultiplier = 1.08f;
        [SerializeField] [Range(1f, 1.24f)] private float recentCadenceRoleSpeedMultiplier = 1.1f;
        [SerializeField] [Min(0f)] private float recentCadenceRoleScaleBonus = 0.06f;
        [SerializeField] [Min(0f)] private float recentCadenceRolePierceBonus = 1f;
        [SerializeField] [Min(0f)] private float recentCadenceRoleHomingBonus = 0.14f;
        [SerializeField] [Min(0)] private int recentCadenceRoleCoreBonusShots = 1;
        [SerializeField] [Min(0)] private int recentCadenceRoleFlankBonusShots = 2;
        [SerializeField] [Min(0)] private int recentCadenceRoleEdgeBonusShots = 2;
        [SerializeField] [Range(0.5f, 1f)] private float recentCadenceRoleCoreIntervalMultiplier = 0.84f;
        [SerializeField] [Range(0.5f, 1f)] private float recentCadenceRoleFlankIntervalMultiplier = 0.9f;
        [SerializeField] [Range(0.5f, 1f)] private float recentCadenceRoleEdgeIntervalMultiplier = 0.88f;
        [SerializeField] [Range(0f, 0.2f)] private float recentCadenceRoleIntervalRelaxPerBurst = 0.08f;
        [SerializeField] [Range(1f, 1.4f)] private float recentPreferredImpactHitAssistMultiplier = 1.16f;
        [SerializeField] [Range(1f, 1.35f)] private float recentPreferredImpactHitRoleStrengthMultiplier = 1.14f;
        [SerializeField] [Range(0f, 0.35f)] private float recentPreferredImpactHitSlotBiasBonus = 0.16f;

        private readonly List<StatModifier> _routePlanStatModifiers = new();
        private readonly List<ProjectileModifier> _routePlanProjectileModifiers = new();
        private readonly List<StatModifier> _breakthroughDriveStatModifiers = new();
        private readonly List<ProjectileModifier> _breakthroughDriveProjectileModifiers = new();
        private readonly List<EnemyHealth> _roomEnemyBuffer = new();
        private readonly List<EnemyController> _breakthroughPressureControllers = new();
        private readonly List<EnemyProjectileLogic> _breakthroughProjectileBuffer = new();
        private readonly List<Vector2> _impactPulseAnchorBuffer = new();
        private readonly List<Vector2> _impactHoldAnchorBuffer = new();
        private readonly List<Vector2> _impactAnchorSelectionBuffer = new();
        private RoomNavigationController _boundNavigationController;
        private RoomController _observedRoom;
        private RoomController _pendingSourceRoom;
        private RoomController _activeRoom;
        private float _pendingExpiresAt = float.NegativeInfinity;
        private float _activeExpiresAt = float.NegativeInfinity;
        private float _recentCollapseExpiresAt = float.NegativeInfinity;
        private float _recentBreakthroughChainExpiresAt = float.NegativeInfinity;
        private string _pendingReasonTag = string.Empty;
        private string _pendingCompareLabel = string.Empty;
        private string _activeReasonTag = string.Empty;
        private string _activeCompareLabel = string.Empty;
        private string _recentCollapseLabel = string.Empty;
        private string _recentBreakthroughChainLabel = string.Empty;
        private Color _pendingAccentColor = Color.white;
        private Color _activeAccentColor = Color.white;
        private bool _pendingHeldPlan;
        private bool _activeHeldPlan;
        private bool _pendingBreakthroughChain;
        private bool _breakthroughDriveActive;
        private bool _breakthroughDriveChainActive;
        private bool _impactHoldActive;
        private bool _preferredImpactHoldDriveActive;
        private float _preferredImpactHoldDriveSide;
        private bool _recentPreferredImpactDriveActive;
        private float _recentPreferredImpactDriveSide;
        private float _recentPreferredImpactDriveStrength;
        private bool _impactSecureHoldChainActive;
        private bool _preferredImpactSecureHoldActive;
        private int _openingExecutionCount;
        private float _nextBreakthroughPressureUpdateAt;
        private float _nextBreakthroughHitPulseAt;
        private float _impactSecureHoldExpiresAt = float.NegativeInfinity;
        private float _nextImpactSecurePulseAt;
        private float _recentOpeningCadenceExpiresAt = float.NegativeInfinity;
        private float _recentOpeningCadenceHitRoleExpiresAt = float.NegativeInfinity;
        private float _recentPreferredImpactHitExpiresAt = float.NegativeInfinity;
        private int _openingCadenceBurstCount;
        private int _recentOpeningCadenceHitRoleBurstCount;
        private string _recentOpeningCadenceLabel = string.Empty;
        private OpeningCadenceVolleyRole _recentOpeningCadenceHitRole;
        private float _recentOpeningCadenceHitRoleWeight;
        private float _recentPreferredImpactHitSide;
        private float _recentPreferredImpactHitStrength;

        public event System.Action OpeningStateChanged;

        public bool IsOpeningActive => HasActiveOpening();
        public bool IsBreakthroughChainReady => _pendingBreakthroughChain && HasActiveOpeningBreakthrough();
        public string ActiveReasonTag => _activeReasonTag;
        public string ActiveCompareLabel => _activeCompareLabel;
        public Color ActiveAccentColor => _activeAccentColor;
        public bool ActiveHeldPlan => _activeHeldPlan;
        public RoomController ActiveRoom => _activeRoom;
        public int OpeningExecutionCount => _openingExecutionCount;
        public bool HasOpeningTargetHint => openingTargetHintController != null && openingTargetHintController.HasActiveTarget;
        public string OpeningTargetCompactTag => openingTargetHintController != null ? openingTargetHintController.ActiveTargetCompactTag : string.Empty;
        public string OpeningTargetDirective => openingTargetHintController != null ? openingTargetHintController.ActiveTargetDirective : string.Empty;
        public string OpeningTargetDetail => openingTargetHintController != null ? openingTargetHintController.ActiveTargetDetail : string.Empty;
        public bool HasOpeningCollapseFeedback => HasRecentCollapseFeedback();
        public string OpeningCollapseLabel => HasRecentCollapseFeedback() ? _recentCollapseLabel : string.Empty;
        public bool HasOpeningCadenceFeedback => HasRecentOpeningCadenceFeedback();
        public string OpeningCadenceLabel => HasRecentOpeningCadenceFeedback() ? _recentOpeningCadenceLabel : string.Empty;
        public bool HasRecentOpeningCadenceHitRoleBurst => HasRecentOpeningCadenceHitRole();
        public string RecentOpeningCadenceHitRoleCompactTag => HasRecentOpeningCadenceHitRole()
            ? ResolveRecentOpeningCadenceHitRoleCompactTag()
            : string.Empty;
        public string RecentOpeningCadenceHitRoleDetail => HasRecentOpeningCadenceHitRole()
            ? ResolveRecentOpeningCadenceHitRoleBurstLabel()
            : string.Empty;
        public Color RecentOpeningCadenceHitRoleAccentColor => HasRecentOpeningCadenceHitRole()
            ? ResolveRecentOpeningCadenceHitRoleAccentColor()
            : _activeAccentColor;
        public bool HasPreferredImpactHoldDrive => _breakthroughDriveActive && _impactHoldActive && _preferredImpactHoldDriveActive;
        public string PreferredImpactHoldDriveCompactTag => HasPreferredImpactHoldDrive
            ? $"{ResolvePreferredSideShortLabel(_preferredImpactHoldDriveSide)}-HOLD"
            : string.Empty;
        public string PreferredImpactHoldDriveDetail => HasPreferredImpactHoldDrive
            ? $"{ResolvePreferredSideLabel(_preferredImpactHoldDriveSide)} HOLD"
            : string.Empty;
        public Color PreferredImpactHoldDriveAccentColor => HasPreferredImpactHoldDrive
            ? Color.Lerp(
                HasRecentOpeningCadenceHitRole() ? ResolveRecentOpeningCadenceHitRoleAccentColor() : _activeAccentColor,
                Color.white,
                0.18f)
            : (HasRecentOpeningCadenceHitRole() ? ResolveRecentOpeningCadenceHitRoleAccentColor() : _activeAccentColor);
        public bool HasRecentPreferredImpactDrive => _breakthroughDriveActive && _impactHoldActive && _recentPreferredImpactDriveActive;
        public string RecentPreferredImpactDriveCompactTag => HasRecentPreferredImpactDrive
            ? $"{ResolvePreferredSideShortLabel(_recentPreferredImpactDriveSide)}-DRIVE"
            : string.Empty;
        public string RecentPreferredImpactDriveDetail => HasRecentPreferredImpactDrive
            ? $"{ResolvePreferredSideLabel(_recentPreferredImpactDriveSide)} CUT DRIVE"
            : string.Empty;
        public Color RecentPreferredImpactDriveAccentColor => HasRecentPreferredImpactDrive
            ? Color.Lerp(
                HasRecentPreferredImpactHitPulse() ? RecentPreferredImpactHitAccentColor : PreferredImpactHoldDriveAccentColor,
                Color.white,
                Mathf.Lerp(0.1f, 0.24f, Mathf.Clamp01(_recentPreferredImpactDriveStrength)))
            : (HasRecentPreferredImpactHitPulse() ? RecentPreferredImpactHitAccentColor : PreferredImpactHoldDriveAccentColor);
        public float RecentPreferredImpactDriveSide => HasRecentPreferredImpactDrive
            ? _recentPreferredImpactDriveSide
            : 0f;
        public float RecentPreferredImpactDriveStrength => HasRecentPreferredImpactDrive
            ? _recentPreferredImpactDriveStrength
            : 0f;
        public bool HasRecentPreferredImpactHit => HasRecentPreferredImpactHitPulse();
        public string RecentPreferredImpactHitCompactTag => HasRecentPreferredImpactHitPulse()
            ? $"{ResolvePreferredSideShortLabel(_recentPreferredImpactHitSide)}-CUT"
            : string.Empty;
        public string RecentPreferredImpactHitDetail => HasRecentPreferredImpactHitPulse()
            ? $"{ResolvePreferredSideLabel(_recentPreferredImpactHitSide)} CUT"
            : string.Empty;
        public Color RecentPreferredImpactHitAccentColor => HasRecentPreferredImpactHitPulse()
            ? Color.Lerp(PreferredImpactHoldDriveAccentColor, Color.white, Mathf.Lerp(0.08f, 0.2f, Mathf.Clamp01(_recentPreferredImpactHitStrength)))
            : PreferredImpactHoldDriveAccentColor;
        public float RecentPreferredImpactHitSide => HasRecentPreferredImpactHitPulse()
            ? _recentPreferredImpactHitSide
            : 0f;
        public float RecentPreferredImpactHitStrength => HasRecentPreferredImpactHitPulse()
            ? _recentPreferredImpactHitStrength
            : 0f;
        public OpeningCadenceVolleyRole RecentOpeningCadenceHitRole => HasRecentOpeningCadenceHitRole()
            ? _recentOpeningCadenceHitRole
            : OpeningCadenceVolleyRole.None;
        public float RecentOpeningCadenceHitRoleWeight => HasRecentOpeningCadenceHitRole()
            ? _recentOpeningCadenceHitRoleWeight
            : 0f;
        public bool IsImpactSecureHoldActive => HasImpactSecureHold();
        public bool IsImpactSecureHoldChainActive => HasImpactSecureHold() && _impactSecureHoldChainActive;
        public bool TryGetImpactSecurePulseAnchors(List<Vector2> anchors, out float anchorRadius, out float normalized)
        {
            anchorRadius = 0f;
            normalized = 0f;

            if (anchors == null || !HasImpactSecureHold() || !TryGetActiveImpactRelief(out Vector2 impactCenter, out float impactRadius, out normalized))
            {
                anchors?.Clear();
                return false;
            }

            BuildImpactSecurePulseAnchors(
                impactCenter,
                impactRadius,
                anchors,
                out float anchorRadiusScale,
                out _);

            anchorRadius = Mathf.Max(0.12f, impactRadius * anchorRadiusScale);
            return anchors.Count > 0;
        }

        public bool TryGetPreferredImpactSecureAnchor(out Vector2 anchor, out float anchorRadius, out float normalized)
        {
            anchor = Vector2.zero;
            anchorRadius = 0f;
            normalized = 0f;

            if (!TryGetImpactSecurePulseAnchors(_impactAnchorSelectionBuffer, out anchorRadius, out normalized)
                || _impactAnchorSelectionBuffer.Count == 0)
            {
                return false;
            }

            Vector2 referencePoint = TryGetOpeningTargetWorldPosition(out Vector2 targetPosition)
                ? targetPosition
                : (Vector2)transform.position;

            anchor = _impactAnchorSelectionBuffer[0];
            float nearestDistanceSqr = (anchor - referencePoint).sqrMagnitude;
            for (int index = 1; index < _impactAnchorSelectionBuffer.Count; index++)
            {
                Vector2 candidateAnchor = _impactAnchorSelectionBuffer[index];
                float candidateDistanceSqr = (candidateAnchor - referencePoint).sqrMagnitude;
                if (candidateDistanceSqr >= nearestDistanceSqr)
                {
                    continue;
                }

                anchor = candidateAnchor;
                nearestDistanceSqr = candidateDistanceSqr;
            }

            return true;
        }

        public bool TryGetPreferredSecureAnchorSide(out float side, out float influence)
        {
            side = 0f;
            influence = 0f;

            Vector2 origin = transform.position;
            Vector2 forwardDirection = Vector2.right;

            if (!TryResolvePreferredOpeningAimDirection(origin, out forwardDirection, out _)
                && !TryGetOpeningTargetWorldPosition(out Vector2 targetPosition))
            {
                return false;
            }

            if (forwardDirection.sqrMagnitude <= 0.0001f && TryGetOpeningTargetWorldPosition(out Vector2 fallbackTargetPosition))
            {
                Vector2 toTarget = fallbackTargetPosition - origin;
                if (toTarget.sqrMagnitude <= 0.0001f)
                {
                    return false;
                }

                forwardDirection = toTarget.normalized;
            }

            return TryResolvePreferredSecureAnchorSide(origin, forwardDirection, out side, out influence);
        }
        public RoomType ActiveRoomType => _activeRoom != null ? _activeRoom.RoomType : RoomType.Normal;
        public float RemainingOpeningNormalized => HasActiveOpening() && _activeRoom != null
            ? Mathf.Clamp01((_activeExpiresAt - Time.time) / Mathf.Max(0.01f, ResolveOpeningDuration(_activeRoom.RoomType, _activeHeldPlan)))
            : 0f;
        public string ActiveOpeningHeadline => HasActiveOpening()
            ? ResolveOpeningHeadline(_activeReasonTag, _activeHeldPlan, _activeCompareLabel)
            : string.Empty;
        public string ActiveOpeningDetail => HasActiveOpening()
            ? ComposeActiveOpeningDetail()
            : string.Empty;

        public bool TryGetOpeningTargetWorldPosition(out Vector2 worldPosition)
        {
            worldPosition = Vector2.zero;

            if (openingTargetHintController == null || !openingTargetHintController.HasActiveTarget || openingTargetHintController.ActiveTarget == null)
            {
                return false;
            }

            worldPosition = openingTargetHintController.ActiveTarget.transform.position;
            return true;
        }

        public bool TryResolveBreakthroughShotDirection(Vector2 shotOrigin, Vector2 baseDirection, out Vector2 adjustedDirection)
        {
            adjustedDirection = baseDirection.sqrMagnitude > 0.0001f
                ? baseDirection.normalized
                : Vector2.right;

            if (!_breakthroughDriveActive
                || !TryResolvePreferredOpeningAimDirection(shotOrigin, out Vector2 targetDirection, out float secureAnchorInfluence))
            {
                return false;
            }
            float maxAssistAngle = _breakthroughDriveChainActive
                ? breakthroughChainDriveAimAssistDegrees
                : breakthroughDriveAimAssistDegrees;
            if (_impactHoldActive)
            {
                maxAssistAngle += impactHoldAimAssistDegreesBonus;
                maxAssistAngle += impactHoldAimAssistDegreesBonus * Mathf.Clamp01(secureAnchorInfluence) * 0.35f;
            }

            if (maxAssistAngle <= 0.01f)
            {
                return false;
            }

            float signedAngle = Vector2.SignedAngle(adjustedDirection, targetDirection);
            float angleMagnitude = Mathf.Abs(signedAngle);
            if (angleMagnitude > maxAssistAngle)
            {
                return false;
            }

            float assistStrength = _breakthroughDriveChainActive
                ? breakthroughChainDriveAimAssistStrength
                : breakthroughDriveAimAssistStrength;
            if (_impactHoldActive)
            {
                assistStrength = Mathf.Clamp01(assistStrength + impactHoldAimAssistStrengthBonus);
                assistStrength = Mathf.Clamp01(assistStrength + (impactHoldAimAssistStrengthBonus * Mathf.Clamp01(secureAnchorInfluence) * 0.32f));
            }

            float assistScale = Mathf.Clamp01(1f - (angleMagnitude / Mathf.Max(0.01f, maxAssistAngle)));
            float assistAngle = signedAngle * Mathf.Clamp01(assistStrength * Mathf.Lerp(0.45f, 1f, assistScale));
            adjustedDirection = Rotate(adjustedDirection, assistAngle).normalized;
            return true;
        }

        private bool TryResolvePreferredOpeningAimDirection(Vector2 shotOrigin, out Vector2 preferredDirection, out float secureAnchorInfluence)
        {
            preferredDirection = Vector2.right;
            secureAnchorInfluence = 0f;

            if (!TryGetOpeningTargetWorldPosition(out Vector2 targetPosition))
            {
                return false;
            }

            Vector2 toTarget = targetPosition - shotOrigin;
            if (toTarget.sqrMagnitude <= 0.0001f)
            {
                return false;
            }

            preferredDirection = toTarget.normalized;

            if (!TryGetPreferredImpactSecureAnchor(out Vector2 secureAnchor, out float anchorRadius, out _))
            {
                return true;
            }

            Vector2 anchorToTarget = targetPosition - secureAnchor;
            if (anchorToTarget.sqrMagnitude <= 0.0001f)
            {
                return true;
            }

            float distanceToAnchor = Vector2.Distance(shotOrigin, secureAnchor);
            float effectiveAnchorRadius = Mathf.Max(0.12f, anchorRadius * 1.18f);
            secureAnchorInfluence = 1f - Mathf.Clamp01(distanceToAnchor / effectiveAnchorRadius);
            if (_impactHoldActive)
            {
                secureAnchorInfluence = Mathf.Max(secureAnchorInfluence, 0.22f);
            }

            preferredDirection = Vector2.Lerp(
                preferredDirection,
                anchorToTarget.normalized,
                Mathf.Lerp(0.24f, 0.82f, secureAnchorInfluence)).normalized;
            return true;
        }

        public void ApplyOpeningCadenceToSpawnRequest(ref ProjectileSpawnRequest spawnRequest, int shotIndex, int shotCount)
        {
            if (!HasActiveOpening())
            {
                return;
            }

            string shotProfileKeyword = ResolveOpeningShotProfileKeyword(_activeCompareLabel);
            if (string.IsNullOrWhiteSpace(shotProfileKeyword))
            {
                return;
            }

            int burstLimit = Mathf.Max(1, openingCadenceBurstLimit);
            if (_openingCadenceBurstCount >= burstLimit)
            {
                return;
            }

            float cadenceStrength = ResolveOpeningCadenceStrength(_openingCadenceBurstCount, burstLimit);
            if (shotCount > 1)
            {
                bool isCenterShot = shotIndex == shotCount / 2;
                if (shotProfileKeyword == "LASER" && isCenterShot)
                {
                    cadenceStrength *= 1.12f;
                }
                else if (shotProfileKeyword == "SPLIT" && !isCenterShot)
                {
                    cadenceStrength *= 1.08f;
                }
            }

            cadenceStrength = Mathf.Max(0f, cadenceStrength);
            if (cadenceStrength <= 0.001f)
            {
                return;
            }

            float cadenceTraitBonus = Mathf.Max(0f, openingCadenceTraitBonus * cadenceStrength);
            float cadenceScaleBonus = Mathf.Max(0f, openingCadenceScaleBonus * cadenceStrength);
            float cadenceDamageMultiplier = Mathf.Lerp(1f, openingCadenceDamageMultiplier, cadenceStrength);
            float cadenceSpeedMultiplier = Mathf.Lerp(1f, openingCadenceSpeedMultiplier, cadenceStrength);
            ResolveOpeningCadenceSlotWeights(shotIndex, shotCount, out float coreWeight, out float flankWeight, out float edgeWeight);
            spawnRequest.OpeningCadenceRole = ResolveOpeningCadenceVolleyRole(shotProfileKeyword, coreWeight, flankWeight, edgeWeight);
            spawnRequest.OpeningCadenceRoleWeight = spawnRequest.OpeningCadenceRole switch
            {
                OpeningCadenceVolleyRole.Core => coreWeight,
                OpeningCadenceVolleyRole.Flank => flankWeight,
                OpeningCadenceVolleyRole.Edge => edgeWeight,
                _ => 0f
            };

            switch (shotProfileKeyword)
            {
                case "LASER":
                    AddProjectileTrait(ref spawnRequest.Traits, ProjectileTraitFlags.Laser, cadenceTraitBonus * Mathf.Lerp(0.82f, 1.3f, coreWeight));
                    spawnRequest.Damage *= cadenceDamageMultiplier * Mathf.Lerp(0.96f, 1.2f, coreWeight);
                    spawnRequest.Speed *= cadenceSpeedMultiplier * Mathf.Lerp(1.04f, 1.16f, coreWeight);
                    spawnRequest.Scale += cadenceScaleBonus * Mathf.Lerp(0.24f, 0.62f, coreWeight);
                    break;
                case "SPLIT":
                    AddProjectileTrait(ref spawnRequest.Traits, ProjectileTraitFlags.Split, cadenceTraitBonus * Mathf.Lerp(0.88f, 1.28f, flankWeight));
                    spawnRequest.Damage *= cadenceDamageMultiplier * Mathf.Lerp(0.96f, 1.12f, flankWeight);
                    spawnRequest.Speed *= cadenceSpeedMultiplier * Mathf.Lerp(0.9f, 1.02f, flankWeight);
                    spawnRequest.Scale += cadenceScaleBonus * Mathf.Lerp(0.42f, 1f, flankWeight);
                    break;
                case "ORBIT":
                    AddProjectileTrait(ref spawnRequest.Traits, ProjectileTraitFlags.Orbit, cadenceTraitBonus * Mathf.Lerp(0.92f, 1.22f, coreWeight));
                    spawnRequest.Damage *= cadenceDamageMultiplier * Mathf.Lerp(0.94f, 1.08f, coreWeight);
                    spawnRequest.Scale += cadenceScaleBonus * Mathf.Lerp(0.76f, 1.28f, coreWeight);
                    spawnRequest.PierceCount += Mathf.CeilToInt(openingCadencePierceBonus * cadenceStrength * Mathf.Lerp(0.34f, 0.62f, coreWeight));
                    break;
                case "SHIELD":
                    AddProjectileTrait(ref spawnRequest.Traits, ProjectileTraitFlags.Shield, cadenceTraitBonus * Mathf.Lerp(0.9f, 1.24f, coreWeight));
                    spawnRequest.Damage *= cadenceDamageMultiplier * Mathf.Lerp(0.96f, 1.1f, coreWeight);
                    spawnRequest.Scale += cadenceScaleBonus * Mathf.Lerp(0.7f, 1.1f, coreWeight);
                    spawnRequest.PierceCount += Mathf.CeilToInt(openingCadencePierceBonus * cadenceStrength * Mathf.Lerp(0.72f, 1.18f, coreWeight));
                    break;
                case "BOUNCE":
                    AddProjectileTrait(ref spawnRequest.Traits, ProjectileTraitFlags.Bounce, cadenceTraitBonus * Mathf.Lerp(0.9f, 1.32f, edgeWeight));
                    spawnRequest.Damage *= cadenceDamageMultiplier * Mathf.Lerp(0.94f, 1.14f, edgeWeight);
                    spawnRequest.Speed *= cadenceSpeedMultiplier * Mathf.Lerp(1.02f, 1.18f, edgeWeight);
                    spawnRequest.Scale += cadenceScaleBonus * Mathf.Lerp(0.18f, 0.58f, edgeWeight);
                    break;
                case "BLAST":
                    AddProjectileTrait(ref spawnRequest.Traits, ProjectileTraitFlags.Explosive, cadenceTraitBonus * Mathf.Lerp(0.88f, 1.34f, coreWeight));
                    spawnRequest.Damage *= cadenceDamageMultiplier * Mathf.Lerp(0.98f, 1.24f, coreWeight);
                    spawnRequest.Scale += cadenceScaleBonus * Mathf.Lerp(0.9f, 1.68f, coreWeight);
                    break;
                case "LEECH":
                    AddProjectileTrait(ref spawnRequest.Traits, ProjectileTraitFlags.Lifesteal, cadenceTraitBonus * Mathf.Lerp(0.9f, 1.22f, coreWeight));
                    spawnRequest.Damage *= cadenceDamageMultiplier * Mathf.Lerp(0.95f, 1.12f, coreWeight);
                    spawnRequest.Scale += cadenceScaleBonus * Mathf.Lerp(0.42f, 0.82f, coreWeight);
                    spawnRequest.PierceCount += Mathf.CeilToInt(openingCadencePierceBonus * cadenceStrength * Mathf.Lerp(0.3f, 0.6f, coreWeight));
                    break;
                case "SHOT RESET":
                    spawnRequest.Damage *= Mathf.Lerp(1f, openingCadenceDamageMultiplier * 0.94f, cadenceStrength);
                    spawnRequest.Speed *= Mathf.Lerp(1f, openingCadenceSpeedMultiplier * 0.92f, cadenceStrength);
                    spawnRequest.Scale += cadenceScaleBonus * Mathf.Lerp(0.2f, 0.42f, coreWeight);
                    break;
            }
        }

        public void ApplyRecentOpeningCadenceHitRoleToSpawnRequest(ref ProjectileSpawnRequest spawnRequest, int shotIndex, int shotCount)
        {
            if (!HasActiveOpening()
                || !HasRecentOpeningCadenceHitRole())
            {
                return;
            }

            int burstLimit = Mathf.Max(1, recentCadenceRoleBurstLimit);
            if (_recentOpeningCadenceHitRoleBurstCount >= burstLimit)
            {
                return;
            }

            float burstStrength = ResolveRecentOpeningCadenceHitRoleBurstStrength(_recentOpeningCadenceHitRoleBurstCount, burstLimit);
            if (burstStrength <= 0.001f)
            {
                return;
            }

            ResolveOpeningCadenceSlotWeights(shotIndex, shotCount, out float coreWeight, out float flankWeight, out float edgeWeight);
            float centerIndex = (shotCount - 1) * 0.5f;
            float signedSlot = shotCount > 1
                ? shotIndex - centerIndex
                : 0f;
            float slotRoleWeight = _recentOpeningCadenceHitRole switch
            {
                OpeningCadenceVolleyRole.Core => shotCount <= 1 ? 1f : Mathf.Lerp(0.28f, 1f, coreWeight),
                OpeningCadenceVolleyRole.Flank => shotCount <= 1 ? 0.68f : Mathf.Lerp(0.24f, 1f, flankWeight),
                OpeningCadenceVolleyRole.Edge => shotCount <= 1 ? 0.64f : Mathf.Lerp(0.22f, 1f, edgeWeight),
                _ => 0f
            };

            if ((_recentOpeningCadenceHitRole == OpeningCadenceVolleyRole.Flank || _recentOpeningCadenceHitRole == OpeningCadenceVolleyRole.Edge)
                && shotCount > 1
                && Mathf.Abs(signedSlot) > 0.001f
                && TryResolvePreferredSecureAnchorSide(spawnRequest.Position, spawnRequest.Direction, out float preferredSide, out float preferredSideInfluence))
            {
                MergeRecentPreferredImpactHitProfile(ref preferredSide, ref preferredSideInfluence);
                float sideMatch = Mathf.Sign(signedSlot) == preferredSide ? 1f : -1f;
                float slotBias = _recentOpeningCadenceHitRole == OpeningCadenceVolleyRole.Flank
                    ? Mathf.Lerp(0.18f, 0.42f, preferredSideInfluence)
                    : Mathf.Lerp(0.22f, 0.48f, preferredSideInfluence);
                if (HasRecentPreferredImpactHitPulse())
                {
                    slotBias += recentPreferredImpactHitSlotBiasBonus * Mathf.Clamp01(_recentPreferredImpactHitStrength);
                }

                slotRoleWeight = Mathf.Clamp01(slotRoleWeight * (1f + (slotBias * sideMatch)));
            }

            float roleStrength = Mathf.Clamp01(Mathf.Lerp(0.52f, 1.18f, Mathf.Clamp01(_recentOpeningCadenceHitRoleWeight)) * burstStrength * slotRoleWeight);
            if (HasRecentPreferredImpactHitPulse())
            {
                roleStrength = Mathf.Clamp01(roleStrength * Mathf.Lerp(1f, recentPreferredImpactHitRoleStrengthMultiplier, Mathf.Clamp01(_recentPreferredImpactHitStrength)));
            }

            if (roleStrength <= 0.001f)
            {
                return;
            }

            if (spawnRequest.OpeningCadenceRole == OpeningCadenceVolleyRole.None || spawnRequest.OpeningCadenceRoleWeight <= roleStrength)
            {
                spawnRequest.OpeningCadenceRole = _recentOpeningCadenceHitRole;
                spawnRequest.OpeningCadenceRoleWeight = roleStrength;
            }

            switch (_recentOpeningCadenceHitRole)
            {
                case OpeningCadenceVolleyRole.Core:
                    spawnRequest.Damage *= Mathf.Lerp(1f, recentCadenceRoleDamageMultiplier, roleStrength);
                    spawnRequest.Scale += recentCadenceRoleScaleBonus * Mathf.Lerp(0.44f, 1.18f, roleStrength);
                    spawnRequest.PierceCount += Mathf.CeilToInt(recentCadenceRolePierceBonus * roleStrength);
                    spawnRequest.HomingStrength += recentCadenceRoleHomingBonus * Mathf.Lerp(0.58f, 1.12f, roleStrength);
                    break;
                case OpeningCadenceVolleyRole.Flank:
                    AddProjectileTrait(ref spawnRequest.Traits, ProjectileTraitFlags.Split, recentCadenceRoleTraitBonus * Mathf.Lerp(0.8f, 1.32f, roleStrength));
                    spawnRequest.Damage *= Mathf.Lerp(1f, recentCadenceRoleDamageMultiplier * 0.96f, roleStrength);
                    spawnRequest.Speed *= Mathf.Lerp(1f, recentCadenceRoleSpeedMultiplier, roleStrength);
                    spawnRequest.Scale += recentCadenceRoleScaleBonus * Mathf.Lerp(0.6f, 1.24f, roleStrength);
                    break;
                case OpeningCadenceVolleyRole.Edge:
                    AddProjectileTrait(ref spawnRequest.Traits, ProjectileTraitFlags.Bounce, recentCadenceRoleTraitBonus * Mathf.Lerp(0.84f, 1.36f, roleStrength));
                    spawnRequest.Damage *= Mathf.Lerp(1f, recentCadenceRoleDamageMultiplier * 0.94f, roleStrength);
                    spawnRequest.Speed *= Mathf.Lerp(1f, recentCadenceRoleSpeedMultiplier * 1.04f, roleStrength);
                    spawnRequest.Scale += recentCadenceRoleScaleBonus * Mathf.Lerp(0.34f, 0.92f, roleStrength);
                    spawnRequest.Knockback *= Mathf.Lerp(1f, 1.12f, roleStrength);
                    break;
            }
        }

        public float ResolveOpeningCadenceFireInterval(float baseFireInterval)
        {
            if (!HasActiveOpening())
            {
                return baseFireInterval;
            }

            string shotProfileKeyword = ResolveOpeningShotProfileKeyword(_activeCompareLabel);
            if (string.IsNullOrWhiteSpace(shotProfileKeyword))
            {
                return baseFireInterval;
            }

            int cadenceOrdinal = Mathf.Clamp(_openingCadenceBurstCount, 1, Mathf.Max(1, openingCadenceBurstLimit));
            float intervalMultiplier = ResolveOpeningCadenceIntervalMultiplier(shotProfileKeyword, cadenceOrdinal);
            return Mathf.Max(0.01f, baseFireInterval * intervalMultiplier);
        }

        public float ResolveRecentOpeningCadenceHitRoleFireInterval(float baseFireInterval)
        {
            if (!HasActiveOpening() || !HasRecentOpeningCadenceHitRole())
            {
                return baseFireInterval;
            }

            int burstLimit = Mathf.Max(1, recentCadenceRoleBurstLimit);
            if (_recentOpeningCadenceHitRoleBurstCount >= burstLimit)
            {
                return baseFireInterval;
            }

            float intervalMultiplier = _recentOpeningCadenceHitRole switch
            {
                OpeningCadenceVolleyRole.Core => recentCadenceRoleCoreIntervalMultiplier,
                OpeningCadenceVolleyRole.Flank => recentCadenceRoleFlankIntervalMultiplier,
                OpeningCadenceVolleyRole.Edge => recentCadenceRoleEdgeIntervalMultiplier,
                _ => 1f
            };

            float weight = Mathf.Clamp01(_recentOpeningCadenceHitRoleWeight);
            float burstRelax = Mathf.Clamp01(_recentOpeningCadenceHitRoleBurstCount / (float)burstLimit);
            float relaxedMultiplier = Mathf.Lerp(intervalMultiplier, Mathf.Min(1f, intervalMultiplier + recentCadenceRoleIntervalRelaxPerBurst), burstRelax);
            return Mathf.Max(0.01f, baseFireInterval * Mathf.Lerp(1f, relaxedMultiplier, weight));
        }

        public int ResolveOpeningCadenceShotCount(int baseShotCount)
        {
            int resolvedBaseCount = Mathf.Max(1, baseShotCount);
            if (!HasActiveOpening())
            {
                return resolvedBaseCount;
            }

            string shotProfileKeyword = ResolveOpeningShotProfileKeyword(_activeCompareLabel);
            if (string.IsNullOrWhiteSpace(shotProfileKeyword))
            {
                return resolvedBaseCount;
            }

            int burstLimit = Mathf.Max(1, openingCadenceBurstLimit);
            if (_openingCadenceBurstCount >= burstLimit)
            {
                return resolvedBaseCount;
            }

            int cadenceOrdinal = Mathf.Clamp(_openingCadenceBurstCount + 1, 1, burstLimit);
            int bonusShots = ResolveOpeningCadenceVolleyBonusShots(shotProfileKeyword, cadenceOrdinal, burstLimit);
            int resolvedShotCount = Mathf.Max(1, resolvedBaseCount + Mathf.Max(0, bonusShots));
            if (PrefersCenteredCadenceVolley(shotProfileKeyword) && resolvedShotCount % 2 == 0)
            {
                resolvedShotCount += 1;
            }

            return resolvedShotCount;
        }

        public int ResolveRecentOpeningCadenceHitRoleShotCount(int baseShotCount)
        {
            int resolvedBaseCount = Mathf.Max(1, baseShotCount);
            if (!HasActiveOpening() || !HasRecentOpeningCadenceHitRole())
            {
                return resolvedBaseCount;
            }

            int burstLimit = Mathf.Max(1, recentCadenceRoleBurstLimit);
            if (_recentOpeningCadenceHitRoleBurstCount >= burstLimit)
            {
                return resolvedBaseCount;
            }

            float weight = Mathf.Clamp01(_recentOpeningCadenceHitRoleWeight);
            int bonusShots = _recentOpeningCadenceHitRole switch
            {
                OpeningCadenceVolleyRole.Core => recentCadenceRoleCoreBonusShots,
                OpeningCadenceVolleyRole.Flank => recentCadenceRoleFlankBonusShots,
                OpeningCadenceVolleyRole.Edge => recentCadenceRoleEdgeBonusShots,
                _ => 0
            };

            int resolvedShotCount = resolvedBaseCount + Mathf.Max(0, Mathf.RoundToInt(bonusShots * Mathf.Lerp(0.68f, 1f, weight)));
            if (_recentOpeningCadenceHitRole == OpeningCadenceVolleyRole.Core && resolvedShotCount % 2 == 0)
            {
                resolvedShotCount += 1;
            }

            return Mathf.Max(1, resolvedShotCount);
        }

        public Vector2 ResolveOpeningCadenceShotDirection(Vector2 baseDirection, int shotIndex, int shotCount)
        {
            Vector2 normalizedDirection = baseDirection.sqrMagnitude > 0.0001f
                ? baseDirection.normalized
                : Vector2.right;

            if (!HasActiveOpening())
            {
                return normalizedDirection;
            }

            string shotProfileKeyword = ResolveOpeningShotProfileKeyword(_activeCompareLabel);
            if (string.IsNullOrWhiteSpace(shotProfileKeyword) || shotCount <= 1)
            {
                return normalizedDirection;
            }

            float centerIndex = (shotCount - 1) * 0.5f;
            float signedSlot = shotIndex - centerIndex;
            if (Mathf.Abs(signedSlot) <= 0.001f)
            {
                return normalizedDirection;
            }

            int cadenceOrdinal = Mathf.Clamp(_openingCadenceBurstCount + 1, 1, Mathf.Max(1, openingCadenceBurstLimit));
            float cadenceStrength = ResolveOpeningCadenceStrength(cadenceOrdinal - 1, Mathf.Max(1, openingCadenceBurstLimit));
            float burstRelax = Mathf.Lerp(1f, 0.72f, Mathf.Clamp01((cadenceOrdinal - 1) / (float)Mathf.Max(1, openingCadenceBurstLimit)));

            float angleOffset = 0f;
            switch (shotProfileKeyword)
            {
                case "LASER":
                    angleOffset = signedSlot * openingCadenceBaseSpreadDegrees * -(1f - openingCadenceLaserSpreadCompression) * cadenceStrength;
                    break;
                case "SPLIT":
                    angleOffset = signedSlot * openingCadenceBaseSpreadDegrees * (openingCadenceSplitSpreadExpansion - 1f) * cadenceStrength;
                    break;
                case "ORBIT":
                    angleOffset = signedSlot * openingCadenceBaseSpreadDegrees * -(1f - openingCadenceOrbitSpreadCompression) * cadenceStrength;
                    angleOffset += Mathf.Sign(signedSlot) * openingCadenceOrbitVolleyTwistDegrees * cadenceStrength * burstRelax;
                    break;
                case "SHIELD":
                    angleOffset = signedSlot * openingCadenceBaseSpreadDegrees * -(1f - openingCadenceShieldSpreadCompression) * cadenceStrength;
                    break;
                case "BOUNCE":
                    angleOffset = signedSlot * openingCadenceBaseSpreadDegrees * (openingCadenceBounceSpreadExpansion - 1f) * cadenceStrength;
                    angleOffset += Mathf.Sign(signedSlot) * openingCadenceBounceVolleyTwistDegrees * cadenceStrength * burstRelax;
                    break;
                case "BLAST":
                    angleOffset = signedSlot * openingCadenceBaseSpreadDegrees * (openingCadenceBlastSpreadExpansion - 1f) * cadenceStrength;
                    break;
                case "LEECH":
                    angleOffset = signedSlot * openingCadenceBaseSpreadDegrees * -(1f - openingCadenceLeechSpreadCompression) * cadenceStrength;
                    break;
                case "SHOT RESET":
                    angleOffset = signedSlot * openingCadenceBaseSpreadDegrees * -0.18f * cadenceStrength;
                    break;
            }

            return Mathf.Abs(angleOffset) > 0.001f
                ? Rotate(normalizedDirection, angleOffset).normalized
                : normalizedDirection;
        }

        public Vector2 ResolveRecentOpeningCadenceHitRoleShotDirection(Vector2 shotOrigin, Vector2 baseDirection, int shotIndex, int shotCount)
        {
            Vector2 normalizedDirection = baseDirection.sqrMagnitude > 0.0001f
                ? baseDirection.normalized
                : Vector2.right;

            if (!HasActiveOpening()
                || !HasRecentOpeningCadenceHitRole()
                || !TryGetOpeningTargetWorldPosition(out Vector2 targetPosition)
                || !TryResolvePreferredOpeningAimDirection(shotOrigin, out Vector2 preferredAimDirection, out float secureAnchorInfluence))
            {
                return normalizedDirection;
            }
            float weight = Mathf.Clamp01(_recentOpeningCadenceHitRoleWeight);
            float assistStrength = Mathf.Lerp(0.18f, 0.52f, weight);
            if (_impactHoldActive)
            {
                assistStrength *= _impactSecureHoldChainActive ? 1.16f : 1.08f;
                assistStrength *= Mathf.Lerp(1f, 1.14f, Mathf.Clamp01(secureAnchorInfluence));
            }
            if (HasRecentPreferredImpactHitPulse())
            {
                assistStrength *= Mathf.Lerp(1f, recentPreferredImpactHitAssistMultiplier, Mathf.Clamp01(_recentPreferredImpactHitStrength));
            }

            float centerIndex = (shotCount - 1) * 0.5f;
            float signedSlot = shotCount > 1
                ? shotIndex - centerIndex
                : 0f;
            TryResolvePreferredSecureAnchorSide(shotOrigin, preferredAimDirection, out float preferredSide, out float preferredSideInfluence);
            MergeRecentPreferredImpactHitProfile(ref preferredSide, ref preferredSideInfluence);

            return _recentOpeningCadenceHitRole switch
            {
                OpeningCadenceVolleyRole.Core => Vector2.Lerp(normalizedDirection, preferredAimDirection, assistStrength).normalized,
                OpeningCadenceVolleyRole.Flank => ResolveRecentFlankFollowUpDirection(normalizedDirection, preferredAimDirection, signedSlot, assistStrength, preferredSide, preferredSideInfluence),
                OpeningCadenceVolleyRole.Edge => ResolveRecentEdgeFollowUpDirection(normalizedDirection, preferredAimDirection, targetPosition, signedSlot, assistStrength, preferredSide, preferredSideInfluence),
                _ => normalizedDirection
            };
        }

        private void Awake()
        {
            ResolveReferences();
            ClearOpening();
        }

        private void OnEnable()
        {
            ResolveReferences();
            GameplayRuntimeEvents.ChoiceRouteResolved += HandleChoiceRouteResolved;
            GameplayRuntimeEvents.PlayerDamaged += HandlePlayerDamaged;
            GameplayRuntimeEvents.EnemyKilled += HandleEnemyKilled;
            GameplayRuntimeEvents.ProjectileFired += HandleProjectileFired;
            GameplayRuntimeEvents.PlayerProjectileHit += HandlePlayerProjectileHit;
            EnsureNavigationBinding();
        }

        private void OnDisable()
        {
            GameplayRuntimeEvents.ChoiceRouteResolved -= HandleChoiceRouteResolved;
            GameplayRuntimeEvents.PlayerDamaged -= HandlePlayerDamaged;
            GameplayRuntimeEvents.EnemyKilled -= HandleEnemyKilled;
            GameplayRuntimeEvents.ProjectileFired -= HandleProjectileFired;
            GameplayRuntimeEvents.PlayerProjectileHit -= HandlePlayerProjectileHit;
            UnbindObservedRoom();
            UnbindNavigation();

            ClearPendingCarry();
            ClearOpening();
        }

        private void Update()
        {
            ResolveReferences();
            EnsureNavigationBinding();

            if (HasPendingCarry() && Time.time >= _pendingExpiresAt)
            {
                ClearPendingCarry();
            }

            if (HasActiveOpening() && Time.time >= _activeExpiresAt)
            {
                ClearOpening();
            }
            else if (_pendingBreakthroughChain && !HasActiveOpeningBreakthrough())
            {
                _pendingBreakthroughChain = false;
                RaiseOpeningStateChanged();
            }
            else if (HasRecentCollapseFeedback() && Time.time >= _recentCollapseExpiresAt)
            {
                ClearRecentCollapseFeedback();
                RaiseOpeningStateChanged();
            }
            else if (HasRecentBreakthroughChainFeedback() && Time.time >= _recentBreakthroughChainExpiresAt)
            {
                ClearRecentBreakthroughChainFeedback();
                RaiseOpeningStateChanged();
            }
            else if (HasRecentOpeningCadenceFeedback() && Time.time >= _recentOpeningCadenceExpiresAt)
            {
                ClearRecentOpeningCadenceFeedback();
                RaiseOpeningStateChanged();
            }
            else if (_recentOpeningCadenceHitRole != OpeningCadenceVolleyRole.None
                && _recentOpeningCadenceHitRoleExpiresAt > 0f
                && Time.time >= _recentOpeningCadenceHitRoleExpiresAt)
            {
                ClearRecentOpeningCadenceHitRole();
                RaiseOpeningStateChanged();
            }
            else if (HasRecentPreferredImpactHitPulse() && Time.time >= _recentPreferredImpactHitExpiresAt)
            {
                ClearRecentPreferredImpactHitPulse();
                RaiseOpeningStateChanged();
            }

            UpdateImpactSecureHold();
            UpdateBreakthroughPressure();
        }

        private void HandleChoiceRouteResolved(ChoiceRouteResolvedSignal signal)
        {
            if (!signal.IsValid || !signal.HasIntent || !signal.HasCarryClosure)
            {
                return;
            }

            _pendingSourceRoom = signal.Room;
            _pendingReasonTag = NormalizeReasonTag(signal.IntentReasonTag);
            _pendingCompareLabel = IsShotProfileCompareLabel(signal.CompareLabel)
                ? signal.CompareLabel
                : string.Empty;
            _pendingAccentColor = signal.AccentColor.a > 0.01f
                ? signal.AccentColor
                : RoomTraversalGuidanceController.ResolveRoomAccent(signal.Room.RoomType);
            _pendingHeldPlan = signal.CarryHeld;
            _pendingExpiresAt = Time.time + Mathf.Max(1f, pendingCarryDuration);
        }

        private void HandlePlayerDamaged(PlayerDamagedSignal signal)
        {
            if (!breakOpeningOnDamage
                || !HasActiveOpening()
                || signal.PlayerHealth != playerHealth)
            {
                return;
            }

            EnsureReceiptPresentation()?.PlayReceipt(
                signal.Position,
                _activeAccentColor,
                ResolveOpeningBreakHeadline(_activeCompareLabel),
                success: false,
                emphasize: _activeHeldPlan,
                detailLabel: ResolveOpeningBreakDetail(_activeReasonTag, _activeCompareLabel),
                detailColor: Color.Lerp(_activeAccentColor, Color.white, 0.12f));
            ClearOpening();
        }

        private void HandleProjectileFired(ProjectileFiredSignal signal)
        {
            if (!HasActiveOpening()
                || signal.Source != transform)
            {
                return;
            }

            string shotProfileKeyword = ResolveOpeningShotProfileKeyword(_activeCompareLabel);
            if (string.IsNullOrWhiteSpace(shotProfileKeyword))
            {
                return;
            }

            int burstLimit = Mathf.Max(1, openingCadenceBurstLimit);
            if (_openingCadenceBurstCount >= burstLimit)
            {
                return;
            }

            int cadenceOrdinal = _openingCadenceBurstCount + 1;
            _openingCadenceBurstCount = cadenceOrdinal;

            float cadenceSustain = ResolveOpeningCadenceSustain(shotProfileKeyword, cadenceOrdinal, burstLimit);
            if (cadenceSustain > 0.001f)
            {
                SustainOpening(cadenceSustain);
            }

            _recentOpeningCadenceLabel = ResolveOpeningCadenceLabel(shotProfileKeyword, cadenceOrdinal, burstLimit, signal.ProjectileCount);
            _recentOpeningCadenceExpiresAt = Time.time + Mathf.Max(0.1f, openingCadenceFeedbackDuration);
            if (HasRecentOpeningCadenceHitRole())
            {
                _recentOpeningCadenceHitRoleBurstCount = Mathf.Min(
                    Mathf.Max(1, recentCadenceRoleBurstLimit),
                    _recentOpeningCadenceHitRoleBurstCount + 1);
            }
            RaiseOpeningStateChanged();

            if (cadenceOrdinal == 1 || cadenceOrdinal >= burstLimit)
            {
                EnsureReceiptPresentation()?.PlayReceipt(
                    signal.Origin,
                    _activeAccentColor,
                    ResolveOpeningCadenceHeadline(shotProfileKeyword, cadenceOrdinal, burstLimit),
                    success: true,
                    emphasize: cadenceOrdinal == 1 || cadenceOrdinal >= burstLimit,
                    detailLabel: ResolveOpeningCadenceDetail(shotProfileKeyword, cadenceOrdinal, burstLimit, signal.ProjectileCount, cadenceSustain),
                    detailColor: Color.Lerp(_activeAccentColor, Color.white, 0.18f));
            }
        }

        private void HandleEnemyKilled(EnemyKilledSignal signal)
        {
            if (!HasActiveOpening()
                || playerHealth == null
                || playerHealth.IsDead
                || signal.EnemyHealth == null
                || !IsPlayerKill(signal.Killer))
            {
                return;
            }

            RoomController assignedRoom = signal.EnemyHealth.GetComponent<RoomEnemyMember>()?.AssignedRoom;
            if (assignedRoom == null || assignedRoom != _activeRoom)
            {
                return;
            }

            EnemyFormationModifier formationModifier = signal.EnemyHealth.GetComponent<EnemyFormationModifier>();
            EnemyFormationPriorityLevel priorityLevel = formationModifier != null
                ? formationModifier.PriorityLevel
                : EnemyFormationPriorityLevel.None;
            BossEnemyController bossEnemyController = signal.EnemyHealth.GetComponent<BossEnemyController>();
            ChampionEnemyModifier championEnemyModifier = signal.EnemyHealth.GetComponent<ChampionEnemyModifier>();
            bool killedHintedTarget = openingTargetHintController != null
                && openingTargetHintController.ActiveTarget == signal.EnemyHealth;
            bool breakthroughChainReady = _pendingBreakthroughChain && HasActiveOpeningBreakthrough();
            bool triggeredPocketBreakthrough = killedHintedTarget && HasActiveOpeningReliefPocket();
            bool killedEliteTarget = championEnemyModifier != null && championEnemyModifier.IsChampion;
            bool relevantKill = killedHintedTarget
                || breakthroughChainReady
                || priorityLevel != EnemyFormationPriorityLevel.None
                || bossEnemyController != null
                || killedEliteTarget;

            if (!relevantKill)
            {
                return;
            }

            float appliedOpeningDuration = SustainOpening(ResolveExecutionSustain(priorityLevel, killedHintedTarget, bossEnemyController != null, killedEliteTarget));
            string collapseLabel = TryTriggerOpeningCollapse(signal.EnemyHealth, priorityLevel, killedHintedTarget, bossEnemyController != null, killedEliteTarget, triggeredPocketBreakthrough);
            float appliedMomentumDuration = 0f;
            if (momentumController != null && momentumController.IsMomentumActive && appliedOpeningDuration > 0.01f)
            {
                appliedMomentumDuration = momentumController.SustainMomentum(appliedOpeningDuration * Mathf.Max(0f, momentumConversionScale));
            }
            bool triggeredImpactSecure = false;
            string impactSecureDetail = string.Empty;
            if (TryTriggerImpactHoldSecure(
                    signal.EnemyHealth,
                    killedHintedTarget,
                    priorityLevel,
                    bossEnemyController != null,
                    killedEliteTarget,
                    breakthroughChainReady,
                    out float secureOpeningDuration,
                    out float secureMomentumDuration,
                    out int secureProjectileClearCount,
                    out int secureHazardClearCount))
            {
                appliedOpeningDuration += secureOpeningDuration;
                appliedMomentumDuration += secureMomentumDuration;
                impactSecureDetail = ResolveImpactSecureDetail(secureOpeningDuration, secureProjectileClearCount, secureHazardClearCount);
                triggeredImpactSecure = true;
            }

            bool triggeredBreakthroughChain = false;
            if (breakthroughChainReady && !triggeredPocketBreakthrough)
            {
                float chainOpeningDuration = SustainOpening(Mathf.Max(0f, breakthroughChainOpeningBonus));
                appliedOpeningDuration += chainOpeningDuration;

                float chainMomentumDuration = 0f;
                if (momentumController != null && momentumController.IsMomentumActive)
                {
                    chainMomentumDuration = momentumController.SustainMomentum(Mathf.Max(0f, breakthroughChainMomentumBonus));
                }

                appliedMomentumDuration += chainMomentumDuration;
                _pendingBreakthroughChain = false;
                int sweepCount = ApplyBreakthroughSweep();
                int clearedHazardCount = ClearBreakthroughLaneHazards();
                _recentBreakthroughChainLabel = clearedHazardCount > 0
                    ? "BREACH CLEAR"
                    : sweepCount > 0
                        ? "BREACH SWEEP"
                        : "BREACH CHAIN";
                _recentBreakthroughChainExpiresAt = Time.time + Mathf.Max(0.1f, breakthroughChainFeedbackDuration);
                TryActivatePocketBreakthrough(0.72f);
                triggeredBreakthroughChain = true;
            }

            if (appliedOpeningDuration <= 0.01f
                && appliedMomentumDuration <= 0.01f
                && string.IsNullOrWhiteSpace(collapseLabel)
                && !triggeredImpactSecure
                && !triggeredBreakthroughChain)
            {
                return;
            }

            if (triggeredPocketBreakthrough)
            {
                _pendingBreakthroughChain = true;
            }

            _openingExecutionCount++;
            RaiseOpeningStateChanged();

            Color executionAccentColor = ResolveExecutionAccentColor(formationModifier, priorityLevel, bossEnemyController != null, killedEliteTarget);
            EnsureReceiptPresentation()?.PlayReceipt(
                signal.Position,
                executionAccentColor,
                ResolveExecutionHeadline(killedHintedTarget, priorityLevel, bossEnemyController != null, triggeredPocketBreakthrough, triggeredBreakthroughChain, triggeredImpactSecure),
                success: true,
                emphasize: killedHintedTarget || priorityLevel == EnemyFormationPriorityLevel.Critical || bossEnemyController != null || triggeredImpactSecure,
                  detailLabel: ResolveExecutionDetail(appliedOpeningDuration, appliedMomentumDuration, killedHintedTarget, collapseLabel, triggeredPocketBreakthrough, triggeredBreakthroughChain, triggeredImpactSecure, impactSecureDetail),
                  detailColor: Color.Lerp(executionAccentColor, Color.white, 0.16f));
        }

        private void HandlePlayerProjectileHit(PlayerProjectileHitSignal signal)
        {
            if (!HasActiveOpening()
                || !_breakthroughDriveActive
                || signal.Source != transform
                || signal.EnemyHealth == null
                || _activeRoom == null
                || Time.time < _nextBreakthroughHitPulseAt
                || openingTargetHintController == null
                || openingTargetHintController.ActiveTarget != signal.EnemyHealth)
            {
                return;
            }

            RoomController assignedRoom = signal.EnemyHealth.GetComponent<RoomEnemyMember>()?.AssignedRoom;
            if (assignedRoom == null || assignedRoom != _activeRoom)
            {
                return;
            }

            bool chainReady = _breakthroughDriveChainActive || IsBreakthroughChainReady;
            _nextBreakthroughHitPulseAt = Time.time + Mathf.Max(0.02f, breakthroughHitPulseCooldown);
            string shotProfileKeyword = ResolveOpeningShotProfileKeyword(_activeCompareLabel);
            OpeningCadenceVolleyRole cadenceRole = signal.OpeningCadenceRole;
            float cadenceRoleWeight = Mathf.Clamp01(signal.OpeningCadenceRoleWeight);
            if (cadenceRole != OpeningCadenceVolleyRole.None && cadenceRoleWeight > 0.01f)
            {
                _recentOpeningCadenceHitRole = cadenceRole;
                _recentOpeningCadenceHitRoleWeight = cadenceRoleWeight;
                _recentOpeningCadenceHitRoleExpiresAt = Time.time + Mathf.Max(0.1f, breakthroughHitFeedbackDuration);
                _recentOpeningCadenceHitRoleBurstCount = 0;
            }
            else
            {
                ClearRecentOpeningCadenceHitRole();
            }

            bool preferredHoldHitActive = TryResolvePreferredImpactHitPulse(
                signal,
                out float preferredHoldHitStrength,
                out float preferredHoldHitSide,
                out Vector2 preferredHitPoint);
            float appliedOpeningDuration = SustainOpening(
                (chainReady ? breakthroughChainHitSustain : breakthroughHitSustain)
                + ResolveShotProfileHitSustainBonus(shotProfileKeyword, chainReady)
                + ResolveOpeningCadenceRoleHitSustainBonus(cadenceRole, cadenceRoleWeight, chainReady)
                + (preferredHoldHitActive
                    ? Mathf.Lerp(0f, preferredImpactHitSustainBonus, preferredHoldHitStrength)
                    : 0f));
            float projectileClearRadius = (chainReady ? breakthroughChainProjectileClearRadius : breakthroughHitProjectileClearRadius)
                * ResolveShotProfileProjectileClearScale(shotProfileKeyword, chainReady)
                * ResolveOpeningCadenceRoleProjectileClearScale(cadenceRole, cadenceRoleWeight);
            float hazardClearRadius = (chainReady ? breakthroughChainHazardClearRadius : breakthroughHitHazardClearRadius)
                * ResolveShotProfileHazardClearScale(shotProfileKeyword, chainReady)
                * ResolveOpeningCadenceRoleHazardClearScale(cadenceRole, cadenceRoleWeight);
            float impactReliefDuration = (chainReady ? breakthroughChainImpactReliefDuration : breakthroughHitImpactReliefDuration)
                * ResolveOpeningCadenceRoleImpactReliefScale(cadenceRole, cadenceRoleWeight);
            if (preferredHoldHitActive)
            {
                projectileClearRadius *= Mathf.Lerp(1f, preferredImpactHitProjectileClearScale, preferredHoldHitStrength);
                hazardClearRadius *= Mathf.Lerp(1f, preferredImpactHitHazardClearScale, preferredHoldHitStrength);
                impactReliefDuration *= Mathf.Lerp(1f, preferredImpactHitReliefScale, preferredHoldHitStrength);
            }

            Vector2 impactPulsePoint = preferredHoldHitActive
                ? preferredHitPoint
                : (Vector2)signal.ImpactPosition;
            int clearedProjectileCount = ClearEnemyProjectilesNearPoint(
                impactPulsePoint,
                projectileClearRadius);
            int clearedHazardCount = ClearHazardsNearImpact(
                impactPulsePoint,
                hazardClearRadius);
            ActivateImpactRelief(
                impactPulsePoint,
                hazardClearRadius,
                impactReliefDuration);

            string hitLabel = ResolveShotProfileHitLabel(shotProfileKeyword, clearedProjectileCount, clearedHazardCount, cadenceRole);
            if (preferredHoldHitActive)
            {
                hitLabel = DecoratePreferredImpactHitLabel(hitLabel, preferredHoldHitSide);
            }

            _recentBreakthroughChainLabel = hitLabel;
            _recentBreakthroughChainExpiresAt = Time.time + Mathf.Max(0.1f, breakthroughHitFeedbackDuration);
            RaiseOpeningStateChanged();

            Color hitAccentColor = preferredHoldHitActive
                ? PreferredImpactHoldDriveAccentColor
                : _activeAccentColor;
            string hitHeadline = ResolveShotProfileHitHeadline(shotProfileKeyword, chainReady, cadenceRole);
            string hitDetail = ResolveBreakthroughHitDetail(appliedOpeningDuration, clearedProjectileCount, clearedHazardCount, shotProfileKeyword, cadenceRole);
            if (preferredHoldHitActive)
            {
                hitHeadline = DecoratePreferredImpactHitHeadline(hitHeadline, preferredHoldHitSide);
                hitDetail = DecoratePreferredImpactHitDetail(hitDetail, preferredHoldHitSide);
                _recentPreferredImpactHitSide = preferredHoldHitSide;
                _recentPreferredImpactHitStrength = preferredHoldHitStrength;
                _recentPreferredImpactHitExpiresAt = Time.time + Mathf.Max(0.1f, breakthroughHitFeedbackDuration);
            }
            else if (HasRecentPreferredImpactHitPulse())
            {
                ClearRecentPreferredImpactHitPulse();
            }

            EnsureReceiptPresentation()?.PlayReceipt(
                impactPulsePoint,
                hitAccentColor,
                hitHeadline,
                success: true,
                emphasize: chainReady || clearedHazardCount > 0,
                detailLabel: hitDetail,
                detailColor: Color.Lerp(hitAccentColor, Color.white, 0.18f));
        }

        private void HandleCurrentRoomChanged(RoomController room)
        {
            if (_activeRoom != null && _activeRoom != room)
            {
                ClearOpening();
            }

            BindObservedRoom(room);

            if (room != null && room.State == RoomState.Combat)
            {
                TryActivateOpening(room);
            }
        }

        private void HandleObservedRoomStateChanged(RoomController room, RoomState state)
        {
            if (room == null || room != _observedRoom)
            {
                return;
            }

            if (state == RoomState.Combat)
            {
                TryActivateOpening(room);
                return;
            }

            if (_activeRoom == room && state != RoomState.Combat)
            {
                ClearOpening();
            }
        }

        private void EnsureNavigationBinding()
        {
            if (roomNavigationController == null)
            {
                roomNavigationController = FindFirstObjectByType<RoomNavigationController>(FindObjectsInactive.Exclude);
            }

            if (roomNavigationController == null || roomNavigationController == _boundNavigationController)
            {
                return;
            }

            UnbindNavigation();
            _boundNavigationController = roomNavigationController;
            _boundNavigationController.CurrentRoomChanged += HandleCurrentRoomChanged;

            if (_observedRoom != _boundNavigationController.CurrentRoom)
            {
                BindObservedRoom(_boundNavigationController.CurrentRoom);
            }
        }

        private void BindObservedRoom(RoomController room)
        {
            if (_observedRoom == room)
            {
                return;
            }

            UnbindObservedRoom();
            _observedRoom = room;

            if (_observedRoom != null)
            {
                _observedRoom.StateChanged += HandleObservedRoomStateChanged;
            }
        }

        private void UnbindObservedRoom()
        {
            if (_observedRoom != null)
            {
                _observedRoom.StateChanged -= HandleObservedRoomStateChanged;
                _observedRoom = null;
            }
        }

        private void UnbindNavigation()
        {
            if (_boundNavigationController != null)
            {
                _boundNavigationController.CurrentRoomChanged -= HandleCurrentRoomChanged;
                _boundNavigationController = null;
            }
        }

        private bool TryActivateOpening(RoomController room)
        {
            if (!HasPendingCarry()
                || room == null
                || room.State != RoomState.Combat
                || room == _pendingSourceRoom
                || playerStats == null)
            {
                return false;
            }

            BuildOpeningModifiers(_pendingReasonTag, _pendingHeldPlan, _pendingCompareLabel);
            if (_routePlanStatModifiers.Count == 0 && _routePlanProjectileModifiers.Count == 0)
            {
                ClearPendingCarry();
                return false;
            }

            float duration = ResolveOpeningDuration(room.RoomType, _pendingHeldPlan);
            _activeRoom = room;
            _activeReasonTag = _pendingReasonTag;
            _activeCompareLabel = _pendingCompareLabel;
            _activeAccentColor = _pendingAccentColor.a > 0.01f
                ? _pendingAccentColor
                : RoomTraversalGuidanceController.ResolveRoomAccent(room.RoomType);
            _activeHeldPlan = _pendingHeldPlan;
            _openingExecutionCount = 0;
            ClearRecentCollapseFeedback();
            ClearRecentOpeningCadenceFeedback();
            _openingCadenceBurstCount = 0;
            _activeExpiresAt = Time.time + duration;
            playerStats.SetRoutePlanRuntimeModifiers(_routePlanStatModifiers, _routePlanProjectileModifiers);
            RaiseOpeningStateChanged();

            EnsureReceiptPresentation()?.PlayReceipt(
                room.CameraFocusPosition,
                _activeAccentColor,
                ResolveOpeningHeadline(_activeReasonTag, _activeHeldPlan, _activeCompareLabel),
                success: true,
                emphasize: _activeHeldPlan || room.RoomType == RoomType.Boss || room.RoomType == RoomType.MiniBoss,
                detailLabel: ResolveOpeningDetail(_activeReasonTag, room.RoomType, 0, _activeCompareLabel),
                detailColor: Color.Lerp(_activeAccentColor, Color.white, 0.18f));

            ClearPendingCarry();
            return true;
        }

        private void BuildOpeningModifiers(string reasonTag, bool heldPlan, string compareLabel)
        {
            _routePlanStatModifiers.Clear();
            _routePlanProjectileModifiers.Clear();

            float strengthScale = heldPlan ? 1f : Mathf.Clamp01(pivotPlanStrengthScale);
            switch (NormalizeReasonTag(reasonTag))
            {
                case "PRESS ADVANTAGE":
                    AddMultiplier(PlayerStatType.Damage, ResolveScaledMultiplier(advantageDamageMultiplier, strengthScale));
                    AddMultiplier(PlayerStatType.FireInterval, ResolveScaledMultiplier(advantageFireRateMultiplier, strengthScale));
                    AddMultiplier(PlayerStatType.Knockback, ResolveScaledMultiplier(advantageKnockbackMultiplier, strengthScale));
                    break;
                case "RECOVERY ONLINE":
                    AddMultiplier(PlayerStatType.MoveSpeed, ResolveScaledMultiplier(recoveryMoveSpeedMultiplier, strengthScale));
                    AddMultiplier(PlayerStatType.FireInterval, ResolveScaledMultiplier(recoveryFireRateMultiplier, strengthScale));
                    AddMultiplier(PlayerStatType.Knockback, ResolveScaledMultiplier(recoveryKnockbackMultiplier, strengthScale));
                    break;
                case "SUPPLY WINDOW":
                    AddMultiplier(PlayerStatType.MoveSpeed, ResolveScaledMultiplier(supplyMoveSpeedMultiplier, strengthScale));
                    AddMultiplier(PlayerStatType.ProjectileSpeed, ResolveScaledMultiplier(supplyProjectileSpeedMultiplier, strengthScale));
                    AddMultiplier(PlayerStatType.Range, ResolveScaledMultiplier(supplyRangeMultiplier, strengthScale));
                    break;
                case "LOADOUT FIND":
                    AddMultiplier(PlayerStatType.Damage, ResolveScaledMultiplier(buildDamageMultiplier, strengthScale));
                    AddMultiplier(PlayerStatType.ProjectileSpeed, ResolveScaledMultiplier(buildProjectileSpeedMultiplier, strengthScale));
                    AddMultiplier(PlayerStatType.Range, ResolveScaledMultiplier(buildRangeMultiplier, strengthScale));
                    break;
                case "SAFE UPGRADE":
                    AddMultiplier(PlayerStatType.MoveSpeed, ResolveScaledMultiplier(cleanMoveSpeedMultiplier, strengthScale));
                    AddMultiplier(PlayerStatType.FireInterval, ResolveScaledMultiplier(cleanFireRateMultiplier, strengthScale));
                    break;
                default:
                    AddMultiplier(PlayerStatType.MoveSpeed, ResolveScaledMultiplier(cleanMoveSpeedMultiplier, strengthScale * 0.9f));
                    AddMultiplier(PlayerStatType.Damage, ResolveScaledMultiplier(buildDamageMultiplier, strengthScale * 0.8f));
                    break;
            }

            ApplyOpeningShotProfileModifiers(compareLabel, strengthScale);
        }

        private float ResolveOpeningDuration(RoomType roomType, bool heldPlan)
        {
            float duration = baseOpeningDuration;
            if (heldPlan)
            {
                duration += heldPlanDurationBonus;
            }

            switch (roomType)
            {
                case RoomType.MiniBoss:
                case RoomType.Challenge:
                    duration += eliteOpeningDurationBonus;
                    break;
                case RoomType.Boss:
                    duration += bossOpeningDurationBonus;
                    break;
            }

            return Mathf.Max(0.25f, duration);
        }

        private void AddMultiplier(PlayerStatType statType, float multiplier)
        {
            AddStatMultiplier(_routePlanStatModifiers, statType, multiplier);
        }

        private void ApplyOpeningShotProfileModifiers(string compareLabel, float strengthScale)
        {
            if (!IsShotProfileCompareLabel(compareLabel))
            {
                return;
            }

            float scaledTraitBonus = Mathf.Max(0f, openingShotProfileTraitBonus * Mathf.Clamp01(strengthScale));
            float scaledScaleBonus = Mathf.Max(0f, openingShotProfileScaleBonus * Mathf.Clamp01(strengthScale));
            float scaledSpeedBonus = Mathf.Max(0f, openingShotProfileSpeedBonus * Mathf.Clamp01(strengthScale));
            float scaledPierceBonus = Mathf.Max(0f, openingShotProfilePierceBonus * Mathf.Clamp01(strengthScale));
            float scaledMultiShotBonus = Mathf.Max(0f, openingShotProfileMultiShotBonus * Mathf.Clamp01(strengthScale));

            if (ContainsShotProfileToken(compareLabel, "LASER"))
            {
                AddProjectileModifier(_routePlanProjectileModifiers, ProjectileModifierType.Laser, StatModifierOperation.Add, scaledTraitBonus);
                AddProjectileModifier(_routePlanProjectileModifiers, ProjectileModifierType.Speed, StatModifierOperation.Add, scaledSpeedBonus);
            }

            if (ContainsShotProfileToken(compareLabel, "SPLIT"))
            {
                AddProjectileModifier(_routePlanProjectileModifiers, ProjectileModifierType.Split, StatModifierOperation.Add, scaledTraitBonus);
                AddProjectileModifier(_routePlanProjectileModifiers, ProjectileModifierType.MultiShot, StatModifierOperation.Add, scaledMultiShotBonus);
            }

            if (ContainsShotProfileToken(compareLabel, "ORBIT"))
            {
                AddProjectileModifier(_routePlanProjectileModifiers, ProjectileModifierType.Orbit, StatModifierOperation.Add, scaledTraitBonus);
                AddProjectileModifier(_routePlanProjectileModifiers, ProjectileModifierType.Scale, StatModifierOperation.Add, scaledScaleBonus);
            }

            if (ContainsShotProfileToken(compareLabel, "SHIELD"))
            {
                AddProjectileModifier(_routePlanProjectileModifiers, ProjectileModifierType.Shield, StatModifierOperation.Add, scaledTraitBonus);
                AddProjectileModifier(_routePlanProjectileModifiers, ProjectileModifierType.Pierce, StatModifierOperation.Add, scaledPierceBonus);
            }

            if (ContainsShotProfileToken(compareLabel, "BOUNCE"))
            {
                AddProjectileModifier(_routePlanProjectileModifiers, ProjectileModifierType.Bounce, StatModifierOperation.Add, scaledTraitBonus);
                AddProjectileModifier(_routePlanProjectileModifiers, ProjectileModifierType.Speed, StatModifierOperation.Add, scaledSpeedBonus * 0.72f);
            }

            if (ContainsShotProfileToken(compareLabel, "BLAST"))
            {
                AddProjectileModifier(_routePlanProjectileModifiers, ProjectileModifierType.Explode, StatModifierOperation.Add, scaledTraitBonus);
                AddProjectileModifier(_routePlanProjectileModifiers, ProjectileModifierType.Scale, StatModifierOperation.Add, scaledScaleBonus);
            }

            if (ContainsShotProfileToken(compareLabel, "LEECH"))
            {
                AddProjectileModifier(_routePlanProjectileModifiers, ProjectileModifierType.Lifesteal, StatModifierOperation.Add, scaledTraitBonus);
                AddProjectileModifier(_routePlanProjectileModifiers, ProjectileModifierType.Scale, StatModifierOperation.Add, scaledScaleBonus * 0.6f);
            }
        }

        private static void AddStatMultiplier(List<StatModifier> target, PlayerStatType statType, float multiplier)
        {
            if (target == null || multiplier <= 1f)
            {
                return;
            }

            target.Add(new StatModifier(
                statType,
                StatModifierOperation.Multiply,
                multiplier));
        }

        private static float ResolveScaledMultiplier(float multiplier, float strengthScale)
        {
            return 1f + ((Mathf.Max(1f, multiplier) - 1f) * Mathf.Clamp01(strengthScale));
        }

        private static bool ContainsShotProfileToken(string compareLabel, string token)
        {
            return !string.IsNullOrWhiteSpace(compareLabel)
                && !string.IsNullOrWhiteSpace(token)
                && compareLabel.IndexOf(token, System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool IsShotProfileCompareLabel(string compareLabel)
        {
            if (string.IsNullOrWhiteSpace(compareLabel))
            {
                return false;
            }

            return compareLabel == "SHOT RESET"
                || compareLabel == "BASELINE"
                || compareLabel.Contains("LASER")
                || compareLabel.Contains("ORBIT")
                || compareLabel.Contains("SHIELD")
                || compareLabel.Contains("SPLIT")
                || compareLabel.Contains("BLAST")
                || compareLabel.Contains("LEECH")
                || compareLabel.Contains("BOUNCE");
        }

        private static string NormalizeReasonTag(string reasonTag)
        {
            return reasonTag switch
            {
                "POWER SPIKE" => "PRESS ADVANTAGE",
                "PATCH HP" => "RECOVERY ONLINE",
                "CASH WINDOW" => "SUPPLY WINDOW",
                "KEY WINDOW" => "SUPPLY WINDOW",
                "BOMB LINE" => "SUPPLY WINDOW",
                "CASH OUT" => "SUPPLY WINDOW",
                "LOOK FOR KEYS" => "SUPPLY WINDOW",
                "RESTOCK BOMBS" => "SUPPLY WINDOW",
                "FILL SLOT" => "LOADOUT FIND",
                _ => reasonTag ?? string.Empty
            };
        }

        private static string ResolveOpeningHeadline(string reasonTag, bool heldPlan, string compareLabel = "")
        {
            if (IsShotProfileCompareLabel(compareLabel))
            {
                return heldPlan
                    ? $"{compareLabel} OPEN"
                    : $"{compareLabel} PIVOT";
            }

            return NormalizeReasonTag(reasonTag) switch
            {
                "PRESS ADVANTAGE" => heldPlan ? "ADV OPEN" : "ADV PIVOT",
                "RECOVERY ONLINE" => heldPlan ? "SAFE OPEN" : "SAFE PIVOT",
                "SUPPLY WINDOW" => heldPlan ? "SUPPLY OPEN" : "SUPPLY PIVOT",
                "LOADOUT FIND" => heldPlan ? "BUILD OPEN" : "BUILD PIVOT",
                "SAFE UPGRADE" => heldPlan ? "CLEAN OPEN" : "CLEAN PIVOT",
                _ => heldPlan ? "PLAN OPEN" : "PIVOT OPEN"
            };
        }

        private static string ResolveOpeningDetail(string reasonTag, RoomType roomType, int executionCount, string compareLabel = "")
        {
            if (IsShotProfileCompareLabel(compareLabel))
            {
                string shotAnchor = compareLabel == "SHOT RESET"
                    ? "RESET WINDOW"
                    : $"{compareLabel} LIVE";
                string shotDetail = roomType switch
                {
                    RoomType.Boss => $"{shotAnchor} / BOSS ENTRY",
                    RoomType.MiniBoss => $"{shotAnchor} / ELITE ENTRY",
                    RoomType.Challenge => $"{shotAnchor} / TEST ROOM",
                    _ => shotAnchor
                };

                return executionCount > 0
                    ? $"{shotDetail} / CUT x{executionCount}"
                    : shotDetail;
            }

            string anchor = NormalizeReasonTag(reasonTag) switch
            {
                "PRESS ADVANTAGE" => "PRESS FIRST WAVE",
                "RECOVERY ONLINE" => "SAFE ENTRY WINDOW",
                "SUPPLY WINDOW" => "CLEAR FAST LINE",
                "LOADOUT FIND" => "TEST NEW BUILD",
                "SAFE UPGRADE" => "CLEAN ENTRY LINE",
                _ => "OPENING WINDOW"
            };

            string detail = roomType switch
            {
                RoomType.Boss => $"{anchor} / BOSS ENTRY",
                RoomType.MiniBoss => $"{anchor} / ELITE ENTRY",
                RoomType.Challenge => $"{anchor} / TEST ROOM",
                _ => anchor
            };

            return executionCount > 0
                ? $"{detail} / CUT x{executionCount}"
                : detail;
        }

        private static string ResolveOpeningBreakHeadline(string compareLabel)
        {
            return IsShotProfileCompareLabel(compareLabel)
                ? $"{compareLabel} CUT"
                : "OPEN CUT";
        }

        private static string ResolveOpeningBreakDetail(string reasonTag, string compareLabel = "")
        {
            if (IsShotProfileCompareLabel(compareLabel))
            {
                return compareLabel == "SHOT RESET"
                    ? "RESET WINDOW LOST"
                    : $"{compareLabel} LOST";
            }

            return NormalizeReasonTag(reasonTag) switch
            {
                "PRESS ADVANTAGE" => "ADV WINDOW BROKE",
                "RECOVERY ONLINE" => "SAFE ENTRY LOST",
                "SUPPLY WINDOW" => "SUPPLY LINE CUT",
                "LOADOUT FIND" => "BUILD TEST CUT",
                "SAFE UPGRADE" => "CLEAN ENTRY LOST",
                _ => "OPENING LOST"
            };
        }

        private string ComposeActiveOpeningDetail()
        {
            string detail = ResolveOpeningDetail(_activeReasonTag, ActiveRoomType, _openingExecutionCount, _activeCompareLabel);
            if (HasActiveOpeningReliefPocket())
            {
                detail = $"{detail} / SAFE POCKET";
            }

            if (HasActiveOpeningImpactRelief())
            {
                detail = $"{detail} / IMPACT RELIEF";
            }

            if (HasActiveOpeningBreakthrough())
            {
                detail = $"{detail} / LANE PRESSURE";
            }

            if (_breakthroughDriveActive)
            {
                detail = $"{detail} / {(_breakthroughDriveChainActive ? "CHAIN DRIVE" : "LANE DRIVE")}";
            }

            if (_impactHoldActive)
            {
                detail = $"{detail} / {(_breakthroughDriveChainActive ? "CHAIN HOLD" : "IMPACT HOLD")}";
                if (_preferredImpactHoldDriveActive)
                {
                    detail = $"{detail} / {ResolvePreferredSideLabel(_preferredImpactHoldDriveSide)} HOLD";
                    if (_recentPreferredImpactDriveActive)
                    {
                        detail = $"{detail} / {ResolvePreferredSideLabel(_recentPreferredImpactDriveSide)} CUT DRIVE";
                    }
                }
            }

            if (HasImpactSecureHold())
            {
                detail = $"{detail} / {(_impactSecureHoldChainActive ? "CHAIN SECURE" : "SECURE HOLD")}";
                if (_preferredImpactSecureHoldActive && TryGetPreferredSecureAnchorSide(out float preferredSide, out _))
                {
                    detail = $"{detail} / {ResolvePreferredSideLabel(preferredSide)} SECURE";
                }
            }

            if (_pendingBreakthroughChain && HasActiveOpeningBreakthrough())
            {
                detail = $"{detail} / BREACH READY";
            }

            if (HasRecentOpeningCadenceFeedback())
            {
                detail = $"{detail} / {_recentOpeningCadenceLabel}";
            }

            if (HasRecentOpeningCadenceHitRole())
            {
                detail = $"{detail} / {ResolveRecentOpeningCadenceHitRoleBurstLabel()}";
            }

            if (HasRecentBreakthroughChainFeedback())
            {
                detail = $"{detail} / {_recentBreakthroughChainLabel}";
            }

            if (HasRecentCollapseFeedback())
            {
                return $"{detail} / {_recentCollapseLabel}";
            }

            return detail;
        }

        private bool HasActiveOpeningReliefPocket()
        {
            if (_activeRoom == null)
            {
                return false;
            }

            CombatRoomArenaPressureController arenaPressureController = _activeRoom.GetComponentInChildren<CombatRoomArenaPressureController>(true);
            return arenaPressureController != null
                && arenaPressureController.TryGetOpeningReliefPocket(out _, out _, out _, out _);
        }

        private bool HasActiveOpeningBreakthrough()
        {
            if (_activeRoom == null)
            {
                return false;
            }

            CombatRoomArenaPressureController arenaPressureController = _activeRoom.GetComponentInChildren<CombatRoomArenaPressureController>(true);
            return arenaPressureController != null
                && arenaPressureController.IsOpeningBreakthroughActive;
        }

        private bool HasActiveOpeningImpactRelief()
        {
            if (_activeRoom == null)
            {
                return false;
            }

            CombatRoomArenaPressureController arenaPressureController = _activeRoom.GetComponentInChildren<CombatRoomArenaPressureController>(true);
            return arenaPressureController != null
                && (arenaPressureController.TryGetOpeningImpactProtectedPocket(out _, out _, out _, out _)
                    || arenaPressureController.TryGetOpeningImpactRelief(out _, out _, out _, out _));
        }

        private bool HasImpactSecureHold()
        {
            return HasActiveOpening()
                && _impactSecureHoldExpiresAt > 0f
                && Time.time < _impactSecureHoldExpiresAt;
        }

        private void ApplyArenaPressureRelief(bool killedHintedTarget, bool killedBossTarget, bool killedEliteTarget)
        {
            if (_activeRoom == null)
            {
                return;
            }

            CombatRoomArenaPressureController arenaPressureController = _activeRoom.GetComponentInChildren<CombatRoomArenaPressureController>(true);
            if (arenaPressureController == null)
            {
                return;
            }

            float durationScale = killedBossTarget
                ? 1.35f
                : killedHintedTarget
                    ? 1.15f
                    : killedEliteTarget
                        ? 1.08f
                        : 1f;
            float minimumBurstDelay = killedBossTarget
                ? 1.4f
                : killedHintedTarget
                    ? 1.15f
                    : killedEliteTarget
                        ? 1.05f
                        : 0.95f;
            arenaPressureController.ApplyOpeningRelief(durationScale, minimumBurstDelay);
        }

        private bool TryActivatePocketBreakthrough(float durationScale = 1f)
        {
            if (_activeRoom == null)
            {
                return false;
            }

            CombatRoomArenaPressureController arenaPressureController = _activeRoom.GetComponentInChildren<CombatRoomArenaPressureController>(true);
            return arenaPressureController != null
                && arenaPressureController.TryActivateOpeningBreakthrough(durationScale);
        }

        private bool HasPendingCarry()
        {
            return _pendingExpiresAt > 0f
                && Time.time < _pendingExpiresAt
                && !string.IsNullOrWhiteSpace(_pendingReasonTag);
        }

        private bool HasActiveOpening()
        {
            return _activeRoom != null
                && _activeExpiresAt > 0f
                && Time.time < _activeExpiresAt
                && !string.IsNullOrWhiteSpace(_activeReasonTag);
        }

        private void ClearPendingCarry()
        {
            _pendingSourceRoom = null;
            _pendingReasonTag = string.Empty;
            _pendingCompareLabel = string.Empty;
            _pendingAccentColor = Color.white;
            _pendingHeldPlan = false;
            _pendingExpiresAt = float.NegativeInfinity;
        }

        private void ClearOpening()
        {
            bool hadOpening = HasActiveOpening() || _activeRoom != null || !string.IsNullOrWhiteSpace(_activeReasonTag);
            _routePlanStatModifiers.Clear();
            _roomEnemyBuffer.Clear();
            ClearBreakthroughPressure();
            ClearBreakthroughDrive();
            playerStats?.SetRoutePlanRuntimeModifiers(null, null);
            _activeRoom = null;
            _activeReasonTag = string.Empty;
            _activeCompareLabel = string.Empty;
            _activeAccentColor = Color.white;
            _activeHeldPlan = false;
            _pendingBreakthroughChain = false;
            _openingExecutionCount = 0;
            _openingCadenceBurstCount = 0;
            _impactSecureHoldChainActive = false;
            _preferredImpactSecureHoldActive = false;
            ClearRecentCollapseFeedback();
            ClearRecentOpeningCadenceFeedback();
            ClearRecentBreakthroughChainFeedback();
            ClearRecentOpeningCadenceHitRole();
            ClearRecentPreferredImpactHitPulse();
            _activeExpiresAt = float.NegativeInfinity;
            _impactSecureHoldExpiresAt = float.NegativeInfinity;
            _nextBreakthroughPressureUpdateAt = 0f;
            _nextBreakthroughHitPulseAt = 0f;
            _nextImpactSecurePulseAt = 0f;

            if (hadOpening)
            {
                RaiseOpeningStateChanged();
            }
        }

        private void ResolveReferences()
        {
            if (playerStats == null)
            {
                playerStats = GetComponent<PlayerStats>();
            }

            if (playerHealth == null)
            {
                playerHealth = GetComponent<PlayerHealth>();
            }

            if (momentumController == null)
            {
                momentumController = GetComponent<PlayerCombatMomentumController>();
            }

            if (openingTargetHintController == null)
            {
                openingTargetHintController = GetComponent<CombatOpeningTargetHintController>();
            }
        }

        public float SustainOpening(float extraDuration)
        {
            if (!HasActiveOpening() || extraDuration <= 0f)
            {
                return 0f;
            }

            float currentRemaining = Mathf.Max(0f, _activeExpiresAt - Time.time);
            float sustainCap = Mathf.Max(currentRemaining, maximumSustainedOpeningDuration);
            float cappedRemaining = Mathf.Min(currentRemaining + extraDuration, sustainCap);
            float appliedDuration = cappedRemaining - currentRemaining;
            if (appliedDuration <= 0.01f)
            {
                return 0f;
            }

            _activeExpiresAt = Time.time + cappedRemaining;
            RaiseOpeningStateChanged();
            return appliedDuration;
        }

        private string TryTriggerOpeningCollapse(
            EnemyHealth killedEnemy,
            EnemyFormationPriorityLevel priorityLevel,
            bool killedHintedTarget,
            bool killedBossTarget,
            bool killedEliteTarget,
            bool triggeredPocketBreakthrough)
        {
            if (_activeRoom == null
                || killedEnemy == null
                || (!killedHintedTarget
                    && priorityLevel != EnemyFormationPriorityLevel.Critical
                    && !killedBossTarget
                    && !killedEliteTarget))
            {
                return string.Empty;
            }

            float freezeDuration = openingCollapseFreezeDuration;
            if (killedHintedTarget)
            {
                freezeDuration += openingCollapseHintedBonus;
            }

            if (priorityLevel == EnemyFormationPriorityLevel.Critical)
            {
                freezeDuration += openingCollapseCriticalBonus;
            }

            if (killedEliteTarget)
            {
                freezeDuration += openingCollapseEliteBonus;
            }

            if (killedBossTarget)
            {
                freezeDuration += openingCollapseBossBonus;
            }

            _activeRoom.CollectAliveEnemies(_roomEnemyBuffer);
            int affectedCount = 0;

            for (int i = 0; i < _roomEnemyBuffer.Count; i++)
            {
                EnemyHealth enemyHealth = _roomEnemyBuffer[i];
                if (enemyHealth == null || enemyHealth == killedEnemy || enemyHealth.IsDead)
                {
                    continue;
                }

                EnemyController enemyController = enemyHealth.GetComponent<EnemyController>();
                if (enemyController == null)
                {
                    continue;
                }

                enemyController.ApplyFreeze(freezeDuration);
                affectedCount++;
            }

            _roomEnemyBuffer.Clear();

            if (affectedCount <= 0)
            {
                return string.Empty;
            }

            if (triggeredPocketBreakthrough)
            {
                TryActivatePocketBreakthrough();
            }

            _recentCollapseLabel = ResolveCollapseLabel(killedHintedTarget, killedBossTarget, killedEliteTarget, triggeredPocketBreakthrough);
            _recentCollapseExpiresAt = Time.time + Mathf.Max(0.1f, openingCollapseFeedbackDuration);
            ApplyArenaPressureRelief(killedHintedTarget, killedBossTarget, killedEliteTarget);
            return _recentCollapseLabel;
        }

        public void NotifyOpeningPresentationChanged()
        {
            if (HasActiveOpening())
            {
                RaiseOpeningStateChanged();
            }
        }

        private float ResolveExecutionSustain(
            EnemyFormationPriorityLevel priorityLevel,
            bool killedHintedTarget,
            bool killedBossTarget,
            bool killedEliteTarget)
        {
            float sustainDuration = baseExecutionSustain;

            if (killedHintedTarget)
            {
                sustainDuration += hintedTargetSustainBonus;
            }

            sustainDuration += priorityLevel switch
            {
                EnemyFormationPriorityLevel.Focus => focusExecutionSustainBonus,
                EnemyFormationPriorityLevel.Critical => criticalExecutionSustainBonus,
                _ => 0f
            };

            if (killedEliteTarget)
            {
                sustainDuration += eliteExecutionSustainBonus;
            }

            if (killedBossTarget)
            {
                sustainDuration += bossExecutionSustainBonus;
            }

            return sustainDuration;
        }

        private Color ResolveExecutionAccentColor(
            EnemyFormationModifier formationModifier,
            EnemyFormationPriorityLevel priorityLevel,
            bool killedBossTarget,
            bool killedEliteTarget)
        {
            if (formationModifier != null)
            {
                if (priorityLevel != EnemyFormationPriorityLevel.None && formationModifier.PriorityColor.a > 0.01f)
                {
                    return formationModifier.PriorityColor;
                }

                if (formationModifier.AccentColor.a > 0.01f)
                {
                    return formationModifier.AccentColor;
                }
            }

            if (killedBossTarget)
            {
                return Color.Lerp(_activeAccentColor, Color.white, 0.22f);
            }

            if (killedEliteTarget)
            {
                return Color.Lerp(_activeAccentColor, Color.white, 0.14f);
            }

            return _activeAccentColor;
        }

        private static string ResolveExecutionHeadline(bool killedHintedTarget, EnemyFormationPriorityLevel priorityLevel, bool killedBossTarget, bool triggeredPocketBreakthrough, bool triggeredBreakthroughChain, bool triggeredImpactSecure)
        {
            if (killedBossTarget)
            {
                return "BOSS CUT";
            }

            if (triggeredBreakthroughChain)
            {
                return "BREACH CHAIN";
            }

            if (triggeredPocketBreakthrough)
            {
                return "POCKET BREAK";
            }

            if (triggeredImpactSecure)
            {
                return "IMPACT SECURE";
            }

            if (killedHintedTarget)
            {
                return "OPEN CONFIRM";
            }

            return priorityLevel switch
            {
                EnemyFormationPriorityLevel.Critical => "OPEN BREAK",
                EnemyFormationPriorityLevel.Focus => "OPEN PICK",
                _ => "OPEN CUT"
            };
        }

        private static string ResolveExecutionDetail(float openingDuration, float momentumDuration, bool killedHintedTarget, string collapseLabel, bool triggeredPocketBreakthrough, bool triggeredBreakthroughChain, bool triggeredImpactSecure, string impactSecureDetail)
        {
            string openingDetail = openingDuration > 0.01f
                ? $"+{openingDuration:0.0}s OPEN"
                : "OPEN HELD";

            if (!string.IsNullOrWhiteSpace(collapseLabel))
            {
                openingDetail = $"{openingDetail} / {collapseLabel}";
            }

            if (triggeredImpactSecure && !string.IsNullOrWhiteSpace(impactSecureDetail))
            {
                openingDetail = $"{openingDetail} / {impactSecureDetail}";
            }

            if (momentumDuration > 0.01f)
            {
                return $"{openingDetail} / FLOW +{momentumDuration:0.0}s";
            }

            if (triggeredBreakthroughChain)
            {
                return $"{openingDetail} / BREACH HELD";
            }

            if (triggeredPocketBreakthrough)
            {
                return $"{openingDetail} / POCKET HELD";
            }

            return killedHintedTarget
                ? $"{openingDetail} / ROUTE HELD"
                : openingDetail;
        }

        private bool IsPlayerKill(Transform source)
        {
            if (source == null)
            {
                return false;
            }

            return source == transform || source.IsChildOf(transform);
        }

        private void UpdateBreakthroughPressure()
        {
            if (Time.time < _nextBreakthroughPressureUpdateAt)
            {
                return;
            }

            _nextBreakthroughPressureUpdateAt = Time.time + Mathf.Max(0.02f, breakthroughPressureUpdateInterval);

            if (!HasActiveOpening() || _activeRoom == null)
            {
                ClearBreakthroughPressure();
                ClearBreakthroughDrive();
                return;
            }

            CombatRoomArenaPressureController arenaPressureController = _activeRoom.GetComponentInChildren<CombatRoomArenaPressureController>(true);
            if (arenaPressureController == null
                || !arenaPressureController.TryGetOpeningBreakthroughLaneData(
                    out Vector2 laneOrigin,
                    out Vector2 laneDirection,
                    out float laneLength,
                    out float laneHalfWidth,
                    out _,
                    out _))
            {
                ClearBreakthroughPressure();
                ClearBreakthroughDrive();
                return;
            }

            ClearBreakthroughPressure();
            _activeRoom.CollectAliveEnemies(_roomEnemyBuffer);

            bool chainReady = IsBreakthroughChainReady;
            UpdateBreakthroughDrive(laneOrigin, laneDirection, laneLength, laneHalfWidth, chainReady);
            UpdateBreakthroughProjectiles(laneOrigin, laneDirection, laneLength, laneHalfWidth, chainReady);

            for (int i = 0; i < _roomEnemyBuffer.Count; i++)
            {
                EnemyHealth enemyHealth = _roomEnemyBuffer[i];
                if (enemyHealth == null || enemyHealth.IsDead)
                {
                    continue;
                }

                EnemyController enemyController = enemyHealth.GetComponent<EnemyController>();
                EnemyMovement enemyMovement = enemyController != null ? enemyController.EnemyMovement : null;
                if (enemyController == null
                    || !TryResolveBreakthroughLaneInfluence(
                        enemyHealth,
                        laneOrigin,
                        laneDirection,
                        laneLength,
                        laneHalfWidth,
                        out float laneFit,
                        out float progress,
                        out Vector2 lateralDirection))
                {
                    continue;
                }

                float pressureMultiplier = ResolveBreakthroughPressureMultiplier(
                    enemyHealth,
                    laneFit,
                    progress,
                    chainReady);
                float contactMultiplier = ResolveBreakthroughPressureContactMultiplier(
                    enemyHealth,
                    laneFit,
                    progress,
                    chainReady);
                float pushImpulse = ResolveBreakthroughPressurePushImpulse(
                    enemyHealth,
                    laneFit,
                    progress,
                    chainReady);

                if (pressureMultiplier >= 0.999f && contactMultiplier >= 0.999f && pushImpulse <= 0.01f)
                {
                    continue;
                }

                enemyController.SetRuntimePressureSpeedMultiplier(pressureMultiplier);
                enemyController.SetRuntimePressureContactDamageMultiplier(contactMultiplier);
                if (enemyMovement != null && pushImpulse > 0.01f)
                {
                    Vector2 pushDirection = (lateralDirection + (laneDirection * breakthroughPressurePushForwardBias)).normalized;
                    enemyMovement.ApplyImpulse(pushDirection * pushImpulse);
                }
                _breakthroughPressureControllers.Add(enemyController);
            }

            _roomEnemyBuffer.Clear();
        }

        private void UpdateBreakthroughProjectiles(
            Vector2 laneOrigin,
            Vector2 laneDirection,
            float laneLength,
            float laneHalfWidth,
            bool chainReady)
        {
            if (!TryResolveBreakthroughLanePointInfluence(
                    transform.position,
                    laneOrigin,
                    laneDirection,
                    laneLength,
                    laneHalfWidth,
                    out _,
                    out float playerProgress,
                    out _))
            {
                return;
            }

            EnemyProjectileLogic.CollectActiveProjectiles(_breakthroughProjectileBuffer);

            float clearRadiusSqr = breakthroughProjectileClearRadius * breakthroughProjectileClearRadius;
            float chainClearProgress = Mathf.Clamp01(playerProgress + breakthroughProjectileChainClearProgressWindow);
            float effectiveLaneHalfWidth = laneHalfWidth + breakthroughProjectileLanePadding;

            for (int i = 0; i < _breakthroughProjectileBuffer.Count; i++)
            {
                EnemyProjectileLogic projectile = _breakthroughProjectileBuffer[i];
                if (projectile == null)
                {
                    continue;
                }

                if (!TryResolveBreakthroughLanePointInfluence(
                        projectile.WorldPosition,
                        laneOrigin,
                        laneDirection,
                        laneLength,
                        effectiveLaneHalfWidth,
                        out float laneFit,
                        out float progress,
                        out Vector2 lateralDirection))
                {
                    continue;
                }

                bool shouldClear = ((projectile.WorldPosition - (Vector2)transform.position).sqrMagnitude <= clearRadiusSqr)
                    || (chainReady && laneFit >= 0.42f && progress <= chainClearProgress);

                if (shouldClear)
                {
                    projectile.ForceDissipate(ProjectileImpactType.Solid);
                    continue;
                }

                float suppressionStrength = Mathf.Clamp01((laneFit * 0.7f) + (progress * 0.3f));
                float targetSpeedMultiplier = chainReady
                    ? breakthroughChainProjectileSpeedMultiplier
                    : breakthroughProjectileSpeedMultiplier;
                float targetLateralBias = chainReady
                    ? breakthroughChainProjectileLateralBias
                    : breakthroughProjectileLateralBias;

                projectile.ApplyRouteLaneInfluence(
                    Mathf.Lerp(1f, targetSpeedMultiplier, suppressionStrength),
                    lateralDirection,
                    targetLateralBias * Mathf.Clamp01((laneFit * 0.84f) + 0.12f));
            }

            _breakthroughProjectileBuffer.Clear();
        }

        private void UpdateBreakthroughDrive(
            Vector2 laneOrigin,
            Vector2 laneDirection,
            float laneLength,
            float laneHalfWidth,
            bool chainReady)
        {
            bool laneDriveActive = TryResolveBreakthroughLanePointInfluence(
                transform.position,
                laneOrigin,
                laneDirection,
                laneLength,
                laneHalfWidth,
                out _,
                out _,
                out _);
            bool impactHoldActive = TryResolveImpactHoldPointInfluence(transform.position, out float impactHoldStrength);
            float preferredImpactHoldStrength = 0f;
            float preferredSecureSide = 0f;
            bool preferredSecureDriveActive = impactHoldActive
                && TryResolvePreferredImpactSecureHoldInfluence(transform.position, out preferredImpactHoldStrength, out preferredSecureSide);
            float preferredDriveSide = preferredSecureDriveActive
                ? Mathf.Sign(preferredSecureSide)
                : 0f;
            bool recentPreferredDriveActive = preferredSecureDriveActive
                && HasRecentPreferredImpactHitPulse()
                && Mathf.Abs(_recentPreferredImpactHitSide) > 0.5f
                && Mathf.Sign(_recentPreferredImpactHitSide) == preferredDriveSide;
            float recentPreferredDriveStrength = recentPreferredDriveActive
                ? Mathf.Clamp01(_recentPreferredImpactHitStrength)
                : 0f;
            bool shouldDrive = laneDriveActive || impactHoldActive;

            if (!shouldDrive)
            {
                ClearBreakthroughDrive();
                return;
            }

            if (_breakthroughDriveActive
                && _breakthroughDriveChainActive == chainReady
                && _impactHoldActive == impactHoldActive
                && _preferredImpactHoldDriveActive == preferredSecureDriveActive
                && (!preferredSecureDriveActive || Mathf.Approximately(_preferredImpactHoldDriveSide, preferredDriveSide))
                && _recentPreferredImpactDriveActive == recentPreferredDriveActive
                && (!recentPreferredDriveActive
                    || (Mathf.Approximately(_recentPreferredImpactDriveSide, preferredDriveSide)
                        && Mathf.Abs(_recentPreferredImpactDriveStrength - recentPreferredDriveStrength) <= 0.05f)))
            {
                return;
            }

            _breakthroughDriveStatModifiers.Clear();
            _breakthroughDriveProjectileModifiers.Clear();
            float driveScale = 1f + (chainReady ? breakthroughChainDriveBonus : 0f);
            float impactHoldScale = impactHoldActive
                ? 1f + ((chainReady ? impactHoldChainBonus : 0f) * Mathf.Clamp01(impactHoldStrength))
                : 1f;
            float moveSpeedMultiplier = breakthroughDriveMoveSpeedMultiplier * driveScale;
            float fireRateMultiplier = breakthroughDriveFireRateMultiplier * driveScale;
            float projectileSpeedMultiplier = breakthroughDriveProjectileSpeedMultiplier * driveScale;
            float knockbackMultiplier = breakthroughDriveKnockbackMultiplier * driveScale;

            if (impactHoldActive)
            {
                float holdBlend = Mathf.Clamp01(impactHoldStrength);
                moveSpeedMultiplier *= Mathf.Lerp(1f, impactHoldMoveSpeedMultiplier * impactHoldScale, holdBlend);
                fireRateMultiplier *= Mathf.Lerp(1f, impactHoldFireRateMultiplier * impactHoldScale, holdBlend);
                projectileSpeedMultiplier *= Mathf.Lerp(1f, impactHoldProjectileSpeedMultiplier * impactHoldScale, holdBlend);
                knockbackMultiplier *= Mathf.Lerp(1f, impactHoldKnockbackMultiplier * impactHoldScale, holdBlend);

                if (preferredSecureDriveActive)
                {
                    float preferredDriveBlend = Mathf.Clamp01(preferredImpactHoldStrength);
                    float preferredDriveMultiplier = Mathf.Lerp(1f, impactPreferredSecureDriveMultiplier, preferredDriveBlend);
                    moveSpeedMultiplier *= preferredDriveMultiplier;
                    fireRateMultiplier *= preferredDriveMultiplier;
                    projectileSpeedMultiplier *= preferredDriveMultiplier;
                    knockbackMultiplier *= preferredDriveMultiplier;

                    if (recentPreferredDriveActive)
                    {
                        float recentPreferredDriveMultiplier = Mathf.Lerp(1f, impactRecentPreferredHitDriveMultiplier, recentPreferredDriveStrength);
                        moveSpeedMultiplier *= recentPreferredDriveMultiplier;
                        fireRateMultiplier *= recentPreferredDriveMultiplier;
                        projectileSpeedMultiplier *= recentPreferredDriveMultiplier;
                        knockbackMultiplier *= recentPreferredDriveMultiplier;
                    }
                }
            }

            AddStatMultiplier(_breakthroughDriveStatModifiers, PlayerStatType.MoveSpeed, moveSpeedMultiplier);
            AddStatMultiplier(_breakthroughDriveStatModifiers, PlayerStatType.FireInterval, fireRateMultiplier);
            AddStatMultiplier(_breakthroughDriveStatModifiers, PlayerStatType.ProjectileSpeed, projectileSpeedMultiplier);
            AddStatMultiplier(_breakthroughDriveStatModifiers, PlayerStatType.Knockback, knockbackMultiplier);
            AddProjectileModifier(
                _breakthroughDriveProjectileModifiers,
                ProjectileModifierType.Pierce,
                StatModifierOperation.Add,
                (chainReady ? breakthroughChainDrivePierceBonus : breakthroughDrivePierceBonus)
                + (impactHoldActive
                    ? Mathf.Lerp(0f, chainReady ? impactChainHoldPierceBonus : impactHoldPierceBonus, Mathf.Clamp01(impactHoldStrength))
                    : 0f)
                + (preferredSecureDriveActive
                    ? Mathf.Lerp(0f, impactPreferredSecurePierceBonus, Mathf.Clamp01(preferredImpactHoldStrength))
                    : 0f)
                + (recentPreferredDriveActive
                    ? Mathf.Lerp(0f, impactRecentPreferredHitPierceBonus, recentPreferredDriveStrength)
                    : 0f));
            AddProjectileModifier(
                _breakthroughDriveProjectileModifiers,
                ProjectileModifierType.Scale,
                StatModifierOperation.Add,
                (chainReady ? breakthroughChainDriveScaleBonus : breakthroughDriveScaleBonus)
                + (impactHoldActive
                    ? Mathf.Lerp(0f, chainReady ? impactChainHoldScaleBonus : impactHoldScaleBonus, Mathf.Clamp01(impactHoldStrength))
                    : 0f)
                + (preferredSecureDriveActive
                    ? Mathf.Lerp(0f, impactPreferredSecureScaleBonus, Mathf.Clamp01(preferredImpactHoldStrength))
                    : 0f)
                + (recentPreferredDriveActive
                    ? Mathf.Lerp(0f, impactRecentPreferredHitScaleBonus, recentPreferredDriveStrength)
                    : 0f));

            float damageMultiplier = chainReady ? breakthroughChainDriveDamageMultiplier : breakthroughDriveDamageMultiplier;
            float invulnerabilityBonus = chainReady ? breakthroughChainDriveInvulnerabilityBonus : breakthroughDriveInvulnerabilityBonus;
            if (impactHoldActive)
            {
                float holdBlend = Mathf.Clamp01(impactHoldStrength);
                damageMultiplier *= Mathf.Lerp(1f, chainReady ? impactChainHoldDamageMultiplier : impactHoldDamageMultiplier, holdBlend);
                invulnerabilityBonus += Mathf.Lerp(0f, chainReady ? impactChainHoldInvulnerabilityBonus : impactHoldInvulnerabilityBonus, holdBlend);

                if (preferredSecureDriveActive)
                {
                    float preferredDriveBlend = Mathf.Clamp01(preferredImpactHoldStrength);
                    damageMultiplier *= Mathf.Lerp(1f, impactPreferredSecureDamageMultiplier, preferredDriveBlend);
                    invulnerabilityBonus += Mathf.Lerp(0f, impactPreferredSecureInvulnerabilityBonus, preferredDriveBlend);

                    if (recentPreferredDriveActive)
                    {
                        damageMultiplier *= Mathf.Lerp(1f, impactRecentPreferredHitDamageMultiplier, recentPreferredDriveStrength);
                        invulnerabilityBonus += Mathf.Lerp(0f, impactRecentPreferredHitInvulnerabilityBonus, recentPreferredDriveStrength);
                    }
                }
            }

            playerStats?.SetRouteBreakthroughRuntimeModifiers(_breakthroughDriveStatModifiers, _breakthroughDriveProjectileModifiers);
            playerHealth?.SetRouteBreakthroughProtection(damageMultiplier, invulnerabilityBonus);
            _breakthroughDriveActive = true;
            _breakthroughDriveChainActive = chainReady;
            _impactHoldActive = impactHoldActive;
            _preferredImpactHoldDriveActive = preferredSecureDriveActive;
            _preferredImpactHoldDriveSide = preferredDriveSide;
            _recentPreferredImpactDriveActive = recentPreferredDriveActive;
            _recentPreferredImpactDriveSide = preferredDriveSide;
            _recentPreferredImpactDriveStrength = recentPreferredDriveStrength;
            RaiseOpeningStateChanged();
        }

        private float ResolveBreakthroughPressureMultiplier(
            EnemyHealth enemyHealth,
            float laneFit,
            float progress,
            bool chainReady)
        {
            if (enemyHealth == null)
            {
                return 1f;
            }

            float pressureStrength = Mathf.Clamp01((laneFit * 0.68f) + (progress * 0.32f));
            float targetMultiplier = chainReady
                ? breakthroughChainPressureSpeedMultiplier
                : breakthroughPressureSpeedMultiplier;

            if (enemyHealth.GetComponent<BossEnemyController>() != null)
            {
                targetMultiplier = Mathf.Max(targetMultiplier, breakthroughBossPressureSpeedMultiplier);
            }

            return Mathf.Lerp(1f, Mathf.Clamp(targetMultiplier, 0.1f, 1f), pressureStrength);
        }

        private float ResolveBreakthroughPressureContactMultiplier(
            EnemyHealth enemyHealth,
            float laneFit,
            float progress,
            bool chainReady)
        {
            if (enemyHealth == null)
            {
                return 1f;
            }

            float pressureStrength = Mathf.Clamp01((laneFit * 0.74f) + (progress * 0.26f));
            float targetMultiplier = chainReady
                ? breakthroughChainPressureContactMultiplier
                : breakthroughPressureContactMultiplier;

            if (enemyHealth.GetComponent<BossEnemyController>() != null)
            {
                targetMultiplier = Mathf.Max(targetMultiplier, breakthroughBossPressureContactMultiplier);
            }

            return Mathf.Lerp(1f, Mathf.Clamp(targetMultiplier, 0.1f, 1f), pressureStrength);
        }

        private float ResolveBreakthroughPressurePushImpulse(
            EnemyHealth enemyHealth,
            float laneFit,
            float progress,
            bool chainReady)
        {
            if (enemyHealth == null)
            {
                return 0f;
            }

            float pressureStrength = Mathf.Clamp01((laneFit * 0.78f) + (progress * 0.22f));
            float targetImpulse = chainReady
                ? breakthroughChainPressurePushImpulse
                : breakthroughPressurePushImpulse;

            if (enemyHealth.GetComponent<BossEnemyController>() != null)
            {
                targetImpulse = Mathf.Min(targetImpulse, breakthroughBossPressurePushImpulse);
            }

            return Mathf.Max(0f, targetImpulse) * pressureStrength;
        }

        private int ApplyBreakthroughSweep()
        {
            if (_activeRoom == null)
            {
                return 0;
            }

            CombatRoomArenaPressureController arenaPressureController = _activeRoom.GetComponentInChildren<CombatRoomArenaPressureController>(true);
            if (arenaPressureController == null
                || !arenaPressureController.TryGetOpeningBreakthroughLaneData(
                    out Vector2 laneOrigin,
                    out Vector2 laneDirection,
                    out float laneLength,
                    out float laneHalfWidth,
                    out _,
                    out _))
            {
                return 0;
            }

            _activeRoom.CollectAliveEnemies(_roomEnemyBuffer);
            int affectedCount = 0;

            for (int i = 0; i < _roomEnemyBuffer.Count; i++)
            {
                EnemyHealth enemyHealth = _roomEnemyBuffer[i];
                if (enemyHealth == null || enemyHealth.IsDead)
                {
                    continue;
                }

                EnemyController enemyController = enemyHealth.GetComponent<EnemyController>();
                EnemyMovement enemyMovement = enemyController != null ? enemyController.EnemyMovement : null;
                if (enemyController == null || enemyMovement == null)
                {
                    continue;
                }

                if (!TryResolveBreakthroughLaneInfluence(
                    enemyHealth,
                    laneOrigin,
                    laneDirection,
                    laneLength,
                    laneHalfWidth,
                    out float laneFit,
                    out float progress,
                    out Vector2 lateralDirection))
                {
                    continue;
                }

                float sweepStrength = Mathf.Clamp01((laneFit * 0.74f) + (progress * 0.26f));

                float impulseMagnitude = enemyHealth.GetComponent<BossEnemyController>() != null
                    ? breakthroughSweepBossImpulse
                    : breakthroughSweepImpulse;
                Vector2 sweepDirection = (lateralDirection + (laneDirection * breakthroughSweepForwardBias)).normalized;
                enemyMovement.ApplyImpulse(sweepDirection * (impulseMagnitude * sweepStrength));
                affectedCount++;
            }

            _roomEnemyBuffer.Clear();
            return affectedCount;
        }

        private bool TryResolveBreakthroughLaneInfluence(
            EnemyHealth enemyHealth,
            Vector2 laneOrigin,
            Vector2 laneDirection,
            float laneLength,
            float laneHalfWidth,
            out float laneFit,
            out float progress,
            out Vector2 lateralDirection)
        {
            laneFit = 0f;
            progress = 0f;
            lateralDirection = Vector2.zero;

            if (enemyHealth == null)
            {
                return false;
            }

            return TryResolveBreakthroughLanePointInfluence(
                enemyHealth.transform.position,
                laneOrigin,
                laneDirection,
                laneLength,
                laneHalfWidth,
                out laneFit,
                out progress,
                out lateralDirection);
        }

        private bool TryResolveBreakthroughLanePointInfluence(
            Vector2 point,
            Vector2 laneOrigin,
            Vector2 laneDirection,
            float laneLength,
            float laneHalfWidth,
            out float laneFit,
            out float progress,
            out Vector2 lateralDirection)
        {
            laneFit = 0f;
            progress = 0f;
            lateralDirection = Vector2.zero;

            if (laneLength <= 0.01f)
            {
                return false;
            }

            Vector2 pointOffset = point - laneOrigin;
            float along = Vector2.Dot(pointOffset, laneDirection);
            if (along <= 0.04f || along >= laneLength)
            {
                return false;
            }

            Vector2 laneNormal = new(-laneDirection.y, laneDirection.x);
            float lateralSigned = Vector2.Dot(pointOffset, laneNormal);
            float lateral = Mathf.Abs(lateralSigned);
            float effectiveHalfWidth = Mathf.Max(0.28f, laneHalfWidth + breakthroughPressureLanePadding);
            if (lateral > effectiveHalfWidth)
            {
                return false;
            }

            progress = Mathf.Clamp01(along / Mathf.Max(0.1f, laneLength));
            laneFit = 1f - Mathf.Clamp01(lateral / effectiveHalfWidth);

            if (Mathf.Abs(lateralSigned) > 0.02f)
            {
                lateralDirection = Mathf.Sign(lateralSigned) * laneNormal;
            }
            else
            {
                float playerSide = Vector2.Dot((point - (Vector2)transform.position), laneNormal);
                lateralDirection = playerSide >= 0f ? laneNormal : -laneNormal;
            }

            return true;
        }

        private int ClearBreakthroughLaneHazards()
        {
            if (_activeRoom == null)
            {
                return 0;
            }

            CombatRoomArenaPressureController arenaPressureController = _activeRoom.GetComponentInChildren<CombatRoomArenaPressureController>(true);
            return arenaPressureController != null
                ? arenaPressureController.ClearHazardsInOpeningBreakthroughLane(breakthroughSweepHazardPadding)
                : 0;
        }

        private int ClearEnemyProjectilesNearPoint(Vector2 point, float radius)
        {
            _breakthroughProjectileBuffer.Clear();
            EnemyProjectileLogic.CollectActiveProjectiles(_breakthroughProjectileBuffer);

            float effectiveRadius = Mathf.Max(0.01f, radius);
            float effectiveRadiusSqr = effectiveRadius * effectiveRadius;
            int clearedCount = 0;

            for (int index = 0; index < _breakthroughProjectileBuffer.Count; index++)
            {
                EnemyProjectileLogic projectile = _breakthroughProjectileBuffer[index];
                if (projectile == null)
                {
                    continue;
                }

                if ((projectile.WorldPosition - point).sqrMagnitude > effectiveRadiusSqr)
                {
                    continue;
                }

                projectile.ForceDissipate(ProjectileImpactType.Damageable);
                clearedCount++;
            }

            _breakthroughProjectileBuffer.Clear();
            return clearedCount;
        }

        private int ClearEnemyProjectilesNearPoints(List<Vector2> points, float radius)
        {
            if (points == null || points.Count == 0)
            {
                return 0;
            }

            if (points.Count == 1)
            {
                return ClearEnemyProjectilesNearPoint(points[0], radius);
            }

            _breakthroughProjectileBuffer.Clear();
            EnemyProjectileLogic.CollectActiveProjectiles(_breakthroughProjectileBuffer);

            float effectiveRadius = Mathf.Max(0.01f, radius);
            float effectiveRadiusSqr = effectiveRadius * effectiveRadius;
            int clearedCount = 0;

            for (int index = 0; index < _breakthroughProjectileBuffer.Count; index++)
            {
                EnemyProjectileLogic projectile = _breakthroughProjectileBuffer[index];
                if (projectile == null)
                {
                    continue;
                }

                Vector2 projectilePosition = projectile.WorldPosition;
                bool insideAnyAnchor = false;
                for (int anchorIndex = 0; anchorIndex < points.Count; anchorIndex++)
                {
                    if ((projectilePosition - points[anchorIndex]).sqrMagnitude <= effectiveRadiusSqr)
                    {
                        insideAnyAnchor = true;
                        break;
                    }
                }

                if (!insideAnyAnchor)
                {
                    continue;
                }

                projectile.ForceDissipate(ProjectileImpactType.Damageable);
                clearedCount++;
            }

            _breakthroughProjectileBuffer.Clear();
            return clearedCount;
        }

        private int ClearEnemyProjectilesNearAnchors(List<Vector2> points, float radius, float preferredSide, float preferredInfluence)
        {
            if (points == null || points.Count == 0)
            {
                return 0;
            }

            if (points.Count <= 1 || Mathf.Abs(preferredSide) <= 0.5f || preferredInfluence <= 0.01f)
            {
                return ClearEnemyProjectilesNearPoints(points, radius);
            }

            _breakthroughProjectileBuffer.Clear();
            EnemyProjectileLogic.CollectActiveProjectiles(_breakthroughProjectileBuffer);

            int clearedCount = 0;
            for (int index = 0; index < _breakthroughProjectileBuffer.Count; index++)
            {
                EnemyProjectileLogic projectile = _breakthroughProjectileBuffer[index];
                if (projectile == null)
                {
                    continue;
                }

                Vector2 projectilePosition = projectile.WorldPosition;
                bool insideAnyAnchor = false;
                for (int anchorIndex = 0; anchorIndex < points.Count; anchorIndex++)
                {
                    float anchorScale = ResolveImpactAnchorScale(anchorIndex, points.Count, preferredSide, preferredInfluence);
                    float effectiveRadius = Mathf.Max(0.01f, radius * anchorScale);
                    if ((projectilePosition - points[anchorIndex]).sqrMagnitude <= effectiveRadius * effectiveRadius)
                    {
                        insideAnyAnchor = true;
                        break;
                    }
                }

                if (!insideAnyAnchor)
                {
                    continue;
                }

                projectile.ForceDissipate(ProjectileImpactType.Damageable);
                clearedCount++;
            }

            _breakthroughProjectileBuffer.Clear();
            return clearedCount;
        }

        private int ClearHazardsNearImpact(Vector2 point, float radius)
        {
            if (_activeRoom == null)
            {
                return 0;
            }

            CombatRoomArenaPressureController arenaPressureController = _activeRoom.GetComponentInChildren<CombatRoomArenaPressureController>(true);
            return arenaPressureController != null
                ? arenaPressureController.ClearHazardsNearPoint(point, radius)
                : 0;
        }

        private int ClearHazardsNearImpactPoints(List<Vector2> points, float radius)
        {
            if (points == null || points.Count == 0)
            {
                return 0;
            }

            if (points.Count == 1)
            {
                return ClearHazardsNearImpact(points[0], radius);
            }

            int clearedCount = 0;
            for (int index = 0; index < points.Count; index++)
            {
                clearedCount += ClearHazardsNearImpact(points[index], radius);
            }

            return clearedCount;
        }

        private int ClearHazardsNearImpactAnchors(List<Vector2> points, float radius, float preferredSide, float preferredInfluence)
        {
            if (points == null || points.Count == 0)
            {
                return 0;
            }

            if (points.Count <= 1 || Mathf.Abs(preferredSide) <= 0.5f || preferredInfluence <= 0.01f)
            {
                return ClearHazardsNearImpactPoints(points, radius);
            }

            int clearedCount = 0;
            for (int index = 0; index < points.Count; index++)
            {
                float anchorScale = ResolveImpactAnchorScale(index, points.Count, preferredSide, preferredInfluence);
                clearedCount += ClearHazardsNearImpact(points[index], radius * anchorScale);
            }

            return clearedCount;
        }

        private bool ActivateImpactRelief(Vector2 point, float radius, float duration)
        {
            if (_activeRoom == null)
            {
                return false;
            }

            CombatRoomArenaPressureController arenaPressureController = _activeRoom.GetComponentInChildren<CombatRoomArenaPressureController>(true);
            return arenaPressureController != null
                && arenaPressureController.TryActivateOpeningImpactRelief(point, radius, duration);
        }

        private bool TryGetActiveImpactRelief(out Vector2 center, out float radius, out float normalized)
        {
            center = Vector2.zero;
            radius = 0f;
            normalized = 0f;

            if (_activeRoom == null)
            {
                return false;
            }

            CombatRoomArenaPressureController arenaPressureController = _activeRoom.GetComponentInChildren<CombatRoomArenaPressureController>(true);
            return arenaPressureController != null
                && (arenaPressureController.TryGetOpeningImpactProtectedPocket(out center, out radius, out _, out normalized)
                    || arenaPressureController.TryGetOpeningImpactRelief(out center, out radius, out _, out normalized));
        }

        private OpeningCadenceVolleyRole BuildImpactSecurePulseAnchors(
            Vector2 impactCenter,
            float impactRadius,
            List<Vector2> anchors,
            out float anchorRadiusScale,
            out float reliefRadiusScale)
        {
            anchors.Clear();
            anchors.Add(impactCenter);
            anchorRadiusScale = 1f;
            reliefRadiusScale = 1f;

            if (!HasRecentOpeningCadenceHitRole())
            {
                return OpeningCadenceVolleyRole.None;
            }

            OpeningCadenceVolleyRole recentRole = _recentOpeningCadenceHitRole;
            float recentRoleWeight = Mathf.Clamp01(_recentOpeningCadenceHitRoleWeight);
            if (!TryResolveImpactSecurePulseAxis(impactCenter, out Vector2 pocketAxis))
            {
                return recentRole;
            }

            switch (recentRole)
            {
                case OpeningCadenceVolleyRole.Core:
                    anchorRadiusScale = Mathf.Lerp(1.08f, 1.2f, recentRoleWeight);
                    reliefRadiusScale = Mathf.Lerp(1.06f, 1.14f, recentRoleWeight);
                    return recentRole;

                case OpeningCadenceVolleyRole.Flank:
                {
                    anchors.Clear();
                    float offset = impactRadius * Mathf.Lerp(0.34f, 0.48f, recentRoleWeight);
                    anchors.Add(impactCenter + (pocketAxis * offset));
                    anchors.Add(impactCenter - (pocketAxis * offset));
                    anchorRadiusScale = Mathf.Lerp(0.8f, 0.92f, recentRoleWeight);
                    reliefRadiusScale = Mathf.Lerp(0.98f, 1.04f, recentRoleWeight);
                    return recentRole;
                }

                case OpeningCadenceVolleyRole.Edge:
                {
                    anchors.Clear();
                    float offset = impactRadius * Mathf.Lerp(0.56f, 0.72f, recentRoleWeight);
                    anchors.Add(impactCenter + (pocketAxis * offset));
                    anchors.Add(impactCenter - (pocketAxis * offset));
                    anchorRadiusScale = Mathf.Lerp(0.68f, 0.82f, recentRoleWeight);
                    reliefRadiusScale = Mathf.Lerp(1f, 1.08f, recentRoleWeight);
                    return recentRole;
                }

                default:
                    return OpeningCadenceVolleyRole.None;
            }
        }

        private bool TryResolveImpactSecurePulseAxis(Vector2 impactCenter, out Vector2 pocketAxis)
        {
            pocketAxis = Vector2.right;

            if (TryGetOpeningTargetWorldPosition(out Vector2 targetWorldPosition))
            {
                Vector2 toTarget = targetWorldPosition - impactCenter;
                if (toTarget.sqrMagnitude > 0.0001f)
                {
                    Vector2 forward = toTarget.normalized;
                    pocketAxis = new Vector2(-forward.y, forward.x);
                    return true;
                }
            }

            if (TryResolvePreferredOpeningAimDirection(impactCenter, out Vector2 preferredAimDirection, out _)
                && preferredAimDirection.sqrMagnitude > 0.0001f)
            {
                Vector2 forward = preferredAimDirection.normalized;
                pocketAxis = new Vector2(-forward.y, forward.x);
                return true;
            }

            return false;
        }

        private bool TryResolveImpactHoldPointInfluence(Vector2 point, out float holdStrength)
        {
            holdStrength = 0f;

            if (HasImpactSecureHold()
                && TryGetImpactSecurePulseAnchors(_impactHoldAnchorBuffer, out float anchorRadius, out float anchorNormalized)
                && _impactHoldAnchorBuffer.Count > 0)
            {
                float strongestHoldStrength = 0f;
                for (int index = 0; index < _impactHoldAnchorBuffer.Count; index++)
                {
                    float candidateStrength = ResolveImpactHoldStrength(
                        point,
                        _impactHoldAnchorBuffer[index],
                        anchorRadius,
                        anchorNormalized);
                    if (candidateStrength > strongestHoldStrength)
                    {
                        strongestHoldStrength = candidateStrength;
                    }
                }

                holdStrength = strongestHoldStrength;
                return holdStrength > 0.01f;
            }

            if (!TryGetActiveImpactRelief(out Vector2 impactCenter, out float impactRadius, out float impactNormalized))
            {
                return false;
            }

            holdStrength = ResolveImpactHoldStrength(point, impactCenter, impactRadius, impactNormalized);
            return holdStrength > 0.01f;
        }

        private static float ResolveImpactHoldStrength(Vector2 point, Vector2 anchor, float radius, float normalized)
        {
            float effectiveRadius = Mathf.Max(0.18f, radius * 0.94f);
            float distance = Vector2.Distance(point, anchor);
            if (distance > effectiveRadius)
            {
                return 0f;
            }

            float edgeWeight = 1f - Mathf.Clamp01(distance / effectiveRadius);
            return Mathf.Clamp01(Mathf.Lerp(0.58f, 1f, edgeWeight) * Mathf.Lerp(0.72f, 1f, normalized));
        }

        private bool TryResolvePreferredImpactSecureHoldInfluence(Vector2 point, out float holdStrength, out float preferredSide)
        {
            holdStrength = 0f;
            preferredSide = 0f;

            if (!HasImpactSecureHold()
                || !TryGetPreferredImpactSecureAnchor(out Vector2 preferredAnchor, out float preferredRadius, out float preferredNormalized))
            {
                return false;
            }

            TryGetPreferredSecureAnchorSide(out preferredSide, out _);
            holdStrength = ResolveImpactHoldStrength(point, preferredAnchor, preferredRadius, preferredNormalized);
            return holdStrength > 0.01f;
        }

        private static float ResolveImpactAnchorScale(int anchorIndex, int anchorCount, float preferredSide, float preferredInfluence)
        {
            if (anchorCount <= 1 || Mathf.Abs(preferredSide) <= 0.5f || preferredInfluence <= 0.01f)
            {
                return 1f;
            }

            int preferredIndex = preferredSide > 0f ? 0 : 1;
            bool preferredAnchor = anchorIndex == preferredIndex;
            return preferredAnchor
                ? Mathf.Lerp(1f, 1.28f, preferredInfluence)
                : Mathf.Lerp(1f, 0.72f, preferredInfluence);
        }

        private bool TryTriggerImpactHoldSecure(
            EnemyHealth enemyHealth,
            bool killedHintedTarget,
            EnemyFormationPriorityLevel priorityLevel,
            bool killedBossTarget,
            bool killedEliteTarget,
            bool chainReady,
            out float appliedOpeningDuration,
            out float appliedMomentumDuration,
            out int clearedProjectileCount,
            out int clearedHazardCount)
        {
            appliedOpeningDuration = 0f;
            appliedMomentumDuration = 0f;
            clearedProjectileCount = 0;
            clearedHazardCount = 0;

            if (!_impactHoldActive
                || enemyHealth == null
                || !TryGetActiveImpactRelief(out Vector2 impactCenter, out float impactRadius, out float impactNormalized))
            {
                return false;
            }

            bool importantKill = killedHintedTarget
                || priorityLevel != EnemyFormationPriorityLevel.None
                || killedBossTarget
                || killedEliteTarget;
            if (!importantKill)
            {
                return false;
            }

            float threatRadius = Mathf.Max(0.32f, impactRadius * 1.18f);
            bool threatensImpactZone = ((Vector2)enemyHealth.transform.position - impactCenter).sqrMagnitude <= threatRadius * threatRadius;
            if (!threatensImpactZone && !killedHintedTarget)
            {
                return false;
            }

            float holdScale = Mathf.Clamp01(Mathf.Lerp(0.72f, 1f, impactNormalized));
            bool preferredSecureHoldActive = TryResolvePreferredImpactSecureHoldInfluence(transform.position, out float preferredHoldStrength, out float preferredSecureSide);
            if (preferredSecureHoldActive)
            {
                holdScale = Mathf.Clamp01(holdScale * Mathf.Lerp(1f, 1f + impactPreferredSecureOpeningScaleBonus, preferredHoldStrength));
            }
            appliedOpeningDuration = SustainOpening((chainReady ? impactChainSecureOpeningBonus : impactSecureOpeningBonus) * holdScale);
            if (momentumController != null && momentumController.IsMomentumActive)
            {
                appliedMomentumDuration = momentumController.SustainMomentum((chainReady ? impactChainSecureMomentumBonus : impactSecureMomentumBonus) * holdScale);
            }

            OpeningCadenceVolleyRole impactSecureRole = BuildImpactSecurePulseAnchors(
                impactCenter,
                impactRadius,
                _impactPulseAnchorBuffer,
                out float anchorRadiusScale,
                out float reliefRadiusScale);
            float clearProjectileRadius = (chainReady ? impactChainSecureProjectileClearRadius : impactSecureProjectileClearRadius)
                * Mathf.Lerp(0.9f, 1.08f, holdScale)
                * anchorRadiusScale;
            float clearHazardRadius = (chainReady ? impactChainSecureHazardClearRadius : impactSecureHazardClearRadius)
                * Mathf.Lerp(0.9f, 1.08f, holdScale)
                * anchorRadiusScale;
            bool hasPreferredSide = TryGetPreferredSecureAnchorSide(out float preferredSide, out float preferredSideInfluence);
            if (preferredSecureHoldActive)
            {
                float preferredScale = Mathf.Lerp(1f, 1f + impactPreferredSecurePulseScaleBonus, preferredHoldStrength);
                clearProjectileRadius *= preferredScale;
                clearHazardRadius *= preferredScale;
            }
            clearedProjectileCount = ClearEnemyProjectilesNearAnchors(
                _impactPulseAnchorBuffer,
                clearProjectileRadius,
                preferredSide,
                preferredSideInfluence);
            clearedHazardCount = ClearHazardsNearImpactAnchors(
                _impactPulseAnchorBuffer,
                clearHazardRadius,
                preferredSide,
                preferredSideInfluence);
            ActivateImpactRelief(
                impactCenter,
                impactRadius * Mathf.Max(1f, impactSecureReliefRadiusScale * reliefRadiusScale)
                * (preferredSecureHoldActive
                    ? Mathf.Lerp(1f, 1f + impactPreferredSecureRadiusScaleBonus, preferredHoldStrength)
                    : 1f),
                chainReady ? impactChainSecureReliefDuration : impactSecureReliefDuration);

            string preferredPrefix = preferredSecureHoldActive ? $"{ResolvePreferredSideLabel(preferredSecureSide)} " : string.Empty;
            _recentBreakthroughChainLabel = impactSecureRole != OpeningCadenceVolleyRole.None
                ? clearedHazardCount > 0
                    ? (chainReady ? $"{preferredPrefix}{ResolveOpeningCadenceRoleHitLabel(impactSecureRole)} CHAIN" : $"{preferredPrefix}{ResolveOpeningCadenceRoleHitLabel(impactSecureRole)} SECURE")
                    : clearedProjectileCount > 0
                        ? $"{preferredPrefix}{ResolveOpeningCadenceRoleHitLabel(impactSecureRole)} HOLD"
                        : $"{preferredPrefix}{ResolveOpeningCadenceRoleHitLabel(impactSecureRole)} ZONE"
                : clearedHazardCount > 0
                    ? (chainReady ? $"{preferredPrefix}CHAIN SECURE" : $"{preferredPrefix}IMPACT SECURE")
                    : clearedProjectileCount > 0
                        ? (chainReady ? $"{preferredPrefix}CHAIN HOLD" : $"{preferredPrefix}IMPACT HOLD")
                        : $"{preferredPrefix}ZONE HOLD";
            _recentBreakthroughChainExpiresAt = Time.time + Mathf.Max(0.1f, impactSecureFeedbackDuration);
            PrimeImpactSecureHold(chainReady);
            _preferredImpactSecureHoldActive = preferredSecureHoldActive;
            return true;
        }

        private void PrimeImpactSecureHold(bool chainReady)
        {
            float duration = Mathf.Max(0.1f, chainReady ? impactChainSecureHoldDuration : impactSecureHoldDuration);
            _impactSecureHoldExpiresAt = Mathf.Max(_impactSecureHoldExpiresAt, Time.time + duration);
            _impactSecureHoldChainActive |= chainReady;
            _nextImpactSecurePulseAt = _nextImpactSecurePulseAt > 0f
                ? Mathf.Min(_nextImpactSecurePulseAt, Time.time)
                : Time.time;
        }

        private void UpdateImpactSecureHold()
        {
            if (!HasImpactSecureHold())
            {
                if (_impactSecureHoldChainActive || _preferredImpactSecureHoldActive || _impactSecureHoldExpiresAt > 0f || _nextImpactSecurePulseAt > 0f)
                {
                    _impactSecureHoldChainActive = false;
                    _preferredImpactSecureHoldActive = false;
                    _impactSecureHoldExpiresAt = float.NegativeInfinity;
                    _nextImpactSecurePulseAt = 0f;
                    RaiseOpeningStateChanged();
                }

                return;
            }

            if (!HasActiveOpeningImpactRelief())
            {
                _impactSecureHoldChainActive = false;
                _preferredImpactSecureHoldActive = false;
                _impactSecureHoldExpiresAt = float.NegativeInfinity;
                _nextImpactSecurePulseAt = 0f;
                RaiseOpeningStateChanged();
                return;
            }

            if (Time.time < _nextImpactSecurePulseAt)
            {
                return;
            }

            if (!TryResolveImpactHoldPointInfluence(transform.position, out float holdStrength)
                || !TryGetActiveImpactRelief(out Vector2 impactCenter, out float impactRadius, out float impactNormalized))
            {
                return;
            }

            bool chainActive = _impactSecureHoldChainActive;
            _nextImpactSecurePulseAt = Time.time + Mathf.Max(0.02f, chainActive ? impactChainSecurePulseInterval : impactSecurePulseInterval);

            float holdScale = Mathf.Clamp01(holdStrength * Mathf.Lerp(0.8f, 1f, impactNormalized));
            bool preferredSecureHoldActive = TryResolvePreferredImpactSecureHoldInfluence(transform.position, out float preferredHoldStrength, out float preferredSecureSide);
            if (preferredSecureHoldActive)
            {
                holdScale = Mathf.Clamp01(holdScale * Mathf.Lerp(1f, 1f + impactPreferredSecurePulseScaleBonus, preferredHoldStrength));
            }
            OpeningCadenceVolleyRole impactPulseRole = BuildImpactSecurePulseAnchors(
                impactCenter,
                impactRadius,
                _impactPulseAnchorBuffer,
                out float anchorRadiusScale,
                out float reliefRadiusScale);
            float pulseProjectileRadius = (chainActive ? impactChainSecurePulseProjectileClearRadius : impactSecurePulseProjectileClearRadius)
                * Mathf.Lerp(0.92f, 1.1f, holdScale)
                * anchorRadiusScale;
            float pulseHazardRadius = (chainActive ? impactChainSecurePulseHazardClearRadius : impactSecurePulseHazardClearRadius)
                * Mathf.Lerp(0.92f, 1.1f, holdScale)
                * anchorRadiusScale;
            bool hasPreferredSide = TryGetPreferredSecureAnchorSide(out float preferredSide, out float preferredSideInfluence);
            if (preferredSecureHoldActive)
            {
                float preferredScale = Mathf.Lerp(1f, 1f + impactPreferredSecurePulseScaleBonus, preferredHoldStrength);
                pulseProjectileRadius *= preferredScale;
                pulseHazardRadius *= preferredScale;
            }
            int clearedProjectileCount = hasPreferredSide
                ? ClearEnemyProjectilesNearAnchors(
                    _impactPulseAnchorBuffer,
                    pulseProjectileRadius,
                    preferredSide,
                    preferredSideInfluence)
                : ClearEnemyProjectilesNearPoints(
                    _impactPulseAnchorBuffer,
                    pulseProjectileRadius);
            int clearedHazardCount = hasPreferredSide
                ? ClearHazardsNearImpactAnchors(
                    _impactPulseAnchorBuffer,
                    pulseHazardRadius,
                    preferredSide,
                    preferredSideInfluence)
                : ClearHazardsNearImpactPoints(
                    _impactPulseAnchorBuffer,
                    pulseHazardRadius);

            bool contestedPulse = clearedProjectileCount > 0 || clearedHazardCount > 0;
            if (!contestedPulse)
            {
                if (_preferredImpactSecureHoldActive != preferredSecureHoldActive)
                {
                    _preferredImpactSecureHoldActive = preferredSecureHoldActive;
                    RaiseOpeningStateChanged();
                }
                return;
            }

            float appliedOpeningDuration = SustainOpening((chainActive ? impactChainSecurePulseOpeningBonus : impactSecurePulseOpeningBonus) * holdScale);
            ActivateImpactRelief(
                impactCenter,
                impactRadius * Mathf.Lerp(1.02f, impactSecureReliefRadiusScale * reliefRadiusScale, holdScale)
                * (preferredSecureHoldActive
                    ? Mathf.Lerp(1f, 1f + impactPreferredSecureRadiusScaleBonus, preferredHoldStrength)
                    : 1f),
                chainActive ? impactChainSecurePulseReliefDuration : impactSecurePulseReliefDuration);
            string preferredPrefix = preferredSecureHoldActive ? $"{ResolvePreferredSideLabel(preferredSecureSide)} " : string.Empty;
            _recentBreakthroughChainLabel = impactPulseRole != OpeningCadenceVolleyRole.None
                ? clearedHazardCount > 0
                    ? (chainActive ? $"{preferredPrefix}{ResolveOpeningCadenceRoleHitLabel(impactPulseRole)} CHAIN" : $"{preferredPrefix}{ResolveOpeningCadenceRoleHitLabel(impactPulseRole)} PULSE")
                    : $"{preferredPrefix}{ResolveOpeningCadenceRoleHitLabel(impactPulseRole)} HOLD"
                : clearedHazardCount > 0
                    ? (chainActive ? $"{preferredPrefix}CHAIN PULSE" : $"{preferredPrefix}SECURE PULSE")
                    : $"{preferredPrefix}HOLD PULSE";
            _recentBreakthroughChainExpiresAt = Time.time + Mathf.Max(0.1f, impactSecurePulseFeedbackDuration);
            if (_preferredImpactSecureHoldActive != preferredSecureHoldActive)
            {
                _preferredImpactSecureHoldActive = preferredSecureHoldActive;
                RaiseOpeningStateChanged();
            }
            if (appliedOpeningDuration <= 0.01f)
            {
                RaiseOpeningStateChanged();
            }
        }

        private static string ResolveBreakthroughHitDetail(float openingDuration, int projectileClearCount, int hazardClearCount, string shotProfileKeyword = "", OpeningCadenceVolleyRole cadenceRole = OpeningCadenceVolleyRole.None)
        {
            string detail = openingDuration > 0.01f
                ? $"OPEN +{openingDuration:0.0}s"
                : "OPEN HOLD";

            if (!string.IsNullOrWhiteSpace(shotProfileKeyword))
            {
                detail = $"{detail} / {ResolveShotProfileHitDetailAnchor(shotProfileKeyword)}";
            }

            if (cadenceRole != OpeningCadenceVolleyRole.None)
            {
                detail = $"{detail} / {ResolveOpeningCadenceRoleHitDetailAnchor(cadenceRole)}";
            }

            if (hazardClearCount > 0)
            {
                return $"{detail} / CLEAR x{hazardClearCount}";
            }

            if (projectileClearCount > 0)
            {
                return $"{detail} / CUT x{projectileClearCount}";
            }

            return detail;
        }

        private bool TryResolvePreferredImpactHitPulse(
            in PlayerProjectileHitSignal signal,
            out float bonusStrength,
            out float preferredSide,
            out Vector2 preferredImpactPoint)
        {
            bonusStrength = 0f;
            preferredSide = 0f;
            preferredImpactPoint = (Vector2)signal.ImpactPosition;

            if (!HasPreferredImpactHoldDrive
                || signal.Direction.sqrMagnitude <= 0.0001f
                || !TryResolvePreferredImpactSecureHoldInfluence((Vector2)signal.ImpactPosition, out float holdStrength, out preferredSide))
            {
                return false;
            }

            float directionalAlignment = 1f;
            if (TryGetPreferredImpactSecureAnchor(out Vector2 preferredAnchor, out _, out _)
                && TryGetOpeningTargetWorldPosition(out Vector2 targetPosition))
            {
                Vector2 expectedDirection = targetPosition - preferredAnchor;
                if (expectedDirection.sqrMagnitude > 0.0001f)
                {
                    float directionDot = Mathf.Clamp(
                        Vector2.Dot(signal.Direction.normalized, expectedDirection.normalized),
                        -1f,
                        1f);
                    directionalAlignment = Mathf.InverseLerp(preferredImpactHitAlignmentThreshold, 1f, directionDot);
                    float anchorBlend = Mathf.Lerp(
                        0.06f,
                        preferredImpactHitAnchorBlend,
                        Mathf.Clamp01(holdStrength * Mathf.Max(0.2f, directionalAlignment)));
                    preferredImpactPoint = Vector2.Lerp((Vector2)signal.ImpactPosition, preferredAnchor, anchorBlend);
                }
            }
            else
            {
                directionalAlignment = Mathf.Lerp(0.42f, 0.72f, Mathf.Clamp01(holdStrength));
            }

            if (_preferredImpactSecureHoldActive)
            {
                directionalAlignment = Mathf.Max(directionalAlignment, 0.42f);
            }

            bonusStrength = Mathf.Clamp01(holdStrength * Mathf.Lerp(0.55f, 1f, directionalAlignment));
            return bonusStrength > 0.01f;
        }

        private static string ResolveShotProfileHitHeadline(string shotProfileKeyword, bool chainReady, OpeningCadenceVolleyRole cadenceRole = OpeningCadenceVolleyRole.None)
        {
            string cadenceRoleLabel = ResolveOpeningCadenceRoleHitLabel(cadenceRole);
            if (string.IsNullOrWhiteSpace(shotProfileKeyword))
            {
                return cadenceRole != OpeningCadenceVolleyRole.None
                    ? chainReady ? $"{cadenceRoleLabel} CHAIN" : $"{cadenceRoleLabel} CUT"
                    : chainReady ? "CHAIN CUT" : "LANE CUT";
            }

            return cadenceRole != OpeningCadenceVolleyRole.None
                ? chainReady
                    ? $"{shotProfileKeyword} {cadenceRoleLabel} CHAIN"
                    : $"{shotProfileKeyword} {cadenceRoleLabel}"
                : chainReady
                    ? $"{shotProfileKeyword} CHAIN"
                    : $"{shotProfileKeyword} CUT";
        }

        private static string ResolveShotProfileHitLabel(string shotProfileKeyword, int projectileClearCount, int hazardClearCount, OpeningCadenceVolleyRole cadenceRole = OpeningCadenceVolleyRole.None)
        {
            string cadenceRoleLabel = ResolveOpeningCadenceRoleHitLabel(cadenceRole);
            if (string.IsNullOrWhiteSpace(shotProfileKeyword))
            {
                string fallbackLabel = hazardClearCount > 0
                    ? "IMPACT CLEAR"
                    : projectileClearCount > 0
                        ? "LANE CUT"
                        : "TARGET CUT";
                return cadenceRole != OpeningCadenceVolleyRole.None
                    ? $"{cadenceRoleLabel} CUT"
                    : fallbackLabel;
            }

            if (hazardClearCount > 0)
            {
                return cadenceRole != OpeningCadenceVolleyRole.None
                    ? $"{shotProfileKeyword} {cadenceRoleLabel}"
                    : $"{shotProfileKeyword} CLEAR";
            }

            if (projectileClearCount > 0)
            {
                return cadenceRole != OpeningCadenceVolleyRole.None
                    ? $"{shotProfileKeyword} {cadenceRoleLabel}"
                    : $"{shotProfileKeyword} CUT";
            }

            return cadenceRole != OpeningCadenceVolleyRole.None
                ? $"{shotProfileKeyword} {cadenceRoleLabel}"
                : $"{shotProfileKeyword} HIT";
        }

        private static string DecoratePreferredImpactHitHeadline(string headline, float preferredSide)
        {
            string preferredLabel = $"{ResolvePreferredSideShortLabel(preferredSide)}-HOLD";
            return string.IsNullOrWhiteSpace(headline)
                ? preferredLabel
                : $"{preferredLabel} {headline}";
        }

        private static string DecoratePreferredImpactHitLabel(string label, float preferredSide)
        {
            string preferredLabel = $"{ResolvePreferredSideShortLabel(preferredSide)}-HOLD";
            return string.IsNullOrWhiteSpace(label)
                ? preferredLabel
                : $"{preferredLabel} {label}";
        }

        private static string DecoratePreferredImpactHitDetail(string detail, float preferredSide)
        {
            string preferredLabel = $"{ResolvePreferredSideLabel(preferredSide)} HOLD";
            return string.IsNullOrWhiteSpace(detail)
                ? preferredLabel
                : $"{detail} / {preferredLabel}";
        }

        private static string ResolveShotProfileHitDetailAnchor(string shotProfileKeyword)
        {
            return shotProfileKeyword switch
            {
                "LASER" => "LINE PRESSURE",
                "SPLIT" => "FAN PRESSURE",
                "ORBIT" => "CLOSE CRUSH",
                "SHIELD" => "GUARD WINDOW",
                "BOUNCE" => "EDGE CUT",
                "BLAST" => "BLAST ZONE",
                "LEECH" => "LEECH HOLD",
                "SHOT RESET" => "RESET WINDOW",
                _ => "SHOT PRESSURE"
            };
        }

        private static string ResolveOpeningCadenceRoleHitLabel(OpeningCadenceVolleyRole cadenceRole)
        {
            return cadenceRole switch
            {
                OpeningCadenceVolleyRole.Core => "CORE",
                OpeningCadenceVolleyRole.Flank => "FLANK",
                OpeningCadenceVolleyRole.Edge => "EDGE",
                _ => string.Empty
            };
        }

        private static string ResolveOpeningCadenceRoleHitDetailAnchor(OpeningCadenceVolleyRole cadenceRole)
        {
            return cadenceRole switch
            {
                OpeningCadenceVolleyRole.Core => "CORE LOCK",
                OpeningCadenceVolleyRole.Flank => "FLANK BREAK",
                OpeningCadenceVolleyRole.Edge => "EDGE CLEAR",
                _ => string.Empty
            };
        }

        private static Vector2 ResolveRecentFlankFollowUpDirection(
            Vector2 normalizedDirection,
            Vector2 toTarget,
            float signedSlot,
            float assistStrength,
            float preferredSide,
            float preferredSideInfluence)
        {
            float side = Mathf.Abs(signedSlot) > 0.001f
                ? Mathf.Sign(signedSlot)
                : 1f;
            if (Mathf.Abs(preferredSide) > 0.5f && Mathf.Abs(signedSlot) <= 0.001f)
            {
                side = preferredSide;
            }

            Vector2 flankNormal = new(-toTarget.y, toTarget.x);
            float sideBias = Mathf.Abs(preferredSide) > 0.5f && Mathf.Sign(side) == Mathf.Sign(preferredSide)
                ? Mathf.Lerp(1f, 1.24f, preferredSideInfluence)
                : Mathf.Abs(preferredSide) > 0.5f
                    ? Mathf.Lerp(1f, 0.76f, preferredSideInfluence)
                    : 1f;
            Vector2 flankDirection = (toTarget + (flankNormal * side * Mathf.Lerp(0.22f, 0.58f, assistStrength) * sideBias)).normalized;
            return Vector2.Lerp(normalizedDirection, flankDirection, assistStrength * 0.94f).normalized;
        }

        private Vector2 ResolveRecentEdgeFollowUpDirection(
            Vector2 normalizedDirection,
            Vector2 toTarget,
            Vector2 targetPosition,
            float signedSlot,
            float assistStrength,
            float preferredSide,
            float preferredSideInfluence)
        {
            Vector2 edgeDirection = ResolveNearestRoomEdgeDirection(targetPosition);
            if (edgeDirection.sqrMagnitude <= 0.0001f)
            {
                float fallbackAngle = (Mathf.Abs(signedSlot) > 0.001f ? Mathf.Sign(signedSlot) : 1f) * Mathf.Lerp(12f, 28f, assistStrength);
                edgeDirection = Rotate(toTarget, fallbackAngle);
            }

            if (Mathf.Abs(preferredSide) > 0.5f)
            {
                Vector2 preferredEdgeDirection = Rotate(toTarget, preferredSide * Mathf.Lerp(18f, 34f, preferredSideInfluence));
                edgeDirection = Vector2.Lerp(edgeDirection.normalized, preferredEdgeDirection.normalized, Mathf.Lerp(0.24f, 0.68f, preferredSideInfluence));
            }

            Vector2 blended = ((toTarget * 0.76f) + (edgeDirection.normalized * 0.58f)).normalized;
            return Vector2.Lerp(normalizedDirection, blended, assistStrength).normalized;
        }

        private bool TryResolvePreferredSecureAnchorSide(Vector2 origin, Vector2 forwardDirection, out float preferredSide, out float influence)
        {
            preferredSide = 0f;
            influence = 0f;

            if (!TryGetPreferredImpactSecureAnchor(out Vector2 secureAnchor, out float anchorRadius, out _))
            {
                return false;
            }

            Vector2 toAnchor = secureAnchor - origin;
            if (toAnchor.sqrMagnitude <= 0.0001f)
            {
                return false;
            }

            Vector2 forward = forwardDirection.sqrMagnitude > 0.0001f
                ? forwardDirection.normalized
                : Vector2.right;
            Vector2 normal = new(-forward.y, forward.x);
            float lateral = Vector2.Dot(toAnchor.normalized, normal);
            if (Mathf.Abs(lateral) <= 0.08f)
            {
                return false;
            }

            preferredSide = Mathf.Sign(lateral);
            float radiusScale = anchorRadius > 0.01f
                ? 1f - Mathf.Clamp01(toAnchor.magnitude / Mathf.Max(0.12f, anchorRadius * 1.8f))
                : 1f;
            influence = Mathf.Clamp01(Mathf.Abs(lateral) * Mathf.Lerp(0.48f, 1f, radiusScale));
            return influence > 0.01f;
        }

        private bool TryResolveRecentPreferredImpactHitProfile(out float preferredSide, out float influence)
        {
            preferredSide = 0f;
            influence = 0f;

            if (!HasRecentPreferredImpactHitPulse())
            {
                return false;
            }

            preferredSide = Mathf.Sign(_recentPreferredImpactHitSide);
            influence = Mathf.Clamp01(_recentPreferredImpactHitStrength);
            return Mathf.Abs(preferredSide) > 0.5f && influence > 0.01f;
        }

        private void MergeRecentPreferredImpactHitProfile(ref float preferredSide, ref float preferredInfluence)
        {
            if (!TryResolveRecentPreferredImpactHitProfile(out float recentPreferredSide, out float recentPreferredInfluence))
            {
                return;
            }

            if (Mathf.Abs(preferredSide) <= 0.5f || preferredInfluence <= 0.01f)
            {
                preferredSide = recentPreferredSide;
                preferredInfluence = recentPreferredInfluence;
                return;
            }

            bool sameSide = Mathf.Sign(preferredSide) == Mathf.Sign(recentPreferredSide);
            if (sameSide)
            {
                preferredInfluence = Mathf.Clamp01(Mathf.Lerp(
                    preferredInfluence,
                    Mathf.Max(preferredInfluence, recentPreferredInfluence),
                    Mathf.Lerp(0.42f, 0.88f, recentPreferredInfluence)));
                return;
            }

            if (recentPreferredInfluence > preferredInfluence * 1.08f)
            {
                preferredSide = recentPreferredSide;
                preferredInfluence = Mathf.Clamp01(Mathf.Lerp(preferredInfluence, recentPreferredInfluence, 0.72f));
            }
        }

        private Vector2 ResolveNearestRoomEdgeDirection(Vector2 worldPosition)
        {
            if (_activeRoom == null)
            {
                return Vector2.zero;
            }

            Bounds roomBounds = _activeRoom.RoomBounds;
            float leftDistance = Mathf.Abs(worldPosition.x - roomBounds.min.x);
            float rightDistance = Mathf.Abs(roomBounds.max.x - worldPosition.x);
            float bottomDistance = Mathf.Abs(worldPosition.y - roomBounds.min.y);
            float topDistance = Mathf.Abs(roomBounds.max.y - worldPosition.y);

            float minDistance = leftDistance;
            Vector2 direction = Vector2.left;

            if (rightDistance < minDistance)
            {
                minDistance = rightDistance;
                direction = Vector2.right;
            }

            if (bottomDistance < minDistance)
            {
                minDistance = bottomDistance;
                direction = Vector2.down;
            }

            if (topDistance < minDistance)
            {
                direction = Vector2.up;
            }

            return direction;
        }

        private static string ResolveOpeningShotProfileKeyword(string compareLabel)
        {
            if (string.IsNullOrWhiteSpace(compareLabel))
            {
                return string.Empty;
            }

            string normalized = compareLabel.Trim().ToUpperInvariant();
            if (normalized.Contains("LASER"))
            {
                return "LASER";
            }

            if (normalized.Contains("SPLIT"))
            {
                return "SPLIT";
            }

            if (normalized.Contains("ORBIT"))
            {
                return "ORBIT";
            }

            if (normalized.Contains("SHIELD"))
            {
                return "SHIELD";
            }

            if (normalized.Contains("BOUNCE"))
            {
                return "BOUNCE";
            }

            if (normalized.Contains("BLAST"))
            {
                return "BLAST";
            }

            if (normalized.Contains("LEECH"))
            {
                return "LEECH";
            }

            if (normalized.Contains("SHOT RESET"))
            {
                return "SHOT RESET";
            }

            return string.Empty;
        }

        private static float ResolveOpeningCadenceStrength(int cadenceIndex, int burstLimit)
        {
            int safeLimit = Mathf.Max(1, burstLimit);
            float normalized = 1f - (Mathf.Clamp(cadenceIndex, 0, safeLimit - 1) / (float)safeLimit);
            return Mathf.Lerp(0.42f, 1f, normalized);
        }

        private static float ResolveRecentOpeningCadenceHitRoleBurstStrength(int burstIndex, int burstLimit)
        {
            int safeLimit = Mathf.Max(1, burstLimit);
            float normalized = 1f - (Mathf.Clamp(burstIndex, 0, safeLimit - 1) / (float)safeLimit);
            return Mathf.Lerp(0.48f, 1f, normalized);
        }

        private float ResolveOpeningCadenceSustain(string shotProfileKeyword, int cadenceOrdinal, int burstLimit)
        {
            float baseSustain = cadenceOrdinal <= 1
                ? openingCadenceFirstBurstSustain
                : openingCadenceRepeatBurstSustain;
            float burstProgress = cadenceOrdinal / (float)Mathf.Max(1, burstLimit);
            float scaledSustain = Mathf.Lerp(baseSustain, baseSustain * 0.45f, Mathf.Clamp01(burstProgress - 0.34f));

            float keywordBonus = shotProfileKeyword switch
            {
                "LASER" => 0.02f,
                "SPLIT" => 0.03f,
                "ORBIT" => 0.015f,
                "SHIELD" => 0.015f,
                "BOUNCE" => 0.018f,
                "BLAST" => 0.035f,
                "LEECH" => 0.02f,
                "SHOT RESET" => 0.01f,
                _ => 0f
            };

            return Mathf.Max(0f, scaledSustain + keywordBonus);
        }

        private float ResolveOpeningCadenceIntervalMultiplier(string shotProfileKeyword, int cadenceOrdinal)
        {
            float baseMultiplier = shotProfileKeyword switch
            {
                "LASER" => openingCadenceFastIntervalMultiplier,
                "SPLIT" => openingCadenceMediumIntervalMultiplier,
                "ORBIT" => openingCadenceHeavyIntervalMultiplier,
                "SHIELD" => openingCadenceMediumIntervalMultiplier,
                "BOUNCE" => openingCadenceFastIntervalMultiplier,
                "BLAST" => openingCadenceHeavyIntervalMultiplier,
                "LEECH" => openingCadenceMediumIntervalMultiplier,
                "SHOT RESET" => openingCadenceResetIntervalMultiplier,
                _ => 1f
            };

            int burstLimit = Mathf.Max(1, openingCadenceBurstLimit);
            int safeOrdinal = Mathf.Clamp(cadenceOrdinal, 1, burstLimit);
            if (safeOrdinal >= burstLimit)
            {
                baseMultiplier = Mathf.Min(baseMultiplier, openingCadenceFinaleIntervalMultiplier);
            }

            float relaxedMultiplier = baseMultiplier + (Mathf.Max(0, safeOrdinal - 1) * Mathf.Max(0f, openingCadenceIntervalRelaxPerBurst));
            return Mathf.Clamp(relaxedMultiplier, Mathf.Min(baseMultiplier, openingCadenceFinaleIntervalMultiplier), 1f);
        }

        private int ResolveOpeningCadenceVolleyBonusShots(string shotProfileKeyword, int cadenceOrdinal, int burstLimit)
        {
            int safeBurstLimit = Mathf.Max(1, burstLimit);
            int safeOrdinal = Mathf.Clamp(cadenceOrdinal, 1, safeBurstLimit);
            bool isFinaleBurst = safeOrdinal >= safeBurstLimit;

            int baseBonus = shotProfileKeyword switch
            {
                "LASER" => openingCadenceLightVolleyBonusShots,
                "SPLIT" => openingCadenceMediumVolleyBonusShots,
                "ORBIT" => openingCadenceLightVolleyBonusShots,
                "SHIELD" => openingCadenceLightVolleyBonusShots,
                "BOUNCE" => openingCadenceMediumVolleyBonusShots,
                "BLAST" => openingCadenceHeavyVolleyBonusShots,
                "LEECH" => openingCadenceLightVolleyBonusShots,
                "SHOT RESET" => openingCadenceLightVolleyBonusShots,
                _ => 0
            };

            if (baseBonus <= 0)
            {
                return 0;
            }

            if (safeOrdinal > 1)
            {
                baseBonus = Mathf.Max(0, baseBonus - 1);
            }

            if (isFinaleBurst)
            {
                baseBonus += 1;
            }

            return Mathf.Max(0, baseBonus);
        }

        private static void ResolveOpeningCadenceSlotWeights(int shotIndex, int shotCount, out float coreWeight, out float flankWeight, out float edgeWeight)
        {
            if (shotCount <= 1)
            {
                coreWeight = 1f;
                flankWeight = 0f;
                edgeWeight = 0f;
                return;
            }

            float centerIndex = (shotCount - 1) * 0.5f;
            float normalizedDistance = Mathf.Clamp01(Mathf.Abs(shotIndex - centerIndex) / Mathf.Max(0.5f, centerIndex));
            coreWeight = 1f - normalizedDistance;
            flankWeight = Mathf.SmoothStep(0f, 1f, normalizedDistance);
            edgeWeight = Mathf.Clamp01((normalizedDistance - 0.35f) / 0.65f);
        }

        private static OpeningCadenceVolleyRole ResolveOpeningCadenceVolleyRole(string shotProfileKeyword, float coreWeight, float flankWeight, float edgeWeight)
        {
            return shotProfileKeyword switch
            {
                "LASER" => OpeningCadenceVolleyRole.Core,
                "BLAST" => OpeningCadenceVolleyRole.Core,
                "ORBIT" => coreWeight >= flankWeight ? OpeningCadenceVolleyRole.Core : OpeningCadenceVolleyRole.Flank,
                "SHIELD" => coreWeight >= flankWeight ? OpeningCadenceVolleyRole.Core : OpeningCadenceVolleyRole.Flank,
                "LEECH" => coreWeight >= flankWeight ? OpeningCadenceVolleyRole.Core : OpeningCadenceVolleyRole.Flank,
                "SPLIT" => flankWeight >= edgeWeight ? OpeningCadenceVolleyRole.Flank : OpeningCadenceVolleyRole.Edge,
                "BOUNCE" => edgeWeight >= flankWeight ? OpeningCadenceVolleyRole.Edge : OpeningCadenceVolleyRole.Flank,
                "SHOT RESET" => coreWeight >= flankWeight ? OpeningCadenceVolleyRole.Core : OpeningCadenceVolleyRole.Flank,
                _ => OpeningCadenceVolleyRole.None
            };
        }

        private static bool PrefersCenteredCadenceVolley(string shotProfileKeyword)
        {
            return shotProfileKeyword == "LASER"
                || shotProfileKeyword == "ORBIT"
                || shotProfileKeyword == "SHIELD"
                || shotProfileKeyword == "LEECH"
                || shotProfileKeyword == "SHOT RESET";
        }

        private static string ResolveOpeningCadenceHeadline(string shotProfileKeyword, int cadenceOrdinal, int burstLimit)
        {
            string keyword = string.IsNullOrWhiteSpace(shotProfileKeyword) ? "OPEN" : shotProfileKeyword;
            return cadenceOrdinal >= Mathf.Max(1, burstLimit)
                ? $"{keyword} FINISH"
                : $"{keyword} BURST";
        }

        private static string ResolveOpeningCadenceLabel(string shotProfileKeyword, int cadenceOrdinal, int burstLimit, int projectileCount)
        {
            string anchor = shotProfileKeyword switch
            {
                "LASER" => "LINE BURST",
                "SPLIT" => "FAN BURST",
                "ORBIT" => "RING BURST",
                "SHIELD" => "GUARD BURST",
                "BOUNCE" => "EDGE BURST",
                "BLAST" => "BLAST BURST",
                "LEECH" => "LEECH BURST",
                "SHOT RESET" => "RESET BURST",
                _ => "OPEN BURST"
            };

            string label = $"{anchor} {Mathf.Clamp(cadenceOrdinal, 1, Mathf.Max(1, burstLimit))}/{Mathf.Max(1, burstLimit)}";
            if (projectileCount > 1)
            {
                label = $"{label} x{projectileCount}";
            }

            return label;
        }

        private static string ResolveOpeningCadenceDetail(string shotProfileKeyword, int cadenceOrdinal, int burstLimit, int projectileCount, float cadenceSustain)
        {
            string anchor = shotProfileKeyword switch
            {
                "LASER" => "FIRST LINE",
                "SPLIT" => "FIRST FAN",
                "ORBIT" => "RING SET",
                "SHIELD" => "GUARD PUSH",
                "BOUNCE" => "EDGE ANGLE",
                "BLAST" => "BLAST PUSH",
                "LEECH" => "LEECH PUSH",
                "SHOT RESET" => "RESET OPEN",
                _ => "OPEN PUSH"
            };
            string volleyRole = shotProfileKeyword switch
            {
                "LASER" => "CORE VOLLEY",
                "SPLIT" => "FLANK VOLLEY",
                "ORBIT" => "RING VOLLEY",
                "SHIELD" => "GUARD VOLLEY",
                "BOUNCE" => "EDGE VOLLEY",
                "BLAST" => "CORE BLAST",
                "LEECH" => "DRAIN VOLLEY",
                "SHOT RESET" => "RESET VOLLEY",
                _ => "OPEN VOLLEY"
            };

            string detail = $"{anchor} / {Mathf.Clamp(cadenceOrdinal, 1, Mathf.Max(1, burstLimit))}/{Mathf.Max(1, burstLimit)}";
            if (projectileCount > 1)
            {
                detail = $"{detail} / {volleyRole} x{projectileCount}";
            }

            if (cadenceSustain > 0.01f)
            {
                detail = $"{detail} / OPEN +{cadenceSustain:0.0}s";
            }

            return detail;
        }

        private static float ResolveShotProfileHitSustainBonus(string shotProfileKeyword, bool chainReady)
        {
            float baseBonus = shotProfileKeyword switch
            {
                "LASER" => 0.06f,
                "SPLIT" => 0.08f,
                "ORBIT" => 0.04f,
                "SHIELD" => 0.03f,
                "BOUNCE" => 0.035f,
                "BLAST" => 0.1f,
                "LEECH" => 0.05f,
                "SHOT RESET" => 0.025f,
                _ => 0f
            };

            return chainReady ? baseBonus * 1.25f : baseBonus;
        }

        private static float ResolveShotProfileProjectileClearScale(string shotProfileKeyword, bool chainReady)
        {
            float scale = shotProfileKeyword switch
            {
                "LASER" => 1.28f,
                "SPLIT" => 1.2f,
                "ORBIT" => 1.08f,
                "SHIELD" => 1.3f,
                "BOUNCE" => 1.12f,
                "BLAST" => 1.18f,
                "LEECH" => 1.06f,
                "SHOT RESET" => 1.02f,
                _ => 1f
            };

            return chainReady ? scale * 1.08f : scale;
        }

        private static float ResolveShotProfileHazardClearScale(string shotProfileKeyword, bool chainReady)
        {
            float scale = shotProfileKeyword switch
            {
                "LASER" => 1.1f,
                "SPLIT" => 1.18f,
                "ORBIT" => 1.04f,
                "SHIELD" => 1.08f,
                "BOUNCE" => 1.22f,
                "BLAST" => 1.36f,
                "LEECH" => 1.02f,
                "SHOT RESET" => 1f,
                _ => 1f
            };

            return chainReady ? scale * 1.08f : scale;
        }

        private static float ResolveOpeningCadenceRoleHitSustainBonus(OpeningCadenceVolleyRole cadenceRole, float cadenceRoleWeight, bool chainReady)
        {
            float baseBonus = cadenceRole switch
            {
                OpeningCadenceVolleyRole.Core => Mathf.Lerp(0f, 0.06f, cadenceRoleWeight),
                OpeningCadenceVolleyRole.Flank => Mathf.Lerp(0f, 0.04f, cadenceRoleWeight),
                OpeningCadenceVolleyRole.Edge => Mathf.Lerp(0f, 0.03f, cadenceRoleWeight),
                _ => 0f
            };

            return chainReady ? baseBonus * 1.1f : baseBonus;
        }

        private static float ResolveOpeningCadenceRoleProjectileClearScale(OpeningCadenceVolleyRole cadenceRole, float cadenceRoleWeight)
        {
            return cadenceRole switch
            {
                OpeningCadenceVolleyRole.Core => Mathf.Lerp(1f, 1.06f, cadenceRoleWeight),
                OpeningCadenceVolleyRole.Flank => Mathf.Lerp(1f, 1.18f, cadenceRoleWeight),
                OpeningCadenceVolleyRole.Edge => Mathf.Lerp(1f, 1.08f, cadenceRoleWeight),
                _ => 1f
            };
        }

        private static float ResolveOpeningCadenceRoleHazardClearScale(OpeningCadenceVolleyRole cadenceRole, float cadenceRoleWeight)
        {
            return cadenceRole switch
            {
                OpeningCadenceVolleyRole.Core => Mathf.Lerp(1f, 1.1f, cadenceRoleWeight),
                OpeningCadenceVolleyRole.Flank => Mathf.Lerp(1f, 1.04f, cadenceRoleWeight),
                OpeningCadenceVolleyRole.Edge => Mathf.Lerp(1f, 1.22f, cadenceRoleWeight),
                _ => 1f
            };
        }

        private static float ResolveOpeningCadenceRoleImpactReliefScale(OpeningCadenceVolleyRole cadenceRole, float cadenceRoleWeight)
        {
            return cadenceRole switch
            {
                OpeningCadenceVolleyRole.Core => Mathf.Lerp(1f, 1.08f, cadenceRoleWeight),
                OpeningCadenceVolleyRole.Flank => Mathf.Lerp(1f, 1.02f, cadenceRoleWeight),
                OpeningCadenceVolleyRole.Edge => Mathf.Lerp(1f, 1.16f, cadenceRoleWeight),
                _ => 1f
            };
        }

        private static void AddProjectileTrait(ref ProjectileTraitState traits, ProjectileTraitFlags flag, float strength)
        {
            if (strength <= 0f)
            {
                return;
            }

            traits.Flags |= flag;
            switch (flag)
            {
                case ProjectileTraitFlags.Explosive:
                    traits.ExplosionStrength += strength;
                    break;
                case ProjectileTraitFlags.Laser:
                    traits.LaserStrength += strength;
                    break;
                case ProjectileTraitFlags.Split:
                    traits.SplitStrength += strength;
                    break;
                case ProjectileTraitFlags.Bounce:
                    traits.BounceStrength += strength;
                    break;
                case ProjectileTraitFlags.Orbit:
                    traits.OrbitStrength += strength;
                    break;
                case ProjectileTraitFlags.Shield:
                    traits.ShieldStrength += strength;
                    break;
                case ProjectileTraitFlags.Lifesteal:
                    traits.LifestealStrength += strength;
                    break;
            }
        }

        private static string ResolveImpactSecureDetail(float openingDuration, int projectileClearCount, int hazardClearCount)
        {
            string detail = openingDuration > 0.01f
                ? $"HOLD +{openingDuration:0.0}s"
                : "HOLD SECURED";

            if (hazardClearCount > 0)
            {
                detail = $"{detail} / CLEAR x{hazardClearCount}";
            }
            else if (projectileClearCount > 0)
            {
                detail = $"{detail} / CUT x{projectileClearCount}";
            }

            return detail;
        }

        private static void AddProjectileModifier(
            List<ProjectileModifier> modifiers,
            ProjectileModifierType modifierType,
            StatModifierOperation operation,
            float value)
        {
            if (modifiers == null || value <= 0f)
            {
                return;
            }

            modifiers.Add(new ProjectileModifier(modifierType, operation, value));
        }

        private void ClearBreakthroughDrive()
        {
            bool hadDrive = _breakthroughDriveActive
                || _impactHoldActive
                || _preferredImpactHoldDriveActive
                || _recentPreferredImpactDriveActive
                || _breakthroughDriveStatModifiers.Count > 0
                || _breakthroughDriveProjectileModifiers.Count > 0;
            if (!hadDrive)
            {
                _breakthroughDriveActive = false;
                _breakthroughDriveChainActive = false;
                _impactHoldActive = false;
                _preferredImpactHoldDriveActive = false;
                _preferredImpactHoldDriveSide = 0f;
                _recentPreferredImpactDriveActive = false;
                _recentPreferredImpactDriveSide = 0f;
                _recentPreferredImpactDriveStrength = 0f;
                return;
            }

            _breakthroughDriveStatModifiers.Clear();
            _breakthroughDriveProjectileModifiers.Clear();
            playerStats?.SetRouteBreakthroughRuntimeModifiers(null, null);
            playerHealth?.SetRouteBreakthroughProtection(1f, 0f);
            _breakthroughDriveActive = false;
            _breakthroughDriveChainActive = false;
            _impactHoldActive = false;
            _preferredImpactHoldDriveActive = false;
            _preferredImpactHoldDriveSide = 0f;
            _recentPreferredImpactDriveActive = false;
            _recentPreferredImpactDriveSide = 0f;
            _recentPreferredImpactDriveStrength = 0f;

            RaiseOpeningStateChanged();
        }

        private void ClearBreakthroughPressure()
        {
            for (int i = 0; i < _breakthroughPressureControllers.Count; i++)
            {
                EnemyController enemyController = _breakthroughPressureControllers[i];
                if (enemyController == null)
                {
                    continue;
                }

                enemyController.SetRuntimePressureSpeedMultiplier(1f);
                enemyController.SetRuntimePressureContactDamageMultiplier(1f);
            }

            _breakthroughPressureControllers.Clear();
        }

        private bool HasRecentCollapseFeedback()
        {
            return _recentCollapseExpiresAt > 0f
                && Time.time < _recentCollapseExpiresAt
                && !string.IsNullOrWhiteSpace(_recentCollapseLabel);
        }

        private void ClearRecentCollapseFeedback()
        {
            _recentCollapseExpiresAt = float.NegativeInfinity;
            _recentCollapseLabel = string.Empty;
        }

        private bool HasRecentBreakthroughChainFeedback()
        {
            return _recentBreakthroughChainExpiresAt > 0f
                && Time.time < _recentBreakthroughChainExpiresAt
                && !string.IsNullOrWhiteSpace(_recentBreakthroughChainLabel);
        }

        private bool HasRecentOpeningCadenceFeedback()
        {
            return _recentOpeningCadenceExpiresAt > 0f
                && Time.time < _recentOpeningCadenceExpiresAt
                && !string.IsNullOrWhiteSpace(_recentOpeningCadenceLabel);
        }

        private bool HasRecentOpeningCadenceHitRole()
        {
            return _recentOpeningCadenceHitRole != OpeningCadenceVolleyRole.None
                && _recentOpeningCadenceHitRoleExpiresAt > 0f
                && Time.time < _recentOpeningCadenceHitRoleExpiresAt;
        }

        private bool HasRecentPreferredImpactHitPulse()
        {
            return _recentPreferredImpactHitExpiresAt > 0f
                && Time.time < _recentPreferredImpactHitExpiresAt
                && Mathf.Abs(_recentPreferredImpactHitSide) > 0.5f
                && _recentPreferredImpactHitStrength > 0.01f;
        }

        private void ClearRecentBreakthroughChainFeedback()
        {
            _recentBreakthroughChainExpiresAt = float.NegativeInfinity;
            _recentBreakthroughChainLabel = string.Empty;
        }

        private void ClearRecentOpeningCadenceFeedback()
        {
            _recentOpeningCadenceExpiresAt = float.NegativeInfinity;
            _recentOpeningCadenceLabel = string.Empty;
        }

        private void ClearRecentOpeningCadenceHitRole()
        {
            _recentOpeningCadenceHitRoleExpiresAt = float.NegativeInfinity;
            _recentOpeningCadenceHitRole = OpeningCadenceVolleyRole.None;
            _recentOpeningCadenceHitRoleWeight = 0f;
            _recentOpeningCadenceHitRoleBurstCount = 0;
        }

        private void ClearRecentPreferredImpactHitPulse()
        {
            _recentPreferredImpactHitExpiresAt = float.NegativeInfinity;
            _recentPreferredImpactHitSide = 0f;
            _recentPreferredImpactHitStrength = 0f;
        }

        private string ResolveRecentOpeningCadenceHitRoleBurstLabel()
        {
            string anchor = ResolveOpeningCadenceRoleHitDetailAnchor(_recentOpeningCadenceHitRole);
            if (string.IsNullOrWhiteSpace(anchor))
            {
                return string.Empty;
            }

            if (_recentOpeningCadenceHitRole != OpeningCadenceVolleyRole.Core
                && TryGetPreferredSecureAnchorSide(out float preferredSide, out _))
            {
                anchor = $"{ResolvePreferredSideLabel(preferredSide)} {anchor}";
            }

            int burstLimit = Mathf.Max(1, recentCadenceRoleBurstLimit);
            int remainingBursts = Mathf.Clamp(burstLimit - _recentOpeningCadenceHitRoleBurstCount, 0, burstLimit);
            return remainingBursts > 0
                ? $"{anchor} / BURST x{remainingBursts}"
                : anchor;
        }

        private string ResolveRecentOpeningCadenceHitRoleCompactTag()
        {
            string roleLabel = ResolveOpeningCadenceRoleHitLabel(_recentOpeningCadenceHitRole);
            if (string.IsNullOrWhiteSpace(roleLabel))
            {
                return string.Empty;
            }

            if (_recentOpeningCadenceHitRole != OpeningCadenceVolleyRole.Core
                && TryGetPreferredSecureAnchorSide(out float preferredSide, out _))
            {
                roleLabel = $"{ResolvePreferredSideShortLabel(preferredSide)}-{roleLabel}";
            }

            int burstLimit = Mathf.Max(1, recentCadenceRoleBurstLimit);
            int remainingBursts = Mathf.Clamp(burstLimit - _recentOpeningCadenceHitRoleBurstCount, 0, burstLimit);
            return remainingBursts > 0
                ? $"{roleLabel} x{remainingBursts}"
                : roleLabel;
        }

        private static string ResolvePreferredSideLabel(float side)
        {
            return side > 0f ? "LEFT" : "RIGHT";
        }

        private static string ResolvePreferredSideShortLabel(float side)
        {
            return side > 0f ? "L" : "R";
        }

        private Color ResolveRecentOpeningCadenceHitRoleAccentColor()
        {
            Color roleColor = _recentOpeningCadenceHitRole switch
            {
                OpeningCadenceVolleyRole.Core => new Color(1f, 0.86f, 0.46f, 1f),
                OpeningCadenceVolleyRole.Flank => new Color(1f, 0.58f, 0.82f, 1f),
                OpeningCadenceVolleyRole.Edge => new Color(0.52f, 0.92f, 1f, 1f),
                _ => _activeAccentColor
            };

            float weight = Mathf.Clamp01(_recentOpeningCadenceHitRoleWeight);
            return Color.Lerp(_activeAccentColor, roleColor, Mathf.Lerp(0.28f, 0.78f, weight));
        }

        private static string ResolveCollapseLabel(bool killedHintedTarget, bool killedBossTarget, bool killedEliteTarget, bool triggeredPocketBreakthrough)
        {
            if (killedBossTarget)
            {
                return "BOSS STAGGER";
            }

            if (triggeredPocketBreakthrough)
            {
                return "POCKET BREAK";
            }

            if (killedHintedTarget)
            {
                return "ROOM STAGGER";
            }

            if (killedEliteTarget)
            {
                return "ELITE BREAK";
            }

            return "OPEN BREAK";
        }

        private static Vector2 Rotate(Vector2 direction, float angleDegrees)
        {
            float radians = angleDegrees * Mathf.Deg2Rad;
            float sin = Mathf.Sin(radians);
            float cos = Mathf.Cos(radians);
            return new Vector2(
                (direction.x * cos) - (direction.y * sin),
                (direction.x * sin) + (direction.y * cos));
        }

        private PlayerInteractionReceiptPresentation EnsureReceiptPresentation()
        {
            if (playerReceiptPresentation != null && playerReceiptPresentation.gameObject == gameObject)
            {
                return playerReceiptPresentation;
            }

            playerReceiptPresentation = GetComponent<PlayerInteractionReceiptPresentation>();
            if (playerReceiptPresentation == null)
            {
                playerReceiptPresentation = gameObject.AddComponent<PlayerInteractionReceiptPresentation>();
            }

            return playerReceiptPresentation;
        }

        private void RaiseOpeningStateChanged()
        {
            OpeningStateChanged?.Invoke();
        }
    }
}
