using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UiPrefabAssetTests
{
    private const string GameplayCanvasPath = "Assets/Prefabs/UI/UI_GameplayCanvas.prefab";
    private const string MainMenuCanvasPath = "Assets/Prefabs/UI/UI_MainMenuCanvas.prefab";
    private const string LevelSelectPanelPath = "Assets/Prefabs/UI/Main Menu/UI_LevelSelectPanel.prefab";
    private const string World01InventoryPanelPath =
        "Assets/Prefabs/UI/World Inventories/World 01/UI_InventoryPanel_World01.prefab";
    private const string World01InventoryItemPath =
        "Assets/Prefabs/UI/World Inventories/World 01/UI_InventoryItem_World01.prefab";
    private const string World01ExecuteButtonPath =
        "Assets/Prefabs/UI/World Inventories/World 01/UI_ExecuteButton_World01.prefab";

    [Test]
    public void GameplayCanvas_ContainsAllAuthoredGameplayUi()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(GameplayCanvasPath);

        Assert.IsNotNull(prefab);
        Assert.AreEqual(1, prefab.GetComponentsInChildren<Canvas>(true).Length);
        Assert.IsNotNull(FindComponent(prefab, "LevelHudPanel"));
        Assert.IsNotNull(FindComponent(prefab, "PlaceableInventoryPanel"));
        Assert.IsNotNull(FindComponent(prefab, "PauseMenuManager"));
        Assert.IsNotNull(FindComponent(prefab, "VictoryScreenManager"));
        Assert.IsNotNull(FindComponent(prefab, "DefeatScreenManager"));
    }

    [Test]
    public void MainMenuCanvas_IsSeparateFromGameplayUi()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(MainMenuCanvasPath);

        Assert.IsNotNull(prefab);
        Assert.IsNotNull(FindComponent(prefab, "MainMenuNavigationController"));
        Assert.IsNotNull(FindComponent(prefab, "LevelSelectController"));
        Assert.IsNull(FindComponent(prefab, "PlaceableInventoryPanel"));
        Assert.IsNull(FindComponent(prefab, "PauseMenuManager"));
        Assert.IsNull(FindComponent(prefab, "VictoryScreenManager"));
        Assert.IsNull(FindComponent(prefab, "DefeatScreenManager"));
    }

    [Test]
    public void LevelSelectBackground_CoversTheWholePanelBehindTheControls()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(LevelSelectPanelPath);
        Assert.IsNotNull(prefab);

        Component controller = FindComponent(prefab, "LevelSelectController");
        Assert.IsNotNull(controller);

        Image background = GetPrivateField<Image>(controller, "selectorBackground");
        Assert.IsNotNull(background);

        RectTransform rect = background.rectTransform;
        Assert.AreEqual(prefab.transform, rect.parent);
        Assert.AreEqual(0, rect.GetSiblingIndex());
        Assert.AreEqual(Vector2.zero, rect.anchorMin);
        Assert.AreEqual(Vector2.one, rect.anchorMax);
        Assert.AreEqual(Vector2.zero, rect.offsetMin);
        Assert.AreEqual(Vector2.zero, rect.offsetMax);
        Assert.IsFalse(background.raycastTarget);
    }

    [Test]
    public void WorldInventories_UseSeparateAuthoredPrefabs()
    {
        for (int world = 1; world <= 4; world++)
        {
            string worldId = world.ToString("00");
            string basePath = $"Assets/Prefabs/UI/World Inventories/World {worldId}";
            string panelPath = $"{basePath}/UI_InventoryPanel_World{worldId}.prefab";
            string itemPath = $"{basePath}/UI_InventoryItem_World{worldId}.prefab";
            string executePath = $"{basePath}/UI_ExecuteButton_World{worldId}.prefab";
            string canvasPath = $"{basePath}/UI_GameplayCanvas_World{worldId}.prefab";

            GameObject panelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(panelPath);
            GameObject itemPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(itemPath);
            GameObject executePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(executePath);
            GameObject canvasPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(canvasPath);

            Assert.IsNotNull(panelPrefab, panelPath);
            Assert.IsNotNull(itemPrefab, itemPath);
            Assert.IsNotNull(executePrefab, executePath);
            Assert.IsNotNull(canvasPrefab, canvasPath);

            Component panel = FindComponent(panelPrefab, "PlaceableInventoryPanel");
            Component item = FindComponent(itemPrefab, "PlaceableInventorySlotView");
            Component execute = FindComponent(executePrefab, "StartExecutionButtonController");

            Assert.IsNotNull(panel, panelPath);
            Assert.IsNotNull(item, itemPath);
            Assert.IsNotNull(execute, executePath);
            Assert.IsNotNull(FindComponent(canvasPrefab, "PlaceableInventoryPanel"), canvasPath);
            Assert.AreSame(item, GetPrivateField<Component>(panel, "slotPrefab"), panelPath);

            Component nestedExecute = GetPrivateField<Component>(panel, "startExecutionButton");
            Assert.IsNotNull(nestedExecute, panelPath);

            Object executeSource = PrefabUtility.GetCorrespondingObjectFromSource(nestedExecute);
            Assert.IsNotNull(executeSource, panelPath);
            Assert.AreEqual(executePath, AssetDatabase.GetAssetPath(executeSource), panelPath);
        }
    }

    [Test]
    public void SlotPrefabs_ExposeAuthoredViews()
    {
        GameObject inventorySlot = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Prefabs/UI/UI_PlaceableInventorySlot.prefab");
        GameObject levelSlot = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Prefabs/UI/UI_LevelSelectSlot.prefab");

        Assert.IsNotNull(inventorySlot);
        Assert.IsNotNull(FindComponent(inventorySlot, "PlaceableInventorySlotView"));
        Assert.IsNotNull(inventorySlot.GetComponent<Button>());
        Assert.IsNotNull(levelSlot);
        Assert.IsNotNull(FindComponent(levelSlot, "LevelSelectSlotView"));
        Assert.IsNotNull(levelSlot.GetComponent<Button>());
    }

    [Test]
    public void WorldDefinitions_HaveLevelSelectorAssetsOnly()
    {
        string[] worldPaths =
        {
            "Assets/ScriptableObjects/World Definitions/World_01.asset",
            "Assets/ScriptableObjects/World Definitions/World_02.asset",
            "Assets/ScriptableObjects/World Definitions/World_03.asset",
            "Assets/ScriptableObjects/World Definitions/World_04.asset"
        };

        for (int i = 0; i < worldPaths.Length; i++)
        {
            ScriptableObject world = AssetDatabase.LoadAssetAtPath<ScriptableObject>(worldPaths[i]);
            Assert.IsNotNull(world, worldPaths[i]);

            SerializedProperty selectorAssets = new SerializedObject(world).FindProperty("levelSelectorAssets");
            Assert.IsNotNull(selectorAssets, worldPaths[i]);
            Assert.IsNotNull(selectorAssets.objectReferenceValue, worldPaths[i]);

            SerializedProperty inventoryAssets = new SerializedObject(world).FindProperty("inventoryUiAssets");
            Assert.IsNull(inventoryAssets, worldPaths[i]);
        }
    }

    [Test]
    public void Menus_UseAuthoredButtonSprites()
    {
        GameObject pauseMenu = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Prefabs/UI/PausedMenu.prefab");
        Component pauseManager = FindComponent(pauseMenu, "PauseMenuManager");
        GameObject mainMenu = AssetDatabase.LoadAssetAtPath<GameObject>(MainMenuCanvasPath);
        Component navigation = FindComponent(mainMenu, "MainMenuNavigationController");

        Assert.IsNotNull(pauseMenu);
        Assert.IsNotNull(pauseManager);
        Assert.IsNotNull(mainMenu);
        Assert.IsNotNull(navigation);

        AssertButtonUsesSprite(
            GetPrivateField<Button>(pauseManager, "resumeButton"),
            "Assets/Sprites/UX/Botones del Menu de pausa/Continuar.png");
        AssertButtonUsesSprite(
            GetPrivateField<Button>(pauseManager, "resetButton"),
            "Assets/Sprites/UX/Botones del Menu de pausa/Reiniciar nivel.png");
        AssertButtonUsesSprite(
            GetPrivateField<Button>(pauseManager, "optionsButton"),
            "Assets/Sprites/UX/Botones del Menu de pausa/Opciones.png");
        AssertButtonUsesSprite(
            GetPrivateField<Button>(pauseManager, "mainMenuButton"),
            "Assets/Sprites/UX/Botones del Menu de pausa/Volver al Menu.png");
        AssertButtonUsesSprite(
            GetPrivateField<Button>(navigation, "optionsButton"),
            "Assets/Sprites/UX/Botones del Menu de pausa/Opciones.png");
        AssertButtonUsesSprite(
            GetPrivateField<Button>(navigation, "creditsButton"),
            "Assets/Sprites/UX/Botones del Menu de pausa/Creditos.png");
    }

    [Test]
    public void AuthoredUi_HasRequiredSerializedReferences()
    {
        GameObject gameplay = AssetDatabase.LoadAssetAtPath<GameObject>(GameplayCanvasPath);
        GameObject mainMenu = AssetDatabase.LoadAssetAtPath<GameObject>(MainMenuCanvasPath);
        GameObject inventorySlot = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Prefabs/UI/UI_PlaceableInventorySlot.prefab");
        GameObject levelSlot = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Prefabs/UI/UI_LevelSelectSlot.prefab");

        AssertAssigned(
            gameplay,
            "LevelHudPanel",
            "pauseMenuManager",
            "levelTitleText",
            "phaseIndicatorText",
            "planningTimerText",
            "tooltipRoot",
            "tooltipText",
            "pauseButton",
            "resetLevelButton");
        AssertAssigned(
            gameplay,
            "PlaceableInventoryPanel",
            "slotsRoot",
            "slotsLayout",
            "slotPrefab",
            "startExecutionButton",
            "panelRectTransform",
            "panelLayout");
        AssertAssigned(
            gameplay,
            "PauseMenuManager",
            "container",
            "pauseActions",
            "optionsPanel",
            "resumeButton",
            "resetButton",
            "optionsButton",
            "mainMenuButton",
            "optionsBackButton");
        AssertAssigned(gameplay, "VictoryScreenManager", "container", "continueButton", "retryButton", "mainMenuButton");
        AssertAssigned(gameplay, "DefeatScreenManager", "container", "subtitleText", "retryButton", "mainMenuButton");
        AssertAssigned(
            mainMenu,
            "MainMenuNavigationController",
            "splashPanel",
            "mainMenuPanel",
            "levelSelectPanel",
            "optionsPanel",
            "creditsPanel",
            "playButton",
            "optionsButton",
            "creditsButton",
            "exitButton",
            "levelSelectBackButton",
            "optionsBackButton",
            "creditsBackButton");
        AssertAssigned(
            mainMenu,
            "LevelSelectController",
            "catalog",
            "slots",
            "previousPageButton",
            "nextPageButton",
            "pageLabel",
            "selectorBackground",
            "previousPageImage",
            "nextPageImage",
            "backButtonImage",
            "titleLabel");
        AssertAssigned(
            inventorySlot,
            "PlaceableInventorySlotView",
            "background",
            "button",
            "slotLayout",
            "contentLayout",
            "icon",
            "iconLayout",
            "textBlockLayout",
            "textBlock",
            "nameText",
            "nameLayout",
            "amountText",
            "amountLayout");
        AssertAssigned(levelSlot, "LevelSelectSlotView", "background", "button", "label");
    }

    [Test]
    public void MigratedScenes_HaveExactlyOneUiCompositionAndEventSystem()
    {
        string[] guids = AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Scenes" });
        List<string> checkedScenes = new List<string>();

        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);

            if (path.Contains("/Settings/") || path.Contains("SceneTemplate"))
            {
                continue;
            }

            Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);

            try
            {
                bool mainMenu = path.EndsWith("MainMenuScene.unity");
                int canvasCount = CountComponentsInScene<Canvas>(scene);
                int eventSystemCount = CountComponentsInScene<EventSystem>(scene);
                Assert.AreEqual(1, canvasCount, path);
                Assert.AreEqual(1, eventSystemCount, path);
                Assert.AreEqual(mainMenu ? 1 : 0, CountNamedRoots(scene, "UI_MainMenuCanvas"), path);
                Assert.AreEqual(mainMenu ? 0 : 1, CountNamedRoots(scene, "UI_GameplayCanvas"), path);

                if (!mainMenu)
                {
                    AssertGameplayCanvasSource(scene, path);
                }

                checkedScenes.Add(path);
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        Assert.AreEqual(20, checkedScenes.Count);
    }

    private static void AssertButtonUsesSprite(Button button, string expectedPath)
    {
        Assert.IsNotNull(button);
        Assert.IsNotNull(button.image);
        Assert.IsNotNull(button.image.sprite);
        Assert.AreEqual(expectedPath, AssetDatabase.GetAssetPath(button.image.sprite));
        Assert.Greater(button.transform.childCount, 0);
        Assert.IsFalse(button.transform.GetChild(0).gameObject.activeSelf);
    }

    private static void AssertGameplayCanvasSource(Scene scene, string scenePath)
    {
        int worldIndex = scenePath.IndexOf("/World ", System.StringComparison.Ordinal);

        if (worldIndex < 0)
        {
            return;
        }

        string worldText = scenePath.Substring(worldIndex + "/World ".Length, 1);
        string worldId = int.Parse(worldText).ToString("00");
        string expectedPath =
            $"Assets/Prefabs/UI/World Inventories/World {worldId}/UI_GameplayCanvas_World{worldId}.prefab";
        GameObject canvasRoot = null;
        GameObject[] roots = scene.GetRootGameObjects();

        for (int i = 0; i < roots.Length; i++)
        {
            if (roots[i].name == "UI_GameplayCanvas")
            {
                canvasRoot = roots[i];
                break;
            }
        }

        Assert.IsNotNull(canvasRoot, scenePath);
        Object source = PrefabUtility.GetCorrespondingObjectFromSource(canvasRoot);
        Assert.IsNotNull(source, scenePath);
        Assert.AreEqual(expectedPath, AssetDatabase.GetAssetPath(source), scenePath);
    }

    private static int CountComponentsInScene<T>(Scene scene) where T : Component
    {
        int count = 0;
        GameObject[] roots = scene.GetRootGameObjects();

        for (int i = 0; i < roots.Length; i++)
        {
            count += roots[i].GetComponentsInChildren<T>(true).Length;
        }

        return count;
    }

    private static int CountNamedRoots(Scene scene, string name)
    {
        int count = 0;
        GameObject[] roots = scene.GetRootGameObjects();

        for (int i = 0; i < roots.Length; i++)
        {
            if (roots[i].name == name)
            {
                count++;
            }
        }

        return count;
    }

    private static Component FindComponent(GameObject root, string typeName)
    {
        System.Type type = System.Type.GetType($"{typeName}, Assembly-CSharp");
        Assert.IsNotNull(type, $"Could not resolve {typeName}.");

        Component[] components = root.GetComponentsInChildren<Component>(true);

        for (int i = 0; i < components.Length; i++)
        {
            if (components[i] != null && type.IsInstanceOfType(components[i]))
            {
                return components[i];
            }
        }

        return null;
    }

    private static void AssertAssigned(GameObject root, string typeName, params string[] fieldNames)
    {
        Component component = FindComponent(root, typeName);
        Assert.IsNotNull(component, $"{typeName} is missing from {root.name}.");

        for (int i = 0; i < fieldNames.Length; i++)
        {
            System.Reflection.FieldInfo field = component.GetType().GetField(
                fieldNames[i],
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"{typeName}.{fieldNames[i]} does not exist.");
            object value = field.GetValue(component);
            Assert.IsNotNull(value, $"{typeName}.{fieldNames[i]} is not assigned.");

            if (value is System.Array array)
            {
                Assert.Greater(array.Length, 0, $"{typeName}.{fieldNames[i]} is empty.");

                for (int j = 0; j < array.Length; j++)
                {
                    Assert.IsNotNull(array.GetValue(j), $"{typeName}.{fieldNames[i]}[{j}] is not assigned.");
                }
            }
        }
    }

    private static T GetPrivateField<T>(Component component, string fieldName) where T : class
    {
        System.Reflection.FieldInfo field = component.GetType().GetField(
            fieldName,
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        Assert.IsNotNull(field, $"{component.GetType().Name}.{fieldName} does not exist.");
        return field.GetValue(component) as T;
    }
}
