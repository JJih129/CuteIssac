using UnityEngine;

namespace CuteIssac.Data.Enemy
{
    [CreateAssetMenu(fileName = "HomingShooterEnemyData", menuName = "CuteIssac/Data/Enemy/Homing Shooter Enemy Data")]
    public sealed class HomingShooterEnemyData : EnemyData
    {
        [Header("Homing Shooter")]
        [SerializeField] [Min(0.1f)] private float fireInterval = 1.55f;
        [SerializeField] [Min(0f)] private float telegraphDuration = 0.28f;
        [SerializeField] [Min(0f)] private float preferredRange = 6.2f;
        [SerializeField] [Min(0f)] private float retreatRange = 3f;
        [SerializeField] [Range(0f, 1f)] private float strafeBlend = 0.55f;
        [SerializeField] [Min(0.05f)] private float strafeSwapInterval = 1f;
        [SerializeField] [Min(0f)] private float moveSpeedWhileTelegraphing = 0.18f;
        [SerializeField] private Color telegraphColor = new(0.5f, 1f, 0.74f, 1f);
        [Space(6f)]
        [SerializeField] [Min(0f)] private float panicBurstTriggerRange = 2.35f;
        [SerializeField] [Min(1)] private int panicBurstProjectileCount = 3;
        [SerializeField] [Min(0f)] private float panicBurstSpreadAngle = 28f;
        [SerializeField] [Min(0.1f)] private float panicBurstCooldown = 2.35f;
        [SerializeField] [Min(0f)] private float panicBurstTelegraphDuration = 0.22f;
        [SerializeField] [Min(0f)] private float panicBurstMoveSpeedWhileTelegraphing = 0.08f;
        [SerializeField] private Color panicBurstTelegraphColor = new(1f, 0.55f, 0.24f, 1f);

        public float FireInterval => fireInterval;
        public float TelegraphDuration => telegraphDuration;
        public float PreferredRange => preferredRange;
        public float RetreatRange => retreatRange;
        public float StrafeBlend => strafeBlend;
        public float StrafeSwapInterval => strafeSwapInterval;
        public float MoveSpeedWhileTelegraphing => moveSpeedWhileTelegraphing;
        public Color TelegraphColor => telegraphColor;
        public float PanicBurstTriggerRange => panicBurstTriggerRange;
        public int PanicBurstProjectileCount => panicBurstProjectileCount;
        public float PanicBurstSpreadAngle => panicBurstSpreadAngle;
        public float PanicBurstCooldown => panicBurstCooldown;
        public float PanicBurstTelegraphDuration => panicBurstTelegraphDuration;
        public float PanicBurstMoveSpeedWhileTelegraphing => panicBurstMoveSpeedWhileTelegraphing;
        public Color PanicBurstTelegraphColor => panicBurstTelegraphColor;
    }
}
