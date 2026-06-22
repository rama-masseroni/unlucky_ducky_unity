using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class LevelHudPanel : MonoBehaviour
{
    [Header("Runtime")]
    [SerializeField] private GameStateManager gameStateManager;
    [SerializeField] private PauseMenuManager pauseMenuManager;

    [Header("Authored view")]
    [SerializeField] private TextMeshProUGUI levelTitleText;
    [SerializeField] private TextMeshProUGUI phaseIndicatorText;
    [SerializeField] private TextMeshProUGUI planningTimerText;
    [SerializeField] private RectTransform tooltipRoot;
    [SerializeField] private TextMeshProUGUI tooltipText;
    [SerializeField] private Button pauseButton;
    [SerializeField] private ResetLevelButtonController resetLevelButton;

    private GameStateManager subscribedGameStateManager;

    public void Configure(GameStateManager manager, PauseMenuManager pauseManager)
    {
        UnsubscribeFromGameStateManager();
        gameStateManager = manager;
        pauseMenuManager = pauseManager;
        resetLevelButton?.SetGameStateManager(gameStateManager);
        SubscribeToGameStateManager();
        RefreshPlanningTimer();
        RefreshPhaseIndicator();
    }

    private void Awake()
    {
        if (gameStateManager == null)
        {
            gameStateManager = GameStateManager.Instance != null
                ? GameStateManager.Instance
                : FindFirstObjectByType<GameStateManager>();
        }

        if (pauseMenuManager == null)
        {
            pauseMenuManager = FindFirstObjectByType<PauseMenuManager>();
        }

        if (pauseButton != null)
        {
            pauseButton.onClick.AddListener(HandlePauseButtonClicked);
        }

        resetLevelButton?.SetGameStateManager(gameStateManager);
        HideTooltip();
        RefreshLevelTitle();
        RefreshPlanningTimer();
        RefreshPhaseIndicator();
    }

    private void Start()
    {
        RefreshLevelTitle();
        RefreshPlanningTimer();
        RefreshPhaseIndicator();
    }

    private void OnEnable()
    {
        if (gameStateManager == null)
        {
            gameStateManager = GameStateManager.Instance != null
                ? GameStateManager.Instance
                : FindFirstObjectByType<GameStateManager>();
        }

        SubscribeToGameStateManager();
    }

    private void OnDisable()
    {
        UnsubscribeFromGameStateManager();
    }

    private void OnDestroy()
    {
        if (pauseButton != null)
        {
            pauseButton.onClick.RemoveListener(HandlePauseButtonClicked);
        }
    }

    public void RefreshLevelTitle()
    {
        if (levelTitleText == null)
        {
            return;
        }

        LevelDefinition levelDefinition = gameStateManager != null
            ? gameStateManager.CurrentLevelDefinition
            : null;

        if (TryGetLevelNumbers(levelDefinition, out int worldNumber, out int levelNumber)
            || TryGetLevelNumbers(SceneManager.GetActiveScene().name, out worldNumber, out levelNumber))
        {
            levelTitleText.text = $"Mundo {worldNumber} - Nivel {levelNumber}";
        }
        else if (levelDefinition != null)
        {
            levelTitleText.text = levelDefinition.LevelName;
        }
    }

    public void RefreshPlanningTimer()
    {
        if (planningTimerText == null)
        {
            return;
        }

        if (gameStateManager == null || !gameStateManager.HasPlanningTimeLimit)
        {
            planningTimerText.gameObject.SetActive(false);
            return;
        }

        planningTimerText.gameObject.SetActive(true);
        planningTimerText.text = FormatPlanningTime(gameStateManager.RemainingPlanningSeconds);
    }

    public void RefreshPhaseIndicator()
    {
        if (phaseIndicatorText == null)
        {
            return;
        }

        LevelPhase phase = gameStateManager != null
            ? gameStateManager.CurrentPhase
            : LevelPhase.Planning;

        phaseIndicatorText.text = phase == LevelPhase.Execution
            ? "Fase: Ejecuci\u00f3n"
            : "Fase: Planificaci\u00f3n";
    }

    public void ShowTooltip(string message, RectTransform source)
    {
        if (tooltipRoot == null || tooltipText == null || source == null)
        {
            return;
        }

        tooltipText.text = message;
        tooltipRoot.gameObject.SetActive(true);
        tooltipRoot.anchoredPosition = new Vector2(
            source.anchoredPosition.x - source.sizeDelta.x * 0.5f,
            source.anchoredPosition.y - source.sizeDelta.y - 10f);
    }

    public void HideTooltip()
    {
        if (tooltipRoot != null)
        {
            tooltipRoot.gameObject.SetActive(false);
        }
    }

    private void HandlePauseButtonClicked()
    {
        if (pauseMenuManager == null)
        {
            pauseMenuManager = FindFirstObjectByType<PauseMenuManager>();
        }

        pauseMenuManager?.PauseButton();
    }

    private void HandlePlanningTimerChanged(float _)
    {
        RefreshPlanningTimer();
    }

    private void HandlePhaseChanged(LevelPhase _)
    {
        RefreshPhaseIndicator();
    }

    private void SubscribeToGameStateManager()
    {
        if (gameStateManager == null || subscribedGameStateManager == gameStateManager)
        {
            return;
        }

        UnsubscribeFromGameStateManager();
        gameStateManager.PlanningTimerChanged += HandlePlanningTimerChanged;
        gameStateManager.PhaseChanged += HandlePhaseChanged;
        subscribedGameStateManager = gameStateManager;
    }

    private void UnsubscribeFromGameStateManager()
    {
        if (subscribedGameStateManager == null)
        {
            return;
        }

        subscribedGameStateManager.PlanningTimerChanged -= HandlePlanningTimerChanged;
        subscribedGameStateManager.PhaseChanged -= HandlePhaseChanged;
        subscribedGameStateManager = null;
    }

    private static bool TryGetLevelNumbers(LevelDefinition levelDefinition, out int worldNumber, out int levelNumber)
    {
        worldNumber = 0;
        levelNumber = 0;

        return levelDefinition != null
            && (TryGetLevelNumbers(levelDefinition.LevelId, out worldNumber, out levelNumber)
                || TryGetLevelNumbers(levelDefinition.name, out worldNumber, out levelNumber));
    }

    private static bool TryGetLevelNumbers(string levelId, out int worldNumber, out int levelNumber)
    {
        worldNumber = 0;
        levelNumber = 0;

        if (string.IsNullOrWhiteSpace(levelId))
        {
            return false;
        }

        string[] parts = levelId.Split('_');
        return parts.Length >= 3
            && int.TryParse(parts[1], out worldNumber)
            && int.TryParse(parts[2], out levelNumber);
    }

    private static string FormatPlanningTime(float seconds)
    {
        int totalSeconds = Mathf.CeilToInt(Mathf.Max(0f, seconds));
        return $"{totalSeconds / 60:00}:{totalSeconds % 60:00}";
    }
}
