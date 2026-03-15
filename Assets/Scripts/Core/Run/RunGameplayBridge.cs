using CuteIssac.Core.Gameplay;
using CuteIssac.Data.Dungeon;
using CuteIssac.Dungeon;
using CuteIssac.Player;
using CuteIssac.Room;
using UnityEngine;

namespace CuteIssac.Core.Run
{
    /// <summary>
    /// Bridges runtime gameplay events into run lifecycle updates without coupling room or player logic to RunManager.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RunGameplayBridge : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private RunManager runManager;
        [SerializeField] private DungeonInstantiator dungeonInstantiator;
        [SerializeField] private PlayerHealth playerHealth;

        private PlayerHealth _subscribedPlayerHealth;

        private void Awake()
        {
            ResolveReferences();
        }

        private void OnEnable()
        {
            ResolveReferences();
            SubscribeCoreSources();
            RebindPlayerHealth();
        }

        private void OnDisable()
        {
            UnsubscribeCoreSources();
            UnbindPlayerHealth();
        }

        private void HandleDungeonInstantiated(DungeonInstantiationResult result)
        {
            ResolveReferences();
            RebindPlayerHealth();
        }

        private void HandlePlayerDied()
        {
            if (runManager == null || !runManager.CurrentContext.HasActiveRun)
            {
                return;
            }

            if (runManager.CurrentState == RunState.Defeat || runManager.CurrentState == RunState.Victory)
            {
                return;
            }

            runManager.EndRun(RunEndReason.Defeat);
        }

        private void HandleRoomResolved(RoomResolvedSignal signal)
        {
            if (runManager == null || signal.Room == null || !runManager.CurrentContext.HasActiveRun)
            {
                return;
            }

            if (signal.RoomType != RoomType.Start)
            {
                runManager.RegisterRoomResolved(signal.RoomType, signal.HadCombatEncounter);
            }
        }

        private void ResolveReferences()
        {
            if (runManager == null)
            {
                runManager = GetComponent<RunManager>();
            }

            if (dungeonInstantiator == null)
            {
                dungeonInstantiator = GetComponent<DungeonInstantiator>();
            }

            if (playerHealth == null)
            {
                playerHealth = FindFirstObjectByType<PlayerHealth>(FindObjectsInactive.Exclude);
            }
        }

        private void SubscribeCoreSources()
        {
            if (dungeonInstantiator != null)
            {
                dungeonInstantiator.DungeonInstantiated -= HandleDungeonInstantiated;
                dungeonInstantiator.DungeonInstantiated += HandleDungeonInstantiated;
            }

            GameplayRuntimeEvents.RoomResolved -= HandleRoomResolved;
            GameplayRuntimeEvents.RoomResolved += HandleRoomResolved;
        }

        private void UnsubscribeCoreSources()
        {
            if (dungeonInstantiator != null)
            {
                dungeonInstantiator.DungeonInstantiated -= HandleDungeonInstantiated;
            }

            GameplayRuntimeEvents.RoomResolved -= HandleRoomResolved;
        }

        private void RebindPlayerHealth()
        {
            if (playerHealth == null)
            {
                playerHealth = FindFirstObjectByType<PlayerHealth>(FindObjectsInactive.Exclude);
            }

            if (_subscribedPlayerHealth == playerHealth)
            {
                return;
            }

            UnbindPlayerHealth();

            if (playerHealth != null)
            {
                playerHealth.Died -= HandlePlayerDied;
                playerHealth.Died += HandlePlayerDied;
                _subscribedPlayerHealth = playerHealth;
            }
        }

        private void UnbindPlayerHealth()
        {
            if (_subscribedPlayerHealth == null)
            {
                return;
            }

            _subscribedPlayerHealth.Died -= HandlePlayerDied;
            _subscribedPlayerHealth = null;
        }

        private void OnValidate()
        {
            ResolveReferences();
        }
    }
}
