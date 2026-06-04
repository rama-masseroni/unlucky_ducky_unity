using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

[InitializeOnLoad]
public static class LevelActorGridSnapper
{
    private const float PositionTolerance = 0.0001f;
    private static bool isSnapping;

    static LevelActorGridSnapper()
    {
        Undo.postprocessModifications += HandlePostprocessModifications;
    }

    private static UndoPropertyModification[] HandlePostprocessModifications(UndoPropertyModification[] modifications)
    {
        if (isSnapping || EditorApplication.isPlayingOrWillChangePlaymode)
        {
            return modifications;
        }

        for (int i = 0; i < modifications.Length; i++)
        {
            Object target = modifications[i].currentValue != null
                ? modifications[i].currentValue.target
                : null;

            if (target is Transform transform && IsPositionModification(modifications[i]))
            {
                SnapIfLevelActor(transform);
            }
        }

        return modifications;
    }

    private static bool IsPositionModification(UndoPropertyModification modification)
    {
        string propertyPath = modification.currentValue != null
            ? modification.currentValue.propertyPath
            : string.Empty;

        return propertyPath == "m_LocalPosition.x"
            || propertyPath == "m_LocalPosition.y"
            || propertyPath == "m_LocalPosition.z";
    }

    private static void SnapIfLevelActor(Transform transform)
    {
        if (transform == null
            || PrefabUtility.IsPartOfPrefabAsset(transform)
            || !transform.gameObject.scene.IsValid()
            || !IsSnappedActor(transform))
        {
            return;
        }

        Tilemap tilemap = FindReferenceTilemap(transform.gameObject.scene);

        if (tilemap == null)
        {
            return;
        }

        Vector3 currentPosition = transform.position;
        Vector3Int cell = tilemap.WorldToCell(currentPosition);
        Vector3 snappedPosition = tilemap.GetCellCenterWorld(cell);
        snappedPosition.z = currentPosition.z;

        if ((snappedPosition - currentPosition).sqrMagnitude <= PositionTolerance * PositionTolerance)
        {
            return;
        }

        isSnapping = true;

        try
        {
            Undo.RecordObject(transform, "Snap Level Actor to Grid");
            transform.position = snappedPosition;
            EditorUtility.SetDirty(transform);
            EditorSceneManager.MarkSceneDirty(transform.gameObject.scene);
        }
        finally
        {
            isSnapping = false;
        }
    }

    private static bool IsSnappedActor(Transform transform)
    {
        return transform.GetComponent<PlayerDuckController>() != null
            || transform.GetComponent<GoalPointController>() != null
            || transform.GetComponent<EnemyRatController>() != null
            || transform.GetComponent<BombController>() != null
            || transform.GetComponent<SensorController>() != null
            || transform.GetComponent<SensorDoorController>() != null
            || transform.GetComponent<DestructibleBlock>() != null
            || transform.GetComponent<PlacedPlaceableInstance>() != null;
    }

    private static Tilemap FindReferenceTilemap(Scene scene)
    {
        GameObject[] roots = scene.GetRootGameObjects();

        for (int i = 0; i < roots.Length; i++)
        {
            Transform grid = FindChildRecursive(roots[i].transform, "Grid")
                ?? FindChildRecursive(roots[i].transform, "Grids");

            if (grid == null)
            {
                continue;
            }

            Tilemap tilemap = grid.GetComponentInChildren<Tilemap>(true);

            if (tilemap != null)
            {
                return tilemap;
            }
        }

        for (int i = 0; i < roots.Length; i++)
        {
            Tilemap tilemap = roots[i].GetComponentInChildren<Tilemap>(true);

            if (tilemap != null)
            {
                return tilemap;
            }
        }

        return null;
    }

    private static Transform FindChildRecursive(Transform root, string name)
    {
        if (root.name == name)
        {
            return root;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform match = FindChildRecursive(root.GetChild(i), name);

            if (match != null)
            {
                return match;
            }
        }

        return null;
    }
}
