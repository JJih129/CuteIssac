using System;
using System.Collections.Generic;
using CuteIssac.Dungeon;

namespace CuteIssac.Core.Run
{
    /// <summary>
    /// Serializable save payload for one active run snapshot.
    /// The data stays scene-agnostic so future load flow can rebuild runtime state from this object.
    /// </summary>
    [Serializable]
    public sealed class RunSaveData
    {
        public int Version = 4;
        public int DungeonSeed;
        public int CurrentFloorIndex;
        public string CurrentRoomId;
        public float PlayerPositionX;
        public float PlayerPositionY;
        public int ClearedRoomCount;
        public int TotalClearedRoomCount;
        public int ResolvedRoomCount;
        public int TotalResolvedRoomCount;
        public int BossRoomClearCount;
        public RunState RunState;
        public RunEndReason EndReason;
        public RunSavedPlayerStats PlayerStats = new();
        public RunSavedInventory Inventory = new();
        public List<RoomExplorationSaveRecord> VisitedRooms = new();
    }

    [Serializable]
    public sealed class RunSavedPlayerStats
    {
        public float CurrentHealth;
        public float MoveSpeed;
        public float Damage;
        public float FireInterval;
        public float ProjectileSpeed;
        public float ProjectileLifetime;
        public float ProjectileScale;
    }

    [Serializable]
    public sealed class RunSavedInventory
    {
        public int Coins;
        public int Keys;
        public int Bombs;
        public string EquippedActiveItemId;
        public int ActiveItemCurrentCharge;
        public string ActiveTimedEffectSourceItemId;
        public float ActiveTimedEffectRemainingSeconds;
        public string HeldConsumableItemId;
        public string EquippedTrinketItemId;
        public string ConsumableTimedEffectSourceItemId;
        public float ConsumableTimedEffectRemainingSeconds;
        public List<string> PassiveItemIds = new();
    }
}
