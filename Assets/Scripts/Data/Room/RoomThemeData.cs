using System;
using System.Collections.Generic;
using CuteIssac.Data.Dungeon;
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

        [Header("Whole Room Override")]
        [Tooltip("Optional full-room visual spawned once for the whole room. Use this for illustrated stage backdrops that already include floor and wall art.")]
        [SerializeField] private GameObject roomVisualPrefab;
        [Tooltip("When enabled, fallback floor/wall/door renderers are hidden while the whole-room visual is active.")]
        [SerializeField] private bool hideFallbackGeometryWhenRoomVisualIsPresent = true;

        [Header("Prefab Overrides")]
        [Tooltip("Optional replacement visual spawned under the floor anchor. Leave empty to keep the built-in floor renderer.")]
        [SerializeField] private GameObject floorVisualPrefab;
        [Tooltip("Optional replacement visual spawned under every wall anchor. Leave empty to keep the built-in wall renderers.")]
        [SerializeField] private GameObject wallVisualPrefab;
        [Tooltip("Optional replacement visual spawned under every door anchor. Leave empty to keep the built-in door renderers.")]
        [SerializeField] private GameObject doorVisualPrefab;
        [Tooltip("Optional decoration prefabs spawned at the room decoration anchor.")]
        [SerializeField] private List<GameObject> decorationPrefabs = new();
        [Tooltip("Optional room-type-specific landmarks. These stay presentation-only so gameplay code remains asset-agnostic.")]
        [SerializeField] private List<RoomTypeLandmarkEntry> roomTypeLandmarks = new();

        [Header("Theme Palette")]
        [SerializeField] private Color primaryAccentColor = new(0.97f, 0.78f, 0.84f, 1f);
        [SerializeField] private Color secondaryAccentColor = new(0.74f, 0.92f, 0.86f, 1f);
        [SerializeField] private Color tertiaryAccentColor = new(0.95f, 0.88f, 0.56f, 1f);
        [SerializeField] private Color highlightAccentColor = new(0.99f, 0.95f, 0.92f, 1f);
        [SerializeField] private Color shadowAccentColor = new(0.43f, 0.28f, 0.26f, 1f);

        [Header("Fallback Colors")]
        [Tooltip("Used when no floor prefab override is assigned.")]
        [SerializeField] private Color floorColor = new(0.16f, 0.23f, 0.35f, 0.22f);
        [Tooltip("Used when no wall prefab override is assigned.")]
        [SerializeField] private Color wallColor = new(0.1f, 0.16f, 0.24f, 0.95f);
        [Tooltip("Used when no door prefab override is assigned.")]
        [SerializeField] private Color doorColor = new(0.45f, 0.75f, 0.96f, 0.9f);

        public string ThemeId => themeId;
        public string DisplayName => displayName;
        public GameObject RoomVisualPrefab => roomVisualPrefab;
        public bool HideFallbackGeometryWhenRoomVisualIsPresent => hideFallbackGeometryWhenRoomVisualIsPresent;
        public GameObject FloorVisualPrefab => floorVisualPrefab;
        public GameObject WallVisualPrefab => wallVisualPrefab;
        public GameObject DoorVisualPrefab => doorVisualPrefab;
        public IReadOnlyList<GameObject> DecorationPrefabs => decorationPrefabs;
        public IReadOnlyList<RoomTypeLandmarkEntry> RoomTypeLandmarks => roomTypeLandmarks;
        public Color PrimaryAccentColor => primaryAccentColor;
        public Color SecondaryAccentColor => secondaryAccentColor;
        public Color TertiaryAccentColor => tertiaryAccentColor;
        public Color HighlightAccentColor => highlightAccentColor;
        public Color ShadowAccentColor => shadowAccentColor;
        public Color FloorColor => floorColor;
        public Color WallColor => wallColor;
        public Color DoorColor => doorColor;

        public void CollectLandmarkEntries(RoomType roomType, List<RoomTypeLandmarkEntry> results)
        {
            if (results == null)
            {
                return;
            }

            for (int index = 0; index < roomTypeLandmarks.Count; index++)
            {
                RoomTypeLandmarkEntry candidate = roomTypeLandmarks[index];

                if (candidate.RoomType == roomType)
                {
                    results.Add(candidate);
                }
            }
        }

        [Serializable]
        public struct DecorationEntry
        {
            public GameObject Prefab;
            public Vector2 LocalOffset;
            public float RotationZ;
            public Vector3 Scale;
        }

        [Serializable]
        public struct RoomTypeLandmarkEntry
        {
            [SerializeField] private RoomType roomType;
            [SerializeField] private GameObject landmarkPrefab;
            [SerializeField] private bool useRuntimeFallback;
            [SerializeField] private Vector2 localOffset;
            [SerializeField] private float rotationZ;
            [SerializeField] private Vector3 scale;

            public RoomType RoomType => roomType;
            public GameObject LandmarkPrefab => landmarkPrefab;
            public bool UseRuntimeFallback => useRuntimeFallback;
            public Vector2 LocalOffset => localOffset;
            public float RotationZ => rotationZ;
            public Vector3 Scale => scale == Vector3.zero ? Vector3.one : scale;
        }
    }
}
