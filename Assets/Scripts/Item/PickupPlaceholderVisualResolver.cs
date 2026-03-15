using System.Collections.Generic;
using CuteIssac.Data.Item;
using UnityEngine;

namespace CuteIssac.Item
{
    /// <summary>
    /// Provides visible runtime fallback art for pickups when authored item icons are missing.
    /// The placeholders are simple shapes so designers can swap them out with real art later.
    /// </summary>
    internal static class PickupPlaceholderVisualResolver
    {
        private static readonly Dictionary<string, Sprite> SpriteCache = new();

        public static void Apply(PickupVisual pickupVisual, ItemData itemData)
        {
            if (pickupVisual == null || itemData == null)
            {
                return;
            }

            ApplyResolvedVisual(
                pickupVisual,
                itemData.Icon,
                itemData.ItemType,
                itemData.ItemCategory,
                itemData.Rarity);
        }

        public static void Apply(PickupVisual pickupVisual, ActiveItemData activeItemData)
        {
            if (pickupVisual == null || activeItemData == null)
            {
                return;
            }

            ApplyResolvedVisual(
                pickupVisual,
                activeItemData.Icon,
                ItemType.Active,
                ItemCategory.Utility,
                ItemRarity.Uncommon);
        }

        public static void Apply(PickupVisual pickupVisual, ConsumableItemData consumableItemData)
        {
            if (pickupVisual == null || consumableItemData == null)
            {
                return;
            }

            ApplyResolvedVisual(
                pickupVisual,
                consumableItemData.Icon,
                ItemType.Consumable,
                ItemCategory.Utility,
                ItemRarity.Common);
        }

        private static void ApplyResolvedVisual(
            PickupVisual pickupVisual,
            Sprite authoredIcon,
            ItemType itemType,
            ItemCategory itemCategory,
            ItemRarity itemRarity)
        {
            if (authoredIcon != null)
            {
                pickupVisual.ApplyRuntimeVisual(
                    authoredIcon,
                    Color.white,
                    new Color(1f, 1f, 1f, 0.32f));
                return;
            }

            string shapeKey = ResolveShapeKey(itemType, itemCategory);
            Sprite placeholderSprite = GetOrCreatePlaceholderSprite(shapeKey);
            Color baseColor = ResolveBaseColor(itemType, itemCategory, itemRarity);
            Color collectedColor = new(baseColor.r, baseColor.g, baseColor.b, 0.26f);
            pickupVisual.ApplyRuntimeVisual(placeholderSprite, baseColor, collectedColor);
        }

        private static string ResolveShapeKey(ItemType itemType, ItemCategory itemCategory)
        {
            switch (itemType)
            {
                case ItemType.Active:
                    return "active_hex";
                case ItemType.Trinket:
                    return "trinket_ring";
                case ItemType.Consumable:
                    return "consumable_capsule";
            }

            return itemCategory switch
            {
                ItemCategory.Damage => "damage_blade",
                ItemCategory.FireRate => "firerate_burst",
                ItemCategory.Movement => "movement_boot",
                ItemCategory.Projectile => "projectile_diamond",
                ItemCategory.Defense => "defense_shield",
                ItemCategory.Summon => "summon_star",
                ItemCategory.Orbital => "orbital_ring",
                ItemCategory.Laser => "laser_beam",
                ItemCategory.Bomb => "bomb_round",
                ItemCategory.Luck => "luck_clover",
                ItemCategory.Economy => "economy_coin",
                _ => "utility_gem"
            };
        }

        private static Color ResolveBaseColor(ItemType itemType, ItemCategory itemCategory, ItemRarity rarity)
        {
            Color rarityColor = rarity switch
            {
                ItemRarity.Uncommon => new Color(0.42f, 0.92f, 0.58f, 1f),
                ItemRarity.Rare => new Color(1f, 0.82f, 0.32f, 1f),
                ItemRarity.Legendary => new Color(1f, 0.44f, 0.82f, 1f),
                ItemRarity.Relic => new Color(1f, 0.92f, 0.62f, 1f),
                ItemRarity.Boss => new Color(1f, 0.38f, 0.38f, 1f),
                _ => new Color(0.72f, 0.84f, 1f, 1f)
            };

            Color categoryColor = itemCategory switch
            {
                ItemCategory.Damage => new Color(1f, 0.46f, 0.4f, 1f),
                ItemCategory.FireRate => new Color(1f, 0.74f, 0.42f, 1f),
                ItemCategory.Movement => new Color(0.46f, 1f, 0.68f, 1f),
                ItemCategory.Projectile => new Color(0.52f, 0.88f, 1f, 1f),
                ItemCategory.Defense => new Color(0.64f, 0.82f, 1f, 1f),
                ItemCategory.Utility => new Color(0.8f, 0.86f, 1f, 1f),
                ItemCategory.Summon => new Color(0.9f, 0.72f, 1f, 1f),
                ItemCategory.Orbital => new Color(0.72f, 0.96f, 1f, 1f),
                ItemCategory.Laser => new Color(1f, 0.62f, 0.78f, 1f),
                ItemCategory.Bomb => new Color(1f, 0.64f, 0.34f, 1f),
                ItemCategory.Luck => new Color(0.68f, 1f, 0.54f, 1f),
                ItemCategory.Economy => new Color(1f, 0.88f, 0.4f, 1f),
                _ => new Color(0.9f, 0.96f, 1f, 1f)
            };

            if (itemType == ItemType.Active)
            {
                categoryColor = new Color(0.56f, 0.84f, 1f, 1f);
            }
            else if (itemType == ItemType.Trinket)
            {
                categoryColor = new Color(0.66f, 1f, 0.86f, 1f);
            }
            else if (itemType == ItemType.Consumable)
            {
                categoryColor = new Color(1f, 0.8f, 0.58f, 1f);
            }

            return Color.Lerp(categoryColor, rarityColor, 0.4f);
        }

        private static Sprite GetOrCreatePlaceholderSprite(string key)
        {
            if (SpriteCache.TryGetValue(key, out Sprite sprite) && sprite != null)
            {
                return sprite;
            }

            sprite = CreatePlaceholderSprite(key);
            SpriteCache[key] = sprite;
            return sprite;
        }

        private static Sprite CreatePlaceholderSprite(string key)
        {
            const int size = 16;
            Texture2D texture = new(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };

            Color clear = new(0f, 0f, 0f, 0f);
            string[] mask = GetMask(key);

            for (int y = 0; y < size; y++)
            {
                string row = mask[size - 1 - y];
                for (int x = 0; x < size; x++)
                {
                    texture.SetPixel(x, y, row[x] == '1' ? Color.white : clear);
                }
            }

            texture.Apply();
            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
            sprite.name = $"RuntimePickup_{key}";
            return sprite;
        }

        private static string[] GetMask(string key)
        {
            return key switch
            {
                "active_hex" => new[]
                {
                    "0000000000000000",
                    "0000000110000000",
                    "0000011111100000",
                    "0001111111110000",
                    "0011111111111000",
                    "0011111111111000",
                    "0111111111111100",
                    "0111111111111100",
                    "0111111111111100",
                    "0111111111111100",
                    "0011111111111000",
                    "0011111111111000",
                    "0001111111110000",
                    "0000011111100000",
                    "0000000110000000",
                    "0000000000000000",
                },
                "trinket_ring" => new[]
                {
                    "0000000000000000",
                    "0000011111100000",
                    "0001111111111000",
                    "0011110000111100",
                    "0111100000011110",
                    "0111000000001110",
                    "1111000000001111",
                    "1111000000001111",
                    "1111000000001111",
                    "1111000000001111",
                    "0111000000001110",
                    "0111100000011110",
                    "0011110000111100",
                    "0001111111111000",
                    "0000011111100000",
                    "0000000000000000",
                },
                "consumable_capsule" => new[]
                {
                    "0000000000000000",
                    "0000001111110000",
                    "0000111111111100",
                    "0001111111111110",
                    "0011111111111111",
                    "0011111111111111",
                    "0011111111111111",
                    "0011111111111111",
                    "0011111111111111",
                    "0011111111111111",
                    "0001111111111110",
                    "0000111111111100",
                    "0000001111110000",
                    "0000000000000000",
                    "0000000000000000",
                    "0000000000000000",
                },
                "damage_blade" => new[]
                {
                    "0000000100000000",
                    "0000001110000000",
                    "0000011111000000",
                    "0000111111100000",
                    "0001111111110000",
                    "0011111111111000",
                    "0000011111100000",
                    "0000011111100000",
                    "0000011111100000",
                    "0000011111100000",
                    "0000011111100000",
                    "0000001111000000",
                    "0000001111000000",
                    "0000010000100000",
                    "0000000000000000",
                    "0000000000000000",
                },
                "firerate_burst" => new[]
                {
                    "0000000100000000",
                    "0000001110000000",
                    "0000011111000000",
                    "0000111111100000",
                    "0001111111110000",
                    "0111111111111110",
                    "0000111111100000",
                    "0000011111000000",
                    "0000111111100000",
                    "0111111111111110",
                    "0001111111110000",
                    "0000111111100000",
                    "0000011111000000",
                    "0000001110000000",
                    "0000000100000000",
                    "0000000000000000",
                },
                "movement_boot" => new[]
                {
                    "0000000000000000",
                    "0000001111000000",
                    "0000001111000000",
                    "0000001111000000",
                    "0000001111000000",
                    "0000001111100000",
                    "0000011111111000",
                    "0000111111111100",
                    "0001111111111100",
                    "0011111111111000",
                    "0011111111100000",
                    "0011111111000000",
                    "0001111111000000",
                    "0000111110000000",
                    "0000000000000000",
                    "0000000000000000",
                },
                "projectile_diamond" => new[]
                {
                    "0000000010000000",
                    "0000000111000000",
                    "0000001111100000",
                    "0000011111110000",
                    "0000111111111000",
                    "0001111111111100",
                    "0011111111111110",
                    "0111111111111111",
                    "0011111111111110",
                    "0001111111111100",
                    "0000111111111000",
                    "0000011111110000",
                    "0000001111100000",
                    "0000000111000000",
                    "0000000010000000",
                    "0000000000000000",
                },
                "defense_shield" => new[]
                {
                    "0000011111100000",
                    "0001111111111000",
                    "0011111111111100",
                    "0011111111111100",
                    "0011111111111100",
                    "0011111111111100",
                    "0011111111111100",
                    "0001111111111000",
                    "0001111111111000",
                    "0000111111110000",
                    "0000111111110000",
                    "0000011111100000",
                    "0000001111000000",
                    "0000000110000000",
                    "0000000000000000",
                    "0000000000000000",
                },
                "summon_star" => new[]
                {
                    "0000000100000000",
                    "0000001110000000",
                    "0100001110000010",
                    "0110011111100110",
                    "0011111111111100",
                    "0001111111111000",
                    "0000111111110000",
                    "1111111111111111",
                    "0000111111110000",
                    "0001111111111000",
                    "0011111111111100",
                    "0110011111100110",
                    "0100001110000010",
                    "0000001110000000",
                    "0000000100000000",
                    "0000000000000000",
                },
                "orbital_ring" => new[]
                {
                    "0000011111100000",
                    "0001111111111000",
                    "0011110000111100",
                    "0111100000011110",
                    "0111001111001110",
                    "1110011111100111",
                    "1110011111100111",
                    "1110011111100111",
                    "1110011111100111",
                    "1110011111100111",
                    "0111001111001110",
                    "0111100000011110",
                    "0011110000111100",
                    "0001111111111000",
                    "0000011111100000",
                    "0000000000000000",
                },
                "laser_beam" => new[]
                {
                    "0000000011000000",
                    "0000000011000000",
                    "0000000011000000",
                    "0000000111100000",
                    "0000001111110000",
                    "0000011111111000",
                    "0000111111111100",
                    "0001111111111110",
                    "0001111111111110",
                    "0000111111111100",
                    "0000011111111000",
                    "0000001111110000",
                    "0000000111100000",
                    "0000000011000000",
                    "0000000011000000",
                    "0000000011000000",
                },
                "bomb_round" => new[]
                {
                    "0000000110000000",
                    "0000001111000000",
                    "0000000110000000",
                    "0000011111100000",
                    "0001111111111000",
                    "0011111111111100",
                    "0111111111111110",
                    "0111111111111110",
                    "0111111111111110",
                    "0111111111111110",
                    "0011111111111100",
                    "0011111111111100",
                    "0001111111111000",
                    "0000111111110000",
                    "0000001111000000",
                    "0000000000000000",
                },
                "luck_clover" => new[]
                {
                    "0000011001100000",
                    "0001111111111000",
                    "0011111111111100",
                    "0011111111111100",
                    "0001111111111000",
                    "0000011111100000",
                    "0000111111110000",
                    "0011111111111100",
                    "0011111111111100",
                    "0000111111110000",
                    "0000011111100000",
                    "0000001111000000",
                    "0000001111000000",
                    "0000000110000000",
                    "0000000100000000",
                    "0000000000000000",
                },
                "economy_coin" => new[]
                {
                    "0000011111100000",
                    "0001111111111000",
                    "0011111111111100",
                    "0111111111111110",
                    "0111110001111110",
                    "1111100000111111",
                    "1111100000111111",
                    "1111100000111111",
                    "1111100000111111",
                    "1111100000111111",
                    "0111110001111110",
                    "0111111111111110",
                    "0011111111111100",
                    "0001111111111000",
                    "0000011111100000",
                    "0000000000000000",
                },
                _ => new[]
                {
                    "0000001111000000",
                    "0000011111100000",
                    "0000111111110000",
                    "0001111111111000",
                    "0011111111111100",
                    "0011111111111100",
                    "0111111111111110",
                    "0111111111111110",
                    "0111111111111110",
                    "0111111111111110",
                    "0011111111111100",
                    "0011111111111100",
                    "0001111111111000",
                    "0000111111110000",
                    "0000011111100000",
                    "0000001111000000",
                }
            };
        }
    }
}
