using UnityEngine;

namespace CuteIssac.Core.Meta
{
    [CreateAssetMenu(fileName = "AchievementData", menuName = "CuteIssac/Data/Meta/Achievement")]
    public sealed class AchievementData : ScriptableObject
    {
        [SerializeField] private string achievementId = "achievement";
        [SerializeField] private string displayName = "Achievement";
        [SerializeField] [TextArea] private string description;
        [SerializeField] private AchievementConditionType conditionType;
        [SerializeField] private string requiredId;
        [SerializeField] [Min(1)] private int requiredCount = 1;
        [SerializeField] private AchievementRewardTargetType rewardTargetType;
        [SerializeField] private string rewardUnlockKey;

        public string AchievementId => achievementId;
        public string DisplayName => displayName;
        public string Description => description;
        public AchievementConditionType ConditionType => conditionType;
        public string RequiredId => requiredId;
        public int RequiredCount => Mathf.Max(1, requiredCount);
        public AchievementRewardTargetType RewardTargetType => rewardTargetType;
        public string RewardUnlockKey => rewardUnlockKey;
    }
}
