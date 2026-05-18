using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PlaceableInventorySet", menuName = "Unlucky Ducky/Placeables/Inventory Set")]
public class PlaceableInventorySet : ScriptableObject
{
    [SerializeField] private List<PlaceableInventoryEntry> entries = new List<PlaceableInventoryEntry>();

    public IReadOnlyList<PlaceableInventoryEntry> Entries => entries;
}
