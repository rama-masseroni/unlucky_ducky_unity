using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class SensorController : MonoBehaviour
{
    [SerializeField] private string connectionId = "A";
    [SerializeField] private GameStateManager gameStateManager;
    [SerializeField] private MonoBehaviour[] connectedReceivers;
    [SerializeField] private bool autoDiscoverReceivers = true;

    public string ConnectionId => connectionId;

    private void Awake()
    {
        Collider2D sensorCollider = GetComponent<Collider2D>();

        if (sensorCollider != null)
        {
            sensorCollider.isTrigger = true;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryActivate(other);
    }

    public bool TryActivate(Collider2D other)
    {
        GridWalkerController activator = other != null ? other.GetComponentInParent<GridWalkerController>() : null;

        if (activator == null)
        {
            return false;
        }

        GameStateManager resolvedGameStateManager = GetGameStateManager();

        if (resolvedGameStateManager == null || resolvedGameStateManager.CurrentPhase != LevelPhase.Execution)
        {
            return false;
        }

        int notifiedReceivers = NotifyReceivers(activator);
        Debug.Log($"Sensor '{name}' activated by '{activator.name}'.", this);

        if (notifiedReceivers == 0)
        {
            Debug.LogWarning($"Sensor '{name}' has no receivers connected to '{connectionId}'.", this);
        }

        return true;
    }

    private GameStateManager GetGameStateManager()
    {
        if (gameStateManager == null)
        {
            gameStateManager = GameStateManager.FindOrCreate();
        }

        return gameStateManager;
    }

    private int NotifyReceivers(GridWalkerController activator)
    {
        int notifiedReceivers = 0;

        if (connectedReceivers == null)
        {
            connectedReceivers = System.Array.Empty<MonoBehaviour>();
        }

        for (int i = 0; i < connectedReceivers.Length; i++)
        {
            if (TryNotifyReceiver(connectedReceivers[i], activator))
            {
                notifiedReceivers++;
            }
        }

        if (!autoDiscoverReceivers)
        {
            return notifiedReceivers;
        }

        MonoBehaviour[] behaviours = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);

        for (int i = 0; i < behaviours.Length; i++)
        {
            MonoBehaviour behaviour = behaviours[i];

            if (IsExplicitReceiver(behaviour))
            {
                continue;
            }

            if (TryNotifyReceiver(behaviour, activator))
            {
                notifiedReceivers++;
            }
        }

        return notifiedReceivers;
    }

    private bool TryNotifyReceiver(MonoBehaviour behaviour, GridWalkerController activator)
    {
        if (behaviour is not ISensorReceiver receiver || !ConnectionMatches(receiver))
        {
            return false;
        }

        receiver.OnSensorActivated(this, activator);
        return true;
    }

    private bool ConnectionMatches(ISensorReceiver receiver)
    {
        return string.Equals(receiver.SensorConnectionId, connectionId, System.StringComparison.Ordinal);
    }

    private bool IsExplicitReceiver(MonoBehaviour behaviour)
    {
        for (int i = 0; i < connectedReceivers.Length; i++)
        {
            if (connectedReceivers[i] == behaviour)
            {
                return true;
            }
        }

        return false;
    }
}
