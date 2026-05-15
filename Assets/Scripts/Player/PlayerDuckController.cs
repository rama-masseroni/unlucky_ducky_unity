using UnityEngine;

public class PlayerDuckController : GridWalkerController
{
    private bool isDead;

    public bool IsDead => isDead;

    public void Kill()
    {
        if (isDead)
        {
            return;
        }

        isDead = true;
        StopMovement();
        Destroy(gameObject);
    }
}
