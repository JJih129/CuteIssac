using CuteIssac.Core.Gameplay;
using CuteIssac.Data.Dungeon;
using UnityEngine;

namespace CuteIssac.Room
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RoomController))]
    public sealed class RoomMomentumRewardController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private RoomController roomController;

        [Header("Scoring")]
        [SerializeField] [Min(0f)] private float matchingFormationScore = 1f;
        [SerializeField] [Min(0f)] private float focusExecutionScore = 0.8f;
        [SerializeField] [Min(0f)] private float criticalExecutionScore = 1.45f;
        [SerializeField] [Min(0f)] private float chainStepScore = 0.22f;
        [SerializeField] [Min(0f)] private float sustainSecondScore = 0.95f;
        [SerializeField] [Min(0f)] private float restoredHealthScore = 0.9f;

        [Header("Reward Thresholds")]
        [SerializeField] [Min(0f)] private float rewardSelectionThreshold = 2.6f;
        [SerializeField] [Min(0f)] private float itemRollThreshold = 4.85f;
        [SerializeField] [Range(0.5f, 1.5f)] private float challengeRoomScoreScale = 1.08f;
        [SerializeField] [Range(0.5f, 1.5f)] private float eliteRoomScoreScale = 1.12f;

        [Header("Feedback")]
        [SerializeField] private Color fallbackAccentColor = new(1f, 0.84f, 0.36f, 1f);

        private float _performanceScore;
        private float _sustainedSeconds;
        private float _restoredHealth;
        private int _executionCount;
        private int _criticalExecutionCount;
        private bool _combatActive;
        private bool _hasCombatData;
        private bool _bonusConsumed;
        private Color _lastAccentColor = Color.white;

        public bool HasLiveCombatProjection => _combatActive && _hasCombatData && !_bonusConsumed;
        public int ExecutionCount => _executionCount;
        public int CriticalExecutionCount => _criticalExecutionCount;
        public Color AccentColor => _lastAccentColor.a > 0.01f ? _lastAccentColor : fallbackAccentColor;

        private void Awake()
        {
            ResolveReferences();
        }

        private void OnEnable()
        {
            ResolveReferences();

            if (roomController != null)
            {
                roomController.CombatStarted -= HandleCombatStarted;
                roomController.CombatStarted += HandleCombatStarted;
                roomController.RoomCleared -= HandleRoomEnded;
                roomController.RoomCleared += HandleRoomEnded;
                roomController.NonCombatResolved -= HandleNonCombatResolved;
                roomController.NonCombatResolved += HandleNonCombatResolved;
                roomController.StateChanged -= HandleRoomStateChanged;
                roomController.StateChanged += HandleRoomStateChanged;
            }

            GameplayRuntimeEvents.MomentumExecuted -= HandleMomentumExecuted;
            GameplayRuntimeEvents.MomentumExecuted += HandleMomentumExecuted;
        }

        private void OnDisable()
        {
            GameplayRuntimeEvents.MomentumExecuted -= HandleMomentumExecuted;

            if (roomController != null)
            {
                roomController.CombatStarted -= HandleCombatStarted;
                roomController.RoomCleared -= HandleRoomEnded;
                roomController.NonCombatResolved -= HandleNonCombatResolved;
                roomController.StateChanged -= HandleRoomStateChanged;
            }
        }

        public bool TryConsumeRewardBonus(
            RoomController resolvedRoom,
            bool allowNonCombatRewards,
            out int bonusRewardSelections,
            out int bonusItemRolls,
            out string title,
            out string subtitle,
            out Color accentColor)
        {
            bonusRewardSelections = 0;
            bonusItemRolls = 0;
            title = string.Empty;
            subtitle = string.Empty;
            accentColor = fallbackAccentColor;

            if (allowNonCombatRewards
                || resolvedRoom == null
                || resolvedRoom != roomController
                || !_hasCombatData
                || _bonusConsumed)
            {
                return false;
            }

            float scaledScore = _performanceScore * ResolveRoomScoreScale(resolvedRoom.RoomType);
            bonusRewardSelections = scaledScore >= rewardSelectionThreshold ? 1 : 0;
            bonusItemRolls = scaledScore >= itemRollThreshold ? 1 : 0;

            if (bonusRewardSelections <= 0 && bonusItemRolls <= 0)
            {
                return false;
            }

            _bonusConsumed = true;
            accentColor = _lastAccentColor.a > 0.01f ? _lastAccentColor : fallbackAccentColor;
            title = bonusItemRolls > 0 ? "Momentum Payout" : "Momentum Reward";
            subtitle = BuildSubtitle(bonusRewardSelections, bonusItemRolls);
            return true;
        }

        public bool TryGetLiveCombatProjection(
            out int bonusRewardSelections,
            out int bonusItemRolls,
            out float rewardSelectionProgress,
            out float itemRollProgress,
            out int executionCount,
            out int criticalExecutionCount,
            out Color accentColor)
        {
            bonusRewardSelections = 0;
            bonusItemRolls = 0;
            rewardSelectionProgress = 0f;
            itemRollProgress = 0f;
            executionCount = _executionCount;
            criticalExecutionCount = _criticalExecutionCount;
            accentColor = AccentColor;

            if (!HasLiveCombatProjection || roomController == null)
            {
                return false;
            }

            float scaledScore = ResolveCurrentScaledScore();
            bonusRewardSelections = scaledScore >= rewardSelectionThreshold ? 1 : 0;
            bonusItemRolls = scaledScore >= itemRollThreshold ? 1 : 0;
            rewardSelectionProgress = rewardSelectionThreshold > 0.01f
                ? Mathf.Clamp01(scaledScore / rewardSelectionThreshold)
                : 1f;
            itemRollProgress = itemRollThreshold > 0.01f
                ? Mathf.Clamp01(scaledScore / itemRollThreshold)
                : 1f;
            return true;
        }

        private void HandleCombatStarted(RoomController startedRoom)
        {
            if (startedRoom != null && startedRoom == roomController)
            {
                ResetTracking();
                _combatActive = true;
            }
        }

        private void HandleRoomEnded(RoomController clearedRoom)
        {
            if (clearedRoom == roomController)
            {
                _combatActive = false;
            }
        }

        private void HandleNonCombatResolved(RoomController resolvedRoom)
        {
            if (resolvedRoom == roomController)
            {
                ResetTracking();
            }
        }

        private void HandleRoomStateChanged(RoomController changedRoom, RoomState state)
        {
            if (changedRoom != roomController)
            {
                return;
            }

            if (state == RoomState.Idle || state == RoomState.Entered)
            {
                ResetTracking();
            }
        }

        private void HandleMomentumExecuted(MomentumExecutionSignal signal)
        {
            if (!_combatActive || signal.Room != roomController || !signal.IsValid)
            {
                return;
            }

            _hasCombatData = true;
            _executionCount++;
            _sustainedSeconds += Mathf.Max(0f, signal.SustainedDuration);
            _restoredHealth += Mathf.Max(0f, signal.RestoredHealth);
            _lastAccentColor = signal.AccentColor;

            float score = 0f;
            if (signal.MatchesActiveFormation)
            {
                score += matchingFormationScore;
            }

            if (signal.IsCriticalExecution)
            {
                _criticalExecutionCount++;
                score += criticalExecutionScore;
            }
            else if (signal.IsPriorityExecution)
            {
                score += focusExecutionScore;
            }

            score += Mathf.Max(0, signal.ChainCount - 1) * chainStepScore;
            score += Mathf.Max(0f, signal.SustainedDuration) * sustainSecondScore;
            score += Mathf.Max(0f, signal.RestoredHealth) * restoredHealthScore;
            _performanceScore += score;
        }

        private float ResolveRoomScoreScale(RoomType roomType)
        {
            return roomType switch
            {
                RoomType.Challenge => challengeRoomScoreScale,
                RoomType.Boss or RoomType.MiniBoss => eliteRoomScoreScale,
                _ => 1f
            };
        }

        private float ResolveCurrentScaledScore()
        {
            return roomController != null
                ? _performanceScore * ResolveRoomScoreScale(roomController.RoomType)
                : _performanceScore;
        }

        private string BuildSubtitle(int bonusRewardSelections, int bonusItemRolls)
        {
            string rewardSegment = bonusRewardSelections > 0 ? $"+Reward {bonusRewardSelections}" : string.Empty;
            string itemSegment = bonusItemRolls > 0 ? $"+Item {bonusItemRolls}" : string.Empty;
            string executionSegment = _criticalExecutionCount > 0
                ? $"{_executionCount} cuts / {_criticalExecutionCount} execute"
                : $"{_executionCount} cuts";

            if (!string.IsNullOrEmpty(rewardSegment) && !string.IsNullOrEmpty(itemSegment))
            {
                return $"{rewardSegment} / {itemSegment} · {executionSegment}";
            }

            if (!string.IsNullOrEmpty(rewardSegment))
            {
                return $"{rewardSegment} · {executionSegment}";
            }

            return $"{itemSegment} · {executionSegment}";
        }

        private void ResetTracking()
        {
            _performanceScore = 0f;
            _sustainedSeconds = 0f;
            _restoredHealth = 0f;
            _executionCount = 0;
            _criticalExecutionCount = 0;
            _combatActive = false;
            _hasCombatData = false;
            _bonusConsumed = false;
            _lastAccentColor = Color.white;
        }

        private void ResolveReferences()
        {
            if (roomController == null)
            {
                roomController = GetComponent<RoomController>();
            }
        }
    }
}
