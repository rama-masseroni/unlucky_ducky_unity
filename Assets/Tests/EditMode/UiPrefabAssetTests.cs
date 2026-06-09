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
            "pageLabel");
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
                checkedScenes.Add(path);
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        Assert.AreEqual(20, checkedScenes.Count);
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
}
