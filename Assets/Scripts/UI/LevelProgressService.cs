using System;
using System.Collections.Generic;
using UnityEngine;

public static class LevelProgressService
{
    private const string ProgressKey = "UnluckyDucky.LevelProgress";
    private const int CurrentVersion = 1;

    private static ILevelProgressStore store = new PlayerPrefsLevelProgressStore();

    public static bool IsCompleted(string levelId)
    {
        return !string.IsNullOrWhiteSpace(levelId) && LoadCompletedLevelIds().Contains(levelId);
    }

    public static bool MarkCompleted(string levelId)
    {
        if (string.IsNullOrWhiteSpace(levelId))
        {
            return false;
        }

        HashSet<string> completedLevelIds = LoadCompletedLevelIds();

        if (!completedLevelIds.Add(levelId))
        {
            return false;
        }

        Save(completedLevelIds);
        return true;
    }

    public static bool IsUnlocked(LevelCatalogEntry entry, IReadOnlyList<LevelCatalogEntry> orderedEntries)
    {
        if (entry == null || !entry.HasSceneName)
        {
            return false;
        }

        if (entry.UnlockedByDefault)
        {
            return true;
        }

        if (orderedEntries == null)
        {
            return false;
        }

        for (int i = 1; i < orderedEntries.Count; i++)
        {
            if (ReferenceEquals(orderedEntries[i], entry))
            {
                return IsCompleted(orderedEntries[i - 1]?.ProgressId);
            }
        }

        return false;
    }

    public static void ResetProgress()
    {
        store.DeleteKey(ProgressKey);
    }

    public static void SetStoreForTests(ILevelProgressStore progressStore)
    {
        store = progressStore ?? new PlayerPrefsLevelProgressStore();
    }

    public static void RestoreDefaultStore()
    {
        store = new PlayerPrefsLevelProgressStore();
    }

    private static HashSet<string> LoadCompletedLevelIds()
    {
        if (!store.HasKey(ProgressKey))
        {
            return new HashSet<string>(StringComparer.Ordinal);
        }

        try
        {
            LevelProgressData data = JsonUtility.FromJson<LevelProgressData>(store.GetString(ProgressKey));

            if (data == null || data.version != CurrentVersion || data.completedLevelIds == null)
            {
                return new HashSet<string>(StringComparer.Ordinal);
            }

            return new HashSet<string>(data.completedLevelIds, StringComparer.Ordinal);
        }
        catch (ArgumentException)
        {
            return new HashSet<string>(StringComparer.Ordinal);
        }
    }

    private static void Save(HashSet<string> completedLevelIds)
    {
        LevelProgressData data = new LevelProgressData
        {
            version = CurrentVersion,
            completedLevelIds = new List<string>(completedLevelIds)
        };
        data.completedLevelIds.Sort(StringComparer.Ordinal);
        store.SetString(ProgressKey, JsonUtility.ToJson(data));
        store.Save();
    }

    [Serializable]
    private sealed class LevelProgressData
    {
        public int version;
        public List<string> completedLevelIds = new List<string>();
    }

    private sealed class PlayerPrefsLevelProgressStore : ILevelProgressStore
    {
        public bool HasKey(string key) => PlayerPrefs.HasKey(key);
        public string GetString(string key) => PlayerPrefs.GetString(key, string.Empty);
        public void SetString(string key, string value) => PlayerPrefs.SetString(key, value);
        public void DeleteKey(string key) => PlayerPrefs.DeleteKey(key);
        public void Save() => PlayerPrefs.Save();
    }
}

public interface ILevelProgressStore
{
    bool HasKey(string key);
    string GetString(string key);
    void SetString(string key, string value);
    void DeleteKey(string key);
    void Save();
}

public sealed class InMemoryLevelProgressStore : ILevelProgressStore
{
    private readonly Dictionary<string, string> values = new Dictionary<string, string>();

    public bool HasKey(string key) => values.ContainsKey(key);

    public string GetString(string key)
    {
        return values.TryGetValue(key, out string value) ? value : string.Empty;
    }

    public void SetString(string key, string value)
    {
        values[key] = value;
    }

    public void DeleteKey(string key)
    {
        values.Remove(key);
    }

    public void Save()
    {
    }
}
