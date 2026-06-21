using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class SensorDoorController : MonoBehaviour, ISensorReceiver, IGridWalkerSolid
{
    [SerializeField] private string sensorConnectionId = "A";
    [SerializeField] private bool startsOpen;
    [SerializeField] private Collider2D blockingCollider;
    [SerializeField] private SpriteRenderer doorRenderer;
    [SerializeField] private Sprite closedSprite;
    [SerializeField] private Sprite openSprite;

    public string SensorConnectionId => sensorConnectionId;
    public bool IsOpen { get; private set; }

    private void Awake()
    {
        if (blockingCollider == null)
        {
            blockingCollider = GetComponent<Collider2D>();
        }

        if (doorRenderer == null)
        {
            doorRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        SetOpen(startsOpen);
    }

    public void OnSensorActivated(SensorController sensor, Component activator)
    {
        SetOpen(!IsOpen);
    }

    public void SetOpen(bool open)
    {
        IsOpen = open;

        if (blockingCollider != null)
        {
            blockingCollider.enabled = !IsOpen;
            blockingCollider.isTrigger = false;
        }

        if (doorRenderer != null)
        {
            Sprite sprite = IsOpen ? openSprite : closedSprite;

            if (sprite != null)
            {
                doorRenderer.sprite = sprite;
            }
        }
    }
}
