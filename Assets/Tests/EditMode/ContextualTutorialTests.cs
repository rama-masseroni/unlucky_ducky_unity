using System;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public class ContextualTutorialTests
{
    private const string SequencePath = "Assets/ScriptableObjects/Tutorials/TutorialTooltipSequence_Sandbox.asset";
    private const string CanvasPath = "Assets/Prefabs/UI/UI_GameplayCanvas.prefab";
    private const string TooltipPath = "Assets/Prefabs/UI/Tutorials/UI_ContextualTutorialTooltip.prefab";

    [Test]
    public void Sequence_HasExpectedOrderAndConditions()
    {
        UnityEngine.Object sequence = AssetDatabase.LoadMainAssetAtPath(SequencePath);
        Assert.IsNotNull(sequence);
        SerializedProperty steps = new SerializedObject(sequence).FindProperty("steps");
        Assert.AreEqual(7, steps.arraySize);
        string[] ids = { "planning", "falling_blocks", "sensors_doors", "bomb", "rat", "execute", "pickaxe" };
        int[] objectives = { 0, 4, 4, 1, 1, 2, 3 };
        for (int i = 0; i < ids.Length; i++)
        {
            SerializedProperty step = steps.GetArrayElementAtIndex(i);
            Assert.AreEqual(ids[i], step.FindPropertyRelative("id").stringValue);
            Assert.AreEqual(objectives[i], step.FindPropertyRelative("objective").enumValueIndex);
        }
        Assert.AreEqual(1, steps.GetArrayElementAtIndex(1).FindPropertyRelative("environmentTarget").enumValueIndex);
        Assert.AreEqual(2, steps.GetArrayElementAtIndex(2).FindPropertyRelative("environmentTarget").enumValueIndex);
        Assert.IsNotNull(steps.GetArrayElementAtIndex(3).FindPropertyRelative("placeable").objectReferenceValue);
        Assert.IsNotNull(steps.GetArrayElementAtIndex(4).FindPropertyRelative("placeable").objectReferenceValue);
        Assert.IsNotNull(steps.GetArrayElementAtIndex(6).FindPropertyRelative("placeable").objectReferenceValue);
    }

    [Test]
    public void GameplayCanvas_HasExactlyOneWiredControllerAndTooltipInstance()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CanvasPath);
        Component[] controllers = FindComponents(prefab, "ContextualTutorialController");
        Assert.AreEqual(1, controllers.Length);
        Assert.AreEqual(1, FindComponents(prefab, "TutorialTooltipView").Length);
        SerializedObject controller = new SerializedObject(controllers[0]);
        Assert.IsNotNull(controller.FindProperty("sequence").objectReferenceValue);
        Assert.IsNotNull(controller.FindProperty("tooltip").objectReferenceValue);
        Assert.IsNotNull(controller.FindProperty("inventoryPanel").objectReferenceValue);
        Assert.IsNotNull(controller.FindProperty("executeButton").objectReferenceValue);
    }

    [Test]
    public void TooltipPrefab_InheritsCanvasInfrastructure()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(TooltipPath);
        Assert.AreEqual(1, FindComponents(prefab, "TutorialTooltipView").Length);
        Assert.IsNull(prefab.GetComponentInChildren<Canvas>(true));
        Assert.IsNull(prefab.GetComponentInChildren<UnityEngine.UI.CanvasScaler>(true));
        Assert.IsNull(prefab.GetComponentInChildren<UnityEngine.UI.GraphicRaycaster>(true));
        Assert.IsFalse(prefab.activeSelf);
    }

    [Test]
    public void Sandbox_MatchesCardsPrototypeInventoryAndTimer()
    {
        SerializedProperty entries = new SerializedObject(AssetDatabase.LoadMainAssetAtPath("Assets/ScriptableObjects/InventorySets/InventorySet_TEST.asset")).FindProperty("entries");
        Assert.AreEqual(6, entries.GetArrayElementAtIndex(0).FindPropertyRelative("amount").intValue);
        Assert.AreEqual(1, entries.GetArrayElementAtIndex(1).FindPropertyRelative("amount").intValue);
        Assert.AreEqual(1, entries.GetArrayElementAtIndex(2).FindPropertyRelative("amount").intValue);
        SerializedObject level = new SerializedObject(AssetDatabase.LoadMainAssetAtPath("Assets/ScriptableObjects/Level definitions/LevelDefinition_TEST.asset"));
        Assert.AreEqual(0f, level.FindProperty("planningTimeLimitSeconds").floatValue);
        Assert.AreEqual("Assets/Scenes/Test_Scene.unity", EditorBuildSettings.scenes[0].path);
    }

    private static Component[] FindComponents(GameObject root, string typeName)
    {
        Type type = Type.GetType(typeName + ", Assembly-CSharp");
        Assert.IsNotNull(type, typeName + " type was not found.");
        return root.GetComponentsInChildren(type, true);
    }
}
