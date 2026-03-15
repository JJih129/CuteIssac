using CuteIssac.Core.Spawning;
using CuteIssac.Enemy;
using UnityEngine;

namespace CuteIssac.Data.Enemy
{
    [CreateAssetMenu(fileName = "SplitterEnemyData", menuName = "CuteIssac/Data/Enemy/Splitter Enemy Data")]
    public sealed class SplitterEnemyData : EnemyData
    {
        [Header("Movement")]
        [SerializeField] [Min(1f)] private float chaseSpeedMultiplier = 1.08f;

        [Header("Split")]
        [SerializeField] private EnemyController childEnemyPrefab;
        [SerializeField] private SpawnReusePolicy childSpawnReusePolicy = SpawnReusePolicy.Pooled;
        [SerializeField] [Min(1)] private int splitChildCount = 2;
        [SerializeField] [Min(0f)] private float childSpawnRadius = 0.55f;
        [SerializeField] [Min(0f)] private float childScatterImpulse = 2.5f;
        [SerializeField] [Min(0f)] private float childSpawnAggroDelay = 0.22f;
        [SerializeField] [Min(0)] private int childPrewarmCount = 2;

        public float ChaseSpeedMultiplier => chaseSpeedMultiplier;
        public EnemyController ChildEnemyPrefab => childEnemyPrefab;
        public SpawnReusePolicy ChildSpawnReusePolicy => childSpawnReusePolicy;
        public int SplitChildCount => splitChildCount;
        public float ChildSpawnRadius => childSpawnRadius;
        public float ChildScatterImpulse => childScatterImpulse;
        public float ChildSpawnAggroDelay => childSpawnAggroDelay;
        public int ChildPrewarmCount => childPrewarmCount;
    }
}
