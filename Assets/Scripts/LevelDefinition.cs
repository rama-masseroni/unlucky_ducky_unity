using UnityEngine;

[CreateAssetMenu(fileName = "LevelDefinition", menuName = "Unlucky Ducky/Levels/Level Definition")]
public class LevelDefinition : ScriptableObject
{
    [SerializeField] private string levelId;
    [SerializeField] private string levelName;
    [SerializeField] private PlaceableInventorySet placeableInventorySet;

    public string LevelId => levelId;
    public string LevelName => string.IsNullOrWhiteSpace(levelName) ? name : levelName;
    public PlaceableInventorySet PlaceableInventorySet => placeableInventorySet;
}
