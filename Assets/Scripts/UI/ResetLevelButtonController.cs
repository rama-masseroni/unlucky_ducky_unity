using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class ResetLevelButtonController : MonoBehaviour
{
    [SerializeField] private GameStateManager gameStateManager;
    [SerializeField] private string labelText = "R";

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

        button.onClick.AddListener(ResetLevel);
        Refresh();
    }

    public void SetGameStateManager(GameStateManager manager)
    {
        gameStateManager = manager;
        Refresh();
    }

    private void ResetLevel()
    {
        if (gameStateManager == null)
        {
            gameStateManager = GameStateManager.FindOrCreate();
        }

        if (gameStateManager != null)
        {
            gameStateManager.ResetCurrentLevel();
        }
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

        if (button != null)
        {
            button.interactable = true;
        }

        if (label != null)
        {
            label.text = labelText;
        }
    }
}
