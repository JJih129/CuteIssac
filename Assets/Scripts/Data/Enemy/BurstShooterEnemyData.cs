using UnityEngine;

namespace CuteIssac.Data.Enemy
{
    [CreateAssetMenu(fileName = "BurstShooterEnemyData", menuName = "CuteIssac/Data/Enemy/Burst Shooter Enemy Data")]
    public sealed class BurstShooterEnemyData : EnemyData
    {
        [Header("Burst Shooter")]
        [SerializeField] [Min(0.1f)] private float fireInterval = 1.9f;
        [SerializeField] [Min(0f)] private float telegraphDuration = 0.34f;
        [SerializeField] [Min(0f)] private float preferredRange = 5.8f;
        [SerializeField] [Min(0f)] private float retreatRange = 2.9f;
        [SerializeField] [Range(0f, 1f)] private float strafeBlend = 0.38f;
        [SerializeField] [Min(0.05f)] private float strafeSwapInterval = 1.15f;
        [SerializeField] [Min(0f)] private float moveSpeedWhileTelegraphing = 0.12f;
        [SerializeField] [Min(3)] private int burstProjectileCount = 5;
        [SerializeField] [Range(0f, 180f)] private float burstSpreadAngle = 40f;
        [SerializeField] private Color telegraphColor = new(1f, 0.58f, 0.22f, 1f);

        public float FireInterval => fireInterval;
        public float TelegraphDuration => telegraphDuration;
        public float PreferredRange => preferredRange;
        public float RetreatRange => retreatRange;
        public float StrafeBlend => strafeBlend;
        public float StrafeSwapInterval => strafeSwapInterval;
        public float MoveSpeedWhileTelegraphing => moveSpeedWhileTelegraphing;
        public int BurstProjectileCount => Mathf.Max(3, burstProjectileCount);
        public float BurstSpreadAngle => burstSpreadAngle;
        public Color TelegraphColor => telegraphColor;
    }
}
