using UnityEngine;

namespace CuteIssac.Data.Item
{
    [CreateAssetMenu(menuName = "CuteIssac/Items/Active Item", fileName = "ActiveItem")]
    public sealed class ActiveItemData : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string itemId = "active_item";
        [SerializeField] private string displayName = "Active Item";
        [SerializeField] [TextArea] private string description = "Reusable active item.";
        [SerializeField] private Sprite icon;

        [Header("Charge")]
        [SerializeField] private ActiveItemChargeRule chargeRule = ActiveItemChargeRule.RoomClear;
        [SerializeField] [Min(1)] private int maxCharge = 4;
        [SerializeField] [Min(1)] private int chargePerRoomClear = 1;
        [SerializeField] [Min(1)] private int chargePerEnemyKill = 1;
        [SerializeField] private bool startFullyCharged;

        [Header("Effect")]
        [SerializeField] private ActiveItemEffectData effect;

        public string ItemId => itemId;
        public string DisplayName => displayName;
        public string Description => description;
        public Sprite Icon => icon;
        public ActiveItemChargeRule ChargeRule => chargeRule;
        public int MaxCharge => maxCharge;
        public int ChargePerRoomClear => chargePerRoomClear;
        public int ChargePerEnemyKill => chargePerEnemyKill;
        public bool StartFullyCharged => startFullyCharged;
        public ActiveItemEffectData Effect => effect;
    }
}
