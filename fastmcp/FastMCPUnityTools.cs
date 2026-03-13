using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.IO;

public static class FastMCPUnityTools
{

    // --------------------------------------------------
    // Scene 생성
    // --------------------------------------------------

    public static void CreateScene()
    {
        string sceneName = System.Environment.GetCommandLineArgs()[System.Environment.GetCommandLineArgs().Length - 1];

        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects);

        string path = $"Assets/Scenes/{sceneName}.unity";

        Directory.CreateDirectory("Assets/Scenes");

        EditorSceneManager.SaveScene(scene, path);

        Debug.Log("Scene Created: " + path);
    }

    // --------------------------------------------------
    // GameObject 생성
    // --------------------------------------------------

    public static void CreateGameObject()
    {
        string name = System.Environment.GetCommandLineArgs()[System.Environment.GetCommandLineArgs().Length - 1];

        GameObject go = new GameObject(name);

        Debug.Log("GameObject Created: " + name);
    }

    // --------------------------------------------------
    // Component 추가
    // --------------------------------------------------

    public static void AddComponent()
    {
        var args = System.Environment.GetCommandLineArgs();

        string objectName = args[args.Length - 2];
        string component = args[args.Length - 1];

        GameObject obj = GameObject.Find(objectName);

        if (obj == null)
        {
            Debug.LogError("GameObject not found");
            return;
        }

        var type = System.Type.GetType(component);

        if (type == null)
        {
            Debug.LogError("Component type not found");
            return;
        }

        obj.AddComponent(type);
    }

    // --------------------------------------------------
    // Prefab 생성
    // --------------------------------------------------

    public static void CreatePrefab()
    {
        var args = System.Environment.GetCommandLineArgs();

        string objectName = args[args.Length - 2];
        string prefabName = args[args.Length - 1];

        GameObject obj = GameObject.Find(objectName);

        if (obj == null)
        {
            Debug.LogError("GameObject not found");
            return;
        }

        Directory.CreateDirectory("Assets/Prefabs");

        string path = $"Assets/Prefabs/{prefabName}.prefab";

        PrefabUtility.SaveAsPrefabAsset(obj, path);

        Debug.Log("Prefab Created: " + path);
    }

    // --------------------------------------------------
    // Build
    // --------------------------------------------------

    public static void BuildGame()
    {
        Directory.CreateDirectory("Build");

        BuildPipeline.BuildPlayer(
            new[] { "Assets/Scenes/SampleScene.unity" },
            "Build/Game.exe",
            BuildTarget.StandaloneWindows64,
            BuildOptions.None
        );

        Debug.Log("Build Completed");
    }

}