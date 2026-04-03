using CuteIssac.Core.Gameplay;
using CuteIssac.Data.Dungeon;
using CuteIssac.Room;
using UnityEngine;
using System.Collections.Generic;

namespace CuteIssac.Item
{
    /// <summary>
    /// Marks reward pickups that should feed room reward collection state back into runtime systems.
    /// The component lives on the pickup instance so pooled reuse does not leave stale event subscriptions behind.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RoomRewardPickupTracker : MonoBehaviour
    {
        private static readonly Dictionary<int, int> s_PendingRewardCountsByRoom = new();

        [SerializeField] private BasePickupLogic pickupLogic;

        private RoomController _sourceRoom;
        private RoomType _sourceRoomType;
        private bool _tracksRoomReward;
        private bool _tracksMomentumReward;
        private bool _isHighValueMomentumReward;
        private Color _momentumAccentColor;
        private int _registeredRoomInstanceId = int.MinValue;

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
            UnregisterPendingReward();
            _sourceRoom = null;
            _sourceRoomType = RoomType.Normal;
            _tracksRoomReward = false;
            _tracksMomentumReward = false;
            _isHighValueMomentumReward = false;
            _momentumAccentColor = Color.clear;
        }

        public void Configure(
            RoomController sourceRoom,
            RoomType sourceRoomType,
            bool isMomentumReward = false,
            bool isHighValueMomentumReward = false,
            Color momentumAccentColor = default)
        {
            UnregisterPendingReward();
            _sourceRoom = sourceRoom;
            _sourceRoomType = sourceRoomType;
            _tracksRoomReward = sourceRoom != null;
            _tracksMomentumReward = _tracksRoomReward && isMomentumReward;
            _isHighValueMomentumReward = _tracksMomentumReward && isHighValueMomentumReward;
            _momentumAccentColor = _tracksMomentumReward && momentumAccentColor.a > 0.01f
                ? momentumAccentColor
                : new Color(1f, 0.84f, 0.36f, 1f);
            RegisterPendingReward();
        }

        private void HandleCollected(BasePickupLogic _)
        {
            if (!_tracksRoomReward || _sourceRoom == null)
            {
                return;
            }

            int remainingRewardCount = UnregisterPendingReward();
            GameplayRuntimeEvents.RaiseRoomRewardCollected(new RoomRewardCollectedSignal(
                _sourceRoom,
                _sourceRoomType,
                remainingRewardCount,
                _tracksMomentumReward,
                _isHighValueMomentumReward,
                _momentumAccentColor));
            _tracksRoomReward = false;
            _tracksMomentumReward = false;
            _isHighValueMomentumReward = false;
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

        private void RegisterPendingReward()
        {
            if (!_tracksRoomReward || _sourceRoom == null || _registeredRoomInstanceId != int.MinValue)
            {
                return;
            }

            _registeredRoomInstanceId = _sourceRoom.GetInstanceID();
            if (!s_PendingRewardCountsByRoom.TryGetValue(_registeredRoomInstanceId, out int pendingCount))
            {
                pendingCount = 0;
            }

            s_PendingRewardCountsByRoom[_registeredRoomInstanceId] = pendingCount + 1;
        }

        private int UnregisterPendingReward()
        {
            if (_registeredRoomInstanceId == int.MinValue)
            {
                return 0;
            }

            int remainingCount = 0;
            if (s_PendingRewardCountsByRoom.TryGetValue(_registeredRoomInstanceId, out int pendingCount))
            {
                remainingCount = Mathf.Max(0, pendingCount - 1);

                if (remainingCount > 0)
                {
                    s_PendingRewardCountsByRoom[_registeredRoomInstanceId] = remainingCount;
                }
                else
                {
                    s_PendingRewardCountsByRoom.Remove(_registeredRoomInstanceId);
                }
            }

            _registeredRoomInstanceId = int.MinValue;
            return remainingCount;
        }
    }
}
