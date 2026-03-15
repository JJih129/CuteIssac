using UnityEngine;

namespace CuteIssac.Data.Enemy
{
    [CreateAssetMenu(fileName = "SniperEnemyData", menuName = "CuteIssac/Data/Enemy/Sniper Enemy Data")]
    public sealed class SniperEnemyData : EnemyData
    {
        [Header("Sniper")]
        [SerializeField] [Min(0.1f)] private float fireInterval = 2.2f;
        [SerializeField] [Min(0f)] private float telegraphDuration = 0.5f;
        [SerializeField] [Min(0f)] private float preferredRange = 8.4f;
        [SerializeField] [Min(0f)] private float retreatRange = 4.8f;
        [SerializeField] [Range(0f, 1f)] private float strafeBlend = 0.16f;
        [SerializeField] [Min(0.05f)] private float strafeSwapInterval = 1.45f;
        [SerializeField] [Min(0f)] private float moveSpeedWhileTelegraphing = 0.04f;
        [SerializeField] private Color telegraphColor = new(1f, 0.24f, 0.3f, 1f);
        [Space(6f)]
        [SerializeField] [Min(0f)] private float followUpTriggerRange = 7.1f;
        [SerializeField] [Min(0f)] private float followUpDelay = 0.18f;
        [SerializeField] [Min(0f)] private float followUpTelegraphDuration = 0.16f;
        [SerializeField] [Min(0f)] private float moveSpeedWhileFollowUpTelegraphing = 0.02f;
        [SerializeField] [Min(0.1f)] private float followUpRecovery = 0.7f;
        [SerializeField] private Color followUpTelegraphColor = new(1f, 0.72f, 0.3f, 1f);

        public float FireInterval => fireInterval;
        public float TelegraphDuration => telegraphDuration;
        public float PreferredRange => preferredRange;
        public float RetreatRange => retreatRange;
        public float StrafeBlend => strafeBlend;
        public float StrafeSwapInterval => strafeSwapInterval;
        public float MoveSpeedWhileTelegraphing => moveSpeedWhileTelegraphing;
        public Color TelegraphColor => telegraphColor;
        public float FollowUpTriggerRange => followUpTriggerRange;
        public float FollowUpDelay => followUpDelay;
        public float FollowUpTelegraphDuration => followUpTelegraphDuration;
        public float MoveSpeedWhileFollowUpTelegraphing => moveSpeedWhileFollowUpTelegraphing;
        public float FollowUpRecovery => followUpRecovery;
        public Color FollowUpTelegraphColor => followUpTelegraphColor;
    }
}
