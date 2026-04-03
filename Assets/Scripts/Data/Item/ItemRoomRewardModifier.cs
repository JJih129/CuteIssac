using System;
using System.Collections.Generic;
using CuteIssac.Data.Dungeon;
using UnityEngine;

namespace CuteIssac.Data.Item
{
    [Serializable]
    public sealed class ItemRoomRewardModifier
    {
        [SerializeField] private List<RoomType> supportedRoomTypes = new();
        [SerializeField] private bool allowOnNonCombatResolve;
        [SerializeField] [Min(0)] private int bonusRewardSelections;
        [SerializeField] [Min(0)] private int bonusItemRolls;

        public IReadOnlyList<RoomType> SupportedRoomTypes => supportedRoomTypes;
        public bool AllowOnNonCombatResolve => allowOnNonCombatResolve;
        public int BonusRewardSelections => Mathf.Max(0, bonusRewardSelections);
        public int BonusItemRolls => Mathf.Max(0, bonusItemRolls);

        public bool Supports(RoomType roomType, bool allowNonCombatResolve)
        {
            if (allowNonCombatResolve && !AllowOnNonCombatResolve)
            {
                return false;
            }

            if (supportedRoomTypes == null || supportedRoomTypes.Count == 0)
            {
                return true;
            }

            for (int index = 0; index < supportedRoomTypes.Count; index++)
            {
                if (supportedRoomTypes[index] == roomType)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
