using UnityEngine;

[CreateAssetMenu(fileName = "WorldDefinition", menuName = "Unlucky Ducky/Worlds/World Definition")]
public class WorldDefinition : ScriptableObject
{
    [SerializeField] private string worldId;
    [SerializeField] private string worldName;

    public string WorldId => worldId;
    public string WorldName => string.IsNullOrWhiteSpace(worldName) ? name : worldName;
}
