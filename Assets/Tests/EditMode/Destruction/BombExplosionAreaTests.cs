using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.TestTools;

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
    public void BombController_WithOffsetDestructibleTilemap_OnlyDestroysWorldCellsInExplosionArea()
    {
        GameObject referenceGridObject = new GameObject("Reference Grid", typeof(Grid));
        Tilemap referenceTilemap = CreateTilemap(referenceGridObject.transform, "Reference Tilemap");
        GameObject breakableGridObject = new GameObject("Breakable Grid", typeof(Grid));
        Tilemap breakableTilemap = CreateTilemap(breakableGridObject.transform, "Breakable Tilemap");
        GameObject fallingGridObject = new GameObject("Falling Grid", typeof(Grid));
        fallingGridObject.transform.position = new Vector3(8f, 0f, 0f);
        Tilemap fallingTilemap = CreateTilemap(fallingGridObject.transform, "Falling Tilemap");
        Type destructibleTilemapLayerType = Type.GetType("DestructibleTilemapLayer, Assembly-CSharp");
        Assert.IsNotNull(destructibleTilemapLayerType);
        breakableTilemap.gameObject.AddComponent(destructibleTilemapLayerType);
        fallingTilemap.gameObject.AddComponent(destructibleTilemapLayerType);

        Vector3Int centerCell = Vector3Int.zero;
        Vector3Int farFallingCellWithSameCoordinates = centerCell;
        breakableTilemap.SetTile(centerCell, ScriptableObject.CreateInstance<Tile>());
        fallingTilemap.SetTile(farFallingCellWithSameCoordinates, ScriptableObject.CreateInstance<Tile>());

        GameObject bombObject = new GameObject("Bomb");
        bombObject.transform.position = referenceTilemap.GetCellCenterWorld(centerCell);
        Type bombControllerType = Type.GetType("BombController, Assembly-CSharp");
        Assert.IsNotNull(bombControllerType);
        Component bomb = bombObject.AddComponent(bombControllerType);
        SetPrivateField(bomb, "referenceTilemap", referenceTilemap);
        SetPrivateField(bomb, "destructibleTilemaps", new[] { breakableTilemap, fallingTilemap });
        SetPrivateField(bomb, "destroyBombAfterExplosion", false);

        try
        {
            MethodInfo explodeMethod = bombControllerType.GetMethod("Explode", BindingFlags.Public | BindingFlags.Instance);
            Assert.IsNotNull(explodeMethod);

            explodeMethod.Invoke(bomb, null);

            Assert.IsFalse(breakableTilemap.HasTile(centerCell));
            Assert.IsTrue(fallingTilemap.HasTile(farFallingCellWithSameCoordinates));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(bombObject);
            UnityEngine.Object.DestroyImmediate(fallingGridObject);
            UnityEngine.Object.DestroyImmediate(breakableGridObject);
            UnityEngine.Object.DestroyImmediate(referenceGridObject);
        }
    }

    [Test]
    public void BombController_WhenSupportBreaksFallingTile_DoesNotBreakSpawnedFallingBlockInSameExplosion()
    {
        Type bombControllerType = Type.GetType("BombController, Assembly-CSharp");
        Type fallingLayerType = Type.GetType("FallingDestructibleTilemapLayer, Assembly-CSharp");
        Type fallingBlockType = Type.GetType("FallingTileBlock, Assembly-CSharp");
        Assert.IsNotNull(bombControllerType);
        Assert.IsNotNull(fallingLayerType);
        Assert.IsNotNull(fallingBlockType);

        GameObject gridObject = new GameObject("Grid", typeof(Grid));
        Tilemap referenceTilemap = CreateTilemap(gridObject.transform, "Reference Tilemap");
        Tilemap supportTilemap = CreateTilemap(gridObject.transform, "Support Tilemap");
        Tilemap fallingTilemap = CreateTilemap(gridObject.transform, "Falling Tilemap");
        Vector3Int supportCell = new Vector3Int(0, 1, 0);
        Vector3Int fallingCell = new Vector3Int(0, 2, 0);
        supportTilemap.SetTile(supportCell, ScriptableObject.CreateInstance<Tile>());
        fallingTilemap.SetTile(fallingCell, ScriptableObject.CreateInstance<Tile>());
        Component fallingLayer = fallingTilemap.gameObject.AddComponent(fallingLayerType);
        SetPrivateField(fallingLayer, "supportTilemaps", new[] { supportTilemap });
        InvokeLevelPhase(fallingLayer, "Execution");
        Assert.IsTrue(fallingTilemap.HasTile(fallingCell));

        GameObject bombObject = new GameObject("Bomb");
        bombObject.transform.position = referenceTilemap.GetCellCenterWorld(Vector3Int.zero);
        Component bomb = bombObject.AddComponent(bombControllerType);
        SetPrivateField(bomb, "referenceTilemap", referenceTilemap);
        SetPrivateField(bomb, "destructibleTilemaps", new[] { supportTilemap, fallingTilemap });
        SetPrivateField(bomb, "destroyBombAfterExplosion", false);

        try
        {
            MethodInfo explodeMethod = bombControllerType.GetMethod("Explode", BindingFlags.Public | BindingFlags.Instance);
            Assert.IsNotNull(explodeMethod);

            explodeMethod.Invoke(bomb, null);

            Assert.IsFalse(supportTilemap.HasTile(supportCell));
            Assert.IsFalse(fallingTilemap.HasTile(fallingCell));
            Assert.AreEqual(1, UnityEngine.Object.FindObjectsByType(fallingBlockType, FindObjectsSortMode.None).Length);
        }
        finally
        {
            DestroyFallingBlocksRoot();
            UnityEngine.Object.DestroyImmediate(bombObject);
            UnityEngine.Object.DestroyImmediate(gridObject);
        }
    }

    [UnityTest]
    public IEnumerator BombController_WhenBreakableObjectColliderBleedsIntoExplosionEdge_DoesNotBreakOutsideCell()
    {
        GameObject gridObject = new GameObject("Grid", typeof(Grid));
        Tilemap referenceTilemap = CreateTilemap(gridObject.transform, "Reference Tilemap");
        Type bombControllerType = Type.GetType("BombController, Assembly-CSharp");
        Type destructibleObjectType = Type.GetType("DestructibleObject, Assembly-CSharp");
        Assert.IsNotNull(bombControllerType);
        Assert.IsNotNull(destructibleObjectType);

        GameObject bombObject = new GameObject("Bomb");
        bombObject.transform.position = referenceTilemap.GetCellCenterWorld(Vector3Int.zero);
        Component bomb = bombObject.AddComponent(bombControllerType);
        SetPrivateField(bomb, "referenceTilemap", referenceTilemap);
        SetPrivateField(bomb, "destructibleTilemaps", Array.Empty<Tilemap>());
        SetPrivateField(bomb, "destroyBombAfterExplosion", false);

        GameObject outsideObject = new GameObject("OutsideBreakable", typeof(BoxCollider2D));
        outsideObject.transform.position = referenceTilemap.GetCellCenterWorld(new Vector3Int(2, 0, 0));
        outsideObject.GetComponent<BoxCollider2D>().size = new Vector2(1.02f, 1f);
        outsideObject.AddComponent(destructibleObjectType);
        Physics2D.SyncTransforms();

        try
        {
            MethodInfo explodeMethod = bombControllerType.GetMethod("Explode", BindingFlags.Public | BindingFlags.Instance);
            Assert.IsNotNull(explodeMethod);

            explodeMethod.Invoke(bomb, null);
            yield return null;

            Assert.IsFalse(outsideObject == null);
        }
        finally
        {
            if (outsideObject != null)
            {
                UnityEngine.Object.DestroyImmediate(outsideObject);
            }

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

    private static Tilemap CreateTilemap(Transform parent, string name)
    {
        GameObject tilemapObject = new GameObject(name, typeof(Tilemap), typeof(TilemapRenderer));
        tilemapObject.transform.SetParent(parent);
        return tilemapObject.GetComponent<Tilemap>();
    }

    private static void InvokeLevelPhase(Component listener, string phaseName)
    {
        Type levelPhaseType = Type.GetType("LevelPhase, UnluckyDucky.Core");
        Assert.IsNotNull(levelPhaseType);
        object phase = Enum.Parse(levelPhaseType, phaseName);

        listener.GetType()
            .GetMethod("OnLevelPhaseChanged", BindingFlags.Public | BindingFlags.Instance)
            .Invoke(listener, new[] { phase });
    }

    private static void DestroyFallingBlocksRoot()
    {
        GameObject root = GameObject.Find("FallingBlocksRoot");

        if (root != null)
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        target.GetType()
            .GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance)
            .SetValue(target, value);
    }
}
