using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PlaceableInventorySlotView : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Authored view")]
    [SerializeField] private Image background;
    [SerializeField] private Button button;
    [SerializeField] private LayoutElement slotLayout;
    [SerializeField] private HorizontalLayoutGroup contentLayout;
    [SerializeField] private Image icon;
    [SerializeField] private LayoutElement iconLayout;
    [SerializeField] private LayoutElement textBlockLayout;
    [SerializeField] private VerticalLayoutGroup textBlock;
    [SerializeField] private Text nameText;
    [SerializeField] private LayoutElement nameLayout;
    [SerializeField] private Text amountText;
    [SerializeField] private LayoutElement amountLayout;

    private PlaceableDefinition definition;
    private PlaceableInventoryRuntimeEntry inventoryEntry;
    private BuildModePlacementController placementController;
    private int amount;
    private bool isDragging;
    private bool interactionsAllowed = true;

    public UnityEvent<PlaceableInventorySlotView> Clicked { get; } = new UnityEvent<PlaceableInventorySlotView>();
    public PlaceableDefinition Definition => definition;
    public int Amount => amount;

    private void Awake()
    {
        if (button != null)
        {
            button.onClick.AddListener(NotifyClicked);
        }
    }

    private void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(NotifyClicked);
        }
    }

    public void Bind(
        PlaceableInventoryRuntimeEntry entry,
        BuildModePlacementController controller)
    {
        inventoryEntry = entry;
        definition = entry != null ? entry.Definition : null;
        placementController = controller;
        amount = entry != null ? entry.Amount : 0;
        gameObject.name = definition != null ? definition.DisplayName : "InventorySlot";

        if (icon != null)
        {
            icon.sprite = definition != null ? definition.Icon : null;
            icon.color = icon.sprite != null ? Color.white : new Color(0.2f, 0.2f, 0.2f, 1f);
        }

        if (nameText != null)
        {
            nameText.text = definition != null ? definition.DisplayName : string.Empty;
        }

        RefreshAmount();
    }

    public void RefreshFromRuntimeEntry()
    {
        if (inventoryEntry != null)
        {
            amount = inventoryEntry.Amount;
        }

        RefreshAmount();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!CanStartDrag())
        {
            return;
        }

        isDragging = true;
        Clicked.Invoke(this);
        placementController.BeginDrag(definition);
        placementController.UpdateDrag(eventData.position);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (isDragging && placementController != null)
        {
            placementController.UpdateDrag(eventData.position);
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isDragging || placementController == null)
        {
            return;
        }

        isDragging = false;
        bool placed = placementController.EndDrag();

        if (placed && inventoryEntry != null && inventoryEntry.TryConsumeOne())
        {
            RefreshFromRuntimeEntry();
        }
    }

    public void SetSelected(bool isSelected)
    {
        if (background != null)
        {
            background.color = isSelected
                ? new Color(0.86f, 0.92f, 1f, 1f)
                : Color.white;
        }
    }

    public void SetInteractionAllowed(bool allowed)
    {
        interactionsAllowed = allowed;
        RefreshAmount();
    }

    private void NotifyClicked()
    {
        if (definition != null && definition.UseMode == PlaceableUseMode.DragToPlace)
        {
            Clicked.Invoke(this);
        }
    }

    private bool CanStartDrag()
    {
        return amount > 0
            && interactionsAllowed
            && definition != null
            && definition.UseMode == PlaceableUseMode.DragToPlace
            && definition.Prefab != null
            && placementController != null
            && placementController.CanUseBuildMode();
    }

    private void RefreshAmount()
    {
        if (inventoryEntry != null)
        {
            amount = inventoryEntry.Amount;
        }

        if (amountText != null)
        {
            amountText.text = amount.ToString();
        }

        if (button != null)
        {
            button.interactable = interactionsAllowed && amount > 0;
        }
    }

}
