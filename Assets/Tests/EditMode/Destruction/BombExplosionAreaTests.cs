using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;

public class BombExplosionAreaTests
{
    [Test]
    public void GetCells_WithRadiusOne_ReturnsThreeByThreeAreaCenteredOnBomb()
    {
        Type areaType = Type.GetType("BombExplosionArea, Assembly-CSharp");
        Assert.IsNotNull(areaType);

        MethodInfo getCellsMethod = areaType.GetMethod("GetCells", BindingFlags.Public | BindingFlags.Static);
        Assert.IsNotNull(getCellsMethod);

        IEnumerable cells = (IEnumerable)getCellsMethod.Invoke(null, new object[] { new Vector3Int(2, 2, 0), 1 });

        int count = 0;
        bool containsBottomLeft = false;
        bool containsCenter = false;
        bool containsTopRight = false;

        foreach (object cellObject in cells)
        {
            Vector3Int cell = (Vector3Int)cellObject;
            count++;
            containsBottomLeft |= cell == new Vector3Int(1, 1, 0);
            containsCenter |= cell == new Vector3Int(2, 2, 0);
            containsTopRight |= cell == new Vector3Int(3, 3, 0);
        }

        Assert.AreEqual(9, count);
        Assert.IsTrue(containsBottomLeft);
        Assert.IsTrue(containsCenter);
        Assert.IsTrue(containsTopRight);
    }

    [Test]
    public void BombController_WithoutDestructibleTilemapLayer_DoesNotDestroyArbitraryTilemaps()
    {
        GameObject gridObject = new GameObject("Grid", typeof(Grid));
        GameObject wallTilemapObject = new GameObject("Walls Tilemap", typeof(Tilemap), typeof(TilemapRenderer));
        wallTilemapObject.transform.SetParent(gridObject.transform);
        Tilemap wallTilemap = wallTilemapObject.GetComponent<Tilemap>();
        Vector3Int cell = Vector3Int.zero;
        wallTilemap.SetTile(cell, ScriptableObject.CreateInstance<Tile>());

        GameObject bombObject = new GameObject("Bomb");
        Type bombControllerType = Type.GetType("BombController, Assembly-CSharp");
        Assert.IsNotNull(bombControllerType);
        Component bomb = bombObject.AddComponent(bombControllerType);

        try
        {
            MethodInfo explodeMethod = bombControllerType.GetMethod("Explode", BindingFlags.Public | BindingFlags.Instance);
            Assert.IsNotNull(explodeMethod);

            explodeMethod.Invoke(bomb, null);

            Assert.IsTrue(wallTilemap.HasTile(cell));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(bombObject);
            UnityEngine.Object.DestroyImmediate(gridObject);
        }
    }
}
