using System.Collections.Generic;
using CuteIssac.Data.Dungeon;
using UnityEngine;

namespace CuteIssac.Data.Room
{
    /// <summary>
    /// Authoring asset for room clear rewards.
    /// Room controllers never know concrete pickup prefabs directly; they ask a spawner to read this table instead.
    /// </summary>
    [CreateAssetMenu(fileName = "RoomRewardTable", menuName = "CuteIssac/Data/Room/Room Reward Table")]
    public sealed class RoomRewardTable : ScriptableObject
    {
        [SerializeField] [Min(1)] private int minimumRewardSelections = 1;
        [SerializeField] [Min(1)] private int maximumRewardSelections = 1;
        [SerializeField] private bool allowDuplicateSelections = true;
        [SerializeField] private List<RoomRewardEntry> rewardEntries = new();

        public int MinimumRewardSelections => Mathf.Max(1, minimumRewardSelections);
        public int MaximumRewardSelections => Mathf.Max(MinimumRewardSelections, maximumRewardSelections);
        public bool AllowDuplicateSelections => allowDuplicateSelections;
        public IReadOnlyList<RoomRewardEntry> RewardEntries => rewardEntries;

        public int GetSelectionCount()
        {
            int minimum = MinimumRewardSelections;
            int maximum = MaximumRewardSelections;
            return Random.Range(minimum, maximum + 1);
        }

        public void CollectCandidates(RoomType roomType, List<RoomRewardEntry> buffer)
        {
            buffer.Clear();

            for (int i = 0; i < rewardEntries.Count; i++)
            {
                RoomRewardEntry entry = rewardEntries[i];

                if (entry.IsValid && entry.Supports(roomType))
                {
                    buffer.Add(entry);
                }
            }
        }
    }
}
