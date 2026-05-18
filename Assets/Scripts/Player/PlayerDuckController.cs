using UnityEngine;
using System.Collections;

public class PlayerDuckController : GridWalkerController
{
    [SerializeField] private bool resetLevelOnDeath = true;
    [SerializeField] private float deathResetDelaySeconds = 0.5f;
    [SerializeField] private GameStateManager gameStateManager;

    private bool isDead;
    private Coroutine deathResetCoroutine;

    public bool IsDead => isDead;

    public void Kill()
    {
        if (isDead)
        {
            return;
        }

        isDead = true;
        SetVisible(false);

        if (resetLevelOnDeath && deathResetCoroutine == null)
        {
            deathResetCoroutine = StartCoroutine(ResetLevelAfterDeath());
        }

        if (Body != null)
        {
            Body.linearVelocity = Vector2.zero;
        }
    }

    protected override void FixedUpdate()
    {
        if (isDead)
        {
            if (Body != null)
            {
                Body.linearVelocity = Vector2.zero;
            }

            return;
        }

        base.FixedUpdate();
    }

    private IEnumerator ResetLevelAfterDeath()
    {
        if (deathResetDelaySeconds > 0f)
        {
            yield return new WaitForSeconds(deathResetDelaySeconds);
        }

        GameStateManager resolvedGameStateManager = GetGameStateManager();

        if (resolvedGameStateManager != null)
        {
            resolvedGameStateManager.ResetCurrentLevel();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private GameStateManager GetGameStateManager()
    {
        if (gameStateManager == null)
        {
            gameStateManager = GameStateManager.FindOrCreate();
        }

        return gameStateManager;
    }

    private void SetVisible(bool visible)
    {
        SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>();

        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].enabled = visible;
        }

        Collider2D[] colliders = GetComponentsInChildren<Collider2D>();

        for (int i = 0; i < colliders.Length; i++)
        {
            colliders[i].enabled = visible;
        }
    }
}
