using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PlaceableInventorySlotView : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private const float BaseIconSize = 68f;
    private const float MinimumIconSize = 24f;

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
        BuildModePlacementController controller,
        float slotHeight,
        float layoutScale)
    {
        inventoryEntry = entry;
        definition = entry != null ? entry.Definition : null;
        placementController = controller;
        amount = entry != null ? entry.Amount : 0;
        gameObject.name = definition != null ? definition.DisplayName : "InventorySlot";

        ApplyLayout(slotHeight, Mathf.Max(0.01f, layoutScale));

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

    private void ApplyLayout(float slotHeight, float scale)
    {
        if (slotLayout != null)
        {
            slotLayout.preferredHeight = slotHeight;
            slotLayout.minHeight = slotHeight;
        }

        if (contentLayout != null)
        {
            contentLayout.padding = new RectOffset(
                ScaledInt(10f, scale),
                ScaledInt(10f, scale),
                ScaledInt(8f, scale),
                ScaledInt(8f, scale));
            contentLayout.spacing = Scaled(10f, scale);
        }

        if (iconLayout != null)
        {
            float iconSize = Mathf.Max(MinimumIconSize, BaseIconSize * scale);
            iconLayout.minWidth = iconSize;
            iconLayout.preferredWidth = iconSize;
            iconLayout.preferredHeight = Mathf.Min(iconSize, Mathf.Max(8f, slotHeight - Scaled(16f, scale)));
        }

        if (textBlock != null)
        {
            textBlock.spacing = Scaled(4f, scale);
        }

        if (textBlockLayout != null)
        {
            textBlockLayout.minWidth = Scaled(72f, scale);
            textBlockLayout.preferredWidth = Scaled(88f, scale);
        }

        ConfigureText(nameText, nameLayout, 12, true, scale);
        ConfigureText(amountText, amountLayout, 18, false, scale);
    }

    private static void ConfigureText(Text text, LayoutElement layout, int baseSize, bool bold, float scale)
    {
        int fontSize = Mathf.Max(bold ? 7 : 9, Mathf.RoundToInt(baseSize * scale));

        if (text != null)
        {
            text.fontSize = fontSize;
        }

        if (layout != null)
        {
            layout.preferredHeight = fontSize + Mathf.Max(4f, 10f * scale);
        }
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

    private static float Scaled(float value, float scale)
    {
        return Mathf.Max(1f, value * scale);
    }

    private static int ScaledInt(float value, float scale)
    {
        return Mathf.Max(1, Mathf.RoundToInt(value * scale));
    }
}
