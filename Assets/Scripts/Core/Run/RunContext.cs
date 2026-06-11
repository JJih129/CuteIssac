using System;
using CuteIssac.Data.Dungeon;
using UnityEngine;

namespace CuteIssac.Core.Run
{
    /// <summary>
    /// Runtime snapshot for the current run. Other systems can read this without depending on scene objects.
    /// </summary>
    [Serializable]
    public sealed class RunContext
    {
        [field: SerializeField] public int Seed { get; private set; }
        [field: SerializeField] public int CurrentFloorIndex { get; private set; }
        [field: SerializeField] public int ClearedRoomCount { get; private set; }
        [field: SerializeField] public int TotalClearedRoomCount { get; private set; }
        [field: SerializeField] public int ResolvedRoomCount { get; private set; }
        [field: SerializeField] public int TotalResolvedRoomCount { get; private set; }
        [field: SerializeField] public int EnemyKillCount { get; private set; }
        [field: SerializeField] public int BossRoomClearCount { get; private set; }
        [field: SerializeField] public bool IsHardMode { get; private set; }
        [field: SerializeField] public bool HasActiveRun { get; private set; }
        [field: SerializeField] public RunEndReason EndReason { get; private set; } = RunEndReason.None;
        [field: SerializeField] public RunState State { get; private set; } = RunState.Idle;
        [field: SerializeField] public RunSpecialRoomDealState SpecialRoomDeals { get; private set; } = new();

        public void Initialize(int seed, int startingFloorIndex, bool hardMode = false)
        {
            Seed = seed;
            CurrentFloorIndex = Mathf.Max(1, startingFloorIndex);
            ClearedRoomCount = 0;
            TotalClearedRoomCount = 0;
            ResolvedRoomCount = 0;
            TotalResolvedRoomCount = 0;
            EnemyKillCount = 0;
            BossRoomClearCount = 0;
            IsHardMode = hardMode;
            SpecialRoomDeals.Reset();
            HasActiveRun = true;
            EndReason = RunEndReason.None;
        }

        public void Restore(
            int seed,
            int currentFloorIndex,
            int clearedRoomCount,
            int totalClearedRoomCount,
            int resolvedRoomCount,
            int totalResolvedRoomCount,
            int enemyKillCount,
            int bossRoomClearCount,
            bool hardMode = false,
            int devilDealsPurchased = 0,
            int angelDealsPurchased = 0,
            int blackMarketDealsPurchased = 0,
            int devilDealsOffered = 0,
            int angelDealsOffered = 0,
            int blackMarketDealsOffered = 0,
            int devilDealsDeclined = 0,
            bool hasPendingDevilDealOffer = false,
            RoomType pendingDevilDealRoomType = RoomType.Curse,
            string pendingDevilDealRuleId = null)
        {
            Seed = seed;
            CurrentFloorIndex = Mathf.Max(1, currentFloorIndex);
            ClearedRoomCount = Mathf.Max(0, clearedRoomCount);
            TotalClearedRoomCount = Mathf.Max(ClearedRoomCount, totalClearedRoomCount);
            ResolvedRoomCount = Mathf.Max(0, resolvedRoomCount);
            TotalResolvedRoomCount = Mathf.Max(ResolvedRoomCount, totalResolvedRoomCount);
            EnemyKillCount = Mathf.Max(0, enemyKillCount);
            BossRoomClearCount = Mathf.Max(0, bossRoomClearCount);
            IsHardMode = hardMode;
            SpecialRoomDeals.Restore(
                devilDealsPurchased,
                angelDealsPurchased,
                blackMarketDealsPurchased,
                devilDealsOffered,
                angelDealsOffered,
                blackMarketDealsOffered,
                devilDealsDeclined,
                hasPendingDevilDealOffer,
                pendingDevilDealRoomType,
                pendingDevilDealRuleId);
            HasActiveRun = true;
            EndReason = RunEndReason.None;
        }

        public void Reset()
        {
            Seed = 0;
            CurrentFloorIndex = 0;
            ClearedRoomCount = 0;
            TotalClearedRoomCount = 0;
            ResolvedRoomCount = 0;
            TotalResolvedRoomCount = 0;
            EnemyKillCount = 0;
            BossRoomClearCount = 0;
            IsHardMode = false;
            SpecialRoomDeals.Reset();
            HasActiveRun = false;
            EndReason = RunEndReason.None;
            State = RunState.Idle;
        }

        public void RegisterRoomClear()
        {
            RegisterRoomResolution(RoomType.Normal, true);
        }

        public void RegisterRoomResolution(RoomType roomType, bool hadCombatEncounter)
        {
            if (!HasActiveRun)
            {
                return;
            }

            ResolvedRoomCount++;
            TotalResolvedRoomCount++;

            if (!hadCombatEncounter)
            {
                return;
            }

            ClearedRoomCount++;
            TotalClearedRoomCount++;

            if (roomType == RoomType.Boss)
            {
                BossRoomClearCount++;
            }
        }

        public void RegisterEnemyKill()
        {
            if (!HasActiveRun)
            {
                return;
            }

            EnemyKillCount++;
        }

        public void AdvanceFloor()
        {
            if (!HasActiveRun)
            {
                return;
            }

            CurrentFloorIndex++;
            ClearedRoomCount = 0;
            ResolvedRoomCount = 0;
        }

        public void RegisterSpecialRoomDealPurchase(SpecialRoomDealType dealType)
        {
            if (!HasActiveRun || dealType == SpecialRoomDealType.None)
            {
                return;
            }

            SpecialRoomDeals.RegisterPurchase(dealType);
        }

        public void RegisterSpecialRoomDealOffer(SpecialRoomDealType dealType, RoomType roomType, string ruleId)
        {
            if (!HasActiveRun || dealType == SpecialRoomDealType.None)
            {
                return;
            }

            SpecialRoomDeals.RegisterOffer(dealType, roomType, ruleId);
        }

        public bool TryConfirmPendingDevilDealDecline(out RoomType roomType, out string ruleId)
        {
            roomType = RoomType.Curse;
            ruleId = string.Empty;

            return HasActiveRun
                && SpecialRoomDeals.TryConfirmPendingDevilDealDecline(out roomType, out ruleId);
        }

        public void SetState(RunState state)
        {
            State = state;

            if (state == RunState.Idle || state == RunState.FrontEnd)
            {
                HasActiveRun = false;
            }
        }

        public void SetEndReason(RunEndReason endReason)
        {
            EndReason = endReason;
        }
    }
}
