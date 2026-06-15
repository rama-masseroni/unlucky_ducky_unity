using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;

public class BombBreakableObjectTests
{
    [Test]
    public void BombController_WithEmptyTileFilter_StillBreaksObjects()
    {
        GameObject gridObject = new GameObject("Grid", typeof(Grid));
        GameObject tilemapObject = new GameObject("Reference Tilemap", typeof(Tilemap), typeof(TilemapRenderer));
        tilemapObject.transform.SetParent(gridObject.transform);
        Tilemap referenceTilemap = tilemapObject.GetComponent<Tilemap>();

        GameObject bombObject = new GameObject("Bomb");
        bombObject.transform.position = referenceTilemap.GetCellCenterWorld(Vector3Int.zero);
        Tile blockedTile = ScriptableObject.CreateInstance<Tile>();
        referenceTilemap.SetTile(Vector3Int.zero, blockedTile);
        Type bombControllerType = Type.GetType("BombController, Assembly-CSharp");
        Assert.IsNotNull(bombControllerType);
        Component bomb = bombObject.AddComponent(bombControllerType);
        SetPrivateField(bomb, "referenceTilemap", referenceTilemap);
        SetPrivateField(bomb, "destructibleTilemaps", new[] { referenceTilemap });
        SetPrivateField(bomb, "tileDestructionFilter", new TileDestructionFilter());
        SetPrivateField(bomb, "destroyBombAfterExplosion", false);

        GameObject breakableObject = new GameObject("Breakable", typeof(BoxCollider2D));
        breakableObject.transform.position = referenceTilemap.GetCellCenterWorld(Vector3Int.zero);
        RecordingBreakable breakable = breakableObject.AddComponent<RecordingBreakable>();
        Physics2D.SyncTransforms();

        try
        {
            bombControllerType.GetMethod("Explode", BindingFlags.Public | BindingFlags.Instance).Invoke(bomb, null);

            Assert.AreEqual(1, breakable.BreakCount);
            Assert.IsTrue(referenceTilemap.HasTile(Vector3Int.zero));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(breakableObject);
            UnityEngine.Object.DestroyImmediate(bombObject);
            UnityEngine.Object.DestroyImmediate(gridObject);
            UnityEngine.Object.DestroyImmediate(blockedTile);
            DestroyGameStateManager();
        }
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        target.GetType()
            .GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance)
            .SetValue(target, value);
    }

    private static void DestroyGameStateManager()
    {
        GameObject manager = GameObject.Find("GameStateManager");

        if (manager != null)
        {
            UnityEngine.Object.DestroyImmediate(manager);
        }
    }
}

public sealed class RecordingBreakable : MonoBehaviour, IBreakable
{
    public int BreakCount { get; private set; }

    public void Break()
    {
        BreakCount++;
    }
}
