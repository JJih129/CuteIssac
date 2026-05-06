using CuteIssac.Data.Item;
using CuteIssac.Data.Dungeon;
using CuteIssac.Player;
using UnityEngine;

namespace CuteIssac.Item
{
    /// <summary>
    /// Grants a passive item through PlayerItemManager so stat recomputation stays centralized.
    /// </summary>
    public sealed class ItemPickupLogic : BasePickupLogic
    {
        [Header("Item Reward")]
        [SerializeField] private ItemData itemData;
        [SerializeField] private RoomRewardPickupTracker roomRewardPickupTracker;
        [SerializeField] [Min(1f)] private float weaponPickupScaleMultiplier = 3f;

        [Header("Pickup Range")]
        [SerializeField] private bool showAutoPickupRange = true;
        [SerializeField] private Color weaponRangeColor = new(0.42f, 0.86f, 1f, 0.72f);
        [SerializeField] private Color artifactRangeColor = new(1f, 0.82f, 0.28f, 0.72f);

        private bool _hasCapturedBaseScale;
        private Vector3 _baseLocalScale = Vector3.one;

        public ItemData ItemData => itemData;

        protected override void Awake()
        {
            base.Awake();
            ResolveReferences();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            ApplyPickupVisual();
        }

        /// <summary>
        /// Runtime content spawners can override the authored item asset before the pickup is collected.
        /// </summary>
        public void ConfigureItem(ItemData configuredItemData)
        {
            itemData = configuredItemData;
            ApplyPickupVisual();
            RefreshPickupRangeIndicator();
        }

        protected override bool TryCollect(PlayerInventory inventory, PlayerHealth health, PlayerItemManager itemManager)
        {
            return itemManager != null && itemManager.AcquirePassiveItem(itemData);
        }

        protected override string BuildPickupFeedbackLabel()
        {
            if (itemData == null)
            {
                return base.BuildPickupFeedbackLabel();
            }

            if (TryBuildCurseRewardFeedbackLabel(out string feedbackLabel))
            {
                return feedbackLabel;
            }

            return itemData.DisplayName.ToUpperInvariant();
        }

        protected override Color ResolvePickupFeedbackColor()
        {
            if (itemData != null && IsCurseRewardPickup())
            {
                return itemData.Rarity switch
                {
                    ItemRarity.Rare => new Color(0.56f, 0.76f, 1f, 1f),
                    ItemRarity.Legendary => new Color(1f, 0.66f, 0.22f, 1f),
                    ItemRarity.Relic => new Color(1f, 0.88f, 0.44f, 1f),
                    ItemRarity.Boss => new Color(1f, 0.42f, 0.36f, 1f),
                    _ => base.ResolvePickupFeedbackColor()
                };
            }

            return base.ResolvePickupFeedbackColor();
        }

        protected override bool ShouldShowPickupRangeIndicator()
        {
            return showAutoPickupRange;
        }

        protected override Color ResolvePickupRangeIndicatorColor()
        {
            return itemData != null && itemData.IsWeaponRelic
                ? weaponRangeColor
                : artifactRangeColor;
        }

        private bool TryBuildCurseRewardFeedbackLabel(out string feedbackLabel)
        {
            feedbackLabel = string.Empty;

            if (!IsCurseRewardPickup())
            {
                return false;
            }

            feedbackLabel = itemData.Rarity switch
            {
                ItemRarity.Rare => $"진귀한 대가 · {itemData.DisplayName.ToUpperInvariant()}",
                ItemRarity.Legendary => $"금단의 대가 · {itemData.DisplayName.ToUpperInvariant()}",
                ItemRarity.Relic => $"유물의 대가 · {itemData.DisplayName.ToUpperInvariant()}",
                ItemRarity.Boss => $"왕관의 대가 · {itemData.DisplayName.ToUpperInvariant()}",
                _ => string.Empty
            };

            return !string.IsNullOrWhiteSpace(feedbackLabel);
        }

        private bool IsCurseRewardPickup()
        {
            ResolveReferences();
            return roomRewardPickupTracker != null
                && roomRewardPickupTracker.TracksRoomReward
                && roomRewardPickupTracker.SourceRoomType == RoomType.Curse;
        }

        private void ResolveReferences()
        {
            if (roomRewardPickupTracker == null)
            {
                TryGetComponent(out roomRewardPickupTracker);
            }
        }

        private void ApplyPickupVisual()
        {
            CacheBaseScale();
            transform.localScale = _baseLocalScale * (itemData != null && itemData.IsWeaponRelic
                ? Mathf.Max(1f, weaponPickupScaleMultiplier)
                : 1f);
            PickupPlaceholderVisualResolver.Apply(PickupVisual, itemData);
        }

        private void CacheBaseScale()
        {
            if (_hasCapturedBaseScale)
            {
                return;
            }

            _baseLocalScale = transform.localScale;
            _hasCapturedBaseScale = true;
        }

        protected override void Reset()
        {
            ResolveReferences();
            base.Reset();
        }

        protected override void OnValidate()
        {
            ResolveReferences();
            base.OnValidate();
        }
    }
}
