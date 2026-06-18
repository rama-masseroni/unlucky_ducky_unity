using UnityEngine;
using UnityEngine.UI;

public class TutorialTooltipView : MonoBehaviour
{
    [SerializeField] private RectTransform root;
    [SerializeField] private Text messageText;
    [SerializeField] private Vector2 screenOffset = new Vector2(-12f, 12f);

    public void Show(string message, RectTransform anchor, bool alignLeftCentered = false)
    {
        if (root == null || messageText == null || anchor == null) return;
        messageText.supportRichText = true;
        messageText.text = message;
        root.gameObject.SetActive(true);
        Canvas.ForceUpdateCanvases();
        Vector3[] corners = new Vector3[4];
        anchor.GetWorldCorners(corners);
        Vector3 anchorPoint = alignLeftCentered
            ? (corners[0] + corners[1]) * 0.5f
            : corners[1];
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(null, anchorPoint);
        RectTransform parent = root.parent as RectTransform;
        if (parent != null && RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screenPoint, null, out Vector2 localPoint))
        {
            if (alignLeftCentered)
                localPoint.y += (root.pivot.y - 0.5f) * root.rect.height;

            root.anchoredPosition = localPoint + new Vector2(
                screenOffset.x,
                alignLeftCentered ? 0f : screenOffset.y);
        }
    }

    public void Hide() => (root != null ? root.gameObject : gameObject).SetActive(false);

    public void ShowWorld(string message, Transform anchor)
    {
        if (root == null || messageText == null || anchor == null) return;
        messageText.supportRichText = true;
        messageText.text = message;
        root.gameObject.SetActive(true);
        Canvas.ForceUpdateCanvases();

        Renderer targetRenderer = anchor.GetComponentInChildren<Renderer>();
        Vector3 worldCenter = targetRenderer != null ? targetRenderer.bounds.center : anchor.position;
        Vector3 worldLeft = targetRenderer != null
            ? new Vector3(targetRenderer.bounds.min.x, worldCenter.y, worldCenter.z)
            : worldCenter;
        Camera worldCamera = Camera.main;
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(worldCamera, worldLeft);
        RectTransform parent = root.parent as RectTransform;
        if (parent != null && RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screenPoint, null, out Vector2 localPoint))
        {
            localPoint.y += (root.pivot.y - 0.5f) * root.rect.height;
            root.anchoredPosition = localPoint + new Vector2(screenOffset.x, 0f);
        }
    }
}
