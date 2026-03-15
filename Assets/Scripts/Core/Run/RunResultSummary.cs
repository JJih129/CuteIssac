using System;

namespace CuteIssac.Core.Run
{
    /// <summary>
    /// Lightweight presentation-ready run summary.
    /// UI can render this without depending on gameplay components directly.
    /// </summary>
    [Serializable]
    public sealed class RunResultSummary
    {
        public RunResultSummary(
            string title,
            string subtitle,
            int collectedItemCount,
            int clearedRoomCount,
            int resolvedRoomCount,
            int coins,
            int keys,
            int bombs,
            int reachedFloor,
            RunEndReason endReason)
        {
            Title = title ?? string.Empty;
            Subtitle = subtitle ?? string.Empty;
            CollectedItemCount = Math.Max(0, collectedItemCount);
            ClearedRoomCount = Math.Max(0, clearedRoomCount);
            ResolvedRoomCount = Math.Max(0, resolvedRoomCount);
            Coins = Math.Max(0, coins);
            Keys = Math.Max(0, keys);
            Bombs = Math.Max(0, bombs);
            ReachedFloor = Math.Max(1, reachedFloor);
            EndReason = endReason;
        }

        public string Title { get; }
        public string Subtitle { get; }
        public int CollectedItemCount { get; }
        public int ClearedRoomCount { get; }
        public int ResolvedRoomCount { get; }
        public int Coins { get; }
        public int Keys { get; }
        public int Bombs { get; }
        public int ReachedFloor { get; }
        public RunEndReason EndReason { get; }
    }
}
