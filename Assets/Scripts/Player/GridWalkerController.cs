using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(Rigidbody2D))]
public class GridWalkerController : MonoBehaviour, ILevelPhaseListener
{
    [Header("Movement")]
    [SerializeField] private float walkSpeed = 1f;
    [SerializeField] private float fallSpeed = 4f;
    [SerializeField] private float slopeSpeedMultiplier = 0.5f;
    [SerializeField] private int initialDirection = 1;

    [Header("Ground Check")]
    [SerializeField] private float groundCheckDistance = 0.08f;
    [SerializeField] private Vector2 groundCheckOffset = new Vector2(-0.24f, -0.28f);
    [SerializeField] private Tilemap[] groundTilemaps;
    [SerializeField] private LayerMask groundObjectMask = 0;
    [SerializeField] private float groundObjectProbeRadius = 0.04f;

    [Header("Obstacle Check")]
    [SerializeField] private float obstacleCheckDistance = 0.08f;
    [SerializeField] private Vector2 obstacleCheckOffset = new Vector2(0.24f, -0.05f);
    [SerializeField] private float obstacleCheckHeight = 0.24f;
    [SerializeField] private Tilemap[] obstacleTilemaps;
    [SerializeField] private LayerMask obstacleObjectMask = 0;
    [SerializeField] private float obstacleObjectProbeRadius = 0.04f;

    private Rigidbody2D body;
    private int horizontalDirection;
    private bool canMove;
    private SpriteRenderer spriteRenderer;

    public float WalkSpeed
    {
        get => walkSpeed;
        set => walkSpeed = Mathf.Max(0f, value);
    }

    protected int HorizontalDirection => horizontalDirection;
    protected Rigidbody2D Body => body;
    public int FacingDirection => horizontalDirection == 0 ? NormalizeDirection(initialDirection) : horizontalDirection;

    protected virtual void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;
        body.freezeRotation = true;
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        SetFacingDirection(initialDirection);
        canMove = false;

        if (groundTilemaps == null || groundTilemaps.Length == 0)
        {
            groundTilemaps = FindObjectsByType<Tilemap>(FindObjectsSortMode.None);
        }

        groundTilemaps = ExcludePlacementBoundaryTilemaps(groundTilemaps);

        if (obstacleTilemaps == null || obstacleTilemaps.Length == 0)
        {
            obstacleTilemaps = groundTilemaps;
        }
        else
        {
            obstacleTilemaps = ExcludePlacementBoundaryTilemaps(obstacleTilemaps);
        }
    }

    private static Tilemap[] ExcludePlacementBoundaryTilemaps(Tilemap[] tilemaps)
    {
        if (tilemaps == null || tilemaps.Length == 0)
        {
            return tilemaps;
        }

        List<Tilemap> gameplayTilemaps = new List<Tilemap>(tilemaps.Length);

        for (int i = 0; i < tilemaps.Length; i++)
        {
            Tilemap tilemap = tilemaps[i];

            if (tilemap != null && tilemap.GetComponent<IGameplayIgnoredTilemap>() == null)
            {
                gameplayTilemaps.Add(tilemap);
            }
        }

        return gameplayTilemaps.ToArray();
    }

    protected virtual void FixedUpdate()
    {
        if (!canMove)
        {
            if (body != null)
            {
                body.linearVelocity = Vector2.zero;
            }

            return;
        }

        GroundProbe groundProbe = ProbeGround();
        Vector2 velocity = DuckMovementRules.ResolveVelocity(
            groundProbe.HasGround,
            groundProbe.Normal,
            walkSpeed,
            fallSpeed,
            slopeSpeedMultiplier,
            horizontalDirection);

        if (groundProbe.HasGround && HasObstacleAhead())
        {
            SetFacingDirection(-horizontalDirection);
            velocity = DuckMovementRules.ResolveVelocity(
                groundProbe.HasGround,
                groundProbe.Normal,
                walkSpeed,
                fallSpeed,
                slopeSpeedMultiplier,
                horizontalDirection);
        }

        body.linearVelocity = velocity;
    }

    public void SetFacingDirection(int direction)
    {
        int normalizedDirection = NormalizeDirection(direction);
        initialDirection = normalizedDirection;
        horizontalDirection = normalizedDirection;
        SyncSpriteFacing();
    }

    public void ToggleFacingDirection()
    {
        SetFacingDirection(-FacingDirection);
    }

    public virtual void OnLevelPhaseChanged(LevelPhase phase)
    {
        canMove = phase == LevelPhase.Execution;

        if (!canMove && body != null)
        {
            body.linearVelocity = Vector2.zero;
        }
    }

    protected void StopMovement()
    {
        if (body != null)
        {
            body.linearVelocity = Vector2.zero;
        }

        enabled = false;
    }

    private GroundProbe ProbeGround()
    {
        Vector2 origin = (Vector2)transform.position + GetDirectionalGroundCheckOffset();

        if (TryProbeTilemapGround(origin, out GroundProbe tilemapProbe))
        {
            return tilemapProbe;
        }

        if (HasGroundObjectAlongProbe(origin))
        {
            return new GroundProbe(true, Vector2.up);
        }

        return new GroundProbe(false, Vector2.up);
    }

    private bool TryProbeTilemapGround(Vector2 worldPosition, out GroundProbe groundProbe)
    {
        if (groundTilemaps == null)
        {
            groundProbe = new GroundProbe(false, Vector2.up);
            return false;
        }

        for (int i = 0; i < groundTilemaps.Length; i++)
        {
            Tilemap tilemap = groundTilemaps[i];

            if (tilemap == null)
            {
                continue;
            }

            if (HasTileAlongProbe(tilemap, worldPosition))
            {
                groundProbe = new GroundProbe(true, Vector2.up);
                return true;
            }
        }

        groundProbe = new GroundProbe(false, Vector2.up);
        return false;
    }

    private bool HasTileAlongProbe(Tilemap tilemap, Vector2 worldPosition)
    {
        const int sampleCount = 4;

        for (int i = 0; i <= sampleCount; i++)
        {
            float distance = groundCheckDistance * i / sampleCount;
            Vector3Int cellPosition = tilemap.WorldToCell(worldPosition + Vector2.down * distance);

            if (tilemap.HasTile(cellPosition))
            {
                return true;
            }
        }

        return false;
    }

    private bool HasGroundObjectAlongProbe(Vector2 worldPosition)
    {
        const int sampleCount = 4;

        for (int i = 0; i <= sampleCount; i++)
        {
            float distance = groundCheckDistance * i / sampleCount;
            Vector2 samplePosition = worldPosition + Vector2.down * distance;

            if (HasColliderAtProbe(samplePosition, groundObjectProbeRadius, groundObjectMask)
                || HasGridWalkerSolidAtProbe(samplePosition, groundObjectProbeRadius))
            {
                return true;
            }
        }

        return false;
    }

    private bool HasObstacleAhead()
    {
        Vector2 origin = (Vector2)transform.position + GetDirectionalObstacleCheckOffset();
        const int verticalSamples = 3;

        for (int y = 0; y < verticalSamples; y++)
        {
            float t = verticalSamples == 1 ? 0f : y / (float)(verticalSamples - 1);
            float verticalOffset = Mathf.Lerp(-obstacleCheckHeight * 0.5f, obstacleCheckHeight * 0.5f, t);
            Vector2 sampleOrigin = origin + Vector2.up * verticalOffset;

            if (HasObstacleTileAlongProbe(sampleOrigin))
            {
                return true;
            }

            if (HasObstacleObjectAlongProbe(sampleOrigin))
            {
                return true;
            }
        }

        return false;
    }

    private bool HasObstacleTileAlongProbe(Vector2 worldPosition)
    {
        if (obstacleTilemaps == null)
        {
            return false;
        }

        const int horizontalSamples = 4;

        for (int i = 0; i < obstacleTilemaps.Length; i++)
        {
            Tilemap tilemap = obstacleTilemaps[i];

            if (tilemap == null)
            {
                continue;
            }

            for (int x = 0; x <= horizontalSamples; x++)
            {
                float distance = obstacleCheckDistance * x / horizontalSamples;
                Vector2 samplePosition = worldPosition + Vector2.right * horizontalDirection * distance;
                Vector3Int cellPosition = tilemap.WorldToCell(samplePosition);

                if (tilemap.HasTile(cellPosition))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private bool HasObstacleObjectAlongProbe(Vector2 worldPosition)
    {
        const int horizontalSamples = 4;

        for (int x = 0; x <= horizontalSamples; x++)
        {
            float distance = obstacleCheckDistance * x / horizontalSamples;
            Vector2 samplePosition = worldPosition + Vector2.right * horizontalDirection * distance;

            if (HasColliderAtProbe(samplePosition, obstacleObjectProbeRadius, obstacleObjectMask)
                || HasGridWalkerSolidAtProbe(samplePosition, obstacleObjectProbeRadius))
            {
                return true;
            }
        }

        return false;
    }

    private bool HasColliderAtProbe(Vector2 position, float radius, LayerMask mask)
    {
        if (mask.value == 0)
        {
            return false;
        }

        Collider2D[] hits = Physics2D.OverlapCircleAll(position, radius, mask);

        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D hit = hits[i];

            if (hit != null && !hit.transform.IsChildOf(transform))
            {
                return true;
            }
        }

        return false;
    }

    private bool HasGridWalkerSolidAtProbe(Vector2 position, float radius)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(position, radius);

        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D hit = hits[i];

            if (hit != null
                && !hit.transform.IsChildOf(transform)
                && hit.GetComponentInParent<IGridWalkerSolid>() != null)
            {
                return true;
            }
        }

        return false;
    }

    private Vector2 GetDirectionalGroundCheckOffset()
    {
        return new Vector2(Mathf.Abs(groundCheckOffset.x) * -horizontalDirection, groundCheckOffset.y);
    }

    private Vector2 GetDirectionalObstacleCheckOffset()
    {
        return new Vector2(Mathf.Abs(obstacleCheckOffset.x) * horizontalDirection, obstacleCheckOffset.y);
    }

    private void SyncSpriteFacing()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = FacingDirection < 0;
        }
    }

    private static int NormalizeDirection(int direction)
    {
        return direction >= 0 ? 1 : -1;
    }

    private void OnDrawGizmosSelected()
    {
        int direction = Application.isPlaying ? FacingDirection : NormalizeDirection(initialDirection);
        Vector2 offset = new Vector2(Mathf.Abs(groundCheckOffset.x) * -direction, groundCheckOffset.y);
        Vector2 origin = (Vector2)transform.position + offset;

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(origin, origin + Vector2.down * groundCheckDistance);
        Gizmos.DrawSphere(origin, 0.025f);

        Vector2 obstacleOffset = new Vector2(Mathf.Abs(obstacleCheckOffset.x) * direction, obstacleCheckOffset.y);
        Vector2 obstacleOrigin = (Vector2)transform.position + obstacleOffset;
        Vector2 lower = obstacleOrigin + Vector2.down * obstacleCheckHeight * 0.5f;
        Vector2 upper = obstacleOrigin + Vector2.up * obstacleCheckHeight * 0.5f;

        Gizmos.color = Color.red;
        Gizmos.DrawLine(lower, upper);
        Gizmos.DrawLine(lower, lower + Vector2.right * direction * obstacleCheckDistance);
        Gizmos.DrawLine(obstacleOrigin, obstacleOrigin + Vector2.right * direction * obstacleCheckDistance);
        Gizmos.DrawLine(upper, upper + Vector2.right * direction * obstacleCheckDistance);
    }

    private readonly struct GroundProbe
    {
        public GroundProbe(bool hasGround, Vector2 normal)
        {
            HasGround = hasGround;
            Normal = normal;
        }

        public bool HasGround { get; }
        public Vector2 Normal { get; }
    }
}
