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
        [Header("Visuals")]
        [Tooltip("Inventory and HUD icon. Keep this small and readable for UI.")]
        [SerializeField] private Sprite icon;
        [Tooltip("Optional world pickup sprite. Falls back to Icon when empty.")]
        [SerializeField] private Sprite worldDropSprite;
        [Tooltip("Optional shop display sprite. Falls back to World Drop Sprite, then Icon when empty.")]
        [SerializeField] private Sprite shopDisplaySprite;

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
        public Sprite WorldDropSprite => worldDropSprite != null ? worldDropSprite : icon;
        public Sprite ShopDisplaySprite => shopDisplaySprite != null ? shopDisplaySprite : WorldDropSprite;
        public ActiveItemChargeRule ChargeRule => chargeRule;
        public int MaxCharge => maxCharge;
        public int ChargePerRoomClear => chargePerRoomClear;
        public int ChargePerEnemyKill => chargePerEnemyKill;
        public bool StartFullyCharged => startFullyCharged;
        public ActiveItemEffectData Effect => effect;
    }
}
