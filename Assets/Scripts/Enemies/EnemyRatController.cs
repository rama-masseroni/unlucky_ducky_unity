using UnityEngine;

public class EnemyRatController : GridWalkerController
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
        if (!killsPlayerOnContact || other == null)
        {
            return;
        }

        PlayerDuckController player = other.GetComponentInParent<PlayerDuckController>();

        if (player == null || player.IsDead)
        {
            return;
        }

        player.Kill();
    }


}
