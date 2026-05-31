using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class BombController : MonoBehaviour, IBreakable, ILevelPhaseListener
{
    [SerializeField] private float explosionDelaySeconds = 3f;
    [SerializeField] private int explosionRadiusInCells = 1;
    [SerializeField] private Tilemap referenceTilemap;
    [SerializeField] private Tilemap[] destructibleTilemaps;
    [SerializeField] private LayerMask destructibleObjectMask = ~0;
    [SerializeField] private bool killsPlayerInExplosionArea = true;
    [SerializeField] private LayerMask playerKillMask = ~0;
    [SerializeField] private bool destroyBombAfterExplosion = true;
    [SerializeField] private BombExplosionAreaVisualizer areaVisualizer;

    private bool hasExploded;
    private bool hasStartedCountdown;
    private Coroutine explosionCoroutine;
    private GameStateManager gameStateManager;

    public bool HasStartedCountdown => hasStartedCountdown;
    public int ExplosionRadiusInCells => explosionRadiusInCells;

    public Tilemap ReferenceTilemap
    {
        get
        {
            if (referenceTilemap == null)
            {
                ResolveTilemaps();
            }

            return referenceTilemap;
        }
    }

    private void Awake()
    {
        ResolveTilemaps();
        ResolveAreaVisualizer();

        if (areaVisualizer != null)
        {
            areaVisualizer.Show(referenceTilemap);
        }
    }

    private void OnEnable()
    {
        gameStateManager = GameStateManager.FindOrCreate();
        gameStateManager.RegisterListener(this);
    }

    private void OnDisable()
    {
        if (explosionCoroutine != null)
        {
            StopCoroutine(explosionCoroutine);
            explosionCoroutine = null;
        }

        if (areaVisualizer != null)
        {
            areaVisualizer.Clear();
        }
    }

    public void OnLevelPhaseChanged(LevelPhase phase)
    {
        if (phase == LevelPhase.Execution)
        {
            StartExplosionCountdown();
        }
    }

    public void Break()
    {
        Destroy(gameObject);
    }

    private IEnumerator ExplodeAfterDelay()
    {
        yield return new WaitForSeconds(explosionDelaySeconds);
        Explode();
    }

    private void StartExplosionCountdown()
    {
        if (hasExploded || hasStartedCountdown)
        {
            return;
        }

        hasStartedCountdown = true;
        explosionCoroutine = StartCoroutine(ExplodeAfterDelay());
    }

    public void Explode()
    {
        if (hasExploded)
        {
            return;
        }

        hasExploded = true;
        ResolveTilemaps();
        ClearAreaVisualizer();

        if (referenceTilemap == null)
        {
            return;
        }

        Vector3Int centerCell = referenceTilemap.WorldToCell(transform.position);
        IReadOnlyList<Vector3Int> affectedCells = BombExplosionArea.GetCells(centerCell, explosionRadiusInCells);

        TilemapDestructionEvents.BeginBatch();

        try
        {
            DestroyTiles(affectedCells);
            BreakObjectsInCells(affectedCells);
            KillPlayersInCells(affectedCells);
        }
        finally
        {
            TilemapDestructionEvents.EndBatch();
        }

        if (destroyBombAfterExplosion)
        {
            Destroy(gameObject);
        }
    }

    private void DestroyTiles(IReadOnlyList<Vector3Int> affectedCells)
    {
        if (destructibleTilemaps == null)
        {
            return;
        }

        for (int tilemapIndex = 0; tilemapIndex < destructibleTilemaps.Length; tilemapIndex++)
        {
            Tilemap tilemap = destructibleTilemaps[tilemapIndex];

            if (tilemap == null)
            {
                continue;
            }

            for (int cellIndex = 0; cellIndex < affectedCells.Count; cellIndex++)
            {
                Vector3 cellCenter = referenceTilemap.GetCellCenterWorld(affectedCells[cellIndex]);
                Vector3Int tilemapCell = tilemap.WorldToCell(cellCenter);
                LevelManager.TryDestroyTileAtCell(tilemap, tilemapCell);
            }
        }
    }

    private void BreakObjectsInCells(IReadOnlyList<Vector3Int> affectedCells)
    {
        HashSet<GameObject> brokenObjects = new HashSet<GameObject>();
        HashSet<Vector3Int> affectedCellSet = new HashSet<Vector3Int>(affectedCells);

        for (int i = 0; i < affectedCells.Count; i++)
        {
            Vector3 cellCenter = referenceTilemap.GetCellCenterWorld(affectedCells[i]);
            Collider2D[] hits = Physics2D.OverlapBoxAll(cellCenter, referenceTilemap.layoutGrid.cellSize, 0f, destructibleObjectMask);

            for (int hitIndex = 0; hitIndex < hits.Length; hitIndex++)
            {
                Collider2D hit = hits[hitIndex];

                if (hit == null || hit.gameObject == gameObject)
                {
                    continue;
                }

                IBreakable breakable = hit.GetComponentInParent<IBreakable>();

                if (breakable == null)
                {
                    continue;
                }

                Component breakableComponent = breakable as Component;
                GameObject breakableObject = breakableComponent != null ? breakableComponent.gameObject : hit.gameObject;

                if (brokenObjects.Contains(breakableObject)
                    || !IsBreakableInAffectedCell(breakableComponent, hit, affectedCellSet))
                {
                    continue;
                }

                brokenObjects.Add(breakableObject);
                breakable.Break();
            }
        }
    }

    private bool IsBreakableInAffectedCell(
        Component breakableComponent,
        Collider2D hit,
        HashSet<Vector3Int> affectedCellSet)
    {
        Vector3 samplePosition = breakableComponent != null
            ? breakableComponent.transform.position
            : hit.transform.position;
        Vector3Int objectCell = referenceTilemap.WorldToCell(samplePosition);
        return affectedCellSet.Contains(objectCell);
    }

    private void KillPlayersInCells(IReadOnlyList<Vector3Int> affectedCells)
    {
        if (!killsPlayerInExplosionArea)
        {
            return;
        }

        HashSet<Vector3Int> affectedCellSet = new HashSet<Vector3Int>(affectedCells);

        for (int i = 0; i < affectedCells.Count; i++)
        {
            Vector3 cellCenter = referenceTilemap.GetCellCenterWorld(affectedCells[i]);
            Collider2D[] hits = Physics2D.OverlapBoxAll(cellCenter, referenceTilemap.layoutGrid.cellSize, 0f, playerKillMask);

            for (int hitIndex = 0; hitIndex < hits.Length; hitIndex++)
            {
                Collider2D hit = hits[hitIndex];

                if (hit == null || hit.gameObject == gameObject)
                {
                    continue;
                }

                if (!IsPlayerInAffectedCell(hit, affectedCellSet))
                {
                    continue;
                }

                PlayerKillRules.TryKillPlayer(hit);
            }
        }
    }

    private bool IsPlayerInAffectedCell(Collider2D hit, HashSet<Vector3Int> affectedCellSet)
    {
        PlayerDuckController player = hit.GetComponentInParent<PlayerDuckController>();
        Vector3 samplePosition = player != null ? player.transform.position : hit.transform.position;
        Vector3Int playerCell = referenceTilemap.WorldToCell(samplePosition);
        return affectedCellSet.Contains(playerCell);
    }

    private void ResolveTilemaps()
    {
        if (destructibleTilemaps == null || destructibleTilemaps.Length == 0)
        {
            DestructibleTilemapLayer[] destructibleLayers = FindObjectsByType<DestructibleTilemapLayer>(FindObjectsSortMode.None);
            destructibleTilemaps = new Tilemap[destructibleLayers.Length];

            for (int i = 0; i < destructibleLayers.Length; i++)
            {
                destructibleTilemaps[i] = destructibleLayers[i].Tilemap;
            }
        }

        if (referenceTilemap == null)
        {
            referenceTilemap = destructibleTilemaps != null && destructibleTilemaps.Length > 0
                ? destructibleTilemaps[0]
                : FindFirstObjectByType<Tilemap>();
        }
    }

    private void ResolveAreaVisualizer()
    {
        if (areaVisualizer == null)
        {
            areaVisualizer = GetComponent<BombExplosionAreaVisualizer>();
        }
    }

    private void ClearAreaVisualizer()
    {
        ResolveAreaVisualizer();

        if (areaVisualizer != null)
        {
            areaVisualizer.Clear();
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(transform.position, Vector3.one * (explosionRadiusInCells * 2 + 1));
    }
}
