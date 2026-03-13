using System.Collections.Generic;
using CuteIssac.Data.Enemy;
using UnityEngine;

namespace CuteIssac.Data.Dungeon
{
    /// <summary>
    /// Authored room metadata used by generation.
    /// This holds stable designer-owned data such as room type, generation hints, and optional room-specific layout candidates.
    /// </summary>
    [CreateAssetMenu(fileName = "RoomData", menuName = "CuteIssac/Data/Dungeon/Room Data")]
    public sealed class RoomData : ScriptableObject
    {
        [SerializeField] private string roomId = "room";
        [SerializeField] private string displayName = "Room";
        [SerializeField] private RoomType roomType = RoomType.Normal;
        [SerializeField] [Min(1)] private int widthInCells = 1;
        [SerializeField] [Min(1)] private int heightInCells = 1;
        [SerializeField] [Min(0)] private int generationWeight = 1;
        [SerializeField] private EnemyWaveData enemyWaveOverride;
        [SerializeField] private List<RoomLayoutData> localLayouts = new();

        public string RoomId => roomId;
        public string DisplayName => displayName;
        public RoomType RoomType => roomType;
        public int WidthInCells => widthInCells;
        public int HeightInCells => heightInCells;
        public int GenerationWeight => generationWeight;
        public EnemyWaveData EnemyWaveOverride => enemyWaveOverride;
        public IReadOnlyList<RoomLayoutData> LocalLayouts => localLayouts;

        public void CollectLocalLayouts(List<RoomLayoutData> results)
        {
            if (results == null)
            {
                return;
            }

            for (int i = 0; i < localLayouts.Count; i++)
            {
                RoomLayoutData layout = localLayouts[i];

                if (layout != null && !results.Contains(layout))
                {
                    results.Add(layout);
                }
            }
        }
    }
}
