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
    private readonly Type mainMenuNavigationControllerType = Type.GetType("MainMenuNavigationController, Assembly-CSharp");
    private readonly Type mainMenuPanelKindType = Type.GetType("MainMenuPanelKind, Assembly-CSharp");

    private readonly List<UnityEngine.Object> createdObjects = new List<UnityEngine.Object>();

    [TearDown]
    public void TearDown()
    {
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
        GameObject contentObject = CreateGameObject("Content", typeof(RectTransform), typeof(GridLayoutGroup));
        GameObject controllerObject = CreateGameObject("LevelSelectController");
        Component controller = controllerObject.AddComponent(levelSelectControllerType);

        Invoke(controller, "Configure", catalog, contentObject.transform);

        IReadOnlyList<Button> createdButtons = (IReadOnlyList<Button>)GetProperty(controller, "CreatedLevelButtons");
        Assert.AreEqual(5, createdButtons.Count);
        Assert.AreEqual(5, contentObject.transform.childCount);
        Assert.AreEqual(2, (int)GetProperty(controller, "TotalPages"));

        Invoke(controller, "ShowNextPage");

        Assert.AreEqual(2, ((IReadOnlyList<Button>)GetProperty(controller, "CreatedLevelButtons")).Count);
        Assert.AreEqual(5, contentObject.transform.childCount);
        Assert.AreEqual(1, (int)GetProperty(controller, "CurrentPageIndex"));
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

    private GameObject CreateGameObject(string name, params Type[] components)
    {
        GameObject gameObject = new GameObject(name, components);
        createdObjects.Add(gameObject);
        return gameObject;
    }

    private static void Invoke(object target, string methodName, params object[] parameters)
    {
        MethodInfo method = FindMethod(target.GetType(), methodName, parameters);
        Assert.IsNotNull(method, $"Could not find method {methodName} on {target.GetType().Name}.");
        method.Invoke(target, parameters);
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
