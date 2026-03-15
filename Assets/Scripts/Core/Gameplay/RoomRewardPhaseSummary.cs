using CuteIssac.Data.Dungeon;

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
            bool isChallengeFinale = false)
        {
            RewardCount = rewardCount;
            ChallengeClearRank = challengeClearRank;
            ChallengePressureTier = challengePressureTier;
            BonusRewardSelections = bonusRewardSelections;
            BonusItemRolls = bonusItemRolls;
            IsChallengeFinale = isChallengeFinale;
        }

        public int RewardCount { get; }
        public ChallengeClearRank ChallengeClearRank { get; }
        public ChallengePressureTier ChallengePressureTier { get; }
        public int BonusRewardSelections { get; }
        public int BonusItemRolls { get; }
        public bool IsChallengeFinale { get; }
        public bool HasRewards => RewardCount > 0;
        public bool HasChallengeBonusPresentation =>
            IsChallengeFinale
            || 
            ChallengeClearRank != ChallengeClearRank.None
            || ChallengePressureTier != ChallengePressureTier.None
            || BonusRewardSelections > 0
            || BonusItemRolls > 0;
    }
}
