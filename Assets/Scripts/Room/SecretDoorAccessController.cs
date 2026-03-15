using CuteIssac.Core.Feedback;
using CuteIssac.Core.Gameplay;
using CuteIssac.Combat;
using UnityEngine;

namespace CuteIssac.Room
{
    /// <summary>
    /// Keeps secret-door reveal logic outside RoomDoor and RoomController.
    /// Dungeon generation can attach this only to entrances that should hide a secret room.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SecretDoorAccessController : MonoBehaviour, IBombReactive
    {
        [Header("References")]
        [SerializeField] private RoomDoor roomDoor;

        [Header("Reveal Rule")]
        [SerializeField] private SecretDoorRevealMode revealMode = SecretDoorRevealMode.BombOnly;
        [SerializeField] private bool logRevealInEditor = true;

        private bool _hasRuntimeConfiguration;

        public void Configure(RoomDoor targetDoor, SecretDoorRevealMode mode)
        {
            roomDoor = targetDoor;
            revealMode = mode;
            _hasRuntimeConfiguration = true;
            ApplyDoorRule();
        }

        public void ReactToBomb(in BombExplosionInfo explosionInfo)
        {
            if (revealMode == SecretDoorRevealMode.BombOnly)
            {
                Reveal();
            }
        }

        public void RevealFromCondition()
        {
            if (revealMode == SecretDoorRevealMode.ExternalCondition || revealMode == SecretDoorRevealMode.StartsRevealed)
            {
                Reveal();
            }
        }

        private void Awake()
        {
            if (roomDoor == null)
            {
                roomDoor = GetComponent<RoomDoor>();
            }

            if (!_hasRuntimeConfiguration)
            {
                ApplyDoorRule();
            }
        }

        private void Reset()
        {
            roomDoor = GetComponent<RoomDoor>();
        }

        private void OnValidate()
        {
            if (roomDoor == null)
            {
                roomDoor = GetComponent<RoomDoor>();
            }
        }

        private void ApplyDoorRule()
        {
            if (roomDoor == null)
            {
                return;
            }

            bool startsRevealed = revealMode == SecretDoorRevealMode.StartsRevealed;
            roomDoor.ConfigureRevealRequirement(!startsRevealed, startsRevealed);
        }

        private void Reveal()
        {
            if (roomDoor == null)
            {
                return;
            }

            roomDoor.RevealSecretAccess();
            GameplayRuntimeEvents.RaiseSecretRoomRevealed(new SecretRoomRevealedSignal(
                roomDoor.OwnerRoom,
                roomDoor.ConnectedRoom,
                roomDoor.DoorDirection));
            GameplayFeedbackEvents.RaiseBannerFeedback(new BannerFeedbackRequest(
                "비밀방 발견",
                "숨겨진 방이 지도에 드러났습니다.",
                new Color(0.86f, 0.6f, 1f, 1f),
                1.7f));

            if (logRevealInEditor)
            {
                Debug.Log($"Secret door revealed for room '{roomDoor.OwnerRoom?.RoomId}'.", roomDoor);
            }
        }
    }
}
