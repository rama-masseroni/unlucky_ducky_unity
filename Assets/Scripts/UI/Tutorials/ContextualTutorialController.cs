using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.Tilemaps;
using System;

public class ContextualTutorialController : MonoBehaviour
{
    [SerializeField] private TutorialTooltipSequence sequence;
    [SerializeField] private TutorialTooltipView tooltip;
    [SerializeField] private PlaceableInventoryPanel inventoryPanel;
    [SerializeField] private BuildModePlacementController placementController;
    [SerializeField] private GameStateManager gameStateManager;
    [SerializeField] private RectTransform executeButton;
    [SerializeField, Min(0.5f)] private float automaticDisplaySeconds = 4f;

    private static readonly HashSet<string> completedSteps = new HashSet<string>();
    private readonly Dictionary<TutorialTooltipStep, RectTransform> anchors = new Dictionary<TutorialTooltipStep, RectTransform>();
    private readonly Dictionary<TutorialTooltipStep, Transform> worldAnchors = new Dictionary<TutorialTooltipStep, Transform>();
    private readonly HashSet<TutorialTooltipStep> automaticallyPresented = new HashSet<TutorialTooltipStep>();
    private TutorialTooltipStep automaticStep;
    private bool showingHover;
    private Coroutine automaticHideRoutine;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSessionState()
    {
        completedSteps.Clear();
    }

    private void Start()
    {
        if (sequence == null || tooltip == null || inventoryPanel == null) return;
        placementController ??= FindFirstObjectByType<BuildModePlacementController>();
        gameStateManager ??= GameStateManager.Instance != null ? GameStateManager.Instance : FindFirstObjectByType<GameStateManager>();
        inventoryPanel.SlotCreated += RegisterSlot;
        inventoryPanel.SlotInteracted += HandleSlotInteraction;
        if (placementController != null) placementController.PlaceablePlaced += HandlePlaced;
        if (gameStateManager != null) gameStateManager.PhaseChanged += HandlePhaseChanged;
        TilemapDestructionEvents.TileDestroyed += HandleTileDestroyed;
        foreach (TutorialTooltipStep step in sequence.Steps)
            if (IsStepAvailable(step) && step.Objective == TutorialTooltipObjective.Planning)
                RegisterAnchor(step, (RectTransform)inventoryPanel.transform);
        foreach (PlaceableInventorySlotView slot in inventoryPanel.SlotViews) RegisterSlot(slot);
        RegisterExecuteHover();
        ShowNextAutomatic();
        StartCoroutine(RegisterEnvironmentTargetsAfterSceneInitialization());
    }

    private void OnDestroy()
    {
        if (inventoryPanel != null) { inventoryPanel.SlotCreated -= RegisterSlot; inventoryPanel.SlotInteracted -= HandleSlotInteraction; }
        if (placementController != null) placementController.PlaceablePlaced -= HandlePlaced;
        if (gameStateManager != null) gameStateManager.PhaseChanged -= HandlePhaseChanged;
        TilemapDestructionEvents.TileDestroyed -= HandleTileDestroyed;
    }

    private void RegisterSlot(PlaceableInventorySlotView slot)
    {
        if (slot == null) return;
        foreach (TutorialTooltipStep step in sequence.Steps)
            if (IsStepAvailable(step) && step.Placeable != null && step.Placeable == slot.Definition)
                RegisterAnchor(step, (RectTransform)slot.transform);
    }

    private void RegisterExecuteHover()
    {
        if (executeButton == null) return;
        foreach (TutorialTooltipStep step in sequence.Steps)
            if (IsStepAvailable(step) && step.Objective == TutorialTooltipObjective.StartExecution) RegisterAnchor(step, executeButton);
    }

    private void RegisterAnchor(TutorialTooltipStep step, RectTransform anchor)
    {
        anchors[step] = anchor;
        TutorialTooltipHoverTarget hover = anchor.GetComponent<TutorialTooltipHoverTarget>() ?? anchor.gameObject.AddComponent<TutorialTooltipHoverTarget>();
        hover.Entered += () =>
        {
            showingHover = true;
            StopAutomaticHide();
            tooltip.Show(step.Message, anchor, ShouldAlignLeftCentered(step));
        };
        hover.Exited += () => { showingHover = false; ShowNextAutomatic(); };
    }

    private void RegisterEnvironmentTargets()
    {
        foreach (TutorialTooltipStep step in sequence.Steps)
        {
            if (step.EnvironmentTarget == TutorialEnvironmentTarget.FallingBlocks)
            {
                HashSet<Transform> registeredTargets = new HashSet<Transform>();
                FallingDestructibleTilemapLayer[] layers = FindObjectsByType<FallingDestructibleTilemapLayer>(FindObjectsSortMode.None);
                for (int i = 0; i < layers.Length; i++)
                {
                    FallingDestructibleTilemapLayer layer = layers[i];
                    if (layer != null && layer.Tilemap != null && layer.Tilemap.GetUsedTilesCount() > 0)
                    {
                        RegisterWorldHover(step, layer.transform);
                        registeredTargets.Add(layer.transform);
                    }
                }

                Tilemap[] tilemaps = FindObjectsByType<Tilemap>(FindObjectsSortMode.None);
                for (int i = 0; i < tilemaps.Length; i++)
                {
                    Tilemap tilemap = tilemaps[i];
                    if (tilemap == null
                        || tilemap.GetUsedTilesCount() <= 0
                        || registeredTargets.Contains(tilemap.transform)
                        || !IsFallingTilemap(tilemap))
                        continue;

                    RegisterWorldHover(step, tilemap.transform);
                }
                continue;
            }

            if (step.EnvironmentTarget == TutorialEnvironmentTarget.SensorsAndDoors)
            {
                SensorController[] sensors = FindObjectsByType<SensorController>(FindObjectsSortMode.None);
                for (int i = 0; i < sensors.Length; i++) RegisterWorldHover(step, sensors[i].transform);

                SensorDoorController[] doors = FindObjectsByType<SensorDoorController>(FindObjectsSortMode.None);
                for (int i = 0; i < doors.Length; i++) RegisterWorldHover(step, doors[i].transform);
            }
        }
    }

    private IEnumerator RegisterEnvironmentTargetsAfterSceneInitialization()
    {
        yield return null;
        RegisterEnvironmentTargets();
        if (!showingHover) ShowNextAutomatic();
    }

    private static bool IsFallingTilemap(Tilemap tilemap)
    {
        Transform current = tilemap.transform;
        while (current != null)
        {
            if (current.name.IndexOf("falling", StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
            current = current.parent;
        }
        return false;
    }

    private void RegisterWorldHover(TutorialTooltipStep step, Transform target)
    {
        if (target == null) return;
        if (!worldAnchors.ContainsKey(step)) worldAnchors[step] = target;

        TutorialTooltipHoverTarget hover = target.GetComponent<TutorialTooltipHoverTarget>() ?? target.gameObject.AddComponent<TutorialTooltipHoverTarget>();
        hover.Entered += () =>
        {
            showingHover = true;
            StopAutomaticHide();
            tooltip.ShowWorld(step.Message, target);
        };
        hover.Exited += () => { showingHover = false; ShowNextAutomatic(); };
    }

    private void HandleSlotInteraction(PlaceableInventorySlotView slot) => CompleteFirst(TutorialTooltipObjective.Planning, null);
    private void HandlePlaced(PlaceableDefinition definition) => CompleteFirst(TutorialTooltipObjective.PlaceDefinition, definition);
    private void HandlePhaseChanged(LevelPhase phase) { if (phase == LevelPhase.Execution) CompleteFirst(TutorialTooltipObjective.StartExecution, null); }
    private void HandleTileDestroyed(Tilemap tilemap, Vector3Int cell)
    {
        PlaceableDefinition selected = inventoryPanel != null ? inventoryPanel.SelectedDefinition : null;
        if (selected != null) CompleteFirst(TutorialTooltipObjective.DestroyTile, selected);
    }

    private void CompleteFirst(TutorialTooltipObjective objective, PlaceableDefinition definition)
    {
        foreach (TutorialTooltipStep step in sequence.Steps)
            if (IsStepAvailable(step) && step.Objective == objective && (definition == null || step.Placeable == definition)) { completedSteps.Add(Key(step)); break; }
        if (!showingHover) ShowNextAutomatic();
    }

    private void ShowNextAutomatic()
    {
        automaticStep = null;
        foreach (TutorialTooltipStep step in sequence.Steps)
            if (IsStepAvailable(step)
                && !completedSteps.Contains(Key(step))
                && (anchors.ContainsKey(step) || worldAnchors.ContainsKey(step))) { automaticStep = step; break; }
        if (automaticStep == null || automaticallyPresented.Contains(automaticStep))
        {
            tooltip.Hide();
            return;
        }

        automaticallyPresented.Add(automaticStep);
        if (worldAnchors.TryGetValue(automaticStep, out Transform worldAnchor))
            tooltip.ShowWorld(automaticStep.Message, worldAnchor);
        else
            tooltip.Show(automaticStep.Message, anchors[automaticStep], ShouldAlignLeftCentered(automaticStep));
        StopAutomaticHide();
        automaticHideRoutine = StartCoroutine(HideAutomaticAfterDelay(automaticStep));
    }

    private IEnumerator HideAutomaticAfterDelay(TutorialTooltipStep step)
    {
        yield return new WaitForSecondsRealtime(automaticDisplaySeconds);
        automaticHideRoutine = null;
        if (!showingHover && automaticStep == step) tooltip.Hide();
        if (step.Objective == TutorialTooltipObjective.EnvironmentInfo)
        {
            completedSteps.Add(Key(step));
            if (!showingHover) ShowNextAutomatic();
        }
    }

    private void StopAutomaticHide()
    {
        if (automaticHideRoutine == null) return;
        StopCoroutine(automaticHideRoutine);
        automaticHideRoutine = null;
    }

    private static bool ShouldAlignLeftCentered(TutorialTooltipStep step)
    {
        return step.Objective == TutorialTooltipObjective.PlaceDefinition
            || step.Objective == TutorialTooltipObjective.DestroyTile
            || step.Objective == TutorialTooltipObjective.StartExecution;
    }

    private bool IsStepAvailable(TutorialTooltipStep step)
    {
        if (step.Objective != TutorialTooltipObjective.Planning) return true;

        string worldId = gameStateManager?.CurrentLevelDefinition?.WorldDefinition?.WorldId;
        return string.IsNullOrWhiteSpace(worldId)
            || string.Equals(worldId, "world_01", System.StringComparison.OrdinalIgnoreCase);
    }

    private string Key(TutorialTooltipStep step) => sequence.PersistenceId + ":" + step.Id;

#if UNITY_EDITOR
    public static void ClearSessionForTests() => completedSteps.Clear();
#endif
}
