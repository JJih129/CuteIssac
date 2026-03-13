using System;
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
        public event Action<RunContext> RunEnded;

        public RunState CurrentState => _context.State;
        public RunContext CurrentContext => _context;

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

            _context.RegisterRoomClear();
        }

        public void AdvanceFloor()
        {
            if (CurrentState != RunState.InRun)
            {
                return;
            }

            _context.AdvanceFloor();
        }

        public void EndRun(bool cleared)
        {
            if (!_context.HasActiveRun)
            {
                return;
            }

            ChangeState(cleared ? RunState.Victory : RunState.Defeat);
            RunEnded?.Invoke(_context);
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
