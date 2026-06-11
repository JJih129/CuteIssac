using CuteIssac.Data.Dungeon;
using UnityEngine;

namespace CuteIssac.Data.Unlock
{
    /// <summary>
    /// ScriptableObject definition for one persistent meta unlock rule.
    /// The target is expressed as a string key so item/enemy pools can query unlocks without hardcoded references.
    /// </summary>
    [CreateAssetMenu(fileName = "UnlockData", menuName = "CuteIssac/Data/Unlock Data")]
    public sealed class UnlockData : ScriptableObject
    {
        [SerializeField] private string unlockId = "unlock";
        [SerializeField] private string displayName = "Meta Unlock";
        [SerializeField] [TextArea] private string description;
        [SerializeField] private UnlockConditionType conditionType = UnlockConditionType.ReachFloor;
        [SerializeField] private UnlockTargetType targetType = UnlockTargetType.Item;
        [SerializeField] private string targetKey = "unlock.key";
        [SerializeField] private RoomType targetRoomType = RoomType.Secret;
        [SerializeField] private string requiredEnemyId;
        [SerializeField] [Min(1)] private int requiredCount = 1;
        [SerializeField] [Min(1)] private int requiredFloorIndex = 2;
        [SerializeField] private string requiredItemId;
        [SerializeField] private string requiredCharacterId;
        [SerializeField] private string requiredClearMark = "run_victory";
        [SerializeField] private string requiredAchievementId;
        [SerializeField] private RoomType requiredRoomType = RoomType.Normal;

        public string UnlockId => unlockId;
        public string DisplayName => displayName;
        public string Description => description;
        public UnlockConditionType ConditionType => conditionType;
        public UnlockTargetType TargetType => targetType;
        public string TargetKey => targetKey;
        public RoomType TargetRoomType => targetRoomType;
        public string RequiredEnemyId => requiredEnemyId;
        public int RequiredCount => Mathf.Max(1, requiredCount);
        public int RequiredFloorIndex => Mathf.Max(1, requiredFloorIndex);
        public string RequiredItemId => requiredItemId;
        public string RequiredCharacterId => requiredCharacterId;
        public string RequiredClearMark => requiredClearMark;
        public string RequiredAchievementId => requiredAchievementId;
        public RoomType RequiredRoomType => requiredRoomType;

        public bool TargetsRoomType(RoomType roomType)
        {
            return targetType == UnlockTargetType.RoomType && targetRoomType == roomType;
        }
    }
}
