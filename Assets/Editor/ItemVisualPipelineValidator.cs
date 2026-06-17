using System.Text;
using CuteIssac.Data.Item;
using UnityEditor;
using UnityEngine;

namespace CuteIssac.Editor.Validation
{
    /// <summary>
    /// Inspector-driven item art workflow guard.
    /// Reports missing UI, world-drop, and shop-display sprites without modifying assets.
    /// </summary>
    public static class ItemVisualPipelineValidator
    {
        private const string MenuPath = "CuteIssac/Validation/Run Item Visual Pipeline Validation";

        [MenuItem(MenuPath)]
        public static void RunFromMenu()
        {
            ValidationSummary summary = RunValidation();

            if (summary.WarningCount > 0)
            {
                Debug.LogWarning(summary.BuildMessage());
                return;
            }

            Debug.Log(summary.BuildMessage());
        }

        public static ValidationSummary RunValidation()
        {
            ValidationSummary summary = new();

            ValidateItemDataAssets(summary);
            ValidateActiveItemDataAssets(summary);
            ValidateConsumableItemDataAssets(summary);
            ValidateShopItemDataAssets(summary);

            return summary;
        }

        private static void ValidateItemDataAssets(ValidationSummary summary)
        {
            string[] guids = AssetDatabase.FindAssets("t:ItemData");
            summary.PassiveItemCount = guids.Length;

            for (int index = 0; index < guids.Length; index++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[index]);
                ItemData itemData = AssetDatabase.LoadAssetAtPath<ItemData>(path);
                if (itemData == null)
                {
                    continue;
                }

                if (itemData.Icon == null)
                {
                    summary.AddWarning(path, itemData.DisplayName, "UI Icon is missing.");
                }

                if (itemData.WorldDropSprite == null)
                {
                    summary.AddWarning(path, itemData.DisplayName, "World Drop Sprite cannot resolve.");
                }

                if (itemData.ShopDisplaySprite == null)
                {
                    summary.AddWarning(path, itemData.DisplayName, "Shop Display Sprite cannot resolve.");
                }
            }
        }

        private static void ValidateActiveItemDataAssets(ValidationSummary summary)
        {
            string[] guids = AssetDatabase.FindAssets("t:ActiveItemData");
            summary.ActiveItemCount = guids.Length;

            for (int index = 0; index < guids.Length; index++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[index]);
                ActiveItemData itemData = AssetDatabase.LoadAssetAtPath<ActiveItemData>(path);
                if (itemData == null)
                {
                    continue;
                }

                if (itemData.Icon == null)
                {
                    summary.AddWarning(path, itemData.DisplayName, "UI Icon is missing.");
                }

                if (itemData.WorldDropSprite == null)
                {
                    summary.AddWarning(path, itemData.DisplayName, "World Drop Sprite cannot resolve.");
                }

                if (itemData.ShopDisplaySprite == null)
                {
                    summary.AddWarning(path, itemData.DisplayName, "Shop Display Sprite cannot resolve.");
                }
            }
        }

        private static void ValidateConsumableItemDataAssets(ValidationSummary summary)
        {
            string[] guids = AssetDatabase.FindAssets("t:ConsumableItemData");
            summary.ConsumableItemCount = guids.Length;

            for (int index = 0; index < guids.Length; index++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[index]);
                ConsumableItemData itemData = AssetDatabase.LoadAssetAtPath<ConsumableItemData>(path);
                if (itemData == null)
                {
                    continue;
                }

                if (itemData.Icon == null)
                {
                    summary.AddWarning(path, itemData.DisplayName, "UI Icon is missing.");
                }

                if (itemData.WorldDropSprite == null)
                {
                    summary.AddWarning(path, itemData.DisplayName, "World Drop Sprite cannot resolve.");
                }

                if (itemData.ShopDisplaySprite == null)
                {
                    summary.AddWarning(path, itemData.DisplayName, "Shop Display Sprite cannot resolve.");
                }
            }
        }

        private static void ValidateShopItemDataAssets(ValidationSummary summary)
        {
            string[] guids = AssetDatabase.FindAssets("t:ShopItemData");
            summary.ShopOfferCount = guids.Length;

            for (int index = 0; index < guids.Length; index++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[index]);
                ShopItemData itemData = AssetDatabase.LoadAssetAtPath<ShopItemData>(path);
                if (itemData == null)
                {
                    continue;
                }

                if (itemData.Icon == null)
                {
                    summary.AddWarning(path, itemData.DisplayName, "Fallback shop UI Icon is missing.");
                }

                if (itemData.ShopDisplaySprite == null)
                {
                    summary.AddWarning(path, itemData.DisplayName, "Shop Display Sprite cannot resolve.");
                }
            }
        }

        public sealed class ValidationSummary
        {
            private const int MaxDisplayedWarnings = 80;
            private readonly StringBuilder _details = new();
            private int _displayedWarningCount;

            public int PassiveItemCount { get; set; }
            public int ActiveItemCount { get; set; }
            public int ConsumableItemCount { get; set; }
            public int ShopOfferCount { get; set; }
            public int WarningCount { get; private set; }

            public void AddWarning(string assetPath, string displayName, string message)
            {
                WarningCount++;
                if (_displayedWarningCount >= MaxDisplayedWarnings)
                {
                    return;
                }

                _displayedWarningCount++;
                _details
                    .Append("- ")
                    .Append(string.IsNullOrWhiteSpace(displayName) ? "(unnamed)" : displayName)
                    .Append(" | ")
                    .Append(message)
                    .Append(" | ")
                    .Append(assetPath)
                    .AppendLine();
            }

            public string BuildMessage()
            {
                StringBuilder builder = new();
                builder
                    .Append("Item Visual Pipeline Validation")
                    .AppendLine()
                    .Append("- Passive Items: ").Append(PassiveItemCount).AppendLine()
                    .Append("- Active Items: ").Append(ActiveItemCount).AppendLine()
                    .Append("- Consumables: ").Append(ConsumableItemCount).AppendLine()
                    .Append("- Shop Offers: ").Append(ShopOfferCount).AppendLine()
                    .Append("- Warnings: ").Append(WarningCount);

                if (WarningCount > 0)
                {
                    builder.AppendLine().Append(_details);
                    int hiddenWarningCount = WarningCount - _displayedWarningCount;
                    if (hiddenWarningCount > 0)
                    {
                        builder
                            .Append("- ... ")
                            .Append(hiddenWarningCount)
                            .AppendLine(" more warnings hidden. Run the validator after narrowing the target art batch.");
                    }
                }

                return builder.ToString();
            }
        }
    }
}
