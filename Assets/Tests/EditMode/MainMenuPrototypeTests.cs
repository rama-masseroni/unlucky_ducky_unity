using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuPrototypeTests
{
    private readonly Type levelCatalogType = Type.GetType("LevelCatalog, Assembly-CSharp");
    private readonly Type levelCatalogEntryType = Type.GetType("LevelCatalogEntry, Assembly-CSharp");
    private readonly Type levelSelectControllerType = Type.GetType("LevelSelectController, Assembly-CSharp");
    private readonly Type levelSelectSlotViewType = Type.GetType("LevelSelectSlotView, Assembly-CSharp");
    private readonly Type levelProgressServiceType = Type.GetType("LevelProgressService, Assembly-CSharp");
    private readonly Type inMemoryLevelProgressStoreType = Type.GetType("InMemoryLevelProgressStore, Assembly-CSharp");
    private readonly Type levelDefinitionType = Type.GetType("LevelDefinition, UnluckyDucky.Core");
    private readonly Type worldLevelSelectorAssetsType = Type.GetType("WorldLevelSelectorAssets, UnluckyDucky.Core");
    private readonly Type levelSelectorSpriteSetType = Type.GetType("LevelSelectorSpriteSet, UnluckyDucky.Core");
    private readonly Type mainMenuNavigationControllerType = Type.GetType("MainMenuNavigationController, Assembly-CSharp");
    private readonly Type mainMenuPanelKindType = Type.GetType("MainMenuPanelKind, Assembly-CSharp");

    private readonly List<UnityEngine.Object> createdObjects = new List<UnityEngine.Object>();

    [SetUp]
    public void SetUp()
    {
        Assert.IsNotNull(inMemoryLevelProgressStoreType);
        object store = Activator.CreateInstance(inMemoryLevelProgressStoreType);
        InvokeStatic(levelProgressServiceType, "SetStoreForTests", store);
    }

    [TearDown]
    public void TearDown()
    {
        InvokeStatic(levelProgressServiceType, "RestoreDefaultStore");

        for (int i = 0; i < createdObjects.Count; i++)
        {
            if (createdObjects[i] != null)
            {
                UnityEngine.Object.DestroyImmediate(createdObjects[i]);
            }
        }

        createdObjects.Clear();
    }

    [Test]
    public void LevelCatalogEntry_WithEmptySceneName_IsNotPlayable()
    {
        Assert.IsNotNull(levelCatalogEntryType);
        object entry = Activator.CreateInstance(levelCatalogEntryType);
        SetPrivateField(entry, "sceneName", string.Empty);
        SetPrivateField(entry, "unlockedByDefault", true);

        Assert.IsFalse((bool)GetProperty(entry, "HasSceneName"));
        Assert.IsFalse((bool)GetProperty(entry, "IsPlayable"));
    }

    [Test]
    public void LevelSelectController_Rebuild_CreatesOnePageWithFiveSlots()
    {
        Assert.IsNotNull(levelCatalogType);
        Assert.IsNotNull(levelSelectControllerType);

        ScriptableObject catalog = CreateCatalog(
            ("Scene_01_01", "Mundo 1", 1),
            ("Scene_01_02", "Mundo 1", 2),
            ("Scene_01_03", "Mundo 1", 3),
            ("Scene_01_04", "Mundo 1", 4),
            ("Scene_01_05", "Mundo 1", 5),
            ("Scene_02_01", "Mundo 2", 6),
            ("Scene_02_02", "Mundo 2", 7));
        GameObject controllerObject = CreateGameObject("LevelSelectController");
        Component controller = controllerObject.AddComponent(levelSelectControllerType);
        Array slots = CreateLevelSlots();

        Invoke(controller, "Configure", catalog, slots, null, null, null);

        IReadOnlyList<Button> createdButtons = (IReadOnlyList<Button>)GetProperty(controller, "CreatedLevelButtons");
        Assert.AreEqual(5, createdButtons.Count);
        Assert.AreEqual(5, slots.Length);
        Assert.AreEqual(2, (int)GetProperty(controller, "TotalPages"));

        Invoke(controller, "ShowNextPage");

        Assert.AreEqual(2, ((IReadOnlyList<Button>)GetProperty(controller, "CreatedLevelButtons")).Count);
        Assert.AreEqual(5, slots.Length);
        Assert.AreEqual(1, (int)GetProperty(controller, "CurrentPageIndex"));
    }

    [Test]
    public void LevelSelectController_Rebuild_KeepsWorldsOnSeparatePages()
    {
        Assert.IsNotNull(levelCatalogType);
        Assert.IsNotNull(levelSelectControllerType);

        ScriptableObject catalog = CreateCatalog(
            ("Scene_01_01", "Mundo 1", 1),
            ("Scene_01_02", "Mundo 1", 2),
            ("Scene_01_03", "Mundo 1", 3),
            ("Scene_01_04", "Mundo 1", 4),
            ("Scene_02_01", "Mundo 2", 5),
            ("Scene_02_02", "Mundo 2", 6));
        GameObject controllerObject = CreateGameObject("LevelSelectController");
        Component controller = controllerObject.AddComponent(levelSelectControllerType);
        Array slots = CreateLevelSlots();

        Invoke(controller, "Configure", catalog, slots, null, null, null);

        Assert.AreEqual(4, ((IReadOnlyList<Button>)GetProperty(controller, "CreatedLevelButtons")).Count);
        Assert.AreEqual(5, slots.Length);
        Assert.AreEqual(2, (int)GetProperty(controller, "TotalPages"));

        Invoke(controller, "ShowNextPage");

        Assert.AreEqual(2, ((IReadOnlyList<Button>)GetProperty(controller, "CreatedLevelButtons")).Count);
        Assert.AreEqual(5, slots.Length);
    }

    [Test]
    public void LevelProgressService_NewGame_OnlyUnlocksDefaultEntry()
    {
        ScriptableObject catalog = CreateProgressionCatalog(
            ("Level_01_01", "Scene_01_01", 1),
            ("Level_01_02", "Scene_01_02", 2));
        IList orderedEntries = (IList)InvokeWithResult(catalog, "GetOrderedEntries");

        Assert.IsTrue((bool)InvokeStaticWithResult(
            levelProgressServiceType,
            "IsUnlocked",
            orderedEntries[0],
            orderedEntries));
        Assert.IsFalse((bool)InvokeStaticWithResult(
            levelProgressServiceType,
            "IsUnlocked",
            orderedEntries[1],
            orderedEntries));
    }

    [Test]
    public void LevelProgressService_CompletionUnlocksOnlyNextCatalogEntry()
    {
        ScriptableObject catalog = CreateProgressionCatalog(
            ("Level_01_01", "Scene_01_01", 1),
            ("Level_01_02", "Scene_01_02", 2),
            ("Level_02_01", "Scene_02_01", 6));
        IList orderedEntries = (IList)InvokeWithResult(catalog, "GetOrderedEntries");

        Assert.IsTrue((bool)InvokeStaticWithResult(
            levelProgressServiceType,
            "MarkCompleted",
            "Level_01_01"));
        Assert.IsTrue((bool)InvokeStaticWithResult(
            levelProgressServiceType,
            "IsUnlocked",
            orderedEntries[1],
            orderedEntries));
        Assert.IsFalse((bool)InvokeStaticWithResult(
            levelProgressServiceType,
            "IsUnlocked",
            orderedEntries[2],
            orderedEntries));

        Assert.IsTrue((bool)InvokeStaticWithResult(
            levelProgressServiceType,
            "MarkCompleted",
            "Level_01_02"));
        Assert.IsTrue((bool)InvokeStaticWithResult(
            levelProgressServiceType,
            "IsUnlocked",
            orderedEntries[2],
            orderedEntries));
        Assert.IsFalse((bool)InvokeStaticWithResult(
            levelProgressServiceType,
            "MarkCompleted",
            "Level_01_02"));
    }

    [Test]
    public void LevelProgressService_CorruptSaveFallsBackToNewGame()
    {
        object store = Activator.CreateInstance(inMemoryLevelProgressStoreType);
        Invoke(store, "SetString", "UnluckyDucky.LevelProgress", "{not-valid-json");
        InvokeStatic(levelProgressServiceType, "SetStoreForTests", store);
        ScriptableObject catalog = CreateProgressionCatalog(
            ("Level_01_01", "Scene_01_01", 1),
            ("Level_01_02", "Scene_01_02", 2));
        IList orderedEntries = (IList)InvokeWithResult(catalog, "GetOrderedEntries");

        Assert.IsTrue((bool)InvokeStaticWithResult(
            levelProgressServiceType,
            "IsUnlocked",
            orderedEntries[0],
            orderedEntries));
        Assert.IsFalse((bool)InvokeStaticWithResult(
            levelProgressServiceType,
            "IsUnlocked",
            orderedEntries[1],
            orderedEntries));
    }

    [Test]
    public void LevelSelectController_LoadLevel_RejectsLockedEntry()
    {
        ScriptableObject catalog = CreateProgressionCatalog(
            ("Level_01_01", "Scene_01_01", 1),
            ("Level_01_02", "Scene_01_02", 2));
        GameObject controllerObject = CreateGameObject("LevelSelectController");
        Component controller = controllerObject.AddComponent(levelSelectControllerType);
        Array slots = CreateLevelSlots();
        string requestedScene = null;
        levelSelectControllerType
            .GetProperty("SceneLoadOverride")
            .SetValue(null, new Action<string>(sceneName => requestedScene = sceneName));

        try
        {
            Invoke(controller, "Configure", catalog, slots, null, null, null);
            IList orderedEntries = (IList)InvokeWithResult(catalog, "GetOrderedEntries");
            Invoke(controller, "LoadLevel", orderedEntries[1]);

            Assert.IsNull(requestedScene);

            InvokeStatic(levelProgressServiceType, "MarkCompleted", "Level_01_01");
            Invoke(controller, "LoadLevel", orderedEntries[1]);

            Assert.AreEqual("Scene_01_02", requestedScene);
        }
        finally
        {
            levelSelectControllerType.GetProperty("SceneLoadOverride").SetValue(null, null);
        }
    }

    [Test]
    public void WorldLevelSelectorAssets_ReturnsNormalAndLockedSpritesByDisplayOrder()
    {
        Assert.IsNotNull(worldLevelSelectorAssetsType);
        Assert.IsNotNull(levelSelectorSpriteSetType);
        ScriptableObject selectorAssets = ScriptableObject.CreateInstance(worldLevelSelectorAssetsType);
        object spriteSet = Activator.CreateInstance(levelSelectorSpriteSetType);
        Sprite normal = CreateSprite();
        Sprite locked = CreateSprite();
        createdObjects.Add(selectorAssets);

        SetPrivateField(spriteSet, "displayOrder", 8);
        SetPrivateField(spriteSet, "normal", normal);
        SetPrivateField(spriteSet, "locked", locked);
        IList levelSprites = (IList)worldLevelSelectorAssetsType
            .GetField("levelSprites", BindingFlags.NonPublic | BindingFlags.Instance)
            .GetValue(selectorAssets);
        levelSprites.Add(spriteSet);

        Assert.AreSame(normal, InvokeWithResult(selectorAssets, "GetLevelSprite", 8, false));
        Assert.AreSame(locked, InvokeWithResult(selectorAssets, "GetLevelSprite", 8, true));
        Assert.IsTrue((bool)InvokeWithResult(selectorAssets, "HasLockedLevelSprite", 8));
        Assert.IsNull(InvokeWithResult(selectorAssets, "GetLevelSprite", 9, false));
    }

    [Test]
    public void LevelSelectSlotView_LockedWithoutDedicatedSprite_UsesNormalSpriteAndFallbackTint()
    {
        Assert.IsNotNull(worldLevelSelectorAssetsType);
        Assert.IsNotNull(levelSelectorSpriteSetType);
        ScriptableObject selectorAssets = ScriptableObject.CreateInstance(worldLevelSelectorAssetsType);
        object spriteSet = Activator.CreateInstance(levelSelectorSpriteSetType);
        Sprite normal = CreateSprite();
        createdObjects.Add(selectorAssets);
        SetPrivateField(spriteSet, "displayOrder", 6);
        SetPrivateField(spriteSet, "normal", normal);
        IList levelSprites = (IList)worldLevelSelectorAssetsType
            .GetField("levelSprites", BindingFlags.NonPublic | BindingFlags.Instance)
            .GetValue(selectorAssets);
        levelSprites.Add(spriteSet);

        object entry = Activator.CreateInstance(levelCatalogEntryType);
        SetPrivateField(entry, "sceneName", "Scene_02_01");
        SetPrivateField(entry, "displayOrder", 6);
        SetPrivateField(entry, "unlockedByDefault", false);

        GameObject slotObject = CreateGameObject(
            "LevelSlot",
            typeof(RectTransform),
            typeof(Image),
            typeof(Button));
        Component slot = slotObject.AddComponent(levelSelectSlotViewType);
        SetPrivateField(slot, "background", slotObject.GetComponent<Image>());
        SetPrivateField(slot, "button", slotObject.GetComponent<Button>());

        Invoke(slot, "Bind", entry, 1, selectorAssets, null);

        Assert.AreSame(normal, slotObject.GetComponent<Image>().sprite);
        Assert.AreEqual(GetProperty(selectorAssets, "LockedFallbackTint"), slotObject.GetComponent<Image>().color);
        Assert.AreEqual(Selectable.Transition.None, slotObject.GetComponent<Button>().transition);
        Assert.IsFalse(slotObject.GetComponent<Button>().interactable);
    }

    [Test]
    public void MainMenuNavigationController_ShowPanel_ActivatesOnlySelectedPanel()
    {
        Assert.IsNotNull(mainMenuNavigationControllerType);
        Assert.IsNotNull(mainMenuPanelKindType);

        GameObject controllerObject = CreateGameObject("NavigationController");
        Component navigation = controllerObject.AddComponent(mainMenuNavigationControllerType);
        GameObject splash = CreateGameObject("SplashPanel");
        GameObject main = CreateGameObject("MainMenuPanel");
        GameObject levelSelect = CreateGameObject("LevelSelectPanel");
        GameObject options = CreateGameObject("OptionsPanel");
        GameObject credits = CreateGameObject("CreditsPanel");

        Invoke(navigation, "ConfigurePanels", splash, main, levelSelect, options, credits);

        object optionsPanelKind = Enum.Parse(mainMenuPanelKindType, "Options");
        Invoke(navigation, "ShowPanel", optionsPanelKind);

        Assert.IsFalse(splash.activeSelf);
        Assert.IsFalse(main.activeSelf);
        Assert.IsFalse(levelSelect.activeSelf);
        Assert.IsTrue(options.activeSelf);
        Assert.IsFalse(credits.activeSelf);
    }

    private ScriptableObject CreateCatalog(params (string sceneName, string worldLabel, int displayOrder)[] entries)
    {
        ScriptableObject catalog = ScriptableObject.CreateInstance(levelCatalogType);
        createdObjects.Add(catalog);
        IList catalogEntries = (IList)levelCatalogType
            .GetField("entries", BindingFlags.NonPublic | BindingFlags.Instance)
            .GetValue(catalog);

        for (int i = 0; i < entries.Length; i++)
        {
            object entry = Activator.CreateInstance(levelCatalogEntryType);
            SetPrivateField(entry, "sceneName", entries[i].sceneName);
            SetPrivateField(entry, "worldLabel", entries[i].worldLabel);
            SetPrivateField(entry, "displayOrder", entries[i].displayOrder);
            SetPrivateField(entry, "unlockedByDefault", true);
            catalogEntries.Add(entry);
        }

        return catalog;
    }

    private ScriptableObject CreateProgressionCatalog(
        params (string levelId, string sceneName, int displayOrder)[] entries)
    {
        Assert.IsNotNull(levelDefinitionType);
        ScriptableObject catalog = ScriptableObject.CreateInstance(levelCatalogType);
        createdObjects.Add(catalog);
        IList catalogEntries = (IList)levelCatalogType
            .GetField("entries", BindingFlags.NonPublic | BindingFlags.Instance)
            .GetValue(catalog);

        for (int i = 0; i < entries.Length; i++)
        {
            ScriptableObject levelDefinition = ScriptableObject.CreateInstance(levelDefinitionType);
            createdObjects.Add(levelDefinition);
            SetPrivateField(levelDefinition, "levelId", entries[i].levelId);

            object entry = Activator.CreateInstance(levelCatalogEntryType);
            SetPrivateField(entry, "levelDefinition", levelDefinition);
            SetPrivateField(entry, "sceneName", entries[i].sceneName);
            SetPrivateField(entry, "worldLabel", i < 2 ? "Mundo 1" : "Mundo 2");
            SetPrivateField(entry, "displayOrder", entries[i].displayOrder);
            SetPrivateField(entry, "unlockedByDefault", i == 0);
            catalogEntries.Add(entry);
        }

        return catalog;
    }

    private Array CreateLevelSlots()
    {
        Assert.IsNotNull(levelSelectSlotViewType);
        Array slots = Array.CreateInstance(levelSelectSlotViewType, 5);

        for (int i = 0; i < slots.Length; i++)
        {
            GameObject slotObject = CreateGameObject(
                $"LevelSlot{i + 1}",
                typeof(RectTransform),
                typeof(Image),
                typeof(Button));
            Component slot = slotObject.AddComponent(levelSelectSlotViewType);
            SetPrivateField(slot, "background", slotObject.GetComponent<Image>());
            SetPrivateField(slot, "button", slotObject.GetComponent<Button>());
            slots.SetValue(slot, i);
        }

        return slots;
    }

    private GameObject CreateGameObject(string name, params Type[] components)
    {
        GameObject gameObject = new GameObject(name, components);
        createdObjects.Add(gameObject);
        return gameObject;
    }

    private Sprite CreateSprite()
    {
        Texture2D texture = new Texture2D(2, 2);
        Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, 2f, 2f), Vector2.one * 0.5f);
        createdObjects.Add(sprite);
        createdObjects.Add(texture);
        return sprite;
    }

    private static void Invoke(object target, string methodName, params object[] parameters)
    {
        MethodInfo method = FindMethod(target.GetType(), methodName, parameters);
        Assert.IsNotNull(method, $"Could not find method {methodName} on {target.GetType().Name}.");
        method.Invoke(target, parameters);
    }

    private static object InvokeWithResult(object target, string methodName, params object[] parameters)
    {
        MethodInfo method = FindMethod(target.GetType(), methodName, parameters);
        Assert.IsNotNull(method, $"Could not find method {methodName} on {target.GetType().Name}.");
        return method.Invoke(target, parameters);
    }

    private static void InvokeStatic(Type targetType, string methodName, params object[] parameters)
    {
        MethodInfo method = FindStaticMethod(targetType, methodName, parameters);
        Assert.IsNotNull(method, $"Could not find static method {methodName} on {targetType?.Name}.");
        method.Invoke(null, parameters);
    }

    private static object InvokeStaticWithResult(Type targetType, string methodName, params object[] parameters)
    {
        MethodInfo method = FindStaticMethod(targetType, methodName, parameters);
        Assert.IsNotNull(method, $"Could not find static method {methodName} on {targetType?.Name}.");
        return method.Invoke(null, parameters);
    }

    private static MethodInfo FindMethod(Type targetType, string methodName, object[] parameters)
    {
        MethodInfo[] methods = targetType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        for (int i = 0; i < methods.Length; i++)
        {
            if (methods[i].Name != methodName)
            {
                continue;
            }

            ParameterInfo[] methodParameters = methods[i].GetParameters();

            if (methodParameters.Length != parameters.Length)
            {
                continue;
            }

            bool matches = true;

            for (int j = 0; j < methodParameters.Length; j++)
            {
                if (parameters[j] != null && !methodParameters[j].ParameterType.IsInstanceOfType(parameters[j]))
                {
                    matches = false;
                    break;
                }
            }

            if (matches)
            {
                return methods[i];
            }
        }

        return null;
    }

    private static MethodInfo FindStaticMethod(Type targetType, string methodName, object[] parameters)
    {
        if (targetType == null)
        {
            return null;
        }

        MethodInfo[] methods = targetType.GetMethods(BindingFlags.Public | BindingFlags.Static);

        for (int i = 0; i < methods.Length; i++)
        {
            if (methods[i].Name != methodName)
            {
                continue;
            }

            ParameterInfo[] methodParameters = methods[i].GetParameters();

            if (methodParameters.Length != parameters.Length)
            {
                continue;
            }

            bool matches = true;

            for (int j = 0; j < methodParameters.Length; j++)
            {
                if (parameters[j] != null && !methodParameters[j].ParameterType.IsInstanceOfType(parameters[j]))
                {
                    matches = false;
                    break;
                }
            }

            if (matches)
            {
                return methods[i];
            }
        }

        return null;
    }

    private static object GetProperty(object target, string propertyName)
    {
        return target.GetType()
            .GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance)
            .GetValue(target);
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        target.GetType()
            .GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance)
            .SetValue(target, value);
    }
}
