using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public class SensorDoorPrefabTests
{
    private static readonly string[] RemovedPlaceableDefinitionPaths =
    {
        "Assets/ScriptableObjects/Items/Placeable_Sensor.asset",
        "Assets/ScriptableObjects/Items/Placeable_Sensor_Door.asset",
        "Assets/ScriptableObjects/Items/Placeable_Open_Door.asset"
    };

    private static readonly string[] RemovedPlaceableDefinitionGuids =
    {
        "ab9618b2bc3e4b28946d30870f2fb2c1",
        "02f59306579d48f2aad9af02feb2dd01",
        "574e25c4d5ec3204e932b9a2f09a4e89"
    };

    [TestCase("Assets/Prefabs/Placeables/Sensor_Door.prefab")]
    [TestCase("Assets/Prefabs/Placeables/Sensor_Door_Open.prefab")]
    public void SensorDoorPrefab_BlockingColliderCoversFullCell(string prefabPath)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

        Assert.IsNotNull(prefab);
        AssertHasSensorDoorController(prefab);

        BoxCollider2D blockingCollider = prefab.GetComponent<BoxCollider2D>();

        Assert.IsNotNull(blockingCollider);
        Assert.AreEqual(Vector2.one, blockingCollider.size);
        Assert.AreEqual(Vector2.zero, blockingCollider.offset);
    }

    [Test]
    public void SensorAndDoorPlaceableDefinitions_AreNotInventoryItems()
    {
        for (int i = 0; i < RemovedPlaceableDefinitionPaths.Length; i++)
        {
            Object removedDefinition = AssetDatabase.LoadAssetAtPath<Object>(RemovedPlaceableDefinitionPaths[i]);
            Assert.IsNull(removedDefinition, $"{RemovedPlaceableDefinitionPaths[i]} should not exist as a PlaceableDefinition.");
        }

        string[] inventorySetGuids = AssetDatabase.FindAssets("t:PlaceableInventorySet", new[] { "Assets/ScriptableObjects/InventorySets" });

        for (int i = 0; i < inventorySetGuids.Length; i++)
        {
            string inventoryPath = AssetDatabase.GUIDToAssetPath(inventorySetGuids[i]);
            string inventoryYaml = File.ReadAllText(inventoryPath);

            for (int guidIndex = 0; guidIndex < RemovedPlaceableDefinitionGuids.Length; guidIndex++)
            {
                Assert.IsFalse(
                    inventoryYaml.Contains(RemovedPlaceableDefinitionGuids[guidIndex]),
                    $"{inventoryPath} still references a removed sensor/door PlaceableDefinition.");
            }
        }
    }

    private static void AssertHasSensorDoorController(GameObject prefab)
    {
        MonoBehaviour[] behaviours = prefab.GetComponentsInChildren<MonoBehaviour>(true);

        for (int i = 0; i < behaviours.Length; i++)
        {
            MonoBehaviour behaviour = behaviours[i];

            if (behaviour != null && behaviour.GetType().Name == "SensorDoorController")
            {
                return;
            }
        }

        Assert.Fail($"{prefab.name} does not include a SensorDoorController.");
    }
}
