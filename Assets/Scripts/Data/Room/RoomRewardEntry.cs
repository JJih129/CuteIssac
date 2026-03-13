using System;
using System.Collections.Generic;
using CuteIssac.Data.Dungeon;
using UnityEngine;

namespace CuteIssac.Data.Room
{
    /// <summary>
    /// One weighted reward option in a room reward table.
    /// The pickup prefab holds the concrete gameplay logic, while this entry controls when and how often it can appear.
    /// </summary>
    [Serializable]
    public struct RoomRewardEntry
    {
        [SerializeField] private RoomRewardType rewardType;
        [SerializeField] private GameObject pickupPrefab;
        [SerializeField] [Min(1)] private int quantity;
        [SerializeField] [Min(0f)] private float weight;
        [SerializeField] private List<RoomType> supportedRoomTypes;

        public RoomRewardType RewardType => rewardType;
        public GameObject PickupPrefab => pickupPrefab;
        public int Quantity => Mathf.Max(1, quantity);
        public float Weight => Mathf.Max(0f, weight);

        public bool IsValid => pickupPrefab != null && Weight > 0f;

        public bool Supports(RoomType roomType)
        {
            if (supportedRoomTypes == null || supportedRoomTypes.Count == 0)
            {
                return true;
            }

            for (int i = 0; i < supportedRoomTypes.Count; i++)
            {
                if (supportedRoomTypes[i] == roomType)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
