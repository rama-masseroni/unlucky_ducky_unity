using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(Tilemap))]
public class FallingDestructibleTilemapLayer : MonoBehaviour, ILevelPhaseListener
{
    [SerializeField] private Tilemap[] supportTilemaps;
    [SerializeField] private LayerMask supportObjectMask = 0;
    [SerializeField] private float gravityScale = 1f;
    [SerializeField] private bool freezeRotation = true;
    [SerializeField] private FallingTileBlock fallingBlockPrefab;
    [SerializeField] private Transform runtimeRoot;
    [SerializeField] private float supportObjectBoxInset = 0.05f;

    private Tilemap tilemap;
    private bool isExecution;

    public Tilemap Tilemap => GetTilemap();

    private void Awake()
    {
        tilemap = GetComponent<Tilemap>();
    }

    private void OnEnable()
    {
        TilemapDestructionEvents.TileDestroyed += HandleTileDestroyed;

        GameStateManager gameStateManager = GameStateManager.Instance != null
            ? GameStateManager.Instance
            : FindFirstObjectByType<GameStateManager>();

        if (gameStateManager != null)
        {
            gameStateManager.RegisterListener(this);
        }
    }

    private void OnDisable()
    {
        TilemapDestructionEvents.TileDestroyed -= HandleTileDestroyed;
    }

    public void OnLevelPhaseChanged(LevelPhase phase)
    {
        isExecution = phase == LevelPhase.Execution;

        if (isExecution)
        {
            ResolveSupportTilemaps();
            EvaluateAllCells();
        }
    }

    private void HandleTileDestroyed(Tilemap destroyedTilemap, Vector3Int destroyedCell)
    {
        if (!isExecution || destroyedTilemap == null)
        {
            return;
        }

        EvaluateAllCells();
    }

    private void EvaluateAllCells()
    {
        Tilemap currentTilemap = GetTilemap();
        BoundsInt bounds = currentTilemap.cellBounds;

        for (int y = bounds.yMin; y < bounds.yMax; y++)
        {
            for (int x = bounds.xMin; x < bounds.xMax; x++)
            {
                TryConvertCellToFallingBlock(new Vector3Int(x, y, bounds.z));
            }
        }
    }

    private bool TryConvertCellToFallingBlock(Vector3Int cell)
    {
        Tilemap currentTilemap = GetTilemap();

        if (!currentTilemap.HasTile(cell) || HasSupportBelow(cell))
        {
            return false;
        }

        Sprite sprite = currentTilemap.GetSprite(cell);
        Color color = currentTilemap.GetColor(cell);
        Vector3 position = currentTilemap.GetCellCenterWorld(cell);
        Vector3 cellSize = currentTilemap.layoutGrid != null ? currentTilemap.layoutGrid.cellSize : Vector3.one;

        currentTilemap.SetTile(cell, null);
        RefreshTilemapCollider(currentTilemap);
        SpawnFallingBlock(sprite, color, position, cellSize);
        return true;
    }

    private bool HasSupportBelow(Vector3Int cell)
    {
        Vector3Int supportCell = new Vector3Int(cell.x, cell.y - 1, cell.z);
        Vector3 supportWorldPosition = GetTilemap().GetCellCenterWorld(supportCell);

        if (HasSupportTileAtWorldPosition(supportWorldPosition))
        {
            return true;
        }

        return HasSupportObjectAtWorldPosition(supportWorldPosition);
    }

    private bool HasSupportTileAtWorldPosition(Vector3 supportWorldPosition)
    {
        ResolveSupportTilemaps();

        if (supportTilemaps == null)
        {
            return false;
        }

        for (int i = 0; i < supportTilemaps.Length; i++)
        {
            Tilemap supportTilemap = supportTilemaps[i];

            if (!IsInitialSupportTilemap(supportTilemap))
            {
                continue;
            }

            if (supportTilemap.HasTile(supportTilemap.WorldToCell(supportWorldPosition)))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsInitialSupportTilemap(Tilemap supportTilemap)
    {
        return supportTilemap != null && supportTilemap.GetComponent<HazardTilemapLayer>() == null;
    }

    private bool HasSupportObjectAtWorldPosition(Vector3 supportWorldPosition)
    {
        if (supportObjectMask.value == 0)
        {
            return false;
        }

        Tilemap currentTilemap = GetTilemap();
        Vector3 cellSize = currentTilemap.layoutGrid != null ? currentTilemap.layoutGrid.cellSize : Vector3.one;
        Vector2 overlapSize = new Vector2(
            Mathf.Max(0.01f, cellSize.x - supportObjectBoxInset),
            Mathf.Max(0.01f, cellSize.y - supportObjectBoxInset));

        Collider2D[] hits = Physics2D.OverlapBoxAll(supportWorldPosition, overlapSize, 0f, supportObjectMask);
        return hits.Length > 0;
    }

    private void SpawnFallingBlock(Sprite sprite, Color color, Vector3 position, Vector3 cellSize)
    {
        Transform parent = GetRuntimeRoot();
        FallingTileBlock fallingBlock = fallingBlockPrefab != null
            ? Instantiate(fallingBlockPrefab, position, Quaternion.identity, parent)
            : CreateDefaultFallingBlock(position, parent);

        fallingBlock.Initialize(
            sprite,
            color,
            cellSize,
            gravityScale,
            freezeRotation,
            GetFallingBlockSupportTilemaps(),
            supportObjectMask);
    }

    private FallingTileBlock CreateDefaultFallingBlock(Vector3 position, Transform parent)
    {
        GameObject blockObject = new GameObject("FallingTileBlock");
        blockObject.transform.SetParent(parent);
        blockObject.transform.position = position;
        return blockObject.AddComponent<FallingTileBlock>();
    }

    private Transform GetRuntimeRoot()
    {
        if (runtimeRoot != null)
        {
            return runtimeRoot;
        }

        GameObject root = GameObject.Find("FallingBlocksRoot");

        if (root == null)
        {
            root = new GameObject("FallingBlocksRoot");
        }

        runtimeRoot = root.transform;
        return runtimeRoot;
    }

    private Tilemap GetTilemap()
    {
        if (tilemap == null)
        {
            tilemap = GetComponent<Tilemap>();
        }

        return tilemap;
    }

    private static void RefreshTilemapCollider(Tilemap tilemap)
    {
        TilemapCollider2D tilemapCollider = tilemap.GetComponent<TilemapCollider2D>();

        if (tilemapCollider != null)
        {
            tilemapCollider.ProcessTilemapChanges();
        }
    }

    private void ResolveSupportTilemaps()
    {
        if (supportTilemaps == null || supportTilemaps.Length == 0)
        {
            supportTilemaps = FindObjectsByType<Tilemap>(FindObjectsSortMode.None);
        }
    }

    private Tilemap[] GetFallingBlockSupportTilemaps()
    {
        ResolveSupportTilemaps();

        HashSet<Tilemap> uniqueTilemaps = new HashSet<Tilemap>();
        List<Tilemap> resolvedTilemaps = new List<Tilemap>();

        if (supportTilemaps != null)
        {
            for (int i = 0; i < supportTilemaps.Length; i++)
            {
                AddUniqueTilemap(supportTilemaps[i], uniqueTilemaps, resolvedTilemaps);
            }
        }

        HazardTilemapLayer[] hazardLayers = FindObjectsByType<HazardTilemapLayer>(FindObjectsSortMode.None);

        for (int i = 0; i < hazardLayers.Length; i++)
        {
            Tilemap hazardTilemap = hazardLayers[i] != null ? hazardLayers[i].Tilemap : null;
            AddUniqueTilemap(hazardTilemap, uniqueTilemaps, resolvedTilemaps);
        }

        return resolvedTilemaps.ToArray();
    }

    private static void AddUniqueTilemap(Tilemap tilemap, HashSet<Tilemap> uniqueTilemaps, List<Tilemap> resolvedTilemaps)
    {
        if (tilemap != null && uniqueTilemaps.Add(tilemap))
        {
            resolvedTilemaps.Add(tilemap);
        }
    }
}
