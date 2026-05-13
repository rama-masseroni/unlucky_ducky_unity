using UnityEngine;

public class DestructibleObject : MonoBehaviour, IBreakable
{
    [SerializeField] private GameObject destructionEffectPrefab;

    public void Break()
    {
        if (destructionEffectPrefab != null)
        {
            Instantiate(destructionEffectPrefab, transform.position, Quaternion.identity);
        }

        Destroy(gameObject);
    }
}
