using UnityEngine;

public static class PlayerKillRules
{
    public static bool TryKillPlayer(Collider2D collider)
    {
        if (collider == null)
        {
            return false;
        }

        PlayerDuckController player = collider.GetComponentInParent<PlayerDuckController>();

        if (player == null || player.IsDead)
        {
            return false;
        }

        player.Kill();
        return true;
    }
}
