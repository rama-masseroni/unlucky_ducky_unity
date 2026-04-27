using UnityEngine;
using UnityEngine.Tilemaps;

public class TestClickToDestroy : MonoBehaviour
{
    private void OnMouseDown()
    {
        Destroy(gameObject);
    }
}
