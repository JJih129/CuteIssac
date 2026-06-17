using CuteIssac.Data.Enemy;
using CuteIssac.Data.Item;
using CuteIssac.Data.Room;
using System.Collections.Generic;
using UnityEngine;

namespace CuteIssac.Data.Dungeon
{
    /// <summary>
    /// 스테이지 단위 콘텐츠 허브입니다.
    /// 기존 FloorConfig를 즉시 대체하지 않고 감싸서, 맵/문/몹/보상/상점 데이터를 한곳에서 확인하고 단계적으로 이관할 수 있게 합니다.
    /// </summary>
    [CreateAssetMenu(fileName = "StageProfile", menuName = "CuteIssac/Data/Dungeon/Stage Profile")]
    public sealed class StageProfile : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string stageId = "stage_floor_1";
        [SerializeField] private string displayName = "Floor 1";

        [Header("Core Data")]
        [Tooltip("현재 던전 생성의 기준 데이터입니다. StageProfile 1차 통합에서는 이 참조를 통해 기존 FloorConfig 기반 코드를 유지합니다.")]
        [SerializeField] private FloorConfig floorConfig;

        [Header("Stage Overrides")]
        [Tooltip("비워두면 FloorConfig의 RoomTheme를 사용합니다.")]
        [SerializeField] private RoomThemeData roomThemeOverride;
        [Tooltip("비워두면 FloorConfig의 EnemyPool을 사용합니다.")]
        [SerializeField] private EnemyPoolData enemyPoolOverride;

        [Header("Reward Overrides")]
        [SerializeField] private RoomRewardTable normalRoomRewardPoolOverride;
        [SerializeField] private RoomRewardTable bossRoomRewardPoolOverride;
        [SerializeField] private RoomRewardTable treasureRoomRewardPoolOverride;
        [SerializeField] private RoomRewardTable shopRoomRewardPoolOverride;
        [SerializeField] private RoomRewardTable secretRoomRewardPoolOverride;
        [SerializeField] private RoomRewardTable curseRoomRewardPoolOverride;

        [Header("Item Pool Overrides")]
        [SerializeField] private ItemPoolData treasureRoomItemPoolOverride;
        [SerializeField] private ItemPoolData challengeRoomItemPoolOverride;
        [SerializeField] private ItemPoolData shopRoomItemPoolOverride;
        [SerializeField] private ItemPoolData bossRewardItemPoolOverride;
        [SerializeField] private ItemPoolData secretRoomItemPoolOverride;
        [SerializeField] private ItemPoolData curseRoomItemPoolOverride;

        [Header("Special Room Overrides")]
        [Tooltip("켜면 아래 챌린지 보상 설정이 FloorConfig 설정 대신 사용됩니다.")]
        [SerializeField] private bool overrideChallengeRewardSettings;
        [Tooltip("토글이 켜졌을 때 사용할 StageProfile 전용 챌린지 보상 설정입니다.")]
        [SerializeField] private ChallengeRewardSettings challengeRewardSettingsOverride = ChallengeRewardSettings.CreateDefault();
        [Tooltip("켜면 아래 시크릿룸 보상 설정이 FloorConfig 설정 대신 사용됩니다.")]
        [SerializeField] private bool overrideSecretRoomRewardSettings;
        [Tooltip("토글이 켜졌을 때 사용할 StageProfile 전용 시크릿룸 보상 설정입니다.")]
        [SerializeField] private SecretRoomRewardSettings secretRoomRewardSettingsOverride = SecretRoomRewardSettings.CreateDefault();
        [Tooltip("비워두면 FloorConfig의 특수방 규칙을 사용합니다.")]
        [SerializeField] private FloorSpecialRoomRules specialRoomRulesOverride;

        [Header("Prewarm")]
        [SerializeField] private StagePrewarmProfile prewarmProfile = new();

        public string StageId => string.IsNullOrWhiteSpace(stageId) ? name : stageId;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? StageId : displayName;
        public FloorConfig FloorConfig => floorConfig;
        public int FloorIndex => floorConfig != null ? floorConfig.FloorIndex : 1;
        public RoomThemeData RoomTheme => roomThemeOverride != null ? roomThemeOverride : floorConfig != null ? floorConfig.RoomTheme : null;
        public EnemyPoolData EnemyPool => enemyPoolOverride != null ? enemyPoolOverride : floorConfig != null ? floorConfig.EnemyPool : null;
        public ChallengeRewardSettings ChallengeRewardSettings => overrideChallengeRewardSettings && challengeRewardSettingsOverride != null
            ? challengeRewardSettingsOverride
            : floorConfig != null
                ? floorConfig.ChallengeRewardSettings
                : ChallengeRewardSettings.CreateDefault();
        public SecretRoomRewardSettings SecretRoomRewardSettings => overrideSecretRoomRewardSettings && secretRoomRewardSettingsOverride != null
            ? secretRoomRewardSettingsOverride
            : floorConfig != null
                ? floorConfig.SecretRoomRewardSettings
                : SecretRoomRewardSettings.CreateDefault();
        public FloorSpecialRoomRules SpecialRoomRules => specialRoomRulesOverride != null ? specialRoomRulesOverride : floorConfig != null ? floorConfig.SpecialRoomRules : null;
        public StagePrewarmProfile PrewarmProfile => prewarmProfile ?? StagePrewarmProfile.CreateDefault();

        public bool TryGetFloorConfig(out FloorConfig resolvedFloorConfig)
        {
            resolvedFloorConfig = floorConfig;
            return resolvedFloorConfig != null;
        }

        public RoomRewardTable GetRewardPool(RoomType roomType)
        {
            RoomRewardTable overridePool = roomType switch
            {
                RoomType.Boss => bossRoomRewardPoolOverride,
                RoomType.MiniBoss => bossRoomRewardPoolOverride,
                RoomType.Treasure => treasureRoomRewardPoolOverride,
                RoomType.Shop => shopRoomRewardPoolOverride,
                RoomType.Secret => secretRoomRewardPoolOverride,
                RoomType.Curse => curseRoomRewardPoolOverride,
                RoomType.Normal or RoomType.Challenge or RoomType.Trap => normalRoomRewardPoolOverride,
                _ => null
            };

            return overridePool != null ? overridePool : floorConfig != null ? floorConfig.GetRewardPool(roomType) : null;
        }

        public ItemPoolData GetItemPool(RoomType roomType)
        {
            ItemPoolData overridePool = roomType switch
            {
                RoomType.Treasure => treasureRoomItemPoolOverride,
                RoomType.Challenge => challengeRoomItemPoolOverride,
                RoomType.Shop => shopRoomItemPoolOverride,
                RoomType.Boss => bossRewardItemPoolOverride,
                RoomType.Secret => secretRoomItemPoolOverride,
                RoomType.Curse => curseRoomItemPoolOverride,
                _ => null
            };

            return overridePool != null ? overridePool : floorConfig != null ? floorConfig.GetItemPool(roomType) : null;
        }

        public EncounterPacingSettings GetEncounterPacing(RoomType roomType)
        {
            return floorConfig != null ? floorConfig.GetEncounterPacing(roomType) : null;
        }

        public void CollectAvailableSpecialRoomRules(List<SpecialRoomRuleData> results)
        {
            if (results == null)
            {
                return;
            }

            FloorSpecialRoomRules rules = SpecialRoomRules;
            if (rules == null)
            {
                return;
            }

            rules.CollectAvailableRules(FloorIndex, results);
        }

        public bool TryGetSpecialRoomRule(RoomType roomType, out SpecialRoomRuleData specialRoomRule)
        {
            FloorSpecialRoomRules rules = SpecialRoomRules;
            IReadOnlyList<SpecialRoomRuleData> ruleList = rules != null ? rules.Rules : null;
            if (ruleList == null)
            {
                specialRoomRule = null;
                return false;
            }

            int floorIndex = FloorIndex;
            for (int i = 0; i < ruleList.Count; i++)
            {
                SpecialRoomRuleData rule = ruleList[i];
                if (rule != null && rule.RoomType == roomType && rule.IsAvailableForFloor(floorIndex))
                {
                    specialRoomRule = rule;
                    return true;
                }
            }

            specialRoomRule = null;
            return false;
        }
    }
}
