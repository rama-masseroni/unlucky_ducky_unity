using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

[InitializeOnLoad]
public static class LevelSceneBootstrapper
{
    private const string AutoBootstrapPreferenceKey = "UnluckyDucky.LevelSceneBootstrapper.AutoBootstrapNewScenes";
    private const string LevelDefinitionsFolder = "Assets/ScriptableObjects/Level definitions";
    private const string InventorySetsFolder = "Assets/ScriptableObjects/InventorySets";
    private const string WorldDefinitionsFolder = "Assets/ScriptableObjects/World Definitions";
    private const string DefaultWorldDefinitionPath = WorldDefinitionsFolder + "/World_01.asset";
    private const string LevelUiRootPrefabPath = "Assets/Prefabs/UI/UI_LevelRoot.prefab";
    private static readonly HashSet<int> AutoBootstrappedSceneHandles = new HashSet<int>();

    static LevelSceneBootstrapper()
    {
        if (Application.isBatchMode)
        {
            return;
        }

        EditorSceneManager.newSceneCreated += HandleNewSceneCreated;
        EditorSceneManager.sceneSaving += HandleSceneSaving;
    }

    [MenuItem("Unlucky Ducky/Level Scenes/Bootstrap Active Scene")]
    public static void BootstrapActiveScene()
    {
        BootstrapScene(SceneManager.GetActiveScene(), createAssets: true);
    }

    [MenuItem("Unlucky Ducky/Level Scenes/Auto Bootstrap New Scenes")]
    private static void ToggleAutoBootstrap()
    {
        EditorPrefs.SetBool(AutoBootstrapPreferenceKey, !IsAutoBootstrapEnabled());
    }

    [MenuItem("Unlucky Ducky/Level Scenes/Auto Bootstrap New Scenes", true)]
    private static bool ToggleAutoBootstrapValidate()
    {
        Menu.SetChecked("Unlucky Ducky/Level Scenes/Auto Bootstrap New Scenes", IsAutoBootstrapEnabled());
        return true;
    }

    private static bool IsAutoBootstrapEnabled()
    {
        return EditorPrefs.GetBool(AutoBootstrapPreferenceKey, true);
    }

    private static void HandleNewSceneCreated(Scene scene, NewSceneSetup setup, NewSceneMode mode)
    {
        if (!IsAutoBootstrapEnabled())
        {
            return;
        }

        EditorApplication.delayCall += () =>
        {
            if (scene.IsValid() && scene.isLoaded)
            {
                AutoBootstrappedSceneHandles.Add(scene.handle);
                BootstrapScene(scene, createAssets: false);
            }
        };
    }

    private static void HandleSceneSaving(Scene scene, string path)
    {
        if (!IsAutoBootstrapEnabled() || string.IsNullOrWhiteSpace(path) || !AutoBootstrappedSceneHandles.Contains(scene.handle))
        {
            return;
        }

        BootstrapScene(scene, createAssets: true);
    }

    private static void BootstrapScene(Scene scene, bool createAssets)
    {
        if (!scene.IsValid() || !scene.isLoaded)
        {
            return;
        }

        EnsureSceneHierarchy(scene);

        if (createAssets)
        {
            LevelDefinition levelDefinition = EnsureLevelAssets(scene);
            AssignLevelDefinition(scene, levelDefinition);
        }

        EditorSceneManager.MarkSceneDirty(scene);
    }

    private static void EnsureSceneHierarchy(Scene scene)
    {
        GameObject levelRoot = FindObjectInScene(scene, "LevelRoot") ?? CreateRoot(scene, "LevelRoot");
        GameObject gridObject = FindObjectInScene(scene, "Grid") ?? FindObjectInScene(scene, "Grids") ?? CreateChild(levelRoot.transform, "Grid");

        if (gridObject.GetComponent<Grid>() == null)
        {
            gridObject.AddComponent<Grid>();
        }

        EnsureTilemap(gridObject.transform, "Walls Tilemap", new[] { "Walls Tilemap" }, addCollider: true, addDestructibleLayer: false, addHazardLayer: false, sortingOrder: 0);
        EnsureTilemap(gridObject.transform, "Breakable Tilemap", new[] { "Breakable Tilemap", "Breakable tilemap", "Destructible Tilemap" }, addCollider: true, addDestructibleLayer: true, addHazardLayer: false, sortingOrder: 1);
        EnsureTilemap(gridObject.transform, "Hazard Tilemap", new[] { "Hazard Tilemap", "Spikes tilemap", "Spikes Tilemap" }, addCollider: true, addDestructibleLayer: false, addHazardLayer: true, sortingOrder: 2);

        GameObject gameplayManagers = FindObjectInScene(scene, "GameplayManagers") ?? CreateRoot(scene, "GameplayManagers");

        if (FindComponentInScene<GameStateManager>(scene) == null)
        {
            GameObject managerObject = CreateChild(gameplayManagers.transform, "GameStateManager");
            managerObject.AddComponent<GameStateManager>();
        }

        if (FindObjectInScene(scene, "PlacedObjectsRoot") == null)
        {
            CreateRoot(scene, "PlacedObjectsRoot");
        }

        EnsureLevelUi(scene);
    }

    private static void EnsureLevelUi(Scene scene)
    {
        Canvas canvas = FindComponentInScene<Canvas>(scene);

        if (canvas == null)
        {
            GameObject uiRoot = FindObjectInScene(scene, "UIRoot") ?? CreateRoot(scene, "UIRoot");
            GameObject canvasObject = CreateChild(uiRoot.transform, "Canvas");
            canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            canvasObject.AddComponent<GraphicRaycaster>();
        }

        if (FindComponentInScene<LevelUiRoot>(scene) == null)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(LevelUiRootPrefabPath);

            if (prefab != null)
            {
                PrefabUtility.InstantiatePrefab(prefab, canvas.transform);
            }
            else
            {
                Debug.LogWarning($"Could not load required level UI prefab at {LevelUiRootPrefabPath}.");
            }
        }

        if (FindComponentInScene<EventSystem>(scene) == null)
        {
            GameObject eventSystemObject = CreateRoot(scene, "EventSystem");
            eventSystemObject.AddComponent<EventSystem>();
            eventSystemObject.AddComponent<InputSystemUIInputModule>();
        }
    }

    private static Tilemap EnsureTilemap(
        Transform parent,
        string name,
        string[] existingNames,
        bool addCollider,
        bool addDestructibleLayer,
        bool addHazardLayer,
        int sortingOrder)
    {
        Transform existing = FindExistingTilemap(parent, existingNames);
        GameObject tilemapObject = existing != null ? existing.gameObject : CreateChild(parent, name);

        Tilemap tilemap = tilemapObject.GetComponent<Tilemap>();

        if (tilemap == null)
        {
            tilemap = tilemapObject.AddComponent<Tilemap>();
        }

        TilemapRenderer renderer = tilemapObject.GetComponent<TilemapRenderer>();

        if (renderer == null)
        {
            renderer = tilemapObject.AddComponent<TilemapRenderer>();
        }

        renderer.sortingOrder = sortingOrder;

        if (addCollider && tilemapObject.GetComponent<TilemapCollider2D>() == null)
        {
            tilemapObject.AddComponent<TilemapCollider2D>();
        }

        if (addDestructibleLayer && tilemapObject.GetComponent<DestructibleTilemapLayer>() == null)
        {
            tilemapObject.AddComponent<DestructibleTilemapLayer>();
        }

        if (addHazardLayer && tilemapObject.GetComponent<HazardTilemapLayer>() == null)
        {
            tilemapObject.AddComponent<HazardTilemapLayer>();
        }

        return tilemap;
    }

    private static Transform FindExistingTilemap(Transform parent, string[] names)
    {
        for (int i = 0; i < names.Length; i++)
        {
            Transform match = FindChildRecursive(parent, names[i]);

            if (match != null && match.GetComponent<Tilemap>() != null)
            {
                return match;
            }
        }

        return null;
    }

    private static LevelDefinition EnsureLevelAssets(Scene scene)
    {
        EnsureFolder(LevelDefinitionsFolder);
        EnsureFolder(InventorySetsFolder);

        LevelAssetNames names = LevelAssetNames.FromSceneName(scene.name);
        string inventoryPath = $"{InventorySetsFolder}/{names.InventorySetAssetName}.asset";
        string levelPath = $"{LevelDefinitionsFolder}/{names.LevelDefinitionAssetName}.asset";

        PlaceableInventorySet inventorySet = AssetDatabase.LoadAssetAtPath<PlaceableInventorySet>(inventoryPath);

        if (inventorySet == null)
        {
            inventorySet = ScriptableObject.CreateInstance<PlaceableInventorySet>();
            inventorySet.name = names.InventorySetAssetName;
            AssetDatabase.CreateAsset(inventorySet, inventoryPath);
        }

        LevelDefinition levelDefinition = AssetDatabase.LoadAssetAtPath<LevelDefinition>(levelPath);
        bool createdLevelDefinition = false;

        if (levelDefinition == null)
        {
            levelDefinition = ScriptableObject.CreateInstance<LevelDefinition>();
            levelDefinition.name = names.LevelDefinitionAssetName;
            AssetDatabase.CreateAsset(levelDefinition, levelPath);
            createdLevelDefinition = true;
        }

        ConfigureLevelDefinition(levelDefinition, inventorySet, names, createdLevelDefinition);
        AssetDatabase.SaveAssets();
        return levelDefinition;
    }

    private static void ConfigureLevelDefinition(
        LevelDefinition levelDefinition,
        PlaceableInventorySet inventorySet,
        LevelAssetNames names,
        bool overwriteEmptyFields)
    {
        SerializedObject serializedLevel = new SerializedObject(levelDefinition);
        SerializedProperty levelId = serializedLevel.FindProperty("levelId");
        SerializedProperty levelName = serializedLevel.FindProperty("levelName");
        SerializedProperty worldDefinition = serializedLevel.FindProperty("worldDefinition");
        SerializedProperty placeableInventorySet = serializedLevel.FindProperty("placeableInventorySet");

        if (overwriteEmptyFields || string.IsNullOrWhiteSpace(levelId.stringValue))
        {
            levelId.stringValue = names.LevelId;
        }

        if (overwriteEmptyFields || string.IsNullOrWhiteSpace(levelName.stringValue))
        {
            levelName.stringValue = names.DisplayName;
        }

        if (worldDefinition.objectReferenceValue == null)
        {
            worldDefinition.objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<WorldDefinition>(names.WorldDefinitionPath) ??
                AssetDatabase.LoadAssetAtPath<WorldDefinition>(DefaultWorldDefinitionPath);
        }

        if (placeableInventorySet.objectReferenceValue == null)
        {
            placeableInventorySet.objectReferenceValue = inventorySet;
        }

        serializedLevel.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(levelDefinition);
    }

    private static void AssignLevelDefinition(Scene scene, LevelDefinition levelDefinition)
    {
        if (levelDefinition == null)
        {
            return;
        }

        GameStateManager gameStateManager = FindComponentInScene<GameStateManager>(scene);

        if (gameStateManager == null)
        {
            return;
        }

        SerializedObject serializedManager = new SerializedObject(gameStateManager);
        SerializedProperty levelDefinitionProperty = serializedManager.FindProperty("levelDefinition");

        if (levelDefinitionProperty.objectReferenceValue == null)
        {
            levelDefinitionProperty.objectReferenceValue = levelDefinition;
            serializedManager.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(gameStateManager);
        }
    }

    private static void EnsureFolder(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath))
        {
            return;
        }

        string parent = Path.GetDirectoryName(folderPath)?.Replace("\\", "/");
        string folderName = Path.GetFileName(folderPath);

        if (!string.IsNullOrWhiteSpace(parent))
        {
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, folderName);
        }
    }

    private static GameObject FindObjectInScene(Scene scene, string name)
    {
        GameObject[] roots = scene.GetRootGameObjects();

        for (int i = 0; i < roots.Length; i++)
        {
            Transform match = FindChildRecursive(roots[i].transform, name);

            if (match != null)
            {
                return match.gameObject;
            }
        }

        return null;
    }

    private static Transform FindChildRecursive(Transform root, string name)
    {
        if (root.name == name)
        {
            return root;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform match = FindChildRecursive(root.GetChild(i), name);

            if (match != null)
            {
                return match;
            }
        }

        return null;
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

    private static GameObject CreateRoot(Scene scene, string name)
    {
        GameObject root = new GameObject(name);
        SceneManager.MoveGameObjectToScene(root, scene);
        return root;
    }

    private static GameObject CreateChild(Transform parent, string name)
    {
        GameObject child = new GameObject(name);
        child.transform.SetParent(parent, false);
        return child;
    }

    private readonly struct LevelAssetNames
    {
        public readonly string LevelDefinitionAssetName;
        public readonly string InventorySetAssetName;
        public readonly string LevelId;
        public readonly string DisplayName;
        public readonly string WorldDefinitionPath;

        private LevelAssetNames(
            string levelDefinitionAssetName,
            string inventorySetAssetName,
            string levelId,
            string displayName,
            string worldDefinitionPath)
        {
            LevelDefinitionAssetName = levelDefinitionAssetName;
            InventorySetAssetName = inventorySetAssetName;
            LevelId = levelId;
            DisplayName = displayName;
            WorldDefinitionPath = worldDefinitionPath;
        }

        public static LevelAssetNames FromSceneName(string sceneName)
        {
            Match match = Regex.Match(sceneName, @"^Scene_(\d{2})_(\d{2})$");

            if (match.Success)
            {
                string worldNumber = match.Groups[1].Value;
                string levelNumber = match.Groups[2].Value;
                return new LevelAssetNames(
                    $"LevelDefinition_{worldNumber}_{levelNumber}",
                    $"InventorySet_{worldNumber}_{levelNumber}",
                    $"Level_{worldNumber}_{levelNumber}",
                    $"Level {int.Parse(worldNumber)}-{int.Parse(levelNumber)}",
                    $"{WorldDefinitionsFolder}/World_{worldNumber}.asset");
            }

            string assetSuffix = string.IsNullOrWhiteSpace(sceneName)
                ? "Custom"
                : Regex.Replace(sceneName.Trim(), @"[^\w]+", "_");

            assetSuffix = Regex.Replace(assetSuffix, @"_+", "_").Trim('_');

            if (string.IsNullOrWhiteSpace(assetSuffix))
            {
                assetSuffix = "Custom";
            }

            return new LevelAssetNames(
                $"LevelDefinition_{assetSuffix}",
                $"InventorySet_{assetSuffix}",
                $"Level_{assetSuffix}",
                assetSuffix.Replace("_", " "),
                DefaultWorldDefinitionPath);
        }
    }
}
