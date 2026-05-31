using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class PlacementAreaOverlayVisualizer : MonoBehaviour, ILevelPhaseListener
{
    [SerializeField] private Color fillColor = new Color(0.05f, 0.05f, 0.05f, 0.38f);
    [SerializeField] private Color stripeColor = new Color(0.18f, 0.18f, 0.18f, 0.48f);
    [SerializeField] private int sortingOrder = 6;

    private const int TextureSize = 32;
    private const int StripeWidthPixels = 4;
    private const int StripeSpacingPixels = 12;

    private readonly List<SpriteRenderer> cellRenderers = new List<SpriteRenderer>();
    private Transform cellsRoot;
    private Sprite overlaySprite;
    private Texture2D overlayTexture;
    private bool isPlanning = true;

    public int VisibleCellCount => cellRenderers.Count;
    public bool IsVisible => cellsRoot != null && cellsRoot.gameObject.activeSelf;

    private void OnDestroy()
    {
        DestroyGeneratedAssets();
    }

    public void OnLevelPhaseChanged(LevelPhase phase)
    {
        isPlanning = phase == LevelPhase.Planning;

        if (!isPlanning)
        {
            Hide();
        }
    }

    public void Show(Tilemap referenceTilemap, BoundsInt validArea, int padding)
    {
        if (!isPlanning || referenceTilemap == null || validArea.size.x <= 0 || validArea.size.y <= 0)
        {
            Hide();
            return;
        }

        EnsureCellsRoot();
        EnsureSprite();

        BoundsInt drawArea = Expand(validArea, Mathf.Max(0, padding));
        int requiredCount = Mathf.Max(0, (drawArea.size.x * drawArea.size.y) - (validArea.size.x * validArea.size.y));
        EnsureRendererCount(requiredCount);

        Vector3 cellSize = referenceTilemap.layoutGrid.cellSize;
        int rendererIndex = 0;

        for (int y = drawArea.yMin; y < drawArea.yMax; y++)
        {
            for (int x = drawArea.xMin; x < drawArea.xMax; x++)
            {
                Vector3Int cell = new Vector3Int(x, y, validArea.zMin);

                if (validArea.Contains(cell))
                {
                    continue;
                }

                SpriteRenderer renderer = cellRenderers[rendererIndex];
                renderer.transform.position = referenceTilemap.GetCellCenterWorld(cell);
                renderer.transform.localScale = new Vector3(cellSize.x, cellSize.y, 1f);
                renderer.sortingOrder = sortingOrder;
                renderer.enabled = true;
                rendererIndex++;
            }
        }

        cellsRoot.gameObject.SetActive(requiredCount > 0);
    }

    public void Show(Tilemap referenceTilemap, BoundsInt drawArea, HashSet<Vector3Int> validCells)
    {
        if (!isPlanning || referenceTilemap == null || drawArea.size.x <= 0 || drawArea.size.y <= 0 || validCells == null)
        {
            Hide();
            return;
        }

        EnsureCellsRoot();
        EnsureSprite();

        int requiredCount = Mathf.Max(0, (drawArea.size.x * drawArea.size.y) - validCells.Count);
        EnsureRendererCount(requiredCount);

        Vector3 cellSize = referenceTilemap.layoutGrid.cellSize;
        int rendererIndex = 0;

        for (int y = drawArea.yMin; y < drawArea.yMax; y++)
        {
            for (int x = drawArea.xMin; x < drawArea.xMax; x++)
            {
                Vector3Int cell = new Vector3Int(x, y, drawArea.zMin);

                if (validCells.Contains(cell))
                {
                    continue;
                }

                SpriteRenderer renderer = cellRenderers[rendererIndex];
                renderer.transform.position = referenceTilemap.GetCellCenterWorld(cell);
                renderer.transform.localScale = new Vector3(cellSize.x, cellSize.y, 1f);
                renderer.sortingOrder = sortingOrder;
                renderer.enabled = true;
                rendererIndex++;
            }
        }

        cellsRoot.gameObject.SetActive(requiredCount > 0);
    }

    public void Hide()
    {
        if (cellsRoot != null)
        {
            cellsRoot.gameObject.SetActive(false);
        }
    }

    private void EnsureCellsRoot()
    {
        if (cellsRoot != null)
        {
            return;
        }

        GameObject root = new GameObject("PlacementAreaInvalidOverlay");
        root.transform.SetParent(transform, false);
        cellsRoot = root.transform;
    }

    private void EnsureRendererCount(int requiredCount)
    {
        while (cellRenderers.Count < requiredCount)
        {
            GameObject cellObject = new GameObject("PlacementAreaInvalidCell");
            cellObject.transform.SetParent(cellsRoot, false);
            SpriteRenderer renderer = cellObject.AddComponent<SpriteRenderer>();
            renderer.sprite = overlaySprite;
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
        if (overlaySprite != null)
        {
            return;
        }

        overlayTexture = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, false);
        overlayTexture.filterMode = FilterMode.Point;
        overlayTexture.wrapMode = TextureWrapMode.Clamp;

        for (int y = 0; y < TextureSize; y++)
        {
            for (int x = 0; x < TextureSize; x++)
            {
                bool isStripe = PositiveModulo(x + y, StripeSpacingPixels) < StripeWidthPixels;
                overlayTexture.SetPixel(x, y, isStripe ? stripeColor : fillColor);
            }
        }

        overlayTexture.Apply();
        overlaySprite = Sprite.Create(
            overlayTexture,
            new Rect(0f, 0f, TextureSize, TextureSize),
            new Vector2(0.5f, 0.5f),
            TextureSize);
    }

    private void DestroyGeneratedAssets()
    {
        if (overlaySprite != null)
        {
            DestroyUnityObject(overlaySprite);
            overlaySprite = null;
        }

        if (overlayTexture != null)
        {
            DestroyUnityObject(overlayTexture);
            overlayTexture = null;
        }
    }

    private static BoundsInt Expand(BoundsInt bounds, int padding)
    {
        return new BoundsInt(
            new Vector3Int(bounds.xMin - padding, bounds.yMin - padding, bounds.zMin),
            new Vector3Int(bounds.size.x + padding * 2, bounds.size.y + padding * 2, bounds.size.z));
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
