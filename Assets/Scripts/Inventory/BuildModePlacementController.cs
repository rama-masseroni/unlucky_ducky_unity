using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Tilemaps;

public class BuildModePlacementController : MonoBehaviour
{
    [SerializeField] private Camera worldCamera;
    [SerializeField] private Tilemap referenceTilemap;
    [SerializeField] private Tilemap[] blockedTilemaps;
    [SerializeField] private Transform placedObjectsRoot;
    [SerializeField] private GameStateManager gameStateManager;
    [SerializeField] private PlaceableInventoryPanel inventoryPanel;
    [SerializeField] private LayerMask occupancyMask = ~0;
    [SerializeField] private float occupancyBoxInset = 0.05f;
    [SerializeField] private Color validPreviewColor = new Color(1f, 1f, 1f, 0.65f);
    [SerializeField] private Color invalidPreviewColor = new Color(1f, 0.2f, 0.2f, 0.65f);

    private PlaceableDefinition activeDefinition;
    private PlacedPlaceableInstance activeMoveInstance;
    private GameObject previewInstance;
    private SpriteRenderer previewRenderer;
    private BombExplosionAreaVisualizer previewBombAreaVisualizer;
    private Vector3Int currentCell;
    private Vector3Int originalMoveCell;
    private Vector3 originalMovePosition;
    private bool hasCurrentCell;
    private bool currentCellIsValid;

    public bool HasActivePlacementInteraction => activeDefinition != null || activeMoveInstance != null;

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
            previewBombAreaVisualizer?.Show(referenceTilemap);
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

    public bool BeginMove(PlacedPlaceableInstance instance)
    {
        if (!CanUseBuildMode() || instance == null || instance.Definition == null || referenceTilemap == null)
        {
            return false;
        }

        activeMoveInstance = instance;
        activeDefinition = instance.Definition;
        originalMovePosition = instance.transform.position;
        originalMoveCell = referenceTilemap.WorldToCell(originalMovePosition);
        CreatePreview();
        SetInstanceVisible(instance, false);
        return true;
    }

    public void UpdateMove(Vector2 screenPosition)
    {
        if (activeMoveInstance == null)
        {
            return;
        }

        UpdateDrag(screenPosition);
    }

    public bool EndMove(Vector2 screenPosition)
    {
        if (activeMoveInstance == null)
        {
            ResetPlacementState();
            return false;
        }

        bool returnedToInventory = IsOverInventoryPanel(screenPosition)
            && ReturnToInventory(activeMoveInstance);

        if (returnedToInventory)
        {
            Destroy(activeMoveInstance.gameObject);
            ResetPlacementState();
            return true;
        }

        bool moved = CanUseBuildMode() && hasCurrentCell && currentCellIsValid;

        if (moved)
        {
            activeMoveInstance.transform.position = referenceTilemap.GetCellCenterWorld(currentCell);
            SetInstanceVisible(activeMoveInstance, true);
            ResetPlacementState();
            return true;
        }

        activeMoveInstance.transform.position = originalMovePosition;
        SetInstanceVisible(activeMoveInstance, true);
        ResetPlacementState();
        return false;
    }

    public void CancelDrag()
    {
        if (activeMoveInstance != null)
        {
            activeMoveInstance.transform.position = originalMovePosition;
            SetInstanceVisible(activeMoveInstance, true);
        }

        ResetPlacementState();
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

        if (IsCellBlockedByTilemap(cell))
        {
            return false;
        }

        return !IsCellOccupiedByCollider(cell);
    }

    private void PlaceAt(Vector3Int cell)
    {
        GameObject prefab = activeDefinition.Prefab;
        Vector3 position = referenceTilemap.GetCellCenterWorld(cell);
        GameObject instance = Instantiate(prefab, position, Quaternion.identity, placedObjectsRoot);
        PlacedPlaceableInstance placedInstance = instance.GetComponent<PlacedPlaceableInstance>();

        if (placedInstance == null)
        {
            placedInstance = instance.AddComponent<PlacedPlaceableInstance>();
        }

        placedInstance.Initialize(activeDefinition, this);
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
        previewBombAreaVisualizer = previewInstance.GetComponentInChildren<BombExplosionAreaVisualizer>(true);

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
        previewBombAreaVisualizer = null;
    }

    private void ResetPlacementState()
    {
        ClearPreview();
        activeDefinition = null;
        activeMoveInstance = null;
        hasCurrentCell = false;
        currentCellIsValid = false;
        originalMoveCell = default;
        originalMovePosition = default;
    }

    private bool IsOverInventoryPanel(Vector2 screenPosition)
    {
        if (inventoryPanel == null)
        {
            inventoryPanel = FindFirstObjectByType<PlaceableInventoryPanel>();
        }

        return inventoryPanel != null && inventoryPanel.ContainsScreenPoint(screenPosition);
    }

    private bool ReturnToInventory(PlacedPlaceableInstance instance)
    {
        if (inventoryPanel == null)
        {
            inventoryPanel = FindFirstObjectByType<PlaceableInventoryPanel>();
        }

        return inventoryPanel != null && inventoryPanel.TryReturnOne(instance.Definition);
    }

    private void SetInstanceVisible(PlacedPlaceableInstance instance, bool visible)
    {
        if (instance == null)
        {
            return;
        }

        SpriteRenderer[] renderers = instance.GetComponentsInChildren<SpriteRenderer>();

        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].enabled = visible;
        }

        Collider2D[] colliders = instance.GetComponentsInChildren<Collider2D>();

        for (int i = 0; i < colliders.Length; i++)
        {
            colliders[i].enabled = visible;
        }
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

        if (worldCamera != null && worldCamera.GetComponent<Physics2DRaycaster>() == null)
        {
            worldCamera.gameObject.AddComponent<Physics2DRaycaster>();
        }

        if (referenceTilemap == null)
        {
            referenceTilemap = FindFirstObjectByType<Tilemap>();
        }

        Tilemap[] sceneTilemaps = FindObjectsByType<Tilemap>(FindObjectsSortMode.None);

        if (blockedTilemaps == null || blockedTilemaps.Length == 0)
        {
            blockedTilemaps = sceneTilemaps;
        }
        else
        {
            blockedTilemaps = MergeTilemaps(blockedTilemaps, sceneTilemaps);
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

        if (inventoryPanel == null)
        {
            inventoryPanel = FindFirstObjectByType<PlaceableInventoryPanel>();
        }
    }

    private bool IsCellBlockedByTilemap(Vector3Int cell)
    {
        if (blockedTilemaps == null)
        {
            return false;
        }

        for (int i = 0; i < blockedTilemaps.Length; i++)
        {
            Tilemap tilemap = blockedTilemaps[i];

            if (tilemap != null && tilemap.HasTile(cell))
            {
                return true;
            }
        }

        return false;
    }

    private bool IsCellOccupiedByCollider(Vector3Int cell)
    {
        Vector3 cellCenter = referenceTilemap.GetCellCenterWorld(cell);
        Vector3 cellSize = referenceTilemap.layoutGrid.cellSize;
        Vector2 overlapSize = new Vector2(
            Mathf.Max(0.01f, cellSize.x - occupancyBoxInset),
            Mathf.Max(0.01f, cellSize.y - occupancyBoxInset));
        Collider2D[] hits = Physics2D.OverlapBoxAll(cellCenter, overlapSize, 0f, occupancyMask);

        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D hit = hits[i];

            if (hit == null || IsActiveMoveInstance(hit))
            {
                continue;
            }

            return true;
        }

        return false;
    }

    private bool IsActiveMoveInstance(Collider2D collider)
    {
        return activeMoveInstance != null
            && collider.GetComponentInParent<PlacedPlaceableInstance>() == activeMoveInstance;
    }

    private static Tilemap[] MergeTilemaps(Tilemap[] configuredTilemaps, Tilemap[] discoveredTilemaps)
    {
        if (configuredTilemaps == null || configuredTilemaps.Length == 0)
        {
            return discoveredTilemaps;
        }

        if (discoveredTilemaps == null || discoveredTilemaps.Length == 0)
        {
            return configuredTilemaps;
        }

        Tilemap[] mergedTilemaps = new Tilemap[configuredTilemaps.Length + discoveredTilemaps.Length];
        int count = 0;

        for (int i = 0; i < configuredTilemaps.Length; i++)
        {
            AddUniqueTilemap(mergedTilemaps, ref count, configuredTilemaps[i]);
        }

        for (int i = 0; i < discoveredTilemaps.Length; i++)
        {
            AddUniqueTilemap(mergedTilemaps, ref count, discoveredTilemaps[i]);
        }

        if (count == mergedTilemaps.Length)
        {
            return mergedTilemaps;
        }

        Tilemap[] trimmedTilemaps = new Tilemap[count];
        System.Array.Copy(mergedTilemaps, trimmedTilemaps, count);
        return trimmedTilemaps;
    }

    private static void AddUniqueTilemap(Tilemap[] tilemaps, ref int count, Tilemap candidate)
    {
        if (candidate == null)
        {
            return;
        }

        for (int i = 0; i < count; i++)
        {
            if (tilemaps[i] == candidate)
            {
                return;
            }
        }

        tilemaps[count] = candidate;
        count++;
    }
}
