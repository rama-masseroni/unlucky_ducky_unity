using UnityEngine;

public static class DynamicPlanningCameraMath
{
    public static float CalculatePlanningOrthographicSize(float fullOrthographicSize, float planningOrthographicScale)
    {
        float safeFullSize = Mathf.Max(0.01f, fullOrthographicSize);
        float safeScale = Mathf.Clamp01(planningOrthographicScale);
        return Mathf.Max(0.01f, safeFullSize * safeScale);
    }

    public static Vector2 CalculateViewExtents(float orthographicSize, float aspect)
    {
        float safeSize = Mathf.Max(0.01f, orthographicSize);
        float safeAspect = Mathf.Max(0.01f, aspect);
        return new Vector2(safeSize * safeAspect, safeSize);
    }

    public static Vector2 ClampCameraCenter(
        Vector2 desiredCenter,
        Vector2 fullViewCenter,
        Vector2 fullViewExtents,
        Vector2 planningViewExtents)
    {
        return new Vector2(
            ClampAxis(desiredCenter.x, fullViewCenter.x, fullViewExtents.x, planningViewExtents.x),
            ClampAxis(desiredCenter.y, fullViewCenter.y, fullViewExtents.y, planningViewExtents.y));
    }

    public static Vector2 ResolveEdgeScrollDirection(
        Vector2 screenPosition,
        Vector2 screenSize,
        float edgeBandPixels)
    {
        if (screenSize.x <= 0f || screenSize.y <= 0f || edgeBandPixels <= 0f)
        {
            return Vector2.zero;
        }

        Vector2 direction = Vector2.zero;
        float safeBand = Mathf.Min(edgeBandPixels, Mathf.Min(screenSize.x, screenSize.y) * 0.5f);

        if (screenPosition.x <= safeBand)
        {
            direction.x -= 1f;
        }
        else if (screenPosition.x >= screenSize.x - safeBand)
        {
            direction.x += 1f;
        }

        if (screenPosition.y <= safeBand)
        {
            direction.y -= 1f;
        }
        else if (screenPosition.y >= screenSize.y - safeBand)
        {
            direction.y += 1f;
        }

        return NormalizeDirection(direction);
    }

    public static Vector2 ResolveKeyboardDirection(bool left, bool right, bool down, bool up)
    {
        Vector2 direction = Vector2.zero;

        if (left)
        {
            direction.x -= 1f;
        }

        if (right)
        {
            direction.x += 1f;
        }

        if (down)
        {
            direction.y -= 1f;
        }

        if (up)
        {
            direction.y += 1f;
        }

        return NormalizeDirection(direction);
    }

    public static Vector2 NormalizeDirection(Vector2 direction)
    {
        return direction.sqrMagnitude > 1f ? direction.normalized : direction;
    }

    public static float CalculateViewportRelativePanDistance(
        float orthographicSize,
        float viewportHeightsPerSecond,
        float deltaSeconds)
    {
        return Mathf.Max(0f, orthographicSize)
            * 2f
            * Mathf.Max(0f, viewportHeightsPerSecond)
            * Mathf.Max(0f, deltaSeconds);
    }

    private static float ClampAxis(float desired, float fullCenter, float fullExtent, float planningExtent)
    {
        if (planningExtent >= fullExtent)
        {
            return fullCenter;
        }

        return Mathf.Clamp(
            desired,
            fullCenter - fullExtent + planningExtent,
            fullCenter + fullExtent - planningExtent);
    }
}
