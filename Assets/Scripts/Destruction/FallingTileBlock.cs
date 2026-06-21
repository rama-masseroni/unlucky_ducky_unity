using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class FallingTileBlock : MonoBehaviour, IBreakable, IGridWalkerSolid
{
    [SerializeField] private GameObject destructionEffectPrefab;
    [SerializeField] private LayerMask supportObjectMask = 0;
    [SerializeField] private LayerMask playerCrushMask = ~0;
    [SerializeField] private float supportProbeDistance = 0.08f;
    [SerializeField] private float supportSnapTolerance = 0.08f;

    private Tilemap[] supportTilemaps;
    private Rigidbody2D body;
    private BoxCollider2D boxCollider;
    private readonly List<Collider2D> ignoredWalkerColliders = new();
    private float lockedX;
    private bool hasPreviousBottomY;
    private float previousBottomY;

    public bool IsFalling => body != null && body.bodyType == RigidbodyType2D.Dynamic;

    private void OnEnable()
    {
        TilemapDestructionEvents.TileDestroyed += HandleTileDestroyed;
    }

    public void Initialize(
        Sprite sprite,
        Color color,
        Vector3 cellSize,
        float gravityScale,
        bool freezeRotation,
        Tilemap[] supportTilemaps,
        LayerMask supportObjectMask)
    {
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        spriteRenderer.sprite = sprite;
        spriteRenderer.color = color;

        boxCollider = GetComponent<BoxCollider2D>();
        Vector2 colliderSize = GetColliderSize(sprite, cellSize);
        boxCollider.size = colliderSize;
        boxCollider.offset = Vector2.zero;
        boxCollider.isTrigger = true;

        transform.localScale = GetScale(sprite, cellSize);

        body = GetComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Dynamic;
        body.gravityScale = gravityScale;
        body.freezeRotation = freezeRotation;
        body.linearVelocity = Vector2.zero;
        body.angularVelocity = 0f;
        body.simulated = true;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        body.constraints = RigidbodyConstraints2D.FreezePositionX;

        if (freezeRotation)
        {
            body.constraints |= RigidbodyConstraints2D.FreezeRotation;
        }

        body.WakeUp();

        lockedX = transform.position.x;
        this.supportTilemaps = supportTilemaps;
        this.supportObjectMask = supportObjectMask;
        IgnoreWalkerCollisions();
        RememberBottomY();
    }

    private void FixedUpdate()
    {
        if (body == null || body.bodyType != RigidbodyType2D.Dynamic)
        {
            return;
        }

        if (body.linearVelocity.y >= -0.01f)
        {
            RememberBottomY();
            return;
        }

        HashSet<Transform> crushedWalkerRoots = TryCrushActors();

        if (TryGetLandingY(crushedWalkerRoots, out float landingY))
        {
            Bounds bounds = boxCollider.bounds;
            Vector3 position = transform.position;
            position.x = lockedX;
            position.y += landingY + bounds.extents.y - bounds.center.y;
            transform.position = position;
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
            body.bodyType = RigidbodyType2D.Static;
            boxCollider.isTrigger = false;
            RestoreWalkerCollisions();
            RememberBottomY();
            return;
        }

        RememberBottomY();
    }

    public void Break()
    {
        RestoreWalkerCollisions();

        if (destructionEffectPrefab != null)
        {
            Instantiate(destructionEffectPrefab, transform.position, Quaternion.identity);
        }

        Destroy(gameObject);
    }

    private void OnDisable()
    {
        TilemapDestructionEvents.TileDestroyed -= HandleTileDestroyed;
        RestoreWalkerCollisions();
    }

    private static Vector2 GetColliderSize(Sprite sprite, Vector3 cellSize)
    {
        if (sprite == null)
        {
            return new Vector2(Mathf.Max(0.01f, cellSize.x), Mathf.Max(0.01f, cellSize.y));
        }

        return new Vector2(
            Mathf.Max(0.01f, sprite.bounds.size.x),
            Mathf.Max(0.01f, sprite.bounds.size.y));
    }

    private static Vector3 GetScale(Sprite sprite, Vector3 cellSize)
    {
        if (sprite == null)
        {
            return Vector3.one;
        }

        Vector3 spriteSize = sprite.bounds.size;
        float scaleX = spriteSize.x > 0f ? cellSize.x / spriteSize.x : 1f;
        float scaleY = spriteSize.y > 0f ? cellSize.y / spriteSize.y : 1f;
        return new Vector3(scaleX, scaleY, 1f);
    }

    private bool TryGetLandingY(HashSet<Transform> ignoredSupportRoots, out float landingY)
    {
        Bounds bounds = boxCollider.bounds;
        float currentBottomY = bounds.min.y;
        float sweepStartY = hasPreviousBottomY ? Mathf.Max(previousBottomY, currentBottomY) : currentBottomY;
        float horizontalInset = Mathf.Min(bounds.extents.x * 0.2f, 0.04f);
        float[] sampleXs =
        {
            bounds.min.x + horizontalInset,
            bounds.center.x,
            bounds.max.x - horizontalInset
        };
        landingY = float.NegativeInfinity;

        Vector2 centerSamplePosition = new Vector2(bounds.center.x, currentBottomY);

        if (TryGetTileLandingY(centerSamplePosition, sweepStartY, currentBottomY, out float tileLandingY))
        {
            landingY = Mathf.Max(landingY, tileLandingY);
        }

        for (int i = 0; i < sampleXs.Length; i++)
        {
            Vector2 samplePosition = new Vector2(sampleXs[i], currentBottomY);

            if (TryGetObjectSupportTop(
                samplePosition,
                sweepStartY,
                currentBottomY,
                ignoredSupportRoots,
                out float objectSupportTop))
            {
                landingY = Mathf.Max(landingY, objectSupportTop);
            }
        }

        if (landingY <= float.NegativeInfinity)
        {
            return false;
        }

        return landingY <= sweepStartY + supportSnapTolerance
            && landingY >= currentBottomY - supportProbeDistance;
    }

    private bool TryGetTileLandingY(Vector2 worldPosition, float sweepStartY, float currentBottomY, out float supportTop)
    {
        supportTop = float.NegativeInfinity;

        if (supportTilemaps == null)
        {
            return false;
        }

        for (int i = 0; i < supportTilemaps.Length; i++)
        {
            Tilemap supportTilemap = supportTilemaps[i];

            if (supportTilemap == null)
            {
                continue;
            }

            Vector3Int upperCell = supportTilemap.WorldToCell(new Vector3(worldPosition.x, sweepStartY, 0f));
            Vector3Int lowerCell = supportTilemap.WorldToCell(new Vector3(worldPosition.x, currentBottomY - supportProbeDistance, 0f));
            int minX = Mathf.Min(upperCell.x, lowerCell.x);
            int maxX = Mathf.Max(upperCell.x, lowerCell.x);
            int minY = Mathf.Min(upperCell.y, lowerCell.y);
            int maxY = Mathf.Max(upperCell.y, lowerCell.y);

            for (int x = minX; x <= maxX; x++)
            {
                for (int y = minY; y <= maxY; y++)
                {
                    Vector3Int supportCell = new Vector3Int(x, y, upperCell.z);

                    if (!supportTilemap.HasTile(supportCell))
                    {
                        continue;
                    }

                    float candidateTop = GetCellLandingWorldY(supportTilemap, supportCell);

                    if (candidateTop <= sweepStartY + supportSnapTolerance
                        && candidateTop >= currentBottomY - supportProbeDistance)
                    {
                        supportTop = Mathf.Max(supportTop, candidateTop);
                    }
                }
            }
        }

        return supportTop > float.NegativeInfinity;
    }

    private bool TryGetObjectSupportTop(
        Vector2 worldPosition,
        float sweepStartY,
        float currentBottomY,
        HashSet<Transform> ignoredSupportRoots,
        out float supportTop)
    {
        supportTop = float.NegativeInfinity;

        if (supportObjectMask.value == 0)
        {
            return false;
        }

        float sweepHeight = Mathf.Max(supportProbeDistance, sweepStartY - currentBottomY + supportProbeDistance);
        Vector2 boxCenter = new Vector2(worldPosition.x, currentBottomY + sweepHeight * 0.5f);
        Vector2 boxSize = new Vector2(supportProbeDistance * 2f, sweepHeight);
        Collider2D[] hits = Physics2D.OverlapBoxAll(boxCenter, boxSize, 0f, supportObjectMask);

        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D hit = hits[i];

            if (hit == null
                || hit.transform.IsChildOf(transform)
                || IsChildOfAny(hit.transform, ignoredSupportRoots))
            {
                continue;
            }

            float candidateTop = hit.bounds.max.y;

            if (candidateTop <= sweepStartY + supportSnapTolerance
                && candidateTop >= currentBottomY - supportProbeDistance)
            {
                supportTop = Mathf.Max(supportTop, candidateTop);
            }
        }

        return supportTop > float.NegativeInfinity;
    }

    private HashSet<Transform> TryCrushActors()
    {
        HashSet<Transform> crushedWalkerRoots = new();

        if (boxCollider == null || playerCrushMask.value == 0)
        {
            return crushedWalkerRoots;
        }

        Bounds bounds = boxCollider.bounds;
        float currentBottomY = bounds.min.y;
        float sweepStartY = hasPreviousBottomY ? Mathf.Max(previousBottomY, currentBottomY) : currentBottomY;
        float sweepHeight = Mathf.Max(supportProbeDistance, sweepStartY - currentBottomY + supportProbeDistance);
        Vector2 crushCenter = new Vector2(bounds.center.x, currentBottomY + sweepHeight * 0.5f);
        Vector2 crushSize = new Vector2(Mathf.Max(0.01f, bounds.size.x), sweepHeight);
        Collider2D[] hits = Physics2D.OverlapBoxAll(crushCenter, crushSize, 0f, playerCrushMask);
        HashSet<IBreakable> crushedBreakableWalkers = new();

        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D hit = hits[i];

            if (hit != null && !hit.transform.IsChildOf(transform))
            {
                GridWalkerController walker = hit.GetComponentInParent<GridWalkerController>();

                if (walker != null)
                {
                    crushedWalkerRoots.Add(walker.transform);
                }

                PlayerKillRules.TryKillPlayer(hit);
                TryBreakCrushedWalker(hit, crushedBreakableWalkers);
            }
        }

        return crushedWalkerRoots;
    }

    private static bool IsChildOfAny(Transform candidate, HashSet<Transform> roots)
    {
        if (candidate == null || roots == null)
        {
            return false;
        }

        foreach (Transform root in roots)
        {
            if (root != null && candidate.IsChildOf(root))
            {
                return true;
            }
        }

        return false;
    }

    private void HandleTileDestroyed(Tilemap _destroyedTilemap, Vector3Int _destroyedCell)
    {
        if (body == null || boxCollider == null || body.bodyType != RigidbodyType2D.Static)
        {
            return;
        }

        if (TryGetLandingY(null, out _))
        {
            return;
        }

        ReactivateFallingAfterSupportLoss();
    }

    private void ReactivateFallingAfterSupportLoss()
    {
        body.bodyType = RigidbodyType2D.Dynamic;
        body.linearVelocity = Vector2.zero;
        body.angularVelocity = 0f;
        boxCollider.isTrigger = true;
        IgnoreWalkerCollisions();
        RememberBottomY();
        body.WakeUp();
    }

    private void TryBreakCrushedWalker(Collider2D hit, HashSet<IBreakable> crushedBreakableWalkers)
    {
        GridWalkerController walker = hit.GetComponentInParent<GridWalkerController>();

        if (walker == null || walker.transform.IsChildOf(transform))
        {
            return;
        }

        IBreakable breakable = hit.GetComponentInParent<IBreakable>();

        if (breakable == null || ReferenceEquals(breakable, this) || !crushedBreakableWalkers.Add(breakable))
        {
            return;
        }

        breakable.Break();
    }

    private void RememberBottomY()
    {
        if (boxCollider == null)
        {
            return;
        }

        previousBottomY = boxCollider.bounds.min.y;
        hasPreviousBottomY = true;
    }

    private void IgnoreWalkerCollisions()
    {
        if (boxCollider == null)
        {
            return;
        }

        GridWalkerController[] walkers = FindObjectsByType<GridWalkerController>(FindObjectsSortMode.None);

        for (int i = 0; i < walkers.Length; i++)
        {
            GridWalkerController walker = walkers[i];

            if (walker == null)
            {
                continue;
            }

            Collider2D[] walkerColliders = walker.GetComponentsInChildren<Collider2D>();

            for (int colliderIndex = 0; colliderIndex < walkerColliders.Length; colliderIndex++)
            {
                Collider2D walkerCollider = walkerColliders[colliderIndex];

                if (walkerCollider == null || walkerCollider.transform.IsChildOf(transform))
                {
                    continue;
                }

                Physics2D.IgnoreCollision(boxCollider, walkerCollider, true);
                ignoredWalkerColliders.Add(walkerCollider);
            }
        }
    }

    private void RestoreWalkerCollisions()
    {
        if (boxCollider == null)
        {
            ignoredWalkerColliders.Clear();
            return;
        }

        for (int i = 0; i < ignoredWalkerColliders.Count; i++)
        {
            Collider2D walkerCollider = ignoredWalkerColliders[i];

            if (walkerCollider != null)
            {
                Physics2D.IgnoreCollision(boxCollider, walkerCollider, false);
            }
        }

        ignoredWalkerColliders.Clear();
    }

    private static float GetCellLandingWorldY(Tilemap tilemap, Vector3Int cell)
    {
        Vector3 center = tilemap.GetCellCenterWorld(cell);
        Grid grid = tilemap.layoutGrid;
        float cellHeight = grid != null ? grid.cellSize.y * Mathf.Abs(grid.transform.lossyScale.y) : 1f;
        return center.y + cellHeight * 0.5f;
    }
}
