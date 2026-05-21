using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

public class GameStateManager : MonoBehaviour
{
    [SerializeField] private LevelDefinition levelDefinition;
    [SerializeField] private PlaceableInventorySet fallbackInventorySet;
    [FormerlySerializedAs("enablePlanningTileDestructionWhenNoLevelDefinition")]
    [SerializeField] private bool enableTileDestructionToolWhenNoLevelDefinition = true;
    [SerializeField] private MonoBehaviour[] phaseListeners;
    [SerializeField] private bool autoDiscoverPhaseListeners = true;

    private PlaceableInventoryRuntime inventory;

    public static GameStateManager Instance { get; private set; }

    public event Action<LevelPhase> PhaseChanged;
    public event Action InventoryChanged;

    public static Action<int, string> SceneReloadOverride { get; set; }

    public LevelPhase CurrentPhase { get; private set; } = LevelPhase.Planning;
    public LevelDefinition CurrentLevelDefinition => levelDefinition;
    public PlaceableInventoryRuntime Inventory => inventory;
    public bool CanStartExecution => inventory == null || inventory.AllRequiredPlanningItemsUsed;

    public bool IsTileDestructionToolEnabled
    {
        get
        {
            WorldDefinition worldDefinition = levelDefinition != null ? levelDefinition.WorldDefinition : null;

            if (worldDefinition != null)
            {
                return worldDefinition.EnableTileDestructionTool;
            }

            return enableTileDestructionToolWhenNoLevelDefinition;
        }
    }

    public static GameStateManager FindOrCreate()
    {
        if (Instance != null)
        {
            return Instance;
        }

        GameStateManager existing = FindFirstObjectByType<GameStateManager>();

        if (existing != null)
        {
            return existing;
        }

        GameObject managerObject = new GameObject("GameStateManager");
        return managerObject.AddComponent<GameStateManager>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        CurrentPhase = LevelPhase.Planning;
        inventory = new PlaceableInventoryRuntime(GetInventorySet());
        inventory.Changed += HandleInventoryChanged;
    }

    private void Start()
    {
        NotifyPhaseListeners(CurrentPhase);
        HandleInventoryChanged();
    }

    private void OnDestroy()
    {
        if (inventory != null)
        {
            inventory.Changed -= HandleInventoryChanged;
        }

        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void SetLevelDefinition(LevelDefinition definition)
    {
        levelDefinition = definition;
        RebuildInventory();
    }

    public void SetFallbackInventorySet(PlaceableInventorySet inventorySet)
    {
        fallbackInventorySet = inventorySet;

        if (levelDefinition == null)
        {
            RebuildInventory();
        }
    }

    private void RebuildInventory()
    {
        PlaceableInventoryRuntime oldInventory = inventory;

        if (oldInventory != null)
        {
            oldInventory.Changed -= HandleInventoryChanged;
        }

        inventory = new PlaceableInventoryRuntime(GetInventorySet());
        inventory.Changed += HandleInventoryChanged;
        HandleInventoryChanged();
    }

    public void RegisterListener(ILevelPhaseListener listener)
    {
        if (listener == null)
        {
            return;
        }

        listener.OnLevelPhaseChanged(CurrentPhase);
    }

    public bool TryStartExecution()
    {
        if (CurrentPhase != LevelPhase.Planning || !CanStartExecution)
        {
            return false;
        }

        CurrentPhase = LevelPhase.Execution;
        NotifyPhaseListeners(CurrentPhase);
        PhaseChanged?.Invoke(CurrentPhase);
        return true;
    }

    public void ResetCurrentLevel()
    {
        Scene activeScene = SceneManager.GetActiveScene();

        if (SceneReloadOverride != null)
        {
            SceneReloadOverride.Invoke(activeScene.buildIndex, activeScene.name);
            return;
        }

        if (activeScene.buildIndex >= 0)
        {
            SceneManager.LoadScene(activeScene.buildIndex);
            return;
        }

        if (!string.IsNullOrWhiteSpace(activeScene.name))
        {
            SceneManager.LoadScene(activeScene.name);
        }
    }

    private PlaceableInventorySet GetInventorySet()
    {
        if (levelDefinition != null && levelDefinition.PlaceableInventorySet != null)
        {
            return levelDefinition.PlaceableInventorySet;
        }

        return fallbackInventorySet;
    }

    private void HandleInventoryChanged()
    {
        InventoryChanged?.Invoke();
    }

    private void NotifyPhaseListeners(LevelPhase phase)
    {
        if (phaseListeners != null)
        {
            for (int i = 0; i < phaseListeners.Length; i++)
            {
                if (phaseListeners[i] is ILevelPhaseListener listener)
                {
                    listener.OnLevelPhaseChanged(phase);
                }
            }
        }

        if (!autoDiscoverPhaseListeners)
        {
            return;
        }

        MonoBehaviour[] behaviours = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);

        for (int i = 0; i < behaviours.Length; i++)
        {
            if (behaviours[i] is ILevelPhaseListener listener)
            {
                listener.OnLevelPhaseChanged(phase);
            }
        }
    }
}
