using System;
using CuteIssac.Room;
using UnityEngine;

namespace CuteIssac.Data.Room
{
    [CreateAssetMenu(fileName = "RoomObstacleArtSet", menuName = "CuteIssac/Data/Room/Room Obstacle Art Set")]
    public sealed class RoomObstacleArtSet : ScriptableObject
    {
        [Serializable]
        public sealed class Entry
        {
            [SerializeField] private RoomObstacleType obstacleType = RoomObstacleType.Rock;
            [SerializeField] private Sprite bodySprite;
            [SerializeField] private Color bodyColor = Color.white;
            [SerializeField] private Vector2 bodyLocalOffset;
            [SerializeField] private Vector2 bodyLocalScale = Vector2.one;
            [SerializeField] private float bodyRotationZ;
            [SerializeField] private int bodySortingOffset;
            [SerializeField] private Sprite accentSprite;
            [SerializeField] private Color accentColor = Color.white;
            [SerializeField] private Vector2 accentLocalOffset;
            [SerializeField] private Vector2 accentLocalScale = Vector2.one;
            [SerializeField] private float accentRotationZ;
            [SerializeField] private int accentSortingOffset = 1;

            public RoomObstacleType ObstacleType => obstacleType;
            public Sprite BodySprite => bodySprite;
            public Color BodyColor => bodyColor;
            public Vector2 BodyLocalOffset => bodyLocalOffset;
            public Vector2 BodyLocalScale => bodyLocalScale;
            public float BodyRotationZ => bodyRotationZ;
            public int BodySortingOffset => bodySortingOffset;
            public Sprite AccentSprite => accentSprite;
            public Color AccentColor => accentColor;
            public Vector2 AccentLocalOffset => accentLocalOffset;
            public Vector2 AccentLocalScale => accentLocalScale;
            public float AccentRotationZ => accentRotationZ;
            public int AccentSortingOffset => accentSortingOffset;
            public bool HasBodySprite => bodySprite != null;
            public bool HasAccentSprite => accentSprite != null;
        }

        [SerializeField] private Entry[] entries = Array.Empty<Entry>();

        public bool TryGetEntry(RoomObstacleType obstacleType, out Entry entry)
        {
            for (int i = 0; i < entries.Length; i++)
            {
                Entry candidate = entries[i];

                if (candidate != null && candidate.ObstacleType == obstacleType)
                {
                    entry = candidate;
                    return true;
                }
            }

            entry = null;
            return false;
        }
    }
}
