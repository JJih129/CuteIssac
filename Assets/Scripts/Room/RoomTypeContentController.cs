using CuteIssac.Core.Audio;
using CuteIssac.Data.Dungeon;
using CuteIssac.Data.Item;
using CuteIssac.Data.Room;
using CuteIssac.Item;
using UnityEngine;

namespace CuteIssac.Room
{
    /// <summary>
    /// Owns non-combat room content and room-type-driven presentation tweaks.
    /// Treasure, shop, and future room-specific content stay here so RoomController only manages encounter state.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RoomTypeContentController : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Owning room that raises entry/clear state events.")]
        [SerializeField] private RoomController roomController;
        [Tooltip("Optional reward spawner that can receive room-type-specific reward table overrides.")]
        [SerializeField] private RoomRewardSpawner roomRewardSpawner;
        [Tooltip("Optional treasure-only spawner. Keeps treasure-room item spawning outside the generic content path.")]
        [SerializeField] private TreasureRoomSpawner treasureRoomSpawner;
        [Tooltip("Catalog that defines what each RoomType should look and feel like.")]
        [SerializeField] private RoomTypeContentCatalog contentCatalog;
        [Tooltip("Optional spawn anchor for non-combat room content such as treasure pickups or shop placeholders.")]
        [SerializeField] private Transform contentSpawnAnchor;
        [Tooltip("Optional anchor for combat-room setpieces. When empty, the room transform is used.")]
        [SerializeField] private Transform combatSetpieceAnchor;
        [Tooltip("Optional parent used to keep spawned room content grouped under the room hierarchy.")]
        [SerializeField] private Transform spawnedContentParent;

        [Header("Visuals")]
        [Tooltip("Optional sprite renderers tinted by the current room type. Leave empty to skip visual tinting.")]
        [SerializeField] private SpriteRenderer[] roomTintTargets;

        private RoomType _runtimeRoomType = RoomType.Normal;
        private RoomData _runtimeRoomData;
        private RoomTypeContentEntry _resolvedEntry;
        private ItemPoolData _runtimeFloorItemPool;
        private bool _hasSpawnedEntryContent;
        private GameObject _spawnedEntryContent;
        private GameObject _spawnedCombatSetpiece;
        private ShopInventory _spawnedShopInventory;
        private readonly System.Collections.Generic.List<Transform> _preferredTargetBuffer = new();

        private void Awake()
        {
            ResolveReferences();

            if (roomController != null)
            {
                roomController.RoomEntered += HandleRoomEntered;
            }
        }

        private void OnDestroy()
        {
            if (roomController != null)
            {
                roomController.RoomEntered -= HandleRoomEntered;
            }

            ClearCombatSetpiece();
        }

        /// <summary>
        /// Generated dungeon flow injects runtime room metadata here.
        /// This is the single handoff point from data-only generation into room-type-specific world content.
        /// </summary>
        public void ConfigureRoom(RoomType roomType, RoomData roomData, ItemPoolData floorItemPoolOverride = null)
        {
            _runtimeRoomType = roomType;
            _runtimeRoomData = roomData;
            _runtimeFloorItemPool = floorItemPoolOverride;
            _hasSpawnedEntryContent = false;
            _spawnedEntryContent = null;
            _spawnedShopInventory = null;

            if (contentCatalog != null && contentCatalog.TryGetEntry(roomType, out RoomTypeContentEntry entry))
            {
                _resolvedEntry = entry;
            }
            else
            {
                _resolvedEntry = null;
            }

            if (roomRewardSpawner != null)
            {
                roomRewardSpawner.ConfigureRewardRules(roomType, _resolvedEntry != null ? _resolvedEntry.RewardTableOverride : null);
                roomRewardSpawner.ConfigureItemRewardPool(
                    roomType,
                    ResolveItemPoolOverride(),
                    _resolvedEntry != null ? _resolvedEntry.ItemPickupPrefabOverride : null);
            }

            treasureRoomSpawner?.ConfigureRoom(
                roomType,
                roomData,
                ResolveItemPoolOverride(),
                _resolvedEntry != null ? _resolvedEntry.ItemPickupPrefabOverride : null);
            ApplyRoomTint();
            InitializeCombatSetpiece();
        }

        public bool TryResolveContentFocusTarget(out Vector3 focusPosition, out float focusRadius)
        {
            ResolveReferences();

            if (treasureRoomSpawner != null && treasureRoomSpawner.TryResolveTreasureFocusTarget(out focusPosition, out focusRadius))
            {
                return true;
            }

            Transform anchor = contentSpawnAnchor != null ? contentSpawnAnchor : transform;
            focusPosition = anchor.position;
            focusRadius = ResolveContentFocusRadius(_runtimeRoomType);
            return contentSpawnAnchor != null
                || _runtimeRoomType == RoomType.Shop
                || _runtimeRoomType == RoomType.Curse
                || _runtimeRoomType == RoomType.Trap;
        }

        public bool TryResolveCombatSetpieceFocusTarget(out Vector3 focusPosition, out float focusRadius)
        {
            if (_spawnedCombatSetpiece != null)
            {
                focusPosition = _spawnedCombatSetpiece.transform.position;
                focusRadius = ResolveCombatFocusRadius(_runtimeRoomType);
                return true;
            }

            Transform anchor = combatSetpieceAnchor != null ? combatSetpieceAnchor : transform;
            focusPosition = anchor.position;
            focusRadius = ResolveCombatFocusRadius(_runtimeRoomType);
            return combatSetpieceAnchor != null || SupportsCombatSetpiece(_runtimeRoomType);
        }

        public void CollectEntryHighlightTargets(System.Collections.Generic.List<Transform> targetBuffer)
        {
            if (targetBuffer == null)
            {
                return;
            }

            ResolveReferences();

            if (treasureRoomSpawner != null)
            {
                treasureRoomSpawner.CollectTreasureChoiceTargets(targetBuffer);
                if (targetBuffer.Count > 0)
                {
                    return;
                }
            }

            if (_spawnedEntryContent != null)
            {
                if (_spawnedEntryContent.TryGetComponent(out ShopInventory shopInventory))
                {
                    shopInventory.CollectAvailableShopTargets(targetBuffer);
                    if (targetBuffer.Count > 0)
                    {
                        return;
                    }
                }

                targetBuffer.Add(_spawnedEntryContent.transform);
                return;
            }

            if (contentSpawnAnchor != null)
            {
                targetBuffer.Add(contentSpawnAnchor);
            }
        }

        public bool TryGetCombatSetpieceTransform(out Transform setpieceTransform)
        {
            setpieceTransform = _spawnedCombatSetpiece != null ? _spawnedCombatSetpiece.transform : combatSetpieceAnchor;
            return setpieceTransform != null || SupportsCombatSetpiece(_runtimeRoomType);
        }

        public bool TryResolveShopAffordanceTarget(out Transform targetTransform, out bool canPurchase)
        {
            targetTransform = null;
            canPurchase = false;

            if (_runtimeRoomType != RoomType.Shop)
            {
                return false;
            }

            ResolveSpawnedShopInventory();

            if (_spawnedShopInventory == null || _spawnedShopInventory.CurrentHighlightedItem == null)
            {
                return false;
            }

            targetTransform = _spawnedShopInventory.CurrentHighlightedItem.transform;
            canPurchase = _spawnedShopInventory.CurrentHighlightedCanPurchase;
            return targetTransform != null;
        }

        public bool TryResolvePreferredEntryHighlightTarget(string reasonTag, out Transform preferredTarget)
        {
            preferredTarget = null;
            ResolveReferences();

            if (_runtimeRoomType == RoomType.Treasure
                && treasureRoomSpawner != null
                && treasureRoomSpawner.TryResolvePreferredTreasureChoice(reasonTag, out preferredTarget))
            {
                return preferredTarget != null;
            }

            if (_runtimeRoomType == RoomType.Shop)
            {
                ResolveSpawnedShopInventory();
                if (_spawnedShopInventory != null
                    && _spawnedShopInventory.TryResolvePreferredItem(reasonTag, out ShopItem preferredItem)
                    && preferredItem != null)
                {
                    preferredTarget = preferredItem.transform;
                    return true;
                }
            }

            _preferredTargetBuffer.Clear();
            CollectEntryHighlightTargets(_preferredTargetBuffer);
            preferredTarget = _preferredTargetBuffer.Count > 0 ? _preferredTargetBuffer[0] : null;
            return preferredTarget != null;
        }

        public bool TryGetSpawnedShopInventory(out ShopInventory shopInventory)
        {
            ResolveSpawnedShopInventory();
            shopInventory = _spawnedShopInventory;
            return shopInventory != null;
        }

        private void HandleRoomEntered(RoomController enteredRoom)
        {
            if (enteredRoom == null || enteredRoom != roomController || _hasSpawnedEntryContent || _resolvedEntry == null)
            {
                return;
            }

            if (!_resolvedEntry.SpawnContentOnFirstEntry && _runtimeRoomType != RoomType.Curse)
            {
                return;
            }

            if (_runtimeRoomType == RoomType.Treasure && treasureRoomSpawner != null && treasureRoomSpawner.CanHandleRoomType(_runtimeRoomType))
            {
                return;
            }

            GameObject entryContentPrefab = _resolvedEntry.EntryContentPrefab;

            if (entryContentPrefab == null)
            {
                TrySpawnFallbackCurseEntryContent();
                return;
            }

            Transform parent = spawnedContentParent != null ? spawnedContentParent : transform;
            Vector3 spawnPosition = contentSpawnAnchor != null ? contentSpawnAnchor.position : transform.position;
            Quaternion spawnRotation = contentSpawnAnchor != null ? contentSpawnAnchor.rotation : Quaternion.identity;
            GameObject spawnedContent = Instantiate(entryContentPrefab, spawnPosition, spawnRotation, parent);

            if (spawnedContent == null)
            {
                Debug.LogWarning($"RoomTypeContentController failed to instantiate entry content for room type {_runtimeRoomType}.", this);
                return;
            }

            spawnedContent.name = $"{_runtimeRoomType}_{entryContentPrefab.name}";
            _spawnedEntryContent = spawnedContent;
            ResolveSpawnedShopInventory();

            if (_runtimeRoomType == RoomType.Shop
                && ResolveItemPoolOverride() != null
                && spawnedContent.TryGetComponent(out ShopInventory shopInventory))
            {
                shopInventory.ConfigureFromItemPool(ResolveItemPoolOverride());
            }

            if (_runtimeRoomType == RoomType.Shop)
            {
                GameAudioEvents.Raise(GameAudioEventType.ShopEntered, spawnPosition);
            }

            _hasSpawnedEntryContent = true;
        }

        private void TrySpawnFallbackCurseEntryContent()
        {
            if (_runtimeRoomType != RoomType.Curse)
            {
                return;
            }

            Transform parent = spawnedContentParent != null ? spawnedContentParent : transform;
            Vector3 spawnPosition = contentSpawnAnchor != null ? contentSpawnAnchor.position : transform.position;
            Quaternion spawnRotation = contentSpawnAnchor != null ? contentSpawnAnchor.rotation : Quaternion.identity;

            GameObject fallbackContent = new("Curse_FallbackEntryContent");
            fallbackContent.transform.SetParent(parent, false);
            fallbackContent.transform.SetPositionAndRotation(spawnPosition, spawnRotation);
            _spawnedEntryContent = fallbackContent;

            CurseRoomEntryVisual curseRoomEntryVisual = fallbackContent.AddComponent<CurseRoomEntryVisual>();
            Color accentColor = _resolvedEntry != null && _resolvedEntry.ApplyRoomTint
                ? _resolvedEntry.RoomTintColor
                : new Color(0.72f, 0.16f, 0.28f, 1f);
            curseRoomEntryVisual.Configure(roomController, accentColor);
            _hasSpawnedEntryContent = true;
        }

        private void ApplyRoomTint()
        {
            if (_resolvedEntry == null || !_resolvedEntry.ApplyRoomTint)
            {
                return;
            }

            for (int i = 0; i < roomTintTargets.Length; i++)
            {
                SpriteRenderer target = roomTintTargets[i];

                if (target != null)
                {
                    target.color = _resolvedEntry.RoomTintColor;
                }
            }
        }

        private void InitializeCombatSetpiece()
        {
            ClearCombatSetpiece();

            if (!SupportsCombatSetpiece(_runtimeRoomType) || roomController == null)
            {
                return;
            }

            Transform parent = spawnedContentParent != null ? spawnedContentParent : transform;
            Vector3 spawnPosition = combatSetpieceAnchor != null ? combatSetpieceAnchor.position : transform.position;
            Quaternion spawnRotation = combatSetpieceAnchor != null ? combatSetpieceAnchor.rotation : Quaternion.identity;
            GameObject authoredSetpiecePrefab = _resolvedEntry != null ? _resolvedEntry.CombatSetpiecePrefab : null;

            if (authoredSetpiecePrefab != null)
            {
                _spawnedCombatSetpiece = Instantiate(authoredSetpiecePrefab, spawnPosition, spawnRotation, parent);
                _spawnedCombatSetpiece.name = $"{_runtimeRoomType}_{authoredSetpiecePrefab.name}";
            }
            else
            {
                _spawnedCombatSetpiece = new GameObject($"{_runtimeRoomType}_CombatSetpiece");
                _spawnedCombatSetpiece.transform.SetParent(parent, false);
                _spawnedCombatSetpiece.transform.SetPositionAndRotation(spawnPosition, spawnRotation);
            }

            CombatRoomArenaPressureController arenaPressureController = _spawnedCombatSetpiece.GetComponent<CombatRoomArenaPressureController>();

            if (arenaPressureController == null)
            {
                arenaPressureController = _spawnedCombatSetpiece.AddComponent<CombatRoomArenaPressureController>();
            }

            arenaPressureController.Configure(roomController, _runtimeRoomType, ResolveCombatSetpieceAccentColor());
        }

        private void ClearCombatSetpiece()
        {
            if (_spawnedCombatSetpiece == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(_spawnedCombatSetpiece);
            }
            else
            {
                DestroyImmediate(_spawnedCombatSetpiece);
            }

            _spawnedCombatSetpiece = null;
        }

        private Color ResolveCombatSetpieceAccentColor()
        {
            if (_resolvedEntry != null && _resolvedEntry.ApplyRoomTint)
            {
                return _resolvedEntry.RoomTintColor;
            }

            return _runtimeRoomType switch
            {
                RoomType.Boss => new Color(0.95f, 0.32f, 0.38f, 1f),
                RoomType.MiniBoss => new Color(0.98f, 0.56f, 0.24f, 1f),
                RoomType.Challenge => new Color(0.98f, 0.66f, 0.24f, 1f),
                _ => new Color(0.82f, 0.86f, 0.96f, 1f)
            };
        }

        private static bool SupportsCombatSetpiece(RoomType roomType)
        {
            return roomType == RoomType.Boss
                || roomType == RoomType.MiniBoss
                || roomType == RoomType.Challenge;
        }

        private static float ResolveContentFocusRadius(RoomType roomType)
        {
            return roomType switch
            {
                RoomType.Treasure => 1.22f,
                RoomType.Shop => 1.08f,
                RoomType.Curse => 1.1f,
                RoomType.Trap => 1.14f,
                _ => 1f
            };
        }

        private static float ResolveCombatFocusRadius(RoomType roomType)
        {
            return roomType switch
            {
                RoomType.Boss => 1.42f,
                RoomType.MiniBoss => 1.28f,
                RoomType.Challenge => 1.24f,
                _ => 1.12f
            };
        }

        private ItemPoolData ResolveItemPoolOverride()
        {
            if (_runtimeRoomData != null && _runtimeRoomData.TreasureItemPoolOverride != null && _runtimeRoomType == RoomType.Treasure)
            {
                return _runtimeRoomData.TreasureItemPoolOverride;
            }

            if (_runtimeFloorItemPool != null)
            {
                return _runtimeFloorItemPool;
            }

            return _resolvedEntry != null ? _resolvedEntry.ItemPoolOverride : null;
        }

        private void ResolveSpawnedShopInventory()
        {
            if (_spawnedShopInventory != null || _spawnedEntryContent == null)
            {
                return;
            }

            _spawnedShopInventory = _spawnedEntryContent.GetComponent<ShopInventory>();

            if (_spawnedShopInventory == null)
            {
                _spawnedShopInventory = _spawnedEntryContent.GetComponentInChildren<ShopInventory>(true);
            }
        }

        private void ResolveReferences()
        {
            if (roomController == null)
            {
                roomController = GetComponent<RoomController>();
            }

            if (roomRewardSpawner == null)
            {
                roomRewardSpawner = GetComponent<RoomRewardSpawner>();
            }

            if (treasureRoomSpawner == null)
            {
                treasureRoomSpawner = GetComponent<TreasureRoomSpawner>();
            }
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
