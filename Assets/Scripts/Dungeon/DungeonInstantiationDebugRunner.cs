using CuteIssac.Core.Bootstrap;
using CuteIssac.Data.Dungeon;
using CuteIssac.Core.Run;
using UnityEngine;

namespace CuteIssac.Dungeon
{
    /// <summary>
    /// Editor-friendly helper that builds a dungeon graph and immediately instantiates it into the current scene.
    /// This keeps test setup light until a full runtime floor flow exists.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DungeonInstantiationDebugRunner : MonoBehaviour
    {
        [SerializeField] private FloorConfig floorConfig;
        [SerializeField] private DungeonInstantiator dungeonInstantiator;
        [SerializeField] private RunManager runManager;
        [SerializeField] private bool generateOnStart = true;
        [SerializeField] private bool useRandomSeed = true;
        [SerializeField] private int fixedSeed = 12345;

        public DungeonMap LastGeneratedMap { get; private set; }

        private void Start()
        {
            ResolveReferences();

            if (generateOnStart && ShouldAutoGenerateOnStart())
            {
                GenerateAndInstantiateDungeon();
            }
        }

        [ContextMenu("Generate And Instantiate Dungeon")]
        public void GenerateAndInstantiateDungeon()
        {
            if (floorConfig == null)
            {
                Debug.LogError("DungeonInstantiationDebugRunner requires a FloorConfig.", this);
                return;
            }

            if (dungeonInstantiator == null)
            {
                dungeonInstantiator = GetComponent<DungeonInstantiator>();
            }

            if (dungeonInstantiator == null)
            {
                Debug.LogError("DungeonInstantiationDebugRunner requires a DungeonInstantiator reference.", this);
                return;
            }

            int seed = useRandomSeed ? Random.Range(int.MinValue, int.MaxValue) : fixedSeed;
            GenerateAndInstantiateDungeon(floorConfig, seed);
        }

        public DungeonInstantiationResult GenerateAndInstantiateDungeon(FloorConfig overrideFloorConfig, int seed)
        {
            ResolveReferences();

            if (overrideFloorConfig == null)
            {
                Debug.LogError("DungeonInstantiationDebugRunner requires a FloorConfig.", this);
                return null;
            }

            if (dungeonInstantiator == null)
            {
                Debug.LogError("DungeonInstantiationDebugRunner requires a DungeonInstantiator reference.", this);
                return null;
            }

            if (dungeonInstantiator.CurrentInstance != null &&
                dungeonInstantiator.CurrentInstance.DungeonMap != null &&
                dungeonInstantiator.CurrentInstance.DungeonMap.FloorConfig == overrideFloorConfig &&
                dungeonInstantiator.CurrentInstance.DungeonMap.Seed == seed)
            {
                return dungeonInstantiator.CurrentInstance;
            }

            RoomGraphBuilder builder = new();
            LastGeneratedMap = builder.Build(overrideFloorConfig, seed);

            if (LastGeneratedMap == null)
            {
                return null;
            }

            DungeonInstantiationResult result = dungeonInstantiator.InstantiateDungeon(LastGeneratedMap);
            Debug.Log($"Generated and instantiated dungeon with seed {seed}\n{DungeonMapDebugFormatter.Format(LastGeneratedMap)}", this);
            return result;
        }

        private void ResolveReferences()
        {
            if (dungeonInstantiator == null)
            {
                dungeonInstantiator = GetComponent<DungeonInstantiator>();
            }

            if (runManager == null)
            {
                runManager = GetComponent<RunManager>();
            }
        }

        private bool ShouldDeferToActiveRun()
        {
            return runManager != null && runManager.CurrentContext.HasActiveRun;
        }

        private bool ShouldAutoGenerateOnStart()
        {
            if (ShouldDeferToActiveRun())
            {
                return false;
            }

            if (GetComponent<FloorTransitionController>() != null)
            {
                return false;
            }

            GameBootstrap bootstrap = GetComponent<GameBootstrap>();
            if (bootstrap != null)
            {
                return false;
            }

            return true;
        }
    }
}
