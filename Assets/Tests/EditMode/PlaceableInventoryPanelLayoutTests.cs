using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

public class PlaceableInventoryPanelLayoutTests
{
    private readonly Type inventoryPanelType = Type.GetType("PlaceableInventoryPanel, Assembly-CSharp");
    private readonly Type placeableDefinitionType = Type.GetType("PlaceableDefinition, UnluckyDucky.Core");
    private readonly Type inventoryEntryType = Type.GetType("PlaceableInventoryEntry, UnluckyDucky.Core");
    private readonly Type inventorySetType = Type.GetType("PlaceableInventorySet, UnluckyDucky.Core");

    private readonly List<UnityEngine.Object> createdObjects = new List<UnityEngine.Object>();
    private GameObject panelObject;

    [TearDown]
    public void TearDown()
    {
        if (panelObject != null)
        {
            UnityEngine.Object.DestroyImmediate(panelObject);
        }

        GameObject gameStateObject = GameObject.Find("GameStateManager");

        if (gameStateObject != null)
        {
            UnityEngine.Object.DestroyImmediate(gameStateObject);
        }

        for (int i = 0; i < createdObjects.Count; i++)
        {
            if (createdObjects[i] != null)
            {
                UnityEngine.Object.DestroyImmediate(createdObjects[i]);
            }
        }
    }

    [Test]
    public void PlaceableInventoryPanel_WithSixEntries_ScalesSlotsToFitAvailablePanelHeight()
    {
        Assert.IsNotNull(inventoryPanelType);
        ScriptableObject inventorySet = CreateInventorySet(6);
        panelObject = new GameObject("InventoryPanel", typeof(RectTransform));
        panelObject.SetActive(false);
        Component panel = panelObject.AddComponent(inventoryPanelType);
        SetPrivateField(panel, "inventorySet", inventorySet);
        SetPrivateField(panel, "panelSize", new Vector2(180f, 360f));

        Invoke(panel, "Awake");
        Invoke(panel, "Start");

        RectTransform panelRect = panelObject.GetComponent<RectTransform>();
        RectTransform slotsRoot = (RectTransform)GetPrivateField(panel, "slotsRoot");
        VerticalLayoutGroup slotsLayout = slotsRoot.GetComponent<VerticalLayoutGroup>();
        float availableHeight = panelRect.sizeDelta.y - 20f - 24f - 40f - 16f;

        Assert.AreEqual(6, slotsRoot.childCount);
        Assert.GreaterOrEqual(panelRect.sizeDelta.x, 220f);
        Assert.GreaterOrEqual(panelRect.sizeDelta.y, 640f);
        Assert.IsTrue(slotsLayout.childControlHeight);
        Assert.Less(slotsLayout.spacing, 8f);

        float usedHeight = slotsLayout.spacing * (slotsRoot.childCount - 1);

        for (int i = 0; i < slotsRoot.childCount; i++)
        {
            LayoutElement slotLayout = slotsRoot.GetChild(i).GetComponent<LayoutElement>();
            Assert.Less(slotLayout.preferredHeight, 92f);
            usedHeight += slotLayout.preferredHeight;
        }

        Assert.LessOrEqual(usedHeight, availableHeight + 0.01f);
    }

    private ScriptableObject CreateInventorySet(int entryCount)
    {
        Assert.IsNotNull(inventorySetType);
        ScriptableObject inventorySet = ScriptableObject.CreateInstance(inventorySetType);
        createdObjects.Add(inventorySet);
        IList entries = (IList)inventorySetType
            .GetField("entries", BindingFlags.NonPublic | BindingFlags.Instance)
            .GetValue(inventorySet);

        for (int i = 0; i < entryCount; i++)
        {
            entries.Add(CreateInventoryEntry(i));
        }

        return inventorySet;
    }

    private object CreateInventoryEntry(int index)
    {
        Assert.IsNotNull(placeableDefinitionType);
        Assert.IsNotNull(inventoryEntryType);
        ScriptableObject definition = ScriptableObject.CreateInstance(placeableDefinitionType);
        createdObjects.Add(definition);
        SetPrivateField(definition, "id", $"item_{index}");
        SetPrivateField(definition, "displayName", $"Item {index + 1}");

        object entry = Activator.CreateInstance(inventoryEntryType);
        SetPrivateField(entry, "definition", definition);
        SetPrivateField(entry, "amount", 1);
        return entry;
    }

    private static void Invoke(object target, string methodName)
    {
        target.GetType()
            .GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance)
            .Invoke(target, null);
    }

    private static object GetPrivateField(object target, string fieldName)
    {
        return target.GetType()
            .GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance)
            .GetValue(target);
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        target.GetType()
            .GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance)
            .SetValue(target, value);
    }
}
