using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseMenuManager : MonoBehaviour
{
    [Header("Runtime")]
    [SerializeField] private GameStateManager gameStateManager;

    [Header("Authored view")]
    [SerializeField] private GameObject container;
    [SerializeField] private GameObject pauseActions;
    [SerializeField] private GameObject optionsPanel;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button resetButton;
    [SerializeField] private Button optionsButton;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Button optionsBackButton;

    public void Configure(GameStateManager manager)
    {
        gameStateManager = manager;
    }

    private void Awake()
    {
        Bind(resumeButton, ResumeButton);
        Bind(resetButton, ResetLevelButton);
        Bind(optionsButton, OptionsButton);
        Bind(mainMenuButton, ReturnToMainMenuButton);
        Bind(optionsBackButton, BackToPauseButton);
        ShowPauseActions();

        if (container != null)
        {
            container.SetActive(false);
        }
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (container != null && container.activeSelf)
            {
                ResumeButton();
            }
            else
            {
                PauseButton();
            }
        }
    }

    public void ResumeButton()
    {
        if (container != null)
        {
            container.SetActive(false);
        }

        Time.timeScale = 1f;
    }

    public void PauseButton()
    {
        ShowPauseActions();

        if (container != null)
        {
            container.SetActive(true);
            container.transform.SetAsLastSibling();
        }

        Time.timeScale = 0f;
    }

    public void ReturnToMainMenuButton()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenuScene");
    }

    public void ResetLevelButton()
    {
        Time.timeScale = 1f;
        gameStateManager?.ResetCurrentLevel();
    }

    public void OptionsButton()
    {
        if (pauseActions != null)
        {
            pauseActions.SetActive(false);
        }

        if (optionsPanel != null)
        {
            optionsPanel.SetActive(true);
        }
    }

    public void BackToPauseButton()
    {
        ShowPauseActions();
    }

    private void ShowPauseActions()
    {
        if (pauseActions != null)
        {
            pauseActions.SetActive(true);
        }

        if (optionsPanel != null)
        {
            optionsPanel.SetActive(false);
        }
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
