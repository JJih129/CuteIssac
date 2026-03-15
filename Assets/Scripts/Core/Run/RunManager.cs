using System;
using CuteIssac.Data.Dungeon;
using CuteIssac.Data.Run;
using UnityEngine;
using Random = UnityEngine.Random;

namespace CuteIssac.Core.Run
{
    /// <summary>
    /// Owns the current run lifecycle and exposes simple hooks for future room, dungeon, and UI systems.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RunManager : MonoBehaviour
    {
        [Header("Fallback Settings")]
        [SerializeField] [Min(1)] private int fallbackStartingFloorIndex = 1;
        [SerializeField] private bool useFixedSeed;
        [SerializeField] private int fixedSeed = 12345;

        public event Action<RunState> StateChanged;
        public event Action<RunContext> RunStarted;
        public event Action<RunContext, RunEndReason> RunEnded;
        public event Action<RunFloorTransitionInfo> FloorTransitionStarted;
        public event Action<RunFloorTransitionInfo> FloorTransitionCompleted;

        public RunState CurrentState => _context.State;
        public RunContext CurrentContext => _context;
        public RunConfiguration Configuration => _runConfiguration;

        private readonly RunContext _context = new();
        private RunConfiguration _runConfiguration;
        private bool _isBootstrapped;

        public void Bootstrap(RunConfiguration runConfiguration)
        {
            _runConfiguration = runConfiguration;
            _isBootstrapped = true;

            if (_context.State == RunState.Idle)
            {
                ChangeState(RunState.FrontEnd);
            }
        }

        [ContextMenu("Start New Run")]
        public void StartNewRun()
        {
            EnsureBootstrapped();

            ChangeState(RunState.StartingRun);

            int seed = ResolveSeed();
            int startingFloorIndex = ResolveStartingFloorIndex();

            _context.Initialize(seed, startingFloorIndex);
            ChangeState(RunState.InRun);
            RunStarted?.Invoke(_context);
        }

        public void StartRestoredRun(RunSaveData saveData)
        {
            if (saveData == null)
            {
                return;
            }

            EnsureBootstrapped();
            ChangeState(RunState.StartingRun);
            _context.Restore(
                saveData.DungeonSeed,
                saveData.CurrentFloorIndex,
                saveData.ClearedRoomCount,
                saveData.TotalClearedRoomCount,
                saveData.ResolvedRoomCount,
                saveData.TotalResolvedRoomCount,
                saveData.BossRoomClearCount);
            ChangeState(RunState.InRun);
            RunStarted?.Invoke(_context);
        }

        public void SetPaused(bool paused)
        {
            if (paused)
            {
                if (CurrentState == RunState.InRun)
                {
                    ChangeState(RunState.Paused);
                }

                return;
            }

            if (CurrentState == RunState.Paused)
            {
                ChangeState(RunState.InRun);
            }
        }

        public void RegisterRoomClear()
        {
            if (CurrentState != RunState.InRun)
            {
                return;
            }

            _context.RegisterRoomResolution(RoomType.Normal, true);
        }

        public void RegisterRoomResolved(RoomType roomType, bool hadCombatEncounter)
        {
            if (CurrentState != RunState.InRun)
            {
                return;
            }

            _context.RegisterRoomResolution(roomType, hadCombatEncounter);
        }

        public void AdvanceFloor()
        {
            if (CurrentState != RunState.InRun)
            {
                return;
            }

            int previousFloorIndex = _context.CurrentFloorIndex;
            ChangeState(RunState.TransitioningFloor);

            RunFloorTransitionInfo transitionInfo =
                new(_context.Seed, previousFloorIndex, previousFloorIndex + 1);

            FloorTransitionStarted?.Invoke(transitionInfo);
            _context.AdvanceFloor();
            FloorTransitionCompleted?.Invoke(transitionInfo);
            ChangeState(RunState.InRun);
        }

        public void EndRun(bool cleared)
        {
            EndRun(cleared ? RunEndReason.Victory : RunEndReason.Defeat);
        }

        public void EndRun(RunEndReason endReason)
        {
            if (!_context.HasActiveRun)
            {
                return;
            }

            _context.SetEndReason(endReason);

            ChangeState(endReason == RunEndReason.Victory ? RunState.Victory : RunState.Defeat);
            RunEnded?.Invoke(_context, endReason);
        }

        public void AbortRun()
        {
            EndRun(RunEndReason.Abandoned);
        }

        public void ReturnToFrontEnd()
        {
            _context.Reset();
            ChangeState(RunState.FrontEnd);
        }

        private void EnsureBootstrapped()
        {
            if (_isBootstrapped)
            {
                return;
            }

            Bootstrap(null);
        }

        private int ResolveStartingFloorIndex()
        {
            if (_runConfiguration != null)
            {
                return _runConfiguration.StartingFloorIndex;
            }

            return Mathf.Max(1, fallbackStartingFloorIndex);
        }

        private int ResolveSeed()
        {
            if (_runConfiguration != null)
            {
                return _runConfiguration.UseFixedSeed
                    ? _runConfiguration.FixedSeed
                    : Random.Range(int.MinValue, int.MaxValue);
            }

            return useFixedSeed
                ? fixedSeed
                : Random.Range(int.MinValue, int.MaxValue);
        }

        public bool TryGetFloorConfig(int floorIndex, out FloorConfig floorConfig)
        {
            if (_runConfiguration != null && _runConfiguration.TryGetFloorConfig(floorIndex, out floorConfig))
            {
                return true;
            }

            floorConfig = null;
            return false;
        }

        public bool HasFloor(int floorIndex)
        {
            return TryGetFloorConfig(floorIndex, out _);
        }

        private void ChangeState(RunState nextState)
        {
            if (_context.State == nextState)
            {
                return;
            }

            _context.SetState(nextState);
            StateChanged?.Invoke(nextState);
        }
    }
}
