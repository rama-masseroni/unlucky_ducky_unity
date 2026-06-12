using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class LevelUiPrefabMigration
{
    public const string LevelUiRootPath = "Assets/Prefabs/UI/UI_LevelRoot.prefab";

    private const string HudPath = "Assets/Prefabs/UI/UI_LevelHudPanel.prefab";
    private const string InventoryPath = "Assets/Prefabs/UI/UI_PlaceableInventoryPanel.prefab";
    private const string PausePath = "Assets/Prefabs/UI/PausedMenu.prefab";
    private const string VictoryPath = "Assets/Prefabs/UI/UI_VictoryScreen.prefab";
    private const string DefeatPath = "Assets/Prefabs/UI/UI_DefeatScreen.prefab";

    [MenuItem("Unlucky Ducky/UI/Generate Level UI and Migrate Scenes")]
    public static void GenerateAndMigrate()
    {
        BuildLevelUiRootPrefab();
        MigrateGameplayScenes();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Level UI prefab generation and scene migration completed.");
    }

    [MenuItem("Unlucky Ducky/UI/Generate Level UI Prefab")]
    public static void BuildLevelUiRootPrefab()
    {
        GameObject hudAsset = LoadPrefab(HudPath);
        GameObject inventoryAsset = LoadPrefab(InventoryPath);
        GameObject pauseAsset = LoadPrefab(PausePath);
        GameObject victoryAsset = LoadPrefab(VictoryPath);
        GameObject defeatAsset = LoadPrefab(DefeatPath);

        GameObject root = new GameObject("UI_LevelRoot", typeof(RectTransform), typeof(LevelUiRoot));
        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        GameObject hud = AddNestedPrefab(hudAsset, root.transform);
        GameObject inventory = AddNestedPrefab(inventoryAsset, root.transform);
        GameObject pause = AddNestedPrefab(pauseAsset, root.transform);
        GameObject victory = AddNestedPrefab(victoryAsset, root.transform);
        GameObject defeat = AddNestedPrefab(defeatAsset, root.transform);

        LevelUiRoot levelUiRoot = root.GetComponent<LevelUiRoot>();
        SerializedObject serializedRoot = new SerializedObject(levelUiRoot);
        SetReference(serializedRoot, "hud", hud.GetComponent<LevelHudPanel>());
        SetReference(serializedRoot, "inventoryPanel", inventory.GetComponent<PlaceableInventoryPanel>());
        SetReference(serializedRoot, "pauseMenu", pause.GetComponent<PauseMenuManager>());
        SetReference(serializedRoot, "victoryScreen", victory.GetComponent<VictoryScreenManager>());
        SetReference(serializedRoot, "defeatScreen", defeat.GetComponent<DefeatScreenManager>());
        serializedRoot.ApplyModifiedPropertiesWithoutUndo();

        PrefabUtility.SaveAsPrefabAsset(root, LevelUiRootPath);
        UnityEngine.Object.DestroyImmediate(root);
    }

    [MenuItem("Unlucky Ducky/UI/Migrate Gameplay Scenes to Level UI Prefab")]
    public static void MigrateGameplayScenes()
    {
        GameObject levelUiPrefab = LoadPrefab(LevelUiRootPath);
        string[] sceneGuids = AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Scenes" });

        for (int i = 0; i < sceneGuids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(sceneGuids[i]);

            if (!IsGameplayScene(path))
            {
                continue;
            }

            Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            Canvas canvas = FindComponentInScene<Canvas>(scene);

            if (canvas == null)
            {
                throw new InvalidOperationException($"{path} has no Canvas.");
            }

            RemoveLegacyLevelUi(scene);
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(levelUiPrefab, canvas.transform);
            instance.name = "UI_LevelRoot";
            Stretch(instance.GetComponent<RectTransform>());
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
    }

    private static bool IsGameplayScene(string path)
    {
        return path.EndsWith("/Test_Scene.unity", StringComparison.Ordinal)
            || (path.Contains("/World ", StringComparison.Ordinal)
                && System.IO.Path.GetFileName(path).StartsWith("Scene_", StringComparison.Ordinal));
    }

    private static void RemoveLegacyLevelUi(Scene scene)
    {
        HashSet<GameObject> targets = new HashSet<GameObject>();
        CollectRoots<LevelUiRoot>(scene, targets);
        CollectRoots<LevelHudPanel>(scene, targets);
        CollectRoots<PlaceableInventoryPanel>(scene, targets);
        CollectRoots<PauseMenuManager>(scene, targets);
        CollectRoots<VictoryScreenManager>(scene, targets);
        CollectRoots<DefeatScreenManager>(scene, targets);

        foreach (GameObject target in targets)
        {
            if (target != null)
            {
                UnityEngine.Object.DestroyImmediate(target);
            }
        }
    }

    private static void CollectRoots<T>(Scene scene, HashSet<GameObject> targets) where T : Component
    {
        GameObject[] roots = scene.GetRootGameObjects();

        for (int i = 0; i < roots.Length; i++)
        {
            T[] components = roots[i].GetComponentsInChildren<T>(true);

            for (int j = 0; j < components.Length; j++)
            {
                GameObject target = components[j].gameObject;
                GameObject prefabRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(target);
                targets.Add(prefabRoot != null ? prefabRoot : target);
            }
        }
    }

    private static GameObject AddNestedPrefab(GameObject prefab, Transform parent)
    {
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        instance.name = prefab.name;
        return instance;
    }

    private static GameObject LoadPrefab(string path)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

        if (prefab == null)
        {
            throw new InvalidOperationException($"Required UI prefab is missing: {path}");
        }

        return prefab;
    }

    private static void SetReference(SerializedObject target, string propertyName, UnityEngine.Object value)
    {
        SerializedProperty property = target.FindProperty(propertyName);

        if (property == null)
        {
            throw new InvalidOperationException($"{target.targetObject.GetType().Name}.{propertyName} is missing.");
        }

        property.objectReferenceValue = value;
    }

    private static T FindComponentInScene<T>(Scene scene) where T : Component
    {
        GameObject[] roots = scene.GetRootGameObjects();

        for (int i = 0; i < roots.Length; i++)
        {
            T component = roots[i].GetComponentInChildren<T>(true);

            if (component != null)
            {
                return component;
            }
        }

        return null;
    }

    private static void Stretch(RectTransform rectTransform)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }
}
