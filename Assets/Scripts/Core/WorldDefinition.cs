using System;
using UnityEngine;

[CreateAssetMenu(fileName = "WorldDefinition", menuName = "Unlucky Ducky/Worlds/World Definition")]
public class WorldDefinition : ScriptableObject
{
    [SerializeField] private string worldId;
    [SerializeField] private string worldName;
    [SerializeField] private WorldLevelSelectorAssets levelSelectorAssets;
    [SerializeField] private WorldInventoryUiAssets inventoryUiAssets;

    public string WorldId => worldId;
    public string WorldName => string.IsNullOrWhiteSpace(worldName) ? name : worldName;
    public WorldLevelSelectorAssets LevelSelectorAssets => levelSelectorAssets;
    public WorldInventoryUiAssets InventoryUiAssets => inventoryUiAssets;
}

[Serializable]
public class WorldInventoryUiAssets
{
    [SerializeField] private Sprite panelBackground;

    public Sprite PanelBackground => panelBackground;
}
