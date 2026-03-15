using CuteIssac.Player;
using UnityEngine;

namespace CuteIssac.Item
{
    /// <summary>
    /// Restores player health when collected.
    /// If the player is already full health the pickup stays in the world.
    /// </summary>
    public sealed class HeartPickupLogic : BasePickupLogic
    {
        [Header("Healing")]
        [SerializeField] [Min(0.5f)] private float healAmount = 1f;

        protected override bool TryCollect(PlayerInventory inventory, PlayerHealth health, PlayerItemManager itemManager)
        {
            return health != null && health.RestoreHealth(healAmount);
        }

        protected override string BuildPickupFeedbackLabel()
        {
            return $"+{healAmount:0.#} HP";
        }

        protected override Color ResolvePickupFeedbackColor()
        {
            return new Color(1f, 0.48f, 0.58f, 1f);
        }
    }
}
