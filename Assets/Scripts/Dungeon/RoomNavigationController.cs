using System;
using CuteIssac.Player;
using CuteIssac.Room;
using UnityEngine;

namespace CuteIssac.Dungeon
{
    /// <summary>
    /// Handles transitions between manually connected rooms.
    /// Keep navigation separate from RoomController so later dungeon generation can replace only the graph source.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RoomNavigationController : MonoBehaviour
    {
        [Header("Scene References")]
        [SerializeField] private PlayerController playerController;
        [SerializeField] private Camera targetCamera;
        [SerializeField] private RoomController[] rooms;
        [SerializeField] private RoomController startingRoom;

        [Header("Behavior")]
        [SerializeField] private bool movePlayerToStartingRoom = true;
        [SerializeField] private bool snapCameraOnTransition = true;
        [SerializeField] private bool hideNonCurrentRooms = true;
        [SerializeField] [Min(0f)] private float transitionCooldown = 0.15f;

        public event Action<RoomController> CurrentRoomChanged;

        public RoomController CurrentRoom { get; private set; }

        private float _lastTransitionTime = float.NegativeInfinity;

        private void Awake()
        {
            if (playerController == null)
            {
                playerController = FindFirstObjectByType<PlayerController>(FindObjectsInactive.Exclude);
            }

            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }

            if (startingRoom == null && rooms.Length > 0)
            {
                startingRoom = rooms[0];
            }

            ApplyInitialRoomState();
        }

        private void OnDestroy()
        {
            UnsubscribeFromRoomEvents();
        }

        /// <summary>
        /// Allows generated room sets to reuse the same navigation controller as manual layouts.
        /// The dungeon instantiator provides the spawned rooms and start room after scene creation.
        /// </summary>
        public void ConfigureGeneratedRooms(RoomController[] generatedRooms, RoomController generatedStartingRoom, PlayerController generatedPlayerController = null)
        {
            UnsubscribeFromRoomEvents();
            rooms = generatedRooms ?? Array.Empty<RoomController>();
            startingRoom = generatedStartingRoom;

            if (generatedPlayerController != null)
            {
                playerController = generatedPlayerController;
            }

            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }

            ApplyInitialRoomState();
        }

        public bool TryTraverse(RoomDoor fromDoor, PlayerController player)
        {
            if (fromDoor == null || player == null || fromDoor.IsLocked)
            {
                return false;
            }

            if (Time.unscaledTime - _lastTransitionTime < transitionCooldown)
            {
                return false;
            }

            RoomController nextRoom = fromDoor.ConnectedRoom;

            if (nextRoom == null)
            {
                return false;
            }

            RoomDoor nextDoor = fromDoor.ConnectedDoor;
            Vector3 nextPosition = nextDoor != null
                ? nextDoor.GetArrivalPosition()
                : nextRoom.DefaultPlayerSpawnPosition;

            SetCurrentRoom(nextRoom);
            player.transform.position = nextPosition;
            nextRoom.EnterRoom();
            SnapCameraTo(nextRoom);
            _lastTransitionTime = Time.unscaledTime;
            return true;
        }

        [ContextMenu("Go To Starting Room")]
        public void GoToStartingRoom()
        {
            if (startingRoom == null || playerController == null)
            {
                return;
            }

            SetCurrentRoom(startingRoom);

            if (movePlayerToStartingRoom)
            {
                playerController.transform.position = startingRoom.DefaultPlayerSpawnPosition;
            }

            startingRoom.EnterRoom();
        }

        private void ApplyInitialRoomState()
        {
            if (rooms == null || rooms.Length == 0)
            {
                return;
            }

            SubscribeToRoomEvents();

            for (int i = 0; i < rooms.Length; i++)
            {
                if (rooms[i] == null)
                {
                    continue;
                }

                bool isStartingRoom = rooms[i] == startingRoom;
                rooms[i].SetCurrentRoom(!hideNonCurrentRooms || isStartingRoom);
            }

            if (startingRoom == null)
            {
                return;
            }

            CurrentRoom = startingRoom;
            SnapCameraTo(startingRoom);

            if (playerController != null && movePlayerToStartingRoom)
            {
                playerController.transform.position = startingRoom.DefaultPlayerSpawnPosition;
            }

            _lastTransitionTime = Time.unscaledTime;
        }

        private void LateUpdate()
        {
            ReconcileCurrentRoomWithPlayerPosition();

            if (!snapCameraOnTransition || CurrentRoom == null || targetCamera == null)
            {
                return;
            }

            Vector3 expectedCameraPosition = CurrentRoom.CameraFocusPosition;
            expectedCameraPosition.z = targetCamera.transform.position.z;

            if ((targetCamera.transform.position - expectedCameraPosition).sqrMagnitude > 0.0001f)
            {
                targetCamera.transform.position = expectedCameraPosition;
            }
        }

        private void ReconcileCurrentRoomWithPlayerPosition()
        {
            if (playerController == null || rooms == null || rooms.Length == 0)
            {
                return;
            }

            Vector3 playerPosition = playerController.transform.position;

            for (int i = 0; i < rooms.Length; i++)
            {
                RoomController room = rooms[i];

                if (room == null || room == CurrentRoom)
                {
                    continue;
                }

                if (room.ContainsWorldPoint(playerPosition))
                {
                    SetCurrentRoom(room);
                    break;
                }
            }
        }

        private void SetCurrentRoom(RoomController nextRoom)
        {
            if (nextRoom == null)
            {
                return;
            }

            if (hideNonCurrentRooms)
            {
                for (int i = 0; i < rooms.Length; i++)
                {
                    if (rooms[i] != null)
                    {
                        rooms[i].SetCurrentRoom(rooms[i] == nextRoom);
                    }
                }
            }
            else
            {
                nextRoom.SetCurrentRoom(true);
            }

            CurrentRoom = nextRoom;
            SnapCameraTo(nextRoom);
            CurrentRoomChanged?.Invoke(nextRoom);
        }

        private void HandleRoomEntered(RoomController enteredRoom)
        {
            if (enteredRoom == null || enteredRoom == CurrentRoom)
            {
                return;
            }

            SetCurrentRoom(enteredRoom);
        }

        private void SubscribeToRoomEvents()
        {
            if (rooms == null)
            {
                return;
            }

            for (int i = 0; i < rooms.Length; i++)
            {
                if (rooms[i] != null)
                {
                    rooms[i].RoomEntered -= HandleRoomEntered;
                    rooms[i].RoomEntered += HandleRoomEntered;
                }
            }
        }

        private void UnsubscribeFromRoomEvents()
        {
            if (rooms == null)
            {
                return;
            }

            for (int i = 0; i < rooms.Length; i++)
            {
                if (rooms[i] != null)
                {
                    rooms[i].RoomEntered -= HandleRoomEntered;
                }
            }
        }

        private void SnapCameraTo(RoomController roomController)
        {
            if (!snapCameraOnTransition || roomController == null || targetCamera == null)
            {
                return;
            }

            Vector3 cameraPosition = roomController.CameraFocusPosition;
            cameraPosition.z = targetCamera.transform.position.z;
            targetCamera.transform.position = cameraPosition;
        }
    }
}
