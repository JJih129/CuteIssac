using CuteIssac.Core.Spawning;
using CuteIssac.Enemy;
using UnityEngine;

namespace CuteIssac.Data.Enemy
{
    [CreateAssetMenu(fileName = "MineLayerEnemyData", menuName = "CuteIssac/Data/Enemy/Mine Layer Enemy Data")]
    public sealed class MineLayerEnemyData : EnemyData
    {
        [Header("Mine Layer Movement")]
        [SerializeField] [Min(0.1f)] private float layInterval = 2.5f;
        [SerializeField] [Min(0f)] private float telegraphDuration = 0.3f;
        [SerializeField] [Min(0f)] private float preferredRange = 5.4f;
        [SerializeField] [Min(0f)] private float retreatRange = 2.8f;
        [SerializeField] [Range(0f, 1f)] private float strafeBlend = 0.42f;
        [SerializeField] [Min(0.05f)] private float strafeSwapInterval = 1.2f;
        [SerializeField] [Min(0f)] private float moveSpeedWhileTelegraphing = 0.1f;
        [SerializeField] private Color telegraphColor = new(0.92f, 0.94f, 0.3f, 1f);

        [Header("Mine Spawn")]
        [SerializeField] private EnemyMineController minePrefab;
        [SerializeField] private SpawnReusePolicy mineSpawnReusePolicy = SpawnReusePolicy.Pooled;
        [SerializeField] [Min(0)] private int minePrewarmCount = 4;
        [SerializeField] private Vector2 mineDropOffset = new(0f, -0.08f);

        [Header("Mine Behavior")]
        [SerializeField] [Min(0f)] private float mineArmDelay = 0.55f;
        [SerializeField] [Min(0f)] private float mineTriggerRange = 1.15f;
        [SerializeField] [Min(0f)] private float mineTriggerWindupSeconds = 0.42f;
        [SerializeField] [Min(0f)] private float mineExplosionRadius = 1.45f;
        [SerializeField] [Min(0f)] private float mineExplosionDamage = 1.35f;
        [SerializeField] [Min(0f)] private float mineExplosionKnockback = 6f;

        public float LayInterval => layInterval;
        public float TelegraphDuration => telegraphDuration;
        public float PreferredRange => preferredRange;
        public float RetreatRange => retreatRange;
        public float StrafeBlend => strafeBlend;
        public float StrafeSwapInterval => strafeSwapInterval;
        public float MoveSpeedWhileTelegraphing => moveSpeedWhileTelegraphing;
        public Color TelegraphColor => telegraphColor;
        public EnemyMineController MinePrefab => minePrefab;
        public SpawnReusePolicy MineSpawnReusePolicy => mineSpawnReusePolicy;
        public int MinePrewarmCount => Mathf.Max(0, minePrewarmCount);
        public Vector2 MineDropOffset => mineDropOffset;
        public float MineArmDelay => mineArmDelay;
        public float MineTriggerRange => mineTriggerRange;
        public float MineTriggerWindupSeconds => mineTriggerWindupSeconds;
        public float MineExplosionRadius => mineExplosionRadius;
        public float MineExplosionDamage => mineExplosionDamage;
        public float MineExplosionKnockback => mineExplosionKnockback;
    }
}
