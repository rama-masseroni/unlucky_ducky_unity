using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;
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
    private readonly Type inventoryEntryType = Type.GetType("PlaceableInventoryEntry, UnluckyDucky.Core");
    private readonly Type inventorySetType = Type.GetType("PlaceableInventorySet, UnluckyDucky.Core");
    private readonly Type runtimeInventoryType = Type.GetType("PlaceableInventoryRuntime, UnluckyDucky.Core");
    private readonly Type gameStateManagerType = Type.GetType("GameStateManager, UnluckyDucky.Core");
    private readonly Type bombControllerType = Type.GetType("BombController, Assembly-CSharp");
    private readonly Type levelDefinitionType = Type.GetType("LevelDefinition, UnluckyDucky.Core");
    private readonly Type goalPointControllerType = Type.GetType("GoalPointController, Assembly-CSharp");
    private readonly Type playerDuckControllerType = Type.GetType("PlayerDuckController, UnluckyDucky.Player");
    private readonly Type resetLevelButtonControllerType = Type.GetType("ResetLevelButtonController, Assembly-CSharp");

    private GameObject gameStateObject;
    private GameObject duckObject;
    private GameObject goalObject;
    private GameObject resetButtonObject;

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

        goalPointControllerType?.GetProperty("SceneLoadOverride").SetValue(null, null);
        gameStateManagerType?.GetProperty("SceneReloadOverride").SetValue(null, null);
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

        bool completedInExecution = (bool)goalPointControllerType
            .GetMethod("TryCompleteLevel")
            .Invoke(goal, new object[] { duck });

        Assert.IsTrue(completedInExecution);
        Assert.AreEqual("Level_02_TestEmpty", requestedScene);
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
        resetLevelButtonControllerType.GetMethod("SetGameStateManager").Invoke(resetButtonController, new object[] { manager });

        Assert.IsFalse((bool)gameStateManagerType.GetProperty("CanStartExecution").GetValue(manager));
        Assert.IsTrue(button.interactable);

        button.onClick.Invoke();

        Assert.AreEqual(1, reloadRequests);
    }

    private object CreateGameStateManager()
    {
        gameStateObject = new GameObject("GameStateManager");
        return gameStateObject.AddComponent(gameStateManagerType);
    }

    private ScriptableObject CreateInventorySetWithOneItem(int amount, out object authoredEntry)
    {
        Assert.IsNotNull(placeableDefinitionType);
        Assert.IsNotNull(inventoryEntryType);
        Assert.IsNotNull(inventorySetType);
        Assert.IsNotNull(runtimeInventoryType);

        ScriptableObject definition = ScriptableObject.CreateInstance(placeableDefinitionType);
        authoredEntry = Activator.CreateInstance(inventoryEntryType);
        SetPrivateField(authoredEntry, "definition", definition);
        SetPrivateField(authoredEntry, "amount", amount);

        ScriptableObject inventorySet = ScriptableObject.CreateInstance(inventorySetType);
        IList entries = (IList)inventorySetType.GetField("entries", BindingFlags.NonPublic | BindingFlags.Instance)
            .GetValue(inventorySet);
        entries.Add(authoredEntry);
        return inventorySet;
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

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        target.GetType()
            .GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance)
            .SetValue(target, value);
    }
}
