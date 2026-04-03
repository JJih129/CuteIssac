using CuteIssac.Room;
using UnityEngine;

namespace CuteIssac.Core.Gameplay
{
    public readonly struct FormationBreakSignal
    {
        public FormationBreakSignal(
            RoomController room,
            string formationId,
            Vector3 worldPosition,
            float breakDuration,
            int affectedEnemyCount,
            Color accentColor)
        {
            Room = room;
            FormationId = formationId ?? string.Empty;
            WorldPosition = worldPosition;
            BreakDuration = Mathf.Max(0.1f, breakDuration);
            AffectedEnemyCount = Mathf.Max(0, affectedEnemyCount);
            AccentColor = accentColor;
        }

        public RoomController Room { get; }
        public string FormationId { get; }
        public Vector3 WorldPosition { get; }
        public float BreakDuration { get; }
        public int AffectedEnemyCount { get; }
        public Color AccentColor { get; }
    }
}
