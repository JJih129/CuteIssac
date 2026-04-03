using System;
using System.Collections.Generic;
using CuteIssac.Data.Dungeon;
using UnityEngine;

namespace CuteIssac.Data.Item
{
    [Serializable]
    public sealed class ItemDoorCostModifier
    {
        [SerializeField] private List<RoomType> supportedRoomTypes = new();
        [SerializeField] [Min(0)] private int keyDiscount;
        [SerializeField] [Min(0f)] private float healthDiscount;

        public IReadOnlyList<RoomType> SupportedRoomTypes => supportedRoomTypes;
        public int KeyDiscount => Mathf.Max(0, keyDiscount);
        public float HealthDiscount => Mathf.Max(0f, healthDiscount);

        public bool Supports(RoomType roomType)
        {
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
