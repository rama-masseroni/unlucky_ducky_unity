using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public sealed class BackgroundCameraFitter : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;
    [SerializeField] private SpriteRenderer targetRenderer;

    private void OnEnable()
    {
        FitToCamera();
    }

    private void LateUpdate()
    {
        FitToCamera();
    }

    private void OnValidate()
    {
        if (targetRenderer == null)
        {
            targetRenderer = GetComponent<SpriteRenderer>();
        }
    }

    private void FitToCamera()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        if (targetRenderer == null)
        {
            targetRenderer = GetComponent<SpriteRenderer>();
        }

        if (targetCamera == null
            || !targetCamera.orthographic
            || targetRenderer == null
            || targetRenderer.sprite == null)
        {
            return;
        }

        Vector3 cameraPosition = targetCamera.transform.position;
        transform.position = new Vector3(cameraPosition.x, cameraPosition.y, transform.position.z);

        Vector2 spriteSize = targetRenderer.sprite.bounds.size;
        if (spriteSize.x <= 0f || spriteSize.y <= 0f)
        {
            return;
        }

        float viewportHeight = targetCamera.orthographicSize * 2f;
        float viewportWidth = viewportHeight * targetCamera.aspect;
        float worldScale = Mathf.Max(viewportWidth / spriteSize.x, viewportHeight / spriteSize.y);
        Vector3 parentScale = transform.parent != null ? transform.parent.lossyScale : Vector3.one;

        transform.localScale = new Vector3(
            DivideByNonZero(worldScale, parentScale.x),
            DivideByNonZero(worldScale, parentScale.y),
            transform.localScale.z);
    }

    private static float DivideByNonZero(float value, float divisor)
    {
        return Mathf.Abs(divisor) > Mathf.Epsilon ? value / Mathf.Abs(divisor) : value;
    }
}
