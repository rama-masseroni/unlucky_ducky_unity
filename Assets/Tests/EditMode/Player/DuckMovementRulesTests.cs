using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;

public class DuckMovementRulesTests
{
    [Test]
    public void ResolveVelocity_WhenGrounded_MovesRightAtWalkSpeed()
    {
        Vector2 velocity = DuckMovementRules.ResolveVelocity(
            hasGroundBelow: true,
            groundNormal: Vector2.up,
            walkSpeed: 2f,
            fallSpeed: 6f,
            slopeSpeedMultiplier: 0.5f,
            horizontalDirection: 1);

        Assert.AreEqual(new Vector2(2f, 0f), velocity);
    }

    [Test]
    public void ResolveVelocity_WhenNoGroundBelow_FallsStraightDown()
    {
        Vector2 velocity = DuckMovementRules.ResolveVelocity(
            hasGroundBelow: false,
            groundNormal: Vector2.up,
            walkSpeed: 2f,
            fallSpeed: 6f,
            slopeSpeedMultiplier: 0.5f,
            horizontalDirection: 1);

        Assert.AreEqual(new Vector2(0f, -6f), velocity);
    }

    [Test]
    public void ResolveVelocity_WhenOnSlope_IncreasesHorizontalSpeed()
    {
        Vector2 slopeNormal = new Vector2(0.6f, 0.8f);

        Vector2 velocity = DuckMovementRules.ResolveVelocity(
            hasGroundBelow: true,
            groundNormal: slopeNormal,
            walkSpeed: 2f,
            fallSpeed: 6f,
            slopeSpeedMultiplier: 0.5f,
            horizontalDirection: 1);

        Assert.AreEqual(2.6f, velocity.x, 0.001f);
        Assert.AreEqual(0f, velocity.y);
    }

    [Test]
    public void GridWalkerController_WhenPlanning_DoesNotMove()
    {
        GameObject gameObject = new GameObject("GridWalker", typeof(Rigidbody2D), typeof(GridWalkerController));
        Rigidbody2D body = gameObject.GetComponent<Rigidbody2D>();
        GridWalkerController controller = gameObject.GetComponent<GridWalkerController>();
        body.linearVelocity = new Vector2(3f, 4f);

        controller.OnLevelPhaseChanged(LevelPhase.Planning);
        typeof(GridWalkerController)
            .GetMethod("FixedUpdate", BindingFlags.NonPublic | BindingFlags.Instance)
            .Invoke(controller, null);

        Assert.AreEqual(Vector2.zero, body.linearVelocity);
        Object.DestroyImmediate(gameObject);
    }

    [Test]
    public void GridWalkerController_WhenGroundObjectProbeHitsCollider_MovesHorizontally()
    {
        GameObject walkerObject = new GameObject("GridWalker", typeof(Rigidbody2D), typeof(BoxCollider2D), typeof(GridWalkerController));
        Rigidbody2D body = walkerObject.GetComponent<Rigidbody2D>();
        GridWalkerController controller = walkerObject.GetComponent<GridWalkerController>();
        GameObject groundObject = new GameObject("GroundObject", typeof(BoxCollider2D));
        groundObject.transform.position = new Vector2(-0.24f, -0.36f);
        groundObject.GetComponent<BoxCollider2D>().size = new Vector2(1f, 0.1f);

        SetPrivateField(controller, "groundTilemaps", new Tilemap[0]);
        SetPrivateField(controller, "obstacleTilemaps", new Tilemap[0]);
        SetPrivateField(controller, "groundObjectMask", new LayerMask { value = 1 << 0 });
        Physics2D.SyncTransforms();

        controller.OnLevelPhaseChanged(LevelPhase.Execution);
        typeof(GridWalkerController)
            .GetMethod("FixedUpdate", BindingFlags.NonPublic | BindingFlags.Instance)
            .Invoke(controller, null);

        Assert.AreEqual(1f, body.linearVelocity.x, 0.001f);
        Assert.AreEqual(0f, body.linearVelocity.y, 0.001f);

        Object.DestroyImmediate(groundObject);
        Object.DestroyImmediate(walkerObject);
    }

    [Test]
    public void GridWalkerController_WhenObstacleObjectProbeHitsCollider_ReversesDirection()
    {
        GameObject walkerObject = new GameObject("GridWalker", typeof(Rigidbody2D), typeof(BoxCollider2D), typeof(GridWalkerController));
        Rigidbody2D body = walkerObject.GetComponent<Rigidbody2D>();
        GridWalkerController controller = walkerObject.GetComponent<GridWalkerController>();
        GameObject groundObject = new GameObject("GroundObject", typeof(BoxCollider2D));
        groundObject.transform.position = new Vector2(-0.24f, -0.36f);
        groundObject.GetComponent<BoxCollider2D>().size = new Vector2(1f, 0.1f);
        GameObject obstacleObject = new GameObject("ObstacleObject", typeof(BoxCollider2D));
        obstacleObject.transform.position = new Vector2(0.32f, -0.05f);
        obstacleObject.GetComponent<BoxCollider2D>().size = new Vector2(0.1f, 0.4f);

        SetPrivateField(controller, "groundTilemaps", new Tilemap[0]);
        SetPrivateField(controller, "obstacleTilemaps", new Tilemap[0]);
        SetPrivateField(controller, "groundObjectMask", new LayerMask { value = 1 << 0 });
        SetPrivateField(controller, "obstacleObjectMask", new LayerMask { value = 1 << 0 });
        Physics2D.SyncTransforms();

        controller.OnLevelPhaseChanged(LevelPhase.Execution);
        typeof(GridWalkerController)
            .GetMethod("FixedUpdate", BindingFlags.NonPublic | BindingFlags.Instance)
            .Invoke(controller, null);

        Assert.Less(body.linearVelocity.x, 0f);
        Assert.AreEqual(0f, body.linearVelocity.y, 0.001f);

        Object.DestroyImmediate(obstacleObject);
        Object.DestroyImmediate(groundObject);
        Object.DestroyImmediate(walkerObject);
    }

    [Test]
    public void GridWalkerController_WhenGroundProbeHitsGridWalkerSolid_MovesHorizontallyWithoutMask()
    {
        GameObject walkerObject = new GameObject("GridWalker", typeof(Rigidbody2D), typeof(BoxCollider2D), typeof(GridWalkerController));
        Rigidbody2D body = walkerObject.GetComponent<Rigidbody2D>();
        GridWalkerController controller = walkerObject.GetComponent<GridWalkerController>();
        GameObject solidObject = new GameObject("SolidObject", typeof(BoxCollider2D), typeof(GridWalkerSolidTestMarker));
        solidObject.transform.position = new Vector2(-0.24f, -0.36f);
        solidObject.GetComponent<BoxCollider2D>().size = new Vector2(1f, 0.1f);

        SetPrivateField(controller, "groundTilemaps", new Tilemap[0]);
        SetPrivateField(controller, "obstacleTilemaps", new Tilemap[0]);
        SetPrivateField(controller, "groundObjectMask", new LayerMask { value = 0 });
        Physics2D.SyncTransforms();

        controller.OnLevelPhaseChanged(LevelPhase.Execution);
        typeof(GridWalkerController)
            .GetMethod("FixedUpdate", BindingFlags.NonPublic | BindingFlags.Instance)
            .Invoke(controller, null);

        Assert.AreEqual(1f, body.linearVelocity.x, 0.001f);
        Assert.AreEqual(0f, body.linearVelocity.y, 0.001f);

        Object.DestroyImmediate(solidObject);
        Object.DestroyImmediate(walkerObject);
    }

    [Test]
    public void GridWalkerController_WhenObstacleProbeHitsGridWalkerSolid_ReversesDirectionWithoutMask()
    {
        GameObject walkerObject = new GameObject("GridWalker", typeof(Rigidbody2D), typeof(BoxCollider2D), typeof(GridWalkerController));
        Rigidbody2D body = walkerObject.GetComponent<Rigidbody2D>();
        GridWalkerController controller = walkerObject.GetComponent<GridWalkerController>();
        GameObject groundObject = new GameObject("GroundObject", typeof(BoxCollider2D), typeof(GridWalkerSolidTestMarker));
        groundObject.transform.position = new Vector2(-0.24f, -0.36f);
        groundObject.GetComponent<BoxCollider2D>().size = new Vector2(1f, 0.1f);
        GameObject obstacleObject = new GameObject("ObstacleObject", typeof(BoxCollider2D), typeof(GridWalkerSolidTestMarker));
        obstacleObject.transform.position = new Vector2(0.32f, -0.05f);
        obstacleObject.GetComponent<BoxCollider2D>().size = new Vector2(0.1f, 0.4f);

        SetPrivateField(controller, "groundTilemaps", new Tilemap[0]);
        SetPrivateField(controller, "obstacleTilemaps", new Tilemap[0]);
        SetPrivateField(controller, "groundObjectMask", new LayerMask { value = 0 });
        SetPrivateField(controller, "obstacleObjectMask", new LayerMask { value = 0 });
        Physics2D.SyncTransforms();

        controller.OnLevelPhaseChanged(LevelPhase.Execution);
        typeof(GridWalkerController)
            .GetMethod("FixedUpdate", BindingFlags.NonPublic | BindingFlags.Instance)
            .Invoke(controller, null);

        Assert.Less(body.linearVelocity.x, 0f);
        Assert.AreEqual(0f, body.linearVelocity.y, 0.001f);

        Object.DestroyImmediate(obstacleObject);
        Object.DestroyImmediate(groundObject);
        Object.DestroyImmediate(walkerObject);
    }

    [Test]
    public void PlayerDuckController_WhenPlanning_DoesNotDieOnHazardTile()
    {
        GameObject gridObject = CreateHazardTilemapWithTile(out Tilemap tilemap, out Vector3Int cell);
        GameObject duckObject = CreateDuckOnCell(tilemap, cell);
        PlayerDuckController duck = duckObject.GetComponent<PlayerDuckController>();

        duck.OnLevelPhaseChanged(LevelPhase.Planning);
        bool killed = duck.TryKillIfTouchingHazard();

        Assert.IsFalse(killed);
        Assert.IsFalse(duck.IsDead);

        Object.DestroyImmediate(duckObject);
        Object.DestroyImmediate(gridObject);
    }

    [Test]
    public void PlayerDuckController_WhenExecution_DiesOnHazardTile()
    {
        GameObject gridObject = CreateHazardTilemapWithTile(out Tilemap tilemap, out Vector3Int cell);
        GameObject duckObject = CreateDuckOnCell(tilemap, cell);
        PlayerDuckController duck = duckObject.GetComponent<PlayerDuckController>();

        duck.OnLevelPhaseChanged(LevelPhase.Execution);
        bool killed = duck.TryKillIfTouchingHazard();

        Assert.IsTrue(killed);
        Assert.IsTrue(duck.IsDead);

        Object.DestroyImmediate(duckObject);
        Object.DestroyImmediate(gridObject);
    }

    [Test]
    public void PlayerDuckController_WhenStandingOnSolidHazard_DiesInExecution()
    {
        GameObject gridObject = CreateHazardTilemapWithTile(out Tilemap tilemap, out Vector3Int cell);
        GameObject duckObject = CreateDuckOnCell(tilemap, cell);
        duckObject.transform.position = tilemap.GetCellCenterWorld(cell) + Vector3.up * 0.65f;
        PlayerDuckController duck = duckObject.GetComponent<PlayerDuckController>();

        duck.OnLevelPhaseChanged(LevelPhase.Execution);
        bool killed = duck.TryKillIfTouchingHazard();

        Assert.IsTrue(killed);
        Assert.IsTrue(duck.IsDead);

        Object.DestroyImmediate(duckObject);
        Object.DestroyImmediate(gridObject);
    }

    [Test]
    public void PlayerDuckController_WhenMovementGroundProbeReachesHazard_DiesInExecution()
    {
        GameObject gridObject = CreateHazardTilemapWithTile(out Tilemap tilemap, out Vector3Int cell);
        GameObject duckObject = CreateDuckOnCell(tilemap, cell);
        duckObject.transform.position = tilemap.GetCellCenterWorld(cell) + Vector3.up * 0.78f;
        PlayerDuckController duck = duckObject.GetComponent<PlayerDuckController>();

        duck.OnLevelPhaseChanged(LevelPhase.Execution);
        bool killed = duck.TryKillIfTouchingHazard();

        Assert.IsTrue(killed);
        Assert.IsTrue(duck.IsDead);

        Object.DestroyImmediate(duckObject);
        Object.DestroyImmediate(gridObject);
    }

    [Test]
    public void PlayerDuckController_WhenFacingSolidHazard_DiesInExecution()
    {
        GameObject gridObject = CreateHazardTilemapWithTile(out Tilemap tilemap, out Vector3Int cell);
        GameObject duckObject = CreateDuckOnCell(tilemap, cell);
        duckObject.transform.position = tilemap.GetCellCenterWorld(cell) + Vector3.left * 0.7f;
        PlayerDuckController duck = duckObject.GetComponent<PlayerDuckController>();

        duck.OnLevelPhaseChanged(LevelPhase.Execution);
        bool killed = duck.TryKillIfTouchingHazard();

        Assert.IsTrue(killed);
        Assert.IsTrue(duck.IsDead);

        Object.DestroyImmediate(duckObject);
        Object.DestroyImmediate(gridObject);
    }

    [Test]
    public void PlayerDuckController_WhenTilemapIsNotHazard_DoesNotDie()
    {
        GameObject gridObject = CreateTilemapWithTile(includeHazardLayer: false, out Tilemap tilemap, out Vector3Int cell);
        GameObject duckObject = CreateDuckOnCell(tilemap, cell);
        PlayerDuckController duck = duckObject.GetComponent<PlayerDuckController>();

        duck.OnLevelPhaseChanged(LevelPhase.Execution);
        bool killed = duck.TryKillIfTouchingHazard();

        Assert.IsFalse(killed);
        Assert.IsFalse(duck.IsDead);

        Object.DestroyImmediate(duckObject);
        Object.DestroyImmediate(gridObject);
    }

    [Test]
    public void PlayerDuckController_WhenHazardTilemapCellIsEmpty_DoesNotDie()
    {
        GameObject gridObject = CreateTilemapWithTile(includeHazardLayer: true, out Tilemap tilemap, out _);
        Vector3Int emptyCell = new Vector3Int(2, 2, 0);
        GameObject duckObject = CreateDuckOnCell(tilemap, emptyCell);
        PlayerDuckController duck = duckObject.GetComponent<PlayerDuckController>();

        duck.OnLevelPhaseChanged(LevelPhase.Execution);
        bool killed = duck.TryKillIfTouchingHazard();

        Assert.IsFalse(killed);
        Assert.IsFalse(duck.IsDead);

        Object.DestroyImmediate(duckObject);
        Object.DestroyImmediate(gridObject);
    }

    [Test]
    public void PlayerKillRules_WhenColliderBelongsToLivingDuck_KillsDuck()
    {
        GameObject gridObject = CreateTilemapWithTile(includeHazardLayer: false, out Tilemap tilemap, out Vector3Int cell);
        GameObject duckObject = CreateDuckOnCell(tilemap, cell);
        PlayerDuckController duck = duckObject.GetComponent<PlayerDuckController>();
        Collider2D collider = duckObject.GetComponent<Collider2D>();

        bool killed = PlayerKillRules.TryKillPlayer(collider);

        Assert.IsTrue(killed);
        Assert.IsTrue(duck.IsDead);

        Object.DestroyImmediate(duckObject);
        Object.DestroyImmediate(gridObject);
    }

    [Test]
    public void PlayerKillRules_WhenColliderHasNoDuck_ReturnsFalse()
    {
        GameObject objectWithoutDuck = new GameObject("ObjectWithoutDuck", typeof(BoxCollider2D));
        Collider2D collider = objectWithoutDuck.GetComponent<Collider2D>();

        bool killed = PlayerKillRules.TryKillPlayer(collider);

        Assert.IsFalse(killed);

        Object.DestroyImmediate(objectWithoutDuck);
    }

    [Test]
    public void PlayerKillRules_WhenDuckIsAlreadyDead_ReturnsFalse()
    {
        GameObject gridObject = CreateTilemapWithTile(includeHazardLayer: false, out Tilemap tilemap, out Vector3Int cell);
        GameObject duckObject = CreateDuckOnCell(tilemap, cell);
        PlayerDuckController duck = duckObject.GetComponent<PlayerDuckController>();
        Collider2D collider = duckObject.GetComponent<Collider2D>();

        duck.Kill();
        bool killedAgain = PlayerKillRules.TryKillPlayer(collider);

        Assert.IsFalse(killedAgain);
        Assert.IsTrue(duck.IsDead);

        Object.DestroyImmediate(duckObject);
        Object.DestroyImmediate(gridObject);
    }

    private static GameObject CreateHazardTilemapWithTile(out Tilemap tilemap, out Vector3Int cell)
    {
        return CreateTilemapWithTile(includeHazardLayer: true, out tilemap, out cell);
    }

    private static GameObject CreateTilemapWithTile(bool includeHazardLayer, out Tilemap tilemap, out Vector3Int cell)
    {
        GameObject gridObject = new GameObject("Grid", typeof(Grid));
        GameObject tilemapObject = new GameObject("Tilemap", typeof(Tilemap), typeof(TilemapRenderer));
        tilemapObject.transform.SetParent(gridObject.transform);

        if (includeHazardLayer)
        {
            tilemapObject.AddComponent<HazardTilemapLayer>();
        }

        tilemap = tilemapObject.GetComponent<Tilemap>();
        cell = new Vector3Int(1, 1, 0);
        Tile tile = ScriptableObject.CreateInstance<Tile>();
        tilemap.SetTile(cell, tile);
        return gridObject;
    }

    private static GameObject CreateDuckOnCell(Tilemap tilemap, Vector3Int cell)
    {
        GameObject duckObject = new GameObject("Duck", typeof(Rigidbody2D), typeof(BoxCollider2D));
        duckObject.transform.position = tilemap.GetCellCenterWorld(cell);

        BoxCollider2D collider = duckObject.GetComponent<BoxCollider2D>();
        collider.size = new Vector2(0.4f, 0.3f);

        PlayerDuckController duck = duckObject.AddComponent<PlayerDuckController>();
        typeof(PlayerDuckController)
            .GetField("resetLevelOnDeath", BindingFlags.NonPublic | BindingFlags.Instance)
            .SetValue(duck, false);

        return duckObject;
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        target.GetType()
            .GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance)
            .SetValue(target, value);
    }

    private sealed class GridWalkerSolidTestMarker : MonoBehaviour, IGridWalkerSolid
    {
    }
}
