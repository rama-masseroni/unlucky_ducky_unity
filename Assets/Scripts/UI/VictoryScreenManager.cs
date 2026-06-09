using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class VictoryScreenManager : MonoBehaviour
{
    private const string MainMenuSceneName = "MainMenuScene";

    [SerializeField] private GameObject container;
    [SerializeField] private Button continueButton;
    [SerializeField] private Button retryButton;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private GameStateManager gameStateManager;

    private string nextSceneName;
    private Action<string> continueAction;

    public bool IsVisible => container != null && container.activeSelf;
    public string NextSceneName => nextSceneName;

    public static VictoryScreenManager FindExisting()
    {
        VictoryScreenManager manager = FindFirstObjectByType<VictoryScreenManager>(FindObjectsInactive.Include);

        if (manager == null)
        {
            Debug.LogError("VictoryScreenManager is missing. Add UI_LevelRoot to the level Canvas.");
        }

        return manager;
    }

    public void Configure(GameStateManager manager)
    {
        gameStateManager = manager;
    }

    private void Awake()
    {
        Bind(continueButton, ContinueButton);
        Bind(retryButton, RetryButton);
        Bind(mainMenuButton, ReturnToMainMenuButton);
        Hide();
    }

    public void Show(string sceneName, Action<string> onContinue)
    {
        nextSceneName = sceneName;
        continueAction = onContinue;

        if (container == null)
        {
            Debug.LogError("Victory screen prefab has no container reference.", this);
            return;
        }

        container.SetActive(true);
        container.transform.SetAsLastSibling();
        Time.timeScale = 0f;
    }

    public void ContinueButton()
    {
        Time.timeScale = 1f;
        Hide();

        if (string.IsNullOrWhiteSpace(nextSceneName))
        {
            SceneManager.LoadScene(MainMenuSceneName);
        }
        else if (continueAction != null)
        {
            continueAction.Invoke(nextSceneName);
        }
        else
        {
            SceneManager.LoadScene(nextSceneName);
        }
    }

    public void RetryButton()
    {
        Time.timeScale = 1f;
        Hide();
        gameStateManager?.ResetCurrentLevel();
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
