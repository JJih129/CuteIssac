using CuteIssac.Core.Gameplay;
using CuteIssac.Data.Dungeon;
using CuteIssac.Room;
using UnityEngine;

namespace CuteIssac.Item
{
    /// <summary>
    /// Marks reward pickups that should feed room reward collection state back into runtime systems.
    /// The component lives on the pickup instance so pooled reuse does not leave stale event subscriptions behind.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RoomRewardPickupTracker : MonoBehaviour
    {
        [SerializeField] private BasePickupLogic pickupLogic;

        private RoomController _sourceRoom;
        private RoomType _sourceRoomType;
        private bool _tracksRoomReward;

        public RoomController SourceRoom => _sourceRoom;
        public RoomType SourceRoomType => _sourceRoomType;
        public bool TracksRoomReward => _tracksRoomReward;

        private void Awake()
        {
            ResolveReferences();

            if (pickupLogic != null)
            {
                pickupLogic.Collected -= HandleCollected;
                pickupLogic.Collected += HandleCollected;
            }
        }

        private void OnDisable()
        {
            _sourceRoom = null;
            _sourceRoomType = RoomType.Normal;
            _tracksRoomReward = false;
        }

        public void Configure(RoomController sourceRoom, RoomType sourceRoomType)
        {
            _sourceRoom = sourceRoom;
            _sourceRoomType = sourceRoomType;
            _tracksRoomReward = sourceRoom != null;
        }

        private void HandleCollected(BasePickupLogic _)
        {
            if (!_tracksRoomReward || _sourceRoom == null)
            {
                return;
            }

            GameplayRuntimeEvents.RaiseRoomRewardCollected(new RoomRewardCollectedSignal(_sourceRoom, _sourceRoomType));
            _tracksRoomReward = false;
        }

        private void ResolveReferences()
        {
            if (pickupLogic == null)
            {
                TryGetComponent(out pickupLogic);
            }
        }

        private void Reset()
        {
            ResolveReferences();
        }

        private void OnValidate()
        {
            ResolveReferences();
        }
    }
}
