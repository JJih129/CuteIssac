using System;
using UnityEngine;

namespace CuteIssac.Data.Dungeon
{
    [Serializable]
    public sealed class ChallengeRewardSettings
    {
        [SerializeField] [Min(5f)] private float sRankTimeSeconds = 24f;
        [SerializeField] [Min(8f)] private float aRankTimeSeconds = 38f;
        [SerializeField] [Min(0)] private int sRankBonusRewardSelections = 1;
        [SerializeField] [Min(0)] private int sRankBonusItemRolls = 1;
        [SerializeField] [Min(0)] private int aRankBonusRewardSelections = 1;
        [SerializeField] [Min(0)] private int aRankBonusItemRolls;
        [SerializeField] [Min(0)] private int bRankBonusRewardSelections;
        [SerializeField] [Min(0)] private int bRankBonusItemRolls;
        [SerializeField] [Min(2)] private int reinforcedWaveThreshold = 2;
        [SerializeField] [Min(0)] private int reinforcedBonusRewardSelections;
        [SerializeField] [Min(0)] private int reinforcedBonusItemRolls;
        [SerializeField] [Min(0)] private int eliteBonusRewardSelections = 1;
        [SerializeField] [Min(0)] private int eliteBonusItemRolls;
        [SerializeField] [Min(0)] private int deadlyBonusRewardSelections = 1;
        [SerializeField] [Min(0)] private int deadlyBonusItemRolls = 1;

        public float SRankTimeSeconds => Mathf.Max(5f, sRankTimeSeconds);
        public float ARankTimeSeconds => Mathf.Max(SRankTimeSeconds, aRankTimeSeconds);

        public ChallengeClearRank EvaluateRank(float combatDuration)
        {
            if (combatDuration <= SRankTimeSeconds)
            {
                return ChallengeClearRank.S;
            }

            if (combatDuration <= ARankTimeSeconds)
            {
                return ChallengeClearRank.A;
            }

            if (combatDuration > 0f)
            {
                return ChallengeClearRank.B;
            }

            return ChallengeClearRank.None;
        }

        public int GetBonusRewardSelections(ChallengeClearRank rank)
        {
            return rank switch
            {
                ChallengeClearRank.S => Mathf.Max(0, sRankBonusRewardSelections),
                ChallengeClearRank.A => Mathf.Max(0, aRankBonusRewardSelections),
                ChallengeClearRank.B => Mathf.Max(0, bRankBonusRewardSelections),
                _ => 0
            };
        }

        public int GetBonusItemRolls(ChallengeClearRank rank)
        {
            return rank switch
            {
                ChallengeClearRank.S => Mathf.Max(0, sRankBonusItemRolls),
                ChallengeClearRank.A => Mathf.Max(0, aRankBonusItemRolls),
                ChallengeClearRank.B => Mathf.Max(0, bRankBonusItemRolls),
                _ => 0
            };
        }

        public ChallengePressureTier EvaluatePressureTier(
            int totalWaveCount,
            int reinforcementEnemyCount,
            int guaranteedChampionCount,
            float championChanceBonus)
        {
            int resolvedWaveThreshold = Mathf.Max(2, reinforcedWaveThreshold);
            bool hasReinforcementWave = totalWaveCount >= resolvedWaveThreshold;
            bool hasElitePressure = guaranteedChampionCount >= 1 || championChanceBonus >= 0.18f;
            bool hasDeadlyPressure = totalWaveCount >= resolvedWaveThreshold + 1 || guaranteedChampionCount >= 2;

            if (hasDeadlyPressure)
            {
                return ChallengePressureTier.Deadly;
            }

            if (hasElitePressure)
            {
                return ChallengePressureTier.Elite;
            }

            if (hasReinforcementWave || reinforcementEnemyCount > 0 || championChanceBonus > 0f)
            {
                return ChallengePressureTier.Reinforced;
            }

            return ChallengePressureTier.None;
        }

        public int GetPressureBonusRewardSelections(ChallengePressureTier pressureTier)
        {
            return pressureTier switch
            {
                ChallengePressureTier.Reinforced => Mathf.Max(0, reinforcedBonusRewardSelections),
                ChallengePressureTier.Elite => Mathf.Max(0, eliteBonusRewardSelections),
                ChallengePressureTier.Deadly => Mathf.Max(0, deadlyBonusRewardSelections),
                _ => 0
            };
        }

        public int GetPressureBonusItemRolls(ChallengePressureTier pressureTier)
        {
            return pressureTier switch
            {
                ChallengePressureTier.Reinforced => Mathf.Max(0, reinforcedBonusItemRolls),
                ChallengePressureTier.Elite => Mathf.Max(0, eliteBonusItemRolls),
                ChallengePressureTier.Deadly => Mathf.Max(0, deadlyBonusItemRolls),
                _ => 0
            };
        }

        public static ChallengeRewardSettings CreateDefault()
        {
            return new ChallengeRewardSettings
            {
                sRankTimeSeconds = 24f,
                aRankTimeSeconds = 38f,
                sRankBonusRewardSelections = 1,
                sRankBonusItemRolls = 1,
                aRankBonusRewardSelections = 1,
                aRankBonusItemRolls = 0,
                bRankBonusRewardSelections = 0,
                bRankBonusItemRolls = 0,
                reinforcedWaveThreshold = 2,
                reinforcedBonusRewardSelections = 0,
                reinforcedBonusItemRolls = 0,
                eliteBonusRewardSelections = 1,
                eliteBonusItemRolls = 0,
                deadlyBonusRewardSelections = 1,
                deadlyBonusItemRolls = 1
            };
        }
    }

    public enum ChallengeClearRank
    {
        None = 0,
        B = 1,
        A = 2,
        S = 3
    }

    public enum ChallengePressureTier
    {
        None = 0,
        Reinforced = 1,
        Elite = 2,
        Deadly = 3
    }
}
