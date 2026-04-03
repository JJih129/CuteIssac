using CuteIssac.Data.Dungeon;
using UnityEngine;

namespace CuteIssac.Core.Gameplay
{
    public readonly struct RoomRewardPhaseSummary
    {
        public RoomRewardPhaseSummary(
            int rewardCount,
            ChallengeClearRank challengeClearRank = ChallengeClearRank.None,
            ChallengePressureTier challengePressureTier = ChallengePressureTier.None,
            int bonusRewardSelections = 0,
            int bonusItemRolls = 0,
            bool isChallengeFinale = false,
            int momentumBonusRewardSelections = 0,
            int momentumBonusItemRolls = 0,
            Color momentumAccentColor = default)
        {
            RewardCount = rewardCount;
            ChallengeClearRank = challengeClearRank;
            ChallengePressureTier = challengePressureTier;
            BonusRewardSelections = bonusRewardSelections;
            BonusItemRolls = bonusItemRolls;
            IsChallengeFinale = isChallengeFinale;
            MomentumBonusRewardSelections = momentumBonusRewardSelections;
            MomentumBonusItemRolls = momentumBonusItemRolls;
            MomentumAccentColor = momentumAccentColor.a > 0.01f
                ? momentumAccentColor
                : new Color(1f, 0.84f, 0.36f, 1f);
        }

        public int RewardCount { get; }
        public ChallengeClearRank ChallengeClearRank { get; }
        public ChallengePressureTier ChallengePressureTier { get; }
        public int BonusRewardSelections { get; }
        public int BonusItemRolls { get; }
        public bool IsChallengeFinale { get; }
        public int MomentumBonusRewardSelections { get; }
        public int MomentumBonusItemRolls { get; }
        public Color MomentumAccentColor { get; }
        public bool HasRewards => RewardCount > 0;
        public bool HasChallengeBonusPresentation =>
            IsChallengeFinale
            || 
            ChallengeClearRank != ChallengeClearRank.None
            || ChallengePressureTier != ChallengePressureTier.None
            || BonusRewardSelections > 0
            || BonusItemRolls > 0;
        public bool HasMomentumBonusPresentation =>
            MomentumBonusRewardSelections > 0
            || MomentumBonusItemRolls > 0;
    }
}
