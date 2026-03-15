using System;
using System.Collections.Generic;
using UnityEngine;

namespace CuteIssac.Data.Room
{
    /// <summary>
    /// Presentation-only room skin data.
    /// Designers can swap this asset per floor to reskin generated rooms without changing room logic.
    /// </summary>
    [CreateAssetMenu(fileName = "RoomThemeData", menuName = "CuteIssac/Data/Room/Room Theme")]
    public sealed class RoomThemeData : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string themeId = "default_room_theme";
        [SerializeField] private string displayName = "Default Theme";

        [Header("Prefab Overrides")]
        [Tooltip("Optional replacement visual spawned under the floor anchor. Leave empty to keep the built-in floor renderer.")]
        [SerializeField] private GameObject floorVisualPrefab;
        [Tooltip("Optional replacement visual spawned under every wall anchor. Leave empty to keep the built-in wall renderers.")]
        [SerializeField] private GameObject wallVisualPrefab;
        [Tooltip("Optional replacement visual spawned under every door anchor. Leave empty to keep the built-in door renderers.")]
        [SerializeField] private GameObject doorVisualPrefab;
        [Tooltip("Optional decoration prefabs spawned at the room decoration anchor.")]
        [SerializeField] private List<GameObject> decorationPrefabs = new();

        [Header("Fallback Colors")]
        [Tooltip("Used when no floor prefab override is assigned.")]
        [SerializeField] private Color floorColor = new(0.16f, 0.23f, 0.35f, 0.22f);
        [Tooltip("Used when no wall prefab override is assigned.")]
        [SerializeField] private Color wallColor = new(0.1f, 0.16f, 0.24f, 0.95f);
        [Tooltip("Used when no door prefab override is assigned.")]
        [SerializeField] private Color doorColor = new(0.45f, 0.75f, 0.96f, 0.9f);

        public string ThemeId => themeId;
        public string DisplayName => displayName;
        public GameObject FloorVisualPrefab => floorVisualPrefab;
        public GameObject WallVisualPrefab => wallVisualPrefab;
        public GameObject DoorVisualPrefab => doorVisualPrefab;
        public IReadOnlyList<GameObject> DecorationPrefabs => decorationPrefabs;
        public Color FloorColor => floorColor;
        public Color WallColor => wallColor;
        public Color DoorColor => doorColor;

        [Serializable]
        public struct DecorationEntry
        {
            public GameObject Prefab;
            public Vector2 LocalOffset;
            public float RotationZ;
            public Vector3 Scale;
        }
    }
}
