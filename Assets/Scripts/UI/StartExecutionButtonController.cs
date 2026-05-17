using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class StartExecutionButtonController : MonoBehaviour
{
    [SerializeField] private GameStateManager gameStateManager;
    [SerializeField] private string planningLabel = "PROBAR NIVEL";
    [SerializeField] private string executionLabel = "EN EJECUCION";

    private Button button;
    private Text label;

    private void Awake()
    {
        button = GetComponent<Button>();
        label = GetComponentInChildren<Text>();

        if (gameStateManager == null)
        {
            gameStateManager = GameStateManager.FindOrCreate();
        }

        button.onClick.AddListener(StartExecution);
    }

    private void OnEnable()
    {
        if (gameStateManager == null)
        {
            gameStateManager = GameStateManager.FindOrCreate();
        }

        gameStateManager.InventoryChanged += Refresh;
        gameStateManager.PhaseChanged += HandlePhaseChanged;
        Refresh();
    }

    private void OnDisable()
    {
        if (gameStateManager == null)
        {
            return;
        }

        gameStateManager.InventoryChanged -= Refresh;
        gameStateManager.PhaseChanged -= HandlePhaseChanged;
    }

    public void SetGameStateManager(GameStateManager manager)
    {
        if (gameStateManager != null && isActiveAndEnabled)
        {
            gameStateManager.InventoryChanged -= Refresh;
            gameStateManager.PhaseChanged -= HandlePhaseChanged;
        }

        gameStateManager = manager;

        if (gameStateManager != null && isActiveAndEnabled)
        {
            gameStateManager.InventoryChanged += Refresh;
            gameStateManager.PhaseChanged += HandlePhaseChanged;
        }

        Refresh();
    }

    private void StartExecution()
    {
        if (gameStateManager != null)
        {
            gameStateManager.TryStartExecution();
        }
    }

    private void HandlePhaseChanged(LevelPhase phase)
    {
        Refresh();
    }

    private void Refresh()
    {
        if (button == null)
        {
            button = GetComponent<Button>();
        }

        if (label == null)
        {
            label = GetComponentInChildren<Text>();
        }

        LevelPhase phase = gameStateManager != null ? gameStateManager.CurrentPhase : LevelPhase.Planning;
        bool isPlanning = phase == LevelPhase.Planning;

        if (button != null)
        {
            button.interactable = gameStateManager != null && isPlanning && gameStateManager.CanStartExecution;
        }

        if (label != null)
        {
            label.text = isPlanning ? planningLabel : executionLabel;
        }
    }
}
