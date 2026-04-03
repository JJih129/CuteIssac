using CuteIssac.Enemy;
using CuteIssac.Room;
using UnityEngine;

namespace CuteIssac.Core.Gameplay
{
    public readonly struct MomentumExecutionSignal
    {
        public MomentumExecutionSignal(
            RoomController room,
            Vector3 position,
            Color accentColor,
            string formationId,
            EnemyFormationPriorityLevel priorityLevel,
            bool matchesActiveFormation,
            int chainCount,
            float sustainedDuration,
            float restoredHealth)
        {
            Room = room;
            Position = position;
            AccentColor = accentColor;
            FormationId = formationId ?? string.Empty;
            PriorityLevel = priorityLevel;
            MatchesActiveFormation = matchesActiveFormation;
            ChainCount = Mathf.Max(0, chainCount);
            SustainedDuration = Mathf.Max(0f, sustainedDuration);
            RestoredHealth = Mathf.Max(0f, restoredHealth);
        }

        public RoomController Room { get; }
        public Vector3 Position { get; }
        public Color AccentColor { get; }
        public string FormationId { get; }
        public EnemyFormationPriorityLevel PriorityLevel { get; }
        public bool MatchesActiveFormation { get; }
        public int ChainCount { get; }
        public float SustainedDuration { get; }
        public float RestoredHealth { get; }
        public bool IsCriticalExecution => PriorityLevel == EnemyFormationPriorityLevel.Critical;
        public bool IsPriorityExecution => PriorityLevel != EnemyFormationPriorityLevel.None;
        public bool IsValid => SustainedDuration > 0f || RestoredHealth > 0f || IsPriorityExecution || MatchesActiveFormation;
    }
}
