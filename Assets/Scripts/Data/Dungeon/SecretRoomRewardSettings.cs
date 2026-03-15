using System;
using UnityEngine;

namespace CuteIssac.Data.Dungeon
{
    [Serializable]
    public sealed class SecretRoomRewardSettings
    {
        [SerializeField] [Min(0)] private int bonusRewardSelections = 1;
        [SerializeField] [Min(0)] private int bonusItemRolls = 1;
        [SerializeField] [Range(0f, 1f)] private float rareBonusChance = 0.18f;
        [SerializeField] [Min(0)] private int rareBonusRewardSelections = 1;
        [SerializeField] [Min(0)] private int rareBonusItemRolls = 1;

        public int BonusRewardSelections => Mathf.Max(0, bonusRewardSelections);
        public int BonusItemRolls => Mathf.Max(0, bonusItemRolls);
        public float RareBonusChance => Mathf.Clamp01(rareBonusChance);
        public int RareBonusRewardSelections => Mathf.Max(0, rareBonusRewardSelections);
        public int RareBonusItemRolls => Mathf.Max(0, rareBonusItemRolls);

        public int ResolveRewardSelectionCount()
        {
            int total = BonusRewardSelections;

            if (RareBonusRewardSelections > 0 && RareBonusChance > 0f && UnityEngine.Random.value <= RareBonusChance)
            {
                total += RareBonusRewardSelections;
            }

            return total;
        }

        public int ResolveItemRollCount()
        {
            int total = BonusItemRolls;

            if (RareBonusItemRolls > 0 && RareBonusChance > 0f && UnityEngine.Random.value <= RareBonusChance)
            {
                total += RareBonusItemRolls;
            }

            return total;
        }

        public static SecretRoomRewardSettings CreateDefault()
        {
            return new SecretRoomRewardSettings
            {
                bonusRewardSelections = 1,
                bonusItemRolls = 1,
                rareBonusChance = 0.18f,
                rareBonusRewardSelections = 1,
                rareBonusItemRolls = 1
            };
        }
    }
}
