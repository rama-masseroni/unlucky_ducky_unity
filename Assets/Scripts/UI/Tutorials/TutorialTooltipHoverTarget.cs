using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class TutorialTooltipHoverTarget : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public event Action Entered;
    public event Action Exited;
    public void OnPointerEnter(PointerEventData eventData) => Entered?.Invoke();
    public void OnPointerExit(PointerEventData eventData) => Exited?.Invoke();
}
