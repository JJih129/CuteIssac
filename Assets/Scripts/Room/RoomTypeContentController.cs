using CuteIssac.Data.Dungeon;
using CuteIssac.Data.Room;
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
        public void ConfigureRoom(RoomType roomType, RoomData roomData)
        {
            _runtimeRoomType = roomType;
            _runtimeRoomData = roomData;
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
            }

            ApplyRoomTint();
        }

        private void HandleRoomEntered(RoomController enteredRoom)
        {
            if (enteredRoom == null || enteredRoom != roomController || _hasSpawnedEntryContent || _resolvedEntry == null)
            {
                return;
            }

            if (!_resolvedEntry.SpawnContentOnFirstEntry)
            {
                return;
            }

            GameObject entryContentPrefab = _resolvedEntry.EntryContentPrefab;

            if (entryContentPrefab == null)
            {
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
