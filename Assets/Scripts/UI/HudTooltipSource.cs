using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(RectTransform))]
public class HudTooltipSource : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private LevelHudPanel hud;
    [SerializeField] private string message;

    public void OnPointerEnter(PointerEventData eventData)
    {
        hud?.ShowTooltip(message, (RectTransform)transform);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        hud?.HideTooltip();
    }
}
