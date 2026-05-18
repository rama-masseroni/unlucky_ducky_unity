using UnityEngine;
using UnityEngine.EventSystems;

public class PlacedPlaceableInstance : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private PlaceableDefinition definition;

    private BuildModePlacementController placementController;
    private bool isDragging;

    public PlaceableDefinition Definition => definition;

    public void Initialize(PlaceableDefinition placeableDefinition, BuildModePlacementController controller)
    {
        definition = placeableDefinition;
        placementController = controller;
    }

    private void Awake()
    {
        if (placementController == null)
        {
            placementController = FindFirstObjectByType<BuildModePlacementController>();
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (placementController == null || !placementController.BeginMove(this))
        {
            return;
        }

        isDragging = true;
        placementController.UpdateMove(eventData.position);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging || placementController == null)
        {
            return;
        }

        placementController.UpdateMove(eventData.position);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isDragging || placementController == null)
        {
            return;
        }

        isDragging = false;
        placementController.EndMove(eventData.position);
    }
}
