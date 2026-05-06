using System;
using CuteIssac.Core.Audio;
using CuteIssac.Data.Dungeon;
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
        [SerializeField] [Min(0f)] private float cameraTraversalHandoffDuration = 0.26f;
        [SerializeField] [Min(0f)] private float cameraTraversalHandoffDistance = 0.88f;
        [SerializeField] [Min(0f)] private float guidedTraversalCameraBonus = 0.28f;

        public event Action<RoomController> CurrentRoomChanged;

        public RoomController CurrentRoom { get; private set; }

        private RoomTraversalGuidanceController _traversalGuidanceController;
        private RoomArrivalCueController _roomArrivalCueController;
        private RoomEntryFocusBeatController _roomEntryFocusBeatController;
        private PlayerTraversalFlowController _playerTraversalFlowController;
        private PlayerTraversalRouteReceiptPresentation _playerTraversalRouteReceiptPresentation;
        private float _cameraTraversalHandoffExpiresAt;
        private float _cameraTraversalHandoffActiveDuration;
        private Vector2 _cameraTraversalHandoffDirection = Vector2.right;
        private float _cameraTraversalHandoffDistanceActive;
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

            EnsureTraversalGuidanceController();
            EnsureRoomArrivalCueController();
            EnsureRoomEntryFocusBeatController();
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

            EnsureTraversalGuidanceController();
            EnsureRoomArrivalCueController();
            EnsureRoomEntryFocusBeatController();
            ApplyInitialRoomState();
        }

        private void EnsureTraversalGuidanceController()
        {
            RoomTraversalGuidanceController guidanceController = GetComponent<RoomTraversalGuidanceController>();

            if (guidanceController == null)
            {
                guidanceController = gameObject.AddComponent<RoomTraversalGuidanceController>();
            }

            _traversalGuidanceController = guidanceController;
            guidanceController.ConfigureRuntime(this, playerController);
        }

        private void EnsureRoomArrivalCueController()
        {
            RoomArrivalCueController arrivalCueController = GetComponent<RoomArrivalCueController>();

            if (arrivalCueController == null)
            {
                arrivalCueController = gameObject.AddComponent<RoomArrivalCueController>();
            }

            _roomArrivalCueController = arrivalCueController;
            arrivalCueController.ConfigureRuntime(this);
        }

        private void EnsureRoomEntryFocusBeatController()
        {
            RoomEntryFocusBeatController focusBeatController = GetComponent<RoomEntryFocusBeatController>();

            if (focusBeatController == null)
            {
                focusBeatController = gameObject.AddComponent<RoomEntryFocusBeatController>();
            }

            _roomEntryFocusBeatController = focusBeatController;
            focusBeatController.ConfigureRuntime(this);
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
            Color traversalAccentColor = RoomTraversalGuidanceController.ResolveRoomAccent(nextRoom.RoomType);
            RoomType traversalRoomType = nextRoom.RoomType;
            TraversalDoorGuidanceStyle traversalDoorStyle = fromDoor.HasUnpaidHealthEntryCost
                ? TraversalDoorGuidanceStyle.GuidedRisk
                : TraversalDoorGuidanceStyle.Guided;
            bool isGuidedRoute = _traversalGuidanceController != null
                && _traversalGuidanceController.TryResolveDoorTraversalGuide(
                    fromDoor,
                    out traversalAccentColor,
                    out traversalRoomType,
                    out traversalDoorStyle);
            string traversalRouteReasonTag = string.Empty;
            string traversalRouteHeadline = BuildTraversalRouteReceiptHeadline(string.Empty, traversalRoomType);
            string traversalRouteDetail = BuildTraversalRouteReceiptDetail(string.Empty, traversalRoomType);
            bool carriesChoicePlan = false;
            bool heldChoicePlan = false;
            string choicePlanReasonTag = string.Empty;
            string choicePlanCompareLabel = string.Empty;
            string choicePlanCarryLabel = string.Empty;

            if (isGuidedRoute
                && _traversalGuidanceController != null
                && _traversalGuidanceController.TryGetActiveGuidanceStatus(
                    fromDoor.OwnerRoom != null ? fromDoor.OwnerRoom : CurrentRoom,
                    out _,
                    out _,
                    out _,
                    out string guidanceDetail,
                    out _,
                    out _,
                    out string guidanceCompactTag,
                    out string guidanceReasonTag))
            {
                traversalRouteReasonTag = guidanceReasonTag;
                traversalRouteHeadline = BuildTraversalRouteReceiptHeadline(guidanceCompactTag, traversalRoomType);
                traversalRouteDetail = BuildTraversalRouteReceiptDetail(guidanceReasonTag, traversalRoomType, guidanceDetail);

                if (_traversalGuidanceController.TryGetActiveChoicePlanCarry(
                        fromDoor.OwnerRoom != null ? fromDoor.OwnerRoom : CurrentRoom,
                        out choicePlanReasonTag,
                        out heldChoicePlan,
                        out choicePlanCompareLabel))
                {
                    carriesChoicePlan = true;
                    choicePlanCarryLabel = heldChoicePlan ? "PLAN HELD" : "REROUTE";
                    string choicePlanDisplayLabel = ResolveChoicePlanCarryDisplayLabel(choicePlanCompareLabel, choicePlanReasonTag);
                    traversalRouteHeadline = BuildTraversalPlanCarryHeadline(guidanceCompactTag, traversalRoomType, heldChoicePlan);
                    traversalRouteDetail = BuildTraversalPlanCarryDetail(choicePlanDisplayLabel, traversalRouteDetail, heldChoicePlan);
                }
            }

            PlayerTraversalFlowController traversalFlowController = EnsureTraversalFlowController(player);
            PlayerTraversalRouteReceiptPresentation traversalRouteReceiptPresentation = EnsureTraversalRouteReceiptPresentation(player);
            DoorTraversalPresentation departurePresentation = EnsureDoorTraversalPresentation(fromDoor);
            DoorTraversalPresentation arrivalPresentation = EnsureDoorTraversalPresentation(nextDoor);
            TraversalGuidanceBeacon departureGuidanceBeacon = EnsureTraversalGuidanceBeacon(fromDoor);
            TraversalGuidanceBeacon arrivalGuidanceBeacon = EnsureTraversalGuidanceBeacon(nextDoor);
            Vector2 traversalDirection = ResolveTraversalDirection(fromDoor.DoorDirection);
            BeginCameraTraversalHandoff(traversalDirection, isGuidedRoute);
            departurePresentation?.PlayDeparturePulse(traversalAccentColor, fromDoor.DoorDirection, isGuidedRoute);

            if (isGuidedRoute && departureGuidanceBeacon != null)
            {
                departureGuidanceBeacon.ShowDoorGuidance(
                    traversalAccentColor,
                    fromDoor.DoorDirection,
                    cameraTraversalHandoffDuration + 0.96f,
                    traversalDoorStyle);
                departureGuidanceBeacon.PlayCommitAccepted();
            }

            SetCurrentRoom(nextRoom);
            player.transform.position = nextPosition;
            nextRoom.EnterRoom();
            SnapCameraTo(nextRoom);
            arrivalPresentation?.PlayArrivalSettle(
                traversalAccentColor,
                nextDoor != null ? nextDoor.DoorDirection : fromDoor.DoorDirection,
                isGuidedRoute,
                traversalRoomType);

            if (isGuidedRoute && arrivalGuidanceBeacon != null)
            {
                arrivalGuidanceBeacon.ShowDoorGuidance(
                    traversalAccentColor,
                    nextDoor != null ? nextDoor.DoorDirection : fromDoor.DoorDirection,
                    cameraTraversalHandoffDuration + 1.08f,
                    traversalDoorStyle);
                arrivalGuidanceBeacon.PlayCommitAccepted();
            }

            _roomArrivalCueController?.PresentArrivalCue(
                nextRoom,
                nextDoor != null ? nextDoor.DoorDirection : fromDoor.DoorDirection,
                isGuidedRoute,
                carriesChoicePlan,
                choicePlanCarryLabel);
            if (isGuidedRoute && traversalRouteReceiptPresentation != null)
            {
                traversalRouteReceiptPresentation.PlayRouteReceipt(
                    traversalDirection,
                    traversalAccentColor,
                    traversalRoomType,
                    traversalRouteHeadline,
                    traversalRouteDetail,
                    true);
            }

            _roomEntryFocusBeatController?.PresentEntryFocusBeat(
                nextRoom,
                nextDoor != null ? nextDoor.DoorDirection : fromDoor.DoorDirection,
                isGuidedRoute,
                traversalRouteReasonTag,
                carriesChoicePlan,
                choicePlanCarryLabel);
            traversalFlowController?.TriggerTraversalBurst(
                traversalDirection,
                traversalAccentColor,
                isGuidedRoute,
                traversalRoomType);
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

        /// <summary>
        /// Development-only room warp. It bypasses doors but keeps normal room activation,
        /// player placement, camera snap, and room entry hooks.
        /// </summary>
        public bool TryDebugWarpToRoomType(RoomType roomType)
        {
            if (rooms == null || rooms.Length == 0 || playerController == null)
            {
                return false;
            }

            for (int i = 0; i < rooms.Length; i++)
            {
                RoomController candidate = rooms[i];

                if (candidate == null || candidate.RoomType != roomType)
                {
                    continue;
                }

                SetCurrentRoom(candidate);
                playerController.transform.position = candidate.DefaultPlayerSpawnPosition;
                candidate.EnterRoom();
                SnapCameraTo(candidate);
                _lastTransitionTime = Time.unscaledTime;
                return true;
            }

            return false;
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

        private PlayerTraversalFlowController EnsureTraversalFlowController(PlayerController targetPlayer)
        {
            if (targetPlayer == null)
            {
                return null;
            }

            if (_playerTraversalFlowController != null && _playerTraversalFlowController.gameObject != targetPlayer.gameObject)
            {
                _playerTraversalFlowController = null;
            }

            if (_playerTraversalFlowController == null)
            {
                _playerTraversalFlowController = targetPlayer.GetComponent<PlayerTraversalFlowController>();
            }

            if (_playerTraversalFlowController == null)
            {
                _playerTraversalFlowController = targetPlayer.gameObject.AddComponent<PlayerTraversalFlowController>();
            }

            return _playerTraversalFlowController;
        }

        private PlayerTraversalRouteReceiptPresentation EnsureTraversalRouteReceiptPresentation(PlayerController targetPlayer)
        {
            if (targetPlayer == null)
            {
                return null;
            }

            if (_playerTraversalRouteReceiptPresentation != null
                && _playerTraversalRouteReceiptPresentation.gameObject != targetPlayer.gameObject)
            {
                _playerTraversalRouteReceiptPresentation = null;
            }

            if (_playerTraversalRouteReceiptPresentation == null)
            {
                _playerTraversalRouteReceiptPresentation = targetPlayer.GetComponent<PlayerTraversalRouteReceiptPresentation>();
            }

            if (_playerTraversalRouteReceiptPresentation == null)
            {
                _playerTraversalRouteReceiptPresentation = targetPlayer.gameObject.AddComponent<PlayerTraversalRouteReceiptPresentation>();
            }

            return _playerTraversalRouteReceiptPresentation;
        }

        private static TraversalGuidanceBeacon EnsureTraversalGuidanceBeacon(RoomDoor targetDoor)
        {
            if (targetDoor == null)
            {
                return null;
            }

            TraversalGuidanceBeacon beacon = targetDoor.GetComponent<TraversalGuidanceBeacon>();
            if (beacon == null)
            {
                beacon = targetDoor.gameObject.AddComponent<TraversalGuidanceBeacon>();
            }

            return beacon;
        }

        private static DoorTraversalPresentation EnsureDoorTraversalPresentation(RoomDoor targetDoor)
        {
            if (targetDoor == null)
            {
                return null;
            }

            DoorTraversalPresentation presentation = targetDoor.GetComponent<DoorTraversalPresentation>();
            if (presentation == null)
            {
                presentation = targetDoor.gameObject.AddComponent<DoorTraversalPresentation>();
            }

            return presentation;
        }

        private void BeginCameraTraversalHandoff(Vector2 traversalDirection, bool guidedRoute)
        {
            if (cameraTraversalHandoffDuration <= 0f || cameraTraversalHandoffDistance <= 0f)
            {
                _cameraTraversalHandoffExpiresAt = 0f;
                _cameraTraversalHandoffActiveDuration = 0f;
                _cameraTraversalHandoffDistanceActive = 0f;
                return;
            }

            _cameraTraversalHandoffDirection = traversalDirection.sqrMagnitude > 0.001f
                ? traversalDirection.normalized
                : Vector2.right;
            _cameraTraversalHandoffActiveDuration = guidedRoute
                ? cameraTraversalHandoffDuration + 0.08f
                : cameraTraversalHandoffDuration;
            _cameraTraversalHandoffDistanceActive = guidedRoute
                ? cameraTraversalHandoffDistance + guidedTraversalCameraBonus
                : cameraTraversalHandoffDistance;
            _cameraTraversalHandoffExpiresAt = Time.unscaledTime + _cameraTraversalHandoffActiveDuration;
        }

        private static Vector2 ResolveTraversalDirection(RoomDirection direction)
        {
            return direction switch
            {
                RoomDirection.Up => Vector2.up,
                RoomDirection.Right => Vector2.right,
                RoomDirection.Down => Vector2.down,
                RoomDirection.Left => Vector2.left,
                _ => Vector2.right
            };
        }

        private static string BuildTraversalRouteReceiptHeadline(string compactTag, RoomType roomType)
        {
            string routeLabel = string.IsNullOrWhiteSpace(compactTag)
                ? ResolveTraversalRouteReceiptLabel(roomType)
                : compactTag.Trim();
            return $"{routeLabel} LOCKED IN";
        }

        private static string BuildTraversalPlanCarryHeadline(string compactTag, RoomType roomType, bool heldChoicePlan)
        {
            string routeLabel = string.IsNullOrWhiteSpace(compactTag)
                ? ResolveTraversalRouteReceiptLabel(roomType)
                : compactTag.Trim();
            return heldChoicePlan
                ? $"{routeLabel} / PLAN HELD"
                : $"{routeLabel} / REROUTE";
        }

        private static string BuildTraversalRouteReceiptDetail(string reasonTag, RoomType roomType, string fallbackDetail = "")
        {
            if (!string.IsNullOrWhiteSpace(reasonTag) && !string.Equals(reasonTag, "NEXT PUSH", StringComparison.OrdinalIgnoreCase))
            {
                return reasonTag.Trim();
            }

            if (!string.IsNullOrWhiteSpace(fallbackDetail))
            {
                string trimmedDetail = fallbackDetail.Trim();
                if (trimmedDetail.Length <= 30)
                {
                    return trimmedDetail;
                }
            }

            return roomType switch
            {
                RoomType.Treasure => "POWER SPIKE",
                RoomType.Shop => "SUPPLY LINE",
                RoomType.Boss => "PRESS BOSS",
                RoomType.Secret => "HIDDEN ROUTE",
                RoomType.Challenge => "TEST BUILD",
                RoomType.MiniBoss => "ELITE PUSH",
                RoomType.Trap => "WATCH STEP",
                RoomType.Curse => "HIGH RISK",
                _ => "NEXT PUSH"
            };
        }

        private static string BuildTraversalPlanCarryDetail(string reasonTag, string fallbackDetail, bool heldChoicePlan)
        {
            string anchor = !string.IsNullOrWhiteSpace(reasonTag)
                ? reasonTag.Trim()
                : !string.IsNullOrWhiteSpace(fallbackDetail)
                    ? fallbackDetail.Trim()
                    : "NEXT PUSH";

            return heldChoicePlan
                ? $"PLAN HELD / {anchor}"
                : $"REROUTE / {anchor}";
        }

        private static string ResolveChoicePlanCarryDisplayLabel(string compareLabel, string fallbackReasonTag)
        {
            return IsShotProfileCompareLabel(compareLabel)
                ? compareLabel.Trim()
                : fallbackReasonTag ?? string.Empty;
        }

        private static bool IsShotProfileCompareLabel(string compareLabel)
        {
            if (string.IsNullOrWhiteSpace(compareLabel))
            {
                return false;
            }

            return compareLabel == "SHOT RESET"
                || compareLabel == "BASELINE"
                || compareLabel.Contains("LASER", StringComparison.Ordinal)
                || compareLabel.Contains("ORBIT", StringComparison.Ordinal)
                || compareLabel.Contains("SHIELD", StringComparison.Ordinal)
                || compareLabel.Contains("SPLIT", StringComparison.Ordinal)
                || compareLabel.Contains("BLAST", StringComparison.Ordinal)
                || compareLabel.Contains("LEECH", StringComparison.Ordinal)
                || compareLabel.Contains("BOUNCE", StringComparison.Ordinal);
        }

        private static string ResolveTraversalRouteReceiptLabel(RoomType roomType)
        {
            return roomType switch
            {
                RoomType.Treasure => "TREASURE",
                RoomType.Shop => "SHOP",
                RoomType.Boss => "BOSS",
                RoomType.Secret => "SECRET",
                RoomType.Challenge => "CHALLENGE",
                RoomType.MiniBoss => "ELITE",
                RoomType.Trap => "TRAP",
                RoomType.Curse => "CURSE",
                _ => "ROUTE"
            };
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
            desiredPosition += ResolveTraversalCameraOffset();
            desiredPosition.z = targetCamera.transform.position.z;
            _cameraTargetPosition = constrainCameraToRoomBounds
                ? ClampCameraPositionToRoom(roomController, desiredPosition)
                : desiredPosition;
        }

        private Vector3 ResolveTraversalCameraOffset()
        {
            if (_cameraTraversalHandoffExpiresAt <= Time.unscaledTime || _cameraTraversalHandoffActiveDuration <= 0f)
            {
                return Vector3.zero;
            }

            float remaining = Mathf.Clamp01((_cameraTraversalHandoffExpiresAt - Time.unscaledTime) / _cameraTraversalHandoffActiveDuration);
            float eased = remaining * remaining;
            Vector2 offset = -_cameraTraversalHandoffDirection * (_cameraTraversalHandoffDistanceActive * eased);
            return new Vector3(offset.x, offset.y, 0f);
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
