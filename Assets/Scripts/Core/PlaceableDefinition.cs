using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[Serializable]
public class TileDestructionFilter
{
    [SerializeField] private List<TileBase> allowedTiles = new List<TileBase>();

    public IReadOnlyList<TileBase> AllowedTiles => allowedTiles;

    public bool Allows(TileBase tile)
    {
        if (tile == null || allowedTiles == null)
        {
            return false;
        }

        for (int i = 0; i < allowedTiles.Count; i++)
        {
            if (allowedTiles[i] == tile)
            {
                return true;
            }
        }

        return false;
    }
}

public enum PlaceableUseMode
{
    DragToPlace,
    ExecutionClickToDestroyTile
}

[CreateAssetMenu(fileName = "PlaceableDefinition", menuName = "Unlucky Ducky/Placeables/Placeable Definition")]
public class PlaceableDefinition : ScriptableObject
{
    [SerializeField] private string id;
    [SerializeField] private string displayName;
    [SerializeField] private GameObject prefab;
    [SerializeField] private Sprite icon;
    [SerializeField] private PlaceableUseMode useMode = PlaceableUseMode.DragToPlace;
    [SerializeField] private TileDestructionFilter destructionFilter = new TileDestructionFilter();

    public string Id => id;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public GameObject Prefab => prefab;
    public Sprite Icon => icon;
    public PlaceableUseMode UseMode => useMode;
    public TileDestructionFilter DestructionFilter => destructionFilter;
    public bool RequiresPlacementBeforeExecution => useMode == PlaceableUseMode.DragToPlace;
}
