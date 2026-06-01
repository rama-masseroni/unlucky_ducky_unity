using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class ResetLevelButtonController : MonoBehaviour
{
    [SerializeField] private GameStateManager gameStateManager;
    [SerializeField] private string labelText = "\u21bb";

    private Button button;
    private Text legacyLabel;
    private TextMeshProUGUI label;

    private void Awake()
    {
        button = GetComponent<Button>();
        legacyLabel = GetComponentInChildren<Text>();
        label = GetComponentInChildren<TextMeshProUGUI>();

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
            label = GetComponentInChildren<TextMeshProUGUI>();
        }

        if (legacyLabel == null)
        {
            legacyLabel = GetComponentInChildren<Text>();
        }

        if (button != null)
        {
            button.interactable = true;
        }

        if (label != null)
        {
            label.text = labelText;
        }

        if (legacyLabel != null)
        {
            legacyLabel.text = labelText;
        }
    }
}
