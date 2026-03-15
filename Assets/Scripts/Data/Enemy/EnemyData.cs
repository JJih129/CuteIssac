using UnityEngine;

namespace CuteIssac.Data.Enemy
{
    public class EnemyData : ScriptableObject
    {
        [SerializeField] private string enemyId = "enemy";
        [SerializeField] [Min(0f)] private float moveSpeed = 2f;
        [SerializeField] [Min(1f)] private float maxHealth = 8f;
        [SerializeField] [Min(0f)] private float contactDamage = 1f;
        [SerializeField] private EnemyVisualSet visualSet;

        public string EnemyId => enemyId;
        public float MoveSpeed => moveSpeed;
        public float MaxHealth => maxHealth;
        public float ContactDamage => contactDamage;
        public EnemyVisualSet VisualSet => visualSet;
    }
}
