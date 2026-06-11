using CuteIssac.Data.Item;
using CuteIssac.Data.Room;
using CuteIssac.Item;
using UnityEngine;

namespace CuteIssac.Data.Dungeon
{
    [CreateAssetMenu(fileName = "SpecialRoomRuleData", menuName = "CuteIssac/Data/Dungeon/Special Room Rule")]
    public sealed class SpecialRoomRuleData : ScriptableObject
    {
        [SerializeField] private string ruleId = "special-room";
        [SerializeField] private RoomType roomType = RoomType.Secret;
        [SerializeField] [Min(1)] private int minFloor = 1;
        [SerializeField] [Min(1)] private int maxFloor = 99;
        [SerializeField] [Range(0f, 1f)] private float baseChance = 1f;
        [SerializeField] [Min(0)] private int guaranteedCount;
        [SerializeField] private bool requiresBossCleared;
        [SerializeField] private SpecialRoomCostType entryCostType;
        [SerializeField] [Min(0)] private int entryCostAmount;
        [SerializeField] [Range(0f, 1f)] private float minimumHealthRatioToEnter;
        [SerializeField] private RoomRewardTable rewardTable;
        [SerializeField] private ItemPoolData itemPool;
        [SerializeField] [Min(0f)] private float rewardMultiplier = 1f;
        [SerializeField] private SpecialRoomDealType dealType = SpecialRoomDealType.None;
        [SerializeField] private string dealDisplayName;
        [SerializeField] private Color dealAccentColor = Color.white;
        [SerializeField] private bool spawnTradeOffers;
        [SerializeField] private ShopCurrencyType tradeCurrencyType = ShopCurrencyType.Health;
        [SerializeField] [Min(1)] private int tradeOfferCount = 2;
        [SerializeField] [Min(1)] private int tradePriceMin = 1;
        [SerializeField] [Min(1)] private int tradePriceMax = 2;
        [SerializeField] private bool hiddenDoor;
        [SerializeField] [Range(0f, 1f)] private float postBossDealChance;
        [SerializeField] [Range(0f, 1f)] private float angelChanceBonusWithoutDevilDealAccepted = 0.25f;
        [SerializeField] [Range(0f, 1f)] private float angelChanceBonusPerDevilDealDeclined = 0.15f;
        [SerializeField] [Range(0f, 1f)] private float angelChanceMultiplierAfterDevilDealAccepted = 0.25f;

        public string RuleId => ruleId;
        public RoomType RoomType => roomType;
        public int MinFloor => Mathf.Max(1, minFloor);
        public int MaxFloor => Mathf.Max(MinFloor, maxFloor);
        public float BaseChance => Mathf.Clamp01(baseChance);
        public int GuaranteedCount => Mathf.Max(0, guaranteedCount);
        public bool RequiresBossCleared => requiresBossCleared;
        public SpecialRoomCostType EntryCostType => entryCostType;
        public int EntryCostAmount => Mathf.Max(0, entryCostAmount);
        public float MinimumHealthRatioToEnter => Mathf.Clamp01(minimumHealthRatioToEnter);
        public RoomRewardTable RewardTable => rewardTable;
        public ItemPoolData ItemPool => itemPool;
        public float RewardMultiplier => Mathf.Max(0f, rewardMultiplier);
        public SpecialRoomDealType DealType => dealType;
        public string DealDisplayName => !string.IsNullOrWhiteSpace(dealDisplayName)
            ? dealDisplayName
            : ResolveDefaultDealDisplayName(dealType);
        public Color DealAccentColor => dealAccentColor == default ? ResolveDefaultDealAccentColor(dealType) : dealAccentColor;
        public bool SpawnTradeOffers => spawnTradeOffers;
        public ShopCurrencyType TradeCurrencyType => tradeCurrencyType;
        public int TradeOfferCount => Mathf.Max(1, tradeOfferCount);
        public int TradePriceMin => Mathf.Max(1, tradePriceMin);
        public int TradePriceMax => Mathf.Max(TradePriceMin, tradePriceMax);
        public bool HiddenDoor => hiddenDoor;
        public float PostBossDealChance => Mathf.Clamp01(postBossDealChance);
        public float AngelChanceBonusWithoutDevilDealAccepted => Mathf.Clamp01(angelChanceBonusWithoutDevilDealAccepted);
        public float AngelChanceBonusPerDevilDealDeclined => Mathf.Clamp01(angelChanceBonusPerDevilDealDeclined);
        public float AngelChanceMultiplierAfterDevilDealAccepted => Mathf.Clamp01(angelChanceMultiplierAfterDevilDealAccepted);
        public bool HasEntryCost => EntryCostAmount > 0 && entryCostType is SpecialRoomCostType.Key or SpecialRoomCostType.Coin or SpecialRoomCostType.Bomb or SpecialRoomCostType.Health;
        public bool HasMinimumHealthGate => MinimumHealthRatioToEnter > 0.001f;
        public bool HasDeal => dealType != SpecialRoomDealType.None && !string.IsNullOrWhiteSpace(DealDisplayName);

        public bool IsAvailableForFloor(int floorIndex)
        {
            return floorIndex >= MinFloor
                && floorIndex <= MaxFloor
                && (BaseChance > 0f || GuaranteedCount > 0 || PostBossDealChance > 0f);
        }

        public string BuildEntryCostLabel()
        {
            if (!HasEntryCost)
            {
                return string.Empty;
            }

            string costLabel = entryCostType switch
            {
                SpecialRoomCostType.Key => "KEY",
                SpecialRoomCostType.Coin => "COIN",
                SpecialRoomCostType.Bomb => "BOMB",
                SpecialRoomCostType.Health => "HP",
                _ => "COST"
            };

            return entryCostType == SpecialRoomCostType.Health
                ? $"{costLabel} -{EntryCostAmount}"
                : $"{costLabel} x{EntryCostAmount}";
        }

        public string BuildEntryRequirementLabel()
        {
            string costLabel = BuildEntryCostLabel();
            if (!HasMinimumHealthGate)
            {
                return costLabel;
            }

            string healthGateLabel = $"HP {Mathf.CeilToInt(MinimumHealthRatioToEnter * 100f)}%+";
            return string.IsNullOrWhiteSpace(costLabel)
                ? healthGateLabel
                : $"{costLabel} / {healthGateLabel}";
        }

        private static string ResolveDefaultDealDisplayName(SpecialRoomDealType type)
        {
            return type switch
            {
                SpecialRoomDealType.Devil => "Devil Deal",
                SpecialRoomDealType.Angel => "Angel Deal",
                SpecialRoomDealType.BlackMarket => "Black Market",
                _ => string.Empty
            };
        }

        private static Color ResolveDefaultDealAccentColor(SpecialRoomDealType type)
        {
            return type switch
            {
                SpecialRoomDealType.Devil => new Color(0.94f, 0.18f, 0.28f, 1f),
                SpecialRoomDealType.Angel => new Color(0.74f, 0.88f, 1f, 1f),
                SpecialRoomDealType.BlackMarket => new Color(0.9f, 0.72f, 0.34f, 1f),
                _ => Color.white
            };
        }
    }
}
