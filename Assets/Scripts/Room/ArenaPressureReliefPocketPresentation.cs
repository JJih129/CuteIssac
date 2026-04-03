using System.Collections.Generic;
using CuteIssac.Combat;
using CuteIssac.Player;
using UnityEngine;

namespace CuteIssac.Room
{
    /// <summary>
    /// Runtime-only presentation for the temporary safe pocket opened by route-opener pressure relief.
    /// The gameplay logic remains in the pressure controller so authored VFX can replace this later.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CombatRoomArenaPressureController))]
    public sealed class ArenaPressureReliefPocketPresentation : MonoBehaviour
    {
        private const float SpriteWorldSize = 0.64f;

        [Header("References")]
        [SerializeField] private CombatRoomArenaPressureController pressureController;
        [SerializeField] private PlayerRoutePlanCarryController routePlanCarryController;

        [Header("Layout")]
        [SerializeField] [Min(0.8f)] private float minimumPocketRadius = 1.4f;
        [SerializeField] [Range(0.45f, 1f)] private float ellipseRatio = 0.76f;
        [SerializeField] [Min(0f)] private float pulseFrequency = 4.2f;
        [SerializeField] [Min(0.2f)] private float targetLaneWidth = 0.08f;
        [SerializeField] [Min(0.1f)] private float targetMarkerRadius = 0.26f;
        [SerializeField] [Min(0.2f)] private float impactMarkerMinimumRadius = 0.34f;

        [Header("Presentation")]
        [SerializeField] [Range(0f, 1f)] private float fillOpacity = 0.16f;
        [SerializeField] [Range(0f, 1f)] private float ringOpacity = 0.72f;
        [SerializeField] [Range(0f, 1f)] private float guideOpacity = 0.9f;
        [SerializeField] [Range(0f, 1f)] private float laneOpacity = 0.72f;
        [SerializeField] [Range(0f, 1f)] private float breakthroughBandOpacity = 0.18f;
        [SerializeField] [Range(0f, 1f)] private float impactFillOpacity = 0.22f;
        [SerializeField] [Range(0f, 1f)] private float impactRingOpacity = 0.82f;
        [SerializeField] [Range(0f, 1f)] private float impactPocketOpacity = 0.38f;

        [Header("Breakthrough Lane")]
        [SerializeField] [Range(1f, 2f)] private float breakthroughLaneWidthMultiplier = 1.35f;
        [SerializeField] [Range(0f, 0.5f)] private float breakthroughLaneOpacityBonus = 0.18f;
        [SerializeField] [Range(0f, 0.6f)] private float breakthroughMarkerScaleBonus = 0.24f;

        private static Sprite s_CircleSprite;
        private static Sprite s_WhiteSprite;

        private Transform _visualRoot;
        private SpriteRenderer _fillRenderer;
        private SpriteRenderer _ringRenderer;
        private SpriteRenderer _coreRenderer;
        private SpriteRenderer _guideLeftRenderer;
        private SpriteRenderer _guideRightRenderer;
        private SpriteRenderer _breakthroughBandRenderer;
        private SpriteRenderer _targetLaneRenderer;
        private SpriteRenderer _targetMarkerRenderer;
        private SpriteRenderer _roleBurstGuidePrimaryRenderer;
        private SpriteRenderer _roleBurstGuideSecondaryRenderer;
        private SpriteRenderer _impactFillRenderer;
        private SpriteRenderer _impactRingRenderer;
        private SpriteRenderer _impactCoreRenderer;
        private SpriteRenderer _impactPocketPrimaryRenderer;
        private SpriteRenderer _impactPocketSecondaryRenderer;
        private readonly List<Vector2> _impactSecureAnchorBuffer = new();
        private float _phaseOffset;

        private void Awake()
        {
            ResolveReferences();
            _phaseOffset = Random.Range(0f, Mathf.PI * 2f);
        }

        private void OnEnable()
        {
            ResolveReferences();
        }

        private void OnDisable()
        {
            SetVisualActive(false);
        }

        private void Update()
        {
            Vector2 center = Vector2.zero;
            float radius = 0f;
            Color accentColor = Color.white;
            float normalizedRelief = 0f;
            if (pressureController == null
                || !pressureController.TryGetOpeningReliefPocket(out center, out radius, out accentColor, out normalizedRelief))
            {
                SetVisualActive(false);
                return;
            }

            EnsureVisual();
            UpdateVisual(center, radius, accentColor, normalizedRelief);
        }

        private void ResolveReferences()
        {
            if (pressureController == null)
            {
                pressureController = GetComponent<CombatRoomArenaPressureController>();
            }

            if (routePlanCarryController == null)
            {
                routePlanCarryController = FindFirstObjectByType<PlayerRoutePlanCarryController>(FindObjectsInactive.Exclude);
            }
        }

        private void EnsureVisual()
        {
            if (_visualRoot != null)
            {
                SetVisualActive(true);
                return;
            }

            GameObject root = new("ArenaPressureReliefPocket");
            root.layer = gameObject.layer;
            root.transform.SetParent(transform, false);
            _visualRoot = root.transform;

            _fillRenderer = CreatePart("Fill", GetCircleSprite(), Vector3.zero, Vector3.one, new Color(1f, 1f, 1f, 0f), 17);
            _ringRenderer = CreatePart("Ring", GetCircleSprite(), Vector3.zero, Vector3.one, new Color(1f, 1f, 1f, 0f), 18);
            _coreRenderer = CreatePart("Core", GetCircleSprite(), Vector3.zero, new Vector3(0.18f, 0.18f, 1f), new Color(1f, 1f, 1f, 0f), 19);
            _guideLeftRenderer = CreatePart("GuideLeft", GetWhiteSprite(), Vector3.zero, new Vector3(0.08f, 0.26f, 1f), new Color(1f, 1f, 1f, 0f), 20);
            _guideRightRenderer = CreatePart("GuideRight", GetWhiteSprite(), Vector3.zero, new Vector3(0.08f, 0.26f, 1f), new Color(1f, 1f, 1f, 0f), 20);
            _breakthroughBandRenderer = CreatePart("BreakthroughBand", GetWhiteSprite(), Vector3.zero, new Vector3(0.24f, 1f, 1f), new Color(1f, 1f, 1f, 0f), 15);
            _targetLaneRenderer = CreatePart("TargetLane", GetWhiteSprite(), Vector3.zero, new Vector3(targetLaneWidth, 1f, 1f), new Color(1f, 1f, 1f, 0f), 16);
            _targetMarkerRenderer = CreatePart("TargetMarker", GetCircleSprite(), Vector3.zero, new Vector3(targetMarkerRadius, targetMarkerRadius, 1f), new Color(1f, 1f, 1f, 0f), 21);
            _roleBurstGuidePrimaryRenderer = CreatePart("RoleBurstGuidePrimary", GetWhiteSprite(), Vector3.zero, new Vector3(targetLaneWidth, 1f, 1f), new Color(1f, 1f, 1f, 0f), 17);
            _roleBurstGuideSecondaryRenderer = CreatePart("RoleBurstGuideSecondary", GetWhiteSprite(), Vector3.zero, new Vector3(targetLaneWidth, 1f, 1f), new Color(1f, 1f, 1f, 0f), 17);
            _impactFillRenderer = CreatePart("ImpactFill", GetCircleSprite(), Vector3.zero, new Vector3(impactMarkerMinimumRadius, impactMarkerMinimumRadius, 1f), new Color(1f, 1f, 1f, 0f), 22);
            _impactRingRenderer = CreatePart("ImpactRing", GetCircleSprite(), Vector3.zero, new Vector3(impactMarkerMinimumRadius, impactMarkerMinimumRadius, 1f), new Color(1f, 1f, 1f, 0f), 23);
            _impactCoreRenderer = CreatePart("ImpactCore", GetCircleSprite(), Vector3.zero, new Vector3(impactMarkerMinimumRadius * 0.4f, impactMarkerMinimumRadius * 0.4f, 1f), new Color(1f, 1f, 1f, 0f), 24);
            _impactPocketPrimaryRenderer = CreatePart("ImpactPocketPrimary", GetCircleSprite(), Vector3.zero, new Vector3(impactMarkerMinimumRadius * 0.72f, impactMarkerMinimumRadius * 0.72f, 1f), new Color(1f, 1f, 1f, 0f), 22);
            _impactPocketSecondaryRenderer = CreatePart("ImpactPocketSecondary", GetCircleSprite(), Vector3.zero, new Vector3(impactMarkerMinimumRadius * 0.72f, impactMarkerMinimumRadius * 0.72f, 1f), new Color(1f, 1f, 1f, 0f), 22);
        }

        private void UpdateVisual(Vector2 center, float radius, Color accentColor, float normalizedRelief)
        {
            if (_visualRoot == null)
            {
                return;
            }

            float resolvedRadius = Mathf.Max(minimumPocketRadius, radius);
            float pulse = 0.5f + (0.5f * Mathf.Sin((Time.time * pulseFrequency) + _phaseOffset));
            float reliefIntensity = Mathf.Lerp(0.58f, 1f, normalizedRelief);
            float diameterScale = (resolvedRadius * 2f) / SpriteWorldSize;
            float ellipseScaleY = diameterScale * ellipseRatio;
            bool breakthroughReady = routePlanCarryController != null && routePlanCarryController.IsBreakthroughChainReady;
            bool secureHoldActive = routePlanCarryController != null && routePlanCarryController.IsImpactSecureHoldActive;
            bool secureHoldChainActive = routePlanCarryController != null && routePlanCarryController.IsImpactSecureHoldChainActive;
            Color brightAccent = Color.Lerp(accentColor, Color.white, 0.24f + (normalizedRelief * 0.14f));
            if (breakthroughReady)
            {
                brightAccent = Color.Lerp(brightAccent, Color.white, 0.16f);
            }

            if (secureHoldActive)
            {
                brightAccent = Color.Lerp(brightAccent, Color.white, secureHoldChainActive ? 0.26f : 0.18f);
                reliefIntensity *= secureHoldChainActive ? 1.18f : 1.1f;
            }

            _visualRoot.position = new Vector3(center.x, center.y, 0f);
            _visualRoot.localRotation = Quaternion.identity;

            if (_fillRenderer != null)
            {
                _fillRenderer.transform.localScale = new Vector3(
                    diameterScale * Mathf.Lerp(0.9f, 0.98f, pulse),
                    ellipseScaleY * Mathf.Lerp(0.9f, 0.98f, pulse),
                    1f);
                _fillRenderer.color = new Color(
                    accentColor.r,
                    accentColor.g,
                    accentColor.b,
                    Mathf.Lerp(fillOpacity * 0.45f, fillOpacity, pulse) * reliefIntensity);
            }

            if (_ringRenderer != null)
            {
                _ringRenderer.transform.localScale = new Vector3(
                    diameterScale * Mathf.Lerp(0.98f, 1.08f, pulse),
                    ellipseScaleY * Mathf.Lerp(0.98f, 1.08f, pulse),
                    1f);
                _ringRenderer.color = new Color(
                    brightAccent.r,
                    brightAccent.g,
                    brightAccent.b,
                    Mathf.Lerp(ringOpacity * 0.52f, ringOpacity, pulse) * reliefIntensity);
            }

            if (_coreRenderer != null)
            {
                float coreScale = Mathf.Lerp(0.16f, 0.24f, pulse) * Mathf.Lerp(0.9f, 1.08f, normalizedRelief);
                _coreRenderer.transform.localScale = new Vector3(coreScale, coreScale * ellipseRatio, 1f);
                _coreRenderer.color = new Color(
                    brightAccent.r,
                    brightAccent.g,
                    brightAccent.b,
                    Mathf.Lerp(0.2f, 0.4f, pulse) * reliefIntensity);
            }

            UpdateGuide(_guideLeftRenderer, resolvedRadius, -1f, brightAccent, pulse, reliefIntensity);
            UpdateGuide(_guideRightRenderer, resolvedRadius, 1f, brightAccent, pulse, reliefIntensity);
            UpdateImpactRelief(center, pulse, reliefIntensity, breakthroughReady, secureHoldActive, secureHoldChainActive);
            UpdateTargetLane(center, brightAccent, pulse, reliefIntensity, breakthroughReady, secureHoldActive, secureHoldChainActive);
        }

        private void UpdateGuide(SpriteRenderer renderer, float resolvedRadius, float side, Color color, float pulse, float reliefIntensity)
        {
            if (renderer == null)
            {
                return;
            }

            float lateralOffset = resolvedRadius * 0.58f;
            float guideHeight = Mathf.Max(0.2f, resolvedRadius * 0.34f);
            float guideWidth = Mathf.Max(0.06f, resolvedRadius * 0.08f);
            renderer.transform.localPosition = new Vector3(side * lateralOffset, 0f, 0f);
            renderer.transform.localScale = new Vector3(
                guideWidth * Mathf.Lerp(0.92f, 1.1f, pulse),
                guideHeight * Mathf.Lerp(0.88f, 1.14f, pulse),
                1f);
            renderer.color = new Color(color.r, color.g, color.b, Mathf.Lerp(guideOpacity * 0.46f, guideOpacity, pulse) * reliefIntensity);
        }

        private void UpdateTargetLane(Vector2 center, Color color, float pulse, float reliefIntensity, bool breakthroughReady, bool secureHoldActive, bool secureHoldChainActive)
        {
            Vector2 targetPosition = Vector2.zero;
            bool hasTarget = routePlanCarryController != null
                && routePlanCarryController.IsOpeningActive
                && routePlanCarryController.TryGetOpeningTargetWorldPosition(out targetPosition);

            Vector2 breakthroughLaneOrigin = Vector2.zero;
            Vector2 breakthroughLaneDirection = Vector2.up;
            float breakthroughLaneLength = 0f;
            float breakthroughLaneHalfWidth = 0f;
            float breakthroughLaneNormalized = 0f;
            bool hasBreakthroughLane = breakthroughReady
                && pressureController != null
                && pressureController.TryGetOpeningBreakthroughLaneData(
                    out breakthroughLaneOrigin,
                    out breakthroughLaneDirection,
                    out breakthroughLaneLength,
                    out breakthroughLaneHalfWidth,
                    out _,
                    out breakthroughLaneNormalized);

            Vector2 impactCenter = Vector2.zero;
            float impactRadius = 0f;
            bool hasImpactRelief = pressureController != null
                && (pressureController.TryGetOpeningImpactProtectedPocket(out impactCenter, out impactRadius, out _, out _)
                    || pressureController.TryGetOpeningImpactRelief(out impactCenter, out impactRadius, out _, out _));
            bool hasRecentRoleBurst = routePlanCarryController != null && routePlanCarryController.HasRecentOpeningCadenceHitRoleBurst;
            OpeningCadenceVolleyRole recentRole = hasRecentRoleBurst
                ? routePlanCarryController.RecentOpeningCadenceHitRole
                : OpeningCadenceVolleyRole.None;
            float recentRoleWeight = hasRecentRoleBurst
                ? Mathf.Clamp01(routePlanCarryController.RecentOpeningCadenceHitRoleWeight)
                : 0f;
            bool recentPreferredDriveActive = routePlanCarryController != null && routePlanCarryController.HasRecentPreferredImpactDrive;
            bool recentPreferredHitActive = routePlanCarryController != null && routePlanCarryController.HasRecentPreferredImpactHit;
            bool preferredHoldDriveActive = routePlanCarryController != null && routePlanCarryController.HasPreferredImpactHoldDrive;
            Color laneColor = recentPreferredDriveActive
                ? routePlanCarryController.RecentPreferredImpactDriveAccentColor
                : recentPreferredHitActive
                ? routePlanCarryController.RecentPreferredImpactHitAccentColor
                : preferredHoldDriveActive
                ? routePlanCarryController.PreferredImpactHoldDriveAccentColor
                : hasRecentRoleBurst
                ? routePlanCarryController.RecentOpeningCadenceHitRoleAccentColor
                : color;

            if (_breakthroughBandRenderer != null)
            {
                if (hasBreakthroughLane)
                {
                    float bandLength = Mathf.Max(0.08f, breakthroughLaneLength);
                    float bandWidth = Mathf.Max(targetLaneWidth * 2.2f, breakthroughLaneHalfWidth * 2f);
                    float bandAngle = Mathf.Atan2(breakthroughLaneDirection.y, breakthroughLaneDirection.x) * Mathf.Rad2Deg - 90f;
                    Vector2 localBandCenter = (breakthroughLaneOrigin - center) + (breakthroughLaneDirection * (bandLength * 0.5f));
                    _breakthroughBandRenderer.transform.localRotation = Quaternion.Euler(0f, 0f, bandAngle);
                    _breakthroughBandRenderer.transform.localPosition = new Vector3(localBandCenter.x, localBandCenter.y, 0f);
                    _breakthroughBandRenderer.transform.localScale = new Vector3(
                        bandWidth * Mathf.Lerp(0.96f, 1.08f, pulse),
                        bandLength,
                        1f);
                    float bandAlpha = Mathf.Lerp(breakthroughBandOpacity * 0.72f, breakthroughBandOpacity, pulse)
                        * reliefIntensity
                        * Mathf.Lerp(0.82f, 1f, breakthroughLaneNormalized);
                    _breakthroughBandRenderer.color = new Color(laneColor.r, laneColor.g, laneColor.b, bandAlpha);
                }
                else
                {
                    SetRendererAlpha(_breakthroughBandRenderer, 0f);
                }
            }

            if (!hasTarget)
            {
                SetRendererAlpha(_targetLaneRenderer, 0f);
                SetRendererAlpha(_targetMarkerRenderer, 0f);
                return;
            }

            Vector2 targetOffset = targetPosition - center;
            float laneLength = targetOffset.magnitude;
            if (laneLength <= 0.12f)
            {
                SetRendererAlpha(_targetLaneRenderer, 0f);
                SetRendererAlpha(_targetMarkerRenderer, 0f);
                return;
            }

            Vector2 targetDirection = targetOffset / laneLength;
            float laneVisualLength = Mathf.Max(0.08f, laneLength - (targetMarkerRadius * 0.36f));
            float laneWidth = targetLaneWidth
                * Mathf.Lerp(0.92f, 1.16f, pulse)
                * (breakthroughReady ? breakthroughLaneWidthMultiplier : 1f);
            if (secureHoldActive)
            {
                laneWidth *= secureHoldChainActive ? 1.34f : 1.2f;
            }
            if (preferredHoldDriveActive)
            {
                laneWidth *= secureHoldChainActive ? 1.16f : 1.08f;
            }
            if (recentPreferredDriveActive)
            {
                laneWidth *= secureHoldChainActive ? 1.3f : 1.2f;
            }
            if (recentPreferredHitActive)
            {
                laneWidth *= secureHoldChainActive ? 1.22f : 1.14f;
            }
            if (hasRecentRoleBurst)
            {
                laneWidth *= recentRole switch
                {
                    OpeningCadenceVolleyRole.Core => Mathf.Lerp(1.08f, 1.22f, recentRoleWeight),
                    OpeningCadenceVolleyRole.Flank => Mathf.Lerp(0.94f, 1.08f, recentRoleWeight),
                    OpeningCadenceVolleyRole.Edge => Mathf.Lerp(0.9f, 1.04f, recentRoleWeight),
                    _ => 1f
                };
            }

            float laneAngle = Mathf.Atan2(targetDirection.y, targetDirection.x) * Mathf.Rad2Deg - 90f;
            float resolvedLaneOpacity = Mathf.Clamp01(laneOpacity + (breakthroughReady ? breakthroughLaneOpacityBonus : 0f));
            if (secureHoldActive)
            {
                resolvedLaneOpacity = Mathf.Clamp01(resolvedLaneOpacity + (secureHoldChainActive ? 0.22f : 0.12f));
            }
            if (preferredHoldDriveActive)
            {
                resolvedLaneOpacity = Mathf.Clamp01(resolvedLaneOpacity + (secureHoldChainActive ? 0.12f : 0.08f));
            }
            if (recentPreferredDriveActive)
            {
                resolvedLaneOpacity = Mathf.Clamp01(resolvedLaneOpacity + (secureHoldChainActive ? 0.2f : 0.14f));
            }
            if (recentPreferredHitActive)
            {
                resolvedLaneOpacity = Mathf.Clamp01(resolvedLaneOpacity + (secureHoldChainActive ? 0.16f : 0.12f));
            }
            if (hasRecentRoleBurst)
            {
                resolvedLaneOpacity = Mathf.Clamp01(resolvedLaneOpacity + Mathf.Lerp(0.08f, 0.2f, recentRoleWeight));
            }

            Vector2 laneAnchor = ResolveTargetLaneAnchor(center, targetPosition, impactCenter, hasImpactRelief, secureHoldActive);

            if (_targetLaneRenderer != null)
            {
                _targetLaneRenderer.transform.localRotation = Quaternion.Euler(0f, 0f, laneAngle);
                Vector2 laneCenter = laneAnchor + (targetDirection * (laneVisualLength * 0.5f));
                _targetLaneRenderer.transform.localPosition = new Vector3(laneCenter.x, laneCenter.y, 0f);
                _targetLaneRenderer.transform.localScale = new Vector3(laneWidth, laneVisualLength, 1f);
                _targetLaneRenderer.color = new Color(laneColor.r, laneColor.g, laneColor.b, Mathf.Lerp(resolvedLaneOpacity * 0.42f, resolvedLaneOpacity, pulse) * reliefIntensity);
            }

            if (_targetMarkerRenderer != null)
            {
                _targetMarkerRenderer.transform.localPosition = new Vector3(targetOffset.x, targetOffset.y, 0f);
                float markerScale = targetMarkerRadius
                    * Mathf.Lerp(0.92f, 1.18f, pulse)
                    * (1f + (breakthroughReady ? breakthroughMarkerScaleBonus : 0f));
                if (secureHoldActive)
                {
                    markerScale *= secureHoldChainActive ? 1.24f : 1.12f;
                }
                if (preferredHoldDriveActive)
                {
                    markerScale *= secureHoldChainActive ? 1.14f : 1.08f;
                }
                if (recentPreferredDriveActive)
                {
                    markerScale *= secureHoldChainActive ? 1.24f : 1.14f;
                }
                if (recentPreferredHitActive)
                {
                    markerScale *= secureHoldChainActive ? 1.18f : 1.12f;
                }
                if (hasRecentRoleBurst)
                {
                    markerScale *= recentRole switch
                    {
                        OpeningCadenceVolleyRole.Core => Mathf.Lerp(1.12f, 1.24f, recentRoleWeight),
                        OpeningCadenceVolleyRole.Flank => Mathf.Lerp(1.06f, 1.16f, recentRoleWeight),
                        OpeningCadenceVolleyRole.Edge => Mathf.Lerp(1.04f, 1.12f, recentRoleWeight),
                        _ => 1f
                    };
                }

                _targetMarkerRenderer.transform.localScale = new Vector3(markerScale, markerScale, 1f);
                _targetMarkerRenderer.color = new Color(laneColor.r, laneColor.g, laneColor.b, Mathf.Lerp(0.36f, 0.74f, pulse) * reliefIntensity);
            }

            UpdateRecentRoleBurstGuides(
                laneAnchor,
                targetDirection,
                laneVisualLength,
                laneWidth,
                laneColor,
                pulse,
                reliefIntensity,
                hasRecentRoleBurst,
                recentRole,
                recentRoleWeight,
                preferredHoldDriveActive,
                secureHoldChainActive,
                recentPreferredDriveActive,
                recentPreferredHitActive);
        }

        private Vector2 ResolveTargetLaneAnchor(
            Vector2 pocketCenter,
            Vector2 targetPosition,
            Vector2 impactCenter,
            bool hasImpactRelief,
            bool secureHoldActive)
        {
            if (!secureHoldActive || routePlanCarryController == null)
            {
                return hasImpactRelief
                    ? impactCenter - pocketCenter
                    : Vector2.zero;
            }

            _impactSecureAnchorBuffer.Clear();
            if (!routePlanCarryController.TryGetImpactSecurePulseAnchors(_impactSecureAnchorBuffer, out _, out _)
                || _impactSecureAnchorBuffer.Count == 0)
            {
                return hasImpactRelief
                    ? impactCenter - pocketCenter
                    : Vector2.zero;
            }

            Vector2 bestAnchor = _impactSecureAnchorBuffer[0];
            float nearestDistanceSqr = (bestAnchor - targetPosition).sqrMagnitude;
            for (int index = 1; index < _impactSecureAnchorBuffer.Count; index++)
            {
                Vector2 candidateAnchor = _impactSecureAnchorBuffer[index];
                float candidateDistanceSqr = (candidateAnchor - targetPosition).sqrMagnitude;
                if (candidateDistanceSqr >= nearestDistanceSqr)
                {
                    continue;
                }

                bestAnchor = candidateAnchor;
                nearestDistanceSqr = candidateDistanceSqr;
            }

            return bestAnchor - pocketCenter;
        }

        private void UpdateRecentRoleBurstGuides(
            Vector2 laneAnchor,
            Vector2 targetDirection,
            float laneLength,
            float laneWidth,
            Color color,
            float pulse,
            float reliefIntensity,
            bool active,
            OpeningCadenceVolleyRole recentRole,
            float recentRoleWeight,
            bool preferredHoldDriveActive,
            bool secureHoldChainActive,
            bool recentPreferredDriveActive,
            bool recentPreferredHitActive)
        {
            if (!active)
            {
                SetRendererAlpha(_roleBurstGuidePrimaryRenderer, 0f);
                SetRendererAlpha(_roleBurstGuideSecondaryRenderer, 0f);
                return;
            }

            Vector2 normal = new(-targetDirection.y, targetDirection.x);
            float laneAngle = Mathf.Atan2(targetDirection.y, targetDirection.x) * Mathf.Rad2Deg - 90f;
            float alpha = Mathf.Lerp(0.18f, 0.42f, pulse) * reliefIntensity * Mathf.Lerp(0.62f, 1f, recentRoleWeight);
            float preferredSide = recentPreferredDriveActive && routePlanCarryController != null
                ? Mathf.Sign(routePlanCarryController.RecentPreferredImpactDriveSide)
                : 0f;
            float preferredSideInfluence = recentPreferredDriveActive && routePlanCarryController != null
                ? Mathf.Clamp01(routePlanCarryController.RecentPreferredImpactDriveStrength)
                : 0f;
            bool hasPreferredSide = recentPreferredDriveActive
                ? Mathf.Abs(preferredSide) > 0.5f && preferredSideInfluence > 0.01f
                : routePlanCarryController != null
                && routePlanCarryController.TryGetPreferredSecureAnchorSide(out preferredSide, out preferredSideInfluence);
            if (preferredHoldDriveActive)
            {
                preferredSideInfluence = Mathf.Clamp01(Mathf.Max(preferredSideInfluence, secureHoldChainActive ? 1f : 0.82f));
            }
            if (recentPreferredHitActive)
            {
                preferredSideInfluence = Mathf.Clamp01(Mathf.Max(preferredSideInfluence, secureHoldChainActive ? 1f : 0.88f));
            }
            if (recentPreferredDriveActive)
            {
                preferredSideInfluence = Mathf.Clamp01(Mathf.Max(preferredSideInfluence, secureHoldChainActive ? 1f : 0.92f));
                alpha *= secureHoldChainActive ? 1.18f : 1.1f;
            }

            switch (recentRole)
            {
                case OpeningCadenceVolleyRole.Core:
                    ApplyRoleBurstGuide(_roleBurstGuidePrimaryRenderer, laneAnchor + (targetDirection * (laneLength * 0.34f)), laneAngle, laneWidth * 1.42f, laneLength * 0.54f, color, alpha);
                    ApplyRoleBurstGuide(_roleBurstGuideSecondaryRenderer, laneAnchor + (targetDirection * (laneLength * 0.72f)), laneAngle, laneWidth * 0.88f, laneLength * 0.22f, color, alpha * 0.72f);
                    break;
                case OpeningCadenceVolleyRole.Flank:
                {
                    float lateralOffset = Mathf.Lerp(laneWidth * 2.1f, laneWidth * 3.2f, recentRoleWeight);
                    float flareAngle = Mathf.Lerp(7f, 14f, recentRoleWeight);
                    float primaryBias = hasPreferredSide
                        ? Mathf.Sign(preferredSide) > 0f
                            ? Mathf.Lerp(1f, 1.28f, preferredSideInfluence)
                            : Mathf.Lerp(1f, 0.68f, preferredSideInfluence)
                        : 1f;
                    float secondaryBias = hasPreferredSide
                        ? Mathf.Sign(preferredSide) < 0f
                            ? Mathf.Lerp(1f, 1.28f, preferredSideInfluence)
                            : Mathf.Lerp(1f, 0.68f, preferredSideInfluence)
                        : 1f;
                    ApplyRoleBurstGuide(_roleBurstGuidePrimaryRenderer, laneAnchor + (targetDirection * (laneLength * 0.48f)) + (normal * lateralOffset), laneAngle + flareAngle, laneWidth * 0.88f * primaryBias, laneLength * 0.46f, color, alpha * primaryBias);
                    ApplyRoleBurstGuide(_roleBurstGuideSecondaryRenderer, laneAnchor + (targetDirection * (laneLength * 0.48f)) - (normal * lateralOffset), laneAngle - flareAngle, laneWidth * 0.88f * secondaryBias, laneLength * 0.46f, color, alpha * secondaryBias);
                    break;
                }
                case OpeningCadenceVolleyRole.Edge:
                {
                    float lateralOffset = Mathf.Lerp(laneWidth * 3f, laneWidth * 4.6f, recentRoleWeight);
                    float flareAngle = Mathf.Lerp(14f, 22f, recentRoleWeight);
                    float primaryBias = hasPreferredSide
                        ? Mathf.Sign(preferredSide) > 0f
                            ? Mathf.Lerp(1f, 1.34f, preferredSideInfluence)
                            : Mathf.Lerp(1f, 0.62f, preferredSideInfluence)
                        : 1f;
                    float secondaryBias = hasPreferredSide
                        ? Mathf.Sign(preferredSide) < 0f
                            ? Mathf.Lerp(1f, 1.34f, preferredSideInfluence)
                            : Mathf.Lerp(1f, 0.62f, preferredSideInfluence)
                        : 1f;
                    ApplyRoleBurstGuide(_roleBurstGuidePrimaryRenderer, laneAnchor + (targetDirection * (laneLength * 0.38f)) + (normal * lateralOffset), laneAngle + flareAngle, laneWidth * 0.76f * primaryBias, laneLength * 0.58f, color, alpha * primaryBias);
                    ApplyRoleBurstGuide(_roleBurstGuideSecondaryRenderer, laneAnchor + (targetDirection * (laneLength * 0.38f)) - (normal * lateralOffset), laneAngle - flareAngle, laneWidth * 0.76f * secondaryBias, laneLength * 0.58f, color, alpha * secondaryBias);
                    break;
                }
                default:
                    SetRendererAlpha(_roleBurstGuidePrimaryRenderer, 0f);
                    SetRendererAlpha(_roleBurstGuideSecondaryRenderer, 0f);
                    break;
            }
        }

        private static void ApplyRoleBurstGuide(SpriteRenderer renderer, Vector2 position, float angle, float width, float length, Color color, float alpha)
        {
            if (renderer == null)
            {
                return;
            }

            renderer.transform.localPosition = new Vector3(position.x, position.y, 0f);
            renderer.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
            renderer.transform.localScale = new Vector3(width, Mathf.Max(0.08f, length), 1f);
            renderer.color = new Color(color.r, color.g, color.b, alpha);
        }

        private void UpdateImpactRelief(Vector2 center, float pulse, float reliefIntensity, bool breakthroughReady, bool secureHoldActive, bool secureHoldChainActive)
        {
            Vector2 impactCenter = Vector2.zero;
            float impactRadius = 0f;
            Color impactColor = Color.white;
            float impactNormalized = 0f;
            bool hasImpactRelief = pressureController != null
                && pressureController.TryGetOpeningImpactRelief(out impactCenter, out impactRadius, out impactColor, out impactNormalized);
            bool hasRecentRoleBurst = routePlanCarryController != null && routePlanCarryController.HasRecentOpeningCadenceHitRoleBurst;
            OpeningCadenceVolleyRole recentRole = hasRecentRoleBurst
                ? routePlanCarryController.RecentOpeningCadenceHitRole
                : OpeningCadenceVolleyRole.None;
            float recentRoleWeight = hasRecentRoleBurst
                ? Mathf.Clamp01(routePlanCarryController.RecentOpeningCadenceHitRoleWeight)
                : 0f;
            bool recentPreferredDriveActive = routePlanCarryController != null && routePlanCarryController.HasRecentPreferredImpactDrive;
            bool recentPreferredHitActive = routePlanCarryController != null && routePlanCarryController.HasRecentPreferredImpactHit;
            bool preferredHoldDriveActive = routePlanCarryController != null && routePlanCarryController.HasPreferredImpactHoldDrive;
            if (recentPreferredDriveActive)
            {
                impactColor = routePlanCarryController.RecentPreferredImpactDriveAccentColor;
            }
            else if (recentPreferredHitActive)
            {
                impactColor = routePlanCarryController.RecentPreferredImpactHitAccentColor;
            }
            else if (preferredHoldDriveActive)
            {
                impactColor = routePlanCarryController.PreferredImpactHoldDriveAccentColor;
            }
            else if (hasRecentRoleBurst)
            {
                impactColor = routePlanCarryController.RecentOpeningCadenceHitRoleAccentColor;
            }

            if (!hasImpactRelief)
            {
                SetRendererAlpha(_impactFillRenderer, 0f);
                SetRendererAlpha(_impactRingRenderer, 0f);
                SetRendererAlpha(_impactCoreRenderer, 0f);
                SetRendererAlpha(_impactPocketPrimaryRenderer, 0f);
                SetRendererAlpha(_impactPocketSecondaryRenderer, 0f);
                return;
            }

            Vector2 localImpactOffset = impactCenter - center;
            float resolvedRadius = Mathf.Max(impactMarkerMinimumRadius, impactRadius);
            float diameterScale = (resolvedRadius * 2f) / SpriteWorldSize;
            float impactPulse = Mathf.Lerp(0.9f, 1.16f, pulse);
            float impactIntensity = Mathf.Lerp(0.7f, 1f, impactNormalized) * reliefIntensity;
            if (breakthroughReady)
            {
                impactIntensity *= 1.08f;
            }

            if (secureHoldActive)
            {
                impactPulse = Mathf.Lerp(impactPulse, secureHoldChainActive ? 1.3f : 1.22f, 0.72f);
                impactIntensity *= secureHoldChainActive ? 1.28f : 1.16f;
            }
            if (preferredHoldDriveActive)
            {
                impactPulse = Mathf.Lerp(impactPulse, secureHoldChainActive ? 1.34f : 1.26f, 0.64f);
                impactIntensity *= secureHoldChainActive ? 1.14f : 1.08f;
            }
            if (recentPreferredDriveActive)
            {
                impactPulse = Mathf.Lerp(impactPulse, secureHoldChainActive ? 1.42f : 1.34f, 0.74f);
                impactIntensity *= secureHoldChainActive ? 1.26f : 1.16f;
            }
            if (recentPreferredHitActive)
            {
                impactPulse = Mathf.Lerp(impactPulse, secureHoldChainActive ? 1.38f : 1.3f, 0.72f);
                impactIntensity *= secureHoldChainActive ? 1.2f : 1.12f;
            }

            if (_impactFillRenderer != null)
            {
                _impactFillRenderer.transform.localPosition = new Vector3(localImpactOffset.x, localImpactOffset.y, 0f);
                _impactFillRenderer.transform.localScale = new Vector3(
                    diameterScale * Mathf.Lerp(0.92f, 1.02f, pulse),
                    diameterScale * Mathf.Lerp(0.92f, 1.02f, pulse),
                    1f);
                _impactFillRenderer.color = new Color(
                    impactColor.r,
                    impactColor.g,
                    impactColor.b,
                    Mathf.Lerp(impactFillOpacity * 0.52f, impactFillOpacity, pulse) * impactIntensity);
            }

            if (_impactRingRenderer != null)
            {
                _impactRingRenderer.transform.localPosition = new Vector3(localImpactOffset.x, localImpactOffset.y, 0f);
                _impactRingRenderer.transform.localScale = new Vector3(
                    diameterScale * impactPulse,
                    diameterScale * impactPulse,
                    1f);
                _impactRingRenderer.color = new Color(
                    impactColor.r,
                    impactColor.g,
                    impactColor.b,
                    Mathf.Lerp(impactRingOpacity * 0.46f, impactRingOpacity, pulse) * impactIntensity);
            }

            if (_impactCoreRenderer != null)
            {
                float coreScale = resolvedRadius * Mathf.Lerp(0.3f, 0.46f, pulse);
                if (hasRecentRoleBurst && recentRole == OpeningCadenceVolleyRole.Core)
                {
                    coreScale *= Mathf.Lerp(1.08f, 1.26f, recentRoleWeight);
                }
                _impactCoreRenderer.transform.localPosition = new Vector3(localImpactOffset.x, localImpactOffset.y, 0f);
                _impactCoreRenderer.transform.localScale = new Vector3(coreScale, coreScale, 1f);
                _impactCoreRenderer.color = new Color(
                    impactColor.r,
                    impactColor.g,
                    impactColor.b,
                    Mathf.Lerp(0.28f, 0.56f, pulse) * impactIntensity);
            }

            UpdateRecentRoleImpactPockets(
                localImpactOffset,
                resolvedRadius,
                impactColor,
                pulse,
                impactIntensity,
                hasRecentRoleBurst,
                recentRole,
                recentRoleWeight,
                preferredHoldDriveActive,
                secureHoldChainActive,
                recentPreferredDriveActive,
                recentPreferredHitActive);
        }

        private void UpdateRecentRoleImpactPockets(
            Vector2 localImpactOffset,
            float resolvedRadius,
            Color impactColor,
            float pulse,
            float impactIntensity,
            bool active,
            OpeningCadenceVolleyRole recentRole,
            float recentRoleWeight,
            bool preferredHoldDriveActive,
            bool secureHoldChainActive,
            bool recentPreferredDriveActive,
            bool recentPreferredHitActive)
        {
            if (!active)
            {
                SetRendererAlpha(_impactPocketPrimaryRenderer, 0f);
                SetRendererAlpha(_impactPocketSecondaryRenderer, 0f);
                return;
            }

            Vector2 pocketAxis = Vector2.right;
            if (routePlanCarryController != null
                && routePlanCarryController.TryGetOpeningTargetWorldPosition(out Vector2 targetPosition))
            {
                Vector2 toTarget = targetPosition - (Vector2)_visualRoot.position - localImpactOffset;
                if (toTarget.sqrMagnitude > 0.0001f)
                {
                    Vector2 forward = toTarget.normalized;
                    pocketAxis = new Vector2(-forward.y, forward.x);
                }
            }

            float opacity = Mathf.Lerp(impactPocketOpacity * 0.6f, impactPocketOpacity, pulse)
                * impactIntensity
                * Mathf.Lerp(0.52f, 1f, recentRoleWeight);
            float preferredSide = recentPreferredDriveActive && routePlanCarryController != null
                ? Mathf.Sign(routePlanCarryController.RecentPreferredImpactDriveSide)
                : recentPreferredHitActive && routePlanCarryController != null
                ? Mathf.Sign(routePlanCarryController.RecentPreferredImpactHitSide)
                : 0f;
            float preferredSideInfluence = recentPreferredDriveActive && routePlanCarryController != null
                ? Mathf.Clamp01(routePlanCarryController.RecentPreferredImpactDriveStrength)
                : recentPreferredHitActive && routePlanCarryController != null
                ? Mathf.Clamp01(routePlanCarryController.RecentPreferredImpactHitStrength)
                : 0f;
            bool hasPreferredSide = recentPreferredDriveActive
                ? Mathf.Abs(preferredSide) > 0.5f && preferredSideInfluence > 0.01f
                : recentPreferredHitActive
                ? Mathf.Abs(preferredSide) > 0.5f && preferredSideInfluence > 0.01f
                : routePlanCarryController != null
                    && routePlanCarryController.TryGetPreferredSecureAnchorSide(out preferredSide, out preferredSideInfluence);
            if (preferredHoldDriveActive)
            {
                preferredSideInfluence = Mathf.Clamp01(Mathf.Max(preferredSideInfluence, secureHoldChainActive ? 1f : 0.82f));
                opacity *= secureHoldChainActive ? 1.14f : 1.08f;
            }
            if (recentPreferredDriveActive)
            {
                preferredSideInfluence = Mathf.Clamp01(Mathf.Max(preferredSideInfluence, secureHoldChainActive ? 1f : 0.94f));
                opacity *= secureHoldChainActive ? 1.24f : 1.16f;
            }
            if (recentPreferredHitActive)
            {
                preferredSideInfluence = Mathf.Clamp01(Mathf.Max(preferredSideInfluence, secureHoldChainActive ? 1f : 0.88f));
                opacity *= secureHoldChainActive ? 1.18f : 1.12f;
            }

            switch (recentRole)
            {
                case OpeningCadenceVolleyRole.Core:
                    ApplyImpactPocketRenderer(
                        _impactPocketPrimaryRenderer,
                        localImpactOffset,
                        resolvedRadius * Mathf.Lerp(0.76f, 0.92f, recentRoleWeight),
                        impactColor,
                        opacity * 0.82f);
                    SetRendererAlpha(_impactPocketSecondaryRenderer, 0f);
                    break;

                case OpeningCadenceVolleyRole.Flank:
                {
                    float lateralOffset = resolvedRadius * Mathf.Lerp(0.28f, 0.42f, recentRoleWeight);
                    float pocketRadius = resolvedRadius * Mathf.Lerp(0.28f, 0.34f, recentRoleWeight);
                    float primaryBias = hasPreferredSide
                        ? Mathf.Sign(preferredSide) > 0f
                            ? Mathf.Lerp(1f, 1.28f, preferredSideInfluence)
                            : Mathf.Lerp(1f, 0.68f, preferredSideInfluence)
                        : 1f;
                    float secondaryBias = hasPreferredSide
                        ? Mathf.Sign(preferredSide) < 0f
                            ? Mathf.Lerp(1f, 1.28f, preferredSideInfluence)
                            : Mathf.Lerp(1f, 0.68f, preferredSideInfluence)
                        : 1f;
                    ApplyImpactPocketRenderer(_impactPocketPrimaryRenderer, localImpactOffset + (pocketAxis * lateralOffset), pocketRadius * Mathf.Lerp(0.92f, 1.08f, preferredSideInfluence), impactColor, opacity * primaryBias);
                    ApplyImpactPocketRenderer(_impactPocketSecondaryRenderer, localImpactOffset - (pocketAxis * lateralOffset), pocketRadius * Mathf.Lerp(0.92f, 1.08f, preferredSideInfluence), impactColor, opacity * secondaryBias);
                    break;
                }

                case OpeningCadenceVolleyRole.Edge:
                {
                    float lateralOffset = resolvedRadius * Mathf.Lerp(0.46f, 0.62f, recentRoleWeight);
                    float pocketRadius = resolvedRadius * Mathf.Lerp(0.16f, 0.24f, recentRoleWeight);
                    float primaryBias = hasPreferredSide
                        ? Mathf.Sign(preferredSide) > 0f
                            ? Mathf.Lerp(1f, 1.34f, preferredSideInfluence)
                            : Mathf.Lerp(1f, 0.62f, preferredSideInfluence)
                        : 1f;
                    float secondaryBias = hasPreferredSide
                        ? Mathf.Sign(preferredSide) < 0f
                            ? Mathf.Lerp(1f, 1.34f, preferredSideInfluence)
                            : Mathf.Lerp(1f, 0.62f, preferredSideInfluence)
                        : 1f;
                    ApplyImpactPocketRenderer(_impactPocketPrimaryRenderer, localImpactOffset + (pocketAxis * lateralOffset), pocketRadius * Mathf.Lerp(0.9f, 1.14f, preferredSideInfluence), impactColor, opacity * 0.94f * primaryBias);
                    ApplyImpactPocketRenderer(_impactPocketSecondaryRenderer, localImpactOffset - (pocketAxis * lateralOffset), pocketRadius * Mathf.Lerp(0.9f, 1.14f, preferredSideInfluence), impactColor, opacity * 0.94f * secondaryBias);
                    break;
                }

                default:
                    SetRendererAlpha(_impactPocketPrimaryRenderer, 0f);
                    SetRendererAlpha(_impactPocketSecondaryRenderer, 0f);
                    break;
            }
        }

        private static void ApplyImpactPocketRenderer(SpriteRenderer renderer, Vector2 localPosition, float radius, Color color, float alpha)
        {
            if (renderer == null)
            {
                return;
            }

            float resolvedRadius = Mathf.Max(0.08f, radius);
            float diameterScale = (resolvedRadius * 2f) / SpriteWorldSize;
            renderer.transform.localPosition = new Vector3(localPosition.x, localPosition.y, 0f);
            renderer.transform.localRotation = Quaternion.identity;
            renderer.transform.localScale = new Vector3(diameterScale, diameterScale, 1f);
            renderer.color = new Color(color.r, color.g, color.b, alpha);
        }

        private void SetVisualActive(bool active)
        {
            if (_visualRoot != null)
            {
                _visualRoot.gameObject.SetActive(active);
            }
        }

        private static void SetRendererAlpha(SpriteRenderer renderer, float alpha)
        {
            if (renderer == null)
            {
                return;
            }

            Color color = renderer.color;
            color.a = alpha;
            renderer.color = color;
        }

        private SpriteRenderer CreatePart(string name, Sprite sprite, Vector3 localPosition, Vector3 localScale, Color color, int sortingOrder)
        {
            GameObject child = new(name);
            child.layer = gameObject.layer;
            child.transform.SetParent(_visualRoot, false);
            child.transform.localPosition = localPosition;
            child.transform.localRotation = Quaternion.identity;
            child.transform.localScale = localScale;

            SpriteRenderer renderer = child.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
            return renderer;
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

            const int size = 64;
            Texture2D texture = new(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                name = "RuntimeArenaPressureReliefPocket"
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

            texture.Apply(false, true);
            s_CircleSprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f),
                100f);
            return s_CircleSprite;
        }
    }
}
