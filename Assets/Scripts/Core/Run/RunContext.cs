using System;
using UnityEngine;

namespace CuteIssac.Core.Run
{
    /// <summary>
    /// Runtime snapshot for the current run. Other systems can read this without depending on scene objects.
    /// </summary>
    [Serializable]
    public sealed class RunContext
    {
        [field: SerializeField] public int Seed { get; private set; }
        [field: SerializeField] public int CurrentFloorIndex { get; private set; }
        [field: SerializeField] public int ClearedRoomCount { get; private set; }
        [field: SerializeField] public bool HasActiveRun { get; private set; }
        [field: SerializeField] public RunState State { get; private set; } = RunState.Idle;

        public void Initialize(int seed, int startingFloorIndex)
        {
            Seed = seed;
            CurrentFloorIndex = Mathf.Max(1, startingFloorIndex);
            ClearedRoomCount = 0;
            HasActiveRun = true;
        }

        public void Reset()
        {
            Seed = 0;
            CurrentFloorIndex = 0;
            ClearedRoomCount = 0;
            HasActiveRun = false;
            State = RunState.Idle;
        }

        public void RegisterRoomClear()
        {
            if (!HasActiveRun)
            {
                return;
            }

            ClearedRoomCount++;
        }

        public void AdvanceFloor()
        {
            if (!HasActiveRun)
            {
                return;
            }

            CurrentFloorIndex++;
            ClearedRoomCount = 0;
        }

        public void SetState(RunState state)
        {
            State = state;

            if (state == RunState.Idle || state == RunState.FrontEnd)
            {
                HasActiveRun = false;
            }
        }
    }
}
