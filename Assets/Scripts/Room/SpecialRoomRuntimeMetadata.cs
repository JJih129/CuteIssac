using CuteIssac.Data.Dungeon;
using UnityEngine;

namespace CuteIssac.Room
{
    [DisallowMultipleComponent]
    public sealed class SpecialRoomRuntimeMetadata : MonoBehaviour
    {
        public string RuleId { get; private set; }
        public SpecialRoomDealType DealType { get; private set; }
        public string DisplayName { get; private set; }
        public string EntryCostLabel { get; private set; }
        public bool SpawnsTradeOffers { get; private set; }
        public Color AccentColor { get; private set; } = Color.white;
        public bool HasDealPresentation => DealType != SpecialRoomDealType.None && !string.IsNullOrWhiteSpace(DisplayName);
        public bool HasEntryCostPresentation => !string.IsNullOrWhiteSpace(EntryCostLabel);

        public void Configure(SpecialRoomRuleData specialRoomRule)
        {
            if (specialRoomRule == null)
            {
                Clear();
                return;
            }

            RuleId = specialRoomRule.RuleId;
            DealType = specialRoomRule.DealType;
            DisplayName = specialRoomRule.DealDisplayName;
            EntryCostLabel = specialRoomRule.BuildEntryRequirementLabel();
            SpawnsTradeOffers = specialRoomRule.SpawnTradeOffers;
            AccentColor = specialRoomRule.DealAccentColor;
        }

        public void Clear()
        {
            RuleId = string.Empty;
            DealType = SpecialRoomDealType.None;
            DisplayName = string.Empty;
            EntryCostLabel = string.Empty;
            SpawnsTradeOffers = false;
            AccentColor = Color.white;
        }
    }
}
