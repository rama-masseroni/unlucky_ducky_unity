using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class PlaceableInventoryPanel : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private PlaceableInventorySet inventorySet;
    [SerializeField] private BuildModePlacementController placementController;
    [SerializeField] private GameStateManager gameStateManager;

    [Header("Authored view")]
    [SerializeField] private RectTransform slotsRoot;
    [SerializeField] private VerticalLayoutGroup slotsLayout;
    [SerializeField] private PlaceableInventorySlotView slotPrefab;
    [SerializeField] private StartExecutionButtonController startExecutionButton;
    [SerializeField] private RectTransform panelRectTransform;
    [SerializeField] private VerticalLayoutGroup panelLayout;

    private readonly List<PlaceableInventorySlotView> slotViews = new List<PlaceableInventorySlotView>();
    private PlaceableInventorySlotView selectedSlot;
    private PlaceableInventoryRuntime runtimeInventory;
    private Vector2 defaultAnchoredPosition;
    private bool hasDefaultAnchoredPosition;

    public PlaceableDefinition SelectedDefinition => selectedSlot != null ? selectedSlot.Definition : null;

    public void Configure(GameStateManager manager, BuildModePlacementController controller)
    {
        gameStateManager = manager;
        placementController = controller;
        startExecutionButton?.SetGameStateManager(gameStateManager);
    }

    private void Awake()
    {
        if (gameStateManager == null)
        {
            gameStateManager = GameStateManager.Instance != null
                ? GameStateManager.Instance
                : FindFirstObjectByType<GameStateManager>();
        }

        if (gameStateManager != null && inventorySet != null)
        {
            gameStateManager.SetFallbackInventorySet(inventorySet);
        }

        if (placementController == null)
        {
            placementController = FindFirstObjectByType<BuildModePlacementController>();
        }

        if (panelRectTransform == null)
        {
            panelRectTransform = GetComponent<RectTransform>();
        }

        if (panelLayout == null)
        {
            panelLayout = GetComponent<VerticalLayoutGroup>();
        }

        if (slotsLayout == null && slotsRoot != null)
        {
            slotsLayout = slotsRoot.GetComponent<VerticalLayoutGroup>();
        }

        RememberDefaultAnchoredPosition();
        startExecutionButton?.SetGameStateManager(gameStateManager);
    }

    private void Start()
    {
        UseRuntimeInventory();
        Rebuild();
    }

    private void OnEnable()
    {
        if (gameStateManager == null)
        {
            gameStateManager = GameStateManager.Instance != null
                ? GameStateManager.Instance
                : FindFirstObjectByType<GameStateManager>();
        }

        if (gameStateManager != null)
        {
            gameStateManager.PhaseChanged += HandlePhaseChanged;
        }
    }

    private void OnDisable()
    {
        if (gameStateManager != null)
        {
            gameStateManager.PhaseChanged -= HandlePhaseChanged;
        }

        if (runtimeInventory != null)
        {
            runtimeInventory.Changed -= HandleInventoryChanged;
        }
    }

    public void SetInventorySet(PlaceableInventorySet newInventorySet)
    {
        inventorySet = newInventorySet;
        Rebuild();
    }

    public void Rebuild()
    {
        if (slotsRoot == null || slotPrefab == null)
        {
            return;
        }

        ClearSlots();

        if (runtimeInventory == null)
        {
            return;
        }

        List<PlaceableInventoryRuntimeEntry> entries = GetVisibleEntries();

        for (int i = 0; i < entries.Count; i++)
        {
            PlaceableInventorySlotView slotView = Instantiate(slotPrefab, slotsRoot);
            slotView.Bind(entries[i], placementController);
            slotView.Clicked.AddListener(SelectSlot);
            slotViews.Add(slotView);
        }
    }

    public bool ContainsScreenPoint(Vector2 screenPosition)
    {
        return panelRectTransform != null
            && RectTransformUtility.RectangleContainsScreenPoint(panelRectTransform, screenPosition);
    }

    public void SetDynamicPlanningCameraInset(bool active, float insetPixels)
    {
        if (panelRectTransform == null)
        {
            return;
        }

        RememberDefaultAnchoredPosition();
        panelRectTransform.anchoredPosition = active
            ? defaultAnchoredPosition + Vector2.left * Mathf.Max(0f, insetPixels)
            : defaultAnchoredPosition;
    }

    public bool TryReturnOne(PlaceableDefinition definition)
    {
        return runtimeInventory != null && runtimeInventory.TryReturnOne(definition);
    }

    private void SelectSlot(PlaceableInventorySlotView slotView)
    {
        selectedSlot = slotView;

        for (int i = 0; i < slotViews.Count; i++)
        {
            slotViews[i].SetSelected(slotViews[i] == selectedSlot);
        }
    }

    private void ClearSlots()
    {
        for (int i = 0; i < slotViews.Count; i++)
        {
            if (slotViews[i] != null)
            {
                slotViews[i].Clicked.RemoveListener(SelectSlot);
            }
        }

        slotViews.Clear();

        if (slotsRoot == null)
        {
            return;
        }

        for (int i = slotsRoot.childCount - 1; i >= 0; i--)
        {
            GameObject child = slotsRoot.GetChild(i).gameObject;

            if (Application.isPlaying)
            {
                Destroy(child);
            }
            else
            {
                DestroyImmediate(child);
            }
        }
    }

    private void UseRuntimeInventory()
    {
        if (runtimeInventory != null)
        {
            runtimeInventory.Changed -= HandleInventoryChanged;
        }

        runtimeInventory = gameStateManager != null && gameStateManager.Inventory != null
            ? gameStateManager.Inventory
            : new PlaceableInventoryRuntime(inventorySet);
        runtimeInventory.Changed += HandleInventoryChanged;
    }

    private List<PlaceableInventoryRuntimeEntry> GetVisibleEntries()
    {
        IReadOnlyList<PlaceableInventoryRuntimeEntry> runtimeEntries = runtimeInventory.Entries;
        List<PlaceableInventoryRuntimeEntry> entries = new List<PlaceableInventoryRuntimeEntry>();

        for (int i = 0; i < runtimeEntries.Count; i++)
        {
            PlaceableInventoryRuntimeEntry entry = runtimeEntries[i];

            if (entry != null && entry.Definition != null)
            {
                entries.Add(entry);
            }
        }

        return entries;
    }

    private void HandlePhaseChanged(LevelPhase phase)
    {
        bool planning = phase == LevelPhase.Planning;

        for (int i = 0; i < slotViews.Count; i++)
        {
            PlaceableDefinition definition = slotViews[i].Definition;
            bool allowInteraction = definition != null
                && definition.UseMode == PlaceableUseMode.ExecutionClickToDestroyTile
                ? phase == LevelPhase.Execution
                : planning;

            slotViews[i].SetInteractionAllowed(allowInteraction);
        }
    }

    private void RememberDefaultAnchoredPosition()
    {
        if (!hasDefaultAnchoredPosition && panelRectTransform != null)
        {
            defaultAnchoredPosition = panelRectTransform.anchoredPosition;
            hasDefaultAnchoredPosition = true;
        }
    }

    private void HandleInventoryChanged()
    {
        for (int i = 0; i < slotViews.Count; i++)
        {
            slotViews[i].RefreshFromRuntimeEntry();
        }
    }

}
