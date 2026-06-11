using UnityEngine;

[CreateAssetMenu(fileName = "WorldDefinition", menuName = "Unlucky Ducky/Worlds/World Definition")]
public class WorldDefinition : ScriptableObject
{
    [SerializeField] private string worldId;
    [SerializeField] private string worldName;
    [SerializeField] private WorldLevelSelectorAssets levelSelectorAssets;

    public string WorldId => worldId;
    public string WorldName => string.IsNullOrWhiteSpace(worldName) ? name : worldName;
    public WorldLevelSelectorAssets LevelSelectorAssets => levelSelectorAssets;
}
