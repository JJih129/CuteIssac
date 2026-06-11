using System;
using System.Collections.Generic;
using UnityEngine;

namespace CuteIssac.Data.Dungeon
{
    [CreateAssetMenu(fileName = "RoomObstacleLayoutData", menuName = "CuteIssac/Data/Dungeon/Room Obstacle Layout")]
    public sealed class RoomObstacleLayoutData : ScriptableObject
    {
        [Serializable]
        public sealed class ObstacleEntry
        {
            [SerializeField] private GameObject prefab;
            [SerializeField] private Vector2 localPosition;
            [SerializeField] private float rotationZ;
            [SerializeField] [Min(0.1f)] private float scaleMultiplier = 1f;

            public GameObject Prefab => prefab;
            public Vector2 LocalPosition => localPosition;
            public float RotationZ => rotationZ;
            public float ScaleMultiplier => Mathf.Max(0.1f, scaleMultiplier);
        }

        [SerializeField] private string layoutId = "obstacle_layout";
        [SerializeField] private bool disablesObstacleSpawning;
        [SerializeField] private List<ObstacleEntry> obstacles = new();

        public string LayoutId => layoutId;
        public bool DisablesObstacleSpawning => disablesObstacleSpawning;
        public IReadOnlyList<ObstacleEntry> Obstacles => obstacles;
        public bool HasAuthoredObstacles => !disablesObstacleSpawning && obstacles.Count > 0;
    }
}
