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
