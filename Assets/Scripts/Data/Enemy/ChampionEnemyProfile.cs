using System;
using CuteIssac.Data.Dungeon;
using UnityEngine;

namespace CuteIssac.Data.Enemy
{
    [CreateAssetMenu(fileName = "ChampionEnemyProfile", menuName = "CuteIssac/Data/Enemy/Champion Enemy Profile")]
    public sealed class ChampionEnemyProfile : ScriptableObject
    {
        [Serializable]
        public sealed class VariantSettings
        {
            [SerializeField] private string variantId = "fast";
            [SerializeField] private string displayName = "Swift";
            [SerializeField] [Min(0f)] private float selectionWeight = 1f;
            [SerializeField] [Min(0.1f)] private float moveSpeedMultiplier = 1.2f;
            [SerializeField] [Min(0.1f)] private float maxHealthMultiplier = 1.2f;
            [SerializeField] [Min(0f)] private float contactDamageMultiplier = 1.1f;
            [SerializeField] [Min(0.5f)] private float visualScaleMultiplier = 1.08f;
            [SerializeField] [Range(0f, 1f)] private float colorBlend = 0.42f;
            [SerializeField] private Color accentColor = Color.white;
            [SerializeField] [Min(0.1f)] private float feedbackDuration = 1.4f;
            [SerializeField] [Min(0f)] private float normalRoomWeightBonus;
            [SerializeField] [Min(0f)] private float challengeRoomWeightBonus;
            [SerializeField] [Min(0f)] private float miniBossRoomWeightBonus;
            [SerializeField] [Min(0f)] private float bossRoomWeightBonus;
            [SerializeField] [Min(0f)] private float challengeFollowupWeightBonus;
            [SerializeField] [Min(0f)] private float challengeFollowupWeightBonusPerWave;
            [SerializeField] [Min(0f)] private float maxChallengeFollowupWeightBonus = 0.6f;
            [SerializeField] [Min(1)] private int floorWeightBonusStart = 3;
            [SerializeField] [Min(0f)] private float floorWeightBonusPerFloor;
            [SerializeField] [Min(0f)] private float maxFloorWeightBonus = 1.2f;

            public VariantSettings(
                string variantId,
                string displayName,
                float selectionWeight,
                float moveSpeedMultiplier,
                float maxHealthMultiplier,
                float contactDamageMultiplier,
                float visualScaleMultiplier,
                float colorBlend,
                Color accentColor,
                float feedbackDuration,
                float normalRoomWeightBonus = 0f,
                float challengeRoomWeightBonus = 0f,
                float miniBossRoomWeightBonus = 0f,
                float bossRoomWeightBonus = 0f,
                float challengeFollowupWeightBonus = 0f,
                float challengeFollowupWeightBonusPerWave = 0f,
                float maxChallengeFollowupWeightBonus = 0.6f,
                int floorWeightBonusStart = 3,
                float floorWeightBonusPerFloor = 0f,
                float maxFloorWeightBonus = 1.2f)
            {
                this.variantId = variantId;
                this.displayName = displayName;
                this.selectionWeight = selectionWeight;
                this.moveSpeedMultiplier = moveSpeedMultiplier;
                this.maxHealthMultiplier = maxHealthMultiplier;
                this.contactDamageMultiplier = contactDamageMultiplier;
                this.visualScaleMultiplier = visualScaleMultiplier;
                this.colorBlend = colorBlend;
                this.accentColor = accentColor;
                this.feedbackDuration = feedbackDuration;
                this.normalRoomWeightBonus = normalRoomWeightBonus;
                this.challengeRoomWeightBonus = challengeRoomWeightBonus;
                this.miniBossRoomWeightBonus = miniBossRoomWeightBonus;
                this.bossRoomWeightBonus = bossRoomWeightBonus;
                this.challengeFollowupWeightBonus = challengeFollowupWeightBonus;
                this.challengeFollowupWeightBonusPerWave = challengeFollowupWeightBonusPerWave;
                this.maxChallengeFollowupWeightBonus = maxChallengeFollowupWeightBonus;
                this.floorWeightBonusStart = floorWeightBonusStart;
                this.floorWeightBonusPerFloor = floorWeightBonusPerFloor;
                this.maxFloorWeightBonus = maxFloorWeightBonus;
            }

            public string VariantId => variantId;
            public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? "Champion" : displayName;
            public float SelectionWeight => Mathf.Max(0f, selectionWeight);
            public float MoveSpeedMultiplier => Mathf.Max(0.1f, moveSpeedMultiplier);
            public float MaxHealthMultiplier => Mathf.Max(0.1f, maxHealthMultiplier);
            public float ContactDamageMultiplier => Mathf.Max(0f, contactDamageMultiplier);
            public float VisualScaleMultiplier => Mathf.Max(0.5f, visualScaleMultiplier);
            public float ColorBlend => Mathf.Clamp01(colorBlend);
            public Color AccentColor => accentColor;
            public float FeedbackDuration => Mathf.Max(0.1f, feedbackDuration);

            public float EvaluateSelectionWeight(int floorIndex, RoomType roomType)
            {
                return EvaluateSelectionWeight(floorIndex, roomType, 0);
            }

            public float EvaluateSelectionWeight(int floorIndex, RoomType roomType, int challengeWaveIndex)
            {
                float contextualWeight = SelectionWeight;

                contextualWeight += roomType switch
                {
                    RoomType.Challenge => challengeRoomWeightBonus,
                    RoomType.MiniBoss => miniBossRoomWeightBonus,
                    RoomType.Boss => bossRoomWeightBonus,
                    _ => normalRoomWeightBonus
                };

                if (roomType == RoomType.Challenge && challengeWaveIndex > 0)
                {
                    float followupBonus = Mathf.Max(0f, challengeFollowupWeightBonus);
                    followupBonus += Mathf.Max(0, challengeWaveIndex - 1) * Mathf.Max(0f, challengeFollowupWeightBonusPerWave);
                    contextualWeight += Mathf.Min(Mathf.Max(0f, maxChallengeFollowupWeightBonus), followupBonus);
                }

                int bonusFloorCount = Mathf.Max(0, floorIndex - Mathf.Max(1, floorWeightBonusStart));
                contextualWeight += Mathf.Min(maxFloorWeightBonus, bonusFloorCount * Mathf.Max(0f, floorWeightBonusPerFloor));
                return Mathf.Max(0f, contextualWeight);
            }
        }

        [Header("Promotion Rules")]
        [SerializeField] [Min(1)] private int minFloorIndex = 2;
        [SerializeField] [Range(0f, 1f)] private float basePromotionChance = 0.05f;
        [SerializeField] [Range(0f, 1f)] private float floorPromotionBonus = 0.025f;
        [SerializeField] [Min(1)] private int floorPromotionBonusStart = 2;
        [SerializeField] [Range(0f, 1f)] private float maxFloorPromotionBonus = 0.16f;
        [SerializeField] [Range(0f, 1f)] private float eliteEncounterBonus = 0.08f;
        [SerializeField] [Range(0f, 1f)] private float normalRoomBonus = 0.01f;
        [SerializeField] [Range(0f, 1f)] private float challengeRoomBonus = 0.04f;
        [SerializeField] [Range(0f, 1f)] private float miniBossRoomBonus = 0.06f;
        [SerializeField] [Range(0f, 1f)] private float bossRoomBonus = 0.08f;
        [SerializeField] [Range(0f, 1f)] private float maxPromotionChance = 0.24f;
        [SerializeField] private bool allowBossRoomPromotion;

        [Header("Champion Variants")]
        [SerializeField] private VariantSettings[] variants =
        {
            new VariantSettings("fast", "Swift", 1f, 1.35f, 1.15f, 1.1f, 1.04f, 0.4f, new Color(0.45f, 0.95f, 1f, 1f), 1.35f, 0.08f, 0.28f, 0f, 0f, 0.18f, 0.08f, 0.34f, 2, 0.04f, 0.4f),
            new VariantSettings("tank", "Bulwark", 0.95f, 0.82f, 1.9f, 1.25f, 1.16f, 0.46f, new Color(1f, 0.78f, 0.35f, 1f), 1.5f, 0.04f, 0f, 0.34f, 0.18f, 0.03f, 0.02f, 0.08f, 3, 0.03f, 0.36f),
            new VariantSettings("explosive", "Volatile", 0.8f, 1.08f, 1.35f, 1.35f, 1.1f, 0.48f, new Color(1f, 0.42f, 0.34f, 1f), 1.55f, 0f, 0.18f, 0.12f, 0.08f, 0.14f, 0.11f, 0.42f, 2, 0.08f, 0.72f),
        };

        public bool AllowsPromotion(RoomType roomType)
        {
            return roomType switch
            {
                RoomType.Boss => allowBossRoomPromotion,
                RoomType.Normal => true,
                RoomType.Challenge => true,
                RoomType.MiniBoss => true,
                _ => false
            };
        }

        public float EvaluatePromotionChance(int floorIndex, EnemyEncounterTier encounterTier, RoomType roomType)
        {
            if (!AllowsPromotion(roomType) || floorIndex < minFloorIndex)
            {
                return 0f;
            }

            float chance = basePromotionChance;
            int bonusStartFloor = Mathf.Max(minFloorIndex, floorPromotionBonusStart);
            int bonusFloorCount = Mathf.Max(0, floorIndex - bonusStartFloor);
            chance += Mathf.Min(maxFloorPromotionBonus, bonusFloorCount * floorPromotionBonus);

            if (encounterTier == EnemyEncounterTier.Elite)
            {
                chance += eliteEncounterBonus;
            }

            if (roomType == RoomType.Normal)
            {
                chance += normalRoomBonus;
            }
            else if (roomType == RoomType.Challenge)
            {
                chance += challengeRoomBonus;
            }
            else if (roomType == RoomType.MiniBoss)
            {
                chance += miniBossRoomBonus;
            }
            else if (roomType == RoomType.Boss)
            {
                chance += bossRoomBonus;
            }

            return Mathf.Clamp01(Mathf.Min(maxPromotionChance, chance));
        }

        public VariantSettings SelectVariant(float normalizedRoll)
        {
            return SelectVariant(normalizedRoll, 1, RoomType.Normal, 0);
        }

        public VariantSettings SelectVariant(float normalizedRoll, int floorIndex, RoomType roomType)
        {
            return SelectVariant(normalizedRoll, floorIndex, roomType, 0);
        }

        public VariantSettings SelectVariant(float normalizedRoll, int floorIndex, RoomType roomType, int challengeWaveIndex)
        {
            if (variants == null || variants.Length == 0)
            {
                return null;
            }

            float totalWeight = 0f;

            for (int i = 0; i < variants.Length; i++)
            {
                if (variants[i] != null)
                {
                    totalWeight += variants[i].EvaluateSelectionWeight(floorIndex, roomType, challengeWaveIndex);
                }
            }

            if (totalWeight <= 0f)
            {
                return variants[0];
            }

            float targetWeight = Mathf.Clamp01(normalizedRoll) * totalWeight;
            float cumulativeWeight = 0f;

            for (int i = 0; i < variants.Length; i++)
            {
                VariantSettings variant = variants[i];

                float variantWeight = variant != null
                    ? variant.EvaluateSelectionWeight(floorIndex, roomType, challengeWaveIndex)
                    : 0f;

                if (variant == null || variantWeight <= 0f)
                {
                    continue;
                }

                cumulativeWeight += variantWeight;

                if (targetWeight <= cumulativeWeight)
                {
                    return variant;
                }
            }

            return variants[variants.Length - 1];
        }
    }
}
