using UnityEngine;
using UnityEngine.InputSystem;

public enum MainMenuPanelKind
{
    Splash,
    MainMenu,
    LevelSelect,
    Options,
    Credits
}

public class MainMenuNavigationController : MonoBehaviour
{
    [SerializeField] private GameObject splashPanel;
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject levelSelectPanel;
    [SerializeField] private GameObject optionsPanel;
    [SerializeField] private GameObject creditsPanel;
    [SerializeField] private bool showSplashOnStart = true;
    [SerializeField] private float splashAutoAdvanceSeconds = 1.5f;

    private MainMenuPanelKind currentPanel;
    private float splashStartedAt;

    public MainMenuPanelKind CurrentPanel => currentPanel;

    public void ConfigurePanels(
        GameObject splash,
        GameObject mainMenu,
        GameObject levelSelect,
        GameObject options,
        GameObject credits)
    {
        splashPanel = splash;
        mainMenuPanel = mainMenu;
        levelSelectPanel = levelSelect;
        optionsPanel = options;
        creditsPanel = credits;
    }

    private void Start()
    {
        if (showSplashOnStart && splashPanel != null)
        {
            splashStartedAt = Time.unscaledTime;
            ShowPanel(MainMenuPanelKind.Splash);
            return;
        }

        ShowMainMenu();
    }

    private void Update()
    {
        if (currentPanel == MainMenuPanelKind.Splash)
        {
            if (Time.unscaledTime - splashStartedAt >= splashAutoAdvanceSeconds || HasConfirmInput())
            {
                ShowMainMenu();
            }

            return;
        }

        if (HasBackInput() && currentPanel != MainMenuPanelKind.MainMenu)
        {
            ShowMainMenu();
        }
    }

    public void ShowMainMenu()
    {
        ShowPanel(MainMenuPanelKind.MainMenu);
    }

    public void ShowLevelSelect()
    {
        ShowPanel(MainMenuPanelKind.LevelSelect);
    }

    public void ShowOptions()
    {
        ShowPanel(MainMenuPanelKind.Options);
    }

    public void ShowCredits()
    {
        ShowPanel(MainMenuPanelKind.Credits);
    }

    public void ExitGame()
    {
        Application.Quit();
    }

    public void ShowPanel(MainMenuPanelKind panel)
    {
        currentPanel = panel;
        SetActive(splashPanel, panel == MainMenuPanelKind.Splash);
        SetActive(mainMenuPanel, panel == MainMenuPanelKind.MainMenu);
        SetActive(levelSelectPanel, panel == MainMenuPanelKind.LevelSelect);
        SetActive(optionsPanel, panel == MainMenuPanelKind.Options);
        SetActive(creditsPanel, panel == MainMenuPanelKind.Credits);
    }

    private static void SetActive(GameObject panel, bool active)
    {
        if (panel != null)
        {
            panel.SetActive(active);
        }
    }

    private static bool HasConfirmInput()
    {
        return Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame
            || Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
    }

    private static bool HasBackInput()
    {
        return Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame
            || Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame;
    }
}
