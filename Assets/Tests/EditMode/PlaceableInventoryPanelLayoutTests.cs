using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
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
    public void PlaceableInventoryPanel_Rebuild_UsesAuthoredSlotLayout()
    {
        Assert.IsNotNull(inventoryPanelType);
        ScriptableObject inventorySet = CreateInventorySet(6);
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Prefabs/UI/UI_PlaceableInventoryPanel.prefab");
        Assert.IsNotNull(prefab);
        panelObject = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        panelObject.SetActive(false);
        Component panel = panelObject.GetComponent(inventoryPanelType);
        SetPrivateField(panel, "inventorySet", inventorySet);

        Invoke(panel, "Awake");
        Invoke(panel, "Start");

        RectTransform slotsRoot = (RectTransform)GetPrivateField(panel, "slotsRoot");
        VerticalLayoutGroup slotsLayout = slotsRoot.GetComponent<VerticalLayoutGroup>();

        Assert.AreEqual(6, slotsRoot.childCount);
        Assert.IsTrue(slotsLayout.childControlHeight);

        for (int i = 0; i < slotsRoot.childCount; i++)
        {
            LayoutElement slotLayout = slotsRoot.GetChild(i).GetComponent<LayoutElement>();
            Assert.AreEqual(92f, slotLayout.preferredHeight);
            Assert.AreEqual(92f, slotLayout.minHeight);
        }
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
