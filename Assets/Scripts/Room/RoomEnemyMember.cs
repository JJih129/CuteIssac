using CuteIssac.Enemy;
using UnityEngine;

namespace CuteIssac.Room
{
    /// <summary>
    /// Registers an enemy with a room without giving the enemy authority over room state.
    /// Use this on test enemies placed in a room or on spawned enemies after assigning the room.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(EnemyHealth))]
    public sealed class RoomEnemyMember : MonoBehaviour
    {
        [SerializeField] private RoomController roomController;
        [SerializeField] private EnemyHealth enemyHealth;

        public RoomController AssignedRoom => roomController;

        private void Awake()
        {
            if (enemyHealth == null)
            {
                enemyHealth = GetComponent<EnemyHealth>();
            }
        }

        private void OnEnable()
        {
            RegisterToAssignedRoom();
        }

        private void OnDisable()
        {
            UnregisterFromAssignedRoom();
        }

        public void AssignRoom(RoomController room)
        {
            if (roomController == room)
            {
                return;
            }

            UnregisterFromAssignedRoom();
            roomController = room;
            RegisterToAssignedRoom();
        }

        private void RegisterToAssignedRoom()
        {
            if (roomController == null || enemyHealth == null || !isActiveAndEnabled)
            {
                return;
            }

            roomController.RegisterEnemy(enemyHealth);
        }

        private void UnregisterFromAssignedRoom()
        {
            if (roomController == null || enemyHealth == null)
            {
                return;
            }

            roomController.UnregisterEnemy(enemyHealth);
        }
    }
}
