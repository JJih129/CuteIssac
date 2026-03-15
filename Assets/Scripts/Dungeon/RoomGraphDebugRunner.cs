using CuteIssac.Data.Dungeon;
using UnityEngine;

namespace CuteIssac.Dungeon
{
    /// <summary>
    /// Small scene helper for generating and visualizing a room graph before scene instantiation is implemented.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RoomGraphDebugRunner : MonoBehaviour
    {
        [Header("Generation")]
        [SerializeField] private FloorConfig floorConfig;
        [SerializeField] private bool useRandomSeed = true;
        [SerializeField] private int fixedSeed = 12345;

        [Header("Gizmos")]
        [SerializeField] private bool drawGizmos = true;
        [SerializeField] private float gizmoCellSize = 4f;
        [SerializeField] private Color startRoomColor = new(0.35f, 0.85f, 0.35f, 0.9f);
        [SerializeField] private Color normalRoomColor = new(0.35f, 0.65f, 0.95f, 0.9f);
        [SerializeField] private Color treasureRoomColor = new(0.95f, 0.82f, 0.3f, 0.9f);
        [SerializeField] private Color shopRoomColor = new(0.3f, 0.95f, 0.82f, 0.9f);
        [SerializeField] private Color bossRoomColor = new(0.95f, 0.3f, 0.3f, 0.9f);
        [SerializeField] private Color secretRoomColor = new(0.75f, 0.45f, 0.95f, 0.9f);
        [SerializeField] private Color curseRoomColor = new(0.84f, 0.32f, 0.66f, 0.9f);
        [SerializeField] private Color connectionColor = new(1f, 0.85f, 0.2f, 1f);

        private DungeonMap _lastGeneratedMap;

        [ContextMenu("Generate Room Graph")]
        public void GenerateRoomGraph()
        {
            int seed = useRandomSeed ? Random.Range(int.MinValue, int.MaxValue) : fixedSeed;
            RoomGraphBuilder builder = new();
            _lastGeneratedMap = builder.Build(floorConfig, seed);

            if (_lastGeneratedMap != null)
            {
                Debug.Log($"Generated room graph with seed {seed}\n{DungeonMapDebugFormatter.Format(_lastGeneratedMap)}", this);
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (!drawGizmos || _lastGeneratedMap == null)
            {
                return;
            }

            foreach (var roomPair in _lastGeneratedMap.RoomsByPosition)
            {
                DungeonRoomNode roomNode = roomPair.Value;
                Vector3 roomPosition = ToWorld(roomNode.GridPosition);

                Gizmos.color = ResolveRoomColor(roomNode.RoomType);
                Gizmos.DrawCube(roomPosition, Vector3.one * (gizmoCellSize * 0.45f));

                Gizmos.color = connectionColor;

                for (int i = 0; i < roomNode.Connections.Count; i++)
                {
                    Vector3 targetPosition = ToWorld(roomNode.Connections[i].TargetPosition);
                    Gizmos.DrawLine(roomPosition, targetPosition);
                }
            }
        }

        private Vector3 ToWorld(GridPosition gridPosition)
        {
            return transform.position + new Vector3(gridPosition.X * gizmoCellSize, gridPosition.Y * gizmoCellSize, 0f);
        }

        private Color ResolveRoomColor(RoomType roomType)
        {
            return roomType switch
            {
                RoomType.Start => startRoomColor,
                RoomType.Treasure => treasureRoomColor,
                RoomType.Shop => shopRoomColor,
                RoomType.Boss => bossRoomColor,
                RoomType.Secret => secretRoomColor,
                RoomType.Curse => curseRoomColor,
                _ => normalRoomColor
            };
        }
    }
}
