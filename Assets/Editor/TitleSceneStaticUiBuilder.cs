using System.Collections.Generic;
using CuteIssac.Core.Meta;
using CuteIssac.Data.Balance;
using CuteIssac.Data.Debug;
using CuteIssac.Data.Dungeon;
using CuteIssac.Data.Enemy;
using CuteIssac.Data.Item;
using CuteIssac.Data.Run;
using CuteIssac.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CuteIssac.Editor
{
    /// <summary>
    /// TitleScene의 상위 UI를 씬 오브젝트로 고정 배치하는 에디터 전용 빌더입니다.
    /// 런타임 생성 코드를 쓰지 않고, 다른 작업자가 Hierarchy/Inspector에서 배치와 스프라이트를 직접 수정할 수 있게 유지합니다.
    /// </summary>
    public static class TitleSceneStaticUiBuilder
    {
        private const string ScenePath = "Assets/Scenes/TitleScene.unity";
        private const int UiLayer = 5;

        [MenuItem("CuteIssac/Title/Rebuild Static Title Scene UI")]
        public static void RebuildStaticTitleSceneUi()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Dictionary<string, Object> preservedArt = PreserveTitleArtReferences();

            DeleteRootIfExists("TitleCanvas");
            DeleteRootIfExists("TitleMenuController");
            DeleteRootIfExists("EventSystem");

            Canvas canvas = CreateTitleCanvas(out CanvasScaler canvasScaler);
            Image backdropImage = CreatePanel("Backdrop", canvas.transform, Color.white, raycastTarget: false);
            Stretch(backdropImage.rectTransform, Vector2.zero, Vector2.one);

            Image shellImage = CreatePanel("TitleShell", canvas.transform, new Color(0.09f, 0.095f, 0.105f, 0.96f), raycastTarget: true);
            Stretch(shellImage.rectTransform, new Vector2(0.14f, 0.12f), new Vector2(0.86f, 0.88f));

            Image logoImage = CreateImage("TitleLogo", shellImage.transform, Color.white, raycastTarget: false);
            Stretch(logoImage.rectTransform, new Vector2(0.06f, 0.79f), new Vector2(0.32f, 0.95f));
            logoImage.preserveAspect = true;

            Text titleText = CreateText("Title", shellImage.transform, "CUTE ISSAC", 42, FontStyle.Bold, TextAnchor.MiddleLeft, Color.white);
            Stretch(titleText.rectTransform, new Vector2(0.34f, 0.82f), new Vector2(0.58f, 0.94f));

            Text subtitleText = CreateText("Subtitle", shellImage.transform, "Run setup and meta progression", 17, FontStyle.Normal, TextAnchor.MiddleLeft, new Color(0.72f, 0.78f, 0.82f, 1f));
            Stretch(subtitleText.rectTransform, new Vector2(0.34f, 0.775f), new Vector2(0.58f, 0.835f));

            Image primaryActionsImage = CreatePanel("PrimaryActions", shellImage.transform, new Color(0.125f, 0.14f, 0.155f, 0.92f), raycastTarget: true);
            Stretch(primaryActionsImage.rectTransform, new Vector2(0.055f, 0.25f), new Vector2(0.32f, 0.735f));

            Image keyArtImage = CreateImage("KeyArt", primaryActionsImage.transform, new Color(0.18f, 0.2f, 0.22f, 0.82f), raycastTarget: false);
            Stretch(keyArtImage.rectTransform, new Vector2(0f, 0.72f), Vector2.one);
            keyArtImage.preserveAspect = true;
            Text keyArtText = CreateText("KeyArtPlaceholder", keyArtImage.transform, "TITLE ART", 18, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0.74f, 0.78f, 0.82f, 0.82f));
            Stretch(keyArtText.rectTransform, Vector2.zero, Vector2.one);

            RectTransform primaryButtons = CreateRect("PrimaryButtons", primaryActionsImage.transform);
            Stretch(primaryButtons, Vector2.zero, new Vector2(1f, 0.72f));
            VerticalLayoutGroup primaryLayout = primaryButtons.gameObject.AddComponent<VerticalLayoutGroup>();
            primaryLayout.padding = new RectOffset(14, 14, 14, 14);
            primaryLayout.spacing = 10f;
            primaryLayout.childControlWidth = true;
            primaryLayout.childControlHeight = false;
            primaryLayout.childForceExpandWidth = true;
            primaryLayout.childForceExpandHeight = false;

            Button newRunButton = CreateButton("New Run", primaryButtons, 56f, 20, 240f);
            AssignButtonAction(newRunButton, TitleMenuButtonAction.NewRun);
            Button continueButton = CreateButton("Continue", primaryButtons, 56f, 20, 240f);
            AssignButtonAction(continueButton, TitleMenuButtonAction.Continue);
            Button characterButton = CreateButton("Character", primaryButtons, 56f, 20, 240f);
            AssignButtonAction(characterButton, TitleMenuButtonAction.Character);
            Button optionsButton = CreateButton("Options", primaryButtons, 56f, 20, 240f);
            AssignButtonAction(optionsButton, TitleMenuButtonAction.Options);

            Image contentPanelImage = CreatePanel("ContentScroll", shellImage.transform, new Color(0.13f, 0.13f, 0.14f, 0.9f), raycastTarget: true);
            Stretch(contentPanelImage.rectTransform, new Vector2(0.35f, 0.25f), new Vector2(0.945f, 0.735f));
            ScrollRect scrollRect = contentPanelImage.gameObject.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.inertia = true;
            scrollRect.scrollSensitivity = 36f;

            Image viewportImage = CreatePanel("Viewport", contentPanelImage.transform, new Color(0f, 0f, 0f, 0.01f), raycastTarget: true);
            Stretch(viewportImage.rectTransform, Vector2.zero, Vector2.one);
            Mask mask = viewportImage.gameObject.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            RectTransform contentRoot = CreateRect("Content", viewportImage.transform);
            contentRoot.anchorMin = new Vector2(0f, 1f);
            contentRoot.anchorMax = new Vector2(1f, 1f);
            contentRoot.pivot = new Vector2(0.5f, 1f);
            contentRoot.offsetMin = Vector2.zero;
            contentRoot.offsetMax = Vector2.zero;
            contentRoot.anchoredPosition = Vector2.zero;

            VerticalLayoutGroup contentLayout = contentRoot.gameObject.AddComponent<VerticalLayoutGroup>();
            contentLayout.padding = new RectOffset(16, 16, 16, 16);
            contentLayout.spacing = 24f;
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = false;
            contentLayout.childForceExpandWidth = true;
            contentLayout.childForceExpandHeight = false;

            ContentSizeFitter contentFitter = contentRoot.gameObject.AddComponent<ContentSizeFitter>();
            contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.viewport = viewportImage.rectTransform;
            scrollRect.content = contentRoot;

            Image utilityBarImage = CreatePanel("UtilityBar", shellImage.transform, new Color(0.11f, 0.12f, 0.13f, 0.88f), raycastTarget: true);
            Stretch(utilityBarImage.rectTransform, new Vector2(0.055f, 0.07f), new Vector2(0.945f, 0.19f));
            HorizontalLayoutGroup utilityLayout = utilityBarImage.gameObject.AddComponent<HorizontalLayoutGroup>();
            utilityLayout.padding = new RectOffset(12, 12, 12, 12);
            utilityLayout.spacing = 10f;
            utilityLayout.childControlWidth = true;
            utilityLayout.childControlHeight = true;
            utilityLayout.childForceExpandWidth = true;
            utilityLayout.childForceExpandHeight = true;
            utilityLayout.childAlignment = TextAnchor.MiddleCenter;

            Button collectionButton = CreateButton("Collection", utilityBarImage.rectTransform, 42f, 15, 150f);
            AssignButtonAction(collectionButton, TitleMenuButtonAction.Collection);
            Button achievementsButton = CreateButton("Achievements", utilityBarImage.rectTransform, 42f, 15, 170f);
            AssignButtonAction(achievementsButton, TitleMenuButtonAction.Achievements);
            Button statsButton = CreateButton("Stats", utilityBarImage.rectTransform, 42f, 15, 120f);
            AssignButtonAction(statsButton, TitleMenuButtonAction.Stats);
            Button creditsButton = CreateButton("Credits", utilityBarImage.rectTransform, 42f, 15, 120f);
            AssignButtonAction(creditsButton, TitleMenuButtonAction.Credits);
            Button resetButton = CreateButton("Reset", utilityBarImage.rectTransform, 42f, 15, 120f);
            AssignButtonAction(resetButton, TitleMenuButtonAction.ResetData);
            Button quitButton = CreateButton("Quit", utilityBarImage.rectTransform, 42f, 15, 100f);
            AssignButtonAction(quitButton, TitleMenuButtonAction.Quit);

            TitleMenuController controller = CreateController(
                canvas,
                canvasScaler,
                shellImage.rectTransform,
                contentRoot,
                scrollRect,
                titleText,
                backdropImage,
                logoImage,
                keyArtImage,
                shellImage,
                primaryActionsImage,
                contentPanelImage,
                utilityBarImage,
                newRunButton,
                continueButton,
                characterButton,
                optionsButton,
                collectionButton,
                achievementsButton,
                statsButton,
                creditsButton,
                resetButton,
                quitButton,
                preservedArt);

            CreateEventSystem();
            SetLayerRecursively(canvas.gameObject, UiLayer);
            EditorUtility.SetDirty(controller);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("Rebuilt TitleScene static UI layout.", controller);
        }

        [MenuItem("CuteIssac/Title/Rebuild Title Collection Source Catalog")]
        public static void RebuildTitleCollectionSourceCatalog()
        {
            TitleCollectionSourceCatalog catalog = LoadOrCreateCollectionSourceCatalog();
            SerializedObject serializedObject = new(catalog);

            FillAssetReferenceList<BalanceConfig>(serializedObject, "balanceConfigs");
            FillAssetReferenceList<RunConfiguration>(serializedObject, "runConfigurations");
            FillAssetReferenceList<FloorConfig>(serializedObject, "floorConfigs");
            FillAssetReferenceList<EnemyPoolData>(serializedObject, "enemyPools");
            FillAssetReferenceList<DevelopmentDebugCatalog>(serializedObject, "debugCatalogs");
            FillAssetReferenceList<ItemData>(serializedObject, "itemDataAssets");
            FillAssetReferenceList<ActiveItemData>(serializedObject, "activeItemDataAssets");

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);

            TitleMenuController controller = Object.FindFirstObjectByType<TitleMenuController>(FindObjectsInactive.Include);
            if (controller != null)
            {
                SerializedObject controllerObject = new(controller);
                SetReference(controllerObject, "collectionSourceCatalog", catalog);
                controllerObject.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(controller);
            }

            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkAllScenesDirty();
            EditorSceneManager.SaveOpenScenes();
            Debug.Log("Rebuilt title collection source catalog.", catalog);
        }

        private static Dictionary<string, Object> PreserveTitleArtReferences()
        {
            Dictionary<string, Object> references = new();
            TitleMenuController controller = Object.FindFirstObjectByType<TitleMenuController>(FindObjectsInactive.Include);

            if (controller == null)
            {
                return references;
            }

            SerializedObject serializedObject = new(controller);
            PreserveObjectReference(serializedObject, references, "backgroundSprite");
            PreserveObjectReference(serializedObject, references, "titleLogoSprite");
            PreserveObjectReference(serializedObject, references, "keyArtSprite");
            PreserveObjectReference(serializedObject, references, "shellSprite");
            PreserveObjectReference(serializedObject, references, "panelSprite");
            PreserveObjectReference(serializedObject, references, "buttonSprite");
            PreserveObjectReference(serializedObject, references, "disabledButtonSprite");
            return references;
        }

        private static void PreserveObjectReference(SerializedObject source, Dictionary<string, Object> target, string propertyName)
        {
            SerializedProperty property = source.FindProperty(propertyName);

            if (property != null && property.objectReferenceValue != null)
            {
                target[propertyName] = property.objectReferenceValue;
            }
        }

        private static TitleMenuController CreateController(
            Canvas canvas,
            CanvasScaler canvasScaler,
            RectTransform shellRoot,
            RectTransform contentRoot,
            ScrollRect scrollRect,
            Text titleText,
            Image backdropImage,
            Image logoImage,
            Image keyArtImage,
            Image shellImage,
            Image primaryActionsImage,
            Image contentPanelImage,
            Image utilityBarImage,
            Button newRunButton,
            Button continueButton,
            Button characterButton,
            Button optionsButton,
            Button collectionButton,
            Button achievementsButton,
            Button statsButton,
            Button creditsButton,
            Button resetButton,
            Button quitButton,
            Dictionary<string, Object> preservedArt)
        {
            GameObject controllerObject = new("TitleMenuController", typeof(TitleMenuController));
            TitleMenuController controller = controllerObject.GetComponent<TitleMenuController>();
            SerializedObject serializedObject = new(controller);

            foreach (KeyValuePair<string, Object> entry in preservedArt)
            {
                SetReference(serializedObject, entry.Key, entry.Value);
            }

            SetReference(serializedObject, "titleCanvas", canvas);
            SetReference(serializedObject, "sceneCanvasScaler", canvasScaler);
            SetReference(serializedObject, "sceneShellRoot", shellRoot);
            SetReference(serializedObject, "sceneContentRoot", contentRoot);
            SetReference(serializedObject, "sceneContentScrollRect", scrollRect);
            SetReference(serializedObject, "sceneTitleText", titleText);
            SetReference(serializedObject, "backdropImage", backdropImage);
            SetReference(serializedObject, "titleLogoImage", logoImage);
            SetReference(serializedObject, "keyArtImage", keyArtImage);
            SetReference(serializedObject, "shellImage", shellImage);
            SetReference(serializedObject, "primaryActionsImage", primaryActionsImage);
            SetReference(serializedObject, "contentPanelImage", contentPanelImage);
            SetReference(serializedObject, "utilityBarImage", utilityBarImage);
            SetReference(serializedObject, "newRunButton", newRunButton);
            SetReference(serializedObject, "sceneContinueButton", continueButton);
            SetReference(serializedObject, "characterButton", characterButton);
            SetReference(serializedObject, "optionsButton", optionsButton);
            SetReference(serializedObject, "collectionButton", collectionButton);
            SetReference(serializedObject, "achievementsButton", achievementsButton);
            SetReference(serializedObject, "statsButton", statsButton);
            SetReference(serializedObject, "creditsButton", creditsButton);
            SetReference(serializedObject, "resetButton", resetButton);
            SetReference(serializedObject, "quitButton", quitButton);
            SetMenuButtonIds(
                serializedObject,
                newRunButton,
                continueButton,
                characterButton,
                optionsButton,
                collectionButton,
                achievementsButton,
                statsButton,
                creditsButton,
                resetButton,
                quitButton);

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            return controller;
        }

        private static void SetMenuButtonIds(SerializedObject serializedObject, params Button[] buttons)
        {
            SerializedProperty property = serializedObject.FindProperty("menuButtonIds");
            if (property == null)
            {
                return;
            }

            property.ClearArray();
            for (int index = 0; index < buttons.Length; index++)
            {
                TitleMenuButtonId buttonId = buttons[index] != null ? buttons[index].GetComponent<TitleMenuButtonId>() : null;
                if (buttonId == null)
                {
                    continue;
                }

                property.InsertArrayElementAtIndex(property.arraySize);
                property.GetArrayElementAtIndex(property.arraySize - 1).objectReferenceValue = buttonId;
            }
        }

        private static void AssignButtonAction(Button button, TitleMenuButtonAction action)
        {
            if (button == null)
            {
                return;
            }

            TitleMenuButtonId buttonId = button.GetComponent<TitleMenuButtonId>();
            if (buttonId == null)
            {
                buttonId = button.gameObject.AddComponent<TitleMenuButtonId>();
            }

            buttonId.Configure(action);
        }

        private static void SetReference(SerializedObject serializedObject, string propertyName, Object value)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);

            if (property != null)
            {
                property.objectReferenceValue = value;
            }
        }

        private static TitleCollectionSourceCatalog LoadOrCreateCollectionSourceCatalog()
        {
            const string catalogPath = "Assets/Resources/TitleCollectionSourceCatalog.asset";

            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            {
                AssetDatabase.CreateFolder("Assets", "Resources");
            }

            TitleCollectionSourceCatalog catalog = AssetDatabase.LoadAssetAtPath<TitleCollectionSourceCatalog>(catalogPath);
            if (catalog != null)
            {
                return catalog;
            }

            catalog = ScriptableObject.CreateInstance<TitleCollectionSourceCatalog>();
            AssetDatabase.CreateAsset(catalog, catalogPath);
            return catalog;
        }

        private static void FillAssetReferenceList<TAsset>(SerializedObject serializedObject, string propertyName)
            where TAsset : Object
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property == null)
            {
                return;
            }

            property.ClearArray();
            string[] guids = AssetDatabase.FindAssets($"t:{typeof(TAsset).Name}");
            System.Array.Sort(
                guids,
                (left, right) => string.Compare(
                    AssetDatabase.GUIDToAssetPath(left),
                    AssetDatabase.GUIDToAssetPath(right),
                    System.StringComparison.OrdinalIgnoreCase));

            int writeIndex = 0;
            for (int index = 0; index < guids.Length; index++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[index]);
                TAsset asset = AssetDatabase.LoadAssetAtPath<TAsset>(path);
                if (asset == null)
                {
                    continue;
                }

                property.InsertArrayElementAtIndex(writeIndex);
                property.GetArrayElementAtIndex(writeIndex).objectReferenceValue = asset;
                writeIndex++;
            }
        }

        private static Canvas CreateTitleCanvas(out CanvasScaler scaler)
        {
            GameObject canvasObject = new("TitleCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 5000;

            scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            return canvas;
        }

        private static void CreateEventSystem()
        {
            GameObject eventSystemObject = new("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            eventSystemObject.GetComponent<InputSystemUIInputModule>().enabled = true;
        }

        private static Button CreateButton(string label, Transform parent, float preferredHeight, int fontSize, float preferredWidth)
        {
            GameObject buttonObject = new(label, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(LayoutElement));
            buttonObject.transform.SetParent(parent, false);

            LayoutElement layoutElement = buttonObject.GetComponent<LayoutElement>();
            layoutElement.preferredHeight = Mathf.Max(34f, preferredHeight);
            layoutElement.minHeight = Mathf.Max(32f, preferredHeight - 8f);
            layoutElement.preferredWidth = Mathf.Max(80f, preferredWidth);

            Image image = buttonObject.GetComponent<Image>();
            image.color = new Color(0.27f, 0.32f, 0.36f, 0.96f);

            Button button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;

            Text text = CreateText("Label", buttonObject.transform, label, fontSize, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
            Stretch(text.rectTransform, Vector2.zero, Vector2.one);
            return button;
        }

        private static Image CreatePanel(string name, Transform parent, Color color, bool raycastTarget)
        {
            Image image = CreateImage(name, parent, color, raycastTarget);
            image.type = Image.Type.Simple;
            return image;
        }

        private static Image CreateImage(string name, Transform parent, Color color, bool raycastTarget)
        {
            GameObject imageObject = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            imageObject.transform.SetParent(parent, false);
            Image image = imageObject.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = raycastTarget;
            return image;
        }

        private static Text CreateText(string name, Transform parent, string value, int fontSize, FontStyle fontStyle, TextAnchor alignment, Color color)
        {
            GameObject textObject = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            textObject.transform.SetParent(parent, false);
            Text text = textObject.GetComponent<Text>();
            text.text = value;
            text.font = ResolveBuiltinFont();
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.alignment = alignment;
            text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            return text;
        }

        private static Font ResolveBuiltinFont()
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return font != null ? font : Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            GameObject rectObject = new(name, typeof(RectTransform));
            rectObject.transform.SetParent(parent, false);
            return rectObject.GetComponent<RectTransform>();
        }

        private static void Stretch(RectTransform rectTransform, Vector2 anchorMin, Vector2 anchorMax)
        {
            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }

        private static void DeleteRootIfExists(string name)
        {
            GameObject target = GameObject.Find(name);

            if (target != null && target.transform.parent == null)
            {
                Object.DestroyImmediate(target);
            }
        }

        private static void SetLayerRecursively(GameObject root, int layer)
        {
            root.layer = layer;

            for (int i = 0; i < root.transform.childCount; i++)
            {
                SetLayerRecursively(root.transform.GetChild(i).gameObject, layer);
            }
        }
    }
}
