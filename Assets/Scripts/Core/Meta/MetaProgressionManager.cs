using System;
using System.Collections.Generic;
using System.Text;
using CuteIssac.Core.Gameplay;
using CuteIssac.Core.Run;
using CuteIssac.Data.Dungeon;
using CuteIssac.Data.Item;
using CuteIssac.Player;
using UnityEngine;

namespace CuteIssac.Core.Meta
{
    [DefaultExecutionOrder(-80)]
    [DisallowMultipleComponent]
    public sealed class MetaProgressionManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private RunManager runManager;
        [SerializeField] private CharacterProfileManager characterProfileManager;
        [SerializeField] private UnlockManager unlockManager;
        [SerializeField] private PlayerInventory playerInventory;
        [SerializeField] private PlayerItemManager playerItemManager;
        [SerializeField] private PlayerActiveItemController playerActiveItemController;

        [Header("Data")]
        [SerializeField] private string achievementResourcePath = "Achievements";

        public event Action<IReadOnlyList<AchievementData>> AchievementsUnlocked;
        public event Action ProgressionChanged;

        private readonly MetaProgressionSaveData _progression = new();
        private readonly List<AchievementData> _achievementDefinitions = new();
        private readonly List<AchievementData> _unlockedThisEvaluation = new();
        private readonly List<string> _newAchievementDescriptions = new();
        private readonly List<string> _newUnlockDescriptions = new();
        private readonly List<string> _newClearMarks = new();
        private readonly HashSet<string> _completedAchievementIds = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, string> _achievementCompletedAtUtc = new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _discoveredItemIds = new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _seenEnemyIds = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, int> _enemyKillCounts = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, int> _roomTypeClearCounts = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, int> _challengeClearRankCounts = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, int> _challengePressureTierCounts = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, int> _specialRewardOfferCounts = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, int> _specialDealOfferCounts = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, int> _specialDealPurchaseCounts = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, int> _specialDealDeclineCounts = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, CharacterProgressionRecord> _characters = new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _currentRunItemIds = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, int> _currentRunEnemyKills = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, int> _currentRunChallengeClearRankCounts = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, int> _currentRunChallengePressureTierCounts = new(StringComparer.OrdinalIgnoreCase);
        private float _runStartRealtime;
        private float _lastCompletedRunSeconds;
        private string _currentRunCharacterId = "default";
        private int _currentRunBombsSpent;
        private bool _currentRoomTookDamage;

        public MetaProgressionSaveData Progression => _progression;
        public float CurrentRunElapsedSeconds => _runStartRealtime > 0f ? Mathf.Max(0f, Time.realtimeSinceStartup - _runStartRealtime) : 0f;
        public float LastCompletedRunSeconds => _lastCompletedRunSeconds;
        public IReadOnlyList<string> LastNewAchievementDescriptions => _newAchievementDescriptions;
        public IReadOnlyList<string> LastNewUnlockDescriptions => _newUnlockDescriptions;
        public IReadOnlyList<string> LastNewClearMarks => _newClearMarks;
        public string LastRunChallengeSummaryText => BuildCurrentRunChallengeSummaryText();

        private void Awake()
        {
            ResolveReferences();
            LoadAchievementDefinitions();
            RebuildIndexes();
        }

        private void OnEnable()
        {
            ResolveReferences();
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        public void Import(MetaProgressionSaveData saveData)
        {
            CopyProgression(saveData ?? new MetaProgressionSaveData(), _progression);
            RebuildIndexes();
            GrantCompletedAchievementRewards();
        }

        public MetaProgressionSaveData Export()
        {
            SyncIndexesToSaveData();
            MetaProgressionSaveData saveData = new();
            CopyProgression(_progression, saveData);
            return saveData;
        }

        public CharacterProgressionRecord GetCharacterRecord(string characterId)
        {
            if (string.IsNullOrWhiteSpace(characterId))
            {
                characterId = "default";
            }

            return _characters.TryGetValue(characterId, out CharacterProgressionRecord record)
                ? record
                : null;
        }

        private void Subscribe()
        {
            if (runManager != null)
            {
                runManager.RunStarted -= HandleRunStarted;
                runManager.RunStarted += HandleRunStarted;
                runManager.RunEnded -= HandleRunEnded;
                runManager.RunEnded += HandleRunEnded;
            }

            GameplayRuntimeEvents.EnemyKilled -= HandleEnemyKilled;
            GameplayRuntimeEvents.EnemyKilled += HandleEnemyKilled;
            GameplayRuntimeEvents.EnemySeen -= HandleEnemySeen;
            GameplayRuntimeEvents.EnemySeen += HandleEnemySeen;
            GameplayRuntimeEvents.RoomResolved -= HandleRoomResolved;
            GameplayRuntimeEvents.RoomResolved += HandleRoomResolved;
            GameplayRuntimeEvents.RoomEntered -= HandleRoomEntered;
            GameplayRuntimeEvents.RoomEntered += HandleRoomEntered;
            GameplayRuntimeEvents.RoomCleared -= HandleRoomCleared;
            GameplayRuntimeEvents.RoomCleared += HandleRoomCleared;
            GameplayRuntimeEvents.RoomRewardPhaseCompleted -= HandleRoomRewardPhaseCompleted;
            GameplayRuntimeEvents.RoomRewardPhaseCompleted += HandleRoomRewardPhaseCompleted;
            GameplayRuntimeEvents.PlayerDamaged -= HandlePlayerDamaged;
            GameplayRuntimeEvents.PlayerDamaged += HandlePlayerDamaged;
            GameplayRuntimeEvents.PlayerBombSpent -= HandlePlayerBombSpent;
            GameplayRuntimeEvents.PlayerBombSpent += HandlePlayerBombSpent;
            GameplayRuntimeEvents.SecretRoomRevealed -= HandleSecretRoomRevealed;
            GameplayRuntimeEvents.SecretRoomRevealed += HandleSecretRoomRevealed;
            GameplayRuntimeEvents.SpecialRoomRewardManifested -= HandleSpecialRoomRewardManifested;
            GameplayRuntimeEvents.SpecialRoomRewardManifested += HandleSpecialRoomRewardManifested;
            GameplayRuntimeEvents.SpecialRoomDealOffered -= HandleSpecialRoomDealOffered;
            GameplayRuntimeEvents.SpecialRoomDealOffered += HandleSpecialRoomDealOffered;
            GameplayRuntimeEvents.SpecialRoomDealPurchased -= HandleSpecialRoomDealPurchased;
            GameplayRuntimeEvents.SpecialRoomDealPurchased += HandleSpecialRoomDealPurchased;
            GameplayRuntimeEvents.SpecialRoomDealDeclined -= HandleSpecialRoomDealDeclined;
            GameplayRuntimeEvents.SpecialRoomDealDeclined += HandleSpecialRoomDealDeclined;

            TryBindPlayerItemManager();
            TryBindPlayerActiveItemController();
        }

        private void Unsubscribe()
        {
            if (runManager != null)
            {
                runManager.RunStarted -= HandleRunStarted;
                runManager.RunEnded -= HandleRunEnded;
            }

            GameplayRuntimeEvents.EnemyKilled -= HandleEnemyKilled;
            GameplayRuntimeEvents.EnemySeen -= HandleEnemySeen;
            GameplayRuntimeEvents.RoomResolved -= HandleRoomResolved;
            GameplayRuntimeEvents.RoomEntered -= HandleRoomEntered;
            GameplayRuntimeEvents.RoomCleared -= HandleRoomCleared;
            GameplayRuntimeEvents.RoomRewardPhaseCompleted -= HandleRoomRewardPhaseCompleted;
            GameplayRuntimeEvents.PlayerDamaged -= HandlePlayerDamaged;
            GameplayRuntimeEvents.PlayerBombSpent -= HandlePlayerBombSpent;
            GameplayRuntimeEvents.SecretRoomRevealed -= HandleSecretRoomRevealed;
            GameplayRuntimeEvents.SpecialRoomRewardManifested -= HandleSpecialRoomRewardManifested;
            GameplayRuntimeEvents.SpecialRoomDealOffered -= HandleSpecialRoomDealOffered;
            GameplayRuntimeEvents.SpecialRoomDealPurchased -= HandleSpecialRoomDealPurchased;
            GameplayRuntimeEvents.SpecialRoomDealDeclined -= HandleSpecialRoomDealDeclined;

            if (playerItemManager != null)
            {
                playerItemManager.ItemAcquired -= HandleItemAcquired;
            }

            if (playerActiveItemController != null)
            {
                playerActiveItemController.ActiveItemEquipped -= HandleActiveItemEquipped;
            }
        }

        private void HandleRunStarted(RunContext _)
        {
            ResolveReferences();
            TryBindPlayerItemManager();
            TryBindPlayerActiveItemController();
            _newAchievementDescriptions.Clear();
            _newUnlockDescriptions.Clear();
            _newClearMarks.Clear();
            _currentRunItemIds.Clear();
            _currentRunEnemyKills.Clear();
            _currentRunChallengeClearRankCounts.Clear();
            _currentRunChallengePressureTierCounts.Clear();
            _currentRunBombsSpent = 0;
            _currentRoomTookDamage = false;
            _runStartRealtime = Time.realtimeSinceStartup;
            _currentRunCharacterId = characterProfileManager != null
                ? characterProfileManager.SelectedCharacterId
                : "default";
        }

        private void HandleRunEnded(RunContext context, RunEndReason endReason)
        {
            _lastCompletedRunSeconds = CurrentRunElapsedSeconds;
            _runStartRealtime = 0f;

            _progression.TotalRuns++;
            _progression.BestFloor = Mathf.Max(_progression.BestFloor, context != null ? context.CurrentFloorIndex : 1);
            _progression.TotalRoomsCleared += context != null ? Mathf.Max(0, context.TotalClearedRoomCount) : 0;
            _progression.TotalRoomsResolved += context != null ? Mathf.Max(0, context.TotalResolvedRoomCount) : 0;
            _progression.TotalBossRoomsCleared += context != null ? Mathf.Max(0, context.BossRoomClearCount) : 0;
            _progression.TotalEnemyKills += context != null ? Mathf.Max(0, context.EnemyKillCount) : 0;
            _progression.TotalRunSeconds += _lastCompletedRunSeconds;

            if (playerInventory != null)
            {
                _progression.TotalCoinsCollected += Mathf.Max(0, playerInventory.Coins);
                _progression.TotalKeysCollected += Mathf.Max(0, playerInventory.Keys);
                _progression.TotalBombsCollected += Mathf.Max(0, playerInventory.Bombs);
            }

            if (endReason == RunEndReason.Victory)
            {
                _progression.TotalWins++;
                _progression.CurrentWinStreak++;
                _progression.BestWinStreak = Mathf.Max(_progression.BestWinStreak, _progression.CurrentWinStreak);
            }
            else if (endReason == RunEndReason.Abandoned)
            {
                _progression.TotalAbandons++;
                _progression.CurrentWinStreak = 0;
            }
            else if (endReason == RunEndReason.Defeat)
            {
                _progression.TotalDefeats++;
                _progression.CurrentWinStreak = 0;
            }

            CharacterProgressionRecord characterRecord = GetOrCreateCharacterRecord(_currentRunCharacterId);
            characterRecord.Runs++;
            characterRecord.TotalRunSeconds += _lastCompletedRunSeconds;
            characterRecord.BestFloor = Mathf.Max(characterRecord.BestFloor, context != null ? context.CurrentFloorIndex : 1);
            characterRecord.BossKills += context != null ? Mathf.Max(0, context.BossRoomClearCount) : 0;
            characterRecord.EnemyKills += context != null ? Mathf.Max(0, context.EnemyKillCount) : 0;

            if (endReason == RunEndReason.Victory)
            {
                characterRecord.Wins++;
                characterRecord.CurrentWinStreak++;
                characterRecord.BestWinStreak = Mathf.Max(characterRecord.BestWinStreak, characterRecord.CurrentWinStreak);
                AddClearMark(characterRecord, "run_victory", _newClearMarks);
                AddClearMark(characterRecord, ResolveBossClearMark(context != null ? context.CurrentFloorIndex : 1), _newClearMarks);
                if (context != null && context.IsHardMode)
                {
                    AddClearMark(characterRecord, "hard_victory", _newClearMarks);
                }
            }
            else if (endReason == RunEndReason.Defeat)
            {
                characterRecord.Defeats++;
                characterRecord.CurrentWinStreak = 0;
            }
            else if (endReason == RunEndReason.Abandoned)
            {
                characterRecord.CurrentWinStreak = 0;
            }

            SyncIndexesToSaveData();
            EvaluateAchievements(endReason, characterRecord);
            ProgressionChanged?.Invoke();
        }

        private void HandleEnemyKilled(EnemyKilledSignal signal)
        {
            if (string.IsNullOrWhiteSpace(signal.EnemyId))
            {
                return;
            }

            _seenEnemyIds.Add(signal.EnemyId);
            IncrementCounter(_enemyKillCounts, signal.EnemyId, 1);
            IncrementCounter(_currentRunEnemyKills, signal.EnemyId, 1);
            EvaluateCurrentRunAchievements();
        }

        private void HandleEnemySeen(CuteIssac.Enemy.EnemyHealth enemyHealth)
        {
            if (enemyHealth == null || string.IsNullOrWhiteSpace(enemyHealth.EnemyId))
            {
                return;
            }

            if (_seenEnemyIds.Add(enemyHealth.EnemyId))
            {
                EvaluateCurrentRunAchievements();
            }
        }

        private void HandleRoomResolved(RoomResolvedSignal signal)
        {
            IncrementCounter(_roomTypeClearCounts, signal.RoomType.ToString(), 1);
            EvaluateCurrentRunAchievements();
        }

        private void HandleRoomEntered(RoomEnteredSignal _)
        {
            _currentRoomTookDamage = false;
        }

        private void HandleRoomCleared(RoomClearSignal signal)
        {
            if (!signal.HadCombatEncounter)
            {
                _currentRoomTookDamage = false;
                return;
            }

            if (!_currentRoomTookDamage)
            {
                _progression.NoHitCombatRoomClears++;
            }

            if (signal.Room != null && signal.Room.RoomType == RoomType.Boss)
            {
                _progression.TotalBossRoomsCleared++;
                AddClearMark(GetOrCreateCharacterRecord(_currentRunCharacterId), ResolveBossClearMark(ResolveCurrentFloorIndex()), _newClearMarks);

                if (_currentRunBombsSpent <= 0)
                {
                    _progression.BossClearsWithoutBombs++;
                }
            }
            else if (signal.Room != null && signal.Room.RoomType == RoomType.Challenge)
            {
                AddClearMark(GetOrCreateCharacterRecord(_currentRunCharacterId), "challenge_room", _newClearMarks);
            }

            _currentRoomTookDamage = false;
            EvaluateCurrentRunAchievements();
        }

        private void HandleRoomRewardPhaseCompleted(RoomRewardPhaseSignal signal)
        {
            if (signal.RoomType != RoomType.Challenge)
            {
                return;
            }

            if (signal.ChallengeClearRank != ChallengeClearRank.None)
            {
                string rankId = signal.ChallengeClearRank.ToString();
                IncrementCounter(_challengeClearRankCounts, rankId, 1);
                IncrementCounter(_currentRunChallengeClearRankCounts, rankId, 1);
                AddClearMark(GetOrCreateCharacterRecord(_currentRunCharacterId), $"challenge_rank_{signal.ChallengeClearRank.ToString().ToLowerInvariant()}", _newClearMarks);
            }

            if (signal.ChallengePressureTier != ChallengePressureTier.None)
            {
                string pressureId = signal.ChallengePressureTier.ToString();
                IncrementCounter(_challengePressureTierCounts, pressureId, 1);
                IncrementCounter(_currentRunChallengePressureTierCounts, pressureId, 1);
                AddClearMark(GetOrCreateCharacterRecord(_currentRunCharacterId), $"challenge_pressure_{signal.ChallengePressureTier.ToString().ToLowerInvariant()}", _newClearMarks);
            }

            EvaluateCurrentRunAchievements();
        }

        private void HandleSecretRoomRevealed(SecretRoomRevealedSignal _)
        {
            AddClearMark(GetOrCreateCharacterRecord(_currentRunCharacterId), "secret_route", _newClearMarks);
            EvaluateCurrentRunAchievements();
        }

        private void HandleSpecialRoomRewardManifested(SpecialRoomRewardManifestedSignal signal)
        {
            if (!signal.IsValid)
            {
                return;
            }

            IncrementCounter(_specialRewardOfferCounts, $"room:{signal.RoomType}", 1);

            if (!string.IsNullOrWhiteSpace(signal.RuleId))
            {
                IncrementCounter(_specialRewardOfferCounts, $"rule:{signal.RuleId}", 1);
            }

            if (signal.DealType != SpecialRoomDealType.None)
            {
                IncrementCounter(_specialRewardOfferCounts, $"deal:{signal.DealType}", 1);
                AddClearMark(GetOrCreateCharacterRecord(_currentRunCharacterId), $"deal_{signal.DealType.ToString().ToLowerInvariant()}", _newClearMarks);
            }

            EvaluateCurrentRunAchievements();
        }

        private void HandleSpecialRoomDealOffered(SpecialRoomDealOfferedSignal signal)
        {
            if (!signal.IsValid)
            {
                return;
            }

            IncrementCounter(_specialDealOfferCounts, $"deal:{signal.DealType}", 1);

            if (!string.IsNullOrWhiteSpace(signal.RuleId))
            {
                IncrementCounter(_specialDealOfferCounts, $"rule:{signal.RuleId}", 1);
            }

            IncrementCounter(_specialDealOfferCounts, $"room:{signal.RoomType}", 1);
            EvaluateCurrentRunAchievements();
        }

        private void HandleSpecialRoomDealPurchased(SpecialRoomDealPurchasedSignal signal)
        {
            if (!signal.IsValid)
            {
                return;
            }

            IncrementCounter(_specialDealPurchaseCounts, $"deal:{signal.DealType}", 1);

            if (!string.IsNullOrWhiteSpace(signal.RuleId))
            {
                IncrementCounter(_specialDealPurchaseCounts, $"rule:{signal.RuleId}", 1);
            }

            IncrementCounter(_specialDealPurchaseCounts, $"room:{signal.RoomType}", 1);
            AddClearMark(GetOrCreateCharacterRecord(_currentRunCharacterId), $"deal_{signal.DealType.ToString().ToLowerInvariant()}_accepted", _newClearMarks);
            EvaluateCurrentRunAchievements();
        }

        private void HandleSpecialRoomDealDeclined(SpecialRoomDealDeclinedSignal signal)
        {
            if (!signal.IsValid)
            {
                return;
            }

            IncrementCounter(_specialDealDeclineCounts, $"deal:{signal.DealType}", 1);

            if (!string.IsNullOrWhiteSpace(signal.RuleId))
            {
                IncrementCounter(_specialDealDeclineCounts, $"rule:{signal.RuleId}", 1);
            }

            IncrementCounter(_specialDealDeclineCounts, $"room:{signal.RoomType}", 1);
            AddClearMark(GetOrCreateCharacterRecord(_currentRunCharacterId), $"deal_{signal.DealType.ToString().ToLowerInvariant()}_declined", _newClearMarks);
            EvaluateCurrentRunAchievements();
        }

        private void HandlePlayerDamaged(PlayerDamagedSignal _)
        {
            _currentRoomTookDamage = true;
        }

        private void HandlePlayerBombSpent(PlayerBombSpentSignal signal)
        {
            _currentRunBombsSpent += Mathf.Max(0, signal.Amount);
        }

        private void HandleItemAcquired(ItemData itemData)
        {
            if (itemData == null || string.IsNullOrWhiteSpace(itemData.ItemId))
            {
                return;
            }

            _discoveredItemIds.Add(itemData.ItemId);
            _currentRunItemIds.Add(itemData.ItemId);
            EvaluateCurrentRunAchievements();
        }

        private void HandleActiveItemEquipped(ActiveItemData activeItemData)
        {
            if (activeItemData == null || string.IsNullOrWhiteSpace(activeItemData.ItemId))
            {
                return;
            }

            _discoveredItemIds.Add(activeItemData.ItemId);
            _currentRunItemIds.Add(activeItemData.ItemId);
            EvaluateCurrentRunAchievements();
        }

        private bool EvaluateAchievements(RunEndReason endReason, CharacterProgressionRecord characterRecord)
        {
            _unlockedThisEvaluation.Clear();

            for (int index = 0; index < _achievementDefinitions.Count; index++)
            {
                AchievementData achievement = _achievementDefinitions[index];

                if (achievement == null
                    || string.IsNullOrWhiteSpace(achievement.AchievementId)
                    || _completedAchievementIds.Contains(achievement.AchievementId)
                    || !IsAchievementMet(achievement, endReason, characterRecord))
                {
                    continue;
                }

                _completedAchievementIds.Add(achievement.AchievementId);
                _achievementCompletedAtUtc[achievement.AchievementId] = DateTime.UtcNow.ToString("O");
                _unlockedThisEvaluation.Add(achievement);

                AddUniqueLabel(_newAchievementDescriptions, string.IsNullOrWhiteSpace(achievement.DisplayName)
                    ? achievement.AchievementId
                    : achievement.DisplayName);

                if (TryGrantAchievementReward(achievement))
                {
                    AddUniqueLabel(_newUnlockDescriptions, UnlockDisplayNameResolver.ResolveUnlockedLabel(achievement.RewardUnlockKey));
                }
            }

            SyncIndexesToSaveData();

            if (_unlockedThisEvaluation.Count > 0)
            {
                AchievementsUnlocked?.Invoke(_unlockedThisEvaluation);
                return true;
            }

            return false;
        }

        private void GrantCompletedAchievementRewards()
        {
            if (unlockManager == null || _completedAchievementIds.Count == 0)
            {
                return;
            }

            for (int index = 0; index < _achievementDefinitions.Count; index++)
            {
                AchievementData achievement = _achievementDefinitions[index];

                if (achievement == null ||
                    string.IsNullOrWhiteSpace(achievement.AchievementId) ||
                    !_completedAchievementIds.Contains(achievement.AchievementId))
                {
                    continue;
                }

                TryGrantAchievementReward(achievement);
            }
        }

        private bool TryGrantAchievementReward(AchievementData achievement)
        {
            return achievement != null
                && achievement.RewardTargetType == AchievementRewardTargetType.UnlockKey
                && !string.IsNullOrWhiteSpace(achievement.RewardUnlockKey)
                && unlockManager != null
                && unlockManager.GrantUnlockKey(achievement.RewardUnlockKey);
        }

        private bool IsAchievementMet(AchievementData achievement, RunEndReason endReason, CharacterProgressionRecord characterRecord)
        {
            return achievement.ConditionType switch
            {
                AchievementConditionType.TotalRuns => _progression.TotalRuns >= achievement.RequiredCount,
                AchievementConditionType.TotalWins => _progression.TotalWins >= achievement.RequiredCount,
                AchievementConditionType.ReachFloor => _progression.BestFloor >= achievement.RequiredCount,
                AchievementConditionType.BossClearCount => _progression.TotalBossRoomsCleared >= achievement.RequiredCount,
                AchievementConditionType.NoHitRoomClearCount => _progression.NoHitCombatRoomClears >= achievement.RequiredCount,
                AchievementConditionType.BossClearWithoutBombCount => _progression.BossClearsWithoutBombs >= achievement.RequiredCount,
                AchievementConditionType.RoomTypeClearCount => _roomTypeClearCounts.TryGetValue(achievement.RequiredId, out int clears) && clears >= achievement.RequiredCount,
                AchievementConditionType.EnemySeen => _seenEnemyIds.Contains(achievement.RequiredId),
                AchievementConditionType.EnemyKillCount => _enemyKillCounts.TryGetValue(achievement.RequiredId, out int kills) && kills >= achievement.RequiredCount,
                AchievementConditionType.ItemDiscovered => _discoveredItemIds.Contains(achievement.RequiredId),
                AchievementConditionType.CharacterWin => endReason == RunEndReason.Victory && characterRecord != null && MatchesId(characterRecord.CharacterId, achievement.RequiredId),
                AchievementConditionType.CharacterClearMark => characterRecord != null && characterRecord.ClearMarks.Contains(achievement.RequiredId),
                AchievementConditionType.SpecialRewardOfferCount => _specialRewardOfferCounts.TryGetValue(achievement.RequiredId, out int offers) && offers >= achievement.RequiredCount,
                AchievementConditionType.SpecialDealOfferCount => _specialDealOfferCounts.TryGetValue(achievement.RequiredId, out int dealOffers) && dealOffers >= achievement.RequiredCount,
                AchievementConditionType.SpecialDealPurchaseCount => _specialDealPurchaseCounts.TryGetValue(achievement.RequiredId, out int purchases) && purchases >= achievement.RequiredCount,
                AchievementConditionType.SpecialDealDeclineCount => _specialDealDeclineCounts.TryGetValue(achievement.RequiredId, out int declines) && declines >= achievement.RequiredCount,
                AchievementConditionType.ChallengeClearRankCount => _challengeClearRankCounts.TryGetValue(achievement.RequiredId, out int ranks) && ranks >= achievement.RequiredCount,
                AchievementConditionType.ChallengePressureTierCount => _challengePressureTierCounts.TryGetValue(achievement.RequiredId, out int pressureTiers) && pressureTiers >= achievement.RequiredCount,
                AchievementConditionType.TotalEnemyKills => _progression.TotalEnemyKills >= achievement.RequiredCount,
                AchievementConditionType.CurrentWinStreak => _progression.CurrentWinStreak >= achievement.RequiredCount,
                AchievementConditionType.BestWinStreak => _progression.BestWinStreak >= achievement.RequiredCount,
                _ => false
            };
        }

        private void EvaluateCurrentRunAchievements()
        {
            CharacterProgressionRecord characterRecord = GetOrCreateCharacterRecord(_currentRunCharacterId);

            if (EvaluateAchievements(RunEndReason.None, characterRecord))
            {
                ProgressionChanged?.Invoke();
            }
        }

        private void ResolveReferences()
        {
            if (runManager == null)
            {
                runManager = GetComponent<RunManager>();
            }

            if (characterProfileManager == null)
            {
                characterProfileManager = GetComponent<CharacterProfileManager>();
            }

            if (unlockManager == null)
            {
                unlockManager = GetComponent<UnlockManager>();
            }

            if (playerInventory == null)
            {
                playerInventory = FindFirstObjectByType<PlayerInventory>(FindObjectsInactive.Exclude);
            }

            if (playerActiveItemController == null)
            {
                playerActiveItemController = FindFirstObjectByType<PlayerActiveItemController>(FindObjectsInactive.Exclude);
            }
        }

        private void TryBindPlayerItemManager()
        {
            if (playerItemManager == null)
            {
                playerItemManager = FindFirstObjectByType<PlayerItemManager>(FindObjectsInactive.Exclude);
            }

            if (playerItemManager == null)
            {
                return;
            }

            playerItemManager.ItemAcquired -= HandleItemAcquired;
            playerItemManager.ItemAcquired += HandleItemAcquired;

            TryBindPlayerActiveItemController();
        }

        private void TryBindPlayerActiveItemController()
        {
            if (playerActiveItemController == null)
            {
                playerActiveItemController = FindFirstObjectByType<PlayerActiveItemController>(FindObjectsInactive.Exclude);
            }

            if (playerActiveItemController == null)
            {
                return;
            }

            playerActiveItemController.ActiveItemEquipped -= HandleActiveItemEquipped;
            playerActiveItemController.ActiveItemEquipped += HandleActiveItemEquipped;
        }

        private void LoadAchievementDefinitions()
        {
            _achievementDefinitions.Clear();

            if (string.IsNullOrWhiteSpace(achievementResourcePath))
            {
                return;
            }

            AchievementData[] loadedDefinitions = Resources.LoadAll<AchievementData>(achievementResourcePath);

            for (int index = 0; index < loadedDefinitions.Length; index++)
            {
                if (loadedDefinitions[index] != null)
                {
                    _achievementDefinitions.Add(loadedDefinitions[index]);
                }
            }
        }

        private CharacterProgressionRecord GetOrCreateCharacterRecord(string characterId)
        {
            if (string.IsNullOrWhiteSpace(characterId))
            {
                characterId = "default";
            }

            if (_characters.TryGetValue(characterId, out CharacterProgressionRecord record))
            {
                return record;
            }

            record = new CharacterProgressionRecord
            {
                CharacterId = characterId
            };
            _characters[characterId] = record;
            return record;
        }

        private void RebuildIndexes()
        {
            _completedAchievementIds.Clear();
            _achievementCompletedAtUtc.Clear();
            _discoveredItemIds.Clear();
            _seenEnemyIds.Clear();
            _enemyKillCounts.Clear();
            _roomTypeClearCounts.Clear();
            _challengeClearRankCounts.Clear();
            _challengePressureTierCounts.Clear();
            _specialRewardOfferCounts.Clear();
            _specialDealOfferCounts.Clear();
            _specialDealPurchaseCounts.Clear();
            _specialDealDeclineCounts.Clear();
            _characters.Clear();

            AddListToSet(_progression.CompletedAchievementIds, _completedAchievementIds);
            if (_progression.CompletedAchievements != null)
            {
                for (int index = 0; index < _progression.CompletedAchievements.Count; index++)
                {
                    AchievementCompletionRecord record = _progression.CompletedAchievements[index];
                    if (record == null || string.IsNullOrWhiteSpace(record.AchievementId))
                    {
                        continue;
                    }

                    _completedAchievementIds.Add(record.AchievementId);
                    _achievementCompletedAtUtc[record.AchievementId] = record.CompletedAtUtc ?? string.Empty;
                }
            }

            foreach (string achievementId in _completedAchievementIds)
            {
                if (!string.IsNullOrWhiteSpace(achievementId) && !_achievementCompletedAtUtc.ContainsKey(achievementId))
                {
                    _achievementCompletedAtUtc[achievementId] = string.Empty;
                }
            }
            AddListToSet(_progression.DiscoveredItemIds, _discoveredItemIds);
            AddListToSet(_progression.SeenEnemyIds, _seenEnemyIds);

            if (_progression.EnemyKillCounts != null)
            {
                for (int index = 0; index < _progression.EnemyKillCounts.Count; index++)
                {
                    MetaProgressionCounterRecord record = _progression.EnemyKillCounts[index];
                    if (record != null && !string.IsNullOrWhiteSpace(record.Id))
                    {
                        _enemyKillCounts[record.Id] = Mathf.Max(0, record.Count);
                    }
                }
            }

            if (_progression.RoomTypeClearCounts != null)
            {
                for (int index = 0; index < _progression.RoomTypeClearCounts.Count; index++)
                {
                    MetaProgressionCounterRecord record = _progression.RoomTypeClearCounts[index];
                    if (record != null && !string.IsNullOrWhiteSpace(record.Id))
                    {
                        _roomTypeClearCounts[record.Id] = Mathf.Max(0, record.Count);
                    }
                }
            }

            if (_progression.ChallengeClearRankCounts != null)
            {
                for (int index = 0; index < _progression.ChallengeClearRankCounts.Count; index++)
                {
                    MetaProgressionCounterRecord record = _progression.ChallengeClearRankCounts[index];
                    if (record != null && !string.IsNullOrWhiteSpace(record.Id))
                    {
                        _challengeClearRankCounts[record.Id] = Mathf.Max(0, record.Count);
                    }
                }
            }

            if (_progression.ChallengePressureTierCounts != null)
            {
                for (int index = 0; index < _progression.ChallengePressureTierCounts.Count; index++)
                {
                    MetaProgressionCounterRecord record = _progression.ChallengePressureTierCounts[index];
                    if (record != null && !string.IsNullOrWhiteSpace(record.Id))
                    {
                        _challengePressureTierCounts[record.Id] = Mathf.Max(0, record.Count);
                    }
                }
            }

            if (_progression.SpecialRewardOfferCounts != null)
            {
                for (int index = 0; index < _progression.SpecialRewardOfferCounts.Count; index++)
                {
                    MetaProgressionCounterRecord record = _progression.SpecialRewardOfferCounts[index];
                    if (record != null && !string.IsNullOrWhiteSpace(record.Id))
                    {
                        _specialRewardOfferCounts[record.Id] = Mathf.Max(0, record.Count);
                    }
                }
            }

            if (_progression.SpecialDealOfferCounts != null)
            {
                for (int index = 0; index < _progression.SpecialDealOfferCounts.Count; index++)
                {
                    MetaProgressionCounterRecord record = _progression.SpecialDealOfferCounts[index];
                    if (record != null && !string.IsNullOrWhiteSpace(record.Id))
                    {
                        _specialDealOfferCounts[record.Id] = Mathf.Max(0, record.Count);
                    }
                }
            }

            if (_progression.SpecialDealPurchaseCounts != null)
            {
                for (int index = 0; index < _progression.SpecialDealPurchaseCounts.Count; index++)
                {
                    MetaProgressionCounterRecord record = _progression.SpecialDealPurchaseCounts[index];
                    if (record != null && !string.IsNullOrWhiteSpace(record.Id))
                    {
                        _specialDealPurchaseCounts[record.Id] = Mathf.Max(0, record.Count);
                    }
                }
            }

            if (_progression.SpecialDealDeclineCounts != null)
            {
                for (int index = 0; index < _progression.SpecialDealDeclineCounts.Count; index++)
                {
                    MetaProgressionCounterRecord record = _progression.SpecialDealDeclineCounts[index];
                    if (record != null && !string.IsNullOrWhiteSpace(record.Id))
                    {
                        _specialDealDeclineCounts[record.Id] = Mathf.Max(0, record.Count);
                    }
                }
            }

            if (_progression.CharacterRecords != null)
            {
                for (int index = 0; index < _progression.CharacterRecords.Count; index++)
                {
                    CharacterProgressionRecord record = _progression.CharacterRecords[index];
                    if (record != null && !string.IsNullOrWhiteSpace(record.CharacterId))
                    {
                        _characters[record.CharacterId] = record;
                    }
                }
            }
        }

        private void SyncIndexesToSaveData()
        {
            ReplaceListFromSet(_progression.CompletedAchievementIds, _completedAchievementIds);
            _progression.CompletedAchievements ??= new List<AchievementCompletionRecord>();
            ReplaceAchievementCompletionRecords(_progression.CompletedAchievements, _achievementCompletedAtUtc);
            ReplaceListFromSet(_progression.DiscoveredItemIds, _discoveredItemIds);
            ReplaceListFromSet(_progression.SeenEnemyIds, _seenEnemyIds);
            ReplaceCounterRecords(_progression.EnemyKillCounts, _enemyKillCounts);
            ReplaceCounterRecords(_progression.RoomTypeClearCounts, _roomTypeClearCounts);
            _progression.ChallengeClearRankCounts ??= new List<MetaProgressionCounterRecord>();
            ReplaceCounterRecords(_progression.ChallengeClearRankCounts, _challengeClearRankCounts);
            _progression.ChallengePressureTierCounts ??= new List<MetaProgressionCounterRecord>();
            ReplaceCounterRecords(_progression.ChallengePressureTierCounts, _challengePressureTierCounts);
            ReplaceCounterRecords(_progression.SpecialRewardOfferCounts, _specialRewardOfferCounts);
            _progression.SpecialDealOfferCounts ??= new List<MetaProgressionCounterRecord>();
            ReplaceCounterRecords(_progression.SpecialDealOfferCounts, _specialDealOfferCounts);
            _progression.SpecialDealPurchaseCounts ??= new List<MetaProgressionCounterRecord>();
            ReplaceCounterRecords(_progression.SpecialDealPurchaseCounts, _specialDealPurchaseCounts);
            _progression.SpecialDealDeclineCounts ??= new List<MetaProgressionCounterRecord>();
            ReplaceCounterRecords(_progression.SpecialDealDeclineCounts, _specialDealDeclineCounts);
            _progression.CharacterRecords.Clear();

            foreach (CharacterProgressionRecord record in _characters.Values)
            {
                if (record != null)
                {
                    _progression.CharacterRecords.Add(record);
                }
            }
        }

        private static void CopyProgression(MetaProgressionSaveData source, MetaProgressionSaveData destination)
        {
            source ??= new MetaProgressionSaveData();
            destination.TotalRuns = Mathf.Max(0, source.TotalRuns);
            destination.TotalWins = Mathf.Max(0, source.TotalWins);
            destination.TotalDefeats = Mathf.Max(0, source.TotalDefeats);
            destination.TotalAbandons = Mathf.Max(0, source.TotalAbandons);
            destination.BestFloor = Mathf.Max(0, source.BestFloor);
            destination.TotalRoomsCleared = Mathf.Max(0, source.TotalRoomsCleared);
            destination.TotalRoomsResolved = Mathf.Max(0, source.TotalRoomsResolved);
            destination.TotalBossRoomsCleared = Mathf.Max(0, source.TotalBossRoomsCleared);
            destination.TotalEnemyKills = Mathf.Max(0, source.TotalEnemyKills);
            destination.CurrentWinStreak = Mathf.Max(0, source.CurrentWinStreak);
            destination.BestWinStreak = Mathf.Max(destination.CurrentWinStreak, source.BestWinStreak);
            destination.NoHitCombatRoomClears = Mathf.Max(0, source.NoHitCombatRoomClears);
            destination.BossClearsWithoutBombs = Mathf.Max(0, source.BossClearsWithoutBombs);
            destination.TotalCoinsCollected = Mathf.Max(0, source.TotalCoinsCollected);
            destination.TotalKeysCollected = Mathf.Max(0, source.TotalKeysCollected);
            destination.TotalBombsCollected = Mathf.Max(0, source.TotalBombsCollected);
            destination.TotalRunSeconds = Mathf.Max(0f, source.TotalRunSeconds);
            ReplaceStringList(destination.DiscoveredItemIds, source.DiscoveredItemIds);
            ReplaceStringList(destination.SeenEnemyIds, source.SeenEnemyIds);
            ReplaceStringList(destination.CompletedAchievementIds, source.CompletedAchievementIds);
            ReplaceAchievementCompletionRecordList(destination.CompletedAchievements, source.CompletedAchievements);
            ReplaceCounterRecordList(destination.EnemyKillCounts, source.EnemyKillCounts);
            ReplaceCounterRecordList(destination.RoomTypeClearCounts, source.RoomTypeClearCounts);
            ReplaceCounterRecordList(destination.ChallengeClearRankCounts, source.ChallengeClearRankCounts);
            ReplaceCounterRecordList(destination.ChallengePressureTierCounts, source.ChallengePressureTierCounts);
            ReplaceCounterRecordList(destination.SpecialRewardOfferCounts, source.SpecialRewardOfferCounts);
            ReplaceCounterRecordList(destination.SpecialDealOfferCounts, source.SpecialDealOfferCounts);
            ReplaceCounterRecordList(destination.SpecialDealPurchaseCounts, source.SpecialDealPurchaseCounts);
            ReplaceCounterRecordList(destination.SpecialDealDeclineCounts, source.SpecialDealDeclineCounts);
            ReplaceCharacterRecordList(destination.CharacterRecords, source.CharacterRecords);
        }

        private static void AddClearMark(CharacterProgressionRecord record, string mark, List<string> newMarks = null)
        {
            if (record == null || string.IsNullOrWhiteSpace(mark))
            {
                return;
            }

            record.ClearMarks ??= new List<string>();
            if (!record.ClearMarks.Contains(mark))
            {
                record.ClearMarks.Add(mark);
                if (newMarks != null && !newMarks.Contains(mark))
                {
                    newMarks.Add(mark);
                }
            }
        }

        private static void AddUniqueLabel(List<string> destination, string label)
        {
            if (destination == null || string.IsNullOrWhiteSpace(label))
            {
                return;
            }

            if (!destination.Contains(label))
            {
                destination.Add(label);
            }
        }

        private int ResolveCurrentFloorIndex()
        {
            return runManager != null && runManager.CurrentContext.HasActiveRun
                ? runManager.CurrentContext.CurrentFloorIndex
                : 1;
        }

        private static string ResolveBossClearMark(int floorIndex)
        {
            return floorIndex switch
            {
                1 => "floor_1_boss",
                2 => "floor_2_boss",
                _ => "final_boss"
            };
        }

        private static void IncrementCounter(Dictionary<string, int> dictionary, string key, int amount)
        {
            if (dictionary == null || string.IsNullOrWhiteSpace(key))
            {
                return;
            }

            dictionary.TryGetValue(key, out int current);
            dictionary[key] = Mathf.Max(0, current + amount);
        }

        private static bool MatchesId(string left, string right)
        {
            return string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
        }

        private static void AddListToSet(List<string> source, HashSet<string> destination)
        {
            if (source == null || destination == null)
            {
                return;
            }

            for (int index = 0; index < source.Count; index++)
            {
                if (!string.IsNullOrWhiteSpace(source[index]))
                {
                    destination.Add(source[index]);
                }
            }
        }

        private static void ReplaceListFromSet(List<string> destination, HashSet<string> source)
        {
            destination.Clear();
            foreach (string value in source)
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    destination.Add(value);
                }
            }
        }

        private static void ReplaceCounterRecords(List<MetaProgressionCounterRecord> destination, Dictionary<string, int> source)
        {
            destination.Clear();
            foreach (KeyValuePair<string, int> pair in source)
            {
                if (!string.IsNullOrWhiteSpace(pair.Key))
                {
                    destination.Add(new MetaProgressionCounterRecord { Id = pair.Key, Count = Mathf.Max(0, pair.Value) });
                }
            }
        }

        private string BuildCurrentRunChallengeSummaryText()
        {
            bool hasRank = _currentRunChallengeClearRankCounts.Count > 0;
            bool hasPressure = _currentRunChallengePressureTierCounts.Count > 0;

            if (!hasRank && !hasPressure)
            {
                return string.Empty;
            }

            StringBuilder builder = new();
            AppendCounterSummary(builder, "Rank", _currentRunChallengeClearRankCounts);
            AppendCounterSummary(builder, "Pressure", _currentRunChallengePressureTierCounts);
            return builder.ToString();
        }

        private static void AppendCounterSummary(StringBuilder builder, string label, Dictionary<string, int> counters)
        {
            if (builder == null || counters == null || counters.Count == 0)
            {
                return;
            }

            if (builder.Length > 0)
            {
                builder.Append(" / ");
            }

            builder.Append(label);
            builder.Append(' ');
            bool appendedAny = false;

            foreach (KeyValuePair<string, int> pair in counters)
            {
                if (string.IsNullOrWhiteSpace(pair.Key) || pair.Value <= 0)
                {
                    continue;
                }

                if (appendedAny)
                {
                    builder.Append(", ");
                }

                builder.Append(pair.Key);
                builder.Append('x');
                builder.Append(pair.Value);
                appendedAny = true;
            }
        }

        private static void ReplaceStringList(List<string> destination, List<string> source)
        {
            destination.Clear();
            if (source == null)
            {
                return;
            }

            for (int index = 0; index < source.Count; index++)
            {
                if (!string.IsNullOrWhiteSpace(source[index]))
                {
                    destination.Add(source[index]);
                }
            }
        }

        private static void ReplaceAchievementCompletionRecords(List<AchievementCompletionRecord> destination, Dictionary<string, string> source)
        {
            destination.Clear();
            foreach (KeyValuePair<string, string> pair in source)
            {
                if (!string.IsNullOrWhiteSpace(pair.Key))
                {
                    destination.Add(new AchievementCompletionRecord
                    {
                        AchievementId = pair.Key,
                        CompletedAtUtc = pair.Value ?? string.Empty
                    });
                }
            }
        }

        private static void ReplaceAchievementCompletionRecordList(List<AchievementCompletionRecord> destination, List<AchievementCompletionRecord> source)
        {
            destination.Clear();
            if (source == null)
            {
                return;
            }

            for (int index = 0; index < source.Count; index++)
            {
                AchievementCompletionRecord record = source[index];
                if (record != null && !string.IsNullOrWhiteSpace(record.AchievementId))
                {
                    destination.Add(new AchievementCompletionRecord
                    {
                        AchievementId = record.AchievementId,
                        CompletedAtUtc = record.CompletedAtUtc ?? string.Empty
                    });
                }
            }
        }

        private static void ReplaceCounterRecordList(List<MetaProgressionCounterRecord> destination, List<MetaProgressionCounterRecord> source)
        {
            destination.Clear();
            if (source == null)
            {
                return;
            }

            for (int index = 0; index < source.Count; index++)
            {
                MetaProgressionCounterRecord record = source[index];
                if (record != null && !string.IsNullOrWhiteSpace(record.Id))
                {
                    destination.Add(new MetaProgressionCounterRecord { Id = record.Id, Count = Mathf.Max(0, record.Count) });
                }
            }
        }

        private static void ReplaceCharacterRecordList(List<CharacterProgressionRecord> destination, List<CharacterProgressionRecord> source)
        {
            destination.Clear();
            if (source == null)
            {
                return;
            }

            for (int index = 0; index < source.Count; index++)
            {
                CharacterProgressionRecord record = source[index];
                if (record == null || string.IsNullOrWhiteSpace(record.CharacterId))
                {
                    continue;
                }

                CharacterProgressionRecord copy = new()
                {
                    CharacterId = record.CharacterId,
                    Runs = Mathf.Max(0, record.Runs),
                    Wins = Mathf.Max(0, record.Wins),
                    Defeats = Mathf.Max(0, record.Defeats),
                    BestFloor = Mathf.Max(0, record.BestFloor),
                    BossKills = Mathf.Max(0, record.BossKills),
                    EnemyKills = Mathf.Max(0, record.EnemyKills),
                    CurrentWinStreak = Mathf.Max(0, record.CurrentWinStreak),
                    BestWinStreak = Mathf.Max(0, Mathf.Max(record.CurrentWinStreak, record.BestWinStreak)),
                    TotalRunSeconds = Mathf.Max(0f, record.TotalRunSeconds)
                };
                ReplaceStringList(copy.ClearMarks, record.ClearMarks);
                destination.Add(copy);
            }
        }
    }
}
