using CuteIssac.Data.Dungeon;
using CuteIssac.Room;
using UnityEngine;

namespace CuteIssac.Core.Gameplay
{
    public readonly struct RoomRewardPhaseSignal
    {
        public RoomRewardPhaseSignal(RoomController room, RoomType roomType, RoomRewardPhaseSummary summary)
        {
            Room = room;
            RoomType = roomType;
            Summary = summary;
        }

        public RoomController Room { get; }
        public RoomType RoomType { get; }
        public RoomRewardPhaseSummary Summary { get; }
        public int RewardCount => Summary.RewardCount;
        public ChallengeClearRank ChallengeClearRank => Summary.ChallengeClearRank;
        public ChallengePressureTier ChallengePressureTier => Summary.ChallengePressureTier;
        public int BonusRewardSelections => Summary.BonusRewardSelections;
        public int BonusItemRolls => Summary.BonusItemRolls;
        public int MomentumBonusRewardSelections => Summary.MomentumBonusRewardSelections;
        public int MomentumBonusItemRolls => Summary.MomentumBonusItemRolls;
        public Color MomentumAccentColor => Summary.MomentumAccentColor;
        public bool IsChallengeFinale => Summary.IsChallengeFinale;
        public bool HasRewards => Summary.HasRewards;
        public bool HasChallengeBonusPresentation => Summary.HasChallengeBonusPresentation;
        public bool HasMomentumBonusPresentation => Summary.HasMomentumBonusPresentation;
    }
}
