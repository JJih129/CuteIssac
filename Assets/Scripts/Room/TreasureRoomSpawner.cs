using System.Collections.Generic;
using CuteIssac.Core.Run;
using CuteIssac.Core.Spawning;
using CuteIssac.Core.Pooling;
using CuteIssac.Data.Dungeon;
using CuteIssac.Data.Item;
using CuteIssac.Item;
using UnityEngine;

namespace CuteIssac.Room
{
    /// <summary>
    /// Spawns treasure-room pickup content on first entry.
    /// Room type and item selection are injected by generated room data so the spawner stays room-local.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TreasureRoomSpawner : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Owning room that raises the room-entered event.")]
        [SerializeField] private RoomController roomController;
        [Tooltip("Pickup prefab used for treasure rewards. It should contain ItemPickupLogic.")]
        [SerializeField] private GameObject treasurePickupPrefab;
        [Tooltip("Optional anchor used as the spawn point. Falls back to this transform when omitted.")]
        [SerializeField] private Transform contentSpawnAnchor;
        [Tooltip("Optional parent for spawned treasure content so the room hierarchy stays organized.")]
        [SerializeField] private Transform spawnedContentParent;

        [Header("Treasure Choice")]
        [Tooltip("How many treasure choices to present. Isaac-style rooms usually offer 2-3 options.")]
        [SerializeField] [Range(1, 3)] private int choiceCount = 3;
        [Tooltip("Horizontal spacing between spawned treasure choices.")]
        [SerializeField] [Min(0.25f)] private float choiceSpacing = 1.7f;
        [Tooltip("Optional vertical lift applied to outer choices so the line feels less flat.")]
        [SerializeField] [Min(0f)] private float outerChoiceLift = 0.15f;
        [SerializeField] private SpawnReusePolicy spawnReusePolicy = SpawnReusePolicy.Pooled;
        [SerializeField] [Min(0)] private int prewarmBufferCount = 1;

        private RoomType _runtimeRoomType = RoomType.Normal;
        private RoomData _runtimeRoomData;
        private ItemPoolData _runtimeItemPool;
        private GameObject _runtimePickupPrefabOverride;
        private bool _hasSpawnedTreasure;
        private bool _hasResolvedTreasureChoice;
        private readonly HashSet<string> _selectedItemIds = new();
        private readonly List<BasePickupLogic> _spawnedChoices = new();
        private readonly List<ItemData> _choiceItemBuffer = new();
        private RunItemPoolService _runItemPoolService;

        private void Awake()
        {
            ResolveReferences();

            if (roomController != null)
            {
                roomController.RoomEntered += HandleRoomEntered;
            }

            _runItemPoolService = FindFirstObjectByType<RunItemPoolService>(FindObjectsInactive.Exclude);
        }

        private void OnDestroy()
        {
            if (roomController != null)
            {
                roomController.RoomEntered -= HandleRoomEntered;
            }

            for (int i = 0; i < _spawnedChoices.Count; i++)
            {
                if (_spawnedChoices[i] != null)
                {
                    _spawnedChoices[i].Collected -= HandleTreasureCollected;
                }
            }
        }

        /// <summary>
        /// Generated room setup injects room metadata here so the spawner only needs local room data.
        /// </summary>
        public void ConfigureRoom(RoomType roomType, RoomData roomData, ItemPoolData itemPoolOverride = null, GameObject pickupPrefabOverride = null)
        {
            _runtimeRoomType = roomType;
            _runtimeRoomData = roomData;
            _runtimeItemPool = roomData != null && roomData.TreasureItemPoolOverride != null
                ? roomData.TreasureItemPoolOverride
                : itemPoolOverride;
            _runtimePickupPrefabOverride = pickupPrefabOverride;
            _hasSpawnedTreasure = false;
            _hasResolvedTreasureChoice = false;
            _selectedItemIds.Clear();
            _spawnedChoices.Clear();
            _choiceItemBuffer.Clear();
        }

        public bool CanHandleRoomType(RoomType roomType)
        {
            return roomType == RoomType.Treasure;
        }

        public bool TryResolveTreasureFocusTarget(out Vector3 focusPosition, out float focusRadius)
        {
            if (!CanHandleRoomType(_runtimeRoomType))
            {
                focusPosition = transform.position;
                focusRadius = 1f;
                return false;
            }

            Vector3 accumulatedPosition = Vector3.zero;
            int activeChoiceCount = 0;

            for (int i = 0; i < _spawnedChoices.Count; i++)
            {
                BasePickupLogic spawnedChoice = _spawnedChoices[i];

                if (spawnedChoice == null)
                {
                    continue;
                }

                accumulatedPosition += spawnedChoice.transform.position;
                activeChoiceCount++;
            }

            if (activeChoiceCount > 0)
            {
                focusPosition = accumulatedPosition / activeChoiceCount;
                focusRadius = Mathf.Max(1.08f, 0.82f + (activeChoiceCount * 0.18f));
                return true;
            }

            Transform anchor = contentSpawnAnchor != null ? contentSpawnAnchor : transform;
            focusPosition = anchor.position;
            focusRadius = Mathf.Max(1.06f, 0.88f + (Mathf.Clamp(choiceCount, 1, 3) * 0.16f));
            return true;
        }

        public void CollectTreasureChoiceTargets(List<Transform> targetBuffer)
        {
            if (targetBuffer == null)
            {
                return;
            }

            for (int i = 0; i < _spawnedChoices.Count; i++)
            {
                BasePickupLogic spawnedChoice = _spawnedChoices[i];

                if (spawnedChoice == null)
                {
                    continue;
                }

                targetBuffer.Add(spawnedChoice.transform);
            }
        }

        public bool TryResolveClosestTreasureChoice(Vector3 referencePosition, float maxDistance, out Transform closestChoice)
        {
            closestChoice = null;
            float closestDistanceSqr = maxDistance > 0f ? maxDistance * maxDistance : float.PositiveInfinity;

            for (int i = 0; i < _spawnedChoices.Count; i++)
            {
                BasePickupLogic spawnedChoice = _spawnedChoices[i];

                if (spawnedChoice == null || !spawnedChoice.gameObject.activeInHierarchy)
                {
                    continue;
                }

                float distanceSqr = (spawnedChoice.transform.position - referencePosition).sqrMagnitude;

                if (distanceSqr > closestDistanceSqr)
                {
                    continue;
                }

                closestDistanceSqr = distanceSqr;
                closestChoice = spawnedChoice.transform;
            }

            return closestChoice != null;
        }

        public bool TryResolvePreferredTreasureChoice(string reasonTag, out Transform preferredChoice)
        {
            preferredChoice = null;
            float bestScore = float.NegativeInfinity;
            float centerIndex = Mathf.Max(0f, (_spawnedChoices.Count - 1) * 0.5f);

            for (int i = 0; i < _spawnedChoices.Count; i++)
            {
                BasePickupLogic spawnedChoice = _spawnedChoices[i];

                if (spawnedChoice == null || !spawnedChoice.gameObject.activeInHierarchy)
                {
                    continue;
                }

                float centerBias = 1.1f - Mathf.Abs(i - centerIndex);
                float rarityScore = 0f;
                if (spawnedChoice is ItemPickupLogic itemPickupLogic && itemPickupLogic.ItemData != null)
                {
                    rarityScore = ResolveRarityPriority(itemPickupLogic.ItemData.Rarity);
                }

                float score = reasonTag switch
                {
                    "POWER SPIKE" => (rarityScore * 2.4f) + (centerBias * 0.45f),
                    "PRESS ADVANTAGE" => (rarityScore * 2.55f) + (centerBias * 0.42f),
                    "LOADOUT FIND" => (rarityScore * 1.6f) + (centerBias * 0.72f),
                    "KEY WINDOW" => (rarityScore * 2.15f) + (centerBias * 0.58f),
                    "SAFE UPGRADE" => (centerBias * 2.1f) + (rarityScore * 0.82f),
                    "RECOVERY ONLINE" => (centerBias * 1.85f) + (rarityScore * 1.02f),
                    _ => (centerBias * 1.24f) + (rarityScore * 1.12f)
                };

                if (score <= bestScore)
                {
                    continue;
                }

                bestScore = score;
                preferredChoice = spawnedChoice.transform;
            }

            return preferredChoice != null;
        }

        private void HandleRoomEntered(RoomController enteredRoom)
        {
            if (enteredRoom == null || enteredRoom != roomController || _hasSpawnedTreasure)
            {
                return;
            }

            if (!CanHandleRoomType(_runtimeRoomType))
            {
                return;
            }

            GameObject pickupPrefab = _runtimePickupPrefabOverride != null ? _runtimePickupPrefabOverride : treasurePickupPrefab;

            if (pickupPrefab == null)
            {
                Debug.LogWarning("TreasureRoomSpawner requires a pickup prefab reference.", this);
                return;
            }

            Transform parent = spawnedContentParent != null ? spawnedContentParent : transform;
            Transform anchor = contentSpawnAnchor != null ? contentSpawnAnchor : transform;

            if (!BuildTreasureChoices())
            {
                Debug.LogWarning("TreasureRoomSpawner could not resolve any treasure choices from the configured item sources.", this);
                return;
            }

            if (spawnReusePolicy == SpawnReusePolicy.Pooled)
            {
                PrefabPoolService.Prewarm(
                    pickupPrefab,
                    Mathf.Max(1, _choiceItemBuffer.Count + prewarmBufferCount));
            }

            for (int choiceIndex = 0; choiceIndex < _choiceItemBuffer.Count; choiceIndex++)
            {
                Vector3 spawnPosition = ResolveChoiceSpawnPosition(anchor.position, choiceIndex, _choiceItemBuffer.Count);
                GameObject spawnedPickup = GameplaySpawnFactory.SpawnGameObject(
                    pickupPrefab,
                    spawnPosition,
                    anchor.rotation,
                    parent,
                    spawnReusePolicy);

                if (spawnedPickup == null)
                {
                    continue;
                }

                spawnedPickup.name = $"{_runtimeRoomType}_{pickupPrefab.name}_{choiceIndex + 1}";

                if (!spawnedPickup.TryGetComponent(out ItemPickupLogic itemPickupLogic))
                {
                    PrefabPoolService.Return(spawnedPickup);
                    Debug.LogWarning("TreasureRoomSpawner requires the treasure pickup prefab to include ItemPickupLogic.", this);
                    continue;
                }

                ItemData selectedItem = _choiceItemBuffer[choiceIndex];
                itemPickupLogic.ConfigureItem(selectedItem);
                itemPickupLogic.Collected += HandleTreasureCollected;
                _spawnedChoices.Add(itemPickupLogic);
            }

            _hasSpawnedTreasure = _spawnedChoices.Count > 0;
        }

        private bool BuildTreasureChoices()
        {
            _choiceItemBuffer.Clear();
            if (_runtimeRoomData != null && _runtimeRoomData.TreasureItemOverride != null)
            {
                _choiceItemBuffer.Add(_runtimeRoomData.TreasureItemOverride);
                _selectedItemIds.Add(_runtimeRoomData.TreasureItemOverride.ItemId);
            }

            while (_choiceItemBuffer.Count < Mathf.Clamp(choiceCount, 1, 3))
            {
                ItemPoolSelectionContext selectionContext = _runItemPoolService != null
                    ? _runItemPoolService.BuildSelectionContext(RoomType.Treasure, _selectedItemIds)
                    : new ItemPoolSelectionContext(RoomType.Treasure, 1, _selectedItemIds, null, null, null, null, null);

                if (_runtimeItemPool == null || !_runtimeItemPool.TrySelectRandomItem(selectionContext, out ItemData selectedItem) || selectedItem == null)
                {
                    break;
                }

                _choiceItemBuffer.Add(selectedItem);
                _selectedItemIds.Add(selectedItem.ItemId);
            }

            for (int index = 0; index < _choiceItemBuffer.Count; index++)
            {
                _runItemPoolService?.RegisterOffer(_choiceItemBuffer[index]);
            }

            return _choiceItemBuffer.Count >= 1;
        }

        private Vector3 ResolveChoiceSpawnPosition(Vector3 anchorPosition, int choiceIndex, int totalChoices)
        {
            float centerOffset = (totalChoices - 1) * 0.5f;
            float xOffset = (choiceIndex - centerOffset) * choiceSpacing;
            float yOffset = totalChoices > 2 && choiceIndex != 1 ? outerChoiceLift : 0f;
            return anchorPosition + new Vector3(xOffset, yOffset, 0f);
        }

        private void HandleTreasureCollected(BasePickupLogic collectedPickup)
        {
            if (_hasResolvedTreasureChoice)
            {
                return;
            }

            _hasResolvedTreasureChoice = true;

            for (int i = 0; i < _spawnedChoices.Count; i++)
            {
                BasePickupLogic spawnedChoice = _spawnedChoices[i];

                if (spawnedChoice == null)
                {
                    continue;
                }

                spawnedChoice.Collected -= HandleTreasureCollected;

                if (spawnedChoice != collectedPickup)
                {
                    spawnedChoice.ReleasePickup(0f);
                }
            }
        }

        private void ResolveReferences()
        {
            if (roomController == null)
            {
                roomController = GetComponent<RoomController>();
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

        private static float ResolveRarityPriority(ItemRarity rarity)
        {
            return rarity switch
            {
                ItemRarity.Uncommon => 1f,
                ItemRarity.Rare => 2.2f,
                ItemRarity.Legendary => 3.4f,
                ItemRarity.Relic => 4.1f,
                ItemRarity.Boss => 4.8f,
                _ => 0.3f
            };
        }
    }
}
