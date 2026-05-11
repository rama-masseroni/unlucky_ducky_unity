using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;

public class LevelManagerTilemapTests
{
    private GameObject gridObject;
    private GameObject tilemapObject;
    private Tilemap tilemap;
    private MethodInfo tryDestroyTileAtCellMethod;

    [SetUp]
    public void SetUp()
    {
        gridObject = new GameObject("Grid", typeof(Grid));
        tilemapObject = new GameObject("Tilemap", typeof(Tilemap), typeof(TilemapRenderer));
        tilemapObject.transform.SetParent(gridObject.transform);
        tilemap = tilemapObject.GetComponent<Tilemap>();

        Type levelManagerType = Type.GetType("LevelManager, Assembly-CSharp");
        Assert.IsNotNull(levelManagerType);

        tryDestroyTileAtCellMethod = levelManagerType.GetMethod(
            "TryDestroyTileAtCell",
            BindingFlags.Public | BindingFlags.Static);

        Assert.IsNotNull(tryDestroyTileAtCellMethod);
    }

    [TearDown]
    public void TearDown()
    {
        UnityEngine.Object.DestroyImmediate(gridObject);
    }

    [Test]
    public void TryDestroyTileAtCell_WhenCellHasTile_RemovesTileFromGridTilemap()
    {
        Vector3Int cellPosition = new Vector3Int(2, 3, 0);
        Tile tile = ScriptableObject.CreateInstance<Tile>();
        tilemap.SetTile(cellPosition, tile);

        bool destroyed = TryDestroyTileAtCell(cellPosition);

        Assert.IsTrue(destroyed);
        Assert.IsFalse(tilemap.HasTile(cellPosition));
    }

    [Test]
    public void TryDestroyTileAtCell_WhenCellIsEmpty_ReturnsFalse()
    {
        Vector3Int cellPosition = new Vector3Int(2, 3, 0);

        bool destroyed = TryDestroyTileAtCell(cellPosition);

        Assert.IsFalse(destroyed);
    }

    private bool TryDestroyTileAtCell(Vector3Int cellPosition)
    {
        return (bool)tryDestroyTileAtCellMethod.Invoke(null, new object[] { tilemap, cellPosition });
    }
}
