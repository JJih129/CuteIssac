using System;
using UnityEngine;

namespace CuteIssac.Data.Item
{
    [Serializable]
    public struct ItemPoolRarityWeight
    {
        [SerializeField] private ItemRarity rarity;
        [SerializeField] [Min(0f)] private float weightMultiplier;

        public ItemRarity Rarity => rarity;
        public float WeightMultiplier => Mathf.Max(0f, weightMultiplier);
    }
}
