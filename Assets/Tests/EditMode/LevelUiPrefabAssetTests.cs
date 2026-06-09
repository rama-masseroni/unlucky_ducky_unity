using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LevelUiPrefabAssetTests
{
    private const string LevelUiRootPath = "Assets/Prefabs/UI/UI_LevelRoot.prefab";
    private readonly System.Type levelUiRootType = System.Type.GetType("LevelUiRoot, Assembly-CSharp");
    private readonly System.Type levelHudPanelType = System.Type.GetType("LevelHudPanel, Assembly-CSharp");
    private readonly System.Type inventoryPanelType = System.Type.GetType("PlaceableInventoryPanel, Assembly-CSharp");
    private readonly System.Type pauseMenuType = System.Type.GetType("PauseMenuManager, Assembly-CSharp");
    private readonly System.Type victoryScreenType = System.Type.GetType("VictoryScreenManager, Assembly-CSharp");
    private readonly System.Type defeatScreenType = System.Type.GetType("DefeatScreenManager, Assembly-CSharp");

    [Test]
    public void LevelUiRoot_ContainsAllAuthoredLevelUiWithoutNestedCanvas()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(LevelUiRootPath);

        Assert.IsNotNull(prefab);
        Assert.IsNotNull(prefab.GetComponent(levelUiRootType));
        Assert.AreEqual(0, prefab.GetComponentsInChildren<Canvas>(true).Length);
        Assert.IsNotNull(prefab.GetComponentInChildren(levelHudPanelType, true));
        Assert.IsNotNull(prefab.GetComponentInChildren(inventoryPanelType, true));
        Assert.IsNotNull(prefab.GetComponentInChildren(pauseMenuType, true));
        Assert.IsNotNull(prefab.GetComponentInChildren(victoryScreenType, true));
        Assert.IsNotNull(prefab.GetComponentInChildren(defeatScreenType, true));
    }

    [Test]
    public void GameplayScenes_HaveOneCanvasEventSystemAndLevelUiRoot()
    {
        string[] guids = AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Scenes" });
        int checkedScenes = 0;

        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);

            if (!path.EndsWith("/Test_Scene.unity")
                && (!path.Contains("/World ") || !System.IO.Path.GetFileName(path).StartsWith("Scene_")))
            {
                continue;
            }

            Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);

            try
            {
                Assert.AreEqual(1, CountInScene<Canvas>(scene), path);
                Assert.AreEqual(1, CountInScene<EventSystem>(scene), path);
                Assert.AreEqual(1, CountInScene(scene, levelUiRootType), path);
                Assert.AreEqual(1, CountInScene(scene, levelHudPanelType), path);
                Assert.AreEqual(1, CountInScene(scene, inventoryPanelType), path);
                Assert.AreEqual(1, CountInScene(scene, pauseMenuType), path);
                Assert.AreEqual(1, CountInScene(scene, victoryScreenType), path);
                Assert.AreEqual(1, CountInScene(scene, defeatScreenType), path);
                checkedScenes++;
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        Assert.AreEqual(19, checkedScenes);
    }

    private static int CountInScene<T>(Scene scene) where T : Component
    {
        int count = 0;
        GameObject[] roots = scene.GetRootGameObjects();

        for (int i = 0; i < roots.Length; i++)
        {
            count += roots[i].GetComponentsInChildren<T>(true).Length;
        }

        return count;
    }

    private static int CountInScene(Scene scene, System.Type type)
    {
        Assert.IsNotNull(type);
        int count = 0;
        GameObject[] roots = scene.GetRootGameObjects();

        for (int i = 0; i < roots.Length; i++)
        {
            count += roots[i].GetComponentsInChildren(type, true).Length;
        }

        return count;
    }
}
