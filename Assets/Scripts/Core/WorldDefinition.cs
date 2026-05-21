using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "WorldDefinition", menuName = "Unlucky Ducky/Worlds/World Definition")]
public class WorldDefinition : ScriptableObject
{
    [SerializeField] private string worldId;
    [SerializeField] private string worldName;
    [FormerlySerializedAs("enablePlanningTileDestruction")]
    [SerializeField] private bool enableTileDestructionTool;

    public string WorldId => worldId;
    public string WorldName => string.IsNullOrWhiteSpace(worldName) ? name : worldName;
    public bool EnableTileDestructionTool => enableTileDestructionTool;
}
