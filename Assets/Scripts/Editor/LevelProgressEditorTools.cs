using UnityEditor;
using UnityEngine;

public static class LevelProgressEditorTools
{
    [MenuItem("Unlucky Ducky/Progress/Reset Local Progress")]
    private static void ResetLocalProgress()
    {
        bool confirmed = EditorUtility.DisplayDialog(
            "Reset local progress",
            "This will lock every catalog level except those unlocked by default.",
            "Reset",
            "Cancel");

        if (!confirmed)
        {
            return;
        }

        LevelProgressService.ResetProgress();
        Debug.Log("Unlucky Ducky local level progress was reset.");
    }
}
