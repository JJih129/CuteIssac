using System;
using System.Collections.Generic;
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

        public event Action<RoomController> CombatStarted;
        public event Action<RoomController> RoomEntered;
        public event Action<RoomController> RoomCleared;

        public string RoomId => roomId;
        public RoomState State { get; private set; } = RoomState.Idle;
        public bool IsCurrentRoom { get; private set; }
        public int AliveEnemyCount { get; private set; }
        public int RegisteredEnemyCount => _registeredEnemies.Count;
        public IReadOnlyList<RoomDoor> RoomDoors => roomDoors;
        public Vector3 DefaultPlayerSpawnPosition => defaultPlayerSpawnPoint != null ? defaultPlayerSpawnPoint.position : transform.position;
        public Vector3 CameraFocusPosition => cameraFocusPoint != null ? cameraFocusPoint.position : transform.position;

        private readonly List<EnemyHealth> _registeredEnemies = new();
        private Vector3 _lastEnemyDeathPosition;
        private bool _hasLastEnemyDeathPosition;
        private bool _isShuttingDown;

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

            PlayerController playerController = FindFirstObjectByType<PlayerController>(FindObjectsInactive.Exclude);

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

            if (other.GetComponentInParent<PlayerController>() == null)
            {
                return;
            }

            EnterRoom();
        }

        public void RegisterEnemy(EnemyHealth enemy)
        {
            if (enemy == null || _registeredEnemies.Contains(enemy))
            {
                return;
            }

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

            int index = _registeredEnemies.IndexOf(enemy);

            if (index < 0)
            {
                return;
            }

            _registeredEnemies.RemoveAt(index);
            UnsubscribeEnemy(enemy);

            if (!enemy.IsDead && AliveEnemyCount > 0)
            {
                AliveEnemyCount--;
                TryResolveRoomClear();
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

            RoomEntered?.Invoke(this);

            if (roomEnemySpawner == null || !roomEnemySpawner.CanStartCombat())
            {
                ClearRoom();
                return;
            }

            State = RoomState.Combat;
            roomEnemySpawner.HandleCombatStarted(this);

            if (AliveEnemyCount <= 0)
            {
                ClearRoom();
                return;
            }

            SetDoorsLocked(true);
            CombatStarted?.Invoke(this);
        }

        [ContextMenu("Clear Room")]
        public void ClearRoom()
        {
            if (_isShuttingDown || State == RoomState.Cleared)
            {
                return;
            }

            State = RoomState.Cleared;
            SetDoorsLocked(false);
            roomRewardSpawner?.HandleRoomCleared(this);
            RoomCleared?.Invoke(this);
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
                ClearRoom();
            }
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
    }
}
