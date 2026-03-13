using CuteIssac.Data.Dungeon;
using CuteIssac.Dungeon;

namespace CuteIssac.UI
{
    /// <summary>
    /// Immutable data packet consumed by the minimap view.
    /// The view should not need room controllers or dungeon objects once this snapshot is built.
    /// </summary>
    public readonly struct MinimapRoomViewData
    {
        public MinimapRoomViewData(GridPosition gridPosition, RoomType roomType, RoomExplorationState explorationState, bool isCleared)
        {
            GridPosition = gridPosition;
            RoomType = roomType;
            ExplorationState = explorationState;
            IsCleared = isCleared;
        }

        public GridPosition GridPosition { get; }
        public RoomType RoomType { get; }
        public RoomExplorationState ExplorationState { get; }
        public bool IsCleared { get; }
    }
}
