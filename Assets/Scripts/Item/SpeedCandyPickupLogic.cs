using CuteIssac.Player;
using UnityEngine;

namespace CuteIssac.Item
{
    /// <summary>
    /// Applies the non-stacking speed candy buff through PlayerSpeedBuffState.
    /// </summary>
    public sealed class SpeedCandyPickupLogic : BasePickupLogic
    {
        [Header("Speed Heart")]
        [SerializeField] [Min(1)] private int speedHeartAmount = 1;
        [SerializeField] [Min(0.05f)] private float duration = 10f;
        [SerializeField] [Min(0.05f)] private float moveSpeedMultiplier = 1.2f;
        [SerializeField] private PlayerSpeedBuffState.DuplicateBuffPolicy duplicatePolicy = PlayerSpeedBuffState.DuplicateBuffPolicy.RefreshDuration;
        [SerializeField] private Sprite statusIcon;
        [SerializeField] private string statusDisplayName = "스피드 하트";

        public int SpeedHeartAmount => speedHeartAmount;
        public float Duration => duration;
        public float MoveSpeedMultiplier => moveSpeedMultiplier;
        public PlayerSpeedBuffState.DuplicateBuffPolicy DuplicatePolicy => duplicatePolicy;

        protected override bool TryCollect(PlayerInventory inventory, PlayerHealth health, PlayerItemManager itemManager)
        {
            if (health == null)
            {
                return false;
            }

            return health.TryGrantSpeedHeart(
                speedHeartAmount,
                duration,
                moveSpeedMultiplier,
                duplicatePolicy,
                ResolveStatusIcon(),
                statusDisplayName);
        }

        protected override string BuildPickupFeedbackLabel()
        {
            return "SPEED HEART";
        }

        protected override Color ResolvePickupFeedbackColor()
        {
            return new Color(0.42f, 0.82f, 1f, 1f);
        }

        private Sprite ResolveStatusIcon()
        {
            return statusIcon != null ? statusIcon : RuntimeShopIconFactory.GetSpeedCandySprite();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            PickupVisual?.ApplyRuntimeVisual(
                ResolveStatusIcon(),
                new Color(0.42f, 0.82f, 1f, 1f),
                new Color(0.9f, 1f, 1f, 0.24f));
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            speedHeartAmount = Mathf.Max(1, speedHeartAmount);
            duration = Mathf.Max(0.05f, duration);
            moveSpeedMultiplier = Mathf.Max(0.05f, moveSpeedMultiplier);
        }
    }
}
