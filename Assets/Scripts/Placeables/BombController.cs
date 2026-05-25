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

    private bool hasExploded;
    private bool hasStartedCountdown;
    private Coroutine explosionCoroutine;
    private GameStateManager gameStateManager;

    public bool HasStartedCountdown => hasStartedCountdown;

    private void Awake()
    {
        ResolveTilemaps();
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

        if (referenceTilemap == null)
        {
            return;
        }

        Vector3Int centerCell = referenceTilemap.WorldToCell(transform.position);
        IReadOnlyList<Vector3Int> affectedCells = BombExplosionArea.GetCells(centerCell, explosionRadiusInCells);

        DestroyTiles(affectedCells);
        BreakObjectsInCells(affectedCells);
        KillPlayersInCells(affectedCells);

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
                LevelManager.TryDestroyTileAtCell(tilemap, affectedCells[cellIndex]);
            }
        }
    }

    private void BreakObjectsInCells(IReadOnlyList<Vector3Int> affectedCells)
    {
        HashSet<GameObject> brokenObjects = new HashSet<GameObject>();

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

                if (brokenObjects.Contains(breakableObject))
                {
                    continue;
                }

                brokenObjects.Add(breakableObject);
                breakable.Break();
            }
        }
    }

    private void KillPlayersInCells(IReadOnlyList<Vector3Int> affectedCells)
    {
        if (!killsPlayerInExplosionArea)
        {
            return;
        }

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

                PlayerKillRules.TryKillPlayer(hit);
            }
        }
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

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(transform.position, Vector3.one * (explosionRadiusInCells * 2 + 1));
    }
}
