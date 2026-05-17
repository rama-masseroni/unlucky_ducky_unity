using UnityEngine;
using UnityEngine.Tilemaps;

public class BuildModePlacementController : MonoBehaviour
{
    [SerializeField] private Camera worldCamera;
    [SerializeField] private Tilemap referenceTilemap;
    [SerializeField] private Tilemap[] blockedTilemaps;
    [SerializeField] private Transform placedObjectsRoot;
    [SerializeField] private GameStateManager gameStateManager;
    [SerializeField] private Color validPreviewColor = new Color(1f, 1f, 1f, 0.65f);
    [SerializeField] private Color invalidPreviewColor = new Color(1f, 0.2f, 0.2f, 0.65f);

    private PlaceableDefinition activeDefinition;
    private GameObject previewInstance;
    private SpriteRenderer previewRenderer;
    private Vector3Int currentCell;
    private bool hasCurrentCell;
    private bool currentCellIsValid;

    private void Awake()
    {
        ResolveSceneReferences();
    }

    public void BeginDrag(PlaceableDefinition definition)
    {
        if (!CanUseBuildMode())
        {
            return;
        }

        activeDefinition = definition;
        CreatePreview();
    }

    public void UpdateDrag(Vector2 screenPosition)
    {
        if (!CanUseBuildMode() || activeDefinition == null || referenceTilemap == null)
        {
            return;
        }

        Vector3 worldPosition = ScreenToWorld(screenPosition);
        currentCell = referenceTilemap.WorldToCell(worldPosition);
        hasCurrentCell = true;
        currentCellIsValid = CanPlaceAt(currentCell);

        if (previewInstance != null)
        {
            previewInstance.transform.position = referenceTilemap.GetCellCenterWorld(currentCell);
            previewInstance.SetActive(true);
        }

        if (previewRenderer != null)
        {
            previewRenderer.color = currentCellIsValid ? validPreviewColor : invalidPreviewColor;
        }
    }

    public bool EndDrag()
    {
        bool placed = false;

        if (CanUseBuildMode() && activeDefinition != null && hasCurrentCell && currentCellIsValid)
        {
            PlaceAt(currentCell);
            placed = true;
        }

        ClearPreview();
        activeDefinition = null;
        hasCurrentCell = false;
        currentCellIsValid = false;
        return placed;
    }

    public void CancelDrag()
    {
        ClearPreview();
        activeDefinition = null;
        hasCurrentCell = false;
        currentCellIsValid = false;
    }

    public bool CanUseBuildMode()
    {
        return gameStateManager == null || gameStateManager.CurrentPhase == LevelPhase.Planning;
    }

    private bool CanPlaceAt(Vector3Int cell)
    {
        if (activeDefinition == null || activeDefinition.Prefab == null || referenceTilemap == null)
        {
            return false;
        }

        if (blockedTilemaps == null)
        {
            return true;
        }

        for (int i = 0; i < blockedTilemaps.Length; i++)
        {
            Tilemap tilemap = blockedTilemaps[i];

            if (tilemap != null && tilemap.HasTile(cell))
            {
                return false;
            }
        }

        return true;
    }

    private void PlaceAt(Vector3Int cell)
    {
        GameObject prefab = activeDefinition.Prefab;
        Vector3 position = referenceTilemap.GetCellCenterWorld(cell);
        Instantiate(prefab, position, Quaternion.identity, placedObjectsRoot);
    }

    private void CreatePreview()
    {
        ClearPreview();

        if (activeDefinition == null || activeDefinition.Prefab == null)
        {
            return;
        }

        previewInstance = Instantiate(activeDefinition.Prefab);
        previewInstance.name = activeDefinition.Prefab.name + "_Preview";
        previewRenderer = previewInstance.GetComponentInChildren<SpriteRenderer>();

        Collider2D[] colliders = previewInstance.GetComponentsInChildren<Collider2D>();

        for (int i = 0; i < colliders.Length; i++)
        {
            colliders[i].enabled = false;
        }

        MonoBehaviour[] behaviours = previewInstance.GetComponentsInChildren<MonoBehaviour>();

        for (int i = 0; i < behaviours.Length; i++)
        {
            behaviours[i].enabled = false;
        }

        previewInstance.SetActive(false);
    }

    private void ClearPreview()
    {
        if (previewInstance != null)
        {
            Destroy(previewInstance);
        }

        previewInstance = null;
        previewRenderer = null;
    }

    private Vector3 ScreenToWorld(Vector2 screenPosition)
    {
        Camera cameraToUse = worldCamera != null ? worldCamera : Camera.main;
        Vector3 worldPosition = cameraToUse.ScreenToWorldPoint(screenPosition);
        worldPosition.z = 0f;
        return worldPosition;
    }

    private void ResolveSceneReferences()
    {
        if (worldCamera == null)
        {
            worldCamera = Camera.main;
        }

        if (referenceTilemap == null)
        {
            referenceTilemap = FindFirstObjectByType<Tilemap>();
        }

        if (blockedTilemaps == null || blockedTilemaps.Length == 0)
        {
            blockedTilemaps = FindObjectsByType<Tilemap>(FindObjectsSortMode.None);
        }

        if (placedObjectsRoot == null)
        {
            GameObject root = GameObject.Find("PlacedObjectsRoot");

            if (root == null)
            {
                root = new GameObject("PlacedObjectsRoot");
            }

            placedObjectsRoot = root.transform;
        }

        if (gameStateManager == null)
        {
            gameStateManager = GameStateManager.FindOrCreate();
        }
    }
}
