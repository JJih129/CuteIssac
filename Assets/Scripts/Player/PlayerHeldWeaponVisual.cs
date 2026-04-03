using System.Collections.Generic;
using UnityEngine;

namespace CuteIssac.Player
{
    [DisallowMultipleComponent]
    public sealed class PlayerHeldWeaponVisual : MonoBehaviour
    {
        private const string StarterVisualKey = "starter_sidearm";
        private const string ResourceBasePath = "WeaponVisuals/";

        private static readonly Dictionary<string, Sprite> SpriteCache = new();

        [Header("References")]
        [SerializeField] private PlayerVisual playerVisual;
        [SerializeField] private PlayerCombat playerCombat;
        [SerializeField] private PlayerWeaponLoadout weaponLoadout;
        [SerializeField] private Transform weaponVisualParent;
        [SerializeField] private Transform weaponVisualRoot;
        [SerializeField] private SpriteRenderer weaponSpriteRenderer;

        [Header("Placement")]
        [SerializeField] [Min(0f)] private float handBackOffset = 0.28f;
        [SerializeField] [Min(0f)] private float lateralOffset = 0.035f;
        [SerializeField] [Min(0.1f)] private float worldScale = 0.55f;
        [SerializeField] private int frontSortingOffset = 1;
        [SerializeField] private int backSortingOffset = -1;
        [SerializeField] private Color spriteTint = Color.white;

        private string _currentVisualKey = string.Empty;
        private Vector2 _lastAimDirection = Vector2.right;
        private PlayerWeaponLoadout _subscribedWeaponLoadout;

        private void Awake()
        {
            ResolveReferences();
            EnsureVisualObjects();
            RefreshWeaponSprite(forceRefresh: true);
            RefreshTransform(forceSortOrderRefresh: true);
        }

        private void OnEnable()
        {
            ResolveReferences();
            EnsureVisualObjects();
            Subscribe();
            RefreshWeaponSprite(forceRefresh: true);
            RefreshTransform(forceSortOrderRefresh: true);
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void LateUpdate()
        {
            if (playerVisual == null || playerCombat == null || weaponLoadout == null || weaponVisualRoot == null || weaponSpriteRenderer == null)
            {
                ResolveReferences();
                EnsureVisualObjects();
                Subscribe();
                RefreshWeaponSprite(forceRefresh: false);
            }

            RefreshTransform(forceSortOrderRefresh: false);
        }

        private void HandleWeaponStateChanged()
        {
            RefreshWeaponSprite(forceRefresh: false);
            RefreshTransform(forceSortOrderRefresh: true);
        }

        private void ResolveReferences()
        {
            if (playerVisual == null)
            {
                TryGetComponent(out playerVisual);
            }

            if (playerCombat == null)
            {
                TryGetComponent(out playerCombat);
            }

            if (weaponLoadout == null)
            {
                TryGetComponent(out weaponLoadout);
            }

            if (weaponVisualParent == null)
            {
                weaponVisualParent = playerVisual != null && playerVisual.MuzzleAnchor != null
                    ? playerVisual.MuzzleAnchor.parent
                    : transform;
            }
        }

        private void EnsureVisualObjects()
        {
            if (weaponVisualParent == null)
            {
                return;
            }

            if (weaponVisualRoot == null)
            {
                Transform existingRoot = weaponVisualParent.Find("HeldWeaponVisual");
                if (existingRoot != null)
                {
                    weaponVisualRoot = existingRoot;
                }
                else
                {
                    GameObject rootObject = new("HeldWeaponVisual");
                    weaponVisualRoot = rootObject.transform;
                    weaponVisualRoot.SetParent(weaponVisualParent, false);
                }
            }

            if (weaponSpriteRenderer == null)
            {
                weaponSpriteRenderer = weaponVisualRoot.GetComponent<SpriteRenderer>();
                if (weaponSpriteRenderer == null)
                {
                    weaponSpriteRenderer = weaponVisualRoot.gameObject.AddComponent<SpriteRenderer>();
                }
            }

            weaponSpriteRenderer.color = spriteTint;
            weaponSpriteRenderer.maskInteraction = SpriteMaskInteraction.None;
        }

        private void Subscribe()
        {
            if (ReferenceEquals(_subscribedWeaponLoadout, weaponLoadout))
            {
                return;
            }

            if (_subscribedWeaponLoadout != null)
            {
                _subscribedWeaponLoadout.WeaponStateChanged -= HandleWeaponStateChanged;
            }

            _subscribedWeaponLoadout = weaponLoadout;

            if (_subscribedWeaponLoadout != null)
            {
                _subscribedWeaponLoadout.WeaponStateChanged -= HandleWeaponStateChanged;
                _subscribedWeaponLoadout.WeaponStateChanged += HandleWeaponStateChanged;
            }
        }

        private void Unsubscribe()
        {
            if (_subscribedWeaponLoadout != null)
            {
                _subscribedWeaponLoadout.WeaponStateChanged -= HandleWeaponStateChanged;
                _subscribedWeaponLoadout = null;
            }
        }

        private void RefreshWeaponSprite(bool forceRefresh)
        {
            if (weaponSpriteRenderer == null)
            {
                return;
            }

            string nextVisualKey = weaponLoadout != null
                ? weaponLoadout.CurrentWeaponVisualKey
                : StarterVisualKey;

            if (string.IsNullOrWhiteSpace(nextVisualKey))
            {
                nextVisualKey = StarterVisualKey;
            }

            if (!forceRefresh && nextVisualKey == _currentVisualKey && weaponSpriteRenderer.sprite != null)
            {
                return;
            }

            Sprite nextSprite = LoadSprite(nextVisualKey);
            if (nextSprite == null && nextVisualKey != StarterVisualKey)
            {
                nextVisualKey = StarterVisualKey;
                nextSprite = LoadSprite(nextVisualKey);
            }

            _currentVisualKey = nextVisualKey;
            weaponSpriteRenderer.sprite = nextSprite;
            weaponSpriteRenderer.enabled = nextSprite != null;
        }

        private void RefreshTransform(bool forceSortOrderRefresh)
        {
            if (weaponVisualRoot == null || weaponSpriteRenderer == null || !weaponSpriteRenderer.enabled)
            {
                return;
            }

            Vector2 aimDirection = ResolveAimDirection();
            bool aimChanged = Vector2.Dot(_lastAimDirection, aimDirection) < 0.9995f;
            _lastAimDirection = aimDirection;

            Transform muzzleAnchor = playerVisual != null ? playerVisual.MuzzleAnchor : null;
            Vector3 muzzlePosition = muzzleAnchor != null
                ? muzzleAnchor.position
                : transform.position + (Vector3)(aimDirection * 0.62f);

            Vector2 lateralDirection = new(-aimDirection.y, aimDirection.x);
            Vector3 heldPosition = muzzlePosition
                - (Vector3)(aimDirection * handBackOffset)
                + (Vector3)(lateralDirection * lateralOffset);

            weaponVisualRoot.position = heldPosition;
            weaponVisualRoot.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg);
            weaponVisualRoot.localScale = new Vector3(worldScale, worldScale, 1f);

            if (forceSortOrderRefresh || aimChanged)
            {
                ApplySorting(aimDirection);
            }
        }

        private Vector2 ResolveAimDirection()
        {
            if (playerCombat != null && playerCombat.LastAttackDirection.sqrMagnitude > 0.0001f)
            {
                return playerCombat.LastAttackDirection.normalized;
            }

            return _lastAimDirection.sqrMagnitude > 0.0001f
                ? _lastAimDirection.normalized
                : Vector2.right;
        }

        private void ApplySorting(Vector2 aimDirection)
        {
            SpriteRenderer bodyRenderer = playerVisual != null ? playerVisual.BodySpriteRenderer : null;
            if (bodyRenderer != null)
            {
                weaponSpriteRenderer.sortingLayerID = bodyRenderer.sortingLayerID;
                weaponSpriteRenderer.sortingLayerName = bodyRenderer.sortingLayerName;
                weaponSpriteRenderer.sortingOrder = bodyRenderer.sortingOrder + (aimDirection.y > 0.2f ? backSortingOffset : frontSortingOffset);
                return;
            }

            weaponSpriteRenderer.sortingOrder = aimDirection.y > 0.2f ? backSortingOffset : frontSortingOffset;
        }

        private static Sprite LoadSprite(string visualKey)
        {
            if (string.IsNullOrWhiteSpace(visualKey))
            {
                return null;
            }

            if (SpriteCache.TryGetValue(visualKey, out Sprite cachedSprite))
            {
                return cachedSprite;
            }

            Sprite loadedSprite = Resources.Load<Sprite>(ResourceBasePath + visualKey);
            if (loadedSprite != null)
            {
                SpriteCache[visualKey] = loadedSprite;
            }

            return loadedSprite;
        }
    }
}
