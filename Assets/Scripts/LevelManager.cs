using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public class LevelManager : MonoBehaviour
{
    [SerializeField]
    private Tilemap tilemap;
    [SerializeField] private GameStateManager gameStateManager;

    private void Awake()
    {
        if (tilemap == null)
        {
            tilemap = GetComponentInChildren<Tilemap>();
        }

        if (gameStateManager == null)
        {
            gameStateManager = GameStateManager.FindOrCreate();
        }
    }

    private void Update()
    {
        if (tilemap == null || Mouse.current == null || Camera.main == null || !CanUseTileDestructionTool())
        {
            return;
        }

        if (Mouse.current.leftButton.wasPressedThisFrame && !IsPointerOverUi())
        {
            Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            TryUseTileDestructionTool(mousePosition);
        }
    }

    private bool CanUseTileDestructionTool()
    {
        return gameStateManager != null
            && gameStateManager.CurrentPhase == LevelPhase.Execution
            && gameStateManager.IsTileDestructionToolEnabled;
    }

    public bool TryUseTileDestructionTool(Vector3 worldPosition)
    {
        if (!CanUseTileDestructionTool())
        {
            return false;
        }

        PlaceableInventoryRuntimeEntry entry = GetTileDestructionToolEntry();

        if (entry == null || entry.Amount <= 0)
        {
            return false;
        }

        if (!TryDestroyTileAtWorldPosition(tilemap, worldPosition))
        {
            return false;
        }

        entry.TryConsumeOne();
        return true;
    }

    private PlaceableInventoryRuntimeEntry GetTileDestructionToolEntry()
    {
        if (gameStateManager == null || gameStateManager.Inventory == null)
        {
            return null;
        }

        return gameStateManager.Inventory.FindFirstEntryByUseMode(PlaceableUseMode.ExecutionClickToDestroyTile);
    }

    private static bool IsPointerOverUi()
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }

    public static bool TryDestroyTileAtWorldPosition(Tilemap tilemap, Vector3 worldPosition)
    {
        if (tilemap == null)
        {
            return false;
        }

        Vector3Int gridPosition = tilemap.WorldToCell(worldPosition);
        return TryDestroyTileAtCell(tilemap, gridPosition);
    }

    public static bool TryDestroyTileAtCell(Tilemap tilemap, Vector3Int gridPosition)
    {
        if (tilemap == null || !tilemap.HasTile(gridPosition))
        {
            return false;
        }

        tilemap.SetTile(gridPosition, null);
        RefreshTilemapCollider(tilemap);
        TilemapDestructionEvents.RaiseTileDestroyed(tilemap, gridPosition);
        return true;
    }

    private static void RefreshTilemapCollider(Tilemap tilemap)
    {
        TilemapCollider2D tilemapCollider = tilemap.GetComponent<TilemapCollider2D>();

        if (tilemapCollider != null)
        {
            tilemapCollider.ProcessTilemapChanges();
        }
    }
}
