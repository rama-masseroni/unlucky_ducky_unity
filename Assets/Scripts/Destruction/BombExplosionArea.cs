using System.Collections.Generic;
using UnityEngine;

public static class BombExplosionArea
{
    public static IReadOnlyList<Vector3Int> GetCells(Vector3Int centerCell, int radius)
    {
        int safeRadius = Mathf.Max(0, radius);
        List<Vector3Int> cells = new List<Vector3Int>((safeRadius * 2 + 1) * (safeRadius * 2 + 1));

        for (int y = -safeRadius; y <= safeRadius; y++)
        {
            for (int x = -safeRadius; x <= safeRadius; x++)
            {
                cells.Add(new Vector3Int(centerCell.x + x, centerCell.y + y, centerCell.z));
            }
        }

        return cells;
    }
}
