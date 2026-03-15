using UnityEngine;

namespace CuteIssac.Data.Enemy
{
    [CreateAssetMenu(fileName = "ShieldEnemyData", menuName = "CuteIssac/Data/Enemy/Shield Enemy Data")]
    public sealed class ShieldEnemyData : EnemyData
    {
        [Header("Shield")]
        [SerializeField] [Min(0f)] private float advanceSpeedMultiplier = 0.92f;
        [SerializeField] [Min(0f)] private float blockFeedbackCooldown = 0.2f;
        [SerializeField] [Min(0f)] private float contactRange = 1.4f;
        [SerializeField] private Color blockFeedbackColor = new(0.42f, 0.92f, 1f, 1f);

        public float AdvanceSpeedMultiplier => advanceSpeedMultiplier;
        public float BlockFeedbackCooldown => blockFeedbackCooldown;
        public float ContactRange => contactRange;
        public Color BlockFeedbackColor => blockFeedbackColor;
    }
}
