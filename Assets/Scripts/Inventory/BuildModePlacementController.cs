using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Tilemaps;

public enum PlacementAreaMode
{
    Rectangle = 0,
    WallBoundary = 1
}

public class BuildModePlacementController : MonoBehaviour, ILevelPhaseListener, IExecutionStartValidator
{
    [Header("Scene References")]
    [SerializeField] private Camera worldCamera;
    [SerializeField] private Tilemap referenceTilemap;
    [SerializeField] private Tilemap[] blockedTilemaps;
    [SerializeField] private Transform placedObjectsRoot;
    [SerializeField] private GameStateManager gameStateManager;
    [SerializeField] private PlaceableInventoryPanel inventoryPanel;

    [Header("Placement Area")]
    [SerializeField] private bool usePlacementAreaLimit = true;
    [SerializeField] private PlacementAreaMode placementAreaMode = PlacementAreaMode.WallBoundary;
    [SerializeField] private Tilemap wallBoundaryTilemap;
    [SerializeField] private bool useManualPlacementArea = false;
    [SerializeField] private Vector3Int placementAreaMinCell = new Vector3Int(-8, -4, 0);
    [SerializeField] private Vector2Int placementAreaSize = new Vector2Int(16, 8);
    [SerializeField] private int automaticPlacementAreaPadding = 2;
    [SerializeField] private int wallBoundarySearchPadding = 2;
    [SerializeField] private int invalidAreaOverlayPadding = 4;
    [SerializeField] private PlacementAreaOverlayVisualizer placementAreaOverlayVisualizer;

    [Header("Placement Rules")]
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
    private bool hasResolvedPlacementArea;
    private HashSet<Vector3Int> wallBoundaryPlacementCells;
    private BoundsInt wallBoundaryDrawArea;
    private string placementAreaValidationError;

    public bool HasActivePlacementInteraction => activeDefinition != null || activeMoveInstance != null;

    private void Awake()
    {
        ResolveSceneReferences();
        RefreshPlacementAreaOverlay();
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

    public void OnLevelPhaseChanged(LevelPhase phase)
    {
        if (phase == LevelPhase.Planning)
        {
            RefreshPlacementAreaOverlay();
        }
        else
        {
            placementAreaOverlayVisualizer?.Hide();
        }
    }

    public BoundsInt PlacementArea => new BoundsInt(
        placementAreaMinCell,
        new Vector3Int(Mathf.Max(0, placementAreaSize.x), Mathf.Max(0, placementAreaSize.y), 1));

    public bool CanStartExecution(out string failureReason)
    {
        ResolvePlacementArea();
        failureReason = placementAreaValidationError;
        return string.IsNullOrWhiteSpace(failureReason);
    }

    private bool CanPlaceAt(Vector3Int cell)
    {
        if (activeDefinition == null || activeDefinition.Prefab == null || referenceTilemap == null)
        {
            return false;
        }

        if (!IsCellInsidePlacementArea(cell))
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

        hasResolvedPlacementArea = false;
        ResolvePlacementArea();
        ResolvePlacementAreaOverlayVisualizer();
    }

    private void ResolvePlacementArea()
    {
        if (!usePlacementAreaLimit || hasResolvedPlacementArea)
        {
            hasResolvedPlacementArea = true;
            return;
        }

        placementAreaValidationError = null;

        if (placementAreaMode == PlacementAreaMode.WallBoundary)
        {
            ResolveWallBoundaryTilemap();

            if (wallBoundaryTilemap == null)
            {
                placementAreaValidationError = "Execution blocked: Placement Area Mode is WallBoundary, but no Walls Tilemap was assigned or found by name.";
                wallBoundaryPlacementCells = null;
                hasResolvedPlacementArea = true;
                return;
            }

            if (!TryBuildWallBoundaryPlacementArea())
            {
                placementAreaValidationError = $"Execution blocked: '{wallBoundaryTilemap.gameObject.name}' does not define a closed wall boundary with a valid interior placement area.";
                wallBoundaryPlacementCells = null;
                hasResolvedPlacementArea = true;
                return;
            }

            placementAreaValidationError = null;
            hasResolvedPlacementArea = true;
            return;
        }

        if (useManualPlacementArea)
        {
            hasResolvedPlacementArea = true;
            return;
        }

        if (TryGetUsedTilemapBounds(blockedTilemaps, out BoundsInt bounds)
            || referenceTilemap != null && TryGetUsedTilemapBounds(new[] { referenceTilemap }, out bounds))
        {
            ApplyAutomaticPlacementArea(bounds);
        }

        hasResolvedPlacementArea = true;
    }

    private void ResolveWallBoundaryTilemap()
    {
        if (wallBoundaryTilemap != null)
        {
            return;
        }

        wallBoundaryTilemap = FindWallBoundaryTilemap(blockedTilemaps);

        if (wallBoundaryTilemap == null)
        {
            Tilemap[] sceneTilemaps = FindObjectsByType<Tilemap>(FindObjectsSortMode.None);
            wallBoundaryTilemap = FindWallBoundaryTilemap(sceneTilemaps);
        }
    }

    private bool TryBuildWallBoundaryPlacementArea()
    {
        if (wallBoundaryTilemap == null || wallBoundaryTilemap.GetUsedTilesCount() == 0)
        {
            return false;
        }

        BoundsInt wallBounds = wallBoundaryTilemap.cellBounds;
        int padding = Mathf.Max(1, wallBoundarySearchPadding);
        BoundsInt searchBounds = Expand(wallBounds, padding);
        HashSet<Vector3Int> wallCells = GetTileCells(wallBoundaryTilemap);
        HashSet<Vector3Int> outsideCells = FloodFillOutside(searchBounds, wallCells);
        HashSet<Vector3Int> validCells = new HashSet<Vector3Int>();

        for (int y = searchBounds.yMin; y < searchBounds.yMax; y++)
        {
            for (int x = searchBounds.xMin; x < searchBounds.xMax; x++)
            {
                Vector3Int cell = new Vector3Int(x, y, searchBounds.zMin);

                if (!wallCells.Contains(cell) && !outsideCells.Contains(cell))
                {
                    validCells.Add(cell);
                }
            }
        }

        if (validCells.Count == 0)
        {
            return false;
        }

        wallBoundaryPlacementCells = validCells;
        wallBoundaryDrawArea = searchBounds;
        return true;
    }

    private void ApplyAutomaticPlacementArea(BoundsInt bounds)
    {
        int padding = Mathf.Max(0, automaticPlacementAreaPadding);
        placementAreaMinCell = new Vector3Int(bounds.xMin - padding, bounds.yMin - padding, bounds.zMin);
        placementAreaSize = new Vector2Int(bounds.size.x + padding * 2, bounds.size.y + padding * 2);
    }

    private static bool TryGetUsedTilemapBounds(Tilemap[] tilemaps, out BoundsInt bounds)
    {
        bounds = default;
        bool hasBounds = false;

        if (tilemaps == null)
        {
            return false;
        }

        for (int i = 0; i < tilemaps.Length; i++)
        {
            Tilemap tilemap = tilemaps[i];

            if (tilemap == null || tilemap.GetUsedTilesCount() == 0)
            {
                continue;
            }

            BoundsInt tilemapBounds = tilemap.cellBounds;

            if (!hasBounds)
            {
                bounds = tilemapBounds;
                hasBounds = true;
                continue;
            }

            bounds = Encapsulate(bounds, tilemapBounds);
        }

        return hasBounds;
    }

    private static Tilemap FindWallBoundaryTilemap(Tilemap[] tilemaps)
    {
        if (tilemaps == null)
        {
            return null;
        }

        for (int i = 0; i < tilemaps.Length; i++)
        {
            Tilemap tilemap = tilemaps[i];

            if (tilemap == null)
            {
                continue;
            }

            string objectName = tilemap.gameObject.name.ToLowerInvariant();

            if (objectName.Contains("wall") || objectName.Contains("walls"))
            {
                return tilemap;
            }
        }

        return null;
    }

    private static HashSet<Vector3Int> GetTileCells(Tilemap tilemap)
    {
        HashSet<Vector3Int> tileCells = new HashSet<Vector3Int>();
        BoundsInt bounds = tilemap.cellBounds;

        for (int y = bounds.yMin; y < bounds.yMax; y++)
        {
            for (int x = bounds.xMin; x < bounds.xMax; x++)
            {
                Vector3Int cell = new Vector3Int(x, y, bounds.zMin);

                if (tilemap.HasTile(cell))
                {
                    tileCells.Add(cell);
                }
            }
        }

        return tileCells;
    }

    private static HashSet<Vector3Int> FloodFillOutside(BoundsInt bounds, HashSet<Vector3Int> blockedCells)
    {
        HashSet<Vector3Int> outsideCells = new HashSet<Vector3Int>();
        Queue<Vector3Int> frontier = new Queue<Vector3Int>();
        Vector3Int startCell = new Vector3Int(bounds.xMin, bounds.yMin, bounds.zMin);

        if (!blockedCells.Contains(startCell))
        {
            frontier.Enqueue(startCell);
            outsideCells.Add(startCell);
        }

        Vector3Int[] directions =
        {
            Vector3Int.left,
            Vector3Int.right,
            Vector3Int.up,
            Vector3Int.down
        };

        while (frontier.Count > 0)
        {
            Vector3Int current = frontier.Dequeue();

            for (int i = 0; i < directions.Length; i++)
            {
                Vector3Int next = current + directions[i];

                if (!bounds.Contains(next) || blockedCells.Contains(next) || outsideCells.Contains(next))
                {
                    continue;
                }

                outsideCells.Add(next);
                frontier.Enqueue(next);
            }
        }

        return outsideCells;
    }

    private static BoundsInt Expand(BoundsInt bounds, int padding)
    {
        return new BoundsInt(
            new Vector3Int(bounds.xMin - padding, bounds.yMin - padding, bounds.zMin),
            new Vector3Int(bounds.size.x + padding * 2, bounds.size.y + padding * 2, Mathf.Max(1, bounds.size.z)));
    }

    private static BoundsInt Encapsulate(BoundsInt first, BoundsInt second)
    {
        int xMin = Mathf.Min(first.xMin, second.xMin);
        int yMin = Mathf.Min(first.yMin, second.yMin);
        int zMin = Mathf.Min(first.zMin, second.zMin);
        int xMax = Mathf.Max(first.xMax, second.xMax);
        int yMax = Mathf.Max(first.yMax, second.yMax);
        int zMax = Mathf.Max(first.zMax, second.zMax);
        return new BoundsInt(
            new Vector3Int(xMin, yMin, zMin),
            new Vector3Int(xMax - xMin, yMax - yMin, zMax - zMin));
    }

    private void ResolvePlacementAreaOverlayVisualizer()
    {
        if (placementAreaOverlayVisualizer != null)
        {
            return;
        }

        placementAreaOverlayVisualizer = GetComponent<PlacementAreaOverlayVisualizer>();

        if (placementAreaOverlayVisualizer == null)
        {
            placementAreaOverlayVisualizer = gameObject.AddComponent<PlacementAreaOverlayVisualizer>();
        }
    }

    private void RefreshPlacementAreaOverlay()
    {
        if (!usePlacementAreaLimit || placementAreaOverlayVisualizer == null)
        {
            placementAreaOverlayVisualizer?.Hide();
            return;
        }

        if (CanUseBuildMode())
        {
            if (placementAreaMode == PlacementAreaMode.WallBoundary && wallBoundaryPlacementCells != null)
            {
                placementAreaOverlayVisualizer.Show(referenceTilemap, wallBoundaryDrawArea, wallBoundaryPlacementCells);
            }
            else
            {
                placementAreaOverlayVisualizer.Show(referenceTilemap, PlacementArea, invalidAreaOverlayPadding);
            }
        }
        else
        {
            placementAreaOverlayVisualizer.Hide();
        }
    }

    private bool IsCellInsidePlacementArea(Vector3Int cell)
    {
        if (!usePlacementAreaLimit)
        {
            return true;
        }

        ResolvePlacementArea();

        if (!string.IsNullOrWhiteSpace(placementAreaValidationError))
        {
            return false;
        }

        if (placementAreaMode == PlacementAreaMode.WallBoundary && wallBoundaryPlacementCells != null)
        {
            return wallBoundaryPlacementCells.Contains(new Vector3Int(cell.x, cell.y, wallBoundaryDrawArea.zMin));
        }

        BoundsInt area = PlacementArea;
        return area.size.x > 0 && area.size.y > 0 && area.Contains(new Vector3Int(cell.x, cell.y, area.zMin));
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
