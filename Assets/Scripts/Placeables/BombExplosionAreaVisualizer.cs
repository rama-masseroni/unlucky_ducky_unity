using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class BombExplosionAreaVisualizer : MonoBehaviour
{
    [SerializeField] private Tilemap referenceTilemap;
    [SerializeField] private int explosionRadiusInCells = 1;
    [SerializeField] private Color fillColor = new Color(1f, 0f, 0f, 0.28f);
    [SerializeField] private Color stripeColor = new Color(0.85f, 0f, 0f, 0.75f);
    [SerializeField] private Color borderColor = new Color(0.35f, 0f, 0f, 0.9f);
    [SerializeField] private int sortingOrder = 8;

    private const int TextureSize = 32;
    private const int BorderPixels = 2;
    private const int StripeWidthPixels = 5;
    private const int StripeSpacingPixels = 12;

    private readonly List<SpriteRenderer> cellRenderers = new List<SpriteRenderer>();
    private Transform cellsRoot;
    private Sprite areaSprite;
    private Texture2D areaTexture;
    private BombController bombController;

    public int VisibleCellCount => cellRenderers.Count;
    public bool IsVisible => cellsRoot != null && cellsRoot.gameObject.activeSelf;

    private void Awake()
    {
        bombController = GetComponent<BombController>();
    }

    private void OnEnable()
    {
        Show();
    }

    private void OnDisable()
    {
        Hide();
    }

    private void OnDestroy()
    {
        DestroyGeneratedAssets();
    }

    private void LateUpdate()
    {
        if (IsVisible)
        {
            Show();
        }
    }

    public void Show()
    {
        Tilemap tilemap = ResolveReferenceTilemap();

        if (tilemap == null)
        {
            Hide();
            return;
        }

        int radius = ResolveExplosionRadius();
        Vector3Int centerCell = tilemap.WorldToCell(transform.position);
        IReadOnlyList<Vector3Int> affectedCells = BombExplosionArea.GetCells(centerCell, radius);
        EnsureCellsRoot();
        EnsureSprite();
        EnsureRendererCount(affectedCells.Count);

        Vector3 cellSize = tilemap.layoutGrid.cellSize;

        for (int i = 0; i < affectedCells.Count; i++)
        {
            SpriteRenderer renderer = cellRenderers[i];
            renderer.transform.position = tilemap.GetCellCenterWorld(affectedCells[i]);
            renderer.transform.localScale = new Vector3(cellSize.x, cellSize.y, 1f);
            renderer.sortingOrder = sortingOrder;
            renderer.enabled = true;
        }

        cellsRoot.gameObject.SetActive(true);
    }

    public void Show(Tilemap tilemap)
    {
        referenceTilemap = tilemap;
        Show();
    }

    public void Hide()
    {
        if (cellsRoot != null)
        {
            cellsRoot.gameObject.SetActive(false);
        }
    }

    public void Clear()
    {
        Hide();

        if (cellsRoot != null)
        {
            DestroyUnityObject(cellsRoot.gameObject);
            cellsRoot = null;
        }

        cellRenderers.Clear();
    }

    private Tilemap ResolveReferenceTilemap()
    {
        if (referenceTilemap != null)
        {
            return referenceTilemap;
        }

        if (bombController != null && bombController.ReferenceTilemap != null)
        {
            referenceTilemap = bombController.ReferenceTilemap;
            return referenceTilemap;
        }

        referenceTilemap = FindFirstObjectByType<Tilemap>();
        return referenceTilemap;
    }

    private int ResolveExplosionRadius()
    {
        if (bombController != null)
        {
            explosionRadiusInCells = bombController.ExplosionRadiusInCells;
        }

        return Mathf.Max(0, explosionRadiusInCells);
    }

    private void EnsureCellsRoot()
    {
        if (cellsRoot != null)
        {
            return;
        }

        GameObject root = new GameObject("BombExplosionAreaVisual");
        root.transform.SetParent(transform, false);
        cellsRoot = root.transform;
    }

    private void EnsureRendererCount(int requiredCount)
    {
        while (cellRenderers.Count < requiredCount)
        {
            GameObject cellObject = new GameObject("BombExplosionAreaCell");
            cellObject.transform.SetParent(cellsRoot, false);
            SpriteRenderer renderer = cellObject.AddComponent<SpriteRenderer>();
            renderer.sprite = areaSprite;
            renderer.sortingOrder = sortingOrder;
            cellRenderers.Add(renderer);
        }

        for (int i = 0; i < cellRenderers.Count; i++)
        {
            cellRenderers[i].enabled = i < requiredCount;
        }
    }

    private void EnsureSprite()
    {
        if (areaSprite != null)
        {
            return;
        }

        areaTexture = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, false);
        areaTexture.filterMode = FilterMode.Point;
        areaTexture.wrapMode = TextureWrapMode.Clamp;

        for (int y = 0; y < TextureSize; y++)
        {
            for (int x = 0; x < TextureSize; x++)
            {
                bool isBorder = x < BorderPixels
                    || y < BorderPixels
                    || x >= TextureSize - BorderPixels
                    || y >= TextureSize - BorderPixels;
                bool isStripe = PositiveModulo(x + y, StripeSpacingPixels) < StripeWidthPixels;
                Color pixelColor = isBorder ? borderColor : isStripe ? stripeColor : fillColor;
                areaTexture.SetPixel(x, y, pixelColor);
            }
        }

        areaTexture.Apply();
        areaSprite = Sprite.Create(
            areaTexture,
            new Rect(0f, 0f, TextureSize, TextureSize),
            new Vector2(0.5f, 0.5f),
            TextureSize);
    }

    private void DestroyGeneratedAssets()
    {
        if (areaSprite != null)
        {
            DestroyUnityObject(areaSprite);
            areaSprite = null;
        }

        if (areaTexture != null)
        {
            DestroyUnityObject(areaTexture);
            areaTexture = null;
        }
    }

    private static void DestroyUnityObject(Object target)
    {
        if (target == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(target);
        }
        else
        {
            DestroyImmediate(target);
        }
    }

    private static int PositiveModulo(int value, int modulo)
    {
        int result = value % modulo;
        return result < 0 ? result + modulo : result;
    }
}
