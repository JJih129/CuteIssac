using System;
using System.Collections.Generic;
using CuteIssac.Data.Dungeon;
using UnityEngine;

namespace CuteIssac.Data.Item
{
    /// <summary>
    /// One weighted passive item candidate inside an item pool.
    /// </summary>
    [Serializable]
    public struct ItemPoolEntry
    {
        [SerializeField] private ItemData itemData;
        [SerializeField] [Min(0f)] private float weight;
        [SerializeField] [Min(1)] private int minimumFloor;
        [SerializeField] [Min(0)] private int maximumFloor;
        [SerializeField] private List<RoomType> allowedRoomTypes;

        public ItemData ItemData => itemData;
        public float Weight => Mathf.Max(0f, weight);
        public bool IsValid => itemData != null && Weight > 0f;

        public bool IsAvailableFor(RoomType roomType, int floorIndex)
        {
            if (!IsValid)
            {
                return false;
            }

            int resolvedMinFloor = Mathf.Max(1, minimumFloor);

            if (floorIndex > 0 && floorIndex < resolvedMinFloor)
            {
                return false;
            }

            if (maximumFloor > 0 && floorIndex > maximumFloor)
            {
                return false;
            }

            if (allowedRoomTypes == null || allowedRoomTypes.Count == 0)
            {
                return true;
            }

            for (int index = 0; index < allowedRoomTypes.Count; index++)
            {
                if (allowedRoomTypes[index] == roomType)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
