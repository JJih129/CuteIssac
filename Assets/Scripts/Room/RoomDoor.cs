using CuteIssac.Player;
using CuteIssac.Dungeon;
using UnityEngine;

namespace CuteIssac.Room
{
    /// <summary>
    /// Door endpoint controlled by a room.
    /// It knows whether passage is currently allowed and optionally references the next room for future dungeon wiring.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RoomDoor : MonoBehaviour
    {
        [Header("Room Link")]
        [SerializeField] private RoomController ownerRoom;
        [SerializeField] private RoomDirection doorDirection = RoomDirection.Up;
        [SerializeField] private RoomController connectedRoom;
        [SerializeField] private RoomDoor connectedDoor;
        [SerializeField] private Transform arrivalPoint;
        [SerializeField] [Min(0f)] private float arrivalInsetDistance = 1.1f;

        [Header("Blocking")]
        [SerializeField] private Collider2D[] blockingColliders;
        [SerializeField] private Collider2D passageTrigger;

        [Header("Visuals")]
        [SerializeField] private GameObject[] lockedStateObjects;
        [SerializeField] private GameObject[] unlockedStateObjects;

        public RoomController OwnerRoom => ownerRoom;
        public RoomDirection DoorDirection => doorDirection;
        public RoomController ConnectedRoom => connectedRoom;
        public RoomDoor ConnectedDoor => connectedDoor;
        public bool IsLocked { get; private set; }

        private bool _isAvailable = true;

        private void Awake()
        {
            ResolveReferences();
        }

        private void Reset()
        {
            ResolveReferences();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            PlayerController playerController = other.GetComponentInParent<PlayerController>();

            if (playerController == null)
            {
                return;
            }

            TryEnter(playerController);
        }

        [ContextMenu("Lock")]
        public void Lock()
        {
            SetLockedInternal(true);
        }

        [ContextMenu("Unlock")]
        public void Unlock()
        {
            SetLockedInternal(false);
        }

        /// <summary>
        /// Called when a player attempts to traverse the door.
        /// Current prototype delegates room traversal to RoomNavigationController so manual layouts and future generators use one path.
        /// </summary>
        public bool TryEnter(PlayerController playerController)
        {
            if (playerController == null || IsLocked)
            {
                return false;
            }

            RoomNavigationController navigationController = FindFirstObjectByType<RoomNavigationController>(FindObjectsInactive.Exclude);

            if (navigationController != null)
            {
                return navigationController.TryTraverse(this, playerController);
            }

            if (connectedRoom != null && connectedRoom.State == RoomState.Idle)
            {
                connectedRoom.EnterRoom();
                return true;
            }

            return connectedRoom != null;
        }

        public void BindOwner(RoomController roomController)
        {
            ownerRoom = roomController;
        }

        /// <summary>
        /// Future dungeon generation can call this once rooms are laid out to wire neighboring doors.
        /// </summary>
        public void SetConnection(RoomController nextRoom, RoomDoor nextDoor = null)
        {
            connectedRoom = nextRoom;
            connectedDoor = nextDoor;
            SetDoorAvailable(nextRoom != null);
        }

        public Vector3 GetArrivalPosition()
        {
            Vector3 basePosition = arrivalPoint != null ? arrivalPoint.position : transform.position;
            Vector2 inwardOffset = GetInwardOffset(doorDirection) * arrivalInsetDistance;
            return basePosition + new Vector3(inwardOffset.x, inwardOffset.y, 0f);
        }

        private void SetLockedInternal(bool locked)
        {
            if (!_isAvailable)
            {
                SetCollidersEnabled(blockingColliders, true);
                IsLocked = true;
                return;
            }

            IsLocked = locked;

            SetCollidersEnabled(blockingColliders, locked);
            SetObjectsActive(lockedStateObjects, locked);
            SetObjectsActive(unlockedStateObjects, !locked);
        }

        private void SetDoorAvailable(bool available)
        {
            _isAvailable = available;

            if (passageTrigger != null)
            {
                passageTrigger.enabled = available;
            }

            SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();

            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = available;
            }

            if (!available)
            {
                SetCollidersEnabled(blockingColliders, true);
                IsLocked = true;
            }
            else
            {
                SetCollidersEnabled(blockingColliders, false);
                IsLocked = false;
            }
        }

        private void ResolveReferences()
        {
            if (ownerRoom == null)
            {
                ownerRoom = GetComponentInParent<RoomController>();
            }

            if (passageTrigger == null)
            {
                passageTrigger = GetComponent<Collider2D>();
            }
        }

        private static void SetCollidersEnabled(Collider2D[] colliders, bool enabled)
        {
            if (colliders == null)
            {
                return;
            }

            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i] != null)
                {
                    colliders[i].enabled = enabled;
                }
            }
        }

        private static void SetObjectsActive(GameObject[] objects, bool active)
        {
            if (objects == null)
            {
                return;
            }

            for (int i = 0; i < objects.Length; i++)
            {
                if (objects[i] != null)
                {
                    objects[i].SetActive(active);
                }
            }
        }

        private static Vector2 GetInwardOffset(RoomDirection direction)
        {
            return direction switch
            {
                RoomDirection.Up => Vector2.down,
                RoomDirection.Right => Vector2.left,
                RoomDirection.Down => Vector2.up,
                RoomDirection.Left => Vector2.right,
                _ => Vector2.zero
            };
        }

        private void OnValidate()
        {
            ResolveReferences();
        }
    }
}
