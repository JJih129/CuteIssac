using System;

namespace CuteIssac.Core.Run
{
    /// <summary>
    /// Immutable payload for floor transition notifications.
    /// </summary>
    [Serializable]
    public readonly struct RunFloorTransitionInfo
    {
        public RunFloorTransitionInfo(int seed, int previousFloorIndex, int nextFloorIndex)
        {
            Seed = seed;
            PreviousFloorIndex = previousFloorIndex;
            NextFloorIndex = nextFloorIndex;
        }

        public int Seed { get; }
        public int PreviousFloorIndex { get; }
        public int NextFloorIndex { get; }
    }
}
