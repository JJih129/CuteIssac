using CuteIssac.Core.Run;
using CuteIssac.Data.Dungeon;
using UnityEngine;

namespace CuteIssac.Dungeon
{
    public static class SpecialRoomDealChanceResolver
    {
        public static int ResolveRoomCount(SpecialRoomRuleData rule, RunContext runContext)
        {
            if (rule == null)
            {
                return 0;
            }

            if (rule.GuaranteedCount > 0)
            {
                return rule.GuaranteedCount;
            }

            return Random.value <= ResolveChance(rule, runContext) ? 1 : 0;
        }

        public static float ResolveChance(SpecialRoomRuleData rule, RunContext runContext)
        {
            if (rule == null)
            {
                return 0f;
            }

            float chance = rule.RequiresBossCleared && rule.PostBossDealChance > 0f
                ? rule.PostBossDealChance
                : rule.BaseChance;

            if (rule.DealType != SpecialRoomDealType.Angel || runContext == null)
            {
                return Mathf.Clamp01(chance);
            }

            RunSpecialRoomDealState dealState = runContext.SpecialRoomDeals;
            if (dealState != null && dealState.HasAcceptedDevilDeal)
            {
                return Mathf.Clamp01(chance * rule.AngelChanceMultiplierAfterDevilDealAccepted);
            }

            float declinedBonus = dealState != null
                ? dealState.DevilDealsDeclined * rule.AngelChanceBonusPerDevilDealDeclined
                : 0f;
            return Mathf.Clamp01(chance + rule.AngelChanceBonusWithoutDevilDealAccepted + declinedBonus);
        }
    }
}
