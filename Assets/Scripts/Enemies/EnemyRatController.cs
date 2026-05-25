using UnityEngine;

public class EnemyRatController : GridWalkerController, IBreakable
{
    [SerializeField] private bool killsPlayerOnContact = true;

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryKillPlayer(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryKillPlayer(other);
    }

    private void TryKillPlayer(Collider2D other)
    {
        if (!killsPlayerOnContact)
        {
            return;
        }

        PlayerKillRules.TryKillPlayer(other);
    }

    public void Break()
    {
        Destroy(gameObject);
    }
}
