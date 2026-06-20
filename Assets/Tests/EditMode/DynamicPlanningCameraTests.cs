using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class DynamicPlanningCameraTests
{
    private readonly Type cameraMathType = Type.GetType("DynamicPlanningCameraMath, Assembly-CSharp");
    private readonly Type placementControllerType = Type.GetType("BuildModePlacementController, Assembly-CSharp");
    private readonly Type placeableDefinitionType = Type.GetType("PlaceableDefinition, UnluckyDucky.Core");

    private GameObject placementObject;
    private ScriptableObject placeableDefinition;

    [TearDown]
    public void TearDown()
    {
        if (placementObject != null)
        {
            UnityEngine.Object.DestroyImmediate(placementObject);
        }

        if (placeableDefinition != null)
        {
            UnityEngine.Object.DestroyImmediate(placeableDefinition);
        }

        GameObject gameStateObject = GameObject.Find("GameStateManager");

        if (gameStateObject != null)
        {
            UnityEngine.Object.DestroyImmediate(gameStateObject);
        }
    }

    [Test]
    public void CalculatePlanningOrthographicSize_UsesSixtyPercentOfFullView()
    {
        Assert.IsNotNull(cameraMathType);

        float planningSize = (float)InvokeMath(
            "CalculatePlanningOrthographicSize",
            new object[] { 10f, 0.6f },
            typeof(float),
            typeof(float));

        Assert.AreEqual(6f, planningSize, 0.001f);
    }

    [Test]
    public void ClampCameraCenter_KeepsPlanningViewInsideFullView()
    {
        Assert.IsNotNull(cameraMathType);

        Vector2 clampedCenter = (Vector2)InvokeMath(
            "ClampCameraCenter",
            new object[]
            {
                new Vector2(9f, -9f),
                Vector2.zero,
                new Vector2(10f, 10f),
                new Vector2(4f, 4f)
            },
            typeof(Vector2),
            typeof(Vector2),
            typeof(Vector2),
            typeof(Vector2));

        Assert.AreEqual(6f, clampedCenter.x, 0.001f);
        Assert.AreEqual(-6f, clampedCenter.y, 0.001f);
    }

    [Test]
    public void ResolveEdgeScrollDirection_ReturnsNormalizedDiagonalDirection()
    {
        Assert.IsNotNull(cameraMathType);

        Vector2 direction = (Vector2)InvokeMath(
            "ResolveEdgeScrollDirection",
            new object[] { new Vector2(1919f, 1f), new Vector2(1920f, 1080f), 64f },
            typeof(Vector2),
            typeof(Vector2),
            typeof(float));

        Assert.AreEqual(1f, direction.magnitude, 0.001f);
        Assert.Greater(direction.x, 0f);
        Assert.Less(direction.y, 0f);
    }

    [Test]
    public void ResolveEdgeScrollDirection_WhenCursorIsAwayFromEdges_ReturnsZero()
    {
        Assert.IsNotNull(cameraMathType);

        Vector2 direction = (Vector2)InvokeMath(
            "ResolveEdgeScrollDirection",
            new object[] { new Vector2(960f, 540f), new Vector2(1920f, 1080f), 64f },
            typeof(Vector2),
            typeof(Vector2),
            typeof(float));

        Assert.AreEqual(Vector2.zero, direction);
    }

    [Test]
    public void CalculateViewportRelativePanDistance_ScalesWithCurrentCameraHeight()
    {
        Assert.IsNotNull(cameraMathType);

        float panDistance = (float)InvokeMath(
            "CalculateViewportRelativePanDistance",
            new object[] { 5f, 0.5f, 1f },
            typeof(float),
            typeof(float),
            typeof(float));

        Assert.AreEqual(5f, panDistance, 0.001f);
    }

    [Test]
    public void BuildModePlacementController_HasActivePlacementInteraction_TracksActiveDefinition()
    {
        Assert.IsNotNull(placementControllerType);
        Assert.IsNotNull(placeableDefinitionType);

        placementObject = new GameObject("BuildModePlacementController");
        Component controller = placementObject.AddComponent(placementControllerType);

        Assert.IsFalse((bool)GetProperty(controller, "HasActivePlacementInteraction"));

        placeableDefinition = ScriptableObject.CreateInstance(placeableDefinitionType);
        SetPrivateField(controller, "activeDefinition", placeableDefinition);

        Assert.IsTrue((bool)GetProperty(controller, "HasActivePlacementInteraction"));

        placementControllerType
            .GetMethod("CancelDrag", BindingFlags.Public | BindingFlags.Instance)
            .Invoke(controller, null);

        Assert.IsFalse((bool)GetProperty(controller, "HasActivePlacementInteraction"));
    }

    private object InvokeMath(string methodName, object[] arguments, params Type[] parameterTypes)
    {
        MethodInfo method = cameraMathType.GetMethod(
            methodName,
            BindingFlags.Public | BindingFlags.Static,
            null,
            parameterTypes,
            null);

        Assert.IsNotNull(method);
        return method.Invoke(null, arguments);
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
