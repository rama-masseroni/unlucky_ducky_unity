using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.TestTools;
using UnityEngine.UI;

public class LevelManagerTilemapTests
{
    private GameObject gridObject;
    private GameObject tilemapObject;
    private Tilemap tilemap;
    private MethodInfo tryDestroyTileAtCellMethod;

    [SetUp]
    public void SetUp()
    {
        gridObject = new GameObject("Grid", typeof(Grid));
        tilemapObject = new GameObject("Tilemap", typeof(Tilemap), typeof(TilemapRenderer));
        tilemapObject.transform.SetParent(gridObject.transform);
        tilemap = tilemapObject.GetComponent<Tilemap>();

        Type levelManagerType = Type.GetType("LevelManager, Assembly-CSharp");
        Assert.IsNotNull(levelManagerType);

        tryDestroyTileAtCellMethod = levelManagerType.GetMethod(
            "TryDestroyTileAtCell",
            BindingFlags.Public | BindingFlags.Static);

        Assert.IsNotNull(tryDestroyTileAtCellMethod);
    }

    [TearDown]
    public void TearDown()
    {
        UnityEngine.Object.DestroyImmediate(gridObject);
    }

    [Test]
    public void TryDestroyTileAtCell_WhenCellHasTile_RemovesTileFromGridTilemap()
    {
        Vector3Int cellPosition = new Vector3Int(2, 3, 0);
        Tile tile = ScriptableObject.CreateInstance<Tile>();
        tilemap.SetTile(cellPosition, tile);

        bool destroyed = TryDestroyTileAtCell(cellPosition);

        Assert.IsTrue(destroyed);
        Assert.IsFalse(tilemap.HasTile(cellPosition));
    }

    [Test]
    public void TryDestroyTileAtCell_WhenCellIsEmpty_ReturnsFalse()
    {
        Vector3Int cellPosition = new Vector3Int(2, 3, 0);

        bool destroyed = TryDestroyTileAtCell(cellPosition);

        Assert.IsFalse(destroyed);
    }

    private bool TryDestroyTileAtCell(Vector3Int cellPosition)
    {
        return (bool)tryDestroyTileAtCellMethod.Invoke(null, new object[] { tilemap, cellPosition });
    }
}

public class LevelPhaseSystemTests
{
    private readonly Type placeableDefinitionType = Type.GetType("PlaceableDefinition, UnluckyDucky.Core");
    private readonly Type placeableUseModeType = Type.GetType("PlaceableUseMode, UnluckyDucky.Core");
    private readonly Type inventoryEntryType = Type.GetType("PlaceableInventoryEntry, UnluckyDucky.Core");
    private readonly Type inventorySetType = Type.GetType("PlaceableInventorySet, UnluckyDucky.Core");
    private readonly Type runtimeInventoryType = Type.GetType("PlaceableInventoryRuntime, UnluckyDucky.Core");
    private readonly Type gameStateManagerType = Type.GetType("GameStateManager, UnluckyDucky.Core");
    private readonly Type bombControllerType = Type.GetType("BombController, Assembly-CSharp");
    private readonly Type levelDefinitionType = Type.GetType("LevelDefinition, UnluckyDucky.Core");
    private readonly Type goalPointControllerType = Type.GetType("GoalPointController, Assembly-CSharp");
    private readonly Type victoryScreenManagerType = Type.GetType("VictoryScreenManager, Assembly-CSharp");
    private readonly Type defeatScreenManagerType = Type.GetType("DefeatScreenManager, Assembly-CSharp");
    private readonly Type levelHudPanelType = Type.GetType("LevelHudPanel, Assembly-CSharp");
    private readonly Type playerDuckControllerType = Type.GetType("PlayerDuckController, UnluckyDucky.Player");
    private readonly Type resetLevelButtonControllerType = Type.GetType("ResetLevelButtonController, Assembly-CSharp");
    private readonly Type buildModePlacementControllerType = Type.GetType("BuildModePlacementController, Assembly-CSharp");
    private readonly Type fallingDestructibleTilemapLayerType = Type.GetType("FallingDestructibleTilemapLayer, Assembly-CSharp");
    private readonly Type fallingTileBlockType = Type.GetType("FallingTileBlock, Assembly-CSharp");
    private readonly Type breakableType = Type.GetType("IBreakable, UnluckyDucky.Core");
    private readonly Type enemyRatControllerType = Type.GetType("EnemyRatController, UnluckyDucky.Enemies");
    private readonly Type hazardTilemapLayerType = Type.GetType("HazardTilemapLayer, UnluckyDucky.Core");
    private readonly Type levelManagerType = Type.GetType("LevelManager, Assembly-CSharp");

    private GameObject gameStateObject;
    private GameObject duckObject;
    private GameObject goalObject;
    private GameObject resetButtonObject;
    private GameObject gridObject;
    private GameObject tilemapObject;
    private GameObject uiPrefabObject;

    [TearDown]
    public void TearDown()
    {
        if (gameStateObject != null)
        {
            UnityEngine.Object.DestroyImmediate(gameStateObject);
        }

        if (duckObject != null)
        {
            UnityEngine.Object.DestroyImmediate(duckObject);
        }

        if (goalObject != null)
        {
            UnityEngine.Object.DestroyImmediate(goalObject);
        }

        if (resetButtonObject != null)
        {
            UnityEngine.Object.DestroyImmediate(resetButtonObject);
        }

        if (uiPrefabObject != null)
        {
            UnityEngine.Object.DestroyImmediate(uiPrefabObject);
        }

        DestroyObjectNamed("VictoryScreenCanvas");
        DestroyObjectNamed("VictoryScreenManager");
        DestroyObjectNamed("DefeatScreenCanvas");
        DestroyObjectNamed("DefeatScreenManager");
        Time.timeScale = 1f;

        if (gridObject != null)
        {
            UnityEngine.Object.DestroyImmediate(gridObject);
        }

        goalPointControllerType?.GetProperty("SceneLoadOverride").SetValue(null, null);
        gameStateManagerType?.GetProperty("SceneReloadOverride").SetValue(null, null);
        gameStateManagerType?.GetProperty("PlanningTimeoutHandler").SetValue(null, null);
        playerDuckControllerType?.GetProperty("DeathScreenHandler").SetValue(null, null);
    }

    [Test]
    public void RuntimeInventory_CopiesAmountsWithoutMutatingInventorySet()
    {
        ScriptableObject inventorySet = CreateInventorySetWithOneItem(1, out object authoredEntry);
        object runtimeInventory = Activator.CreateInstance(runtimeInventoryType, inventorySet);

        Assert.IsFalse((bool)runtimeInventoryType.GetProperty("AllItemsUsed").GetValue(runtimeInventory));

        object runtimeEntry = GetFirstRuntimeEntry(runtimeInventory);
        bool consumed = (bool)runtimeEntry.GetType().GetMethod("TryConsumeOne").Invoke(runtimeEntry, null);

        Assert.IsTrue(consumed);
        Assert.IsTrue((bool)runtimeInventoryType.GetProperty("AllItemsUsed").GetValue(runtimeInventory));
        Assert.AreEqual(1, inventoryEntryType.GetProperty("Amount").GetValue(authoredEntry));
    }

    [Test]
    public void RuntimeInventory_ReturnsConsumedItemsWithoutExceedingInitialAmount()
    {
        ScriptableObject inventorySet = CreateInventorySetWithOneItem(1, out _);
        object runtimeInventory = Activator.CreateInstance(runtimeInventoryType, inventorySet);
        object runtimeEntry = GetFirstRuntimeEntry(runtimeInventory);

        Assert.IsFalse((bool)runtimeEntry.GetType().GetMethod("TryReturnOne").Invoke(runtimeEntry, null));
        Assert.IsTrue((bool)runtimeEntry.GetType().GetMethod("TryConsumeOne").Invoke(runtimeEntry, null));
        Assert.AreEqual(0, runtimeEntry.GetType().GetProperty("Amount").GetValue(runtimeEntry));

        Assert.IsTrue((bool)runtimeEntry.GetType().GetMethod("TryReturnOne").Invoke(runtimeEntry, null));
        Assert.AreEqual(1, runtimeEntry.GetType().GetProperty("Amount").GetValue(runtimeEntry));
        Assert.IsFalse((bool)runtimeEntry.GetType().GetMethod("TryReturnOne").Invoke(runtimeEntry, null));
        Assert.AreEqual(1, runtimeEntry.GetType().GetProperty("Amount").GetValue(runtimeEntry));
    }

    [Test]
    public void GameStateManager_OnlyStartsExecutionWhenRuntimeInventoryIsEmpty()
    {
        ScriptableObject inventorySet = CreateInventorySetWithOneItem(1, out _);
        object manager = CreateGameStateManager();
        gameStateManagerType.GetMethod("SetFallbackInventorySet").Invoke(manager, new object[] { inventorySet });

        Assert.IsFalse((bool)gameStateManagerType.GetProperty("CanStartExecution").GetValue(manager));
        Assert.IsFalse((bool)gameStateManagerType.GetMethod("TryStartExecution").Invoke(manager, null));

        object runtimeInventory = gameStateManagerType.GetProperty("Inventory").GetValue(manager);
        object runtimeEntry = GetFirstRuntimeEntry(runtimeInventory);
        runtimeEntry.GetType().GetMethod("TryConsumeOne").Invoke(runtimeEntry, null);

        Assert.IsTrue((bool)gameStateManagerType.GetProperty("CanStartExecution").GetValue(manager));
        Assert.IsTrue((bool)gameStateManagerType.GetMethod("TryStartExecution").Invoke(manager, null));
        Assert.AreEqual("Execution", gameStateManagerType.GetProperty("CurrentPhase").GetValue(manager).ToString());
    }

    [Test]
    public void GameStateManager_CanStartExecutionWithUnusedExecutionTileDestructionTool()
    {
        ScriptableObject inventorySet = CreateInventorySetWithOneItem(
            1,
            GetUseMode("ExecutionClickToDestroyTile"),
            out _);
        object manager = CreateGameStateManager();
        gameStateManagerType.GetMethod("SetFallbackInventorySet").Invoke(manager, new object[] { inventorySet });

        Assert.IsTrue((bool)gameStateManagerType.GetProperty("CanStartExecution").GetValue(manager));
        Assert.IsTrue((bool)gameStateManagerType.GetMethod("TryStartExecution").Invoke(manager, null));
    }

    [Test]
    public void GameStateManager_PlanningTimer_CountsDownOnlyInPlanning()
    {
        object manager = CreateGameStateManager();
        ScriptableObject levelDefinition = CreateLevelDefinitionWithPlanningTime(10f);
        gameStateManagerType.GetMethod("SetLevelDefinition").Invoke(manager, new object[] { levelDefinition });

        InvokePlanningTimerTick(manager, 3f);

        Assert.AreEqual(7f, (float)gameStateManagerType.GetProperty("RemainingPlanningSeconds").GetValue(manager), 0.001f);
        Assert.IsTrue((bool)gameStateManagerType.GetMethod("TryStartExecution").Invoke(manager, null));

        InvokePlanningTimerTick(manager, 3f);

        Assert.AreEqual(7f, (float)gameStateManagerType.GetProperty("RemainingPlanningSeconds").GetValue(manager), 0.001f);
    }

    [Test]
    public void GameStateManager_PlanningTimer_ZeroLimitDoesNotTimeout()
    {
        object manager = CreateGameStateManager();
        ScriptableObject levelDefinition = CreateLevelDefinitionWithPlanningTime(0f);
        bool timedOut = false;
        RegisterPlanningTimeoutHandler(_ =>
        {
            timedOut = true;
            return true;
        });

        gameStateManagerType.GetMethod("SetLevelDefinition").Invoke(manager, new object[] { levelDefinition });
        InvokePlanningTimerTick(manager, 30f);

        Assert.IsFalse((bool)gameStateManagerType.GetProperty("HasPlanningTimeLimit").GetValue(manager));
        Assert.AreEqual(0f, (float)gameStateManagerType.GetProperty("RemainingPlanningSeconds").GetValue(manager), 0.001f);
        Assert.IsFalse(timedOut);
        Assert.IsTrue((bool)gameStateManagerType.GetProperty("CanStartExecution").GetValue(manager));
    }

    [Test]
    public void GameStateManager_PlanningTimer_TimeoutShowsDefeatAndBlocksExecution()
    {
        object manager = CreateGameStateManager();
        ScriptableObject levelDefinition = CreateLevelDefinitionWithPlanningTime(1f);
        string timeoutMessage = null;
        RegisterPlanningTimeoutHandler(message =>
        {
            timeoutMessage = message;
            return true;
        });

        gameStateManagerType.GetMethod("SetLevelDefinition").Invoke(manager, new object[] { levelDefinition });
        InvokePlanningTimerTick(manager, 1.1f);

        Assert.AreEqual(0f, (float)gameStateManagerType.GetProperty("RemainingPlanningSeconds").GetValue(manager), 0.001f);
        Assert.AreEqual("Se acab\u00f3 el tiempo de planeaci\u00f3n", timeoutMessage);
        Assert.IsFalse((bool)gameStateManagerType.GetProperty("CanStartExecution").GetValue(manager));
        Assert.IsFalse((bool)gameStateManagerType.GetMethod("TryStartExecution").Invoke(manager, null));
        Assert.AreEqual("Planning", gameStateManagerType.GetProperty("CurrentPhase").GetValue(manager).ToString());
    }

    [Test]
    public void LevelHudPanel_PlanningTimer_ShowsFormattedTimeAndHidesWithoutLimit()
    {
        Assert.IsNotNull(levelHudPanelType);

        object manager = CreateGameStateManager();
        ScriptableObject levelDefinition = CreateLevelDefinitionWithPlanningTime(65.2f);
        gameStateManagerType.GetMethod("SetLevelDefinition").Invoke(manager, new object[] { levelDefinition });

        GameObject hudObject = InstantiateUiPrefab("Assets/Prefabs/UI/UI_LevelHudPanel.prefab");
        Component hud = hudObject.GetComponent(levelHudPanelType);
        SetPrivateField(hud, "gameStateManager", manager);

        try
        {
            levelHudPanelType.GetMethod("RefreshPlanningTimer").Invoke(hud, null);
            Type textMeshProType = Type.GetType("TMPro.TextMeshProUGUI, Unity.TextMeshPro");
            Assert.IsNotNull(textMeshProType);
            Component timerText = (Component)hudObject.transform.Find("PlanningTimerText").GetComponent(textMeshProType);
            Assert.IsNotNull(timerText);

            Assert.IsTrue(timerText.gameObject.activeSelf);
            Assert.AreEqual("01:06", textMeshProType.GetProperty("text").GetValue(timerText));

            gameStateManagerType.GetMethod("SetLevelDefinition").Invoke(manager, new object[] { CreateLevelDefinitionWithPlanningTime(0f) });
            levelHudPanelType.GetMethod("RefreshPlanningTimer").Invoke(hud, null);

            Assert.IsFalse(timerText.gameObject.activeSelf);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(hudObject);
        }
    }

    [Test]
    public void LevelManager_TileDestructionTool_DoesNotDestroyOrConsumeInPlanning()
    {
        CreateLevelManagerWithPickaxe(1, out Component levelManager, out Tilemap testTilemap, out object runtimeEntry);
        Vector3Int cellPosition = new Vector3Int(2, 3, 0);
        Tile tile = ScriptableObject.CreateInstance<Tile>();
        testTilemap.SetTile(cellPosition, tile);

        bool used = (bool)levelManagerType
            .GetMethod("TryUseTileDestructionTool")
            .Invoke(levelManager, new object[] { testTilemap.GetCellCenterWorld(cellPosition) });

        Assert.IsFalse(used);
        Assert.IsTrue(testTilemap.HasTile(cellPosition));
        Assert.AreEqual(1, runtimeEntry.GetType().GetProperty("Amount").GetValue(runtimeEntry));
    }

    [Test]
    public void LevelManager_TileDestructionTool_DestroyingTileConsumesOneInExecution()
    {
        object manager = CreateLevelManagerWithPickaxe(1, out Component levelManager, out Tilemap testTilemap, out object runtimeEntry);
        Assert.IsTrue((bool)gameStateManagerType.GetMethod("TryStartExecution").Invoke(manager, null));

        Vector3Int cellPosition = new Vector3Int(2, 3, 0);
        Tile tile = ScriptableObject.CreateInstance<Tile>();
        testTilemap.SetTile(cellPosition, tile);

        bool used = (bool)levelManagerType
            .GetMethod("TryUseTileDestructionTool")
            .Invoke(levelManager, new object[] { testTilemap.GetCellCenterWorld(cellPosition) });

        Assert.IsTrue(used);
        Assert.IsFalse(testTilemap.HasTile(cellPosition));
        Assert.AreEqual(0, runtimeEntry.GetType().GetProperty("Amount").GetValue(runtimeEntry));
    }

    [Test]
    public void LevelManager_TileDestructionTool_ClickingEmptyCellDoesNotConsume()
    {
        object manager = CreateLevelManagerWithPickaxe(1, out Component levelManager, out Tilemap testTilemap, out object runtimeEntry);
        Assert.IsTrue((bool)gameStateManagerType.GetMethod("TryStartExecution").Invoke(manager, null));

        bool used = (bool)levelManagerType
            .GetMethod("TryUseTileDestructionTool")
            .Invoke(levelManager, new object[] { testTilemap.GetCellCenterWorld(new Vector3Int(2, 3, 0)) });

        Assert.IsFalse(used);
        Assert.AreEqual(1, runtimeEntry.GetType().GetProperty("Amount").GetValue(runtimeEntry));
    }

    [Test]
    public void BombController_DoesNotStartCountdownInPlanning()
    {
        CreateGameStateManager();
        GameObject bombObject = new GameObject("Bomb");
        Component bomb = bombObject.AddComponent(bombControllerType);

        Assert.IsFalse((bool)bombControllerType.GetProperty("HasStartedCountdown").GetValue(bomb));

        UnityEngine.Object.DestroyImmediate(bombObject);
    }

    [Test]
    public void GoalPoint_OnlyCompletesLevelDuringExecution()
    {
        Assert.IsNotNull(levelDefinitionType);
        Assert.IsNotNull(goalPointControllerType);
        Assert.IsNotNull(victoryScreenManagerType);
        Assert.IsNotNull(playerDuckControllerType);

        object manager = CreateGameStateManager();
        ScriptableObject levelDefinition = ScriptableObject.CreateInstance(levelDefinitionType);
        SetPrivateField(levelDefinition, "nextSceneName", "Level_02_TestEmpty");
        gameStateManagerType.GetMethod("SetLevelDefinition").Invoke(manager, new object[] { levelDefinition });

        duckObject = new GameObject("Duck", typeof(Rigidbody2D), typeof(BoxCollider2D));
        Component duck = duckObject.AddComponent(playerDuckControllerType);
        goalObject = new GameObject("Goal", typeof(BoxCollider2D));
        Component goal = goalObject.AddComponent(goalPointControllerType);

        string requestedScene = null;
        goalPointControllerType.GetProperty("SceneLoadOverride").SetValue(null, new Action<string>(sceneName => requestedScene = sceneName));

        bool completedInPlanning = (bool)goalPointControllerType
            .GetMethod("TryCompleteLevel")
            .Invoke(goal, new object[] { duck });

        Assert.IsFalse(completedInPlanning);
        Assert.IsNull(requestedScene);

        Assert.IsTrue((bool)gameStateManagerType.GetMethod("TryStartExecution").Invoke(manager, null));
        InstantiateUiPrefab("Assets/Prefabs/UI/UI_VictoryScreen.prefab");

        bool completedInExecution = (bool)goalPointControllerType
            .GetMethod("TryCompleteLevel")
            .Invoke(goal, new object[] { duck });

        Assert.IsTrue(completedInExecution);
        Assert.IsNull(requestedScene);

        Component victoryScreen = uiPrefabObject.GetComponent(victoryScreenManagerType);
        Assert.IsNotNull(victoryScreen);
        Assert.IsTrue((bool)GetProperty(victoryScreen, "IsVisible"));
        Assert.AreEqual("Level_02_TestEmpty", GetProperty(victoryScreen, "NextSceneName"));

        victoryScreenManagerType.GetMethod("ContinueButton").Invoke(victoryScreen, null);

        Assert.AreEqual("Level_02_TestEmpty", requestedScene);
    }

    [Test]
    public void PlayerDuckController_WhenKilled_ShowsDefeatScreenAndRetryResetsLevel()
    {
        Assert.IsNotNull(gameStateManagerType);
        Assert.IsNotNull(playerDuckControllerType);
        Assert.IsNotNull(defeatScreenManagerType);

        CreateGameStateManager();
        InstantiateUiPrefab("Assets/Prefabs/UI/UI_DefeatScreen.prefab");
        int reloadRequests = 0;
        gameStateManagerType.GetProperty("SceneReloadOverride").SetValue(null, new Action<int, string>((_, _) => reloadRequests++));
        RegisterDefeatScreenHandler();

        duckObject = new GameObject("Duck", typeof(Rigidbody2D), typeof(BoxCollider2D));
        Component duck = duckObject.AddComponent(playerDuckControllerType);

        playerDuckControllerType.GetMethod("Kill").Invoke(duck, null);

        Assert.IsTrue((bool)GetProperty(duck, "IsDead"));

        Component defeatScreen = uiPrefabObject.GetComponent(defeatScreenManagerType);
        Assert.IsNotNull(defeatScreen);
        Assert.IsTrue((bool)GetProperty(defeatScreen, "IsVisible"));
        Assert.AreEqual(0f, Time.timeScale);

        defeatScreenManagerType.GetMethod("RetryButton").Invoke(defeatScreen, null);

        Assert.AreEqual(1f, Time.timeScale);
        Assert.AreEqual(1, reloadRequests);
    }

    [Test]
    public void GameStateManager_ResetCurrentLevel_RequestsReloadInPlanningAndExecution()
    {
        Assert.IsNotNull(gameStateManagerType);

        object manager = CreateGameStateManager();
        int reloadRequests = 0;
        gameStateManagerType.GetProperty("SceneReloadOverride").SetValue(null, new Action<int, string>((_, _) => reloadRequests++));

        gameStateManagerType.GetMethod("ResetCurrentLevel").Invoke(manager, null);
        Assert.IsTrue((bool)gameStateManagerType.GetMethod("TryStartExecution").Invoke(manager, null));
        gameStateManagerType.GetMethod("ResetCurrentLevel").Invoke(manager, null);

        Assert.AreEqual(2, reloadRequests);
    }

    [Test]
    public void ResetLevelButton_InvokesResetEvenWhenInventoryIsNotEmpty()
    {
        Assert.IsNotNull(resetLevelButtonControllerType);

        ScriptableObject inventorySet = CreateInventorySetWithOneItem(1, out _);
        object manager = CreateGameStateManager();
        gameStateManagerType.GetMethod("SetFallbackInventorySet").Invoke(manager, new object[] { inventorySet });

        int reloadRequests = 0;
        gameStateManagerType.GetProperty("SceneReloadOverride").SetValue(null, new Action<int, string>((_, _) => reloadRequests++));

        resetButtonObject = new GameObject("ResetLevelButton", typeof(RectTransform), typeof(Button));
        Button button = resetButtonObject.GetComponent<Button>();
        Component resetButtonController = resetButtonObject.AddComponent(resetLevelButtonControllerType);
        resetLevelButtonControllerType
            .GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(resetButtonController, null);
        resetLevelButtonControllerType.GetMethod("SetGameStateManager").Invoke(resetButtonController, new object[] { manager });

        Assert.IsFalse((bool)gameStateManagerType.GetProperty("CanStartExecution").GetValue(manager));
        Assert.IsTrue(button.interactable);

        button.onClick.Invoke();

        Assert.AreEqual(1, reloadRequests);
    }

    [Test]
    public void BuildModePlacementController_CannotPlaceOnDiscoveredBlockedTilemap()
    {
        Assert.IsNotNull(buildModePlacementControllerType);

        GameObject placedRoot = new GameObject("PlacedObjectsRoot");
        GameObject controllerObject = new GameObject("BuildModePlacementController");
        GameObject prefab = new GameObject("BombPrefab");
        CreateGameStateManager();
        GameObject grid = CreatePlacementGrid(
            out Tilemap referenceTilemap,
            out Tilemap configuredBreakableTilemap,
            out Tilemap discoveredWallTilemap);
        Vector3Int blockedCell = new Vector3Int(2, 1, 0);
        discoveredWallTilemap.SetTile(blockedCell, ScriptableObject.CreateInstance<Tile>());
        Component controller = controllerObject.AddComponent(buildModePlacementControllerType);

        try
        {
            ScriptableObject definition = CreatePlaceableDefinitionWithPrefab(prefab);
            SetPrivateField(controller, "activeDefinition", definition);
            SetPrivateField(controller, "referenceTilemap", referenceTilemap);
            SetPrivateField(controller, "blockedTilemaps", new[] { configuredBreakableTilemap });

            buildModePlacementControllerType
                .GetMethod("ResolveSceneReferences", BindingFlags.NonPublic | BindingFlags.Instance)
                .Invoke(controller, null);

            bool canPlace = InvokeCanPlaceAt(controller, blockedCell);

            Assert.IsFalse(canPlace);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(controllerObject);
            UnityEngine.Object.DestroyImmediate(prefab);
            UnityEngine.Object.DestroyImmediate(grid);
            UnityEngine.Object.DestroyImmediate(placedRoot);
        }
    }

    [Test]
    public void BuildModePlacementController_CannotPlaceOnOccupiedColliderCell()
    {
        Assert.IsNotNull(buildModePlacementControllerType);

        GameObject placedRoot = new GameObject("PlacedObjectsRoot");
        GameObject controllerObject = new GameObject("BuildModePlacementController");
        GameObject prefab = new GameObject("BombPrefab");
        CreateGameStateManager();
        GameObject grid = CreatePlacementGrid(
            out Tilemap referenceTilemap,
            out _,
            out _);
        Vector3Int occupiedCell = new Vector3Int(3, 1, 0);
        GameObject occupyingObject = new GameObject("OccupyingObject", typeof(BoxCollider2D));
        occupyingObject.transform.position = referenceTilemap.GetCellCenterWorld(occupiedCell);
        Component controller = controllerObject.AddComponent(buildModePlacementControllerType);

        try
        {
            ScriptableObject definition = CreatePlaceableDefinitionWithPrefab(prefab);
            SetPrivateField(controller, "activeDefinition", definition);
            SetPrivateField(controller, "referenceTilemap", referenceTilemap);
            SetPrivateField(controller, "blockedTilemaps", Array.Empty<Tilemap>());
            Physics2D.SyncTransforms();

            bool canPlace = InvokeCanPlaceAt(controller, occupiedCell);

            Assert.IsFalse(canPlace);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(occupyingObject);
            UnityEngine.Object.DestroyImmediate(controllerObject);
            UnityEngine.Object.DestroyImmediate(prefab);
            UnityEngine.Object.DestroyImmediate(grid);
            UnityEngine.Object.DestroyImmediate(placedRoot);
        }
    }

    [Test]
    public void FallingDestructibleTilemapLayer_WhenExecution_ConvertsUnsupportedTileToFallingBlock()
    {
        Assert.IsNotNull(fallingDestructibleTilemapLayerType);
        Assert.IsNotNull(fallingTileBlockType);

        GameObject grid = CreatePlacementGrid(out Tilemap fallingTilemap, out _, out _);
        Vector3Int fallingCell = new Vector3Int(0, 1, 0);
        fallingTilemap.SetTile(fallingCell, ScriptableObject.CreateInstance<Tile>());
        Component layer = fallingTilemap.gameObject.AddComponent(fallingDestructibleTilemapLayerType);
        SetPrivateField(layer, "supportTilemaps", new[] { fallingTilemap });

        try
        {
            InvokeLevelPhase(layer, "Execution");

            Assert.IsFalse(fallingTilemap.HasTile(fallingCell));
            Assert.AreEqual(1, CountFallingBlocks());
        }
        finally
        {
            DestroyFallingBlocksRoot();
            UnityEngine.Object.DestroyImmediate(grid);
        }
    }

    [Test]
    public void FallingDestructibleTilemapLayer_WhenTileHasSupport_DoesNotFall()
    {
        Assert.IsNotNull(fallingDestructibleTilemapLayerType);

        GameObject grid = CreatePlacementGrid(out Tilemap fallingTilemap, out Tilemap supportTilemap, out _);
        Vector3Int fallingCell = new Vector3Int(0, 1, 0);
        Vector3Int supportCell = new Vector3Int(0, 0, 0);
        fallingTilemap.SetTile(fallingCell, ScriptableObject.CreateInstance<Tile>());
        supportTilemap.SetTile(supportCell, ScriptableObject.CreateInstance<Tile>());
        Component layer = fallingTilemap.gameObject.AddComponent(fallingDestructibleTilemapLayerType);
        SetPrivateField(layer, "supportTilemaps", new[] { supportTilemap });

        try
        {
            InvokeLevelPhase(layer, "Execution");

            Assert.IsTrue(fallingTilemap.HasTile(fallingCell));
            Assert.AreEqual(0, CountFallingBlocks());
        }
        finally
        {
            DestroyFallingBlocksRoot();
            UnityEngine.Object.DestroyImmediate(grid);
        }
    }

    [Test]
    public void FallingDestructibleTilemapLayer_WhenOnlyHazardTileIsBelow_ConvertsToFallingBlock()
    {
        Assert.IsNotNull(fallingDestructibleTilemapLayerType);
        Assert.IsNotNull(fallingTileBlockType);
        Assert.IsNotNull(hazardTilemapLayerType);

        GameObject grid = CreatePlacementGrid(out Tilemap fallingTilemap, out _, out _);
        Tilemap hazardTilemap = CreateChildTilemap(grid.transform, "Hazard Tilemap");
        hazardTilemap.gameObject.AddComponent(hazardTilemapLayerType);
        Vector3Int fallingCell = new Vector3Int(0, 1, 0);
        Vector3Int hazardCell = Vector3Int.zero;
        fallingTilemap.SetTile(fallingCell, ScriptableObject.CreateInstance<Tile>());
        hazardTilemap.SetTile(hazardCell, ScriptableObject.CreateInstance<Tile>());
        Component layer = fallingTilemap.gameObject.AddComponent(fallingDestructibleTilemapLayerType);
        SetPrivateField(layer, "supportTilemaps", new[] { hazardTilemap });

        try
        {
            InvokeLevelPhase(layer, "Execution");

            Assert.IsFalse(fallingTilemap.HasTile(fallingCell));
            Assert.IsTrue(hazardTilemap.HasTile(hazardCell));
            Assert.AreEqual(1, CountFallingBlocks());
        }
        finally
        {
            DestroyFallingBlocksRoot();
            UnityEngine.Object.DestroyImmediate(grid);
        }
    }

    [Test]
    public void FallingDestructibleTilemapLayer_AddsActiveHazardsAsFallingBlockLandingTargets()
    {
        Assert.IsNotNull(fallingDestructibleTilemapLayerType);
        Assert.IsNotNull(fallingTileBlockType);
        Assert.IsNotNull(hazardTilemapLayerType);

        GameObject grid = CreatePlacementGrid(out Tilemap fallingTilemap, out _, out _);
        Tilemap hazardTilemap = CreateChildTilemap(grid.transform, "Hazard Tilemap");
        hazardTilemap.gameObject.AddComponent(hazardTilemapLayerType);
        Vector3Int fallingCell = new Vector3Int(0, 2, 0);
        Vector3Int hazardCell = Vector3Int.zero;
        fallingTilemap.SetTile(fallingCell, ScriptableObject.CreateInstance<Tile>());
        hazardTilemap.SetTile(hazardCell, ScriptableObject.CreateInstance<Tile>());
        Component layer = fallingTilemap.gameObject.AddComponent(fallingDestructibleTilemapLayerType);
        SetPrivateField(layer, "supportTilemaps", new[] { fallingTilemap });

        try
        {
            InvokeLevelPhase(layer, "Execution");

            Component block = GetOnlyFallingBlock();
            GameObject blockObject = block.gameObject;
            Rigidbody2D body = blockObject.GetComponent<Rigidbody2D>();
            blockObject.transform.position = hazardTilemap.GetCellCenterWorld(hazardCell);
            body.linearVelocity = Vector2.down * 8f;
            Physics2D.SyncTransforms();

            fallingTileBlockType
                .GetMethod("FixedUpdate", BindingFlags.NonPublic | BindingFlags.Instance)
                .Invoke(block, null);

            Assert.AreEqual(RigidbodyType2D.Static, body.bodyType);
            Assert.AreEqual(hazardTilemap.GetCellCenterWorld(hazardCell).y + 1f, blockObject.transform.position.y, 0.001f);
        }
        finally
        {
            DestroyFallingBlocksRoot();
            UnityEngine.Object.DestroyImmediate(grid);
        }
    }

    [Test]
    public void FallingDestructibleTilemapLayer_UsesWorldPositionForSupportTilemaps()
    {
        Assert.IsNotNull(fallingDestructibleTilemapLayerType);

        GameObject fallingGrid = new GameObject("Falling Grid", typeof(Grid));
        Tilemap fallingTilemap = CreateChildTilemap(fallingGrid.transform, "Falling Tilemap");
        GameObject supportGrid = new GameObject("Support Grid", typeof(Grid));
        supportGrid.transform.position = new Vector3(3f, 0f, 0f);
        Tilemap supportTilemap = CreateChildTilemap(supportGrid.transform, "Support Tilemap");
        Vector3Int fallingCell = new Vector3Int(0, 1, 0);
        Vector3Int localSupportCell = supportTilemap.WorldToCell(fallingTilemap.GetCellCenterWorld(new Vector3Int(0, 0, 0)));
        fallingTilemap.SetTile(fallingCell, ScriptableObject.CreateInstance<Tile>());
        supportTilemap.SetTile(localSupportCell, ScriptableObject.CreateInstance<Tile>());
        Component layer = fallingTilemap.gameObject.AddComponent(fallingDestructibleTilemapLayerType);
        SetPrivateField(layer, "supportTilemaps", new[] { supportTilemap });

        try
        {
            InvokeLevelPhase(layer, "Execution");

            Assert.IsTrue(fallingTilemap.HasTile(fallingCell));
            Assert.AreEqual(0, CountFallingBlocks());
        }
        finally
        {
            DestroyFallingBlocksRoot();
            UnityEngine.Object.DestroyImmediate(supportGrid);
            UnityEngine.Object.DestroyImmediate(fallingGrid);
        }
    }

    [Test]
    public void FallingDestructibleTilemapLayer_WhenPlanning_DoesNotFall()
    {
        Assert.IsNotNull(fallingDestructibleTilemapLayerType);

        GameObject grid = CreatePlacementGrid(out Tilemap fallingTilemap, out _, out _);
        Vector3Int fallingCell = new Vector3Int(0, 1, 0);
        fallingTilemap.SetTile(fallingCell, ScriptableObject.CreateInstance<Tile>());
        Component layer = fallingTilemap.gameObject.AddComponent(fallingDestructibleTilemapLayerType);
        SetPrivateField(layer, "supportTilemaps", new[] { fallingTilemap });

        try
        {
            InvokeLevelPhase(layer, "Planning");

            Assert.IsTrue(fallingTilemap.HasTile(fallingCell));
            Assert.AreEqual(0, CountFallingBlocks());
        }
        finally
        {
            DestroyFallingBlocksRoot();
            UnityEngine.Object.DestroyImmediate(grid);
        }
    }

    [Test]
    public void FallingDestructibleTilemapLayer_WhenSupportTileIsDestroyed_ReevaluatesTileAbove()
    {
        Assert.IsNotNull(fallingDestructibleTilemapLayerType);
        Assert.IsNotNull(levelManagerType);

        GameObject grid = CreatePlacementGrid(out Tilemap fallingTilemap, out Tilemap supportTilemap, out _);
        Vector3Int fallingCell = new Vector3Int(0, 1, 0);
        Vector3Int supportCell = new Vector3Int(0, 0, 0);
        fallingTilemap.SetTile(fallingCell, ScriptableObject.CreateInstance<Tile>());
        supportTilemap.SetTile(supportCell, ScriptableObject.CreateInstance<Tile>());
        Component layer = fallingTilemap.gameObject.AddComponent(fallingDestructibleTilemapLayerType);
        SetPrivateField(layer, "supportTilemaps", new[] { supportTilemap });

        try
        {
            InvokeLevelPhase(layer, "Execution");
            Assert.IsTrue(fallingTilemap.HasTile(fallingCell));

            levelManagerType
                .GetMethod("TryDestroyTileAtCell", BindingFlags.Public | BindingFlags.Static)
                .Invoke(null, new object[] { supportTilemap, supportCell });

            Assert.IsFalse(fallingTilemap.HasTile(fallingCell));
            Assert.AreEqual(1, CountFallingBlocks());
        }
        finally
        {
            DestroyFallingBlocksRoot();
            UnityEngine.Object.DestroyImmediate(grid);
        }
    }

    [Test]
    public void FallingTileBlock_ImplementsBreakable()
    {
        Assert.IsNotNull(fallingTileBlockType);
        Assert.IsNotNull(breakableType);

        Assert.IsTrue(breakableType.IsAssignableFrom(fallingTileBlockType));
    }

    [Test]
    public void FallingTileBlock_LandsOnLogicalSupportTilemap()
    {
        Assert.IsNotNull(fallingTileBlockType);

        GameObject supportGrid = new GameObject("Support Grid", typeof(Grid));
        Tilemap supportTilemap = CreateChildTilemap(supportGrid.transform, "Support Tilemap");
        Vector3Int supportCell = Vector3Int.zero;
        supportTilemap.SetTile(supportCell, ScriptableObject.CreateInstance<Tile>());
        GameObject blockObject = new GameObject("FallingTileBlock");
        Component block = blockObject.AddComponent(fallingTileBlockType);
        blockObject.transform.position = supportTilemap.GetCellCenterWorld(new Vector3Int(0, 1, 0));

        try
        {
            fallingTileBlockType
                .GetMethod("Initialize")
                .Invoke(block, new object[]
                {
                    null,
                    Color.white,
                    Vector3.one,
                    1f,
                    true,
                    new[] { supportTilemap },
                    new LayerMask { value = 0 }
                });

            Rigidbody2D body = blockObject.GetComponent<Rigidbody2D>();
            body.linearVelocity = Vector2.down;
            Physics2D.SyncTransforms();

            fallingTileBlockType
                .GetMethod("FixedUpdate", BindingFlags.NonPublic | BindingFlags.Instance)
                .Invoke(block, null);

            Assert.AreEqual(RigidbodyType2D.Static, body.bodyType);
            Assert.AreEqual(1f, blockObject.transform.position.y, 0.001f);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(blockObject);
            UnityEngine.Object.DestroyImmediate(supportGrid);
        }
    }

    [Test]
    public void FallingTileBlock_WhenVelocityIsZero_DoesNotLandBeforePhysicsMovesIt()
    {
        Assert.IsNotNull(fallingTileBlockType);

        GameObject supportGrid = new GameObject("Support Grid", typeof(Grid));
        Tilemap supportTilemap = CreateChildTilemap(supportGrid.transform, "Support Tilemap");
        Vector3Int supportCell = Vector3Int.zero;
        supportTilemap.SetTile(supportCell, ScriptableObject.CreateInstance<Tile>());
        GameObject blockObject = new GameObject("FallingTileBlock");
        Component block = blockObject.AddComponent(fallingTileBlockType);
        blockObject.transform.position = supportTilemap.GetCellCenterWorld(new Vector3Int(0, 1, 0));

        try
        {
            fallingTileBlockType
                .GetMethod("Initialize")
                .Invoke(block, new object[]
                {
                    null,
                    Color.white,
                    Vector3.one,
                    1f,
                    true,
                    new[] { supportTilemap },
                    new LayerMask { value = 0 }
                });

            Rigidbody2D body = blockObject.GetComponent<Rigidbody2D>();
            body.linearVelocity = Vector2.zero;
            Physics2D.SyncTransforms();

            fallingTileBlockType
                .GetMethod("FixedUpdate", BindingFlags.NonPublic | BindingFlags.Instance)
                .Invoke(block, null);

            Assert.AreEqual(RigidbodyType2D.Dynamic, body.bodyType);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(blockObject);
            UnityEngine.Object.DestroyImmediate(supportGrid);
        }
    }

    [Test]
    public void FallingTileBlock_WhenItMovesPastLogicalSupport_LandsOnSupport()
    {
        Assert.IsNotNull(fallingTileBlockType);

        GameObject supportGrid = new GameObject("Support Grid", typeof(Grid));
        Tilemap supportTilemap = CreateChildTilemap(supportGrid.transform, "Support Tilemap");
        supportTilemap.SetTile(Vector3Int.zero, ScriptableObject.CreateInstance<Tile>());
        GameObject blockObject = new GameObject("FallingTileBlock");
        Component block = blockObject.AddComponent(fallingTileBlockType);
        blockObject.transform.position = supportTilemap.GetCellCenterWorld(new Vector3Int(0, 2, 0));

        try
        {
            fallingTileBlockType
                .GetMethod("Initialize")
                .Invoke(block, new object[]
                {
                    null,
                    Color.white,
                    Vector3.one,
                    1f,
                    true,
                    new[] { supportTilemap },
                    new LayerMask { value = 0 }
                });

            Rigidbody2D body = blockObject.GetComponent<Rigidbody2D>();
            blockObject.transform.position = supportTilemap.GetCellCenterWorld(new Vector3Int(0, 0, 0));
            body.linearVelocity = Vector2.down * 8f;
            Physics2D.SyncTransforms();

            fallingTileBlockType
                .GetMethod("FixedUpdate", BindingFlags.NonPublic | BindingFlags.Instance)
                .Invoke(block, null);

            Assert.AreEqual(RigidbodyType2D.Static, body.bodyType);
            Assert.AreEqual(1f, blockObject.transform.position.y, 0.001f);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(blockObject);
            UnityEngine.Object.DestroyImmediate(supportGrid);
        }
    }

    [Test]
    public void FallingTileBlock_WhenOnlyAdjacentSideTileIsInEdgeProbe_DoesNotLand()
    {
        Assert.IsNotNull(fallingTileBlockType);

        GameObject supportGrid = new GameObject("Support Grid", typeof(Grid));
        Tilemap supportTilemap = CreateChildTilemap(supportGrid.transform, "Support Tilemap");
        supportTilemap.SetTile(new Vector3Int(-1, 0, 0), ScriptableObject.CreateInstance<Tile>());
        GameObject blockObject = new GameObject("FallingTileBlock");
        Component block = blockObject.AddComponent(fallingTileBlockType);
        blockObject.transform.position = supportTilemap.GetCellCenterWorld(new Vector3Int(0, 1, 0)) + Vector3.left * 0.45f;

        try
        {
            InitializeFallingBlock(block, new[] { supportTilemap });
            Rigidbody2D body = blockObject.GetComponent<Rigidbody2D>();
            body.linearVelocity = Vector2.down;
            Physics2D.SyncTransforms();

            fallingTileBlockType
                .GetMethod("FixedUpdate", BindingFlags.NonPublic | BindingFlags.Instance)
                .Invoke(block, null);

            Assert.AreEqual(RigidbodyType2D.Dynamic, body.bodyType);
            Assert.IsTrue((body.constraints & RigidbodyConstraints2D.FreezePositionX) != 0);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(blockObject);
            UnityEngine.Object.DestroyImmediate(supportGrid);
        }
    }

    [Test]
    public void FallingTileBlock_IsTriggerWhileFallingAndSolidAfterLanding()
    {
        Assert.IsNotNull(fallingTileBlockType);

        GameObject supportGrid = new GameObject("Support Grid", typeof(Grid));
        Tilemap supportTilemap = CreateChildTilemap(supportGrid.transform, "Support Tilemap");
        supportTilemap.SetTile(Vector3Int.zero, ScriptableObject.CreateInstance<Tile>());
        GameObject blockObject = new GameObject("FallingTileBlock");
        Component block = blockObject.AddComponent(fallingTileBlockType);
        blockObject.transform.position = supportTilemap.GetCellCenterWorld(new Vector3Int(0, 1, 0));

        try
        {
            InitializeFallingBlock(block, new[] { supportTilemap });
            BoxCollider2D collider = blockObject.GetComponent<BoxCollider2D>();
            Rigidbody2D body = blockObject.GetComponent<Rigidbody2D>();

            Assert.IsTrue(collider.isTrigger);

            body.linearVelocity = Vector2.down;
            Physics2D.SyncTransforms();

            fallingTileBlockType
                .GetMethod("FixedUpdate", BindingFlags.NonPublic | BindingFlags.Instance)
                .Invoke(block, null);

            Assert.AreEqual(RigidbodyType2D.Static, body.bodyType);
            Assert.IsFalse(collider.isTrigger);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(blockObject);
            UnityEngine.Object.DestroyImmediate(supportGrid);
        }
    }

    [Test]
    public void FallingTileBlock_WhenLandingOnHazardTilemap_RestsAboveHazardCell()
    {
        Assert.IsNotNull(fallingTileBlockType);
        Assert.IsNotNull(hazardTilemapLayerType);

        GameObject supportGrid = new GameObject("Support Grid", typeof(Grid));
        Tilemap hazardTilemap = CreateChildTilemap(supportGrid.transform, "Hazard Tilemap");
        hazardTilemap.gameObject.AddComponent(hazardTilemapLayerType);
        Vector3Int hazardCell = Vector3Int.zero;
        hazardTilemap.SetTile(hazardCell, ScriptableObject.CreateInstance<Tile>());
        GameObject blockObject = new GameObject("FallingTileBlock");
        Component block = blockObject.AddComponent(fallingTileBlockType);
        blockObject.transform.position = hazardTilemap.GetCellCenterWorld(new Vector3Int(0, 2, 0));

        try
        {
            InitializeFallingBlock(block, new[] { hazardTilemap });
            Rigidbody2D body = blockObject.GetComponent<Rigidbody2D>();
            blockObject.transform.position = hazardTilemap.GetCellCenterWorld(hazardCell);
            body.linearVelocity = Vector2.down * 8f;
            Physics2D.SyncTransforms();

            fallingTileBlockType
                .GetMethod("FixedUpdate", BindingFlags.NonPublic | BindingFlags.Instance)
                .Invoke(block, null);

            Assert.AreEqual(RigidbodyType2D.Static, body.bodyType);
            Assert.AreEqual(hazardTilemap.GetCellCenterWorld(hazardCell).y + 1f, blockObject.transform.position.y, 0.001f);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(blockObject);
            UnityEngine.Object.DestroyImmediate(supportGrid);
        }
    }

    [Test]
    public void FallingTileBlock_WhenFallingOntoDuck_KillsDuck()
    {
        Assert.IsNotNull(fallingTileBlockType);
        Assert.IsNotNull(playerDuckControllerType);

        GameObject blockObject = new GameObject("FallingTileBlock");
        Component block = blockObject.AddComponent(fallingTileBlockType);
        blockObject.transform.position = new Vector3(0f, 2f, 0f);
        GameObject duckObject = CreateTestDuck(new Vector3(0f, 0.8f, 0f));

        try
        {
            InitializeFallingBlock(block, new Tilemap[0]);
            Rigidbody2D body = blockObject.GetComponent<Rigidbody2D>();
            blockObject.transform.position = new Vector3(0f, 1.2f, 0f);
            body.linearVelocity = Vector2.down * 8f;
            Physics2D.SyncTransforms();

            fallingTileBlockType
                .GetMethod("FixedUpdate", BindingFlags.NonPublic | BindingFlags.Instance)
                .Invoke(block, null);

            Assert.IsTrue(IsDuckDead(duckObject));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(duckObject);
            UnityEngine.Object.DestroyImmediate(blockObject);
        }
    }

    [UnityTest]
    public IEnumerator FallingTileBlock_WhenFallingOntoRat_BreaksRat()
    {
        Assert.IsNotNull(fallingTileBlockType);
        Assert.IsNotNull(enemyRatControllerType);

        GameObject blockObject = new GameObject("FallingTileBlock");
        Component block = blockObject.AddComponent(fallingTileBlockType);
        blockObject.transform.position = new Vector3(0f, 2f, 0f);
        GameObject ratObject = CreateTestRat(new Vector3(0f, 0.8f, 0f));

        try
        {
            InitializeFallingBlock(block, new Tilemap[0]);
            Rigidbody2D body = blockObject.GetComponent<Rigidbody2D>();
            blockObject.transform.position = new Vector3(0f, 1.2f, 0f);
            body.linearVelocity = Vector2.down * 8f;
            Physics2D.SyncTransforms();

            fallingTileBlockType
                .GetMethod("FixedUpdate", BindingFlags.NonPublic | BindingFlags.Instance)
                .Invoke(block, null);

            yield return null;

            Assert.IsTrue(ratObject == null);
        }
        finally
        {
            if (ratObject != null)
            {
                UnityEngine.Object.DestroyImmediate(ratObject);
            }

            UnityEngine.Object.DestroyImmediate(blockObject);
        }
    }

    [Test]
    public void FallingTileBlock_WhenStaticAndTouchingDuck_DoesNotKillDuck()
    {
        Assert.IsNotNull(fallingTileBlockType);
        Assert.IsNotNull(playerDuckControllerType);

        GameObject blockObject = new GameObject("FallingTileBlock");
        Component block = blockObject.AddComponent(fallingTileBlockType);
        blockObject.transform.position = new Vector3(0f, 1.2f, 0f);
        GameObject duckObject = CreateTestDuck(new Vector3(0f, 0.8f, 0f));

        try
        {
            InitializeFallingBlock(block, new Tilemap[0]);
            Rigidbody2D body = blockObject.GetComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Static;
            Physics2D.SyncTransforms();

            fallingTileBlockType
                .GetMethod("FixedUpdate", BindingFlags.NonPublic | BindingFlags.Instance)
                .Invoke(block, null);

            Assert.IsFalse(IsDuckDead(duckObject));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(duckObject);
            UnityEngine.Object.DestroyImmediate(blockObject);
        }
    }

    [Test]
    public void FallingTileBlock_WhenNotFallingDownAndTouchingDuck_DoesNotKillDuck()
    {
        Assert.IsNotNull(fallingTileBlockType);
        Assert.IsNotNull(playerDuckControllerType);

        GameObject blockObject = new GameObject("FallingTileBlock");
        Component block = blockObject.AddComponent(fallingTileBlockType);
        blockObject.transform.position = new Vector3(0f, 1.2f, 0f);
        GameObject duckObject = CreateTestDuck(new Vector3(0f, 0.8f, 0f));

        try
        {
            InitializeFallingBlock(block, new Tilemap[0]);
            Rigidbody2D body = blockObject.GetComponent<Rigidbody2D>();
            body.linearVelocity = Vector2.zero;
            Physics2D.SyncTransforms();

            fallingTileBlockType
                .GetMethod("FixedUpdate", BindingFlags.NonPublic | BindingFlags.Instance)
                .Invoke(block, null);

            Assert.IsFalse(IsDuckDead(duckObject));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(duckObject);
            UnityEngine.Object.DestroyImmediate(blockObject);
        }
    }

    private object CreateGameStateManager()
    {
        gameStateObject = new GameObject("GameStateManager");
        return gameStateObject.AddComponent(gameStateManagerType);
    }

    private ScriptableObject CreateInventorySetWithOneItem(int amount, out object authoredEntry)
    {
        return CreateInventorySetWithOneItem(amount, GetUseMode("DragToPlace"), out authoredEntry);
    }

    private ScriptableObject CreateInventorySetWithOneItem(int amount, object useMode, out object authoredEntry)
    {
        Assert.IsNotNull(placeableDefinitionType);
        Assert.IsNotNull(placeableUseModeType);
        Assert.IsNotNull(inventoryEntryType);
        Assert.IsNotNull(inventorySetType);
        Assert.IsNotNull(runtimeInventoryType);

        ScriptableObject definition = ScriptableObject.CreateInstance(placeableDefinitionType);
        SetPrivateField(definition, "useMode", useMode);
        authoredEntry = Activator.CreateInstance(inventoryEntryType);
        SetPrivateField(authoredEntry, "definition", definition);
        SetPrivateField(authoredEntry, "amount", amount);

        ScriptableObject inventorySet = ScriptableObject.CreateInstance(inventorySetType);
        IList entries = (IList)inventorySetType.GetField("entries", BindingFlags.NonPublic | BindingFlags.Instance)
            .GetValue(inventorySet);
        entries.Add(authoredEntry);
        return inventorySet;
    }

    private ScriptableObject CreateLevelDefinitionWithPlanningTime(float planningTimeLimitSeconds)
    {
        Assert.IsNotNull(levelDefinitionType);
        ScriptableObject levelDefinition = ScriptableObject.CreateInstance(levelDefinitionType);
        SetPrivateField(levelDefinition, "planningTimeLimitSeconds", planningTimeLimitSeconds);
        return levelDefinition;
    }

    private void InvokePlanningTimerTick(object manager, float deltaSeconds)
    {
        gameStateManagerType
            .GetMethod("TickPlanningTimer", BindingFlags.NonPublic | BindingFlags.Instance)
            .Invoke(manager, new object[] { deltaSeconds });
    }

    private void RegisterPlanningTimeoutHandler(Func<string, bool> handler)
    {
        PropertyInfo handlerProperty = gameStateManagerType.GetProperty("PlanningTimeoutHandler");
        Assert.IsNotNull(handlerProperty);

        Delegate handlerDelegate = Delegate.CreateDelegate(handlerProperty.PropertyType, handler.Target, handler.Method);
        handlerProperty.SetValue(null, handlerDelegate);
    }

    private object CreateLevelManagerWithPickaxe(
        int amount,
        out Component levelManager,
        out Tilemap testTilemap,
        out object runtimeEntry)
    {
        ScriptableObject inventorySet = CreateInventorySetWithOneItem(
            amount,
            GetUseMode("ExecutionClickToDestroyTile"),
            out _);
        object manager = CreateGameStateManager();
        gameStateManagerType.GetMethod("SetFallbackInventorySet").Invoke(manager, new object[] { inventorySet });

        gridObject = new GameObject("Grid", typeof(Grid));
        tilemapObject = new GameObject("Tilemap", typeof(Tilemap), typeof(TilemapRenderer));
        tilemapObject.transform.SetParent(gridObject.transform);
        testTilemap = tilemapObject.GetComponent<Tilemap>();

        GameObject levelManagerObject = new GameObject("LevelManager");
        levelManagerObject.transform.SetParent(gridObject.transform);
        levelManager = levelManagerObject.AddComponent(levelManagerType);
        SetPrivateField(levelManager, "tilemap", testTilemap);
        SetPrivateField(levelManager, "gameStateManager", manager);

        object runtimeInventory = gameStateManagerType.GetProperty("Inventory").GetValue(manager);
        runtimeEntry = GetFirstRuntimeEntry(runtimeInventory);
        return manager;
    }

    private object GetUseMode(string name)
    {
        Assert.IsNotNull(placeableUseModeType);
        return Enum.Parse(placeableUseModeType, name);
    }

    private object GetFirstRuntimeEntry(object runtimeInventory)
    {
        IEnumerable entries = (IEnumerable)runtimeInventoryType.GetProperty("Entries").GetValue(runtimeInventory);

        foreach (object entry in entries)
        {
            return entry;
        }

        Assert.Fail("Expected at least one runtime inventory entry.");
        return null;
    }

    private GameObject CreatePlacementGrid(
        out Tilemap referenceTilemap,
        out Tilemap breakableTilemap,
        out Tilemap wallTilemap)
    {
        gridObject = new GameObject("Grid", typeof(Grid));
        referenceTilemap = CreateChildTilemap(gridObject.transform, "Reference Tilemap");
        breakableTilemap = CreateChildTilemap(gridObject.transform, "Breakable Tilemap");
        wallTilemap = CreateChildTilemap(gridObject.transform, "Walls Tilemap");
        return gridObject;
    }

    private static Tilemap CreateChildTilemap(Transform parent, string name)
    {
        GameObject tilemapObject = new GameObject(name, typeof(Tilemap), typeof(TilemapRenderer));
        tilemapObject.transform.SetParent(parent);
        return tilemapObject.GetComponent<Tilemap>();
    }

    private ScriptableObject CreatePlaceableDefinitionWithPrefab(GameObject prefab)
    {
        ScriptableObject definition = ScriptableObject.CreateInstance(placeableDefinitionType);
        SetPrivateField(definition, "prefab", prefab);
        return definition;
    }

    private bool InvokeCanPlaceAt(Component controller, Vector3Int cell)
    {
        return (bool)buildModePlacementControllerType
            .GetMethod("CanPlaceAt", BindingFlags.NonPublic | BindingFlags.Instance)
            .Invoke(controller, new object[] { cell });
    }

    private void InvokeLevelPhase(Component listener, string phaseName)
    {
        Type levelPhaseType = Type.GetType("LevelPhase, UnluckyDucky.Core");
        Assert.IsNotNull(levelPhaseType);
        object phase = Enum.Parse(levelPhaseType, phaseName);

        listener.GetType()
            .GetMethod("OnLevelPhaseChanged", BindingFlags.Public | BindingFlags.Instance)
            .Invoke(listener, new[] { phase });
    }

    private int CountFallingBlocks()
    {
        UnityEngine.Object[] blocks = UnityEngine.Object.FindObjectsByType(fallingTileBlockType, FindObjectsSortMode.None);
        return blocks.Length;
    }

    private Component GetOnlyFallingBlock()
    {
        UnityEngine.Object[] blocks = UnityEngine.Object.FindObjectsByType(fallingTileBlockType, FindObjectsSortMode.None);
        Assert.AreEqual(1, blocks.Length);
        return (Component)blocks[0];
    }

    private void InitializeFallingBlock(Component block, Tilemap[] supportTilemaps)
    {
        fallingTileBlockType
            .GetMethod("Initialize")
            .Invoke(block, new object[]
            {
                null,
                Color.white,
                Vector3.one,
                1f,
                true,
                supportTilemaps,
                new LayerMask { value = 0 }
            });
    }

    private GameObject CreateTestDuck(Vector3 position)
    {
        GameObject duckObject = new GameObject("Duck", typeof(Rigidbody2D), typeof(BoxCollider2D));
        duckObject.transform.position = position;
        BoxCollider2D collider = duckObject.GetComponent<BoxCollider2D>();
        collider.size = new Vector2(0.4f, 0.3f);
        Component duck = duckObject.AddComponent(playerDuckControllerType);
        SetPrivateField(duck, "resetLevelOnDeath", false);
        return duckObject;
    }

    private GameObject CreateTestRat(Vector3 position)
    {
        GameObject ratObject = new GameObject("Rat", typeof(Rigidbody2D), typeof(BoxCollider2D));
        ratObject.transform.position = position;
        BoxCollider2D collider = ratObject.GetComponent<BoxCollider2D>();
        collider.size = new Vector2(0.4f, 0.3f);
        ratObject.AddComponent(enemyRatControllerType);
        return ratObject;
    }

    private bool IsDuckDead(GameObject duckObject)
    {
        Component duck = duckObject.GetComponent(playerDuckControllerType);
        return (bool)playerDuckControllerType.GetProperty("IsDead").GetValue(duck);
    }

    private void RegisterDefeatScreenHandler()
    {
        MethodInfo handlerMethod = defeatScreenManagerType.GetMethod(
            "ShowForPlayerDeath",
            BindingFlags.Public | BindingFlags.Static);
        Assert.IsNotNull(handlerMethod);

        PropertyInfo handlerProperty = playerDuckControllerType.GetProperty("DeathScreenHandler");
        Assert.IsNotNull(handlerProperty);

        Delegate handler = Delegate.CreateDelegate(handlerProperty.PropertyType, handlerMethod);
        handlerProperty.SetValue(null, handler);
    }

    private GameObject InstantiateUiPrefab(string path)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        Assert.IsNotNull(prefab, $"Missing UI prefab at {path}.");
        uiPrefabObject = UnityEngine.Object.Instantiate(prefab);
        return uiPrefabObject;
    }

    private static void DestroyFallingBlocksRoot()
    {
        GameObject root = GameObject.Find("FallingBlocksRoot");

        if (root != null)
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        target.GetType()
            .GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance)
            .SetValue(target, value);
    }

    private static object GetProperty(object target, string propertyName)
    {
        return target.GetType()
            .GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance)
            .GetValue(target);
    }

    private static void DestroyObjectNamed(string objectName)
    {
        GameObject gameObject = GameObject.Find(objectName);

        if (gameObject != null)
        {
            UnityEngine.Object.DestroyImmediate(gameObject);
        }
    }
}
