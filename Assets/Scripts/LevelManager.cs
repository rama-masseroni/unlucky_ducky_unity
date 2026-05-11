using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.InputSystem;

public class LevelManager : MonoBehaviour
{
    [SerializeField]
    private Tilemap tilemap;

    private void Awake()
    {
        if (tilemap == null)
        {
            tilemap = GetComponentInChildren<Tilemap>();
        }
    }

    private void Update()
    {
        if (tilemap == null || Mouse.current == null || Camera.main == null)
        {
            return;
        }

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            TryDestroyTileAtWorldPosition(tilemap, mousePosition);
        }
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
