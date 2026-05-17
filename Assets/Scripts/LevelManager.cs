using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.InputSystem;

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
        if (tilemap == null || Mouse.current == null || Camera.main == null || !CanDestroyTilesByClick())
        {
            return;
        }

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            TryDestroyTileAtWorldPosition(tilemap, mousePosition);
        }
    }

    private bool CanDestroyTilesByClick()
    {
        return gameStateManager != null
            && gameStateManager.CurrentPhase == LevelPhase.Planning
            && gameStateManager.IsPlanningTileDestructionEnabled;
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
        return true;
    }
}
