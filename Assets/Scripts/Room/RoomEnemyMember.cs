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

        private void Awake()
        {
            if (enemyHealth == null)
            {
                enemyHealth = GetComponent<EnemyHealth>();
            }
        }

        private void Start()
        {
            roomController?.RegisterEnemy(enemyHealth);
        }

        private void OnDestroy()
        {
            roomController?.UnregisterEnemy(enemyHealth);
        }

        public void AssignRoom(RoomController room)
        {
            if (roomController == room)
            {
                return;
            }

            roomController?.UnregisterEnemy(enemyHealth);
            roomController = room;
            roomController?.RegisterEnemy(enemyHealth);
        }
    }
}
