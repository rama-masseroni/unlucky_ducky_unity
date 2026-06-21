using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class SensorInteractionTests
{
    private readonly Type gameStateManagerType = Type.GetType("GameStateManager, UnluckyDucky.Core");
    private readonly Type sensorControllerType = Type.GetType("SensorController, Assembly-CSharp");
    private readonly Type sensorDoorControllerType = Type.GetType("SensorDoorController, Assembly-CSharp");
    private readonly Type gridWalkerControllerType = Type.GetType("GridWalkerController, UnluckyDucky.Player");

    private GameObject gameStateObject;
    private GameObject sensorObject;
    private GameObject walkerObject;
    private GameObject otherObject;
    private GameObject doorObject;

    [TearDown]
    public void TearDown()
    {
        DestroyIfExists(doorObject);
        DestroyIfExists(otherObject);
        DestroyIfExists(walkerObject);
        DestroyIfExists(sensorObject);
        DestroyIfExists(gameStateObject);
        LogAssert.NoUnexpectedReceived();
    }

    [Test]
    public void SensorController_WhenExecutionAndGridWalkerActivates_LogsActivation()
    {
        Component sensor = CreateSensor();
        Collider2D activatorCollider = CreateWalker("Duck").GetComponent<Collider2D>();
        CreateGameStateManagerAndStartExecution();

        LogAssert.Expect(LogType.Log, "Sensor 'Sensor' activated by 'Duck'.");
        LogAssert.Expect(LogType.Warning, "Sensor 'Sensor' has no receivers connected to 'A'.");

        bool activated = InvokeTryActivate(sensor, activatorCollider);

        Assert.IsTrue(activated);
    }

    [Test]
    public void SensorController_WhenPlanning_DoesNotActivate()
    {
        Component sensor = CreateSensor();
        Collider2D activatorCollider = CreateWalker("Duck").GetComponent<Collider2D>();
        CreateGameStateManager();

        bool activated = InvokeTryActivate(sensor, activatorCollider);

        Assert.IsFalse(activated);
    }

    [Test]
    public void SensorController_WhenColliderHasNoGridWalker_DoesNotActivate()
    {
        Component sensor = CreateSensor();
        otherObject = new GameObject("StaticBlock", typeof(BoxCollider2D));
        CreateGameStateManagerAndStartExecution();

        bool activated = InvokeTryActivate(sensor, otherObject.GetComponent<Collider2D>());

        Assert.IsFalse(activated);
    }

    [Test]
    public void SensorDoorController_WhenConnectedExplicitly_TogglesOpenClosed()
    {
        Component sensor = CreateSensor();
        Component door = CreateDoor(startsOpen: false);
        Collider2D doorCollider = doorObject.GetComponent<Collider2D>();
        Collider2D activatorCollider = CreateWalker("Duck").GetComponent<Collider2D>();
        SetPrivateField(sensor, "connectedReceivers", new[] { door });
        CreateGameStateManagerAndStartExecution();

        Assert.IsFalse((bool)sensorDoorControllerType.GetProperty("IsOpen").GetValue(door));
        Assert.IsTrue(doorCollider.enabled);

        LogAssert.Expect(LogType.Log, "Sensor 'Sensor' activated by 'Duck'.");
        Assert.IsTrue(InvokeTryActivate(sensor, activatorCollider));
        Assert.IsTrue((bool)sensorDoorControllerType.GetProperty("IsOpen").GetValue(door));
        Assert.IsFalse(doorCollider.enabled);

        LogAssert.Expect(LogType.Log, "Sensor 'Sensor' activated by 'Duck'.");
        Assert.IsTrue(InvokeTryActivate(sensor, activatorCollider));
        Assert.IsFalse((bool)sensorDoorControllerType.GetProperty("IsOpen").GetValue(door));
        Assert.IsTrue(doorCollider.enabled);
    }

    [Test]
    public void SensorDoorController_WhenConnectionIdMatches_AutoDiscoversAndTogglesDoor()
    {
        Component sensor = CreateSensor();
        Component door = CreateDoor(startsOpen: false);
        Collider2D doorCollider = doorObject.GetComponent<Collider2D>();
        Collider2D activatorCollider = CreateWalker("Duck").GetComponent<Collider2D>();
        CreateGameStateManagerAndStartExecution();

        LogAssert.Expect(LogType.Log, "Sensor 'Sensor' activated by 'Duck'.");
        Assert.IsTrue(InvokeTryActivate(sensor, activatorCollider));

        Assert.IsTrue((bool)sensorDoorControllerType.GetProperty("IsOpen").GetValue(door));
        Assert.IsFalse(doorCollider.enabled);
    }

    [Test]
    public void SensorDoorController_WhenConnectionIdDoesNotMatch_DoesNotToggleDoor()
    {
        Component sensor = CreateSensor();
        Component door = CreateDoor(startsOpen: false);
        Collider2D doorCollider = doorObject.GetComponent<Collider2D>();
        Collider2D activatorCollider = CreateWalker("Duck").GetComponent<Collider2D>();
        SetPrivateField(door, "sensorConnectionId", "B");
        CreateGameStateManagerAndStartExecution();

        LogAssert.Expect(LogType.Log, "Sensor 'Sensor' activated by 'Duck'.");
        LogAssert.Expect(LogType.Warning, "Sensor 'Sensor' has no receivers connected to 'A'.");
        Assert.IsTrue(InvokeTryActivate(sensor, activatorCollider));

        Assert.IsFalse((bool)sensorDoorControllerType.GetProperty("IsOpen").GetValue(door));
        Assert.IsTrue(doorCollider.enabled);
    }

    private Component CreateSensor()
    {
        Assert.IsNotNull(sensorControllerType);
        sensorObject = new GameObject("Sensor", typeof(BoxCollider2D));
        return sensorObject.AddComponent(sensorControllerType);
    }

    private GameObject CreateWalker(string name)
    {
        Assert.IsNotNull(gridWalkerControllerType);
        walkerObject = new GameObject(name, typeof(Rigidbody2D), typeof(BoxCollider2D));
        walkerObject.AddComponent(gridWalkerControllerType);
        return walkerObject;
    }

    private Component CreateDoor(bool startsOpen)
    {
        Assert.IsNotNull(sensorDoorControllerType);
        doorObject = new GameObject("Door", typeof(BoxCollider2D));
        Component door = doorObject.AddComponent(sensorDoorControllerType);
        SetPrivateField(door, "startsOpen", startsOpen);
        sensorDoorControllerType
            .GetMethod("Awake", BindingFlags.NonPublic | BindingFlags.Instance)
            .Invoke(door, null);
        return door;
    }

    private object CreateGameStateManager()
    {
        Assert.IsNotNull(gameStateManagerType);
        gameStateObject = new GameObject("GameStateManager");
        return gameStateObject.AddComponent(gameStateManagerType);
    }

    private void CreateGameStateManagerAndStartExecution()
    {
        object gameStateManager = CreateGameStateManager();
        bool started = (bool)gameStateManagerType.GetMethod("TryStartExecution").Invoke(gameStateManager, null);
        Assert.IsTrue(started);
    }

    private bool InvokeTryActivate(Component sensor, Collider2D activatorCollider)
    {
        return (bool)sensorControllerType
            .GetMethod("TryActivate", BindingFlags.Public | BindingFlags.Instance)
            .Invoke(sensor, new object[] { activatorCollider });
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
