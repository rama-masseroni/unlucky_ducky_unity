using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;

public class PlacedPlaceableInstanceClickTests
{
    private readonly Type placedPlaceableInstanceType = Type.GetType("PlacedPlaceableInstance, Assembly-CSharp");
    private readonly Type buildModePlacementControllerType = Type.GetType("BuildModePlacementController, Assembly-CSharp");
    private readonly Type gameStateManagerType = Type.GetType("GameStateManager, UnluckyDucky.Core");
    private readonly Type gridWalkerControllerType = Type.GetType("GridWalkerController, UnluckyDucky.Player");

    private GameObject gameStateObject;
    private GameObject placementControllerObject;
    private GameObject placedObject;
    private GameObject placedObjectsRoot;

    [TearDown]
    public void TearDown()
    {
        DestroyIfExists(placedObject);
        DestroyIfExists(placementControllerObject);
        DestroyIfExists(placedObjectsRoot);
        DestroyIfExists(gameStateObject);

        GameObject discoveredRoot = GameObject.Find("PlacedObjectsRoot");
        DestroyIfExists(discoveredRoot);
    }

    [Test]
    public void PlacedPlaceableInstance_WhenClickedInPlanning_TogglesGridWalkerDirection()
    {
        Component gameStateManager = CreateGameStateManager();
        Component placementController = CreatePlacementController(gameStateManager);
        Component placedInstance = CreatePlacedGridWalkerInstance(placementController, out Component walker);

        Assert.AreEqual(1, GetFacingDirection(walker));

        InvokePointerClick(placedInstance);

        Assert.AreEqual(-1, GetFacingDirection(walker));
    }

    [Test]
    public void PlacedPlaceableInstance_WhenClickedInExecution_DoesNotToggleGridWalkerDirection()
    {
        Component gameStateManager = CreateGameStateManager();
        Component placementController = CreatePlacementController(gameStateManager);
        Component placedInstance = CreatePlacedGridWalkerInstance(placementController, out Component walker);

        bool started = (bool)gameStateManagerType.GetMethod("TryStartExecution").Invoke(gameStateManager, null);
        Assert.IsTrue(started);

        InvokePointerClick(placedInstance);

        Assert.AreEqual(1, GetFacingDirection(walker));
    }

    private Component CreateGameStateManager()
    {
        Assert.IsNotNull(gameStateManagerType);
        gameStateObject = new GameObject("GameStateManager");
        return gameStateObject.AddComponent(gameStateManagerType);
    }

    private Component CreatePlacementController(Component gameStateManager)
    {
        Assert.IsNotNull(buildModePlacementControllerType);
        placementControllerObject = new GameObject("BuildModePlacementController");
        Component placementController = placementControllerObject.AddComponent(buildModePlacementControllerType);
        SetPrivateField(placementController, "gameStateManager", gameStateManager);
        placedObjectsRoot = GameObject.Find("PlacedObjectsRoot");
        return placementController;
    }

    private Component CreatePlacedGridWalkerInstance(Component placementController, out Component walker)
    {
        Assert.IsNotNull(placedPlaceableInstanceType);
        Assert.IsNotNull(gridWalkerControllerType);
        placedObject = new GameObject("PlacedRat", typeof(Rigidbody2D), typeof(BoxCollider2D));
        walker = placedObject.AddComponent(gridWalkerControllerType);
        Component placedInstance = placedObject.AddComponent(placedPlaceableInstanceType);
        SetPrivateField(placedInstance, "placementController", placementController);
        return placedInstance;
    }

    private static void InvokePointerClick(Component placedInstance)
    {
        PointerEventData eventData = new PointerEventData(null)
        {
            button = PointerEventData.InputButton.Left
        };

        placedInstance.GetType()
            .GetMethod("OnPointerClick", BindingFlags.Public | BindingFlags.Instance)
            .Invoke(placedInstance, new object[] { eventData });
    }

    private static int GetFacingDirection(Component walker)
    {
        return (int)walker.GetType()
            .GetProperty("FacingDirection", BindingFlags.Public | BindingFlags.Instance)
            .GetValue(walker);
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        target.GetType()
            .GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance)
            .SetValue(target, value);
    }

    private static void DestroyIfExists(GameObject gameObject)
    {
        if (gameObject != null)
        {
            UnityEngine.Object.DestroyImmediate(gameObject);
        }
    }
}
