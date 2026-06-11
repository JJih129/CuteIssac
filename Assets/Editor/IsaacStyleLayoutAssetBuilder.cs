using CuteIssac.Data.Dungeon;
using CuteIssac.Room;
using UnityEditor;
using UnityEngine;

namespace CuteIssac.EditorTools
{
    public static class IsaacStyleLayoutAssetBuilder
    {
        private const string Root = "Assets/Data/Dungeon/IsaacStyleLayouts";
        private const string ObstacleRoot = Root + "/ObstacleLayouts";
        private const string LayoutRoot = Root + "/RoomLayouts";

        [MenuItem("CuteIssac/Build Isaac Style Room Layouts")]
        public static void RebuildFromMenu()
        {
            Debug.Log(Rebuild());
        }

        public static string Rebuild()
        {
            EnsureFolders();

            GameObject roomPrefabObject = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Rooms/BasicGeneratedRoom.prefab");
            RoomController roomPrefab = roomPrefabObject != null ? roomPrefabObject.GetComponent<RoomController>() : null;
            GameObject rock = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/RockObstacle.prefab");
            GameObject pit = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/PitObstacle.prefab");
            GameObject spike = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/SpikeObstacle.prefab");
            GameObject web = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/WebObstacle.prefab");
            GameObject fountain = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/FountainObstacle.prefab");

            if (roomPrefab == null || rock == null || pit == null || spike == null || web == null || fountain == null)
            {
                return "Missing base room or environment prefabs.";
            }

            RoomObstacleLayoutData f1Clear = SaveObstacleLayout("Floor1/F1_ClearSpecial", true);
            RoomObstacleLayoutData f1Secret = SaveObstacleLayout("Floor1/F1_SecretCache", false,
                Entry(rock, -2.2f, 0.75f), Entry(rock, 2.2f, 0.75f), Entry(fountain, 0f, 0f, 0f, 1.05f));
            RoomObstacleLayoutData f1Cross = SaveObstacleLayout("Floor1/F1_Normal_CenterRocks", false,
                Entry(rock, -2.5f, 1.1f), Entry(rock, 2.5f, 1.1f), Entry(rock, -2.5f, -1.1f), Entry(rock, 2.5f, -1.1f));
            RoomObstacleLayoutData f1Lane = SaveObstacleLayout("Floor1/F1_Normal_WebLane", false,
                Entry(web, -2.8f, 0f), Entry(web, 2.8f, 0f), Entry(rock, -0.95f, 1.35f), Entry(rock, 0.95f, -1.35f));
            RoomObstacleLayoutData f1Trap = SaveObstacleLayout("Floor1/F1_Trap_SpikeIntro", false,
                Entry(spike, -1.2f, 0f), Entry(spike, 0f, 0f), Entry(spike, 1.2f, 0f), Entry(rock, -3.2f, 1.45f), Entry(rock, 3.2f, -1.45f));

            RoomObstacleLayoutData f2Clear = SaveObstacleLayout("Floor2/F2_ClearSpecial", true);
            RoomObstacleLayoutData f2Secret = SaveObstacleLayout("Floor2/F2_SecretFountainPocket", false,
                Entry(fountain, 0f, 0f, 0f, 1.1f), Entry(rock, -1.6f, 1.2f), Entry(rock, 1.6f, 1.2f), Entry(web, 0f, -1.35f));
            RoomObstacleLayoutData f2Pits = SaveObstacleLayout("Floor2/F2_Normal_PitIslands", false,
                Entry(pit, -2.45f, 0f), Entry(pit, 2.45f, 0f), Entry(rock, 0f, 1.45f), Entry(rock, 0f, -1.45f));
            RoomObstacleLayoutData f2WebSpikes = SaveObstacleLayout("Floor2/F2_Normal_WebSpikes", false,
                Entry(web, -2.2f, 1.05f), Entry(web, 2.2f, -1.05f), Entry(spike, -0.9f, -0.6f), Entry(spike, 0.9f, 0.6f));
            RoomObstacleLayoutData f2Trap = SaveObstacleLayout("Floor2/F2_Trap_SpikeBridge", false,
                Entry(pit, -2.1f, 0f), Entry(pit, 2.1f, 0f), Entry(spike, -0.7f, 0f), Entry(spike, 0.7f, 0f), Entry(web, 0f, 1.45f));
            RoomObstacleLayoutData f2Mini = SaveObstacleLayout("Floor2/F2_MiniBoss_SparsePits", false,
                Entry(pit, -3.1f, 1.35f), Entry(pit, 3.1f, -1.35f));

            RoomObstacleLayoutData f3Clear = SaveObstacleLayout("Floor3/F3_ClearSpecial", true);
            RoomObstacleLayoutData f3Secret = SaveObstacleLayout("Floor3/F3_SecretVoidCache", false,
                Entry(pit, -2.3f, 0f), Entry(pit, 2.3f, 0f), Entry(fountain, 0f, 0f, 0f, 1.1f));
            RoomObstacleLayoutData f3Cross = SaveObstacleLayout("Floor3/F3_Normal_VoidCross", false,
                Entry(pit, -2.4f, 0f), Entry(pit, 2.4f, 0f), Entry(spike, 0f, 1.2f), Entry(spike, 0f, -1.2f));
            RoomObstacleLayoutData f3Ring = SaveObstacleLayout("Floor3/F3_Normal_HazardRing", false,
                Entry(spike, -1.7f, 1.05f), Entry(spike, 1.7f, 1.05f), Entry(spike, -1.7f, -1.05f), Entry(spike, 1.7f, -1.05f), Entry(pit, 0f, 0f));
            RoomObstacleLayoutData f3Trap = SaveObstacleLayout("Floor3/F3_Trap_CrossPressure", false,
                Entry(pit, -2.6f, 0f), Entry(pit, 2.6f, 0f), Entry(spike, -0.85f, 0.85f), Entry(spike, 0.85f, 0.85f), Entry(spike, -0.85f, -0.85f), Entry(spike, 0.85f, -0.85f));
            RoomObstacleLayoutData f3Mini = SaveObstacleLayout("Floor3/F3_MiniBoss_OpenCorners", false,
                Entry(pit, -3.1f, 1.45f), Entry(pit, 3.1f, 1.45f), Entry(pit, -3.1f, -1.45f), Entry(pit, 3.1f, -1.45f));

            RoomLayoutSet floor1Set = SaveLayoutSet("Floor1RoomLayoutSet", new[]
            {
                SaveRoomLayout("Floor1/F1_ClearSpecialLayout", roomPrefab, f1Clear, 1, RoomType.Start, RoomType.Treasure, RoomType.Shop, RoomType.Boss, RoomType.MiniBoss),
                SaveRoomLayout("Floor1/F1_SecretCacheLayout", roomPrefab, f1Secret, 1, RoomType.Secret),
                SaveRoomLayout("Floor1/F1_NormalCenterRocksLayout", roomPrefab, f1Cross, 4, RoomType.Normal),
                SaveRoomLayout("Floor1/F1_NormalWebLaneLayout", roomPrefab, f1Lane, 3, RoomType.Normal),
                SaveRoomLayout("Floor1/F1_TrapSpikeIntroLayout", roomPrefab, f1Trap, 1, RoomType.Trap, RoomType.Challenge, RoomType.Curse)
            });

            RoomLayoutSet floor2Set = SaveLayoutSet("Floor2RoomLayoutSet", new[]
            {
                SaveRoomLayout("Floor2/F2_ClearSpecialLayout", roomPrefab, f2Clear, 1, RoomType.Start, RoomType.Treasure, RoomType.Shop, RoomType.Boss),
                SaveRoomLayout("Floor2/F2_SecretFountainPocketLayout", roomPrefab, f2Secret, 1, RoomType.Secret),
                SaveRoomLayout("Floor2/F2_NormalPitIslandsLayout", roomPrefab, f2Pits, 4, RoomType.Normal),
                SaveRoomLayout("Floor2/F2_NormalWebSpikesLayout", roomPrefab, f2WebSpikes, 3, RoomType.Normal),
                SaveRoomLayout("Floor2/F2_TrapSpikeBridgeLayout", roomPrefab, f2Trap, 2, RoomType.Trap, RoomType.Challenge, RoomType.Curse),
                SaveRoomLayout("Floor2/F2_MiniBossSparsePitsLayout", roomPrefab, f2Mini, 1, RoomType.MiniBoss)
            });

            RoomLayoutSet floor3Set = SaveLayoutSet("Floor3RoomLayoutSet", new[]
            {
                SaveRoomLayout("Floor3/F3_ClearSpecialLayout", roomPrefab, f3Clear, 1, RoomType.Start, RoomType.Treasure, RoomType.Shop, RoomType.Boss),
                SaveRoomLayout("Floor3/F3_SecretVoidCacheLayout", roomPrefab, f3Secret, 1, RoomType.Secret),
                SaveRoomLayout("Floor3/F3_NormalVoidCrossLayout", roomPrefab, f3Cross, 4, RoomType.Normal),
                SaveRoomLayout("Floor3/F3_NormalHazardRingLayout", roomPrefab, f3Ring, 3, RoomType.Normal),
                SaveRoomLayout("Floor3/F3_TrapCrossPressureLayout", roomPrefab, f3Trap, 2, RoomType.Trap, RoomType.Challenge, RoomType.Curse),
                SaveRoomLayout("Floor3/F3_MiniBossOpenCornersLayout", roomPrefab, f3Mini, 1, RoomType.MiniBoss)
            });

            AssignFloorLayoutSet("Assets/Data/Dungeon/Floor1DungeonConfig.asset", floor1Set);
            AssignFloorLayoutSet("Assets/Data/Dungeon/Floor2DungeonConfig.asset", floor2Set);
            AssignFloorLayoutSet("Assets/Data/Dungeon/Floor3DungeonConfig.asset", floor3Set);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return "Created Isaac-style room layout sets for floors 1-3.";
        }

        private static void EnsureFolders()
        {
            EnsureFolder("Assets", "Editor");
            EnsureFolder("Assets/Data/Dungeon", "IsaacStyleLayouts");
            EnsureFolder(Root, "ObstacleLayouts");
            EnsureFolder(Root, "RoomLayouts");
            EnsureFolder(LayoutRoot, "Floor1");
            EnsureFolder(LayoutRoot, "Floor2");
            EnsureFolder(LayoutRoot, "Floor3");
            EnsureFolder(ObstacleRoot, "Floor1");
            EnsureFolder(ObstacleRoot, "Floor2");
            EnsureFolder(ObstacleRoot, "Floor3");
        }

        private static void EnsureFolder(string parent, string name)
        {
            string path = parent + "/" + name;
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, name);
            }
        }

        private static ObstacleSpec Entry(GameObject prefab, float x, float y, float rotationZ = 0f, float scale = 1f)
        {
            return new ObstacleSpec(prefab, new Vector2(x, y), rotationZ, scale);
        }

        private static RoomObstacleLayoutData SaveObstacleLayout(string relativeName, bool disabled, params ObstacleSpec[] entries)
        {
            string path = ObstacleRoot + "/" + relativeName + ".asset";
            RoomObstacleLayoutData asset = AssetDatabase.LoadAssetAtPath<RoomObstacleLayoutData>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<RoomObstacleLayoutData>();
                AssetDatabase.CreateAsset(asset, path);
            }

            SerializedObject serialized = new SerializedObject(asset);
            serialized.FindProperty("layoutId").stringValue = relativeName.Replace('/', '_').ToLowerInvariant();
            serialized.FindProperty("disablesObstacleSpawning").boolValue = disabled;
            SerializedProperty obstacles = serialized.FindProperty("obstacles");
            obstacles.arraySize = entries.Length;

            for (int i = 0; i < entries.Length; i++)
            {
                SerializedProperty item = obstacles.GetArrayElementAtIndex(i);
                item.FindPropertyRelative("prefab").objectReferenceValue = entries[i].Prefab;
                item.FindPropertyRelative("localPosition").vector2Value = entries[i].LocalPosition;
                item.FindPropertyRelative("rotationZ").floatValue = entries[i].RotationZ;
                item.FindPropertyRelative("scaleMultiplier").floatValue = entries[i].ScaleMultiplier;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static RoomLayoutData SaveRoomLayout(string relativeName, RoomController roomPrefab, RoomObstacleLayoutData obstacleLayout, int weight, params RoomType[] roomTypes)
        {
            string path = LayoutRoot + "/" + relativeName + ".asset";
            RoomLayoutData asset = AssetDatabase.LoadAssetAtPath<RoomLayoutData>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<RoomLayoutData>();
                AssetDatabase.CreateAsset(asset, path);
            }

            SerializedObject serialized = new SerializedObject(asset);
            serialized.FindProperty("layoutId").stringValue = relativeName.Replace('/', '_').ToLowerInvariant();
            serialized.FindProperty("roomPrefab").objectReferenceValue = roomPrefab;
            serialized.FindProperty("obstacleLayout").objectReferenceValue = obstacleLayout;
            serialized.FindProperty("supportedDoorMask").intValue = 15;
            serialized.FindProperty("selectionWeight").intValue = Mathf.Max(1, weight);

            SerializedProperty supportedRoomTypes = serialized.FindProperty("supportedRoomTypes");
            supportedRoomTypes.arraySize = roomTypes.Length;
            for (int i = 0; i < roomTypes.Length; i++)
            {
                supportedRoomTypes.GetArrayElementAtIndex(i).enumValueIndex = (int)roomTypes[i];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static RoomLayoutSet SaveLayoutSet(string name, RoomLayoutData[] layouts)
        {
            string path = Root + "/" + name + ".asset";
            RoomLayoutSet asset = AssetDatabase.LoadAssetAtPath<RoomLayoutSet>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<RoomLayoutSet>();
                AssetDatabase.CreateAsset(asset, path);
            }

            SerializedObject serialized = new SerializedObject(asset);
            SerializedProperty layoutList = serialized.FindProperty("layouts");
            layoutList.arraySize = layouts.Length;
            for (int i = 0; i < layouts.Length; i++)
            {
                layoutList.GetArrayElementAtIndex(i).objectReferenceValue = layouts[i];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static void AssignFloorLayoutSet(string configPath, RoomLayoutSet layoutSet)
        {
            FloorConfig floorConfig = AssetDatabase.LoadAssetAtPath<FloorConfig>(configPath);
            if (floorConfig == null || layoutSet == null)
            {
                return;
            }

            SerializedObject serialized = new SerializedObject(floorConfig);
            SerializedProperty sharedLayoutSets = serialized.FindProperty("sharedLayoutSets");
            sharedLayoutSets.arraySize = 1;
            sharedLayoutSets.GetArrayElementAtIndex(0).objectReferenceValue = layoutSet;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(floorConfig);
        }

        private readonly struct ObstacleSpec
        {
            public ObstacleSpec(GameObject prefab, Vector2 localPosition, float rotationZ, float scaleMultiplier)
            {
                Prefab = prefab;
                LocalPosition = localPosition;
                RotationZ = rotationZ;
                ScaleMultiplier = scaleMultiplier;
            }

            public GameObject Prefab { get; }
            public Vector2 LocalPosition { get; }
            public float RotationZ { get; }
            public float ScaleMultiplier { get; }
        }
    }
}
