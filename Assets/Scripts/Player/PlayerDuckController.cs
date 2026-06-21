using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class PlayerDuckController : GridWalkerController
{
    [SerializeField] private bool resetLevelOnDeath = true;
    [SerializeField] private float deathResetDelaySeconds = 0.5f;
    [SerializeField] private GameStateManager gameStateManager;
    [SerializeField] private float hazardContactProbeDistance = 0.25f;
    [SerializeField] private bool logBlocksTraveledPerSecond = true;
    [SerializeField] private float movementLogIntervalSeconds = 1f;
    [SerializeField] private Grid movementLogReferenceGrid;

    private bool isDead;
    private Coroutine deathResetCoroutine;
    private LevelPhase currentPhase = LevelPhase.Planning;
    private Vector3 movementLogSamplePosition;
    private float movementLogElapsedSeconds;
    private float movementLogBlockSize = 1f;

    public bool IsDead => isDead;
    public static Func<PlayerDuckController, bool> DeathScreenHandler { get; set; }

    protected override void Awake()
    {
        base.Awake();
        ResolveMovementLogBlockSize();
        ResetMovementLogSample();
    }

    public void Kill()
    {
        if (isDead)
        {
            return;
        }

        isDead = true;
        SetVisible(false);

        if (Body != null)
        {
            Body.linearVelocity = Vector2.zero;
        }

        if (resetLevelOnDeath && deathResetCoroutine == null)
        {
            if (DeathScreenHandler != null && DeathScreenHandler.Invoke(this))
            {
                return;
            }

            deathResetCoroutine = StartCoroutine(ResetLevelAfterDeath());
        }
    }

    protected override void FixedUpdate()
    {
        if (isDead)
        {
            if (Body != null)
            {
                Body.linearVelocity = Vector2.zero;
            }

            return;
        }

        TryKillIfTouchingHazard();

        if (isDead)
        {
            return;
        }

        base.FixedUpdate();
        TryKillIfTouchingHazard();
        UpdateMovementDistanceLog();
    }

    public override void OnLevelPhaseChanged(LevelPhase phase)
    {
        currentPhase = phase;
        base.OnLevelPhaseChanged(phase);

        if (phase == LevelPhase.Execution)
        {
            ResetMovementLogSample();
        }
    }

    public bool TryKillIfTouchingHazard()
    {
        if (isDead || currentPhase != LevelPhase.Execution || !IsTouchingHazardTile())
        {
            return false;
        }

        Kill();
        return true;
    }

    private IEnumerator ResetLevelAfterDeath()
    {
        if (deathResetDelaySeconds > 0f)
        {
            yield return new WaitForSeconds(deathResetDelaySeconds);
        }

        GameStateManager resolvedGameStateManager = GetGameStateManager();

        if (resolvedGameStateManager != null)
        {
            resolvedGameStateManager.ResetCurrentLevel();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private GameStateManager GetGameStateManager()
    {
        if (gameStateManager == null)
        {
            gameStateManager = GameStateManager.FindOrCreate();
        }

        return gameStateManager;
    }

    private void ResolveMovementLogBlockSize()
    {
        if (movementLogReferenceGrid == null)
        {
            movementLogReferenceGrid = FindFirstObjectByType<Grid>();
        }

        if (movementLogReferenceGrid == null)
        {
            movementLogBlockSize = 1f;
            return;
        }

        Vector3 cellSize = movementLogReferenceGrid.cellSize;
        movementLogBlockSize = Mathf.Max(Mathf.Abs(cellSize.x), Mathf.Abs(cellSize.y), 0.01f);
    }

    private void ResetMovementLogSample()
    {
        movementLogSamplePosition = transform.position;
        movementLogElapsedSeconds = 0f;
    }

    private void UpdateMovementDistanceLog()
    {
        if (!logBlocksTraveledPerSecond
            || currentPhase != LevelPhase.Execution
            || movementLogIntervalSeconds <= 0f)
        {
            return;
        }

        movementLogElapsedSeconds += Time.fixedDeltaTime;

        if (movementLogElapsedSeconds < movementLogIntervalSeconds)
        {
            return;
        }

        float worldDistance = Vector2.Distance(movementLogSamplePosition, transform.position);
        float blocksTraveled = worldDistance / movementLogBlockSize;
        float blocksPerInterval = blocksTraveled * movementLogIntervalSeconds / movementLogElapsedSeconds;

        Debug.Log($"Ducky viajo {blocksPerInterval:F2} bloques en {movementLogIntervalSeconds:F2} s.", this);
        ResetMovementLogSample();
    }

    private bool IsTouchingHazardTile()
    {
        Collider2D bodyCollider = GetComponent<Collider2D>();

        if (bodyCollider == null)
        {
            return false;
        }

        Bounds bounds = bodyCollider.bounds;
        IReadOnlyList<HazardTilemapLayer> hazardLayers = HazardTilemapLayer.ActiveLayers;

        for (int i = 0; i < hazardLayers.Count; i++)
        {
            HazardTilemapLayer hazardLayer = hazardLayers[i];
            Tilemap tilemap = hazardLayer != null ? hazardLayer.Tilemap : null;

            if (tilemap != null && ColliderTouchesTilemap(tilemap, bounds))
            {
                return true;
            }
        }

        return false;
    }

    private bool ColliderTouchesTilemap(Tilemap tilemap, Bounds bounds)
    {
        return BoundsOverlapTile(tilemap, bounds)
            || HasHazardAtBottomProbe(tilemap, bounds)
            || HasHazardAtFrontProbe(tilemap, bounds);
    }

    private bool BoundsOverlapTile(Tilemap tilemap, Bounds bounds)
    {
        Vector3Int minCell = tilemap.WorldToCell(bounds.min);
        Vector3Int maxCell = tilemap.WorldToCell(bounds.max);

        for (int x = minCell.x; x <= maxCell.x; x++)
        {
            for (int y = minCell.y; y <= maxCell.y; y++)
            {
                if (tilemap.HasTile(new Vector3Int(x, y, minCell.z)))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private bool HasHazardAtBottomProbe(Tilemap tilemap, Bounds bounds)
    {
        float probeY = bounds.min.y - hazardContactProbeDistance;
        float[] sampleXs = { bounds.min.x, bounds.center.x, bounds.max.x };

        for (int i = 0; i < sampleXs.Length; i++)
        {
            if (HasTileAtWorldPosition(tilemap, new Vector2(sampleXs[i], probeY)))
            {
                return true;
            }
        }

        return false;
    }

    private bool HasHazardAtFrontProbe(Tilemap tilemap, Bounds bounds)
    {
        float probeX = HorizontalDirection >= 0
            ? bounds.max.x + hazardContactProbeDistance
            : bounds.min.x - hazardContactProbeDistance;
        float[] sampleYs = { bounds.min.y, bounds.center.y, bounds.max.y };

        for (int i = 0; i < sampleYs.Length; i++)
        {
            if (HasTileAtWorldPosition(tilemap, new Vector2(probeX, sampleYs[i])))
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasTileAtWorldPosition(Tilemap tilemap, Vector2 worldPosition)
    {
        if (!tilemap.HasTile(tilemap.WorldToCell(worldPosition)))
        {
            return false;
        }

        return !HasGridWalkerSolidCoverAtWorldPosition(worldPosition);
    }

    private static bool HasGridWalkerSolidCoverAtWorldPosition(Vector2 worldPosition)
    {
        Collider2D[] hits = Physics2D.OverlapPointAll(worldPosition);

        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D hit = hits[i];

            if (hit != null && hit.GetComponentInParent<IGridWalkerSolid>() != null)
            {
                return true;
            }
        }

        return false;
    }

    private void SetVisible(bool visible)
    {
        SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>();

        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].enabled = visible;
        }

        Collider2D[] colliders = GetComponentsInChildren<Collider2D>();

        for (int i = 0; i < colliders.Length; i++)
        {
            colliders[i].enabled = visible;
        }
    }
}
