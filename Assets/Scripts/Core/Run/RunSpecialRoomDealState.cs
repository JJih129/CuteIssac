using System;
using CuteIssac.Data.Dungeon;
using UnityEngine;

namespace CuteIssac.Core.Run
{
    [Serializable]
    public sealed class RunSpecialRoomDealState
    {
        [field: SerializeField] public int DevilDealsPurchased { get; private set; }
        [field: SerializeField] public int AngelDealsPurchased { get; private set; }
        [field: SerializeField] public int BlackMarketDealsPurchased { get; private set; }
        [field: SerializeField] public int DevilDealsOffered { get; private set; }
        [field: SerializeField] public int AngelDealsOffered { get; private set; }
        [field: SerializeField] public int BlackMarketDealsOffered { get; private set; }
        [field: SerializeField] public int DevilDealsDeclined { get; private set; }
        [field: SerializeField] public bool HasPendingDevilDealOffer { get; private set; }
        [field: SerializeField] public RoomType PendingDevilDealRoomType { get; private set; } = RoomType.Curse;
        [field: SerializeField] public string PendingDevilDealRuleId { get; private set; } = string.Empty;

        public bool HasAcceptedDevilDeal => DevilDealsPurchased > 0;
        public bool HasDeclinedDevilDeal => DevilDealsDeclined > 0;

        public void Restore(
            int devilDealsPurchased,
            int angelDealsPurchased,
            int blackMarketDealsPurchased,
            int devilDealsOffered,
            int angelDealsOffered,
            int blackMarketDealsOffered,
            int devilDealsDeclined,
            bool hasPendingDevilDealOffer,
            RoomType pendingDevilDealRoomType,
            string pendingDevilDealRuleId)
        {
            DevilDealsPurchased = Mathf.Max(0, devilDealsPurchased);
            AngelDealsPurchased = Mathf.Max(0, angelDealsPurchased);
            BlackMarketDealsPurchased = Mathf.Max(0, blackMarketDealsPurchased);
            DevilDealsOffered = Mathf.Max(0, devilDealsOffered);
            AngelDealsOffered = Mathf.Max(0, angelDealsOffered);
            BlackMarketDealsOffered = Mathf.Max(0, blackMarketDealsOffered);
            DevilDealsDeclined = Mathf.Max(0, devilDealsDeclined);
            HasPendingDevilDealOffer = hasPendingDevilDealOffer;
            PendingDevilDealRoomType = pendingDevilDealRoomType;
            PendingDevilDealRuleId = pendingDevilDealRuleId ?? string.Empty;
        }

        public void Reset()
        {
            DevilDealsPurchased = 0;
            AngelDealsPurchased = 0;
            BlackMarketDealsPurchased = 0;
            DevilDealsOffered = 0;
            AngelDealsOffered = 0;
            BlackMarketDealsOffered = 0;
            DevilDealsDeclined = 0;
            HasPendingDevilDealOffer = false;
            PendingDevilDealRoomType = RoomType.Curse;
            PendingDevilDealRuleId = string.Empty;
        }

        public void RegisterOffer(SpecialRoomDealType dealType, RoomType roomType, string ruleId)
        {
            switch (dealType)
            {
                case SpecialRoomDealType.Devil:
                    DevilDealsOffered++;
                    HasPendingDevilDealOffer = true;
                    PendingDevilDealRoomType = roomType;
                    PendingDevilDealRuleId = ruleId ?? string.Empty;
                    break;
                case SpecialRoomDealType.Angel:
                    AngelDealsOffered++;
                    break;
                case SpecialRoomDealType.BlackMarket:
                    BlackMarketDealsOffered++;
                    break;
            }
        }

        public void RegisterPurchase(SpecialRoomDealType dealType)
        {
            switch (dealType)
            {
                case SpecialRoomDealType.Devil:
                    DevilDealsPurchased++;
                    HasPendingDevilDealOffer = false;
                    PendingDevilDealRuleId = string.Empty;
                    break;
                case SpecialRoomDealType.Angel:
                    AngelDealsPurchased++;
                    break;
                case SpecialRoomDealType.BlackMarket:
                    BlackMarketDealsPurchased++;
                    break;
            }
        }

        public bool TryConfirmPendingDevilDealDecline(out RoomType roomType, out string ruleId)
        {
            roomType = PendingDevilDealRoomType;
            ruleId = PendingDevilDealRuleId;

            if (!HasPendingDevilDealOffer)
            {
                return false;
            }

            DevilDealsDeclined++;
            HasPendingDevilDealOffer = false;
            PendingDevilDealRuleId = string.Empty;
            return true;
        }
    }
}
