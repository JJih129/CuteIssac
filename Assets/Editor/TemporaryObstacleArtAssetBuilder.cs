using System.IO;
using CuteIssac.Data.Room;
using CuteIssac.Room;
using UnityEditor;
using UnityEngine;

namespace CuteIssac.EditorTools
{
    public static class TemporaryObstacleArtAssetBuilder
    {
        private const int TextureSize = 128;
        private const string ArtRoot = "Assets/Art/Temporary";
        private const string ObstacleArtRoot = ArtRoot + "/Obstacles";
        private const string ArtSetRoot = "Assets/Data/Room/ObstacleArtSets";
        private const string ArtSetPath = ArtSetRoot + "/DefaultObstacleArtSet.asset";

        [MenuItem("CuteIssac/Build Temporary Obstacle Art")]
        public static void RebuildFromMenu()
        {
            Debug.Log(Rebuild());
        }

        public static string Rebuild()
        {
            EnsureFolders();

            Sprite rock = SaveSprite("temp_obstacle_rock.png", DrawRock());
            Sprite pit = SaveSprite("temp_obstacle_pit.png", DrawPit());
            Sprite spike = SaveSprite("temp_obstacle_spike.png", DrawSpike());
            Sprite web = SaveSprite("temp_obstacle_web.png", DrawWeb());
            Sprite fountain = SaveSprite("temp_obstacle_fountain.png", DrawFountain());
            RoomObstacleArtSet artSet = SaveArtSet(rock, pit, spike, web, fountain);
            AssignArtSetToPrefab("Assets/Prefabs/Environment/RockObstacle.prefab", artSet);
            AssignArtSetToPrefab("Assets/Prefabs/Environment/PitObstacle.prefab", artSet);
            AssignArtSetToPrefab("Assets/Prefabs/Environment/SpikeObstacle.prefab", artSet);
            AssignArtSetToPrefab("Assets/Prefabs/Environment/WebObstacle.prefab", artSet);
            AssignArtSetToPrefab("Assets/Prefabs/Environment/FountainObstacle.prefab", artSet);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return "Created temporary obstacle sprites and assigned DefaultObstacleArtSet.";
        }

        private static void EnsureFolders()
        {
            EnsureFolder("Assets", "Art");
            EnsureFolder("Assets/Art", "Temporary");
            EnsureFolder(ArtRoot, "Obstacles");
            EnsureFolder("Assets/Data", "Room");
            EnsureFolder("Assets/Data/Room", "ObstacleArtSets");
        }

        private static void EnsureFolder(string parent, string name)
        {
            string path = parent + "/" + name;
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, name);
            }
        }

        private static Sprite SaveSprite(string fileName, Texture2D texture)
        {
            string path = ObstacleArtRoot + "/" + fileName;
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;

            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 96f;
                importer.mipmapEnabled = false;
                importer.filterMode = FilterMode.Point;
                importer.alphaIsTransparency = true;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static RoomObstacleArtSet SaveArtSet(Sprite rock, Sprite pit, Sprite spike, Sprite web, Sprite fountain)
        {
            RoomObstacleArtSet artSet = AssetDatabase.LoadAssetAtPath<RoomObstacleArtSet>(ArtSetPath);

            if (artSet == null)
            {
                artSet = ScriptableObject.CreateInstance<RoomObstacleArtSet>();
                AssetDatabase.CreateAsset(artSet, ArtSetPath);
            }

            SerializedObject serialized = new SerializedObject(artSet);
            SerializedProperty entries = serialized.FindProperty("entries");
            entries.arraySize = 5;
            ConfigureEntry(entries.GetArrayElementAtIndex(0), RoomObstacleType.Rock, rock, new Vector2(1.1f, 1.05f));
            ConfigureEntry(entries.GetArrayElementAtIndex(1), RoomObstacleType.Pit, pit, new Vector2(1.14f, 0.98f));
            ConfigureEntry(entries.GetArrayElementAtIndex(2), RoomObstacleType.Spike, spike, new Vector2(1.12f, 1.02f));
            ConfigureEntry(entries.GetArrayElementAtIndex(3), RoomObstacleType.Web, web, new Vector2(1.08f, 1.08f));
            ConfigureEntry(entries.GetArrayElementAtIndex(4), RoomObstacleType.Fountain, fountain, new Vector2(1.16f, 1.16f));
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(artSet);
            return artSet;
        }

        private static void ConfigureEntry(SerializedProperty entry, RoomObstacleType obstacleType, Sprite bodySprite, Vector2 scale)
        {
            entry.FindPropertyRelative("obstacleType").enumValueIndex = (int)obstacleType;
            entry.FindPropertyRelative("bodySprite").objectReferenceValue = bodySprite;
            entry.FindPropertyRelative("bodyColor").colorValue = Color.white;
            entry.FindPropertyRelative("bodyLocalOffset").vector2Value = Vector2.zero;
            entry.FindPropertyRelative("bodyLocalScale").vector2Value = scale;
            entry.FindPropertyRelative("bodyRotationZ").floatValue = 0f;
            entry.FindPropertyRelative("bodySortingOffset").intValue = 0;
            entry.FindPropertyRelative("accentSprite").objectReferenceValue = null;
            entry.FindPropertyRelative("accentColor").colorValue = Color.white;
            entry.FindPropertyRelative("accentLocalOffset").vector2Value = Vector2.zero;
            entry.FindPropertyRelative("accentLocalScale").vector2Value = Vector2.one;
            entry.FindPropertyRelative("accentRotationZ").floatValue = 0f;
            entry.FindPropertyRelative("accentSortingOffset").intValue = 1;
        }

        private static void AssignArtSetToPrefab(string prefabPath, RoomObstacleArtSet artSet)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);

            if (root == null)
            {
                return;
            }

            RoomObstacleVisual visual = root.GetComponent<RoomObstacleVisual>();
            RoomObstacleController controller = root.GetComponent<RoomObstacleController>();

            if (visual != null)
            {
                SpriteRenderer bodyRenderer = root.GetComponent<SpriteRenderer>();
                SpriteRenderer accentRenderer = EnsureChildRenderer(root.transform, "ObstacleAccent", bodyRenderer);

                if (bodyRenderer != null && controller != null && artSet.TryGetEntry(controller.ObstacleType, out RoomObstacleArtSet.Entry entry) && entry != null)
                {
                    bodyRenderer.sprite = entry.BodySprite;
                    bodyRenderer.color = entry.BodyColor;
                    bodyRenderer.transform.localPosition = entry.BodyLocalOffset;
                    bodyRenderer.transform.localScale = new Vector3(entry.BodyLocalScale.x, entry.BodyLocalScale.y, 1f);
                    bodyRenderer.transform.localRotation = Quaternion.Euler(0f, 0f, entry.BodyRotationZ);
                    bodyRenderer.enabled = entry.HasBodySprite;
                }

                SerializedObject serialized = new SerializedObject(visual);
                serialized.FindProperty("bodyRenderer").objectReferenceValue = bodyRenderer;
                serialized.FindProperty("accentRenderer").objectReferenceValue = accentRenderer;
                serialized.FindProperty("artSet").objectReferenceValue = artSet;
                serialized.FindProperty("preferArtSet").boolValue = true;
                serialized.FindProperty("useProceduralVisual").boolValue = true;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(visual);
            }

            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            PrefabUtility.UnloadPrefabContents(root);
        }

        private static SpriteRenderer EnsureChildRenderer(Transform root, string childName, SpriteRenderer referenceRenderer)
        {
            Transform child = root.Find(childName);

            if (child == null)
            {
                GameObject childObject = new GameObject(childName);
                childObject.transform.SetParent(root, false);
                childObject.layer = root.gameObject.layer;
                child = childObject.transform;
            }

            SpriteRenderer renderer = child.GetComponent<SpriteRenderer>();

            if (renderer == null)
            {
                renderer = child.gameObject.AddComponent<SpriteRenderer>();
            }

            if (referenceRenderer != null)
            {
                renderer.sortingLayerID = referenceRenderer.sortingLayerID;
                renderer.sortingOrder = referenceRenderer.sortingOrder + 1;
            }

            renderer.enabled = false;
            return renderer;
        }

        private static Texture2D CreateCanvas()
        {
            Texture2D texture = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

            Clear(texture);
            return texture;
        }

        private static Texture2D DrawRock()
        {
            Texture2D texture = CreateCanvas();
            FillEllipse(texture, 64, 72, 48, 34, new Color32(110, 73, 92, 170));
            FillEllipse(texture, 64, 62, 40, 36, new Color32(191, 157, 177, 255));
            FillEllipse(texture, 56, 52, 24, 12, new Color32(244, 219, 232, 255));
            FillEllipse(texture, 80, 66, 13, 9, new Color32(159, 109, 137, 255));
            DrawLine(texture, 58, 60, 48, 80, new Color32(112, 72, 99, 255), 4);
            DrawLine(texture, 74, 56, 84, 76, new Color32(112, 72, 99, 255), 4);
            DrawOutline(texture, new Color32(74, 45, 62, 255));
            texture.Apply();
            return texture;
        }

        private static Texture2D DrawPit()
        {
            Texture2D texture = CreateCanvas();
            FillEllipse(texture, 64, 66, 50, 36, new Color32(69, 45, 82, 210));
            FillEllipse(texture, 64, 60, 42, 30, new Color32(172, 132, 188, 255));
            FillEllipse(texture, 64, 66, 34, 22, new Color32(17, 12, 27, 255));
            FillEllipse(texture, 53, 50, 12, 6, new Color32(226, 198, 236, 230));
            FillEllipse(texture, 82, 55, 10, 5, new Color32(226, 198, 236, 210));
            DrawOutline(texture, new Color32(45, 28, 58, 255));
            texture.Apply();
            return texture;
        }

        private static Texture2D DrawSpike()
        {
            Texture2D texture = CreateCanvas();
            FillEllipse(texture, 64, 82, 48, 18, new Color32(68, 45, 60, 170));
            FillRect(texture, 28, 72, 100, 88, new Color32(144, 93, 115, 255));
            FillRect(texture, 34, 66, 94, 75, new Color32(205, 122, 138, 255));
            DrawTriangle(texture, 38, 72, 50, 28, 62, 72, new Color32(247, 237, 250, 255));
            DrawTriangle(texture, 53, 72, 64, 22, 75, 72, new Color32(247, 237, 250, 255));
            DrawTriangle(texture, 68, 72, 80, 30, 92, 72, new Color32(247, 237, 250, 255));
            DrawLine(texture, 50, 34, 54, 70, new Color32(179, 155, 178, 255), 3);
            DrawLine(texture, 64, 27, 68, 70, new Color32(179, 155, 178, 255), 3);
            DrawLine(texture, 80, 36, 84, 70, new Color32(179, 155, 178, 255), 3);
            DrawOutline(texture, new Color32(76, 47, 65, 255));
            texture.Apply();
            return texture;
        }

        private static Texture2D DrawWeb()
        {
            Texture2D texture = CreateCanvas();
            FillEllipse(texture, 64, 66, 48, 48, new Color32(30, 38, 52, 96));
            DrawEllipseOutline(texture, 64, 64, 45, 45, new Color32(226, 247, 255, 230), 3);
            DrawEllipseOutline(texture, 64, 64, 30, 30, new Color32(170, 226, 255, 210), 3);
            DrawEllipseOutline(texture, 64, 64, 16, 16, new Color32(226, 247, 255, 210), 2);
            DrawLine(texture, 64, 20, 64, 108, new Color32(238, 251, 255, 235), 3);
            DrawLine(texture, 20, 64, 108, 64, new Color32(238, 251, 255, 235), 3);
            DrawLine(texture, 32, 32, 96, 96, new Color32(190, 235, 255, 220), 3);
            DrawLine(texture, 96, 32, 32, 96, new Color32(190, 235, 255, 220), 3);
            FillEllipse(texture, 64, 64, 7, 7, new Color32(255, 255, 255, 255));
            texture.Apply();
            return texture;
        }

        private static Texture2D DrawFountain()
        {
            Texture2D texture = CreateCanvas();
            FillEllipse(texture, 64, 86, 50, 20, new Color32(28, 52, 75, 120));
            FillEllipse(texture, 64, 78, 44, 30, new Color32(185, 232, 255, 255));
            FillEllipse(texture, 64, 74, 34, 18, new Color32(66, 211, 255, 255));
            FillRect(texture, 56, 48, 72, 80, new Color32(176, 224, 255, 255));
            DrawLine(texture, 64, 20, 64, 58, new Color32(237, 251, 255, 255), 5);
            DrawLine(texture, 52, 28, 60, 58, new Color32(137, 219, 255, 255), 4);
            DrawLine(texture, 76, 28, 68, 58, new Color32(137, 219, 255, 255), 4);
            FillEllipse(texture, 64, 28, 18, 7, new Color32(237, 251, 255, 230));
            FillEllipse(texture, 46, 38, 8, 8, new Color32(237, 251, 255, 230));
            FillEllipse(texture, 82, 38, 8, 8, new Color32(237, 251, 255, 230));
            DrawOutline(texture, new Color32(56, 106, 144, 255));
            texture.Apply();
            return texture;
        }

        private static void Clear(Texture2D texture)
        {
            Color32 clear = new Color32(0, 0, 0, 0);
            Color32[] pixels = texture.GetPixels32();

            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = clear;
            }

            texture.SetPixels32(pixels);
        }

        private static void DrawOutline(Texture2D texture, Color32 color)
        {
            DrawEllipseOutline(texture, 64, 64, 51, 43, color, 2);
        }

        private static void FillRect(Texture2D texture, int minX, int minY, int maxX, int maxY, Color32 color)
        {
            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    SetPixel(texture, x, y, color);
                }
            }
        }

        private static void FillEllipse(Texture2D texture, int centerX, int centerY, int radiusX, int radiusY, Color32 color)
        {
            int minX = centerX - radiusX;
            int maxX = centerX + radiusX;
            int minY = centerY - radiusY;
            int maxY = centerY + radiusY;

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    float dx = (x - centerX) / (float)radiusX;
                    float dy = (y - centerY) / (float)radiusY;

                    if ((dx * dx) + (dy * dy) <= 1f)
                    {
                        SetPixel(texture, x, y, color);
                    }
                }
            }
        }

        private static void DrawEllipseOutline(Texture2D texture, int centerX, int centerY, int radiusX, int radiusY, Color32 color, int thickness)
        {
            for (int y = centerY - radiusY - thickness; y <= centerY + radiusY + thickness; y++)
            {
                for (int x = centerX - radiusX - thickness; x <= centerX + radiusX + thickness; x++)
                {
                    float dx = (x - centerX) / (float)radiusX;
                    float dy = (y - centerY) / (float)radiusY;
                    float value = (dx * dx) + (dy * dy);

                    if (value >= 0.88f && value <= 1.12f)
                    {
                        SetPixel(texture, x, y, color);
                    }
                }
            }
        }

        private static void DrawTriangle(Texture2D texture, int ax, int ay, int bx, int by, int cx, int cy, Color32 color)
        {
            int minX = Mathf.Min(ax, Mathf.Min(bx, cx));
            int maxX = Mathf.Max(ax, Mathf.Max(bx, cx));
            int minY = Mathf.Min(ay, Mathf.Min(by, cy));
            int maxY = Mathf.Max(ay, Mathf.Max(by, cy));

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    if (IsInTriangle(x, y, ax, ay, bx, by, cx, cy))
                    {
                        SetPixel(texture, x, y, color);
                    }
                }
            }
        }

        private static bool IsInTriangle(int px, int py, int ax, int ay, int bx, int by, int cx, int cy)
        {
            float denominator = ((by - cy) * (ax - cx)) + ((cx - bx) * (ay - cy));
            float alpha = (((by - cy) * (px - cx)) + ((cx - bx) * (py - cy))) / denominator;
            float beta = (((cy - ay) * (px - cx)) + ((ax - cx) * (py - cy))) / denominator;
            float gamma = 1f - alpha - beta;
            return alpha >= 0f && beta >= 0f && gamma >= 0f;
        }

        private static void DrawLine(Texture2D texture, int startX, int startY, int endX, int endY, Color32 color, int thickness)
        {
            int dx = Mathf.Abs(endX - startX);
            int dy = Mathf.Abs(endY - startY);
            int sx = startX < endX ? 1 : -1;
            int sy = startY < endY ? 1 : -1;
            int err = dx - dy;
            int x = startX;
            int y = startY;

            while (true)
            {
                FillEllipse(texture, x, y, thickness, thickness, color);

                if (x == endX && y == endY)
                {
                    break;
                }

                int e2 = err * 2;

                if (e2 > -dy)
                {
                    err -= dy;
                    x += sx;
                }

                if (e2 < dx)
                {
                    err += dx;
                    y += sy;
                }
            }
        }

        private static void SetPixel(Texture2D texture, int x, int y, Color32 color)
        {
            if (x < 0 || y < 0 || x >= TextureSize || y >= TextureSize)
            {
                return;
            }

            texture.SetPixel(x, TextureSize - 1 - y, color);
        }
    }
}
