using UnityEngine;

namespace CuteIssac.Data.Enemy
{
    [CreateAssetMenu(fileName = "SupportHealerEnemyData", menuName = "CuteIssac/Data/Enemy/Support Healer Enemy Data")]
    public sealed class SupportHealerEnemyData : EnemyData
    {
        [Header("Movement")]
        [SerializeField] [Min(0f)] private float preferredRange = 4.9f;
        [SerializeField] [Min(0f)] private float retreatRange = 2.4f;
        [SerializeField] [Range(0f, 1f)] private float strafeBlend = 0.36f;
        [SerializeField] [Min(0.05f)] private float strafeSwapInterval = 1.2f;

        [Header("Heal")]
        [SerializeField] [Min(0.1f)] private float healInterval = 2.75f;
        [SerializeField] [Min(0f)] private float healAmount = 2f;
        [SerializeField] [Min(0f)] private float healRange = 4.5f;
        [SerializeField] [Min(0.05f)] private float allyScanInterval = 0.35f;
        [SerializeField] [Min(0f)] private float telegraphDuration = 0.42f;
        [SerializeField] [Min(0f)] private float moveSpeedWhileTelegraphing = 0.06f;
        [SerializeField] private Color telegraphColor = new(0.38f, 1f, 0.54f, 1f);

        public float PreferredRange => preferredRange;
        public float RetreatRange => retreatRange;
        public float StrafeBlend => strafeBlend;
        public float StrafeSwapInterval => strafeSwapInterval;
        public float HealInterval => healInterval;
        public float HealAmount => healAmount;
        public float HealRange => healRange;
        public float AllyScanInterval => allyScanInterval;
        public float TelegraphDuration => telegraphDuration;
        public float MoveSpeedWhileTelegraphing => moveSpeedWhileTelegraphing;
        public Color TelegraphColor => telegraphColor;
    }
}
