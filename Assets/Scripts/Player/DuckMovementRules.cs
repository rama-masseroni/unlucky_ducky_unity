using UnityEngine;

public static class DuckMovementRules
{
    public static Vector2 ResolveVelocity(
        bool hasGroundBelow,
        Vector2 groundNormal,
        float walkSpeed,
        float fallSpeed,
        float slopeSpeedMultiplier,
        int horizontalDirection)
    {
        int direction = horizontalDirection >= 0 ? 1 : -1;

        if (!hasGroundBelow)
        {
            return new Vector2(0f, -Mathf.Abs(fallSpeed));
        }

        float slopeFactor = Mathf.Abs(groundNormal.x);
        float horizontalSpeed = walkSpeed * (1f + slopeFactor * slopeSpeedMultiplier);

        return new Vector2(horizontalSpeed * direction, 0f);
    }
}
