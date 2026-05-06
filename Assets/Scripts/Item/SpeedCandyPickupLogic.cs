using CuteIssac.Player;
using UnityEngine;

namespace CuteIssac.Item
{
    /// <summary>
    /// Applies the non-stacking speed candy buff through PlayerSpeedBuffState.
    /// </summary>
    public sealed class SpeedCandyPickupLogic : BasePickupLogic
    {
        [Header("Speed Candy")]
        [SerializeField] [Min(0.05f)] private float duration = 10f;
        [SerializeField] [Min(0.05f)] private float moveSpeedMultiplier = 1.2f;
        [SerializeField] private PlayerSpeedBuffState.DuplicateBuffPolicy duplicatePolicy = PlayerSpeedBuffState.DuplicateBuffPolicy.RefreshDuration;
        [SerializeField] private bool addMissingStateComponent = true;
        [SerializeField] private bool logDebugMessages;

        public float Duration => duration;
        public float MoveSpeedMultiplier => moveSpeedMultiplier;
        public PlayerSpeedBuffState.DuplicateBuffPolicy DuplicatePolicy => duplicatePolicy;

        protected override bool TryCollect(PlayerInventory inventory, PlayerHealth health, PlayerItemManager itemManager)
        {
            if (health == null || !TryResolveSpeedBuffState(health, out PlayerSpeedBuffState speedBuffState))
            {
                return false;
            }

            bool applied = speedBuffState.TryApplyBuff(duration, moveSpeedMultiplier, duplicatePolicy);

            if (!applied && logDebugMessages)
            {
                Debug.Log("Speed candy had no effect because a speed buff is already active and duplicate policy is Ignore.", this);
            }

            return applied;
        }

        protected override string BuildPickupFeedbackLabel()
        {
            return "SPEED UP";
        }

        protected override Color ResolvePickupFeedbackColor()
        {
            return new Color(0.42f, 0.82f, 1f, 1f);
        }

        private bool TryResolveSpeedBuffState(PlayerHealth health, out PlayerSpeedBuffState speedBuffState)
        {
            speedBuffState = health.GetComponent<PlayerSpeedBuffState>();

            if (speedBuffState != null)
            {
                return true;
            }

            if (!addMissingStateComponent)
            {
                return false;
            }

            speedBuffState = health.gameObject.AddComponent<PlayerSpeedBuffState>();
            return speedBuffState != null;
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            duration = Mathf.Max(0.05f, duration);
            moveSpeedMultiplier = Mathf.Max(0.05f, moveSpeedMultiplier);
        }
    }
}
