using System;
using UnityEngine;

namespace CuteIssac.Data.Visual
{
    [CreateAssetMenu(fileName = "SpriteImportValidationProfile", menuName = "CuteIssac/Visual/Sprite Import Validation Profile")]
    public sealed class SpriteImportValidationProfile : ScriptableObject
    {
        [SerializeField] private string artRoot = "Assets/Art";
        [SerializeField] private SpriteCategoryRule fallbackRule = SpriteCategoryRule.CreateFallback();
        [SerializeField] private SpriteCategoryRule[] categoryRules =
        {
            SpriteCategoryRule.CreatePickupRule(),
            SpriteCategoryRule.CreateDoorAndMapRule(),
            SpriteCategoryRule.CreateCharacterRule(),
            SpriteCategoryRule.CreateWeaponRule(),
        };

        public string ArtRoot => string.IsNullOrWhiteSpace(artRoot) ? "Assets/Art" : artRoot;
        public SpriteCategoryRule FallbackRule => fallbackRule;
        public SpriteCategoryRule[] CategoryRules => categoryRules;

        public SpriteCategoryRule ResolveRule(string assetPath)
        {
            if (categoryRules != null)
            {
                for (int i = 0; i < categoryRules.Length; i++)
                {
                    SpriteCategoryRule rule = categoryRules[i];
                    if (rule != null && rule.Matches(assetPath))
                    {
                        return rule;
                    }
                }
            }

            return fallbackRule != null ? fallbackRule : SpriteCategoryRule.CreateFallback();
        }

        [Serializable]
        public sealed class SpriteCategoryRule
        {
            [SerializeField] private string categoryName = "Fallback";
            [SerializeField] private string[] pathContains = Array.Empty<string>();
            [SerializeField] private bool validatePixelsPerUnit = true;
            [SerializeField] [Min(0f)] private float minPixelsPerUnit = 64f;
            [SerializeField] [Min(0f)] private float maxPixelsPerUnit = 256f;
            [SerializeField] private bool validatePivot = true;
            [SerializeField] private Vector2 recommendedPivot = new(0.5f, 0.5f);
            [SerializeField] [Min(0f)] private float pivotTolerance = 0.08f;
            [SerializeField] private bool validateSpriteRect = true;
            [SerializeField] private Vector2 maxSpriteRectSize = new(1024f, 1024f);
            [SerializeField] private bool validateWorldBounds = true;
            [SerializeField] private Vector2 maxWorldSize = new(3f, 3f);

            public string CategoryName => string.IsNullOrWhiteSpace(categoryName) ? "Fallback" : categoryName;
            public bool ValidatePixelsPerUnit => validatePixelsPerUnit;
            public float MinPixelsPerUnit => minPixelsPerUnit;
            public float MaxPixelsPerUnit => maxPixelsPerUnit;
            public bool ValidatePivot => validatePivot;
            public Vector2 RecommendedPivot => recommendedPivot;
            public float PivotTolerance => pivotTolerance;
            public bool ValidateSpriteRect => validateSpriteRect;
            public Vector2 MaxSpriteRectSize => maxSpriteRectSize;
            public bool ValidateWorldBounds => validateWorldBounds;
            public Vector2 MaxWorldSize => maxWorldSize;

            public bool Matches(string assetPath)
            {
                if (string.IsNullOrWhiteSpace(assetPath) || pathContains == null || pathContains.Length == 0)
                {
                    return false;
                }

                for (int i = 0; i < pathContains.Length; i++)
                {
                    string token = pathContains[i];
                    if (!string.IsNullOrWhiteSpace(token)
                        && assetPath.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return true;
                    }
                }

                return false;
            }

            public static SpriteCategoryRule CreatePickupRule()
            {
                return new SpriteCategoryRule
                {
                    categoryName = "Pickup",
                    pathContains = new[] { "/Items/", "coin", "pickup", "bomb", "key" },
                    minPixelsPerUnit = 80f,
                    maxPixelsPerUnit = 256f,
                    maxSpriteRectSize = new Vector2(512f, 512f),
                    maxWorldSize = new Vector2(1.2f, 1.2f),
                };
            }

            public static SpriteCategoryRule CreateDoorAndMapRule()
            {
                return new SpriteCategoryRule
                {
                    categoryName = "DoorAndMap",
                    pathContains = new[] { "/Map/", "door", "RoomVisual", "DoorVisual" },
                    minPixelsPerUnit = 64f,
                    maxPixelsPerUnit = 256f,
                    maxSpriteRectSize = new Vector2(4096f, 4096f),
                    maxWorldSize = new Vector2(24f, 16f),
                };
            }

            public static SpriteCategoryRule CreateCharacterRule()
            {
                return new SpriteCategoryRule
                {
                    categoryName = "Character",
                    pathContains = new[] { "/Player/", "/enemy/", "/boss/", "npc" },
                    minPixelsPerUnit = 80f,
                    maxPixelsPerUnit = 256f,
                    maxSpriteRectSize = new Vector2(1024f, 1024f),
                    maxWorldSize = new Vector2(3f, 3f),
                };
            }

            public static SpriteCategoryRule CreateWeaponRule()
            {
                return new SpriteCategoryRule
                {
                    categoryName = "Weapon",
                    pathContains = new[] { "/gun/", "/Weapons/", "weapon" },
                    minPixelsPerUnit = 80f,
                    maxPixelsPerUnit = 256f,
                    maxSpriteRectSize = new Vector2(1024f, 1024f),
                    maxWorldSize = new Vector2(3.5f, 2f),
                };
            }

            public static SpriteCategoryRule CreateFallback()
            {
                return new SpriteCategoryRule
                {
                    categoryName = "Fallback",
                    pathContains = Array.Empty<string>(),
                    minPixelsPerUnit = 64f,
                    maxPixelsPerUnit = 256f,
                    maxSpriteRectSize = new Vector2(2048f, 2048f),
                    maxWorldSize = new Vector2(8f, 8f),
                };
            }
        }
    }
}
