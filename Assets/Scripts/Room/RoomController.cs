using System;
using System.Collections.Generic;
using CuteIssac.Combat;
using CuteIssac.Core.Audio;
using CuteIssac.Core.Gameplay;
using CuteIssac.Data.Dungeon;
using CuteIssac.Dungeon;
using CuteIssac.Enemy;
using CuteIssac.Player;
using UnityEngine;

namespace CuteIssac.Room
{
    /// <summary>
    /// Owns room encounter state.
    /// It detects player entry, seals the room for combat, tracks registered enemies, and unlocks doors on clear.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider2D))]
    public sealed class RoomController : MonoBehaviour
    {
        [Header("Room Setup")]
        [SerializeField] private string roomId = "Room";
        [SerializeField] private RoomType roomType = RoomType.Normal;
        [SerializeField] private Collider2D roomBoundsTrigger;
        [SerializeField] private RoomDoor[] roomDoors;
        [SerializeField] private List<EnemyHealth> initialEnemies = new();
        [SerializeField] private RoomEnemySpawner roomEnemySpawner;

        [Header("Navigation")]
        [SerializeField] private Transform defaultPlayerSpawnPoint;
        [SerializeField] private Transform cameraFocusPoint;
        [SerializeField] private GameObject[] roomContentRoots;
        [SerializeField] private bool hideWhenNotCurrent = true;

        [Header("Rewards")]
        [SerializeField] private RoomRewardSpawner roomRewardSpawner;

        [Header("Clear Cleanup")]
        [SerializeField] private bool clearEnemyProjectilesOnCombatClear = true;

        public event Action<RoomController> CombatStarted;
        public event Action<RoomController> RoomEntered;
        public event Action<RoomResolvedSignal> RoomResolved;
        public event Action<RoomController> RoomCleared;
        public event Action<RoomController> NonCombatResolved;
        public event Action<RoomController, int> RewardPhaseCompleted;
        public event Action<RoomController, RoomState> StateChanged;

        public string RoomId => roomId;
        public RoomType RoomType => roomType;
        public RoomState State { get; private set; } = RoomState.Idle;
        public bool HasResolvedRoom => State == RoomState.Resolved || State == RoomState.Rewarded;
        public int LastRewardSpawnCount { get; private set; }
        public bool HasRewardContent => LastRewardSpawnCount > 0;
        public bool IsCurrentRoom { get; private set; }
        public int AliveEnemyCount { get; private set; }
        public float CurrentCombatDuration => State == RoomState.Combat && _hasCombatStartTimestamp
            ? Mathf.Max(0f, Time.time - _combatStartedAt)
            : _lastCombatDuration;
        public float LastCombatDuration => _lastCombatDuration;
        public int RegisteredEnemyCount => _registeredEnemies.Count;
        public IReadOnlyList<RoomDoor> RoomDoors => roomDoors;
        public Vector3 DefaultPlayerSpawnPosition => defaultPlayerSpawnPoint != null ? defaultPlayerSpawnPoint.position : transform.position;
        public Vector3 CameraFocusPosition => cameraFocusPoint != null
            ? cameraFocusPoint.position
            : roomBoundsTrigger != null
                ? roomBoundsTrigger.bounds.center
                : transform.position;
        public Bounds RoomBounds => roomBoundsTrigger != null
            ? roomBoundsTrigger.bounds
            : new Bounds(transform.position, Vector3.zero);

        private readonly List<EnemyHealth> _registeredEnemies = new();
        private readonly Dictionary<EnemyHealth, int> _registeredEnemyIndices = new();
        private Vector3 _lastEnemyDeathPosition;
        private bool _hasLastEnemyDeathPosition;
        private bool _isShuttingDown;
        private bool _hadCombatEncounter;
        private float _combatStartedAt;
        private float _lastCombatDuration;
        private bool _hasCombatStartTimestamp;

        private void Awake()
        {
            if (roomBoundsTrigger == null)
            {
                roomBoundsTrigger = GetComponent<Collider2D>();
            }

            if (roomDoors == null || roomDoors.Length == 0)
            {
                roomDoors = GetComponentsInChildren<RoomDoor>(true);
            }

            for (int i = 0; i < roomDoors.Length; i++)
            {
                if (roomDoors[i] != null)
                {
                    roomDoors[i].ValidateDirectionFromTransform(false);
                }
            }

            if (roomBoundsTrigger != null && !roomBoundsTrigger.isTrigger)
            {
                Debug.LogWarning("RoomController works best with a trigger collider for room entry detection.", this);
            }

            SyncInitialEnemies();
            BindDoors();
            SetCurrentRoom(false);
            SetDoorsLocked(false);

            if (roomEnemySpawner == null)
            {
                roomEnemySpawner = GetComponent<RoomEnemySpawner>();
            }

            if (roomRewardSpawner == null)
            {
                roomRewardSpawner = GetComponent<RoomRewardSpawner>();
            }
        }

        private void Start()
        {
            if (State != RoomState.Idle || roomBoundsTrigger == null)
            {
                return;
            }

            PlayerController playerController = ResolveActivePlayerController();

            if (playerController != null && roomBoundsTrigger.OverlapPoint(playerController.transform.position))
            {
                EnterRoom();
            }
        }

        private void OnDestroy()
        {
            _isShuttingDown = true;

            for (int i = 0; i < _registeredEnemies.Count; i++)
            {
                UnsubscribeEnemy(_registeredEnemies[i]);
            }
        }

        private void OnApplicationQuit()
        {
            _isShuttingDown = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (State != RoomState.Idle)
            {
                return;
            }

            if (!IsPlayerCollider(other))
            {
                return;
            }

            EnterRoom();
        }

        public void RegisterEnemy(EnemyHealth enemy)
        {
            if (enemy == null || _registeredEnemyIndices.ContainsKey(enemy))
            {
                return;
            }

            _registeredEnemyIndices.Add(enemy, _registeredEnemies.Count);
            _registeredEnemies.Add(enemy);
            SubscribeEnemy(enemy);

            if (!enemy.IsDead)
            {
                AliveEnemyCount++;
            }
        }

        public void UnregisterEnemy(EnemyHealth enemy)
        {
            if (enemy == null)
            {
                return;
            }

            if (!_registeredEnemyIndices.TryGetValue(enemy, out int index))
            {
                return;
            }

            RemoveRegisteredEnemyAt(index);
            UnsubscribeEnemy(enemy);

            if (!enemy.IsDead && AliveEnemyCount > 0)
            {
                AliveEnemyCount--;
                TryResolveRoomClear();
            }
        }

        public void CollectAliveEnemies(List<EnemyHealth> targetBuffer)
        {
            if (targetBuffer == null)
            {
                return;
            }

            targetBuffer.Clear();

            for (int i = 0; i < _registeredEnemies.Count; i++)
            {
                EnemyHealth enemy = _registeredEnemies[i];
                if (enemy == null || enemy.IsDead)
                {
                    continue;
                }

                targetBuffer.Add(enemy);
            }
        }

        public void SetCurrentRoom(bool isCurrent)
        {
            IsCurrentRoom = isCurrent;

            if (!hideWhenNotCurrent)
            {
                return;
            }

            for (int i = 0; i < roomContentRoots.Length; i++)
            {
                if (roomContentRoots[i] != null)
                {
                    roomContentRoots[i].SetActive(isCurrent);
                }
            }
        }

        [ContextMenu("Enter Room")]
        public void EnterRoom()
        {
            if (_isShuttingDown || State != RoomState.Idle)
            {
                return;
            }

            SetState(RoomState.Entered);
            LastRewardSpawnCount = 0;
            RoomEntered?.Invoke(this);

            if (roomEnemySpawner == null || !roomEnemySpawner.CanStartCombat())
            {
                _hadCombatEncounter = false;
                ResolveNonCombatRoom();
                return;
            }

            _hadCombatEncounter = true;
            _combatStartedAt = Time.time;
            _hasCombatStartTimestamp = true;
            _lastCombatDuration = 0f;
            SetState(RoomState.Combat);
            SetDoorsLocked(true);
            roomEnemySpawner.HandleCombatStarted(this);

            if (AliveEnemyCount <= 0)
            {
                ClearRoom();
                return;
            }

            CombatStarted?.Invoke(this);
        }

        [ContextMenu("Clear Room")]
        public void ClearRoom()
        {
            if (_isShuttingDown || HasResolvedRoom)
            {
                return;
            }

            FinalizeCombatDuration();
            SetState(RoomState.Resolved);
            SetDoorsLocked(false);
            ClearEnemyProjectilesOnRoomClear();
            RoomRewardPhaseSummary rewardSummary = roomRewardSpawner != null
                ? roomRewardSpawner.HandleRoomCleared(this)
                : default;
            RoomResolvedSignal resolution = BuildResolutionSignal(true);
            GameplayRuntimeEvents.RaiseRoomResolved(resolution);
            GameAudioEvents.Raise(GameAudioEventType.RoomCleared, CameraFocusPosition);
            GameplayRuntimeEvents.RaiseRoomCleared(new RoomClearSignal(this, _hadCombatEncounter));
            RoomResolved?.Invoke(resolution);
            RoomCleared?.Invoke(this);
            CompleteRewardPhase(rewardSummary);
        }

        public void ResolveNonCombatRoom()
        {
            if (_isShuttingDown || HasResolvedRoom)
            {
                return;
            }

            _lastCombatDuration = 0f;
            _hasCombatStartTimestamp = false;
            SetState(RoomState.Resolved);
            SetDoorsLocked(false);
            RoomRewardPhaseSummary rewardSummary = roomRewardSpawner != null
                ? roomRewardSpawner.HandleRoomResolvedWithoutCombat(this)
                : default;
            RoomResolvedSignal resolution = BuildResolutionSignal(false);
            GameplayRuntimeEvents.RaiseRoomResolved(resolution);
            RoomResolved?.Invoke(resolution);
            NonCombatResolved?.Invoke(this);
            CompleteRewardPhase(rewardSummary);
        }

        public void ConfigureRuntimeMetadata(string runtimeRoomId, RoomType runtimeRoomType)
        {
            if (!string.IsNullOrWhiteSpace(runtimeRoomId))
            {
                roomId = runtimeRoomId;
            }

            roomType = runtimeRoomType;
        }

        public void DebugForceCombatState()
        {
            if (_isShuttingDown || State == RoomState.Combat || HasResolvedRoom)
            {
                return;
            }

            _hadCombatEncounter = true;
            _combatStartedAt = Time.time;
            _hasCombatStartTimestamp = true;
            _lastCombatDuration = 0f;
            SetState(RoomState.Combat);
            SetDoorsLocked(true);

            if (roomEnemySpawner != null)
            {
                roomEnemySpawner.HandleCombatStarted(this);
            }

            if (AliveEnemyCount <= 0)
            {
                ClearRoom();
                return;
            }

            CombatStarted?.Invoke(this);
        }

        public void DebugBeginInjectedCombatState()
        {
            if (_isShuttingDown || State == RoomState.Combat || HasResolvedRoom)
            {
                return;
            }

            _hadCombatEncounter = true;
            _combatStartedAt = Time.time;
            _hasCombatStartTimestamp = true;
            _lastCombatDuration = 0f;
            SetState(RoomState.Combat);
            SetDoorsLocked(true);
            CombatStarted?.Invoke(this);
        }

        public void ApplyRestoredResolvedState(bool hadRewardContent, bool hadCombatEncounter)
        {
            if (_isShuttingDown)
            {
                return;
            }

            _hadCombatEncounter = hadCombatEncounter;
            LastRewardSpawnCount = hadRewardContent ? Mathf.Max(1, LastRewardSpawnCount) : 0;
            _hasCombatStartTimestamp = false;
            roomEnemySpawner?.RestoreEncounterResolvedState();
            roomRewardSpawner?.RestoreRewardSpawnState(hadRewardContent);
            SetDoorsLocked(false);
            SetState(RoomState.Rewarded);
        }

        /// <summary>
        /// Reward spawners can prefer the last defeated enemy position so drops feel tied to the clear.
        /// Falls back to the room transform when no enemy death has been observed yet.
        /// </summary>
        public bool TryGetLastEnemyDeathPosition(out Vector3 position)
        {
            position = _lastEnemyDeathPosition;
            return _hasLastEnemyDeathPosition;
        }

        public int AliveChampionEnemyCount => CountAliveChampionEnemies();

        public bool TryGetChampionEncounterSummary(out string variantLabel, out Color accentColor, out int aliveChampionCount)
        {
            variantLabel = string.Empty;
            accentColor = Color.white;
            aliveChampionCount = 0;

            for (int i = 0; i < _registeredEnemies.Count; i++)
            {
                EnemyHealth enemyHealth = _registeredEnemies[i];
                if (enemyHealth == null || enemyHealth.IsDead)
                {
                    continue;
                }

                ChampionEnemyModifier championModifier = enemyHealth.GetComponent<ChampionEnemyModifier>();
                if (championModifier == null || !championModifier.IsChampion)
                {
                    continue;
                }

                aliveChampionCount++;
                if (string.IsNullOrWhiteSpace(variantLabel))
                {
                    variantLabel = championModifier.VariantLabel;
                    accentColor = championModifier.VariantAccentColor;
                }
            }

            return aliveChampionCount > 0;
        }

        public bool TryGetUpcomingChallengeWaveThreat(
            out int nextWaveNumber,
            out int totalWaveCount,
            out int enemyCount,
            out int guaranteedChampionCount,
            out float championChanceBonus)
        {
            nextWaveNumber = 0;
            totalWaveCount = 0;
            enemyCount = 0;
            guaranteedChampionCount = 0;
            championChanceBonus = 0f;

            return roomEnemySpawner != null
                && roomEnemySpawner.TryGetUpcomingChallengeWaveThreat(
                    out nextWaveNumber,
                    out totalWaveCount,
                    out enemyCount,
                    out guaranteedChampionCount,
                    out championChanceBonus);
        }

        public bool TryGetChallengeRewardPressure(
            out int totalWaveCount,
            out int reinforcementEnemyCount,
            out int guaranteedChampionCount,
            out float championChanceBonus)
        {
            totalWaveCount = 0;
            reinforcementEnemyCount = 0;
            guaranteedChampionCount = 0;
            championChanceBonus = 0f;

            return roomEnemySpawner != null
                && roomEnemySpawner.TryGetChallengeRewardPressure(
                    out totalWaveCount,
                    out reinforcementEnemyCount,
                    out guaranteedChampionCount,
                    out championChanceBonus);
        }

        /// <summary>
        /// Navigation and exploration systems can use the room trigger as the single source of truth for room membership.
        /// </summary>
        public bool ContainsWorldPoint(Vector3 worldPosition)
        {
            return roomBoundsTrigger != null
                ? roomBoundsTrigger.OverlapPoint(worldPosition)
                : false;
        }

        [ContextMenu("Refresh Registered Enemies")]
        public void RefreshRegisteredEnemies()
        {
            for (int i = _registeredEnemies.Count - 1; i >= 0; i--)
            {
                UnsubscribeEnemy(_registeredEnemies[i]);
            }

            _registeredEnemies.Clear();
            _registeredEnemyIndices.Clear();
            AliveEnemyCount = 0;
            SyncInitialEnemies();
        }

        /// <summary>
        /// Generated dungeon instantiation resolves scene door links by cardinal direction.
        /// Returning false is a setup issue on the room prefab, not a traversal problem.
        /// </summary>
        public bool TryGetDoor(RoomDirection direction, out RoomDoor roomDoor)
        {
            for (int i = 0; i < roomDoors.Length; i++)
            {
                RoomDoor candidate = roomDoors[i];

                if (candidate != null && candidate.DoorDirection == direction)
                {
                    roomDoor = candidate;
                    return true;
                }
            }

            roomDoor = null;
            return false;
        }

        private void SyncInitialEnemies()
        {
            for (int i = 0; i < initialEnemies.Count; i++)
            {
                RegisterEnemy(initialEnemies[i]);
            }
        }

        private static PlayerController ResolveActivePlayerController()
        {
            return PlayerRegistry.ActiveController != null
                ? PlayerRegistry.ActiveController
                : FindFirstObjectByType<PlayerController>(FindObjectsInactive.Exclude);
        }

        private static bool IsPlayerCollider(Collider2D other)
        {
            if (other == null)
            {
                return false;
            }

            PlayerController playerController = PlayerRegistry.ActiveController;
            if (playerController == null)
            {
                return other.GetComponentInParent<PlayerController>() != null;
            }

            Transform playerTransform = playerController.transform;
            Transform otherTransform = other.transform;
            return otherTransform == playerTransform || otherTransform.IsChildOf(playerTransform);
        }

        private void RemoveRegisteredEnemyAt(int index)
        {
            int lastIndex = _registeredEnemies.Count - 1;
            EnemyHealth removedEnemy = _registeredEnemies[index];
            _registeredEnemyIndices.Remove(removedEnemy);

            if (index != lastIndex)
            {
                EnemyHealth movedEnemy = _registeredEnemies[lastIndex];
                _registeredEnemies[index] = movedEnemy;
                _registeredEnemyIndices[movedEnemy] = index;
            }

            _registeredEnemies.RemoveAt(lastIndex);
        }

        private void SubscribeEnemy(EnemyHealth enemy)
        {
            enemy.Died += HandleEnemyDied;
            enemy.DiedWithSource += HandleEnemyDiedWithSource;
        }

        private void UnsubscribeEnemy(EnemyHealth enemy)
        {
            if (enemy != null)
            {
                enemy.Died -= HandleEnemyDied;
                enemy.DiedWithSource -= HandleEnemyDiedWithSource;
            }
        }

        private void HandleEnemyDiedWithSource(EnemyHealth enemyHealth)
        {
            if (_isShuttingDown || enemyHealth == null)
            {
                return;
            }

            _lastEnemyDeathPosition = enemyHealth.transform.position;
            _hasLastEnemyDeathPosition = true;
        }

        private void HandleEnemyDied()
        {
            if (_isShuttingDown)
            {
                return;
            }

            if (AliveEnemyCount > 0)
            {
                AliveEnemyCount--;
            }

            TryResolveRoomClear();
        }

        private void TryResolveRoomClear()
        {
            if (State != RoomState.Combat)
            {
                return;
            }

            if (AliveEnemyCount <= 0)
            {
                if (roomEnemySpawner != null && roomEnemySpawner.TryAdvanceChallengeWave(this))
                {
                    return;
                }

                ClearRoom();
            }
        }

        private int CountAliveChampionEnemies()
        {
            int aliveChampionCount = 0;

            for (int i = 0; i < _registeredEnemies.Count; i++)
            {
                EnemyHealth enemyHealth = _registeredEnemies[i];
                if (enemyHealth == null || enemyHealth.IsDead)
                {
                    continue;
                }

                ChampionEnemyModifier championModifier = enemyHealth.GetComponent<ChampionEnemyModifier>();
                if (championModifier != null && championModifier.IsChampion)
                {
                    aliveChampionCount++;
                }
            }

            return aliveChampionCount;
        }

        private void SetDoorsLocked(bool locked)
        {
            for (int i = 0; i < roomDoors.Length; i++)
            {
                if (roomDoors[i] != null)
                {
                    if (locked)
                    {
                        roomDoors[i].Lock();
                    }
                    else
                    {
                        roomDoors[i].Unlock();
                    }
                }
            }
        }

        private void BindDoors()
        {
            for (int i = 0; i < roomDoors.Length; i++)
            {
                if (roomDoors[i] != null)
                {
                    roomDoors[i].BindOwner(this);
                }
            }
        }

        private RoomResolvedSignal BuildResolutionSignal(bool hadCombatEncounter)
        {
            return new RoomResolvedSignal(this, roomType, hadCombatEncounter);
        }

        private void CompleteRewardPhase(RoomRewardPhaseSummary rewardSummary)
        {
            LastRewardSpawnCount = Mathf.Max(0, rewardSummary.RewardCount);
            SetState(RoomState.Rewarded);
            GameplayRuntimeEvents.RaiseRoomRewardPhaseCompleted(new RoomRewardPhaseSignal(this, roomType, rewardSummary));
            RewardPhaseCompleted?.Invoke(this, LastRewardSpawnCount);
        }

        private void FinalizeCombatDuration()
        {
            if (!_hadCombatEncounter || !_hasCombatStartTimestamp)
            {
                _lastCombatDuration = 0f;
                return;
            }

            _lastCombatDuration = Mathf.Max(0f, Time.time - _combatStartedAt);
            _hasCombatStartTimestamp = false;
        }

        private void ClearEnemyProjectilesOnRoomClear()
        {
            if (!clearEnemyProjectilesOnCombatClear || !_hadCombatEncounter)
            {
                return;
            }

            EnemyProjectileLogic.DissipateAllActive(ProjectileImpactType.None);
        }

        private void SetState(RoomState nextState)
        {
            if (State == nextState)
            {
                return;
            }

            State = nextState;
            StateChanged?.Invoke(this, nextState);
        }
    }
}
