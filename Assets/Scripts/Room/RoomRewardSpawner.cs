using System.Collections.Generic;
using CuteIssac.Core.Audio;
using CuteIssac.Core.Feedback;
using CuteIssac.Core.Gameplay;
using CuteIssac.Core.Pooling;
using CuteIssac.Core.Run;
using CuteIssac.Core.Scene;
using CuteIssac.Core.Spawning;
using CuteIssac.Data.Dungeon;
using CuteIssac.Data.Item;
using CuteIssac.Data.Room;
using CuteIssac.Item;
using CuteIssac.Player;
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
        [Tooltip("Scene-authored reference hub. If empty, the active GameplaySceneContext is used before fallback scene search.")]
        [SerializeField] private GameplaySceneContext sceneContext;
        [Tooltip("Owning room that requests reward drops after the encounter is cleared.")]
        [SerializeField] private RoomController roomController;
        [Tooltip("Data-driven reward table for this room.")]
        [SerializeField] private RoomRewardTable rewardTable;
        [Tooltip("Optional anchor for dropped rewards. Defaults to this transform if left empty.")]
        [SerializeField] private Transform rewardSpawnAnchor;
        [Tooltip("Optional parent for spawned pickup instances.")]
        [SerializeField] private Transform spawnedRewardParent;
        [Tooltip("Optional room-level combat bonus tracker that can add extra clear rewards.")]
        [SerializeField] private RoomMomentumRewardController momentumRewardController;
        [Tooltip("Optional player item manager that can inject passive clear reward bonuses before rewards spawn.")]
        [SerializeField] private PlayerItemManager playerItemManager;
        [Tooltip("Optional player inventory used for Isaac-style key economy assist.")]
        [SerializeField] private PlayerInventory playerInventory;
        [Tooltip("Optional player health used for Isaac-style adaptive room clear drops.")]
        [SerializeField] private PlayerHealth playerHealth;

        [Header("Reward Rules")]
        [Tooltip("Current room category used to filter reward table entries. Dungeon generation can override this later.")]
        [SerializeField] private RoomType roomTypeForRewards = RoomType.Normal;
        [Tooltip("When enabled, rewards spawn near the last enemy that died in this room before falling back to the anchor.")]
        [SerializeField] private bool preferLastEnemyDeathPosition = true;
        [SerializeField] [Min(0f)] private float scatterRadius = 0.8f;
        [SerializeField] private bool logRewardSpawnsInEditor = true;
        [SerializeField] private SpawnReusePolicy rewardSpawnReusePolicy = SpawnReusePolicy.Pooled;
        [SerializeField] [Min(0)] private int rewardPrewarmBufferCount = 1;

        [Header("Isaac-Style Reward Placement")]
        [SerializeField] private bool useIsaacStyleRewardPlacement = true;
        [SerializeField] private bool normalRewardsUseRoomCenter = true;
        [SerializeField] [Min(0f)] private float clearRewardForwardOffset = 0.18f;
        [SerializeField] [Min(0.1f)] private float clearRewardRingRadius = 0.58f;
        [SerializeField] [Min(0f)] private float clearRewardRingRadiusStep = 0.18f;

        [Header("Isaac-Style Clear Reward Odds")]
        [SerializeField] private bool useIsaacStyleClearRewardOdds = true;
        [SerializeField] [Range(0f, 1f)] private float normalClearRewardChance = 0.45f;
        [SerializeField] [Range(0f, 1f)] private float challengeClearRewardChance = 1f;
        [SerializeField] [Range(0f, 1f)] private float miniBossClearRewardChance = 1f;
        [SerializeField] [Range(0f, 1f)] private float secretClearRewardChance = 1f;
        [SerializeField] [Range(0f, 1f)] private float trapClearRewardChance = 0.35f;
        [SerializeField] [Range(0f, 1f)] private float curseClearRewardChance = 0.55f;

        [Header("Isaac-Style Key Economy Assist")]
        [SerializeField] private bool enableLockedKeyDoorAssist = true;
        [SerializeField] [Range(0f, 1f)] private float lockedKeyDoorAssistChanceWhenNoReward = 0.65f;
        [SerializeField] [Range(0f, 1f)] private float lockedKeyDoorAssistChanceAfterReward = 0.22f;
        [SerializeField] private bool enableHiddenSecretBombAssist = true;
        [SerializeField] [Range(0f, 1f)] private float hiddenSecretBombAssistChanceWhenNoReward = 0.55f;
        [SerializeField] [Range(0f, 1f)] private float hiddenSecretBombAssistChanceAfterReward = 0.18f;

        [Header("Isaac-Style Adaptive Essentials")]
        [SerializeField] private bool enableAdaptiveEssentialRewardWeights = true;
        [SerializeField] [Min(1f)] private float lowHealthHeartWeightMultiplier = 2.35f;
        [SerializeField] [Range(0f, 1f)] private float fullHealthHeartWeightMultiplier = 0.35f;
        [SerializeField] [Min(1f)] private float noKeyWeightMultiplier = 1.75f;
        [SerializeField] [Min(1f)] private float noBombWeightMultiplier = 1.6f;
        [SerializeField] [Min(1f)] private float lowCoinWeightMultiplier = 1.35f;

        [Header("Boss Rewards")]
        [Tooltip("Isaac-style boss rooms primarily drop one item pedestal. Resource sprays are optional bonuses.")]
        [SerializeField] private bool spawnBossResourceBonus;
        [SerializeField] [Range(0f, 1f)] private float bossResourceBonusChance = 0.25f;
        [Tooltip("How many normal enemy drop rolls the optional boss resource bonus should emulate.")]
        [SerializeField] [Min(0)] private int bossEquivalentEnemyDropRollCount = 3;
        [SerializeField] private bool spawnBossFallbackResourcesWhenItemUnavailable = true;
        [SerializeField] [Min(0)] private int bossFallbackCoinCount = 4;
        [SerializeField] [Min(0)] private int bossFallbackKeyCount = 1;
        [SerializeField] [Min(0)] private int bossFallbackBombCount = 1;

        [Header("Item Reward Fallback")]
        [SerializeField] private bool spawnItemFallbackResourcesWhenPoolUnavailable = true;
        [SerializeField] [Min(0)] private int itemFallbackCoinCount = 3;
        [SerializeField] [Min(0)] private int itemFallbackKeyCount = 1;
        [SerializeField] [Min(0)] private int itemFallbackBombCount;

        [Header("Challenge Reveal Layout")]
        [SerializeField] [Min(0.2f)] private float challengeRevealPrimarySpacing = 1.05f;
        [SerializeField] [Min(0.2f)] private float challengeRevealSecondarySpacing = 0.92f;
        [SerializeField] [Min(0f)] private float challengeRevealForwardOffset = 0.82f;
        [SerializeField] [Min(0f)] private float challengeRevealRowOffset = 0.72f;
        [SerializeField] [Min(0f)] private float challengeRevealArcLift = 0.18f;
        [SerializeField] [Range(0f, 1f)] private float challengeRevealEliteSpreadBoost = 0.18f;
        [SerializeField] [Range(0f, 1f)] private float challengeRevealDeadlySpreadBoost = 0.36f;

        [Header("Momentum Reveal Layout")]
        [SerializeField] [Min(0f)] private float momentumRevealForwardOffset = 1.08f;
        [SerializeField] [Min(0.2f)] private float momentumRevealSpacing = 0.92f;
        [SerializeField] [Min(0f)] private float momentumRevealRowOffset = 0.58f;
        [SerializeField] [Min(0f)] private float momentumRevealArcLift = 0.18f;

        private readonly List<RoomRewardEntry> _candidateEntries = new();
        private readonly List<RoomRewardEntry> _selectionPool = new();
        private readonly HashSet<string> _selectedRewardItemIds = new(System.StringComparer.OrdinalIgnoreCase);
        private RoomRewardTable _runtimeDefaultRewardTable;
        private RoomRewardTable _runtimeRewardTableOverride;
        private ItemPoolData _runtimeItemRewardPool;
        private GameObject _runtimeItemRewardPickupPrefab;
        private ChallengeRewardSettings _runtimeChallengeRewardSettings;
        private SecretRoomRewardSettings _runtimeSecretRewardSettings;
        private SpecialRoomRuleData _runtimeSpecialRoomRule;
        private float _runtimeRewardMultiplier = 1f;
        private bool _hasSpawnedRewards;
        private bool _isShuttingDown;
        private RunItemPoolService _runItemPoolService;
        private RoomRewardPhaseSummary _activeRewardPhaseSummary;
        private int _activeRewardLayoutCount;

        public bool HasSpawnedRewards => _hasSpawnedRewards;

        public bool TryGetPrimaryRewardAnchorPosition(out Vector3 position)
        {
            if (rewardSpawnAnchor != null)
            {
                position = rewardSpawnAnchor.position;
                return true;
            }

            position = transform.position;
            return true;
        }

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
            _runtimeSpecialRoomRule = null;
            _runtimeRewardMultiplier = 1f;
            _hasSpawnedRewards = false;
        }

        /// <summary>
        /// Generated room content can override the prefab default reward table without teaching RoomController about reward variants.
        /// </summary>
        public void ConfigureRewardRules(RoomType roomType, RoomRewardTable rewardTableOverride)
        {
            roomTypeForRewards = roomType;
            _runtimeRewardTableOverride = rewardTableOverride;
            _runtimeSpecialRoomRule = null;
            _runtimeRewardMultiplier = 1f;
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

        public void ConfigureSpecialRoomRule(RoomType roomType, SpecialRoomRuleData specialRoomRule)
        {
            roomTypeForRewards = roomType;
            _runtimeSpecialRoomRule = specialRoomRule;

            if (specialRoomRule == null)
            {
                _runtimeRewardMultiplier = 1f;
                return;
            }

            if (specialRoomRule.RewardTable != null)
            {
                _runtimeRewardTableOverride = specialRoomRule.RewardTable;
            }

            if (specialRoomRule.ItemPool != null)
            {
                _runtimeItemRewardPool = specialRoomRule.ItemPool;
                _selectedRewardItemIds.Clear();
            }

            _runtimeRewardMultiplier = specialRoomRule.RewardMultiplier;
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

            ResolveReferencesFromSceneContext();

            if (_runItemPoolService == null)
            {
                _runItemPoolService = FindFirstObjectByType<RunItemPoolService>(FindObjectsInactive.Exclude);
            }

            if (playerItemManager == null)
            {
                playerItemManager = FindFirstObjectByType<PlayerItemManager>(FindObjectsInactive.Exclude);
            }

            if (playerInventory == null)
            {
                playerInventory = FindFirstObjectByType<PlayerInventory>(FindObjectsInactive.Exclude);
            }

            if (playerHealth == null)
            {
                playerHealth = FindFirstObjectByType<PlayerHealth>(FindObjectsInactive.Exclude);
            }

            if (momentumRewardController == null)
            {
                momentumRewardController = GetComponent<RoomMomentumRewardController>();

                if (momentumRewardController == null)
                {
                    momentumRewardController = gameObject.AddComponent<RoomMomentumRewardController>();
                }
            }
        }

        private void ResolveReferencesFromSceneContext()
        {
            if (sceneContext == null)
            {
                sceneContext = GameplaySceneContext.Active;
            }

            if (sceneContext == null)
            {
                return;
            }

            sceneContext.ResolveMissingReferences();
            _runItemPoolService ??= sceneContext.RunItemPoolService;
            playerItemManager ??= sceneContext.PlayerItemManager;
            playerInventory ??= sceneContext.PlayerInventory;
            playerHealth ??= sceneContext.PlayerHealth;
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
            if (_isShuttingDown || _hasSpawnedRewards || resolvedRoom == null || roomController != resolvedRoom)
            {
                return default;
            }

            if (roomTypeForRewards == RoomType.Boss)
            {
                return HandleBossRewardRoom(resolvedRoom);
            }

            RoomRewardTable resolvedRewardTable = ResolveRewardTable();
            ChallengeClearRank challengeClearRank = ResolveChallengeClearRank(resolvedRoom, allowNonCombatRewards);
            ChallengePressureTier challengePressureTier = ResolveChallengePressureTier(resolvedRoom, allowNonCombatRewards);
            int bonusRewardSelections = ResolveChallengeBonusRewardSelections(challengeClearRank);
            int bonusItemRolls = ResolveChallengeBonusItemRolls(challengeClearRank);
            int pressureBonusRewardSelections = ResolveChallengePressureBonusRewardSelections(challengePressureTier);
            int pressureBonusItemRolls = ResolveChallengePressureBonusItemRolls(challengePressureTier);
            int secretBonusRewardSelections = ResolveSecretBonusRewardSelections(allowNonCombatRewards);
            int secretBonusItemRolls = ResolveSecretBonusItemRolls(allowNonCombatRewards);
            int passiveBonusRewardSelections = 0;
            int passiveBonusItemRolls = 0;
            string passiveBonusTitle = string.Empty;
            string passiveBonusSubtitle = string.Empty;
            Color passiveBonusAccentColor = Color.white;
            int momentumBonusRewardSelections = 0;
            int momentumBonusItemRolls = 0;
            string momentumBonusTitle = string.Empty;
            string momentumBonusSubtitle = string.Empty;
            Color momentumBonusAccentColor = Color.white;

            if (allowNonCombatRewards && !ShouldSpawnRewardsOnNonCombatResolve())
            {
                return default;
            }

            if (momentumRewardController != null)
            {
                momentumRewardController.TryConsumeRewardBonus(
                    resolvedRoom,
                    allowNonCombatRewards,
                    out momentumBonusRewardSelections,
                    out momentumBonusItemRolls,
                    out momentumBonusTitle,
                    out momentumBonusSubtitle,
                    out momentumBonusAccentColor);
            }

            if (playerItemManager != null)
            {
                playerItemManager.TryGetRoomRewardBonus(
                    roomTypeForRewards,
                    allowNonCombatRewards,
                    out passiveBonusRewardSelections,
                    out passiveBonusItemRolls,
                    out passiveBonusTitle,
                    out passiveBonusSubtitle,
                    out passiveBonusAccentColor);
            }

            int expectedStandardRewardSpawnCount = EstimateRewardSpawnCount(
                resolvedRewardTable,
                bonusRewardSelections + pressureBonusRewardSelections + passiveBonusRewardSelections,
                secretBonusRewardSelections,
                bonusItemRolls + pressureBonusItemRolls + passiveBonusItemRolls,
                secretBonusItemRolls,
                allowNonCombatRewards);
            int expectedRewardSpawnCount = EstimateRewardSpawnCount(
                resolvedRewardTable,
                bonusRewardSelections + pressureBonusRewardSelections + passiveBonusRewardSelections + momentumBonusRewardSelections,
                secretBonusRewardSelections,
                bonusItemRolls + pressureBonusItemRolls + passiveBonusItemRolls + momentumBonusItemRolls,
                secretBonusItemRolls,
                allowNonCombatRewards);
            _activeRewardPhaseSummary = new RoomRewardPhaseSummary(
                expectedRewardSpawnCount,
                challengeClearRank,
                challengePressureTier,
                bonusRewardSelections + pressureBonusRewardSelections + passiveBonusRewardSelections + momentumBonusRewardSelections,
                bonusItemRolls + pressureBonusItemRolls + passiveBonusItemRolls + momentumBonusItemRolls,
                IsChallengeFinale(resolvedRoom, allowNonCombatRewards),
                momentumBonusRewardSelections,
                momentumBonusItemRolls,
                momentumBonusAccentColor);
            _activeRewardLayoutCount = expectedStandardRewardSpawnCount;

            int spawnIndex = 0;
            int momentumSpawnIndex = 0;

            if (resolvedRewardTable != null)
            {
                resolvedRewardTable.CollectCandidates(ResolveRewardFilterRoomType(), _candidateEntries);
                FilterRoomClearRewardCandidates(_candidateEntries);

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

                    int baseSelectionCount = ResolveBaseRewardSelectionCount(
                        resolvedRewardTable,
                        _selectionPool.Count,
                        allowNonCombatRewards);
                    baseSelectionCount = ApplyRuntimeRewardMultiplier(baseSelectionCount);
                    int standardBonusSelectionCount = bonusRewardSelections
                        + pressureBonusRewardSelections
                        + passiveBonusRewardSelections
                        + secretBonusRewardSelections;
                    spawnIndex += SpawnRewardSelectionsFromPool(
                        resolvedRewardTable,
                        baseSelectionCount + standardBonusSelectionCount,
                        spawnIndex,
                        false,
                        ref momentumSpawnIndex,
                        momentumBonusAccentColor);
                    spawnIndex += SpawnRewardSelectionsFromPool(
                        resolvedRewardTable,
                        momentumBonusRewardSelections,
                        spawnIndex,
                        true,
                        ref momentumSpawnIndex,
                        momentumBonusAccentColor);
                }
            }

            int standardItemRollCount = ShouldSpawnItemPoolReward()
                ? ApplyRuntimeRewardMultiplier(1) + bonusItemRolls + pressureBonusItemRolls + passiveBonusItemRolls + secretBonusItemRolls
                : 0;

            for (int itemRollIndex = 0; itemRollIndex < standardItemRollCount; itemRollIndex++)
            {
                spawnIndex += SpawnItemRewardFromPool(spawnIndex);
            }

            int standardSpawnCount = spawnIndex;

            for (int momentumItemRollIndex = 0; momentumItemRollIndex < momentumBonusItemRolls; momentumItemRollIndex++)
            {
                spawnIndex += SpawnItemRewardFromPool(
                    spawnIndex,
                    markMomentumBonus: true,
                    momentumSpawnIndex: momentumSpawnIndex,
                    momentumAccentColor: momentumBonusAccentColor);
                momentumSpawnIndex++;
            }

            int economyAssistCount = TrySpawnLockedKeyDoorAssist(resolvedRoom, spawnIndex, spawnIndex > 0);
            spawnIndex += economyAssistCount;

            if (economyAssistCount <= 0)
            {
                spawnIndex += TrySpawnHiddenSecretBombAssist(resolvedRoom, spawnIndex, spawnIndex > 0);
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
                bonusRewardSelections + pressureBonusRewardSelections + momentumBonusRewardSelections,
                bonusItemRolls + pressureBonusItemRolls + momentumBonusItemRolls);
            RaiseSecretRewardFeedback(secretBonusRewardSelections, secretBonusItemRolls);
            RaisePassiveRewardFeedback(
                passiveBonusRewardSelections,
                passiveBonusItemRolls,
                passiveBonusTitle,
                passiveBonusSubtitle,
                passiveBonusAccentColor);
            RaiseMomentumRewardFeedback(
                momentumBonusRewardSelections,
                momentumBonusItemRolls,
                momentumBonusTitle,
                momentumBonusSubtitle,
                momentumBonusAccentColor);

            RoomRewardPhaseSummary result = new RoomRewardPhaseSummary(
                spawnIndex,
                challengeClearRank,
                challengePressureTier,
                bonusRewardSelections + pressureBonusRewardSelections + passiveBonusRewardSelections + momentumBonusRewardSelections,
                bonusItemRolls + pressureBonusItemRolls + passiveBonusItemRolls + momentumBonusItemRolls,
                IsChallengeFinale(resolvedRoom, allowNonCombatRewards),
                momentumBonusRewardSelections,
                momentumBonusItemRolls,
                momentumBonusAccentColor);
            _activeRewardPhaseSummary = result;
            _activeRewardLayoutCount = Mathf.Max(_activeRewardLayoutCount, standardSpawnCount);
            return result;
        }

        private RoomRewardPhaseSummary HandleBossRewardRoom(RoomController resolvedRoom)
        {
            if (resolvedRoom == null || roomController != resolvedRoom)
            {
                return default;
            }

            int spawnedCount = 0;
            int spawnIndex = 0;
            spawnedCount += SpawnBossResourceBudget(ref spawnIndex);
            int artifactRewardCount = SpawnBossArtifactReward();
            spawnedCount += artifactRewardCount;

            if (artifactRewardCount <= 0)
            {
                spawnedCount += SpawnBossFallbackResourceReward(ref spawnIndex);
            }

            _hasSpawnedRewards = spawnedCount > 0;
            _activeRewardPhaseSummary = new RoomRewardPhaseSummary(spawnedCount);
            _activeRewardLayoutCount = spawnedCount;

            if (_hasSpawnedRewards && logRewardSpawnsInEditor)
            {
                Debug.Log($"RoomRewardSpawner spawned {spawnedCount} boss reward pickup(s) for room '{resolvedRoom.RoomId}'.", this);
            }

            if (_hasSpawnedRewards)
            {
                GameAudioEvents.Raise(GameAudioEventType.RewardSpawned, ResolveBossRewardCenter());
            }

            return _activeRewardPhaseSummary;
        }

        private int SpawnBossResourceBudget(ref int spawnIndex)
        {
            if (!spawnBossResourceBonus || Random.value > bossResourceBonusChance)
            {
                return 0;
            }

            int spawnedCount = 0;
            int rollCount = Mathf.Max(0, bossEquivalentEnemyDropRollCount);

            for (int rollIndex = 0; rollIndex < rollCount; rollIndex++)
            {
                switch (CuteIssac.Item.EnemyDropRules.RollDropKind(
                    CuteIssac.Item.EnemyDropRules.CoinDropChance,
                    CuteIssac.Item.EnemyDropRules.AmmoDropChance,
                    CuteIssac.Item.EnemyDropRules.BombDropChance,
                    CuteIssac.Item.EnemyDropRules.KeyDropChance,
                    CuteIssac.Item.EnemyDropRules.NoDropChance))
                {
                    case EnemyDropKind.Coins:
                        spawnedCount += SpawnBossCoinDrops(ref spawnIndex, CuteIssac.Item.EnemyDropRules.RollCoinDropCount(
                            CuteIssac.Item.EnemyDropRules.SingleCoinChance,
                            CuteIssac.Item.EnemyDropRules.DoubleCoinChance,
                            CuteIssac.Item.EnemyDropRules.TripleCoinChance));
                        break;
                    case EnemyDropKind.Ammo:
                        if (TrySpawnBossResourcePickup(EnemyDropKind.Ammo, spawnIndex + 1))
                        {
                            spawnIndex++;
                            spawnedCount++;
                        }
                        break;
                    case EnemyDropKind.Bomb:
                        if (TrySpawnBossResourcePickup(EnemyDropKind.Bomb, spawnIndex + 1))
                        {
                            spawnIndex++;
                            spawnedCount++;
                        }
                        break;
                    case EnemyDropKind.Key:
                        if (TrySpawnBossResourcePickup(EnemyDropKind.Key, spawnIndex + 1))
                        {
                            spawnIndex++;
                            spawnedCount++;
                        }
                        break;
                }
            }

            return spawnedCount;
        }

        private int SpawnBossFallbackResourceReward(ref int spawnIndex)
        {
            if (!spawnBossFallbackResourcesWhenItemUnavailable)
            {
                return 0;
            }

            int spawnedCount = 0;
            int keyCount = playerInventory != null && playerInventory.Keys > 0
                ? 0
                : bossFallbackKeyCount;
            int bombCount = playerInventory != null && playerInventory.Bombs > 0
                ? 0
                : bossFallbackBombCount;

            for (int keyIndex = 0; keyIndex < keyCount; keyIndex++)
            {
                if (TrySpawnBossResourcePickup(EnemyDropKind.Key, spawnIndex + 1))
                {
                    spawnIndex++;
                    spawnedCount++;
                }
            }

            for (int bombIndex = 0; bombIndex < bombCount; bombIndex++)
            {
                if (TrySpawnBossResourcePickup(EnemyDropKind.Bomb, spawnIndex + 1))
                {
                    spawnIndex++;
                    spawnedCount++;
                }
            }

            for (int coinIndex = 0; coinIndex < bossFallbackCoinCount; coinIndex++)
            {
                if (TrySpawnBossResourcePickup(EnemyDropKind.Coins, spawnIndex + 1))
                {
                    spawnIndex++;
                    spawnedCount++;
                }
            }

            if (spawnedCount > 0)
            {
                GameplayFeedbackEvents.RaiseBannerFeedback(new BannerFeedbackRequest(
                    "BOSS CACHE",
                    "Item pool empty. Resource cache opened.",
                    new Color(1f, 0.76f, 0.28f, 1f),
                    1.85f));
            }

            return spawnedCount;
        }

        private int SpawnBossCoinDrops(ref int spawnIndex, int coinCount)
        {
            int spawnedCount = 0;
            int actualCoinCount = Mathf.Max(1, coinCount);

            for (int coinIndex = 0; coinIndex < actualCoinCount; coinIndex++)
            {
                if (TrySpawnBossResourcePickup(EnemyDropKind.Coins, spawnIndex + 1))
                {
                    spawnIndex++;
                    spawnedCount++;
                }
            }

            return spawnedCount;
        }

        private bool TrySpawnBossResourcePickup(EnemyDropKind dropKind, int spawnIndex)
        {
            return TrySpawnRuntimeResourcePickup(
                dropKind,
                ResolveBossRewardPosition(spawnIndex),
                ResolveBossResourcePickupName(dropKind));
        }

        private int TrySpawnLockedKeyDoorAssist(RoomController resolvedRoom, int spawnIndex, bool alreadySpawnedReward)
        {
            if (!enableLockedKeyDoorAssist
                || roomTypeForRewards != RoomType.Normal
                || resolvedRoom == null
                || playerInventory == null
                || playerInventory.Keys > 0
                || !HasLockedKeyDoor(resolvedRoom, playerInventory.Keys))
            {
                return 0;
            }

            float chance = alreadySpawnedReward
                ? lockedKeyDoorAssistChanceAfterReward
                : lockedKeyDoorAssistChanceWhenNoReward;

            if (chance <= 0f || Random.value > chance)
            {
                return 0;
            }

            return TrySpawnRuntimeResourcePickup(EnemyDropKind.Key, ResolveSpawnPosition(spawnIndex), "LockedDoorKeyAssist")
                ? 1
                : 0;
        }

        private int TrySpawnHiddenSecretBombAssist(RoomController resolvedRoom, int spawnIndex, bool alreadySpawnedReward)
        {
            if (!enableHiddenSecretBombAssist
                || roomTypeForRewards != RoomType.Normal
                || resolvedRoom == null
                || playerInventory == null
                || playerInventory.Bombs > 0
                || !HasHiddenSecretDoor(resolvedRoom))
            {
                return 0;
            }

            float chance = alreadySpawnedReward
                ? hiddenSecretBombAssistChanceAfterReward
                : hiddenSecretBombAssistChanceWhenNoReward;

            if (chance <= 0f || Random.value > chance)
            {
                return 0;
            }

            return TrySpawnRuntimeResourcePickup(EnemyDropKind.Bomb, ResolveSpawnPosition(spawnIndex), "HiddenSecretBombAssist")
                ? 1
                : 0;
        }

        private static bool HasLockedKeyDoor(RoomController room, int availableKeys)
        {
            if (room == null)
            {
                return false;
            }

            IReadOnlyList<RoomDoor> roomDoors = room.RoomDoors;
            if (roomDoors == null)
            {
                return false;
            }

            for (int i = 0; i < roomDoors.Count; i++)
            {
                RoomDoor roomDoor = roomDoors[i];
                if (roomDoor == null || roomDoor.IsLocked || roomDoor.ConnectedRoom == null)
                {
                    continue;
                }

                int requiredKeys = roomDoor.RequiredKeysToEnter;
                if (requiredKeys > 0 && availableKeys < requiredKeys)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasHiddenSecretDoor(RoomController room)
        {
            if (room == null)
            {
                return false;
            }

            IReadOnlyList<RoomDoor> roomDoors = room.RoomDoors;
            if (roomDoors == null)
            {
                return false;
            }

            for (int i = 0; i < roomDoors.Count; i++)
            {
                RoomDoor roomDoor = roomDoors[i];
                if (roomDoor == null || !roomDoor.HasUnrevealedSecretAccess)
                {
                    continue;
                }

                RoomController connectedRoom = roomDoor.ConnectedRoom;
                if (connectedRoom != null && connectedRoom.RoomType == RoomType.Secret)
                {
                    return true;
                }
            }

            return false;
        }

        private int SpawnBossArtifactReward()
        {
            if (_runtimeItemRewardPool == null || _runtimeItemRewardPickupPrefab == null)
            {
                if (logRewardSpawnsInEditor)
                {
                    Debug.LogWarning(
                        $"RoomRewardSpawner boss reward is missing an item pool or pickup prefab for room '{roomController?.RoomId}'.",
                        this);
                }

                return 0;
            }

            RoomType selectionRoomType = ResolveItemSelectionRoomType();
            ItemPoolSelectionContext selectionContext = _runItemPoolService != null
                ? _runItemPoolService.BuildSelectionContext(selectionRoomType, _selectedRewardItemIds)
                : new ItemPoolSelectionContext(selectionRoomType, 1, _selectedRewardItemIds, null, null, null, null, null);

            if (!_runtimeItemRewardPool.TrySelectRandomNonWeaponItem(selectionContext, out ItemData selectedItem) || selectedItem == null)
            {
                if (logRewardSpawnsInEditor)
                {
                    Debug.LogWarning(
                        $"RoomRewardSpawner could not resolve an artifact boss reward for room '{roomController?.RoomId}'.",
                        this);
                }

                return 0;
            }

            PrewarmPickupIfNeeded(_runtimeItemRewardPickupPrefab, 1);

            GameObject rewardObject = GameplaySpawnFactory.SpawnGameObject(
                _runtimeItemRewardPickupPrefab,
                ResolveBossRewardCenter(),
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
                return 1;
            }

            Debug.LogWarning("RoomRewardSpawner boss reward prefab is missing ItemPickupLogic.", rewardObject);
            PrefabPoolService.Return(rewardObject);
            return 0;
        }

        private Vector3 ResolveBossRewardCenter()
        {
            return rewardSpawnAnchor != null ? rewardSpawnAnchor.position : transform.position;
        }

        private Vector3 ResolveBossRewardPosition(int spawnIndex)
        {
            Vector3 center = ResolveBossRewardCenter();

            if (scatterRadius <= 0f || spawnIndex <= 0)
            {
                return center;
            }

            float angle = 137.5f * spawnIndex * Mathf.Deg2Rad;
            float radius = Mathf.Max(0f, scatterRadius) * Mathf.Clamp01(0.35f + (0.18f * spawnIndex));
            Vector2 offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            return center + new Vector3(offset.x, offset.y, 0f);
        }

        private void OnDestroy()
        {
            _isShuttingDown = true;
        }

        private void OnApplicationQuit()
        {
            _isShuttingDown = true;
        }

        private int SpawnRewardSelectionsFromPool(
            RoomRewardTable resolvedRewardTable,
            int selectionCount,
            int startSpawnIndex,
            bool markMomentumBonus,
            ref int momentumSpawnIndex,
            Color momentumAccentColor)
        {
            if (resolvedRewardTable == null || selectionCount <= 0)
            {
                return 0;
            }

            int spawnedCount = 0;

            for (int selectionIndex = 0; selectionIndex < selectionCount; selectionIndex++)
            {
                if (_selectionPool.Count == 0)
                {
                    break;
                }

                int selectedIndex = SelectWeightedIndex(_selectionPool, ResolveAdaptiveRewardWeightMultiplier);

                if (selectedIndex < 0)
                {
                    break;
                }

                RoomRewardEntry selectedEntry = _selectionPool[selectedIndex];

                for (int quantityIndex = 0; quantityIndex < selectedEntry.Quantity; quantityIndex++)
                {
                    SpawnRewardInstance(
                        selectedEntry,
                        startSpawnIndex + spawnedCount,
                        markMomentumBonus,
                        markMomentumBonus ? momentumSpawnIndex : -1,
                        momentumAccentColor);
                    spawnedCount++;

                    if (markMomentumBonus)
                    {
                        momentumSpawnIndex++;
                    }
                }

                if (!resolvedRewardTable.AllowDuplicateSelections)
                {
                    _selectionPool.RemoveAt(selectedIndex);
                }
            }

            return spawnedCount;
        }

        private void SpawnRewardInstance(
            RoomRewardEntry rewardEntry,
            int spawnIndex,
            bool markMomentumBonus = false,
            int momentumSpawnIndex = -1,
            Color momentumAccentColor = default)
        {
            GameObject pickupPrefab = rewardEntry.PickupPrefab;

            if (pickupPrefab == null)
            {
                return;
            }

            PrewarmPickupIfNeeded(pickupPrefab, rewardEntry.Quantity);

            Vector3 spawnPosition = markMomentumBonus
                ? ResolveMomentumRewardSpawnPosition(momentumSpawnIndex)
                : ResolveSpawnPosition(spawnIndex);
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

            ConfigureRewardPickupTracking(
                spawnedPickup,
                markMomentumBonus,
                false,
                momentumAccentColor);

            if (markMomentumBonus)
            {
                ConfigureMomentumRewardPresentation(
                    spawnedPickup,
                    momentumAccentColor,
                    isHighValue: false,
                    momentumSpawnIndex);
            }
        }

        private int SpawnItemRewardFromPool(
            int spawnIndex,
            bool markMomentumBonus = false,
            int momentumSpawnIndex = -1,
            Color momentumAccentColor = default)
        {
            if (!ShouldSpawnItemPoolReward())
            {
                return 0;
            }

            RoomType selectionRoomType = ResolveItemSelectionRoomType();
            ItemPoolSelectionContext selectionContext = _runItemPoolService != null
                ? _runItemPoolService.BuildSelectionContext(selectionRoomType, _selectedRewardItemIds)
                : new ItemPoolSelectionContext(selectionRoomType, 1, _selectedRewardItemIds, null, null, null, null, null);

            if (!_runtimeItemRewardPool.TrySelectRandomItem(selectionContext, out ItemData selectedItem) || selectedItem == null)
            {
                return SpawnItemRewardFallbackResources(spawnIndex, markMomentumBonus, momentumSpawnIndex);
            }

            PrewarmPickupIfNeeded(_runtimeItemRewardPickupPrefab, 1);

            Vector3 spawnPosition = markMomentumBonus
                ? ResolveMomentumRewardSpawnPosition(momentumSpawnIndex)
                : ResolveSpawnPosition(spawnIndex);
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

            ConfigureRewardPickupTracking(
                rewardObject,
                markMomentumBonus,
                markMomentumBonus,
                momentumAccentColor);

            if (markMomentumBonus)
            {
                ConfigureMomentumRewardPresentation(
                    rewardObject,
                    momentumAccentColor,
                    isHighValue: true,
                    momentumSpawnIndex);
            }

            if (rewardObject.TryGetComponent(out ItemPickupLogic itemPickupLogic))
            {
                itemPickupLogic.ConfigureItem(selectedItem);
                _selectedRewardItemIds.Add(selectedItem.ItemId);
                _runItemPoolService?.RegisterOffer(selectedItem);

                if (IsSpecialRoomItemReward())
                {
                    GameplayRuntimeEvents.RaiseSpecialRoomRewardManifested(new SpecialRoomRewardManifestedSignal(
                        roomController,
                        roomTypeForRewards,
                        _runtimeSpecialRoomRule != null ? _runtimeSpecialRoomRule.RuleId : string.Empty,
                        _runtimeSpecialRoomRule != null ? _runtimeSpecialRoomRule.DealType : SpecialRoomDealType.None,
                        selectedItem));
                }

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

        private int SpawnItemRewardFallbackResources(
            int spawnIndex,
            bool markMomentumBonus,
            int momentumSpawnIndex)
        {
            if (!spawnItemFallbackResourcesWhenPoolUnavailable)
            {
                return 0;
            }

            int spawnedCount = 0;
            spawnedCount += SpawnItemFallbackResourcePicks(EnemyDropKind.Key, itemFallbackKeyCount, spawnIndex + spawnedCount, markMomentumBonus, momentumSpawnIndex);
            spawnedCount += SpawnItemFallbackResourcePicks(EnemyDropKind.Bomb, itemFallbackBombCount, spawnIndex + spawnedCount, markMomentumBonus, momentumSpawnIndex + spawnedCount);
            spawnedCount += SpawnItemFallbackResourcePicks(EnemyDropKind.Coins, itemFallbackCoinCount, spawnIndex + spawnedCount, markMomentumBonus, momentumSpawnIndex + spawnedCount);

            if (spawnedCount > 0)
            {
                GameplayFeedbackEvents.RaiseBannerFeedback(new BannerFeedbackRequest(
                    "ITEM CACHE",
                    "Item pool empty. Resource cache opened.",
                    new Color(0.92f, 0.78f, 0.36f, 1f),
                    1.65f));
            }

            return spawnedCount;
        }

        private int SpawnItemFallbackResourcePicks(
            EnemyDropKind dropKind,
            int count,
            int startSpawnIndex,
            bool markMomentumBonus,
            int startMomentumSpawnIndex)
        {
            int spawnedCount = 0;

            for (int index = 0; index < count; index++)
            {
                Vector3 spawnPosition = markMomentumBonus
                    ? ResolveMomentumRewardSpawnPosition(startMomentumSpawnIndex + spawnedCount)
                    : ResolveSpawnPosition(startSpawnIndex + spawnedCount);

                if (TrySpawnRuntimeResourcePickup(dropKind, spawnPosition, ResolveItemFallbackPickupName(dropKind)))
                {
                    spawnedCount++;
                }
            }

            return spawnedCount;
        }

        private bool TrySpawnRuntimeResourcePickup(EnemyDropKind dropKind, Vector3 spawnPosition, string objectName)
        {
            GameObject pickupObject = RuntimePickupFactory.SpawnDefaultEnemyDropPickup(
                dropKind,
                spawnPosition,
                spawnedRewardParent,
                objectName);

            if (pickupObject == null)
            {
                return false;
            }

            ConfigureRewardPickupTracking(pickupObject);
            return true;
        }

        private static string ResolveBossResourcePickupName(EnemyDropKind dropKind)
        {
            return dropKind switch
            {
                EnemyDropKind.Ammo => "AmmoPickup",
                EnemyDropKind.Bomb => "BombPickup",
                EnemyDropKind.Key => "KeyPickup",
                _ => "CoinPickup"
            };
        }

        private static string ResolveItemFallbackPickupName(EnemyDropKind dropKind)
        {
            return dropKind switch
            {
                EnemyDropKind.Ammo => "ItemFallbackAmmo",
                EnemyDropKind.Key => "ItemFallbackKey",
                EnemyDropKind.Bomb => "ItemFallbackBomb",
                _ => "ItemFallbackCoin"
            };
        }

        private void ConfigureRewardPickupTracking(
            GameObject rewardObject,
            bool isMomentumReward = false,
            bool isHighValueMomentumReward = false,
            Color momentumAccentColor = default)
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

            rewardPickupTracker.Configure(
                roomController,
                roomTypeForRewards,
                isMomentumReward,
                isHighValueMomentumReward,
                momentumAccentColor);
        }

        private static void ConfigureMomentumRewardPresentation(GameObject rewardObject, Color accentColor, bool isHighValue, int momentumSpawnIndex)
        {
            if (rewardObject == null)
            {
                return;
            }

            MomentumRewardPickupPresentation presentation = rewardObject.GetComponent<MomentumRewardPickupPresentation>();

            if (presentation == null)
            {
                presentation = rewardObject.AddComponent<MomentumRewardPickupPresentation>();
            }

            presentation.Configure(accentColor, isHighValue, momentumSpawnIndex);
        }

        private void PrewarmPickupIfNeeded(GameObject pickupPrefab, int expectedSpawnCount)
        {
            if (rewardSpawnReusePolicy != SpawnReusePolicy.Pooled || pickupPrefab == null)
            {
                return;
            }

            PrefabPoolService.EnsurePrewarmed(
                pickupPrefab,
                Mathf.Max(1, expectedSpawnCount + rewardPrewarmBufferCount));
        }

        private Vector3 ResolveSpawnPosition(int spawnIndex)
        {
            Vector3 center = ResolveClearRewardCenter();

            if (ShouldUseChallengeRevealLayout())
            {
                return center + ResolveChallengeRevealOffset(spawnIndex);
            }

            if (useIsaacStyleRewardPlacement)
            {
                return center + ResolveIsaacStyleClearRewardOffset(spawnIndex);
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

        private Vector3 ResolveClearRewardCenter()
        {
            if (useIsaacStyleRewardPlacement && normalRewardsUseRoomCenter && roomController != null)
            {
                return roomController.CameraFocusPosition + new Vector3(0f, clearRewardForwardOffset, 0f);
            }

            Vector3 center = rewardSpawnAnchor != null ? rewardSpawnAnchor.position : transform.position;

            if (preferLastEnemyDeathPosition && roomController != null && roomController.TryGetLastEnemyDeathPosition(out Vector3 lastEnemyDeathPosition))
            {
                center = lastEnemyDeathPosition;
            }

            return center;
        }

        private Vector3 ResolveIsaacStyleClearRewardOffset(int spawnIndex)
        {
            if (spawnIndex <= 0)
            {
                return Vector3.zero;
            }

            int ringIndex = Mathf.Max(0, (spawnIndex - 1) / 6);
            int slotIndex = (spawnIndex - 1) % 6;
            float radius = clearRewardRingRadius + (clearRewardRingRadiusStep * ringIndex);
            float angle = (90f + (slotIndex * 60f) + (ringIndex * 30f)) * Mathf.Deg2Rad;
            return new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f);
        }

        private Vector3 ResolveMomentumRewardSpawnPosition(int momentumSpawnIndex)
        {
            Vector3 center = rewardSpawnAnchor != null ? rewardSpawnAnchor.position : transform.position;

            if (preferLastEnemyDeathPosition && roomController != null && roomController.TryGetLastEnemyDeathPosition(out Vector3 lastEnemyDeathPosition))
            {
                center = lastEnemyDeathPosition;
            }

            int totalMomentumCount = Mathf.Max(
                1,
                _activeRewardPhaseSummary.MomentumBonusRewardSelections + _activeRewardPhaseSummary.MomentumBonusItemRolls);

            if (momentumSpawnIndex < 0)
            {
                momentumSpawnIndex = 0;
            }

            int columns = totalMomentumCount >= 4 ? 3 : totalMomentumCount;
            int rowIndex = columns > 0 ? momentumSpawnIndex / columns : 0;
            int columnIndex = columns > 0 ? momentumSpawnIndex % columns : 0;
            float centerColumn = (columns - 1) * 0.5f;
            float normalizedColumn = centerColumn > 0f
                ? (columnIndex - centerColumn) / centerColumn
                : 0f;
            float horizontalOffset = normalizedColumn * momentumRevealSpacing;
            float verticalOffset = momentumRevealForwardOffset - (rowIndex * momentumRevealRowOffset);
            verticalOffset += (1f - Mathf.Abs(normalizedColumn)) * momentumRevealArcLift;
            return center + new Vector3(horizontalOffset, verticalOffset, 0f);
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
            int secretBonusItemRolls,
            bool allowNonCombatRewards)
        {
            int estimatedEntryCount = 0;

            if (resolvedRewardTable != null)
            {
                resolvedRewardTable.CollectCandidates(ResolveRewardFilterRoomType(), _candidateEntries);
                FilterRoomClearRewardCandidates(_candidateEntries);
                if (_candidateEntries.Count > 0)
                {
                    int selectionCount = EstimateRuntimeRewardMultiplier(EstimateBaseRewardSelectionCount(
                            resolvedRewardTable,
                            _candidateEntries.Count,
                            allowNonCombatRewards))
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
                ? EstimateRuntimeRewardMultiplier(1) + Mathf.Max(0, bonusItemRolls) + Mathf.Max(0, secretBonusItemRolls)
                : 0;

            return estimatedEntryCount + estimatedItemCount;
        }

        private int ApplyRuntimeRewardMultiplier(int count)
        {
            if (count <= 0)
            {
                return 0;
            }

            float multiplier = Mathf.Max(0f, _runtimeRewardMultiplier);
            if (Mathf.Approximately(multiplier, 1f))
            {
                return count;
            }

            if (multiplier <= 0f)
            {
                return 0;
            }

            float scaledCount = count * multiplier;
            int wholeCount = Mathf.FloorToInt(scaledCount);
            float fractionalCount = scaledCount - wholeCount;

            if (fractionalCount > 0f && Random.value < fractionalCount)
            {
                wholeCount++;
            }

            return Mathf.Max(1, wholeCount);
        }

        private int EstimateRuntimeRewardMultiplier(int count)
        {
            if (count <= 0)
            {
                return 0;
            }

            float multiplier = Mathf.Max(0f, _runtimeRewardMultiplier);
            if (multiplier <= 0f)
            {
                return 0;
            }

            return Mathf.Max(1, Mathf.RoundToInt(count * multiplier));
        }

        private delegate float RewardWeightMultiplierResolver(RoomRewardEntry entry);

        private static int SelectWeightedIndex(List<RoomRewardEntry> entries, RewardWeightMultiplierResolver multiplierResolver = null)
        {
            float totalWeight = 0f;

            for (int i = 0; i < entries.Count; i++)
            {
                RoomRewardEntry entry = entries[i];
                totalWeight += entry.Weight * ResolveSelectionWeightMultiplier(entry, multiplierResolver);
            }

            if (totalWeight <= 0f)
            {
                return -1;
            }

            float threshold = Random.value * totalWeight;

            for (int i = 0; i < entries.Count; i++)
            {
                RoomRewardEntry entry = entries[i];
                threshold -= entry.Weight * ResolveSelectionWeightMultiplier(entry, multiplierResolver);

                if (threshold <= 0f)
                {
                    return i;
                }
            }

            return entries.Count - 1;
        }

        private static float ResolveSelectionWeightMultiplier(
            RoomRewardEntry entry,
            RewardWeightMultiplierResolver multiplierResolver)
        {
            return multiplierResolver != null ? Mathf.Max(0f, multiplierResolver(entry)) : 1f;
        }

        private float ResolveAdaptiveRewardWeightMultiplier(RoomRewardEntry entry)
        {
            if (!enableAdaptiveEssentialRewardWeights || !IsAdaptiveEssentialRewardRoom(roomTypeForRewards))
            {
                return 1f;
            }

            switch (entry.RewardType)
            {
                case RoomRewardType.Heart:
                    return ResolveHeartRewardWeightMultiplier();
                case RoomRewardType.Key:
                    return playerInventory != null && playerInventory.Keys <= 0
                        ? noKeyWeightMultiplier
                        : 1f;
                case RoomRewardType.Bomb:
                    return playerInventory != null && playerInventory.Bombs <= 0
                        ? noBombWeightMultiplier
                        : 1f;
                case RoomRewardType.Coin:
                    return playerInventory != null && playerInventory.Coins < 5
                        ? lowCoinWeightMultiplier
                        : 1f;
                default:
                    return 1f;
            }
        }

        private float ResolveHeartRewardWeightMultiplier()
        {
            if (playerHealth == null)
            {
                return 1f;
            }

            float maxHealth = Mathf.Max(1f, playerHealth.MaxHealth);
            float healthRatio = Mathf.Clamp01(playerHealth.CurrentHealth / maxHealth);

            if (healthRatio <= 0.45f)
            {
                return lowHealthHeartWeightMultiplier;
            }

            return healthRatio >= 0.98f ? fullHealthHeartWeightMultiplier : 1f;
        }

        private static bool IsAdaptiveEssentialRewardRoom(RoomType roomType)
        {
            return roomType == RoomType.Normal
                || roomType == RoomType.Challenge
                || roomType == RoomType.MiniBoss
                || roomType == RoomType.Trap
                || roomType == RoomType.Curse;
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
                    || roomTypeForRewards == RoomType.Curse
                    || _runtimeSpecialRoomRule != null)
                && _runtimeItemRewardPool != null
                && _runtimeItemRewardPickupPrefab != null;
        }

        private bool ShouldSpawnRewardsOnNonCombatResolve()
        {
            return roomTypeForRewards == RoomType.Secret
                || roomTypeForRewards == RoomType.Trap
                || roomTypeForRewards == RoomType.Curse;
        }

        private int ResolveBaseRewardSelectionCount(RoomRewardTable resolvedRewardTable, int availableCandidateCount, bool allowNonCombatRewards)
        {
            if (resolvedRewardTable == null || availableCandidateCount <= 0)
            {
                return 0;
            }

            int selectionCount = resolvedRewardTable.GetSelectionCount();
            if (!resolvedRewardTable.AllowDuplicateSelections)
            {
                selectionCount = Mathf.Min(selectionCount, availableCandidateCount);
            }

            if (selectionCount <= 0 || !useIsaacStyleClearRewardOdds)
            {
                return Mathf.Max(0, selectionCount);
            }

            float chance = ResolveClearRewardChance(allowNonCombatRewards);
            if (chance >= 1f)
            {
                return selectionCount;
            }

            if (chance <= 0f)
            {
                return 0;
            }

            return Random.value <= chance ? selectionCount : 0;
        }

        private int EstimateBaseRewardSelectionCount(RoomRewardTable resolvedRewardTable, int availableCandidateCount, bool allowNonCombatRewards)
        {
            if (resolvedRewardTable == null || availableCandidateCount <= 0)
            {
                return 0;
            }

            float averageSelectionCount = (resolvedRewardTable.MinimumRewardSelections + resolvedRewardTable.MaximumRewardSelections) * 0.5f;
            if (!resolvedRewardTable.AllowDuplicateSelections)
            {
                averageSelectionCount = Mathf.Min(averageSelectionCount, availableCandidateCount);
            }

            if (!useIsaacStyleClearRewardOdds)
            {
                return Mathf.Max(0, Mathf.RoundToInt(averageSelectionCount));
            }

            float expectedCount = averageSelectionCount * ResolveClearRewardChance(allowNonCombatRewards);
            return expectedCount > 0f ? Mathf.Max(1, Mathf.RoundToInt(expectedCount)) : 0;
        }

        private float ResolveClearRewardChance(bool allowNonCombatRewards)
        {
            if (!useIsaacStyleClearRewardOdds)
            {
                return 1f;
            }

            return roomTypeForRewards switch
            {
                RoomType.Normal => normalClearRewardChance,
                RoomType.Challenge => challengeClearRewardChance,
                RoomType.MiniBoss => miniBossClearRewardChance,
                RoomType.Secret => secretClearRewardChance,
                RoomType.Trap => trapClearRewardChance,
                RoomType.Curse => curseClearRewardChance,
                _ => allowNonCombatRewards ? 1f : 1f
            };
        }

        private void FilterRoomClearRewardCandidates(List<RoomRewardEntry> candidates)
        {
            if (candidates == null || candidates.Count == 0)
            {
                return;
            }

            bool allowPassiveItems = roomTypeForRewards != RoomType.Normal;

            candidates.RemoveAll(entry =>
                entry.RewardType != RoomRewardType.Coin &&
                entry.RewardType != RoomRewardType.Heart &&
                entry.RewardType != RoomRewardType.Key &&
                entry.RewardType != RoomRewardType.Bomb &&
                (entry.RewardType != RoomRewardType.PassiveItem || !allowPassiveItems));
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
                ChallengeClearRank.S => "Challenge Rank S",
                ChallengeClearRank.A => "Challenge Rank A",
                ChallengeClearRank.B => "Challenge Clear",
                _ => "Challenge Reward"
            };

            string rewardSummary;
            if (bonusRewardSelections > 0 && bonusItemRolls > 0)
            {
                rewardSummary = $"+Reward {bonusRewardSelections} / +Item Roll {bonusItemRolls}";
            }
            else if (bonusRewardSelections > 0)
            {
                rewardSummary = $"+Reward {bonusRewardSelections}";
            }
            else if (bonusItemRolls > 0)
            {
                rewardSummary = $"+Item Roll {bonusItemRolls}";
            }
            else
            {
                rewardSummary = "Standard reward";
            }

            string pressureSummary = challengePressureTier switch
            {
                ChallengePressureTier.Deadly => "Deadly pressure cleared",
                ChallengePressureTier.Elite => "Elite pressure cleared",
                ChallengePressureTier.Reinforced => "Reinforced pressure cleared",
                _ => string.Empty
            };

            string subtitle = string.IsNullOrEmpty(pressureSummary)
                ? rewardSummary
                : $"{rewardSummary} - {pressureSummary}";

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
                subtitle = $"+Reward {bonusRewardSelections} / +Item Roll {bonusItemRolls}";
            }
            else if (bonusRewardSelections > 0)
            {
                subtitle = $"+Reward {bonusRewardSelections}";
            }
            else
            {
                subtitle = $"+Item Roll {bonusItemRolls}";
            }

            GameplayFeedbackEvents.RaiseBannerFeedback(new BannerFeedbackRequest(
                "Secret Cache Found",
                subtitle,
                new Color(0.88f, 0.62f, 1f, 1f),
                1.9f));
        }

        private static void RaiseMomentumRewardFeedback(
            int bonusRewardSelections,
            int bonusItemRolls,
            string title,
            string subtitle,
            Color accentColor)
        {
            if (bonusRewardSelections <= 0 && bonusItemRolls <= 0)
            {
                return;
            }

            GameplayFeedbackEvents.RaiseBannerFeedback(new BannerFeedbackRequest(
                string.IsNullOrWhiteSpace(title) ? "Momentum Payout" : title,
                string.IsNullOrWhiteSpace(subtitle)
                    ? bonusItemRolls > 0
                        ? $"+Reward {bonusRewardSelections} / +Item {bonusItemRolls}"
                        : $"+Reward {bonusRewardSelections}"
                    : subtitle,
                accentColor.a > 0.01f ? accentColor : new Color(1f, 0.84f, 0.36f, 1f),
                1.65f));
        }

        private static void RaisePassiveRewardFeedback(
            int bonusRewardSelections,
            int bonusItemRolls,
            string title,
            string subtitle,
            Color accentColor)
        {
            if (bonusRewardSelections <= 0 && bonusItemRolls <= 0)
            {
                return;
            }

            GameplayFeedbackEvents.RaiseBannerFeedback(new BannerFeedbackRequest(
                string.IsNullOrWhiteSpace(title) ? "ITEM CACHE BONUS" : title,
                string.IsNullOrWhiteSpace(subtitle)
                    ? bonusItemRolls > 0
                        ? $"+REWARD {bonusRewardSelections} / +ITEM {bonusItemRolls}"
                        : $"+REWARD {bonusRewardSelections}"
                    : subtitle,
                accentColor.a > 0.01f ? accentColor : new Color(0.72f, 0.86f, 1f, 1f),
                1.72f));
        }

        private RoomType ResolveRewardFilterRoomType()
        {
            if (_runtimeSpecialRoomRule != null && _runtimeSpecialRoomRule.RewardTable != null)
            {
                return roomTypeForRewards;
            }

            return roomTypeForRewards switch
            {
                RoomType.MiniBoss => RoomType.Boss,
                RoomType.Trap => RoomType.Normal,
                RoomType.Curse => RoomType.Normal,
                _ => roomTypeForRewards
            };
        }

        private RoomType ResolveItemSelectionRoomType()
        {
            if (_runtimeSpecialRoomRule == null)
            {
                return roomTypeForRewards;
            }

            return _runtimeSpecialRoomRule.RoomType;
        }

        private bool IsSpecialRoomItemReward()
        {
            return roomController != null
                && _runtimeSpecialRoomRule != null
                && (roomTypeForRewards == RoomType.Secret
                    || roomTypeForRewards == RoomType.Challenge
                    || roomTypeForRewards == RoomType.Curse
                    || _runtimeSpecialRoomRule.DealType != SpecialRoomDealType.None);
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
