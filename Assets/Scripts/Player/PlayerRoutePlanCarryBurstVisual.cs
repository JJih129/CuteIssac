using CuteIssac.Combat;
using UnityEngine;

namespace CuteIssac.Player
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerRoutePlanCarryController))]
    public sealed class PlayerRoutePlanCarryBurstVisual : MonoBehaviour
    {
        [SerializeField] private PlayerRoutePlanCarryController routePlanCarryController;
        [SerializeField] private PlayerVisual playerVisual;
        [SerializeField] private PlayerCombat playerCombat;
        [SerializeField] private Vector3 localOffset = new(0f, -0.12f, 0f);
        [SerializeField] [Min(0.1f)] private float pulseSpeed = 5.8f;
        [SerializeField] [Min(0.1f)] private float bobSpeed = 2.6f;
        [SerializeField] [Min(0f)] private float bobAmplitude = 0.035f;
        [SerializeField] [Min(0f)] private float secureAnchorVisualOffsetScale = 0.22f;
        [SerializeField] [Range(0f, 1f)] private float ringOpacity = 0.32f;
        [SerializeField] [Range(0f, 1f)] private float guideOpacity = 0.68f;

        private static Sprite s_WhiteSprite;
        private static Sprite s_CircleSprite;

        private Transform _root;
        private SpriteRenderer _ringRenderer;
        private SpriteRenderer _coreRenderer;
        private SpriteRenderer _guidePrimaryRenderer;
        private SpriteRenderer _guideSecondaryRenderer;
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
            ResolveReferences();

            if (routePlanCarryController == null
                || !routePlanCarryController.IsOpeningActive
                || !routePlanCarryController.HasRecentOpeningCadenceHitRoleBurst)
            {
                SetVisualActive(false);
                return;
            }

            EnsureVisual();
            UpdateVisual();
        }

        private void ResolveReferences()
        {
            if (routePlanCarryController == null)
            {
                routePlanCarryController = GetComponent<PlayerRoutePlanCarryController>();
            }

            if (playerVisual == null)
            {
                playerVisual = GetComponent<PlayerVisual>();
            }

            if (playerCombat == null)
            {
                playerCombat = GetComponent<PlayerCombat>();
            }
        }

        private void EnsureVisual()
        {
            if (_root != null)
            {
                SetVisualActive(true);
                return;
            }

            GameObject rootObject = new("RoutePlanCarryBurstVisual");
            rootObject.layer = gameObject.layer;
            _root = rootObject.transform;
            _root.SetParent(transform, false);

            _ringRenderer = CreatePart(
                "BurstRing",
                GetCircleSprite(),
                localOffset,
                new Vector3(0.92f, 0.58f, 1f),
                new Color(1f, 1f, 1f, 0f),
                -4);

            _coreRenderer = CreatePart(
                "BurstCore",
                GetCircleSprite(),
                localOffset + new Vector3(0f, 0.38f, 0f),
                new Vector3(0.14f, 0.14f, 1f),
                new Color(1f, 1f, 1f, 0f),
                -2);

            _guidePrimaryRenderer = CreatePart(
                "BurstGuidePrimary",
                GetWhiteSprite(),
                localOffset,
                new Vector3(0.54f, 0.055f, 1f),
                new Color(1f, 1f, 1f, 0f),
                -3);

            _guideSecondaryRenderer = CreatePart(
                "BurstGuideSecondary",
                GetWhiteSprite(),
                localOffset,
                new Vector3(0.54f, 0.048f, 1f),
                new Color(1f, 1f, 1f, 0f),
                -3);
        }

        private void UpdateVisual()
        {
            if (_root == null || routePlanCarryController == null)
            {
                return;
            }

            if (!_root.gameObject.activeSelf)
            {
                _root.gameObject.SetActive(true);
            }

            float pulse = 0.5f + (0.5f * Mathf.Sin((Time.time * pulseSpeed) + _phaseOffset));
            float bob = Mathf.Sin((Time.time * bobSpeed) + _phaseOffset) * bobAmplitude;
            float weight = Mathf.Clamp01(routePlanCarryController.RecentOpeningCadenceHitRoleWeight);
            bool recentPreferredDriveActive = routePlanCarryController.HasRecentPreferredImpactDrive;
            bool recentPreferredHitActive = routePlanCarryController.HasRecentPreferredImpactHit;
            bool preferredHoldDriveActive = routePlanCarryController.HasPreferredImpactHoldDrive;
            Color accentColor = recentPreferredDriveActive
                ? routePlanCarryController.RecentPreferredImpactDriveAccentColor
                : recentPreferredHitActive
                ? routePlanCarryController.RecentPreferredImpactHitAccentColor
                : preferredHoldDriveActive
                ? routePlanCarryController.PreferredImpactHoldDriveAccentColor
                : routePlanCarryController.RecentOpeningCadenceHitRoleAccentColor;
            OpeningCadenceVolleyRole cadenceRole = routePlanCarryController.RecentOpeningCadenceHitRole;
            float preferredSide = recentPreferredDriveActive
                ? Mathf.Sign(routePlanCarryController.RecentPreferredImpactDriveSide)
                : recentPreferredHitActive
                ? Mathf.Sign(routePlanCarryController.RecentPreferredImpactHitSide)
                : 0f;
            float preferredInfluence = recentPreferredDriveActive
                ? Mathf.Clamp01(routePlanCarryController.RecentPreferredImpactDriveStrength)
                : recentPreferredHitActive
                ? Mathf.Clamp01(routePlanCarryController.RecentPreferredImpactHitStrength)
                : 0f;
            bool hasPreferredSide = recentPreferredDriveActive
                ? Mathf.Abs(preferredSide) > 0.5f && preferredInfluence > 0.01f
                : recentPreferredHitActive
                ? Mathf.Abs(preferredSide) > 0.5f && preferredInfluence > 0.01f
                : preferredHoldDriveActive
                    && routePlanCarryController.TryGetPreferredSecureAnchorSide(out preferredSide, out preferredInfluence);
            float preferredGuideBias = hasPreferredSide ? Mathf.Clamp01(preferredInfluence) : 0f;
            bool primaryPreferred = hasPreferredSide && preferredSide > 0f;

            Vector2 secureAnchorOffset = ResolveSecureAnchorOffset();
            _root.localPosition = new Vector3(secureAnchorOffset.x, bob + secureAnchorOffset.y, 0f);

            Vector2 guideDirection = ResolveGuideDirection();
            float guideAngle = Mathf.Atan2(guideDirection.y, guideDirection.x) * Mathf.Rad2Deg;
            Vector2 guideNormal = new(-guideDirection.y, guideDirection.x);

            if (_ringRenderer != null)
            {
                float roleRadius = cadenceRole switch
                {
                    OpeningCadenceVolleyRole.Core => 0.96f,
                    OpeningCadenceVolleyRole.Flank => 1.02f,
                    OpeningCadenceVolleyRole.Edge => 1.08f,
                    _ => 0.92f
                };
                if (recentPreferredDriveActive)
                {
                    roleRadius *= Mathf.Lerp(1f, 1.18f, preferredGuideBias);
                }
                else if (recentPreferredHitActive)
                {
                    roleRadius *= Mathf.Lerp(1f, 1.14f, preferredGuideBias);
                }
                else if (preferredHoldDriveActive)
                {
                    roleRadius *= Mathf.Lerp(1f, 1.08f, preferredGuideBias);
                }
                _ringRenderer.transform.localScale = new Vector3(
                    roleRadius * Mathf.Lerp(0.92f, 1.08f, pulse),
                    roleRadius * 0.62f * Mathf.Lerp(0.9f, 1.04f, pulse),
                    1f);
                _ringRenderer.color = new Color(
                    accentColor.r,
                    accentColor.g,
                    accentColor.b,
                    Mathf.Lerp(ringOpacity * 0.6f, ringOpacity, pulse) * Mathf.Lerp(0.58f, 1f, weight));
            }

            if (_coreRenderer != null)
            {
                _coreRenderer.transform.localPosition = localOffset + new Vector3(guideDirection.x, guideDirection.y, 0f) * 0.34f;
                float coreScale = Mathf.Lerp(0.12f, 0.19f, pulse) * Mathf.Lerp(0.9f, 1.24f, weight);
                if (recentPreferredDriveActive)
                {
                    coreScale *= Mathf.Lerp(1f, 1.28f, preferredGuideBias);
                }
                else if (recentPreferredHitActive)
                {
                    coreScale *= Mathf.Lerp(1f, 1.22f, preferredGuideBias);
                }
                else if (preferredHoldDriveActive)
                {
                    coreScale *= Mathf.Lerp(1f, 1.16f, preferredGuideBias);
                }
                _coreRenderer.transform.localScale = Vector3.one * coreScale;
                _coreRenderer.color = new Color(
                    Mathf.Lerp(accentColor.r, 1f, 0.22f),
                    Mathf.Lerp(accentColor.g, 1f, 0.22f),
                    Mathf.Lerp(accentColor.b, 1f, 0.22f),
                    Mathf.Lerp(0.44f, 0.82f, pulse) * (recentPreferredDriveActive
                        ? Mathf.Lerp(1f, 1.24f, preferredGuideBias)
                        : recentPreferredHitActive
                        ? Mathf.Lerp(1f, 1.18f, preferredGuideBias)
                        : preferredHoldDriveActive
                            ? Mathf.Lerp(1f, 1.12f, preferredGuideBias)
                            : 1f));
            }

            if (_guidePrimaryRenderer != null && _guideSecondaryRenderer != null)
            {
                float primaryGuideScale = hasPreferredSide
                    ? (primaryPreferred ? Mathf.Lerp(1f, 1.26f, preferredGuideBias) : Mathf.Lerp(1f, 0.74f, preferredGuideBias))
                    : 1f;
                float secondaryGuideScale = hasPreferredSide
                    ? (primaryPreferred ? Mathf.Lerp(1f, 0.74f, preferredGuideBias) : Mathf.Lerp(1f, 1.26f, preferredGuideBias))
                    : 1f;
                switch (cadenceRole)
                {
                    case OpeningCadenceVolleyRole.Core:
                        ApplyGuideRenderer(_guidePrimaryRenderer, localOffset + ((Vector3)(guideDirection * 0.2f)), guideAngle, 0.68f, 0.064f, accentColor, pulse, weight, recentPreferredDriveActive ? Mathf.Lerp(1f, 1.26f, preferredGuideBias) : recentPreferredHitActive ? Mathf.Lerp(1f, 1.2f, preferredGuideBias) : preferredHoldDriveActive ? Mathf.Lerp(1f, 1.14f, preferredGuideBias) : 1f);
                        ApplyGuideRenderer(_guideSecondaryRenderer, localOffset + ((Vector3)(guideDirection * 0.44f)), guideAngle, 0.34f, 0.042f, accentColor, pulse * 0.86f, weight * 0.84f, recentPreferredDriveActive ? Mathf.Lerp(1f, 1.18f, preferredGuideBias) : recentPreferredHitActive ? Mathf.Lerp(1f, 1.12f, preferredGuideBias) : preferredHoldDriveActive ? Mathf.Lerp(1f, 1.08f, preferredGuideBias) : 1f);
                        break;
                    case OpeningCadenceVolleyRole.Flank:
                        ApplyGuideRenderer(_guidePrimaryRenderer, localOffset + ((Vector3)(guideDirection * 0.24f)) + ((Vector3)(guideNormal * 0.18f)), guideAngle + 9f, 0.58f, 0.056f, accentColor, pulse, weight, primaryGuideScale);
                        ApplyGuideRenderer(_guideSecondaryRenderer, localOffset + ((Vector3)(guideDirection * 0.24f)) - ((Vector3)(guideNormal * 0.18f)), guideAngle - 9f, 0.58f, 0.056f, accentColor, pulse * 0.92f, weight, secondaryGuideScale);
                        break;
                    case OpeningCadenceVolleyRole.Edge:
                        ApplyGuideRenderer(_guidePrimaryRenderer, localOffset + ((Vector3)(guideDirection * 0.2f)) + ((Vector3)(guideNormal * 0.28f)), guideAngle + 18f, 0.66f, 0.052f, accentColor, pulse, weight, primaryGuideScale);
                        ApplyGuideRenderer(_guideSecondaryRenderer, localOffset + ((Vector3)(guideDirection * 0.2f)) - ((Vector3)(guideNormal * 0.28f)), guideAngle - 18f, 0.66f, 0.052f, accentColor, pulse * 0.9f, weight, secondaryGuideScale);
                        break;
                    default:
                        _guidePrimaryRenderer.enabled = false;
                        _guideSecondaryRenderer.enabled = false;
                        break;
                }
            }
        }

        private Vector2 ResolveGuideDirection()
        {
            if (routePlanCarryController != null
                && routePlanCarryController.TryGetPreferredImpactSecureAnchor(out Vector2 secureAnchor, out _, out _))
            {
                if (routePlanCarryController.TryGetOpeningTargetWorldPosition(out Vector2 preferredTargetPosition))
                {
                    Vector2 anchorToTarget = preferredTargetPosition - secureAnchor;
                    if (anchorToTarget.sqrMagnitude > 0.0001f)
                    {
                        return anchorToTarget.normalized;
                    }
                }

                Vector2 toAnchor = secureAnchor - (Vector2)transform.position;
                if (toAnchor.sqrMagnitude > 0.0001f)
                {
                    return toAnchor.normalized;
                }
            }

            if (routePlanCarryController != null
                && routePlanCarryController.TryGetOpeningTargetWorldPosition(out Vector2 targetPosition))
            {
                Vector2 toTarget = targetPosition - (Vector2)transform.position;
                if (toTarget.sqrMagnitude > 0.0001f)
                {
                    return toTarget.normalized;
                }
            }

            if (playerCombat != null && playerCombat.LastAttackDirection.sqrMagnitude > 0.0001f)
            {
                return playerCombat.LastAttackDirection.normalized;
            }

            return Vector2.up;
        }

        private Vector2 ResolveSecureAnchorOffset()
        {
            if (routePlanCarryController == null
                || !routePlanCarryController.TryGetPreferredImpactSecureAnchor(out Vector2 secureAnchor, out float anchorRadius, out _))
            {
                return Vector2.zero;
            }

            Vector2 toAnchor = secureAnchor - (Vector2)transform.position;
            float distance = toAnchor.magnitude;
            if (distance <= 0.0001f || anchorRadius <= 0.0001f)
            {
                return Vector2.zero;
            }

            float normalizedDistance = Mathf.Clamp01(distance / Mathf.Max(0.01f, anchorRadius));
            Vector2 anchorDirection = toAnchor / distance;
            return anchorDirection * (secureAnchorVisualOffsetScale * normalizedDistance);
        }

        private void ApplyGuideRenderer(SpriteRenderer renderer, Vector3 localPosition, float angle, float length, float thickness, Color accentColor, float pulse, float weight, float emphasisScale = 1f)
        {
            if (renderer == null)
            {
                return;
            }

            float alphaScale = emphasisScale >= 1f
                ? Mathf.Lerp(1f, 1.12f, Mathf.Clamp01((emphasisScale - 1f) / 0.26f))
                : Mathf.Lerp(1f, 0.82f, Mathf.Clamp01((1f - emphasisScale) / 0.26f));
            renderer.enabled = true;
            renderer.transform.localPosition = localPosition;
            renderer.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
            renderer.transform.localScale = new Vector3(
                length * emphasisScale * Mathf.Lerp(0.92f, 1.16f, pulse),
                thickness * emphasisScale * Mathf.Lerp(0.88f, 1.18f, pulse),
                1f);
            renderer.color = new Color(
                accentColor.r,
                accentColor.g,
                accentColor.b,
                Mathf.Lerp(guideOpacity * 0.42f, guideOpacity, pulse) * Mathf.Lerp(0.56f, 1f, weight) * alphaScale);
        }

        private void SetVisualActive(bool active)
        {
            if (_root != null)
            {
                _root.gameObject.SetActive(active);
            }
        }

        private SpriteRenderer CreatePart(string name, Sprite sprite, Vector3 partOffset, Vector3 localScale, Color color, int sortingOrder)
        {
            GameObject child = new(name);
            child.layer = gameObject.layer;
            child.transform.SetParent(_root, false);
            child.transform.localPosition = partOffset;
            child.transform.localScale = localScale;

            SpriteRenderer renderer = child.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = ResolveSortingOrder(sortingOrder);
            return renderer;
        }

        private int ResolveSortingOrder(int fallbackOrder)
        {
            SpriteRenderer bodyRenderer = playerVisual != null ? playerVisual.BodySpriteRenderer : null;
            return bodyRenderer != null
                ? bodyRenderer.sortingOrder + fallbackOrder
                : fallbackOrder;
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
                name = "RuntimeRoutePlanCarryBurstCircle"
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
            s_CircleSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
            return s_CircleSprite;
        }
    }
}
