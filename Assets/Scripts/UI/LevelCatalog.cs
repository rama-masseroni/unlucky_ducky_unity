using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "LevelCatalog", menuName = "Unlucky Ducky/Levels/Level Catalog")]
public class LevelCatalog : ScriptableObject
{
    [SerializeField] private List<LevelCatalogEntry> entries = new List<LevelCatalogEntry>();

    public IReadOnlyList<LevelCatalogEntry> Entries => entries;

    public List<LevelCatalogEntry> GetOrderedEntries()
    {
        List<LevelCatalogEntry> orderedEntries = new List<LevelCatalogEntry>();

        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i] != null)
            {
                orderedEntries.Add(entries[i]);
            }
        }

        orderedEntries.Sort(CompareEntries);
        return orderedEntries;
    }

    private static int CompareEntries(LevelCatalogEntry left, LevelCatalogEntry right)
    {
        int orderComparison = left.DisplayOrder.CompareTo(right.DisplayOrder);

        if (orderComparison != 0)
        {
            return orderComparison;
        }

        return string.Compare(left.DisplayName, right.DisplayName, StringComparison.Ordinal);
    }
}

[Serializable]
public class LevelCatalogEntry
{
    [SerializeField] private LevelDefinition levelDefinition;
    [SerializeField] private string sceneName;
    [SerializeField] private string worldLabel;
    [SerializeField] private int displayOrder;
    [SerializeField] private bool unlockedByDefault = true;

    public LevelDefinition LevelDefinition => levelDefinition;
    public WorldDefinition WorldDefinition => levelDefinition != null ? levelDefinition.WorldDefinition : null;
    public string SceneName => sceneName;
    public string WorldLabel => string.IsNullOrWhiteSpace(worldLabel) ? "Mundo" : worldLabel;
    public int DisplayOrder => displayOrder;
    public bool UnlockedByDefault => unlockedByDefault;
    public bool HasSceneName => !string.IsNullOrWhiteSpace(sceneName);
    public bool IsPlayable => unlockedByDefault && HasSceneName;

    public string DisplayName
    {
        get
        {
            if (levelDefinition != null)
            {
                return levelDefinition.LevelName;
            }

            return string.IsNullOrWhiteSpace(sceneName) ? "Nivel sin escena" : sceneName;
        }
    }
}
