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

    [Test]
    public void BombController_WhenDuckIsInsideExplosionArea_KillsDuck()
    {
        GameObject gridObject = new GameObject("Grid", typeof(Grid));
        GameObject tilemapObject = new GameObject("Reference Tilemap", typeof(Tilemap), typeof(TilemapRenderer));
        tilemapObject.transform.SetParent(gridObject.transform);
        Tilemap referenceTilemap = tilemapObject.GetComponent<Tilemap>();
        Vector3Int centerCell = Vector3Int.zero;

        GameObject bombObject = new GameObject("Bomb");
        bombObject.transform.position = referenceTilemap.GetCellCenterWorld(centerCell);
        Type bombControllerType = Type.GetType("BombController, Assembly-CSharp");
        Assert.IsNotNull(bombControllerType);
        Component bomb = bombObject.AddComponent(bombControllerType);
        SetPrivateField(bomb, "referenceTilemap", referenceTilemap);
        SetPrivateField(bomb, "destroyBombAfterExplosion", false);

        Type playerDuckControllerType = Type.GetType("PlayerDuckController, UnluckyDucky.Player");
        Assert.IsNotNull(playerDuckControllerType);
        GameObject duckObject = new GameObject("Duck", typeof(Rigidbody2D), typeof(BoxCollider2D));
        duckObject.transform.position = referenceTilemap.GetCellCenterWorld(centerCell);
        Component duck = duckObject.AddComponent(playerDuckControllerType);
        SetPrivateField(duck, "resetLevelOnDeath", false);
        Physics2D.SyncTransforms();

        try
        {
            MethodInfo explodeMethod = bombControllerType.GetMethod("Explode", BindingFlags.Public | BindingFlags.Instance);
            Assert.IsNotNull(explodeMethod);

            explodeMethod.Invoke(bomb, null);

            Assert.IsTrue((bool)playerDuckControllerType.GetProperty("IsDead").GetValue(duck));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(duckObject);
            UnityEngine.Object.DestroyImmediate(bombObject);
            UnityEngine.Object.DestroyImmediate(gridObject);
        }
    }

    [Test]
    public void EnemyRatController_IsBreakableSoBombExplosionCanDestroyIt()
    {
        Type enemyRatControllerType = Type.GetType("EnemyRatController, UnluckyDucky.Enemies");
        Type breakableType = Type.GetType("IBreakable, UnluckyDucky.Core");

        Assert.IsNotNull(enemyRatControllerType);
        Assert.IsNotNull(breakableType);
        Assert.IsTrue(breakableType.IsAssignableFrom(enemyRatControllerType));
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        target.GetType()
            .GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance)
            .SetValue(target, value);
    }
}
