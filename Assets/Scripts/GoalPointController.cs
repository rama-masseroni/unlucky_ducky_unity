using UnityEngine;
using UnityEngine.SceneManagement;
using System;

[RequireComponent(typeof(Collider2D))]
public class GoalPointController : MonoBehaviour
{
    [SerializeField] private GameStateManager gameStateManager;

    private bool hasCompletedLevel;

    public static Action<string> SceneLoadOverride { get; set; }

    private void Awake()
    {
        Collider2D goalCollider = GetComponent<Collider2D>();

        if (goalCollider != null)
        {
            goalCollider.isTrigger = true;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerDuckController duck = other != null ? other.GetComponentInParent<PlayerDuckController>() : null;
        TryCompleteLevel(duck);
    }

    public bool TryCompleteLevel(PlayerDuckController duck)
    {
        if (hasCompletedLevel || duck == null || duck.IsDead)
        {
            return false;
        }

        GameStateManager resolvedGameStateManager = GetGameStateManager();

        if (resolvedGameStateManager == null || resolvedGameStateManager.CurrentPhase != LevelPhase.Execution)
        {
            return false;
        }

        LevelDefinition levelDefinition = resolvedGameStateManager.CurrentLevelDefinition;

        if (levelDefinition == null || string.IsNullOrWhiteSpace(levelDefinition.NextSceneName))
        {
            Debug.LogWarning("Goal reached, but the current LevelDefinition has no next scene configured.", this);
            return false;
        }

        hasCompletedLevel = true;
        LoadScene(levelDefinition.NextSceneName);
        return true;
    }

    protected virtual void LoadScene(string sceneName)
    {
        if (SceneLoadOverride != null)
        {
            SceneLoadOverride.Invoke(sceneName);
            return;
        }

        SceneManager.LoadScene(sceneName);
    }

    private GameStateManager GetGameStateManager()
    {
        if (gameStateManager == null)
        {
            gameStateManager = GameStateManager.FindOrCreate();
        }

        return gameStateManager;
    }
}
