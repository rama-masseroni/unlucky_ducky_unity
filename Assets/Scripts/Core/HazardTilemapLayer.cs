using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(Tilemap))]
public class HazardTilemapLayer : MonoBehaviour
{
    private static readonly List<HazardTilemapLayer> activeLayers = new List<HazardTilemapLayer>();

    public static IReadOnlyList<HazardTilemapLayer> ActiveLayers => activeLayers;

    public Tilemap Tilemap => GetComponent<Tilemap>();

    private void OnEnable()
    {
        if (!activeLayers.Contains(this))
        {
            activeLayers.Add(this);
        }
    }

    private void OnDisable()
    {
        activeLayers.Remove(this);
    }
}
