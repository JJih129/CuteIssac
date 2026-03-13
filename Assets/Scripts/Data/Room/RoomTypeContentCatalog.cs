using System;
using System.Collections.Generic;
using CuteIssac.Data.Dungeon;
using UnityEngine;

namespace CuteIssac.Data.Room
{
    /// <summary>
    /// Maps RoomType to presentation-facing room content rules.
    /// Generated room instances can read this once and stay agnostic about concrete treasure/shop/boss prefab choices.
    /// </summary>
    [CreateAssetMenu(fileName = "RoomTypeContentCatalog", menuName = "CuteIssac/Data/Room/Room Type Content Catalog")]
    public sealed class RoomTypeContentCatalog : ScriptableObject
    {
        [SerializeField] private List<RoomTypeContentEntry> entries = new();

        public bool TryGetEntry(RoomType roomType, out RoomTypeContentEntry entry)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                RoomTypeContentEntry candidate = entries[i];

                if (candidate != null && candidate.RoomType == roomType)
                {
                    entry = candidate;
                    return true;
                }
            }

            entry = null;
            return false;
        }
    }

    [Serializable]
    public sealed class RoomTypeContentEntry
    {
        [SerializeField] private RoomType roomType = RoomType.Normal;
        [SerializeField] private RoomRewardTable rewardTableOverride;
        [SerializeField] private GameObject entryContentPrefab;
        [SerializeField] private bool spawnContentOnFirstEntry = true;
        [SerializeField] private bool applyRoomTint;
        [SerializeField] private Color roomTintColor = Color.white;

        public RoomType RoomType => roomType;
        public RoomRewardTable RewardTableOverride => rewardTableOverride;
        public GameObject EntryContentPrefab => entryContentPrefab;
        public bool SpawnContentOnFirstEntry => spawnContentOnFirstEntry && entryContentPrefab != null;
        public bool ApplyRoomTint => applyRoomTint;
        public Color RoomTintColor => roomTintColor;
    }
}
