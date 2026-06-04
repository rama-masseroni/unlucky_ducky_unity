using UnityEngine;

[CreateAssetMenu(fileName = "LevelDefinition", menuName = "Unlucky Ducky/Levels/Level Definition")]
public class LevelDefinition : ScriptableObject
{
    [SerializeField] private string levelId;
    [SerializeField] private string levelName;
    [SerializeField] private string nextSceneName;
    [SerializeField] private WorldDefinition worldDefinition;
    [SerializeField] private PlaceableInventorySet placeableInventorySet;
    [Min(0f)]
    [SerializeField] private float planningTimeLimitSeconds;

    public string LevelId => levelId;
    public string LevelName => string.IsNullOrWhiteSpace(levelName) ? name : levelName;
    public string NextSceneName => nextSceneName;
    public WorldDefinition WorldDefinition => worldDefinition;
    public PlaceableInventorySet PlaceableInventorySet => placeableInventorySet;
    public float PlanningTimeLimitSeconds => Mathf.Max(0f, planningTimeLimitSeconds);
}
