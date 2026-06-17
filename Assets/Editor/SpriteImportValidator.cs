using System.Collections.Generic;
using CuteIssac.Data.Visual;
using UnityEditor;
using UnityEngine;

namespace CuteIssac.EditorTools
{
    public static class SpriteImportValidator
    {
        private const string DefaultProfilePath = "Assets/Data/Visual/SpriteImportValidationProfile.asset";
        private const int MaxLoggedIssues = 80;

        [MenuItem("CuteIssac/Validation/Run Art Sprite Import Validation")]
        public static void ValidateAllArtSpritesFromMenu()
        {
            SpriteImportValidationProfile profile = LoadOrCreateDefaultProfile();
            ValidationReport report = ValidateArtSprites(profile);
            LogReport(report, "Assets/Art sprite import validation");
        }

        [MenuItem("Assets/CuteIssac/Validate Selected Sprites", priority = 2100)]
        public static void ValidateSelectedSpritesFromMenu()
        {
            SpriteImportValidationProfile profile = LoadOrCreateDefaultProfile();
            ValidationReport report = ValidateSelectedSprites(profile);
            LogReport(report, "Selected sprite import validation");
        }

        [MenuItem("Assets/CuteIssac/Validate Selected Sprites", true)]
        public static bool CanValidateSelectedSprites()
        {
            return Selection.assetGUIDs != null && Selection.assetGUIDs.Length > 0;
        }

        public static ValidationReport ValidateArtSprites(SpriteImportValidationProfile profile)
        {
            profile = profile != null ? profile : LoadOrCreateDefaultProfile();
            string artRoot = profile.ArtRoot;
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { artRoot });
            return ValidateTextureGuids(profile, guids);
        }

        public static ValidationReport ValidateSelectedSprites(SpriteImportValidationProfile profile)
        {
            profile = profile != null ? profile : LoadOrCreateDefaultProfile();
            HashSet<string> textureGuids = new();
            string[] selectedGuids = Selection.assetGUIDs;

            for (int i = 0; i < selectedGuids.Length; i++)
            {
                string selectedPath = AssetDatabase.GUIDToAssetPath(selectedGuids[i]);
                if (string.IsNullOrWhiteSpace(selectedPath))
                {
                    continue;
                }

                if (AssetDatabase.IsValidFolder(selectedPath))
                {
                    string[] folderGuids = AssetDatabase.FindAssets("t:Texture2D", new[] { selectedPath });
                    for (int folderIndex = 0; folderIndex < folderGuids.Length; folderIndex++)
                    {
                        textureGuids.Add(folderGuids[folderIndex]);
                    }
                }
                else if (AssetImporter.GetAtPath(selectedPath) is TextureImporter)
                {
                    textureGuids.Add(selectedGuids[i]);
                }
            }

            string[] guids = new string[textureGuids.Count];
            textureGuids.CopyTo(guids);
            return ValidateTextureGuids(profile, guids);
        }

        public static SpriteImportValidationProfile LoadOrCreateDefaultProfile()
        {
            SpriteImportValidationProfile profile = AssetDatabase.LoadAssetAtPath<SpriteImportValidationProfile>(DefaultProfilePath);
            if (profile != null)
            {
                return profile;
            }

            EnsureFolder("Assets", "Data");
            EnsureFolder("Assets/Data", "Visual");

            profile = ScriptableObject.CreateInstance<SpriteImportValidationProfile>();
            AssetDatabase.CreateAsset(profile, DefaultProfilePath);
            AssetDatabase.SaveAssets();
            return profile;
        }

        private static ValidationReport ValidateTextureGuids(SpriteImportValidationProfile profile, string[] guids)
        {
            ValidationReport report = new();

            if (guids == null)
            {
                return report;
            }

            for (int i = 0; i < guids.Length; i++)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
                ValidateTextureAsset(profile, assetPath, report);
            }

            return report;
        }

        private static void ValidateTextureAsset(
            SpriteImportValidationProfile profile,
            string assetPath,
            ValidationReport report)
        {
            if (string.IsNullOrWhiteSpace(assetPath))
            {
                return;
            }

            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                return;
            }

            report.IncrementCheckedAssetCount();
            SpriteImportValidationProfile.SpriteCategoryRule rule = profile.ResolveRule(assetPath);
            if (importer.textureType != TextureImporterType.Sprite)
            {
                report.Add(assetPath, rule.CategoryName, "Texture type is not Sprite.");
                return;
            }

            if (rule.ValidatePixelsPerUnit)
            {
                float ppu = importer.spritePixelsPerUnit;
                if (ppu < rule.MinPixelsPerUnit || ppu > rule.MaxPixelsPerUnit)
                {
                    report.Add(
                        assetPath,
                        rule.CategoryName,
                        $"PPU {ppu:0.##} is outside recommended range {rule.MinPixelsPerUnit:0.##}-{rule.MaxPixelsPerUnit:0.##}.");
                }
            }

            List<Sprite> sprites = LoadSprites(assetPath);
            if (sprites.Count == 0)
            {
                report.Add(assetPath, rule.CategoryName, "No Sprite sub-asset was found after import.");
                return;
            }

            for (int i = 0; i < sprites.Count; i++)
            {
                ValidateSprite(rule, assetPath, sprites[i], report);
            }
        }

        private static List<Sprite> LoadSprites(string assetPath)
        {
            List<Sprite> sprites = new();
            Object mainAsset = AssetDatabase.LoadMainAssetAtPath(assetPath);
            if (mainAsset is Sprite mainSprite)
            {
                sprites.Add(mainSprite);
            }

            Object[] subAssets = AssetDatabase.LoadAllAssetRepresentationsAtPath(assetPath);
            for (int i = 0; i < subAssets.Length; i++)
            {
                if (subAssets[i] is Sprite sprite && !sprites.Contains(sprite))
                {
                    sprites.Add(sprite);
                }
            }

            return sprites;
        }

        private static void ValidateSprite(
            SpriteImportValidationProfile.SpriteCategoryRule rule,
            string assetPath,
            Sprite sprite,
            ValidationReport report)
        {
            if (sprite == null)
            {
                return;
            }

            Rect rect = sprite.rect;
            if (rule.ValidateSpriteRect
                && (rect.width > rule.MaxSpriteRectSize.x || rect.height > rule.MaxSpriteRectSize.y))
            {
                report.Add(
                    assetPath,
                    rule.CategoryName,
                    $"{sprite.name} rect {rect.width:0}x{rect.height:0}px exceeds recommended max {rule.MaxSpriteRectSize.x:0}x{rule.MaxSpriteRectSize.y:0}px.");
            }

            Vector3 worldSize = sprite.bounds.size;
            if (rule.ValidateWorldBounds
                && (worldSize.x > rule.MaxWorldSize.x || worldSize.y > rule.MaxWorldSize.y))
            {
                report.Add(
                    assetPath,
                    rule.CategoryName,
                    $"{sprite.name} world bounds {worldSize.x:0.##}x{worldSize.y:0.##} exceeds recommended max {rule.MaxWorldSize.x:0.##}x{rule.MaxWorldSize.y:0.##}. Check PPU, sliced rect, and runtime scale.");
            }

            if (rule.ValidatePivot && rect.width > 0f && rect.height > 0f)
            {
                Vector2 normalizedPivot = new(sprite.pivot.x / rect.width, sprite.pivot.y / rect.height);
                Vector2 pivotDelta = normalizedPivot - rule.RecommendedPivot;
                if (Mathf.Abs(pivotDelta.x) > rule.PivotTolerance || Mathf.Abs(pivotDelta.y) > rule.PivotTolerance)
                {
                    report.Add(
                        assetPath,
                        rule.CategoryName,
                        $"{sprite.name} pivot {normalizedPivot.x:0.##},{normalizedPivot.y:0.##} differs from recommended {rule.RecommendedPivot.x:0.##},{rule.RecommendedPivot.y:0.##}.");
                }
            }
        }

        private static void LogReport(ValidationReport report, string title)
        {
            if (report.IssueCount == 0)
            {
                Debug.Log($"[SpriteImportValidator] {title}: checked {report.CheckedAssetCount} texture assets, no issues.");
                return;
            }

            int count = Mathf.Min(report.IssueCount, MaxLoggedIssues);
            for (int i = 0; i < count; i++)
            {
                Debug.LogWarning("[SpriteImportValidator] " + report.Issues[i]);
            }

            if (report.IssueCount > MaxLoggedIssues)
            {
                Debug.LogWarning($"[SpriteImportValidator] {report.IssueCount - MaxLoggedIssues} additional issues were omitted from the console.");
            }

            Debug.LogWarning($"[SpriteImportValidator] {title}: checked {report.CheckedAssetCount} texture assets, issues={report.IssueCount}.");
        }

        private static void EnsureFolder(string parent, string name)
        {
            string path = parent + "/" + name;
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, name);
            }
        }

        public sealed class ValidationReport
        {
            private readonly List<string> _issues = new();

            public IReadOnlyList<string> Issues => _issues;
            public int CheckedAssetCount { get; private set; }
            public int IssueCount => _issues.Count;

            public void Add(string assetPath, string category, string message)
            {
                _issues.Add($"{assetPath} [{category}] {message}");
            }

            public void IncrementCheckedAssetCount()
            {
                CheckedAssetCount++;
            }
        }
    }
}
