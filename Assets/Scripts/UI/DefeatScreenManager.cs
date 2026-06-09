using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class DefeatScreenManager : MonoBehaviour
{
    private const string MainMenuSceneName = "MainMenuScene";
    private const string DefaultSubtitle = "\u00a1El pato se pinch\u00f3!";
    private const string PlanningTimeoutSubtitle = "Se acab\u00f3 el tiempo de planeaci\u00f3n";

    [SerializeField] private GameObject container;
    [SerializeField] private TextMeshProUGUI subtitleText;
    [SerializeField] private Button retryButton;
    [SerializeField] private Button mainMenuButton;

    public bool IsVisible => container != null && container.activeSelf;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterDeathHandler()
    {
        PlayerDuckController.DeathScreenHandler = ShowForPlayerDeath;
        GameStateManager.PlanningTimeoutHandler = ShowForPlanningTimeout;
    }

    public static bool ShowForPlayerDeath(PlayerDuckController _)
    {
        DefeatScreenManager manager = FindOrCreate();

        if (manager == null)
        {
            return false;
        }

        manager.Show(DefaultSubtitle);
        return true;
    }

    public static bool ShowForPlanningTimeout(string message)
    {
        DefeatScreenManager manager = FindOrCreate();

        if (manager == null)
        {
            return false;
        }

        manager.Show(string.IsNullOrWhiteSpace(message) ? PlanningTimeoutSubtitle : message);
        return true;
    }

    public static DefeatScreenManager FindOrCreate()
    {
        DefeatScreenManager manager = FindFirstObjectByType<DefeatScreenManager>(FindObjectsInactive.Include);

        if (manager == null)
        {
            Debug.LogError("DefeatScreenManager is missing. Add UI_GameplayCanvas to the level scene.");
        }

        return manager;
    }

    private void Awake()
    {
        Bind(retryButton, RetryButton);
        Bind(mainMenuButton, ReturnToMainMenuButton);
        Hide();
    }

    public void Show()
    {
        Show(DefaultSubtitle);
    }

    public void Show(string subtitle)
    {
        if (subtitleText != null)
        {
            subtitleText.text = string.IsNullOrWhiteSpace(subtitle) ? DefaultSubtitle : subtitle;
        }

        if (container == null)
        {
            Debug.LogError("Defeat screen prefab has no container reference.", this);
            return;
        }

        container.SetActive(true);
        container.transform.SetAsLastSibling();
        Time.timeScale = 0f;
    }

    public void RetryButton()
    {
        Time.timeScale = 1f;
        Hide();
        GameStateManager.FindOrCreate()?.ResetCurrentLevel();
    }

    public void ReturnToMainMenuButton()
    {
        Time.timeScale = 1f;
        Hide();
        SceneManager.LoadScene(MainMenuSceneName);
    }

    private void Hide()
    {
        container?.SetActive(false);
    }

    private static void Bind(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null)
        {
            return;
        }

        button.onClick.RemoveListener(action);
        button.onClick.AddListener(action);
    }
}
