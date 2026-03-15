using CuteIssac.Enemy;
using CuteIssac.Room;
using UnityEngine;

namespace CuteIssac.Core.Gameplay
{
    public readonly struct ChampionEnemyPromotedSignal
    {
        public ChampionEnemyPromotedSignal(RoomController room, EnemyController enemy, string variantLabel, Color accentColor)
        {
            Room = room;
            Enemy = enemy;
            VariantLabel = variantLabel;
            AccentColor = accentColor;
        }

        public RoomController Room { get; }
        public EnemyController Enemy { get; }
        public string VariantLabel { get; }
        public Color AccentColor { get; }
        public bool IsValid => Room != null && Enemy != null && !string.IsNullOrWhiteSpace(VariantLabel);
    }
}
