using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(Tilemap), typeof(TilemapRenderer))]
public sealed class PlacementBoundaryTilemapLayer : MonoBehaviour, IGameplayIgnoredTilemap
{
    private void Awake()
    {
        GetComponent<TilemapRenderer>().enabled = false;
    }
}
