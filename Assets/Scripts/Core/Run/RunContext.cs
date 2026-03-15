using System;
using CuteIssac.Data.Dungeon;
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
        [field: SerializeField] public int TotalClearedRoomCount { get; private set; }
        [field: SerializeField] public int ResolvedRoomCount { get; private set; }
        [field: SerializeField] public int TotalResolvedRoomCount { get; private set; }
        [field: SerializeField] public int BossRoomClearCount { get; private set; }
        [field: SerializeField] public bool HasActiveRun { get; private set; }
        [field: SerializeField] public RunEndReason EndReason { get; private set; } = RunEndReason.None;
        [field: SerializeField] public RunState State { get; private set; } = RunState.Idle;

        public void Initialize(int seed, int startingFloorIndex)
        {
            Seed = seed;
            CurrentFloorIndex = Mathf.Max(1, startingFloorIndex);
            ClearedRoomCount = 0;
            TotalClearedRoomCount = 0;
            ResolvedRoomCount = 0;
            TotalResolvedRoomCount = 0;
            BossRoomClearCount = 0;
            HasActiveRun = true;
            EndReason = RunEndReason.None;
        }

        public void Restore(
            int seed,
            int currentFloorIndex,
            int clearedRoomCount,
            int totalClearedRoomCount,
            int resolvedRoomCount,
            int totalResolvedRoomCount,
            int bossRoomClearCount)
        {
            Seed = seed;
            CurrentFloorIndex = Mathf.Max(1, currentFloorIndex);
            ClearedRoomCount = Mathf.Max(0, clearedRoomCount);
            TotalClearedRoomCount = Mathf.Max(ClearedRoomCount, totalClearedRoomCount);
            ResolvedRoomCount = Mathf.Max(0, resolvedRoomCount);
            TotalResolvedRoomCount = Mathf.Max(ResolvedRoomCount, totalResolvedRoomCount);
            BossRoomClearCount = Mathf.Max(0, bossRoomClearCount);
            HasActiveRun = true;
            EndReason = RunEndReason.None;
        }

        public void Reset()
        {
            Seed = 0;
            CurrentFloorIndex = 0;
            ClearedRoomCount = 0;
            TotalClearedRoomCount = 0;
            ResolvedRoomCount = 0;
            TotalResolvedRoomCount = 0;
            BossRoomClearCount = 0;
            HasActiveRun = false;
            EndReason = RunEndReason.None;
            State = RunState.Idle;
        }

        public void RegisterRoomClear()
        {
            RegisterRoomResolution(RoomType.Normal, true);
        }

        public void RegisterRoomResolution(RoomType roomType, bool hadCombatEncounter)
        {
            if (!HasActiveRun)
            {
                return;
            }

            ResolvedRoomCount++;
            TotalResolvedRoomCount++;

            if (!hadCombatEncounter)
            {
                return;
            }

            ClearedRoomCount++;
            TotalClearedRoomCount++;

            if (roomType == RoomType.Boss)
            {
                BossRoomClearCount++;
            }
        }

        public void AdvanceFloor()
        {
            if (!HasActiveRun)
            {
                return;
            }

            CurrentFloorIndex++;
            ClearedRoomCount = 0;
            ResolvedRoomCount = 0;
        }

        public void SetState(RunState state)
        {
            State = state;

            if (state == RunState.Idle || state == RunState.FrontEnd)
            {
                HasActiveRun = false;
            }
        }

        public void SetEndReason(RunEndReason endReason)
        {
            EndReason = endReason;
        }
    }
}
