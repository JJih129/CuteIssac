using System;
using CuteIssac.Data.Dungeon;

namespace CuteIssac.Dungeon
{
    /// <summary>
    /// Serializable runtime snapshot of one room's exploration state.
    /// Save systems can reuse this without depending on UI node views.
    /// </summary>
    [Serializable]
    public struct RoomExplorationSaveRecord
    {
        public RoomExplorationSaveRecord(
            int gridX,
            int gridY,
            RoomType roomType,
            RoomExplorationState explorationState,
            bool isCleared,
            bool hasRewardContent,
            bool hasCollectedRewardContent)
        {
            GridX = gridX;
            GridY = gridY;
            RoomType = roomType;
            ExplorationState = explorationState;
            IsCleared = isCleared;
            HasRewardContent = hasRewardContent;
            HasCollectedRewardContent = hasCollectedRewardContent;
        }

        public int GridX;
        public int GridY;
        public RoomType RoomType;
        public RoomExplorationState ExplorationState;
        public bool IsCleared;
        public bool HasRewardContent;
        public bool HasCollectedRewardContent;
    }
}
