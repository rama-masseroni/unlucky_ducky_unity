using UnityEngine;

public class DestructibleBlock : MonoBehaviour, IBreakable
{
    [Tooltip("Efecto visual o sonido opcional al destruirse")]
    [SerializeField] private GameObject destructionEffectPrefab;

    public void Break()
    {
        // 1. Mostrar efecto visual / sonido si lo hay
        if (destructionEffectPrefab != null)
        {
            Instantiate(destructionEffectPrefab, transform.position, Quaternion.identity);
        }

        // 2. Aquí a futuro le avisarías a la Grid que esta celda quedó libre
        // Ejemplo: LevelGrid.Instance.ClearCell(transform.position);

        // 3. Destruir el GameObject
        Destroy(gameObject);
    }
}
