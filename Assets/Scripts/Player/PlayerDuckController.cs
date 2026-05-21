using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections;

public class PlayerDuckController : GridWalkerController
{
    [SerializeField] private bool resetLevelOnDeath = true;
    [SerializeField] private float deathResetDelaySeconds = 0.5f;
    [SerializeField] private GameStateManager gameStateManager;
    [SerializeField] private float hazardContactProbeDistance = 0.25f;

    private bool isDead;
    private Coroutine deathResetCoroutine;
    private LevelPhase currentPhase = LevelPhase.Planning;

    public bool IsDead => isDead;

    protected override void Awake()
    {
        base.Awake();
    }

    public void Kill()
    {
        if (isDead)
        {
            return;
        }

        isDead = true;
        SetVisible(false);

        if (resetLevelOnDeath && deathResetCoroutine == null)
        {
            deathResetCoroutine = StartCoroutine(ResetLevelAfterDeath());
        }

        if (Body != null)
        {
            Body.linearVelocity = Vector2.zero;
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
    }

    public override void OnLevelPhaseChanged(LevelPhase phase)
    {
        currentPhase = phase;
        base.OnLevelPhaseChanged(phase);
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
        return tilemap.HasTile(tilemap.WorldToCell(worldPosition));
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
