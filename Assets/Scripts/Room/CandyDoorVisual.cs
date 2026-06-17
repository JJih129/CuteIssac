using CuteIssac.Data.Visual;
using CuteIssac.Dungeon;
using UnityEngine;

namespace CuteIssac.Room
{
    /// <summary>
    /// Builds a simple candy-themed door visual with separate locked and unlocked states,
    /// then binds those state roots to the parent RoomDoor.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CandyDoorVisual : MonoBehaviour
    {
        [Header("Layout")]
        [SerializeField] private SortingOrderProfile sortingOrderProfile;
        [SerializeField] private Vector2 visualSize = new(2.45f, 2.25f);
        [SerializeField] private float visualSortingOrder = 34f;
        [SerializeField] private Vector2 verticalSpriteVisualSize = new(1.86f, 1.92f);
        [SerializeField] private Vector2 horizontalSpriteVisualSize = new(1.72f, 1.78f);
        [SerializeField] private Vector2 topDoorOffset = new(0f, -0.38f);
        [SerializeField] private Vector2 rightDoorOffset = new(0.6f, 0f);
        [SerializeField] private Vector2 bottomDoorOffset = new(0f, 0.52f);
        [SerializeField] private Vector2 leftDoorOffset = new(-0.6f, 0f);
        [SerializeField] private Vector2 topSpriteLocalOffset = new(0f, 0.18f);
        [SerializeField] private Vector2 rightSpriteLocalOffset = new(0f, 0.2f);
        [SerializeField] private Vector2 bottomSpriteLocalOffset = Vector2.zero;
        [SerializeField] private Vector2 leftSpriteLocalOffset = new(0f, 0.2f);
        [SerializeField] private Vector2 rightOpenSpriteLocalOffset = new(0f, 0.7f);
        [SerializeField] private Vector2 leftOpenSpriteLocalOffset = new(0f, 0.7f);
        [SerializeField] private bool topSpriteFlipX = false;
        [SerializeField] private bool rightSpriteFlipX = false;
        [SerializeField] private bool bottomSpriteFlipX = false;
        [SerializeField] private bool leftSpriteFlipX = false;
        [SerializeField] private bool rightOpenSpriteFlipY = true;
        [SerializeField] private bool leftOpenSpriteFlipY = true;
        [Header("Sprite Orientation")]
        [Tooltip("Closed door sprite rotation offsets applied after the door root is rotated to each wall direction.")]
        [SerializeField] private float closedSpriteUpRotationOffset;
        [SerializeField] private float closedSpriteRightRotationOffset;
        [SerializeField] private float closedSpriteDownRotationOffset;
        [SerializeField] private float closedSpriteLeftRotationOffset;
        [Tooltip("Open door sprite rotation offsets applied after the door root is rotated to each wall direction.")]
        [SerializeField] private float openSpriteUpRotationOffset;
        [SerializeField] private float openSpriteRightRotationOffset;
        [SerializeField] private float openSpriteDownRotationOffset;
        [SerializeField] private float openSpriteLeftRotationOffset;
        [SerializeField] [Min(0.5f)] private float topDirectionScaleMultiplier = 0.96f;
        [SerializeField] [Min(0.5f)] private float rightDirectionScaleMultiplier = 1f;
        [SerializeField] [Min(0.5f)] private float bottomDirectionScaleMultiplier = 1f;
        [SerializeField] [Min(0.5f)] private float leftDirectionScaleMultiplier = 1f;
        [SerializeField] [Min(0.5f)] private float closedSpriteScaleMultiplier = 1f;
        [SerializeField] [Min(0.5f)] private float openSpriteScaleMultiplier = 0.94f;
        [SerializeField] private int topDirectionSortingOffset = 0;
        [SerializeField] private int rightDirectionSortingOffset = -4;
        [SerializeField] private int bottomDirectionSortingOffset = 0;
        [SerializeField] private int leftDirectionSortingOffset = -4;
        [SerializeField] private Color spriteShadowColor = new(0f, 0f, 0f, 0.18f);
        [SerializeField] private Vector2 topShadowLocalOffset = new(0f, -0.04f);
        [SerializeField] private Vector2 rightShadowLocalOffset = new(0.02f, -0.06f);
        [SerializeField] private Vector2 bottomShadowLocalOffset = new(0f, -0.03f);
        [SerializeField] private Vector2 leftShadowLocalOffset = new(-0.02f, -0.06f);
        [SerializeField] [Min(1f)] private float shadowScaleMultiplier = 1.04f;

        [Header("State Motion")]
        [SerializeField] [Min(0.01f)] private float statePopDuration = 0.16f;
        [SerializeField] [Min(1f)] private float statePopScale = 1.08f;
        [SerializeField] [Min(0f)] private float openIdlePulseAmplitude = 0.02f;
        [SerializeField] [Min(0f)] private float openIdlePulseSpeed = 2.1f;

        [Header("Sprite States")]
        [Tooltip("Assigned image used while the room is sealed by enemies.")]
        [SerializeField] private Sprite closedDoorSprite;
        [Tooltip("Optional second image for doors authored as left/right halves.")]
        [SerializeField] private Sprite closedDoorSecondarySprite;
        [Tooltip("Assigned image used when the room is open / cleared.")]
        [SerializeField] private Sprite openDoorSprite;
        [Tooltip("Optional second image for open doors authored as left/right halves.")]
        [SerializeField] private Sprite openDoorSecondarySprite;
        [SerializeField] private Vector2 closedSplitSpriteHalfOffset = new(0.48f, 0f);
        [SerializeField] private Vector2 openSplitSpriteHalfOffset = new(0.48f, 0f);
        [SerializeField] [Range(0.1f, 1f)] private float splitSpriteWidthMultiplier = 0.52f;
        [SerializeField] private Sprite closedDoorSpriteUp;
        [SerializeField] private Sprite closedDoorSpriteRight;
        [SerializeField] private Sprite closedDoorSpriteDown;
        [SerializeField] private Sprite closedDoorSpriteLeft;
        [SerializeField] private Sprite openDoorSpriteUp;
        [SerializeField] private Sprite openDoorSpriteRight;
        [SerializeField] private Sprite openDoorSpriteDown;
        [SerializeField] private Sprite openDoorSpriteLeft;
        [SerializeField] private bool preferSpriteStatesWhenAssigned = true;

        [Header("Palette")]
        [SerializeField] private Color frostingColor = new(0.97f, 0.91f, 0.82f, 1f);
        [SerializeField] private Color candyStripeColor = new(0.86f, 0.67f, 0.92f, 1f);
        [SerializeField] private Color candyStripeSecondaryColor = new(0.98f, 0.73f, 0.84f, 1f);
        [SerializeField] private Color closedDoorColor = new(0.58f, 0.36f, 0.24f, 1f);
        [SerializeField] private Color closedDoorInsetColor = new(0.4f, 0.24f, 0.16f, 1f);
        [SerializeField] private Color openVoidColor = new(0.08f, 0.06f, 0.1f, 0.96f);
        [SerializeField] private Color openGlowColor = new(0.99f, 0.94f, 0.66f, 0.72f);
        [SerializeField] private Color cookieBadgeColor = new(0.9f, 0.77f, 0.54f, 1f);

        private static Sprite s_FallbackSprite;

        private RoomDoor _roomDoor;
        private GameObject _lockedRoot;
        private GameObject _unlockedRoot;
        private SpriteRenderer[] _childRenderers = System.Array.Empty<SpriteRenderer>();
        private bool _wasLockedStateActive;
        private bool _wasUnlockedStateActive;
        private float _lockedStateActivatedAt = float.NegativeInfinity;
        private float _unlockedStateActivatedAt = float.NegativeInfinity;

        private void Awake()
        {
            ResolveRoomDoor();
            EnsureBuilt();
            BindToDoor();
            SyncStateAnimationTracking();
        }

        private void OnEnable()
        {
            ResolveRoomDoor();
            EnsureBuilt();
            BindToDoor();
            SyncStateAnimationTracking();
        }

        private void Update()
        {
            RefreshConnectionVisibility();
            AnimateStateRoots();
        }

        private void OnDestroy()
        {
            if (_roomDoor != null)
            {
                _roomDoor.ClearRuntimeStateObjects();
            }
        }

        private void ResolveRoomDoor()
        {
            if (_roomDoor == null)
            {
                _roomDoor = GetComponentInParent<RoomDoor>();
            }

            _roomDoor?.ValidateDirectionFromTransform(false);
        }

        private void BindToDoor()
        {
            if (_roomDoor == null)
            {
                return;
            }

            _roomDoor.SetRuntimeStateObjects(
                _lockedRoot != null ? new[] { _lockedRoot } : null,
                _unlockedRoot != null ? new[] { _unlockedRoot } : null);
        }

        private void EnsureBuilt()
        {
            _lockedRoot = EnsureStateRoot("LockedState");
            _unlockedRoot = EnsureStateRoot("UnlockedState");

            if (preferSpriteStatesWhenAssigned && HasAnyAssignedSprite())
            {
                BuildSpriteState(_lockedRoot.transform, "ClosedSprite", closedDoorSprite, closedDoorSecondarySprite, closedSplitSpriteHalfOffset);
                BuildSpriteState(_unlockedRoot.transform, "OpenSprite", openDoorSprite, openDoorSecondarySprite, openSplitSpriteHalfOffset);
                DisableFallbackParts(_lockedRoot.transform);
                DisableFallbackParts(_unlockedRoot.transform);
            }
            else
            {
                BuildLockedState(_lockedRoot.transform);
                BuildUnlockedState(_unlockedRoot.transform);
            }

            ApplyOrientationAndOffset();
            CacheChildRenderers();
            RefreshConnectionVisibility();
        }

        private GameObject EnsureStateRoot(string name)
        {
            Transform child = transform.Find(name);

            if (child != null)
            {
                return child.gameObject;
            }

            GameObject root = new(name);
            root.transform.SetParent(transform, false);
            root.transform.localPosition = Vector3.zero;
            root.transform.localRotation = Quaternion.identity;
            root.transform.localScale = Vector3.one;
            return root;
        }

        private void BuildLockedState(Transform root)
        {
            BuildSharedFrame(root, false);
            CreateOrUpdatePart(root, "DoorPanel", Vector2.zero, new Vector2(visualSize.x * 0.52f, visualSize.y * 0.72f), closedDoorColor, 3);
            CreateOrUpdatePart(root, "DoorInset", new Vector2(0f, -0.08f), new Vector2(visualSize.x * 0.4f, visualSize.y * 0.5f), closedDoorInsetColor, 4);
        }

        private void BuildUnlockedState(Transform root)
        {
            BuildSharedFrame(root, true);
            CreateOrUpdatePart(root, "OpenVoid", Vector2.zero, new Vector2(visualSize.x * 0.5f, visualSize.y * 0.78f), openVoidColor, 3);
            CreateOrUpdatePart(root, "OpenGlow", new Vector2(0f, visualSize.y * 0.18f), new Vector2(visualSize.x * 0.42f, visualSize.y * 0.08f), openGlowColor, 4);
        }

        private void BuildSharedFrame(Transform root, bool useAlternateStripe)
        {
            Color stripeColor = useAlternateStripe ? candyStripeSecondaryColor : candyStripeColor;
            CreateOrUpdatePart(root, "LeftPillar", new Vector2(-visualSize.x * 0.36f, -visualSize.y * 0.02f), new Vector2(visualSize.x * 0.14f, visualSize.y * 0.86f), stripeColor, 0);
            CreateOrUpdatePart(root, "RightPillar", new Vector2(visualSize.x * 0.36f, -visualSize.y * 0.02f), new Vector2(visualSize.x * 0.14f, visualSize.y * 0.86f), stripeColor, 0);
            CreateOrUpdatePart(root, "Lintel", new Vector2(0f, visualSize.y * 0.34f), new Vector2(visualSize.x * 0.62f, visualSize.y * 0.12f), frostingColor, 1);
            CreateOrUpdatePart(root, "TopBadge", new Vector2(0f, visualSize.y * 0.48f), new Vector2(visualSize.x * 0.24f, visualSize.y * 0.18f), cookieBadgeColor, 2);
        }

        private void BuildSpriteState(Transform root, string childName, Sprite sprite, Sprite secondarySprite, Vector2 splitHalfOffset)
        {
            bool isOpenSprite = childName == "OpenSprite";
            Sprite resolvedSprite = ResolveDirectionalSprite(sprite, isOpenSprite);
            Sprite resolvedSecondarySprite = ResolveDirectionalSecondarySprite(secondarySprite, isOpenSprite);
            bool useSplitLayout = resolvedSecondarySprite != null;

            BuildSpritePart(root, childName, resolvedSprite, isOpenSprite, useSplitLayout ? -splitHalfOffset : Vector2.zero, useSplitLayout);
            BuildSpritePart(root, $"{childName}Secondary", resolvedSecondarySprite, isOpenSprite, useSplitLayout ? splitHalfOffset : Vector2.zero, useSplitLayout);
        }

        private void BuildSpritePart(Transform root, string childName, Sprite sprite, bool isOpenSprite, Vector2 additionalLocalOffset, bool useSplitLayout)
        {
            Transform child = root.Find(childName);
            Transform shadowChild = root.Find($"{childName}Shadow");

            if (child == null)
            {
                GameObject childObject = new(childName);
                childObject.transform.SetParent(root, false);
                child = childObject.transform;
            }

            if (shadowChild == null)
            {
                GameObject shadowObject = new($"{childName}Shadow");
                shadowObject.transform.SetParent(root, false);
                shadowChild = shadowObject.transform;
            }

            Vector2 spriteLocalOffset = ResolveSpriteLocalOffset(isOpenSprite);
            child.localPosition = new Vector3(spriteLocalOffset.x + additionalLocalOffset.x, spriteLocalOffset.y + additionalLocalOffset.y, 0f);
            child.localRotation = Quaternion.Euler(0f, 0f, ResolveSpriteRotationOffset(isOpenSprite));
            Vector3 spriteScale = ResolveSpriteScale(sprite, isOpenSprite, useSplitLayout);
            child.localScale = spriteScale;

            Vector2 shadowLocalOffset = ResolveShadowLocalOffset();
            shadowChild.localPosition = new Vector3(
                spriteLocalOffset.x + additionalLocalOffset.x + shadowLocalOffset.x,
                spriteLocalOffset.y + additionalLocalOffset.y + shadowLocalOffset.y,
                0f);
            shadowChild.localRotation = child.localRotation;
            shadowChild.localScale = spriteScale * shadowScaleMultiplier;

            SpriteRenderer renderer = child.GetComponent<SpriteRenderer>();
            if (renderer == null)
            {
                renderer = child.gameObject.AddComponent<SpriteRenderer>();
            }

            SpriteRenderer shadowRenderer = shadowChild.GetComponent<SpriteRenderer>();
            if (shadowRenderer == null)
            {
                shadowRenderer = shadowChild.gameObject.AddComponent<SpriteRenderer>();
            }

            renderer.enabled = sprite != null;
            renderer.sprite = sprite;
            renderer.color = Color.white;
            renderer.flipX = ResolveSpriteFlipX();
            renderer.flipY = ResolveSpriteFlipY(isOpenSprite);
            renderer.sortingOrder = ResolveVisualSortingOrder() + 6 + ResolveDirectionSortingOffset();

            shadowRenderer.enabled = sprite != null;
            shadowRenderer.sprite = sprite;
            shadowRenderer.color = spriteShadowColor;
            shadowRenderer.flipX = renderer.flipX;
            shadowRenderer.flipY = renderer.flipY;
            shadowRenderer.sortingOrder = ResolveVisualSortingOrder() + 5 + ResolveDirectionSortingOffset();
        }

        private Vector3 ResolveSpriteScale(Sprite sprite, bool isOpenSprite, bool useSplitLayout)
        {
            if (sprite == null || sprite.bounds.size.x <= 0.0001f || sprite.bounds.size.y <= 0.0001f)
            {
                return Vector3.one;
            }

            Vector2 targetSize = _roomDoor != null && (_roomDoor.DoorDirection == RoomDirection.Left || _roomDoor.DoorDirection == RoomDirection.Right)
                ? horizontalSpriteVisualSize
                : verticalSpriteVisualSize;
            if (useSplitLayout)
            {
                targetSize.x *= splitSpriteWidthMultiplier;
            }

            float fitScale = Mathf.Min(
                targetSize.x / sprite.bounds.size.x,
                targetSize.y / sprite.bounds.size.y);

            fitScale *= isOpenSprite ? openSpriteScaleMultiplier : closedSpriteScaleMultiplier;
            fitScale *= ResolveDirectionScaleMultiplier();

            return new Vector3(fitScale, fitScale, 1f);
        }

        private int ResolveVisualSortingOrder()
        {
            return SortingOrderProfile.ResolveDoorBaseOrder(sortingOrderProfile, Mathf.RoundToInt(visualSortingOrder));
        }

        private Vector2 ResolveSpriteLocalOffset(bool isOpenSprite)
        {
            if (_roomDoor == null)
            {
                return Vector2.zero;
            }

            if (isOpenSprite)
            {
                return _roomDoor.DoorDirection switch
                {
                    RoomDirection.Right => rightOpenSpriteLocalOffset,
                    RoomDirection.Left => leftOpenSpriteLocalOffset,
                    _ => ResolveClosedSpriteLocalOffset()
                };
            }

            return ResolveClosedSpriteLocalOffset();
        }

        private Vector2 ResolveClosedSpriteLocalOffset()
        {
            if (_roomDoor == null)
            {
                return Vector2.zero;
            }

            return _roomDoor.DoorDirection switch
            {
                RoomDirection.Up => topSpriteLocalOffset,
                RoomDirection.Right => rightSpriteLocalOffset,
                RoomDirection.Down => bottomSpriteLocalOffset,
                RoomDirection.Left => leftSpriteLocalOffset,
                _ => Vector2.zero
            };
        }

        private bool HasAnyAssignedSprite()
        {
            return closedDoorSprite != null || closedDoorSecondarySprite != null ||
                   openDoorSprite != null || openDoorSecondarySprite != null ||
                   closedDoorSpriteUp != null || closedDoorSpriteRight != null || closedDoorSpriteDown != null || closedDoorSpriteLeft != null ||
                   openDoorSpriteUp != null || openDoorSpriteRight != null || openDoorSpriteDown != null || openDoorSpriteLeft != null;
        }

        private bool ResolveSpriteFlipX()
        {
            if (_roomDoor == null)
            {
                return false;
            }

            return _roomDoor.DoorDirection switch
            {
                RoomDirection.Up => topSpriteFlipX,
                RoomDirection.Right => rightSpriteFlipX,
                RoomDirection.Down => bottomSpriteFlipX,
                RoomDirection.Left => leftSpriteFlipX,
                _ => false
            };
        }

        private bool ResolveSpriteFlipY(bool isOpenSprite)
        {
            if (_roomDoor == null || !isOpenSprite)
            {
                return false;
            }

            return _roomDoor.DoorDirection switch
            {
                RoomDirection.Right => rightOpenSpriteFlipY,
                RoomDirection.Left => leftOpenSpriteFlipY,
                _ => false
            };
        }

        private Sprite ResolveDirectionalSprite(Sprite fallbackSprite, bool isOpenSprite)
        {
            if (_roomDoor == null)
            {
                return fallbackSprite;
            }

            Sprite directionalSprite = (_roomDoor.DoorDirection, isOpenSprite) switch
            {
                (RoomDirection.Up, false) => closedDoorSpriteUp,
                (RoomDirection.Right, false) => closedDoorSpriteRight,
                (RoomDirection.Down, false) => closedDoorSpriteDown,
                (RoomDirection.Left, false) => closedDoorSpriteLeft,
                (RoomDirection.Up, true) => openDoorSpriteUp,
                (RoomDirection.Right, true) => openDoorSpriteRight,
                (RoomDirection.Down, true) => openDoorSpriteDown,
                (RoomDirection.Left, true) => openDoorSpriteLeft,
                _ => null
            };

            return directionalSprite != null ? directionalSprite : fallbackSprite;
        }

        private Sprite ResolveDirectionalSecondarySprite(Sprite fallbackSprite, bool isOpenSprite)
        {
            // Directional secondary slots are intentionally not exposed yet; current art only needs paired halves.
            return fallbackSprite;
        }

        private float ResolveDirectionScaleMultiplier()
        {
            if (_roomDoor == null)
            {
                return 1f;
            }

            return _roomDoor.DoorDirection switch
            {
                RoomDirection.Up => topDirectionScaleMultiplier,
                RoomDirection.Right => rightDirectionScaleMultiplier,
                RoomDirection.Down => bottomDirectionScaleMultiplier,
                RoomDirection.Left => leftDirectionScaleMultiplier,
                _ => 1f
            };
        }

        private int ResolveDirectionSortingOffset()
        {
            if (_roomDoor == null)
            {
                return 0;
            }

            return _roomDoor.DoorDirection switch
            {
                RoomDirection.Up => topDirectionSortingOffset,
                RoomDirection.Right => rightDirectionSortingOffset,
                RoomDirection.Down => bottomDirectionSortingOffset,
                RoomDirection.Left => leftDirectionSortingOffset,
                _ => 0
            };
        }

        private Vector2 ResolveShadowLocalOffset()
        {
            if (_roomDoor == null)
            {
                return Vector2.zero;
            }

            return _roomDoor.DoorDirection switch
            {
                RoomDirection.Up => topShadowLocalOffset,
                RoomDirection.Right => rightShadowLocalOffset,
                RoomDirection.Down => bottomShadowLocalOffset,
                RoomDirection.Left => leftShadowLocalOffset,
                _ => Vector2.zero
            };
        }

        private float ResolveSpriteRotationOffset(bool isOpenSprite)
        {
            if (_roomDoor == null)
            {
                return 0f;
            }

            return (_roomDoor.DoorDirection, isOpenSprite) switch
            {
                (RoomDirection.Up, false) => closedSpriteUpRotationOffset,
                (RoomDirection.Right, false) => closedSpriteRightRotationOffset,
                (RoomDirection.Down, false) => closedSpriteDownRotationOffset,
                (RoomDirection.Left, false) => closedSpriteLeftRotationOffset,
                (RoomDirection.Up, true) => openSpriteUpRotationOffset,
                (RoomDirection.Right, true) => openSpriteRightRotationOffset,
                (RoomDirection.Down, true) => openSpriteDownRotationOffset,
                (RoomDirection.Left, true) => openSpriteLeftRotationOffset,
                _ => 0f
            };
        }

        private static void DisableFallbackParts(Transform root)
        {
            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);
                if (child.name == "ClosedSprite" || child.name == "ClosedSpriteShadow" ||
                    child.name == "ClosedSpriteSecondary" || child.name == "ClosedSpriteSecondaryShadow" ||
                    child.name == "OpenSprite" || child.name == "OpenSpriteShadow" ||
                    child.name == "OpenSpriteSecondary" || child.name == "OpenSpriteSecondaryShadow")
                {
                    continue;
                }

                child.gameObject.SetActive(false);
            }
        }

        private void CreateOrUpdatePart(Transform parent, string name, Vector2 localPosition, Vector2 localScale, Color color, int sortingOffset)
        {
            Transform child = parent.Find(name);

            if (child == null)
            {
                GameObject childObject = new(name);
                childObject.transform.SetParent(parent, false);
                child = childObject.transform;
            }

            child.localPosition = new Vector3(localPosition.x, localPosition.y, 0f);
            child.localRotation = Quaternion.identity;
            child.localScale = new Vector3(localScale.x, localScale.y, 1f);

            SpriteRenderer renderer = child.GetComponent<SpriteRenderer>();
            if (renderer == null)
            {
                renderer = child.gameObject.AddComponent<SpriteRenderer>();
            }

            renderer.sprite = ResolveVisualSprite();
            renderer.color = color;
            renderer.sortingOrder = ResolveVisualSortingOrder() + sortingOffset;
        }

        private void ApplyOrientationAndOffset()
        {
            if (_roomDoor == null)
            {
                return;
            }

            float rotationZ = _roomDoor.DoorDirection switch
            {
                RoomDirection.Up => 0f,
                RoomDirection.Right => 90f,
                RoomDirection.Down => 180f,
                RoomDirection.Left => 270f,
                _ => 0f
            };

            Vector2 offset = _roomDoor.DoorDirection switch
            {
                RoomDirection.Up => topDoorOffset,
                RoomDirection.Right => rightDoorOffset,
                RoomDirection.Down => bottomDoorOffset,
                RoomDirection.Left => leftDoorOffset,
                _ => Vector2.zero
            };

            transform.localPosition = new Vector3(offset.x, offset.y, 0f);
            transform.localRotation = Quaternion.Euler(0f, 0f, rotationZ);
        }

        private Sprite ResolveVisualSprite()
        {
            SpriteRenderer parentRenderer = _roomDoor != null ? _roomDoor.GetComponent<SpriteRenderer>() : null;
            if (parentRenderer != null && parentRenderer.sprite != null)
            {
                return parentRenderer.sprite;
            }

            if (s_FallbackSprite == null)
            {
                Texture2D texture = new(1, 1, TextureFormat.RGBA32, false)
                {
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp
                };
                texture.SetPixel(0, 0, Color.white);
                texture.Apply();
                s_FallbackSprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
            }

            return s_FallbackSprite;
        }

        private void SyncStateAnimationTracking()
        {
            float settledTime = Time.unscaledTime - statePopDuration;

            _wasLockedStateActive = _lockedRoot != null && _lockedRoot.activeSelf;
            _wasUnlockedStateActive = _unlockedRoot != null && _unlockedRoot.activeSelf;
            _lockedStateActivatedAt = settledTime;
            _unlockedStateActivatedAt = settledTime;

            if (_lockedRoot != null)
            {
                _lockedRoot.transform.localScale = Vector3.one;
            }

            if (_unlockedRoot != null)
            {
                _unlockedRoot.transform.localScale = Vector3.one;
            }
        }

        private void CacheChildRenderers()
        {
            _childRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        }

        private void RefreshConnectionVisibility()
        {
            bool hasConnectedRoom = _roomDoor != null && _roomDoor.ConnectedRoom != null;
            bool shouldShowLocked = hasConnectedRoom && _roomDoor != null && _roomDoor.IsLocked;
            bool shouldShowUnlocked = hasConnectedRoom && !shouldShowLocked;

            if (_lockedRoot != null)
            {
                _lockedRoot.SetActive(shouldShowLocked);
            }

            if (_unlockedRoot != null)
            {
                _unlockedRoot.SetActive(shouldShowUnlocked);
            }

            if (_childRenderers == null || _childRenderers.Length == 0)
            {
                return;
            }

            for (int i = 0; i < _childRenderers.Length; i++)
            {
                SpriteRenderer renderer = _childRenderers[i];
                if (renderer == null)
                {
                    continue;
                }

                renderer.enabled = hasConnectedRoom;
            }
        }

        private void AnimateStateRoots()
        {
            float now = Time.unscaledTime;
            bool lockedActive = _lockedRoot != null && _lockedRoot.activeSelf;
            bool unlockedActive = _unlockedRoot != null && _unlockedRoot.activeSelf;

            if (lockedActive && !_wasLockedStateActive)
            {
                _lockedStateActivatedAt = now;
            }

            if (unlockedActive && !_wasUnlockedStateActive)
            {
                _unlockedStateActivatedAt = now;
            }

            ApplyStateRootScale(
                _lockedRoot != null ? _lockedRoot.transform : null,
                lockedActive,
                _lockedStateActivatedAt,
                false,
                now);

            ApplyStateRootScale(
                _unlockedRoot != null ? _unlockedRoot.transform : null,
                unlockedActive,
                _unlockedStateActivatedAt,
                true,
                now);

            _wasLockedStateActive = lockedActive;
            _wasUnlockedStateActive = unlockedActive;
        }

        private void ApplyStateRootScale(Transform root, bool active, float activatedAt, bool addIdlePulse, float now)
        {
            if (root == null)
            {
                return;
            }

            if (!active)
            {
                root.localScale = Vector3.one;
                return;
            }

            float scale = 1f;

            if (statePopDuration > 0.0001f && !float.IsNegativeInfinity(activatedAt))
            {
                float t = Mathf.Clamp01((now - activatedAt) / statePopDuration);
                scale *= Mathf.Lerp(statePopScale, 1f, EaseOutCubic(t));
            }

            if (addIdlePulse && openIdlePulseAmplitude > 0.0001f && openIdlePulseSpeed > 0.0001f)
            {
                scale *= 1f + Mathf.Sin(now * openIdlePulseSpeed * Mathf.PI * 2f) * openIdlePulseAmplitude;
            }

            root.localScale = Vector3.one * scale;
        }

        private static float EaseOutCubic(float t)
        {
            float inv = 1f - Mathf.Clamp01(t);
            return 1f - (inv * inv * inv);
        }
    }
}
