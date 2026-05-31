using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public class SensorDoorPrefabTests
{
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
