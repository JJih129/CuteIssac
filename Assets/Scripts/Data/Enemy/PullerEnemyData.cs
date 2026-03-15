using UnityEngine;

namespace CuteIssac.Data.Enemy
{
    [CreateAssetMenu(fileName = "PullerEnemyData", menuName = "CuteIssac/Data/Enemy/Puller Enemy Data")]
    public sealed class PullerEnemyData : EnemyData
    {
        [Header("Movement")]
        [SerializeField] [Min(0f)] private float preferredRange = 4.8f;
        [SerializeField] [Min(0f)] private float retreatRange = 2.6f;
        [SerializeField] [Range(0f, 1f)] private float orbitBlend = 0.42f;
        [SerializeField] [Min(0.05f)] private float strafeSwapInterval = 1.08f;
        [SerializeField] [Min(1f)] private float surgeSpeedMultiplier = 1.12f;

        [Header("Pull")]
        [SerializeField] [Min(0.1f)] private float pullInterval = 2.15f;
        [SerializeField] [Min(0f)] private float pullRange = 5.2f;
        [SerializeField] [Min(0f)] private float pullDamage = 1f;
        [SerializeField] [Min(0f)] private float telegraphDuration = 0.36f;
        [SerializeField] [Min(0f)] private float moveSpeedWhileTelegraphing = 0.08f;
        [SerializeField] private Color telegraphColor = new(0.42f, 0.96f, 1f, 1f);

        public float PreferredRange => preferredRange;
        public float RetreatRange => retreatRange;
        public float OrbitBlend => orbitBlend;
        public float StrafeSwapInterval => strafeSwapInterval;
        public float SurgeSpeedMultiplier => surgeSpeedMultiplier;
        public float PullInterval => pullInterval;
        public float PullRange => pullRange;
        public float PullDamage => pullDamage;
        public float TelegraphDuration => telegraphDuration;
        public float MoveSpeedWhileTelegraphing => moveSpeedWhileTelegraphing;
        public Color TelegraphColor => telegraphColor;
    }
}
