using System;
using CuteIssac.Core.Audio;
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
        [SerializeField] [Min(0f)] private float cameraLerpSpeed = 7.5f;
        [SerializeField] private bool constrainCameraToRoomBounds = true;

        public event Action<RoomController> CurrentRoomChanged;

        public RoomController CurrentRoom { get; private set; }

        private float _lastTransitionTime = float.NegativeInfinity;
        private Vector3 _cameraTargetPosition;

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
            GameAudioEvents.Raise(GameAudioEventType.DoorTraversed, nextRoom.CameraFocusPosition);
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

        public bool TryRestoreRoomState(RoomController targetRoom, Vector3 playerPosition, bool snapCamera = true)
        {
            if (targetRoom == null)
            {
                return false;
            }

            SetCurrentRoom(targetRoom);

            if (playerController != null)
            {
                playerController.transform.position = playerPosition;
            }

            if (snapCamera)
            {
                SnapCameraTo(targetRoom);
            }

            _lastTransitionTime = Time.unscaledTime;
            return true;
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
            SetCameraTarget(startingRoom);

            if (snapCameraOnTransition)
            {
                SnapCameraTo(startingRoom);
            }

            if (playerController != null && movePlayerToStartingRoom)
            {
                playerController.transform.position = startingRoom.DefaultPlayerSpawnPosition;
            }

            _lastTransitionTime = Time.unscaledTime;
        }

        private void LateUpdate()
        {
            ReconcileCurrentRoomWithPlayerPosition();

            if (CurrentRoom == null || targetCamera == null)
            {
                return;
            }

            SetCameraTarget(CurrentRoom);

            if (cameraLerpSpeed <= 0f)
            {
                targetCamera.transform.position = _cameraTargetPosition;
                return;
            }

            targetCamera.transform.position = Vector3.Lerp(
                targetCamera.transform.position,
                _cameraTargetPosition,
                1f - Mathf.Exp(-cameraLerpSpeed * Time.unscaledDeltaTime));
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
            SetCameraTarget(nextRoom);

            if (snapCameraOnTransition)
            {
                SnapCameraTo(nextRoom);
            }

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
            if (roomController == null || targetCamera == null)
            {
                return;
            }

            SetCameraTarget(roomController);
            targetCamera.transform.position = _cameraTargetPosition;
        }

        private void SetCameraTarget(RoomController roomController)
        {
            if (roomController == null || targetCamera == null)
            {
                return;
            }

            Vector3 desiredPosition = roomController.CameraFocusPosition;
            desiredPosition.z = targetCamera.transform.position.z;
            _cameraTargetPosition = constrainCameraToRoomBounds
                ? ClampCameraPositionToRoom(roomController, desiredPosition)
                : desiredPosition;
        }

        private Vector3 ClampCameraPositionToRoom(RoomController roomController, Vector3 desiredPosition)
        {
            Bounds roomBounds = roomController.RoomBounds;

            if (!targetCamera.orthographic)
            {
                return desiredPosition;
            }

            float verticalExtent = targetCamera.orthographicSize;
            float horizontalExtent = verticalExtent * targetCamera.aspect;
            Vector3 clampedPosition = desiredPosition;
            Vector3 roomCenter = roomBounds.center;

            float minX = roomBounds.min.x + horizontalExtent;
            float maxX = roomBounds.max.x - horizontalExtent;
            clampedPosition.x = minX <= maxX
                ? Mathf.Clamp(clampedPosition.x, minX, maxX)
                : roomCenter.x;

            float minY = roomBounds.min.y + verticalExtent;
            float maxY = roomBounds.max.y - verticalExtent;
            clampedPosition.y = minY <= maxY
                ? Mathf.Clamp(clampedPosition.y, minY, maxY)
                : roomCenter.y;

            clampedPosition.z = desiredPosition.z;
            return clampedPosition;
        }
    }
}
