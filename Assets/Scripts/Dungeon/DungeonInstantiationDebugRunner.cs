using CuteIssac.Data.Dungeon;
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
        [SerializeField] private bool generateOnStart = true;
        [SerializeField] private bool useRandomSeed = true;
        [SerializeField] private int fixedSeed = 12345;

        public DungeonMap LastGeneratedMap { get; private set; }

        private void Start()
        {
            if (generateOnStart)
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
            RoomGraphBuilder builder = new();
            LastGeneratedMap = builder.Build(floorConfig, seed);

            if (LastGeneratedMap == null)
            {
                return;
            }

            dungeonInstantiator.InstantiateDungeon(LastGeneratedMap);
            Debug.Log($"Generated and instantiated dungeon with seed {seed}\n{DungeonMapDebugFormatter.Format(LastGeneratedMap)}", this);
        }
    }
}
