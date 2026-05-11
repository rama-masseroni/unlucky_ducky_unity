using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "LevelDefinition", menuName = "Scriptable Objects/LevelDefinition")]
public class LevelDefinition : ScriptableObject
{
    private const int MAX_LEVELS = 20;
    private Vector2Int LEVEL_DIMENSION;
    private List<PlaceableDefinition> itemsToUse;
}
