using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class CardsTutorialPrototypeBuild
{
    private const string OutputPath = "Builds/CardsTutorialPrototype/UnluckyDucky_CardsTutorial.exe";

    private static readonly string[] BuildScenes =
    {
        "Assets/Scenes/World 1/Scene_01_01.unity",
        "Assets/Scenes/World 1/Scene_01_02.unity",
        "Assets/Scenes/World 2/Scene_02_01.unity",
        "Assets/Scenes/World 3/Scene_03_01.unity",
        "Assets/Scenes/MainMenuScene.unity"
    };

    private static readonly NextSceneOverride[] NextSceneOverrides =
    {
        new NextSceneOverride("Assets/ScriptableObjects/Level definitions/LevelDefinition_01_02.asset", "Scene_02_01"),
        new NextSceneOverride("Assets/ScriptableObjects/Level definitions/LevelDefinition_02_01.asset", "Scene_03_01"),
        new NextSceneOverride("Assets/ScriptableObjects/Level definitions/LevelDefinition_03_01.asset", "MainMenuScene")
    };

    [MenuItem("Unlucky Ducky/Build/Cards Tutorial Prototype (Windows)")]
    public static void BuildWindows()
    {
        Dictionary<string, byte[]> originalAssets = CaptureAssets();

        try
        {
            ApplyNextScenes();
            Directory.CreateDirectory(Path.GetDirectoryName(OutputPath));

            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = BuildScenes,
                locationPathName = OutputPath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            });

            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException($"Cards tutorial build failed: {report.summary.result}");
            }

            Debug.Log($"Cards tutorial build created at {Path.GetFullPath(OutputPath)}");
        }
        finally
        {
            RestoreAssets(originalAssets);
        }
    }

    private static Dictionary<string, byte[]> CaptureAssets()
    {
        Dictionary<string, byte[]> originals = new Dictionary<string, byte[]>();

        foreach (NextSceneOverride sceneOverride in NextSceneOverrides)
        {
            originals.Add(sceneOverride.AssetPath, File.ReadAllBytes(sceneOverride.AssetPath));
        }

        return originals;
    }

    private static void ApplyNextScenes()
    {
        foreach (NextSceneOverride sceneOverride in NextSceneOverrides)
        {
            LevelDefinition definition = AssetDatabase.LoadAssetAtPath<LevelDefinition>(sceneOverride.AssetPath);

            if (definition == null)
            {
                throw new InvalidOperationException($"Missing level definition: {sceneOverride.AssetPath}");
            }

            SerializedObject serializedDefinition = new SerializedObject(definition);
            serializedDefinition.FindProperty("nextSceneName").stringValue = sceneOverride.NextSceneName;
            serializedDefinition.ApplyModifiedPropertiesWithoutUndo();
        }

        AssetDatabase.SaveAssets();
    }

    private static void RestoreAssets(Dictionary<string, byte[]> originals)
    {
        foreach (KeyValuePair<string, byte[]> original in originals)
        {
            File.WriteAllBytes(original.Key, original.Value);
            AssetDatabase.ImportAsset(original.Key, ImportAssetOptions.ForceUpdate);
        }
    }

    private readonly struct NextSceneOverride
    {
        public NextSceneOverride(string assetPath, string nextSceneName)
        {
            AssetPath = assetPath;
            NextSceneName = nextSceneName;
        }

        public string AssetPath { get; }
        public string NextSceneName { get; }
    }
}
