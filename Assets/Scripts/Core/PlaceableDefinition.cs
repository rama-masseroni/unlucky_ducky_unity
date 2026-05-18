using UnityEngine;

[CreateAssetMenu(fileName = "PlaceableDefinition", menuName = "Unlucky Ducky/Placeables/Placeable Definition")]
public class PlaceableDefinition : ScriptableObject
{
    [SerializeField] private string id;
    [SerializeField] private string displayName;
    [SerializeField] private GameObject prefab;
    [SerializeField] private Sprite icon;

    public string Id => id;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public GameObject Prefab => prefab;
    public Sprite Icon => icon;
}
