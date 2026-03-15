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
        public MinimapRoomViewData(
            GridPosition gridPosition,
            RoomType roomType,
            RoomExplorationState explorationState,
            bool hasUpConnection,
            bool hasDownConnection,
            bool hasLeftConnection,
            bool hasRightConnection,
            bool hasUpSecretConnection,
            bool hasDownSecretConnection,
            bool hasLeftSecretConnection,
            bool hasRightSecretConnection,
            bool isCleared,
            bool hasRewardContent,
            bool hasCollectedRewardContent)
        {
            GridPosition = gridPosition;
            RoomType = roomType;
            ExplorationState = explorationState;
            HasUpConnection = hasUpConnection;
            HasDownConnection = hasDownConnection;
            HasLeftConnection = hasLeftConnection;
            HasRightConnection = hasRightConnection;
            HasUpSecretConnection = hasUpSecretConnection;
            HasDownSecretConnection = hasDownSecretConnection;
            HasLeftSecretConnection = hasLeftSecretConnection;
            HasRightSecretConnection = hasRightSecretConnection;
            IsCleared = isCleared;
            HasRewardContent = hasRewardContent;
            HasCollectedRewardContent = hasCollectedRewardContent;
        }

        public GridPosition GridPosition { get; }
        public RoomType RoomType { get; }
        public RoomExplorationState ExplorationState { get; }
        public bool HasUpConnection { get; }
        public bool HasDownConnection { get; }
        public bool HasLeftConnection { get; }
        public bool HasRightConnection { get; }
        public bool HasUpSecretConnection { get; }
        public bool HasDownSecretConnection { get; }
        public bool HasLeftSecretConnection { get; }
        public bool HasRightSecretConnection { get; }
        public bool IsCleared { get; }
        public bool HasRewardContent { get; }
        public bool HasCollectedRewardContent { get; }
    }
}
