using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public static class TilemapDestructionEvents
{
    public static event Action<Tilemap, Vector3Int> TileDestroyed;

    private static readonly List<PendingTileDestruction> pendingEvents = new List<PendingTileDestruction>();
    private static int batchDepth;

    public static void BeginBatch()
    {
        batchDepth++;
    }

    public static void EndBatch()
    {
        if (batchDepth <= 0)
        {
            return;
        }

        batchDepth--;

        if (batchDepth > 0)
        {
            return;
        }

        for (int i = 0; i < pendingEvents.Count; i++)
        {
            PendingTileDestruction pendingEvent = pendingEvents[i];
            TileDestroyed?.Invoke(pendingEvent.Tilemap, pendingEvent.Cell);
        }

        pendingEvents.Clear();
    }

    public static void RaiseTileDestroyed(Tilemap tilemap, Vector3Int cell)
    {
        if (batchDepth > 0)
        {
            pendingEvents.Add(new PendingTileDestruction(tilemap, cell));
            return;
        }

        TileDestroyed?.Invoke(tilemap, cell);
    }

    private readonly struct PendingTileDestruction
    {
        public PendingTileDestruction(Tilemap tilemap, Vector3Int cell)
        {
            Tilemap = tilemap;
            Cell = cell;
        }

        public Tilemap Tilemap { get; }
        public Vector3Int Cell { get; }
    }
}
