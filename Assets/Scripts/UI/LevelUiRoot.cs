using UnityEngine;

[DefaultExecutionOrder(-1000)]
[RequireComponent(typeof(RectTransform))]
public sealed class LevelUiRoot : MonoBehaviour
{
    [SerializeField] private LevelHudPanel hud;
    [SerializeField] private PlaceableInventoryPanel inventoryPanel;
    [SerializeField] private PauseMenuManager pauseMenu;
    [SerializeField] private VictoryScreenManager victoryScreen;
    [SerializeField] private DefeatScreenManager defeatScreen;

    private void Awake()
    {
        GameStateManager gameStateManager = GameStateManager.Instance != null
            ? GameStateManager.Instance
            : FindFirstObjectByType<GameStateManager>();
        BuildModePlacementController placementController = FindFirstObjectByType<BuildModePlacementController>();

        if (gameStateManager == null)
        {
            Debug.LogError("LevelUiRoot requires a GameStateManager in the level scene.", this);
        }

        hud?.Configure(gameStateManager, pauseMenu);
        inventoryPanel?.Configure(gameStateManager, placementController);
        pauseMenu?.Configure(gameStateManager);
        victoryScreen?.Configure(gameStateManager);
        defeatScreen?.Configure(gameStateManager);
    }
}
