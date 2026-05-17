using System.Reflection;
using NUnit.Framework;
using UnityEngine;

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
}
