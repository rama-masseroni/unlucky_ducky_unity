using System;
using System.Collections.Generic;
using UnityEngine;

public enum TutorialTooltipObjective
{
    Planning,
    PlaceDefinition,
    StartExecution,
    DestroyTile,
    EnvironmentInfo
}

public enum TutorialEnvironmentTarget
{
    None,
    FallingBlocks,
    SensorsAndDoors
}

[Serializable]
public class TutorialTooltipStep
{
    [SerializeField] private string id;
    [SerializeField, TextArea(2, 4)] private string message;
    [SerializeField] private TutorialTooltipObjective objective;
    [SerializeField] private PlaceableDefinition placeable;
    [SerializeField] private TutorialEnvironmentTarget environmentTarget;

    public string Id => id;
    public string Message => message;
    public TutorialTooltipObjective Objective => objective;
    public PlaceableDefinition Placeable => placeable;
    public TutorialEnvironmentTarget EnvironmentTarget => environmentTarget;
}

[CreateAssetMenu(fileName = "TutorialTooltipSequence", menuName = "Unlucky Ducky/Tutorial/Tooltip Sequence")]
public class TutorialTooltipSequence : ScriptableObject
{
    [SerializeField] private string persistenceId = "contextual_tooltips";
    [SerializeField] private List<TutorialTooltipStep> steps = new List<TutorialTooltipStep>();

    public string PersistenceId => string.IsNullOrWhiteSpace(persistenceId) ? name : persistenceId;
    public IReadOnlyList<TutorialTooltipStep> Steps => steps;
}
