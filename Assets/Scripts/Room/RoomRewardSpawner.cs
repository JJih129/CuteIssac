using System.Collections.Generic;
using CuteIssac.Core.Audio;
using CuteIssac.Core.Feedback;
using CuteIssac.Core.Gameplay;
using CuteIssac.Core.Pooling;
using CuteIssac.Core.Run;
using CuteIssac.Core.Spawning;
using CuteIssac.Data.Dungeon;
using CuteIssac.Data.Item;
using CuteIssac.Data.Room;
using CuteIssac.Item;
using UnityEngine;

namespace CuteIssac.Room
{
    /// <summary>
    /// Spawns room clear rewards from authored data.
    /// RoomController decides when a room is cleared; this component decides what prefab to drop and where to place it.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RoomRewardSpawner : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Owning room that requests reward drops after the encounter is cleared.")]
        [SerializeField] private RoomController roomController;
        [Tooltip("Data-driven reward table for this room.")]
        [SerializeField] private RoomRewardTable rewardTable;
        [Tooltip("Optional anchor for dropped rewards. Defaults to this transform if left empty.")]
        [SerializeField] private Transform rewardSpawnAnchor;
        [Tooltip("Optional parent for spawned pickup instances.")]
        [SerializeField] private Transform spawnedRewardParent;

        [Header("Reward Rules")]
        [Tooltip("Current room category used to filter reward table entries. Dungeon generation can override this later.")]
        [SerializeField] private RoomType roomTypeForRewards = RoomType.Normal;
        [Tooltip("When enabled, rewards spawn near the last enemy that died in this room before falling back to the anchor.")]
        [SerializeField] private bool preferLastEnemyDeathPosition = true;
        [SerializeField] [Min(0f)] private float scatterRadius = 0.8f;
        [SerializeField] private bool logRewardSpawnsInEditor = true;
        [SerializeField] private SpawnReusePolicy rewardSpawnReusePolicy = SpawnReusePolicy.Pooled;
        [SerializeField] [Min(0)] private int rewardPrewarmBufferCount = 1;

        [Header("Challenge Reveal Layout")]
        [SerializeField] [Min(0.2f)] private float challengeRevealPrimarySpacing = 1.05f;
        [SerializeField] [Min(0.2f)] private float challengeRevealSecondarySpacing = 0.92f;
        [SerializeField] [Min(0f)] private float challengeRevealForwardOffset = 0.82f;
        [SerializeField] [Min(0f)] private float challengeRevealRowOffset = 0.72f;
        [SerializeField] [Min(0f)] private float challengeRevealArcLift = 0.18f;
        [SerializeField] [Range(0f, 1f)] private float challengeRevealEliteSpreadBoost = 0.18f;
        [SerializeField] [Range(0f, 1f)] private float challengeRevealDeadlySpreadBoost = 0.36f;

        private readonly List<RoomRewardEntry> _candidateEntries = new();
        private readonly List<RoomRewardEntry> _selectionPool = new();
        private readonly HashSet<string> _selectedRewardItemIds = new();
        private RoomRewardTable _runtimeDefaultRewardTable;
        private RoomRewardTable _runtimeRewardTableOverride;
        private ItemPoolData _runtimeItemRewardPool;
        private GameObject _runtimeItemRewardPickupPrefab;
        private ChallengeRewardSettings _runtimeChallengeRewardSettings;
        private SecretRoomRewardSettings _runtimeSecretRewardSettings;
        private bool _hasSpawnedRewards;
        private bool _isShuttingDown;
        private RunItemPoolService _runItemPoolService;
        private RoomRewardPhaseSummary _activeRewardPhaseSummary;
        private int _activeRewardLayoutCount;

        public bool HasSpawnedRewards => _hasSpawnedRewards;

        /// <summary>
        /// Generated rooms inject their resolved room type so reward filtering follows the generated content instead of the prefab default.
        /// </summary>
        public void ConfigureRoomType(RoomType roomType)
        {
            roomTypeForRewards = roomType;
        }

        /// <summary>
        /// Generated floor data can provide a floor-specific default reward pool.
        /// Room-type-specific content may still override it later.
        /// </summary>
        public void ConfigureFloorRewardPool(RoomType roomType, RoomRewardTable defaultRewardTable)
        {
            roomTypeForRewards = roomType;
            _runtimeDefaultRewardTable = defaultRewardTable;
            _runtimeRewardTableOverride = null;
            _hasSpawnedRewards = false;
        }

        /// <summary>
        /// Generated room content can override the prefab default reward table without teaching RoomController about reward variants.
        /// </summary>
        public void ConfigureRewardRules(RoomType roomType, RoomRewardTable rewardTableOverride)
        {
            roomTypeForRewards = roomType;
            _runtimeRewardTableOverride = rewardTableOverride;
            _hasSpawnedRewards = false;
        }

        /// <summary>
        /// Some room types add a passive item reward on top of the regular pickup table.
        /// The room still stays prefab-agnostic because both the pool and pickup prefab are injected from data.
        /// </summary>
        public void ConfigureItemRewardPool(RoomType roomType, ItemPoolData itemRewardPool, GameObject itemRewardPickupPrefab)
        {
            roomTypeForRewards = roomType;
            _runtimeItemRewardPool = itemRewardPool;
            _runtimeItemRewardPickupPrefab = itemRewardPickupPrefab;
            _selectedRewardItemIds.Clear();
            _hasSpawnedRewards = false;
        }

        public void ConfigureChallengeRewards(RoomType roomType, ChallengeRewardSettings challengeRewardSettings)
        {
            roomTypeForRewards = roomType;
            _runtimeChallengeRewardSettings = challengeRewardSettings;
            _hasSpawnedRewards = false;
        }

        public void ConfigureSecretRewards(RoomType roomType, SecretRoomRewardSettings secretRoomRewardSettings)
        {
            roomTypeForRewards = roomType;
            _runtimeSecretRewardSettings = secretRoomRewardSettings;
            _hasSpawnedRewards = false;
        }

        public RoomRewardPhaseSummary HandleRoomCleared(RoomController clearedRoom)
        {
            return HandleResolvedRoom(clearedRoom, allowNonCombatRewards: false);
        }

        public RoomRewardPhaseSummary HandleRoomResolvedWithoutCombat(RoomController resolvedRoom)
        {
            return HandleResolvedRoom(resolvedRoom, allowNonCombatRewards: true);
        }

        [ContextMenu("Spawn Debug Rewards")]
        public void SpawnDebugRewards()
        {
            HandleRoomCleared(roomController);
        }

        [ContextMenu("Reset Reward Spawn State")]
        public void ResetRewardSpawnState()
        {
            _hasSpawnedRewards = false;
        }

        public void RestoreRewardSpawnState(bool hadRewardContent)
        {
            _hasSpawnedRewards = hadRewardContent;
            _activeRewardPhaseSummary = default;
            _activeRewardLayoutCount = 0;
        }

        private void Awake()
        {
            if (roomController == null)
            {
                roomController = GetComponent<RoomController>();
            }

            if (rewardSpawnAnchor == null)
            {
                rewardSpawnAnchor = transform;
            }

            if (_runItemPoolService == null)
            {
                _runItemPoolService = FindFirstObjectByType<RunItemPoolService>(FindObjectsInactive.Exclude);
            }
        }

        private RoomRewardTable ResolveRewardTable()
        {
            if (_runtimeRewardTableOverride != null)
            {
                return _runtimeRewardTableOverride;
            }

            if (_runtimeDefaultRewardTable != null)
            {
                return _runtimeDefaultRewardTable;
            }

            return rewardTable;
        }

        private RoomRewardPhaseSummary HandleResolvedRoom(RoomController resolvedRoom, bool allowNonCombatRewards)
        {
            RoomRewardTable resolvedRewardTable = ResolveRewardTable();
            ChallengeClearRank challengeClearRank = ResolveChallengeClearRank(resolvedRoom, allowNonCombatRewards);
            ChallengePressureTier challengePressureTier = ResolveChallengePressureTier(resolvedRoom, allowNonCombatRewards);
            int bonusRewardSelections = ResolveChallengeBonusRewardSelections(challengeClearRank);
            int bonusItemRolls = ResolveChallengeBonusItemRolls(challengeClearRank);
            int pressureBonusRewardSelections = ResolveChallengePressureBonusRewardSelections(challengePressureTier);
            int pressureBonusItemRolls = ResolveChallengePressureBonusItemRolls(challengePressureTier);
            int secretBonusRewardSelections = ResolveSecretBonusRewardSelections(allowNonCombatRewards);
            int secretBonusItemRolls = ResolveSecretBonusItemRolls(allowNonCombatRewards);

            if (_isShuttingDown || _hasSpawnedRewards || resolvedRoom == null || roomController != resolvedRoom)
            {
                return default;
            }

            if (allowNonCombatRewards && !ShouldSpawnRewardsOnNonCombatResolve())
            {
                return default;
            }

            int expectedRewardSpawnCount = EstimateRewardSpawnCount(
                resolvedRewardTable,
                bonusRewardSelections + pressureBonusRewardSelections,
                secretBonusRewardSelections,
                bonusItemRolls + pressureBonusItemRolls,
                secretBonusItemRolls);
            _activeRewardPhaseSummary = new RoomRewardPhaseSummary(
                expectedRewardSpawnCount,
                challengeClearRank,
                challengePressureTier,
                bonusRewardSelections + pressureBonusRewardSelections,
                bonusItemRolls + pressureBonusItemRolls,
                IsChallengeFinale(resolvedRoom, allowNonCombatRewards));
            _activeRewardLayoutCount = expectedRewardSpawnCount;

            int spawnIndex = 0;

            if (resolvedRewardTable != null)
            {
                resolvedRewardTable.CollectCandidates(ResolveRewardFilterRoomType(), _candidateEntries);

                if (_candidateEntries.Count == 0)
                {
                    if (logRewardSpawnsInEditor && IsRewardEligibleRoomType(roomTypeForRewards))
                    {
                        Debug.LogWarning($"RoomRewardSpawner found no reward candidates for room type {roomTypeForRewards}.", this);
                    }
                }
                else
                {
                    _selectionPool.Clear();
                    _selectionPool.AddRange(_candidateEntries);

                    int selectionCount = resolvedRewardTable.AllowDuplicateSelections
                        ? resolvedRewardTable.GetSelectionCount()
                        : Mathf.Min(resolvedRewardTable.GetSelectionCount(), _selectionPool.Count);
                    selectionCount += bonusRewardSelections;
                    selectionCount += pressureBonusRewardSelections;
                    selectionCount += secretBonusRewardSelections;

                    for (int selectionIndex = 0; selectionIndex < selectionCount; selectionIndex++)
                    {
                        if (_selectionPool.Count == 0)
                        {
                            break;
                        }

                        int selectedIndex = SelectWeightedIndex(_selectionPool);

                        if (selectedIndex < 0)
                        {
                            break;
                        }

                        RoomRewardEntry selectedEntry = _selectionPool[selectedIndex];

                        for (int quantityIndex = 0; quantityIndex < selectedEntry.Quantity; quantityIndex++)
                        {
                            SpawnRewardInstance(selectedEntry, spawnIndex);
                            spawnIndex++;
                        }

                        if (!resolvedRewardTable.AllowDuplicateSelections)
                        {
                            _selectionPool.RemoveAt(selectedIndex);
                        }
                    }
                }
            }

            int itemRollCount = ShouldSpawnItemPoolReward()
                ? 1 + bonusItemRolls + pressureBonusItemRolls + secretBonusItemRolls
                : 0;

            for (int itemRollIndex = 0; itemRollIndex < itemRollCount; itemRollIndex++)
            {
                spawnIndex += SpawnItemRewardFromPool(spawnIndex);
            }

            _hasSpawnedRewards = spawnIndex > 0;

            if (_hasSpawnedRewards && logRewardSpawnsInEditor)
            {
                Debug.Log($"RoomRewardSpawner spawned {spawnIndex} reward pickup(s) for room '{resolvedRoom.RoomId}'.", this);
            }

            if (_hasSpawnedRewards)
            {
                GameAudioEvents.Raise(GameAudioEventType.RewardSpawned, ResolveSpawnPosition(0));
            }

            RaiseChallengeRewardFeedback(
                challengeClearRank,
                challengePressureTier,
                bonusRewardSelections + pressureBonusRewardSelections,
                bonusItemRolls + pressureBonusItemRolls);
            RaiseSecretRewardFeedback(secretBonusRewardSelections, secretBonusItemRolls);

            RoomRewardPhaseSummary result = new RoomRewardPhaseSummary(
                spawnIndex,
                challengeClearRank,
                challengePressureTier,
                bonusRewardSelections + pressureBonusRewardSelections,
                bonusItemRolls + pressureBonusItemRolls,
                IsChallengeFinale(resolvedRoom, allowNonCombatRewards));
            _activeRewardPhaseSummary = result;
            _activeRewardLayoutCount = Mathf.Max(_activeRewardLayoutCount, spawnIndex);
            return result;
        }

        private void OnDestroy()
        {
            _isShuttingDown = true;
        }

        private void OnApplicationQuit()
        {
            _isShuttingDown = true;
        }

        private void SpawnRewardInstance(RoomRewardEntry rewardEntry, int spawnIndex)
        {
            GameObject pickupPrefab = rewardEntry.PickupPrefab;

            if (pickupPrefab == null)
            {
                return;
            }

            PrewarmPickupIfNeeded(pickupPrefab, rewardEntry.Quantity);

            Vector3 spawnPosition = ResolveSpawnPosition(spawnIndex);
            Quaternion spawnRotation = Quaternion.identity;
            GameObject spawnedPickup = GameplaySpawnFactory.SpawnGameObject(
                pickupPrefab,
                spawnPosition,
                spawnRotation,
                spawnedRewardParent,
                rewardSpawnReusePolicy);

            if (spawnedPickup == null)
            {
                Debug.LogWarning($"RoomRewardSpawner failed to spawn reward '{pickupPrefab.name}'.", this);
                return;
            }

            ConfigureRewardPickupTracking(spawnedPickup);
        }

        private int SpawnItemRewardFromPool(int spawnIndex)
        {
            if (!ShouldSpawnItemPoolReward())
            {
                return 0;
            }

            ItemPoolSelectionContext selectionContext = _runItemPoolService != null
                ? _runItemPoolService.BuildSelectionContext(roomTypeForRewards, _selectedRewardItemIds)
                : new ItemPoolSelectionContext(roomTypeForRewards, 1, _selectedRewardItemIds, null, null, null, null, null);

            if (!_runtimeItemRewardPool.TrySelectRandomItem(selectionContext, out ItemData selectedItem) || selectedItem == null)
            {
                return 0;
            }

            PrewarmPickupIfNeeded(_runtimeItemRewardPickupPrefab, 1);

            Vector3 spawnPosition = ResolveSpawnPosition(spawnIndex);
            GameObject rewardObject = GameplaySpawnFactory.SpawnGameObject(
                _runtimeItemRewardPickupPrefab,
                spawnPosition,
                Quaternion.identity,
                spawnedRewardParent,
                rewardSpawnReusePolicy);

            if (rewardObject == null)
            {
                return 0;
            }

            ConfigureRewardPickupTracking(rewardObject);

            if (rewardObject.TryGetComponent(out ItemPickupLogic itemPickupLogic))
            {
                itemPickupLogic.ConfigureItem(selectedItem);
                _selectedRewardItemIds.Add(selectedItem.ItemId);
                _runItemPoolService?.RegisterOffer(selectedItem);

                if (roomTypeForRewards == RoomType.Curse && roomController != null)
                {
                    GameplayRuntimeEvents.RaiseCurseRewardManifested(new CurseRewardManifestedSignal(roomController, selectedItem));
                }

                return 1;
            }

            Debug.LogWarning("RoomRewardSpawner item reward prefab is missing ItemPickupLogic.", rewardObject);
            PrefabPoolService.Return(rewardObject);
            return 0;
        }

        private void ConfigureRewardPickupTracking(GameObject rewardObject)
        {
            if (rewardObject == null || roomController == null)
            {
                return;
            }

            BasePickupLogic pickupLogic = rewardObject.GetComponent<BasePickupLogic>();

            if (pickupLogic == null)
            {
                return;
            }

            RoomRewardPickupTracker rewardPickupTracker = rewardObject.GetComponent<RoomRewardPickupTracker>();

            if (rewardPickupTracker == null)
            {
                rewardPickupTracker = rewardObject.AddComponent<RoomRewardPickupTracker>();
            }

            rewardPickupTracker.Configure(roomController, roomTypeForRewards);
        }

        private void PrewarmPickupIfNeeded(GameObject pickupPrefab, int expectedSpawnCount)
        {
            if (rewardSpawnReusePolicy != SpawnReusePolicy.Pooled || pickupPrefab == null)
            {
                return;
            }

            PrefabPoolService.Prewarm(
                pickupPrefab,
                Mathf.Max(1, expectedSpawnCount + rewardPrewarmBufferCount));
        }

        private Vector3 ResolveSpawnPosition(int spawnIndex)
        {
            Vector3 center = rewardSpawnAnchor != null ? rewardSpawnAnchor.position : transform.position;

            if (preferLastEnemyDeathPosition && roomController != null && roomController.TryGetLastEnemyDeathPosition(out Vector3 lastEnemyDeathPosition))
            {
                center = lastEnemyDeathPosition;
            }

            if (ShouldUseChallengeRevealLayout())
            {
                return center + ResolveChallengeRevealOffset(spawnIndex);
            }

            if (scatterRadius <= 0f || spawnIndex <= 0)
            {
                return center;
            }

            float angle = 137.5f * spawnIndex * Mathf.Deg2Rad;
            float radius = scatterRadius * Mathf.Clamp01(0.45f + (0.22f * spawnIndex));
            Vector2 offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            return center + new Vector3(offset.x, offset.y, 0f);
        }

        private bool ShouldUseChallengeRevealLayout()
        {
            return roomTypeForRewards == RoomType.Challenge
                && _activeRewardPhaseSummary.HasChallengeBonusPresentation
                && _activeRewardLayoutCount > 1;
        }

        private Vector3 ResolveChallengeRevealOffset(int spawnIndex)
        {
            if (spawnIndex <= 0)
            {
                return new Vector3(0f, challengeRevealForwardOffset, 0f);
            }

            int columns = _activeRewardLayoutCount >= 6 ? 4 : 3;
            int rowIndex = spawnIndex / columns;
            int columnIndex = spawnIndex % columns;
            float centerColumn = (columns - 1) * 0.5f;
            float normalizedColumn = centerColumn > 0f
                ? (columnIndex - centerColumn) / centerColumn
                : 0f;

            float spreadScale = _activeRewardPhaseSummary.ChallengePressureTier switch
            {
                ChallengePressureTier.Deadly => 1f + challengeRevealDeadlySpreadBoost,
                ChallengePressureTier.Elite => 1f + challengeRevealEliteSpreadBoost,
                _ => 1f
            };

            float rowSpacing = rowIndex == 0 ? challengeRevealPrimarySpacing : challengeRevealSecondarySpacing;
            float horizontalOffset = normalizedColumn * rowSpacing * spreadScale;
            float verticalOffset = challengeRevealForwardOffset - (rowIndex * challengeRevealRowOffset);
            verticalOffset += (1f - Mathf.Abs(normalizedColumn)) * challengeRevealArcLift;

            return new Vector3(horizontalOffset, verticalOffset, 0f);
        }

        private int EstimateRewardSpawnCount(
            RoomRewardTable resolvedRewardTable,
            int bonusRewardSelections,
            int secretBonusRewardSelections,
            int bonusItemRolls,
            int secretBonusItemRolls)
        {
            int estimatedEntryCount = 0;

            if (resolvedRewardTable != null)
            {
                resolvedRewardTable.CollectCandidates(ResolveRewardFilterRoomType(), _candidateEntries);
                if (_candidateEntries.Count > 0)
                {
                    int selectionCount = resolvedRewardTable.GetSelectionCount()
                        + Mathf.Max(0, bonusRewardSelections)
                        + Mathf.Max(0, secretBonusRewardSelections);

                    int averageQuantity = 1;
                    int totalQuantity = 0;
                    for (int i = 0; i < _candidateEntries.Count; i++)
                    {
                        totalQuantity += Mathf.Max(1, _candidateEntries[i].Quantity);
                    }

                    if (_candidateEntries.Count > 0)
                    {
                        averageQuantity = Mathf.Max(1, Mathf.RoundToInt((float)totalQuantity / _candidateEntries.Count));
                    }

                    if (!resolvedRewardTable.AllowDuplicateSelections)
                    {
                        selectionCount = Mathf.Min(selectionCount, _candidateEntries.Count);
                    }

                    estimatedEntryCount = Mathf.Max(0, selectionCount) * averageQuantity;
                }
            }

            int estimatedItemCount = ShouldSpawnItemPoolReward()
                ? 1 + Mathf.Max(0, bonusItemRolls) + Mathf.Max(0, secretBonusItemRolls)
                : 0;

            return estimatedEntryCount + estimatedItemCount;
        }

        private static int SelectWeightedIndex(List<RoomRewardEntry> entries)
        {
            float totalWeight = 0f;

            for (int i = 0; i < entries.Count; i++)
            {
                totalWeight += entries[i].Weight;
            }

            if (totalWeight <= 0f)
            {
                return -1;
            }

            float threshold = Random.value * totalWeight;

            for (int i = 0; i < entries.Count; i++)
            {
                threshold -= entries[i].Weight;

                if (threshold <= 0f)
                {
                    return i;
                }
            }

            return entries.Count - 1;
        }

        private static bool IsRewardEligibleRoomType(RoomType roomType)
        {
            return roomType == RoomType.Normal
                || roomType == RoomType.Challenge
                || roomType == RoomType.MiniBoss
                || roomType == RoomType.Secret
                || roomType == RoomType.Trap
                || roomType == RoomType.Curse;
        }

        private bool ShouldSpawnItemPoolReward()
        {
            return (roomTypeForRewards == RoomType.Boss
                    || roomTypeForRewards == RoomType.Secret
                    || roomTypeForRewards == RoomType.Challenge
                    || roomTypeForRewards == RoomType.Curse)
                && _runtimeItemRewardPool != null
                && _runtimeItemRewardPickupPrefab != null;
        }

        private bool ShouldSpawnRewardsOnNonCombatResolve()
        {
            return roomTypeForRewards == RoomType.Secret
                || roomTypeForRewards == RoomType.Trap
                || roomTypeForRewards == RoomType.Curse;
        }

        private ChallengeClearRank ResolveChallengeClearRank(RoomController resolvedRoom, bool allowNonCombatRewards)
        {
            if (allowNonCombatRewards
                || roomTypeForRewards != RoomType.Challenge
                || resolvedRoom == null
                || _runtimeChallengeRewardSettings == null
                || resolvedRoom.LastCombatDuration <= 0f)
            {
                return ChallengeClearRank.None;
            }

            return _runtimeChallengeRewardSettings.EvaluateRank(resolvedRoom.LastCombatDuration);
        }

        private int ResolveChallengeBonusRewardSelections(ChallengeClearRank challengeClearRank)
        {
            return _runtimeChallengeRewardSettings != null
                ? _runtimeChallengeRewardSettings.GetBonusRewardSelections(challengeClearRank)
                : 0;
        }

        private int ResolveChallengeBonusItemRolls(ChallengeClearRank challengeClearRank)
        {
            return _runtimeChallengeRewardSettings != null
                ? _runtimeChallengeRewardSettings.GetBonusItemRolls(challengeClearRank)
                : 0;
        }

        private ChallengePressureTier ResolveChallengePressureTier(RoomController resolvedRoom, bool allowNonCombatRewards)
        {
            if (allowNonCombatRewards
                || roomTypeForRewards != RoomType.Challenge
                || resolvedRoom == null
                || _runtimeChallengeRewardSettings == null
                || !resolvedRoom.TryGetChallengeRewardPressure(
                    out int totalWaveCount,
                    out int reinforcementEnemyCount,
                    out int guaranteedChampionCount,
                    out float championChanceBonus))
            {
                return ChallengePressureTier.None;
            }

            return _runtimeChallengeRewardSettings.EvaluatePressureTier(
                totalWaveCount,
                reinforcementEnemyCount,
                guaranteedChampionCount,
                championChanceBonus);
        }

        private int ResolveChallengePressureBonusRewardSelections(ChallengePressureTier challengePressureTier)
        {
            return _runtimeChallengeRewardSettings != null
                ? _runtimeChallengeRewardSettings.GetPressureBonusRewardSelections(challengePressureTier)
                : 0;
        }

        private int ResolveChallengePressureBonusItemRolls(ChallengePressureTier challengePressureTier)
        {
            return _runtimeChallengeRewardSettings != null
                ? _runtimeChallengeRewardSettings.GetPressureBonusItemRolls(challengePressureTier)
                : 0;
        }

        private int ResolveSecretBonusRewardSelections(bool allowNonCombatRewards)
        {
            if (!allowNonCombatRewards
                || roomTypeForRewards != RoomType.Secret
                || _runtimeSecretRewardSettings == null)
            {
                return 0;
            }

            return _runtimeSecretRewardSettings.ResolveRewardSelectionCount();
        }

        private int ResolveSecretBonusItemRolls(bool allowNonCombatRewards)
        {
            if (!allowNonCombatRewards
                || roomTypeForRewards != RoomType.Secret
                || _runtimeSecretRewardSettings == null
                || _runtimeItemRewardPool == null
                || _runtimeItemRewardPickupPrefab == null)
            {
                return 0;
            }

            return _runtimeSecretRewardSettings.ResolveItemRollCount();
        }

        private bool IsChallengeFinale(RoomController resolvedRoom, bool allowNonCombatRewards)
        {
            return !allowNonCombatRewards
                && roomTypeForRewards == RoomType.Challenge
                && resolvedRoom != null
                && resolvedRoom.LastCombatDuration > 0f;
        }

        private void RaiseChallengeRewardFeedback(
            ChallengeClearRank challengeClearRank,
            ChallengePressureTier challengePressureTier,
            int bonusRewardSelections,
            int bonusItemRolls)
        {
            if (challengeClearRank == ChallengeClearRank.None && challengePressureTier == ChallengePressureTier.None)
            {
                return;
            }

            string title = challengeClearRank switch
            {
                ChallengeClearRank.S => "도전 평가 S",
                ChallengeClearRank.A => "도전 평가 A",
                ChallengeClearRank.B => "도전 완주",
                _ => "도전 압박 보상"
            };

            string rewardSummary;
            if (bonusRewardSelections > 0 && bonusItemRolls > 0)
            {
                rewardSummary = $"+보상 {bonusRewardSelections} / +아이템 {bonusItemRolls}";
            }
            else if (bonusRewardSelections > 0)
            {
                rewardSummary = $"+보상 {bonusRewardSelections}";
            }
            else if (bonusItemRolls > 0)
            {
                rewardSummary = $"+아이템 {bonusItemRolls}";
            }
            else
            {
                rewardSummary = "기본 보상 획득";
            }

            string pressureSummary = challengePressureTier switch
            {
                ChallengePressureTier.Deadly => "치명 압박 돌파",
                ChallengePressureTier.Elite => "엘리트 압박 돌파",
                ChallengePressureTier.Reinforced => "증원 압박 돌파",
                _ => string.Empty
            };

            string subtitle = string.IsNullOrEmpty(pressureSummary)
                ? rewardSummary
                : $"{rewardSummary} · {pressureSummary}";

            Color accentColor = challengeClearRank switch
            {
                ChallengeClearRank.S => new Color(1f, 0.78f, 0.24f, 1f),
                ChallengeClearRank.A => new Color(0.99f, 0.53f, 0.22f, 1f),
                _ => challengePressureTier switch
                {
                    ChallengePressureTier.Deadly => new Color(1f, 0.42f, 0.18f, 1f),
                    ChallengePressureTier.Elite => new Color(0.96f, 0.5f, 0.2f, 1f),
                    _ => new Color(0.84f, 0.39f, 0.22f, 1f)
                }
            };

            GameplayFeedbackEvents.RaiseBannerFeedback(new BannerFeedbackRequest(
                title,
                subtitle,
                accentColor,
                1.85f));
        }

        private void RaiseSecretRewardFeedback(int bonusRewardSelections, int bonusItemRolls)
        {
            if (roomTypeForRewards != RoomType.Secret || (bonusRewardSelections <= 0 && bonusItemRolls <= 0))
            {
                return;
            }

            string subtitle;
            if (bonusRewardSelections > 0 && bonusItemRolls > 0)
            {
                subtitle = $"+보상 {bonusRewardSelections} / +아이템 {bonusItemRolls}";
            }
            else if (bonusRewardSelections > 0)
            {
                subtitle = $"+보상 {bonusRewardSelections}";
            }
            else
            {
                subtitle = $"+아이템 {bonusItemRolls}";
            }

            GameplayFeedbackEvents.RaiseBannerFeedback(new BannerFeedbackRequest(
                "비밀 은닉처 발견",
                subtitle,
                new Color(0.88f, 0.62f, 1f, 1f),
                1.9f));
        }

        private RoomType ResolveRewardFilterRoomType()
        {
            return roomTypeForRewards switch
            {
                RoomType.MiniBoss => RoomType.Boss,
                RoomType.Trap => RoomType.Normal,
                RoomType.Curse => RoomType.Normal,
                _ => roomTypeForRewards
            };
        }

        private void Reset()
        {
            if (roomController == null)
            {
                roomController = GetComponent<RoomController>();
            }

            if (rewardSpawnAnchor == null)
            {
                rewardSpawnAnchor = transform;
            }
        }

        private void OnDrawGizmosSelected()
        {
            Vector3 center = rewardSpawnAnchor != null ? rewardSpawnAnchor.position : transform.position;
            float radius = Mathf.Max(0.05f, scatterRadius);

            Gizmos.color = new Color(1f, 0.84f, 0.2f, 0.9f);
            Gizmos.DrawSphere(center, 0.08f);

            Gizmos.color = new Color(1f, 0.84f, 0.2f, 0.35f);
            Gizmos.DrawWireSphere(center, radius);
        }
    }
}
